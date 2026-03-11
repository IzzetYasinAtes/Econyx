using System.Globalization;
using Econyx.Application;
using Econyx.Application.Configuration;
using Econyx.Dashboard;
using Econyx.Dashboard.Components;
using Econyx.Dashboard.Hubs;
using Econyx.Infrastructure;
using Microsoft.AspNetCore.Localization;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((_, _, loggerConfig) => loggerConfig
        .MinimumLevel.Information()
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
            formatProvider: CultureInfo.InvariantCulture)
        .WriteTo.File("logs/econyx-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            formatProvider: CultureInfo.InvariantCulture));

    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.Configure<TradingOptions>(
        builder.Configuration.GetSection(TradingOptions.SectionName));
    builder.Services.Configure<AiOptions>(
        builder.Configuration.GetSection(AiOptions.SectionName));
    builder.Services.Configure<PlatformOptions>(
        builder.Configuration.GetSection(PlatformOptions.SectionName));

    builder.Services.AddSignalR();
    builder.Services.AddLocalization(opts => opts.ResourcesPath = "Resources");
    builder.Services.AddControllers();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<Microsoft.AspNetCore.Components.Server.Circuits.CircuitHandler, CultureCircuitHandler>();

    builder.Services.Configure<RequestLocalizationOptions>(opts =>
    {
        var supported = new[] { new CultureInfo("en"), new CultureInfo("tr") };
        opts.DefaultRequestCulture = new RequestCulture("en");
        opts.SupportedCultures = supported;
        opts.SupportedUICultures = supported;
    });

    var app = builder.Build();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    app.UseRequestLocalization();
    app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
    app.UseHttpsRedirection();
    app.UseAntiforgery();
    app.MapStaticAssets();
    app.MapControllers();

    app.MapHub<TradingHub>("/tradinghub");

    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    await app.Services.ApplyMigrationsAsync();

    Log.Information("Econyx Dashboard starting...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Econyx Dashboard terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
