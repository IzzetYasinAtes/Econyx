using System.Diagnostics.CodeAnalysis;
using Econyx.Core.ValueObjects;

namespace Econyx.Domain.ValueObjects;

[SuppressMessage("Usage", "CA1036:Override methods on comparable types", Justification = "== and != are defined in ValueObject base class")]
public sealed class Money : ValueObject, IComparable<Money>
{
    public decimal Amount { get; }
    public string Currency { get; }

    #pragma warning disable CS8618
    private Money() { }
    #pragma warning restore CS8618

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency = "USDC")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");

        return new Money(amount, currency);
    }

    public static Money Zero(string currency = "USDC") => new(0, currency);

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount - right.Amount, left.Currency);
    }

    public static Money operator *(Money money, decimal multiplier) =>
        new(money.Amount * multiplier, money.Currency);

    public static Money operator *(decimal multiplier, Money money) =>
        money * multiplier;

    public static bool operator >(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount > right.Amount;
    }

    public static bool operator <(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount < right.Amount;
    }

    public static bool operator >=(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount >= right.Amount;
    }

    public static bool operator <=(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount <= right.Amount;
    }

    public int CompareTo(Money? other)
    {
        if (other is null) return 1;
        EnsureSameCurrency(this, other);
        return Amount.CompareTo(other.Amount);
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Amount;
        yield return Currency;
    }

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException(
                $"Cannot operate on Money with different currencies: {left.Currency} vs {right.Currency}.");
    }

    public override string ToString() => $"{Amount:F2} {Currency}";
}
