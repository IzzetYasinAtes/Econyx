using Econyx.Core.Interfaces;
using Econyx.Domain.Entities;

namespace Econyx.Domain.Repositories;

public interface IBalanceSnapshotRepository : IRepository<BalanceSnapshot, Guid>
{
    Task<BalanceSnapshot?> GetLatestAsync(CancellationToken ct = default);
    Task<IReadOnlyList<BalanceSnapshot>> GetHistoryAsync(DateTime from, DateTime until, CancellationToken ct = default);
}
