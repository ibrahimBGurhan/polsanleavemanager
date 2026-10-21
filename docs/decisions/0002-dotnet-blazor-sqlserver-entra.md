# ADR 0002 — .NET 10 + Blazor + SQL Server + Entra ID

- Status: Accepted
- Date: 2026-10-21

## Context
Kimlik doğrulama şirket dizini (Entra ID) ile SSO olmalı. Uygulama iç kullanım içindir; ayrı bir frontend
ekibi yoktur. Geliştirici makinelerinde .NET 10 SDK ve SQL Server LocalDB mevcut, Docker yok.

## Decision
.NET 10 (LTS), ASP.NET Core, Blazor (Server interaktif render), EF Core 10 ve SQL Server kullanıyoruz.
Kimlik doğrulama `Microsoft.Identity.Web` ile Entra ID OIDC; roller Entra App Roles. Integration testleri
yerelde LocalDB, CI'da SQL Server servis konteyneri üzerinde gerçek SQL Server ile çalışır.

## Consequences
- Kazanç: tek dil (C#), tek toolchain, tek deploy; Entra entegrasyonu birinci sınıf; check adımları az.
- Maliyet: Blazor Server kalıcı bağlantı (SignalR) gerektirir — yüzlerce kullanıcı için sorun değil, ama
  bağlantı kopmalarında UX düşünülmeli. LocalDB yalnızca Windows'ta; CI farklı bir SQL Server kurulumu kullanır.

## Alternatives considered
- **.NET API + React SPA:** iki toolchain (dotnet + npm), daha fazla check adımı; frontend uzmanlığı gerektirmeyen iç araç için gereksiz.
- **Node/TypeScript (NestJS + React):** Entra entegrasyonu daha fazla el emeği; ekip/şirket ekosistemine daha uzak.
- **PostgreSQL:** lisans avantajı, ama kurumda SQL Server mevcut ve Microsoft ekosistemiyle uyumlu.
- **EF InMemory / SQLite ile integration testi:** SQL Server'dan farklı davranır, sahte güven verir.

## Revisit triggers
- Harici (internet) kullanıcı veya mobil ihtiyacı → SPA/API ayrımı.
- Docker'ın geliştirici makinelerine gelmesi → Testcontainers'a geçiş.
- .NET 10 destek sonu (Kasım 2028) → sonraki LTS.
