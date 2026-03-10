using Econyx.Core.Interfaces;
using Econyx.Domain.Entities;
using Econyx.Domain.Enums;

namespace Econyx.Domain.Repositories;

public interface IApiKeyConfigurationRepository : IRepository<ApiKeyConfiguration, Guid>
{
    Task<ApiKeyConfiguration?> GetByProviderAsync(AiProviderType provider, CancellationToken ct = default);
}
