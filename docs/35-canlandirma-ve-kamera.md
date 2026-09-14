# 35 — Canlandırma katmanı, çalışan ekipman ve iki parmak kamera

Bu doküman, salonun **durağan bir diyoramadan** çalışan bir restorana dönüştüğü turu kaydediyor. Üç iş birbirine bağlı: figürler artık **yürüyor**, ekipman artık **çalıştığını gösteriyor**, ve oyuncu artık kamerayı **kendi** yönetiyor. Üçünün ortak sonucu: simülasyonun bildiği şeyler ekranda görünür hale geldi.

Önceki durum: simülasyon her an hangi masanın hangi aşamada olduğunu, hangi istasyonda kaç tabak piştiğini biliyordu; ekranda garson ile aşçı **olduğu yerde duruyordu**, müşteriler masalarında **bir anda beliriyordu**, fırın ile ocak **hiçbir zaman** çalıştığını belli etmiyordu.

---

## 1. Yürüyüş katmanı

`Game/Walker.cs` + `Game/Paths.cs`. İkisi de yeni.

**Yol bulma yok, koridor var.** Kat planı sabit, odalar dikdörtgen, herkes aynı iki şey arasında gidip geliyor. İki parçalı bir yol — önce ön koridora çık (`Paths.LaneZ = 0,55`), sonra hedefin hizasından yukarı — hem her masaya ulaşıyor hem de bakıldığında *"kapıdan girip masasına gitti"* diye okunuyor. A\* bu sahne için gereksiz. Çarpışma da yok: figürler birbirinin içinden geçebilir, ve bu, tıkanıp dönüp kalmalarından iyidir.

| kim | nereden | nereye | ne zaman |
|---|---|---|---|
| müşteri | `Paths.Outside` (z = −0,85, kapının dışı) | masası | grup oturunca |
| müşteri | masası | `Paths.Outside` | grup kalkınca |
| bekleyen grup | kapı | `Paths.QueueSpot(i)` | masa yokken |
| garson | `Paths.SalonHome` (kasanın yanı) | `Paths.BesideTable` | sipariş / servis / hesap |
| aşçı | `Paths.CookHome` | `Paths.Fridge` → `Paths.KitchenPost(istasyon)` | iş başlayınca |

**Hız oyunun hızına bağlı.** `Walker.Speed = 1,15 m/sn` ×1'de. Simülasyon ×16'ya kadar hızlanıyor; yürüyüş **gerçek zamanda** kalırsa figür hâlâ yolun yarısındayken yemek gelmiş oluyor. Çarpan `GameApp`'ten her karede yazılıyor (`Paused ? 0 : TimeScale / BaseTimeScale`). Ama çarpanın tavanı var: ×4,5 üstünde yürüyüş okunmuyor, o yüzden **ışınlanıyor** — görülmeyecek kadar hızlı bir yürüyüş, yürüyüş değil titremedir.

**Duraklatınca yürüyüş de duruyor.** `GameSpeed = 0` → `Walker.Update` erken çıkıyor. Duraklatma ekranı açıkken salonun akmaya devam etmesi, duraklatmanın ne işe yaradığı sorusunu doğuruyordu.

**Garson yemeği taşıyor.** `RestaurantView.ShowPlate` tabağı garsonun eline bağlıyor, masaya varınca masaya bırakıyor. Tabak `Yemek/plate-deep` prefabı; taşıma ile servis arasında ayrı bir model yok.

---

## 2. Çalışan ekipman

`Game/Appliance.cs`. Yeni. Fırın/ocak başına bir bileşen.

**Parçacık yok.** docs/19 düşük seviye bir Adreno hedefliyor ve parçacık sistemi o sınıf cihazlarda belgelenmiş bir doldurma darboğazı. Alev de lamba da **birer kutu**: emissive renkli, gölge atmayan, çarpışanı olmayan. Üç ocak için altı kutu — çizim çağrısı olarak ölçülemez.

Dört ayrı hata üst üste düzeltildi, hepsi **görüntüyle** bulundu:

| belirti | sebep | düzeltme |
|---|---|---|
| kapak fırının içine gömülüyor | model pivotu kapağın **ortasında** | menteşe alt kenara, kapak ona bağlandı (`OpenAngle = −72°`) |
| lamba kapakla birlikte dönüyor | lamba menteşeye bağlıydı | gerçek fırında lamba gövdede durur → ama paketin kapak ağı **tamamen opak**, içerideki hiçbir şey camdan görünmüyor → lamba cam ile kapak yüzeyi **arasına** alındı |
| lamba yana kaymış, iki ocağın arasında yanıyor | menteşe alt kenarda, kapak modelinin yerel x'i 0,268 — ofsetler o kaymayı miras alıyordu | konum `pivot.InverseTransformPoint(b.center)` ile **görsel merkezden** hesaplanıyor |
| cam ve lamba beş kat küçük | menteşe modelin 0,204 ölçeğinin altında | `Panel()` boyutu ebeveynin `lossyScale`'ine bölüyor — çağıran taraf **metre** yazıyor |
| cam opak | URP'de `_Surface = 1` yalnızca **denetçi** ayarı | çalışma zamanında `_SrcBlend` / `_DstBlend` da yazılıyor |
| alev gözün önünde yanıyor | gözler ±%21 varsayılmıştı | **tepeden render edilip ölçüldü**: x simetrik (±%20,5), z **değil** — arka sıra +%20,4, ön sıra −%7,5 (ön kenardaki düğme sırası ızgarayı arkaya itmiş) |

Ölçüm görüntüsü: `Lokanta/Figur olcek goruntusu` → `render/olcek_ocak_ustten.png`.

---

## 3. İki parmak: yakınlaştırma ve döndürme

`Game/CameraRig.cs` + `Game/Quality.cs` (yeni).

| sınır | değer | neden |
|---|---|---|
| yakınlaştırma | 0,45 – 1,00 | 1,00 = docs/31'in ölçülmüş varsayılan çerçevesi. **Daha uzağa çıkılamıyor**: o çerçeve zaten her şeyi gösteriyor, uzaklaşmak yalnızca dokunma hedefini küçültür. 0,45 ≈ ×2,2 büyütme |
| döndürme | ±35° | serbest bırakmak iki şeyi bozuyor: dokunma hedefi ölçümü belli bir açıda yapıldı, ve arkadan bakıldığında mutfak salonun önüne geçiyor |

**Görüş açısı daraltılmıyor, kamera yaklaşıyor.** FOV'u daraltmak da büyütürdü ama perspektifi değiştirir ve dokunma hedefi ölçümünün kullandığı hesabı geçersiz kılar.

**Yaklaşınca çözünürlük yükseliyor.** Oyun 0,8 render ölçeğinde çiziliyor (piksel sayısı −%36, varsayılan çerçevede fark edilmiyor). ×2,2 büyütmede o yumuşaklık **görünür** oluyor; ayrıca yaklaşmış bir kamerada ekranda çok daha az şey var, yani tam çözünürlüğün bütçesi de var. Eşik 0,85 — küçük bir kaydırmada açılıp kapanmasın diye varsayılandan belirgin uzak.

**Genel görünüm ikisini de sıfırlıyor.** Oyuncunun her zaman bilinen bir yere dönebileceği bir yol olmalı.

### Parmak ile turun aynı yoldan geçmesi

`ApplyGesture(zoomDelta, twist)` **tek** giriş noktası: hem `HandlePinch` hem otomatik tur oradan geçiyor. Ayrı bir giriş yolu bırakmak, denetimin hiçbir zaman gerçek kodu ölçmemesi demekti — tur dokunmatik üretemiyor ve sınırlarla ilgili her şey ölçülmeden kalırdı.

### Turun yakaladığı gerçek hata: dönen kamera geri dönmüyordu

İlk yazımda tur şunu soruyordu:

```csharp
Note(Mathf.Approximately(Rig.Zoom, 1f) && Mathf.Approximately(Rig.YawOffset, 0f), ...)
```

**Yeşildi ve yanlıştı.** Kayan geçiş yalnızca **konumu** taşıyordu; açıyı yalnızca parmak hareketi yazıyordu. Yani oyuncu kamerayı çevirip "genel görünüm"e bastığında kamera doğru yere gidiyor ama **yan bakmaya devam ediyordu** — çevirdiği kamerayı düzeltmenin çaresi kalmıyordu. Alanlar (`Zoom`, `YawOffset`) doğruyu söylüyor, kamera söylemiyordu.

Düzeltme iki taraflı:

- `CameraRig.Begin()` geçişi başlatan **tek** yol oldu ve `_fromRot`'u da saklıyor; `Update` artık `Quaternion.Slerp(_fromRot, LookRotation, k)` uyguluyor.
- Tur artık **kameranın kendisine** soruyor: `Quaternion.Angle(Rig.transform.rotation, CameraFit.Rotation) < 1°`.

Doğrulandı: düzeltme geri alınınca kontrol **35,0 derece sapma** ile kırmızıya düşüyor.

> Aynı ders, bu projede kaçıncı kez: bir kontrolün yeşil olması, doğru şeyi ölçtüğü anlamına gelmiyor.

---

## 4. Mobilya ve karakter ölçekleri — yeniden

Kullanıcının iki cümlesi: *"masa karakterlerin başına değiyor gibi"* ve *"karakterler niye masaların köşesine oturuyor"*. İkisi de doğruydu ve ikisinin de sebebi farklıydı.

### Masa başa değiyordu

Ölçüldü: oturan figürün başı masanın **0,37 m** üstündeydi; gerçekçi oran 0,51. Kök sebep: **mobilya gerçek ölçekte, karakterler yarı ölçekteydi**. Karakterleri büyütmek oyuncak görünümünü bozuyordu, o yüzden mobilya indi:

| | önce | sonra |
|---|---|---|
| yemek masası | 0,74 m | **0,55 m** |
| sandalye | 0,92 m | **0,68 m** |
| `SitLift` | 0,35 | **0,26** |
| baş – masa açıklığı | 0,37 m | **0,47 m** |

Mutfak tezgâhları **bilerek** 0,92'de bırakıldı: ayakta çalışılan bir yüzey, oturulan bir masa değil.

Ayrıca kullanıcının önerisiyle **gövde değil kafa** küçültüldü (`ArtPrefabs.HeadScale = 0,80`, `head` kemiği ölçekleniyor). Bu paketin figürleri boylarından çok **enleriyle** büyük — kafa gövdenin üçte biri; küçülteceğin şey de o.

### Köşeye oturma: masa altıgendi

`tableRound` bir **altıgen** ve köşeleri ±Z'de. Dört oturak 90°'de duruyor; 60°'lik kenarlarla hiçbir açıda hizalanamaz — misafirler zorunlu olarak köşeye düşüyordu.

Kullanıcı kare seçti. `Mobilya/table` prefabı kullanılıyor ve kurulumda **kare yapılıyor**: `TableSquareZ()` renderer sınırlarını bir kez ölçüp `x/z` oranını `localScale.z`'ye yazıyor (0,25–4 arası kırpılı, önbellekli). Sabit bir sayı yazmak, model paketi değişince sessizce bozulurdu.

**Dört yönde de aynı uzaklık:** `SeatRadius = 0,58 m` tek sabit.

```
0,58 = 0,41 (yarı en) + 0,17 payanda
üst sınır, komşu masadan : 0,58 + figür eni/2 (0,32) = 0,90 ≤ 0,925  (hücre 1,85)
boş sandalyeler Z'de     : 0,58 + 0,15               = 0,73 ≤ 0,85   (hücre 1,70)
```

Görsel doğrulama: `render/olcek_masa_ustten.png` (tepeden, dört sandalye eşit uzaklıkta) ve `render/olcek_masa_oyun.png` (oyun açısı). İkisi de `RestaurantView.SeatAt()`'i çağırıyor — ölçüm aracı hesabı **yeniden yazmıyor**, oyunun kendi kodunu kullanıyor.

**İki misafir X çiftine oturuyor** (`SeatOrder` = 1, 3, 2, 0). Oyun kamerası bakışında X çifti yatay yayılıyor ve iki figür de tam görünüyor; Z çiftinde arkadaki, öndekinin ve masanın arkasına saklanıyordu. Dördü de çizilemiyor: hücre 1,85 × 1,70 m'de dört oturan figür hiçbir makul ölçekte sığmıyor (docs/31). Simülasyon etkilenmiyor — grup yine dört kişilik, fiş de öyle.

### Oturma: figür minderin 7,4 cm altındaydı

Kullanıcının iki cümlesi — *"sırt ve arka tarafları sandalyenin üstüne geliyor"* ve *"dizleri sandalyenin içine girmiş gibi"* — **aynı tek hatanın** iki belirtisiydi. `SitLift` hiç ölçülmemişti; göz kararı yazılmıştı.

Ölçüm aracı bunun için yazıldı (`Editor/FigureShot` → `OTURMA` satırları). Üç yanlış yöntem denendi ve üçü de kaydedildi, çünkü her biri ayrı bir tuzak:

| yöntem | neden çalışmadı |
|---|---|
| sınır kutusu | oturan figürün eni **1,01 m** çıktı — o **kollar**; kutunun altı bacak, arkası omuz olabiliyor |
| köşe örnekleme | paketin mesh'leri **Read/Write kapalı** (`isReadable: 0`), `.vertices` boş dönüyor; okunabilse bile **düşük poligonda** kutu gövdenin yalnızca sekiz köşesi var, ortasında hiç köşe yok |
| ışın — ama sandalyenin çarpışanı sahnede kalmış | ölçüm **sessizce sandalyeyi** okudu: figür 7,4 cm kaldırıldığında bile **aynı sayıyı** verdi. Değişmeyen bir ölçüm, ölçmediği şeyin habercisidir |

Doğrusu: pozlanmış mesh `BakeMesh` ile alınıyor (o mesh **bizim**, paketin ayarı engel değil), geçici bir `MeshCollider`'a takılıyor, yüzeyler ışınla okunuyor — ve **sandalyenin çarpışanları önce kaldırılıyor**.

Çıkan sayılar:

| | önce | sonra |
|---|---|---|
| minder yüzeyi | 0,355 m | 0,355 m |
| figürün leğen altı | 0,281 m | **0,355 m** |
| sırtın arkası / sırtlığın önü | 0,108 m **içinde** | 0,042 m **önünde** |
| ayaklar | — | yerden 0,14 m yukarıda, boşlukta |
| `SitLift` | 0,26 | **0,316** |
| `SitForward` (yeni) | — | **0,15** |
| oturulan sandalyenin yarıçapı (yeni) | — | **0,65** (boş sandalye 0,58'de, masaya yapışık) |

**Boş sandalye masaya yapışık, oturulan sandalye geride.** Gerçekte de oturmak için sandalye geri çekilir; burada ayrıca sırtlığa pay açıyor.

### Dizden kırmak mümkün değil — ve gerekmiyor

Bir tur, oturuşta bacak kemikleri dinlenme açısına (aşağı) yazıldı. Sonuç daha kötüydü: **iskelette diz yok** — bacak başına tek kemik var (`root, leg-left, leg-right, torso, arm-left, arm-right, head`), yani bacağı aşağı çevirmek uyluğu da çeviriyor ve uyluk minderin ön kenarını kesiyor.

Ölçüm bunu kesinleştirdi: **kalça minderin üstünde duracaksa, dizden kırılamayan bir bacak minderin içinden geçmek zorunda.** Paketin kendi klibi bu yüzden uyluğu yatay tutuyor — bacak minderin üzerinde uzanıyor, ayaklar ön tarafta boşluğa sarkıyor. Chibi oranlarda doğru duruş bu.

Kemik eklemek teknik olarak mümkün (bacak boyunca 22 ayrı köşe halkası var, yani yeni bir kemik gerçekten bükerdi) ama gerekmedi: asıl hata bacakta değil yükseklikteydi.

### Karakter 0,95 → 1,00 m

Kullanıcının isteği ("çok çok az büyütelim"). Komşu masa payı hâlâ tutuyor ve yerleşim denetimi iki mutfakta da **0 çakışma** veriyor. Baş artık masanın **0,57 m** üstünde (0,47'den): figür minderin içine gömülü olmaktan çıkıp gerçekten üstüne oturduğu için.

> Hedef boy artık **tek yerde**: `ArtPrefabs.CharacterHeight`. Yerleşim denetimi onu okuyor — uzun süre "hedef 1,28" yazmıştı, hedef çoktan değişmişti.

### Diz kemiği modele EKLENDİ

Kullanıcının sorusu: *"Peki modele sen kemik ekleyip düzeltebilir misin?"* Evet — ve gerekiyordu.

Paketin iskeleti: `root, leg-left, leg-right, torso, arm-left, arm-right, head`. **Bacak başına tek kemik, diz yok.** Sonucu ölçüldü: kalça minderin üstünde duracaksa, kalçadan aşağı inen tek parça bir bacak minderin içinden geçmek zorunda. Paketin kendi oturma klibi bu yüzden uyluğu yatay tutup ayakları öne uzatıyor — "sandalyede oturan insan" değil "yere bağdaş kurmuş insan".

Kemik **üretim hattında** ekleniyor (`ArtPrefabs.AddKnees`), tek seferlik bir düzenleme olarak değil: bir sonraki "Model prefablarını üret" çalışması onu silerdi.

| adım | ne yapılıyor |
|---|---|
| okunabilirlik | Karakter klasöründeki FBX'lerde `isReadable` açılıyor — paket kapalı geliyor ve `.vertices` boş dönüyordu |
| ayırma düzlemi | Bacağın kendi köşelerinin ortasına en yakın **iki halkanın arası**. Halkanın *üzerinden* geçerse düz gölgeli modelde aynı noktadaki iki köşe farklı kemiğe düşer ve yüzey açılır; aralarından geçince yalnızca tek bir dörtgen geriliyor |
| yeniden ağırlıklandırma | Düzlemin altındaki 64 köşe yeni kemiğe bağlanıyor, mesh **kopyası** varlık olarak kaydediliyor (paketin dosyası değişmiyor) |
| bağlanma matrisi | `diz.worldToLocalMatrix * renderer.localToWorldMatrix` |

**Bağlanma matrisi kendi kendini sınıyor:** aynı formül paketin *kendi* bacak kemiğine uygulanıp modelin getirdiği matrisle karşılaştırılıyor. Tutmazsa üretilen kemik de yanlış olurdu — ve bu, sessizce kayan bir mesh demek.

Duruş `Figure.BendKnees` ile kuruluyor: **uyluk öne, baldır aşağı**. Hiçbir klip diz kemiğini oynatmıyor, yani bütün eski duruşlar aynen duruyor.

Üç tuzak, üçü de ölçümle bulundu:

- **Bağlanma açısı çalışma anında okunamaz.** İlk yazım ilk kullanımda okuyordu ve okuduğu şey bağlanma açısı değildi — klip pozu çoktan değiştirmişti. Açılar artık **prefab üretiminde** kaydediliyor (`Figure.LegRest` / `KneeRest`).
- **Açıyla kurmak yerine yöne nişanla.** Uyluk ile baldırın kemik eksenleri aynı değil: uyluk beklendiği gibi dönerken baldır bambaşka yere gidiyordu. `Aim()` kemiğin bağlanma durumundaki "aşağı" eksenini bulup istenen yöne çeviriyor; eksenin nereye baktığını bilmek gerekmiyor.
- **Değmeyen yüzey ölçülmüştü.** Leğenin *ortası* mindere oturtuluyordu ve sayı yeşildi, ama değen yüzey **uylukların altı** — o da kalça kemiğinin 9,4 cm altında. `SitLift` 0,316 → **0,410**, masa 0,55 → **0,58**. Ölçüt de düzeltildi.

Sonuç: leğen sapması +0,094 (uylukların üstünde, doğru), **bacak payı 0,000** (uyluklar tam minderin üstünde), sırt payı 0,005 (sırtlığın önünde). Ayaklar yerden 0,23 m yukarıda boşlukta — bu paketin bacakları boyun %32'si (gerçekte %52) ve ayakları yere değdiren sandalye 0,24 m olurdu.

### Aşçı artık iş yapıyor ve baktığı yöne dönüyor

Üç ayrı hata:

1. **Varışta açı sabit 180° yazılıyordu** — aşçı ne yaparsa yapsın aynı yöne bakıyor, ocağı arkası dönük kullanıyordu. Açı artık **ocağın kendi konumundan** geliyor (`Paths.FaceFrom(tezgah, StovePos(...))`), ve eşleme `UpdateAppliances` ile aynı — yani aşçı **yanan** ocağa dönüyor.
2. **Boşta duran personel de sabit açıyla duruyordu.** Artık `float.NaN` geçiliyor: yürüdüğü yönde kalıyor.
3. **Animator ~1 sn sonra kapanıyordu** — oturan müşteri için doğru (kıpırdamıyor), çalışan aşçı için yanlıştı: doğrama klibinin **ilk karesinde** donup kalıyordu. Çalışan figür artık `HoldAwake()` çağırıyor. Duruş sayısı yeşildi, görüntü ölüydü.

Mutfak işleri paketin hazır kliplerinden kuruldu, yeni animasyon üretilmedi: `attack-melee-right` → doğrama (yukarıdan aşağı inen kol), `interact-left` → yıkama, `pick-up` → malzeme alma. İstasyona göre sabit dağıtılıyor, yani üç ocakta üç ayrı hareket var ama görüntü titremiyor.

### Odaları ayıran saydam duvarlar

Kat planı tek bir zemin levhası gibi okunuyordu; odaların sınırını yalnızca 4 cm'lik bir boşluk ve renk farkı söylüyordu.

| karar | değer | neden |
|---|---|---|
| yükseklik | 1,15 m | karakter 1,00 m; oda hattını çiziyor ama 34°'lik bakışta içerisi görünüyor. Tam boy (2,4 m) ön sırayı tamamen kapatırdı |
| alfa | 0,20 | duvar **orada** olduğu anlaşılsın, arkasındaki masayı ve aşçıyı gizlemesin |
| ön kenar | çizilmiyor | kapı orada ve kamera oradan bakıyor |
| çarpışan | yok | dokunma hedefi oda zemini (docs/31); duvara çarpan ışın oda seçimini bozardı |
| gölge | kapalı | URP'nin gölge geçişi alfayı okumuyor — saydam duvar **opak** gölge düşürüp salona siyah şeritler çiziyordu |

**Ortak kenar bir kez çiziliyor.** İki kez çizilseydi alfa üst üste biner ve o duvar diğerlerinden koyu olurdu — bakan kişi "orada daha kalın bir duvar var" diye bir anlam uydururdu. Anahtar, santimetreye yuvarlanmış uç noktalar.

Yerleşim denetiminde duvarlar **hariç**: oda sınırında duruyorlar ve o sınıra dayalı her tezgâhla tanım gereği kesişiyorlar.

---

## Doğrulama

| ne | sonuç |
|---|---|
| `tools/check.py` | 12/12 |
| çekirdek testleri | 220 |
| otomatik tur (gerçek Windows yapısı) | **57/57** |
| yerleşim denetimi (`PlacementAudit`), iki mutfak | **0 çakışan çift** |
| baş – masa açıklığı | 0,63 m |
| oturma (`OTURMA SONUC`) | leğen +0,094 / bacak 0,000 / sırt 0,005 |
| AAB | 31,1 MB, **0 uyarı** |

Yeniden ölçmek için:

```powershell
tools\unity\shot.ps1 -Method "Lokanta.EditorTools.FigureShot.Capture"     # ölçek görüntüleri
tools\unity\shot.ps1 -Method "Lokanta.EditorTools.PlacementAudit.Run"     # çakışma denetimi
tools\unity\run.ps1  -Method "Lokanta.EditorTools.BuildPlayer.Windows"    # sonra otomatik tur
```
