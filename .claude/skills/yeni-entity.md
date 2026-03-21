---
name: yeni-entity
description: Yeni bir domain entity olusturma rehberi (Onion Architecture kurallarina uygun)
---

# Yeni Entity Olusturma

Asagidaki adimlari sirayla uygula:

## Adim 1: Domain Entity (src/Econyx.Domain/Entities/)

```csharp
public sealed class {EntityName} : BaseEntity<Guid>
{
    // Private constructor
    private {EntityName}() { }

    // Public properties (private set)
    public string Property { get; private set; } = null!;

    // Static factory method
    public static {EntityName} Create(/* parametreler */)
    {
        ArgumentNullException.ThrowIfNull(param);
        return new {EntityName} { /* property atamalari */ };
    }

    // Domain behavior methods
    public void UpdateSomething(/* params */)
    {
        // Validasyon + state degisikligi
    }
}
```

## Adim 2: Repository Interface (src/Econyx.Domain/Repositories/)

```csharp
public interface I{EntityName}Repository : IRepository<{EntityName}, Guid>
{
    // Entity'ye ozel query metodlari
}
```

## Adim 3: EF Core Configuration (src/Econyx.Infrastructure/Persistence/Configurations/)

```csharp
public sealed class {EntityName}Configuration : IEntityTypeConfiguration<{EntityName}>
{
    public void Configure(EntityTypeBuilder<{EntityName}> builder)
    {
        builder.HasKey(x => x.Id);
        // Fluent API ile property konfigurasyonu
        // Money icin: builder.ConfigureMoney(x => x.Amount, "Amount");
    }
}
```

## Adim 4: DbContext'e Ekle (src/Econyx.Infrastructure/Persistence/EconyxDbContext.cs)

```csharp
public DbSet<{EntityName}> {EntityName}s => Set<{EntityName}>();
```

## Adim 5: Repository Implementation (src/Econyx.Infrastructure/Persistence/Repositories/)

```csharp
public sealed class {EntityName}Repository(EconyxDbContext context)
    : I{EntityName}Repository
{
    // IRepository<T, TId> metodlarini implemente et
}
```

## Adim 6: DI Kaydi (src/Econyx.Infrastructure/DependencyInjection.cs)

```csharp
services.AddScoped<I{EntityName}Repository, {EntityName}Repository>();
```

## Adim 7: Migration Olustur

```bash
dotnet ef migrations add Add{EntityName} --project src/Econyx.Infrastructure --startup-project src/Econyx.Worker
```

## Adim 8: Testler (tests/Econyx.Domain.Tests/Entities/)

```csharp
public sealed class {EntityName}Tests
{
    [Fact]
    public void Create_WithValidParameters_ShouldReturn{EntityName}() { }

    [Fact]
    public void Create_WithInvalidParameters_ShouldThrowException() { }
}
```
