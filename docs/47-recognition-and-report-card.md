# 47 — Tanıma ve haftalık karne

*14 Eylül 2026.* Soru şuydu: *"oyun içi görev başarma duygusunu oyuncuya
hissettirmek için günlük görevler mi olsa?"*

Teşhis doğruydu, ilaç değil. Bu belge ikisini de anlatıyor.

---

## 1. Gerçek boşluk: puan var, görünürlük yok

Oyun yedi eksende puan veriyor ([08](08-endgame.md)) ve oyuncu onları **tam
bir kez** görüyordu — altmışıncı günde. Göremediğin bir şeyde ilerleme
hissedemezsin, ve geç öğrenilen bir ölçüte göre oynanamaz.

Yani sorun "başarı hissi yok" değil, **"başarı ölçülüyor ama gösterilmiyor"**.

---

## 2. Günlük görev neden yanlış ilaçtı

**Kurguyu bozuyor.** Oyunun tek cümlelik vaadi "Patronsun, aşçı değil"
([44](44-store-texts.md) mağaza metninin ilk satırı). Patrona her sabah
görev listesi veren kim? Bir görev panosu oyuncuyu görünmez bir patronun
çalışanı yapıyor — oyunun olmamaya söz verdiği tek şey.

**Ödül doymuş eksene düşüyor.** Bu projenin yasası: *doymuş bir eksene ödenen
ödül görünmez.* Klasik günlük görev ödülü paradır ve harness'a göre iyi
oyuncu altmışıncı günü ~21.000 kasayla bitiriyor — ödül tam da hissedileceği
anda hissedilmiyor.

**İyi oynamayı cezalandırabiliyor.** "Bugün 40 kişi ağırla" görevi, oyuncunun
bilerek bir kişi eksik çalışıp para biriktirdiği güne denk gelirse onun
**stratejisini başarısızlığa çeviriyor.**

Bir de günlük görevler bir **tutundurma** aracı: reklamı ve oyun içi satışı
olan oyunların problemi. Burada ikisi de yok.

---

## 3. Yapılan: atama değil tanıma

Ayrım tek cümlede: *görev* "yarın şunu yap" der, *nişan* "bugün şunu
başardın" der. İkincisi geriye dönük olduğu için oyuncunun planıyla **asla
çatışmıyor**.

### A. Haftalık karne

`Score()` zaten mevcut durumun **saf bir fonksiyonu** — kampanyanın herhangi
bir gününde çalışıyor. Yani karne için yeni bir hesap yazmak gerekmedi; iki
ayrı hesap bir gün birbirinden ayrılır ve hangisinin doğru olduğu anlaşılmaz.

Yedinci günün kapanışında yedi eksen fotoğraflanıyor ve akşam raporunda
**geçen haftaya göre farkıyla** gösteriliyor. 60 gün ≈ 9 hafta: tek başarı anı
**dokuza** çıkıyor, ve oyuncu neyle ölçüldüğünü iş işten geçmeden öğreniyor.

Haftalık, günlük değil: eksenler bir günde kıpırdamıyor ve oyunun kendi ritmi
zaten haftalık (ücret ve kira haftalık ödeniyor, zirve haftada iki gün).

**Sıfırıncı haftanın fotoğrafı kurucuda çekiliyor.** Bu satır olmasaydı geçen
hafta sıfır sayılır ve oyuncu yedinci günde "Mekân +33" gibi, **kendisinin
yapmadığı** bir sıçrama görürdü — devraldığı dükkânın puanı onun kazancı
değil. Test bunu tutuyor (`Ilk_karnenin_farki_devralinani_saymiyor`).

### B. Yedi nişan

| nişan | koşul |
|---|---|
| Kimse aç dönmedi | zirve günü, sıfır geri çevrilen **ve** sıfır kızgın |
| Zirveyi eksik kadroyla geçtin | zirve, kadro gerekenin altında, kızgın yok |
| Defter kapandı | veresiye **verildi** ve tamamı tahsil edildi |
| Seni tanıdılar | bir müdavimin ilk hikâye sahnesi |
| Kasada on bin | — |
| Dükkânı büyüttün | ilk kademe genişlemesi |
| Semtin konuştuğu | itibar 90 |

Hepsi simülasyonun zaten bildiği şeylerden; tek yeni takip "defter bir kez
açıldı mı" bayrağı. Ödül **para değil**: görülmek.

Kazanılmamış nişanlar da [`BadgeScreen`](../unity/Assets/Lokanta/Game/Ui/BadgeScreen.cs)'de
adıyla duruyor — oyuncunun **kendi** hedefini seçebilmesi için neyin mümkün
olduğunu görmesi gerekiyor. Ama görev listesi değil: hiçbiri "bugün şunu yap"
demiyor ve hiçbirinin süresi yok.

---

## 4. Ölçüm üç şeyi düzeltti

### Birim hatası: nişan birinci günde dağıtılıyordu

`CashMilestone = 10000` yazmıştım. `_cash` **santi-sikke** ve başlangıç kasası
800.000 santi (8.000 sikke) — yani eşik 100 sikkeydi ve nişan **birinci günün
sonunda** veriliyordu. Hiçbir şey kırılmıyordu; yalnızca "ilk on bin" diye bir
tanıma, hiç kazanılmadan dağıtılıyordu.

Bu tam da nişanların tehlikeli tarafı: **kırılmaları değil, hak edilmeden
verilmeleri.** Ölçüm olmasaydı görünmezdi.

### Dağılım: başarı eğrisi birinci haftada ölüyordu

İlk beş nişanla makul oyuncu 5., 6. ve 8. günlerde üç tanıma alıyor, sonra
**elli iki gün sessizlik** vardı. "Dükkânı büyüttün" ve "Semtin konuştuğu"
bunun için eklendi — ikincisi geç geliyor çünkü itibar masa kademesinin
tavanına kırpılıyor (`reputationCapCenti` 5500/7500/9000/10000), yani **90'ı
görmek önce genişlemeyi gerektiriyor.**

### Ölçen oyuncu da ölçülmeli

İlk kalibrasyonda hiçbir nişan kazanılmadı ve bu, nişanların ulaşılamaz
olduğunu **göstermiyordu** — ölçen botun kötü oynadığını gösteriyordu. Pasif
bot zirveyi tek aşçı tek garsonla karşılıyor; elbette kimse mutlu ayrılmıyor.
Stok ve kadro bilen bir oyuncuya geçince üç nişan ilk sekiz günde geldi.

*Bir ölçüm aracının kendi yeteneği, ölçtüğü şeyin bir özelliği gibi
görünüyor.*

### Turun oyuncusu: 7'de 6

Son doğrulama turu (Türk mutfağı, 60 gün, gerçek Windows yapısı) kampanyada
**yedi nişandan altısını** kazandı. Yani set ne ulaşılamaz ne de bedava:
iyi oynayan bir kampanya çoğunu topluyor, kalan bir tanesi ise ayrı bir
**karar** istiyor — turun botu her sabah gereken kadroyu kuruyor, dolayısıyla
"zirveyi eksik kadroyla geçmek" onun hiç oynamadığı oyun.

Test botunun (yalnızca stok + kadro bilen) 5-6-8. günlerde üç nişan alması ile
turun botunun altı nişan alması arasındaki fark, tanımanın **oynayışa göre
değiştiğini** gösteriyor. İstenen buydu.

---

## 5. Üçüncü kez ateşlenen tuzak — artık kapandı

Metin ailesi beyaz listesi **iki dilde** duruyordu: `gen_loc.py:SCREEN_KEY`
(Python) ve `LocTests.cs` (C#). `LocTests.cs`'in kendi yorumu bunu zaten
yazıyordu:

> AYNI LİSTE gen_loc.py:SCREEN_KEY içinde de duruyor ve ikisi AYRIŞABİLİR —
> nitekim ayrıştı.

Üçüncü kez ayrıştı, bu sefer bana: `badge.` ailesini üretece ekledim, teste
eklemedim.

Artık `gen_loc.py` **`LocTests.cs`'i okuyup** iki listeyi karşılaştırıyor ve
ayrışırlarsa üretimi reddediyor. Mutasyonla iki yönde de doğrulandı:

```
uretecten silindi: cikis 1  ->  gen_loc.py'de YOK : badge.
testten silindi  : cikis 1  ->  LocTests.cs'de YOK : badge.
```

Yorumun "birini değiştiren ötekini de değiştirmeli" uyarısı artık bir dilek
değil, bir kontrol.

Tarama **yalnızca ilgili bloğa** bakıyor: ilk hâli bütün dosyayı tarıyordu ve
testin başka bir yerindeki `EndsWith("Key")` de aile sanıldı — kontrol kendi
kendini kırmızı yaktı.

---

## 6. Kapsam

- Kayıt sürümü 20 → **21** (`badges`, `badgesToday`, `creditEverOpened`,
  `weekAxis`, `weekAxisPrev`, `weekReportDay`).
- `Theme.AxisRow` **tek uygulama**: yıl sonu karnesi ve haftalık karne aynı
  satır biçimini kullanıyor, yani oyuncu altmışıncı günde yeni bir tablo
  öğrenmiyor. İkinci bir kopya, bu projede defalarca olan şeye davetiye
  olurdu.
- 6 yeni test (233 → **239**).
- Tur kontrolleri akşam raporunun içine kondu — kontrolün koşabileceği **tek
  an** orası, çünkü "bugün kazanıldı" işareti `AdvanceToNextDay`'de siliniyor.
  Hiç ölçülemezse `OLCULEMEDI` diyor: koşmayan bir kontrol, geçen bir
  kontrolle dışarıdan aynı görünüyor.
