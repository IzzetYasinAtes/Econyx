namespace Econyx.Infrastructure;

using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI;
using Anthropic.SDK;
using Econyx.Application.Configuration;
using Econyx.Application.Ports;
using Econyx.Core.Interfaces;
using Econyx.Domain.Enums;
using Econyx.Domain.Repositories;
using Econyx.Infrastructure.Adapters;
using Econyx.Infrastructure.Adapters.Polymarket;
using Econyx.Infrastructure.AiServices;
using Econyx.Infrastructure.AiServices.OpenRouter;
using Econyx.Infrastructure.Persistence;
using Econyx.Infrastructure.Persistence.Repositories;
using Econyx.Infrastructure.Secrets;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddPersistence(config);
        services.AddRepositories();
        services.AddPlatformAdapters(config);
        services.AddAiServices(config);
        services.AddSecretManagers(config);

        return services;
    }

    public static async Task ApplyMigrationsAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EconyxDbContext>();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Econyx.Migrations");

        MigrationLog.CheckingMigrations(logger);

        try
        {
            var pending = await db.Database.GetPendingMigrationsAsync();
            var pendingList = pending.ToList();

            if (pendingList.Count > 0)
            {
                MigrationLog.PendingMigrationsFound(logger, pendingList.Count);
                await db.Database.MigrateAsync();
                MigrationLog.MigrationCompleted(logger);
            }
            else
            {
                MigrationLog.DatabaseUpToDate(logger);
            }
        }
        catch (Exception ex)
        {
            MigrationLog.MigrationFailed(logger, ex);
            await db.Database.EnsureCreatedAsync();
            MigrationLog.DatabaseCreated(logger);
        }
    }

    private static void AddPersistence(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<EconyxDbContext>(options =>
            options.UseSqlServer(
                config.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(EconyxDbContext).Assembly.FullName)));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }

    private static void AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IMarketRepository, MarketRepository>();
        services.AddScoped<IPositionRepository, PositionRepository>();
        services.AddScoped<ITradeRepository, TradeRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IBalanceSnapshotRepository, BalanceSnapshotRepository>();
        services.AddScoped<IAiModelConfigurationRepository, AiModelConfigurationRepository>();
        services.AddScoped<IApiKeyConfigurationRepository, ApiKeyConfigurationRepository>();
    }

    private static void AddPlatformAdapters(this IServiceCollection services, IConfiguration config)
    {
        var tradingOptions = config.GetSection(TradingOptions.SectionName).Get<TradingOptions>()
            ?? new TradingOptions();

        services.AddPolymarket(config);

        services.AddSingleton<PolymarketAdapter>();

        if (tradingOptions.Mode == TradingMode.Paper)
        {
            services.AddSingleton<IPlatformAdapter, PaperTradingAdapter>();
        }
        else
        {
            services.AddSingleton<IPlatformAdapter>(sp => sp.GetRequiredService<PolymarketAdapter>());
        }
    }

    private static void AddAiServices(this IServiceCollection services, IConfiguration config)
    {
        var aiOptions = config.GetSection(AiOptions.SectionName).Get<AiOptions>() ?? new AiOptions();

        services.AddSingleton(new AiResponseCache(TimeSpan.FromMinutes(aiOptions.CacheDurationMinutes)));

        // Anthropic direct API
        services.AddSingleton<AnthropicClient>(_ => new AnthropicClient());
        services.AddSingleton<ClaudeAnalysisService>();

        // OpenAI direct API -- keyed to avoid collision with OpenRouter's IChatClient
        services.AddKeyedSingleton<IChatClient>("openai-direct", (sp, _) =>
        {
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty;
            var client = new OpenAIClient(apiKey);
            return client.GetChatClient(aiOptions.OpenAI.Model).AsIChatClient();
        });
        services.AddSingleton<OpenAiAnalysisService>(sp =>
            new OpenAiAnalysisService(
                sp.GetRequiredKeyedService<IChatClient>("openai-direct"),
                Microsoft.Extensions.Options.Options.Create(aiOptions),
                sp.GetRequiredService<AiResponseCache>(),
                sp.GetRequiredService<ILogger<OpenAiAnalysisService>>()));

        // OpenRouter gateway -- uses OpenAI-compatible endpoint
        services.AddHttpClient<IOpenRouterClient, OpenRouterHttpClient>(client =>
        {
            client.BaseAddress = new Uri(aiOptions.OpenRouter.BaseUrl.TrimEnd('/') + "/");
            var apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") ?? string.Empty;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            client.DefaultRequestHeaders.Add("X-Title", "Econyx Trading Bot");
        });

        services.AddKeyedSingleton<IChatClient>("openrouter", (sp, _) =>
        {
            var orApiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") ?? string.Empty;
            var orClient = new OpenAIClient(
                new System.ClientModel.ApiKeyCredential(orApiKey),
                new OpenAI.OpenAIClientOptions
                {
                    Endpoint = new Uri(aiOptions.OpenRouter.BaseUrl)
                });
            return orClient.GetChatClient(aiOptions.OpenRouter.DefaultModel).AsIChatClient();
        });

        services.AddSingleton<OpenRouterAnalysisService>(sp =>
            new OpenRouterAnalysisService(
                sp.GetRequiredKeyedService<IChatClient>("openrouter"),
                sp.GetRequiredService<AiResponseCache>(),
                sp.GetRequiredService<ILogger<OpenRouterAnalysisService>>()));

        // Factory -- resolves the active provider at runtime
        services.AddSingleton<IAiProviderFactory, AiProviderFactory>();
    }

    private static void AddSecretManagers(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<IApiKeyEncryptor, ApiKeyEncryptor>();

        var useKeyVault = !string.IsNullOrEmpty(config["Azure:KeyVault:VaultUri"]);

        if (useKeyVault)
        {
            services.AddSingleton<ISecretManager, AzureKeyVaultSecretManager>();
        }
        else
        {
            services.AddSingleton<ISecretManager, DpapiSecretManager>();
        }
    }
}

internal static partial class MigrationLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Veritabani migration kontrolu yapiliyor...")]
    public static partial void CheckingMigrations(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "{Count} bekleyen migration bulundu, uygulaniyor...")]
    public static partial void PendingMigrationsFound(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Migration basariyla tamamlandi.")]
    public static partial void MigrationCompleted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Veritabani guncel, bekleyen migration yok.")]
    public static partial void DatabaseUpToDate(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Migration sirasinda hata olustu. EnsureCreated deneniyor...")]
    public static partial void MigrationFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Veritabani EnsureCreated ile olusturuldu.")]
    public static partial void DatabaseCreated(ILogger logger);
}
