namespace Econyx.Worker.Services;

using Econyx.Application.Commands.ClosePosition;
using Econyx.Application.Configuration;
using Econyx.Application.Ports;
using Econyx.Domain.Entities;
using Econyx.Domain.Enums;
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
            "PositionMonitorService başlatıldı — SL: %{SL}, TP: %{TP}",
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
                _logger.LogError(ex, "Pozisyon izleme sırasında hata oluştu");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task MonitorPositionsAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var positionRepo = scope.ServiceProvider.GetRequiredService<IPositionRepository>();
        var marketRepo = scope.ServiceProvider.GetRequiredService<IMarketRepository>();
        var platform = scope.ServiceProvider.GetRequiredService<IPlatformAdapter>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var openPositions = await positionRepo.GetOpenPositionsAsync(ct);

        if (openPositions.Count == 0)
            return;

        _logger.LogDebug("{Count} açık pozisyon izleniyor", openPositions.Count);

        foreach (var position in openPositions)
        {
            try
            {
                await EvaluatePositionAsync(position, marketRepo, platform, mediator, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Pozisyon değerlendirme hatası — PositionId: {Id}", position.Id);
            }
        }
    }

    private async Task EvaluatePositionAsync(
        Position position,
        IMarketRepository marketRepo,
        IPlatformAdapter platform,
        IMediator mediator,
        CancellationToken ct)
    {
        var market = await marketRepo.GetByIdAsync(position.MarketId, ct);

        if (market is { Status: MarketStatus.Resolved })
        {
            var resolvedPrice = market.ResolvedOutcome is not null
                ? DetermineResolvedPrice(position.Side, market.ResolvedOutcome)
                : position.CurrentPrice;

            _logger.LogInformation(
                "Pazar çözümlendi — pozisyon kapatılıyor: {Market}", position.MarketQuestion);

            await mediator.Send(new ClosePositionCommand(position.Id, resolvedPrice), ct);
            return;
        }

        var firstOutcome = market?.Outcomes.FirstOrDefault();
        if (firstOutcome is null)
            return;

        var currentProbability = await platform.GetPriceAsync(firstOutcome.Token.Value, ct);
        var currentPrice = Money.Create(currentProbability.Value);
        position.UpdatePrice(currentPrice);

        var pnl = position.CalculatePnL();
        var entryAmount = position.EntryPrice.Amount * position.Quantity;

        if (entryAmount <= 0)
            return;

        var pnlPercent = (pnl.Amount / entryAmount) * 100m;

        if (pnlPercent <= -_tradingOptions.StopLossPercent)
        {
            _logger.LogWarning(
                "Stop-loss tetiklendi — pozisyon: {Market}, PnL: {PnL}%",
                position.MarketQuestion, pnlPercent.ToString("F2"));

            await mediator.Send(new ClosePositionCommand(position.Id, currentPrice), ct);
        }
        else if (pnlPercent >= _tradingOptions.TakeProfitPercent)
        {
            _logger.LogInformation(
                "Take-profit tetiklendi — pozisyon: {Market}, PnL: {PnL}%",
                position.MarketQuestion, pnlPercent.ToString("F2"));

            await mediator.Send(new ClosePositionCommand(position.Id, currentPrice), ct);
        }
    }

    private static Money DetermineResolvedPrice(TradeSide side, string resolvedOutcome)
    {
        var isYesResolution = resolvedOutcome.Equals("Yes", StringComparison.OrdinalIgnoreCase);
        var price = (side == TradeSide.Yes && isYesResolution) || (side == TradeSide.No && !isYesResolution)
            ? 1.00m
            : 0.00m;

        return Money.Create(price);
    }
}
