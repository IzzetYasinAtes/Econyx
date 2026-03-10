namespace Econyx.Application.Queries.GetDashboardStats;

using Econyx.Application.Configuration;
using Econyx.Application.Ports;
using Econyx.Core.Interfaces;
using Econyx.Domain.Entities;
using Econyx.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Options;

public sealed class GetDashboardStatsHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    private readonly IPlatformAdapter _platform;
    private readonly IRepository<Position, Guid> _positionRepository;
    private readonly IRepository<Trade, Guid> _tradeRepository;
    private readonly IRepository<BalanceSnapshot, Guid> _snapshotRepository;
    private readonly TradingOptions _options;
    private static readonly DateTime StartTime = DateTime.UtcNow;

    public GetDashboardStatsHandler(
        IPlatformAdapter platform,
        IRepository<Position, Guid> positionRepository,
        IRepository<Trade, Guid> tradeRepository,
        IRepository<BalanceSnapshot, Guid> snapshotRepository,
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

        var dailyApiCost = 0m;
        var runwayDays = balance.Amount > 0 && dailyApiCost > 0
            ? (int)(balance.Amount / dailyApiCost)
            : int.MaxValue;

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
