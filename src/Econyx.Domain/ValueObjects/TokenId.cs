using Econyx.Core.ValueObjects;

namespace Econyx.Domain.ValueObjects;

public sealed class TokenId : ValueObject
{
    public string Value { get; }

    private TokenId(string value) => Value = value;

    public static TokenId Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new TokenId(value);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
