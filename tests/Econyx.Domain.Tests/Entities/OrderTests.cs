using Econyx.Domain.Entities;
using Econyx.Domain.Enums;
using Econyx.Domain.ValueObjects;
using FluentAssertions;

namespace Econyx.Domain.Tests.Entities;

public sealed class OrderTests
{
    private static Order CreateValidOrder() =>
        Order.Create(
            Guid.NewGuid(),
            "token-yes-123",
            TradeSide.Yes,
            OrderType.Limit,
            Money.Create(0.30m),
            100m,
            TradingMode.Paper,
            PlatformType.Polymarket);

    [Fact]
    public void Create_ShouldSetProperties()
    {
        var marketId = Guid.NewGuid();
        var order = Order.Create(
            marketId, "tok-1", TradeSide.Yes, OrderType.Limit,
            Money.Create(0.5m), 50m, TradingMode.Paper, PlatformType.Polymarket);

        order.MarketId.Should().Be(marketId);
        order.TokenId.Should().Be("tok-1");
        order.Side.Should().Be(TradeSide.Yes);
        order.Type.Should().Be(OrderType.Limit);
        order.Price.Amount.Should().Be(0.5m);
        order.Quantity.Should().Be(50m);
        order.Status.Should().Be(OrderStatus.Pending);
        order.Mode.Should().Be(TradingMode.Paper);
        order.Platform.Should().Be(PlatformType.Polymarket);
        order.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void Create_ShouldThrow_ForEmptyTokenId()
    {
        var act = () => Order.Create(
            Guid.NewGuid(), "", TradeSide.Yes, OrderType.Limit,
            Money.Create(0.5m), 10m, TradingMode.Paper, PlatformType.Polymarket);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldThrow_ForZeroQuantity()
    {
        var act = () => Order.Create(
            Guid.NewGuid(), "tok-1", TradeSide.Yes, OrderType.Limit,
            Money.Create(0.5m), 0m, TradingMode.Paper, PlatformType.Polymarket);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_ShouldThrow_ForNullPrice()
    {
        var act = () => Order.Create(
            Guid.NewGuid(), "tok-1", TradeSide.Yes, OrderType.Limit,
            null!, 10m, TradingMode.Paper, PlatformType.Polymarket);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Fill_ShouldUpdateStatus()
    {
        var order = CreateValidOrder();

        order.Fill(Money.Create(0.30m), 100m);

        order.Status.Should().Be(OrderStatus.Filled);
        order.FilledPrice!.Amount.Should().Be(0.30m);
        order.FilledQuantity.Should().Be(100m);
        order.FilledAt.Should().NotBeNull();
    }

    [Fact]
    public void Fill_Partially_ShouldSetPartiallyFilled()
    {
        var order = CreateValidOrder();

        order.Fill(Money.Create(0.30m), 50m);

        order.Status.Should().Be(OrderStatus.PartiallyFilled);
        order.FilledQuantity.Should().Be(50m);
    }

    [Fact]
    public void Fill_ShouldThrow_WhenAlreadyFilled()
    {
        var order = CreateValidOrder();
        order.Fill(Money.Create(0.30m), 100m);

        var act = () => order.Fill(Money.Create(0.30m), 10m);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_ShouldSetCancelledStatus()
    {
        var order = CreateValidOrder();

        order.Cancel("User requested");

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.RejectionReason.Should().Be("User requested");
    }

    [Fact]
    public void Cancel_ShouldThrow_WhenAlreadyCancelled()
    {
        var order = CreateValidOrder();
        order.Cancel();

        var act = () => order.Cancel();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Reject_ShouldSetRejectedStatus()
    {
        var order = CreateValidOrder();

        order.Reject("Insufficient funds");

        order.Status.Should().Be(OrderStatus.Rejected);
        order.RejectionReason.Should().Be("Insufficient funds");
    }

    [Fact]
    public void Reject_ShouldThrow_WhenNotPending()
    {
        var order = CreateValidOrder();
        order.Fill(Money.Create(0.30m), 100m);

        var act = () => order.Reject("too late");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SetPlatformOrderId_ShouldUpdateId()
    {
        var order = CreateValidOrder();

        order.SetPlatformOrderId("poly-order-123");

        order.PlatformOrderId.Should().Be("poly-order-123");
        order.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void SetPlatformOrderId_ShouldThrow_ForEmpty()
    {
        var order = CreateValidOrder();

        var act = () => order.SetPlatformOrderId("");

        act.Should().Throw<ArgumentException>();
    }
}
