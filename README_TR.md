# Econyx - Otonom Prediction Market Trading Botu

**.NET 10 LTS** + **Onion Architecture** ile insa edilmis, AI destekli, multi-platform prediction market trading botu.

## Proje Ozeti

OpenClaw Agent benzeri, Polymarket (ve ileride Kalshi, Manifold) uzerinde calisan tam otonom bir trading botu. AI (Claude + OpenAI) destekli hibrit strateji motoru ile yanlis fiyatlanmis kontratlari bulup, otomatik islem acar.

### Neden .NET 10 LTS?

- 3 yil resmi destek (Kasim 2028'e kadar)
- EF Core 10: SQL Server JSON column ve vector search destegi
- Blazor: `PersistentState` attribute, Brotli pre-compression (~50KB payload)
- Gelistirilmis NativeAOT ve JIT performansi

---

## Onion Architecture - Katman Yapisi

Bagimlilik yonu her zaman iceriye dogrudur. Dis katmanlar ic katmanlara bagimlidir, ic katmanlar hicbir zaman dis katmanlara bagimli degildir.

```
                    ┌─────────────────────────────────┐
                    │   PRESENTATION (En Dis Halka)    │
                    │   Blazor Dashboard / Worker      │
                    │                                  │
                    │  ┌───────────────────────────┐   │
                    │  │   INFRASTRUCTURE           │   │
                    │  │   EF Core / Polymarket.Net │   │
                    │  │   AI Services / Secrets    │   │
                    │  │                            │   │
                    │  │  ┌─────────────────────┐   │   │
                    │  │  │   APPLICATION        │   │   │
                    │  │  │   CQRS / Strategies  │   │   │
                    │  │  │   Port Interfaces    │   │   │
                    │  │  │                      │   │   │
                    │  │  │  ┌───────────────┐   │   │   │
                    │  │  │  │    DOMAIN      │   │   │   │
                    │  │  │  │  Entities /    │   │   │   │
                    │  │  │  │  Services      │   │   │   │
                    │  │  │  │               │   │   │   │
                    │  │  │  │  ┌─────────┐  │   │   │   │
                    │  │  │  │  │  CORE   │  │   │   │   │
                    │  │  │  │  │ Base    │  │   │   │   │
                    │  │  │  │  │ Abstrs  │  │   │   │   │
                    │  │  │  │  └─────────┘  │   │   │   │
                    │  │  │  └───────────────┘   │   │   │
                    │  │  └─────────────────────┘   │   │
                    │  └───────────────────────────┘   │
                    └─────────────────────────────────┘
```

---

## Solution Yapisi

```
Econyx.sln
│
├── global.json                          .NET 10 SDK versiyon pinleme
├── Directory.Build.props                Ortak proje ayarlari
├── Directory.Packages.props             Central Package Management
│
├── src/
│   ├── Econyx.Core/                     Base abstractions (sifir dis bagimlilik)
│   ├── Econyx.Domain/                   Entity'ler, Domain Service'ler, Events
│   ├── Econyx.Application/              CQRS, Stratejiler, Port Interface'ler
│   ├── Econyx.Infrastructure/           EF Core, Polymarket, AI, Secrets
│   ├── Econyx.Worker/                   Background Services (Host)
│   └── Econyx.Dashboard/               Blazor Server Dashboard
│
└── tests/
    ├── Econyx.Core.Tests/
    ├── Econyx.Domain.Tests/
    ├── Econyx.Application.Tests/
    ├── Econyx.Infrastructure.Tests/
    └── Econyx.Worker.Tests/
```

### Bagimlilik Akisi

```
Core  <--  Domain  <--  Application  <--  Infrastructure  <--  Worker / Dashboard
```

- **Core** ve **Domain**: Sifir dis NuGet bagimliligi (sadece .NET BCL)
- **Application**: Sadece MediatR + FluentValidation
- **Infrastructure**: Tum agir bagimliliklar (EF Core, Polymarket.Net, AI SDK, Polly)

---

## Katman Detaylari

### Halka 1: Core (En Ic)

- `BaseEntity<TId>` - Tum entity'ler icin base class, domain event collection
- `AggregateRoot<TId>` - Aggregate root'lar icin domain event dispatch
- `ValueObject` - Structural equality ile value object base
- `IDomainEvent` - Domain event marker interface
- `IRepository<T>` - Generic repository (GetById, Add, Update, Delete)
- `IUnitOfWork` - Transaction yonetimi
- `Result<T>` / `Error` - Railway-oriented error handling

### Halka 2: Domain

- **Entity'ler**: Market, Position, Trade, Order, BalanceSnapshot
- **Value Object'ler**: Money, Probability, Edge, TokenId, MarketOutcome
- **Domain Service'ler**: RiskCalculator (Kelly criterion), EdgeCalculator, PnLCalculator
- **Domain Event'ler**: EdgeDetectedEvent, OrderPlacedEvent, PositionClosedEvent, BalanceCriticalEvent

### Halka 3: Application

- **Port Interface'ler**: IPlatformAdapter, IAiAnalysisService, ISecretManager, INotificationService
- **CQRS**: ScanMarketsCommand, PlaceOrderCommand, ClosePositionCommand + Query'ler
- **Strateji Motoru**:
  - `RuleBasedStrategy` - Hacim, spread, kategori filtresi (maliyet: 0)
  - `AiAnalysisStrategy` - AI ile fair value hesaplama (maliyet: API call)
  - `HybridStrategy` - Once rule filter, sonra AI (optimize maliyet)

### Halka 4: Infrastructure

- **EF Core 10 + SQL Server** - JSON columns, LeftJoin, parameterized queries
- **Polymarket.Net** - CLOB REST + WebSocket client
- **AI**: Claude (Anthropic SDK) + OpenAI, prompt template'ler, response cache
- **Polly v8** - Retry, circuit breaker, timeout
- **Secret Management** - DPAPI (dev) / Azure Key Vault (prod)

### Halka 5: Presentation

- **Worker**: MarketScanner, TradeExecutor, PositionMonitor, BalanceTracker, HealthMonitor
- **Dashboard**: Blazor Server, SignalR real-time, ApexCharts

---

## Trading Akisi

```
Her 5 Dakika (Cycle):
1. MarketScannerService -> ScanMarketsCommand tetikler
2. IPlatformAdapter.GetMarketsAsync() -> ~500+ acik market
3. RuleBasedStrategy.Filter() -> ~20-50 market (on-filtre)
4. IAiAnalysisService.Analyze() -> Fair value tahminleri
5. EdgeCalculator -> edge = |fair_value - market_price| - fees
6. RiskCalculator -> Kelly criterion ile pozisyon buyuklugu
7. PlaceOrderCommand -> Paper (sanal) veya Live (gercek CLOB)
8. SignalR -> Dashboard real-time guncelleme
```

---

## Konfigurasyon

Temel ayarlar `appsettings.json` uzerinden yapilir:

- `Trading:Mode` - "Paper" veya "Live"
- `Trading:InitialBalance` - Baslangic bakiyesi (default: $50)
- `Trading:MinEdgeThreshold` - Minimum edge esik degeri (default: 0.06)
- `AI:Provider` - "Claude" veya "OpenAI"
- `Platforms:Polymarket:BaseUrl` - Polymarket CLOB API
- `ConnectionStrings:DefaultConnection` - SQL Server baglanti

---

## Teknoloji Stack

| Teknoloji | Versiyon | Kullanim |
|-----------|----------|----------|
| .NET | 10 LTS | Runtime + SDK |
| C# | 14 | Temel dil |
| EF Core | 10 | ORM + SQL Server |
| Polymarket.Net | Latest | CLOB REST + WebSocket |
| MediatR | Latest | CQRS pattern |
| FluentValidation | Latest | Request validation |
| Polly | v8 | Resilience |
| Blazor Server | .NET 10 | Dashboard UI |
| SignalR | .NET 10 | Real-time |
| ApexCharts.Blazor | Latest | Grafikler |
| Serilog | Latest | Structured logging |
| Anthropic SDK | Latest | Claude AI |
| Microsoft.Extensions.AI | Latest | OpenAI |
| Nethereum | Latest | Polygon wallet |

---

## Baslangic

```bash
# Repoyu klonla
git clone https://github.com/user/Econyx.git
cd Econyx

# Build
dotnet build

# Worker'i calistir (paper trading)
dotnet run --project src/Econyx.Worker

# Dashboard'u calistir
dotnet run --project src/Econyx.Dashboard
```

## Lisans

MIT
