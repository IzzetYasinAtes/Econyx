using Econyx.Core.Entities;
using Econyx.Domain.Enums;
using Econyx.Domain.ValueObjects;

namespace Econyx.Domain.Entities;

public sealed class BalanceSnapshot : BaseEntity<Guid>
{
    public Money Balance { get; private set; } = null!;
    public Money TotalPnL { get; private set; } = null!;
    public decimal TotalPnLPercent { get; private set; }
    public int OpenPositionCount { get; private set; }
    public int TotalTrades { get; private set; }
    public decimal WinRate { get; private set; }
    public Money ApiCosts { get; private set; } = null!;
    public TradingMode Mode { get; private set; }

    private BalanceSnapshot() { }

    public static BalanceSnapshot Create(
        Money balance,
        Money totalPnL,
        decimal totalPnLPercent,
        int openPositionCount,
        int totalTrades,
        decimal winRate,
        Money apiCosts,
        TradingMode mode)
    {
        ArgumentNullException.ThrowIfNull(balance);
        ArgumentNullException.ThrowIfNull(totalPnL);
        ArgumentNullException.ThrowIfNull(apiCosts);

        return new BalanceSnapshot
        {
            Id = Guid.NewGuid(),
            Balance = balance,
            TotalPnL = totalPnL,
            TotalPnLPercent = totalPnLPercent,
            OpenPositionCount = openPositionCount,
            TotalTrades = totalTrades,
            WinRate = winRate,
            ApiCosts = apiCosts,
            Mode = mode
        };
    }
}
