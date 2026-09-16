# 55 — Çeviri incelemesi: kadro ikiye ayrılmıştı, Çince ekran bozuktu

*16 Eylül 2026.* Kullanıcının isteği:

> *"Agentlar oluşturup çevirilerin doğru olup olmadığını kontrol et. Ayrıca mağaza
> sayfası metinlerini de diğer diller için oluştur. Proje klasör yapısını ona göre
> düzenli ve anlaşılır hale getir."*

Dört agent koştu — İspanyolca, Çince, Arapça ve **İngilizce** (varsayılan dil ve
öbür üçünün çevrildiği kaynak: oradaki bir hata dört kez kopyalanmış olur). Her
biri 635 anahtarı tek tek gezdi.

**Dört rapor da aynı yere işaret etti ve haklıydılar.** Bu belgenin konusu
bulduklarından çok, *nasıl* bulundukları: dördü de ölçülebilir bir iddia
üretti, ben de her birini koda ya da yazı tipine sordum.

---

## 1. Müdavim kadrosu ikiye ayrılmıştı

[54](54-five-languages.md)'te üç dil eklerken müdavimlerin meslek satırlarını ve
hikâye sahnelerini **çevirmemişim — yeniden uydurmuşum**. Yirmi müdavimin
on üçü es/zh/ar'da başka biri oldu:

| kişi | içerikteki arketip | tr/en | es/zh/ar (yanlış) |
|---|---|---|---|
| Rasim Amca | `insaat_iscisi` | şantiye ustabaşı | taksi şoförü |
| Tolga | `gece_vardiyasi` | gece vardiyası güvenlik | fitness eğitmeni |
| Sevda | `diyet_yapan` (sevdiği: yeşil salata) | diyetisyen | hemşire |
| Ozan | `mac_grubu` (4-6 kişi, akşam) | amatör futbolcu | stajyer |

Bu bir üslup farkı değil: `Simulation.BindRegularsToPlan()` müdavimi
`archetypeBase` ile gelen bir gruba bağlıyor. Ozan'ın Çince sahnesi *"öğle
başında ilk gelir, hep yalnız"* diyordu; oyun onu **akşam, dört-altı kişilik
bir grupla** getiriyor. Metin, oyuncunun gözünün önünde yalanlanıyordu.

Üç tablonun yirmişer müdavimi de tr/en kadrosuna göre yeniden yazıldı.

**Bunu yakalayacak bir denetim yok ve uydurmak da istemiyorum.** Anahtar
kümesi doğruydu, yer tutucular doğruydu, hiçbir sayı tutmuyordu ki tutmasın.
"Bu satır bu kişiyi mi anlatıyor" sorusunu ancak okuyan biri sorar — bu turda
agentlar sordu.

---

## 2. Çince yapı üç yerden bozuktu ve yazı tipi denetimi yeşildi

`check_font.py` *"hepsi kapsanıyor"* diyordu. Diyebiliyordu, çünkü yanlış soruyu
soruyordu: **"Çince tablosunda hangi karakter var"**. Oysa `UiRoot.FontForLanguage`
dil Çince'yken **bütün ağacı** Noto Sans SC ile çiziyor — yalnızca Çince metni
değil.

Ölçüldü, üçü de gerçekti:

1. **Personel isimleri.** `content/names.json` yerelleştirme tablosunda değil.
   Doksan altı ismin **on altısı** (Ayşe, İbrahim, Yağmur, Sıla…) alt kümede
   olmayan harfler taşıyor. Çince oynayan biri her altı personelden birini
   `Ay□e` diye görüyordu.
2. **Koda gömülü simgeler.** Akşam raporundaki eksi işareti `U+2212` ve menüdeki
   madde imi `U+2022` doğrudan C# içinde. Hiçbir tabloda yoklar, alt kümede de
   yoklardı — **her gider satırı** `□1.200 ¤` çıkıyordu.
3. **Dil seçicideki Arapça düğmesi.** Çince arayüzde `العربية` yedi boş kutu.
   Arapça okuyan biri kendi dilini bulamıyordu.

### Üçünün de çözümü aynı yerde değil

`ğ İ ı Ş ş` **kaynak fontta da yok** (ölçüldü: Noto Sans SC 30.890 kod noktası
taşıyor, Latin Extended-A yok). Yani alt kümeye eklenerek çözülemez. `Loc.PersonName`
yazıldı: dil Çince'yken bu beş harfi katlıyor — Çince içerik tablosunun özel
adlarda zaten uyguladığı kuralın aynısı.

`U+2212` ve `U+2022` alt kümeye **girdi**. Arapça giremez (font taşımıyor), o
yüzden düğmeye Rubik yerel olarak veriliyor — Çince düğmesindeki çözümün aynısı.

### Alt küme artık bir araç

`tools/art/subset_font.py` yazıldı. İlk alt küme elle çıkarılmıştı ve **hangi
karakterlerin istendiği hiçbir yerde yazmıyordu** — üç boşluğun sebebi buydu.
Karakter kümesi tek yerde (`check_font.cjk_characters()`) tanımlı: araç ondan
üretiyor, denetçi onu arıyor. İki liste olsaydı ayrışırlardı ve ayrışma yine
boş kutu demekti.

Denetim ilk koşusunda kırmızı yandı — Çince düzeltmelerimin getirdiği yeni bir
karakter (`齐`) alt kümede yoktu.

---

## 3. Simülasyonu yalanlayan yedi metin

Hepsi koda sorularak doğrulandı:

| anahtar | ne diyordu | kod ne yapıyor |
|---|---|---|
| `badge.short_peak` | "one short" (tam bir kişi eksik) | `_cooks < gereken \|\| _salon < gereken` — **herhangi** bir eksik |
| `ui.morning.days_keep` | "days left" (kalan stok) | `KeepDays` = **raf ömrü** |
| `ui.menu.subtitle` | "Everything on the menu is kept in stock" (güvence) | TR uyarı: geniş menü para bağlar |
| `ui.service.target_auto` | "worst" | `MostImpatientParty()` |
| `notice.plates_out_busy` | "the dishwasher" | İşe alınabilir bulaşıkçı **yok** |
| `ui.staff.fire_confirm` | "experience resets" | `Fire()` kaydı **siliyor** |
| `notice.tenure_zincir` | "ask **for** {0}" | TR "ona soruyor" = ona **danışıyorlar** |

`days_keep` en pahalısıydı: *"3 days left"* okuyan oyuncu "üç günlük stoğum var"
anlıyor ve **almıyor**; metin ise "üç gün dayanır" demek istiyor, yani *al*.
Soğuk hava deposunun satıldığı tek satır bu.

### Devralınan garson "devraldığın aşçı" yazıyordu

`TraitText` huyu **olmayan** kişiye `ui.staff.inherited` yazıyor. Devralınan
salon personelinin de huyu yok (`Simulation.cs:800`, `_salonTraitA[0] = -1`).
Yani garsonun kartında *"Huysuz — devraldığın aşçı"* yazıyordu, beş dilde
birden. Ayrı bir anahtar eklendi (`ui.staff.inherited_salon`).

### Müdahale ipucu yanlış kabloya bağlıymış

`Hints.cs` `InterventionsPerDay` (taban, 4) okuyordu; günlük hak
`InterventionsToday` = taban + masa artışı. İpucu 1. günde doğru, ilk
genişlemeden sonra oyuncuya hakkını **olduğundan az** söylüyordu. Metin
doğruydu, kablo yanlıştı.

---

## 4. Dile özgü olanlar

**İspanyolca — çekim yapan bir dil, İngilizce ve Türkçe yapmıyor.** İstasyon
adlarının yarısı dişil (`Parrilla`, `Bebidas`), personel isimlerinin yarısı
kadın. `{0} mejorado` "Parrilla **mejorado**" basıyordu. Yedi metin
uyum-gerektirmeyen kalıba çevrildi (`Mejora: {0} — nivel {1}`). Ayrıca
`Dejar ir` = *gitmesine izin ver*; kırmızı bir düğmede "eve gitsin" diye
okunabiliyordu → `Despedir`.

**Arapça — sayı-isim uyumu.** Arapça 1 / 2 / 3-10 / 11-99 / 100+ için ayrı
çekim istiyor, kod ise çıplak bir tamsayı koyuyor. **On altı metin** yalnızca
bir sayı bandında doğruydu: `{0} أيام` "1 أيام" basıyordu. Hepsi her sayı için
doğru olan bölük kalıbına çevrildi (`{0} من الأيام`). Ayrıca `{0} / {1}` gibi
arada yalnızca nötr karakter olan çiftler iki yönlü algoritmada aynalanıyor —
araya güçlü bir Arapça sözcük kondu.

**Çince — 厨房 mutfak *odası* demek, mutfak *türü* değil.** *"你先开哪种厨房？"*
("hangi mutfak odasını açıyorsun") sorusunun altında `快餐` ve `土耳其餐馆`
kartları duruyordu; soru kendi cevaplarına uymuyordu. Ayrıca `毛利` hem bir para
tutarını hem bir yüzdeyi etiketliyordu, ve defter ekranındaki iki **oran**
etiketi emir kipinde yazıldığı için yanlarındaki düğmenin kopyası gibi
görünüyordu.

---

## 5. Üreteç yer tutucuyu sayamıyordu

`compare()` yer tutucuları **küme** olarak karşılaştırıyordu, yani
`"{0} ... {0}"` ile `"{0}"` aynıydı. İngilizce kıdem bildirimi personelin adını
iki kez basıyordu ve kapı hiçbir şey demedi. Artık sayılı: mutasyonla iki yönde
de doğrulandı.

```
yer tutucu farkli: notice.tenure_zincir  tr={0} {1}  en={0}x2 {1}
```

---

## 6. Mağaza metinleri beş dilde

Kısa açıklama, tam açıklama ve gizlilik politikası İspanyolca, Çince ve Arapça
için yazıldı ([44](44-store-texts.md)). Karakter sayıları **elle
yazılmıştı ve ikisi yanlıştı** (60 diyordu, 61'di). `tools/content/check_store_texts.py`
artık ölçüyor: kısa açıklama ≤ 80, tam açıklama ≤ 4000, ve her dilin ikisi de
var mı.

| dil | kısa | tam |
|---|---:|---:|
| TR | 61 | 1.609 |
| EN | 68 | 1.707 |
| ES | 66 | 1.850 |
| ZH | 21 | 599 |
| AR | 57 | 1.446 |

---

## 7. Klasör düzeni

Beş dil, `tools/content/` içine dil başına iki dosya koydu — altı üretecin ve
iki denetçinin arasında on dosya. `tools/content/languages/` açıldı; ad kalıbı
(`loc_<dil>[_ui].py`) aynı kaldı.

Kök `README.md`'ye **"Depoda ne nerede"** haritası eklendi ve `docs/README.md`'ye
konuya göre bir giriş — elli iki numaralı belge kronolojik bir günlük ve o
sıra bozulmamalı, ama gezilebilir de değildi.

Üç denetçiyi (`audit_content.py`, `check_licenses.py`, `check_urp.py`) topic
klasörlerine taşımayı **denemedim**: on beş belge atıfını bayatlatırdı, kazancı
yoktu.

### Bunun yerine taşımayı güvenli yapan bir denetim

`tools/check_docs.py`: her göreli bağlantı var olan bir dosyayı gösteriyor
mu, ve her numaralı belge dizinde anılıyor mu. İlk koşusunda **dokuz kırık
bağlantı** buldu — biri [54](54-five-languages.md)'te benim yazdığım yanlış numaraydı,
kalan sekizi belgeler yeniden numaralandığında kırılmış ve aylardır öyle
duruyordu.

Ayrıca `docs/README.md` kendi kendisiyle çelişiyordu: alttaki kütük *"otuz yedi
kalemin hepsi kapandı"* derken tablonun on üç satırında hâlâ "Karar bekliyor"
yazıyordu.

---

## Kapanmayanlar

- **Raporların SHOULD FIX bölümleri kısmen uygulandı.** Anlamı değiştiren ve
  simülasyonu yalanlayan her madde girdi; üslup önerilerinin bir kısmı
  girmedi. Dördü de eksiksiz listeler bıraktı.
- **Hâlâ hiçbir dili anadili konuşuru okumadı.** Agent incelemesi bunun yerini
  tutmuyor — ama boş bir sayfadan çok daha iyi bir başlangıç noktası bıraktı.
- **`role.bulasikci`, `role.kasiyer` ve `ui.hud.angry` ölü anahtarlar**: beş
  tabloda bakımı yapılıyor, hiçbir yerde çizilmiyorlar. `loc_tarama.py`
  ölü `ui.*` anahtarlarını arıyor ama `role.*` ailesine bakmıyor.
