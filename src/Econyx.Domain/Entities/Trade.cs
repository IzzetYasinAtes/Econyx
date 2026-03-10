using Econyx.Core.Entities;
using Econyx.Domain.Enums;
using Econyx.Domain.ValueObjects;

namespace Econyx.Domain.Entities;

public sealed class Trade : BaseEntity<Guid>
{
    public Guid PositionId { get; private set; }
    public Guid MarketId { get; private set; }
    public string MarketQuestion { get; private set; } = null!;
    public TradeSide Side { get; private set; }
    public Money EntryPrice { get; private set; } = null!;
    public Money ExitPrice { get; private set; } = null!;
    public decimal Quantity { get; private set; }
    public Money PnL { get; private set; } = null!;
    public Money Fees { get; private set; } = null!;
    public string StrategyName { get; private set; } = null!;
    public PlatformType Platform { get; private set; }
    public TimeSpan Duration { get; private set; }
    public DateTime ClosedAt { get; private set; }

    private Trade() { }

    public static Trade Create(
        Guid positionId,
        Guid marketId,
        string marketQuestion,
        TradeSide side,
        Money entryPrice,
        Money exitPrice,
        decimal quantity,
        Money pnl,
        Money fees,
        string strategyName,
        PlatformType platform,
        TimeSpan duration)
    {
        return new Trade
        {
            Id = Guid.NewGuid(),
            PositionId = positionId,
            MarketId = marketId,
            MarketQuestion = marketQuestion,
            Side = side,
            EntryPrice = entryPrice,
            ExitPrice = exitPrice,
            Quantity = quantity,
            PnL = pnl,
            Fees = fees,
            StrategyName = strategyName,
            Platform = platform,
            Duration = duration,
            ClosedAt = DateTime.UtcNow
        };
    }
}
