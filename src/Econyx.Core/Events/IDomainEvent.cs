using MediatR;

namespace Econyx.Core.Events;

public interface IDomainEvent : INotification
{
    DateTime OccurredOn { get; }
}
