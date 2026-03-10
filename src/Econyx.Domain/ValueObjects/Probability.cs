using System.Diagnostics.CodeAnalysis;
using Econyx.Core.ValueObjects;

namespace Econyx.Domain.ValueObjects;

[SuppressMessage("Usage", "CA1036:Override methods on comparable types", Justification = "== and != are defined in ValueObject base class")]
public sealed class Probability : ValueObject, IComparable<Probability>
{
    public decimal Value { get; }

    private Probability(decimal value) => Value = value;

    public static Probability Create(decimal value)
    {
        if (value is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(value), "Probability must be between 0 and 1.");

        return new Probability(value);
    }

    public static Probability FromPercentage(decimal percentage) =>
        Create(percentage / 100m);

    public static Probability Even => new(0.5m);
    public static Probability Certain => new(1m);
    public static Probability Impossible => new(0m);

    public decimal ToPercentage() => Value * 100m;

    public int CompareTo(Probability? other) =>
        other is null ? 1 : Value.CompareTo(other.Value);

    public static bool operator >(Probability left, Probability right) =>
        left.Value > right.Value;

    public static bool operator <(Probability left, Probability right) =>
        left.Value < right.Value;

    public static bool operator >=(Probability left, Probability right) =>
        left.Value >= right.Value;

    public static bool operator <=(Probability left, Probability right) =>
        left.Value <= right.Value;

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToString() => $"{Value:P1}";
}
