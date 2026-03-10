using Econyx.Domain.ValueObjects;

namespace Econyx.Domain.Services;

public static class EdgeCalculator
{
    public static Edge Calculate(
        Probability fairValue,
        Probability marketPrice,
        decimal estimatedFees = 0.02m)
    {
        ArgumentNullException.ThrowIfNull(fairValue);
        ArgumentNullException.ThrowIfNull(marketPrice);

        decimal rawEdge = Math.Abs(fairValue.Value - marketPrice.Value) - estimatedFees;
        return Edge.Create(rawEdge);
    }
}
