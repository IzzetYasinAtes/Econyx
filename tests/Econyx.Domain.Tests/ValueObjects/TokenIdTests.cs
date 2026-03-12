using Econyx.Domain.ValueObjects;
using FluentAssertions;

namespace Econyx.Domain.Tests.ValueObjects;

public sealed class TokenIdTests
{
    [Fact]
    public void Create_ShouldStoreValue()
    {
        var token = TokenId.Create("abc123");

        token.Value.Should().Be("abc123");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_ShouldThrow_ForInvalidInput(string? value)
    {
        var act = () => TokenId.Create(value!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equality_ShouldReturnTrue_ForSameValue()
    {
        var a = TokenId.Create("token1");
        var b = TokenId.Create("token1");

        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equality_ShouldReturnFalse_ForDifferentValues()
    {
        var a = TokenId.Create("token1");
        var b = TokenId.Create("token2");

        (a != b).Should().BeTrue();
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        TokenId.Create("xyz").ToString().Should().Be("xyz");
    }
}
