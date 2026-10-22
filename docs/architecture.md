# Architecture

> Bootstrap'ta karar verilen mimari (2026-10-21), spec 0001 ile netleşti (2026-10-22). Ajanlar planlamadan önce bu dosyayı okur.

## System overview
Polsan Leave Manager, Polsan çalışanlarının izin talebi oluşturduğu, yöneticilerin onayladığı/
reddettiği ve İK'nın izin bakiyelerini ve politikalarını yönettiği şirket içi bir web uygulamasıdır.
Tek şirket, yüzlerce kullanıcı ölçeğindedir. Mimari tarzı **modüler monolit**tir: tek deploy, tek
veritabanı, kod içinde sert modül sınırları (ADR-0001). Yığın: .NET 10 (LTS), ASP.NET Core, Blazor,
EF Core 10, SQL Server; kimlik doğrulama Entra ID SSO (ADR-0002).

## Modules / components and ownership

| Module | Single responsibility | Owns |
|---|---|---|
| `Identity` | Çalışan kaydı, Entra kimliğiyle eşleştirme (oid, UPN), çalışan–yönetici ilişkisi, seed içe aktarma | Çalışan profili, yönetici bağı |
| `Leave` | İzin talebi yaşam döngüsü ve onay akışı (BR-4, BR-5, BR-6, BR-7); bakiyenin **borç** tarafı ve Kalan · Bekleyen · Kullanılabilir özeti | `LeaveRequest` |
| `Balance` | Bakiyenin **alacak** tarafı: açılış bakiyesi, hak ediş (BR-1, BR-2; okuma anında hesaplanır), İK düzeltmeleri (BR-8) | Düzeltme kayıtları (yalnızca ekleme), açılış tarihi |
| `Calendar` | Resmî tatiller ve iş günü hesabı (BR-3) | Tatil listesi, tanımlı yıllar |
| `Web` | Blazor UI, Entra girişi, kompozisyon kökü, seed içe aktarma komutu; sunum, iş kuralı yok | — |
| `SharedKernel` | Ortak yapı taşları: `Result<T>`, `Error`, `IBusinessClock` (İstanbul "bugün") | — |

Proje düzeni: her modül için `PolsanLeave.<Modül>` (iç uygulama, `Internal` namespace'i dahil) ve
`PolsanLeave.<Modül>.Contracts` (public arayüz, DTO'lar). Çözüm dosyası: `PolsanLeave.slnx`.

Bakiyenin iki modüle bölünmesinin nedeni FD-4'tür: Leave → Balance yönü serbest, tersi yasak. Böylece
BR-4/BR-5 kontrolü ve talep kaydı tek modülde, tek transaction'da ve çalışan başına kilit
(`sp_getapplock`) altında yapılır (plan 0001, Risk 1 ve 3).

## Communication rules
- Modüller birbirini **yalnızca `Contracts` arayüzleri üzerinden, doğrudan (senkron) çağrı** ile kullanır.
- Event/mesajlaşma MVP'de yok; gerekirse ADR ile eklenir.
- Her modül kendi `DbContext`'ine ve kendi tablolarına sahiptir (aynı veritabanı, ayrı şema:
  `identity`, `leave`, `balance`, `calendar`). Başka modülün tablosuna erişim yasaktır.
- Modüller `DbContext`'i **fabrika** ile (`IDbContextFactory`) kullanır; her servis çağrısı kendi kısa
  ömürlü context'ini açar. Blazor Server devresi tek ve uzun bir DI kapsamıdır, olay işleyicileri iç içe
  geçer; kapsamlı (scoped) context eşzamanlı erişim hatası verir (plan 0001, D9; `CircuitScopeIntegrationTests`).
- Servisler "kim" bilgisini yalnızca `ICurrentEmployee`'den alır; UI'dan çalışan veya talep ID'si almaz (IDOR).
- Web modüllerin `Contracts` servislerini çağırır. Kompozisyon kökü (`Web/Composition`) ve seed içe
  aktarma (`Web/Seeding`) da yalnızca modüllerin public kayıt ve `Contracts` API'sini kullanır.

## Forbidden dependencies (make them testable)
Her kural `PolsanLeave.ArchitectureTests` içinde denetlenir ve `scripts/check` içinde çalışır.

- **FD-1:** Bir modül başka bir modülün `Internal` tiplerine, `DbContext`'ine veya tablolarına erişemez;
  yalnızca `PolsanLeave.<Modül>.Contracts` projesine referans verebilir.
- **FD-2:** Domain katmanı (`*.Internal.Domain`) ASP.NET Core, EF Core ve Blazor'a bağımlı olamaz.
- **FD-3:** `Web` iş kuralı içermez ve veritabanına doğrudan erişmez; yalnızca `Contracts` servislerini çağırır.
- **FD-4:** Döngüsel modül bağımlılığı yasak. İzin verilen yön: `Leave` → `Balance`, `Calendar`,
  `Identity`; `Balance` → `Calendar`, `Identity`; `Identity` → —; `Calendar` → —.
- **FD-5:** `DateTime.Now` / `DateTime.UtcNow` / `DateTimeOffset.Now` kullanılmaz; zaman `TimeProvider`
  ile enjekte edilir. Test, üretim derlemelerinin IL kodunu tarar.
- **FD-6:** Test amaçlı sahte kimlik doğrulama handler'ı production derlemesine girmez; hiçbir üretim
  derlemesi `PolsanLeave.TestInfrastructure`'a referans vermez.

## Deliberately out of scope
- Bordro / mevcut İK sistemi entegrasyonu
- Mobil uygulama
- Çok kademeli onay zinciri, vekâlet, otomatik eskalasyon
- Yarım gün / saatlik izin
- Çok şirketli (multi-tenant) yapı
- Event-driven iletişim, mikroservisler
- Production migration dağıtımı (migration bundle) — ayrı spec (plan 0001, Risk 7)
