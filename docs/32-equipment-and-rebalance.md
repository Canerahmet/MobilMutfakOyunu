# 32 — Ekipman sistemi ve ekonominin yeniden dengelenmesi

10 Eylül 2026. Faz 0'ın açık kalan tek denge uyarısı kapandı: **makul oyuncuda para 5,5. haftada sorun olmaktan çıkıyordu, hedef sekizinci haftadan önce olmamasıydı.** [29-phase0-simulation.md](29-phase0-simulation.md) bunun bir denge sorunu değil eksik içerik olduğunu yazmıştı. Doğruydu, ama eksik olan yalnızca içerik değildi.

---

## 1. Yazılan şey

[27-time-model.md](27-time-model.md) **Karar D**'yi Mart'ta almıştı ama simülasyon onu hiç uygulamamıştı:

```
prepMs      yemeğin DUVAR SAATİ süresi, oyuncunun gördüğü sayı
cookBusyMs  prepMs × attendBp / 10000, aşçı havuzunu tüketen sayı
```

Simülasyon aşçıyı yemeğin **bütün duvar saati** boyunca meşgul tutuyordu. Yani fırınla ızgara arasında hiçbir fark yoktu, `station` alanı bir ikon seçicisinden ibaretti ve ekipmanın anlatacak hikâyesi yoktu.

Artık üç şey var:

| Kavram | Ne | Nerede |
|---|---|---|
| **İstasyon yuvası** | Bir istasyonun aynı anda kaç tabak aldığı | `Simulation._stationBusy` |
| **`attendBp`** | Duvar saatinin yüzde kaçının aşçının elinde geçtiği | `content/equipment.json` |
| **Ekipman kademesi** | Yuva ekler ya da `attendBp` düşürür, `prepMs`'e **dokunmaz** | `CommandKind.BuyEquipment` |

Sipariş, istasyon işlerine bölünüyor: ana yemek ızgaraya, içecek içecek istasyonuna. Aynı istasyona giden kalemler tek işte birleşiyor. İş, aynı anda kaç tabak pişireceğine üç sınırın küçüğüne göre karar veriyor:

```
take = min(grubun tabak sayısı, boş yuva, aşçının aynı anda bakabileceği tabak)
```

**Üçüncü sınır sonradan eklendi ve eklenmesi şarttı.** Onsuz dört kişilik bir grup dört yuvayı birden tutuyordu ama aşçı onlara sırayla bakıyordu; yuvalar boş boş dolu görünüyordu. Ölçüm bunu yakaladı: ekipman alan mutfağın memnuniyeti 87'den 81'e **düşüyordu**. Sınır `ceil(10000 / attendBp)` ve [27-time-model.md](27-time-model.md) §3.3 ile aynı sayıyı veriyor: kademe 4 zirvesinde 8,66 eş zamanlı tabak, 4 aşçı, yani aşçı başına 2,2.

### Ekipman merdiveni

`content/equipment.json` **üretilen dosya**; `tools/balance/export.py` yazıyor. Fiyatlar elle konmuyor, **gerekli olduğu kademenin kirasından** türetiliyor — kira zaten olgun hafta marj hedefinden çözüldüğü için ekonominin ölçeğini taşıyor.

<!-- ÜRETİLEN: ekipman -->
| İstasyon | `attendBp` | Kademe | Yuva | `attendBp` | Fiyat | Gerekli olduğu masa |
|---|---|---|---|---|---|---|
| Ocak | 3500 | t0 | 1 | 3500 | — | 4 |
|  |  | t1 | 2 | 3500 | 2.340 | 7 |
|  |  | t2 | 3 | 3500 | 3.480 | 10 |
|  |  | t3 | 4 | 2800 | 10.000 | 14 |
| Izgara | 5600 | t0 | 1 | 5600 | — | 4 |
|  |  | t1 | 2 | 5600 | 2.340 | 7 |
|  |  | t2 | 3 | 5600 | 3.480 | 10 |
|  |  | t3 | 4 | 3500 | 10.000 | 14 |
| Fırın | 2000 | t0 | 1 | 2000 | — | 4 |
|  |  | t1 | 2 | 2000 | 6.000 | 14 |
| Soğuk | 10000 | t0 | 1 | 10000 | — | 4 |
|  |  | t1 | 1 | 8000 | 3.480 | isteğe bağlı |
| İçecek | 10000 | t0 | 1 | 10000 | — | 4 |
|  |  | t1 | 1 | 6500 | 3.480 | isteğe bağlı |
| Tatlı | 8000 | t0 | 1 | 8000 | — | 4 |
|  |  | t1 | 1 | 6000 | 3.480 | isteğe bağlı |

Merdivenin tamamı **48.080 sikke**.
<!-- /ÜRETİLEN: ekipman -->

Son kademe 10.000 sikke — [12-economy.md](12-economy.md) §7'nin istediği 8.000–12.000 bandında. Merdivenin tamamı **48.080 sikke**; makul oyuncunun altmış günlük neti **42.627**. **Yani her şeyi alamıyor, seçmek zorunda** — ve zorunlu olanlar (yuva ekleyenler) 37.640, isteğe bağlı olanlar (asçıyı erken bırakanlar) 10.440. Hedef tam olarak buydu.

---

## 2. Bu değişikliğin açtığı ikinci sorun

Aşçı yemeğin tamamı boyunca değil yalnızca `attendBp` kadar meşgul kalınca **mutfak darboğaz olmaktan çıktı.** Bütün stratejilerin servis oranı yükseldi.

Ve bu, çok tanıdık bir hataya yol açtı: [06-plan-status.md](06-plan-status.md)'nun kayıtlı en pahalı hatası, kapalı form modelin talebin tamamının ağırlandığını varsayması ve kiraların o varsayımdan çözülmesiydi. Ölçülen gerçekleşme oranı %65 çıkmış, kiralar %35 fazla olduğu için düşürülmüştü.

Mutfak düzelince **aynı ölçüm %93,35 verdi.** Yani kiralar artık fazla değil, **eksikti**.

### Tek atışlık ölçüm neden yanlış

O oranı doğrudan uygulayıp kiraları yeniden çözdürdüm. Sonuç felaketti:

| Strateji | Kira %65 ile | Kira %93 ile |
|---|---|---|
| makul | +32.398 | **-33.958** |
| plancı | +10.526 | +1.084, 32. günde borç |
| genişlemeyen | +12.537 | +11.155 |

Genişlemek tuzağa dönüştü. Sebebi basit ve önemli:

> **Oran parametreleri belirliyor, parametreler oranı belirliyor.** %93,35 ölçümü ESKİ ucuz kiralarla alınmıştı. Yeni kiralar uygulanınca aynı strateji genişleyemedi ve oran çöktü.

Bu bir sabit nokta problemi, tek atışla çözülmez.

### `tools/balance/calibrate.py`

Bunun için yazıldı. Bir aday gerçekleşme oranı için bütün zinciri koşuyor:

```
solve.py  →  model.py  →  export.py  →  simülasyon  →  tasarım hedefleri
```

ve sonucu puanlıyor. Hedefler mutlak sayı değil **sıralama**: pasif oyuncu batmalı, pervasız genişleyen batmalı, makul oyuncu kazanmalı, büyümek 1,8–4,0 kat ödüllendirmeli, plancı takvimi tutturabilmeli, para sekizinci haftadan önce önemsizleşmemeli.

```
 6500  kira [650, 1550, 2250, 4000]   ceza 11
 7000  kira [850, 1950, 2900, 5000]   ceza  0   TEMİZ
 7500  kira [1050, 2350, 3600, 5250]  ceza 14
 8000  kira [1250, 2750, 4250, 6250]  ceza 14
 8500  kira [1400, 3150, 4900, 7300]  ceza 24
 9000  kira [1600, 3550, 5600, 8350]  ceza 36
 9335  kira [1700, 3800, 6050, 9050]  ceza 36
```

**Sabit nokta 7000.** Ne eski %65, ne ölçülen %93. Aradaki her şey ölçülerek elendi.

Yeni parametreler: kira **850 / 1950 / 2900 / 5000**, kapasite ölçeği 1,00 (aşçı 30, garson 26, bulaşıkçı 48, kasiyer 70), patron katkısı 1,3 iş-günü, genişleme bedelleri 0 / 2500 / 4500 / 8000 — yani kapasite ve genişleme değişmedi, **yalnızca kira %31 arttı.**

---

## 3. Ölçünün kendisi de yanlışmış

"Para sorun olmaktan çıktı" ölçüsü şuydu: *kasa, en pahalı genişlemenin üç katını aşarsa.*

Ekipman yazılmadan önce doğruydu. Sonrasında iki kez yanlış oldu:

1. Ekipmanı hiç saymıyordu. Oyuncunun biriktirdiği bir şey vardı ama ölçü "önemsizleşti" diyordu.
2. Ekipman eklendi ama "üç kat" keyfi kaldı: elde 30.000 varken 20.000'lik iki ekipman duruyorsa para hâlâ önemli.

Doğru tanım: **kasa, geriye kalan bütün satın alınabilirleri tek seferde ödeyebiliyorsa** biriktirecek bir şey kalmamış demektir. `Simulation.RemainingPurchaseCost()` bunu veriyor: kalan genişleme kademeleri artı kalan ekipman basamakları.

Bu tanımla ölçüm **hiçbir stratejide tetiklenmiyor.** Altmış gün boyunca her zaman alınacak bir şey kalıyor.

---

## 4. Sonuç

| strateji | son kasa | itibar | masa | servis | kayıp | ilk borç | önemsiz |
|---|---:|---:|---:|---:|---:|---:|---:|
| pasif | -6.638 | 0,0 | 4 | 13 | 0 | **35** | — |
| sadece_hal | 4.753 | 66,0 | 4 | 976 | 58 | — | — |
| **makul** | **32.398** | 98,4 | 7 | 2.161 | 35 | — | — |
| genişlemeyen | 12.537 | 90,4 | 4 | 1.122 | 41 | — | — |
| atılgan | -70.952 | 0,0 | 14 | 274 | 7 | **7** | — |
| plancı | 8.892 | 75,2 | 14 | 2.966 | 30 | — | — |
| yüksek_fiyat | 7.009 | 0,0 | 4 | 510 | 6 | — | — |
| fazla_kadro | 1.838 | 80,5 | 4 | 1.118 | 49 | **56** | — |

**Bütün tasarım hedefleri tutuyor, ceza sıfır:**

- İhmalin bedeli var: pasif oyuncu 35. günde batıyor.
- Pervasız büyüme cezalandırılıyor: atılgan 7. günde borca düşüyor.
- Büyümek **2,58 kat** ödüllendiriyor (12.537 → 32.398), [12-economy.md](12-economy.md)'nin "üç kat, on bir değil" bandında.
- Plancı takvimi artık **tutturulabiliyor**: 14 masa, %94 servis, 2.966 kişi. Ekipman öncesinde 7,4 masada tıkanıyordu.
- Fazla kadro 56. günde batırıyor.
- Yüksek fiyat hayatta kalıyor ama iyi oyunun beşte birini kazanıyor ve itibarı sıfır.
- **Para hiçbir stratejide, hiçbir haftada önemsizleşmiyor.**

Model tutarlılık kontrolleri 20/20, testler 106/106.

---

## 5. Bu turda ölçümün yakaladığı üç hata

Hiçbiri koddan bakarak görünmüyordu; üçü de sayıdan çıktı.

| Hata | Nasıl göründü | Neden önemliydi |
|---|---|---|
| **İş tek yuvada sıraya diziliyordu** | Ekipman almak hiçbir şeyi değiştirmiyordu | Yuva sayısı süreyi etkilemiyorsa ekipman satmanın karşılığı yok |
| **İş, aşçının bakamayacağı kadar yuva tutuyordu** | Ekipman alan mutfağın memnuniyeti 87 → 81 **düşüyordu** | Yükseltme oyuncuyu cezalandırıyordu |
| **Ortalama memnuniyet yanlış ölçüydü** | Ekipmanlı mutfak daha çok grup ağırlıyor ama "memnuniyeti düşük" görünüyordu | O ortalama yalnızca **ağırlanan** müşteriler üzerinden; dar mutfak zor vakayı hiç servis etmiyor ve ortalaması yüksek çıkıyor |

Üçüncüsü en sinsisi: metrik yanlış olduğu için doğru davranan sistem yanlış görünüyordu. Doğru ölçü servis edilen grup sayısı ve ciro.

Ayrıca bir test yalan söylüyormuş: `NewSim(cooks: 3, salon: 4)` diyordu ama dört masada kadro tavanı üç, yani dört salon işesi sessizce reddediliyordu ve "güçlü kadro" aslında yalnızca fazladan iki aşçı demekti. Test artık kurulumu açıkça doğruluyor.

---

## 7. Depo: soğuk hava ve `spoilDays`

Depo odasının işi yoktu ve [31-rooms-and-camera.md](31-rooms-and-camera.md) "iş verilemezse yerleşimden çıkarılmalı" diyordu. İş bulundu ve zaten oradaydı.

**`spoilDays` içerikte vardı ama simülasyon onu hiç okumuyordu.** Bozulabilir malzemelerin raf ömürleri 1 ile 45 gün arasında değişiyor. Kod ise şuydu:

```csharp
if (_content.Ingredients[i].Perishable) _stockGrams[i] = 0;
```

Yirmi gün dayanan soğan ile bir gün dayanan kıyma aynı gece çöpe gidiyordu.

Bu bir hata değil, **tasarlanmış temel**: [12-economy.md](12-economy.md) §3 "bozulabilir malzeme günü kapatınca değerinin tamamını kaybeder" diyor. O yüzden soğuk hava bir *düzeltme* değil, o temeli **değiştiren yükseltme**.

### Merdiven

<!-- ÜRETİLEN: depo -->
| Kademe | `keepBp` | Kurtardığı malzeme | Fiyat |
|---|---:|---:|---:|
| t0 | 0 | 0 / 36 | — |
| t1 | 2500 | 13 / 36 | 2.340 |
| t2 | 10000 | 33 / 36 | 3.480 |

Merdivenin tamamı **5.820 sikke**. Bozulabilir malzeme **36** kalem.

Bir kademe bir malzemeyi ancak ömrünü **2 güne** çıkarabiliyorsa kurtarıyor: ömür 1 ile ömür 0 aynı gece çöpe gidiyor.
<!-- /ÜRETİLEN: depo -->

**İki basamak, üç değil.** Merdiven bir süre üç basamaklıydı ve
üçüncüsü **hiçbir fiyatta çalışmadı**: 8.000 sikkede hiç satın
alınmıyordu (temkinli kural 32.000 kasa istiyor, makul oyuncu 25.000'de
zirve yapıyor), 4.500'e indirilince alınıyor ve **−3.700 kaybettiriyor**.

Sebep fiyat değil **takvim**. İkinci basamaktan sonra geriye yılda ~4.700
sikkelik zayiat kalıyor ve üçüncü basamak onun bir kısmını kurtarıyor;
altmış günlük bir kampanyada hiçbir fiyat bunu ödetemez. Daha ucuza
indirmek de çözüm değil — o zaman bir **karar** olmaktan çıkıp otomatik
bir alıma dönüşüyor.

Basamakları eşitlemek için birinci basamak da düşürülmüştü (keepBp 1500)
ve **daha kötü** oldu: makul oyuncuya kazandırdığı +2.720'den −234'e indi.
Birinci basamak zaten iyi ayarlıymış; bozuk olan yalnızca üst ikisiydi.

Yaş malzeme başına tutuluyor ve alım yapınca **ağırlıklı ortalama** alınıyor. Basit "alınca sıfırla" kuralı bir istismar açıyordu: her gün bir gram alıp saati sonsuza kadar sıfırda tutabiliyordun.

### Neden bu iş, dekordan fazlası

Menü genişliği **iki yönlü** bir eksen. Bozulabilir her şey gece öldüğü için menüde duran her yemek her gün yeniden stoklanmalı ve arta kalan çöpe gidiyor — yani geniş menü pahalı. Ama dar menü de bedava değil: açık olan ana yemeklerden menüde olmayanı müşteri **soruyor** ve bulamayınca memnuniyeti düşüyor (`Simulation.Awaited`).

O ikinci yarı uzun süre **yoktu** ve bu, oyunun en derin denge hatasıydı: ceza yalnızca *ekipmanı olmayan* yemekler için işliyordu, menüden çıkarılan yemek hiçbir zaman sorulmuş sayılmıyordu. Ölçüldü: menüde tek ana yemek tutan oyuncu makul oyuncuyu fast food'da %12, Türk mutfağında %38 geçiyordu. Yani dar menü **kesin baskın stratejiydi**, soğuk havanın ikinci ödülü değersizdi ve otuz iki yemeklik envanterin var olma sebebi ortadan kalkmıştı.

Ceza **oranlı**: açık ana yemeklerin yarısı menüde değilse ceza yarım, hiçbiri yoksa tam. Mutlak sayıyla denendi ve aşırıydı — menüsünü makul ölçüde daraltan oyuncu ile tek yemek tutanı aynı kefeye koyuyor, ikincisini dokuzuncu günde iflas ettiriyordu. `tek_yemek` stratejisi denge aracında bir **kabul testi** olarak duruyor.

Soğuk hava o kısıtı gevşetiyor, yani **menü genişliği satın aldırıyor** — ve [09-content-inventory.md](09-content-inventory.md)'nin otuz iki yemeğinin var olma sebebi oluyor. İçerik envanteri, hal aşaması ve ekipman merdiveni bu parça olmadan birbirinden kopuktu.

Ölçülen etkisi: `plancı` stratejisi 8.892'den **16.880**'e çıktı, itibarı 100'e vurdu ve 2.966 yerine 3.114 kişi ağırladı.

### Depo odası

Depo mutfağın sağ kenarına yapışık **13,4 m²**lik küçük bir arka oda; içinde soğuk hava odası ve kuru raf var, ikisi malzeme listesinin bozulabilir / bozulmaz ayrımına karşılık geliyor. Mutfaktaki buzdolabı kaldırıldı — soğuk saklama artık deponun işi, ikisini birden göstermek yalan olurdu.

---

## 8. Açık kalanlar
- **Depo hâlâ dokunma sınırına yakın:** genel görünümde 51 dp, asgari 48. Daha da küçültülemez.
- **Ekipman arayüzü yok.** [16-screens-and-tutorial.md](16-screens-and-tutorial.md) ekran 14 "Yükseltme ve ekipman" diyor ama içeriği yazılmadı. Oda görünümünde mutfağa dokunmak buraya açılmalı.
- ~~İkinci mutfak (Türk) ölçülmedi.~~ **Ölçüldü ve kırık çıktı**; sebep ekipman değil menü gruplarıydı. Bkz. [33-second-cuisine.md](33-second-cuisine.md). Düzeltmeden sonra aynı ekipman merdiveni ayar gerektirmeden çalışıyor.
- **`attendBp` ekipman kademesiyle düşüyor ama personel deneyimiyle düşmüyor.** [27-time-model.md](27-time-model.md) §595 bunu açık bırakmıştı, hâlâ açık.
