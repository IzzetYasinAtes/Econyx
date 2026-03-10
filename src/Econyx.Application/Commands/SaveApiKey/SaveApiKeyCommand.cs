using Econyx.Core.Primitives;
using Econyx.Domain.Enums;
using MediatR;

namespace Econyx.Application.Commands.SaveApiKey;

public sealed record SaveApiKeyCommand(
    AiProviderType Provider,
    string ApiKey) : IRequest<Result>;
