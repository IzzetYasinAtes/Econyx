using Econyx.Domain.Enums;
using MediatR;

namespace Econyx.Application.Queries.GetActiveAiConfig;

public sealed record GetActiveAiConfigQuery : IRequest<ActiveAiConfigDto?>;

public sealed record ActiveAiConfigDto(
    Guid Id,
    AiProviderType Provider,
    string ModelId,
    string DisplayName,
    int MaxTokens,
    int ContextLength,
    decimal PromptPricePer1M,
    decimal CompletionPricePer1M);
