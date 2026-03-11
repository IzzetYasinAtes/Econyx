using Econyx.Core.Entities;
using Econyx.Domain.Enums;
using Econyx.Domain.Events;
using Econyx.Domain.Services;
using Econyx.Domain.ValueObjects;

namespace Econyx.Domain.Entities;

public sealed class Position : AggregateRoot<Guid>
{
    public Guid MarketId { get; private set; }
    public string MarketQuestion { get; private set; } = null!;
    public string TokenId { get; private set; } = null!;
    public PlatformType Platform { get; private set; }
    public TradeSide Side { get; private set; }
    public Money EntryPrice { get; private set; } = null!;
    public Money CurrentPrice { get; private set; } = null!;
    public decimal Quantity { get; private set; }
    public string StrategyName { get; private set; } = null!;
    public bool IsOpen { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public Money? ExitPrice { get; private set; }

    private Position() { }

    public static Position Create(
        Guid marketId,
        string marketQuestion,
        string tokenId,
        PlatformType platform,
        TradeSide side,
        Money entryPrice,
        decimal quantity,
        string strategyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(marketQuestion);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenId);
        ArgumentException.ThrowIfNullOrWhiteSpace(strategyName);
        ArgumentNullException.ThrowIfNull(entryPrice);

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");

        return new Position
        {
            Id = Guid.NewGuid(),
            MarketId = marketId,
            MarketQuestion = marketQuestion,
            TokenId = tokenId,
            Platform = platform,
            Side = side,
            EntryPrice = entryPrice,
            CurrentPrice = entryPrice,
            Quantity = quantity,
            StrategyName = strategyName,
            IsOpen = true
        };
    }

    public void UpdatePrice(Money currentPrice)
    {
        ArgumentNullException.ThrowIfNull(currentPrice);

        if (!IsOpen)
            throw new InvalidOperationException("Cannot update price on a closed position.");

        CurrentPrice = currentPrice;
        IncrementVersion();
    }

    public void Close(Money exitPrice)
    {
        ArgumentNullException.ThrowIfNull(exitPrice);

        if (!IsOpen)
            throw new InvalidOperationException("Position is already closed.");

        ExitPrice = exitPrice;
        CurrentPrice = exitPrice;
        IsOpen = false;
        ClosedAt = DateTime.UtcNow;
        IncrementVersion();

        RaiseDomainEvent(new PositionClosedEvent(Id, MarketId, CalculatePnL(), StrategyName));
    }

    public Money CalculatePnL() =>
        PnLCalculator.CalculateUnrealizedPnL(EntryPrice, ExitPrice ?? CurrentPrice, Quantity, Side);
}
