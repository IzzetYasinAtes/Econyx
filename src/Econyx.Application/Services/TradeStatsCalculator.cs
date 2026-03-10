namespace Econyx.Application.Services;

using Econyx.Domain.Entities;
using Econyx.Domain.ValueObjects;

public static class TradeStatsCalculator
{
    public static (Money TotalPnL, decimal WinRate) Calculate(IReadOnlyList<Trade> trades)
    {
        if (trades.Count == 0)
            return (Money.Zero(), 0m);

        var totalPnL = trades.Aggregate(Money.Zero(), (sum, t) => sum + t.PnL);
        var winRate = (decimal)trades.Count(t => t.PnL.Amount > 0) / trades.Count;

        return (totalPnL, winRate);
    }
}
