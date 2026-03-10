using Econyx.Application.Ports;
using MediatR;

namespace Econyx.Application.Queries.GetAvailableModels;

public sealed record GetAvailableModelsQuery : IRequest<IReadOnlyList<OpenRouterModelDto>>;
