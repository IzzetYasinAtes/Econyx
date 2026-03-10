using Econyx.Application.Ports;
using MediatR;

namespace Econyx.Application.Queries.GetAvailableModels;

public sealed class GetAvailableModelsHandler
    : IRequestHandler<GetAvailableModelsQuery, IReadOnlyList<OpenRouterModelDto>>
{
    private readonly IOpenRouterClient _openRouterClient;

    public GetAvailableModelsHandler(IOpenRouterClient openRouterClient)
    {
        _openRouterClient = openRouterClient;
    }

    public async Task<IReadOnlyList<OpenRouterModelDto>> Handle(
        GetAvailableModelsQuery request,
        CancellationToken cancellationToken)
    {
        return await _openRouterClient.GetAvailableModelsAsync(cancellationToken);
    }
}
