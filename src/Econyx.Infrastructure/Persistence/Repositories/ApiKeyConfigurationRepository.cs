using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Econyx.Domain.Entities;
using Econyx.Domain.Enums;
using Econyx.Domain.Repositories;

namespace Econyx.Infrastructure.Persistence.Repositories;

internal sealed class ApiKeyConfigurationRepository : IApiKeyConfigurationRepository
{
    private readonly EconyxDbContext _context;

    public ApiKeyConfigurationRepository(EconyxDbContext context) => _context = context;

    public async Task<ApiKeyConfiguration?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.ApiKeyConfigurations.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<ApiKeyConfiguration>> GetAllAsync(CancellationToken ct = default)
        => await _context.ApiKeyConfigurations.ToListAsync(ct);

    public async Task<IReadOnlyList<ApiKeyConfiguration>> FindAsync(
        Expression<Func<ApiKeyConfiguration, bool>> predicate, CancellationToken ct = default)
        => await _context.ApiKeyConfigurations.Where(predicate).ToListAsync(ct);

    public async Task AddAsync(ApiKeyConfiguration entity, CancellationToken ct = default)
        => await _context.ApiKeyConfigurations.AddAsync(entity, ct);

    public void Update(ApiKeyConfiguration entity)
        => _context.ApiKeyConfigurations.Update(entity);

    public void Remove(ApiKeyConfiguration entity)
        => _context.ApiKeyConfigurations.Remove(entity);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await _context.ApiKeyConfigurations.AnyAsync(c => c.Id == id, ct);

    public async Task<int> CountAsync(CancellationToken ct = default)
        => await _context.ApiKeyConfigurations.CountAsync(ct);

    public async Task<ApiKeyConfiguration?> GetByProviderAsync(AiProviderType provider, CancellationToken ct = default)
        => await _context.ApiKeyConfigurations
            .FirstOrDefaultAsync(c => c.Provider == provider, ct);
}
