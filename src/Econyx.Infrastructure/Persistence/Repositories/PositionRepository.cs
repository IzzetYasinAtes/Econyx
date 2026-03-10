namespace Econyx.Infrastructure.Persistence.Repositories;

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Econyx.Domain.Entities;
using Econyx.Domain.Repositories;

internal sealed class PositionRepository : IPositionRepository
{
    private readonly EconyxDbContext _context;

    public PositionRepository(EconyxDbContext context) => _context = context;

    public async Task<Position?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Positions.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Position>> GetAllAsync(CancellationToken ct = default)
        => await _context.Positions.ToListAsync(ct);

    public async Task<IReadOnlyList<Position>> FindAsync(Expression<Func<Position, bool>> predicate, CancellationToken ct = default)
        => await _context.Positions.Where(predicate).ToListAsync(ct);

    public async Task AddAsync(Position entity, CancellationToken ct = default)
        => await _context.Positions.AddAsync(entity, ct);

    public void Update(Position entity)
        => _context.Positions.Update(entity);

    public void Remove(Position entity)
        => _context.Positions.Remove(entity);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await _context.Positions.AnyAsync(p => p.Id == id, ct);

    public async Task<int> CountAsync(CancellationToken ct = default)
        => await _context.Positions.CountAsync(ct);

    public async Task<IReadOnlyList<Position>> GetOpenPositionsAsync(CancellationToken ct = default)
        => await _context.Positions
            .Where(p => p.IsOpen)
            .ToListAsync(ct);
}
