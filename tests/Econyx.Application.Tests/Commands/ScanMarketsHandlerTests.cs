using Econyx.Application.Commands.ScanMarkets;
using Econyx.Application.Ports;
using Econyx.Application.Strategies;
using Econyx.Domain.Entities;
using Econyx.Domain.Enums;
using Econyx.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace Econyx.Application.Tests.Commands;

public sealed class ScanMarketsHandlerTests
{
    private readonly Mock<IPlatformAdapter> _platformMock = new();
    private readonly Mock<IStrategy> _strategyMock = new();

    private ScanMarketsHandler CreateHandler() => new(
        _platformMock.Object,
        _strategyMock.Object);

    private static Market CreateMarket()
    {
        var outcomes = new[]
        {
            MarketOutcome.Create("Yes", Probability.Create(0.5m), TokenId.Create("tok-yes"))
        };

        return Market.Create(
            "ext-1", PlatformType.Polymarket,
            "Test?", "Desc", "Cat", outcomes, 50_000m, 0.02m);
    }

    [Fact]
    public async Task Handle_ShouldReturnScanResults()
    {
        var markets = new List<Market> { CreateMarket(), CreateMarket() };
        var signals = new List<StrategySignal>
        {
            new(Guid.NewGuid(), "Q?", "tok-1", TradeSide.Yes,
                Edge.Create(0.1m), Probability.Create(0.6m), Probability.Create(0.5m),
                0.8m, "Test", "reason")
        };

        _platformMock
            .Setup(x => x.GetMarketsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(markets);

        _strategyMock
            .Setup(x => x.EvaluateAsync(It.IsAny<IReadOnlyList<Market>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(signals);

        var handler = CreateHandler();
        var result = await handler.Handle(new ScanMarketsCommand(), CancellationToken.None);

        result.MarketsScanned.Should().Be(2);
        result.Signals.Should().HaveCount(1);
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptySignals_WhenNoMarkets()
    {
        _platformMock
            .Setup(x => x.GetMarketsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Market>());

        _strategyMock
            .Setup(x => x.EvaluateAsync(It.IsAny<IReadOnlyList<Market>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StrategySignal>());

        var handler = CreateHandler();
        var result = await handler.Handle(new ScanMarketsCommand(), CancellationToken.None);

        result.MarketsScanned.Should().Be(0);
        result.Signals.Should().BeEmpty();
    }
}
