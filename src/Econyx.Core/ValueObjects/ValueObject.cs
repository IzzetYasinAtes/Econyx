namespace Econyx.Core.ValueObjects;

public abstract class ValueObject : IEquatable<ValueObject>
{
    protected abstract IEnumerable<object?> GetAtomicValues();

    public bool Equals(ValueObject? other) =>
        other is not null && GetAtomicValues().SequenceEqual(other.GetAtomicValues());

    public override bool Equals(object? obj) =>
        obj is ValueObject other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var value in GetAtomicValues())
            hash.Add(value);
        return hash.ToHashCode();
    }

    public static bool operator ==(ValueObject? left, ValueObject? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(ValueObject? left, ValueObject? right) =>
        !(left == right);
}
