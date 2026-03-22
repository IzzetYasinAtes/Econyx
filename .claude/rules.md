# Econyx — Yazilim Kurallari ve Konvansiyonlari

## Mimari Kurallar

### Katman Bagimliligi (Kesin Kural)
- Bagimlilik her zaman ICERIYE dogru akar: Core ← Domain ← Application ← Infrastructure ← Presentation
- Ic katmanlar DIS katmanlara ASLA referans veremez
- Core ve Domain katmanlarinda DIS NuGet paketi KULLANILAMAZ (sadece .NET BCL)
- Application katmaninda sadece MediatR ve FluentValidation bulunabilir
- Infrastructure katmani tum agir bagimliliklari (EF Core, AI SDK, Polymarket.Net, Polly) icerir
- Presentation (Worker/Dashboard) sadece DI kaydi ve host konfigurasyonu yapar

### Yeni Ozellik Ekleme Sirasi
1. Once Domain'de entity/value object/event tanimla
2. Sonra Application'da command/query/handler yaz
3. Infrastructure'da repository/adapter/service implemente et
4. Worker/Dashboard'da kullan

## Kodlama Konvansiyonlari

### Entity ve Value Object Olusturma
- Entity'ler `private` constructor + `static Create()` factory metodu ile olusturulur
- Value object'ler `sealed class` veya `record` olarak tanimlanir ve immutable olur
- Factory metodlarinda validasyon yapilir (`ArgumentNullException.ThrowIfNull()`, `ArgumentException.ThrowIfNullOrWhiteSpace()`)
- Domain validasyon ihlallerinde `InvalidOperationException` firlatilir

### Result<T> Pattern (Hata Yonetimi)
- Is mantigi hatalari icin ASLA exception firlatma — `Result<T>` kullan
- `Result.Success(value)` ve `Result.Failure<T>(error)` ile dondur
- `Error` record'u: `Error.Validation()`, `Error.NotFound()`, `Error.Conflict()`, `Error.Failure()` factory metodlari
- Exception sadece programlama hatalari ve beklenmeyen durumlar icin (ArgumentException, InvalidOperationException)

### CQRS Pattern
- Her command/query ayri bir klasorde: `Commands/{Name}/` veya `Queries/{Name}/`
- Handler isimlendirme: `{Name}Handler : IRequestHandler<{Name}Command, Result<T>>`
- Validator isimlendirme: `{Name}Validator : AbstractValidator<{Name}Command>`
- Command'lar `IRequest<Result<T>>` implement eder

### Async/Await
- Tum IO operasyonlari `async` olmali
- Her async metod `CancellationToken ct = default` parametresi almali
- Task dondurmeden once `ct.ThrowIfCancellationRequested()` kontrol edilmeli

### Naming Conventions
- Namespace: `Econyx.{Katman}.{Ozellik}` (klasor yapisini takip eder)
- Interface: `I` oneki (IRepository, IPlatformAdapter)
- Port interface'ler: `Application/Ports/` altinda
- Repository interface'ler: `Domain/Repositories/` altinda
- Implementation: Infrastructure altinda ilgili klasorde

### Dependency Injection
- Her katman kendi `DependencyInjection.cs` uzanti metodunu saglar
- Constructor injection kullanilir (field injection YASAK)
- `services.Add{Katman}()` pattern'i: `AddApplication()`, `AddInfrastructure(config)`

### sealed Kullanimi
- Kalitim gerektirmeyen tum class'lar `sealed` olmali
- Domain entity'leri ve value object'ler `sealed` tanimlanir

## Test Kurallari

### Test Framework
- xUnit + Moq + FluentAssertions
- Infrastructure testleri icin InMemory EF Core (`TestDbContextFactory`)

### Test Isimlendirme
- Metod adi: `{MetodAdi}_{Senaryo}_{BeklenenSonuc}` pattern'i
- Ornek: `Create_WithValidParameters_ShouldReturnOrder`

### Test Katmanlari
- **Core.Tests:** Primitives (Result, Error), BaseEntity, ValueObject
- **Domain.Tests:** Entity factory'ler, value object validasyonlari, domain service hesaplamalari
- **Application.Tests:** Handler'lar (mock repo + mock adapter), strateji mantigi
- **Infrastructure.Tests:** Repository CRUD (InMemory DB)
- **Worker.Tests:** Service mantigi (mock mediator + mock adapter)

### Test Yazma Kurallari
- Her entity Create metodu icin pozitif ve negatif test yaz
- Handler testlerinde tum dependency'ler mock'lanir
- FluentAssertions ile assert yap (`.Should().Be()`, `.Should().NotBeNull()`)
- Test basina tek bir davranis test edilir

## EF Core Kurallari

### Entity Configuration
- Fluent API kullanilir (data annotation YASAK)
- Her entity icin ayri `{Entity}Configuration : IEntityTypeConfiguration<T>` class'i
- Money value object icin `MoneyOwnershipExtensions` kullanilir (owned type)

### Migration
- Migration isimlendirme: aciklayici isim (`AddTokenIdToPositionAndOrder`, `AddAiModelConfiguration`)
- Migration olusturma: `dotnet ef migrations add <Ad> --project src/Econyx.Infrastructure --startup-project src/Econyx.Worker`

## Strateji Pattern

### Yeni Strateji Ekleme
1. `IStrategy` interface'ini implemente et
2. `EvaluateAsync(markets, ct)` metodunu yaz
3. `StrategySignal` record'u dondur
4. `DependencyInjection.cs`'de DI'a kaydet
5. `HybridStrategy`'de entegre et (gerekiyorsa)

### Volatilite Tabanli Piyasa Secimi
- Piyasalar yuksek 24 saatlik hacme sahip olmali (MinVolumeUsd: $50,000)
- Sonuclanma tarihi 30 gun icinde olan piyasalar onceliklendirilir
- Bu iki kosul, likidite ve zaman baskisi olan piyasalarda daha iyi edge firsatlari saglar

### StrategySignal
- `MarketId`, `TokenId`, `RecommendedSide` (Yes/No), `Edge`, `FairValue`, `MarketPrice`, `Confidence`, `StrategyName`, `Reasoning`

## Background Service Kurallari

- Her service `BackgroundService` base class'ından turetilir
- `ExecuteAsync` icinde sonsuz dongu + `Task.Delay(interval)` pattern'i
- try-catch ile hata loglanir, servis DURMAAZ (graceful degradation)
- Serilog ile structured logging kullanilir

## Guvenlik

- API key'ler `IApiKeyEncryptor` ile sifrelenir (DPAPI dev / Azure Key Vault prod)
- Hassas bilgiler ASLA loglara yazilmaz
- appsettings.json'da gercek credential bulunmaz (varsayilan placeholder degerler)
- `ISecretManager` uzerinden secret erisimi
