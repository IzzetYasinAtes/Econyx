using Econyx.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Econyx.Infrastructure.Tests;

internal static class TestDbContextFactory
{
    public static EconyxDbContext Create()
    {
        var options = new DbContextOptionsBuilder<EconyxDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EconyxDbContext(options, new Mock<IMediator>().Object);
    }
}
