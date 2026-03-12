using Econyx.Domain.Entities;
using Econyx.Domain.Enums;
using Econyx.Domain.ValueObjects;
using FluentAssertions;

namespace Econyx.Domain.Tests.Entities;

public sealed class PositionTests
{
    private static Position CreateOpenPosition(
        decimal entryPrice = 0.30m,
        decimal quantity = 100m,
        TradeSide side = TradeSide.Yes) =>
        Position.Create(
            Guid.NewGuid(),
            "Will BTC hit 100k?",
            "token-yes",
            PlatformType.Polymarket,
            side,
            Money.Create(entryPrice),
            quantity,
            "RuleBased");

    [Fact]
    public void Create_ShouldSetProperties()
    {
        var marketId = Guid.NewGuid();
        var position = Position.Create(
            marketId, "Test?", "tok-1", PlatformType.Polymarket,
            TradeSide.Yes, Money.Create(0.4m), 50m, "Hybrid");

        position.MarketId.Should().Be(marketId);
        position.MarketQuestion.Should().Be("Test?");
        position.TokenId.Should().Be("tok-1");
        position.Side.Should().Be(TradeSide.Yes);
        position.EntryPrice.Amount.Should().Be(0.4m);
        position.CurrentPrice.Amount.Should().Be(0.4m);
        position.Quantity.Should().Be(50m);
        position.StrategyName.Should().Be("Hybrid");
        position.IsOpen.Should().BeTrue();
        position.ClosedAt.Should().BeNull();
        position.ExitPrice.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrow_ForInvalidMarketQuestion(string? question)
    {
        var act = () => Position.Create(
            Guid.NewGuid(), question!, "tok", PlatformType.Polymarket,
            TradeSide.Yes, Money.Create(0.5m), 10m, "s");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldThrow_ForZeroQuantity()
    {
        var act = () => Position.Create(
            Guid.NewGuid(), "Q?", "tok", PlatformType.Polymarket,
            TradeSide.Yes, Money.Create(0.5m), 0m, "s");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdatePrice_ShouldUpdateCurrentPrice()
    {
        var position = CreateOpenPosition();
        var newPrice = Money.Create(0.50m);

        position.UpdatePrice(newPrice);

        position.CurrentPrice.Should().Be(newPrice);
        position.Version.Should().Be(1);
    }

    [Fact]
    public void UpdatePrice_ShouldThrow_WhenClosed()
    {
        var position = CreateOpenPosition();
        position.Close(Money.Create(0.50m));

        var act = () => position.UpdatePrice(Money.Create(0.60m));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Close_ShouldSetClosedState()
    {
        var position = CreateOpenPosition();
        var exitPrice = Money.Create(0.50m);

        position.Close(exitPrice);

        position.IsOpen.Should().BeFalse();
        position.ExitPrice.Should().Be(exitPrice);
        position.CurrentPrice.Should().Be(exitPrice);
        position.ClosedAt.Should().NotBeNull();
        position.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void Close_ShouldThrow_WhenAlreadyClosed()
    {
        var position = CreateOpenPosition();
        position.Close(Money.Create(0.5m));

        var act = () => position.Close(Money.Create(0.6m));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CalculatePnL_ShouldBePositive_ForYesSidePriceIncrease()
    {
        var position = CreateOpenPosition(entryPrice: 0.30m, quantity: 100m, side: TradeSide.Yes);
        position.UpdatePrice(Money.Create(0.50m));

        var pnl = position.CalculatePnL();

        pnl.Amount.Should().Be(20m);
    }

    [Fact]
    public void CalculatePnL_ShouldBeNegative_ForYesSidePriceDecrease()
    {
        var position = CreateOpenPosition(entryPrice: 0.50m, quantity: 100m, side: TradeSide.Yes);
        position.UpdatePrice(Money.Create(0.30m));

        var pnl = position.CalculatePnL();

        pnl.Amount.Should().Be(-20m);
    }

    [Fact]
    public void CalculatePnL_ShouldBePositive_ForNoSidePriceDecrease()
    {
        var position = CreateOpenPosition(entryPrice: 0.70m, quantity: 100m, side: TradeSide.No);
        position.UpdatePrice(Money.Create(0.50m));

        var pnl = position.CalculatePnL();

        pnl.Amount.Should().Be(20m);
    }
}
