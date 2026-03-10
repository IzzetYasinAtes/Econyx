using Econyx.Core.Interfaces;
using Econyx.Domain.Entities;

namespace Econyx.Domain.Repositories;

public interface ITradeRepository : IRepository<Trade, Guid>
{
    Task<IReadOnlyList<Trade>> GetByMarketIdAsync(Guid marketId, CancellationToken ct = default);
    Task<IReadOnlyList<Trade>> GetRecentAsync(int count, CancellationToken ct = default);
}
