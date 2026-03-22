# Econyx Usage Guide

## What is Polymarket?

Polymarket is a decentralized prediction market platform built on the Polygon blockchain. Users buy and sell shares of event outcomes (e.g., "Will Bitcoin reach $100K by June 2026?"). Each share is priced between $0.00 and $1.00, representing the market's perceived probability of the event occurring. If the event happens, "Yes" shares pay $1.00; if not, they pay $0.00.

**Key Concepts:**

- **Market**: A question about a future event with defined outcomes (usually Yes/No)
- **Share Price**: Represents probability (e.g., $0.65 = market thinks 65% chance)
- **USDC**: The stablecoin (1 USDC = 1 USD) used for all transactions
- **Polygon Network**: The blockchain where Polymarket operates (low gas fees)
- **CLOB (Central Limit Order Book)**: Polymarket's on-chain order book for placing limit/market orders

## How Does Econyx Work?

Econyx is an autonomous trading bot that continuously scans Polymarket for mispriced contracts. Here's the complete flow:

### 1. Market Scanning (Every 2 Minutes)

The bot fetches all open markets from Polymarket's API (~500+ markets at any time). It checks each market's volume, spread, and status.

### 2. Rule-Based Pre-Filter

Before spending money on AI calls, a rule-based filter eliminates markets that don't meet basic criteria:

- Minimum trading volume ($50,000 default)
- Maximum bid-ask spread (5 cents default)
- Price anomalies: shares priced below $0.15 (potential undervalued) or above $0.85 (potential overvalued)

This reduces ~500 markets down to ~20-50 candidates.

### 3. AI Fair Value Analysis

For each candidate market, the bot sends the question, description, and current prices to an AI model (Claude, GPT, Gemini, etc.). The AI estimates the **true probability** of each outcome.

**Example:**
- Market Question: "Will the Fed cut rates in June 2026?"
- Current price: $0.30 (market thinks 30% chance)
- AI estimate: $0.55 (AI thinks 55% chance)
- **Edge**: |0.55 - 0.30| = 0.25 (25% edge)

### 4. Hybrid Confirmation

The bot only trades when **both** the rule-based system and the AI agree on the direction. This dual confirmation reduces false signals significantly.

### 5. Position Sizing (Kelly Criterion)

Instead of betting a fixed amount, the bot uses the Kelly Criterion formula:

```
kelly_fraction = edge / odds
position_size = balance * min(kelly_fraction, max_position_percent)
```

This mathematically optimal formula bets more when the edge is larger and less when it's smaller, maximizing long-term growth while limiting risk.

### 6. Risk Management

- **Stop-Loss**: Automatically closes a position if the loss exceeds 15% (configurable)
- **Take-Profit**: Automatically closes when profit exceeds 25% (configurable)
- **Max Positions**: Limits total open positions to 20 (configurable)
- **Survival Mode**: Reduces activity when balance drops below a critical threshold

## Prerequisites

- **.NET 10 SDK** (download from https://dotnet.microsoft.com)
- **SQL Server** (LocalDB, Express, or full edition)
- **Polymarket Account** (for live trading)
- **AI API Key** (at least one: OpenRouter, Anthropic, or OpenAI)

## Step 1: Create a Polymarket Account

1. Go to https://polymarket.com
2. Click **Sign Up**
3. Choose one of three methods:
   - **Google Account**: Click "Continue with Google"
   - **Email**: Enter your email and verify with the 6-digit code sent to you
   - **Crypto Wallet**: Connect MetaMask, Rabby, or Phantom wallet
4. Set your display name/username
5. Complete KYC verification (required for trading):
   - Provide full name, date of birth, and address
   - Upload government-issued ID if requested
   - Processing takes up to 3-5 business days
6. Deposit USDC on Polygon network to your Polymarket wallet

## Step 2: Get Your Polymarket Private Key

For bot trading via the CLOB API, you need your wallet's private key:

1. If using MetaMask: Settings > Security > Export Private Key
2. **Never share this key with anyone**
3. Store it securely (you'll enter it in the Econyx dashboard)

## Step 3: Get an AI API Key

You need at least one AI provider key. Options:

### OpenRouter (Recommended - Access to All Models)
1. Go to https://openrouter.ai
2. Create an account
3. Go to Keys > Create Key
4. Add credits ($5-10 to start)

### Anthropic (Claude Direct)
1. Go to https://console.anthropic.com
2. Create an account
3. Go to API Keys > Create Key
4. Add credits

### OpenAI (GPT Direct)
1. Go to https://platform.openai.com
2. Create an account
3. Go to API Keys > Create New Secret Key
4. Add credits

## Step 4: Clone and Build

```bash
git clone https://github.com/user/Econyx.git
cd Econyx
dotnet build
```

## Step 5: Configure the Database

The application automatically creates the database and runs migrations on first startup. The default connection string in `appsettings.json` points to:

```
Server=localhost,1433;Database=EconyxDb;User Id=sa;Password=Your_Password_Here;TrustServerCertificate=True;
```

Update this in `src/Econyx.Worker/appsettings.json` and `src/Econyx.Dashboard/appsettings.json` to match your SQL Server setup.

For SQL Server LocalDB:
```
Server=(localdb)\MSSQLLocalDB;Database=EconyxDb;Trusted_Connection=True;TrustServerCertificate=True;
```

## Step 6: Start the Application

```bash
dotnet run --project src/Econyx.Worker
dotnet run --project src/Econyx.Dashboard
```

Open your browser and navigate to `https://localhost:5001` (or the URL shown in the terminal).

## Step 7: Configure via Dashboard

1. **Settings > API Keys**: Enter your AI API key(s) and Polymarket private key
2. **Settings > Active Model**: Select your preferred AI model
3. **Settings > Trading**: Review and adjust trading parameters
4. **Settings > Platform**: Verify Polymarket connection

## Step 8: Paper Trading (Recommended First)

By default, Econyx runs in **Paper Trading** mode. This simulates all trades without using real money. Use this to:

- Verify your AI API key works
- Watch the bot find and execute simulated trades
- Understand the edge and signal quality
- Fine-tune parameters (edge threshold, position size, etc.)

Monitor the Dashboard to see:
- **Balance**: Your simulated balance growth/decline
- **Positions**: Currently open simulated positions
- **Trades**: History of all executed trades
- **Win Rate**: Percentage of profitable trades

## Step 9: Go Live (When Ready)

When you're comfortable with the paper trading results:

1. Go to Settings > Trading > Mode
2. Switch from "Paper" to "Live"
3. Save the configuration
4. The bot will now execute real trades on Polymarket using your deposited USDC

**Warning**: Live trading involves real financial risk. Start with a small amount you can afford to lose.

## Configuration Reference

| Parameter | Default | Description |
|-----------|---------|-------------|
| `Trading:Mode` | Paper | Paper (simulated) or Live (real money) |
| `Trading:InitialBalance` | 50 | Starting paper balance in USD |
| `Trading:ScanIntervalMinutes` | 2 | How often to scan markets |
| `Trading:MaxOpenPositions` | 20 | Maximum simultaneous open positions |
| `Trading:MaxPositionSizePercent` | 2 | Max % of balance per position |
| `Trading:MinEdgeThreshold` | 0.06 | Minimum edge to enter a trade (6%) |
| `Trading:MinVolumeUsd` | 50000 | Minimum market volume filter |
| `Trading:MaxSpreadCents` | 5 | Maximum bid-ask spread in cents |
| `Trading:StopLossPercent` | 15 | Close position if loss exceeds this % |
| `Trading:TakeProfitPercent` | 25 | Close position if profit exceeds this % |
| `Trading:SurvivalModeThresholdUsd` | 10 | Reduce activity below this balance |

## Tips for Better Results

1. **Start with Paper Trading** - Always validate your strategy before going live
2. **Use a Good AI Model** - Claude Sonnet or GPT-4o provide the best fair value estimates
3. **Don't Lower MinEdgeThreshold Too Much** - Below 5% edge, transaction costs eat into profits
4. **Monitor Daily API Costs** - AI calls cost money; the dashboard shows your daily API spend
5. **Keep MaxPositionSizePercent Low** - 2% per position protects against any single bad trade
6. **Watch High-Volume Markets** - More liquid markets have tighter spreads and better execution

## Troubleshooting

**Bot isn't finding any signals:**
- Check your AI API key is valid and has credits
- Verify the MinEdgeThreshold isn't too high (try 0.05)
- Ensure MinVolumeUsd isn't filtering out too many markets

**Dashboard shows no data:**
- Verify the Worker service is running
- Check the database connection string
- Look at the Worker terminal/logs for errors

**AI calls failing:**
- Verify your API key in Settings > API Keys
- Check your AI provider account has sufficient credits
- Try switching to a different model

---
---

# Econyx Kullanim Kilavuzu

## Polymarket Nedir?

Polymarket, Polygon blockchain uzerinde kurulu merkeziyetsiz bir tahmin piyasasi platformudur. Kullanicilar gelecekteki olaylarin sonuclari uzerinde hisse alip satarlar (ornegin "Bitcoin Haziran 2026'ya kadar 100.000$'a ulasacak mi?"). Her hisse $0.00 ile $1.00 arasinda fiyatlanir ve piyasanin olayin gerceklesme olasiligina dair algisini temsil eder. Olay gerceklesirse "Evet" hisseleri $1.00, gerceklesmezse $0.00 odar.

**Temel Kavramlar:**

- **Piyasa (Market)**: Gelecekteki bir olay hakkinda bir soru ve tanimli sonuclar (genellikle Evet/Hayir)
- **Hisse Fiyati**: Olasiligi temsil eder (ornegin $0.65 = piyasa %65 olasılik dusuyor)
- **USDC**: Tum islemlerde kullanilan stablecoin (1 USDC = 1 USD)
- **Polygon Agi**: Polymarket'in uzerinde calistigi blockchain (dusuk islem ucreti)
- **CLOB (Central Limit Order Book)**: Polymarket'in limit/market emirleri icin zincir uzerindeki emir defteri

## Econyx Nasil Calisiyor?

Econyx, Polymarket'te yanlis fiyatlanmis kontratlari surekli tarayan otonom bir trading botudur. Tam akis:

### 1. Piyasa Tarama (Her 2 Dakikada)

Bot, Polymarket API'sinden tum acik piyasalari ceker (herhangi bir anda ~500+ piyasa). Her piyasanin hacmini, spreadini ve durumunu kontrol eder.

### 2. Kural Tabanli On-Filtre

AI cagrilarina para harcamadan once, kural tabanli filtre temel kriterleri karsilamayan piyasalari eler:

- Minimum islem hacmi ($50,000 varsayilan)
- Maksimum alim-satim farki (5 sent varsayilan)
- Fiyat anomalileri: $0.15 altinda fiyatlanmis (potansiyel olarak degerinin altinda) veya $0.85 uzerinde (potansiyel olarak asiri degerli)

Bu, ~500 piyasayi ~20-50 adaya dusurur.

### 3. AI ile Gercek Deger Analizi

Her aday piyasa icin bot, soruyu, aciklamayi ve guncel fiyatlari bir AI modeline (Claude, GPT, Gemini vb.) gonderir. AI her sonuc icin **gercek olasiligi** tahmin eder.

**Ornek:**
- Piyasa Sorusu: "Fed Haziran 2026'da faiz indirir mi?"
- Guncel fiyat: $0.30 (piyasa %30 olasılik dusuyor)
- AI tahmini: $0.55 (AI %55 olasılik dusuyor)
- **Edge (Avantaj)**: |0.55 - 0.30| = 0.25 (%25 edge)

### 4. Hibrit Onay

Bot sadece **hem** kural tabanli sistem **hem de** AI ayni yonde hemfikir oldugunda islem acar. Bu cift onay yanlis sinyalleri onemli olcude azaltir.

### 5. Pozisyon Boyutlandirma (Kelly Kriteri)

Bot sabit bir miktar yerine, Kelly Kriteri formulunu kullanir:

```
kelly_orani = edge / odds
pozisyon_boyutu = bakiye * min(kelly_orani, maks_pozisyon_yuzdesi)
```

Bu matematiksel olarak optimal formul, edge buyuk oldugunda daha fazla, kucuk oldugunda daha az yatirim yapar ve riski sinirlarken uzun vadeli buyumeyi maksimize eder.

### 6. Risk Yonetimi

- **Zarar Durdur (Stop-Loss)**: Zarar %15'i asarsa pozisyonu otomatik kapatir (ayarlanabilir)
- **Kar Al (Take-Profit)**: Kar %25'i asarsa otomatik kapatir (ayarlanabilir)
- **Maks Pozisyon**: Toplam acik pozisyonlari 20 ile sinirlar (ayarlanabilir)
- **Hayatta Kalma Modu**: Bakiye kritik esik degerinin altina duserse aktiviteyi azaltir

## Gereksinimler

- **.NET 10 SDK** (https://dotnet.microsoft.com adresinden indirin)
- **SQL Server** (LocalDB, Express veya tam surum)
- **Polymarket Hesabi** (canli islem icin)
- **AI API Anahtari** (en az biri: OpenRouter, Anthropic veya OpenAI)

## Adim 1: Polymarket Hesabi Olusturma

1. https://polymarket.com adresine gidin
2. **Sign Up** (Kaydol) tusuna basin
3. Uc yontemden birini secin:
   - **Google Hesabi**: "Continue with Google" tusuna basin
   - **E-posta**: E-postanizi girin ve gonderilen 6 haneli kod ile dogrulayin
   - **Kripto Cuzdan**: MetaMask, Rabby veya Phantom cuzdanini baglayin
4. Goruntu adinizi/kullanici adinizi ayarlayin
5. KYC dogrulamasini tamamlayin (islem icin zorunlu):
   - Ad, soyad, dogum tarihi ve adres bilgilerinizi girin
   - Istenirse resmi kimlik belgesi yukleyin
   - Islem suresi 3-5 is gunu
6. Polygon aginda USDC yatirin

## Adim 2: Polymarket Ozel Anahtari Alma

CLOB API uzerinden bot islemleri icin cuzdaninizin ozel anahtarina ihtiyaciniz var:

1. MetaMask kullaniyorsaniz: Ayarlar > Guvenlik > Ozel Anahtari Disa Aktar
2. **Bu anahtari kimseyle paylamayin**
3. Guvenli bir sekilde saklayin (Econyx dashboard'una gireceksiniz)

## Adim 3: AI API Anahtari Alma

En az bir AI saglayici anahtarina ihtiyaciniz var. Secenekler:

### OpenRouter (Onerilen - Tum Modellere Erisim)
1. https://openrouter.ai adresine gidin
2. Hesap olusturun
3. Keys > Create Key'e gidin
4. Kredi yukleyin (baslamak icin $5-10)

### Anthropic (Claude Direkt)
1. https://console.anthropic.com adresine gidin
2. Hesap olusturun
3. API Keys > Create Key'e gidin
4. Kredi yukleyin

### OpenAI (GPT Direkt)
1. https://platform.openai.com adresine gidin
2. Hesap olusturun
3. API Keys > Create New Secret Key'e gidin
4. Kredi yukleyin

## Adim 4: Klonlama ve Derleme

```bash
git clone https://github.com/user/Econyx.git
cd Econyx
dotnet build
```

## Adim 5: Veritabani Yapilandirmasi

Uygulama ilk baslatmada otomatik olarak veritabanini olusturur ve migration'lari uygular. Varsayilan baglanti dizesi `appsettings.json` icinde:

```
Server=localhost,1433;Database=EconyxDb;User Id=sa;Password=Your_Password_Here;TrustServerCertificate=True;
```

Bunu `src/Econyx.Worker/appsettings.json` ve `src/Econyx.Dashboard/appsettings.json` dosyalarinda SQL Server yapilandirmaniza gore guncelleyin.

SQL Server LocalDB icin:
```
Server=(localdb)\MSSQLLocalDB;Database=EconyxDb;Trusted_Connection=True;TrustServerCertificate=True;
```

## Adim 6: Uygulamayi Baslatma

```bash
dotnet run --project src/Econyx.Worker
dotnet run --project src/Econyx.Dashboard
```

Tarayicinizi acin ve `https://localhost:5001` adresine gidin (veya terminalde gosterilen URL).

## Adim 7: Dashboard Uzerinden Yapilandirma

1. **Ayarlar > API Anahtarlari**: AI API anahtari/anahtarlarinizi ve Polymarket ozel anahtarinizi girin
2. **Ayarlar > Aktif Model**: Tercih ettiginiz AI modelini secin
3. **Ayarlar > Islem**: Islem parametrelerini inceleyin ve ayarlayin
4. **Ayarlar > Platform**: Polymarket baglantisini dogrulayin

## Adim 8: Kagit Uzerinde Islem (Once Onerilen)

Varsayilan olarak Econyx **Paper Trading** modunda calisir. Bu, gercek para kullanmadan tum islemleri simule eder. Bunu kullanarak:

- AI API anahtarinizin calistigini dogrulayin
- Botun simule edilmis islemleri bulup yurutmesini izleyin
- Edge ve sinyal kalitesini anlayin
- Parametreleri ince ayarlayin (edge esigi, pozisyon boyutu vb.)

Dashboard'dan izleyin:
- **Bakiye**: Simule edilmis bakiye buyumesi/dususu
- **Pozisyonlar**: Suanda acik simule pozisyonlar
- **Islemler**: Yurutulen tum islemlerin gecmisi
- **Kazanma Orani**: Karlı islemlerin yuzdesi

## Adim 9: Canli Isleme Gecis (Hazir Olunca)

Kagit islem sonuclari sizi tatmin ettiginde:

1. Ayarlar > Islem > Mod'a gidin
2. "Paper"dan "Live"a gecin
3. Yapilandirmayi kaydedin
4. Bot artik yatirdiginiz USDC ile Polymarket'te gercek islemler yapacak

**Uyari**: Canli islem gercek finansal risk icerir. Kaybetmeyi goze alabilecaginiz kucuk bir miktar ile baslayin.

## Yapilandirma Referansi

| Parametre | Varsayilan | Aciklama |
|-----------|------------|----------|
| `Trading:Mode` | Paper | Paper (simule) veya Live (gercek para) |
| `Trading:InitialBalance` | 50 | Baslangic kagit bakiyesi (USD) |
| `Trading:ScanIntervalMinutes` | 2 | Piyasa tarama sikligi (dakika) |
| `Trading:MaxOpenPositions` | 20 | Maksimum es zamanli acik pozisyon |
| `Trading:MaxPositionSizePercent` | 2 | Pozisyon basina maks bakiye yuzdesi |
| `Trading:MinEdgeThreshold` | 0.06 | Islem acmak icin min edge (%6) |
| `Trading:MinVolumeUsd` | 50000 | Minimum piyasa hacmi filtresi |
| `Trading:MaxSpreadCents` | 5 | Maksimum alim-satim farki (sent) |
| `Trading:StopLossPercent` | 15 | Zarar bu yuzedeyi asarsa pozisyonu kapat |
| `Trading:TakeProfitPercent` | 25 | Kar bu yuzedeyi asarsa pozisyonu kapat |
| `Trading:SurvivalModeThresholdUsd` | 10 | Bu bakiyenin altinda aktiviteyi azalt |

## Daha Iyi Sonuclar Icin Ipuclari

1. **Paper Trading ile Baslayin** - Canli gecmeden once her zaman stratejinizi dogrulayin
2. **Iyi Bir AI Model Kullanin** - Claude Sonnet veya GPT-4o en iyi fair value tahminlerini sunar
3. **MinEdgeThreshold'u Cok Dusurmeyin** - %5 edge'in altinda islem maliyetleri karlari yer
4. **Gunluk API Maliyetlerini Izleyin** - AI cagrilari paraya mal olur; dashboard gunluk API harcamanizi gosterir
5. **MaxPositionSizePercent'i Dusuk Tutun** - Pozisyon basina %2, tek bir kotu islemden korur
6. **Yuksek Hacimli Piyasalari Izleyin** - Daha likit piyasalarda spreadler daha dar, islem daha iyi

## Sorun Giderme

**Bot hic sinyal bulmuyor:**
- AI API anahtarinizin gecerli oldugunu ve kredisi oldugunu kontrol edin
- MinEdgeThreshold'un cok yuksek olmadigini dogrulayin (0.05 deneyin)
- MinVolumeUsd'nin cok fazla piyasayi filtrelemediginden emin olun

**Dashboard veri gostermiyor:**
- Worker servisinin calistigini dogrulayin
- Veritabani baglanti dizesini kontrol edin
- Worker terminalindeki/log'larindaki hatalara bakin

**AI cagrilari basarisiz oluyor:**
- Ayarlar > API Anahtarlari'ndaki anahtarinizi dogrulayin
- AI saglayici hesabinizda yeterli kredi olup olmadigini kontrol edin
- Farkli bir modele gecmeyi deneyin
