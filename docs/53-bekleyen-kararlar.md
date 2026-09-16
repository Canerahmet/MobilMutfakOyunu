# 53 — Bekleyen kararlar kapandı: bulaşıkçı, kombo, personelin sesi

*15 Eylül 2026.* Kullanıcı bekleyen tasarım kararlarını devretti:

> *"Tüm bu bekleyen kararlar için farklı agentlar ile ayrı ayrı düşün ve en iyi
> yolu kendin uygula. Repliklerin doğal olmasına dikkat et, bunun için
> internetteki kaynaklardan faydalanabilirsin. Bulaşıkçı için de benzer oyun
> varsa oradaki mekanikleri ek olarak incelemek isteyebilirsin."*

İki araştırma turu koştu (on iki sevk edilmiş oyunun mekaniği; oyun yazarlığı
ve Türkçe lokanta/zincir konuşma kaydı), sonra ölçüm. **Bu belgenin asıl
konusu, ölçümün beni iki kez yanılttığı yer.**

---

## 1. Ölçtüğüm "iyileşme" yoktu — yapılar farklıydı

Bulaşıkçının domine bir seçenek olduğunu [49](49-ulasilamayan-mekanikler.md)
yazmıştı: tabaksız bekleme `planci` 263'e karşı `bulasikci` **349**.

İki değişiklik yazdım ve 12 tohumda **349 → 275 → 229** gördüm. Temiz bir
iyileşme hikâyesiydi ve **yanlıştı**: 349 sayısı docs/49'dan geliyordu, yani
self servisten, günün sivriltilmesinden, kiradan ve yıkama hızından *önceki*
bir yapıdan. Kendi yapımda "önce"yi hiç ölçmemiştim.

Ölçtüm. Aynı yapı, 32 tohum:

| strateji | HEAD | kriz devri | + boşta-dönüş |
|---|---:|---:|---:|
| makul | 255 | **240** | 240 |
| planci | 179 | 180 | 180 |
| imzaci | 193 | **186** | 186 |
| **bulaşıkçı** | **198** | **197** | **216** |

İki şey birden çıktı:

**(a) Açık karar kendiliğinden kapanmıştı.** Bugünkü yapıda bulaşıkçı 198'e
karşı 179 — docs/49'un yazdığı 349'a karşı 263 uçurumu yok. Aradaki
değişiklikler (self servis, sivri gün, uzman yıkama hızı) sorunu zaten
büyük ölçüde eritmiş. *Açık bir kararı yeniden ölçmeden çözmeye kalkışmak,
olmayan bir hastalığı tedavi etmektir.*

**(b) "Düzeltmem" bozuyordu.** Boşta-dönüş kolu bulaşıkçıyı 197'den 216'ya
çıkardı.

---

## 2. Araştırma haklıydı, ama benim vakam değildi

On iki oyun tarandı (RimWorld, Dwarf Fortress, Two Point Hospital, PlateUp!,
Overcooked, Prison Architect, Tavern Keeper, Supermarket Simulator…). Bulgu
kesin: **sevk edilmiş hiçbir oyunda uzman işe almak genel havuzu sessizce
kapatmıyor.** Dışlama ya oyuncunun seçtiği bir kip (Dwarf Fortress'te
*Everybody / Nobody / Only selected* üçlüsü), ya bir onay kutusu, ya da kaba
personel sınıfları arasında. Kuyruk teorisindeki adı **de-pooling**: ayrı
kuyruk, ortak havuzdan kötüdür.

Supermarket Simulator tam bu hatayı sevk etmiş: kasiyeri kovup kasaya kendin
geçmek daha kârlı, ve forumun en çok tekrarlanan isteği *"boştaki personel
yardım etsin"*.

Ben de onu yaptım — ve **ölçüm hayır dedi.** İki deneme, ikisi de makuldü:

| deneme | gerekçe | sonuç | ölçek |
|---|---|---:|---|
| yıkayacak şey yokken salona dönsün | de-pooling'i kapat | 197 → **216** | 32 tohum, HEAD'e karşı |
| yığın bir eşiği geçmeden yıkamasın | uzman tek tabak için lavaboya gitmesin | 229 → **293** | 12 tohum, **aynı ölçekte değil** |

İkinci satır sadece **yönü** gösteriyor; 229 rakamı §1'de çürütülen 12
tohumluk zincirin ucundan geliyor ve 32 tohumluk tabloyla karşılaştırılamaz.
Aynı tabloda yan yana koymak, bu belgenin şikâyet ettiği hatanın tekrarıydı —
ölçek sütunu o yüzden var.

İkisi de aynı şeyi bozuyor: **uzmanın bütün değeri aralıksız ve hemen
yıkamasında.** Salona dönen bulaşıkçı, tabak kirlendiğinde bir müşteri işine
bağlı kalıyor ve lavaboya geç dönüyor. Beklemeye alınan bulaşıkçı ise yığını
büyütüyor.

Kullanıcının kuralı (*"bulaşıkçı alınca herkes kendi işini yapar"*) yerinde
kalıyor — ama artık **varsayım olarak değil, ölçüm olarak.**

*Bir araştırma bulgusu ne kadar sağlam olursa olsun, senin vakanda geçerli
olduğunu ölçmeden uygulamak, akıl yürütmeyle yazılmış bir koruma yazmaktır.*

---

## 3. Kalan ve tutulan tek değişiklik: mutfak durduğunda salon lavaboya

Temiz tabak bitince tabak dolum döngüsü **komple duruyor**: pişmiş yemek
tezgâhta bekliyor. O anda bir garsonun yeni sipariş alması değersiz iş.

Bu dal artık **müşteri işinden önce** geliyor.

docs/49'da aynı fikir denenmiş ve 351 → 351 vermişti. Sebep: istisna müşteri
işinden *sonra* yazılmış ve "boş kişi" aramıştı; zirvede salon dolu olduğu
için hiç ateşlenmedi. **Boş kişi aramak yanlış soruydu** — doğru soru "şu an
yapılan iş değerli mi".

### İlk yazımda üç kusur vardı; eleştiri turu buldu

1. **Bayrak mandallanıyordu.** `PlateUp()`'ın "pişmiş grup yok" çıkışı
   `_plateStalled`'ı temizlemiyordu. Tıkanan grup sabırsızlanıp kalkınca
   (`LeaveAngry` `_pCooked`'u sıfırlamıyor) bayrak takılı kalıyor, salon kriz
   dalında kilitleniyor, kimse sipariş almadığı için yeni pişmiş grup da
   oluşmuyordu — bayrak kendini besliyordu. Beteri: o durumda **raporladığım
   tek ölçüt düşüyor**, çünkü kalkan müşteri tıkanmayı da götürüyor. Ölçüm
   kendi en kötü hâline karşı kördü.
2. **Eşik yoktu.** Tek kirli tabak için beş sunucu birden lavaboya gidiyor,
   dördü boş dönüyordu.
3. **Sayaç karışıyordu.** Kriz yıkaması `_salonRushWashes`'e ekleniyordu —
   `PlateTests`'in bulaşıkçı iddiasını taşıyan sayaç. Kendi yorumunun
   yasakladığı karışım.

Üçü de düzeltildi; bayrak artık her tick yeniden hesaplanan bir **türev**,
yani kayda da determinizm karmasına da girmesi gerekmiyor.

### Düzeltmeden sonra, bütün eksenler (32 tohum, Türk mutfağı)

| strateji | tabaksız | ağırlanan grup | son kasa | kızgın |
|---|---:|---:|---:|---:|
| makul | 255 → **240** | 1883 → 1883 | 18.439 → 18.442 | 7 → 7 |
| planci | 179 → **178** | 2567 → 2552 | 23.443 → 23.490 | 16 → 16 |
| imzaci | 193 → **186** | 1849 → 1849 | 15.074 → 15.075 | 7 → 7 |
| bulaşıkçı | 198 → **195** | 2547 → 2534 | 23.607 → 23.749 | 15 → 15 |

Artık hiçbir strateji HEAD'in gerisinde değil. Ama dürüst kalan iki şey var:

- **Bedel ağırlanan grupta görünüyor:** `planci` −15, `bulaşıkçı` −13 grup
  (%0,6). Sunucuyu müşteriden çekmenin karşılığı bu. Kasa yine de yükseldiği
  için takas kabul edildi, ama takasın olmadığını söylemek yanlış olurdu.
- **Dağılım ölçülmedi.** Ne stddev ne güven aralığı var; küçük farklar
  gürültüden ayrılamaz.

Bu tabloyu ilk yazımda tek sütunla vermiştim. *İyileştirdiğim ekseni ölçüp
tehlikeye attığımı ölçmemek, bu belgenin şikâyet ettiği şeyin kendisi.*

RimWorld'ün **yangın** davranışı da tam bu kalıp: nadir, ağır, kapsamlı bir
koşul normal önceliği geçer.

---

## 4. Kombo: cevap self servisle tersine dönmüş

[45](45-tasarim-incelemesi.md) §18 kombo ekseninin "var olmayan bir stratejiyi
koruduğunu" yazmıştı: `zirvede_kapat` ile `imzaci` **birebir aynı 2016 grubu**
ağırlıyordu, çünkü kombonun mutfak yükü ısırmıyordu.

O ölçüm **self servisten önceydi.** Fast food'un salon yükü yarıya inince
darboğaz mutfağa geçti. Yeniden ölçüldü:

**12 tohum, fast food, 60 gün — ve kriz dalı yokken ölçüldü:**

| strateji | son kasa | servis | kombo% | tabaksız |
|---|---:|---:|---:|---:|
| makul (kombo yok) | 22.492 | 2617 | %0,0 | 846 |
| imzacı (hep açık) | 22.163 | 2559 | %18,1 | 1018 |
| **zirvede_kapat** | **23.474** | 2564 | %16,8 | **792** |

İki uyarı, ikisi de bu belgenin kendi tezinden çıkıyor:

- **12 tohum.** §1 tam da 12 tohumluk bir sonucun 32'de çürüdüğünü anlatıyor.
  Bu tablo o riske açık ve yeniden ölçülmeden kapanmış sayılmamalı.
- **Tabaksız sütunu §1'inkiyle kıyaslanamaz** (846 vs 240): farklı mutfak,
  farklı tohum sayısı, ve kriz dalı henüz yokken.

Kombo artık **58 grup kaybettiriyor** ve zirvede kapatmak hep açık tutmayı
**+1.311** geçiyor (bu ölçekte +%5,9 ve −%2,2; dağılım ölçülmedi).

Vaadin **"mutfağı yorar"** yarısı destekleniyor (tabaksız 1018 > 846).
**"Ortalama fişi yükseltir"** yarısı için tabloda sütun yok — kasadan geri
hesaplarsan +%0,8 çıkıyor ama son kasa masraf sonrası, yani fiş değil. Yani
"ilk kez doğru" dediğim şeyin yarısı hâlâ ölçülmemiş durumda.

**Dengeye dokunulmadı** — yazılı bant (`imzaci/makul` %90–130) zaten
sağlanıyor: %98,5 (hep açık) ve %104,4 (zirvede kapat).

**Eksen de düz kalıyor, ve bu artık bir eksiklik değil:** iyi oyunun payı
*daha düşük* (%16,8 < %18,1). Oran tabanlı hiçbir hedef bunu düzeltemez;
hedefi yükseltmek daha kötü oynayanı ödüllendirirdi.

### Ama düğmeye hiç basılmıyordu

Kombo düğmesi `GameScreen`'de vardı ve **tur ona hiç basmıyordu.** Bu projede
tıpatıp aynı boş kapsam iki kez çıktı (`SetQuality`, `CollectCredit`):
mekanik çekirdekte eksiksiz, ekranda düğmesi var, komutun geçtiğini kimse
ölçmüyor.

Üstelik kombo artık **oyunun en iyi oyununun tek kapısı**. Komut geçmeseydi o
oyun oynanamazdı ve hiçbir şey bunu söylemezdi. Tur artık iki yönlü basıyor:
çeviriyor, doğruluyor, geri alıyor.

---

## 5. Personelin sesi: yirmi müdavimin altmış satırı vardı, personelin sıfır

| | müdavim | personel (önce) |
|---|---|---|
| metin anahtarı | 5 (`ad`, `iş`, 3 sahne) | **0** |
| ekranda | akşam tam ekran sahne | ad + rol + iki kuru etiket |

Oyunun tamamındaki tek yarı-anlatı personel dizesi `ui.staff.inherited` idi.

### Uygulanan: huyun sesi

Araştırmanın en yüksek kaldıraçlı bulgusu Two Point Hospital'in yapısal
numarası: **mekanik ad ile insan cümlesi ayrı iki dize.** `Cheap` mekaniği,
*"Will work for peanuts"* metni.

`trait.<id>.desc` mekaniği anlatmaya devam ediyor ("günün son çeyreğinde
yavaşlar"). Yeni `trait.<id>.voice` kişiyi anlatıyor:

```
Çabuk Yorulan   .desc  "Günün son çeyreğinde yavaşlar."
                .voice "Akşama doğru ayakları konuşmaya başlıyor."

Huysuz          .desc  "Ekibin moralini aşağı çeker."
                .voice "Herkesle bir derdi var. Çoğunda da haklı."

Tecrübeli       .desc  "Pahalıdır, hızlıdır, daha fazla gelişmez."
                .voice "Otuz yıldır bu iş. Öğretilecek bir şey kalmamış."
```

On iki huy × iki dil. Aday kartında, rol başlığının hemen altında ve
**birinci** huydan geliyor —
iki ses üst üste binince kişi değil liste okunuyor. Oyuncunun bir personeli
dikkatle okuduğu tek an orası.

### Sesin kuralları (uydurulmadı, çıkarıldı)

- **Davranışı adlandır, kişiyi değil.** Dwarf Fortress "tembel" demiyor,
  *"finds obligations confining"* diyor. "Huysuz"un satırı onu kötü ilan
  etmiyor, çoğunda haklı buluyor.
- **Açıklamayı esirge.** RimWorld: *"Somehow, HE survived."* Müdavim sesi
  zaten böyle: *"Oturuyor, sen biliyorsun."*
- **Düz ve kısa.** Sevk edilmiş bark yazısının tek ortak uyarısı: şirinliğe
  uzanan replik üçüncü saatte ekşir, onuncu saatte dayanılmaz olur.
- **Simülasyonun yalanlayabileceği hiçbir şey söyleme** — Ludeon'un kendi
  yazım kılavuzunun kuralı, ve bu projenin "çağrı yeri yalan söyleyen alan"
  kuralının aynısı.

### Yapılmayanlar ve sebepleri

**Fonetik şive yok.** *Geliyom*, *napıyon*, *uşağum* — hiçbiri. Belgelenen
alay göstergesi tam olarak bu. Personel standart yazılı Türkçe konuşuyor,
sınıf sözdizimiyle.

**"Evet, Şef!" yok.** O, fes takmış bir "Yes, Chef!". Belgelenen karşılık
heyecansız *"Tamam şef"*, lokantada *"tamam usta"* ya da *"eyvallah"*.

**Üç isimli personel yazılmadı.** [14](14-personel-sistemi.md) bunu tasarlamış
— mutfak başına elle yazılmış üç kişi, sabit huylar, kendi sahneleri — ve
**hiç uygulanmamış**: ne içerik dosyası, ne anahtar, ne kod yolu. Müdavim
altyapısı (`StoryBeat`, kapı, `StoryScreen`) aynen kullanılabilir; eksik olan
içerik, ve o içerik kullanıcının sesi.

**Uzun kıdem anı yazılmadı.** Araştırma bunu *sahipsiz en güçlü an* olarak
işaretledi ve Türkçe kaynaklardaki asıl şikâyetle örtüşüyor: bulaşıkçı
kendine *"restoranın kalbi"* diyor ama *"hiçbir şey yapmıyormuşuz gibi
görünüyoruz"*. Yani **"beni gör" diyen bir satır "bana zam ver"den daha
sert iner.** Ama kıdem takibi `SaveVersion` artırır ve göç yolu yazılmadan
sürüm artırmak altmış günlük kampanyaları siler ([README](README.md)).
Sıradaki iş bu.

---

## 6. Denetimin kendisinde üçüncü bir kopya vardı

`.voice` ailesi eklenince metin denetimi kırmızı yandı: *"LocTests.cs'de YOK:
.voice"* — oysa eklemiştim.

Sebep: aile listesi **üç yerde** duruyordu. İki kopyayı karşılaştıran kontrol
(`gen_loc.py` ↔ `LocTests.cs`) kendi prob listesini elle tutuyordu. İki kopyayı
denetleyen bir kontrol, üçüncü bir kopya üzerine kurulamaz.

İki taraf da artık **kaynaktan** okunuyor. Mutasyonla doğrulandı: `.voice`
testten çıkarılınca üretim reddediyor.

---

## 7. Eleştiri turu: iki agent, yirmi bir bulgu, altı gerçek kusur

Kullanıcı *"sonrasında farklı agentlarla eleştir ve karara bağla"* demişti.
İki düşman gözü koştu — biri mekaniğe ve ölçüme, biri Türkçe repliklere. İkisi
de işe yaradı ve **ikisi de bu belgenin ilk hâlinde yalan bulduğu için asıl
değerini gösterdi.**

### Koda inen kusurlar

**Bayrak mandallanıyordu** (§3'te anlatıldı) — en ciddi olanı, ve ölçümün
kendisi ona karşı kördü.

**Tur kontrolü totolojiydi.** `Loc.T` eksik anahtarda `[anahtar]` döndürüyor,
kart da aynı çağrıyı yapıyor. Yani **çeviri hiç yokken iki taraf da aynı
yanlış dizeyi üretiyor ve kontrol yeşil geçiyordu.** Kendi mutasyonum bunu
yakalayamazdı çünkü kartın dizesini değiştirmiştim, anahtarı değil. Artık iki
şart birden aranıyor, ve ayrıca `Her_huyun_sesi_var` testi eklendi: on üçüncü
bir huy eklense on üç denetimin hiçbiri konuşmazdı.

**Denetimin kendisi vekile dönmüştü.** §6'da aile listesini kaynaktan okumaya
çevirmiştim — ve o, davranışsal bir probu **metinsel** bir probla değiştirmek
oldu: "kaynakta yazıyor mu" diye soruyordu, "gerçekten muaf mı" diye değil.
Şimdi ikisi birden: aileler kaynaktan çıkarılıyor, sonra her biri
`SCREEN_KEY`'e **soruluyor**. İki yönde de mutasyonla doğrulandı.

**Yorumda ölçülmemiş iddia.** Kriz dalının yorumu "bulaşıkçı varken kriz zaten
oluşmuyor" diyordu. Ölçüm tersini söylüyor (bulaşıkçı kolu oynuyor, yani dal
ateşleniyor). Yorum düzeltildi: dal bulaşıkçıdan bağımsız ateşleniyor **ve
öyle olmalı** — duran bir mutfakta bulaşıkçı zaten geride kalmış demektir.

### Repliklerde: iki satır simülasyonun yalanladığı şeyi söylüyordu

Belgeye *"simülasyonun yalanlayabileceği hiçbir şey söyleme"* diye yazdığım
kuralı, aynı oturumda iki kez çiğnemişim:

- `CleanlinessBp` **yalnızca** `TraitSum(1, ...)` ile, yani salonda masa
  toplarken okunuyor. Aşçıya düşen "Hızlı ama Dağınık"ın hiçbir bedeli yok —
  ama satırım mutfak tezgâhını işaret ediyordu, yani etkinin *kanıtlanabilir
  şekilde olmadığı* yeri.
- `MoraleAura` iki havuzdan da toplanıyor, yani bulaşıkçıya da düşüyor — ama
  satırım "O mutfaktayken" diyordu.

Dokuz satır yeniden yazıldı. İki dil hatası da çıktı: *"elleri birbirine
dolanıyor"* deyimin yarım hatırlanmış hâliydi (doğrusu **eli ayağına
dolaşmak**), *"ayakları konuşmaya başlıyor"* ise İngilizce bir deyimin
kalıbıydı. İngilizce tabloda da sahipsiz iyelik sızmıştı (*"the hands get
tangled"*) ve iki satırda zamir yanlış öncüle bağlanıyordu.

Üç satır olduğu gibi kaldı — en iyisi `suratsiz`: *"İşini yapar, konuşmaz.
Bazı masalar üstüne alınıyor."* Kişiyi yargılamıyor, mekaniği birebir
karşılıyor, ve tepkiyi simülasyonun koyduğu yere — müşteriye — koyuyor.

*Kendi kuralını yazdığın belgede o kuralı çiğnemek, kuralın işe yaradığını
gösterir: onu bulan şey kuralın kendisiydi.*

---

## 8. Kayıt göç yolu: kural yazılmıştı, iki yerde uygulanmıştı

Personelin uzun kıdem anını yazmak `SaveVersion` artırmayı gerektiriyordu ve
[README](README.md) bunu yasaklıyordu: *"Bunu yazmadan içerik yaması
çıkarılmamalı."*

Bakınca kural **iki dosyada yazılıydı** — `StateIO.cs` ve `Simulation.Save.cs`
ikisi de *"yeni alanlar `Has()` ile okunur ve yoksa varsayılanda bırakılır"*
diyordu. Sayınca: **126 okumanın 2'sinde** uygulanmış. Yine akıl yürütmeyle
yazılmış, hiç koşturulmamış bir koruma.

### `Has()` zaten yanlış araçtı

Bütün okumaları `Has()` ile sarmak mekanizmayı kurardı ama bir şeyi de yok
ederdi: **`Has()` bir alanın yokluğunu her zaman meşru sayar.** Yani gerçekten
bozuk bir kayıtla eski bir kaydı ayırt edemez. 21. sürüm kaydında `badges`
yoksa o kayıt bozuktur ve patlaması *doğrudur*.

Doğru araç **sürüm kapısı**:

```csharp
if (version >= 21) { ...21'de eklenen alanlar... }
```

Eski kayıtta atlanıyor, 21. sürüm kaydında eksikse hâlâ patlıyor. İkisi
ayrışıyor.

### Ve mekanizmanın koştuğu kanıtlandı

`Eski_surum_kaydi_aciliyor` gerçek bir 21. sürüm kaydı üretiyor, 21'de eklenen
altı alanı siliyor, sürümü 20 yapıyor ve yüklüyor — yani yayından sonraki
gerçek durumun aynısı. Ölçüt iki yönlü: kayıt **açılacak** *ve* eksik alanlar
**varsayılanda kalacak**; yalnızca birincisini sormak, her şeyi sıfırlayan bir
göç yolunu da yeşil geçirirdi.

`Cok_eski_surum_reddediliyor` da kapının hâlâ bir kapı olduğunu söylüyor —
yoksa "her sürümü kabul et, alanları boş bırak" gibi bir uygulama da geçerdi.

**Testin kendi içine koyduğum doğrulama satırı beni bir kez durdurdu:** ilk
yazımda alanları `header` düğümünde aradım, oysa sürüm orada ama alanlar
`restaurant`'ta. O satır olmasaydı test hiçbir şey silmeden, mekanizmayı hiç
sınamadan yeşil geçecekti — *kurduğu "eski kayıt" gerçek olmayan bir göç
testi, göç testi değildir.*

Mutasyonla da doğrulandı: kapı `if (true)` yapılınca test
`Kayitta alan yok: badges` ile kırmızı yanıyor.

`MinReadableVersion = 20` — bir adım geri. Daha eskisi **uydurma olurdu**:
9–14 arası sürümlerin neyi değiştirdiği belgesiz, yani onlar için doğru kapıyı
kimse yazamaz. Yayınlanmış kayıt da yok.

Böylece personelin uzun kıdem anının önündeki engel kalktı.

---

## 9. Göç yolunu kurarken bulunan yalan: "0 gün çalıştı"

Uzun kıdem anını yazmak için kıdeme bakınca `StaffDaysWorked` çıktı — ve
**kıdem döndürmüyordu, deneyim döndürüyordu** (`_cookXpDays`). Deneyim ise
huya bağlı:

| huy | `XpBp` | ekranda görünen |
|---|---:|---|
| `tecrubeli` | 0 | altmış gün çalışan kişi **"0 gün"** |
| `cirak` | 20000 (2×) | otuz gün çalışan kişi **"60 gün"** |

Personel kartı `"Seviye {seviye} ({gün} gün)"` yazıyor. Yani oyuncuya, altmış
gündür dükkânda olan birinin hiç çalışmadığı söyleniyordu. **Aynı oturumda
repliklerde düzelttiğim hatanın kod tarafındaki kardeşi:** simülasyonun
yalanladığı bir sayıyı ekrana yazmak.

Kıdem artık ayrı izleniyor (`_cookTenure` / `_salonTenure`), huydan bağımsız,
her çalışılan gün +1. İşten çıkarmadaki kaydırmaya da eklendi — XP'nin gittiği
her yere kıdem de gidiyor, yoksa çıkarılan kişinin kıdemi yerine geçene
yapışırdı.

`SaveVersion` 21 → 22, ve bu **az önce yazılan göç yolunun ilk gerçek
kullanımı**. Eski kayıtta kıdem yok; sıfırdan sayılmaya başlıyor. Uydurmak
(örneğin XP'den türetmek) tam da düzeltilen yalanı başka bir kılıkta geri
getirirdi.

### Test iki kez zayıf yazıldı

Birincisi **sonsuz döngüye** girdi: `while (gun < 12) { sim.Tick(); }` yazdım,
oysa gün ancak `OpenService` + `CloseDay` + `AdvanceToNextDay` ile dönüyor.

İkincisi **hiçbir şey ölçmüyordu**: yalnızca devralınan aşçıya bakıyordu ve
onun huyu yok, yani deneyimi de kıdemi kadar artıyor — `StaffDaysWorked`'i
yine XP'ye bağlayan bir gerileme orada **eşit** çıkar ve test sessizce
geçerdi. Şimdi test aday havuzlarında bir `tecrubeli` arıyor, işe alıyor, ve
*"elli günde bir `tecrubeli` aday çıkmadı — test ölçüm yapamadı"* diye ayrı
bir kol taşıyor.

Mutasyonla doğrulandı: `StaffDaysWorked` yine XP döndürünce test kırmızı
yanıyor.

### Ve ekranda görüldü — ama ilk bakılan kare yanlış kareydi

Kod düzeldi, test geçti, mutasyon kanıtladı. Sonra tur karesine baktım:
personel kartı **"Seviye 0 (0 gün)"** yazıyordu.

Bir an düzeltmenin tutmadığını sandım. Tutmuştu — **kare 1. günün sabahıydı**
ve orada "0 gün" doğru: kimse henüz bir gün çalışmamış.

Bunun asıl anlamı şu: **hata zaten o karede görünmezdi.** Kart aylarca "0 gün"
yazsa bile birinci günün karesi aynı şeyi gösterirdi. Yani bu hatayı yakalayan
bir kontrol, günün ilerisine bakmak zorunda.

Tur artık 5. günde personel ekranını açıyor ve ölçütü ekrandaki metin:

```
tamam : Kidem ilerliyor (5. gun, 4 gun)
tamam : Personel kartinda kidem gercek sayiyi gosteriyor
```

*Bir sayının doğru olduğunu görmek için doğru ana bakmak gerekiyor; yanlış an,
yanlış cevabı da doğru gösterir.*
