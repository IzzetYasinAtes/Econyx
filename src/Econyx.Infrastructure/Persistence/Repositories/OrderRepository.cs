namespace Econyx.Infrastructure.Persistence.Repositories;

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Econyx.Domain.Entities;
using Econyx.Domain.Enums;
using Econyx.Domain.Repositories;

internal sealed class OrderRepository : IOrderRepository
{
    private readonly EconyxDbContext _context;

    public OrderRepository(EconyxDbContext context) => _context = context;

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct = default)
        => await _context.Orders.ToListAsync(ct);

    public async Task<IReadOnlyList<Order>> FindAsync(Expression<Func<Order, bool>> predicate, CancellationToken ct = default)
        => await _context.Orders.Where(predicate).ToListAsync(ct);

    public async Task AddAsync(Order entity, CancellationToken ct = default)
        => await _context.Orders.AddAsync(entity, ct);

    public void Update(Order entity)
        => _context.Orders.Update(entity);

    public void Remove(Order entity)
        => _context.Orders.Remove(entity);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await _context.Orders.AnyAsync(o => o.Id == id, ct);

    public async Task<int> CountAsync(CancellationToken ct = default)
        => await _context.Orders.CountAsync(ct);

    public async Task<IReadOnlyList<Order>> GetPendingOrdersAsync(CancellationToken ct = default)
        => await _context.Orders
            .Where(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.PartiallyFilled)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync(ct);
}
