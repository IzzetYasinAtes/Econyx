namespace Econyx.Worker.Services;

using Econyx.Application.Ports;
using Econyx.Core.Interfaces;
using Econyx.Domain.Enums;
using Econyx.Domain.Repositories;
using Econyx.Domain.ValueObjects;

public sealed class TradeExecutorService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TradeExecutorService> _logger;

    public TradeExecutorService(
        IServiceScopeFactory scopeFactory,
        ILogger<TradeExecutorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TradeExecutorService started — check interval: {Interval}s", CheckInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckPendingOrdersAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking pending orders");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task CheckPendingOrdersAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var orderRepo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var platform = scope.ServiceProvider.GetRequiredService<IPlatformAdapter>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var pendingOrders = await orderRepo.GetPendingOrdersAsync(ct);

        if (pendingOrders.Count == 0)
            return;

        _logger.LogDebug("Checking {Count} pending orders", pendingOrders.Count);

        foreach (var order in pendingOrders)
        {
            try
            {
                if (order.Mode == TradingMode.Live && !string.IsNullOrEmpty(order.PlatformOrderId))
                {
                    var currentPrice = await platform.GetPriceAsync(order.PlatformOrderId, ct);
                    order.Fill(Money.Create(currentPrice.Value), order.Quantity);

                    _logger.LogInformation(
                        "Order filled (Live) — OrderId: {OrderId}, Price: {Price}",
                        order.Id, currentPrice);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking order status — OrderId: {OrderId}", order.Id);
            }
        }

        await unitOfWork.SaveChangesAsync(ct);
    }
}
