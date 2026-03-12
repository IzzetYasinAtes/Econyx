using Econyx.Core.Entities;
using Econyx.Core.Events;
using FluentAssertions;

namespace Econyx.Core.Tests.Entities;

public sealed class BaseEntityTests
{
    [Fact]
    public void DomainEvents_ShouldBeEmptyByDefault()
    {
        var entity = new TestEntity();

        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void RaiseDomainEvent_ShouldAddEvent()
    {
        var entity = new TestEntity();

        entity.AddTestEvent();

        entity.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAll()
    {
        var entity = new TestEntity();
        entity.AddTestEvent();
        entity.AddTestEvent();

        entity.ClearDomainEvents();

        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void CreatedAt_ShouldBeSetAutomatically()
    {
        var before = DateTime.UtcNow;
        var entity = new TestEntity();
        var after = DateTime.UtcNow;

        entity.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    private sealed class TestEntity : BaseEntity<Guid>
    {
        public TestEntity() => Id = Guid.NewGuid();

        public void AddTestEvent() => RaiseDomainEvent(new TestDomainEvent());
    }

    private sealed record TestDomainEvent : DomainEventBase;
}
