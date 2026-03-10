namespace Econyx.Worker.Services;

using Econyx.Application.Configuration;
using Econyx.Application.Ports;
using Econyx.Domain.Repositories;
using Econyx.Domain.ValueObjects;
using Microsoft.Extensions.Options;

public sealed class HealthMonitorService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TradingOptions _tradingOptions;
    private readonly ILogger<HealthMonitorService> _logger;
    private readonly DateTime _startTime = DateTime.UtcNow;

    public HealthMonitorService(
        IServiceScopeFactory scopeFactory,
        IOptions<TradingOptions> tradingOptions,
        ILogger<HealthMonitorService> logger)
    {
        _scopeFactory = scopeFactory;
        _tradingOptions = tradingOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HealthMonitorService started — check interval: {Interval} min",
            CheckInterval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunHealthChecksAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during health checks");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task RunHealthChecksAsync(CancellationToken ct)
    {
        var uptime = DateTime.UtcNow - _startTime;
        var platformOk = await CheckPlatformAsync(ct);
        var dbOk = await CheckDatabaseAsync(ct);
        var balanceOk = await CheckBalanceAsync(ct);

        _logger.LogInformation(
            "Health report — uptime: {Uptime:d\\.hh\\:mm\\:ss}, platform: {Platform}, database: {Db}, balance: {Balance}",
            uptime,
            platformOk ? "OK" : "FAIL",
            dbOk ? "OK" : "FAIL",
            balanceOk ? "OK" : "CRITICAL");

        if (!balanceOk)
        {
            _logger.LogCritical(
                "Balance is below survival threshold ({Threshold} USD)! Consider halting trading.",
                _tradingOptions.SurvivalModeThresholdUsd);
        }
    }

    private async Task<bool> CheckPlatformAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var platform = scope.ServiceProvider.GetRequiredService<IPlatformAdapter>();
            await platform.GetBalanceAsync(ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Platform API health check failed");
            return false;
        }
    }

    private async Task<bool> CheckDatabaseAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var snapshotRepo = scope.ServiceProvider.GetRequiredService<IBalanceSnapshotRepository>();
            await snapshotRepo.GetLatestAsync(ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database health check failed");
            return false;
        }
    }

    private async Task<bool> CheckBalanceAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var snapshotRepo = scope.ServiceProvider.GetRequiredService<IBalanceSnapshotRepository>();
            var latest = await snapshotRepo.GetLatestAsync(ct);

            if (latest is null)
                return true;

            var threshold = Money.Create(_tradingOptions.SurvivalModeThresholdUsd);
            return latest.Balance >= threshold;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking balance");
            return false;
        }
    }
}
