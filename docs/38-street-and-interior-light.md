# 38 — Sokağın düzeni, iç aydınlatma, kapıların kaldırılması

*12 Eylül 2026.* Kullanıcının dört isteği ve her birinin altından çıkan ölçüm.

> **Sokak ışıkları başta ortada ve sonda olsun. Şu an simetrik değil, aralarındaki
> mesafe o kötü görünüyor. Ayrıca karakterler sokakta yürürken birbirlerinin
> içinden geçiyor. Akşam olunca restoranın içi karanlık oluyor; iç ışıklandırma
> olsun fakat lambalar fiziksel olarak gözükmesin, tavanda olacakları için.
> Giriş ve mutfak kapısı dışındaki kapıları kaldıralım.**

---

## 1. Sokak lambaları: eşit aralık bir *ölçü*, bir izlenim değil

Eski kod dört direği arsaya eşit bölüyordu ve biri kapının önüne düştüğü için
**2,2 m kaydırılıyordu**. Aralıklar 2,3 / 4,5 / 4,5 m oluyordu. Kaydırma kodda
tek satırdı ve masumca duruyordu; bozduğu şey ancak ekranda görülüyordu.

Üç direk, eşit aralıklı. Ama ilk düzeltme de yanlıştı ve bunu **render gösterdi**:

| deneme | direkler | açılış kademesinde görünen |
|---|---|---|
| eski | 4,45 / 6,75 / 11,25 / 15,75 | dört direk, aralıklar 2,3 / 4,5 / 4,5 |
| düzeltme 1 | 0 / 9 / 18 (arsaya göre) | **iki** direk, sola yatık |
| **düzeltme 2** | **0 / *açık genişlik*/2 / *açık genişlik*** | üç direk, simetrik |

Kamera **arsayı değil açık odaları** çerçeveliyor (`CameraFit.OpenBounds`).
Açılışta oyuncu 0–13,4 m arasını görüyor, yani 18 m'deki direk ekranda yok.
Kâğıt üzerinde simetrik olan dizilim ekranda asimetrikti — tam düzeltilmek
istenen şikâyet.

Ölçülebilir hâli `RestaurantView.LampSpacingError`: komşu direkler arasındaki
mesafelerin en büyüğü eksi en küçüğü. Simetrik bir dizide **0,00**. Tur bunu
her koşuda soruyor, çünkü bu tam olarak "tek satırlık masum bir kaydırma"yla
tekrar bozulabilecek bir şey.

### 1b. Direk değil, fener *(12 Eylül akşamı eklendi)*

Kullanıcı bir referans görseli getirdi — sekizgen kaideli, inceleyerek yükselen,
kolu kıvrılan klasik sokak feneri — ve **"ışık konusunda geliştirme yapmamız
lazım, sokak için şu tarz bir model daha iyi olabilir"** dedi.

Eski lamba **üç kutuydu**: 0,09 m'lik bir direk, bir kol, ucunda 0,26 m'lik
beyaz bir küp. Telefon ölçüsünde bu "lamba" değil "ince bir çubuğun ucundaki
beyaz nokta" diye okunuyordu.

Yeni lamba, referansın parçalarıyla ve **tek örgüde**:

| parça | ölçü |
|---|---|
| taban levhası + konik kaide | r 0,185 → 0,088, y 0 – 0,36 |
| gövde (direk) | r 0,052 → 0,042, y 0,41 – 1,62 |
| iki bilezik | y 0,95 ve 1,585 |
| fener | **direğin tepesinde**: yatak + etek + cam + bilezik + konik kapak + tepe süsü, y 1,60 – 2,26 |

Onbeş ayrı kutu onbeş çizim çağrısı demekti; hepsi **iki örgüde** birleşiyor
(metal + cam) ve üç lamba aynı iki örgüyü paylaşıyor — lamba başına **2 çizim**.

İlk yazımda fener **kıvrık bir kolun ucundaydı** (referansın ortadaki modeli) ve
kaldırımın üstüne sarkıyordu. Kullanıcı tepeye istedi — referansın en sağdaki
modeli — ve oyunun kamerası bunu haklı çıkardı: kol +Z'ye, yani **kameraya
doğru** uzanıyordu ve sabit açıda tamamen kısalıyordu. Görünmeyen bir kol feneri
"direğe saplanmış" gibi gösteriyordu; tepedeki fener her açıdan fener.

Işık da lambayla birlikte taşındı (`LampHeadZ = 0`). Havuz 2,9 m derin ve direk
bordürde, yani kaldırımın yürüyüş şeridi (`Paths.PavementZ ± 0,35`) havuzun
**içinde** kalıyor — ışık yürünen yeri aydınlatmaya devam ediyor.

**Ölçek kâğıtta değil ekranda seçildi.** İlk ölçüde model doğruydu ama oyunun
gerçek çerçevesinde lamba 40 piksel ve fener onun beşte biri — sekiz piksellik
bir leke. Fener yarıçapları ×1,25 büyütüldü; karar *render'a bakarak* verildi.

**Fenerin altı ölçülüyor, yazılmıyor.** Fener kaldırımın tam üstünde ve altından
1,10 m'lik figürler geçiyor. Modeli büyüten biri feneri yürüyüş hizasına
indirebilir ve hiçbir şey uyarmaz — figür lambanın içinden geçer, oyun çalışmaya
devam eder. `LampLanternBottom` sayıyı **örgünün kendisinden** okuyor ve tur her koşuda
soruyor. Ölçünün tanımı da bir kez değişti: önce "z ekseninde fenere yakın
köşeler" diye seçiliyordu ve fener **kolun ucundayken** bu doğru bir ayrımdı;
fener direğe çıkınca aynı koşul bütün lambayı — kaide dahil — seçiyor ve ölçü
0,00 m dönüyor. Yerine **siluet**: direk yarıçapı 0,052, ondan geniş ve belden
yukarı olan her şey fener gövdesidir. *Bir ölçünün tanımı, ölçtüğü şeyin
geometrisi değişince onunla birlikte değişmek zorunda.*

### 1c. Gece ışığı: üç katman

URP'de ek ışıklar kapalı (`m_AdditionalLightsRenderingMode: 0`) ve **HDR de
kapalı** (`m_SupportsHDR: 0`) — yani ne nokta ışığı var ne de parlama (bloom).
1'in üzerindeki emisyon yalnızca beyaza kırpılıyor. Gece ışığı bu yüzden
**çizilen** bir şey:

| katman | ne yapıyor |
|---|---|
| **huzme** | fenerden yere inen koni. Işığın *kaynaktan çıktığı* ancak bununla görünüyor |
| **hale** | fenerin çevresindeki parlama — parlamanın (bloom) yerini tutuyor |
| **havuz** | kaldırımda yatan elips; sokak boyunca uzun, çünkü daire "yerde duran bir daire" diye okunuyordu |

Üçü de var olan `custom_lightpool` malzemesini kullanıyor: **saydam gölgelendirici
varyantı yapıya ancak bir `.mat` varlığı onu kullanıyorsa giriyor** (docs/37) —
yeni bir malzeme yazmak o tuzağa yeniden girmek olurdu. Huzme dokunun orta
satırını okuyor: fenerde parlak (u=0,62), yerde sönük (u=0,98).

İki hata ve ikisi de görüntüye bakınca çıktı:

1. **Hale hiç görünmüyordu.** Unity'nin Quad'ı **−Z'ye** bakıyor; kameranın
   açısını doğrudan vermek levhayı ters çeviriyor ve arka yüz ayıklandığı için
   levha hiç çizilmiyor. Hale oradaydı, sırtını dönmüştü.
2. **Huzme görünmüyordu.** Havuzla aynı malzeme ve aynı güç: yatan bir levha bir
   bakışta geniş, dik duran bir koni ise incecik. Huzmenin gücü ayrı yazıldı.

Sönük lambanın camı da değişti: kapalıyken koyu griydi (0,24), yani gündüz camla
demir aynı renkti ve fener "ucu kalınlaşmış bir direk" diye okunuyordu. Gerçek
bir fenerin camı gündüz de beyazdır.

---

## 2. Yayalar birbirinin içinden geçiyordu — iki katman, dört ölçüm

### Çözüm iki katmanlı

1. **Şerit.** Karşı yönde yürüyen iki figür aynı çizgide olursa karşılaşma
   kaçınılmaz. İki ayrı şerit (`Paths.PavementLane`) bunu **yapısal** olarak
   yok ediyor: sağa giden bordür tarafında, sola giden bina tarafında.
2. **İtişme.** Aynı şeritte birbirine yetişenler ve sohbet için duranlar için,
   her karenin **sonunda** (`LateUpdate`) gövdeleri ayıran küçük bir düzeltme.
   Yol bulma değil — yalnızca iki gövdenin üst üste binmesini engelliyor.

İkisi ayrı olmak zorunda: yalnızca itişme bırakılsa karşıdan gelen ikili
birbirini frenleyerek geçerdi (kaldırım tıkanır); yalnızca şerit bırakılsa
aynı yöndeki hızlı biri yavaş olanın içinden geçerdi.

İtişme **en az %62 yanal**. Sebebi: aynı şeritte arkadan yetişen biri için iki
gövdeyi ayıran doğru neredeyse tamamen yürüme eksenindedir; o yönde itmek öne
gideni hızlandırıp arkadakini yavaşlatır — ikisi hiç yan yana gelmez, yani
kimse kimseyi **geçemez**. Yana eğince arkadaki kayıp geçiyor.

### Şerit aralığı nereden geliyor: üç kez yanlış şey ölçüldü

Şerit aralığı bir **mesafe** istiyor ve o mesafeyi gözle seçmek, düzeltilen
hatanın ikinci kez yapılması olurdu. Ölçüm üç kez yanlış şeyi ölçtü:

| ölçüm | sonuç | neden yanlış |
|---|---|---|
| sınırlayıcı kutu | 0,60 × **1,14 m** | bir metre boyunda figür için olamayacak bir ayak izi — **kol açıklığı** |
| kalça bandı (%18–%50) | **1,08 m** | bu oranlarda **eller** de o hizada |
| omuz bandı | 0,58 m | doğru ama yetersiz: **baş** omuzdan geniş |
| **tüm profil** | **0,67 m** (baş) | kullanılan sayı |

İkisi de "makul" bir gerekçeyle seçilmiş bir banttı ve ikisi de kol ölçüyordu.
Doğru bandı bulmanın tek yolu **bütün profili basmak** oldu:

```
PROFIL y 0,00-0,10  en geniş çap 0,44 m   ayaklar
PROFIL y 0,10-0,20  en geniş çap 0,95 m   eller
PROFIL y 0,20-0,31  en geniş çap 1,08 m   eller (en geniş)
PROFIL y 0,31-0,41  en geniş çap 0,98 m
PROFIL y 0,41-0,51  en geniş çap 0,76 m
PROFIL y 0,51-0,61  en geniş çap 0,58 m   omuzlar
PROFIL y 0,61-0,72  en geniş çap 0,67 m   BAŞ  <- kullanılan
PROFIL y 0,72-0,82  en geniş çap 0,65 m
PROFIL y 0,82-0,92  en geniş çap 0,54 m
PROFIL y 0,92-1,02  en geniş çap 0,50 m
```

Bu paketin oranlarında kafa gövdenin üçte biri, yani **baş omuzlardan geniş**.
`PlacementAudit.Profil` artık kalıcı: bir sonraki sefer figür ölçeği ya da
paketi değişirse bakılacak yer bu satırlar.

**Kollar bilinçli olarak dışarıda.** 1,08 m arayla iki şerit, kaldırımı
restoranın yarısı kadar derin yapardı. İki gövdenin ayrı geçmesi yetiyor; bir
elin ötekinin elinin içinden geçmesi bu kamera mesafesinde birkaç piksel.

### Bedeli ölçüldü: çerçeveye giren sokak 1,10 → 1,94 m

Kaldırım 1,40 m olmak zorundaydı (0,70 şerit aralığı + gövdelerin yarıları).
İlk denemede kaldırım genişletildi ama **çerçeve büyütülmedi**: bordür ve
asfalt tamamen çerçevenin dışına düştü, sokak asfaltsız bir kaldırım şeridine
döndü. `CameraFit.StreetInFrame` de büyütüldü ve dokunma hedefi yeniden
ölçüldü — ayrıntı [docs/31](31-rooms-and-camera.md), **59 dp** (asgari 48).

Aynı ölçümde `docs/31`'deki **71 dp'nin bayat** olduğu görüldü: o sayı sokak
hiç yokken alınmıştı ve sokağın kendisi onu zaten ~64'e indirmişti. Kimse
yeniden ölçmemişti.

### Turun ilk koşusunda yakaladığı hata

Yeni kontrol ilk koşusunda kırmızı döndü: **"en kötü 4 çift iç içe"**.

Sebep şerit ya da itişme değildi. `Walker`, zaman çarpanı `TeleportAbove`'u
(4,5) geçtiğinde yürüyüşü çizmeyip yolun sonuna **ışınlanıyor**. Sokakta yolun
sonu yalnızca iki nokta — sağa gidenin çıkışı ve sola gidenin çıkışı — yani
yüksek hızda beş yaya o iki noktada üst üste yığılıyordu. Üç + iki, tam **dört
iç içe çift**.

Çözüm: sokak oyun saatini takip etmiyor. `Walker.SpeedCap` eklendi ve sokak
için 4'e ayarlandı (ışınlanma eşiğinin hemen altı). Gerekçesi mekanik değil
kurgusal: **sokaktan geçenlerin simülasyonda karşılığı yok**, "hızlı ileri
sardım" demek "kaldırım boşaldı" demek olmamalı.

Ayrıca yayalar artık lamba direklerine de çarpmıyor (`PostClear`, direk için
ayrı ve daha küçük bir eşik — dış şerit direğe 0,45 m uzakta ve eşik `Personal`
kadar olsa dış şeritteki her yaya sürekli içeri itilirdi).

### Sonraki üç kırmızı

Yığılma düzeltildikten sonra kontrol **kararsız** oldu: bir koşuda 0, bir
koşuda "en kötü 1 çift". Üç ayrı sebep çıktı.

**1. İtişme sabit hızlıydı.** Kaldırımı çapraz geçen bir *müşteri* bir yayayı
itiyor, o yaya bir başkasına giriyordu — zincir. Sabit hızlı bir düzeltme
zincirin sonuna bir kare geriden yetişiyor. Düzeltme **orantılı** oldu: iki
gövde ne kadar iç içe girmişse o kadar ayrılıyor (teğet geçerken düzeltme
sıfıra yakın, yani titreme yok), hız yalnızca tavan. Artı karede iki gevşeme
geçişi.

**2. Ölçüm hiç çizilmeyen bir kareyi ölçüyordu.** Unity'de `yield return null`
bir coroutine'i **Update ile LateUpdate arasında** uyandırıyor. İtişme
`LateUpdate`'te çalışıyor, yani tur konumları *Walker taşıdıktan sonra ama
itişme düzelttiğinden önce* okuyordu. Oyuncunun gördüğü karede iç içe geçme
yoktu; ölçülen kare ekrana hiç çıkmıyordu.

Sayı artık `LateUpdate`'in sonunda, düzeltme bittikten sonra yazılıp
saklanıyor — soran kim olursa olsun **çizilen karenin** sayısını alıyor.

Bu, bu projedeki ölçüm tuzaklarının tanıdık şekli: metrik doğruydu, *ne zaman*
okunduğu yanlıştı.

**3. Düzeltmelerin sırası yanlıştı.** Ölçüm anı düzeltilince kırmızı *devam
etti* — yani bu sefer çizilen karede gerçekten iç içe geçme vardı. Sebep:
her kişi için önce karşılıklı ayırma, sonra engel itmesi yapılıyordu. Bir
kişinin **son işlemi** "müşteriden/direkten uzaklaş" oluyordu ve o itme, onu
zaten ayrılmış olduğu komşusunun içine geri sokabiliyordu; kimse bir daha
bakmıyordu.

Doğru sıra: önce tek taraflı kısıtlar (engeller, şerit), **en son** karşılıklı
ayırma — ve o adım bir geçiş hiçbir çifti düzeltmeyene kadar yineleniyor.
Çizilen karede son sözü söyleyen şey, kontrolün ölçtüğü şey oldu.

Üçünün ortak dersi: *"düzeltme var"* ile *"düzeltilmiş hâli çiziliyor"* ayrı
iki iddia. İkincisini ölçmek gerekiyor.

### Kapıların kaldırılması turu bozdu — ölçümün kendisini

İç kapılar kalkınca (kanat 8 → 2) turun **canlılık penceresi** her koşuda
kırk saniyenin tamamını koşmaya başladı: `açılan kapı > 0` koşulu artık yalnızca
giriş ve mutfak kapısıyla sağlanabiliyor, eskiden sekiz kapıdan biri hep
birinin yakınındaydı. Pencere erken çıkamayınca arkasından gelen **ayrı bir 25
saniyelik masa beklemesi** servisin yoğun anını çoktan geçmiş oluyordu.

Tanı satırı eklenince sebep bir bakışta göründü:

```
TANI canlilik penceresi: 40,0 sn, servis 65% -> 100%
```

Pencere güne **%65 ilerlemiş** halde başlıyor ve **%100'e** dayanıyordu — yani
ondan sonraki her kontrol boş bir salonu ölçüyordu. İki ayrı sebep:

1. **×240 hız kapanmıyordu.** O hız yalnızca "salon dolsun" beklemesi içindi ama
   altındaki kamera bölümü iki kez `WaitForSeconds(MoveSeconds)` bekliyor ve
   ×240'ta bir gerçek saniye dört servis dakikası. Kamera kontrollerinin hızla
   hiçbir işi yok — hepsi arayüz ve kamera. Hız artık oradan itibaren ×4.
2. **Masa seçimi ayrı bir pencereydi.** Artık aynı pencerede, salon *doluyken*
   seçiliyor.

Ve pencereye ikinci bir tavan kondu: süre (40 sn) **ve gün** (%80). Süre tavanı
tek başına yetmiyordu çünkü o kırk saniyenin güne oranı koşudan koşuya
değişiyor. Gün tavanına takılırsa tanı bunu `(GUN TAVANI)` diye yazıyor.

Tanı bir sonraki turda **üçüncü** bir kaynağı gösterdi: aynı tur üç koşuda
pencereye **%15, %18 ve %50** ilerlemiş halde giriyordu. Fark, penceresinden
önceki "salon dolsun" beklemesinden geliyordu — o da ×240'ta koşuyor ve on iki
saniyesi servis gününün yarısı edebiliyor. Ona da gün tavanı kondu (%35).

Dördüncüsü daha dardı: seçilen masa pencerede **bir kez kilitleniyordu** ve
pencere birkaç saniye daha sürünce o grup kalkabiliyordu — pencere yeşil
çıkıyor, kullanıldığı yerde masa boş oluyordu. Artık her karede tazeleniyor ve
kullanılacağı anda yeniden okunuyor. *Tutulan şey ile kullanılan şey aynı anda
geçerli olmalı* — bu turun daha önce iki kez öğrendiği bir ders.

Kayda değer olan şu: bu, kullanıcının istediği bir değişikliğin **ölçüm
altyapısını** bozmasıydı ve kırmızı, bozulan yeri değil bambaşka bir yeri
gösteriyordu ("mutfakta iş yapılmıyor"). Tanı satırı olmadan burada mutfak
kodu aranırdı.

---

## 3. İç aydınlatma: lambalar görünmüyor, ışıkları görünüyor

Kullanıcının gerekçesi doğru: kamera **tavanı olmayan** bir binaya yukarıdan
bakıyor. Bir tavan armatürü çizilse kendi aydınlattığı yeri kapatırdı — ve
zaten orada bir tavan yok, yani armatür havada asılı dururdu.

**Yalnızca ışık havuzu.** Sokak lambalarındakiyle aynı teknik (ışıksız +
toplayıcı harmanlanan levha), çünkü URP varlığında ek ışıklar **kapalı**
(`m_AdditionalLightsRenderingMode: 0`, mobil bütçesi) ve sahneye konan bir spot
hiçbir şey yapmaz — uyarısız.

Oda ölçüsüne göre ızgara (2,90 m aralık hedefi, havuzlar birbirine %55 taşıyor).
Tek büyük bir havuz, dikdörtgen bir odada ortası parlak kenarı karanlık bir
leke veriyor — "tavan aydınlatması" değil "yerde bir fener".

Renk sokaktakinden **farklı**: sokak lambası sodyum sarısı, içerisi sıcak beyaz.
İkisi aynı renk olsa "içerisi" ile "dışarısı" aynı yerin devamı gibi okunurdu.

### Yansıma: gerçekte var, burada yok

Kullanıcının ikinci turda sorduğu şey: *"içerideki ışıklar yeterli değil, biraz
daha şiddetini artırmak mı lazım. Normalde ışık yansıyarak diğer kısımları da
aydınlatmaz mı gerçekte"*.

Haklı, ve cevabı teknik: bu sahnede **hiç yansıma yok**. Küresel aydınlatma
kapalı ve ışık haritası **pişirilemez** bile — bütün restoran çalışma anında
kuruluyor (`RestaurantView` geometriyi masa sayısına göre üretiyor), yani
pişirilecek sabit bir sahne yok. Yansımanın yerini tutabilecek iki şey var:
**ortam ışığı** (üniform) ve **ters yönden gelen gölgesiz bir dolgu** (gölgeli
yüzleri kaldırır — yansımanın gözle görülen işi büyük ölçüde budur).

İkisi de gece yanlış yöne ayarlıydı. Dolgu ışığı gece **maviydi**
(0,42 / 0,46 / 0,70) ve 0,22 şiddetindeydi: sıcak anahtarın vurmadığı her yüzey
*soğuk* bir ışıkla dolduruluyordu — sıcak bir salonun içinde yansımanın
yapacağının tam tersi.

### Ölçüm: içerisi, dışarısından karanlıktı

Göze "yetersiz" diyen şeyi sayıya çevirdim (`scratchpad/parlaklik.py`, render'ın
bölge bölge luma dağılımı):

| bölge | önce | sonra | gündüz |
|---|---|---|---|
| salon **ortanca** | 46,3 | **66,8** | 87,4 |
| salon ortalama | 65,6 | 81,5 | 88,1 |
| mutfak ortanca | 55,9 | 71,9 | 81,7 |
| **sokak ortalama** | **51,1** | **32,3** | 143,6 |

İlk sütun sorunu tek satırda anlatıyor: **salonun ortanca pikseli 46, sokağın
ortalaması 51** — restoranın içi, önündeki kaldırımdan karanlıktı. Ortalamaya
bakan biri bunu göremezdi (65,6 vs 51,1); ortanca ile ortalama arasındaki fark
da dağılımı söylüyor: havuzların altı parlak, araları karanlık.

### Neden sadece şiddet artırmak yetmiyor

İki sebep, ikisi de ölçüldü:

1. **sRGB sıkıştırması.** Işığı %20 artırmak parlaklığı yaklaşık %8 artırıyor
   (`%20^(1/2,2)`). Ölçülen ilk deneme tam bunu verdi: +%20 ışık → +%7 parlaklık.
   Ortancayı 50'den 75'e çıkarmak **2,5 kat** ışık ister, ve o da havuzların
   altını patlatır.
2. **Elimdeki her kaldıraç küresel.** Ortam, dolgu, sıcak anahtar — hepsi
   kaldırımı da restoran kadar aydınlatıyor, çünkü URP'de ek (yerel) ışıklar ve
   ışık katmanları kapalı. Şiddeti artırmak *farkı* hiç değiştirmiyordu.

Çözüm ikili oldu: **üniform bileşeni büyüt** (ortam 0,21 → 0,66, dolgu sıcak ve
0,22 → 0,75) ve **dışarıyı yerel olarak karart** — sokak levhaları gece kendi
malzemelerinden %28'e iniyor (`RestaurantView.TintStreet`). Gerçekte de doğrusu
bu: gece bir kaldırım, üzerine düşen ışık neyse o kadar aydınlıktır. Gündüz
hiçbir şey değişmiyor (tonlama 1,00).

Tur ikisini **birlikte** soruyor — yalnızca iç ışığa bakan bir kontrol, sokak da
aynı oranda parlarken yeşil kalırdı.

### Asıl sebep havuzlar değildi

Havuzlar yalnızca **zemini** aydınlatıyor: masa, sandalye ve figürler onlardan
hiçbir şey almıyor. "Restoranın içi karanlık" şikâyetinin asıl sebebi buydu —
zemin aydınlanıyordu ama üzerindeki her şey karanlıkta kalıyordu. Sıcak dolgu
ışığı (`DayLight.Warm`) güçlendirildi: 1,55 → 2,25 → **3,20**, yönü neredeyse
tepeden. (Üçüncü sayı ikinci ölçümden sonra geldi; belge bir süre 2,25'te
kaldı ve kodu geriden takip etti.)

### İç ışıklar sokak lambalarından **önce** yanıyor

`RoomThreshold` 0,62, `LampThreshold` 0,76. İkisi aynı anahtara bağlı olsaydı
akşam üstü salon karanlık kalırdı; kullanıcının gördüğü pencere tam o aralıktı.
Bir lokanta zaten kendi ışığını sokak lambalarından önce açar.

---

## 4. Kanatlı kapı yalnızca ikisinde

Kullanıcının kararı doğru ve sebebi de var: gerçek bir lokantada salon ile salon
arasında kapı olmaz, açık bir geçiş olur. **Kanat yalnızca bir eşiği işaretler**
— sokaktan salona, salondan mutfağa. Üstelik sekiz saydam kanadın 34 derecelik
bir bakışta sürekli açılıp kapanması hareketin kendisini gürültüye çevirmişti:
göz her karede sahnenin en çok kıpırdayan yerine gidiyor ve orası oyunun bilgisi
değil.

**Boşluk her geçitte, kanat yalnızca ikisinde.** `Connect()` hangi odanın
hangisine açıldığını söylemeye devam ediyor ve o kural bozulmadı (depoya
yalnızca mutfaktan). Değişen tek şey o boşluğa bir kanat konup konmadığı.

Bu değişiklik "kapı sayısı ≥ 5" kontrolünü kırmızı yapardı ve kontrolü silmek
kolay yol olurdu. Ama silinen şey gerçekten ölçülmesi gereken şeydi: **bir
odanın kapatılmadığı**. İki ayrı sayı oldu:

| kontrol | ne ölçüyor |
|---|---|
| `DoorCount == 2` | kanat tam iki: giriş + mutfak |
| `GapCount ≥ LinkCount + 1` | her komşuluk için bir geçiş, artı ana kapı |

---

## Yeni ölçümler (tur)

```
tamam : Kanatlı kapı yalnızca giriş ve mutfak (2)
tamam : Her komşuluk için geçiş boşluğu var (6 boşluk / 5 komşuluk)
tamam : Yayalar birbirinin içinden geçmiyor (en kötü 0 çift)
tamam : Yayalar lamba direğine girmiyor (en kötü 0 kişi, 3 direk biliniyor)
tamam : Sokak lambaları eşit aralıklı (3 direk, sapma 0,00 m)
tamam : İç tavan ışığı kuruldu (16 adet)
tamam : İç ışıklar yalnızca gün ilerledikçe yanıyor
tamam : Akşam iç dolgusu yanıyor (2,76)
tamam : İç ışıklar sokak lambalarından önce yanıyor (0,62 < 0,76)
```

`PlacementAudit`: 0 çakışan çift, 15/15 eşya doğru yönde, gövde bandı 0,67 m —
yaya en az mesafesi 0,68, şerit aralığı 0,70, **mesafe yeterli**.

Direk kontrolünde bir boşluk daha kapatıldı: `PostOverlaps` direkler hiç
yüklenmemişse de **0** dönüyordu, yani "itişme çalışıyor" değil "ölçecek bir şey
yok" demek de yeşildi. Artık o durumda 1 dönüyor ve tur ayrıca kaç direk
bilindiğini soruyor.
