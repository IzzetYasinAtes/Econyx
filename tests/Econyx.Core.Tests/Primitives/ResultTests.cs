using Econyx.Core.Primitives;
using FluentAssertions;

namespace Econyx.Core.Tests.Primitives;

public sealed class ResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_ShouldCreateFailedResult()
    {
        var error = Error.Failure("Something went wrong");

        var result = Result.Failure(error);

        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void SuccessGeneric_ShouldStoreValue()
    {
        var result = Result.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void FailureGeneric_ShouldThrowWhenAccessingValue()
    {
        var result = Result.Failure<int>(Error.Failure("fail"));

        var act = () => result.Value;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ImplicitConversion_ShouldCreateSuccessResult()
    {
        Result<string> result = "hello";

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("hello");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenSuccessWithError()
    {
        var act = () => Result.Failure<int>(Error.None);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenFailureWithoutError()
    {
        var act = () => new TestResult(false, Error.None);

        act.Should().Throw<InvalidOperationException>();
    }

    private sealed class TestResult : Result
    {
        public TestResult(bool isSuccess, Error error) : base(isSuccess, error) { }
    }
}
