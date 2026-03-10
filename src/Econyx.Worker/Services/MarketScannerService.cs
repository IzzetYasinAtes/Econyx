namespace Econyx.Worker.Services;

using System.Diagnostics;
using Econyx.Application.Commands.PlaceOrder;
using Econyx.Application.Commands.ScanMarkets;
using Econyx.Application.Configuration;
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

    public MarketScannerService(
        IServiceScopeFactory scopeFactory,
        IOptions<TradingOptions> tradingOptions,
        ILogger<MarketScannerService> logger)
    {
        _scopeFactory = scopeFactory;
        _tradingOptions = tradingOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "MarketScannerService başlatıldı — tarama aralığı: {Interval} dk, mod: {Mode}",
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

                _logger.LogInformation(
                    "Döngü #{Cycle} — {Scanned} pazar tarandı, {Signals} sinyal bulundu, süre: {Duration:N0}ms",
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
                _logger.LogError(ex, "Döngü #{Cycle} tarama sırasında hata oluştu", cycle);
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
            _logger.LogInformation("Maksimum açık pozisyon sayısına ulaşıldı ({Max}), yeni emir verilmiyor",
                _tradingOptions.MaxOpenPositions);
            return;
        }

        var signalsToProcess = scanResult.Signals.Take(availableSlots);

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
                    _logger.LogDebug("Sinyal {Market} için pozisyon boyutu sıfır, atlanıyor", signal.MarketQuestion);
                    continue;
                }

                var cmd = new PlaceOrderCommand(signal, positionSize, _tradingOptions.Mode);
                var result = await mediator.Send(cmd, ct);

                if (result.IsSuccess)
                {
                    _logger.LogInformation(
                        "Emir verildi — pazar: {Market}, boyut: {Size}, yön: {Side}",
                        signal.MarketQuestion, positionSize, signal.RecommendedSide);
                }
                else
                {
                    _logger.LogWarning(
                        "Emir başarısız — pazar: {Market}, hata: {Error}",
                        signal.MarketQuestion, result.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sinyal işlenirken hata: {Market}", signal.MarketQuestion);
            }
        }
    }
}
