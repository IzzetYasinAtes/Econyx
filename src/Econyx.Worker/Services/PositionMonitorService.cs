namespace Econyx.Worker.Services;

using Econyx.Application.Commands.ClosePosition;
using Econyx.Application.Configuration;
using Econyx.Application.Ports;
using Econyx.Domain.Entities;
using Econyx.Domain.Repositories;
using Econyx.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Options;

public sealed class PositionMonitorService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(60);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TradingOptions _tradingOptions;
    private readonly ILogger<PositionMonitorService> _logger;

    public PositionMonitorService(
        IServiceScopeFactory scopeFactory,
        IOptions<TradingOptions> tradingOptions,
        ILogger<PositionMonitorService> logger)
    {
        _scopeFactory = scopeFactory;
        _tradingOptions = tradingOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "PositionMonitorService started — SL: %{SL}, TP: %{TP}",
            _tradingOptions.StopLossPercent,
            _tradingOptions.TakeProfitPercent);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MonitorPositionsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during position monitoring");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task MonitorPositionsAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var positionRepo = scope.ServiceProvider.GetRequiredService<IPositionRepository>();
        var platform = scope.ServiceProvider.GetRequiredService<IPlatformAdapter>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var openPositions = await positionRepo.GetOpenPositionsAsync(ct);

        if (openPositions.Count == 0)
            return;

        _logger.LogDebug("Monitoring {Count} open positions", openPositions.Count);

        foreach (var position in openPositions)
        {
            try
            {
                await EvaluatePositionAsync(position, platform, mediator, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Position evaluation error — PositionId: {Id}", position.Id);
            }
        }
    }

    private async Task EvaluatePositionAsync(
        Position position,
        IPlatformAdapter platform,
        IMediator mediator,
        CancellationToken ct)
    {
        var currentProbability = await platform.GetPriceAsync(position.TokenId, ct);
        if (currentProbability is null)
        {
            _logger.LogWarning("Price unavailable for {TokenId}, skipping position {Id}", position.TokenId, position.Id);
            return;
        }

        var currentPrice = Money.Create(currentProbability.Value);
        position.UpdatePrice(currentPrice);

        var pnl = position.CalculatePnL();
        var entryAmount = position.EntryPrice.Amount * position.Quantity;

        if (entryAmount <= 0)
            return;

        var holdDuration = DateTime.UtcNow - position.CreatedAt;
        var pnlPercent = (pnl.Amount / entryAmount) * 100m;

        if (holdDuration.TotalHours >= _tradingOptions.MaxHoldHours)
        {
            _logger.LogInformation(
                "Max hold time reached — position: {Market}, PnL: {PnL}%, held: {Hours:F1}h",
                position.MarketQuestion, pnlPercent.ToString("F2"), holdDuration.TotalHours);

            await mediator.Send(new ClosePositionCommand(position.Id, currentPrice), ct);
        }
        else if (pnlPercent <= -_tradingOptions.StopLossPercent)
        {
            _logger.LogWarning(
                "Stop-loss triggered — position: {Market}, PnL: {PnL}%",
                position.MarketQuestion, pnlPercent.ToString("F2"));

            await mediator.Send(new ClosePositionCommand(position.Id, currentPrice), ct);
        }
        else if (pnlPercent >= _tradingOptions.TakeProfitPercent)
        {
            _logger.LogInformation(
                "Take-profit triggered — position: {Market}, PnL: {PnL}%",
                position.MarketQuestion, pnlPercent.ToString("F2"));

            await mediator.Send(new ClosePositionCommand(position.Id, currentPrice), ct);
        }
    }
}
