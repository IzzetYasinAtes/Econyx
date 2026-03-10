namespace Econyx.Application.Queries.GetOpenPositions;

using Econyx.Domain.Enums;
using Econyx.Domain.ValueObjects;
using MediatR;

public sealed record GetOpenPositionsQuery : IRequest<IReadOnlyList<PositionDto>>;

public sealed record PositionDto(
    Guid Id,
    Guid MarketId,
    string MarketQuestion,
    PlatformType Platform,
    TradeSide Side,
    Money EntryPrice,
    Money CurrentPrice,
    decimal Quantity,
    string StrategyName,
    Money UnrealizedPnL,
    DateTime OpenedAt);
