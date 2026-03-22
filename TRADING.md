# Econyx Trading Mechanism — Detailed Technical Documentation

## Table of Contents

1. [High-Level Architecture](#high-level-architecture)
2. [The Trading Cycle](#the-trading-cycle)
3. [Strategy Engine](#strategy-engine)
4. [Edge Calculation](#edge-calculation)
5. [Position Sizing — Kelly Criterion](#position-sizing--kelly-criterion)
6. [Order Execution](#order-execution)
7. [Position Monitoring — Stop-Loss & Take-Profit](#position-monitoring--stop-loss--take-profit)
8. [Trade Closing & PnL Calculation](#trade-closing--pnl-calculation)
9. [AI Prompt Engineering](#ai-prompt-engineering)
10. [Background Services](#background-services)
11. [Configuration Reference](#configuration-reference)
12. [Complete Trading Flow Diagram](#complete-trading-flow-diagram)
13. [Example Scenarios](#example-scenarios)

---

## High-Level Architecture

Econyx runs as a set of background services that continuously scan prediction markets, identify mispriced contracts using a hybrid AI + rule-based strategy, and execute trades automatically.

```
[Polymarket API] <---> [MarketScannerService]
                            |
                    [HybridStrategy]
                     /              \
        [RuleBasedStrategy]    [AiAnalysisStrategy]
                     \              /
                    [StrategySignal]
                            |
                    [PlaceOrderHandler]
                     /              \
                [Order]          [Position]
                   |                 |
          [TradeExecutorService]  [PositionMonitorService]
                                     |
                            [ClosePositionHandler]
                                     |
                                 [Trade]
```

---

## The Trading Cycle

Every `ScanIntervalMinutes` (default: 1 minute), the following cycle executes:

### Step 1: Fetch Markets
`MarketScannerService` triggers `ScanMarketsCommand`, which calls `IPlatformAdapter.GetMarketsAsync()` to fetch all open markets from Polymarket (~500+ markets at any time).

### Step 2: Generate Signals
The `HybridStrategy` evaluates all markets through a two-stage filter:

1. **RuleBasedStrategy** — Free, instant pre-filter that eliminates most markets
2. **AiAnalysisStrategy** — Expensive AI call only for markets that pass the rule filter
3. **Dual confirmation** — Only signals where both strategies agree on direction are kept

### Step 3: Calculate Position Size
For each confirmed signal, `RiskCalculator.CalculatePositionSize()` uses the Kelly Criterion to determine the optimal bet size.

### Step 4: Place Orders
`PlaceOrderCommand` creates an `Order` and `Position` entity. In Paper mode, orders fill instantly. In Live mode, orders are sent to Polymarket's CLOB API.

### Step 5: Monitor Positions
`PositionMonitorService` checks open positions every 60 seconds, applying stop-loss and take-profit rules.

### Step 6: Close & Record
When a position hits SL/TP or the market resolves, `ClosePositionHandler` closes the position and creates a `Trade` record with PnL.

---

## Strategy Engine

### RuleBasedStrategy (Cost: $0)

The rule-based strategy acts as a fast, free pre-filter. It looks for extreme price anomalies that suggest a market is mispriced.

**Entry Conditions:**

| Condition | Signal | Edge Formula |
|-----------|--------|-------------|
| Price < $0.15 | Buy Yes | `edge = 0.15 - price` |
| Price > $0.85 | Buy No | `edge = price - 0.85` |

**Filters applied before evaluation:**
- `MarketStatus == Open`
- `VolumeUsd >= MinVolumeUsd` (default: $50,000)
- `Spread <= MaxSpreadCents / 100` (default: 5 cents)
- `edge >= MinEdgeThreshold` (default: 0.06)

**Confidence:** Fixed at 0.6 for all rule-based signals.

**Rationale:** Markets priced below 15 cents or above 85 cents are in "extreme" territory. If the true probability differs from the market price by more than the edge threshold, there's a potential value opportunity.

### AiAnalysisStrategy (Cost: API Call)

The AI strategy sends each candidate market to a large language model (Claude, GPT-4o, Gemini, etc.) and asks it to estimate the **fair probability** of each outcome.

**Process:**
1. Build a structured prompt with market question, description, category, volume, and current prices
2. Send to AI model via `IAiAnalysisService.AnalyzeMarketAsync()`
3. Parse the JSON response to extract `fairValue`, `confidence`, and `reasoning` for each outcome
4. Calculate edge: `edgeValue = fairValue - marketPrice`
5. If `|edgeValue| >= MinEdgeThreshold`, generate a signal

**Direction determination:**
- `edgeValue > 0` (AI thinks it's underpriced) → Buy **Yes**
- `edgeValue < 0` (AI thinks it's overpriced) → Buy **No**

**Confidence:** Comes directly from the AI model's self-assessment (0.0 to 1.0).

### HybridStrategy (Default)

The hybrid strategy is the production default. It combines both approaches for maximum reliability while minimizing AI costs.

**Algorithm:**
1. Run `RuleBasedStrategy` on all markets → get rule signals
2. If no rule signals, stop (no AI calls needed, saving money)
3. Filter markets to only those with rule signals
4. Run `AiAnalysisStrategy` on filtered markets only
5. For each AI signal, check if a matching rule signal exists with the **same MarketId AND same Side**
6. If both agree: combine into a Hybrid signal with `confidence = (ruleConfidence + aiConfidence) / 2`
7. If they disagree: discard the signal (no trade)

**Why this works:** The rule filter catches obvious mispricings for free. The AI confirms (or denies) the opportunity with deeper analysis. Only when both systems agree does the bot trade, dramatically reducing false signals.

---

## Edge Calculation

Edge represents the bot's perceived advantage over the market.

### Formula

```
edge = |fairValue - marketPrice| - estimatedFees
```

Where:
- `fairValue` = AI's estimated true probability (or 0.15/0.85 for rule-based)
- `marketPrice` = Current market price on Polymarket
- `estimatedFees` = Platform fees (default: 2%)

### The `Edge` Value Object

```csharp
Edge.Create(rawEdge)        // Creates an Edge value object
edge.AbsoluteValue          // The raw edge value
edge.IsActionable(threshold) // Returns true if AbsoluteValue >= threshold
```

### Example

- Market price: $0.30 (market thinks 30% chance)
- AI fair value: $0.55 (AI thinks 55% chance)
- Edge = |0.55 - 0.30| - 0.02 = 0.23 (23%)
- MinEdgeThreshold = 0.06 → Edge is actionable

---

## Position Sizing — Kelly Criterion

The Kelly Criterion is a mathematical formula that determines the optimal fraction of your bankroll to bet, maximizing long-term growth while limiting risk of ruin.

### Formula

```
odds = (1 / fairValue) - 1
kellyFraction = edge / odds
cappedFraction = min(kellyFraction, maxPositionSizePercent)
positionSize = balance * max(cappedFraction, 0)
```

### Step-by-Step Breakdown

1. **Calculate odds:** Convert probability to decimal odds. If fairValue = 0.55, then odds = (1/0.55) - 1 = 0.818
2. **Kelly fraction:** Divide edge by odds. If edge = 0.23 and odds = 0.818, then kelly = 0.23 / 0.818 = 0.281 (28.1%)
3. **Cap it:** Apply `MaxPositionSizePercent` (default 2%) as upper limit. So capped = min(0.281, 0.02) = 0.02
4. **Position size:** Multiply by balance. If balance = $1000, then position = $1000 * 0.05 = $50

### Why Kelly?

- **High edge + low odds** = bet more (you have a bigger advantage)
- **Low edge + high odds** = bet less (smaller advantage, bigger risk)
- **Capping at 5%** prevents over-betting even when Kelly suggests a large fraction, adding a safety margin

### Quantity Calculation

After position size is determined:

```
quantity = positionSize / marketPrice
```

For example: $50 position at $0.30 market price = 166.67 shares.

---

## Order Execution

### PlaceOrderHandler

When a signal passes all filters:

1. Create an `Order` entity with:
   - `TokenId` from the signal (specific Polymarket outcome token)
   - `Price` = market price (the limit price, e.g., 0.30)
   - `Quantity` = positionSize / marketPrice
   - `Mode` = Paper or Live

2. **Paper mode:** Order fills instantly at market price. No real money moves.

3. **Live mode:**
   - Send order to Polymarket CLOB API via `IPlatformAdapter.PlaceOrderAsync()`
   - Save the platform order ID for tracking
   - Fill the order with the execution price

4. Create a `Position` entity tracking:
   - `MarketId`, `MarketQuestion`, `TokenId`
   - `EntryPrice` (the fill price)
   - `Quantity`, `Side` (Yes/No)
   - `StrategyName` (which strategy produced this signal)

### TradeExecutorService (Live Mode Only)

For Live mode, orders may not fill immediately. The `TradeExecutorService` runs every 30 seconds to:

1. Fetch all pending orders from the database
2. For each Live order with a platform order ID:
   - Query current price via `platform.GetPriceAsync(order.TokenId)`
   - Fill the order at current price
3. Save all changes

---

## Position Monitoring — Stop-Loss & Take-Profit

`PositionMonitorService` runs every 60 seconds and evaluates all open positions.

### Evaluation Logic

For each open position:

1. **Get current price:** `platform.GetPriceAsync(position.TokenId)` — uses the token ID stored on the position to get the exact outcome's current price
2. **Update position:** `position.UpdatePrice(currentPrice)`
3. **Calculate PnL percentage:**

```
pnl = position.CalculatePnL()
entryAmount = entryPrice * quantity
pnlPercent = (pnl / entryAmount) * 100
```

4. **Check thresholds:**

| Condition | Action | Default |
|-----------|--------|---------|
| `pnlPercent <= -StopLossPercent` | Close position (stop loss) | -10% |
| `pnlPercent >= TakeProfitPercent` | Close position (take profit) | +15% |

### PnL Calculation

PnL depends on the trade direction:

- **Yes side:** `PnL = (currentPrice - entryPrice) * quantity`
  - You profit when the price goes UP (event becomes more likely)
- **No side:** `PnL = (entryPrice - currentPrice) * quantity`
  - You profit when the price goes DOWN (event becomes less likely)

### Example

- Entered Yes at $0.30, quantity = 166.67
- Current price = $0.55
- PnL = (0.55 - 0.30) * 166.67 = $41.67
- Entry amount = 0.30 * 166.67 = $50.00
- PnL% = (41.67 / 50.00) * 100 = +83.3%
- TakeProfitPercent = 15% → **Take profit triggered**, position closes

---

## Trade Closing & PnL Calculation

When a position is closed (via SL/TP or market resolution):

1. `ClosePositionHandler` receives the `ClosePositionCommand` with `PositionId` and `ExitPrice`
2. `position.Close(exitPrice)` — marks position as closed, records exit price and timestamp
3. `position.CalculatePnL()` — calculates final PnL
4. A `Trade` record is created with:
   - Entry price, exit price, quantity
   - Realized PnL and fees
   - Duration (how long the position was open)
   - Strategy name that produced the signal
5. The `PositionClosedEvent` domain event fires for any subscribers

### Realized PnL Formula

```
grossPnl = (exitPrice - entryPrice) * quantity  [for Yes]
grossPnl = (entryPrice - exitPrice) * quantity  [for No]
realizedPnl = grossPnl - fees
```

---

## AI Prompt Engineering

The AI prompt is the core of the system's intelligence. Here's the exact prompt template:

### Prompt Structure

```
You are an expert prediction market analyst. Your task is to estimate
fair probabilities for market outcomes.

## Market Details
**Question:** [e.g., "Will Bitcoin reach $100K by June 2026?"]
**Description:** [Full market description]
**Category:** [e.g., "Crypto"]
**24h Volume (USD):** $[e.g., 1,250,000]

## Current Outcomes & Market Prices
  - "Yes": current market price = 0.3000 (30.0%)
  - "No": current market price = 0.7000 (70.0%)

## Instructions
1. Analyze the question using your knowledge and reasoning.
2. Estimate the fair probability (0.0 to 1.0) for EACH outcome.
3. Probabilities should sum to approximately 1.0 for binary markets.
4. Provide a confidence score (0.0 to 1.0).
5. Explain your reasoning in 2-4 sentences.

## Response Format
{
  "outcomes": [
    { "name": "Yes", "fairValue": 0.55 },
    { "name": "No", "fairValue": 0.45 }
  ],
  "confidence": 0.72,
  "reasoning": "Based on current market trends..."
}
```

### Response Parsing

The `FairValueResponseParser` extracts:
- `fairValue` for each outcome (clamped to 0-1 range)
- `confidence` score
- `reasoning` text

### AI Response Caching

To reduce API costs, responses are cached for `CacheDurationMinutes` (default: 30 minutes). The same market won't be re-analyzed within this window.

---

## Background Services

| Service | Interval | Purpose |
|---------|----------|---------|
| `MarketScannerService` | 1 min | Scan markets, generate signals, place orders |
| `TradeExecutorService` | 30 sec | Check pending Live orders for fill status |
| `PositionMonitorService` | 60 sec | Monitor open positions, trigger SL/TP |
| `BalanceTrackerService` | periodic | Track balance snapshots over time |
| `HealthMonitorService` | periodic | Platform connectivity and database health checks |

---

## Configuration Reference

| Parameter | Default | Description |
|-----------|---------|-------------|
| `Mode` | Paper | Paper (simulated) or Live (real money) |
| `InitialBalance` | $50 | Starting paper balance |
| `ScanIntervalMinutes` | 1 | How often to scan markets |
| `MaxOpenPositions` | 20 | Maximum simultaneous positions |
| `MaxPositionSizePercent` | 2% | Kelly cap — max % of balance per trade |
| `MinEdgeThreshold` | 0.06 | Minimum edge required (6%) |
| `MinVolumeUsd` | $50,000 | Minimum 24h volume filter |
| `MaxSpreadCents` | 5 | Maximum bid-ask spread (cents) |
| `StopLossPercent` | 10% | Close if loss exceeds this |
| `TakeProfitPercent` | 15% | Close if profit exceeds this |
| `MaxHoldMinutes` | 15 | Maximum time to hold a position (minutes) |
| `SurvivalModeThresholdUsd` | $10 | Reduce activity below this balance |

---

## Complete Trading Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    MARKET SCANNING (Every 1 min)                │
│                                                                 │
│  Polymarket API → 500+ markets                                  │
│       │                                                         │
│       ▼                                                         │
│  ┌─────────────────────────────────────────┐                    │
│  │         RULE-BASED PRE-FILTER           │                    │
│  │  • Volume >= $50,000                    │                    │
│  │  • Spread <= 5¢                         │                    │
│  │  • Price < 15¢ OR Price > 85¢           │                    │
│  │  • Edge >= 6%                           │                    │
│  └─────────────────────────────────────────┘                    │
│       │ ~20-50 candidates                                       │
│       ▼                                                         │
│  ┌─────────────────────────────────────────┐                    │
│  │         AI FAIR VALUE ANALYSIS          │                    │
│  │  • Send market to Claude/GPT/Gemini     │                    │
│  │  • Get fair probability estimate        │                    │
│  │  • Calculate AI edge                    │                    │
│  └─────────────────────────────────────────┘                    │
│       │                                                         │
│       ▼                                                         │
│  ┌─────────────────────────────────────────┐                    │
│  │         HYBRID DUAL CONFIRMATION        │                    │
│  │  Rule signal + AI signal = SAME side?   │                    │
│  │  YES → Trade    NO → Skip               │                    │
│  └─────────────────────────────────────────┘                    │
│       │ ~1-5 confirmed signals                                  │
│       ▼                                                         │
│  ┌─────────────────────────────────────────┐                    │
│  │         KELLY CRITERION SIZING          │                    │
│  │  odds = (1/fairValue) - 1              │                     │
│  │  kelly = edge / odds                   │                     │
│  │  size = balance × min(kelly, 5%)       │                     │
│  └─────────────────────────────────────────┘                    │
│       │                                                         │
│       ▼                                                         │
│  ORDER + POSITION CREATED                                       │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                 POSITION MONITORING (Every 60 sec)              │
│                                                                 │
│  For each open position:                                        │
│       │                                                         │
│       ▼                                                         │
│  Get current price via position.TokenId                         │
│       │                                                         │
│       ▼                                                         │
│  Calculate PnL%                                                 │
│       │                                                         │
│       ├── PnL% <= -10% → STOP LOSS → Close position            │
│       ├── PnL% >= +15% → TAKE PROFIT → Close position          │
│       └── Otherwise → Continue monitoring                       │
│                                                                 │
│  On close: Trade record created with realized PnL               │
└─────────────────────────────────────────────────────────────────┘
```

---

## Example Scenarios

### Scenario 1: Successful Yes Trade

1. **Market:** "Will the Fed cut rates in June 2026?"
2. **Current price:** $0.28 (market thinks 28% chance)
3. **Rule filter:** Price < 0.15? No. Price > 0.85? No. → No rule signal for this specific market
4. **Result:** This market would NOT be traded by the rule filter alone

### Scenario 2: Successful Hybrid Trade

1. **Market:** "Will ETH reach $10K by July 2026?"
2. **Current price:** $0.08 (market thinks 8% chance)
3. **Rule filter:** Price < 0.15 → Yes! Edge = 0.15 - 0.08 = 0.07 (7%), above threshold
4. **AI analysis:** Fair value = 0.22, confidence = 0.68
5. **AI edge:** |0.22 - 0.08| = 0.14 (14%), above threshold, direction = Yes
6. **Hybrid check:** Both say Yes → Signal confirmed!
7. **Kelly sizing:**
   - odds = (1/0.22) - 1 = 3.545
   - kelly = 0.14 / 3.545 = 0.0395 (3.95%)
   - cap = min(3.95%, 5%) = 3.95%
   - balance = $500 → position size = $19.75
8. **Quantity:** $19.75 / $0.08 = 246.88 shares
9. **Position opened:** Yes at $0.08, 246.88 shares
10. **After 3 days:** Price rises to $0.18
11. **PnL:** (0.18 - 0.08) * 246.88 = $24.69
12. **PnL%:** ($24.69 / $19.75) * 100 = +125%
13. **Take-profit (25%) was triggered** when price hit ~$0.10

### Scenario 3: Stop-Loss Triggered

1. **Market:** "Will SpaceX launch Starship in April 2026?"
2. **Entered Yes at $0.12**, 416.67 shares ($50 position)
3. **Price drops to $0.09** due to launch delay announcement
4. **PnL:** (0.09 - 0.12) * 416.67 = -$12.50
5. **PnL%:** (-$12.50 / $50) * 100 = -25%
6. **Stop-loss at -10% was triggered** when price hit ~$0.108

---
---

# Econyx Alim-Satim Mekanizmasi — Detayli Teknik Dokumantasyon

## Icerik

1. [Ust Duzey Mimari](#ust-duzey-mimari)
2. [Islem Dongusu](#islem-dongusu)
3. [Strateji Motoru](#strateji-motoru)
4. [Edge (Avantaj) Hesaplama](#edge-avantaj-hesaplama)
5. [Pozisyon Boyutlandirma — Kelly Kriteri](#pozisyon-boyutlandirma--kelly-kriteri)
6. [Emir Yurutme](#emir-yurutme)
7. [Pozisyon Izleme — Zarar Durdur ve Kar Al](#pozisyon-izleme--zarar-durdur-ve-kar-al)
8. [Islem Kapatma ve Kar/Zarar Hesaplama](#islem-kapatma-ve-karzarar-hesaplama)
9. [AI Prompt Muhendisligi](#ai-prompt-muhendisligi)
10. [Arka Plan Servisleri](#arka-plan-servisleri)
11. [Yapilandirma Referansi](#yapilandirma-referansi)
12. [Tam Islem Akis Diyagrami](#tam-islem-akis-diyagrami)
13. [Ornek Senaryolar](#ornek-senaryolar)

---

## Ust Duzey Mimari

Econyx, tahmin piyasalarini surekli tarayan, hibrit AI + kural tabanli strateji kullanarak yanlis fiyatlanmis kontratlari bulan ve otomatik islem yapan bir dizi arka plan servisinden olusur.

```
[Polymarket API] <---> [MarketScannerService]
                            |
                    [HybridStrategy]
                     /              \
        [RuleBasedStrategy]    [AiAnalysisStrategy]
                     \              /
                    [StrategySignal]
                            |
                    [PlaceOrderHandler]
                     /              \
                [Order]          [Position]
                   |                 |
          [TradeExecutorService]  [PositionMonitorService]
                                     |
                            [ClosePositionHandler]
                                     |
                                 [Trade]
```

---

## Islem Dongusu

Her `ScanIntervalMinutes` (varsayilan: 1 dakika) suresinde asagidaki dongu calisir:

### Adim 1: Piyasalari Cek
`MarketScannerService`, `ScanMarketsCommand`'i tetikler. Bu komut `IPlatformAdapter.GetMarketsAsync()` ile Polymarket'ten tum acik piyasalari ceker (~500+ piyasa).

### Adim 2: Sinyal Uret
`HybridStrategy` tum piyasalari iki asamali filtreden gecirir:

1. **RuleBasedStrategy** — Ucretsiz, aninda on-filtre, cogu piyasayi eler
2. **AiAnalysisStrategy** — Sadece kural filtresini gecen piyasalar icin pahali AI cagrisi
3. **Cift onay** — Sadece her iki stratejinin de ayni yonde hemfikir oldugu sinyaller kalir

### Adim 3: Pozisyon Boyutu Hesapla
Her onaylanmis sinyal icin `RiskCalculator.CalculatePositionSize()`, Kelly Kriteri kullanarak optimal bahis boyutunu belirler.

### Adim 4: Emir Ver
`PlaceOrderCommand`, `Order` ve `Position` entity'leri olusturur. Paper modda emirler aninda dolar. Live modda emirler Polymarket'in CLOB API'sine gonderilir.

### Adim 5: Pozisyonlari Izle
`PositionMonitorService`, acik pozisyonlari her 60 saniyede kontrol eder ve zarar-durdur / kar-al kurallarini uygular.

### Adim 6: Kapat ve Kaydet
Bir pozisyon SL/TP'ye ulastiginda veya piyasa sonuclandiginda, `ClosePositionHandler` pozisyonu kapatir ve kar/zarar ile birlikte bir `Trade` kaydi olusturur.

---

## Strateji Motoru

### RuleBasedStrategy (Maliyet: $0)

Kural tabanli strateji, hizli ve ucretsiz bir on-filtre gorevi gorur. Bir piyasanin yanlis fiyatlandigini gosteren asiri fiyat anomalilerini arar.

**Giris Kosullari:**

| Kosul | Sinyal | Edge Formulu |
|-------|--------|-------------|
| Fiyat < $0.15 | Yes Al | `edge = 0.15 - fiyat` |
| Fiyat > $0.85 | No Al | `edge = fiyat - 0.85` |

**Degerlendirme oncesi uygulanan filtreler:**
- `MarketStatus == Open`
- `VolumeUsd >= MinVolumeUsd` (varsayilan: $50,000)
- `Spread <= MaxSpreadCents / 100` (varsayilan: 5 sent)
- `edge >= MinEdgeThreshold` (varsayilan: 0.06)

**Guven (Confidence):** Tum kural tabanli sinyaller icin sabit 0.6.

**Mantik:** 15 sentin altinda veya 85 sentin uzerinde fiyatlanan piyasalar "asiri" bolgedededir. Gercek olasilik piyasa fiyatindan edge esik degeri kadar farklilik gosteriyorsa, potansiyel bir deger firsati vardir.

### AiAnalysisStrategy (Maliyet: API Cagrisi)

AI stratejisi, her aday piyasayi buyuk bir dil modeline (Claude, GPT-4o, Gemini vb.) gonderir ve her sonucun **gercek olasiliigini** tahmin etmesini ister.

**Surec:**
1. Piyasa sorusu, aciklama, kategori, hacim ve guncel fiyatlarla yapilandirilmis bir prompt olustur
2. AI modeline `IAiAnalysisService.AnalyzeMarketAsync()` ile gonder
3. JSON yanitini ayristirarak her sonuc icin `fairValue`, `confidence` ve `reasoning` cikart
4. Edge hesapla: `edgeValue = fairValue - marketPrice`
5. `|edgeValue| >= MinEdgeThreshold` ise sinyal uret

**Yon belirleme:**
- `edgeValue > 0` (AI dusuk fiyatlanmis dusuyor) → **Yes** al
- `edgeValue < 0` (AI asiri fiyatlanmis dusuyor) → **No** al

**Guven (Confidence):** AI modelinin kendi oz degerlemesinden gelir (0.0 - 1.0).

### HybridStrategy (Varsayilan)

Hibrit strateji, uretim ortamindaki varsayilan stratejidir. Maksimum guvenilirlik saglarken AI maliyetlerini minimize etmek icin her iki yaklasimi birlestirir.

**Algoritma:**
1. `RuleBasedStrategy`'i tum piyasalarda calistir → kural sinyalleri al
2. Kural sinyali yoksa dur (AI cagrisi gerekmez, para tasarrufu)
3. Piyasalari sadece kural sinyali olanlarla filtrele
4. `AiAnalysisStrategy`'i sadece filtrelenmis piyasalarda calistir
5. Her AI sinyali icin, **ayni MarketId VE ayni Side** ile eslesebilen kural sinyali kontrol et
6. Her ikisi de hemfikirise: Hibrit sinyale birletir, `confidence = (kuralConfidence + aiConfidence) / 2`
7. Farkli yondeyse: Sinyali at (islem yok)

**Neden calisiyor:** Kural filtresi belirgin yanlis fiyatlamalari ucretsiz yakalar. AI, daha derin analizle firsati onaylar (veya reddeder). Sadece her iki sistem de hemfikir oldugunda bot islem yapar, yanlis sinyalleri onemli olcude azaltir.

---

## Edge (Avantaj) Hesaplama

Edge, botun piyasaya karsi algidigi avantaji temsil eder.

### Formul

```
edge = |fairValue - marketPrice| - estimatedFees
```

Burada:
- `fairValue` = AI'nin tahmini gercek olasilik (veya kural tabanli icin 0.15/0.85)
- `marketPrice` = Polymarket'teki guncel piyasa fiyati
- `estimatedFees` = Platform ucretleri (varsayilan: %2)

### Ornek

- Piyasa fiyati: $0.30 (piyasa %30 sans dusuyor)
- AI fair value: $0.55 (AI %55 sans dusuyor)
- Edge = |0.55 - 0.30| - 0.02 = 0.23 (%23)
- MinEdgeThreshold = 0.06 → Edge aksiyona gecirilebilir

---

## Pozisyon Boyutlandirma — Kelly Kriteri

Kelly Kriteri, uzun vadeli buyumeyi maksimize ederken iflas riskini sinirlandirarak, bankanizin hangi kesrini bahse yatirmaniz gerektigini belirleyen matematiksel bir formuldur.

### Formul

```
odds = (1 / fairValue) - 1
kellyFraction = edge / odds
cappedFraction = min(kellyFraction, maxPositionSizePercent)
positionSize = balance * max(cappedFraction, 0)
```

### Adim Adim Aciklama

1. **Odds hesapla:** Olasiligi ondalik odds'a cevir. fairValue = 0.55 ise, odds = (1/0.55) - 1 = 0.818
2. **Kelly kesri:** Edge'i odds'a bol. edge = 0.23 ve odds = 0.818 ise, kelly = 0.23 / 0.818 = 0.281 (%28.1)
3. **Sinirla:** `MaxPositionSizePercent` (varsayilan %5) ust sinir olarak uygula. sinirli = min(0.281, 0.05) = 0.05
4. **Pozisyon boyutu:** Bakiye ile carp. Bakiye = $1000 ise, pozisyon = $1000 * 0.05 = $50

### Neden Kelly?

- **Yuksek edge + dusuk odds** = daha fazla yatir (daha buyuk avantajin var)
- **Dusuk edge + yuksek odds** = daha az yatir (daha kucuk avantaj, daha buyuk risk)
- **%5 siniri** Kelly buyuk bir kesir onerdiginde bile asiri bahis yapmayi onler, guvenlik marji ekler

### Miktar Hesaplama

Pozisyon boyutu belirlendikten sonra:

```
miktar = pozisyonBoyutu / piyasaFiyati
```

Ornegin: $0.30 piyasa fiyatinda $50 pozisyon = 166.67 hisse.

---

## Emir Yurutme

### PlaceOrderHandler

Bir sinyal tum filtreleri gectiginde:

1. Su bilgilerle `Order` entity'si olustur:
   - Sinyalden gelen `TokenId` (belirli Polymarket sonuc tokeni)
   - `Price` = piyasa fiyati (limit fiyat, ornegin 0.30)
   - `Quantity` = pozisyonBoyutu / piyasaFiyati
   - `Mode` = Paper veya Live

2. **Paper mod:** Emir aninda piyasa fiyatindan dolar. Gercek para hareket etmez.

3. **Live mod:**
   - Emir `IPlatformAdapter.PlaceOrderAsync()` ile Polymarket CLOB API'sine gonderilir
   - Takip icin platform emir kimlik numarasi kaydedilir
   - Emir yurutme fiyati ile doldurulur

4. Izleyen bilgilerle `Position` entity'si olustur:
   - `MarketId`, `MarketQuestion`, `TokenId`
   - `EntryPrice` (dolum fiyati)
   - `Quantity`, `Side` (Yes/No)
   - `StrategyName` (sinyali ureten strateji)

### TradeExecutorService (Sadece Live Mod)

Live modda emirler aninda dolmayabilir. `TradeExecutorService` her 30 saniyede calisir:

1. Veritabanindan bekleyen tum emirleri cek
2. Platform emir kimligi olan her Live emir icin:
   - `platform.GetPriceAsync(order.TokenId)` ile guncel fiyati sorgula
   - Emri guncel fiyattan doldur
3. Tum degisiklikleri kaydet

---

## Pozisyon Izleme — Zarar Durdur ve Kar Al

`PositionMonitorService` her 60 saniyede calisir ve tum acik pozisyonlari degerlendirir.

### Degerlendirme Mantigi

Her acik pozisyon icin:

1. **Guncel fiyat al:** `platform.GetPriceAsync(position.TokenId)` — pozisyonda saklanan token kimligi ile kesin sonucun guncel fiyatini alir
2. **Pozisyonu guncelle:** `position.UpdatePrice(currentPrice)`
3. **Kar/Zarar yuzdesi hesapla:**

```
pnl = position.CalculatePnL()
girisUcreti = girisFiyati * miktar
pnlYuzde = (pnl / girisUcreti) * 100
```

4. **Esik degerlerini kontrol et:**

| Kosul | Aksiyon | Varsayilan |
|-------|---------|-----------|
| `pnlYuzde <= -StopLossPercent` | Pozisyonu kapat (zarar durdur) | -%10 |
| `pnlYuzde >= TakeProfitPercent` | Pozisyonu kapat (kar al) | +%15 |

### Kar/Zarar Hesaplama

Kar/zarar islem yonune baglidir:

- **Yes tarafi:** `PnL = (guncelFiyat - girisFiyati) * miktar`
  - Fiyat YUKSELDIGINDE kar edersiniz (olay daha olasi hale gelir)
- **No tarafi:** `PnL = (girisFiyat - guncelFiyat) * miktar`
  - Fiyat DUSTUGUNDE kar edersiniz (olay daha az olasi hale gelir)

### Ornek

- $0.30 fiyattan Yes giris yapildi, miktar = 166.67
- Guncel fiyat = $0.55
- PnL = (0.55 - 0.30) * 166.67 = $41.67
- Giris tutari = 0.30 * 166.67 = $50.00
- PnL% = (41.67 / 50.00) * 100 = +%83.3
- TakeProfitPercent = %15 → Fiyat ~$0.10'a ulastiginda **kar al tetiklendi**, pozisyon kapandi

---

## Islem Kapatma ve Kar/Zarar Hesaplama

Bir pozisyon kapatildiginda (SL/TP veya piyasa sonuclanmasi ile):

1. `ClosePositionHandler`, `PositionId` ve `ExitPrice` ile `ClosePositionCommand` alir
2. `position.Close(exitPrice)` — pozisyonu kapali olarak isaretler, cikis fiyatini ve zaman damgasini kaydeder
3. `position.CalculatePnL()` — nihai kar/zarar hesaplar
4. Su bilgilerle `Trade` kaydi olusturulur:
   - Giris fiyati, cikis fiyati, miktar
   - Gerceklesmis kar/zarar ve ucretler
   - Sure (pozisyon ne kadar acik kaldi)
   - Sinyali ureten strateji adi
5. `PositionClosedEvent` domain event'i aboneler icin tetiklenir

### Gerceklesmis Kar/Zarar Formulu

```
brutPnl = (cikisFiyati - girisFiyati) * miktar  [Yes icin]
brutPnl = (girisFiyati - cikisFiyati) * miktar  [No icin]
gerceklesenPnl = brutPnl - ucretler
```

---

## AI Prompt Muhendisligi

AI prompt'u, sistemin zekasinin cekirdegi niteliigindedir. Prompt'un ozeti:

### Prompt Yapisi

AI modeline sunulan bilgiler:
- **Piyasa sorusu** (ornegin "Fed Haziran 2026'da faiz indirir mi?")
- **Aciklama** (piyasanin tam tanimi)
- **Kategori** (ornegin "Ekonomi")
- **24s Hacim** (dolar cinsinden)
- **Her sonuc icin guncel piyasa fiyati** (ornegin Yes: 0.30, No: 0.70)

AI'dan istenen:
1. Her sonuc icin gercek olasilik tahmini (0.0 - 1.0)
2. Binary piyasalar icin toplam ≈ 1.0
3. Guven skoru (0.0 - 1.0)
4. 2-4 cumlelik akil yurutme

Yanit formati: JSON (`outcomes`, `confidence`, `reasoning`)

### AI Yanit Onbellegi

API maliyetlerini azaltmak icin yanitlar `CacheDurationMinutes` (varsayilan: 30 dakika) sure ile onbellege alinir. Ayni piyasa bu pencere icinde tekrar analiz edilmez.

---

## Arka Plan Servisleri

| Servis | Aralik | Amac |
|--------|--------|------|
| `MarketScannerService` | 1 dk | Piyasalari tara, sinyal uret, emir ver |
| `TradeExecutorService` | 30 sn | Live bekleyen emirlerin dolum durumunu kontrol et |
| `PositionMonitorService` | 60 sn | Acik pozisyonlari izle, SL/TP tetikle |
| `BalanceTrackerService` | periyodik | Zaman icinde bakiye goruntulerini takip et |
| `HealthMonitorService` | periyodik | Platform baglantisi ve veritabani saglik kontrolleri |

---

## Yapilandirma Referansi

| Parametre | Varsayilan | Aciklama |
|-----------|------------|----------|
| `Mode` | Paper | Paper (simule) veya Live (gercek para) |
| `InitialBalance` | $50 | Baslangic kagit bakiyesi |
| `ScanIntervalMinutes` | 1 | Piyasa tarama sikligi (dakika) |
| `MaxOpenPositions` | 20 | Maks es zamanli acik pozisyon |
| `MaxPositionSizePercent` | %5 | Kelly siniri — islem basina maks bakiye yuzdesi |
| `MinEdgeThreshold` | 0.06 | Gereken minimum edge (%6) |
| `MinVolumeUsd` | $50,000 | Minimum 24s hacim filtresi |
| `MaxSpreadCents` | 5 | Maks alim-satim farki (sent) |
| `StopLossPercent` | %10 | Zarar bunu asarsa kapat |
| `TakeProfitPercent` | %15 | Kar bunu asarsa kapat |
| `MaxHoldMinutes` | 15 | Pozisyon tutma suresi limiti (dakika) |
| `SurvivalModeThresholdUsd` | $10 | Bu bakiyenin altinda aktiviteyi azalt |

---

## Tam Islem Akis Diyagrami

```
┌─────────────────────────────────────────────────────────────────┐
│                    PIYASA TARAMA (Her 1 dk)                     │
│                                                                 │
│  Polymarket API → 500+ piyasa                                   │
│       │                                                         │
│       ▼                                                         │
│  ┌─────────────────────────────────────────┐                    │
│  │        KURAL TABANLI ON-FILTRE          │                    │
│  │  • Hacim >= $50,000                     │                    │
│  │  • Spread <= 5¢                         │                    │
│  │  • Fiyat < 15¢ VEYA Fiyat > 85¢         │                    │
│  │  • Edge >= %6                           │                    │
│  └─────────────────────────────────────────┘                    │
│       │ ~20-50 aday                                             │
│       ▼                                                         │
│  ┌─────────────────────────────────────────┐                    │
│  │        AI GERCEK DEGER ANALIZI          │                    │
│  │  • Piyasayi Claude/GPT/Gemini'ye gonder │                    │
│  │  • Gercek olasilik tahmini al           │                    │
│  │  • AI edge hesapla                      │                    │
│  └─────────────────────────────────────────┘                    │
│       │                                                         │
│       ▼                                                         │
│  ┌─────────────────────────────────────────┐                    │
│  │        HIBRIT CIFT ONAY                 │                    │
│  │  Kural sinyali + AI sinyali = AYNI yon? │                    │
│  │  EVET → Islem    HAYIR → Atla           │                    │
│  └─────────────────────────────────────────┘                    │
│       │ ~1-5 onaylanmis sinyal                                  │
│       ▼                                                         │
│  ┌─────────────────────────────────────────┐                    │
│  │        KELLY KRITERI BOYUTLANDIRMA      │                    │
│  │  odds = (1/fairValue) - 1               │                    │
│  │  kelly = edge / odds                    │                    │
│  │  boyut = bakiye × min(kelly, %5)        │                    │
│  └─────────────────────────────────────────┘                    │
│       │                                                         │
│       ▼                                                         │
│  EMIR + POZISYON OLUSTURULDU                                    │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                POZISYON IZLEME (Her 60 sn)                      │
│                                                                 │
│  Her acik pozisyon icin:                                        │
│       │                                                         │
│       ▼                                                         │
│  position.TokenId ile guncel fiyat al                           │
│       │                                                         │
│       ▼                                                         │
│  PnL% hesapla                                                   │
│       │                                                         │
│       ├── PnL% <= -%10 → ZARAR DURDUR → Pozisyonu kapat         │
│       ├── PnL% >= +%15 → KAR AL → Pozisyonu kapat               │
│       └── Aksi halde → Izlemeye devam                           │
│                                                                 │
│  Kapatmada: Gerceklesen kar/zarar ile Trade kaydi olusturulur   │
└─────────────────────────────────────────────────────────────────┘
```

---

## Ornek Senaryolar

### Senaryo 1: Basarili Yes Islemi

1. **Piyasa:** "ETH Temmuz 2026'ya kadar $10K'ya ulasacak mi?"
2. **Guncel fiyat:** $0.08 (piyasa %8 sans dusuyor)
3. **Kural filtresi:** Fiyat < 0.15 → Evet! Edge = 0.15 - 0.08 = 0.07 (%7), esik uzerinde
4. **AI analizi:** Fair value = 0.22, guven = 0.68
5. **AI edge:** |0.22 - 0.08| = 0.14 (%14), esik uzerinde, yon = Yes
6. **Hibrit kontrol:** Ikisi de Yes diyor → Sinyal onaylandi!
7. **Kelly boyutlandirma:**
   - odds = (1/0.22) - 1 = 3.545
   - kelly = 0.14 / 3.545 = 0.0395 (%3.95)
   - sinir = min(%3.95, %5) = %3.95
   - bakiye = $500 → pozisyon boyutu = $19.75
8. **Miktar:** $19.75 / $0.08 = 246.88 hisse
9. **Pozisyon acildi:** $0.08'den Yes, 246.88 hisse
10. **3 gun sonra:** Fiyat $0.18'e yukseliyor
11. **PnL:** (0.18 - 0.08) * 246.88 = $24.69
12. **PnL%:** ($24.69 / $19.75) * 100 = +%125
13. Fiyat ~$0.10'a ulastiginda **kar al (%25) tetiklendi**

### Senaryo 2: Zarar Durdur Tetiklendi

1. **Piyasa:** "SpaceX Nisan 2026'da Starship firlitacak mi?"
2. $0.12'den **Yes giris yapildi**, 416.67 hisse ($50 pozisyon)
3. Firlatma erteleme duyurusu nedeniyle **fiyat $0.09'a dusuyor**
4. **PnL:** (0.09 - 0.12) * 416.67 = -$12.50
5. **PnL%:** (-$12.50 / $50) * 100 = -%25
6. Fiyat ~$0.108'e ulastiginda **zarar durdur (-%10) tetiklendi**
