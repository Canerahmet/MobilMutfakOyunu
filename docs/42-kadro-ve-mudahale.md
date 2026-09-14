# 42 — Kadro kararı ve müdahalenin değeri

*12 Eylül 2026.* Denge aracı bütün değişikliklerden sonra yeniden koşturuldu
(12 tohum, 60 gün). İki bulgu çıktı; ikincisi beklenmiyordu.

---

## 1. Eski açık madde kapandı: pasif oyuncu batıyor

| soru | cevap |
|---|---|
| Hiç müdahale etmeyen oyuncu ne oluyor | koşuların **%100'ü** borca düşüyor, ortalama **56. gün** |
| Sürekli piyasa üstü fiyat | %75 batıyor |
| Kombo şişirme | %67 batıyor |
| Makul oyuncu | 19.155 kasa, 79 itibar |
| Plancı | 24.255 kasa, 95 itibar |

`project-simulation` belleğinde yıllardır duran *"pasif oyuncu hâlâ batmıyor"*
notu artık geçersiz.

---

## 2. Müdahale nötr görünüyordu — çünkü kurtarılacak bir şey yoktu

Müdahaleci bot, müdahale etmeyenle **aynı sayıda kişi ağırlıyordu** (2021) ve
biraz daha az kazanıyordu. İlk şüphe doğru yerdeydi: *"reddedilen bir bot, bot
değildir"* — bu proje fiyat tavanı geldiğinde bir botun sessizce kopyaya
dönüştüğünü görmüştü. O yüzden önce **sayıldı**:

```
=== mudahale (strateji basina) ===
  mudahaleci             2880 gecti /   2880 denendi
  baskili_mudahale       2880 gecti /   2880 denendi
```

*(Sayaçlar bir süre **statikti ve hiç sıfırlanmıyordu**: iki kolun toplamı tek
satırda basılıyor ve sayaçların var oluş sebebi — kol başına "müdahale gerçekten
oldu mu" — okunamıyordu. Artık strateji başına.)*

Yani mekanik çalışıyor. Sorun tavandaydı: **iyi yönetilen bir restoranda günde
~0,3 grup kaçıyor**, yani müdahalenin kurtaracağı bir şey yok.

Bunu ayırmak için **baskı çifti** yazıldı: aynı oyuncunun bir garson eksik
çalışan iki kopyası, aralarındaki tek fark müdahale.

| bot | son kasa | ağırlanan | masadan kızgın |
|---|---:|---:|---:|
| makul (tam kadro) | 19.155 | 2.021 | 2 |
| baskılı (1 eksik) | 22.153 | 1.960 | 13 |
| **baskılı + müdahale** | **22.513** | 1.967 | 11 |

**Müdahale baskı altında kazandırıyor** (+360 sikke) ve masadan kızgın ayrılan
grup sayısını düşürüyor (13 → 11). Yani mekanik zayıf değil; rahat bir
restoranda ölçülemiyor. Oyunun vaadi de tam buydu: *"patronsun,
yetişemediğinde sen müdahale edersin".*

**Sayılar yeniden ölçüldü ve küçüldü** (eskiden +861). Sebebi bilinen bir
düzeltme: akşam verilen kadro kararı artık `RequiredCrewTomorrow()` ile
**yarının** gün tipine bakıyor. Eskiden biten günün tipine bakıyordu, yani
cuma akşamı hafta içi kadrosu kurulup cumartesi zirvesine eksik giriliyordu —
müdahalenin kurtardığı şeyin bir kısmı aslında o hatanın yarattığı baskıydı.
Ölçüm, ölçtüğü şeyi *iyileştirince* küçülen bir sayı: iyi haber.

**"Kayıp" sütunu artık MASADAN KIZGIN AYRILANI sayıyor**, kapıdan döneni değil.
İkisi tek sayıya katlanıyordu; biri servis sorunu, öteki kapasite sorunu.

---

## 3. Beklenmeyen bulgu: kadro tavsiyesinin adı yanlıştı

Yukarıdaki tabloda asıl çarpıcı satır şu: **bir garson eksik çalışmak yaklaşık
3.000 sikke daha kazandırıyor** (ilk ölçümde 5.300 idi; `RequiredCrewTomorrow`
düzeltmesinden sonra fark küçüldü ama yön değişmedi). Sebebi arayınca `RequiredCrewToday()` çıktı — adında "bugün"
yazıyor ama her gün **hafta sonu** çarpanıyla hesaplıyordu:

```csharp
int peak = DemandModel.CustomersPerDay(..., _economy.WeekendMultiplierBp);
```

Ücret **her gün** ödeniyor, zirve ise haftada iki gün. Arayüz "bugün 3 kişi
gerek" yazıyor, oyuncu tutuyor ve neden para kaybettiğini hiçbir yerden
öğrenemiyordu.

**İki düzeltme:**

1. `RequiredCrewToday()` artık gerçekten bugünü ölçüyor (hafta içi / hafta sonu
   ayrımıyla). Zirve ayrı bir metot: `RequiredCrewPeak()`.
2. Ekran artık **"Herkese yetişmek için: 1 + 3 · hafta sonu: 1 + 4"** yazıyor.
   Sayı bir kapasite hesabı, kâr için en iyi sayı değil — ve bu bir **karar**:
   *"gereken"* demek kararı gizliyordu.

Botun işten çıkarma kuralına da **ısrar** eklendi: eksikse hemen alıyor, fazlaysa
üç gün üst üste fazla olmasını bekliyor. İşten çıkarma deneyimi sıfırlıyor; cuma
tutup pazartesi kovan bir bot mekaniğin bedelini ödeyip kararını hiç vermiyordu.

**Fark kapanmadı, küçüldü** (kadro 4,6 → 4,2 kişi, maaş 26.284 → 25.572).

---

## 4. "Puanı kaybediyor" — bunu yazdım ve ölçmemiştim

Yukarıdaki bölümün ilk hâli şöyle bitiyordu: *"eksik kadro parayı kazanıyor,
puanı kaybediyor."* Kulağa doğru geliyordu ve **ölçülmemişti**. Bu projenin
kuralı açık: ölçülmemiş bir cümle, belgede duran bir tahmindir.

Yıl sonu puanı (docs/08, yedi eksen) denge aracına eklendi. Ölçüm:

| strateji | puan | varlık | itibar | müdavim | ekip | mekân | sağlam | imza |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| pasif | 9 | 0 | 0 | 0 | 27 | 28 | 0 | 9 |
| makul | **65** | 25 | 79 | 86 | **73** | 55 | 100 | 38 |
| baskılı | **64** | **29** | 78 | 86 | **64** | 55 | 100 | 38 |
| baskılı + müdahale | 64 | 30 | 79 | 85 | 64 | 55 | 100 | 38 |
| plancı | **77** | 32 | 95 | 86 | 72 | 86 | 100 | 73 |

**Cümlenin yarısı doğruydu:** eksik kadro *varlık* ekseninde kazanıyor (29 / 25)
ve *ekip* ekseninde kaybediyor (64 / 73). Ama **itibar aynı** (78 / 79) — ben
düşeceğini yazmıştım — ve **toplam neredeyse eşit: 64'e 65.**

Doğru cümle şu: eksik kadro bir **takas**, bir hata değil. Oyuncu parayı ve
varlık puanını kazanıyor, ekip puanını ve 65 kişiyi kaybediyor; terazi bir
puanla dengede. Gerilim gerçek, ama "yanlış oynuyorsun" demiyor — *"neyi
önemsiyorsun"* diye soruyor. Bir yönetim oyununda istenen tam da bu.

Puan tablosu artık her denge koşusunda basılıyor: bir sonraki "şu strateji
kazandırıyor" cümlesi, kasaya bakıp puanı unutamayacak.

---

## 5. Tablonun açtığı ikinci soru: "imza" ekseni imzayı ölçmüyor

Aynı tabloda şu satırlar yan yana duruyor:

| strateji | imza ekseni |
|---|---:|
| makul (komboyu hiç açmıyor) | 38 |
| **imzacı (her sabah komboyu açıyor)** | **38** |
| plancı (çok genişliyor) | 73 |
| atılgan (çok genişliyor, batıyor) | 69 |

[docs/08](08-oyun-sonu.md) mutfağa özel eksen için *"bu eksen, imza mekaniğini
**doğrudan** ödüllendirir"* diyor. Fast food'un imza mekaniği **kombo**; ekseni
ise `peakCovers` — **en yüksek günlük kuver**. Ölçüm gösteriyor ki eksen komboyu
değil **genişlemeyi** izliyor: komboyu açan bot ile hiç açmayan bot aynı puanı
alıyor, en yüksek puanlar ise en çok masa açanlarda.

Türk mutfağında aynı sorun yok: ekseni `creditCollected`, yani veresiye tahsilat
oranı — mekaniğin kendisi.

Bu bir **tasarım kararı** bekliyor ve ikisi de savunulabilir:

- **Ekseni bırak, cümleyi düzelt.** "En yüksek kuver" fast food kimliğinin
  kendisi (hız ve hacim); yalnızca docs/08'in "doğrudan ödüllendirir" cümlesi
  fazla iddialı.
- **Ekseni değiştir.** Ana yemek siparişlerinin yüzde kaçı kombo oldu — o zaman
  eksen bir *karar* ölçer (kombo mutfağı da yorar, zirvede kapatmak gerekebilir).

### Karar (13 Eylül 2026): eksen değişti

**İkinci seçenek seçildi.** Gerekçe ölçümde: `peakCovers` ile "Mekân" ekseni
aynı şeyi izliyordu — plancı bot Mekân'da 86, imza ekseninde 73 alıyordu, yani
yedi eksenden ikisi tek bir davranışı (genişleme) iki kez ödüllendiriyordu.
Türk mutfağının ekseni ise bir *karar* ölçüyor. Asimetri tasarımın kendisinde
değil, fast food'un ekseninde.

Hedef **ölçümden** geldi, uydurulmadı. Önce ham oran basıldı (12 tohum, 60 gün):

| bot | kombo payı |
|---|---:|
| imzacı (her sabah açıyor) | **%17,4** |
| kombo_şişmesi (açıyor ama batıyor) | %5,3 |
| diğer on yedi bot | %0,0 |

Hedef **%15**: "çoğu gün aç" tam puan veriyor. Kombo mutfak yükünü de
artırdığı için zirvede kapatmak meşru bir oyun ve eksen onu cezalandırmamalı.

Sonuç:

| bot | imza (önce → sonra) | toplam (önce → sonra) |
|---|---|---|
| imzacı | 38 → **100** | 65 → **74** |
| makul | 38 → 0 | 65 → 60 |
| plancı | 73 → 0 | 77 → 67 |

Kombo kullanmak artık **14 puan** değerinde ve eksen genişlemeyi hiç izlemiyor.
İmzacı her iki mutfakta da en yüksek puanlı strateji oldu (74) — "makul oyna ve
mutfağının imzasını kullan" en iyi oyun olmalıydı, artık öyle.

`peakCovers` dalı **silindi**: hiçbir mutfak kullanmıyor ve bu projenin kuralı
açık — çağrı yerleri kalmayan bir dal bağlanmaz, silinir.

### Ve eksen değişince görünmeyen bir gider ortaya çıktı

Türk mutfağını `--strateji imzaci` ile koşunca gelir tablosu **tutmadı: 134
sikke**. Sebebi: veresiye açılırken ikram edilen çayın bedeli kasadan çıkıyor
ve **hiçbir gider kalemine yazılmıyordu.** Veresiye açan bir oyuncunun
mutabakatı asla kapanamazdı.

Görünmemesinin sebebi de öğretici: Türk mutfağı ile `imzaci` botu **hiç birlikte
koşulmamıştı**, çünkü `--strateji` bayrağı kabul edilip hiç okunmuyordu
([docs/43](43-inceleme-ve-olcum.md) §4). Bir bayrağı düzeltmek, bir sızıntıyı
ortaya çıkardı.

Çay artık kendi sütununda ve mutabakat sıfır. Görünür olması da doğru:
**veresiye bedava değil** ve bedelinin hiçbir yerde olmaması mekaniği
olduğundan ucuz gösteriyordu.
