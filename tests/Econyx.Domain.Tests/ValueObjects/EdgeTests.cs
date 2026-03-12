using Econyx.Domain.ValueObjects;
using FluentAssertions;

namespace Econyx.Domain.Tests.ValueObjects;

public sealed class EdgeTests
{
    [Fact]
    public void Create_ShouldStoreValue()
    {
        var edge = Edge.Create(0.15m);

        edge.Value.Should().Be(0.15m);
    }

    [Fact]
    public void AbsoluteValue_ShouldReturnAbsolute()
    {
        var negative = Edge.Create(-0.10m);

        negative.AbsoluteValue.Should().Be(0.10m);
    }

    [Fact]
    public void IsActionable_ShouldReturnTrue_WhenAboveThreshold()
    {
        var edge = Edge.Create(0.10m);

        edge.IsActionable(0.05m).Should().BeTrue();
    }

    [Fact]
    public void IsActionable_ShouldReturnFalse_WhenBelowThreshold()
    {
        var edge = Edge.Create(0.02m);

        edge.IsActionable(0.05m).Should().BeFalse();
    }

    [Fact]
    public void ComparisonOperators_ShouldWork()
    {
        var small = Edge.Create(0.05m);
        var big = Edge.Create(0.15m);

        (big > small).Should().BeTrue();
        (small < big).Should().BeTrue();
    }

    [Fact]
    public void Equality_ShouldReturnTrue_ForSameValue()
    {
        var a = Edge.Create(0.10m);
        var b = Edge.Create(0.10m);

        (a == b).Should().BeTrue();
    }

    [Fact]
    public void CompareTo_Null_ShouldReturnPositive()
    {
        Edge.Create(0.1m).CompareTo(null).Should().BePositive();
    }
}
