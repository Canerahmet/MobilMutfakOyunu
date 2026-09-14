# 36 — Sokak, günün saati, kapılar ve mutfağın aşamaları

12 Eylül 2026. Bu tur, restoranı bir **kat planı** olmaktan çıkarıp bir **yer** haline getiriyor: dışarısı var, saat geçiyor, odaların kapısı var ve mutfakta gerçekten yemek yapılıyor.

Beş iş birbirine bağlı ve hepsi aynı soruya cevap veriyor: *ekranda gördüğüm şey, simülasyonun bildiği şeyi anlatıyor mu?*

---

## 1. Odalar arası kapılar

Duvarlar bir önceki turda geldi ama figürler onların içinden geçiyordu — bir duvarın ne işe yaradığı ancak kapısı varken belli oluyor.

### Duvarlar hat hat, kapılar oda çiftlerinden

İki yazım denendi ve ikisi de yanlıştı:

| yazım | hata |
|---|---|
| her odanın dört kenarı ayrı | ortak hatlar **iki kez** çizilip alfa üst üste biniyordu; o duvar diğerlerinden koyu çıkıyor ve "orada daha kalın bir duvar var" diye bir anlam uyduruyordu |
| hat başına **tek** kapı | x = 5,2 hattında **üç ayrı komşuluk** var (Giriş–Bulaşık, Mutfak–Bulaşık, Mutfak–Depo) ve tek kapı yalnızca birine yarıyordu |

Doğrusu: duvarlar hat hat (aralıkların **birleşimi**, tek parça), kapılar **oda çiftlerinden**. Her komşu ve birbirine açılan çift için bir kapı, ortak kenarın ortasına — ortak kenar ön koridoru (`Paths.LaneZ`) içeriyorsa oraya, çünkü geçiş zaten oradan oluyor.

**Boşluk kırpılıyor, elenmiyor.** İlk kural "iki yanında da en az 15 cm duvar kalsın" idi ve bu, en çok kullanılan üç kapıyı birden eliyordu: koridor duvarın en başında, yani solda hiç duvar kalmıyor. Köşe hizasındaki bir açıklığın kendisi zaten doğru.

### Odaların mantığı: depoya yalnızca mutfaktan

Kullanıcının kuralı, `Connect()` içinde tek yerde yazıyor:

```
Depo  ->  yalnizca Mutfak
digerleri -> komsu ise acilir
```

Gerçek bir lokantada da kiler mutfağın arkasındadır; salondan ya da bulaşıkhaneden doğrudan girilmez. Bulaşıkhane zaten mutfağa bitişik (x = 5,2 hattında z 4,0–5,4 ortak kenar).

**Kural sınanıyor** (`RestaurantView.AccessOk`): kapı grafiği geziliyor ve iki şey doğrulanıyor — her açık oda girişten ulaşılabilir mi, ve *mutfak kapatıldığında* depoya hâlâ girilebiliyor mu. İkincisi "yalnızca mutfaktan" kuralının makine karşılığı. Otomatik tur bunu her koşuda soruyor.

### Kapılar yaklaşana açılıyor

`Door.cs`. Menteşe kanadın **kenarında** (model pivotundan döndürmek kanadı duvara gömüyor — fırın kapağında tam bu olmuştu). Algılama yarıçapı **1,10 m**: yürüme hızı 1,15 m/sn ve kapı 0,28 sn'de açılıyor, yani figür varmadan yaklaşık 0,8 m önce kanat tam açık oluyor.

Çarpışan yok: dokunma hedefi oda zemini (docs/31 ölçümü), kapıya çarpan bir ışın oda seçimini bozardı. Kapı figürü **durdurmuyor** da — yalnızca görüntü.

---

## 2. Sokak

Müşteriler çerçevenin alt kenarında beliriyordu ve orası hiçbir şeydi. "Dışarıdan geldi" duygusunu veren şey, gelinen yerin **var olması**.

Üç levha: kaldırım, bordür, asfalt. Dört sokak lambası. Müşteriler artık `Paths.Street(i)`'de — kapıdan birkaç metre uzakta — belirip kaldırımda yürüyerek geliyor.

> **Bu bölümün sayıları [docs/38](38-sokak-ve-ic-isik.md)'de değişti.** Dört
> lamba üç oldu ve arsaya değil *açık odalara* göre yerleşiyor; kaldırım iki
> yaya şeridi alacak kadar genişledi (yayalar birbirinin içinden geçiyordu) ve
> yürüme çizgisi asfaltın üstünden kaldırıma taşındı — eskiden `PavementZ`
> −1,05'ti ve kaldırım −0,62 ile −0,02 arasındaydı, yani herkes yolda
> yürüyordu.

**Çerçeve 1,10 m öne açıldı ve bu ölçülerek seçildi.** Kamera çerçevesi derinliğe bağlı; önden eklenen her metre restoranı ekranda küçültüyor. Dokunma hedefi tabanı **71 → 64 dp** düştü; Google'ın asgarisi 48 dp.

> Sonradan **1,94 m** oldu ([docs/38](38-sokak-ve-ic-isik.md)), taban **59 dp**.
> Ayrıca yukarıdaki 64 o gün *hesaplandı ama ölçülmedi*; docs/31'deki 71 de
> sokak eklendiğinde güncellenmemişti.

### Yoldan geçenler

`StreetLife.cs`. Kullanıcının cümlesi: *"sokaktan geçen karakterlerin tamamı lokantaya gitmesin, bazıları yola devam etsin veya kendi aralarında konuşup sonra yola devam etsinler."*

Beş kişi, kaldırımda iki yönde. Yan yana gelen ikili %35 olasılıkla durup **birbirine dönerek** 2,2–4,5 saniye konuşuyor, sonra devam ediyor. Aynı ikilinin hemen tekrar konuşmaması için 8 saniyelik bir bekleme var — olmazsa "sohbet" değil "tıkanma" diye okunuyor.

**Simülasyona hiç dokunmuyorlar:** müşteri değiller, çekirdek onları bilmiyor, hiçbiri masaya oturmuyor. Rastgelelik kendi tohumunda (sabit), çekirdeğin RNG'sine dokunmadan — tur her koşuda aynı sokağı görmeli.

---

## 3. Günün saati

*"Oyun içerisinde sabah öğle ve akşam ayırımı belli değil."* Doğruydu: günün evresi yalnızca arayüzdeki bir yazıdan okunuyordu.

`DayLight.cs` **dört kanalı birden** değiştiriyor, çünkü tek kanal (örneğin yalnızca ışık şiddeti) "akşam oldu" değil "biri lambayı kıstı" diye okunuyor:

| kanal | sabah | öğle | ikindi | akşam |
|---|---|---|---|---|
| güneşin açısı | 26° / 148° | 62° / 208° | 34° / 246° | 10° / 268° |
| ışığın rengi | soğuk-sıcak | nötr | sıcak | mor-mavi |
| şiddet | 1,05 | 1,55 | 1,25 | **0,32** |
| arka plan (gökyüzü) | soluk mavi | açık mavi | sıcak | neredeyse siyah |
| lambalar | — | — | — | **yanıyor** |

Kaynak tek bir sayı: `Simulation.ServiceProgressBp`. Değerler ara değerleniyor — kesme geçiş yok, yoksa "saat 14 oldu" diye bir kare atlıyor.

> **Çekirdek oran TAMSAYI vermeli.** İlk yazım `ServiceProgress01` adıyla `float` döndürüyordu ve kayan nokta yasağı testi haklı olarak kırmızıya düştü (docs/23). Oran on binde cinsinden veriliyor; kayan noktaya çevirmek görünümün işi.

### Sokak lambası: sahnede duran ama çizilmeyen ışık

İlk yazımda lamba başına bir **nokta ışığı** kondu ve hiçbir şey yapmadı: URP varlığında ek ışıklar **kapalı** (`m_AdditionalLightsRenderingMode: 0`, docs/19 mobil bütçesi). Sahnede duran ama çizime hiç girmeyen bir ışık — "lamba yanıyor" diye bakıp hiçbir şey görmemek demek, ve hiçbir şey uyarmaz.

Yerine: kaldırımda yatan, **ışıksız ve toplayıcı** harmanlanan bir levha, üzerinde çalışma anında üretilen yumuşak daire dokusu (64×64, diske dosya koymadan). Bedava ve gerçekten görünüyor.

Düz renkli bir levha **kare** bir ışık havuzu veriyordu; kenarı yumuşayan bir daire aynı levhayı gerçek bir ışık havuzuna çeviriyor.

---

## 4. Mutfağın aşamaları

*"Aşçı yemek pişirirken malzemeleri alsın, onları yıkasın, doğrasın, pişirsin; ocağa tava koysun, piştikten sonra tavadaki yemeği tabağa aktarsın."*

`CookRoutine.cs` — dokuz aşamalı bir sıra:

```
dolaba git -> AL (malzeme elinde) -> tezgaha git -> YIKA -> DOGRA
   -> ocaga git (TAVA ocakta) -> PISIR -> TABAKLA -> pasaya birak
```

Klipler paketin hazır kliplerinden; yeni animasyon üretilmedi: `attack-melee-right` → doğrama (yukarıdan aşağı inen kol), `interact-left` → yıkama, `pick-up` → malzeme alma. Tava paket taşımadığı için üç kutudan yapılıyor (gövde, sap, içindeki yemek).

### Neden bir sıra, neden her karede simülasyona bakmıyor

Ölçüldü (otomatik tur, `"simulasyon is verdi 145 kez"`): **çekirdek aşçının işini karelerin ancak %10'unda açık tutuyor** — bir iş birkaç yüz milisaniye sürüyor. Görünüm bunu doğrudan izleyince aşçı buzdolabına doğru iki adım atıp geri dönüyordu; **hiçbir istasyona varamıyor**, dolayısıyla hiçbir çalışma duruşuna giremiyordu. Mutfak bomboştu ve sebebi buydu.

Çözüm: çekirdek bir iş verdiğinde görünüm **bir sıra başlatıyor** ve onu sonuna kadar oynatıyor. Ekonomi değişmiyor — burası çekirdeğe hiçbir şey yazmıyor, yalnızca okuduğunu insan hızında anlatıyor.

### İki tuzak daha

- **Aşçılar HER KAREDE sorulmalı.** Personel döngüsü "görev değişti mi" diye bakıyordu ve bir tikten kısa bir işi **kaçırıyordu**: simülasyon on kez iş verdi, görünüm sıfır kez gördü. Aşçı dalı artık kendi döngüsünde, her karede.
- **Çalışan figürün Animator'u açık kalmalı.** `Figure` geçişten ~1 sn sonra Animator'ı kapatıyor — oturan müşteri için doğru (kıpırdamıyor), çalışan aşçı için felaket: doğrama klibinin **ilk karesinde** donup kalıyordu. "Duruş verildi" yeşildi, görüntü ölüydü.

### Aşçı baktığı yöne dönüyor

Varışta açı sabit **180°** yazılıyordu; aşçı ne yaparsa yapsın aynı yöne bakıyor, ocağı arkası dönük kullanıyordu. Açı artık hedefin konumundan geliyor — ve hedef **aşamadan**: yıkarken tezgâha, pişirirken ocağa. Denetim önce "çalışan aşçı ocağa bakar" varsayıyordu ve 176° sapma ölçtü; haklıydı, aşçı doğru yere bakıyordu, denetim yanlış hedefi soruyordu.

Boşta duran personel de artık sabit açıyla durmuyor: yürüdüğü yönde kalıyor.

---

## 5. Garsonun tepsisi

*"Garson her seferinde belirli sayıda yemek ve içecek taşıyabilsin; tepsi kullanıp kullanmaması da bu sayıyı etkilesin."*

Kural: **tek tabak elde, iki ve üstü tepsiyle.** Tepsi kapasitesi 3 — dördüncüsü figürün eninden taşıyor. Sayı simülasyondan okunuyor (kaç masa yemek bekliyor), yani uydurma değil.

**Görünüm katmanında, kısıt değil görüntü.** Çekirdeğin salon kapasitesi kişi-gün modeli; sefer başına taşıma kısıtı eklemek ekonomiyi değiştirir ve altmış günlük dengenin yeniden çözülmesini gerektirir. Gerçek bir mekanik isteniyorsa maliyeti bu ve ayrıca kalibrasyon koşusu gerekir.

---

## 6. Eşyaların doğrultusu — ölçüldü

*"Restoran içerisindeki eşyaların yerleşimini ve doğrultularını kontrol et."*

Kural ölçülebilir: **duvara dayalı bir eşya odanın içine bakmalı.** Ocağın ağzı duvara dönük olamaz, tezgâhın ön yüzü duvara bakamaz. Eşyanın "önü" `RestaurantView.PropYaw` kuralından geliyor, yani ikinci bir varsayım yok.

Sonuç: **15 duvara dayalı eşya, 0 tanesi ters.** (Eşik paspası hariç tutuldu: yatay bir levhanın "ön yüzü" yok — denetim onu ters sayıyordu ve bu bir hata değil, kuralın o nesneye uymamasıydı.)

Neden ölçüm: "gözle bakıldı" bir kez doğrudur; kat planı ya da yerleşim değişince kimse yeniden bakmaz.

---

## Doğrulama

| ne | sonuç |
|---|---|
| `tools/check.py` | 12/12 |
| çekirdek testleri | 220 |
| otomatik tur (gerçek Windows yapısı) | **69/69** |
| yerleşim denetimi, iki mutfak | 0 çakışma, 15/15 doğru doğrultu |
| dokunma hedefi tabanı (20:9) | 64 dp (asgari 48) — *bugün 59, bkz. docs/38* |
| oda erişim kuralları | tamam (depoya yalnızca mutfaktan) |

Turun bu turda eklenen soruları: oda duvarları kuruldu / duvarlar saydam ve çarpışansız / kapılar kuruldu / yaklaşan figür kapıyı açtı / müşteri sokaktan geliyor / sokakta yoldan geçenler var / oda erişim kuralları / mutfakta iş yapılıyor / çalışan figürün animasyonu işliyor / aşçı hedefe dönük / sabah ile akşam arka planı farklı / güneşin açısı gün içinde değişiyor / akşam güneş zayıflıyor / sokak lambaları yalnızca akşam yanıyor.

### Ölçümün kendisi ölçtüğü şeyi bozuyordu

Kaydetmeye değer: canlılık kontrolleri (yürüyen figür, açılan kapı, sokaktaki müşteri, çalışan aşçı) önce **ayrı ayrı** bekliyordu ve tur servisi ×240'ta geçiyor — dördü üst üste yaklaşık yirmi beş saniye ediyor, o sırada servis penceresi kapanıyor ve son kontroller **boş** bir salonu ölçüp kırmızıya düşüyordu. Hız normale çekildi ve hepsi **tek döngüde** örnekleniyor.

Aynı sınıftan ikinci bir tane: masa seçimi kontrolü "dolu masa sayısı"nı bekleyip sonra ayrı bir döngüde "etkin masa" arıyordu; aradaki birkaç karede masa boşalabiliyordu. **Beklenen şey ile aranan şey aynı olmalı.**
