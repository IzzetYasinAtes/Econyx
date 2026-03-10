namespace Econyx.Infrastructure.AiServices;

using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Econyx.Application.Configuration;
using Econyx.Application.Ports;
using Econyx.Domain.ValueObjects;
using Econyx.Infrastructure.AiServices.PromptTemplates;

internal sealed partial class OpenAiAnalysisService : IAiAnalysisService
{
    private readonly IChatClient _chatClient;
    private readonly OpenAIOptions _options;
    private readonly AiResponseCache _cache;
    private readonly ILogger<OpenAiAnalysisService> _logger;

    private const decimal InputTokenCostPer1M = 2.5m;
    private const decimal OutputTokenCostPer1M = 10.0m;

    public OpenAiAnalysisService(
        IChatClient chatClient,
        IOptions<AiOptions> aiOptions,
        AiResponseCache cache,
        ILogger<OpenAiAnalysisService> logger)
    {
        _chatClient = chatClient;
        _options = aiOptions.Value.OpenAI;
        _cache = cache;
        _logger = logger;
    }

    public string ProviderName => "OpenAI";

    public async Task<FairValueResult> AnalyzeMarketAsync(MarketAnalysisRequest request, CancellationToken ct = default)
    {
        var cacheKey = $"openai:{request.Question}:{string.Join(",", request.CurrentPrices)}";
        if (_cache.TryGet<FairValueResult>(cacheKey, out var cached) && cached is not null)
        {
            LogCacheHit(_logger, request.Question);
            return cached;
        }

        var prompt = MarketAnalysisPrompt.Build(request);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "You are an expert prediction market analyst. Always respond with valid JSON only."),
            new(ChatRole.User, prompt)
        };

        LogCallingOpenAi(_logger, request.Question);

        var chatOptions = new ChatOptions
        {
            MaxOutputTokens = _options.MaxTokens,
            ModelId = _options.Model
        };

        var response = await _chatClient.GetResponseAsync(messages, chatOptions, ct);

        var responseText = response.Text ?? "{}";
        var inputTokens = response.Usage?.InputTokenCount ?? 0;
        var outputTokens = response.Usage?.OutputTokenCount ?? 0;
        var cost = (inputTokens * InputTokenCostPer1M / 1_000_000m) +
                   (outputTokens * OutputTokenCostPer1M / 1_000_000m);

        LogOpenAiResponse(_logger, inputTokens, outputTokens, cost);

        var result = ParseResponse(responseText, cost);
        _cache.Set(cacheKey, result);
        return result;
    }

    private static FairValueResult ParseResponse(string json, decimal apiCost)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var outcomes = new List<OutcomeFairValue>();
            if (root.TryGetProperty("outcomes", out var outcomesArray))
            {
                foreach (var item in outcomesArray.EnumerateArray())
                {
                    var name = item.GetProperty("name").GetString() ?? "Unknown";
                    var fairValue = item.GetProperty("fairValue").GetDecimal();
                    fairValue = Math.Clamp(fairValue, 0m, 1m);
                    outcomes.Add(new OutcomeFairValue(name, Probability.Create(fairValue)));
                }
            }

            var confidence = root.TryGetProperty("confidence", out var confProp)
                ? Math.Clamp(confProp.GetDecimal(), 0m, 1m)
                : 0.5m;

            var reasoning = root.TryGetProperty("reasoning", out var reasonProp)
                ? reasonProp.GetString() ?? string.Empty
                : string.Empty;

            return new FairValueResult(outcomes, confidence, reasoning, apiCost);
        }
        catch (JsonException)
        {
            return new FairValueResult([], 0m, $"Failed to parse AI response: {json[..Math.Min(200, json.Length)]}", apiCost);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cache hit for market analysis: {Question}")]
    private static partial void LogCacheHit(ILogger logger, string question);

    [LoggerMessage(Level = LogLevel.Information, Message = "Calling OpenAI for market analysis: {Question}")]
    private static partial void LogCallingOpenAi(ILogger logger, string question);

    [LoggerMessage(Level = LogLevel.Information, Message = "OpenAI response: {InputTokens} input, {OutputTokens} output, ${Cost}")]
    private static partial void LogOpenAiResponse(ILogger logger, long inputTokens, long outputTokens, decimal cost);
}
