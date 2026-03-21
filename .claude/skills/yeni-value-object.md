---
name: yeni-value-object
description: Yeni bir domain value object olusturma rehberi
---

# Yeni Value Object Olusturma

## Adim 1: Value Object (src/Econyx.Domain/ValueObjects/)

```csharp
namespace Econyx.Domain.ValueObjects;

using Econyx.Core.ValueObjects;

public sealed class {Name} : ValueObject
{
    // Private constructor
    private {Name}(/* parametreler */) { }

    // Public properties (readonly)
    public decimal Value { get; }

    // Static factory method
    public static {Name} Create(decimal value)
    {
        if (value < 0)
            throw new ArgumentException("{Name} negatif olamaz.", nameof(value));

        return new {Name}(value);
    }

    // Davranis metodlari
    public bool MeetsThreshold(decimal threshold) => Value >= threshold;

    // ValueObject base zorunlulugu
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Value;
    }
}
```

## Adim 2: EF Core Configuration (owned type veya conversion)

### Owned Type (Money gibi birden fazla property):
```csharp
builder.OwnsOne(x => x.{Name}, nav =>
{
    nav.Property(p => p.Value).HasColumnName("{Name}Value");
});
```

### Value Conversion (tek property):
```csharp
builder.Property(x => x.{Name})
    .HasConversion(
        v => v.Value,
        v => {Name}.Create(v));
```

## Adim 3: Test (tests/Econyx.Domain.Tests/ValueObjects/)

```csharp
public sealed class {Name}Tests
{
    [Fact]
    public void Create_WithValidValue_ShouldSucceed() { }

    [Fact]
    public void Create_WithInvalidValue_ShouldThrowException() { }

    [Fact]
    public void Equals_WithSameValues_ShouldBeTrue() { }

    [Fact]
    public void Equals_WithDifferentValues_ShouldBeFalse() { }
}
```

## Notlar
- Value object'ler IMMUTABLE olmali (sadece readonly property'ler)
- `sealed class` kullan
- `GetAtomicValues()` esitlik kontrolu icin tum alanları dondurur
- Domain mantigi value object icinde olmali (ornegin `Edge.IsActionable()`, `Probability.ToPercentage()`)
