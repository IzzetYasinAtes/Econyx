namespace Econyx.Infrastructure.Persistence.Repositories;

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Econyx.Domain.Entities;
using Econyx.Domain.Repositories;

internal sealed class BalanceSnapshotRepository : IBalanceSnapshotRepository
{
    private readonly EconyxDbContext _context;

    public BalanceSnapshotRepository(EconyxDbContext context) => _context = context;

    public async Task<BalanceSnapshot?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.BalanceSnapshots.FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<IReadOnlyList<BalanceSnapshot>> GetAllAsync(CancellationToken ct = default)
        => await _context.BalanceSnapshots.ToListAsync(ct);

    public async Task<IReadOnlyList<BalanceSnapshot>> FindAsync(Expression<Func<BalanceSnapshot, bool>> predicate, CancellationToken ct = default)
        => await _context.BalanceSnapshots.Where(predicate).ToListAsync(ct);

    public async Task AddAsync(BalanceSnapshot entity, CancellationToken ct = default)
        => await _context.BalanceSnapshots.AddAsync(entity, ct);

    public void Update(BalanceSnapshot entity)
        => _context.BalanceSnapshots.Update(entity);

    public void Remove(BalanceSnapshot entity)
        => _context.BalanceSnapshots.Remove(entity);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await _context.BalanceSnapshots.AnyAsync(b => b.Id == id, ct);

    public async Task<int> CountAsync(CancellationToken ct = default)
        => await _context.BalanceSnapshots.CountAsync(ct);

    public async Task<BalanceSnapshot?> GetLatestAsync(CancellationToken ct = default)
        => await _context.BalanceSnapshots
            .OrderByDescending(b => b.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<BalanceSnapshot>> GetHistoryAsync(DateTime from, DateTime until, CancellationToken ct = default)
        => await _context.BalanceSnapshots
            .Where(b => b.CreatedAt >= from && b.CreatedAt <= until)
            .OrderBy(b => b.CreatedAt)
            .ToListAsync(ct);
}
