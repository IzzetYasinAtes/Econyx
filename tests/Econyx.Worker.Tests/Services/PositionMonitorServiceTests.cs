using Econyx.Application.Commands.ClosePosition;
using Econyx.Application.Configuration;
using Econyx.Application.Ports;
using Econyx.Core.Primitives;
using Econyx.Domain.Entities;
using Econyx.Domain.Enums;
using Econyx.Domain.Repositories;
using Econyx.Domain.ValueObjects;
using Econyx.Worker.Services;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Econyx.Worker.Tests.Services;

public sealed class PositionMonitorServiceTests
{
    private readonly Mock<IPositionRepository> _positionRepoMock = new();
    private readonly Mock<IPlatformAdapter> _platformMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();

    private readonly TradingOptions _options = new()
    {
        StopLossPercent = 50m,
        TakeProfitPercent = 100m,
        MaxHoldMinutes = 1440
    };

    private PositionMonitorService CreateService()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_positionRepoMock.Object);
        services.AddSingleton(_platformMock.Object);
        services.AddSingleton(_mediatorMock.Object);
        var provider = services.BuildServiceProvider();

        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        return new PositionMonitorService(
            scopeFactory,
            Options.Create(_options),
            NullLogger<PositionMonitorService>.Instance);
    }

    private static Position CreateOpenPosition(
        decimal entryPrice = 0.30m,
        decimal quantity = 100m,
        TradeSide side = TradeSide.Yes)
    {
        return Position.Create(
            Guid.NewGuid(), "Test?", "tok-yes",
            PlatformType.Polymarket, side,
            Money.Create(entryPrice), quantity, "RuleBased");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldTriggerStopLoss_WhenLossExceedsThreshold()
    {
        var position = CreateOpenPosition(entryPrice: 0.50m, quantity: 100m);

        _positionRepoMock
            .Setup(x => x.GetOpenPositionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Position> { position });

        _platformMock
            .Setup(x => x.GetPriceAsync("tok-yes", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Probability.Create(0.10m));

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ClosePositionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Money.Create(0m)));

        var service = CreateService();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var task = service.StartAsync(cts.Token);
        await Task.Delay(2000);
        await service.StopAsync(CancellationToken.None);

        _mediatorMock.Verify(
            x => x.Send(It.IsAny<ClosePositionCommand>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldTriggerTakeProfit_WhenProfitExceedsThreshold()
    {
        var position = CreateOpenPosition(entryPrice: 0.30m, quantity: 100m);

        _positionRepoMock
            .Setup(x => x.GetOpenPositionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Position> { position });

        _platformMock
            .Setup(x => x.GetPriceAsync("tok-yes", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Probability.Create(0.95m));

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ClosePositionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(Money.Create(0m)));

        var service = CreateService();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var task = service.StartAsync(cts.Token);
        await Task.Delay(2000);
        await service.StopAsync(CancellationToken.None);

        _mediatorMock.Verify(
            x => x.Send(It.IsAny<ClosePositionCommand>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotClose_WhenPnLWithinBounds()
    {
        var position = CreateOpenPosition(entryPrice: 0.40m, quantity: 100m);

        _positionRepoMock
            .Setup(x => x.GetOpenPositionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Position> { position });

        _platformMock
            .Setup(x => x.GetPriceAsync("tok-yes", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Probability.Create(0.45m));

        var service = CreateService();

        var task = service.StartAsync(CancellationToken.None);
        await Task.Delay(2000);
        await service.StopAsync(CancellationToken.None);

        _mediatorMock.Verify(
            x => x.Send(It.IsAny<ClosePositionCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldDoNothing_WhenNoOpenPositions()
    {
        _positionRepoMock
            .Setup(x => x.GetOpenPositionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Position>());

        var service = CreateService();

        var task = service.StartAsync(CancellationToken.None);
        await Task.Delay(2000);
        await service.StopAsync(CancellationToken.None);

        _platformMock.Verify(
            x => x.GetPriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
