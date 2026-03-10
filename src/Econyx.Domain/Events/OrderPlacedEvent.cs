using Econyx.Core.Events;
using Econyx.Domain.Enums;
using Econyx.Domain.ValueObjects;

namespace Econyx.Domain.Events;

public sealed record OrderPlacedEvent(
    Guid OrderId,
    Guid MarketId,
    TradeSide Side,
    Money Price,
    decimal Quantity,
    TradingMode Mode) : DomainEventBase;
