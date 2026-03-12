using Econyx.Domain.ValueObjects;
using FluentAssertions;

namespace Econyx.Domain.Tests.ValueObjects;

public sealed class ProbabilityTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(0.5)]
    [InlineData(1)]
    public void Create_ShouldAcceptValidValues(decimal value)
    {
        var prob = Probability.Create(value);

        prob.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void Create_ShouldThrow_ForOutOfRange(decimal value)
    {
        var act = () => Probability.Create(value);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void FromPercentage_ShouldConvert()
    {
        var prob = Probability.FromPercentage(75m);

        prob.Value.Should().Be(0.75m);
    }

    [Fact]
    public void ToPercentage_ShouldConvert()
    {
        var prob = Probability.Create(0.35m);

        prob.ToPercentage().Should().Be(35m);
    }

    [Fact]
    public void StaticValues_ShouldBeCorrect()
    {
        Probability.Even.Value.Should().Be(0.5m);
        Probability.Certain.Value.Should().Be(1m);
        Probability.Impossible.Value.Should().Be(0m);
    }

    [Fact]
    public void ComparisonOperators_ShouldWork()
    {
        var low = Probability.Create(0.2m);
        var high = Probability.Create(0.8m);

        (high > low).Should().BeTrue();
        (low < high).Should().BeTrue();
        (high >= low).Should().BeTrue();
        (low <= high).Should().BeTrue();
    }

    [Fact]
    public void Equality_ShouldReturnTrue_ForSameValue()
    {
        var a = Probability.Create(0.5m);
        var b = Probability.Create(0.5m);

        (a == b).Should().BeTrue();
    }

    [Fact]
    public void CompareTo_Null_ShouldReturnPositive()
    {
        Probability.Even.CompareTo(null).Should().BePositive();
    }
}
