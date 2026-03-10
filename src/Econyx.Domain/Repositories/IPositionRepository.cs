using Econyx.Core.Interfaces;
using Econyx.Domain.Entities;

namespace Econyx.Domain.Repositories;

public interface IPositionRepository : IRepository<Position, Guid>
{
    Task<IReadOnlyList<Position>> GetOpenPositionsAsync(CancellationToken ct = default);
}
