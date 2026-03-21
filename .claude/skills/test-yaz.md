---
name: test-yaz
description: Projede test yazma ve calistirma rehberi (xUnit + Moq + FluentAssertions)
---

# Test Yazma Rehberi

## Test Isimlendirme Pattern'i
```
{MetodAdi}_{Senaryo}_{BeklenenSonuc}
```

Ornekler:
- `Create_WithValidParameters_ShouldReturnOrder`
- `Handle_WhenMarketNotFound_ShouldReturnFailure`
- `CalculateEdge_WithHighSpread_ShouldReturnZero`

## Domain Entity Testi

```csharp
public sealed class {Entity}Tests
{
    [Fact]
    public void Create_WithValidParameters_ShouldReturn{Entity}()
    {
        // Act
        var entity = {Entity}.Create(/* params */);

        // Assert
        entity.Should().NotBeNull();
        entity.Property.Should().Be(expectedValue);
    }

    [Fact]
    public void Create_WithNullParam_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => {Entity}.Create(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
```

## Application Handler Testi

```csharp
public sealed class {Name}HandlerTests
{
    private readonly Mock<IRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ILogger<{Name}Handler>> _loggerMock = new();

    private {Name}Handler CreateHandler() =>
        new(_repoMock.Object, _uowMock.Object, _loggerMock.Object);

    [Fact]
    public async Task Handle_ValidCommand_ShouldReturnSuccess()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

## Infrastructure Repository Testi

```csharp
public sealed class {Entity}RepositoryTests
{
    private static EconyxDbContext CreateContext() =>
        TestDbContextFactory.Create();

    [Fact]
    public async Task Add_ShouldPersistEntity()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new {Entity}Repository(context);
        var entity = {Entity}.Create(/* params */);

        // Act
        await repository.AddAsync(entity);
        await context.SaveChangesAsync();

        // Assert
        var saved = await repository.GetByIdAsync(entity.Id);
        saved.Should().NotBeNull();
    }
}
```

## Test Calistirma

```bash
# Tum testler
dotnet test

# Belirli proje
dotnet test tests/Econyx.Domain.Tests

# Belirli test class'i
dotnet test --filter "FullyQualifiedName~OrderTests"

# Verbose cikti
dotnet test --verbosity detailed
```

## Notlar
- Her test tek bir davranis test eder
- Arrange-Act-Assert (AAA) pattern'i kullanilir
- FluentAssertions ile assert: `.Should().Be()`, `.Should().BeTrue()`, `.Should().Throw<>()`
- Mock'larda `It.IsAny<>()` ile esnek eslestirme
- `CancellationToken.None` test'lerde kullanilir
