namespace Econyx.Infrastructure.AiServices;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Econyx.Application.Configuration;
using Econyx.Application.Ports;
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
        => FairValueResponseParser.Parse(json, apiCost);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cache hit for market analysis: {Question}")]
    private static partial void LogCacheHit(ILogger logger, string question);

    [LoggerMessage(Level = LogLevel.Information, Message = "Calling Claude for market analysis: {Question}")]
    private static partial void LogCallingClaude(ILogger logger, string question);

    [LoggerMessage(Level = LogLevel.Information, Message = "Claude response: {InputTokens} input, {OutputTokens} output, ${Cost}")]
    private static partial void LogClaudeResponse(ILogger logger, int inputTokens, int outputTokens, decimal cost);
}
