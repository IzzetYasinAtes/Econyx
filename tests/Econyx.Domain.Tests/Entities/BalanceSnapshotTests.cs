using Econyx.Domain.Entities;
using Econyx.Domain.Enums;
using Econyx.Domain.ValueObjects;
using FluentAssertions;

namespace Econyx.Domain.Tests.Entities;

public sealed class BalanceSnapshotTests
{
    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        var snapshot = BalanceSnapshot.Create(
            Money.Create(500m),
            Money.Create(50m),
            10m,
            3,
            15,
            0.65m,
            Money.Create(2.5m),
            TradingMode.Paper);

        snapshot.Balance.Amount.Should().Be(500m);
        snapshot.TotalPnL.Amount.Should().Be(50m);
        snapshot.TotalPnLPercent.Should().Be(10m);
        snapshot.OpenPositionCount.Should().Be(3);
        snapshot.TotalTrades.Should().Be(15);
        snapshot.WinRate.Should().Be(0.65m);
        snapshot.ApiCosts.Amount.Should().Be(2.5m);
        snapshot.Mode.Should().Be(TradingMode.Paper);
        snapshot.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_ShouldThrow_ForNullBalance()
    {
        var act = () => BalanceSnapshot.Create(
            null!, Money.Create(0m), 0m, 0, 0, 0m,
            Money.Create(0m), TradingMode.Paper);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_ShouldThrow_ForNullApiCosts()
    {
        var act = () => BalanceSnapshot.Create(
            Money.Create(100m), Money.Create(0m), 0m, 0, 0, 0m,
            null!, TradingMode.Paper);

        act.Should().Throw<ArgumentNullException>();
    }
}
