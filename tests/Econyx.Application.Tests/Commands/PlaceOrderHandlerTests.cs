using Econyx.Application.Commands.PlaceOrder;
using Econyx.Application.Ports;
using Econyx.Application.Strategies;
using Econyx.Core.Interfaces;
using Econyx.Domain.Entities;
using Econyx.Domain.Enums;
using Econyx.Domain.Repositories;
using Econyx.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace Econyx.Application.Tests.Commands;

public sealed class PlaceOrderHandlerTests
{
    private readonly Mock<IPlatformAdapter> _platformMock = new();
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly Mock<IPositionRepository> _positionRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private PlaceOrderHandler CreateHandler() => new(
        _platformMock.Object,
        _orderRepoMock.Object,
        _positionRepoMock.Object,
        _unitOfWorkMock.Object);

    private static StrategySignal CreateSignal(
        TradeSide side = TradeSide.Yes,
        decimal marketPrice = 0.30m) =>
        new(
            Guid.NewGuid(),
            "Will BTC hit 100k?",
            "tok-yes",
            side,
            Edge.Create(0.10m),
            Probability.Create(0.40m),
            Probability.Create(marketPrice),
            0.8m,
            "RuleBased",
            "Price is low");

    [Fact]
    public async Task Handle_PaperMode_ShouldCreateOrderAndPosition()
    {
        _platformMock.Setup(x => x.Platform).Returns(PlatformType.Polymarket);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = CreateHandler();
        var signal = CreateSignal();
        var command = new PlaceOrderCommand(signal, Money.Create(30m), TradingMode.Paper);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        _orderRepoMock.Verify(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
        _positionRepoMock.Verify(x => x.AddAsync(It.IsAny<Position>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_LiveMode_ShouldCallPlatform()
    {
        _platformMock.Setup(x => x.Platform).Returns(PlatformType.Polymarket);
        _platformMock
            .Setup(x => x.PlaceOrderAsync(It.IsAny<PlaceOrderRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("poly-order-123");
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = CreateHandler();
        var signal = CreateSignal();
        var command = new PlaceOrderCommand(signal, Money.Create(30m), TradingMode.Live);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _platformMock.Verify(
            x => x.PlaceOrderAsync(It.IsAny<PlaceOrderRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_LiveMode_PlatformFails_ShouldReturnFailure()
    {
        _platformMock.Setup(x => x.Platform).Returns(PlatformType.Polymarket);
        _platformMock
            .Setup(x => x.PlaceOrderAsync(It.IsAny<PlaceOrderRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Platform unavailable"));
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = CreateHandler();
        var signal = CreateSignal();
        var command = new PlaceOrderCommand(signal, Money.Create(30m), TradingMode.Live);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Platform unavailable");
    }

    [Fact]
    public async Task Handle_ShouldCalculateCorrectQuantity()
    {
        _platformMock.Setup(x => x.Platform).Returns(PlatformType.Polymarket);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        Order? capturedOrder = null;
        _orderRepoMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((o, _) => capturedOrder = o);

        var handler = CreateHandler();
        var signal = CreateSignal(marketPrice: 0.50m);
        var command = new PlaceOrderCommand(signal, Money.Create(50m), TradingMode.Paper);

        await handler.Handle(command, CancellationToken.None);

        capturedOrder.Should().NotBeNull();
        capturedOrder!.Quantity.Should().Be(100m);
        capturedOrder.Price.Amount.Should().Be(0.50m);
    }
}
