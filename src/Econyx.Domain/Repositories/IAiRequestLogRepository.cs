using Econyx.Core.Interfaces;
using Econyx.Domain.Entities;

namespace Econyx.Domain.Repositories;

public interface IAiRequestLogRepository : IRepository<AiRequestLog, Guid>
{
    Task<(IReadOnlyList<AiRequestLog> Items, int TotalCount)> GetPagedAsync(
        int pageSize, int page, CancellationToken ct = default);

    Task DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct = default);
}
