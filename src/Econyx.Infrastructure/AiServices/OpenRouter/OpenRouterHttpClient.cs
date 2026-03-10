using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Econyx.Application.Configuration;
using Econyx.Application.Ports;

namespace Econyx.Infrastructure.AiServices.OpenRouter;

internal sealed partial class OpenRouterHttpClient : IOpenRouterClient
{
    private readonly HttpClient _httpClient;
    private readonly OpenRouterOptions _options;
    private readonly ILogger<OpenRouterHttpClient> _logger;

    private IReadOnlyList<OpenRouterModelDto>? _cachedModels;
    private DateTime _cacheExpiry = DateTime.MinValue;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public OpenRouterHttpClient(
        HttpClient httpClient,
        IOptions<AiOptions> aiOptions,
        ILogger<OpenRouterHttpClient> logger)
    {
        _httpClient = httpClient;
        _options = aiOptions.Value.OpenRouter;
        _logger = logger;
    }

    public async Task<IReadOnlyList<OpenRouterModelDto>> GetAvailableModelsAsync(CancellationToken ct = default)
    {
        if (_cachedModels is not null && DateTime.UtcNow < _cacheExpiry)
            return _cachedModels;

        LogFetchingModels(_logger);

        try
        {
            var response = await _httpClient.GetFromJsonAsync<OpenRouterModelsResponse>(
                "models", JsonOptions, ct);

            if (response?.Data is null)
                return _cachedModels ?? [];

            var models = response.Data
                .Select(m => new OpenRouterModelDto(
                    m.Id,
                    m.Name ?? m.Id,
                    m.Description,
                    m.ContextLength,
                    ParsePrice(m.Pricing?.Prompt),
                    ParsePrice(m.Pricing?.Completion)))
                .OrderBy(m => m.Name)
                .ToList();

            _cachedModels = models;
            _cacheExpiry = DateTime.UtcNow.AddMinutes(_options.ModelCacheMinutes);

            LogModelsFetched(_logger, models.Count);

            return models;
        }
        catch (Exception ex)
        {
            LogModelsFetchFailed(_logger, ex);
            return _cachedModels ?? [];
        }
    }

    private static decimal ParsePrice(string? pricePerToken)
    {
        if (string.IsNullOrEmpty(pricePerToken) ||
            !decimal.TryParse(pricePerToken, System.Globalization.CultureInfo.InvariantCulture, out var perToken))
            return 0m;

        return perToken * 1_000_000m;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "OpenRouter model listesi aliniyor...")]
    private static partial void LogFetchingModels(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "OpenRouter'dan {Count} model alindi")]
    private static partial void LogModelsFetched(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "OpenRouter model listesi alinamadi")]
    private static partial void LogModelsFetchFailed(ILogger logger, Exception ex);
}

internal sealed record OpenRouterModelsResponse(List<OpenRouterModelData>? Data);

internal sealed record OpenRouterModelData(
    string Id,
    string? Name,
    string? Description,
    [property: JsonPropertyName("context_length")] int ContextLength,
    OpenRouterPricing? Pricing);

internal sealed record OpenRouterPricing(
    string? Prompt,
    string? Completion);
