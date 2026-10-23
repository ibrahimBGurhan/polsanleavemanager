# Spec 0002 — Sıfır test korumasının otomatik testi (mini)

- Status: Shipped
- Mode: lite
- Plan: `specs/plans/0002-plan.md`
- Source: Spec 0001 ship sonrası not — review (plan 0001 D11, I-2) "no self-test covers the guard"; `/change` talebi ve triage onayı İbrahim Gürhan, 2026-10-23
- Design: — (kullanıcı arayüzü yok)
- Supersedes: —
- Revisions: 2026-10-23 — R-08: PB-1 revize edildi (araç testi için yeni `[fast]` adım; plan 0002 R-10 çatışması; onay: İbrahim Gürhan)

## Intent
`scripts/check`'in test adımlarındaki sıfır test koruması, hiç test çalışmadığında "yeşil" sonucu engeller. Bu
koruma CI'da ANSI renk kodlu çıktıyı tanımadığı için 94 test geçerken kırmızıya düştü (plan 0001, D11); hata ilk
kez CI'da görüldü çünkü korumanın kendisini sınayan bir test yoktu. Bu değişiklikle koruma, renkli ve renksiz
çıktıda doğru karar verdiğini kanıtlayan otomatik bir testle korunur; test hem yerelde hem CI'da çalışır.

## Changed behavior
- [x] CB-1 — Korumanın gördüğü test özeti renk kodlu ve en az bir test çalıştığını söylüyorsa (ör. `ESC[m  total: 94`) adım geçer; otomatik test bunu doğrular.
- [x] CB-2 — Özet renk kodlu ve sıfır test söylüyorsa (ör. `ESC[m  total: 0`) adım "zero tests ran" ile kırmızıya düşer; otomatik test bunu doğrular.
- [x] CB-3 — Renk kodu olmayan özetlerde aynı iki karar geçerlidir (`  total: 94` geçer, `  total: 0` kırmızı); otomatik test bunu doğrular.
- [x] CB-4 — Test, projenin üç test adımının (`test-unit`, `test-integration`, `test-e2e`) gerçekte kullandığı koruma mantığını sınar; korumanın bir kopyasını değil. Adımlardan birinin koruması bozulursa (ör. renk kodu temizliği kaldırılırsa) test kırmızıya döner.
- [x] CB-5 — Test, CI'da her PR'da ve yerelde tek bir komutla çalışır; test makinesinde .NET, veritabanı veya tarayıcı gerektirmez.

## Preserved behavior
- [x] PB-1 — `./scripts/check` ve `--fast` profillerinin mevcut altı adımı, sırası ve çıktı biçimi korunur (yeşil adım son 8 satır, kırmızı adım tüm çıktı — plan 0001 D12); araç testi için yalnızca tek bir yeni `[fast]` adım eklenir (`full, 7 steps` / `fast, 4 steps`).
- [x] PB-2 — Gerçek bir test çalıştırmasında sıfır test koruması bugünkü kararları verir: 94/40/13 testli çalıştırma yeşil, çıkış kodu sıfırdan farklı test çalıştırması kırmızı.
- [x] PB-3 — `./scripts/selftest`'in mevcut 82 senaryosu geçmeye devam eder; selftest şablonda ve bu projede aynı davranma özelliğini kaybetmez.
- [x] PB-4 — `./scripts/doctor --strict` temiz kalır.

## Out of scope
- Korumanın davranışını değiştirmek (ör. ANSI dışı imleç kodlarını da temizlemek — review D11 I-1, yalnızca CI'da görülürse).
- `scripts/check`'in kendisinde (ANEW çekirdeği) değişiklik.
- Spec 0001'in davranışları ve testleri.

## Definition of Done
- [x] `scripts/check` green
- [x] Independent review done; real findings fixed, noise rejected with written rationale
- [x] Criterion ↔ evidence table complete for CB-* **and** PB-*
- [x] Spec moved to `specs/done/` (immutable there)
