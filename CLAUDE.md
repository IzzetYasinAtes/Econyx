# Econyx - Claude Code Proje Rehberi

## Proje Ozeti

Econyx, Polymarket (ve gelecekte Kalshi, Manifold) uzerinde calisan AI destekli, tam otonom bir prediction market trading botudur. .NET 10 LTS + Onion Architecture ile insa edilmistir. Hibrit strateji motoru (kural tabanlı + AI) ile yanlis fiyatlanmis kontratlari bulur ve otomatik islem acar.

## Teknoloji Yigini

- **Runtime:** .NET 10 LTS, C# 14 (LangVersion preview)
- **ORM:** EF Core 10 + SQL Server
- **CQRS:** MediatR 12.4.1
- **Validasyon:** FluentValidation 11.11.0
- **Resilience:** Polly v8
- **AI:** Anthropic SDK, OpenRouter, OpenAI (Microsoft.Extensions.AI)
- **Platform:** Polymarket.Net (CLOB REST + WebSocket)
- **UI:** Blazor Server + SignalR + ApexCharts
- **Logging:** Serilog
- **Test:** xUnit + Moq + FluentAssertions + InMemory EF Core
- **Secret:** DPAPI (dev) / Azure Key Vault (prod)

## Mimari: Onion Architecture

```
Core  <--  Domain  <--  Application  <--  Infrastructure  <--  Worker / Dashboard
```

### Katman Kurallari
- **Core + Domain:** Sifir dis NuGet bagimliligi (sadece .NET BCL)
- **Application:** Sadece MediatR + FluentValidation
- **Infrastructure:** Tum agir bagimliliklar (EF Core, AI SDK, Polymarket.Net, Polly)
- **Presentation (Worker/Dashboard):** DI kaydi ve host konfigurasyonu

## Solution Yapisi

```
src/
  Econyx.Core/           → BaseEntity, AggregateRoot, ValueObject, Result<T>, IRepository, IUnitOfWork
  Econyx.Domain/         → Entities, ValueObjects, DomainServices, Events, Enums, Repositories
  Econyx.Application/    → Commands, Queries, Strategies, Ports, Behaviors, Configuration
  Econyx.Infrastructure/ → Persistence (EF Core), Adapters, AiServices, Secrets, DI
  Econyx.Worker/         → Background Services (MarketScanner, PositionMonitor, vb.)
  Econyx.Dashboard/      → Blazor Server (Pages, Components, Hubs, Resources)

tests/
  Econyx.Core.Tests/
  Econyx.Domain.Tests/
  Econyx.Application.Tests/
  Econyx.Infrastructure.Tests/
  Econyx.Worker.Tests/
```

## Temel Komutlar

```bash
# Derleme
dotnet build

# Testleri calistir
dotnet test

# Worker baslatma
dotnet run --project src/Econyx.Worker

# Dashboard baslatma
dotnet run --project src/Econyx.Dashboard

# Belirli test projesi
dotnet test tests/Econyx.Domain.Tests

# Migration olusturma (Infrastructure projesinden)
dotnet ef migrations add <MigrationName> --project src/Econyx.Infrastructure --startup-project src/Econyx.Worker
```

## Build Ayarlari

- `TreatWarningsAsErrors: true` — Tum uyarilar hata olarak kabul edilir
- `AnalysisLevel: latest-recommended` — En guncel analyzer kurallari aktif
- `Nullable: enable` — Nullable reference types zorunlu
- Central Package Management (`Directory.Packages.props`) kullanilir

## Trading Akisi

```
Her 1 Dakika:
1. MarketScannerService → ScanMarketsCommand tetikler
2. IPlatformAdapter.GetMarketsAsync() → ~500+ acik piyasa
3. RuleBasedStrategy.Filter() → ~20-50 aday (on-filtre, ucretsiz)
4. AiAnalysisStrategy.Analyze() → AI fair value tahmini (API maliyetli)
5. HybridStrategy → Cift onay (kural + AI ayni yon = islem)
6. RiskCalculator → Kelly Kriteri ile pozisyon boyutu
7. PlaceOrderCommand → Paper (sanal) veya Live (gercek CLOB)
8. PositionMonitorService → SL/TP kontrolu (her 60 sn)
9. SignalR → Dashboard guncelleme
```

> **Not:** Piyasa secimi volatilite tabanlidir — yuksek 24s hacimli ve sonuclanma tarihi 30 gun icinde olan piyasalar onceliklendirilir. Kisa vadeli kripto "Up or Down" piyasalari (BTC, ETH, SOL) onceliklidir; bu piyasalar 1 saat icinde sonuclanir, yuksek hacme ve sik fiyat degisimlerine sahiptir. Uzun vadeli politik piyasalar dusuk onceliklidir.

## Domain Modeli

### Entities
- `Market` (AggregateRoot) — Piyasa sorusu, sonuclar, fiyatlar
- `Position` (AggregateRoot) — Acik/kapali pozisyon, giris/cikis fiyati
- `Order` (BaseEntity) — Emir (Pending→Filled/Cancelled/Rejected)
- `Trade` (BaseEntity) — Kapanmis islem + PnL
- `BalanceSnapshot` (BaseEntity) — Bakiye gecmisi
- `AiModelConfiguration` (BaseEntity) — AI model ayarlari
- `ApiKeyConfiguration` (BaseEntity) — Sifrelenmis API anahtarlari

### Value Objects
- `Money` — Miktar + para birimi, aritmetik operatorler
- `Probability` — [0, 1] araligi, yuzde donusumu
- `Edge` — fairValue - marketPrice farki
- `TokenId` — Polymarket outcome token ID
- `MarketOutcome` — Sonuc adi + fiyat + token

### Domain Services (Static)
- `EdgeCalculator` — Edge hesaplama (fees dahil)
- `RiskCalculator` — Kelly Kriteri ile pozisyon boyutlandirma
- `PnLCalculator` — Gerceklesmis/gerceklesmemis kar/zarar

## Port Interfaces (Application Katmani)

- `IPlatformAdapter` — Platform soyutlama (GetMarkets, PlaceOrder, GetPrice)
- `IAiAnalysisService` — AI analiz servisi
- `IAiProviderFactory` — AI provider factory
- `ISecretManager` — Secret yonetimi
- `IApiKeyEncryptor` — API key sifreleme/maskeleme
- `IOpenRouterClient` — OpenRouter HTTP istemcisi

## Konfigürasyon

Temel ayarlar `appsettings.json` ile yonetilir:
- `Trading:Mode` — "Paper" veya "Live"
- `Trading:MinEdgeThreshold` — Minimum edge esigi (varsayilan: 0.06)
- `Trading:MaxPositionSizePercent` — Kelly cap (varsayilan: %5)
- `AI:Provider` — "OpenRouter", "Anthropic" veya "OpenAI"
- `ConnectionStrings:DefaultConnection` — SQL Server baglantisi

## Lokalizasyon

Dashboard Turkce ve Ingilizce destekler. Resource dosyalari `Dashboard/Resources/` altinda.
