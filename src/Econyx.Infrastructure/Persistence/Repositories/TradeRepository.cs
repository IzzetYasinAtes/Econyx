namespace Econyx.Infrastructure.Persistence.Repositories;

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Econyx.Domain.Entities;
using Econyx.Domain.Repositories;

internal sealed class TradeRepository : ITradeRepository
{
    private readonly EconyxDbContext _context;

    public TradeRepository(EconyxDbContext context) => _context = context;

    public async Task<Trade?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Trades.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<Trade>> GetAllAsync(CancellationToken ct = default)
        => await _context.Trades.ToListAsync(ct);

    public async Task<IReadOnlyList<Trade>> FindAsync(Expression<Func<Trade, bool>> predicate, CancellationToken ct = default)
        => await _context.Trades.Where(predicate).ToListAsync(ct);

    public async Task AddAsync(Trade entity, CancellationToken ct = default)
        => await _context.Trades.AddAsync(entity, ct);

    public void Update(Trade entity)
        => _context.Trades.Update(entity);

    public void Remove(Trade entity)
        => _context.Trades.Remove(entity);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await _context.Trades.AnyAsync(t => t.Id == id, ct);

    public async Task<int> CountAsync(CancellationToken ct = default)
        => await _context.Trades.CountAsync(ct);

    public async Task<IReadOnlyList<Trade>> GetByMarketIdAsync(Guid marketId, CancellationToken ct = default)
        => await _context.Trades
            .Where(t => t.MarketId == marketId)
            .OrderByDescending(t => t.ClosedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Trade>> GetRecentAsync(int count, CancellationToken ct = default)
        => await _context.Trades
            .OrderByDescending(t => t.ClosedAt)
            .Take(count)
            .ToListAsync(ct);
}
