namespace Econyx.Application.Queries.GetDashboardStats;

using Econyx.Domain.ValueObjects;
using MediatR;

public sealed record GetDashboardStatsQuery : IRequest<DashboardStatsDto>;

public sealed record DashboardStatsDto(
    Money Balance,
    Money TotalPnL,
    decimal WinRate,
    Money ApiCosts,
    int OpenPositions,
    int TotalTrades,
    int MarketsScanned,
    decimal AvgEdge,
    Money BestTrade,
    Money WorstTrade,
    decimal SharpeRatio,
    TimeSpan Uptime,
    int CycleCount,
    decimal DailyApiCost,
    int RunwayDays);
