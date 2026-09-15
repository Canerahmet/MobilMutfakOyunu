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

| deneme | gerekçe | sonuç |
|---|---|---:|
| yığın bir eşiği geçmeden yıkamasın | uzman tek tabak için lavaboya gitmesin | 229 → **293** |
| yıkayacak şey yokken salona dönsün | de-pooling'i kapat | 197 → **216** |

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

Ölçüldü (32 tohum): `makul` 255 → **240**, `imzaci` 193 → **186**, `planci` ve
`bulasikci` değişmedi. Kimseye zarar yok.

RimWorld'ün **yangın** davranışı da tam bu kalıp: nadir, ağır, kapsamlı bir
koşul normal önceliği geçer.

---

## 4. Kombo: cevap self servisle tersine dönmüş

[45](45-tasarim-incelemesi.md) §18 kombo ekseninin "var olmayan bir stratejiyi
koruduğunu" yazmıştı: `zirvede_kapat` ile `imzaci` **birebir aynı 2016 grubu**
ağırlıyordu, çünkü kombonun mutfak yükü ısırmıyordu.

O ölçüm **self servisten önceydi.** Fast food'un salon yükü yarıya inince
darboğaz mutfağa geçti. Yeniden ölçüldü:

| strateji | son kasa | servis | kombo% | tabaksız |
|---|---:|---:|---:|---:|
| makul (kombo yok) | 22.492 | 2617 | %0,0 | 846 |
| imzacı (hep açık) | 22.163 | 2559 | %18,1 | 1018 |
| **zirvede_kapat** | **23.474** | 2564 | %16,8 | **792** |

Kombo artık **58 grup kaybettiriyor** ve zirvede kapatmak hep açık tutmayı
**+1.311** geçiyor. Tasarımın yazılı vaadi ("ortalama fişi yükseltir ama
mutfağı yorar") ilk kez doğru.

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

On iki huy × iki dil. Aday kartının en üstünde, **birinci** huydan geliyor —
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
