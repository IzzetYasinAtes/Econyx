using Econyx.Domain.Enums;
using MediatR;

namespace Econyx.Application.Queries.GetApiKeyStatus;

public sealed record GetApiKeyStatusQuery : IRequest<IReadOnlyList<ApiKeyStatusDto>>;

public sealed record ApiKeyStatusDto(
    AiProviderType Provider,
    bool IsConfigured,
    string? MaskedKey);
