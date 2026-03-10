using Econyx.Core.Interfaces;
using Econyx.Domain.Entities;
using Econyx.Domain.Enums;

namespace Econyx.Domain.Repositories;

public interface IMarketRepository : IRepository<Market, Guid>
{
    Task<Market?> GetByExternalIdAsync(string externalId, PlatformType platform, CancellationToken ct = default);
    Task<IReadOnlyList<Market>> GetOpenMarketsAsync(CancellationToken ct = default);
}
