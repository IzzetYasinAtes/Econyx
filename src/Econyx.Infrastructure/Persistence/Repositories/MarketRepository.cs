namespace Econyx.Infrastructure.Persistence.Repositories;

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Econyx.Domain.Entities;
using Econyx.Domain.Enums;
using Econyx.Domain.Repositories;

internal sealed class MarketRepository : IMarketRepository
{
    private readonly EconyxDbContext _context;

    public MarketRepository(EconyxDbContext context) => _context = context;

    public async Task<Market?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Markets.FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<IReadOnlyList<Market>> GetAllAsync(CancellationToken ct = default)
        => await _context.Markets.ToListAsync(ct);

    public async Task<IReadOnlyList<Market>> FindAsync(Expression<Func<Market, bool>> predicate, CancellationToken ct = default)
        => await _context.Markets.Where(predicate).ToListAsync(ct);

    public async Task AddAsync(Market entity, CancellationToken ct = default)
        => await _context.Markets.AddAsync(entity, ct);

    public void Update(Market entity)
        => _context.Markets.Update(entity);

    public void Remove(Market entity)
        => _context.Markets.Remove(entity);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await _context.Markets.AnyAsync(m => m.Id == id, ct);

    public async Task<int> CountAsync(CancellationToken ct = default)
        => await _context.Markets.CountAsync(ct);

    public async Task<Market?> GetByExternalIdAsync(string externalId, PlatformType platform, CancellationToken ct = default)
        => await _context.Markets
            .FirstOrDefaultAsync(m => m.ExternalId == externalId && m.Platform == platform, ct);

    public async Task<IReadOnlyList<Market>> GetOpenMarketsAsync(CancellationToken ct = default)
        => await _context.Markets
            .Where(m => m.Status == MarketStatus.Open)
            .ToListAsync(ct);
}
