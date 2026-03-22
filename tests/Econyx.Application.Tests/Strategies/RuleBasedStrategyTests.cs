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
    public async Task EvaluateAsync_ShouldGenerateBuySignal_WhenPriceBelow15Percent()
    {
        var market = CreateMarket(yesPrice: 0.08m);
        var strategy = CreateStrategy();

        var signals = await strategy.EvaluateAsync([market]);

        var buySignal = signals.First(s => s.TokenId == "tok-yes");
        buySignal.RecommendedSide.Should().Be(TradeSide.Yes);
        buySignal.MarketId.Should().Be(market.Id);
        buySignal.StrategyName.Should().Be("RuleBased");
    }

    [Fact]
    public async Task EvaluateAsync_ShouldBuyComplementaryToken_WhenPriceAbove85Percent()
    {
        var market = CreateMarket(yesPrice: 0.95m);
        var strategy = CreateStrategy();

        var signals = await strategy.EvaluateAsync([market]);

        // Yes at 0.95 → buy complementary No token at 0.05
        signals.Should().ContainSingle();
        signals[0].TokenId.Should().Be("tok-no");
        signals[0].RecommendedSide.Should().Be(TradeSide.Yes);
        signals[0].MarketPrice.Value.Should().Be(0.05m);
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
    public async Task EvaluateAsync_ShouldSkipSignals_WhenEdgeBelowThreshold()
    {
        var market = CreateMarket(yesPrice: 0.12m);
        var strategy = CreateStrategy();

        var signals = await strategy.EvaluateAsync([market]);

        signals.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_ShouldHandleMultipleMarkets()
    {
        var markets = new[]
        {
            CreateMarket(yesPrice: 0.05m),
            CreateMarket(yesPrice: 0.50m),
            CreateMarket(yesPrice: 0.95m)
        };
        var strategy = CreateStrategy();

        var signals = await strategy.EvaluateAsync(markets);

        // Market 1: Yes at 0.05 → buy Yes token
        // Market 2: Yes at 0.50 → no signal (middle range)
        // Market 3: Yes at 0.95 → buy complementary No token
        signals.Should().HaveCount(2);
        signals.Should().OnlyContain(s => s.RecommendedSide == TradeSide.Yes);
    }
}
