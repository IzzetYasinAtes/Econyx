using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MediatR;

namespace Econyx.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<EconyxDbContext>
{
    public EconyxDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EconyxDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\MSSQLLocalDB;Database=EconyxDb;Trusted_Connection=True;TrustServerCertificate=True;",
            sql => sql.MigrationsAssembly(typeof(EconyxDbContext).Assembly.FullName));

        return new EconyxDbContext(optionsBuilder.Options, new NoOpMediator());
    }

    private sealed class NoOpMediator : IMediator
    {
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken ct = default)
            => AsyncEnumerable.Empty<TResponse>();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken ct = default)
            => AsyncEnumerable.Empty<object?>();

        public Task Publish(object notification, CancellationToken ct = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken ct = default) where TNotification : INotification => Task.CompletedTask;
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default) => Task.FromResult<TResponse>(default!);
        public Task Send<TRequest>(TRequest request, CancellationToken ct = default) where TRequest : IRequest => Task.CompletedTask;
        public Task<object?> Send(object request, CancellationToken ct = default) => Task.FromResult<object?>(null);
    }
}
