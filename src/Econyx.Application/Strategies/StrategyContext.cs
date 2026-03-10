namespace Econyx.Application.Strategies;

using Econyx.Application.Configuration;
using Econyx.Domain.Entities;
using Econyx.Domain.ValueObjects;

public sealed class StrategyContext
{
    public Money Balance { get; }
    public IReadOnlyList<Position> OpenPositions { get; }
    public TradingOptions Options { get; }

    public StrategyContext(Money balance, IReadOnlyList<Position> openPositions, TradingOptions options)
    {
        ArgumentNullException.ThrowIfNull(balance);
        ArgumentNullException.ThrowIfNull(openPositions);
        ArgumentNullException.ThrowIfNull(options);

        Balance = balance;
        OpenPositions = openPositions;
        Options = options;
    }
}
