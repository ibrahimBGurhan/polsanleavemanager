# ADR 0001 — Modüler monolit

- Status: Accepted
- Date: 2026-10-21

## Context
Polsan Leave Manager tek şirket, yüzlerce kullanıcılık bir iç uygulamadır; küçük bir ekip geliştirir.
Alanın en riskli kısmı (yasal yıllık izin hesabı, bakiye denetim izi) iş kurallarındadır, ölçekte değil.

## Decision
Uygulamayı tek deploy edilen, tek veritabanlı bir modüler monolit olarak kuruyoruz: `Identity`, `Leave`,
`Balance`, `Calendar`, `Web`. Modüller yalnızca `Contracts` arayüzleri üzerinden doğrudan çağrıyla
konuşur; sınırlar mimari testleriyle zorlanır (`docs/architecture.md`, FD-1…FD-6).

## Consequences
- Kazanç: tek deploy, tek transaction sınırı, basit operasyon; sınırlar sayesinde ileride modül ayrılabilir.
- Maliyet: sınır disiplini yalnızca mimari testlerine bağlıdır; testler zayıflarsa monolit "büyük çamur topu"na döner.
- Modüller aynı veritabanını paylaşır (ayrı şemalar); şema sahipliği kural ile korunur, fiziksel olarak değil.

## Alternatives considered
- **Mikroservisler:** bu ölçekte yalnızca operasyonel yük (ağ, dağıtık transaction, çoklu deploy); kazanç yok.
- **Katmanlı klasik monolit (modülsüz):** başlangıçta daha hızlı, ama iş kuralları katmanlar arasına dağılır ve sınır test edilemez.

## Revisit triggers
- Bir modülün bağımsız ölçeklenmesi veya ayrı ekip tarafından sahiplenilmesi ihtiyacı.
- Modüller arası asenkron iş akışı ihtiyacı (bildirim, entegrasyon) — event'ler için yeni ADR.
