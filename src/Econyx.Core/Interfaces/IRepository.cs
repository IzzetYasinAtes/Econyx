using System.Linq.Expressions;
using Econyx.Core.Entities;

namespace Econyx.Core.Interfaces;

public interface IRepository<T, TId>
    where T : BaseEntity<TId>
    where TId : notnull
{
    Task<T?> GetByIdAsync(TId id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
    Task<bool> ExistsAsync(TId id, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
}
