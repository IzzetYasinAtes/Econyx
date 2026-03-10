namespace Econyx.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

    private static void AddPersistence(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<EconyxDbContext>(options =>
            options.UseSqlServer(
                config.GetConnectionString("Default"),
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

        if (string.Equals(aiOptions.Provider, "Claude", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<AnthropicClient>(_ => new AnthropicClient());
            services.AddSingleton<IAiAnalysisService, ClaudeAnalysisService>();
        }
        else
        {
            services.AddSingleton<IChatClient>(sp =>
            {
                var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty;
                var client = new OpenAIClient(apiKey);
                return client.GetChatClient(aiOptions.OpenAI.Model).AsIChatClient();
            });
            services.AddSingleton<IAiAnalysisService, OpenAiAnalysisService>();
        }
    }

    private static void AddSecretManagers(this IServiceCollection services, IConfiguration config)
    {
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
