---
name: yeni-query
description: Yeni bir CQRS query ve handler olusturma rehberi
---

# Yeni Query Olusturma

## Adim 1: DTO (src/Econyx.Application/Queries/{Name}/)

```csharp
namespace Econyx.Application.Queries.{Name};

public record {Name}Dto(
    /* response alanlari */
);
```

## Adim 2: Query Record (ayni klasorde)

```csharp
namespace Econyx.Application.Queries.{Name};

using Econyx.Core.Primitives;
using MediatR;

public record {Name}Query(
    /* filtre parametreleri */
) : IRequest<Result<{Name}Dto>>;
```

## Adim 3: Handler (ayni klasorde)

```csharp
namespace Econyx.Application.Queries.{Name};

using Econyx.Core.Primitives;
using MediatR;
using Microsoft.Extensions.Logging;

public sealed class {Name}Handler(
    I{Repository}Repository repository,
    ILogger<{Name}Handler> logger)
    : IRequestHandler<{Name}Query, Result<{Name}Dto>>
{
    public async Task<Result<{Name}Dto>> Handle(
        {Name}Query request,
        CancellationToken ct)
    {
        // 1. Repository'den veri cek
        // 2. DTO'ya map'le
        // 3. Result dondur
        return Result.Success(dto);
    }
}
```

## Notlar
- Query'ler veri DEGISTIRMEZ, sadece okur
- IUnitOfWork kullanilmaz (SaveChanges yok)
- DTO'lar record olarak tanimlanir (immutable)
- Pagination gerekiyorsa: `PageNumber`, `PageSize` parametreleri ekle
