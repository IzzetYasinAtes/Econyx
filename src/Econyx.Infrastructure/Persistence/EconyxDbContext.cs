namespace Econyx.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Econyx.Domain.Entities;
using Econyx.Core.Entities;
using MediatR;

public class EconyxDbContext : DbContext
{
    private readonly IMediator _mediator;

    public EconyxDbContext(DbContextOptions<EconyxDbContext> options, IMediator mediator) : base(options)
    {
        _mediator = mediator;
    }

    public DbSet<Market> Markets => Set<Market>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Trade> Trades => Set<Trade>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<BalanceSnapshot> BalanceSnapshots => Set<BalanceSnapshot>();
    public DbSet<AiModelConfiguration> AiModelConfigurations => Set<AiModelConfiguration>();
    public DbSet<ApiKeyConfiguration> ApiKeyConfigurations => Set<ApiKeyConfiguration>();
    public DbSet<TradingConfiguration> TradingConfigurations => Set<TradingConfiguration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EconyxDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var domainEvents = ChangeTracker.Entries<BaseEntity<Guid>>()
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in domainEvents)
            await _mediator.Publish(domainEvent, cancellationToken);

        return result;
    }
}
