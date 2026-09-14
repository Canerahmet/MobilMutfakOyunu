# 41 — Arayüzün yeniden tasarımı ve mekânın kimliği

*12 Eylül 2026.* Kullanıcı üç referans görsel getirdi ve isteği kademeli büyüdü:

> **"Oyun arayüzünü baştan tasarlamanı istiyorum, referanstaki görsel gibi bir
> arayüz olsun."**
>
> **"Farklı mutfaklar için şöyle bir referans tasarım var."** (dört mutfak yan
> yana: burger, Türk, İtalyan, Japon)
>
> **"Sadece buton ve şeyler değil; tüm restoran, karakterler, mutfak, mobilyalar,
> modeller, ışıklar — kısacası her şey referanstaki gibi olsun. Gerekirse sıfırdan
> modelleri vs. her şeyi yap. Oyun arka plan mantığı kalsın."**

Son cümle işin sınırını da çiziyor: **çekirdek simülasyon dokunulmaz, görünüm
katmanı yeniden kurulabilir.** Bu belge o çalışmanın birinci ve ikinci dilimini
yazıyor.

---

## 1. Referans ne söylüyor

Dört kare yan yana konunca ortak olan ve değişen şeyler net ayrılıyor:

| değişmeyen | değişen |
|---|---|
| kamera açısı, arayüz yerleşimi, renk rolleri | duvarın rengi ve malzemesi |
| rozet + çubuk, kapsüller, dört düğme, yeşil eylem | tabelanın ışığı |
| kartların yeri ve biçimi | yerdeki halı, saksılar, tabela rengi |

Yani **arayüz sabit, mekân kimlik taşıyor.** [docs/10](10-mutfak-kimligi.md)
bunu zaten kural olarak yazmıştı: *mutfaklar ortam, ışık, siluet ve kıyafetle
ayrışır.* Referans o kuralın resmi.

---

## 2. Arayüz: şerit değil kart

Eski oyun ekranı iki **tam genişlikte koyu şeritti** ve içlerinde eş değer gri
düğmeler yan yana duruyordu. Ölçüldü: sabah 179 dp, servis 152 dp, akşam 199 dp.

Yeni düzen referansın düzeni — köşelerde yüzen kartlar ve kapsüller:

```
[12] Servis ▓▓▓▓░░░   Kiraya 6 gün        (🪙 8.000)(💎 30,0/55)  (⚙)
┌ Bugün ────────┐                                    ┌ Ciro       ┐
│ ● Ağırlanan 12│                                    │ 1.006 ¤    │
│ ● Kızgın     0│                                    ├ Memnuniyet ┤
│ ● Masa    2/4 │                                    │ 94,7       │
└───────────────┘                                    └────────────┘
[⏸][×1] [ Mutfağı hızlandır | Çay | İlgi ●●●● ]        [▶▶ Günü kapat]
```

**Yeniden ölçüldü (13 Eylül 2026, iki dilde de aynı): sabah 154 dp, servis
154 dp, akşam 172 dp.**

| aşama | eski | yeni | fark |
|---|---:|---:|---:|
| sabah | 179 | 154 | salona **+25 dp** |
| servis | 152 | 154 | **−2 dp** |
| akşam | 199 | 172 | salona **+27 dp** |

Yani kazanç sabah ve akşamda; **serviste iki dp kaybediliyor** ve ekran o
aşamada daha çok şey söylüyor. Bu belge bir süre "üç aşamada da 156 dp" ve
"salona 23–43 dp daha fazla yer" yazdı; ikisi de ölçümün gerisinde kalmıştı ve
servis satırı **zaten o zaman da bir kayıptı** (152 → 156). Kazanan üç aşamayı
tek cümlede toplamak, kaybeden aşamayı ortalamanın içinde gizliyordu.

### Renk artık üç rol

Eskiden tek vurgu rengi (bakır) sekiz ayrı iş yapıyordu. Referansın ayrımı
alındı:

| renk | anlamı | nerede |
|---|---|---|
| **mavi** | "buraya gir" | Hal, Menü, Personel, Ekipman, Gün raporu |
| **yeşil** | "oyunu ilerlet" | Servisi aç, Günü kapat, Ertesi gün |
| **bakır** | "dikkat / değer" | kalan müdahale hakkı, itibar, gün çubuğu |

### Uydurulmayan şeyler

Referansta seviye çubuğu, elmas ve "Bölüm 3" var; bu oyunda yok. Onların yerine
oyunun **kendi** sayıları kondu ve hepsi zaten vardı, yalnızca görünmüyordu:

| referans | Lokanta'daki karşılığı |
|---|---|
| seviye rozeti + XP çubuğu | **gün numarası + servis gününün ilerlemesi** |
| altın + yeşil "+" | kasa + **kredi ekranını açan** düğme |
| elmas | itibar (tavanıyla birlikte: 30,0 / 55) |
| "Günlük Hedefler" listesi | sabah **açılış kontrol listesi**, serviste **bugünün akışı** |
| "Saatlik Gelir / Müşteri Memnuniyeti" | **ciro ve ortalama memnuniyet** — ikisi de yalnızca akşam raporunda görünüyordu |
| "Bölüm 3 → yeni müşteriler" | aşama düğmesi + alt satırı ("1. gün başlıyor") |

**Servis gününün ilerlemesi** en dikkat çekici olanı: bu sayı oyunda vardı
(`ServiceProgressBp` — gölgelerin yönü, ışığın rengi ve sokak lambaları ondan
okunuyor) ama oyuncuya hiç gösterilmiyordu. "Ne kadar kaldı" sorusunun cevabı
yalnızca gökyüzünün rengindeydi.

### Simgeler çiziliyor

Hiçbir simge yazı tipinden gelmiyor. Bu bir tercih değil, bir ders: yıldız
karakteri Rubik'te yok ve oyuncuya **boş kutu** olarak görünüyordu
(`tools/art/check_font.py` yakaladı). Hepsi dikdörtgen, daire ve döndürmeden
kuruluyor (`Icons.cs`). UI Toolkit'in kendi çizim API'si (Painter2D) de
kullanılmıyor — bu projenin bütün "editörde çalıştı, yapıda çıkmadı"
hikâyeleri yapıya dolaylı giren bir şeye güvenmekten çıktı.

### Turun bulduğu iki şey

1. **Tur "Servisi aç"ı bulamadı.** Yeni düğmelerin `text` alanı boş; yazı
   içerideki bir etikette. Tur metne göre tıklıyordu, bulamayınca **sessizce
   devam etti** — servis hiç açılmadı ve sonraki bütün kontroller sabah ekranını
   ölçmeye başladı. Düzeltme: tur da, kırpılma ölçümü de artık düğmenin
   **görünen** yazısına bakıyor. *Oyuncu düğmenin alanına değil yazısına bakar.*
2. **"Duraklat oyunu durdurdu" kırmızıydı** ve sebebi oyun değil aramaydı:
   duraklat artık simgeli. Kip düğmeleri adlarıyla bulunuyor (`ClickNamed`).

---

## 3. Mekân: arka perde, sarkıt, tabela, halı

Görünüm katmanına prosedürel bir model kurucusu eklendi (`Modeler.cs`): kutu ve
çok kenarlı prizma, **renge göre** tek örgüye toplanıyor. Bütün süsleme, kaç
parçadan oluşursa oluşsun bir avuç çizim.

| parça | ne yapıyor |
|---|---|
| **arka perde** | 2,60 m yüksek dolu duvar + iki yan dönüş. Oda duvarları 1,15 m ve saydam (salonun içi görünmeli); arkası ise boştu, mekân "kat planı" gibi okunuyordu |
| **tabela** | kapının üstünde, **neon çerçeve** + amblem |
| **menü tahtası** | salonun arka duvarında, satırları açık şeritlerle |
| **davlumbaz** | ocak sırasının üstünde; her mutfakta paslanmaz |
| **saksılar** | cephe boyunca, kapının önü boş |
| **halı** | yalnızca Türk mutfağında |

### Sarkıtlar denendi ve geri alındı

Kullanıcı [docs/38](38-sokak-ve-ic-isik.md)'de *"lambalar fiziksel olarak
gözükmesin, tavanda olacakları için"* demişti ve iç aydınlatma yalnızca yerdeki
ışık havuzlarıyla yapılmıştı. Referansta ise sarkıt lambalar mekânın en belirgin
öğesi; bu belgenin ilk yazımında o karar geri alındı ve sarkıtlar eklendi.

**Sonra tekrar kaldırıldılar** (aşağıda, yedinci dilim) ve docs/38'in kararı
geçerli kaldı: referansın kamerası daha alçak, orada sarkıt mekânın yarısı;
bizimki 34 dereceden ve tavansız bir binaya bakıyor. Sarkıtlar aydınlattıkları
yeri kapatıyordu.

Yani referansta olan her şey bizim kameramızda işe yaramıyor — **açı, listeden
önce gelir.** Kodda `Pendants()` diye boş gövdeli bir metot da kalmadı;
gerekçe `BuildDecor`'un başında duruyor.

### Tabela neden yazısız

Dünyada metin çizmek için ayrı bir paket gerekiyor (TextMeshPro) ve bu projede
yok. Tabela bunun yerine **renk ve ışıkla** konuşuyor. İlk denemede levhanın
bütün yüzü yanıyordu ve ekranda *"ışıklı sarı bir kalas"* olarak okunuyordu —
tabela değil lamba. Gerçek tabelalarda yanan şey yazı ve **çerçeve**; neon
çerçeve aynı işi görüyor.

### Lamba ışığı tabeladan ayrı

İlk denemede sarkıtların ağzı da tabela rengiyle yanıyordu ve hızlı yemek salonu
**pembe** bir ışıkla doluyordu. Tabela kimliğin rengi (neon kırmızı), lamba ise
her lokantada aynı şey: sıcak beyaz.

### İkinci dilim: zemin, banko, raf, sedir, korkuluk, kıyafet

| parça | ne yapıyor |
|---|---|
| **zemin deseni** | Türk'te uzun ahşap tahta, hızlı yemekte kare fayans (dama). Tek düz renk 34 derecelik bakışta **boş** bir alan olarak okunuyordu; desen ayrıca ölçek veriyor |
| **servis bankosu** | mutfağın önünde sıcak teşhir tezgâhı: paslanmaz tabla, sıralı kaplar, cam siper. Referansın en karakteristik mutfak parçası |
| **duvar rafları** | mutfak ve depoda, üzerinde kaplar — "çalışılan bir yer" |
| **sedir** | salonların arka duvarı boyunca bank |
| **teras korkuluğu** | cephe ile kaldırım arasında; kapının önü açık |
| **kıyafet** | aşçıya beyaz kep + beyaz önlük, garsona koyu önlük |

**Kıyafet iki kez sessizce hiçbir şey yapmadı** ve ikisi de öğretici:

1. Kep, kemiğe göre **sabit sayılarla** kondu (0,105 m yukarı, 0,17 m geniş) ve
   ekranda hiçbir şey görünmedi: kep başın **içinde** kaldı. Sebep kemik
   zincirinin kendi ölçeği (baş kemiğinin `lossyScale`'i 1,19) ve baş kemiğinin
   başın ortasında değil boynunda durması. Artık ölçü **figürden** okunuyor:
   derinin dünya kutusunun tepesi ve figürün boyu.
2. İkinci yazım `SkinnedMeshRenderer` arıyordu; paketin karakteri **iki ayrı
   parçadan** kuruluyor (`body-mesh`, `head-mesh`). Bileşen bulunamayınca metot
   başta dönüyordu — kıyafet yok, hata da yok. Şimdi bütün çizicilerin kutusu
   birleştiriliyor.

İkisi de aynı sınıf: *bir şeyin görünmemesi, hata vermemesiyle aynı anda olabilir.*
Teşhisi yapan şey log değil **render** oldu.

### Üçüncü dilim: vitrin kasası ve teras oturması

**Cephe artık bir dükkân cephesi:** alt bordür, dikmeler ve ince bir kiriş.
Cam eklenmedi (saydam malzeme yapıda opak çizilebiliyor, [docs/37]); eklenen şey
**kasa** — cam, aradaki boşluk. İlk denemede kiriş 2,05 m'deydi ve görüntüde
salonun ön sırasının önünden geçen **koyu bir bant** olarak çıktı: oyuncunun
masaları gördüğü yeri kapatıyordu. Oda duvarları zaten 1,15 m; cephe de o hattın
üzerine çıkmamalı. Kiriş 1,34 m'ye indi.

**Terasta iki masa** var ve **boşlar** — simülasyon dışarıda servis yapmıyor.
Boş bir teras masası yine de "burası bir lokanta" diyor; dolusu yalan söylerdi.
Masalar `StreetObstacles`'a kaydedildi: kaldırım aynı zamanda yayaların yürüdüğü
yer ve kaydedilmeseydi geçenler masanın içinden geçerdi. *Bir şeyi sahneye
koymak, onu yolun bir parçası yapmak demek.*

### Dördüncü dilim: mobilya da kimlik taşıyor

Paketin mobilya malzemelerinin **dokusu yok** — hepsi düz bir `_BaseColor`
(`Mobilya_wood`, `Mobilya_carpet`, …). Yani mutfağa göre renklendirmek için
dokuyla uğraşmak gerekmiyor: malzemenin bir **kopyası** çıkarılıp rengi paletten
yazılıyor.

**Kopya malzeme başına bir tane.** Alternatifi her çiziciye property block
yazmaktı ve o, çizicileri SRP toplu çiziminin **dışına** atıyor (bu proje bunu
zemin levhalarında öğrendi): yüz parça mobilya, yüz ayrı çizim demekti. Tek
kopya, aynı malzemeyi paylaşan bütün mobilyayı birden boyuyor.

Sonuç: hızlı yemekte **kırmızı sandalyeler**, Türk tarafında koyu bordo minderler
ve sıcak ahşap.

**Metal boyanmıyor.** İlk denemede paletin metali (Türk'te pirinç) bütün metal
parçalara gitti ve mutfak **altın** oldu: lavabo, tezgâh, buzdolabı. Davlumbazda
öğrenilen şey burada da geçerli — ekipman her lokantada paslanmazdır; pirinç bir
süsleme rengi (korkuluk, fener) ve orada kalıyor.

### Beşinci dilim: masada yemek

Referansın dört karesinde de masaların üzerinde tabak ve yemek var; bizim
masalarımız **servis edilirken bile boştu** — oyuncunun "şu masa yiyor" bilgisini
alabileceği tek yer rozetti.

Koşul **çekirdekten**: yalnızca yemeği gelmiş masada (`Eating` / `WaitingToPay`)
tabak var. Her masaya tabak koymak daha kolay olurdu ve yalan olurdu — bekleyen
masa ile yiyen masa ekranda aynı görünürdü. Tabaklar havuzlu (bir kez kurulup
açılıp kapanıyor) ve yemek paketin malzeme modellerinden geliyor, yani kendi
malzemeleriyle toplu çizime giriyorlar.

**Tabaklar ilk denemede masanın 2,4 m üstünde durdu.** Tabla yüksekliğini
taşıyıcının bütün çizicilerinden ölçüyordum; taşıyıcının içinde sandalyeler
(0,9 m) ve **rozet** (2,45 m) de var. Ölçüm artık masa kurulurken, sandalyeler
eklenmeden önce yapılıyor — o anda taşıyıcının içinde yalnızca masa var.

Tur artık ikisini birden soruyor: *"yiyen masa varsa ekranda tabak görülüyor mu"*
— "çekirdek yiyor" ile "oyuncu görüyor" ayrı iddialar (bulaşıkta öğrenilen ders).

### Altıncı dilim: okunabilirlik

Sahne süslendikçe iki şey birikti ve ikisi de **görüntüyü soldurdu**:

1. **Duvarların sütlü beyazı.** Saydam duvarlar 0,16 alfayla bütün salonun
   üzerine bir pus bindiriyordu; altındaki renkler (halı, tahta, kırmızı
   sandalye) soluyordu. Oda ayrımı zaten zeminden ve eşyadan okunuyor —
   duvarın işi **sınırı çizmek**, alanı boyamak değil. 0,10'a indi.
   *Not: `RestaurantView`'de bir `WallColor` alanı vardı ve hiçbir yerden
   okunmuyordu; gerçek renk `ozel_duvar.mat` varlığında, çünkü saydam malzeme
   bir `.mat` varlığı olmak zorunda ([docs/37]). Ölü alan silindi — ama
   **özeti silinmemişti**: sahipsiz kalan yorum bir süre daha orada durdu ve
   üstelik daha da eski bir değeri (0,20) anlatıyordu. Bir alanı silerken onu
   anlatan yorumu bırakmak, yorumu **belge**den **efsane**ye çeviriyor.*
2. **Zemin rengi paletten kopuktu.** Oda tabanı sabit üç renkti (salon / mutfak /
   servis) ve desenin arasından görünüyordu: hızlı yemekte sıcak kahve bir taban,
   Türk'te soğuk gri bir taban. Artık paletten geliyor; servis odaları yine de
   ayrılıyor (mutfak ve bulaşık daha soğuk, depo daha koyu) çünkü oyuncunun
   "burası arka taraf" ayrımını yapabilmesi gerekiyor.

### Yedinci dilim: gölgeler ve tavan lambaları

> **"Dükkân içi ışık ve gölgelerde problem var, odalardaki gölgeler başka
> odalara kayıyor. Başka problemler de var, düzelt. Tavandaki ışıklar
> gözükmesin, onların gözükmesine gerek yok."**

**Gölge taşmasının iki sebebi vardı ve ikisi de ölçülebilir:**

1. **Saydam duvarlar gölge düşürüyordu.** Duvarlar cam (alfa 0,10) ama gölge
   haritasında katı: 1,15 m'lik bir levha, güneş 10 derecedeyken **6,5 m**
   uzunluğunda koyu bir bant bırakıyor ve o bant komşu odanın yarısını
   kaplıyordu. Yanlışlığı iki katlı — cam bir bölme zaten gölge düşürmez, ve
   düşürdüğü gölge oyuncunun bakması gereken yere düşüyordu.
2. **Güneşin eğimi çok alçaktı.** Eğim sabah 26, akşam 10 dereceydi; gölge boyu
   `h / tan(açı)`, yani 1,8 m'lik bir buzdolabı akşam **10 metre** gölge
   bırakıyor. Eğim artık 44–66 derece bandında: en uzun gölge ~1,9 m, odaların
   en darı 3,2 m. **Günün saati kaybolmuyor** — yön (azimut) 148'den 268'e
   dönmeye devam ediyor ve saati asıl o söylüyor; boy değil *yön* okunuyor.
   Akşam gölge gücü de 0,35'e iniyor: geceyi aydınlatan şey yönlü güneş değil,
   iç ışıklar.

**Tavan lambaları kaldırıldı.** Bu parça bir kez vardı, referans görselleri
üzerine eklenmişti; kullanıcı iki kez aynı şeyi söyledi. Referansın kamerası
daha alçak ve orada sarkıt mekânın yarısı; bizimki 34 dereceden ve **tavansız**
bir binaya bakıyor — sarkıtlar aydınlattıkları yeri kapatan nesnelere
dönüyordu. Işık duruyor: yerdeki havuzlar ve akşamın sıcak dolgusu.

**"Başka problemler" iki tane çıktı, ikisi de yakın planda:**

- **Servis bankosu paket tezgâhlarıyla aynı yerde duruyordu** — tezgâh kutuları
  bankonun içinden çıkıyor, kaplar havada asılı duruyordu. Ön sıra tezgâhları
  kaldırıldı; banko zaten daha iyi bir tezgâh (tabla, sıralı kaplar, cam siper)
  ve aşçının çalışma noktaları ocaklardan türüyor.
- **Zeminde düzensiz koyu lekeler**: desen levhalarının alt yüzü zemin
  levhasının üst yüzüyle **aynı düzlemdeydi** (ikisi de y=0) — z-kavgası.
  Uzaktan "gölge kırıntısı" gibi okunuyordu ve beni gölge ayarlarında
  arattıracaktı. Desen 6 mm yukarı alındı.

---

## 4. Şimdiye kadarki kimlik farkı

| | hızlı yemek | Türk |
|---|---|---|
| duvar | kömür grisi | sıcak tuğla |
| vurgu | neon kırmızı | bakır |
| tabela | kırmızı neon | amber |
| zemin | halı yok | kilim (bordo + bakır bordür) |
| metal | çelik | pirinç |
| zemin deseni | kare fayans (dama) | uzun ahşap tahta |
| servis kapları | kırmızı | amber |
| sandalye minderi | kırmızı | koyu bordo |
| cephe dikmesi | kırmızı | bakır |

---

## 5. Kamera açısı: referansın üç çeyreği ve bedeli

> **"Kamera açısı da referanstaki gibi olsa daha iyi değil mi? Referans
> noktasından çok uzaktayız şu an."**

Doğru. Referansın dört karesinde de bina **üç çeyrek** duruyor — iki yüzü birden
görünüyor. Bizimki tam karşıdan bakıyordu (`CameraFit.Yaw = 0`) ve bu **bilerek**
seçilmişti: [docs/31](31-oda-ve-kamera.md) −12 derecelik dönmenin dokunma hedefi
tabanını 71 dp'den 48'e düşürdüğünü ölçmüştü.

O yüzden önce **ölçüldü** (`RoomLayout.Capture`, TABAN = en küçük açık odanın
şeritli kısa kenarı):

| dönme | TABAN 20:9 | TABAN 16:9 |
|---|---|---|
| 0° | 53 dp | 66 dp |
| 10° | 45 dp | 57 dp |
| 18° | 41 dp | 51 dp |
| 30° | 39 dp | 49 dp |

**Ve bir de telefonda bakıldı.** 20 derece görüntü aracında (1280×560) harika
duruyordu; gerçek yapıda, 873×393 ve şeritler yerindeyken **bina küçüldü** —
döndürülmüş bir dikdörtgen ekranda daha geniş yer istiyor, sığdırma da kamerayı
geri çekiyor. Yani dönme tek başına referansa yaklaştırmıyor, *uzaklaştırıyor*.
Aracın karesi ile telefonun karesi aynı soruya iki ayrı cevap veriyor; karar
telefonunki.

**10 derece seçildi.** Referansın üç çeyrek görünüşü geliyor, kayıp en küçük
adımda duruyor. 48 dp'nin altına inmesi bilinen bir bedel — ve tabanın altında
kalan şey bir düğme değil, beş metrelik bir odanın ekrandaki **kısa** kenarı;
uzun kenarı iki katından fazla ve oyuncu iki parmakla yaklaşabiliyor.

**13 Eylül'de yeniden ölçüldü ve sayı 45 değil 42–43 dp çıktı** (20:9; 16:9'da
52–54). Oyun değişmedi — *ölçüm* düzeldi: araç şeridin aldığı yeri %40
sayıyordu, gerçek en kötü aşama %43,8. Kabul edilen bedel aynı yerde duruyor,
ama artık doğru sayıyla duruyor. `RoomLayout` bunu her koşuda **uyarı** olarak
basıyor (48 dp sektör tabanı) ve **40 dp'nin altına düşerse kırmızı** yanıyor:
kabul edilmiş bir bedeli her koşuda hata diye bildirmek, gerçek bir gerilemeyi
gürültünün içinde kaybetmek olurdu.

Ölçüm aracının kendisi de düzeltildi: şerit oranları `0,13 / 0,17` (toplam %30)
yazılıydı ve "üst sınır" olduğu söyleniyordu. Tur şeridi **gerçekten** ölçüyor
ve o gün 156 dp / 393 dp = **%40** çıktı. Yani sayı üst sınır değil alt sınırdı
— ölçüm, hedefin gerçekte olduğundan büyük olduğunu söylüyordu.

**Bu kopya bir daha sessizce eskidi.** 13 Eylül ölçümünde en kötü aşama (akşam)
**172 dp**, yani %43,8; araç hâlâ %40'a göre hesaplıyordu ve dokunma hedefini
yine olduğundan büyük bildiriyordu. Bant 172 dp'ye kuruldu ve `RoomLayout`
artık kopyasını turun ölçtüğü değerle karşılaştırıp altında kalırsa **kırmızı**
yanıyor. Aynı sayının iki yerde durması kaçınılmazdı (araç editör kipinde
arayüzü kuramıyor); kaçınılabilir olan, ikisinin ayrıştığının hiç
görülmemesiydi.

### Ve o "asıl çözüm" yanlış çıktı

Burada bir cümle yazmıştım: *"referansın binası kare, bizimki uzun bir şerit; kat
planı derinleşirse hem dönme hem boyut geri gelir."* **Ölçüm bunu çürüttü.**

Arayüzsüz bir kare (873×393) çekilince görüldü ki bina ekranı **zaten
dolduruyor**: genişlik sınırda, derinlikte az bir pay var. 34 derecelik eğim
derinliği `sin(34°) = 0,56` ile sıkıştırıyor, yani 18 × 9,6 m'lik arsa ekranda
**3,3:1** oranında duruyor; şeritler arasındaki bant ise 3,7:1. Arsa kare olsaydı
bina **küçülürdü**, büyümezdi.

Gerçek darboğaz arayüzün kendisiydi: şeritler 393 dp'nin 156'sını alıyordu (%40).
Simge düğmeleri 62 → 54 dp, kapsül 42 → 38 dp indi; menü düğmesi dokunma
tabanında (48 dp) kaldı.

*Bir tahmini belgeye yazmak onu doğru yapmıyor — bu satırlar, planı yeniden
çizmeye başlamadan önce bir kare almanın karşılığı.*

---

## 6. Sırada ne var

Bu belge birinci ve ikinci dilimi kapsıyor. Referansa yaklaşmak için kalanlar:

1. **Mobilya modelleri** — paket modelleri duruyor ve artık **mutfağın tonunu
   alıyor**; prosedürel olarak yeniden yazmak (ahşap üst + koyu iskelet) hâlâ
   açık ama kazancı azaldı
2. ~~Karakterler~~ ✅ aşçı kepi ve önlük; müşteri çeşitliliği sırada
2b. ~~Masada yemek~~ ✅
3. ~~Teras~~ ✅ korkuluk ve saksılar; dış oturma sırada
4. ~~Servis bankosu~~ ✅
5. ~~Zemin dokusu~~ ✅
6. ~~Kat planının derinleşmesi~~ ❌ **ölçüm gereksiz olduğunu gösterdi** (§5)
