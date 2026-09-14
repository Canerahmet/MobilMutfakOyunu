# 39 — Tabak döngüsü, bulaşık nöbeti ve devralınan kadro

*12 Eylül 2026.* Kullanıcının isteği:

> **Oyun ilk başladığında aşçı ve garson olsun. Bulaşıklar çok biriktiği zaman
> garson bulaşıkları yıkamaya geçsin. Bulaşıkçı alınca herkes kendi işini yapar.**
>
> **Bulaşıkçının orada boş ve kirli tabaklar biriksin, bulaşıkçı onları lavaboda
> eliyle yıkasın ve temiz tabakları diğer tarafa dizsin. Aşçı oradan tabağı alıp
> yemek koysun, garson ise yemek konulan tabağı servis etsin. Oyunda belirli
> sayıda tabak olsun ki gerçekçi olsun — yani temiz tabak bitince bulaşıkçının
> yıkaması beklensin. Ayrıca garson yemek yenilen tabakları alıp bulaşıkçının
> kirli tabak kısmına bıraksın.**

Bu bir icat değil: [docs/14](14-personel-sistemi.md) bulaşıkçıyı dört rolden biri
olarak tarif ediyor (ücret 90, kapasite 48, salon yükünün %28'i) ve gerekçesini
de yazmış — ***"tabak biterse servis durur. Görünmeyen ama tıkanınca fark edilen
bir darboğaz. Oyuncuya 'önemsiz görünen şeyi ihmal etme' dersi veriyor."***
Tasarlanmıştı, uygulanmamıştı: simülasyon dört rolü iki havuza indirgiyor ve
bulaşık salon kapasitesinin içine gömülüydü.

---

## 1. Devralınan kadro: bir aşçı, bir garson

Önceden salon **boştu** ve birinci gün bütün servisi patron tek başına yapıyordu.
İki sebeple yanlış: oyun "patronsun, şef değilsin" diyor ama açılışta oyuncunun
gördüğü şey tek kişilik bir dükkân — kendisi; ve garsonun ne yaptığını görmeden
"garson tuttum" kararının ne işe yaradığı anlaşılmıyor.

Aşçıda olduğu gibi: morali **başlangıç morali** (yoksa istifa eşiğinin altında
doğar ve ikinci gün salon boşalır), huyu **yok** (devralınan personel sıradan;
karakter *seçtiğin* kişilerle gelir), ama **adı var**.

| | önce | sonra |
|---|---|---|
| `makul` son kasa | 19.441 | 19.797 |
| itibar | 77,6 | 77,8 |
| ağırlanan | 2.018 | 2.033 |

Pasif ve bütün istismar botları hâlâ 56. günde iflas ediyor. **Yeniden
kalibrasyon gerekmedi.**

---

## 2. Tabak döngüsü

```
temiz  ──(aşçı tabaklıyor)──▶  kullanımda  ──(garson masayı topluyor)──▶  kirli
  ▲                                                                        │
  └──────────────────────(lavaboda yıkanıyor)──────────────────────────────┘
```

Kademeye göre **14 / 20 / 26 / 34** tabak, içerikten geliyor
(`tools/balance/export.py`). Temiz tabak bitince aşçı pişen yemeği çıkaramıyor
(`PlateUp`) ve mutfak bekliyor; bekleme **sayılıyor** (`PlateBlockedTicks`) —
"bulaşıkçı ihmal edildi" cümlesinin ölçüsü budur.

### Korunum bir değişmez

`temiz + kullanımda + kirli = toplam`, **her tick'te** sınanıyor
(`PlateTests.Tabak_sayisi_korunuyor`), gün sonunda değil: ara bir durumda
bozulup sonunda toparlanan bir sayaç, gün sonu kontrolünden geçerdi. Sızan bir
tabak, servisi gün gün yavaşlatan ve sebebi hiçbir yerde görünmeyen bir hata
olurdu.

### Tabak sayısı: üç kez ölçüldü, üçü de yanlış çıktı

| kat | tier 0 | sonuç |
|---|---|---|
| ×6 | 24 | 14 günde **0 tick** bekleme — darboğaz yok, mekanik dekoratif |
| ×4 | 16 | yine 0 tick |
| ×3 | 12 | **otomatik tur 1. günde lokantayı kilitledi** |
| **×2 + 6** | **14** | ölçülen basınç: en az temiz 1,3 / 34 |

Aradaki ders iki parçalı:

**Neden ×6 ve ×4 ısırmadı:** eşzamanlı kullanılan tabak sayısını **masa sayısı**
sınırlıyor (masa başına en fazla dört kişi), yani tabak `masa × 4`'ün üstündeyse
tanım gereği hiç bitmez. Denge aracı bunu gizledi çünkü botları hızla büyüyor ve
ortalamalar birinci kademeyi yutuyor.

**Neden ×3 kilitledi:** dört masaya on iki tabak; dört kişilik iki grup stoğu
tüketiyor, üçüncü grup masaya oturuyor, sipariş veriyor, mutfak tabak bulamıyor,
sabrı bitiyor, kızgın çıkıyor — ve gün kimse servis edilmeden kapanıyordu. **Bunu
tur yakaladı, denge aracı değil.**

### Ölüm sarmalını kıran satır

```csharp
if (_platesClean < _pSize[best]) return;   // SeatWaitingParties
```

Temiz tabak yoksa yeni grup **masaya oturtulmuyor**. Gerçek bir lokantada da
doğrusu bu: tabak yoksa oturtmazsın, oturtup aç bırakmazsın. Bekleyen grup
kapıda bekliyor, temiz tabak çıkınca giriyor — **basınç görünür, dükkân
kilitlenmiyor.** Bu satırla birlikte sayı 14'e indirilebildi ve darboğaz gerçek
oldu.

Katsayı küçük, tampon büyük (`masa × 2 + 6`) ve bu bilinçli: darboğaz **küçük**
dükkânda değil **büyük** dükkânda olmalı. Dört masalı bir lokantanın tabak krizi
yok; on dört masalının var, çünkü tabak masayla doğrusal büyürken servis hızı
masa × devir ile büyüyor.

---

## 3. Bulaşıkçı: ayrı bir havuz değil, lavaboya ayrılmış bir salon çalışanı

docs/14 salonu **"garson + bulaşıkçı + kasiyer, tek iş havuzu"** diye kuruyor ve
maaş o harmandan geliyor. Ayrı bir personel havuzu açmak, aynı kişiyi iki ücret
tablosunda saymak olurdu — üstelik kendi huy/moral/istifa/aday dizilerini,
kayıt alanlarını ve arayüzünü isterdi.

Oyuncunun kararı zaten aynı: **bir kişilik kadroyu lavaboya ayırıyor**
(`CommandKind.SetDishwashers`). Ayırmazsa bulaşık birikince garson kendiliğinden
lavaboya geçiyor ve servis aksıyor.

Salon kadrosunun tamamı lavaboya verilemiyor — o zaman kimse servis yapmaz ve
oyun kendi kendini kilitlerdi.

### Histerezis şart

Tek eşikle garson bir tabak yıkayıp servise dönüyor, bir sonraki karede geri
geliyordu: **"yıkıyor" değil "gidip geliyor"** diye okunuyordu. Başlama eşiği ile
bırakma eşiği ayrı.

### Neden yıkama *süresi* büyütülmedi

İlk düşünce "yıkama yavaş olsun ki yığın birikssin" idi ve **yanlıştı**:
`ClearMs` (masa toplama, 6.000 ms) toplam salon süresinin %31'i ve docs/14'te
bulaşıkçı payı %28 — yani masa toplama zaten o payı taşıyor. Süreyi büyütmek
aynı işi **iki kez** saymak olurdu. Basınç süreden değil **sayıdan** geliyor.

---

## 4. Ekranda ne görünüyor

Bulaşıkhane tek fonksiyondan kuruluyor (`BuildDishStation`), çünkü hepsi
birbirinin konumundan türüyor: yıkayan figür lavabonun önünde, **kirli yığın
solda, temiz yığın sağda**. Ayrı yerlerde hesaplamak aynı sayıyı iki yere yazmak
olurdu — bu projede beş kez sessizce ayrıştı.

**Tek lavabo, iki değil** — ve bunu *yerleşim denetimi* söyledi: iki lavabo + iki
tezgâh 0,16 m çakışıyordu. Sebep aritmetik: oda 3,2 m ve dört nesnenin her biri
~0,84 m, yani 3,36 m gerekiyor. Tezgâh + lavabo + tezgâh 2,52 m ve rahatça
sığıyor.

Yığınların yüksekliği tezgâhtan **ölçülerek** alınıyor (`TopOf`), yazılarak
değil: ilk yazımda 0,92 m tahmin edildi ve tabaklar havada asılı kaldı.

Garson lavaboya **elinde kirli tabaklarla** gidiyor ve varınca bırakıyor —
kullanıcının "garson yemek yenilen tabakları alıp bulaşıkçının kirli tabak
kısmına bıraksın" cümlesinin karşılığı.

Yığınlar **akışa göre** yerleşiyor: kirli sağda (salonlar x > 8,4), temiz solda
(mutfak x < 5,2). İlk yazım tersiydi ve her tabak odayı boşuna bir kez daha kat
ediyordu.

### Aşçı tabağı alıyor

`CookRoutine` sırasına iki aşama eklendi — **TabagaGit** ve **TabagiAl** — ve
ikisi de sıranın **en başında**, pişirmenin ortasında değil: sıcak tavayı bırakıp
odadan çıkan bir aşçı "tabağı alıyor" değil "bir yere gitti" diye okunur. Gerçek
bir mutfakta da tabak önceden hazırlanır.

Bu yol Mutfak–Bulaşık geçidinden geçiyor ve **o geçit kıl payı var**: ortak kenar
tam 1,40 m, eşik de tam 1,40 (`DoorWidth + MinJamb × 2`). Ölçüldü — geçit *var*
(bağ sayısı 5; olmasa 4 olurdu), çünkü `5.4f − 4.0f` kayan noktada 1,4000001 ve
eşik 1,4000000. Yani bu kenar güvenli bir paya değil, **son basamağa** dayanıyor.
Kat planı değişirse ilk bakılacak yer burası: mutfakla bulaşıkhanenin komşuluğu,
oyunun en çok kullanılan geçidi.

### Musluk akıyor, tabak süngerle ovuluyor, köpük var

Yıkayan figürün bir elinde **tabak**, ötekinde **sünger**; musluktan **su
akıyor** ve yalnızca biri yıkarken akıyor — sürekli akan bir musluk "yıkanıyor"
demiyor, "unutulmuş" diyor.

Su **malzeme varlığı** olmak zorunda (`ozel_su.mat`): URP saydam geçişin
gölgelendirici varyantını ona başvuran bir varlık yoksa yapıya koymuyor ve
çalışma anında kurulan saydam malzeme cihazda **opak** çiziliyor — editörde
hiçbir belirti vermeden. Bu proje o hatayı duvarlarda bir kez yaşadı.

Köpük teknenin içinde: üst üste binen, farklı boyda beş parça. İlk denemem eşit
boyda ve eşit aralıklı dört levhaydı ve köpük değil **fayans** gibi okunuyordu.

Bunu görebilmek için ayrı bir yakın plan gerekti (`bulasik_*.png`): editör
kipinde kimse yıkamıyor, personel boşta kuruluyor. `PreviewWash()` bir figürü
lavaboya koyup yıkatıyor. *"Su akıyor mu, sünger var mı"* sorusunun cevabı bir
sayıda değil görüntüde — ölçüm burada **bakmak**.

### Ama musluk oyunda hiç açılmıyordu

Kurulum doğruydu, davranış yanlıştı ve tanı satırı bunu üç koşu boyunca
söylüyordu: **`lavaboda görülen 0`**. Çekirdek tabakları yıkıyordu, ekranda
kimse lavaboya varmıyordu.

Sebep aşçıdakiyle aynı sınıf: çekirdeğin yıkama görevi **2.000 ms** ve oyun ×4'te
koşuyor — yarım saniye. Lavaboya yürüyüş birkaç saniye, yani figür **varmadan**
görev bitiyor, `SalonWashing` false oluyor ve görünüm onu yolun ortasında başka
yere yolluyor.

İlk düzeltmem de işe yaramadı ve sebebi öğreticiydi: tutma sayacını **varış geri
çağrısında** başlatmıştım — ama varış hiç gerçekleşmiyordu, yani o satır hiç
çalışmıyordu. Sayaç artık **yola çıkarken** başlıyor (güvenlik süresiyle;
tavansız bırakmak bir kere takılan figürü gün boyu lavaboya kilitlerdi), varınca
gerçek bekleme süresine iniyor.

Çekirdek "yıkandı" bilgisini veriyor; **süreyi görünüm anlatıyor** — `CookRoutine`
ile aynı kalıp.

### Ve ölçü değişti

`WashingCount`'u dar bir pencerede örneklemek neredeyse hep sıfır veriyordu.
Yeni ölçü birikmeli: `WashSeenFrames` — *bugün oyuncu lavaboda birini gördü mü*.
Turun yeni sorusu bu, ve düzeltmeden önce **0**, sonra **53 kare**.

Kayda değer olan: **simülasyonun bir şeyi yapması ile oyuncunun onu görmesi ayrı
iki iddia.** "Tabaklar yıkanıyor (6 adet)" kontrolü yeşildi ve doğruydu — ama
ekranda hiçbir şey olmuyordu.

### Darboğaz oyuncuya söyleniyor

Temiz tabak bitince mutfak duruyordu ama oyuncuya **hiçbir şey söylenmiyordu**:
servis sebepsiz yavaşlıyor ve bu, mekanik değil **hata** gibi okunur. docs/14
bulaşıkçıyı *"görünmeyen ama tıkanınca fark edilen"* bir darboğaz diye tarif
ediyor — "fark edilen" kısmı ancak söylenirse oluyor.

`SimEventKind.PlatesOut` eklendi:

> *Temiz tabak bitti — mutfak bekliyor. Lavaboda 6 kirli tabak var.*

Bulaşıkçı atanmışsa cümle değişiyor (*"bulaşıkçı yetişemiyor"*): ona "bulaşıkçı
tut" demek, olmayan bir düğmeyi aratmak olurdu. **Sebep ve çare aynı satırda.**

Her tick'te değil, **her yeni tıkanmada bir kez**: sürekli duyurmak bildirim
şeridini tek cümleyle doldururdu, susmak ise sorunun kendisiydi. Bayrak tabak
çıkınca düşüyor.

### Bulaşık nöbeti düğmesi turda koşuluyor

Yeni bir arayüz yolu ve bu projede sınanmayan her yol en az bir kez sessizce
bozuldu. Tur düğmeye basıyor ve **çekirdeğin sayısının** değiştiğini
doğruluyor — ekranın yazısına değil, durumun kendisine bakarak. Sonra geri
alıyor: tur günün kalanını *devraldığı* kadroyla oynamalı, kendi değiştirdiğiyle
değil, yoksa sonraki ölçümler başka bir oyunu ölçer.

---

## 5. Yürüyüş: ayaklar yerde kayıyordu

`Anim.speed` **hiçbir yerde ayarlanmıyordu**. Figür `Walker.Speed × oyun hızı`
ile yol alıyor ama yürüme klibi hep 1× oynuyordu:

| durum | yer hızı | klip temposu | sapma |
|---|---|---|---|
| ×1 | 1,15 m/sn | 1,28 m/sn | %11 |
| ×4 | 4,60 m/sn | 1,28 m/sn | **%260** |

Sokaktaki yayaların hızları ayrıca rastgele (0,92–1,34) ve onlar da
eşleşmiyordu.

### Klibin doğal hızı ölçüldü, tahmin edilmedi

Klip yerinde sayıyor (kök hareketi yok), yani "bu klip saniyede kaç metreye
karşılık geliyor" hiçbir yerde yazmıyor. `PlacementAudit.YuruyusHizi` onu
ölçüyor:

```
adım boyu = çevrim boyunca iki ayağın EN UZAK açılması
bir çevrim = iki adım
doğal hız  = 2 × adım / klip süresi
```

Ayaklar **kemikten değil mesh'ten** bulunuyor — bu iskelette ayak kemiği yok
(bacak başına tek kemik + sonradan eklenen diz). Her bacağın etkilediği köşeler
arasından en alçak olanı o bacağın ayağı sayılıyor.

Sonuç: **klip 0,67 sn, adım 0,426 m → 1,28 m/sn.** `Figure.WalkClipSpeed` bu
sayı; sapması varsa denetim *"SAPMA VAR"* diye yazıyor.

Artık `Walker` her karede `Body.SetGroundSpeed(Speed × çarpan)` çağırıyor ve
klip yer hızına göre dönüyor. Yürüyüş bitince (`Arrive`/`Stop`/`Warp`) tempo
normale dönüyor — oturma ya da doğrama klibinin hızı yer hızıyla ilgili değil.

**Turun yeni sorusu** `WalkSlipWorst`: yürüyen her figürde `Anim.speed ×
WalkClipSpeed` ile yer hızı arasındaki bağıl sapma. Gözle fark edilmesi zor bir
hata — ve tam o yüzden aylarca durdu. Ölçülen: **%0**.

---

## 6. Yeni ölçümler

**Çekirdek testleri (223):**
- `Tabak_sayisi_korunuyor` — her tick'te `temiz + kullanımda + kirli = toplam`
- `Bulasikci_mutfagin_tabak_beklemesini_dusuruyor` — kadro **eşit** tutuluyor,
  yoksa ölçülen şey bulaşıkçı değil fazladan bir kişi olur
- `Tabak_basinci_zirvede_olculuyor` — iddia etmiyor, sayıları basıyor

**Denge aracı** yeni bir tablo basıyor (`tabak basıncı`): tabaksız bekleme, en az
temiz, en çok kirli. Bir darboğazın "var olması" yetmez, bir yerde
**hissedilmesi** gerekir; hissedilmeyen mekanik dekorasyondur.

**Otomatik tur:** tabaklar kirleniyor / tabaklar yıkanıyor / tabak sayısı
korunuyor — üçü ayrı, çünkü üçü de ayrı şekilde sessizce bozulabilir.

### Kontroller iki kez yanlış yerdeydi

Bu üç kontrol **iki kez** yanlış yere kondu ve ikisini de tanı gösterdi.

İlk yazım bu üçünü **canlılık penceresine** koydu ve kontrol kararsız oldu:
pencerenin günün %9'unda açıldığı bir koşuda daha kimse yemeğini bitirmemiş,
yani hiçbir masa toplanmamış ve "tabaklar kirleniyor" kırmızıya düşüyordu.
Ölçülen şey döngünün işlemesi değil, **pencerenin ne zaman açıldığıydı**.

İkinci hata aynı ailedendi: sayaçlar **anlık**tı. "Şu an lavaboda kaç tabak var"
döngünün işleyip işlemediğini söylemiyor — gün başında sıfırdır. Sayaçlar
birikmeli oldu (`PlatesDirtiedToday`) ve kontroller **gün sonuna** taşındı.

Üçüncüsü de tanı satırının gösterdiği bir şeydi: salon dolma beklemesi bir
koşuda %43'te kendi tavanına çarpıyor ve pencereye yalnızca 1,4 saniye
kalıyordu. Beklemenin tavanı %45'ten **%25'e** indi — beklemenin kendisi,
ölçülecek günü tüketmemeli.

**Ve dördüncüsü:** blok *"gün sonunda soruluyor"* diye yazılıydı ama aslında
kamera bölümünden önce, **servis ortasında** duruyordu. Bir koşuda bütün ölçüm
bölümü günün ilk %12'sinde geçti ve sayaçlar yine sıfır çıktı. Yorumun doğru,
kodun yanlış olması — bu projenin en çok tuzağa düştüğü şekil. Artık gerçekten
`Günü kapat`'tan sonra.

### Ayak kayması ölçümü de bir kez yanlış okudu

`WalkSlipWorst` önce `Speed × GameSpeed`'i **yeniden hesaplıyordu** ve bir koşuda
%123 sapma bildirdi. Sokakta bir sorun yoktu: `GameSpeed`'i `GameApp.Update`
yazıyor ve kare içindeki sırası `Walker.Update`'e göre garanti değil, yani oyun
hızının değiştiği karede klip temposu **bir önceki hıza** göre ölçülüyordu.
Figür o karede doğru mesafeyi kat etmişti ve bir sonraki kare zaten
düzeltiyordu.

Artık `Walker.LastGroundSpeed` okunuyor — *o karede gerçekten kat edilen
mesafe*. Ölçülmesi gereken şey zaten oydu: "klip temposu, kat edilen mesafeye
uydu mu".

| | değer |
|---|---|
| `makul` son kasa | 19.071 (tabaktan önce 19.558, −%2,5) |
| `makul` en az temiz tabak | **1,3 / 34** |
| `planci` tabaksız bekleme | 52 tick |
| tur | **88/88**, üç ardışık koşuda |
| lavaboda yıkayan görüldü | **53 kare** (önce 0) |
| yürüyüşte ayak kayması | **%0** (önce ×4'te %260) |
| çekirdek testleri | 223 |
| yerleşim denetimi | 0 çakışma, 26/26 doğru yön |
