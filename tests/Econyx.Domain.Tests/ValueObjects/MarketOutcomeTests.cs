using Econyx.Domain.ValueObjects;
using FluentAssertions;

namespace Econyx.Domain.Tests.ValueObjects;

public sealed class MarketOutcomeTests
{
    [Fact]
    public void Create_ShouldSetProperties()
    {
        var price = Probability.Create(0.6m);
        var token = TokenId.Create("tok-1");

        var outcome = MarketOutcome.Create("Yes", price, token);

        outcome.Name.Should().Be("Yes");
        outcome.Price.Should().Be(price);
        outcome.Token.Should().Be(token);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrow_ForInvalidName(string? name)
    {
        var act = () => MarketOutcome.Create(name!, Probability.Even, TokenId.Create("t1"));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldThrow_ForNullPrice()
    {
        var act = () => MarketOutcome.Create("Yes", null!, TokenId.Create("t1"));

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_ShouldThrow_ForNullToken()
    {
        var act = () => MarketOutcome.Create("Yes", Probability.Even, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Equality_ShouldReturnTrue_ForSameValues()
    {
        var a = MarketOutcome.Create("Yes", Probability.Create(0.5m), TokenId.Create("t1"));
        var b = MarketOutcome.Create("Yes", Probability.Create(0.5m), TokenId.Create("t1"));

        (a == b).Should().BeTrue();
    }
}
