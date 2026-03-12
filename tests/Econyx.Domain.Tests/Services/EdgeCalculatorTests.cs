using Econyx.Domain.Services;
using Econyx.Domain.ValueObjects;
using FluentAssertions;

namespace Econyx.Domain.Tests.Services;

public sealed class EdgeCalculatorTests
{
    [Fact]
    public void Calculate_ShouldReturnPositiveEdge_WhenFairValueAboveMarketPrice()
    {
        var fairValue = Probability.Create(0.60m);
        var marketPrice = Probability.Create(0.40m);

        var edge = EdgeCalculator.Calculate(fairValue, marketPrice);

        edge.Value.Should().Be(0.18m);
    }

    [Fact]
    public void Calculate_ShouldAccountForFees()
    {
        var fairValue = Probability.Create(0.60m);
        var marketPrice = Probability.Create(0.50m);

        var edge = EdgeCalculator.Calculate(fairValue, marketPrice, estimatedFees: 0.05m);

        edge.Value.Should().Be(0.05m);
    }

    [Fact]
    public void Calculate_ShouldReturnNegative_WhenEdgeBelowFees()
    {
        var fairValue = Probability.Create(0.51m);
        var marketPrice = Probability.Create(0.50m);

        var edge = EdgeCalculator.Calculate(fairValue, marketPrice, estimatedFees: 0.02m);

        edge.Value.Should().BeNegative();
    }

    [Fact]
    public void Calculate_ShouldReturnZeroEdge_WhenEqualPrices()
    {
        var price = Probability.Create(0.50m);

        var edge = EdgeCalculator.Calculate(price, price, estimatedFees: 0m);

        edge.Value.Should().Be(0m);
    }

    [Fact]
    public void Calculate_ShouldThrow_ForNullFairValue()
    {
        var act = () => EdgeCalculator.Calculate(null!, Probability.Even);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Calculate_ShouldThrow_ForNullMarketPrice()
    {
        var act = () => EdgeCalculator.Calculate(Probability.Even, null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
