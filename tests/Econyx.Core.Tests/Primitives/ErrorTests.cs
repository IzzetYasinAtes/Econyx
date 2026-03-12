using Econyx.Core.Primitives;
using FluentAssertions;

namespace Econyx.Core.Tests.Primitives;

public sealed class ErrorTests
{
    [Fact]
    public void None_ShouldHaveEmptyCodeAndMessage()
    {
        Error.None.Code.Should().BeEmpty();
        Error.None.Message.Should().BeEmpty();
    }

    [Fact]
    public void NullValue_ShouldHaveCorrectCode()
    {
        Error.NullValue.Code.Should().Be("Error.NullValue");
    }

    [Fact]
    public void Validation_ShouldCreateValidationError()
    {
        var error = Error.Validation("Name is required");

        error.Code.Should().Be("Error.Validation");
        error.Message.Should().Be("Name is required");
    }

    [Fact]
    public void NotFound_ShouldIncludeEntityAndId()
    {
        var error = Error.NotFound("Market", Guid.Empty);

        error.Code.Should().Be("Error.NotFound");
        error.Message.Should().Contain("Market").And.Contain(Guid.Empty.ToString());
    }

    [Fact]
    public void Conflict_ShouldCreateConflictError()
    {
        var error = Error.Conflict("Already exists");

        error.Code.Should().Be("Error.Conflict");
        error.Message.Should().Be("Already exists");
    }

    [Fact]
    public void Failure_ShouldCreateFailureError()
    {
        var error = Error.Failure("Something broke");

        error.Code.Should().Be("Error.Failure");
        error.Message.Should().Be("Something broke");
    }

    [Fact]
    public void Equality_ShouldWorkCorrectly()
    {
        var error1 = Error.Validation("test");
        var error2 = Error.Validation("test");

        error1.Should().Be(error2);
    }
}
