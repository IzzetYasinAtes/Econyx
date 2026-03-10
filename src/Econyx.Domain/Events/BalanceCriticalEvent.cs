using Econyx.Core.Events;
using Econyx.Domain.ValueObjects;

namespace Econyx.Domain.Events;

public sealed record BalanceCriticalEvent(
    Money CurrentBalance,
    Money Threshold) : DomainEventBase;
