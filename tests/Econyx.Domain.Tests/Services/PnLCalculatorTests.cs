using Econyx.Domain.Enums;
using Econyx.Domain.Services;
using Econyx.Domain.ValueObjects;
using FluentAssertions;

namespace Econyx.Domain.Tests.Services;

public sealed class PnLCalculatorTests
{
    [Fact]
    public void CalculateUnrealizedPnL_YesSide_PriceUp_ShouldBePositive()
    {
        var entry = Money.Create(0.30m);
        var current = Money.Create(0.50m);

        var pnl = PnLCalculator.CalculateUnrealizedPnL(entry, current, 100m, TradeSide.Yes);

        pnl.Amount.Should().Be(20m);
    }

    [Fact]
    public void CalculateUnrealizedPnL_YesSide_PriceDown_ShouldBeNegative()
    {
        var entry = Money.Create(0.50m);
        var current = Money.Create(0.30m);

        var pnl = PnLCalculator.CalculateUnrealizedPnL(entry, current, 100m, TradeSide.Yes);

        pnl.Amount.Should().Be(-20m);
    }

    [Fact]
    public void CalculateUnrealizedPnL_NoSide_PriceDown_ShouldBePositive()
    {
        var entry = Money.Create(0.70m);
        var current = Money.Create(0.50m);

        var pnl = PnLCalculator.CalculateUnrealizedPnL(entry, current, 100m, TradeSide.No);

        pnl.Amount.Should().Be(20m);
    }

    [Fact]
    public void CalculateUnrealizedPnL_NoSide_PriceUp_ShouldBeNegative()
    {
        var entry = Money.Create(0.50m);
        var current = Money.Create(0.70m);

        var pnl = PnLCalculator.CalculateUnrealizedPnL(entry, current, 100m, TradeSide.No);

        pnl.Amount.Should().Be(-20m);
    }

    [Fact]
    public void CalculateUnrealizedPnL_ShouldThrow_ForNullEntry()
    {
        var act = () => PnLCalculator.CalculateUnrealizedPnL(
            null!, Money.Create(0.5m), 100m, TradeSide.Yes);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CalculateRealizedPnL_ShouldSubtractFees()
    {
        var entry = Money.Create(0.30m);
        var exit = Money.Create(0.50m);
        var fees = Money.Create(1m);

        var pnl = PnLCalculator.CalculateRealizedPnL(entry, exit, 100m, TradeSide.Yes, fees);

        pnl.Amount.Should().Be(19m);
    }

    [Fact]
    public void CalculateRealizedPnL_ShouldThrow_ForNullFees()
    {
        var act = () => PnLCalculator.CalculateRealizedPnL(
            Money.Create(0.3m), Money.Create(0.5m), 100m, TradeSide.Yes, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CalculateUnrealizedPnL_SamePrice_ShouldBeZero()
    {
        var price = Money.Create(0.50m);

        var pnl = PnLCalculator.CalculateUnrealizedPnL(price, price, 100m, TradeSide.Yes);

        pnl.Amount.Should().Be(0m);
    }
}
