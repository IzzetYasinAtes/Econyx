using Econyx.Domain.ValueObjects;
using FluentAssertions;

namespace Econyx.Domain.Tests.ValueObjects;

public sealed class MoneyTests
{
    [Fact]
    public void Create_ShouldCreateMoney()
    {
        var money = Money.Create(100m);

        money.Amount.Should().Be(100m);
        money.Currency.Should().Be("USDC");
    }

    [Fact]
    public void Create_WithCustomCurrency_ShouldSetCurrency()
    {
        var money = Money.Create(50m, "ETH");

        money.Currency.Should().Be("ETH");
    }

    [Fact]
    public void Create_ShouldThrow_ForNegativeAmount()
    {
        var act = () => Money.Create(-1m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_ShouldThrow_ForNullCurrency()
    {
        var act = () => Money.Create(10m, null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Zero_ShouldReturnZeroAmount()
    {
        var zero = Money.Zero();

        zero.Amount.Should().Be(0);
        zero.Currency.Should().Be("USDC");
    }

    [Fact]
    public void Addition_ShouldAddAmounts()
    {
        var a = Money.Create(30m);
        var b = Money.Create(20m);

        var result = a + b;

        result.Amount.Should().Be(50m);
    }

    [Fact]
    public void Subtraction_ShouldSubtractAmounts()
    {
        var a = Money.Create(50m);
        var b = Money.Create(20m);

        var result = a - b;

        result.Amount.Should().Be(30m);
    }

    [Fact]
    public void Multiplication_ShouldMultiply()
    {
        var money = Money.Create(10m);

        var result = money * 3m;

        result.Amount.Should().Be(30m);
    }

    [Fact]
    public void MultiplicationReversed_ShouldMultiply()
    {
        var money = Money.Create(10m);

        var result = 3m * money;

        result.Amount.Should().Be(30m);
    }

    [Fact]
    public void DifferentCurrency_ShouldThrowOnAddition()
    {
        var usd = Money.Create(10m, "USD");
        var eth = Money.Create(5m, "ETH");

        var act = () => usd + eth;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ComparisonOperators_ShouldWorkCorrectly()
    {
        var small = Money.Create(10m);
        var big = Money.Create(50m);

        (big > small).Should().BeTrue();
        (small < big).Should().BeTrue();
        (big >= small).Should().BeTrue();
        (small <= big).Should().BeTrue();
    }

    [Fact]
    public void Equality_ShouldReturnTrue_ForSameAmountAndCurrency()
    {
        var a = Money.Create(10m);
        var b = Money.Create(10m);

        (a == b).Should().BeTrue();
    }

    [Fact]
    public void CompareTo_ShouldReturnCorrectOrder()
    {
        var small = Money.Create(10m);
        var big = Money.Create(20m);

        small.CompareTo(big).Should().BeNegative();
        big.CompareTo(small).Should().BePositive();
        small.CompareTo(Money.Create(10m)).Should().Be(0);
    }

    [Fact]
    public void CompareTo_ShouldReturnPositive_ForNull()
    {
        var money = Money.Create(10m);

        money.CompareTo(null).Should().BePositive();
    }

    [Fact]
    public void ToString_ShouldContainCurrency()
    {
        var money = Money.Create(99.5m);

        money.ToString().Should().Contain("USDC");
    }
}
