using Econyx.Core.Interfaces;
using Econyx.Domain.Entities;

namespace Econyx.Domain.Repositories;

public interface IAiModelConfigurationRepository : IRepository<AiModelConfiguration, Guid>
{
    Task<AiModelConfiguration?> GetActiveAsync(CancellationToken ct = default);
    Task DeactivateAllAsync(CancellationToken ct = default);
}
