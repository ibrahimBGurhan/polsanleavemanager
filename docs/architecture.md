# Architecture

> Bootstrap'ta karar verilen mimari (2026-10-21). Ajanlar planlamadan önce bu dosyayı okur.

## System overview
Polsan Leave Manager, Polsan çalışanlarının izin talebi oluşturduğu, yöneticilerin onayladığı/
reddettiği ve İK'nın izin bakiyelerini ve politikalarını yönettiği şirket içi bir web uygulamasıdır.
Tek şirket, yüzlerce kullanıcı ölçeğindedir. Mimari tarzı **modüler monolit**tir: tek deploy, tek
veritabanı, kod içinde sert modül sınırları (ADR-0001). Yığın: .NET 10 (LTS), ASP.NET Core, Blazor,
EF Core 10, SQL Server; kimlik doğrulama Entra ID SSO (ADR-0002).

## Modules / components and ownership

| Module | Single responsibility | Owns |
|---|---|---|
| `Identity` | Entra ID girişi, çalışan profili, çalışan–yönetici ilişkisi, roller | Çalışan profili, yönetici bağı |
| `Leave` | İzin talebi yaşam döngüsü ve onay akışı (BR-5, BR-6, BR-7) | `LeaveRequest` |
| `Balance` | Hak ediş, bakiye, İK düzeltmeleri (BR-1, BR-2, BR-4, BR-8) | `LeaveBalance`, düzeltme kayıtları |
| `Calendar` | Resmî tatiller ve iş günü hesabı (BR-3) | Tatil listesi |
| `Web` | Blazor UI ve HTTP uç noktaları; sunum, iş kuralı yok | — |

Proje düzeni: her modül için `PolsanLeave.<Modül>` (iç uygulama, `Internal` namespace'i dahil) ve
`PolsanLeave.<Modül>.Contracts` (public arayüz, DTO'lar). Çözüm dosyası: `PolsanLeave.slnx`.

## Communication rules
- Modüller birbirini **yalnızca `Contracts` arayüzleri üzerinden, doğrudan (senkron) çağrı** ile kullanır.
- Event/mesajlaşma MVP'de yok; gerekirse ADR ile eklenir.
- Her modül kendi `DbContext`'ine ve kendi tablolarına sahiptir (aynı veritabanı, ayrı şema:
  `identity`, `leave`, `balance`, `calendar`). Başka modülün tablosuna erişim yasaktır.

## Forbidden dependencies (make them testable)
Her kural `PolsanLeave.ArchitectureTests` içinde NetArchTest ile denetlenir ve `scripts/check` içinde çalışır.

- **FD-1:** Bir modül başka bir modülün `Internal` tiplerine, `DbContext`'ine veya tablolarına erişemez;
  yalnızca `PolsanLeave.<Modül>.Contracts` projesine referans verebilir.
- **FD-2:** Domain katmanı (`*.Internal.Domain`) ASP.NET Core, EF Core ve Blazor'a bağımlı olamaz.
- **FD-3:** `Web` iş kuralı içermez ve veritabanına doğrudan erişmez; yalnızca `Contracts` servislerini çağırır.
- **FD-4:** Döngüsel modül bağımlılığı yasak. İzin verilen yön: `Leave` → `Balance`, `Calendar`,
  `Identity`; `Balance` → `Calendar`, `Identity`; `Identity` → —; `Calendar` → —.
- **FD-5:** `DateTime.Now` / `DateTime.UtcNow` / `DateTimeOffset.Now` kullanılmaz; zaman `TimeProvider`
  ile enjekte edilir.
- **FD-6:** Test amaçlı sahte kimlik doğrulama handler'ı production derlemesine girmez (yalnızca test projelerinde).

## Deliberately out of scope
- Bordro / mevcut İK sistemi entegrasyonu
- Mobil uygulama
- Çok kademeli onay zinciri, vekâlet, otomatik eskalasyon
- Yarım gün / saatlik izin
- Çok şirketli (multi-tenant) yapı
- Event-driven iletişim, mikroservisler
