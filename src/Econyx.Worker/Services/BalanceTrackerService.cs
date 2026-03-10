namespace Econyx.Worker.Services;

using Econyx.Application.Commands.TakeSnapshot;
using Econyx.Domain.Repositories;
using MediatR;

public sealed class BalanceTrackerService : BackgroundService
{
    private static readonly TimeSpan SnapshotInterval = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BalanceTrackerService> _logger;

    public BalanceTrackerService(
        IServiceScopeFactory scopeFactory,
        ILogger<BalanceTrackerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BalanceTrackerService started — snapshot interval: {Interval} min",
            SnapshotInterval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var snapshotRepo = scope.ServiceProvider.GetRequiredService<IBalanceSnapshotRepository>();

                var result = await mediator.Send(new TakeSnapshotCommand(), stoppingToken);

                if (result.IsSuccess)
                {
                    var latest = await snapshotRepo.GetLatestAsync(stoppingToken);
                    if (latest is not null)
                    {
                        _logger.LogInformation(
                            "Balance snapshot taken — balance: {Balance}, PnL: {PnL} ({PnLPct:F2}%), " +
                            "open positions: {Open}, total trades: {Trades}, win rate: {WinRate:F1}%",
                            latest.Balance,
                            latest.TotalPnL,
                            latest.TotalPnLPercent,
                            latest.OpenPositionCount,
                            latest.TotalTrades,
                            latest.WinRate);
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to take balance snapshot: {Error}", result.Error);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during balance tracking");
            }

            await Task.Delay(SnapshotInterval, stoppingToken);
        }
    }
}
