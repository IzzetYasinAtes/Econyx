using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Econyx.Application.Ports;
using Econyx.Domain.ValueObjects;
using Econyx.Infrastructure.AiServices.PromptTemplates;

namespace Econyx.Infrastructure.AiServices.OpenRouter;

internal sealed partial class OpenRouterAnalysisService : IAiAnalysisService
{
    private readonly IChatClient _chatClient;
    private readonly AiResponseCache _cache;
    private readonly ILogger<OpenRouterAnalysisService> _logger;

    private string _modelId = "anthropic/claude-sonnet-4-20250514";
    private int _maxTokens = 4096;
    private decimal _promptPricePer1M;
    private decimal _completionPricePer1M;

    public OpenRouterAnalysisService(
        IChatClient chatClient,
        AiResponseCache cache,
        ILogger<OpenRouterAnalysisService> logger)
    {
        _chatClient = chatClient;
        _cache = cache;
        _logger = logger;
    }

    public string ProviderName => $"OpenRouter ({_modelId})";

    public void Configure(string modelId, int maxTokens, decimal promptPricePer1M, decimal completionPricePer1M)
    {
        _modelId = modelId;
        _maxTokens = maxTokens;
        _promptPricePer1M = promptPricePer1M;
        _completionPricePer1M = completionPricePer1M;
    }

    public async Task<FairValueResult> AnalyzeMarketAsync(MarketAnalysisRequest request, CancellationToken ct = default)
    {
        var cacheKey = $"openrouter:{_modelId}:{request.Question}:{string.Join(",", request.CurrentPrices)}";
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

        LogCallingOpenRouter(_logger, _modelId, request.Question);

        var chatOptions = new ChatOptions
        {
            MaxOutputTokens = _maxTokens,
            ModelId = _modelId
        };

        var response = await _chatClient.GetResponseAsync(messages, chatOptions, ct);

        var responseText = response.Text ?? "{}";
        var inputTokens = response.Usage?.InputTokenCount ?? 0;
        var outputTokens = response.Usage?.OutputTokenCount ?? 0;
        var cost = (inputTokens * _promptPricePer1M / 1_000_000m) +
                   (outputTokens * _completionPricePer1M / 1_000_000m);

        LogOpenRouterResponse(_logger, _modelId, inputTokens, outputTokens, cost);

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

    [LoggerMessage(Level = LogLevel.Information, Message = "Calling OpenRouter ({ModelId}) for: {Question}")]
    private static partial void LogCallingOpenRouter(ILogger logger, string modelId, string question);

    [LoggerMessage(Level = LogLevel.Information, Message = "OpenRouter ({ModelId}) response: {InputTokens} input, {OutputTokens} output, ${Cost}")]
    private static partial void LogOpenRouterResponse(ILogger logger, string modelId, long inputTokens, long outputTokens, decimal cost);
}
