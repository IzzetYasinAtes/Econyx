using Econyx.Core.Events;
using Econyx.Domain.ValueObjects;

namespace Econyx.Domain.Events;

public sealed record EdgeDetectedEvent(
    Guid MarketId,
    string MarketQuestion,
    Edge Edge,
    Probability FairValue,
    Probability MarketPrice) : DomainEventBase;
