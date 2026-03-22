namespace Econyx.Worker.Services;

using System.Diagnostics;
using Econyx.Application.Commands.PlaceOrder;
using Econyx.Application.Commands.ScanMarkets;
using Econyx.Application.Configuration;
using Econyx.Application.Ports;
using Econyx.Domain.Repositories;
using Econyx.Domain.Services;
using Econyx.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Options;

public sealed class MarketScannerService : BackgroundService
{
    private static long _cycleCount;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TradingOptions _tradingOptions;
    private readonly ILogger<MarketScannerService> _logger;
    private readonly IScanStatistics _scanStatistics;

    public MarketScannerService(
        IServiceScopeFactory scopeFactory,
        IOptions<TradingOptions> tradingOptions,
        ILogger<MarketScannerService> logger,
        IScanStatistics scanStatistics)
    {
        _scopeFactory = scopeFactory;
        _tradingOptions = tradingOptions.Value;
        _logger = logger;
        _scanStatistics = scanStatistics;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "MarketScannerService started — scan interval: {Interval} min, mode: {Mode}",
            _tradingOptions.ScanIntervalMinutes,
            _tradingOptions.Mode);

        var interval = TimeSpan.FromMinutes(_tradingOptions.ScanIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            var cycle = Interlocked.Increment(ref _cycleCount);
            var sw = Stopwatch.StartNew();

            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var result = await mediator.Send(new ScanMarketsCommand(), stoppingToken);

                _scanStatistics.RecordScan(result.MarketsScanned);

                _logger.LogInformation(
                    "Cycle #{Cycle} — {Scanned} markets scanned, {Signals} signals found, duration: {Duration:N0}ms",
                    cycle, result.MarketsScanned, result.Signals.Count, sw.ElapsedMilliseconds);

                if (result.Signals.Count > 0)
                    await PlaceOrdersAsync(scope.ServiceProvider, mediator, result, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scan cycle #{Cycle}", cycle);
            }

            sw.Stop();
            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task PlaceOrdersAsync(
        IServiceProvider services,
        IMediator mediator,
        ScanMarketsResult scanResult,
        CancellationToken ct)
    {
        var snapshotRepo = services.GetRequiredService<IBalanceSnapshotRepository>();
        var positionRepo = services.GetRequiredService<IPositionRepository>();

        var latestSnapshot = await snapshotRepo.GetLatestAsync(ct);
        var balance = latestSnapshot?.Balance ?? Money.Create(_tradingOptions.InitialBalance);

        var openPositions = await positionRepo.GetOpenPositionsAsync(ct);
        var availableSlots = _tradingOptions.MaxOpenPositions - openPositions.Count;

        if (availableSlots <= 0)
        {
            _logger.LogInformation("Maximum open position limit reached ({Max}), skipping new orders",
                _tradingOptions.MaxOpenPositions);
            return;
        }

        var existingMarketIds = openPositions.Select(p => p.MarketId).ToHashSet();

        var signalsToProcess = scanResult.Signals
            .Where(s => !existingMarketIds.Contains(s.MarketId))
            .GroupBy(s => s.MarketId)
            .Select(g => g.OrderByDescending(s => s.Edge.AbsoluteValue).First())
            .Take(availableSlots);

        foreach (var signal in signalsToProcess)
        {
            try
            {
                var positionSize = RiskCalculator.CalculatePositionSize(
                    balance,
                    signal.Edge,
                    signal.FairValue,
                    _tradingOptions.MaxPositionSizePercent / 100m);

                if (positionSize.Amount <= 0)
                {
                    _logger.LogDebug("Position size is zero for signal {Market}, skipping", signal.MarketQuestion);
                    continue;
                }

                var cmd = new PlaceOrderCommand(signal, positionSize, _tradingOptions.Mode);
                var result = await mediator.Send(cmd, ct);

                if (result.IsSuccess)
                {
                    _logger.LogInformation(
                        "Order placed — market: {Market}, size: {Size}, side: {Side}",
                        signal.MarketQuestion, positionSize, signal.RecommendedSide);
                }
                else
                {
                    _logger.LogWarning(
                        "Order failed — market: {Market}, error: {Error}",
                        signal.MarketQuestion, result.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing signal: {Market}", signal.MarketQuestion);
            }
        }
    }
}
