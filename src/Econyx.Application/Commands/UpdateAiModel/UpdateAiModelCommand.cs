using Econyx.Core.Primitives;
using Econyx.Domain.Enums;
using MediatR;

namespace Econyx.Application.Commands.UpdateAiModel;

public sealed record UpdateAiModelCommand(
    AiProviderType Provider,
    string ModelId,
    string DisplayName,
    int MaxTokens,
    int ContextLength,
    decimal PromptPricePer1M,
    decimal CompletionPricePer1M) : IRequest<Result>;
