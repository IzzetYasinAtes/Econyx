using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using Econyx.Application.Ports;
using Econyx.Application.Configuration;
using Econyx.Infrastructure.AiServices.PromptTemplates;

namespace Econyx.Infrastructure.AiServices.OpenRouter;

internal sealed partial class OpenRouterAnalysisService : IAiAnalysisService
{
    private readonly AiResponseCache _cache;
    private readonly ILogger<OpenRouterAnalysisService> _logger;
    private readonly string _baseUrl;

    private string _modelId = "anthropic/claude-sonnet-4-20250514";
    private string _apiKey = string.Empty;
    private int _maxTokens = 4096;
    private decimal _promptPricePer1M;
    private decimal _completionPricePer1M;

    public OpenRouterAnalysisService(
        AiResponseCache cache,
        IOptions<AiOptions> aiOptions,
        ILogger<OpenRouterAnalysisService> logger)
    {
        _cache = cache;
        _logger = logger;
        _baseUrl = aiOptions.Value.OpenRouter.BaseUrl;
    }

    public string ProviderName => $"OpenRouter ({_modelId})";

    public void Configure(string modelId, int maxTokens, decimal promptPricePer1M, decimal completionPricePer1M, string apiKey)
    {
        _modelId = modelId;
        _maxTokens = maxTokens;
        _promptPricePer1M = promptPricePer1M;
        _completionPricePer1M = completionPricePer1M;
        _apiKey = apiKey;
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

        var client = new OpenAIClient(
            new System.ClientModel.ApiKeyCredential(_apiKey),
            new OpenAIClientOptions
            {
                Endpoint = new Uri(_baseUrl)
            });

        var chatClient = client.GetChatClient(_modelId).AsIChatClient();
        var response = await chatClient.GetResponseAsync(messages, chatOptions, ct);

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
        => FairValueResponseParser.Parse(json, apiCost);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cache hit for market analysis: {Question}")]
    private static partial void LogCacheHit(ILogger logger, string question);

    [LoggerMessage(Level = LogLevel.Information, Message = "Calling OpenRouter ({ModelId}) for: {Question}")]
    private static partial void LogCallingOpenRouter(ILogger logger, string modelId, string question);

    [LoggerMessage(Level = LogLevel.Information, Message = "OpenRouter ({ModelId}) response: {InputTokens} input, {OutputTokens} output, ${Cost}")]
    private static partial void LogOpenRouterResponse(ILogger logger, string modelId, long inputTokens, long outputTokens, decimal cost);
}
