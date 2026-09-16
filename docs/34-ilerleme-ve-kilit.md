# 34 — İlerleme: kilit, karmaşıklık, mevsim ve "soran müşteri"

10 Eylül 2026. Bu belge, bir günde bulunan **beş ölü içerik** ve onları canlandırırken açılan iki deliği anlatıyor.

---

## 1. Önce şu soru: içeriğin vaat ettiği her şeyin karşılığı var mı?

Aynı gün içinde dört ayrı hata bulundu ve dördü de tek bir sınıftandı:

| Ne vaat ediliyordu | Kod ne yapıyordu |
|---|---|
| `spoilDays` — 44 malzemenin raf ömrü, 1 ile 45 gün | Hepsini her gece siliyordu |
| `tatli` grubu — 9 tatlı yemeği | `PickOrder` tatlıya hiç bakmıyordu |
| Yemek grupları mutfağa özel | Fast food sözlüğü sabit kodlanmıştı |
| `attendBp` — [27](27-zaman-modeli.md) Karar D | Mutfak hiç uygulamamıştı |

Hiçbiri derlemeyi bozmuyor, hiçbiri testi kırmıyor. Hepsi **sessizce ölü.** Bu sınıf hata tahminle bulunmaz, taranır.

`tools/audit_content.py` bunun için yazıldı. Üç soru soruyor:

1. `content/*.json` içinde olup hiçbir DTO'nun bağlamadığı anahtar var mı?
2. Çekirdek tipinde olup **hiçbir yerde** okunmayan özellik var mı?
3. [13-veri-semalari.md](13-veri-semalari.md)'de olup üretilen içerikte olmayan alan var mı?

İkinci soru dikkatli sorulmalı: bir alanın *çekirdekte* okunmaması tek başına hata değil — dilim süreleri çekirdek dışında `TimingConfig`'e veriliyor, kapasiteler yükleyicide doğrulanıyor. Asıl bulgu **hiçbir yerde** okunmayan alan.

### İlk koşuşta bulduğu iki tam sistem

**Mevsim fiyatları.** 77 malzemenin hepsinde dört mevsimlik çarpan var ve **35'inin gerçek oynaması** var: domates yazın %14 ucuz, kışın %20 pahalı. Simülasyon hep taban fiyatı ödüyordu.

**Malzeme kalitesi.** Üç kademe (`dusuk` / `standart` / `yuksek`), hem fiyatı hem memnuniyeti etkiliyor. Hiç okunmuyordu; **§9'da yazıldı.**

Ayrıca `seasonDays` ve `unlockSeason` **DTO'ya bağlanmış ama çekirdeğe hiç ulaşmıyor** — denetimin iki kontrolü arasından kaçan üçüncü bir sınıf. Denetleyici bunu göremiyor; bir sonraki turda eklenmeli.

---

## 2. Mevsim uygulandı

Kampanya 60 gün, mevsim 15 gün: ilkbahar, yaz, sonbahar, kış. Halden alınan malzemenin fiyatı mevsime göre değişiyor.

**Tek başına bu bir karar değil, sadece bir gider oynaması.** Karar olması için ucuzken alıp saklayabilmek gerekiyor — ve o tam olarak [32](32-ekipman-ve-yeniden-denge.md) §7'de yazılan **soğuk hava** merdiveni. İki parça birbirini tamamlıyor:

```
sonbaharda soğan %10 ucuz  +  soğuk oda (raf ömrünün %60'ı)
      = kışa kadar dayanan ucuz stok
```

Denge aracında `plancı` stratejisi bunu kullanınca **%9 kazandı** (17.722 → 19.327). Yani mevsim artık dekor değil.

Yapısal bir yan etki de var ve kasıtlı bırakıldı: kış hem **en pahalı** mevsim hem de kampanyanın **en yoğun** dönemi. Maliyet eğrisi sona doğru yükseliyor.

---

## 3. Yemek kilidi: takvimden başarıya

### Durum neydi

Yemekler zaten ilk günden açık değildi: 32 yemeğin **6'sı** (2 ana yemek) gün 1'de açık, gerisi 3. günden 57. güne teker teker geliyordu.

Ama açılma bir **takvimdi**. `unlockDay` geldiği gün yemek açılıyordu; oyuncu bunun için hiçbir şey yapmıyordu.

### Ne oldu

Kilit artık üç şart birden istiyor:

| Şart | Ne anlama geliyor |
|---|---|
| `unlockDay` | Tempo tabanı — en erken ne zaman ([09](09-icerik-envanteri.md)) |
| `unlockReputationCenti` | **Kazanılan** şey |
| `requiresStationTier` | **Satın alınan** şey |

Üçüncüsü önemli: **ekipman almak artık menü açıyor.** [32](32-ekipman-ve-yeniden-denge.md)'de yazılan istasyon merdiveni yalnızca hız satmıyordu artık; içerik satıyor.

### `complexity` canlandı — ama ödül olarak değil

`complexity` alanı (1..3) içerikte vardı ve hiçbir yerde okunmuyordu. Veri zaten anlamlıydı: karmaşıklık 3 yemekler karmaşıklık 1'in **iki katı fiyatlı ve iki katı hazırlama süreli**.

İlk uygulama memnuniyeti karmaşıklığa göre **iki yönde** büyütüyordu. Ölçüm hemen yakaladı:

> Türk menüsünün 17'si karmaşıklık 3. Hiçbir şey yapmayan `sadece_hal` oyuncusu 5.094'ten **33.488**'e fırladı.

Sebep basit: müşterilerin çoğu zaten memnun, yani büyüteç pratikte tek yönlü çalıştı ve itibarı şişirdi.

**Ödül zaten fiyatta.** Karmaşıklık artık yalnızca **risk**: usta işi yemeği geç götürürsen müşteri daha çok kızıyor, iyi götürürsen fazladan ödül yok.

---

## 4. Kilit sisteminin açtığı delik

Kilit yazılınca ölçüm ikinci bir sorun gösterdi:

| | kilit öncesi | kilit sonrası |
|---|---:|---:|
| `sadece_hal` (hiçbir şey yapmayan) | 3.386 | **18.583** |
| `fazla_kadro` | 1.921 | **14.403** |

**Kilitli yemek = stoklanmayan yemek = masrafsız yemek.** Menüyü daraltmanın kazancını, menüyü hiç yönetmeyen oyuncu bedavaya aldı. Tasarlanmış bir gerilim tersine döndü.

### Çözüm: müşteri gelip soruyor

> "Yeni yemek kilitleri açılınca müşteriler onu gelip sorsun; böylece gerekli masrafı yapmazsa müşteri memnuniyeti düşer."

Günü ve itibarı gelmiş ama **ekipmanı alınmamış** bir ana yemek varsa, müşteri onu soruyor. Bulamayınca memnuniyeti düşüyor.

İki ayrıntı önemli:

- **Yalnızca oyuncunun kapatabileceği eksik sorulur.** Takvimin daha getirmediği yemek sorulmuyor; o haksızlık olurdu.
- **Ceza bekleyen yemek sayısıyla ölçekleniyor.** Bir yemeği atlamak küçük bir eksik; sekizini atlamak ihmal, ve mahalle bunu konuşuyor.

| | kilit sonrası | soran müşteri sonrası |
|---|---:|---:|
| `sadece_hal` | 18.583 | **14.424** |
| `fazla_kadro` | 14.403 | **5.106** |

Deliği kapattı. Ve mekanik, ekipman satın almayı soyut bir hız kazancından **masadaki hayal kırıklığına** çevirdiği için [02-tasarim-onerisi.md](02-tasarim-onerisi.md)'nin "görünür büyüme" ilkesiyle örtüşüyor.

---

## 5. Kuralı yine tek mutfağa göre yazdım

Kilit kuralı önce mutlaktı: *karmaşıklık 3 → istasyon kademesi 2.*

Fast food'da 32 yemeğin **2'si** karmaşıklık 3. Türk lokantasında **17'si**. Yani kural, Türk menüsünün yarısını bir anda kilitledi ve ölçüm bunu gösterdi: `sadece_hal` iyi oyunun **%89'unu** kazanıyordu, çünkü kilitli menü ucuz menü demek.

Kural artık **mutfağın kendi dağılımına** göre: yemekler karmaşıklık ve fiyata göre sıralanıp üçe bölünüyor — alt %45 ekipman istemez, sonraki %35 kademe 1, üst %20 kademe 2. İki mutfak da 17 / 9 / 6.

Bu, [33-ikinci-mutfak.md](33-ikinci-mutfak.md)'ün dersinin tekrarı ve o belgeyi yazan kişi tarafından tekrarlandı. `tools/balance/calibrate.py` artık **iki mutfağı da** koşuyor; tek mutfakla kalibre etmek bu yüzden artık mümkün değil.

---

## 6. Bu turun bıraktığı durum

Dört yeni sistem birlikte ekonomiyi **zorlaştırdı**: mevsim maliyeti, kilit şartı, karmaşıklık riski ve soran müşteri. Kalibrasyon iki mutfakla yeniden çözüldü ve **gerçekleşme oranı 6.500'e döndü**, kiralar 650 / 1.550 / 2.250 / 4.000.

Ara bir noktada denge marjinaldi: hiçbir şey yapmayan oyuncu bazı ayarlarda tam planı uygulayanı geçiyordu. §7'deki itibar tabanı bunu da kapattı.

**Sonuç, iki mutfakta da:**

| strateji | fast food | Türk | boşaldı |
|---|---:|---:|---:|
| hiçbir şey yapmayan | −4.943 | −4.722 | **5. gün** |
| sadece malzeme alan | 12.311 | 14.920 | 32 / 36 |
| **iyi oyuncu** | **24.876** (itibar 91,0) | **26.211** | hiç |
| genişlemeyen | 11.519 | 12.859 | hiç |
| pervasız genişleyen | −51.604 (7. gün borç) | −5.433 (23. gün) | 8 / 11 |
| **plancı** | **17.501** (itibar **100,0**) | **20.419** (itibar **87,5**) | hiç / 54 |
| yüksek fiyat | 3.790 | 3.145 | 9 / 6 |
| fazla kadro | 7.947 | 13.110 | 52 / hiç |

**Kalibrasyon cezası SIFIR, iki mutfakta da.** Bütün tasarım hedefleri geçiyor:

- İhmalin bedeli var: hiçbir şey yapmayanın dükkânı **5. günde boşalıyor**, 42. günde batıyor
- Kötü yöneten **zor da olsa geçiyor**: 32. güne kadar sürünüyor, batmıyor
- Pervasız büyüme 7. günde borca düşürüyor
- Büyümek **2,3 kat** ödüllendiriyor
- **Zamanında yatırım yapanın itibarı iki mutfakta da 80 üstünde** (92,1 ve 87,5)
- Fazla kadro ve yüksek fiyat cezalandırılıyor
- Para hiçbir haftada önemsizleşmiyor

120 test, 20/20 model kontrolü.

### İlk hafta kuralı: talebin tabanı fazla yüksekmiş

> "Hiç iş yapmayan biri ilk haftayı geçemesin. Kötü yönettiği durumda zor da olsa geçebilsin."

**Başlangıç kasasıyla denendi ve ölçüm reddetti.** 8.000'den 1.725'e (ilk hafta giderinin %85'i) inince hiçbir şey yapmayan gerçekten erken battı, ama `plancı` −18.976'ya, iyi oyuncunun itibarı 16,2'ye düştü ve masası 4'te kaldı. Sebep yapısal: **gerçek bir oyuncunun işletebilmesi için gereken tampon, aynı zamanda ölü bir restoranı haftalarca ayakta tutan tampon.**

İki şey daha çıktı ve ikisi de yönü değiştirdi.

**Birincisi:** talep çarpanı `0,5 + itibar` idi, yani itibar sıfırken bile restoran taban talebin **yarısını** alıyordu. Kimsenin konuşmadığı bir dükkân yarı doluymuş gibi davranıyordu; ihmalin ölüm sarmalı gerçekte yoktu.

**İkincisi:** [08-oyun-sonu.md](08-oyun-sonu.md) kapanışı zaten reddetmiş — *"Kayıt hâlâ silinmez, oyun hâlâ bitmez."* Batma bir son değil, bir **merdiven**. Yani "ilk haftayı geçememek" oyunun bitmesi olamaz.

Ve hiçbir şey yapmayan oyuncunun **cirosu sıfır** (malzemesi yok, herkesi kapıdan çeviriyor), yani talep düşse de batma günü değişmiyor: kasa ÷ haftalık gider = 42. gün. Saf aritmetik.

### Kabul edilen çözüm: batma günü değil BOŞALMA günü

Talep eğrisi 20 puanın altında dikleştirildi — sıfırda taban talebin %10'u kalıyor. Kırılma noktası başlangıç itibarının (30) **altında**, ve bu önemli: önce 30'a konmuştu, test yakaladı — itibar ilk günden erimeye başladığı için her oyuncu daha ikinci günde dik bölgeye giriyor ve açılış haftası herkes için çöküyordu.

Denge aracına **"boşaldı"** sütunu eklendi: itibarın 20 puanın altına indiği gün, yani dükkânın gözle görülür biçimde boşaldığı an.

| strateji | boşaldı | ilk borç |
|---|---:|---:|
| **hiçbir şey yapmayan** | **5. gün** | 42 |
| pervasız genişleyen | 8 | 7 |
| kötü fiyatlayan | 9 | — |
| kötü yöneten | 32 | — |
| fazla kadro | 45 | — |
| **iyi oyuncu** | **hiç** | — |
| **plancı** | **hiç** (Türk'te 54) | — |

Kural artık tutuyor, doğru okunduğunda: **hiçbir şey yapmayanın dükkânı beşinci günde boşalıyor.** Oyuncu işin bittiğini ilk haftanın içinde görüyor; kasadaki para yalnızca cenazeyi geciktiriyor. Kötü yöneten 32. güne kadar sürünüyor — "zor da olsa geçiyor". İyi oynayan hiç çökmüyor.

Kurgusal olarak da doğru: parası olan bir restoran hemen kapanmaz, süründürür.

**Bu değişiklik iyi oyuna hiç dokunmuyor.** İtibarını 20 üstünde tutan herkes eskisiyle birebir aynı sayıları alıyor; yalnızca çöken cezalanıyor.

## 7. Mutfak farkının sebebi mutfak değildi

Karmaşıklık bağıl hâle getirildikten sonra bile fark duruyordu: iyi oyuncunun itibarı fast food'da 94,2, Türk lokantasında 74,5. Yirmi puan.

Ölçüm sebebi başka yere koydu:

| | ort. memnuniyet | ağırlanan | son itibar |
|---|---:|---:|---:|
| fast food | 86,7 | 3.062 | **94,2** |
| Türk | 83,2 | 2.860 | **74,5** |

**Memnuniyet farkı 3,5 puan, itibar farkı 20 puan.** Yani sorun mutfakta değil, itibar eğrisinde.

Sebep sönümleme kuralı. [29](29-faz0-simulasyon.md)'da itibarın dokuz günde tavana vurmasını engellemek için kazanç kalan boşlukla çarpılmaya başlanmıştı:

```
kazanç = kazanç × (10000 − itibar) / 10000
```

O iş görülüyor. Ama çarpan **doğrusal** olduğu için tepeye yakın bölge bıçak sırtı: itibar 90'a gelince kazanç onda birine iniyor ve günlük 0,3 puanlık erime onu yeniyor. O bölgede memnuniyetteki 3,5 puanlık fark, denge noktasında 20 puanlık farka dönüşüyor.

**Çözüm: sönümlemeye taban.** Kalan boşluk 30 puanın altına inse bile çarpan 30 puanmış gibi hesaplanıyor. Erken sönümleme aynen duruyor — itibar hâlâ dokuz günde tavana vurmuyor — ama tepedeki bölge bıçak sırtı olmaktan çıkıyor.

| itibar (`plancı`) | taban öncesi | taban sonrası |
|---|---:|---:|
| fast food | 94,2 | 92,1 |
| Türk | 74,5 | **87,5** |
| **fark** | **20 puan** | **4,6 puan** |

Yan etki olarak Türk lokantasında `plancı` (19.912) artık `sadece_hal`'i (16.931) geçiyor; sıralama ihlali de bu değişiklikle kapandı.

**Ders:** iki mutfak arasındaki farkı mutfakta aramak yanlıştı. Fark, ikisinin de içinden geçtiği bir eğrinin doğrusal olmayan bölgesinde büyümüştü.

---

## 8. İçerik ile kodun sessiz ayrışması kapatıldı

Denetleyicinin üçüncü kontrolü **19 alan** buldu: DTO bağlıyor ama çekirdeğe hiç ulaşmıyor. Hepsi karara bağlandı ve **ikisi gerçek hataydı.**

### Gerçek hata 1: yemek yeme süresi

İçerik **38 saniye** diyordu, `TimingConfig` **45** kullanıyordu. Masa devir hızında %18 fark, yıllardır ayrışık ve kimse görmemiş. Düzeltildikten sonra `plancı`nın itibarı 92,1'den **100,0**'a çıktı, kaybı 25'ten 22'ye indi.

### Gerçek hata 2: servis günü uzunluğu

İçerik **120.000 ms** diyordu, kod **480.000** kullanıyordu — dört kat. Burada **kod doğruydu**: [27-zaman-modeli.md](27-zaman-modeli.md) gün uzunluğunu 480.000'e çıkardığında içerik güncellenmemişti. İçerik düzeltildi, artık ikisi tek kaynaktan.

### Sessiz ama zararsız olanlar

Kredi şartları (1,35× / 8 hafta / üç seçenek), kira günü, memnuniyet nötr eşiği (6000), düşük fiyat tabanı — hepsi içerikte yazılıydı, kodda sabitti, değerleri **tesadüfen** aynıydı. Artık içerikten okunuyorlar.

Doğrulama: yeniden düzenleme sonrası denge aracı **birebir aynı** sayıları verdi; sonra yemek süresi düzeltmesiyle beklenen yönde değişti.

### Ölü alanı değişmeze çevirmek

`TierIndex` (sık / orta / nadir) hiçbir yerde okunmuyordu. [13-veri-semalari.md](13-veri-semalari.md) §144 "trafik payları sıklık kademesinden türetilir" diyor ve içerik buna uyuyor — sık 550–1300, orta 220–400, nadir 100–150, hiç örtüşme yok — ama bunu hiçbir şey zorlamıyordu.

Artık yüklemede kontrol ediliyor: bir arketipin kademesi değiştirilip ağırlığı unutulursa **oyun açılmıyor**. Ölü alan, iki veri parçasının sessizce ayrışmasını engelleyen bir değişmeze dönüştü. Bu, ölü alanlarla başa çıkmanın üçüncü yolu: uygula, sil, ya da **değişmeze çevir**.

---

## 9. Malzeme kalitesi: içerik zaten karar vermişti

Denetleyicinin bulduğu son büyük ölü sistem. 77 malzemenin hepsinde üç kademelik bir tablo vardı ve simülasyon hiç okumuyordu:

```json
"qualityPriceMultiplierBp": { "dusuk": 8000, "standart": 10000, "yuksek": 12500 },
"qualitySatisfactionCenti": { "dusuk": -1200, "standart": 0, "yuksek": 800 }
```

### Tasarım kararı veride yazılıydı

Üç duyarlılık sınıfı var ve en hassas altısının **hepsi et**:

| sınıf | ucuz fiyat | ucuz memnuniyet | kaç malzeme | örnek |
|---|---:|---:|---:|---|
| **et** | −%25 | **−20 puan** | 6 | kıyma, tavuk göğsü, balık fileto, dana/kuzu kuşbaşı, kuzu pirzola |
| orta | −%20 | −12 puan | 38 | domates, süt, yumurta, sosis, ekmek |
| duyarsız | −%15 | −5 puan | 33 | tuz, karabiber, un, şeker, makarna |

Yani **ucuza kaçmak tuzda serbest, ette felaket** kuralı içeriğe yazılmış durumda.

### Bu yüzden tek bir küresel ayar yetiyor

Kalite malzeme başına seçilseydi 77 karar olurdu; [16-ekranlar-ve-ogretici.md](16-ekranlar-ve-ogretici.md)'nin günde 40–60 dokunuşluk bütçesi bunu kaldırmaz. Ama duyarlılık dağılımı sayesinde **tek ayar bile yemeğe göre farklı sonuç veriyor**: etli menü ağır cezalanıyor, makarna menüsü zar zor fark ediyor.

Stok kalitesi ağırlıklı ortalamayla karışıyor — ucuz alıp sonra pahalı alan, elindeki ucuz maldan hemen kurtulamıyor.

### Ortalama almak yanlıştı ve ölçüm yakaladı

İlk uygulamada yemeğin kalite etkisi malzemelerinin **ortalaması** alınıyordu. Fast food'da doğru göründü, Türk mutfağında ters çıktı:

> `ucuz_malzeme` 35.200 kazanıyordu — iyi oyunun 26.211'inden **fazla**.

Sebep ortalamanın kendisiydi: tencereye atılan ucuz soğan, **ucuz eti gizliyordu**. Altı malzemeli sulu yemek cezayı altıya bölüyor, üç malzemeli hamburger üçe. Kendi yazdığım yorum bunun tersini söylüyordu — "müşteri *eti ucuzmuş* der" — ama kod öyle davranmıyordu.

Doğru kural: **en belirleyici malzeme ne diyorsa o.** Müşteri yanındaki soğanları saymıyor.

| | fast food | Türk |
|---|---:|---:|
| **ortalama** ile | 9.117 | **35.200** ← tuzak değil, strateji |
| **en belirleyici** ile | 9.117 | **11.604** ← tuzak |

`QualityTests.Ucuz_et_kalabalik_tarifin_arkasina_saklanamiyor` bu gerilemeyi koruyor: çok malzemeli yemek, az malzemeli yemekten daha az cezalanamaz.

### Sonuç

| | `makul` | `ucuz_malzeme` |
|---|---:|---:|
| son kasa (fast food) | 24.876 | **9.117** |
| son kasa (Türk) | 26.211 | **11.604** |
| itibar | 91,0 / 58,0 | 16,3 / 18,2 |
| ulaştığı masa | 7,9 / 8,0 | 4,4 / 4,4 |

Zincir okunabilir: birim maliyet düşüyor → itibar çöküyor → müşteri azalıyor → büyüme duruyor. Malzemeden kısmak bir tuzak, ve **neden** tuzak olduğu görünüyor.

---

## 10. Adlandırılmış ekipman

> "Özel ekipmandan kastım mesela pide yapmak istersek taş fırın gereksin. Döner için döner takılan tezgâh gereksin. Veya İtalyan makarna için parmesan peyniri gereksin."

[09-icerik-envanteri.md](09-icerik-envanteri.md) bunu zaten planlamıştı: **mutfak başına 10 özel pişirme istasyonu**. Yapılmamıştı; §3'ün "istasyon kademesi ≥ N" kuralı onun soluk vekiliydi.

`equipment.json` artık `cuisineStations` taşıyor: paylaşılan altı istasyonun **ardına** eklenen, mutfağa özel, **adlandırılmış** ekipman.

| mutfak | ekipman | fiyat | açtığı |
|---|---|---:|---|
| Türk | **taş fırın** | 2.700 | börek, kadayıf, revani, fırın makarna, musakka, karnıyarık, patlıcan kebabı |
| fast food | **milkshake makinesi** | 1.860 | milkshake |
| fast food | **waffle makinesi** | 2.700 | waffle |

Paylaşılan altıdan tek farkı **başlangıçta olmamaları**: satın alınana kadar bağlı yemekler kilitli, günü ve itibarı gelse bile. Fark soyuttan somuta — "ızgara kademesi 2 gerekli" değil, **"taş fırın al, börek açılsın"**.

**Döner ve pide verilemedi çünkü içerikte yok.** Türk menüsünde köfte, tavuk şiş, adana, kanat, pirzola var; döner yok. Mekanizma hazır, o yemekler yazıldığında `doner_ocagi` ve `pide_firini` tek satırla tanımlanıp bağlanır. Onluk kadro içerik işi.

### İki yerde yazılan şey, çapraz kontrol edilmeli

`equipment.json` her ekipmanın açtığı yemekleri listeliyor ama yemekler de kendi istasyonunu söylüyor. Bugün beş kez iki yerde yazılı bir şeyin sessizce ayrıştığını gördükten sonra liste sadece belge bırakılmadı: yüklemede çapraz kontrol ediliyor. Bir yemek başka istasyona taşınıp liste güncellenmezse **oyun açılmıyor**.

### Gerilemenin sebebi içerik değil botun kararıydı

Ekipman eklendikten sonra ölçüm fast food'da `plancı`yı batmış gösterdi: −1.896, itibar 35,6, kadro 11'den 6,9'a. Günlük veri sebebi tek bakışta verdi:

| gün | kasa |
|---|---:|
| 55 | 19.649 |
| **56** (ödeme günü) | **330** |
| 57 | 163 → malzeme alınamıyor, 4 müşteri |
| 58–60 | sıfır servis, itibar 100'den 31'e |

On dört masada haftalık sabit gider ~19.000 ve restoran haftada tam o kadar biriktiriyor. Yeni iki ekipman (4.560) bıçak sırtını aşağı itti. Yani içerik yanlış değildi — **strateji maaş parasını harcıyordu.**

Düzeltme stratejide: ekipman alımı haftalık ödemeyi yiyemez. Gerçek oyuncu maaş günü yaklaşırken waffle makinesi almaz.

| | ekipman öncesi | ekipman sonrası | ödeme koruması ile |
|---|---:|---:|---:|
| `plancı` fast food | 17.501 | **−1.896** | **21.468** (itibar 100) |
| `plancı` Türk | 20.419 | — | **23.581** (itibar 100) |

Düzeltme yalnızca gerilemeyi kapatmadı, botu **eskisinden yetkin** yaptı. Bu, denge aracının ikinci işi: ekonomiyi ölçerken stratejilerin beceriksizliğini de gösteriyor.

---

## 11. Sipariş tercihi: uydurmak yerine türetmek

[13-veri-semalari.md](13-veri-semalari.md) arketip başına bir sipariş tercihi tasarlamıştı:

```json
"orderPreference": { "sulu": 0.6, "pilav": 0.25, "corba": 0.15 }
```

Simülasyon bunu okumuyordu — **bütün müşteriler aynı dağılımla sipariş veriyordu.** Şemayı birebir uygulamak 24 arketip için elle ağırlık tablosu yazmak demekti, ve o tablolar uydurma olurdu: hangi arketipin pilavı %25 mi %30 mu sevdiğini söyleyecek hiçbir dayanak yok.

**Onun yerine zaten yüklü olan karakter alanlarından türetildi.**

### Yemek seçimi: `priceSensitivityBp`

Bu alan içerikte vardı, 4.000 ile 25.000 arasında değişiyordu, ve yalnızca *memnuniyet cezasında* kullanılıyordu — yemek **seçiminde** hiç rol oynamıyordu.

Artık rolden yemek seçimi eşit olasılıklı değil: duyarlılık 10.000 nötr, üstü ucuza, altı pahalıya yaslanıyor. Hiçbir yemek tamamen dışlanmıyor (taban ağırlık var), sadece dağılım kayıyor.

| mutfak | en duyarlı | ort. fiş | en duyarsız | ort. fiş |
|---|---|---:|---|---:|
| Türk | öğrenci (23.000) | 51,5 | yemek eleştirmeni (4.000) | 60,7 |
| fast food | pazarlıkçı (25.000) | **29,1** | yemek eleştirmeni (4.000) | **43,2** |

Fast food'da en cimri ile en cömert müşteri arasında **%48 fiş farkı** — sıfır yeni içerikle.

### Ek kalemler: `tipChanceBp`

Tatlı olasılığı da arketipe göre değişiyor. Bahşiş eğilimi, harcamaya yatkınlığın vekili: çok bahşiş bırakan tatlı da alır, hiç bırakmayan almaz. Bahşişi 0 olan denetim görevlisi tatlı olasılığının yarısını, 3.200 ile mahalle toplu yemeği iki katını alıyor.

### Neden bu daha iyi

Uydurma tablo yazmak, veriyi **iki kere** yazmak demekti: karakter bir yerde (fiyat duyarlılığı), tercih başka yerde. Bugün beş kez iki yerde yazılan şeyin sessizce ayrıştığını gördükten sonra bu yola girmek yanlış olurdu.

Türetilmiş hâlinde bir arketipin fiyat duyarlılığını değiştirmek, sipariş davranışını **kendiliğinden** değiştiriyor. Ve `PriceSensitivityBp` ile `TipChanceBp` artık çift iş görüyor.

**Ölçülen yan etkisi:** iyi oyuncu 23.636'dan **26.434**'e, itibarı 86,5'ten **92,4**'e çıktı. Sebep, fiyata duyarsız müşterilerin artık pahalı yemeği gerçekten seçmesi.

---

## 12. Patron müdahalesi: etki vardı, kısıt yoktu

[02-tasarim-onerisi.md](02-tasarim-onerisi.md) §10 patronun servis sırasındaki rolünü tek cümleyle tanımlıyor: *"servis sırasında sadece krizlere müdahale edersin"*. §59 de sınırı koyuyor: **gün başına 3–5 hak**.

Simülasyonda müdahalenin **etkisi** yazılıydı — özür ve ikram +15 puan, patron ilgisi +20, ve ilgi gösterilen müşteri biraz daha bekliyor. Ama iki kısıt yoktu:

| Ne eksikti | Sonucu |
|---|---|
| **Hak sayısı** — `interventionsPerDay: 4` içerikte yazılı, hiçbir şey zorlamıyor | Her kızgın müşteri bedava kurtarılabiliyordu; kriz yönetimi bir kaynak değil sınırsız bir düğmeydi |
| **İkram maliyeti** — [12-ekonomi.md](12-ekonomi.md) §3 "porsiyon başına 2 maliyet" diyor | Çay bedava bir memnuniyet musluğuydu |

İkisi de eklendi. Ve bir hata daha çıktı, testle: hakkı yalnızca gün açılışında kuruyordum, ama simülasyon **birinci güne zaten açık başlıyor** — yani ilk gün sıfır hakla geçiyordu. Kurucuya da kondu.

### Ölçüldü: müdahale kazandırıyor ama para değil itibar olarak

`mudahaleci` stratejisi makul oyuncu gibi oynuyor, ayrıca serviste sabrı en az kalan masaya patron ilgisi gösteriyor.

| | `makul` | `mudahaleci` |
|---|---:|---:|
| son kasa | 26.434 | **27.019** (+%2,2) |
| itibar | 92,4 | **99,5** |
| ulaştığı masa | 7,8 | 8,1 |
| ağırlanan | 2.216 | 2.288 |

Kasadaki fark küçük, itibardaki fark büyük. Bu doğru şekil: docs/02 patronun servis rolünü **kriz yönetimi** diye tanımlıyor, para musluğu diye değil. Zaten itibarı tavana yakın bir restoranda kazanç küçük görünüyor; asıl değeri zorlanan bir restoranda ortaya çıkar.

### Hedefi yanlış şeye bağladım, ikinci mutfak düzeltti

Kalibrasyona önce şu hedef kondu: *müdahale etmek, etmemekten kötü olamaz* — ölçü son kasa. Türk mutfağı reddetti: `mudahaleci` 24.058, `makul` 28.638.

Ama aynı koşuda:

| | `makul` | `mudahaleci` |
|---|---:|---:|
| itibar | 56,5 | **81,3** |
| ulaştığı masa | 8,4 | **11,0** |
| ağırlanan | 1.998 | **2.152** |
| son kasa | 28.638 | 24.058 |

Müdahaleci daha küçük değil **daha büyük** bir işletme çalıştırıyor. Kasası az çünkü yükselen itibar stratejiyi daha çok genişlemeye ve daha çok kadroya itmiş — parayı kaybetmemiş, **yatırmış**.

Yani ölçü yanlıştı. Müdahalenin doğrudan etkilediği şey **itibar ve ağırlanan müşteri**; kasa, stratejinin o itibarla ne yaptığına bağlı ve bu mekaniğin ölçüsü olamaz. Hedef ikisine çevrildi, artı "müdahaleci borca düşmemeli".

**Genel ders:** bir mekaniği, doğrudan etkilemediği bir sayıyla ölçmek onu haksız yere mahkûm edebilir. İtibarı yükselten bir şey, o itibarla büyüyen bir stratejinin elinde son kasayı **düşürür**.

### Yazılmayan dördüncü tür

docs/02 üç örnek veriyor: *"bir istasyonu hızlandır, bekleyen masaya ikram gönder, VIP müşteriyi bizzat karşıla."* İkincisi ve üçüncüsü var. **Birincisi — istasyon hızlandırma — yok**, çünkü `InterventionKind` üç değer taşıyor ve o [23-cekirdek-sozlesmesi.md](23-cekirdek-sozlesmesi.md)'de kapalı liste. Eklenmesi sözleşme değişikliği; ayrı bir karar.

---

## 13. İstasyon hızlandırma: sözleşmeyi açmaya değdi

§12'nin sonu bu türü "ayrı bir karar" diye bıraktı. Karar verildi: **yazıldı.**

`InterventionKind` [23-cekirdek-sozlesmesi.md](23-cekirdek-sozlesmesi.md)'de kapalı bir liste olduğu için dördüncü değer eklemek sözleşme değişikliği. Değdirdi çünkü diğer iki müdahale de **bekleyen masayı** hedefliyor; ikisi de mutfağa dokunamıyor. Servis saatinde tıkanan yer genelde mutfak, ve patronun elinde mutfağa dair hiçbir düğme yoktu.

### Patron pişirmiyor

[14-personel-sistemi.md](14-personel-sistemi.md) net: patron mutfakta çalışmaz. Yani mekanik "patron tezgâha geçer" olamaz. Bunun yerine `RushStation` **o istasyonda pişmekte olan işlerin kalan duvar saatinin %40'ını siler** — patron mutfağa girip "şu masa bekliyor" der, bir tabak öne alınır. Aşçının kapasitesini değil, **sıranın önceliğini** değiştirir.

Bu, [27-…](27-zaman-modeli.md) Karar D'yi de bozmuyor: `prepMs` içerik tarafında sabit kalıyor, kısalan şey o anki *kalan* süre — ekipmanın yaptığı gibi kalıcı bir hızlanma değil, günde birkaç kez kullanılabilen bir kerelik hak.

### Boş istasyonu hızlandırmak hakkı yakmıyor

Müdahale hakkı günlük ve sayılı. Boş bir istasyona basmak hakkı harcasaydı, mekanik oyuncuyu **arayüzü yanlış okuduğu için** cezalandırırdı — hangi istasyonun dolu olduğu zoom seviyesine göre her zaman görünmüyor. Boş istasyon reddediliyor, hak duruyor. İki test bunu koruyor.

---

## 14. Hal fiyat oynaklığı: takip edilecek bir şey olmalı

[12-ekonomi.md](12-ekonomi.md) §3 şunu söylüyordu: *"erken alım avantajı yok, stok bozuluyor. Ucuz güne denk gelmek şans değil, TAKİP meselesi."* İçerikte `priceVolatilityBp: 2500` yazılıydı. **Okunmuyordu.** Yani hal her gün aynı fiyatı veriyordu ve takip edilecek hiçbir şey yoktu.

Artık gün açılışında (`AdvanceToNextDay`) her malzeme için ayrı bir günlük çarpan atılıyor — `Market` akışından, ±%25 bandında. Sabah hale gidildiğinde bugünün fiyatı görülüyor; sipariş o fiyattan kesiliyor.

### Neden gün açılışında, servis açılışında değil

Oyuncu sabah stok kararını verirken **bugünün** fiyatını görmeli. Çarpan servis açılışında atılsaydı, oyuncu dünkü fiyata bakıp alır, fiyat sonradan oynardı — karar değil kumar olurdu.

### Oynaklık tek başına bir karar değil

Kırk dört malzemenin fiyatı her gün oynasa da, bugün ucuz olanı alıp yarına saklayamıyorsan bu sadece bir gider gürültüsü. Kararı yapan şey **soğuk hava merdiveni**: `CanKeep` + `KeepDays`. Harness'ın `stockAhead` mantığı zaten bunu yapıyordu (bugünkü fiyat yıl ortalamasının %92'sinin altındaysa dört güne kadar stok) — o karşılaştırma şimdiye kadar sadece **mevsimi** ölçüyordu, çünkü günlük oynaklık yoktu. Aynı satır artık iki sinyali birden okuyor: mevsim (yavaş, öngörülebilir) ve hal (hızlı, öngörülemez).

Ortalama, oynaklığı içermez — oynaklığın ortalaması 1,0 — yani karşılaştırma bozulmuyor.

### Ölçüm: denge iyileşti

| | önce | sonra |
|---|---:|---:|
| kalibrasyon cezası (iki mutfak) | 8 | **0** |

Oynaklık dengeyi bozmadı, **düzeltti**. Sebebi muhtemelen şu: sabit fiyat, malzemeden kısan stratejiyi fazla öngörülebilir kılıyordu; oynaklık aynı stratejiye risk ekliyor ama soğuk havaya yatırım yapmış oyuncuya risk eklemiyor. Yani oynaklık, zaten satılan bir merdivenin **karşılığını** yaratıyor.

### Belirlenimcilik

Oynama rastgele ama tohuma bağlı: aynı tohum aynı fiyat dizisini veriyor, `_rngMarket` ve `_marketBp` kayda giriyor (sürüm 8). `Hal_fiyatlari_her_gun_oynuyor` ikisini birden koruyor — fiyat oynuyor **ve** ikiz simülasyon aynı ilk fiyatı veriyor.

### Bir kenar notu: kalibrasyon sessizce yanlış cevap verebiliyordu

Bu ölçümü alırken `calibrate.py` yedi adayın hepsine aynı cezayı (1998) verdi ve en kötüsünü "en iyi" diye yazdı. Sebep dengede değildi: harness'ın **Debug** çıktısı bu makinede "Application Control" politikasına takılıyor, hiç çalışmıyor. `run()` hatayı yutuyordu, tablo boş geliyordu, her aday eşitleniyordu.

İki şey düzeltildi: harness artık `-c Release` ile koşuyor, **ve** tablo tamamen boşsa `calibrate.py` artık ceza verip devam etmiyor, hata fırlatıyor. Ceza mekanizması *tasarım* ihlalini ölçer; *koşu* hatasını cezaya çevirmek, bozuk bir ölçümü geçerli bir sonuç gibi gösteriyor.

---

## 15. Personel deneyimi: kimin ne kadar süredir burada olduğu

[14-personel-sistemi.md](14-personel-sistemi.md) şunu yazmıştı: *çalışılan her gün 1 puan, 30 puanda seviye, azami 3 seviye, her seviye hız +%10.* `staff-roles.json` dört rolün her birine `xpSpeedBp: [10000, 11000, 12000, 13000]` merdivenini taşıyordu. **Hiçbiri okunmuyordu.**

### Deneyim sayılamaz

Simülasyon personeli **sayı** olarak tutuyor: `_cooks`, `_salon`. Deneyim bu modele sığmıyor — "kaç aşçı var" sorusunun cevabı bir sayı, ama "ne kadar süredir burada" sorusunun cevabı *kişiye* ait. Bu yüzden iki dizi eklendi: `_cookXpDays[]`, `_salonXpDays[]`. İşe alım **sona** ekler, çıkarma **sondan** alır.

Sondan alması bilinçli: aksi halde "en deneyimliyi kov" diye anlamsız bir karar doğardı. Geri alınan da yeni biri — kadro kesip geri almak bedava değil.

### Kısalan şey pişme süresi değil

Deneyim `attendMs`'i bölüyor, `prepMs`'i değil. Yani **deneyimli aşçı daha çabuk serbest kalıyor, yemek daha çabuk pişmiyor.** [27-…](27-zaman-modeli.md) Karar D ekipman için ne diyorsa deneyim için de aynısı geçerli; `Deneyim_yemegin_pisme_suresine_dokunmuyor` bunu koruyor. Salon tarafında aynı şey: garsonun işi kısalıyor, müşterinin yemek yeme süresi değil.

Patron (salon dizininin sıfırıncı elemanı) deneyim kazanmıyor — zaten `OwnerAdjusted` ile ayrı bir çarpanı var.

### Ölçüm: deneyim en çok DAR KADROYA yarıyor

Mekaniğin bağlayıcı olup olmadığını anlamak için merdiven abartıldı (+%10/20/30 yerine +%100/200/300) ve fast food koşusu tekrarlandı:

| strateji | gerçek merdiven | abartılmış |
|---|---:|---:|
| `genislemeyen` itibar | 64,7 | **89,4** |
| `genislemeyen` ağırlanan | 1.091 | 1.160 |
| `fazla_kadro` itibar | 30,1 | 36,9 |
| `makul` itibar | 99,1 | 99,5 |
| `planci` itibar | 100,0 | 100,0 |

Büyük ve iyi kadrolu işletmelerde etki **doygun** — zaten tavandalar. Fark, dört masada iki kişiyle dönen `genislemeyen`de çıkıyor. Yani deneyim tam olması gereken yerde işliyor: **yeni birini alamayacak kadar küçük olan işletmeye, elindekini tutmanın karşılığını veriyor.**

Gerçek merdivenle etki ölçülü kalıyor (kalibrasyon cezası yine 0, ölçülen gerçekleşme 10.196 → 10.207). Bu bir strateji değil, arka planda biriken bir ödül — ve maaşı zaten haftalık %2,2 bileşik zamla ödeniyor. **Elindekini tut, hızlansınlar; tutmanın maliyeti de artsın.**

### 60 günde üçüncü seviye yok

30 gün = 1. seviye, 60 gün = 2. seviye, 90 gün = 3. Yani kampanya süresinde tavana ulaşılamıyor; üçüncü seviye [29-…](08-oyun-sonu.md)'un serbest oyununa ait. Bu bir eksik değil, süre yapısının sonucu — ama testte açıkça yazılı, çünkü fark edilmeden değiştirilecek bir sayı.

---

## 16. Kalan üç ölü alan: üçünün de kaderi farklı

[Beşinci ders](#) diyordu ki bir ölü alanın üç kaderi var: **yaz, sil, ya da değişmeze çevir.** Kuyrukta kalan üç madde üçünü de örnekliyor.

### `weeklyWageMultiplierBp` → **değişmez**

`economy.json` hem `weeklyXpWageGrowthBp: 220`'yi hem de onun on haftalık bileşiklerini taşıyordu. Aynı gerçek iki yerde. Tablo silinmedi — denge aracı ve arayüz onu okuyor — ama artık `ContentLoader` açılışta üretecine karşı doğruluyor, bir baz puanlık toleransla (tablo Python'da, doğrulama C#'ta hesaplanıyor).

### `unlockSeason` → **değişmez**

32 yemeğin her biri hem `unlockDay` hem `unlockSeason` taşıyor, ve ikincisi birincisinden türetilebiliyor: `(gün - 1) / 15 + 1`. Yükleyici artık bunu doğruluyor ve `DishDef.UnlockSeason` olarak taşıyor; ilerleme ekranı yemekleri buna göre gruplayacak. Üstüne [09-…](09-icerik-envanteri.md)'un ilerleme eğrisi de teste bağlandı: mevsim başına 13/8/6/5 yemek, iki mutfakta da.

`ContentSetLoader` mevsim uzunluğunu bu yüzden `economy.json`'dan okuyor — ikinci bir sabit yazmak, az önce kapatılan hatanın aynısını açardı.

### `ownerPool` → **değişmez**

İçerik `"salon"` yazıyor ve kod zaten salon **varsayıyor**: `StaffingModel` patronun iş gününü salon yükünden düşüyor, `DispatchSalon` patronu sıfırıncı garson olarak çalıştırıyor, docs/14 patronun mutfakta çalışmasını yasaklıyor. Alan bir *seçim* değil, bir *varsayımın yazılı hali*. Silmek varsayımı görünmez yapardı; okumak için ikinci bir havuz yazmak gerekirdi ve tasarım onu istemiyor. Başka bir havuz yazılırsa oyun artık açılmıyor.

### `Crew.SalonWorkMicro` → **silindi**

Tek gerçek silme. Zirve günün salon iş yükünü taşıyordu, hiçbir yerde okunmuyordu — ve **dört kurucusundan ikisi ona `0` yazıyordu.** Yani birisi onu okumaya kalksa yanlış değer okuyacaktı. Yük zaten `peakCustomers`'tan yeniden hesaplanabiliyor.

**Ders:** bir alanın yazılmamış olması onu silinecek yapmaz; ama **çağrı yerlerinden bazıları ona yalan yazıyorsa**, o alan artık ölü değil *tuzak*. Silmek tek doğru kader.

Denetleyicinin 1., 2. ve 3. kontrolü artık **hiçbir şey bulmuyor.**

---

## 17. Döner ve pide — ve üretecin sildiği kilit sistemi

Kullanıcının istediği hal buydu: *"pide yapmak istersek taş fırın gereksin. Döner için döner takılan tezgah gereksin."* Taş fırın §10'da yazılmıştı; döner ve pide **içerikte yoktu**, o yüzden bağlanamamışlardı.

### Menü 32'de kalıyor: dört giren, dört çıkan

| Çıkan | Neden |
|---|---|
| `tavuk_kanat_izgara` | fast food okunuyor; `tavuk_kanat` malzemesi de Türk listesinden çıktı |
| `firin_makarna` | İtalyan mutfağına ait; `kasar` yerine yeni pideye bağlandı |
| `kiymali_ispanak` | on bir sulu yemek arasında en zayıfı; `ispanak` malzemesi de silindi |
| `kabak_dolma` | aynı |

| Giren | Ekipman | Mevsim |
|---|---|---|
| `doner` | **döner ocağı** | 2 |
| `iskender` | döner ocağı | 4 |
| `kiymali_pide` | **pide fırını** | 3 |
| `lahmacun` | pide fırını | 4 |

Dördü de `izgara` grubunda, yani **ana** rolde: menünün omurgasına dokunuyorlar, kenarına değil. Tek yeni malzeme `doner_eti`. Mevsim dağılımı bozulmadı (13/8/6/5), ilerleme eğrisi 6 → 13 → 21 → 27 → 32 aynı kaldı.

Türk mutfağı artık üç adlandırılmış ekipman taşıyor (taş fırın 2.700, döner ocağı 1.860, pide fırını 4.800 sikke), toplam **on bir yemek** onların arkasında. docs/09 mutfak başına on istasyon istiyor; üç ile beş arası bir yerdeyiz.

### Bunu yazarken bulunan asıl şey: üreteç kilit sistemini siliyordu

`content/dishes/*.json` **üretilen** dosyalar. Ama `requiresStationTier`, `unlockReputationCenti` ve adlandırılmış istasyon adları oraya doğrudan yazılmıştı — üretici `tools/content/gen_dishes.py` onları **bilmiyordu**.

Yani `python tools/content/gen_dishes.py` çalıştırmak:
- yedi Türk yemeğinin istasyonunu `tas_firin`'den `firin`'e geri çeviriyor,
- 64 yemeğin **hepsinin** `requiresStationTier`'ını 0 yapıyor,
- `unlockReputationCenti`'yi siliyordu.

Yani içerik üreticisini çalıştırmak, o günün en büyük tasarım işini — itibar + ekipman kilidini — **tek komutta yok ediyordu.** Hiçbir test kırılmıyordu çünkü testler o an üretilmiş içeriği okuyor. Bunu ancak üreteci gerçekten çalıştırınca gördüm.

**Üç şey düzeltildi:**

1. Adlandırılmış istasyonlar artık `gen_dishes.py`'de: yemek satırı kendi istasyonunu yazıyor, `CUISINE_STATIONS` mutfağa hangi ekipmanların ait olduğunu söylüyor.
2. Kilit kapıları `unlock_gates()` içinde **üretiliyor**: yemekler (karmaşıklık, fiyat) ile sıralanıp bölünüyor (alt %53 ekipmansız, sonraki %28 kademe 1, üst %19 kademe 2 — 32 yemekte 17/9/6), adlandırılmış ekipmana bağlı olan tam olarak kademe 1, açılış menüsü hep 0; itibar eşiği gün başına 0,9 puan.
3. Ekipmanın **`opens` listesi artık türetiliyor**: `model.py` onu elle tutmuyor, `export.py` yemek dosyalarını tarayıp çıkarıyor. Hangi yemeğin hangi ekipmanı istediği zaten yemeğin kendi `station` alanında yazıyor; ikinci bir liste tutmak bu projede tam da az önce ayrışan şeydi.

`ContentTests.Kilit_kapilari_uretecin_kuralina_uyuyor` aynı kuralı dışarıdan sınıyor, ve üreteci iki kez çalıştırmak artık **bit bit aynı** içerik veriyor.

### Denge: iki kez bot suçluydu, ekonomi değil

Yeni içerik kalibrasyonu bozdu (ceza 0 → 16) ve iki ihlalin ikisi de stratejinin kararıydı:

**Birincisi — `genislemeyen` itibarı 85,1'den 50,7'ye düştü.** Sebep: kredisi vardı, `Equipment.Upgrade` kredi varken **hiçbir** isteğe bağlı ekipman almıyordu, dolayısıyla döner ocağını hiç alamıyordu — ve müşteriler her gün döner soruyordu (§6'daki "soran müşteri" mekaniği). Altmış günün yarısını "yok" diyerek geçiriyordu. Kural değişti: kredi varken **yalnızca menü açan** ekipman alınabiliyor, iki haftalık sabit gider korunarak.

Bu kuralın ikinci yarısı da ölçülerek geldi: bir haftalık koruma yetmedi, `planci` genişleme takvimini kaçırdı (12,5 masa, hedef 13) çünkü parayı ocağa yatırıp kademeyi geciktirdi. İki haftaya çıkınca ikisi de düzeldi.

**Sonuç:** `makul` 30.364 → **33.208** ve itibar 64,2 → **100,0**; büyüme çarpanı 4,14 → **3,80** (hedef bandı 1,8-4,0). **İki mutfak da temiz, ceza 0.** 153 test.

**Ders (yedincinin devamı):** üretilen bir dosyaya elle yazılan her alan, üretecin bir sonraki çalıştırmasında ölür. Bu, "aynı şey iki yerde yazılı" hatasının en sinsi hali — çünkü iki yer ayrışmıyor, biri **diğerini siliyor**.

---

## 18. İmza mekanikleri: mutfağı mutfaktan ayıran tek şey

[07-mutfak-sistemi.md](07-mutfak-sistemi.md) bunu **"en önemli satır"** diye yazmıştı: *"satın almanın yeniden boyama değil başka bir oyun olduğunu gösteren şey bu."* [23-…](23-cekirdek-sozlesmesi.md) §8.2 şemayı da yazmıştı — `cuisines/*.json` içine bir `signature` bloğu, *"kind bilinmiyorsa doğrulama reddeder, blok eksikse mutfak yüklenmez."*

**Blok hiç yoktu ve mutfaklar sorunsuz yükleniyordu.** Yani iki mutfak, farklı menü taşıyan aynı oyundu.

| Mutfak | Mekanik | Ne yapıyor |
|---|---|---|
| fast food | **kombo** | Kombonun ana yemeğini seçen grup yanını ve içeceğini de **kesin** alıyor, üçüne indirimli tek fiyat ödüyor, ve o iş aşçıyı **%20 daha uzun** bağlıyor |
| Türk | **veresiye** | Sık gelen müşteri hesabı deftere yazmayı **istiyor**; açarsan nakit akışın bozulur, açmazsan memnuniyet düşer |

Mekanik kodda, sayılar veride. Blok yoksa, `kind` tanınmıyorsa, kombo mutfağı **rahatlatıyorsa** (`kitchenLoadBp < 10000` — bedava kazanç) ya da kombo indirimi indirim değilse oyun açılmıyor.

### Mekanik ikinci mevsimde geliyor

[09-…](09-icerik-envanteri.md): *"İmza mekaniği ikinci mevsimin başında gelir. Birinci mevsime konursa öğretici yükü çok ağırlaşır, çünkü oyuncu zaten menü ve fiyatı öğreniyor."* `SignatureDef.FromDay` bunu taşıyor (mevsim uzunluğu + 1 = 16. gün) ve yükleyici kombonun üç kalemini o güne göre denetliyor: mekanik geldiğinde kilitli olan bir kombo hiç satılamaz.

### Veresiye üç kez yanlış yazıldı, üçünde de ölçüm söyledi

**Birinci deneme — prim düğmesi.** Veresiye açmak memnuniyet primi veriyordu. Ölçüm: `imzacı` 29.151, `makul` 32.676. Sebep basit ve öğretici: **iyi oynayan zaten itibar tavanında.** 100/100 iken +8 puanlık bir memnuniyet primi hiçbir şey satın almıyor; geriye yalnızca batan hesapların maliyeti kalıyor.

**İkinci deneme — sadakat talebi.** Tahsil edilen her hesap talebe kalıcı %0,6 ekliyor (tavan %15). Ölçüm: ağırlanan müşteri 2.331 → 2.334. **Üç kişi.** Sebep yine aynı sınıftan: iyi oynayan **kapasite sınırında**, servis oranı %97, kapıdan dönen %1. Talep artırmak, masası dolu bir restorana hiçbir şey vermiyor.

**Üçüncü deneme — doğru yön.** Mekanik ters kuruluydu: veresiye **teklif edilen bir prim değil, istenen bir şey.** Sık gelen müşteri (`TierIndex 0`) %12 olasılıkla hesabı deftere yazmayı istiyor; vermezsen memnuniyeti düşüyor. Kullanıcının kendi fikri olan "kilitli yemeği soran müşteri" mekaniğiyle (§6) tam olarak aynı fikir. Kazanç tarafı da paraya bağlandı: hesabını kapatan **üstüne %12 koyuyor**, ve deftere yazılan adama çay konuyor — çay tahsilat şansını %85'ten %95'e çıkarıyor ve maliyeti içerikteki `teaCostCenti` (bugüne kadar okunmayan bir alan).

### Ölçüm: mekanik tavanda nötr, DAR yerde kazandırıyor

| | veresiye veren | geri çeviren |
|---|---:|---:|
| kasa | 19.270 | 17.999 |
| defter | 169 | — |
| itibar | 17,8 | 17,1 |
| sadakat | %12,0 | — |

Bu, **itibarı tavanda olmayan** küçük bir lokanta. Veren, çevirenin %8 önünde. Altmış günlük tam kampanyada, itibarı 100'de doymuş bir `makul`e karşı `imzacı` 30.465 + 1.883 defter = 32.348'e karşı 32.987 — yani **nötr**.

Bu, personel deneyiminde bulunan şeyle aynı şekil (§15): **mekanik, sıkışmış oyuncuya yarıyor; tavandaki oyuncuya değmiyor.** Ve doğrusu bu — tavandaki oyuncuya da kazandırsaydı mekanik karar değil mecburiyet olurdu.

Fast food'da kombo doğrudan kazandırıyor: fiş 55,5 → **56,2**, kasa 26.167 → **26.698**, ama itibar 99,2 → 97,7 — mutfak gerçekten daha yorgun. Takas görünür.

### Kalibrasyona yeni bir hedef

`imzacı`, `makul`'ün **%90 ile %130'u arasında** kalmalı ve borca düşmemeli. Alt sınır mekaniğin tuzak olmadığını, üst sınır mecburiyet olmadığını sınıyor. Ölçü **kasa + defter**, çünkü altmışıncı günde tahsil edilmemiş veresiye kaybolmuş para değil ([08](08-oyun-sonu.md) net varlık). Harness tablosuna bunun için bir **`defter`** sütunu eklendi.

**Ders (dokuzuncunun kardeşi): bir mekaniğin ödülünü, oyuncunun zaten DOYMUŞ olduğu bir eksene bağlamak, mekaniği görünmez yapar.** İtibar 100'de, masalar dolu. Bu iki eksende ödeme yapan iki tasarım denendi ve ikisi de ölçümde sıfır çıktı. Üçüncüsü paraya ve bir CEZAYA bağlandı — ve çalıştı. Yeni bir mekanik yazarken ilk soru "ne veriyor" değil, **"verdiği şeyin oyuncuda yeri var mı"** olmalı.

---

## 19. İsimli düzenli müşteriler

[11-musteri-sistemi.md](11-musteri-sistemi.md) ayrımı net koymuştu: *"isimli müşteri tek bir kişidir, elle yazılmıştır, hikayesi vardır ve hep aynı kişidir. Arketip ise binlerce müşteri üretir."* [09](09-icerik-envanteri.md) mutfak başına on tane istiyor, her birinin üç-dört sahnelik hikâyesiyle. `content/regulars/` klasörü **hiç yoktu.**

Artık var: `tools/content/gen_regulars.py`, mutfak başına on kişi. Türk tarafında Hasan Usta (esnaf komşu, kuru fasulye, 3. gün), Nazife Teyze, Selim Bey, Rasim Amca…; fast food tarafında Deniz, Burak, Elif…

### Davranışı arketipten geliyor, kimliği kendinden

Düzenli müşteri bir **arketibi taban alıyor** ([13](13-veri-semalari.md)): sabır, grup büyüklüğü, fiyat duyarlılığı, geliş saati — hepsi oradan. Böylece davranış kodu tek yol izliyor. Kendine ait olan üç şey var:

| Alan | Ne yapıyor |
|---|---|
| `favouriteDish` | Menüde varsa onu sipariş ediyor; yoksa memnuniyeti düşüyor |
| `arrivesFromDay` | Kampanyaya ne zaman giriyor (3. günden 47. güne yayılı) |
| `veresiyeEligible` | Türk mutfağında deftere yazılabilir mi |

### En önemli değişmez: talebi ŞİŞİRMİYOR

İsimli müşteri, günün planına **eklenmiyor** — planın içinden **yer alıyor**. `BindRegularsToPlan`, gelen düzenli müşteriyi kendi arketibindeki bir plan satırına bağlıyor; yoksa en erken boş satırı onun adına yazıyor.

Bu bilinçli: aksi halde her yeni isim ekonomiyi büyütürdü ve **kalibrasyon her içerik eklemesinde kayardı.** `RegularTests.Talebi_SISIRMIYOR` ilk üç günü karşılaştırıyor — ilk düzenli müşteri 3. günde geldiği için 1. ve 2. gün birebir aynı, dolayısıyla 3. günün itibarı da aynı, ve planı da aynı çıkmalı. Sonraki günlerde sayılar ayrışıyor ve **ayrılmaları doğru**: sevdiği yemeği bulan müşteri daha memnun ayrılıyor, itibar farklı işliyor. O fark mekaniğin kendisi.

### Veresiye kimliğine kavuştu

[13](13-veri-semalari.md) `veresiyeEligible` alanını düzenli müşteri dosyasına koymuştu ve doğrusu buydu: **veresiye adını bildiğin birine açılır.** §18'de bu kural "sık gelen arketip"ten türetiliyordu — çalışan ama kimliksiz bir yaklaşım. Artık içerik varsa oradan geliyor; yoksa eski türetim yedek olarak duruyor (birim testleri düzenli müşteri dosyası olmadan koşuyor).

Bunun bir yan etkisi ölçüldü: aday kitle daraldığı için (yedi kişi, her biri günlerin yarısında uğruyor) veresiye isteme oranı %12 ile neredeyse hiç işlemiyordu — altmış günde üç hesap. Oran **%40'a** çıkarıldı; artık günlük bir karar.

---

## 20. Personel huyları ve moral

[14-personel-sistemi.md](14-personel-sistemi.md) on iki huy ve bir moral tablosu yazmıştı. Çekirdekte `huy` diye bir şey **yoktu**.

### Huylar: ikisi bedelsiz göründü, ölçüm sordu

Üreteç bir denge kuralı dayatıyor — docs/14'ün kendi cümlesi: *"hiçbir huy saf iyi veya saf kötü değil."* Kural ilk yazıldığında **iki huyu yakaladı**: müşteriyle iyi anlaşan (+8 memnuniyet) ve ekip moralini yükselten (+10 moral). İkisinin de etki tablosunda hiçbir bedeli yok.

Ama bedelleri **var**, sadece huyun içinde değil **havuzun** içinde: her birinin bir kötü ikizi var (suratsız, huysuz) ve ikisi çakışıyor. İşe alım ikisinden birini çekiyor — yani iyi huy seçilen bir avantaj değil, bir **şans**. Kural buna göre yazıldı: *bir huyun ya kendi içinde bedeli olacak, ya çakıştığı bir huy onun aynası olacak.*

Çakışmalar yüklemede **simetri** açısından da denetleniyor: A ile B çakışıyorsa B ile A da çakışmalı. Tek yönlü yazılmış bir çakışma, işe alım kodunda sessizce çalışmayan bir kural bırakırdı — yani iki çakışan huyu taşıyan bir personel üretilebilirdi.

### Hız artık tek bir çarpanda toplanıyor

Deneyim + huy − moral − yoğunluk − yorgunluk. Hepsi aynı yerde, çünkü hepsi aynı şeyi söylüyor: bu kişi bu işi ne kadar çabuk bitiriyor. Taban %20 — üç kötü huyun üst üste gelmesi kişiyi tamamen durdurmasın diye.

**Yoğun dilim mutfağa göre türetiliyor**, sabit "ikinci dilim" değil: [28](28-zirve-karari.md) Karar G payları değil **süreleri** değiştirdi, yani mutfağın zirvesi dilim uzunluğunda yazıyor. Sabit yazmak, Türk lokantasının öğle zirvesini fast food'a da dayatırdı.

### Moral: tablo bir OLAY listesi, sürüklenme modeli değil

İlk uygulama docs/14'ün tablosunu birebir aldı ve ölçüm reddetti: **iyi yönetilen bir dükkânda bile bütün kadro bir ayda istifa ediyordu.** Sebep tablonun eksikliği değil, tablonun **ne olduğu**: docs/14 bir olay listesi veriyor (maaş +5 / −25, yoğun gün −3, izin +10, zam +15), ve bunların yarısı oyuncunun *yapması* gereken şeyler. Sadece olayları uygulayınca merdiven tek yönlü aşağı gidiyor.

İki düzeltme:

1. **Sakin gün toparlatıyor** (+2), ama başlangıç moralini **geçmiyor**. Yani sakin günler personeli mutlu etmiyor, yalnızca normale döndürüyor; üstüne çıkmak için oyuncunun bir şey yapması lazım.
2. **"Yoğun gün" mutlak eşik değil, kadroya göre eşik.** İlk tanım "masa başına üç grup" idi ve iyi yönetilen bir dükkânda her gün yoğun sayılıyordu. Doğrusu: mutfak **tasarım kapasitesinin** üstünde çalıştıysa yoğun. Aynı müşteri sayısı iki aşçıyla sakin, bir aşçıyla yorucu.

### Ve bir kurucu hatası

Başlangıç kadrosu huy ve moral **almıyordu** — `RollTraits` yalnızca `Hire` içinden çağrılıyordu, oysa oyun bir aşçıyla başlıyor. O aşçının morali **0** ile başlıyordu, yani istifa eşiğinin altında: restoran **ikinci gün aşçısız** kalıyordu. Günlük iz olmadan bu hata "moral sistemi çok sert" diye yanlış teşhis edilirdi.

**Ders:** bir alanın varsayılanı `0` ise ve `0` anlamlı bir değerse (burada "morali sıfır"), o alanı kuran her yolu ayrı ayrı sormak gerekiyor. `Hire` yolu doğruydu; kurucu yolu hiç yoktu.

---

## 21. Huy sistemi dengeyi bozdu, ve sebebi üç kez BOT çıktı

Huylar kodlanınca fast food'da iyi oyuncunun itibarı **96,5'ten 87'ye** indi ve bazı koşularda dükkân boşaldı. Denge kaybı gerçekti; sebebi ekonomi değildi.

### Ölçüm nasıl daraltıldı

Tahmin etmek yerine **her etkiyi tek tek kapatıp** koştum:

| Yalnızca bu etki açık | `makul` itibarı |
|---|---:|
| hiçbiri | 96,5 |
| `speedBp` | 95,7 |
| `peakPenaltyBp` | 95,5 |
| `fatiguePenaltyBp` | 98,6 |
| `cleanlinessBp` | 96,5 |
| `satisfactionCenti` | 97,0 |
| `xpBp` | 99,9 |
| **`wageBp`** | **86,9** |

Tek bir alan. Ve `wageBp`'nin kendisi zararsız — kişi başı ücret ortalamada **+%1,2**. Zarar veren şey **oynaklık**: eşiğe bağlı kararlar (kredi çekme, ekipman alma, genişleme) tek yönlü, yani kötü bir çekiliş kalıcı olarak geriletiyor, iyi bir çekiliş takvimin önüne geçiremiyor.

### Eksik olan şey mekanik değil, KARAR'dı

docs/14 cevabı zaten yazmıştı ve ben yarısını kodlamıştım: *"İşe alım ekranında aynı anda **üç aday** görünür. Adaylar üretilir: rol, iki huy, görünüm, isim. Aday havuzu her üç günde bir yenilenir. Beğenmediğin adayı reddedebilirsin ama yenisi hemen gelmez."*

Havuz olmadan huy bir **piyango**: pahalı bir kadro çekiyorsun ve bununla yaşıyorsun. Havuzla birlikte bir **karar**: üç adaya bakıp seçiyorsun.

Havuz yazıldı (`CandidateSlots = 3`, üç günde bir yenileniyor, alınan adayın yeri hemen dolmuyor) ve denge aracına iki şey eklendi:

1. **`Hiring.Pick`** — adaylar arasından hız + memnuniyet − ücret en yükseği.
2. **`Hiring.ReplaceWorst`** — elindeki kişi kapıdaki adaydan belirgin olarak kötüyse değiştir. Değişim bedelli: gidenin deneyimi sıfırlanıyor.

İkincisi ilk yazımda **hiç tetiklenmedi** — eşiği 3.000 koymuştum, huy puanlarının toplam yayılımı ise ~4.700. Eşik 1.200'e inince:

| | huysuz taban | huylar açık, botsuz | huylar + **seçim** |
|---|---:|---:|---:|
| `makul` kasa | 24.540 | 20.886 | **28.617** |
| `makul` itibar | 96,5 | 87,5 | **99,3** |
| `genislemeyen` itibar | — | 61,0 | **71,3** |

Yani huy sistemi, oyuncu **kadrosunu yönetince** dengeyi bozmuyor — **iyileştiriyor.**

### Başlangıç aşçısı huysuz

Bir şey daha çıktı: başlangıç aşçısını oyuncu **seçmiyor**, oyun veriyor. Ona rastgele huy atmak, kampanyanın ilk gününde görünmez bir zar atmak demek — ve ölçümde kötü huylu bir başlangıç aşçısı çeken koşuda memnuniyet 9.000'den 5.000'e sızıyor, dükkân dört masada kalıyor ve altmış gün toparlanamıyordu. Devraldığın aşçı **sıradan**; karakter, **seçtiğin** kişilerle geliyor.

### Ve bir hedef, tavana çarptığı için yanlış ölçmeye başladı

Kalibrasyon "müdahale itibarı artırmalı" diyordu. `makul` kendi kadrosunu yönetmeyi öğrenip **99,3**'e çıkınca müdahalenin yükseltecek yeri kalmadı ve mekanik "işe yaramıyor" göründü — aynı koşuda Türk mutfağında müdahaleci 100,0, makul 96,1, yani **boşluk olan yerde çalışıyor.**

Hedef "her zaman daha iyi"den "hiçbir zaman daha kötü değil"e çevrildi (3 puan tolerans). Bu §18'in dersinin aynısı, bu kez *ölçen* tarafta: **bir mekaniği, oyuncunun zaten doymuş olduğu bir eksende ölçmek onu haksız yere mahkûm eder.**

---

## 22. Unity görünüm katmanı: ilk dilim

Altmış günlük bir ekonomi, iki mutfak, iki imza mekaniği, isimli müşteriler ve huylu personel — hepsi vardı ve **hiçbiri ekranda değildi**. `unity/Assets/Lokanta/` altında yalnızca dört editör betiği duruyordu.

Bu dilim oyunu oynanabilir yapmıyor; **görülebilir** yapıyor. Amaç, en büyük bilinmeyeni açmak: bu eğlenceli mi?

### İçerik platformun arkasına alındı

Yükleyici dosya yolu okuyordu. Android'de dosya yolu diye bir şey yok: içerik APK'nın içinde. `IContentSource` portu yazıldı ([26](04-mimari.md)'nın "platform portun arkasında" kuralı) ve iki uygulaması var:

| Kaynak | Nerede |
|---|---|
| `DirectoryContentSource` | testler, denge aracı, editör |
| `ResourcesContentSource` | oyun; Unity Resources, her platformda **senkron** |

StreamingAssets seçilmedi çünkü Android'de yalnızca `UnityWebRequest` ile ve **asenkron** okunuyor; içerik yükleme senkron bir işlem (doğrulama yapıyor, hata fırlatıyor) ve onu asenkron yapmak bütün zinciri bulaştırırdı. Bedeli, `content/`'in `Assets/Resources/content/` altına kopyalanması — **Lokanta > İçeriği Resources'a kopyala**, ve yanında **İçerik kopyası güncel mi** kontrolü. Bayat bir kopya, dengenin değişmiş gibi görünmesine yol açar ve sebebi saatler alır.

### Kat planı çalışma zamanına taşındı — ve ilk kez test edildi

Plan yalnızca `Editor/RoomLayout.cs` içindeydi, yani **çalışma zamanı planı bilmiyordu**. Aynı sayıları iki yere yazmak bu projede dört kez sessizce ayrıştı; plan `Lokanta.Game.RoomPlan`'a taşındı ve editör aracı artık ondan **türetiyor** (orada kalan tek şey renk).

Taşınınca bir şey daha oldu: plan `Lokanta.Core.csproj`'a girdi ve **testlenebilir** hale geldi. `RoomPlanTests` altı şey soruyor ve hepsi geçiyor:

- odalar arsayı **tam** kaplıyor (172,80 m² = 172,80 m²)
- odalar üst üste binmiyor, arsanın dışına taşmıyor
- masa sayıları **kademe tablosuyla aynı**: 4 / 7 / 10 / 14 — plan ile ekonomi aynı şeyi söylemeli, yoksa oyuncu satın aldığı masayı salonda göremez
- her salon odası masalarını alıyor (`Fit()` epsilonu olmadan bir sütun kayboluyor)
- masa noktaları kendi odasının içinde
- en yakın iki masa 1,70 m — dokunma hedefi çakışmıyor

Bunların hepsi daha önce **yalnızca Unity açılınca** sınanıyordu.

### Ve `float` yasağının kapsamı yazıldı

Plan çekirdek derlemesine girince [23](23-cekirdek-sozlesmesi.md) §2.5'in yansıma testi onu yakaladı: `PlotW : Single`. Kural doğru ve gevşetilmedi — **kapsamı yazıldı**: tarama artık yalnızca `Lokanta.Core.*` ad alanını geziyor.

Ayrım şu: determinizm **simülasyona** ait, çünkü aynı komut dizisi telefonda ve denge aracında aynı sonucu vermeli. Kat planı sonuca girmiyor, ekranda nerede durduğunu söylüyor; 18,0 × 9,6 m bir arsayı tamsayıyla yazmak, ölçüyü santimetreye çevirip her yerde bölmek olurdu ve hiçbir şey kazandırmazdı. Bu ayrım **yazılmadığı sürece** bir gün birisi simülasyona float sokmak için bu testi gevşetir.

### Yazılanlar

| Dosya | İş |
|---|---|
| `GameHost` | İçeriği yükler, `Simulation`'ı kurar, **sabit adımlı** tick döngüsünü sürer |
| `RestaurantView` | Odaları ve masaları kurar, her karede simülasyondan **okuyarak** günceller |
| `CameraRig` | İki kademeli kamera: genel görünüm ↔ odaya yaklaşma |
| `HudView` | Gün, kasa, itibar, aşama düğmeleri |
| `Editor/BuildGameScene` | Sahneyi **kurar** — elle kurulan sahne, kimin neyi nereye bağladığını kimsenin hatırlamadığı bir dosyaya döner |

**Sabit adım en önemli satır.** Unity'nin değişken `deltaTime`'ı çekirdeğe hiç girmiyor; `GameHost`'ta bir biriktirici var ve o kadar. Artık **atılmıyor**, biriktiriliyor — atmak, yavaş karede günü kısaltırdı. Bir karede en fazla 400 tick işleniyor ki geç bir kare "ölüm sarmalı" üretmesin.

Görünüm katmanı simülasyonu **okur, ona yazmaz**; yazan tek şey komut. Kayıt ve tekrar oynatma bunun üzerine kurulu.

### Ne yapılmadı

Bu dilim kutu-prizma bir yer tutucu. Müşteri figürü yok, animasyon yok, gerçek arayüz yok (HUD bilerek IMGUI — işi tasarım değil, görünürlük). Odaya yaklaşınca dokunma hedefinin **masa takımına** dönmesi de yazılmadı; kamera yaklaşıyor ama hedef hâlâ oda. [16](16-ekranlar-ve-ogretici.md)'nın on dört ekranı ve [24](24-sanat-hatti.md)'ün mesh hattı önümüzde.

**Bu katman burada derlenmedi.** `UnityEngine`'e bağlı olduğu için `dotnet build` onu görmüyor; ilk derleme Unity açıldığında olacak.

---

## 23. Sıradakiler

- ~~Malzeme kalitesi~~ **yazıldı** (§9).
- ~~`orderPreference` ölü~~ **türetilerek yazıldı** (§11).
- ~~Özel ekipman adlandırılmalı~~ **mekanizma yazıldı** (§10). Kalan iş **içerik**: döner ve pide gibi yemekler menüye eklenmeli ki `doner_ocagi` ve `pide_firini` onlara bağlanabilsin. docs/09 mutfak başına 10 istasyon planlıyor, şu an Türk'te 1, fast food'da 2 var.
- ~~Denetleyicinin kör noktası~~ **kapandı**: üçüncü kontrol yazıldı ve 19 alan buldu (§8).
- ~~Patron müdahalesi~~ **kısıtları yazıldı** (§12), dördüncü tür de (§13).
- ~~İstasyon hızlandırma~~ **yazıldı** (§13); `InterventionKind` dört değer taşıyor.
- ~~Hal fiyat oynaklığı~~ **yazıldı** (§14).
- ~~Personel deneyimi~~ **yazıldı** (§15).
- ~~`weeklyWageMultiplierBp`, `unlockSeason`, `ownerPool`, `SalonWorkMicro`~~ **kapandı** (§16).
- **Kalan kuyruk artık içerik ve arayüz işi**, ölü sistem değil:
  - ~~döner/pide~~ **yazıldı** (§17). Adlandırılmış ekipman sayısı Türk'te 3, fast food'da 2; docs/09 mutfak başına 10 istiyor, yani hâlâ içerik işi var.
  - denetleyicinin 4. kontrolü, docs/13 şemasında olup üretilen içerikte olmayan 63 alan sayıyor. Bunlar ölü alan değil, **yazılmamış içerik**: şema, denge aracının ürettiğinden daha geniş bir oyunu tarif ediyor.
  - ~~oda görünümü~~ **ilk dilimi yazıldı** (§22): iki kademeli kamera ve kat planı sahnede. Yerleşim ekranı ([16](16-ekranlar-ve-ogretici.md) ekran 14) ve odaya yaklaşınca masa takımına dönen dokunma hedefi hâlâ yazılmadı.
  - ~~imza mekanikleri~~ **yazıldı** (§18); İtalyan (`courses`) ve Japon (`broth`) blokları şema tarafında hazır, o mutfaklar yazılınca kodlanacak.
  - ~~isimli düzenli müşteriler~~ **yazıldı** (§19).
  - ~~personel huyları ve moral~~ **yazıldı** (§20).
  - **iş yükseltmeleri** (`content/upgrades.json`, [13](13-veri-semalari.md)) ve **yıl sonu puanlaması** (`scoreAxis`, [08](08-oyun-sonu.md)) hâlâ yazılmadı.
  - **Unity görünüm katmanı** — sıradaki büyük iş.
- **`SalonWorkMicro`** çekirdekte hiç okunmuyor; ya kullanılmalı ya silinmeli.
