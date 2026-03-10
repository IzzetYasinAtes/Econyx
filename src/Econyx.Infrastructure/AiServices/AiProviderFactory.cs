using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Econyx.Application.Configuration;
using Econyx.Application.Ports;
using Econyx.Domain.Enums;
using Econyx.Domain.Repositories;
using Econyx.Infrastructure.AiServices.OpenRouter;

namespace Econyx.Infrastructure.AiServices;

internal sealed partial class AiProviderFactory : IAiProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AiOptions _options;
    private readonly ILogger<AiProviderFactory> _logger;

    public AiProviderFactory(
        IServiceProvider serviceProvider,
        IOptions<AiOptions> options,
        ILogger<AiProviderFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IAiAnalysisService> GetProviderAsync(CancellationToken ct = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var modelRepo = scope.ServiceProvider.GetRequiredService<IAiModelConfigurationRepository>();
        var keyRepo = scope.ServiceProvider.GetRequiredService<IApiKeyConfigurationRepository>();
        var encryptor = _serviceProvider.GetRequiredService<IApiKeyEncryptor>();

        var activeConfig = await modelRepo.GetActiveAsync(ct);

        AiProviderType provider;
        string modelId;
        int maxTokens;
        decimal promptPrice, completionPrice;

        if (activeConfig is not null)
        {
            provider = activeConfig.Provider;
            modelId = activeConfig.ModelId;
            maxTokens = activeConfig.MaxTokens;
            promptPrice = activeConfig.PromptPricePer1M;
            completionPrice = activeConfig.CompletionPricePer1M;
            LogUsingDbConfig(_logger, provider, modelId);
        }
        else
        {
            provider = Enum.TryParse<AiProviderType>(_options.Provider, true, out var parsed)
                ? parsed
                : AiProviderType.OpenRouter;
            modelId = provider switch
            {
                AiProviderType.OpenRouter => _options.OpenRouter.DefaultModel,
                AiProviderType.Anthropic => _options.Claude.Model,
                AiProviderType.OpenAI => _options.OpenAI.Model,
                _ => _options.OpenRouter.DefaultModel
            };
            maxTokens = _options.OpenRouter.MaxTokens;
            promptPrice = 0m;
            completionPrice = 0m;
            LogUsingDefaultConfig(_logger, provider);
        }

        var apiKey = await ResolveApiKeyAsync(provider, keyRepo, encryptor, ct);

        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException(
                $"{provider} API key'i bulunamadi. Dashboard > Settings sayfasindan API key giriniz.");
        }

        return ResolveProvider(provider, modelId, maxTokens, promptPrice, completionPrice, apiKey);
    }

    private async Task<string?> ResolveApiKeyAsync(
        AiProviderType provider,
        IApiKeyConfigurationRepository keyRepo,
        IApiKeyEncryptor encryptor,
        CancellationToken ct)
    {
        var keyConfig = await keyRepo.GetByProviderAsync(provider, ct);
        if (keyConfig is { IsConfigured: true })
        {
            try
            {
                return encryptor.Decrypt(keyConfig.EncryptedKey);
            }
            catch
            {
                LogKeyDecryptFailed(_logger, provider);
            }
        }

        // Environment variable'dan da dene
        return provider switch
        {
            AiProviderType.OpenRouter => Environment.GetEnvironmentVariable("OPENROUTER_API_KEY"),
            AiProviderType.Anthropic => Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"),
            AiProviderType.OpenAI => Environment.GetEnvironmentVariable("OPENAI_API_KEY"),
            _ => null
        };
    }

    private IAiAnalysisService ResolveProvider(
        AiProviderType provider, string modelId, int maxTokens,
        decimal promptPrice, decimal completionPrice, string apiKey)
    {
        return provider switch
        {
            AiProviderType.OpenRouter => ConfigureOpenRouter(modelId, maxTokens, promptPrice, completionPrice, apiKey),
            AiProviderType.Anthropic => _serviceProvider.GetRequiredService<ClaudeAnalysisService>(),
            AiProviderType.OpenAI => _serviceProvider.GetRequiredService<OpenAiAnalysisService>(),
            _ => ConfigureOpenRouter(modelId, maxTokens, promptPrice, completionPrice, apiKey)
        };
    }

    private OpenRouterAnalysisService ConfigureOpenRouter(
        string modelId, int maxTokens, decimal promptPrice, decimal completionPrice, string apiKey)
    {
        var service = _serviceProvider.GetRequiredService<OpenRouterAnalysisService>();
        service.Configure(modelId, maxTokens, promptPrice, completionPrice);
        return service;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Veritabanindan AI konfigurasyonu yuklendi: {Provider} / {ModelId}")]
    private static partial void LogUsingDbConfig(ILogger logger, AiProviderType provider, string modelId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Varsayilan AI konfigurasyonu kullaniliyor: {Provider}")]
    private static partial void LogUsingDefaultConfig(ILogger logger, AiProviderType provider);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Provider} API key DB'den decrypt edilemedi, environment variable deneniyor")]
    private static partial void LogKeyDecryptFailed(ILogger logger, AiProviderType provider);
}
