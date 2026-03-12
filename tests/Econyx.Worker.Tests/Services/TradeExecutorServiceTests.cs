using Econyx.Application.Ports;
using Econyx.Core.Interfaces;
using Econyx.Domain.Entities;
using Econyx.Domain.Enums;
using Econyx.Domain.Repositories;
using Econyx.Domain.ValueObjects;
using Econyx.Worker.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Econyx.Worker.Tests.Services;

public sealed class TradeExecutorServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly Mock<IPlatformAdapter> _platformMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private TradeExecutorService CreateService()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_orderRepoMock.Object);
        services.AddSingleton(_platformMock.Object);
        services.AddSingleton(_unitOfWorkMock.Object);
        var provider = services.BuildServiceProvider();

        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        return new TradeExecutorService(
            scopeFactory,
            NullLogger<TradeExecutorService>.Instance);
    }

    private static Order CreatePendingLiveOrder()
    {
        var order = Order.Create(
            Guid.NewGuid(), "tok-yes", TradeSide.Yes, OrderType.Limit,
            Money.Create(0.30m), 100m, TradingMode.Live, PlatformType.Polymarket);

        order.SetPlatformOrderId("poly-123");
        return order;
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFillLivePendingOrders()
    {
        var order = CreatePendingLiveOrder();

        _orderRepoMock
            .Setup(x => x.GetPendingOrdersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { order });

        _platformMock
            .Setup(x => x.GetPriceAsync("tok-yes", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Probability.Create(0.35m));

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = CreateService();

        var task = service.StartAsync(CancellationToken.None);
        await Task.Delay(2000);
        await service.StopAsync(CancellationToken.None);

        order.Status.Should().Be(OrderStatus.Filled);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldDoNothing_WhenNoPendingOrders()
    {
        _orderRepoMock
            .Setup(x => x.GetPendingOrdersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        var service = CreateService();

        var task = service.StartAsync(CancellationToken.None);
        await Task.Delay(2000);
        await service.StopAsync(CancellationToken.None);

        _platformMock.Verify(
            x => x.GetPriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSkipPaperOrders()
    {
        var paperOrder = Order.Create(
            Guid.NewGuid(), "tok-yes", TradeSide.Yes, OrderType.Limit,
            Money.Create(0.30m), 100m, TradingMode.Paper, PlatformType.Polymarket);

        _orderRepoMock
            .Setup(x => x.GetPendingOrdersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { paperOrder });

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = CreateService();

        var task = service.StartAsync(CancellationToken.None);
        await Task.Delay(2000);
        await service.StopAsync(CancellationToken.None);

        _platformMock.Verify(
            x => x.GetPriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        paperOrder.Status.Should().Be(OrderStatus.Pending);
    }
}
