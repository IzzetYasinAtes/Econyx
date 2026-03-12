using Econyx.Domain.Entities;
using Econyx.Domain.Enums;
using Econyx.Domain.ValueObjects;
using Econyx.Infrastructure.Persistence.Repositories;
using FluentAssertions;

namespace Econyx.Infrastructure.Tests.Persistence;

public sealed class OrderRepositoryTests : IDisposable
{
    private readonly Econyx.Infrastructure.Persistence.EconyxDbContext _context;
    private readonly OrderRepository _repository;

    public OrderRepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _repository = new OrderRepository(_context);
    }

    private static Order CreateOrder(
        OrderStatus targetStatus = OrderStatus.Pending,
        TradingMode mode = TradingMode.Paper)
    {
        var order = Order.Create(
            Guid.NewGuid(), "tok-yes", TradeSide.Yes, OrderType.Limit,
            Money.Create(0.30m), 100m, mode, PlatformType.Polymarket);

        if (targetStatus == OrderStatus.Filled)
            order.Fill(Money.Create(0.30m), 100m);

        return order;
    }

    [Fact]
    public async Task AddAsync_ShouldPersistOrder()
    {
        var order = CreateOrder();

        await _repository.AddAsync(order);
        await _context.SaveChangesAsync();

        var found = await _repository.GetByIdAsync(order.Id);
        found.Should().NotBeNull();
        found!.TokenId.Should().Be("tok-yes");
    }

    [Fact]
    public async Task GetPendingOrdersAsync_ShouldReturnOnlyPending()
    {
        var pending = CreateOrder(OrderStatus.Pending);
        var filled = CreateOrder(OrderStatus.Filled);

        await _repository.AddAsync(pending);
        await _repository.AddAsync(filled);
        await _context.SaveChangesAsync();

        var result = await _repository.GetPendingOrdersAsync();

        result.Should().ContainSingle();
        result[0].Id.Should().Be(pending.Id);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenExists()
    {
        var order = CreateOrder();
        await _repository.AddAsync(order);
        await _context.SaveChangesAsync();

        var exists = await _repository.ExistsAsync(order.Id);

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenNotExists()
    {
        var exists = await _repository.ExistsAsync(Guid.NewGuid());

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task CountAsync_ShouldReturnCorrectCount()
    {
        await _repository.AddAsync(CreateOrder());
        await _repository.AddAsync(CreateOrder());
        await _context.SaveChangesAsync();

        var count = await _repository.CountAsync();

        count.Should().Be(2);
    }

    [Fact]
    public async Task Remove_ShouldDeleteOrder()
    {
        var order = CreateOrder();
        await _repository.AddAsync(order);
        await _context.SaveChangesAsync();

        _repository.Remove(order);
        await _context.SaveChangesAsync();

        var found = await _repository.GetByIdAsync(order.Id);
        found.Should().BeNull();
    }

    public void Dispose() => _context.Dispose();
}
