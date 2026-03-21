---
name: yeni-background-service
description: Yeni bir Worker background service olusturma rehberi
---

# Yeni Background Service Olusturma

## Adim 1: Service Class (src/Econyx.Worker/Services/)

```csharp
namespace Econyx.Worker.Services;

using Econyx.Application.Configuration;
using MediatR;
using Microsoft.Extensions.Options;

public sealed class {Name}Service(
    IServiceScopeFactory scopeFactory,
    IOptions<TradingOptions> tradingOptions,
    ILogger<{Name}Service> logger)
    : BackgroundService
{
    private readonly TradingOptions _options = tradingOptions.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("{Service} baslatildi", nameof({Name}Service));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                // Is mantigi burada
                // var result = await mediator.Send(new SomeCommand(), stoppingToken);

                logger.LogDebug("{Service} dongusu tamamlandi", nameof({Name}Service));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{Service} hata olustu", nameof({Name}Service));
            }

            await Task.Delay(TimeSpan.FromSeconds(_intervalSeconds), stoppingToken);
        }

        logger.LogInformation("{Service} durduruldu", nameof({Name}Service));
    }
}
```

## Adim 2: DI Kaydi (src/Econyx.Worker/Program.cs)

```csharp
builder.Services.AddHostedService<{Name}Service>();
```

## Adim 3: Test (tests/Econyx.Worker.Tests/Services/)

```csharp
public sealed class {Name}ServiceTests
{
    private readonly Mock<IMediator> _mediatorMock = new();

    [Fact]
    public async Task ExecuteAsync_ShouldSendCommand()
    {
        // Service'in is mantigini test et
    }
}
```

## Notlar
- `IServiceScopeFactory` ile scoped servisler alinir (Background service singleton'dir)
- try-catch ile hata loglanir, servis DURMAZ
- `OperationCanceledException` yakalanir ve graceful shutdown yapilir
- Interval degerleri `TradingOptions`'dan gelir
- Serilog structured logging kullanilir
