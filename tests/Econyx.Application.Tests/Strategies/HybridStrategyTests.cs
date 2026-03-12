using Econyx.Application.Configuration;
using Econyx.Application.Ports;
using Econyx.Application.Strategies;
using Econyx.Domain.Entities;
using Econyx.Domain.Enums;
using Econyx.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace Econyx.Application.Tests.Strategies;

public sealed class HybridStrategyTests
{
    private readonly TradingOptions _options = new()
    {
        MinVolumeUsd = 10_000m,
        MaxSpreadCents = 5m,
        MinEdgeThreshold = 0.05m
    };

    private static Market CreateMarket(decimal yesPrice)
    {
        var outcomes = new[]
        {
            MarketOutcome.Create("Yes", Probability.Create(yesPrice), TokenId.Create("tok-yes")),
            MarketOutcome.Create("No", Probability.Create(1 - yesPrice), TokenId.Create("tok-no"))
        };

        return Market.Create(
            $"ext-{Guid.NewGuid():N}", PlatformType.Polymarket,
            "Test?", "Desc", "Cat", outcomes, 50_000m, 0.02m);
    }

    [Fact]
    public void Name_ShouldBeHybrid()
    {
        var mockFactory = new Mock<IAiProviderFactory>();
        var ruleStrategy = new RuleBasedStrategy(Options.Create(_options));
        var aiStrategy = new AiAnalysisStrategy(mockFactory.Object, Options.Create(_options));
        var hybrid = new HybridStrategy(ruleStrategy, aiStrategy);

        hybrid.Name.Should().Be("Hybrid");
    }

    [Fact]
    public async Task EvaluateAsync_ShouldReturnEmpty_WhenNoRuleSignals()
    {
        var mockFactory = new Mock<IAiProviderFactory>();
        var ruleStrategy = new RuleBasedStrategy(Options.Create(_options));
        var aiStrategy = new AiAnalysisStrategy(mockFactory.Object, Options.Create(_options));
        var hybrid = new HybridStrategy(ruleStrategy, aiStrategy);

        var market = CreateMarket(yesPrice: 0.50m);

        var signals = await hybrid.EvaluateAsync([market]);

        signals.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_ShouldCombineSignals_WhenBothAgree()
    {
        var market = CreateMarket(yesPrice: 0.08m);

        var mockAiService = new Mock<IAiAnalysisService>();
        mockAiService
            .Setup(x => x.AnalyzeMarketAsync(It.IsAny<MarketAnalysisRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FairValueResult(
                [new OutcomeFairValue("Yes", Probability.Create(0.20m))],
                0.8m, "AI says buy", 0.01m));

        var mockFactory = new Mock<IAiProviderFactory>();
        mockFactory
            .Setup(x => x.GetProviderAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockAiService.Object);

        var ruleStrategy = new RuleBasedStrategy(Options.Create(_options));
        var aiStrategy = new AiAnalysisStrategy(mockFactory.Object, Options.Create(_options));
        var hybrid = new HybridStrategy(ruleStrategy, aiStrategy);

        var signals = await hybrid.EvaluateAsync([market]);

        signals.Should().ContainSingle();
        signals[0].StrategyName.Should().Be("Hybrid");
        signals[0].Reasoning.Should().Contain("[Rule]").And.Contain("[AI]");
    }
}
