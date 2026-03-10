using Econyx.Domain.ValueObjects;

namespace Econyx.Domain.Services;

public static class RiskCalculator
{
    public static Money CalculatePositionSize(
        Money balance,
        Edge edge,
        Probability fairValue,
        decimal maxPositionSizePercent)
    {
        ArgumentNullException.ThrowIfNull(balance);
        ArgumentNullException.ThrowIfNull(edge);
        ArgumentNullException.ThrowIfNull(fairValue);

        if (maxPositionSizePercent is <= 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(maxPositionSizePercent), "Must be between 0 (exclusive) and 1.");

        if (fairValue.Value == 0)
            return Money.Zero(balance.Currency);

        decimal odds = (1m / fairValue.Value) - 1m;
        if (odds <= 0)
            return Money.Zero(balance.Currency);

        decimal kellyFraction = edge.AbsoluteValue / odds;
        decimal cappedFraction = Math.Min(kellyFraction, maxPositionSizePercent);
        decimal positionSize = balance.Amount * Math.Max(cappedFraction, 0);

        return Money.Create(Math.Round(positionSize, 2), balance.Currency);
    }
}
