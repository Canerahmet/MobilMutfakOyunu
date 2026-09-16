# 43 — Beş agentli inceleme: ölçmeyen ölçümler

*13 Eylül 2026.* Kullanıcının isteği üzerine beş agent baştan sona denetledi
(çekirdek simülasyon, görünüm katmanı, arayüz + yerelleştirme, tur + ölçüm,
belge–kod tutarlılığı). Hiçbiri dosya değiştirmedi; hepsi `dosya:satır` ile
bulgu yazdı. **Altmış bulgu, hepsi işlendi.**

Rakamlardan çok bir *desen* çıktı ve o desen bu belgenin konusu.

---

## 1. En ciddi bulgu: turun 107 kontrolü hiçbir şeyi kıramıyordu

`Autopilot` bütün kontrolleri `Debug.LogWarning` ile yazıp **her zaman
`Application.Quit(0)`** çağırıyordu. `run.ps1`'in ölümcül deseni
`SORUNLAR:|Fatal Error|Aborting batchmode` — `HATA` o listede yoktu. Üstelik
turu çalıştıran **bir betik bile yoktu**: `-lokanta-tur` bütün projede yalnızca
`Autopilot.cs` içinde geçiyordu.

Yani "tur 107/107 yeşil" cümlesi, pratikte **"kimse bakmadı"** anlamına
geliyordu.

| düzeltme | nerede |
|---|---|
| `Application.Quit(kaldi > 0 ? 1 : 0)` | `Autopilot` |
| `HATA  :` ölümcül desene eklendi, rapor `-Context 0, 400` | `run.ps1` |
| `tour.ps1` yazıldı (yapı + N koşu + özet) | yeni |
| `check.py --unity` Unity denetçilerini de listeliyor | `check.py` |

**Çökme ile "kontrol kaldı" ayrı şeyler.** `Application.Quit(1)` bu Windows
yapısında bir süre `0xC0000005` ile döndü — çıkış kodu tek başına ikisini ayırt
edemiyor, ve ters yönü daha tehlikeli: gerçek bir çökme "kontrol kaldı" diye
okunur ve kimse bakmaz. Tur artık `<çıktı>/ozet.txt` yazıyor
(geçti / kaldı / ölçülemedi); dosya yoksa tur bitmeden ölmüş demektir.

**Çökmenin sebebi SESTİ** ve bulunması üç denemeyi aldı:

1. `Application.Quit` bir korotinin içinden çağrılıyordu — Unity kapanış
   temizliğine çalışmaya devam eden bir korotin yığınıyla giriyor. Düzeltildi
   (bayrak bırakılıp `Update`'ten çağrılıyor), **çökme sürdü**.
2. Müziğin `OnAudioFilterRead`'i **ses iş parçacığından** çağrılıyor ve
   kapanışta klip yok edilirken hâlâ içeride olabiliyor. Bir `volatile` bayrak
   ve `AudioSource.Stop()` eklendi, **çökme yine sürdü**.
3. Bayrak yetmiyor çünkü Unity geri çağrıyı **çağırmaya devam ediyor**:
   `enabled = false` ile bileşen kapatıldı ve `AudioListener.pause = true`
   eklendi. Çıkış kodu 0.

Aradaki fark şu: ilk iki deneme geri çağrının *içini* susturdu, üçüncüsü geri
çağrının **çağrılmasını** durdurdu.

**Ve burada bir hipotez kurup yanıldım.** Ses düzeltmesinden sonraki iki koşu
sıfır kodla temiz kapandı, sonraki bir koşu sıfır dışı kodla çöktü; buradan
*"`Application.Quit(1)` kapanışı bozuyor"* sonucunu çıkardım ve turu kod
vermeyecek şekilde değiştirdim. Sonraki üç koşuda **ikisi yine çöktü — çıkış
kodu sıfırken.** Üç örnekten kurulan desen, dördüncüde dağıldı.

Bu belgenin konusu tam olarak buydu ve ben de aynı hataya düştüm: *üç
gözlemden çıkan bir örüntü ölçüm değil tahmindir.*

**Bugünkü durum, ölçülmüş hâliyle:** çökme kapanışta, **aralıklı** (üç koşunun
ikisinde), yönetilen kodun dışında (Unity çökme dökümü üretmiyor) ve turun
sonucunu etkilemiyor. Ses düzeltmesi sıklığı düşürdü ama bitirmedi. Açık bir
madde olarak duruyor.

Çıkış kodunu kaldırma kararı yine de **doğru** kaldı, ama farklı bir gerekçeyle:
iki ayrı şeyi (kaç kontrol kaldı / süreç sağlıklı kapandı mı) tek sayıya
bindirmek, ikisini de okunamaz yapıyordu. Ayrıldılar — sonuç `ozet.txt`'te,
çıkış kodu ise yalnızca kapanışın sağlığını söylüyor. `tour.ps1` artık ikisini
ayrı ayrı bildiriyor:

```
kosu 1 -> cikis -1073741819
  ozet: 112 gecti, 0 kaldi, 3 olculemedi
  -> UYARI: tur tamamlandi ama surec -1073741819 ile kapandi
```

Oyuncu için de önemliydi: kapanışta çöken bir oyun Windows'un "program
çalışmayı durdurdu" penceresini gösterir, ve oyunun duraklatma menüsünde
"Kaydet ve çık" var.

*Ama `ozet.txt` kalıyor: çıkış kodu iki farklı şeyi tek sayıya katlıyor ve bu
bir daha olacak.*

---

## 2. Üçüncü bir sonuç gerekti: **ÖLÇÜLEMEDİ**

Kontrollerin çoğu şu kalıptaydı:

```csharp
Note(toplam == 0 || oran > 0.6f, "klip ilerliyor");
```

Hiçbir şey olmadıysa **yeşil**. İki kararlılık koşusunda

```
tamam : Calisan personelin klibi ilerliyor (0%, 0 kare)
```

satırı tam böyle yeşil kaldı: kullanıcının özellikle istediği iş animasyonu
hiç ölçülmedi ve tur 107/107 yazdı.

`NoteIf(ölçtü, iddia, ...)` üçüncü bir sonuç üretiyor ve tur sonunda **"kaç
kontrol hiç sınanamadı"** basılıyor. Vakum artık sessiz değil. Canlılık
penceresi de iş klibi ölçülmeden çıkmıyor.

Aynı ailede üç düzeltme daha:

- **Aynı karede ihlal sayılıyor.** "Yiyen masada tabak var" iki *ayrı* karenin
  maksimumunu karşılaştırıyordu: 10. karede tabaksız yiyen bir masa, 500.
  karedeki başka bir masanın tabağıyla "doğrulanmış" sayılıyordu.
- **Dönüş sınırı kırılamıyordu.** `YawOffset <= MaxYawLimit` iddiası,
  `Mathf.Clamp(..., -MaxYaw, MaxYaw)` ile üretilen bir değeri **kendi
  sınırına** soruyordu. `ApplyGesture`'ın girdisi tamamen yok sayılsa bu
  kontrol *ve* ardından gelen "≈ 0" kontrolü ikisi birden yeşil kalırdı.
  Artık önce "gerçekten dönüyor mu" soruluyor.
- **`TrayCount >= 0`** bir `int` sayaç için her zaman doğruydu: sıfır bilgi.
  Artık `TrayCount <= StaffCount`.

Ve iki ölçüm yalnızca `Debug.Log`'a giriyordu: `GameShot`'un sahne bütçesi
(eşiksiz) ve `RoomLayout`'un 48 dp dokunma hedefi (bir satır **metin** olarak
yazılı, hiçbir koşulda karşılaştırılmıyor). İkisi de artık bir eşiğe bağlı.

Dokunma hedefinde eşik **iki kademeli** olmak zorundaydı: proje 48 dp'nin
altına inmeyi ölçüp kabul etmişti (docs/41), yani 48'i kırmızı yapmak verilmiş
bir kararı her koşuda hata diye bildirmek olurdu. 48 altı uyarı, 40 altı
kırmızı.

Ve bağlandığı anda bir şey ortaya çıktı: aracın şerit kopyası %40'ta kalmıştı,
gerçek en kötü aşama **%43,8**. Yani kabul edilen sayı da yanlıştı — taban 45
değil **42–43 dp**. Oyun değişmedi, ölçüm düzeldi.

`GameShot`'un yeni üçüncü sayısı (toplu çizim dışı çizici) da bir tahmini
çürüttü: denetim ~160 diye tahmin etmişti, ölçüm **84** dedi. Tahmin, gereksiz
bir iyileştirmeyi haklı gösterecek yöndeydi.

---

## 3. Ölçüm penceresi ölçtüğü günü yiyordu — üçüncü kez

Tur, canlılık penceresini **günün neresinde açtığını** hiç sormuyordu. İki
ardışık koşuda pencere %11'de ve %43'te açıldı; %43'te açılan koşuda salon
**boştu** ve altı kontrol birden kırmızıya düştü — hepsi mutfağı, kapıyı,
sokağı gösteriyordu, hiçbirinde sorun yoktu.

İki sebep vardı, ikisi de "ölçüm kendi kaynağını tüketiyor" ailesinden:

1. x240 hızda alınan **bir ekran görüntüsü** günün dörtte birini harcıyordu
   (yarım saniye × 240 ≈ 120 sim-saniyesi). Duraklatma görüntüden sonraydı.
2. "İkinci masa" beklemesi yirmi saniyenin tamamını kullanıp masayı yine
   bulamıyor ve günü %25'ten %41'e taşıyordu. Gün tavanı %60'tı.

Düzeltme: duraklatma görüntüden **önce**, bekleme tavanı %30, ve **pencerenin
nerede açıldığı artık bir kontrol**:

```
Note(gunBasi < 3500, "Canlilik penceresi gunun basinda basladi")
```

Bu satır olmadan aradaki fark görünmüyordu ve bir sonraki sefer yine "salon
boş" diye okunacaktı.

Bekleme tavanını düşürmek **yeni bir kararsızlık** yarattı: "servis dolu masa
üretiyor" kontrolü beklemenin son karesindeki *anlık* doluluğa bakıyordu ve
grup o karede kalkmış olabiliyordu. İddia "servis dolu masa **üretiyor**", yani
kümülatif; artık beklemenin gördüğü en yüksek değer soruluyor.

---

## 4. Çekirdek: iki satır bir korumayı **taklit** ediyordu

```csharp
int enFazla = _salon > 0 ? _salon - 0 : 0;   // == _salon, her zaman
if (enFazla > _salon) enFazla = _salon;      // hiç doğru olamaz
```

Yorum "salon kadrosunun tamamı lavaboya verilemez, yoksa oyun kendi kendini
kilitler" diyordu. Denetim ikisinin de no-op olduğunu **doğru** buldu — ama
yanlış olan koda değil **yoruma** aitti: `DispatchSalon` sıfırıncı sunucuyu
(patronu) lavaboya hiç koymuyor, yani kilit diye bir şey yok. Üstelik ilk gün
tek salon çalışanı varken onu lavaboya vermek **meşru bir karar** ve taban
konunca tabak darboğazı kampanyanın ilk günlerinde hiç denenemiyordu.

İlk düzeltmem tabanı *uygulamaktı* ve turu kırdı ("Bulaşık nöbetine atama
0 → 0"). Doğru düzeltme ölü satırları silmek ve yorumu gerçeğe çekmekti.

**Bir bulgunun teşhisi doğru, önerdiği çare yanlış olabilir.**

### Batma merdiveni kilidi çözmüyor, sonsuza kadar tekrarlıyordu

Merdivenin üçüncü basamağı borcu siliyor ve kasayı **sıfıra** bırakıyordu; ama
`Buy` `cost > _cash` ile reddediyor — sıfırla da hiçbir malzeme alınamıyor.
Kanıt kendi testimizin tanı çıktısındaydı: 10. günden 40. güne kadar **her gün
"0 grup, 4 masa"**. Otuz beş gün üst üste tek müşteri yok, kira ve maaş
kesilmeye devam ediyor, merdiven her hafta yeniden iniyor.

Merdiven tam olarak bu yumuşak kilidi çözmek için yazılmıştı.

Basamak artık dükkânı **çalışır** halde bırakıyor: bir günlük önerilen stoğun
bedeli kadar kurtarma payı — uydurma bir sayı değil, `RecommendedRestock` zaten
menüyü, talebi ve emniyet payını biliyor. Bedeli var: kurtarma değeri yıl sonu
sağlamlık eksenine yazılıyor.

### Küçülme tabak değişmezini kırıyordu

`Expand` fark kadar temiz tabak **ekliyor**, `Downsize` hiçbir şey
çıkarmıyordu. Hata kendini gizliyordu: gece `AdvanceToNextDay` tabakları
kademeye yeniden yazıyor, yani değişmez yalnızca **o gün** kırıktı — ve
`Tabak_sayisi_korunuyor` hiç küçülme koşmadığı için görmüyordu. Yeni test:
`Kuculen_dukkan_tabak_da_kaybediyor`.

### Kadro kararı akşam veriliyor ama **yarını** etkiliyor

`ReasonablePlayer.OnEvening` `RequiredCrewToday()` soruyordu ve
`AdvanceToNextDay` o çağrıdan **sonra** geliyor: cuma akşamı hafta içi kadrosu
kurulup cumartesi zirvesine eksik giriliyordu. Metodun adı doğruydu, **çağıranı
yanlış günü** soruyordu. `RequiredCrewTomorrow()` eklendi.

Yan etkisi ölçüldü ve docs/42'ye işlendi: müdahalenin baskı altındaki kazancı
+861'den +360'a indi. **Ölçüm, ölçtüğü şeyi iyileştirince küçülen bir sayı.**

### Denge aracının kendi bayrakları

`--solve` `KnownFlags` listesinde yoktu: kira çözücü yazıldı ve **hiç
çalıştırılamadı** (araç "Bilinmeyen bayrak" deyip 2 ile çıkıyordu). `--tohum`
ve `--strateji` kabul ediliyor ama hiç okunmuyordu — `--strateji baskili` yazan
kişi hata almıyor, tam koşuyu alıyor ve filtrelediğini sanıyordu. Bu, `CheckArgs`'ın
kendi yorumundaki tehlikenin **listenin içinde** tekrarlanmış hâliydi.

`Interventionist.Tried/Applied` statikti ve hiç sıfırlanmıyordu: iki kolun
toplamı tek satırda basılıyor ve sayaçların var oluş sebebi — kol başına
"müdahale gerçekten oldu mu" — okunamıyordu.

Ve bir uyarı **matematiksel olarak ateşlenemiyordu**: `peopleLost` GRUP,
`peopleServed` KİŞİ sayıyordu ve `Warnings()` ikisini doğrudan
karşılaştırıyordu (2.042 kişi / 16 grup, eşik 408). Metin "dörtte bir" diyor,
matematik "beşte bir" yapıyordu.

---

## 5. Sıfırı sıfırla karşılaştıran testler

```
bulasikcisiz: tabaksiz bekleme 0 tick, yikanan 15, servis 7
bulasikcili : tabaksiz bekleme 0 tick, yikanan 15, servis 7
```

İki koşu **birebir aynı** ve tek iddia `0 <= 0` idi: bulaşıkçı mekaniği
tamamen silinse test yine yeşil kalırdı. Dört masalık sakin bir dükkânda
ölçülecek bir şey yok.

Yeni test önce **baskı kuruyor** (yirmi beş gün sağlıklı büyüyen bir dükkân),
baskının gerçekten oluştuğunu **ön koşul olarak iddia ediyor**, sonra
karşılaştırıyor. Ölçtüğü şey de değişti: "tabaksız bekleme" değil, **salonun
işini bırakıp lavaboya kaç kez koştuğu** (`SalonRushWashes`) — bulaşıkçının
bütün değeri orada, ve salonun *boş vakitte* bulaşığa bakması zaten zararsız.
Sonuç: **31 koşuya karşı 3.**

Kardeş bulgu: "tabak basıncı zirvede ölçülüyor" testi *ölü bir dükkânı*
ölçüyordu (bkz. §4). Test botu artık mantıklı büyüyor (bir kademe, bedelin üç
katı kasada varsa) ve **son on günde müşteri ağırlandığı** bir ön koşul olarak
iddia ediliyor.

`NoFloatTests` de yalnızca **imzaları** tarıyordu; metot gövdesindeki
`double k = a / (double)b;` bu korumadan sorunsuz geçerdi — oysa determinizmi
bozan tam olarak odur. Kaynak taraması eklendi (yorumlar ve metin sabitleri
ayıklanarak), ve taramanın **kaç dosya gördüğü** de iddia ediliyor: yol yanlış
olsa döngü hiç dönmez ve test "ihlal yok" diye yeşil kalırdı.

---

## 6. Arayüz: görünmez sayılar ve kodda kalmış Türkçe

**"Bugün" kartındaki üç sayı krem üstünde beyazdı** — ölçüldü, kontrast
**1,08:1**. Servis boyunca oyuncunun bakacağı tek kart bu. Aynı karttaki
`CheckRow` doğru yapıyordu (koyu mürekkep), yani kartın yarısı okunuyordu ve
hata gözle yakalanmadı. Renk seçimi **çağırana bırakıldığı** için oldu; karar
artık `Kit.CountRow`'un içinde ve açık zemine koyu mürekkep zorunlu.

Oyunun en çok basılan düğmesi (`Cta`) da eşiğin altındaydı: `Go` üzerinde beyaz
19 dp kalın başlık **2,54:1** (eşik 3:1) ve %82 saydam alt satır ~2,2:1 (eşik
4,5:1) — o alt satır geri dönüşü olmayan kararı açıklayan tek cümle. Yüz
`GoDeep`'e indi (7,0:1), saydamlık kalktı.

Kredi düğmesi **30 × 30 dp** idi — projenin kendi ölçütü 52, Google'ın tabanı
48 — ve anlamı yalnızca `tooltip` ile veriliyordu; **dokunmatikte tooltip hiç
görünmez.** Görsel 30 kaldı, dokunma alanı 52 oldu.

**Kodda gömülü Türkçe: 42 dize.** `gen_loc.py`'nin kendi docstring'i bunu
*itiraf ediyordu* ("yirmi beş kadar arayüz dizesi hâlâ kodun içinde gömülü")
ama hiçbir denetim kırmıyordu ve sayı büyümüştü. En kötüsü servis müdahale
geri bildirimleriydi — oyunun ana döngüsündeki **tek** geri bildirim kanalı:
İngilizce oynayan biri düğmeye basıp *"3. masaya çay ikram edildi"* okuyordu.
Üstelik `". masaya "` bir Türkçe sıra eki; çevrilse bile kalıp çalışmazdı.

Üç yeni denetim `gen_loc.py`'ye bağlandı (`loc_tarama.py`):

| denetim | ne yakalıyor |
|---|---|
| kurucu taraması | `Theme.Text/Btn/Title/Head/Field` ve `Toast`'ın ilk argümanı sabit metinse |
| Türkçe harf taraması | `Ui/` altında Türkçeye özgü harf taşıyan **her** sabit dize (parça parça birleşenler dâhil) |
| ölü anahtar taraması | tabloda olan, hiçbir kaynağın istemediği `ui.*` |

Üçüncüsü **18 ölü anahtar** buldu: iki dilde bakımı yapılan, çevrilen, hiç
gösterilmeyen metin. `SCREEN_KEY` bütün `ui.*` ailesini fazlalık denetiminden
muaf tuttuğu için bu sınıf asla görünmüyordu.

Ayrıca:

- Yüzde işaretinin yeri koda gömülüydü (`"%" + n` → İngilizce'de "Margin %62").
  `Loc.Percent` kültürün `PercentPositivePattern`'ine bakıyor.
- Aynı şeyin iki adı vardı: dişli düğmesi `ui.hud.menu` = "Menü" ile menü
  tahtası `ui.morning.menu` = "Menü"; dişli artık "Duraklat". `ui.hud.angry`
  Türkçe'de duygu durumu ("Kızgın"), İngilizce'de çıkıp giden sayısı
  ("Walkouts") diyordu — **aynı sayı, iki farklı şey.**
- Kriz çipinde seçim **yalnızca renkle** veriliyordu (kızıl-yeşil körlüğünde
  iki renk de benzer parlaklıkta) — işaret ve çerçeve eklendi.
- Kırpılma ölçümü yalnızca **alt şeridi** tarıyordu ve `_bottom == null` ise
  sıfır dönüyordu: "ölçülemedi" ile "temiz" aynı sonucu veriyordu. Artık üst
  şerit ve kartlar da taranıyor, ölçülemeyen durum −1.

---

## 7. Görünüm: sahiplenilmeyen örgüler

`Modeler.Build` her çağrıda `new Mesh()` üretiyor ve **Mesh, GameObject yok
edilince peşinden gitmiyor**. En kötü yol kıyafetler: kadro bileşimi her
değiştiğinde tüm personel yeniden giydiriliyor ve kişi başı üç-dört örgü
yaratılıyor. Altmış günlük bir kampanyada binlerce yetim örgü.

Aynı sınıf ton kopyalarında da vardı (`_tinted`): sözlük her `Rebuild`'te
boşaltılıyor ama kopyalar yok edilmiyordu.

Çözüm sahipliği **nesnenin kendisine** koymak: `OwnedMesh` bileşeni örgüyü
GameObject'in ömrüne bağlıyor, yani `Clear()`, `Strip()`, sahne değişimi ve
prefab silinmesi hepsi aynı yoldan geçiyor. (Kapanışta devre dışı: Unity zaten
her şeyi boşaltıyor ve o sırada elle `Destroy` çağırmak süreci çökertebiliyor.)

Diğerleri:

- **Lamba halesi kameraya sabit açıyla kuruluyordu** ve yanındaki yorum
  "kameranın açısı sabit" diyordu. Bu, iki parmakla çevirme eklendiğinde
  geçersiz kaldı: ±35°'de hale inceliyor ve fener gövdesi ortasını kapatıyor.
  Sessiz bir bozulma — yalnızca "gece biraz sönük" olarak görünüyor.
- **Oyun kipinde `Clear()` bir kare boyunca çift geometri bırakıyordu.**
  Görünen bedeli tek karelik iki kat çizim; sinsi olanı `WallCount`,
  `WallsClear` ve `AccessOk`'ın o karede **iki katını sayması**.
- `CookRoutine.Pan` her tabakta üç nesne yaratıp yok ediyordu; projenin geri
  kalanı her yerde havuz kullanıyor ve gerekçesini yazmış.
- `PanMaterial` sessizce `null` dönebiliyordu → cihazda magenta tava, editörde
  hiçbir belirti.
- `Figure.BendKnees` üç alan için null kontrolü yapıp `Legs` için yapmıyordu.
- `_isKlip` ölçüm sözlüğü `Clear()`'da temizlenmiyordu: yeni personel eski
  indeksin ilerleme değeriyle karşılaştırılıyor ve **düzelen bir hatayı bozuk
  gösteriyordu**.
- `Wardrobe.Dress`'in dört sessiz erken çıkışı vardı; hepsi uyarı basıyor ve
  tur "giydirilen personel N/M" diye soruyor.
- Ölü kod: `RoomDining`, `Find(string)`, `_props`, `_pots`, iki kez yazılmış
  `shadowCastingMode`, boş gövdeli `Pendants()` + `PendantY`, `Modeler`'ın hiç
  çağrılmayan parlak dalı ve `ColorCount`, `Theme.Chip`, `Theme.MenuIcon`,
  `Icons.Clock`, `Icons.Trend`, ve her `Tick`'te metni üretilip koşulsuz
  gizlenen üç etiket.

---

## 8. Belge–kod: sayılar kodun gerisinde kalıyor

| belge | yazıyordu | gerçek |
|---|---|---|
| docs/41 | sarkıt lambalar var (§1 ve §3) **ve** kaldırıldı (§7) | kodda `Pendants()` boş gövdeli |
| docs/41 | `[docs/25](25-mutfak-kimligi.md)` | o dosya yok; doğrusu docs/10 |
| docs/38 | `Warm` 1,55 → **2,25** | kodda 3,20 |
| docs/40 | 524 anahtar | 572 |
| README | 88 test | 225 |
| README | "iki denge sorunu **açık**" | docs/42 ikisini de kapatmış |
| docs/19 | "ölçülen" bütçe tablosu | iki parça çıkarılmadan önce ölçülmüş |
| MorningScreens | "4.300 sikke, itibarı biraz düşüyor" | docs/42 ölçtü: itibar **değişmiyor** |

Son satır en öğretici olanı: yorumdaki "itibarı düşüyor" tam da docs/42 §4'ün
**ölçüp çürüttüğü** tahmindi. Bir belge cümlesi düzeltildiğinde onu kopyalayan
yorum düzeltilmiyor, ve yorum belgeden daha uzun yaşıyor.

Aynı aile: `WallColor` alanı silindiğinde **özeti silinmemişti** — sahipsiz
kalan yorum bir süre daha durdu ve üstelik daha da eski bir değeri (0,20)
anlatıyordu. Bir alanı silerken onu anlatan yorumu bırakmak, yorumu
**belge**den **efsane**ye çeviriyor.

---

## 8b. Denetimin görmediği bir boşluk: renderlere bakınca çıktı

Beş agent de kodu okudu ve altmış bulgu çıkardı, ama biri yoktu: **ocak
üstündeki kapları hiçbir kontrol sormuyordu.** Kullanıcının isteğiydi
("mutfakta ocak üzerine tencere vs konulmuyor, gerçekçi bir mutfak görüntüsü
yok"), kod yazıldı, tur 112 kontrol koşuyor — ve kapların varlığını ölçen tek
satır yoktu. Ocak yerleşimi değişse ya da `BuildPots` çağrısı düşse mutfak
sessizce yeniden boşalırdı ve bunu ancak kullanıcı görürdü.

Bulunma şekli de anlamlı: **renderlere bakınca.** Kaynak okuyan bir denetim
"şu satır yanlış" diyebiliyor ama "şu satır hiç yok" demek için neyin olması
gerektiğini bilmesi lazım — o bilgi kodda değil, istekte.

Aynı bakış iki şeyi daha düzeltti, ikisi de ölçerek:

- Salon **karanlık göründü**; ölçüldü (ortanca luma) ve gerçek yapıda **78,9**
  çıktı, sokak 43,6 — yani içerisi dışarıdan aydınlık, docs/38'in hedeflediği
  durum. Karanlık olan `GameShot` görüntüsüydü ve o, projenin zaten belgelediği
  editör toplu kip artefaktı.
- Zeminin koyu arduvaz olması **gerileme değil palet kararı**: hızlı yemek gri
  fayans, Türk sıcak ahşap + halı. İki render yan yana konunca kimlik sisteminin
  `ClearTints` refactor'ından sağ çıktığı da görülmüş oldu.

İkisinde de ilk tepkim "bir şey bozulmuş" idi ve ikisinde de yanlıştı. **Gözle
bakmak soruyu buluyor, ölçüm cevabı veriyor** — ve bu ikisi birbirinin yerine
geçmiyor.

---

## 8c. Mağaza çözünürlüğü bir hata sınıfını daha açtı

Mağaza ekran görüntüsü için tur 2,5 kat çözünürlükte koşturuldu (aynı dp
yerleşimi, büyük hali). O render iki düğmenin **üst üste bindiğini** gösterdi —
ve şeridin iki mevcut ölçümü de **yeşildi**: yükseklik bütçede, hiçbir *yazı*
kırpılmamış. Çünkü kırpılmak yerine biniyordu.

Sebep: UI Toolkit'te `flex-shrink` varsayılanı CSS'in aksine **sıfır**. Satır
sığmayınca hiçbir şey daralmıyor, son öğeler taşıp bir öncekinin üstüne
biniyor.

Ölçümü yazarken **üç kez yanlış şeye baktım** ve üçü de bu belgenin konusu:

| deneme | ne ölçtü | neden yeşil kaldı |
|---|---|---|
| 1 | `_bottom`'ın **doğrudan** çocukları | orada tek bir satır kapsayıcısı var, karşılaştıracak kardeş yok |
| 2 | **bütün** öğeler, ikişer ikişer | `Kit.Cta`'nın çift oku iki üçgeni **kasten** 6 dp bindiriyor; simgeler de şekil üstüne şekil |
| 3 | yalnızca **düğmeler** | — doğrusu bu: iki düğmenin binmesi her zaman hatadır, iki üçgenin binmesi bir simgedir |

Üçüncü deneme hatayı **adıyla** söyledi:

```
ust uste binen dugme (servis/en): Attention > worst x Close the day (13 dp)
ust uste binen dugme (gun 40, 14 masa): Ilgi > sabirsiz x Veresiye ac (87 dp)
```

İngilizcesi **birinci günde** oluyordu, yani her İngilizce oyuncu görüyordu.

Düzeltme tek hamlede olmadı ve her adımı ölçüm yönlendirdi: daraltmaya izin
verilince binme bitti ama **üç düğme kırpılmaya** başladı (hata görünmez
biçimden ölçülebilir biçime döndü); hedef eki iki düğmenin yazısından çıkıp
kendi rozetine alındı; "Mutfağı hızlandır" → "Hızlandır"; düğme dolgusu 16 →
8 dp; "Veresiye aç" → "Veresiye". Her adımda sayı düştü: 3 → 3 → 2 → 0.

**Hedef ekini kaldırmak ayrıca bir tasarım düzeltmesiydi:** aynı bilgi iki
düğmede birden yazıyordu ve dize uzunluğu *dile* bağlıydı — şerit daha önce de
aynı duvara çarpmış, ek "en sabırsız"dan tek kelimeye indirilmişti. Rozet
uzunluğu dilden bağımsız kılıyor.

### Ve bir de ölçümün kendi tuzağı

Mağaza görüntüsünün ilk hali bütün HUD sayılarını **boş** gösterdi. Oyunda
hata yok: ekran yeniden kurulunca değerler bir sonraki `Tick()`te yazılıyor ve
bu zaten bilinen, çözülmüş bir durum. Ama görüntü `Refresh()` ile **aynı
karede** alınmıştı — ve bir ekran görüntüsü tam olarak tek bir karedir.

*Bir kare süren bir boşluk oyuncuya görünmez; bir ekran görüntüsüne tamamen
görünür.*

---

## 8d. Yayın öncesi son tarama: kaydın kırıldığı an

Turun o ana kadarki kontrolleri oyunun **çalıştığı** hâlini ölçüyor. Yayın öncesi son
bakılan yer, oyunun **bozulduğu** hâliydi: oyuncunun kaydı okunamazsa ekranda
ne yazıyor?

Yol büyük ölçüde sağlamdı ve bunun sebebi daha önceki turlardı:

* Yazma gerçekten atomik (`Flush(true)` + tek `File.Replace`), yani yarım kayıt
  diye bir durum yok.
* Sürüm **özette de** duruyor, yani `SaveVersion` arttığında yuva kartı sağlıklı
  bir kampanya göstermeye devam etmiyor.
* Okunamayan yuva sessizce "boş" değil, **bozuk** görünüyor — sessiz "boş",
  oyuncunun üzerine yazıp gerçekten kaybetmesi demekti.
* `LoadSlot` yanlışa düşerse `ErrorScreen` açılıyor, oyun boş bir sahneye
  düşmüyor.

Kalan tek şey bir **çelişki** idi: bozuk bir yuvanın kartında üstteki rozet
"bozuk kayıt" derken, hemen altındaki pasif düğme "Boş" diyordu. Aynı kartın
iki yarısı iki farklı şey söylüyordu ve bu, oyuncunun altmış günlük
kampanyasını bulamadığı anda okuduğu tek ekrandı. Pasif düğme artık
`ui.slot.unloadable` ("Yüklenemiyor" / "Cannot be loaded") diyor.

Asıl eksik etiket değildi: **bu yol hiç koşulmuyordu.** Tur her zaman temiz bir
yuvayla başlıyordu, yani kaydın kırıldığı ekran altmış günlük turun uğramadığı
tek ekrandı. Tura beş ölçüm eklendi — dördüncü yuvaya geçerli biçimde ama
yanlış sürümlü bir özet yazılıyor (bozuk dosyanın `catch` dalı değil, sürüm
kontrolünün kendisi sınanıyor) ve şunlar aranıyor:

| ölçüm | neyi kırıyor |
|---|---|
| Bozuk kayıt varken ana menüde Devam | oyuncu kaydının varlığını göremiyor |
| Bozuk yuva bozuk görünüyor | sessizce "boş" görünüp üzerine yazılıyor |
| Düğmesi "Yüklenemiyor" diyor | kartın iki yarısı çelişiyor |
| Bozuk yuva yüklenmiyor | basılabilir bir Devam düğmesi kalmış |
| Bozuk yuva silinebiliyor | oyuncu yuvayı kurtaramıyor |

Kontrol sayısı 119'dan 124'e çıktı. Sondaki iki ölçüm tek başına vakumda
yeşil olabilirdi (ekran hiç açılmasaydı da geçerlerdi); birinci ve üçüncü
ölçüm ekranın açıldığını ve **Yükleme kipinde** olduğunu kanıtladığı için
değiller.

*Bir hata yolunun düzgün çalışması yetmez; hata yolunun kendi içinde de tutarlı
konuşması ve bunun ÖLÇÜLÜYOR olması gerekiyor.*

---

## 8e. İki ad, tek resim

Mağaza turundan çıkan dosyaları listelerken `12-kampanya-sonu.png` ile
`13-degerlendirme.png` **aynı bayt boyutundaydı**. İki farklı ekranın görüntüsü
aynı boyutta olabilir — ama `md5sum` ikisinin **bayt bayt aynı** olduğunu
gösterdi.

Sebep basit ve düzeltmesi de öyle: kampanya bittiği anda yıl sonu
değerlendirmesi zaten yığının üstünde. Yani "kampanya sonu" diye ayrı bir kare
**yok**; `Shot("12-kampanya-sonu")` değerlendirme ekranını ikinci kez
çekiyordu. Dosya yazıldığı için hiçbir şey uyarmıyordu.

Bu, belgenin geri kalanıyla aynı sınıftan: **görüntünün adı, gösterdiği şeyin
tek kaydı ve yanlış olabiliyor.** Tur ekran görüntülerini kendi gözüyle
denetlemiyordu.

Artık denetliyor. `RecordShot` her PNG'nin FNV-1a özetini bir deftere yazıyor
ve tur sonunda tek bir kontrol soruyor: aynı resim ikinci bir ad altında
yazıldı mı? Yazıldıysa **çifti adıyla** söylüyor (`12-kampanya-sonu =
13-degerlendirme`), "1 yinelenen" demiyor.

**Canlılık koşulunu ilk yazışımda yine yanlış kurdum.** `_shot >= 2` idi:
`RecordShot` hiç çağrılmasa bile `_shotDup` null kalır ve kontrol yeşil
dönerdi — ölçümün kendisinin koştuğunu değil, görüntü alındığını ölçüyordu.
Koşul artık `_shotHash.Count >= 2`, yani **defterde iki ayrı özet olması**.
Ölçülen sonuç: `19 goruntu, 19 ayri resim`. Kaldırılan görüntüyle birlikte
bu sayı 20/19 olurdu — kontrol gerçekten kırılabiliyor.


---

## 8f. 126 kontrolün göremediği şey: görüntünün kendisi

Mağaza turu 126 kontrolün hepsini yeşil verdi. Sonra çıkan PNG'ye **baktım**:
salonun tam ortasında üç bildirim balonu yığılmıştı — *"Soruldu ama yok:
Musakka / Nohut / Kıymalı Pide"* — ve restoranın kendisi görünmüyordu. Mağaza
listesine gidecek görselde oyun, üzerine binmiş üç olumsuz cümlenin altında
kalıyordu.

Bir önceki koşuda hiç balon yoktu. Yani hata "her zaman bozuk" değil,
**belirsiz**di: aynı kod iki farklı mağaza görseli üretiyordu ve hangisinin
çıkacağı o anki simülasyona bağlıydı.

Balonlar 3,5 saniye yaşıyor. Yakalama artık `NoticeCount == 0` olan bir kare
bekliyor (en fazla 12 sn) ve bunu bir kontrol olarak yazıyor:
`Magaza goruntusunde salonun ustu acik (0 balon)`. Ölçüm görüntü
**alındıktan sonra** yapılıyor — önce ölçmek, araya giren karelerde düşen bir
balonu kaçırırdı.

Sonuç: 40. gün, 14 masanın 10'u dolu, Ciro 1.188, Memnuniyet 78,5, itibar
96,8/100 ve salon açık.

Bu bölümün asıl konusu balonlar değil. **Bir kontrol ancak sorduğu soruyu
yanıtlar.** Tur görüntünün alındığını, HUD'un dolu olduğunu, düğmelerin
binmediğini, yazının kırpılmadığını ölçüyordu — hiçbiri "bu resim oyunu
gösteriyor mu" sorusunu sormuyordu. O soruyu ilk kez bir insan sordu.


---

## 8g. Işık ve gölge: figürler havada duruyordu

Kullanıcının sorusu: "oyun içi ışık ve gölge konusunda problem var mı". Vardı.

**Önce haritayı çıkarmak gerekti — oyunda gölgeyi kim düşürüyor?**

| kaynak | düşürür | alır |
|---|---|---|
| `Modeler`'ın ürettiği her şey (zemin kaplaması, duvar, tezgâh, masa, sandalye) | **hayır** | **hayır** |
| Oda zeminleri (`CreatePrimitive` küp) | evet | evet |
| Cam bölmeler, lamba direkleri, rozetler | hayır (her biri gerekçeli) | hayır |
| Karakterler (Kenney prefabı, varsayılan ayar) | **evet** | evet |

Yani oyundaki **tek gölge karakter gölgesi**. `Modeler.Build` bunu üretilen her
örgüde kapatıyor ve — her kararın paragrafla açıklandığı bir dosyada — **tek
satır gerekçe yazmıyor**. Bu tek başına bir hata değil (yalnızca hareket eden
şeylerin gölge düşürmesi savunulabilir bir üslup), ama sonucu şuydu: figürün
gölgesi bozuksa yanında kıyaslanacak başka gölge yok, yani bozukluk "üslup"
gibi okunuyor.

**Kusur.** Ekran görüntüsünü büyütünce figürün gölgesi **ayağından kopuktu**:
iri, bulanık, gövde boyunun bir kısmı kadar uzağa düşen bir leke. Yanındaki
sandalye zemine yapışık dururken insan havada duruyor gibi görünüyordu.

Hesap: gölge mesafesi 45 m, harita 1024 → **8,8 cm/texel**. Unity'nin varsayılan
`m_ShadowNormalBias` değeri 1,0 ve örneği normal boyunca **bir texel**
kaydırıyor — 1,10 m'lik bir figürde gövde boyunun %8'i. Klasik *peter-panning*.

**Neden hiçbir denetim yakalamadı.** `check_urp.py`, `.asset` ile
`ProjectSetup.cs`'i karşılaştırıyor — ama yalnızca `ProjectSetup`'ın
**ayarladığı** alanları. `m_ShadowDepthBias` ve `m_ShadowNormalBias` o listede
hiç yoktu, dolayısıyla denetimin kapsamı dışındaydılar ve Unity varsayılanında
kalmışlardı. *Bir denetçi, kendisine verilen listeden fazlasını göremez — ve
listenin eksik olduğunu da söyleyemez.*

**Düzeltme.** Harita 1024 → 2048 (texel 4,4 cm), bias 1,0/1,0 → 0,6/0,4. İki
kaldıraç da texel boyuna bağlı olduğu için birlikte çalışıyorlar: kayma
8,8 cm'den ~1,8 cm'ye iniyor. 2048 burada ucuz, çünkü gölge geçişinde
neredeyse hiçbir şey yok — maliyet haritanın temizlenmesi ve 8 MB bellek,
rasterlenen alan bir avuç küçük figür.

Her iki değer de **iki yere birden** yazıldı (belgenin kendi uyardığı kopya
tuzağı: `LokantaURP.asset` + `ProjectSetup.cs`) ve artık denetleniyorlar —
karşılaştırılan alan sayısı 12'den **14**'e çıktı.

Ölçülen sonuç: gölge ayağa yapıştı, kenarları keskinleşti, iç mekânda gölge
kiri (acne) oluşmadı, tur 128/0.

**Kusur olmayan ama ölçülen iki şey.** Aynı taramada piksel parlaklığı da
ölçüldü:

| | mutfak | yemek salonu | sokak |
|---|---|---|---|
| akşam | 79,3 | 48,6 | 47,3 |
| servis başı | 57,5 | 46,0 | 145,3 |

Akşam düzeltmesi (`DayLight`) mutfağı aydınlatmış ama yemek
salonu hâlâ koyultulmuş sokakla **aynı** parlaklıkta; gündüz ise içerisi
kaldırımın üçte biri kadar. İkisi de okunabilir durumda olduğu için ışık
yeniden dengelenmedi — ama şu kayda geçti: **"içerisi dışarıdan aydınlık
olmalı" kuralı mutfakta sağlanıyor, yemek salonunda sağlanmıyor.** Turun bu
konudaki kontrolü ortam ışığı *ayarına* bakıyor, ekrandaki piksele değil.


---

## 8h. Pah: "modeller çok keskin duruyor"

Kullanıcının isteği: mobilya ve eşyalar biraz daha oval kenarlı olsun.

`Modeler.BoxAt` kutuyu altı ayrı yüzden kuruyordu ve yorumu da bunu bilinçli
bir tercih olarak yazıyordu ("her biri KENDİ köşeleriyle: düz gölgeleme, keskin
kenar"). Yumuşatmanın doğru yolu **pah** (chamfer): 6 içerlek yüz + 12 kenar
şeridi + 8 köşe üçgeni, yani **12 üçgen yerine 44**.

**Bu mesafede pahın işi silueti değiştirmek değil.** 4,5 cm'lik bir pah
uzaklaştırılmış görünümde ekranda bir iki piksel. İşi, kenar boyunca **ışığı
kırmak**: pah yüzeyi komşu iki yüzden farklı bir açıda durduğu için her kenarda
ince bir açık şerit beliriyor. Düz gölgelemeli bir sahnede yumuşaklığı veren
şey o şerit.

**Kapıyı ilk seferinde yanlış eksene koydum.** Koşul "en ince kenar ≥ 5 cm"
idi ve oyunun *en görünür* parçasını eliyordu: masa tablası 0,80 × **0,04** ×
0,80, yani en ince kenarı 4 cm. Tabla, tezgâh üstü, sandalye oturağı, raf —
hepsi yassı levha, hepsi eleniyordu. Ölçüm de bunu söyledi: üçgen sayısı
yalnızca **%12** arttı, çünkü pah asıl mobilyaya hiç değmemişti.

İnce ekseni korumak zaten kapının işi değil — pah her eksende o eksenin
%28'iyle sınırlı, yani 4 cm'lik tablada dikey pah kendiliğinden 1,1 cm'ye
iniyor. Doğru kural **kaç kenarın büyük olduğu**: iki kenarı ≥ 10 cm olan
parçalar (levha ve blok) pahlanıyor, tek uzun eksenli çubuklar (korkuluk
çıtası, masa ayağı, direk) elenmeye devam ediyor — onlarda pah görünmez kalır
ama 44 üçgene mal olurdu.

| | üçgen (14 masa) | çizici |
|---|---:|---:|
| pah yok | 37.182 | 254 |
| yanlış kapı (en ince kenar) | 41.854 | 254 |
| doğru kapı (iki büyük kenar) | **44.638** | 254 |

Bütçe 80 bin; çizici sayısı değişmiyor, çünkü pah yeni nesne üretmiyor,
var olan örgüye üçgen ekliyor. Pah **büyüklüğünü** artırmak üçgen sayısını hiç
değiştirmiyor — kapı ölçülere bakıyor, pahın kendisine değil.

**Sarım yönü elle yazılmıyor, hesaplanıyor.** Yirmi altı yüzeyin sarımını elle
doğru yazmak, bir tanesinin ters olup içeri bakan bir yüzey üretmesi demekti —
ve ters yüzey hiçbir hata vermeden **görünmez** oluyor, yani sessizce bir delik
açılırdı. Şekil dışbükey ve yerelde merkezi başlangıçta olduğu için ölçüt
basit: yüzeyin normali kendi merkezinden dışarı bakmalı, bakmıyorsa sıra ters
çevriliyor.

### Ve pah düzeltilirken mağaza karesi bozuldu

Yeni kare **6/14 masa** gösteriyordu; oysa yakalama "yarısı dolsun" diye
bekliyor. Sebep bir önceki bölümde eklediğim balon beklemesiydi: iki bekleme
**arka arkaya** duruyordu ve ikincisi beklerken birincisi bozuluyordu —
misafirler kalkıyor, kare eşiğin altına düşüyordu. **Bekleme, beklediği şeyi
tüketiyordu** (bu belgenin §9'undaki üçüncü kalıbın aynısı).

Üç koşul artık tek döngüde, aynı karede aranıyor; ve "salon dolu" da görüntü
alındıktan sonra ayrı bir kontrol olarak yazılıyor. Yoksa bu sessizce
bozulurdu ve kimse söylemezdi. Sonuç: 9/14 masa, Ciro 1.377, balon yok.


---

## 9. Desen

Altmış bulgunun büyük çoğunluğu tek bir cümlede toplanıyor:

> **Bir şeyin ölçüldüğünü söyleyen satır, o şeyin ölçüldüğünü kanıtlamaz.**

Üç biçimde çıktı:

1. **Ölçüm hiçbir şeyi kıramıyor** — turun çıkış kodu, `GameShot`'ta eşiksiz
   `Debug.Log`, 48 dp'nin metin olarak yazılıp hiç karşılaştırılmaması,
   `--solve`'un hiç çalıştırılamaması.
2. **Ölçüm vakumda yeşil** — `Note(toplam == 0 || ...)`, `TrayCount >= 0`,
   `0 <= 0`, `Mathf.Clamp`'i kendi sınırına sormak, eşiği kırılamayacak
   birimle karşılaştırmak.
3. **Ölçüm ölçtüğü kaynağı tüketiyor** — canlılık penceresi, ikinci masa
   beklemesi, x240'ta alınan ekran görüntüsü.

Ve bir de yorumun kendisi: `_salon - 0`, "itibarı düşüyor", "başka ekranlar
okuyor", "Fazla biriken zaman ATILIYOR", "kameranın açısı sabit" — hepsi kodun
yapmadığı bir şeyi anlatan cümlelerdi. **Kodun yapmadığını anlatan bir yorum,
yorum değil yanlış bilgidir**, çünkü bir sonraki okuyucuyu kontrol etmekten
alıkoyar.

Beş agentin de tek bir dosya değiştirmemesi kasıtlıydı ve işe yaradı: bulgunun
**teşhisi** ile **çaresi** ayrı işler, ve §4'te görüldüğü gibi teşhis doğruyken
çare yanlış olabiliyor.
