# Security

> Bootstrap'ta belirlendi (2026-10-21). Ajanlar her planda ve review'da bu kurallara uyar.

## Secrets
- Secrets never enter the repo, specs, prompts, or chat. `.env` is gitignored; provide `.env.example`.
- Agents never print secret values, even when debugging.
- Geliştirme: `dotnet user-secrets`. Production: ortam değişkenleri veya Azure Key Vault.
- Entra kimlik bilgisi: client secret yerine mümkünse sertifika veya managed identity.
- Sır içeren ayarlar için depoya yalnızca anahtar adlarını içeren bir `*.example.json` girer.
- **İstisna:** sır içermeyen geliştirme ayarı (`appsettings.Development.json`: LocalDB + Integrated Security
  bağlantı dizesi, log seviyeleri) depoda tutulur. Bu dosyaya parola, client secret, anahtar veya gerçek
  sunucu adresi eklemek yasaktır; Entra tenant/client ID'leri de buraya değil user-secrets'a yazılır
  (`docs/development.md`).

## Input & output
- Tüm girdi sınırda doğrulanır: `DataAnnotations` (biçim) + domain doğrulaması (iş kuralı, `Result` hatası).
- Blazor varsayılan HTML-encode eder; `MarkupString` kullanmak yasaktır.
- Veri erişimi yalnızca EF Core parametreli sorgularla; ham SQL birleştirme (`FromSqlRaw` + string birleştirme) yasaktır.
- Hata mesajlarında stack trace, SQL veya kişisel veri yoktur (`docs/conventions.md`, Error handling).

## Personal data (KVKK)
- İzin açıklamaları ve hastalık izinleri kişisel veridir; hastalık izninde **tanı bilgisi toplanmaz** —
  yalnızca tarih aralığı ve tür.
- Loglarda isim/e-posta yerine çalışan ID'si kullanılır.

## AuthN / AuthZ
- Kimlik doğrulama yalnızca Entra ID (`Microsoft.Identity.Web`, OIDC). Yerel parola yoktur.
- **Varsayılan reddetme:** global `RequireAuthenticatedUser` politikası; anonim erişim yalnızca açıkça
  işaretlenmiş yerlerde (MVP'de yalnızca hata sayfası).
- Roller Entra App Roles: `Employee`, `HrAdmin`. Yönetici rolü app role değildir; çalışan–yönetici
  ilişkisinden türetilir.
- **Yetkilendirme servis katmanında** (modül `Contracts` servisleri) uygulanır, UI'da değil (IDOR önlemi).
  UI'daki gizleme yalnızca kullanım kolaylığıdır.
- Yönetici yalnızca doğrudan raporlarının taleplerini görür ve karara bağlar (BR-6); bakiye düzeltmesi yalnızca `HrAdmin` (BR-8).
- **Çalışan eşleştirme:** giriş kalıcı olarak Entra `oid` ile tanınır. `oid` ilk girişte UPN ile bağlanır; UPN için
  Entra'nın `upn` optional claim'i kullanılır (`preferred_username` değişebilir ve yetkilendirme için
  önerilmez; yalnızca yedek). Başka bir hesaba bağlı kayıt UPN ile ele geçirilemez.
- **Varsayım:** `Employee` rolü yalnızca kurum içi **Member** kullanıcılara atanır; misafir (B2B) hesaplara
  atanmaz. Bu varsayım değişirse ilk bağlama kuralı yeniden değerlendirilir (review 1, F-4).
- Sahte test kimlik doğrulaması production derlemesine girmez (FD-6).

## Dependencies
- Yeni NuGet paketi plan onayının parçasıdır: plan dosyasında gerekçesiyle listelenir; insan onaylar.
- Yalnızca izin verici lisanslar (MIT, Apache-2.0, BSD); diğerleri ADR gerektirir.
- Bilinen zafiyetli paket check'i kırmızı yapar (`vulnerable` adımı, `dotnet list package --vulnerable --include-transitive`).

## Review lens
Security is a mandatory dimension of every independent review (see `prompts/review.md`), not a
separate afterthought phase.
