namespace Econyx.Infrastructure.AiServices;

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Econyx.Application.Configuration;
using Econyx.Application.Ports;
using Econyx.Domain.ValueObjects;
using Econyx.Infrastructure.AiServices.PromptTemplates;

internal sealed partial class ClaudeAnalysisService : IAiAnalysisService
{
    private readonly AnthropicClient _client;
    private readonly ClaudeOptions _options;
    private readonly AiResponseCache _cache;
    private readonly ILogger<ClaudeAnalysisService> _logger;

    private const decimal InputTokenCostPer1M = 3.0m;
    private const decimal OutputTokenCostPer1M = 15.0m;

    public ClaudeAnalysisService(
        AnthropicClient client,
        IOptions<AiOptions> aiOptions,
        AiResponseCache cache,
        ILogger<ClaudeAnalysisService> logger)
    {
        _client = client;
        _options = aiOptions.Value.Claude;
        _cache = cache;
        _logger = logger;
    }

    public string ProviderName => "Claude";

    public async Task<FairValueResult> AnalyzeMarketAsync(MarketAnalysisRequest request, CancellationToken ct = default)
    {
        var cacheKey = $"claude:{request.Question}:{string.Join(",", request.CurrentPrices)}";
        if (_cache.TryGet<FairValueResult>(cacheKey, out var cached) && cached is not null)
        {
            LogCacheHit(_logger, request.Question);
            return cached;
        }

        var prompt = MarketAnalysisPrompt.Build(request);

        var parameters = new MessageParameters
        {
            Messages = [new Message(RoleType.User, prompt)],
            Model = _options.Model,
            MaxTokens = _options.MaxTokens,
            System = [new SystemMessage("You are an expert prediction market analyst. Always respond with valid JSON only.")]
        };

        LogCallingClaude(_logger, request.Question);

        var response = await _client.Messages.GetClaudeMessageAsync(parameters, ct);

        var responseText = response.Content?.FirstOrDefault()?.ToString() ?? "{}";
        var inputTokens = response.Usage?.InputTokens ?? 0;
        var outputTokens = response.Usage?.OutputTokens ?? 0;
        var cost = (inputTokens * InputTokenCostPer1M / 1_000_000m) +
                   (outputTokens * OutputTokenCostPer1M / 1_000_000m);

        LogClaudeResponse(_logger, inputTokens, outputTokens, cost);

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

    [LoggerMessage(Level = LogLevel.Information, Message = "Calling Claude for market analysis: {Question}")]
    private static partial void LogCallingClaude(ILogger logger, string question);

    [LoggerMessage(Level = LogLevel.Information, Message = "Claude response: {InputTokens} input, {OutputTokens} output, ${Cost}")]
    private static partial void LogClaudeResponse(ILogger logger, int inputTokens, int outputTokens, decimal cost);
}
