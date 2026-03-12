using Econyx.Core.ValueObjects;
using FluentAssertions;

namespace Econyx.Core.Tests.ValueObjects;

public sealed class ValueObjectTests
{
    [Fact]
    public void Equals_ShouldReturnTrue_ForSameValues()
    {
        var a = new TestValueObject("hello", 42);
        var b = new TestValueObject("hello", 42);

        a.Equals(b).Should().BeTrue();
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_ForDifferentValues()
    {
        var a = new TestValueObject("hello", 42);
        var b = new TestValueObject("world", 42);

        a.Equals(b).Should().BeFalse();
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_ForNull()
    {
        var a = new TestValueObject("hello", 42);

        a.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_ShouldBeEqual_ForSameValues()
    {
        var a = new TestValueObject("hello", 42);
        var b = new TestValueObject("hello", 42);

        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void GetHashCode_ShouldDiffer_ForDifferentValues()
    {
        var a = new TestValueObject("hello", 42);
        var b = new TestValueObject("world", 99);

        a.GetHashCode().Should().NotBe(b.GetHashCode());
    }

    [Fact]
    public void Operators_ShouldHandleNulls()
    {
        TestValueObject? a = null;
        TestValueObject? b = null;

        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
    }

    private sealed class TestValueObject : ValueObject
    {
        private readonly string _text;
        private readonly int _number;

        public TestValueObject(string text, int number)
        {
            _text = text;
            _number = number;
        }

        protected override IEnumerable<object?> GetAtomicValues()
        {
            yield return _text;
            yield return _number;
        }
    }
}
