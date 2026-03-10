using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Econyx.Domain.Entities;
using Econyx.Domain.Repositories;

namespace Econyx.Infrastructure.Persistence.Repositories;

internal sealed class AiModelConfigurationRepository : IAiModelConfigurationRepository
{
    private readonly EconyxDbContext _context;

    public AiModelConfigurationRepository(EconyxDbContext context) => _context = context;

    public async Task<AiModelConfiguration?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.AiModelConfigurations.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<AiModelConfiguration>> GetAllAsync(CancellationToken ct = default)
        => await _context.AiModelConfigurations.ToListAsync(ct);

    public async Task<IReadOnlyList<AiModelConfiguration>> FindAsync(
        Expression<Func<AiModelConfiguration, bool>> predicate, CancellationToken ct = default)
        => await _context.AiModelConfigurations.Where(predicate).ToListAsync(ct);

    public async Task AddAsync(AiModelConfiguration entity, CancellationToken ct = default)
        => await _context.AiModelConfigurations.AddAsync(entity, ct);

    public void Update(AiModelConfiguration entity)
        => _context.AiModelConfigurations.Update(entity);

    public void Remove(AiModelConfiguration entity)
        => _context.AiModelConfigurations.Remove(entity);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await _context.AiModelConfigurations.AnyAsync(c => c.Id == id, ct);

    public async Task<int> CountAsync(CancellationToken ct = default)
        => await _context.AiModelConfigurations.CountAsync(ct);

    public async Task<AiModelConfiguration?> GetActiveAsync(CancellationToken ct = default)
        => await _context.AiModelConfigurations
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task DeactivateAllAsync(CancellationToken ct = default)
        => await _context.AiModelConfigurations
            .Where(c => c.IsActive)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.IsActive, false), ct);
}
