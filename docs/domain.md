# Domain

> Bootstrap'ta belirlenen ortak dil (2026-10-21). Bir terim burada yoksa, AI kendi anlamını uydurur.
> Kod tanımlayıcıları İngilizcedir (parantez içinde).

## Ubiquitous language

| Term | Meaning | Notes / not to be confused with |
|---|---|---|
| Çalışan (`Employee`) | Entra ID'den gelen kullanıcı; bir yöneticisi ve işe giriş tarihi vardır | Entra `oid` ile eşlenir; kendi parolası yoktur |
| Yönetici (`Manager`) | Çalışanın doğrudan amiri; doğrudan raporlarının taleplerini onaylar/reddeder | App role değildir — çalışan–yönetici ilişkisinden türetilir |
| İK (`HrAdmin`) | İzin türlerini, resmî tatilleri ve bakiye düzeltmelerini yöneten rol | Entra App Role |
| İzin türü (`LeaveType`) | Yıllık, mazeret, ücretsiz, hastalık (rapor) vb. Bakiyeden düşüp düşmediği türe bağlıdır | Hastalık izninde tanı bilgisi tutulmaz |
| İzin talebi (`LeaveRequest`) | Tarih aralığı + tür + açıklama. Durumlar: `Pending`, `Approved`, `Rejected`, `Cancelled` | "Aktif talep" = `Pending` veya `Approved` |
| İzin bakiyesi (`LeaveBalance`) | Bir çalışanın bir tür için hak edilen, kullanılan ve kalan gün sayısı | Her değişiklik bir talebe ya da düzeltmeye bağlıdır |
| Hak ediş (`Accrual`) | İşe giriş yıldönümünde yıllık izin hakkının bakiyeye eklenmesi | — |
| Bakiye düzeltmesi (`BalanceAdjustment`) | İK'nın gerekçeli manuel bakiye değişikliği | Silinmez; ters düzeltme ile geri alınır |
| İş günü (`WorkingDay`) | Cumartesi, Pazar ve resmî tatil olmayan gün | İzin süresi iş günü olarak sayılır |
| Kıdem (`Seniority`) | İşe giriş tarihinden itibaren tamamlanan tam yıl sayısı | Hak ediş hesabının girdisi |

## Business rules
- **BR-1:** Yıllık izin hakkı kıdeme göre: 1 yıl ile 5 yıl arası (5 dahil) 14 gün; 5 yıldan fazla,
  15 yıldan az 20 gün; 15 yıl ve üstü 26 gün. 18 yaş ve altı veya 50 yaş ve üstü çalışanlara
  en az 20 gün verilir; yaş, hak edişin eklendiği gün (işe giriş yıldönümü) hesaplanır.
  (4857 sayılı İş Kanunu md. 53)
- **BR-2:** Hak, işe giriş yıldönümünde eklenir; ilk yıl dolmadan yıllık izin hakkı yoktur.
- **BR-3:** İzin süresi iş günü olarak hesaplanır; Cumartesi, Pazar ve resmî tatiller düşülür.
  Yalnızca tam gün izin vardır.
- **BR-4:** Bakiyeden düşen bir türde, kalan bakiyeyi aşan talep oluşturulamaz (eksi bakiye/avans izin yok).
- **BR-5:** Aynı çalışanın tarih aralıkları çakışan iki aktif (`Pending`/`Approved`) talebi olamaz.
- **BR-6:** Talebi yalnızca çalışanın doğrudan yöneticisi onaylar/reddeder; kimse kendi talebini onaylayamaz.
- **BR-7:** Çalışan `Pending` talebini iptal edebilir; `Approved` talebi yalnızca izin başlangıç
  tarihinden önce iptal edebilir — bakiye iade edilir.
- **BR-8:** Bakiye düzeltmesini yalnızca İK yapar ve gerekçe zorunludur.
- **BR-9:** Kullanılmayan yıllık izin sonraki yıla sınırsız devreder (yıllık izin yanmaz).
- **BR-10:** Yanıtlanmayan talep `Pending` kalır; otomatik onay/red/eskalasyon yoktur.

## Key domain invariants
- Bakiye asla sessizce değişmez: her değişiklik bir talebe veya gerekçeli bir düzeltmeye bağlıdır (denetim izi).
- `Approved` ve `Rejected` talepler düzenlenemez; tek izin verilen geçiş BR-7'deki iptaldir.
- Fiziksel silme yoktur; iptal ve ters düzeltme kullanılır.
- Kalan bakiye = hak ediş toplamı + düzeltmeler − onaylı (iptal edilmemiş) taleplerin iş günü toplamı.
