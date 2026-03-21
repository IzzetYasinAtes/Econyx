---
name: yeni-command
description: Yeni bir CQRS command ve handler olusturma rehberi (MediatR pattern)
---

# Yeni Command Olusturma

## Adim 1: Command Record (src/Econyx.Application/Commands/{Name}/)

```csharp
namespace Econyx.Application.Commands.{Name};

using Econyx.Core.Primitives;
using MediatR;

public record {Name}Command(
    /* parametreler */
) : IRequest<Result<{ResponseType}>>;
```

## Adim 2: Validator (ayni klasorde)

```csharp
namespace Econyx.Application.Commands.{Name};

using FluentValidation;

public sealed class {Name}Validator : AbstractValidator<{Name}Command>
{
    public {Name}Validator()
    {
        RuleFor(x => x.Property)
            .NotEmpty()
            .WithMessage("Property bos olamaz.");
    }
}
```

## Adim 3: Handler (ayni klasorde)

```csharp
namespace Econyx.Application.Commands.{Name};

using Econyx.Core.Interfaces;
using Econyx.Core.Primitives;
using MediatR;
using Microsoft.Extensions.Logging;

public sealed class {Name}Handler(
    I{Repository}Repository repository,
    IUnitOfWork unitOfWork,
    ILogger<{Name}Handler> logger)
    : IRequestHandler<{Name}Command, Result<{ResponseType}>>
{
    public async Task<Result<{ResponseType}>> Handle(
        {Name}Command request,
        CancellationToken ct)
    {
        // 1. Veriyi al
        // 2. Domain mantigi calistir
        // 3. Kaydet
        await unitOfWork.SaveChangesAsync(ct);
        // 4. Result dondur
        return Result.Success(response);
    }
}
```

## Adim 4: Test (tests/Econyx.Application.Tests/Commands/)

```csharp
public sealed class {Name}HandlerTests
{
    private readonly Mock<I{Repository}Repository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();

    [Fact]
    public async Task Handle_ValidCommand_ShouldSucceed()
    {
        // Arrange
        var handler = new {Name}Handler(_repoMock.Object, _uowMock.Object, ...);
        var command = new {Name}Command(/* params */);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
```

## Notlar
- Command'lar `IRequest<Result<T>>` implement eder
- ValidationBehavior otomatik olarak validator'u calistirir (MediatR pipeline)
- LoggingBehavior otomatik olarak request/response loglar
- Handler'da `CancellationToken` her zaman iletilir
