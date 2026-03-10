namespace Econyx.Application.Queries.GetTradeHistory;

using Econyx.Domain.Enums;
using Econyx.Domain.ValueObjects;
using MediatR;

public sealed record GetTradeHistoryQuery(int PageSize = 20, int Page = 1) : IRequest<TradeHistoryDto>;

public sealed record TradeHistoryDto(IReadOnlyList<TradeDto> Items, int TotalCount);

public sealed record TradeDto(
    Guid Id,
    Guid MarketId,
    string MarketQuestion,
    TradeSide Side,
    Money EntryPrice,
    Money ExitPrice,
    decimal Quantity,
    Money PnL,
    string StrategyName,
    PlatformType Platform,
    TimeSpan Duration,
    DateTime ClosedAt);
