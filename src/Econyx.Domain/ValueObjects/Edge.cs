using System.Diagnostics.CodeAnalysis;
using Econyx.Core.ValueObjects;

namespace Econyx.Domain.ValueObjects;

[SuppressMessage("Usage", "CA1036:Override methods on comparable types", Justification = "== and != are defined in ValueObject base class")]
public sealed class Edge : ValueObject, IComparable<Edge>
{
    public decimal Value { get; }

    private Edge() => Value = 0;
    private Edge(decimal value) => Value = value;

    public static Edge Create(decimal value) => new(value);

    public decimal AbsoluteValue => Math.Abs(Value);

    public bool IsActionable(decimal threshold) => AbsoluteValue >= threshold;

    public int CompareTo(Edge? other) =>
        other is null ? 1 : Value.CompareTo(other.Value);

    public static bool operator >(Edge left, Edge right) =>
        left.Value > right.Value;

    public static bool operator <(Edge left, Edge right) =>
        left.Value < right.Value;

    public static bool operator >=(Edge left, Edge right) =>
        left.Value >= right.Value;

    public static bool operator <=(Edge left, Edge right) =>
        left.Value <= right.Value;

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToString() => $"{Value:P2} edge";
}
