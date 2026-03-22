using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Econyx.Domain.Entities;
using Econyx.Domain.Repositories;

namespace Econyx.Infrastructure.Persistence.Repositories;

internal sealed class AiRequestLogRepository : IAiRequestLogRepository
{
    private readonly EconyxDbContext _context;

    public AiRequestLogRepository(EconyxDbContext context) => _context = context;

    public async Task<AiRequestLog?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.AiRequestLogs.FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task<IReadOnlyList<AiRequestLog>> GetAllAsync(CancellationToken ct = default)
        => await _context.AiRequestLogs.OrderByDescending(l => l.CreatedAt).ToListAsync(ct);

    public async Task<IReadOnlyList<AiRequestLog>> FindAsync(
        Expression<Func<AiRequestLog, bool>> predicate, CancellationToken ct = default)
        => await _context.AiRequestLogs.Where(predicate).ToListAsync(ct);

    public async Task AddAsync(AiRequestLog entity, CancellationToken ct = default)
        => await _context.AiRequestLogs.AddAsync(entity, ct);

    public void Update(AiRequestLog entity)
        => _context.AiRequestLogs.Update(entity);

    public void Remove(AiRequestLog entity)
        => _context.AiRequestLogs.Remove(entity);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await _context.AiRequestLogs.AnyAsync(l => l.Id == id, ct);

    public async Task<int> CountAsync(CancellationToken ct = default)
        => await _context.AiRequestLogs.CountAsync(ct);

    public async Task<(IReadOnlyList<AiRequestLog> Items, int TotalCount)> GetPagedAsync(
        int pageSize, int page, CancellationToken ct = default)
    {
        var totalCount = await _context.AiRequestLogs.CountAsync(ct);
        var items = await _context.AiRequestLogs
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct = default)
        => await _context.AiRequestLogs
            .Where(l => l.CreatedAt < cutoff)
            .ExecuteDeleteAsync(ct);
}
