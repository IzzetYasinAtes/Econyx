# Econyx - Autonomous Prediction Market Trading Bot

Built with **.NET 10 LTS** and **Onion Architecture**, an AI-powered, multi-platform prediction market trading bot.

## Overview

An OpenClaw Agent-inspired, fully autonomous trading bot operating on Polymarket (and future platforms like Kalshi, Manifold). Uses a hybrid strategy engine powered by AI (Claude + OpenAI) to find mispriced contracts and execute trades automatically.

### Why .NET 10 LTS?

- 3 years of official support (until November 2028)
- EF Core 10: SQL Server JSON column and vector search support
- Blazor: `PersistentState` attribute, Brotli pre-compression (~50KB payload)
- Improved NativeAOT and JIT performance

---

## Onion Architecture - Layer Structure

Dependencies always flow inward. Outer layers depend on inner layers, never the reverse.

```
                    ┌─────────────────────────────────┐
                    │   PRESENTATION (Outermost Ring)   │
                    │   Blazor Dashboard / Worker       │
                    │                                   │
                    │  ┌───────────────────────────┐    │
                    │  │   INFRASTRUCTURE           │    │
                    │  │   EF Core / Polymarket.Net │    │
                    │  │   AI Services / Secrets    │    │
                    │  │                            │    │
                    │  │  ┌─────────────────────┐   │    │
                    │  │  │   APPLICATION        │   │    │
                    │  │  │   CQRS / Strategies  │   │    │
                    │  │  │   Port Interfaces    │   │    │
                    │  │  │                      │   │    │
                    │  │  │  ┌───────────────┐   │   │    │
                    │  │  │  │    DOMAIN      │   │   │    │
                    │  │  │  │  Entities /    │   │   │    │
                    │  │  │  │  Services      │   │   │    │
                    │  │  │  │               │   │   │    │
                    │  │  │  │  ┌─────────┐  │   │   │    │
                    │  │  │  │  │  CORE   │  │   │   │    │
                    │  │  │  │  │ Base    │  │   │   │    │
                    │  │  │  │  │ Abstrs  │  │   │   │    │
                    │  │  │  │  └─────────┘  │   │   │    │
                    │  │  │  └───────────────┘   │   │    │
                    │  │  └─────────────────────┘   │    │
                    │  └───────────────────────────┘    │
                    └─────────────────────────────────┘
```

---

## Solution Structure

```
Econyx.sln
│
├── global.json                          .NET 10 SDK version pinning
├── Directory.Build.props                Shared project settings
├── Directory.Packages.props             Central Package Management
│
├── src/
│   ├── Econyx.Core/                     Base abstractions (zero external deps)
│   ├── Econyx.Domain/                   Entities, Domain Services, Events
│   ├── Econyx.Application/              CQRS, Strategies, Port Interfaces
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

### Dependency Flow

```
Core  <--  Domain  <--  Application  <--  Infrastructure  <--  Worker / Dashboard
```

- **Core** and **Domain**: Zero external NuGet dependencies (only .NET BCL)
- **Application**: Only MediatR + FluentValidation
- **Infrastructure**: All heavy dependencies (EF Core, Polymarket.Net, AI SDKs, Polly)

---

## Layer Details

### Ring 1: Core (Innermost)

- `BaseEntity<TId>` - Base class for all entities with domain event collection
- `AggregateRoot<TId>` - Aggregate roots with domain event dispatch
- `ValueObject` - Structural equality base for value objects
- `IDomainEvent` - Domain event marker interface
- `IRepository<T>` - Generic repository (GetById, Add, Update, Delete)
- `IUnitOfWork` - Transaction management
- `Result<T>` / `Error` - Railway-oriented error handling

### Ring 2: Domain

- **Entities**: Market, Position, Trade, Order, BalanceSnapshot
- **Value Objects**: Money, Probability, Edge, TokenId, MarketOutcome
- **Domain Services**: RiskCalculator (Kelly criterion), EdgeCalculator, PnLCalculator
- **Domain Events**: EdgeDetectedEvent, OrderPlacedEvent, PositionClosedEvent, BalanceCriticalEvent

### Ring 3: Application

- **Port Interfaces**: IPlatformAdapter, IAiAnalysisService, ISecretManager, INotificationService
- **CQRS**: ScanMarketsCommand, PlaceOrderCommand, ClosePositionCommand + Queries
- **Strategy Engine**:
  - `RuleBasedStrategy` - Volume, spread, category filter (cost: 0)
  - `AiAnalysisStrategy` - AI fair value estimation (cost: API call)
  - `HybridStrategy` - Rule filter first, then AI (optimized cost)

### Ring 4: Infrastructure

- **EF Core 10 + SQL Server** - JSON columns, LeftJoin, parameterized queries
- **Polymarket.Net** - CLOB REST + WebSocket client
- **AI**: Claude (Anthropic SDK) + OpenAI, prompt templates, response caching
- **Polly v8** - Retry, circuit breaker, timeout
- **Secret Management** - DPAPI (dev) / Azure Key Vault (prod)

### Ring 5: Presentation

- **Worker**: MarketScanner, TradeExecutor, PositionMonitor, BalanceTracker, HealthMonitor
- **Dashboard**: Blazor Server, SignalR real-time, ApexCharts

---

## Trading Flow

```
Every 5 Minutes (Cycle):
1. MarketScannerService -> Triggers ScanMarketsCommand
2. IPlatformAdapter.GetMarketsAsync() -> ~500+ open markets
3. RuleBasedStrategy.Filter() -> ~20-50 markets (pre-filter)
4. IAiAnalysisService.Analyze() -> Fair value estimates
5. EdgeCalculator -> edge = |fair_value - market_price| - fees
6. RiskCalculator -> Position size via Kelly criterion
7. PlaceOrderCommand -> Paper (simulated) or Live (real CLOB)
8. SignalR -> Dashboard real-time update
```

---

## Configuration

Core settings managed via `appsettings.json`:

- `Trading:Mode` - "Paper" or "Live"
- `Trading:InitialBalance` - Starting balance (default: $50)
- `Trading:MinEdgeThreshold` - Minimum edge threshold (default: 0.06)
- `AI:Provider` - "Claude" or "OpenAI"
- `Platforms:Polymarket:BaseUrl` - Polymarket CLOB API
- `ConnectionStrings:DefaultConnection` - SQL Server connection

---

## Tech Stack

| Technology | Version | Usage |
|------------|---------|-------|
| .NET | 10 LTS | Runtime + SDK |
| C# | 14 | Primary language |
| EF Core | 10 | ORM + SQL Server |
| Polymarket.Net | Latest | CLOB REST + WebSocket |
| MediatR | Latest | CQRS pattern |
| FluentValidation | Latest | Request validation |
| Polly | v8 | Resilience |
| Blazor Server | .NET 10 | Dashboard UI |
| SignalR | .NET 10 | Real-time |
| ApexCharts.Blazor | Latest | Charts |
| Serilog | Latest | Structured logging |
| Anthropic SDK | Latest | Claude AI |
| Microsoft.Extensions.AI | Latest | OpenAI |
| Nethereum | Latest | Polygon wallet |

---

## Getting Started

```bash
# Clone the repo
git clone https://github.com/user/Econyx.git
cd Econyx

# Build
dotnet build

# Run Worker (paper trading)
dotnet run --project src/Econyx.Worker

# Run Dashboard
dotnet run --project src/Econyx.Dashboard
```

## License

MIT
