using Econyx.Application;
using Econyx.Application.Configuration;
using Econyx.Infrastructure;
using Econyx.Worker.Services;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Econyx Worker başlatılıyor...");

    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog(config => config
        .ReadFrom.Configuration(builder.Configuration)
        .WriteTo.Console()
        .WriteTo.File("logs/econyx-.log", rollingInterval: RollingInterval.Day));

    builder.Services.Configure<TradingOptions>(
        builder.Configuration.GetSection(TradingOptions.SectionName));
    builder.Services.Configure<AiOptions>(
        builder.Configuration.GetSection(AiOptions.SectionName));
    builder.Services.Configure<PlatformOptions>(
        builder.Configuration.GetSection(PlatformOptions.SectionName));

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddHostedService<MarketScannerService>();
    builder.Services.AddHostedService<TradeExecutorService>();
    builder.Services.AddHostedService<PositionMonitorService>();
    builder.Services.AddHostedService<BalanceTrackerService>();
    builder.Services.AddHostedService<HealthMonitorService>();

    var host = builder.Build();

    await host.Services.ApplyMigrationsAsync();

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Econyx Worker beklenmedik şekilde sonlandı");
}
finally
{
    await Log.CloseAndFlushAsync();
}
