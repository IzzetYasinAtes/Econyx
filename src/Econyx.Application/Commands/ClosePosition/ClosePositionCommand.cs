namespace Econyx.Application.Commands.ClosePosition;

using Econyx.Core.Primitives;
using Econyx.Domain.ValueObjects;
using MediatR;

public sealed record ClosePositionCommand(Guid PositionId, Money ExitPrice) : IRequest<Result<Money>>;
