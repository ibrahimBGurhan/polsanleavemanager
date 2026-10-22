# Development

> Yerel geliştirme ortamı (spec 0001, plan adım 10). Kurallar `docs/conventions.md`, `docs/testing.md` ve
> `docs/security.md`'de; bu dosya yalnızca "nasıl çalıştırırım" sorusunu cevaplar.

## Gereksinimler
- .NET SDK 10.0.402 veya üstü (`global.json` sabitler)
- SQL Server LocalDB (Visual Studio veya SQL Server Express ile gelir; `sqllocaldb info` ile kontrol)
- Git Bash (Windows) — `./scripts/check` POSIX sh betiğidir

## İlk kurulum (bir kez)
```sh
dotnet tool restore                      # dotnet-ef (dotnet-tools.json)
dotnet build PolsanLeave.slnx -c Release
# Playwright Chromium (E2E). Windows PowerShell 5.1 yeterli; pwsh varsa o da olur:
powershell -NoProfile -ExecutionPolicy Bypass -File tests/PolsanLeave.E2E/bin/Release/net10.0/playwright.ps1 install chromium
./scripts/check                          # full; iç döngüde ./scripts/check --fast
```
`./scripts/check` testler için `PolsanLeaveTest` adlı ayrı bir LocalDB örneğini kendisi oluşturur, başlatır
ve adım sonunda durdurur. Geliştirme veritabanınıza (`MSSQLLocalDB`) dokunmaz.

## Uygulamayı yerelde çalıştırmak

### 1. Entra ID uygulama kaydı (bir kez; Entra yöneticisi veya BT)
1. Entra yönetim merkezi → **App registrations** → **New registration**
   - Ad: `Polsan Leave Manager (Dev)`; hesap türü: **Single tenant**
   - Redirect URI (Web): `https://localhost:7179/signin-oidc`
2. **Authentication**: front-channel logout URL `https://localhost:7179/signout-callback-oidc`; **ID tokens** işaretli.
3. **App roles** → iki rol oluşturun (Allowed member types: Users/Groups):
   - `Employee` (değer `Employee`) — uygulamayı kullanabilir
   - `HrAdmin` (değer `HrAdmin`) — İK işlemleri (sonraki spec'ler)
4. **Enterprise applications** → bu uygulama → **Users and groups**: kendinize `Employee` rolünü atayın.
5. Client secret **gerekmez** (yalnızca oturum açma; API çağrısı yok).

### 2. Gizli olmayan ayarlar (user-secrets)
Tenant ve client ID gizli değildir ama ortama özgüdür; depoya yazılmaz:
```sh
dotnet user-secrets --project src/PolsanLeave.Web init
dotnet user-secrets --project src/PolsanLeave.Web set "AzureAd:TenantId" "<tenant-id>"
dotnet user-secrets --project src/PolsanLeave.Web set "AzureAd:ClientId" "<client-id>"
```
Bağlantı dizesi `appsettings.Development.json`'da: `(localdb)\MSSQLLocalDB`, veritabanı `PolsanLeave`.
Development ortamında şema başlangıçta otomatik oluşturulur (migration'lar uygulanır).

### 3. Başlangıç verisi (seed)
Depodaki `seed/dev-seed.json` yalnızca **kurgusal** kişiler içerir (`@ornek.polsan.test`). Kendi hesabınızla
giriş yapabilmek için bir kopyasına kendinizi ekleyin — `*.local.json` dosyaları git'e girmez:
```sh
cp seed/dev-seed.json seed/dev.local.json
# seed/dev.local.json içine kendi UPN'inizle bir çalışan ekleyin (managerEmployeeNumber: "1001")
dotnet run --project src/PolsanLeave.Web -- --import-seed seed/dev.local.json
```
İçe aktarma idempotenttir: çalışanlar sicil numarasıyla güncellenir, açılış bakiyesi çalışan başına bir kez
yazılır, açılış tarihi sonradan değiştirilemez. **Gerçek İK verisi (doğum tarihleri — KVKK) asla depoya
girmez**; production içe aktarma dosyası depo dışında tutulur.

### 4. Çalıştırma
```sh
dotnet run --project src/PolsanLeave.Web --launch-profile https
```
`https://localhost:7179` → Entra girişi → "İzinlerim".

## Migration eklemek
Her modülün kendi `DbContext`'i ve migration klasörü vardır:
```sh
dotnet ef migrations add <Ad> --project src/PolsanLeave.<Modül> --startup-project src/PolsanLeave.<Modül> \
  --output-dir Internal/Data/Migrations --namespace PolsanLeave.<Modül>.Internal.Data.Migrations
```
Bilinen tuzak: `dotnet ef` model snapshot dosyasını `src/PolsanLeave.<Modül>/PolsanLeave/<Modül>/...` altına
koyabilir; `Internal/Data/Migrations/` içine taşıyın ve boş klasörü silin. Migration dosyaları
`.editorconfig`'te üretilmiş kod olarak işaretlidir (analyzer'lar uygulanmaz).

## CI
`.github/workflows/check.yml` SQL Server'ı servis konteyneri olarak çalıştırır; depo secret'ı
`CI_MSSQL_SA_PASSWORD` gerekir (yalnızca CI'daki geçici konteynerin parolası). Playwright Chromium CI'da
her çalıştırmada kurulur.
