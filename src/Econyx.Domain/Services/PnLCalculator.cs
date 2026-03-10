using Econyx.Domain.Enums;
using Econyx.Domain.ValueObjects;

namespace Econyx.Domain.Services;

public static class PnLCalculator
{
    public static Money CalculateUnrealizedPnL(
        Money entryPrice,
        Money currentPrice,
        decimal quantity,
        TradeSide side)
    {
        ArgumentNullException.ThrowIfNull(entryPrice);
        ArgumentNullException.ThrowIfNull(currentPrice);

        return side == TradeSide.Yes
            ? (currentPrice - entryPrice) * quantity
            : (entryPrice - currentPrice) * quantity;
    }

    public static Money CalculateRealizedPnL(
        Money entryPrice,
        Money exitPrice,
        decimal quantity,
        TradeSide side,
        Money fees)
    {
        ArgumentNullException.ThrowIfNull(entryPrice);
        ArgumentNullException.ThrowIfNull(exitPrice);
        ArgumentNullException.ThrowIfNull(fees);

        var grossPnl = side == TradeSide.Yes
            ? (exitPrice - entryPrice) * quantity
            : (entryPrice - exitPrice) * quantity;

        return grossPnl - fees;
    }
}
