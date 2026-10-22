# Spec 0001 — Yıllık izin talebi oluşturma

- Status: In progress
- Mode: lite
- Plan: `specs/plans/0001-plan.md`

## Intent
Polsan çalışanları bugün yıllık izin taleplerini sistem dışında iletiyor; kalan izin günlerini bilmiyor
ve talepte düşecek günü elle hesaplıyor. Bu özellikle çalışan şirket hesabıyla (Entra ID) giriş yapar,
güncel yıllık izin bakiyesini görür ve bir tarih aralığı seçerek yıllık izin talebi oluşturur; sistem
süreyi iş günü olarak hesaplar, bakiyeyi aşan veya başka bir talebiyle çakışan talebi baştan engeller.
Oluşan talep `Pending` durumunda yönetici kararını bekler. Başarı: çalışan, yasal kurallara göre
hesaplanmış bakiyesiyle, kimseye sormadan geçerli bir talep oluşturabiliyor. Bu özellikte yönetici
onayı/reddi, iptal, diğer izin türleri, İK ekranları ve bildirimler yoktur. Çalışan ana verisi, açılış
bakiyeleri ve resmî tatiller bu dilimde İK'nın sağladığı başlangıç verisinden gelir.

## Requirements
- R-1 **Giriş:** Uygulamaya yalnızca Entra ID ile giriş yapılır. Giriş yapmamış kullanıcı Entra girişine
  yönlendirilir. `Employee` rolü olmayan kullanıcı uygulamayı kullanamaz.
- R-2 **Çalışan profili:** Giriş yapan kullanıcı, Entra kimliğiyle eşleşen çalışan kaydına bağlanır.
  Çalışan kaydı (ad soyad, işe giriş tarihi, doğum tarihi, doğrudan yönetici) İK'nın sağladığı
  başlangıç verisinden gelir. Eşleşen kaydı olmayan kullanıcı talep oluşturamaz.
- R-3 **Bakiye:** Çalışanın yıllık izin bakiyesi şöyle oluşur:
  - Açılış bakiyesi: İK'nın başlangıç verisinde verdiği, gerekçesi "Açılış bakiyesi" olan bakiye
    düzeltmesi (sistemin açılış tarihine kadarki devreden hakkı kapsar). Sistemin açılış tarihi de
    başlangıç verisinin parçasıdır; uygulamada sabit değildir.
  - Hak ediş: sistemin açılış tarihi (dahil) ve sonrasındaki her işe giriş yıldönümünde, o yıldönümünde
    tamamlanan kıdem yılı ve o gündeki yaşa göre BR-1 hakkı eklenir (BR-2). İlk yıldönümünden önce hak yoktur.
  - Kalan = açılış bakiyesi + hak edişler + diğer düzeltmeler − onaylanmış (iptal edilmemiş) taleplerin iş günü toplamı.
  - Bekleyen = `Pending` taleplerin iş günü toplamı.
  - Kullanılabilir = Kalan − Bekleyen.
  - Kullanılmayan gün sonraki yıla sınırsız devreder (BR-9).
- R-4 **İş günü hesabı:** Talebin süresi, başlangıç ve bitiş dahil aralıktaki iş günü sayısıdır; Cumartesi,
  Pazar ve resmî tatiller sayılmaz (BR-3). Arife günleri iş günüdür. Resmî tatiller (2026 ve 2027)
  başlangıç verisinden gelir.
- R-5 **Talep oluşturma:** Çalışan başlangıç tarihi, bitiş tarihi ve isteğe bağlı açıklama (en fazla 500
  karakter) girerek yıllık izin talebi oluşturur. Talep `Pending` durumunda, çalışanın doğrudan
  yöneticisine atanmış olarak kaydedilir.
- R-6 **Doğrulama — talep şu durumlarda oluşturulmaz ve çalışana nedeni Türkçe gösterilir:**
  - başlangıç tarihi bugünden önce ("bugün" = `Europe/Istanbul` takvim günü);
  - bitiş tarihi başlangıç tarihinden önce;
  - başlangıç tarihi bugünden 365 günden daha ileri;
  - aralıkta hiç iş günü yok;
  - talep edilen iş günü sayısı kullanılabilir bakiyeden fazla (BR-4; bakiye kontrolü bugünkü bakiyeyle,
    gelecekteki hak edişler hesaba katılmaz);
  - aralık, çalışanın `Pending` veya `Approved` başka bir talebiyle en az bir gün çakışıyor (BR-5);
  - çalışanın tanımlı doğrudan yöneticisi yok;
  - açıklama 500 karakteri aşıyor;
  - aralıktaki herhangi bir günün yılı için tatil takvimi tanımlı değil
    ("Bu tarihler için tatil takvimi henüz tanımlı değil").
  Bu kurallar (özellikle BR-4 ve BR-5) aynı çalışanın eşzamanlı gönderimlerinde de korunur.
- R-7 **Taleplerim:** Çalışan kendi taleplerini (başlangıç, bitiş, iş günü sayısı, durum, oluşturulma
  zamanı) en yeni en üstte olacak şekilde görür. Başka çalışanların taleplerini göremez.
- R-8 **Önizleme:** Çalışan tarihleri seçtiğinde, göndermeden önce talebin kaç iş günü tutacağını ve
  talep sonrası kullanılabilir bakiyeyi görür.

## User experience
- Flow:
  1. Çalışan uygulamayı açar → Entra girişi → ana sayfa: bakiye özeti (Kalan · Bekleyen · Kullanılabilir)
     ve "Taleplerim" listesi, "Yeni izin talebi" butonu.
  2. "Yeni izin talebi" → form: başlangıç tarihi, bitiş tarihi, açıklama. Tarihler seçilince önizleme
     (iş günü sayısı, talep sonrası kullanılabilir bakiye) görünür.
  3. "Gönder" → doğrulama başarılıysa başarı mesajı ve ana sayfaya dönüş; yeni talep listede `Beklemede`
     olarak en üstte. Başarısızsa form, girilen değerler korunarak hata mesajıyla kalır.
- States:
  - Ana sayfa: yükleniyor · bakiye + liste (dolu) · liste boş ("Henüz izin talebiniz yok") ·
    profil tanımlı değil ("Profiliniz tanımlı değil, İK ile iletişime geçin"; talep butonu yok) ·
    beklenmeyen hata (genel mesaj + korelasyon ID).
  - Form: boş · önizleme · doğrulama hatası (R-6 nedeni) · gönderiliyor (buton pasif, çift gönderim yok) ·
    başarı · onaylayıcı tanımlı değil ("Onaylayıcı tanımlı değil, İK ile iletişime geçin"; gönderim kapalı).
  - Yetki yok: `Employee` rolü olmayan kullanıcı → "Bu uygulamayı kullanma yetkiniz yok" sayfası.
- Status labels (UI): `Pending` = Beklemede, `Approved` = Onaylandı, `Rejected` = Reddedildi, `Cancelled` = İptal edildi.
- Design reference: none — the flow above is the design.
- Accessibility: tüm form alanları etiketli; form klavyeyle doldurulup gönderilebilir; hata mesajı ilgili
  alanla ilişkilendirilmiş ve ekran okuyucuya duyurulur; renk tek başına durum göstermez (durum metni de var).

## Constraints & out of scope
- Constraints: 4857 sayılı İş Kanunu md. 53 (BR-1); KVKK — açıklama alanı kişisel veridir, loglanmaz.
  Tarihler `Europe/Istanbul` takvimine göre değerlendirilir.
- Out of scope (sonraki özellikler):
  - Yöneticinin talebi görmesi, onaylaması, reddetmesi
  - Talep iptali (BR-7) ve düzenleme
  - Yıllık izin dışındaki izin türleri
  - İK ekranları: çalışan kaydı, tatil takvimi, bakiye düzeltmesi
  - Entra/Graph ile çalışan verisi senkronizasyonu
  - E-posta/Teams bildirimleri
  - 2027 sonrası tatil verisi
- Note: Bu dilimde talebi `Approved` yapan bir davranış yoktur (onay spec 0002'de). `Approved` taleplere
  dayanan kriterler (AC-15, AC-29) hazır test verisiyle doğrulanır.

## Acceptance criteria
Giriş ve yetki
- [ ] AC-1 — Giriş yapmamış kullanıcı herhangi bir sayfaya gittiğinde Entra girişine yönlendirilir.
- [ ] AC-2 — `Employee` rolü olmayan giriş yapmış kullanıcı "yetkiniz yok" sayfasını görür ve talep oluşturamaz.
- [ ] AC-3 — Eşleşen çalışan kaydı olmayan kullanıcı ana sayfada "Profiliniz tanımlı değil" mesajını görür; talep oluşturamaz.
- [ ] AC-4 — Bir çalışan başka bir çalışanın talebini hiçbir yoldan (liste, doğrudan adres/ID) göremez.

Bakiye (BR-1, BR-2, BR-9)
- [ ] AC-5 — Açılış bakiyesi 10, açılıştan sonra yıldönümü olmamış, talebi olmayan çalışan: Kalan 10, Bekleyen 0, Kullanılabilir 10.
- [ ] AC-6 — Açılıştan sonraki yıldönümünde 1. kıdem yılını tamamlayan (yaş 30) çalışana 14 gün eklenir.
- [ ] AC-7 — Yıldönümünde tamamlanan kıdem 5 yıl (yaş 30) → 14 gün; 6 yıl → 20 gün.
- [ ] AC-8 — Yıldönümünde tamamlanan kıdem 14 yıl (yaş 40) → 20 gün; 15 yıl → 26 gün.
- [ ] AC-9 — Kıdem 3 yıl, yıldönümünde yaş 18 → 20 gün; yaş 19 → 14 gün.
- [ ] AC-10 — Kıdem 3 yıl, yıldönümünde yaş 50 → 20 gün; yaş 49 → 14 gün.
- [ ] AC-11 — Kıdem 20 yıl, yaş 55 → 26 gün (yaş alt sınırı daha yüksek hakkı düşürmez).
- [ ] AC-12 — İşe giriş tarihinden bir yıl dolmamış çalışanın hak edişi 0'dır (açılış bakiyesi yoksa Kullanılabilir 0).
- [ ] AC-13 — Açılış tarihinden önceki yıldönümleri için sistem hak ediş eklemez (yalnızca açılış bakiyesi sayılır).
- [ ] AC-14 — 29 Şubat'ta işe girmiş çalışanın yıldönümü artık olmayan yıllarda 28 Şubat'tır.
- [ ] AC-15 — Kalan bakiye, onaylanmış taleplerin iş günü toplamı kadar azalır; Kullanılabilir, bekleyen taleplerin iş günü toplamı kadar daha azdır.
- [ ] AC-16 — Önceki yıllardan kullanılmayan günler Kalan'da korunur (devir sınırsız).

İş günü hesabı (BR-3)
- [ ] AC-17 — Pazartesi–Cuma, tatilsiz bir hafta = 5 iş günü.
- [ ] AC-18 — Cuma–Pazartesi aralığı = 2 iş günü (hafta sonu sayılmaz).
- [ ] AC-19 — Resmî tatil içeren aralıkta tatil günü sayılmaz (ör. 29 Ekim 2026 Perşembe içeren 26–30 Ekim 2026 = 4 iş günü).
- [ ] AC-20 — Arife günü (ör. Kurban Bayramı arifesi) iş günü olarak sayılır.
- [ ] AC-21 — Başlangıç = bitiş = bir iş günü → 1 iş günü.

Talep oluşturma ve doğrulama (R-5, R-6)
- [ ] AC-22 — Geçerli bir talep `Pending` durumunda, çalışanın doğrudan yöneticisine atanmış olarak kaydedilir; çalışan başarı mesajı görür ve talep "Taleplerim"in en üstünde "Beklemede" olarak görünür.
- [ ] AC-23 — Başlangıç bugün olan talep kabul edilir; dün olan reddedilir.
- [ ] AC-24 — Bitiş başlangıçtan önceyse reddedilir.
- [ ] AC-25 — Başlangıç bugün + 365 gün ise kabul edilir; bugün + 366 gün ise reddedilir.
- [ ] AC-26 — Yalnızca hafta sonu ve/veya resmî tatil içeren aralık reddedilir.
- [ ] AC-27 — Talep edilen iş günü = Kullanılabilir bakiye ise kabul edilir; Kullanılabilir + 1 ise reddedilir.
- [ ] AC-28 — Bekleyen bir talep, yeni talebin bakiye kontrolünde düşülür (Kalan 10, Bekleyen 6 iken 5 günlük talep reddedilir).
- [ ] AC-29 — `Pending` veya `Approved` bir talebiyle tek gün bile çakışan talep reddedilir; `Rejected` veya `Cancelled` taleple çakışma engel değildir.
- [ ] AC-30 — Doğrudan yöneticisi tanımlı olmayan çalışan formda "Onaylayıcı tanımlı değil" mesajını görür; talep oluşturamaz.
- [ ] AC-31 — 500 karakterlik açıklama kabul edilir; 501 karakterlik reddedilir; boş açıklama kabul edilir.
- [ ] AC-32 — Reddedilen gönderimde form girilen değerleri korur ve nedeni ilgili alanın yanında Türkçe gösterir; hiçbir talep kaydı oluşmaz.
- [ ] AC-33 — "Gönder"e art arda iki kez basmak tek talep oluşturur.
- [ ] AC-40 — Tatil takvimi tanımlı olmayan bir yıla (ör. 2028) uzanan talep "Bu tarihler için tatil takvimi henüz tanımlı değil" nedeniyle reddedilir.
- [ ] AC-41 — Aynı çalışanın birbiriyle çakışan (veya birlikte kullanılabilir bakiyeyi aşan) iki talebi eşzamanlı gönderildiğinde en fazla biri kabul edilir.
- [ ] AC-34 — Bakiye değişikliği yalnızca talep veya gerekçeli düzeltme kaynaklıdır; her bakiye hareketinin kaynağı izlenebilir (denetim izi değişmezi).

Ekranlar ve durumlar
- [ ] AC-35 — Tarihler seçilince önizleme iş günü sayısını ve talep sonrası kullanılabilir bakiyeyi gösterir; aralık geçersizse (ör. bitiş başlangıçtan önce) önizleme yerine ilgili doğrulama mesajı görünür.
- [ ] AC-36 — Hiç talebi olmayan çalışan "Henüz izin talebiniz yok" mesajını görür.
- [ ] AC-37 — "Taleplerim" başlangıç, bitiş, iş günü, Türkçe durum etiketi ve oluşturulma zamanını gösterir; en yeni en üstte.
- [ ] AC-38 — Beklenmeyen bir hatada kullanıcı genel Türkçe mesaj ve korelasyon ID görür; stack trace veya teknik detay görmez.
- [ ] AC-39 — Form yalnızca klavyeyle doldurulup gönderilebilir; tüm alanların erişilebilir etiketi vardır; hata mesajı alana bağlıdır.

## Definition of Done
- [ ] Every acceptance criterion mapped to proof (test or reproducible observation)
- [ ] `scripts/check` green
- [ ] Independent review done; real findings fixed, noise rejected with written rationale
- [ ] Docs / ADRs updated if behavior or architecture changed
- [ ] Spec moved to `specs/done/` (it becomes immutable there)

## Scorecard (fill at ship — honest numbers make the process improvable)
| Metric | Value |
|---|---|
| Spec revisions | |
| Fix rounds | |
| Review findings: real / noise | |
| Regressions introduced | |
| Bugs escaped to production | |
