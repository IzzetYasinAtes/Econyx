using Econyx.Core.Events;
using Econyx.Domain.ValueObjects;

namespace Econyx.Domain.Events;

public sealed record PositionClosedEvent(
    Guid PositionId,
    Guid MarketId,
    Money PnL,
    string StrategyName) : DomainEventBase;
