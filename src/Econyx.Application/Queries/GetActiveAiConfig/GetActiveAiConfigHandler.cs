using Econyx.Domain.Repositories;
using MediatR;

namespace Econyx.Application.Queries.GetActiveAiConfig;

public sealed class GetActiveAiConfigHandler
    : IRequestHandler<GetActiveAiConfigQuery, ActiveAiConfigDto?>
{
    private readonly IAiModelConfigurationRepository _repository;

    public GetActiveAiConfigHandler(IAiModelConfigurationRepository repository)
    {
        _repository = repository;
    }

    public async Task<ActiveAiConfigDto?> Handle(
        GetActiveAiConfigQuery request,
        CancellationToken cancellationToken)
    {
        var config = await _repository.GetActiveAsync(cancellationToken);

        if (config is null)
            return null;

        return new ActiveAiConfigDto(
            config.Id,
            config.Provider,
            config.ModelId,
            config.DisplayName,
            config.MaxTokens,
            config.ContextLength,
            config.PromptPricePer1M,
            config.CompletionPricePer1M);
    }
}
