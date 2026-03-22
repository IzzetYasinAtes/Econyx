using Econyx.Core.Interfaces;
using Econyx.Domain.Entities;

namespace Econyx.Domain.Repositories;

public interface ITradingConfigurationRepository : IRepository<TradingConfiguration, Guid>
{
    Task<TradingConfiguration?> GetActiveAsync(CancellationToken ct = default);
}
