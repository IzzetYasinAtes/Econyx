using Econyx.Domain.Entities;
using Econyx.Domain.Enums;
using Econyx.Domain.ValueObjects;
using Econyx.Infrastructure.Persistence.Repositories;
using FluentAssertions;

namespace Econyx.Infrastructure.Tests.Persistence;

public sealed class TradeRepositoryTests : IDisposable
{
    private readonly Econyx.Infrastructure.Persistence.EconyxDbContext _context;
    private readonly TradeRepository _repository;

    public TradeRepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _repository = new TradeRepository(_context);
    }

    private static Trade CreateTrade(Guid? marketId = null)
    {
        return Trade.Create(
            Guid.NewGuid(),
            marketId ?? Guid.NewGuid(),
            "Test market?",
            TradeSide.Yes,
            Money.Create(0.30m),
            Money.Create(0.50m),
            100m,
            Money.Create(20m),
            Money.Create(0.5m),
            "Hybrid",
            PlatformType.Polymarket,
            TimeSpan.FromHours(2));
    }

    [Fact]
    public async Task AddAsync_ShouldPersistTrade()
    {
        var trade = CreateTrade();

        await _repository.AddAsync(trade);
        await _context.SaveChangesAsync();

        var found = await _repository.GetByIdAsync(trade.Id);
        found.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByMarketIdAsync_ShouldFilterByMarketId()
    {
        var marketId = Guid.NewGuid();
        var matchingTrade = CreateTrade(marketId);
        var otherTrade = CreateTrade();

        await _repository.AddAsync(matchingTrade);
        await _repository.AddAsync(otherTrade);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByMarketIdAsync(marketId);

        result.Should().ContainSingle();
        result[0].MarketId.Should().Be(marketId);
    }

    [Fact]
    public async Task GetRecentAsync_ShouldReturnLimitedResults()
    {
        for (var i = 0; i < 5; i++)
        {
            await _repository.AddAsync(CreateTrade());
        }

        await _context.SaveChangesAsync();

        var result = await _repository.GetRecentAsync(3);

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task CountAsync_ShouldReturnTotalCount()
    {
        await _repository.AddAsync(CreateTrade());
        await _repository.AddAsync(CreateTrade());
        await _context.SaveChangesAsync();

        var count = await _repository.CountAsync();

        count.Should().Be(2);
    }

    public void Dispose() => _context.Dispose();
}
