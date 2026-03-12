using Econyx.Domain.Entities;
using Econyx.Domain.Enums;
using Econyx.Domain.ValueObjects;
using FluentAssertions;

namespace Econyx.Domain.Tests.Entities;

public sealed class TradeTests
{
    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        var positionId = Guid.NewGuid();
        var marketId = Guid.NewGuid();
        var duration = TimeSpan.FromHours(3);

        var trade = Trade.Create(
            positionId, marketId, "Will BTC hit 100k?",
            TradeSide.Yes,
            Money.Create(0.30m), Money.Create(0.50m),
            100m,
            Money.Create(20m), Money.Create(0.5m),
            "Hybrid", PlatformType.Polymarket, duration);

        trade.PositionId.Should().Be(positionId);
        trade.MarketId.Should().Be(marketId);
        trade.MarketQuestion.Should().Be("Will BTC hit 100k?");
        trade.Side.Should().Be(TradeSide.Yes);
        trade.EntryPrice.Amount.Should().Be(0.30m);
        trade.ExitPrice.Amount.Should().Be(0.50m);
        trade.Quantity.Should().Be(100m);
        trade.PnL.Amount.Should().Be(20m);
        trade.Fees.Amount.Should().Be(0.5m);
        trade.StrategyName.Should().Be("Hybrid");
        trade.Platform.Should().Be(PlatformType.Polymarket);
        trade.Duration.Should().Be(duration);
        trade.Id.Should().NotBeEmpty();
    }
}
