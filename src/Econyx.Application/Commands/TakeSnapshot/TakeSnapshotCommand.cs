namespace Econyx.Application.Commands.TakeSnapshot;

using Econyx.Core.Primitives;
using MediatR;

public sealed record TakeSnapshotCommand : IRequest<Result<Guid>>;
