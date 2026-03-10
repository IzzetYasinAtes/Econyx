using Econyx.Core.ValueObjects;

namespace Econyx.Domain.ValueObjects;

public sealed class MarketOutcome : ValueObject
{
    public string Name { get; }
    public Probability Price { get; }
    public TokenId Token { get; }

    #pragma warning disable CS8618
    private MarketOutcome() { }
    #pragma warning restore CS8618

    private MarketOutcome(string name, Probability price, TokenId token)
    {
        Name = name;
        Price = price;
        Token = token;
    }

    public static MarketOutcome Create(string name, Probability price, TokenId token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(price);
        ArgumentNullException.ThrowIfNull(token);

        return new MarketOutcome(name, price, token);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Name;
        yield return Price;
        yield return Token;
    }

    public override string ToString() => $"{Name} @ {Price}";
}
