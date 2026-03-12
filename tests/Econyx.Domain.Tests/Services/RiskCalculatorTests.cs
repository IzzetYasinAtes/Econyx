using Econyx.Domain.Services;
using Econyx.Domain.ValueObjects;
using FluentAssertions;

namespace Econyx.Domain.Tests.Services;

public sealed class RiskCalculatorTests
{
    [Fact]
    public void CalculatePositionSize_ShouldReturnPositiveValue()
    {
        var balance = Money.Create(1000m);
        var edge = Edge.Create(0.10m);
        var fairValue = Probability.Create(0.60m);

        var size = RiskCalculator.CalculatePositionSize(balance, edge, fairValue, 0.05m);

        size.Amount.Should().BeGreaterThan(0);
        size.Amount.Should().BeLessThanOrEqualTo(50m);
    }

    [Fact]
    public void CalculatePositionSize_ShouldRespectMaxCap()
    {
        var balance = Money.Create(1000m);
        var edge = Edge.Create(0.50m);
        var fairValue = Probability.Create(0.60m);

        var size = RiskCalculator.CalculatePositionSize(balance, edge, fairValue, 0.05m);

        size.Amount.Should().BeLessThanOrEqualTo(50m);
    }

    [Fact]
    public void CalculatePositionSize_ShouldReturnZero_WhenFairValueIsZero()
    {
        var balance = Money.Create(1000m);
        var edge = Edge.Create(0.10m);
        var fairValue = Probability.Create(0m);

        var size = RiskCalculator.CalculatePositionSize(balance, edge, fairValue, 0.05m);

        size.Amount.Should().Be(0m);
    }

    [Fact]
    public void CalculatePositionSize_ShouldReturnZero_WhenFairValueIsCertain()
    {
        var balance = Money.Create(1000m);
        var edge = Edge.Create(0.10m);
        var fairValue = Probability.Certain;

        var size = RiskCalculator.CalculatePositionSize(balance, edge, fairValue, 0.05m);

        size.Amount.Should().Be(0m);
    }

    [Fact]
    public void CalculatePositionSize_ShouldThrow_ForInvalidMaxPercent_Zero()
    {
        var act = () => RiskCalculator.CalculatePositionSize(
            Money.Create(100m), Edge.Create(0.1m), Probability.Even, 0m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CalculatePositionSize_ShouldThrow_ForInvalidMaxPercent_OverOne()
    {
        var act = () => RiskCalculator.CalculatePositionSize(
            Money.Create(100m), Edge.Create(0.1m), Probability.Even, 1.1m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CalculatePositionSize_ShouldThrow_ForNullBalance()
    {
        var act = () => RiskCalculator.CalculatePositionSize(
            null!, Edge.Create(0.1m), Probability.Even, 0.05m);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CalculatePositionSize_ShouldReturnZero_ForZeroEdge()
    {
        var balance = Money.Create(1000m);
        var edge = Edge.Create(0m);
        var fairValue = Probability.Create(0.6m);

        var size = RiskCalculator.CalculatePositionSize(balance, edge, fairValue, 0.05m);

        size.Amount.Should().Be(0m);
    }

    [Fact]
    public void CalculatePositionSize_ShouldPreserveCurrency()
    {
        var balance = Money.Create(500m, "USDC");
        var edge = Edge.Create(0.10m);
        var fairValue = Probability.Create(0.60m);

        var size = RiskCalculator.CalculatePositionSize(balance, edge, fairValue, 0.05m);

        size.Currency.Should().Be("USDC");
    }
}
