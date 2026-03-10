namespace Econyx.Application.Queries.GetDashboardStats;

using Econyx.Application.Configuration;
using Econyx.Application.Ports;
using Econyx.Domain.Entities;
using Econyx.Domain.Repositories;
using Econyx.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Options;

public sealed class GetDashboardStatsHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    private readonly IPlatformAdapter _platform;
    private readonly IPositionRepository _positionRepository;
    private readonly ITradeRepository _tradeRepository;
    private readonly IBalanceSnapshotRepository _snapshotRepository;
    private readonly TradingOptions _options;
    private static readonly DateTime StartTime = DateTime.UtcNow;

    public GetDashboardStatsHandler(
        IPlatformAdapter platform,
        IPositionRepository positionRepository,
        ITradeRepository tradeRepository,
        IBalanceSnapshotRepository snapshotRepository,
        IOptions<TradingOptions> options)
    {
        _platform = platform;
        _positionRepository = positionRepository;
        _tradeRepository = tradeRepository;
        _snapshotRepository = snapshotRepository;
        _options = options.Value;
    }

    public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var balance = await _platform.GetBalanceAsync(cancellationToken);
        var openPositions = await _positionRepository.FindAsync(p => p.IsOpen, cancellationToken);
        var allTrades = await _tradeRepository.GetAllAsync(cancellationToken);
        var snapshots = await _snapshotRepository.GetAllAsync(cancellationToken);

        var totalPnL = allTrades.Count > 0
            ? allTrades.Aggregate(Money.Zero(), (sum, t) => sum + t.PnL)
            : Money.Zero();

        var winCount = allTrades.Count(t => t.PnL.Amount > 0);
        var winRate = allTrades.Count > 0 ? (decimal)winCount / allTrades.Count : 0m;

        var bestTrade = allTrades.Count > 0
            ? allTrades.MaxBy(t => t.PnL.Amount)!.PnL
            : Money.Zero();

        var worstTrade = allTrades.Count > 0
            ? allTrades.MinBy(t => t.PnL.Amount)!.PnL
            : Money.Zero();

        var uptime = DateTime.UtcNow - StartTime;

        var totalApiCost = snapshots.Count > 0
            ? snapshots.Sum(s => s.ApiCosts.Amount)
            : 0m;
        var uptimeDays = uptime.TotalDays;
        var dailyApiCost = uptimeDays >= 1 && totalApiCost > 0
            ? totalApiCost / (decimal)uptimeDays
            : 0m;
        var runwayDays = dailyApiCost > 0
            ? (int)(balance.Amount / dailyApiCost)
            : -1;

        return new DashboardStatsDto(
            balance,
            totalPnL,
            winRate,
            Money.Zero(),
            openPositions.Count,
            allTrades.Count,
            0,
            0m,
            bestTrade,
            worstTrade,
            0m,
            uptime,
            snapshots.Count,
            dailyApiCost,
            runwayDays);
    }
}
