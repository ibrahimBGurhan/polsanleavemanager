# Conventions

> Bootstrap'ta belirlendi (2026-10-21). Buradaki her kural ya araçla zorlanır (tercih edilen) ya da review'da denetlenir.

## Language
<!-- Set at bootstrap (step 0) and mirrored in AGENTS.md. Changing it later is allowed — edit both
     places; existing documents are not translated retroactively. -->
- Chat language (interviews, sessions): Turkish (tr)
- Document language (docs/, specs, plans, ADRs, review and verify reports): Turkish (tr)
- Always English (protocol, not prose): ANEW core files, template headings and field labels,
  `Status:` values (Draft / Approved / In progress / Shipped), `Approved by / on:`, `Source:`.
- Code identifiers, branch names, commit messages: English
- UI metinleri: Türkçe, `.resx` kaynak dosyalarında (koda gömülü metin yok)
- Agent messages (handoff summaries, refusals, `Next:` lines): chat language; command names, file
  paths and protocol fields verbatim.

## Language & framework versions
- .NET 10 (LTS) SDK, `global.json` ile sabitlenir; C# 14
- ASP.NET Core 10, Blazor (Server interaktif render), EF Core 10, SQL Server
- `Microsoft.Identity.Web` (Entra ID, OpenID Connect)
- Paket sürümleri merkezi: `Directory.Packages.props` (Central Package Management)
- `Directory.Build.props`: `Nullable=enable`, `TreatWarningsAsErrors=true`, `AnalysisLevel=latest-recommended`

## Naming
- .NET standardı: tipler/metotlar PascalCase, yerel değişken/parametre camelCase, private alan `_camelCase`, arayüz `I` önekli
- Projeler: `PolsanLeave.<Modül>`, `PolsanLeave.<Modül>.Contracts`, `PolsanLeave.<Modül>.Tests`, `PolsanLeave.ArchitectureTests`
- Test metotları: `Method_Condition_ExpectedResult` (ör. `Submit_InsufficientBalance_ReturnsBalanceInsufficient`)
- Hata kodları: `<Modül>.<Hata>` (ör. `Balance.Insufficient`, `Leave.Overlap`)
- Branch adları: `docs/git.md`

## Error handling
- Beklenen iş hataları (yetersiz bakiye, çakışma, yetkisiz geçiş vb.) **exception değil**, `Result<T>`
  döner; her hatanın sabit bir kodu vardır (yukarıdaki biçim). UI hata koduna göre Türkçe mesaj gösterir.
- Beklenmeyen hatalar exception olarak kalır; global handler loglar, kullanıcıya genel Türkçe mesaj +
  korelasyon ID gösterir.
- Kullanıcıya asla sızmaz: stack trace, SQL, iç tip adları, kişisel veri.

## Data rules
- İzin tarihleri `DateOnly` (saat ve saat dilimi yok).
- Zaman damgaları UTC, `DateTimeOffset`; gösterimde `Europe/Istanbul`. Şimdiki zaman yalnızca `TimeProvider` ile (FD-5).
- Gün sayıları `int` (yarım gün yok). Yarım gün gelirse ADR ile `decimal`'e geçilir.
- ID'ler `Guid` (v7, `Guid.CreateVersion7()`); Entra kullanıcı eşlemesi ayrı alanda (`EntraObjectId`).
- Fiziksel silme yok: iptal / ters düzeltme (denetim izi, `docs/domain.md`).

## Enforced by tooling
- Derleyici: nullable, tüm uyarılar hata (`TreatWarningsAsErrors`)
- .NET analyzer'ları (`AnalysisLevel=latest-recommended`) + `.editorconfig`
- Biçim: `dotnet format --verify-no-changes` (check adımı `format`)
- Mimari: FD-1 ile FD-6, `PolsanLeave.ArchitectureTests` (check adımı `test-unit`)
- Zafiyetli paket: `dotnet list package --vulnerable` (check adımı `vulnerable`)
