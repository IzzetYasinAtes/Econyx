---
name: yeni-strateji
description: Yeni bir trading stratejisi olusturma rehberi (IStrategy pattern)
---

# Yeni Strateji Olusturma

## Adim 1: Strateji Class'i (src/Econyx.Application/Strategies/)

```csharp
namespace Econyx.Application.Strategies;

using Econyx.Application.Configuration;
using Econyx.Domain.Entities;
using Econyx.Domain.Enums;
using Econyx.Domain.Services;
using Econyx.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed class {Name}Strategy(
    IOptions<TradingOptions> tradingOptions,
    ILogger<{Name}Strategy> logger)
    : IStrategy
{
    private readonly TradingOptions _options = tradingOptions.Value;

    public string Name => "{Name}";

    public async Task<IReadOnlyList<StrategySignal>> EvaluateAsync(
        IReadOnlyList<Market> markets,
        CancellationToken ct = default)
    {
        var signals = new List<StrategySignal>();

        foreach (var market in markets)
        {
            ct.ThrowIfCancellationRequested();

            // 1. Filtreleme mantigi
            // 2. Fair value hesaplama
            // 3. Edge hesaplama
            var edge = EdgeCalculator.Calculate(fairValue, marketPrice);

            if (!edge.IsActionable(_options.MinEdgeThreshold))
                continue;

            // 4. Sinyal olustur
            signals.Add(new StrategySignal(
                MarketId: market.Id,
                MarketQuestion: market.Question,
                TokenId: tokenId,
                RecommendedSide: side,
                Edge: edge,
                FairValue: fairValue,
                MarketPrice: marketPrice,
                Confidence: confidence,
                StrategyName: Name,
                Reasoning: reasoning));
        }

        return signals;
    }
}
```

## Adim 2: DI Kaydi (src/Econyx.Application/DependencyInjection.cs)

```csharp
services.AddScoped<IStrategy, {Name}Strategy>();
```

## Adim 3: HybridStrategy Entegrasyonu (gerekiyorsa)

`HybridStrategy` icinde yeni stratejiyi cift onay mekanizmasina ekle.

## Adim 4: Test (tests/Econyx.Application.Tests/Strategies/)

```csharp
public sealed class {Name}StrategyTests
{
    [Fact]
    public async Task EvaluateAsync_WithActionableEdge_ShouldReturnSignal() { }

    [Fact]
    public async Task EvaluateAsync_WithLowEdge_ShouldReturnEmpty() { }

    [Fact]
    public async Task EvaluateAsync_WithClosedMarket_ShouldSkip() { }
}
```

## Notlar
- `EdgeCalculator.Calculate()` ve `edge.IsActionable()` kullanilir
- `TradingOptions` ile esik degerleri konfigurasyondan gelir
- Confidence [0.0, 1.0] araliginda olmali
- `StrategySignal.Reasoning` alaninda sinyal sebebi aciklanir
