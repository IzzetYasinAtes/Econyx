using Econyx.Core.Entities;
using FluentAssertions;

namespace Econyx.Core.Tests.Entities;

public sealed class AggregateRootTests
{
    [Fact]
    public void Version_ShouldStartAtZero()
    {
        var aggregate = new TestAggregate();

        aggregate.Version.Should().Be(0);
    }

    [Fact]
    public void IncrementVersion_ShouldIncreaseAndSetUpdatedAt()
    {
        var aggregate = new TestAggregate();

        aggregate.BumpVersion();

        aggregate.Version.Should().Be(1);
        aggregate.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void IncrementVersion_CalledMultipleTimes_ShouldIncrement()
    {
        var aggregate = new TestAggregate();

        aggregate.BumpVersion();
        aggregate.BumpVersion();
        aggregate.BumpVersion();

        aggregate.Version.Should().Be(3);
    }

    private sealed class TestAggregate : AggregateRoot<Guid>
    {
        public TestAggregate() => Id = Guid.NewGuid();

        public void BumpVersion() => IncrementVersion();
    }
}
