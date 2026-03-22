using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Econyx.Domain.Entities;
using Econyx.Domain.Repositories;

namespace Econyx.Infrastructure.Persistence.Repositories;

internal sealed class TradingConfigurationRepository : ITradingConfigurationRepository
{
    private readonly EconyxDbContext _context;

    public TradingConfigurationRepository(EconyxDbContext context) => _context = context;

    public async Task<TradingConfiguration?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.TradingConfigurations.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<TradingConfiguration>> GetAllAsync(CancellationToken ct = default)
        => await _context.TradingConfigurations.ToListAsync(ct);

    public async Task<IReadOnlyList<TradingConfiguration>> FindAsync(
        Expression<Func<TradingConfiguration, bool>> predicate, CancellationToken ct = default)
        => await _context.TradingConfigurations.Where(predicate).ToListAsync(ct);

    public async Task AddAsync(TradingConfiguration entity, CancellationToken ct = default)
        => await _context.TradingConfigurations.AddAsync(entity, ct);

    public void Update(TradingConfiguration entity)
        => _context.TradingConfigurations.Update(entity);

    public void Remove(TradingConfiguration entity)
        => _context.TradingConfigurations.Remove(entity);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await _context.TradingConfigurations.AnyAsync(c => c.Id == id, ct);

    public async Task<int> CountAsync(CancellationToken ct = default)
        => await _context.TradingConfigurations.CountAsync(ct);

    public async Task<TradingConfiguration?> GetActiveAsync(CancellationToken ct = default)
        => await _context.TradingConfigurations
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .FirstOrDefaultAsync(ct);
}
