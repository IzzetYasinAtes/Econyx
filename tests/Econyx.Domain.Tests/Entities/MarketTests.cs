using Econyx.Domain.Entities;
using Econyx.Domain.Enums;
using Econyx.Domain.ValueObjects;
using FluentAssertions;

namespace Econyx.Domain.Tests.Entities;

public sealed class MarketTests
{
    private static Market CreateValidMarket(
        decimal yesPrice = 0.6m,
        decimal volume = 50_000m,
        decimal spread = 0.02m)
    {
        var outcomes = new[]
        {
            MarketOutcome.Create("Yes", Probability.Create(yesPrice), TokenId.Create("tok-yes")),
            MarketOutcome.Create("No", Probability.Create(1 - yesPrice), TokenId.Create("tok-no"))
        };

        return Market.Create(
            "ext-1", PlatformType.Polymarket, "Will it rain?",
            "Description", "Weather", outcomes, volume, spread);
    }

    [Fact]
    public void Create_ShouldSetProperties()
    {
        var market = CreateValidMarket();

        market.ExternalId.Should().Be("ext-1");
        market.Platform.Should().Be(PlatformType.Polymarket);
        market.Question.Should().Be("Will it rain?");
        market.Status.Should().Be(MarketStatus.Open);
        market.Outcomes.Should().HaveCount(2);
        market.VolumeUsd.Should().Be(50_000m);
    }

    [Fact]
    public void Create_ShouldThrow_ForEmptyExternalId()
    {
        var act = () => Market.Create(
            "", PlatformType.Polymarket, "Q?", "D", "C",
            [], 1000m, 0.01m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldThrow_ForEmptyQuestion()
    {
        var act = () => Market.Create(
            "ext-1", PlatformType.Polymarket, "", "D", "C",
            [], 1000m, 0.01m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateOutcomes_ShouldReplaceOutcomes()
    {
        var market = CreateValidMarket();
        var newOutcomes = new[]
        {
            MarketOutcome.Create("Yes", Probability.Create(0.7m), TokenId.Create("tok-yes")),
            MarketOutcome.Create("No", Probability.Create(0.3m), TokenId.Create("tok-no"))
        };

        market.UpdateOutcomes(newOutcomes, 60_000m, 0.01m);

        market.Outcomes.Should().HaveCount(2);
        market.Outcomes[0].Price.Value.Should().Be(0.7m);
        market.VolumeUsd.Should().Be(60_000m);
        market.Spread.Should().Be(0.01m);
        market.Version.Should().Be(1);
    }

    [Fact]
    public void Close_ShouldSetStatusToClosed()
    {
        var market = CreateValidMarket();

        market.Close();

        market.Status.Should().Be(MarketStatus.Closed);
    }

    [Fact]
    public void Resolve_ShouldSetStatusAndOutcome()
    {
        var market = CreateValidMarket();

        market.Resolve("Yes");

        market.Status.Should().Be(MarketStatus.Resolved);
        market.ResolvedOutcome.Should().Be("Yes");
    }

    [Fact]
    public void Resolve_ShouldThrow_ForEmptyOutcome()
    {
        var market = CreateValidMarket();

        var act = () => market.Resolve("");

        act.Should().Throw<ArgumentException>();
    }
}
