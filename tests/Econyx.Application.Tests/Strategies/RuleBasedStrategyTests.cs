using Econyx.Application.Configuration;
using Econyx.Application.Strategies;
using Econyx.Domain.Entities;
using Econyx.Domain.Enums;
using Econyx.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Econyx.Application.Tests.Strategies;

public sealed class RuleBasedStrategyTests
{
    private readonly TradingOptions _options = new()
    {
        MinVolumeUsd = 10_000m,
        MaxSpreadCents = 5m,
        MinEdgeThreshold = 0.05m
    };

    private RuleBasedStrategy CreateStrategy() =>
        new(Options.Create(_options));

    private static Market CreateMarket(
        decimal yesPrice,
        decimal volume = 50_000m,
        decimal spread = 0.02m,
        MarketStatus status = MarketStatus.Open)
    {
        var outcomes = new[]
        {
            MarketOutcome.Create("Yes", Probability.Create(yesPrice), TokenId.Create("tok-yes")),
            MarketOutcome.Create("No", Probability.Create(1 - yesPrice), TokenId.Create("tok-no"))
        };

        var market = Market.Create(
            $"ext-{Guid.NewGuid():N}", PlatformType.Polymarket,
            "Test question?", "Description", "Category",
            outcomes, volume, spread);

        if (status == MarketStatus.Closed)
            market.Close();
        else if (status == MarketStatus.Resolved)
            market.Resolve("Yes");

        return market;
    }

    [Fact]
    public void Name_ShouldBeRuleBased()
    {
        CreateStrategy().Name.Should().Be("RuleBased");
    }

    [Fact]
    public async Task EvaluateAsync_ShouldGenerateBuySignal_WhenPriceInUnderpricedZone()
    {
        var market = CreateMarket(yesPrice: 0.30m);
        var strategy = CreateStrategy();

        var signals = await strategy.EvaluateAsync([market]);

        signals.Should().ContainSingle();
        signals[0].TokenId.Should().Be("tok-yes");
        signals[0].RecommendedSide.Should().Be(TradeSide.Yes);
        signals[0].StrategyName.Should().Be("RuleBased");
    }

    [Fact]
    public async Task EvaluateAsync_ShouldBuyComplementaryToken_WhenPriceInOverpricedZone()
    {
        var market = CreateMarket(yesPrice: 0.70m);
        var strategy = CreateStrategy();

        var signals = await strategy.EvaluateAsync([market]);

        // Yes at 0.70 → buy complementary No token at 0.30
        signals.Should().ContainSingle();
        signals[0].TokenId.Should().Be("tok-no");
        signals[0].RecommendedSide.Should().Be(TradeSide.Yes);
        signals[0].MarketPrice.Value.Should().Be(0.30m);
    }

    [Fact]
    public async Task EvaluateAsync_ShouldReturnEmpty_WhenPriceInMiddleRange()
    {
        var market = CreateMarket(yesPrice: 0.50m);
        var strategy = CreateStrategy();

        var signals = await strategy.EvaluateAsync([market]);

        signals.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_ShouldSkipClosedMarkets()
    {
        var market = CreateMarket(yesPrice: 0.05m, status: MarketStatus.Closed);
        var strategy = CreateStrategy();

        var signals = await strategy.EvaluateAsync([market]);

        signals.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_ShouldSkipLowVolumeMarkets()
    {
        var market = CreateMarket(yesPrice: 0.08m, volume: 5_000m);
        var strategy = CreateStrategy();

        var signals = await strategy.EvaluateAsync([market]);

        signals.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_ShouldSkipHighSpreadMarkets()
    {
        var market = CreateMarket(yesPrice: 0.08m, spread: 0.10m);
        var strategy = CreateStrategy();

        var signals = await strategy.EvaluateAsync([market]);

        signals.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_ShouldSkipSignals_WhenPriceOutsideTargetZone()
    {
        // Price at 0.08 is outside target zone (0.20-0.45), should be skipped
        var market = CreateMarket(yesPrice: 0.08m);
        var strategy = CreateStrategy();

        var signals = await strategy.EvaluateAsync([market]);

        signals.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_ShouldHandleMultipleMarkets()
    {
        var markets = new[]
        {
            CreateMarket(yesPrice: 0.30m),    // Yes in underpriced zone → buy Yes
            CreateMarket(yesPrice: 0.50m),    // Middle → no signal
            CreateMarket(yesPrice: 0.70m)     // Yes in overpriced zone → buy No (complementary)
        };
        var strategy = CreateStrategy();

        var signals = await strategy.EvaluateAsync(markets);

        signals.Should().HaveCount(2);
        signals.Should().OnlyContain(s => s.RecommendedSide == TradeSide.Yes);
    }
}
