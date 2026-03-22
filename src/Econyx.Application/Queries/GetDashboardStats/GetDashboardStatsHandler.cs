namespace Econyx.Application.Queries.GetDashboardStats;

using Econyx.Application.Ports;
using Econyx.Application.Services;
using Econyx.Domain.Repositories;
using Econyx.Domain.ValueObjects;
using MediatR;

public sealed class GetDashboardStatsHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    private readonly IPlatformAdapter _platform;
    private readonly IPositionRepository _positionRepository;
    private readonly ITradeRepository _tradeRepository;
    private readonly IBalanceSnapshotRepository _snapshotRepository;
    private readonly IScanStatistics _scanStatistics;
    private static readonly DateTime StartTime = DateTime.UtcNow;

    public GetDashboardStatsHandler(
        IPlatformAdapter platform,
        IPositionRepository positionRepository,
        ITradeRepository tradeRepository,
        IBalanceSnapshotRepository snapshotRepository,
        IScanStatistics scanStatistics)
    {
        _platform = platform;
        _positionRepository = positionRepository;
        _tradeRepository = tradeRepository;
        _snapshotRepository = snapshotRepository;
        _scanStatistics = scanStatistics;
    }

    public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var openPositions = await _positionRepository.FindAsync(p => p.IsOpen, cancellationToken);
        var allTrades = await _tradeRepository.GetAllAsync(cancellationToken);
        var snapshots = await _snapshotRepository.GetAllAsync(cancellationToken);

        var latestSnapshot = snapshots.Count > 0
            ? snapshots.OrderByDescending(s => s.CreatedAt).First()
            : null;
        var balance = latestSnapshot?.Balance ?? await _platform.GetBalanceAsync(cancellationToken);

        var (totalPnL, winRate) = TradeStatsCalculator.Calculate(allTrades);

        var bestTrade = allTrades.Count > 0
            ? allTrades.MaxBy(t => t.PnL.Amount)!.PnL
            : Money.Zero();

        var worstTrade = allTrades.Count > 0
            ? allTrades.MinBy(t => t.PnL.Amount)!.PnL
            : Money.Zero();

        var uptime = DateTime.UtcNow - StartTime;

        var sharpeRatio = CalculateSharpeRatio(allTrades);
        var avgEdge = CalculateAvgEdge(allTrades);

        return new DashboardStatsDto(
            balance,
            totalPnL,
            winRate * 100,
            openPositions.Count,
            allTrades.Count,
            _scanStatistics.TotalMarketsScanned,
            avgEdge,
            bestTrade,
            worstTrade,
            sharpeRatio,
            uptime,
            snapshots.Count);
    }

    private static decimal CalculateSharpeRatio(IReadOnlyList<Domain.Entities.Trade> trades)
    {
        if (trades.Count < 2)
            return 0m;

        var returns = trades
            .Select(t =>
            {
                var entryAmount = t.EntryPrice.Amount * t.Quantity;
                return entryAmount > 0 ? t.PnL.Amount / entryAmount : 0m;
            })
            .ToList();

        var avgReturn = returns.Average();
        var variance = returns.Sum(r => (r - avgReturn) * (r - avgReturn)) / (returns.Count - 1);
        var stdDev = (decimal)Math.Sqrt((double)variance);

        return stdDev > 0 ? Math.Round(avgReturn / stdDev, 2) : 0m;
    }

    private static decimal CalculateAvgEdge(IReadOnlyList<Domain.Entities.Trade> trades)
    {
        if (trades.Count == 0)
            return 0m;

        var edges = trades
            .Select(t =>
            {
                var entryAmount = t.EntryPrice.Amount * t.Quantity;
                return entryAmount > 0 ? t.PnL.Amount / entryAmount * 100 : 0m;
            })
            .ToList();

        return Math.Round(edges.Average(), 1);
    }
}
