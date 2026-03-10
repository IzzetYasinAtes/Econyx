using Econyx.Domain.Enums;
using Econyx.Domain.Repositories;
using MediatR;

namespace Econyx.Application.Queries.GetApiKeyStatus;

public sealed class GetApiKeyStatusHandler
    : IRequestHandler<GetApiKeyStatusQuery, IReadOnlyList<ApiKeyStatusDto>>
{
    private readonly IApiKeyConfigurationRepository _repository;

    public GetApiKeyStatusHandler(IApiKeyConfigurationRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ApiKeyStatusDto>> Handle(
        GetApiKeyStatusQuery request,
        CancellationToken cancellationToken)
    {
        var result = new List<ApiKeyStatusDto>();

        foreach (var provider in Enum.GetValues<AiProviderType>())
        {
            var config = await _repository.GetByProviderAsync(provider, cancellationToken);
            result.Add(new ApiKeyStatusDto(
                provider,
                config?.IsConfigured ?? false,
                config?.MaskedDisplay));
        }

        return result;
    }
}
