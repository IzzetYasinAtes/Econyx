using Econyx.Core.Interfaces;
using Econyx.Domain.Entities;

namespace Econyx.Domain.Repositories;

public interface IOrderRepository : IRepository<Order, Guid>
{
    Task<IReadOnlyList<Order>> GetPendingOrdersAsync(CancellationToken ct = default);
}
