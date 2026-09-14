# Zaman Modeli

**Son güncelleme:** 10 Eylül 2026
**Kütük maddeleri:** [23-cekirdek-sozlesmesi.md](23-cekirdek-sozlesmesi.md) karar bekleyen madde 1, A4 ekonomi sayıları, A8 personel kapasitesi
**Durum:** Parti B. Bu dosya bağlayıcıdır. Bütün süreler `tools/balance/timing.py` tarafından türetiliyor; elle yazılmış milisaniye yok.

**Doğrulama:** `python tools/balance/timing.py --check` — 47 tutarlılık testi.

---

## Neden bu dosya var

Üç dosya birbiriyle çelişiyordu.

| Kaynak | İddia |
|---|---|
| [23-cekirdek-sozlesmesi.md](23-cekirdek-sozlesmesi.md) §1.3 | Servis günü 4.800 tick = 480.000 ms |
| [14-personel-sistemi.md](14-personel-sistemi.md) | Aşçı kapasitesi 28 müşteri/gün |
| `content/dishes/fastfood.json` | Hamburger `prepMs` = 75.000 |

Aritmetik: 480.000 ÷ 28 = **17.143 ms**, bir müşterinin aşçıdan aldığı toplam mesai. Tek bir hamburger 75.000 ms tutuyorsa bir aşçı günde 480.000 ÷ 75.000 = **6,4 hamburger** yapar, 28 müşterilik iş değil. Üretilen `prepMs` değerleri ile kapasite modeli menü ortalamasında **4,35 kat**, hamburgerde **7,5 kat** ayrıydı.

İkinci çelişki: zirve günde 97 müşteri, 14 masa, 4 aşçı, 7 salon personeli. Hiçbir yerde 97 müşterinin 480.000 ms'ye sığıp sığmadığı hesaplanmamıştı.

Üçüncü çelişki: kurye arketipinin sabrı 8.000 ms. Oturma + sipariş + pişirme + servis sıfır yükte 26.000 ms sürüyor. Kurye her seferinde sinirli çıkıp gidiyordu.

Bu dosya üçünü tek bir aritmetiğe bağlıyor.

---

## 1. Servis günü uzunluğu

### 1.1 Gün uzunluğu doluluğu değiştirmez

Önce yanlış bir sezgiyi kapatmak gerekiyor: "gün kısa geliyorsa uzatalım."

Aşağıdaki bölüm 2 bütün görev sürelerini **günün oranı olarak** tanımlıyor: garson mesaisi gün ÷ 25, aşçı mesaisi gün ÷ 28. Gün iki katına çıkarsa bütün süreler iki katına çıkar ve doluluk yüzdeleri **aynı kalır**. Yani gün uzunluğu fizibiliteyi ne düzeltir ne bozar.

Gün uzunluğunun gerçekten belirlediği üç şey var:

| Ne | Nasıl bağlı |
|---|---|
| Oturum uzunluğu | Bir gün bir oturuşta bitmeli |
| Kampanya süresi | 60 gün × gün uzunluğu |
| Mutlak içerik sabitleriyle oran | `patienceMs` (8.000–30.000) ve `prepMs` mutlak sayılar; gün büyürse türetilen süreler büyür, sabır büyümez |

Üçüncü satır kritik ve gün uzunluğunu **her iki yönden** sınırlıyor.

### 1.2 Üst sınır: sabır

Zirve dilimde harcanan ortalama sabır (bölüm 6) 480.000 ms'lik günde 11.288 ms. Bu sayı gün uzunluğuyla doğru orantılı: 11.288 ÷ 480.000 = **0,023517 × gün**.

Arketip sabır basamaklarında 10.000 ms'den sonraki basamak 13.000 ms. Zirvede kaybedilen kitlenin 10.000 ms bandında kalması için:

```
0,023517 × gün < 13.000  →  gün < 552.800 ms
```

Gün 552.800 ms'yi aşarsa 13.000 ms sabırlı arketipler de (çocuklu ebeveyn, %3,8 pay) zirvede kaybedilmeye başlar ve zirve dilimi kaybı %10,2'den %14,0'a çıkar.

### 1.3 Alt sınır: okunabilirlik

En basit yemeğin (ızgara, karmaşıklık 1) `prepMs` değeri 10.000 ms, yani 0,0208333 × gün. Bir pişirme animasyonunun okunabilmesi için alt sınır 8.000 ms kabul edildi:

```
0,0208333 × gün ≥ 8.000  →  gün ≥ 384.000 ms
```

Bunun altında ızgara animasyonu bir çakma oluyor ve `station` alanı görsel olarak anlamsızlaşıyor.

### 1.4 Karar

Bant **384.000 – 552.800 ms**. Mevcut değer 480.000 ms bandın ortasında ve şu üç ölçütü de tutturuyor:

| Ölçüt | Değer | Hedef | Sonuç |
|---|---|---|---|
| Servis günü | 480.000 ms = 8,0 dk | — | — |
| Gün toplamı (servis + sabah/akşam 1.200 tick) | 6.000 tick = 10,0 dk | ≤ 12 dk, tek oturuş | Geçti |
| Dokunuş aralığı (60 dokunuş) | 480.000 ÷ 60 = 8.000 ms | 6.000–12.000 ms | Geçti |
| Kampanya, 1x | 60 × 6.000 tick = 360.000 tick = **10,00 saat** | 8–14 saat | Geçti |
| Kampanya, 2x | **5,00 saat** | — | — |

Kampanya süresi tam 10 saat çıkıyor çünkü 60 × 600.000 ms = 36.000.000 ms.

**Karar A: servis günü 4.800 tick (480.000 ms) olarak kalıyor. 60 günlük kampanya oyuncuya 1x hızda 10 saat 0 dakika, 2x hızda 5 saat 0 dakika oynanış veriyor. Gün uzunluğu değişmediği için kimseye yeni bir maliyet çıkmıyor. [23-cekirdek-sozlesmesi.md](23-cekirdek-sozlesmesi.md) karar bekleyen madde 1 kapandı.**

---

## 2. Görev süreleri

### 2.1 Türetme kuralı

Tek kural: **bir rolün bir müşteri için harcadığı toplam milisaniye = servis günü ÷ o rolün kapasitesi.** Böylece "günde 25 müşteri" cümlesi ile "müşteri başına 19.200 ms" cümlesi aynı cümle olur.

| Rol | Havuz | Kapasite | Gün ÷ kapasite | Kullanılan ms | Geri çarpım | Artık |
|---|---|---|---|---|---|---|
| Aşçı | mutfak | 28 | 17.142,857 | **17.143** | 480.004 | +4 ms |
| Garson | salon | 25 | 19.200,000 | **19.200** | 480.000 | **0** |
| Bulaşıkçı | salon | 46 | 10.434,783 | **10.435** | 480.010 | +10 ms |
| Kasiyer | salon | 66 | 7.272,727 | **7.273** | 480.018 | +18 ms |

Yuvarlama [23-cekirdek-sozlesmesi.md](23-cekirdek-sozlesmesi.md) §2.3 kuralıyla: yarısı sıfırdan uzağa. Artıklar müşteri başına 1 ms'nin altında; 28 müşteride 4 ms, günün on binde birinden küçük. Doğrulama testleri bu payı `± kapasite` toleransıyla kabul ediyor.

Salon havuzu toplamı:

```
19.200 + 10.435 + 7.273 = 36.908 ms
480.000 × 0,0768907 (model.py SALON_LOAD) = 36.907,5 → 36.908, birebir
```

### 2.2 Müşterinin yolculuğu

| Görev | Birim | Havuz | ms | Masayı bloke eder mi |
|---|---|---|---|---|
| Masa bekleme | — | yok | 0 | Hayır, henüz masası yok |
| Karşılama ve oturtma | kişi | salon / garson | 3.200 | Evet |
| Sipariş alma | kişi | salon / garson | 7.000 | Evet |
| Pişirme beklemesi | masa | mutfak / aşçı | 20.000 | Evet |
| Servis | kişi | salon / garson | 9.000 | Evet |
| Yeme | masa | yok | 38.000 | Evet |
| Ödeme | kişi | salon / kasiyer | 7.273 | Evet |
| Masayı toplama | kişi | salon / bulaşıkçı | 4.435 | Evet |
| Bulaşık | kişi | salon / bulaşıkçı | 6.000 | **Hayır**, evyede |

Toplamlar:

```
garson     : 3.200 + 7.000 + 9.000        = 19.200 = gün ÷ 25   TAM
bulaşıkçı  : 4.435 + 6.000                = 10.435 = gün ÷ 46
kasiyer    : 7.273                        =  7.273 = gün ÷ 66
salon      : 19.200 + 10.435 + 7.273      = 36.908 = gün × 0,0768907
mutfak     : 2,2 kalem × ortalama pişirme = 17.127 ≈ gün ÷ 28   (bölüm 4)
```

**Bulaşığın masadan ayrılması bilinçli.** Tabak masadan kalkınca masa boşalır, yıkama arkada devam eder. Bu, [14-personel-sistemi.md](14-personel-sistemi.md)'deki "tabak biterse servis durur" darboğazını korurken masa devrini serbest bırakıyor. Masayı bloke eden personel süresi:

```
36.908 − 6.000 = 30.908 ms/kişi
```

### 2.3 Masa devri

Bir masa bir kişiyi değil bir **grubu** ağırlıyor. `content/archetypes/*.json` ölçümü (ağırlık × ortalama grup büyüklüğü):

| Ölçüm | Değer |
|---|---|
| Ortalama grup büyüklüğü, tüm arketipler | 2,1195 kişi |
| Ortalama grup büyüklüğü, oturanlar (kurye hariç) | **2,1847 kişi** |
| Kurye kafa payı (masa işgal etmez) | %2,59 |

Kurye [11-musteri-sistemi.md](11-musteri-sistemi.md)'de "paket alan, masa işgal etmez" olarak tanımlı; masa hesabından düşülüyor.

```
masa devri = 2,1847 × 30.908 + 20.000 + 38.000
           = 67.525 + 20.000 + 38.000
           = 125.525 ms  (1.255 tick)
```

Kişi başına masa süresi 125.525 ÷ 2,1847 = **57.457 ms**.

**Karar B: görev süreleri yukarıdaki tabloda. Garson mesaisi × 25, bulaşıkçı × 46, kasiyer × 66 ve aşçı × 28 servis gününü ±1 ms/müşteri hatayla geri veriyor. Bir masa devri 125.525 ms.**

### 2.4 "~120 saniyelik servis" ne demekti

[23-cekirdek-sozlesmesi.md](23-cekirdek-sozlesmesi.md) §1.3 "bir müşteri servisi ~1.200 tick, 120 s" diyor ve `content/economy.json` `serviceMs: 120000` yazıyor. Bu sayı **müşteri başına değil, masa devri başına** doğru:

| Okuma | Değer | Doğru mu |
|---|---|---|
| Müşteri başına | 57.457 ms (575 tick) | 120 s iki kat fazla |
| Masa devri başına | 125.525 ms (1.255 tick) | 120 s ile %4,6 fark |

§1.3'teki satır "bir masa devri ~1.255 tick" olarak düzeltilmeli, `serviceMs` alanı `tableTurnMs: 125525` olmalı.

---

## 3. Eş zamanlılık

### 3.1 Karar noktası

Bir aşçı aynı anda tek yemekle mi ilgilenir, yoksa fırın pişirirken doğrama yapabilir mi? Bu, `prepMs`'in ne olduğunu belirliyor.

| Model | `prepMs` ne | Sonuç |
|---|---|---|
| Tek yemek | Aşçının meşgul süresi | Ortalama `prepMs` = 17.143 ÷ 2,2 = **7.792 ms** |
| Eş zamanlı | Duvar saati; meşguliyet ayrı | Ortalama `prepMs` ≈ 21.306 ms, meşguliyet 7.788 ms |

Tek yemek modelinde bütün yemekler 8 saniyenin altına iniyor. O ölçekte fırınla ızgara arasında görsel fark kalmıyor, `station` alanı yalnızca bir ikon seçicisine dönüşüyor ve ekipman yükseltmesinin anlatacağı hikaye kalmıyor.

### 3.2 Seçilen model

```
cookBusyMs = prepMs × attendBp / 10000        aşçı havuzunu tüketen sayı
prepMs     = yemeğin duvar saati süresi        oyuncunun gördüğü sayı
```

`attendBp` istasyonun fiziksel doğası: duvar saatinin yüzde kaçı aşçının **elinde** geçiyor.

| İstasyon | `attendBp` | Neden |
|---|---|---|
| İçecek | 10000 | Bardağı doldurur, boşluk yok |
| Soğuk | 10000 | Doğrama, tamamen elde |
| Tatlı | 8000 | Tabak süsleme |
| Izgara | 5600 | Koymak, çevirmek, almak; arada boşluk var |
| Ocak | 3500 | Sepeti daldırır, bırakır |
| Fırın | 2000 | Koyar, kapatır, gider |

### 3.3 Sonuçları

**`station` alanı yük taşıyor.** Artık üç şeyi birden söylüyor: hangi ekipman gerekli, aşçıyı ne kadar bağlıyor (`attendBp`), ve aynı anda kaç tabak alabiliyor (yuva sayısı).

**Ekipman yükseltmesi iki eksende çalışıyor, ve hiçbiri `prepMs`'i kısaltmıyor:**

| Eksen | Ne yapar | Örnek |
|---|---|---|
| Yuva | İstasyonun eş zamanlı tabak sayısını artırır | İkinci fritöz |
| `attendBp` | Aşçıyı daha erken serbest bırakır | Çift taraflı ızgara: çevirme yok, 5600 → 3500 |

Yemeğin pişme süresi fizik; yükseltme paralelliği satın alıyor, zamanı değil. Bu, "ekipman satın alınca her şey hızlanır" enflasyonunu baştan kapatıyor.

**Zirvede gereken yuva sayısı** (dilim payı %30, 97 müşteri):

| İstasyon | Eş zamanlı tabak | Gereken yuva |
|---|---|---|
| Izgara | 3,23 | 4 |
| Ocak | 3,33 | 4 |
| Fırın | 1,13 | 2 |
| İçecek | 0,68 | 1 |
| Soğuk | 0,20 | 1 |
| Tatlı | 0,08 | 1 |

Toplam 8,66 eş zamanlı tabak, 4 aşçı ile. Yani her aşçı ortalama 2,2 tabak taşıyor; eş zamanlılığın görünür karşılığı bu. Meşguliyet toplamı 4,15 aşçı-birimi, dilim doluluğu %103,9 ile birebir tutuyor.

**Karar D: bir aşçı birden fazla istasyonu eş zamanlı yürütür. `prepMs` duvar saatidir, aşçı meşguliyeti `prepMs × attendBp / 10000`'dir. `station` alanı `attendBp` ve yuva sayısını taşır. Ekipman yükseltmesi yuva ekler veya `attendBp` düşürür, `prepMs`'e dokunmaz. Kademe 4'te ızgara ve ocak 4'er, fırın 2, diğerleri 1'er yuva ister.**

---

## 4. prepMs formülü

### 4.1 Müşteri başına kaç kalem

Formülün girdisi. `content/dishes/fastfood.json` menü yapısı 12 ana + 8 yan + 6 içecek + 6 tatlı. İmza mekaniği kombo ([07-mutfak-sistemi.md](07-mutfak-sistemi.md), `economy.json` `combo.items`) üç kalem: ana + yan + içecek. Kombo herkesin aldığı şey değil; kurye "tek kalem", yalnız müşteri 1–2 kalem alıyor.

Kabul edilen karışım:

| Grup | Kalem/müşteri | Menü ortalama karmaşıklık | Menü ortalama fiyat |
|---|---|---|---|
| Ana | 1,0 | 1,6667 | 4.516,7 |
| Yan | 0,6 | 1,1250 | 2.618,8 |
| İçecek | 0,5 | 1,0000 | 1.883,3 |
| Tatlı | 0,1 | 2,1667 | 3.925,0 |
| **Toplam** | **2,2** | — | — |

**Doğrulaması fiş tutarı.** Karışımı menü fiyatlarıyla çarpınca:

```
1,0 × 4.516,7 + 0,6 × 2.618,8 + 0,5 × 1.883,3 + 0,1 × 3.925,0
= 4.516,7 + 1.571,3 + 941,7 + 392,5
= 7.422,2 santi-sikke = 74,2 sikke
```

`model.py` sekizinci hafta hedef fişi 75 sikke. Sapma %1,0. Kombo indirimi (`priceBp` 8125) ile kombo fiş primi (`ticketBonusBp` 1500) birbirini yaklaşık götürüyor. **2,2 kalem tahmin değil, fişten okunan sayı.**

### 4.2 Formül

Ağırlıklı karmaşıklık birimi:

```
1,0 × 1,6667 + 0,6 × 1,1250 + 0,5 × 1,0000 + 0,1 × 2,1667 = 3,0584 birim/müşteri
```

Aşçı mesaisi bu birimlere dağıtılacak:

```
3,0584 × UNIT_BUSY = 17.143  →  UNIT_BUSY = 5.605,2
```

**`UNIT_BUSY = 5.600 ms` seçildi.** Sebep: bu değerle bütün istasyonlarda `prepMs` tamsayı çıkıyor (`5.600 × 10000 / attendBp` her `attendBp` için bölünüyor). Sapma 5.600 × 3,0584 = 17.127 ms, hedef 17.143 ms, **%0,09**.

```
cookBusyMs(karmaşıklık) = 5.600 × karmaşıklık
prepMs(istasyon, karmaşıklık) = 5.600 × karmaşıklık × 10000 / attendBp(istasyon)
```

### 4.3 Sonuç tablosu

| İstasyon | `attendBp` | Karmaşıklık 1 | Karmaşıklık 2 | Karmaşıklık 3 |
|---|---|---|---|---|
| İçecek | 10000 | 5.600 | 11.200 | 16.800 |
| Soğuk | 10000 | 5.600 | 11.200 | 16.800 |
| Tatlı | 8000 | 7.000 | 14.000 | 21.000 |
| Izgara | 5600 | **10.000** | **20.000** | **30.000** |
| Ocak | 3500 | 16.000 | 32.000 | 48.000 |
| Fırın | 2000 | 28.000 | 56.000 | 84.000 |

Aşçı meşguliyeti istasyondan bağımsız: karmaşıklık 1 için 5.600, 2 için 11.200, 3 için 16.800 ms. Bu yüzden kapasite kimliği menünün istasyon dağılımından etkilenmiyor.

Istasyonsuz referans değer soruluyorsa ızgara sütunu kullanılmalı: fast food'un ana istasyonu orası ve karmaşıklığa göre **10.000 / 20.000 / 30.000 ms**.

**Karar C: `prepMs = 5.600 × karmaşıklık × 10000 ÷ attendBp`. Müşteri başına 2,2 kalem, fiş tutarından doğrulandı. Karmaşıklık 1/2/3 için ızgarada 10.000 / 20.000 / 30.000 ms.**

### 4.4 İçerikle fark

Bu dosyanın yazılma sebebi olan ölçüm, 10 Eylül 2026 sabahı `content/dishes/fastfood.json`:

| Ölçüm | O günkü içerik | Türetilen | Oran |
|---|---|---|---|
| 32 yemeğin `prepMs` ortalaması | 92.656 ms | 21.306 ms | 4,35 kat |
| Hamburger | 75.000 ms | 10.000 ms | 7,5 kat |
| Çikolatalı kek (fırın, k=3) | 260.000 ms | 84.000 ms | 3,1 kat |
| Gazoz (içecek, k=1) | 20.000 ms | 5.600 ms | 3,6 kat |

**Ara durum.** İçerik bu analiz sürerken yeniden üretildi. Yeni değerler yalnızca karmaşıklığa bağlı, istasyon boyutu düşürülmüş:

| Mutfak | Karmaşıklık 1 | Karmaşıklık 2 | Karmaşıklık 3 |
|---|---|---|---|
| Fast food | 8.000 | 11.500 | 17.500 |
| Türk | 6.000 | 8.500 | 12.500 |

Bu ara durum da kapasite modelini tutturmuyor. Sipariş karışımıyla müşteri başına aşçı mesaisi:

```
1,0 × 10.333 + 0,6 × 8.438 + 0,5 × 8.000 + 0,1 × 12.917 = 20.688 ms
hedef 17.143 ms  →  sapma +%20,7
aşçı mesaisi sayılırsa: 480.000 ÷ 20.688 = 23,2 müşteri/gün, kapasite 28
```

Ayrıca `prepMs` istasyondan bağımsız hale gelmiş; Karar D'nin dayandığı `attendBp` boyutu kayboluyor ve fırın ile içecek aynı hızda pişiyor.

`content/dishes/fastfood.json` ve `content/dishes/turk.json` **bölüm 4.2 formülünden yeniden üretilmeli.** Bu dosyalara elle dokunulmadı; üretici `tools/balance/export.py` formülü çağıracak.

---

## 5. Zirve fizibilitesi

### 5.1 Gün ortalaması doluluk

Zirve gün: `model.py` sekizinci hafta hafta sonu. 97 müşteri, 4 aşçı, 7 salon personeli + patron 1,4 iş-günü, 14 masa.

| Havuz | Gereken ms | Var olan ms | Doluluk |
|---|---|---|---|
| Mutfak | 97 × 17.143 = 1.662.871 | 4 × 480.000 = 1.920.000 | **%86,6** |
| Salon | 97 × 36.908 = 3.580.076 | 8,4 × 480.000 = 4.032.000 | **%88,8** |
| Masa | 43,25 devir × 125.525 = 5.428.923 | 14 × 480.000 = 6.720.000 | **%80,8** |

Masa devir sayısı: 97 × (1 − 0,0259) = 94,49 oturan kişi ÷ 2,1847 = 43,25 grup. Masa başına 43,25 ÷ 14 = 3,09 devir/gün, devir başına günün %26,2'si.

**Üç havuz da %100'ün altında.** Gün ortalamasında 97 müşteri 14 masaya, 4 aşçıya ve 7 salon personeline sığıyor. Darboğaz salon, tam da [14-personel-sistemi.md](14-personel-sistemi.md)'nin istediği gibi.

### 5.2 Dilim yoğunlaşması havuzları patlatıyor

Gün ortalaması yetmiyor, çünkü müşteriler günün dört diliminde eşit dağılmıyor. Bir dilim günün dörtte biri, yani kapasitenin de dörtte biri. Dilim payı %25'i aşarsa o dilimde iş birikir.

`content/archetypes/*.json` ölçümü ([12-ekonomi.md](12-ekonomi.md) §5.6'nın gerçekleşmiş hâli):

| Mutfak | Dilim 1 | Dilim 2 | Dilim 3 | Dilim 4 |
|---|---|---|---|---|
| Fast food | %11,1 | **%37,5** | %16,2 | **%35,3** |
| Türk | %9,4 | **%60,6** | %17,5 | %12,5 |

Fast food öğle dilimi, mevcut içerik profiliyle:

| Havuz | Dilim doluluğu | Biriken iş | Dilim sonu boş bekleme |
|---|---|---|---|
| Mutfak | %129,9 | 143.577 ms | 35.894 ms |
| Salon | %133,2 | 334.528 ms | 39.825 ms |
| Masa | %121,2 | 355.846 ms | 25.418 ms |
| **Toplam** | — | — | **101.137 ms** (ortalama 50.568) |

Dilim 4 de (%35,3) aynı şekilde patlıyor: %122 / %125 / %114, toplam 74.083 ms.

Ortalama 50.568 ms boş bekleme, fast food havuzundaki **en sabırlı** arketipin (aile, 30.000 ms) bile sabrının 1,7 katı. Bu profille günün **%72,8'i** çıkıp gidiyor. Ekonomi modeli 97 müşterinin hepsinin servis edildiğini varsayıyor; %72,8 kayıpla bütün büyüme eğrisi çöker.

> **DÜZELTME, 10 Eylül 2026.** Buradaki %72,8 rakamı **açık döngü** hesabıdır ve gerçeğinden büyüktür. Sabrı biten müşteri çıkıp giderken kuyruktaki yerini de boşaltıyor, yani kendisinden sonrakinin beklemesini kısaltıyor. Kapalı döngü hesabında aynı profilin kaybı **%13,36**. Türk profili için %30,47. Yoğunluk tavanı (eşit dilimde %28,16) değişmiyor, doğru. Ayrıntı ve düzeltilmiş tablo: [28-zirve-karari.md](28-zirve-karari.md). Aşağıdaki F kararı bu düzeltmeden önce yazıldı; **28 numaralı doküman onun yerine geçer.**

### 5.3 Azami dilim payı

Hiçbir havuzun dilim içinde %100'ü aşmaması için:

```
azami dilim payı = 0,25 ÷ en yüksek havuz doluluğu
                 = 0,25 ÷ 0,888
                 = %28,2
```

### 5.4 Ne değişmeli

Üç seçenek var, ikisi imkânsız.

**Seçenek 1: kadroyu ve masayı büyüt.** %37,5'i emmek için her havuz 1,5 kat büyümeli:

| Kaynak | Gereken | Tavan | Sonuç |
|---|---|---|---|
| Salon personeli | 1,5 × 7,4584 − 1,4 = 9,79 → 10 | — | — |
| Aşçı | 1,5 × 3,464 = 5,20 → 6 | — | — |
| Toplam kadro | 16 | **12** | İmkânsız |
| Masa | 1,5 × 0,808 × 14 = 16,97 → 17 | **14** | İmkânsız |

Kadro tavanı 12, masa tavanı 14. Seçenek 1 hiçbir oyuncu kararıyla ulaşılamıyor.

**Seçenek 2: günü uzat.** Bölüm 1.1: doluluk ölçekten bağımsız. Hiçbir şey değişmiyor.

**Seçenek 3: geliş profilini yatıştır.** Tek çalışan seçenek. Dilim payı tavanı %30 konuyor.

| Profil | Mutfak | Salon | Masa | Ort. boş bekleme | Günlük kayıp |
|---|---|---|---|---|---|
| İçerik (%37,5) | %129,9 | %133,2 | %121,2 | 50.568 ms | %72,8 (açık döngü; kapalı döngüde %13,36) |
| Tavan (%28,2) | %97,5 | %100,0 | %91,0 | 0 ms | %0 |
| **Önerilen (%30)** | **%103,9** | **%106,5** | **%96,9** | **6.288 ms** | **%6,1** |

**%30 bilinçli olarak tavanın 1,8 puan üstünde.** Tam tavanda öğle zirvesi hiç kuyruk üretmez ve [12-ekonomi.md](12-ekonomi.md)'nin istediği "gerçek bir kriz anı" ortadan kalkar. %30'da zirve sabırsızları ısırıyor ama günü yıkmıyor.

Fast food için önerilen dağılım **%20 / %30 / %20 / %30**. İki keskin zirve korunuyor, zirvelerin sivriliği kırpılıyor.

### 5.5 Türk mutfağı

%60,6 hiçbir kadroyla mümkün değil: havuzlar %210 / %215 / %196'ya çıkıyor, salon 2,4 kat kadro istiyor, yani 17 salon personeli. Tavan 12.

Türk mutfağının kimliği "öğle zirvesi çok sert" ([10-mutfak-kimligi.md](10-mutfak-kimligi.md), [11-musteri-sistemi.md](11-musteri-sistemi.md)) ama bu kimlik **hangi dilimlerin dolu olduğuyla** taşınabilir, tek dilime yığmakla değil. Önerilen: **%15 / %30 / %30 / %25**. Öğle ve öğleden sonra dolu, akşam sönük — İtalyan'ın tam tersi, ve fizibil.

Alternatif, daha iddialı bir yol da var: Türk mutfağının servis günü daha kısa olur (esnaf lokantası öğlen açılır, akşam kapanır) ve günlük talep aynı oranda düşer. Bu, kimliği daha güçlü taşır ama talep formülünü mutfağa bağımlı hale getirir. Şimdilik açılmadı; bkz. karar bekleyen madde 2.

**Karar F: zirve gün doluluğu mutfak %86,6, salon %88,8, masa %80,8 — üçü de %100 altında, 97 müşteri sığıyor. Ama dilim payı %28,2'yi aşamaz. Mevcut içerik profilleri (fast food %37,5, Türk %60,6) fizibil değil ve kadro veya masa artırarak kurtarılamaz, çünkü gereken 16 personel ve 17 masa tavanların üstünde. `arrivalWeightsBp` değerleri hiçbir dilim %30'u aşmayacak şekilde yeniden normalleştirilmeli.**

---

## 6. Sabır

### 6.1 Eski tanım kırık

[12-ekonomi.md](12-ekonomi.md) §5.2: "Sabır; oturmayı, siparişin alınmasını ve yemeğin gelmesini beklerken azalır." Yani sabır kapıdan yemeğe kadar geçen **toplam duvar saati**.

Sıfır yükte, yani restoran bomboşken, hiç kuyruk yokken:

| Durum | Hesap | Süre |
|---|---|---|
| Oturan müşteri (ızgara k=1) | 3.200 + 7.000 + 10.000 + 9.000 | **29.200 ms** |
| Kurye, masasız (ızgara k=1) | 7.000 + 10.000 + 9.000 | **26.000 ms** |

Kuryenin sabrı 8.000 ms. Sıfır yükte bile gerekli sürenin **3,25 katı** kısa. Aceleci öğrenci 10.000 ms: 2,9 kat kısa. Şikayetçi müşteri 9.000 ms: 2,9 kat kısa.

Bu üç arketip, oyun mükemmel oynansa bile **her seferinde** sinirli çıkıp giderdi. Bu bir denge sorunu değil, tanım hatası.

İki çıkış yolu vardı:

| Yol | Bedeli |
|---|---|
| Sabır değerlerini büyüt | Zorunlu taban (29.200 ms) baskın olur; 8.000–30.000 aralığı 35.000–90.000'e sıkışır, arketipler arası 1:3,75 farkı 1:2,6'ya iner. Ayırt edicilik kaybolur |
| Sabrın neyi saydığını değiştir | İçerik değişmez, 1:3,75 farkı korunur |

İkincisi seçildi.

### 6.2 Yeni tanım

**Sabır sayacı yalnızca boş beklemede işler.** Boş bekleme = müşteriyle kimsenin ilgilenmediği ve yemeğinin de henüz başlamadığı süre:

- masa kuyruğu (boş masa yok),
- garson kuyruğu (oturdu, kimse sipariş almaya gelmedi),
- mutfak kuyruğu (sipariş verildi, boş aşçı yok, yemek henüz başlamadı),
- pass kuyruğu (yemek hazır, taşıyacak garson yok).

Pişirmenin kendisi boş bekleme değil — ama bedavaya da geçmiyor:

```
bekleme_puanı = boş_bekleme_ms + pişirme_ms × cookPatienceWeightBp / 10000
cookPatienceWeightBp = 2500
```

Yemeğin pişme süresi sabrın çeyrek ağırlığıyla sayılıyor. Bunun üç sonucu var:

1. **Sıfır yükte kimse kaybedilmiyor.** Boş bekleme sıfır, kalan yalnızca pişirme payı.
2. **Ağır menü hâlâ riskli.** Ocakta karmaşıklık 2 bir yemek (32.000 ms) tek başına 8.000 ms sabır yakıyor; kuryenin bütün sabrı. [23-cekirdek-sozlesmesi.md](23-cekirdek-sozlesmesi.md) §8.3'ün "ağır yemek çok satılırsa mutfak tıkanır" niyeti korunuyor ve doğrudan menü kararına bağlanıyor.
3. **Sabır saf bir kadro sinyali oluyor.** [14-personel-sistemi.md](14-personel-sistemi.md)'deki "eksik kadronun cezası" döngüsü (personel yetmiyor → bekleme uzuyor → sabır tükeniyor → itibar düşüyor) birebir bu sayaca karşılık geliyor. Oyuncu işe alarak düzeltebileceği bir şeyden cezalandırılıyor, düzeltemeyeceği bir şeyden değil.

[12-ekonomi.md](12-ekonomi.md) §5.4'teki memnuniyet formülü aynen çalışıyor: `−(beklenen ÷ sabır) × 60`, "beklenen" artık bekleme puanı.

### 6.3 Sayılar

| Durum | Boş bekleme | Pişirme payı | Bekleme puanı | Kurye (8.000) | Öğrenci (10.000) |
|---|---|---|---|---|---|
| Sıfır yük, ızgara k=1 | 0 | 2.500 | **2.500** | Kalıyor, memnuniyet 81 | Kalıyor, memnuniyet 85 |
| Sıfır yük, ocak k=2 | 0 | 8.000 | **8.000** | Çıkıyor | Kalıyor, memnuniyet 52 |
| Zirve ortalama (%30 profil) | 6.288 | 5.000 | **11.288** | Çıkıyor | Çıkıyor |
| Zirve azami (%30 profil) | 12.575 | 5.000 | **17.575** | Çıkıyor | Çıkıyor |
| Zirve, kurye (masasız) | 3.930 | 2.500 | **6.430** | **Kalıyor** | — |

Kurye masa ve mutfak kuyruğuna girmiyor, yalnızca salon kuyruğuna giriyor; bu yüzden zirvede ortalama olarak hayatta kalıyor. En sabırsız arketibin en dayanıklı arketip olması tesadüf değil: masa işgal etmemesi onu koruyor.

### 6.4 Zirvede ne kadar kaybediliyor

Fast food arketip havuzunda sabır dağılımı, kafa ağırlıklı:

| Sabır | Kafa payı | Zirvede (11.288 puan) |
|---|---|---|
| 8.000 (kurye) | %2,6 | Masasız, kalıyor |
| 9.000 (şikayetçi) | %1,0 | Çıkıyor |
| 10.000 (aceleci öğrenci) | %9,2 | Çıkıyor |
| 13.000 ve üstü | %87,2 | Kalıyor |

Zirve dilimde kaybedilen pay **%10,2** (şikayetçi %1,0 + aceleci öğrenci %9,2), gün ortalaması **%6,1** çünkü kuyruk yalnızca iki zirve diliminde oluşuyor: 0,30 × %10,2 × 2 = %6,1.

Kayıp ayrıca yalnızca hafta sonu günlerinde:

| Gün | Müşteri | Salon dilim doluluğu | Kuyruk | Kayıp |
|---|---|---|---|---|
| Hafta içi | 77 | %84,6 | Yok | %0 |
| Hafta sonu | 97 | %106,5 | Var | %6,1 |

Haftalık ciro etkisi: %6,1 × (2 × 97) ÷ (5 × 77 + 2 × 97) = **%2,05**.

`model.py` cirosu bu yüzden bir **üst sınır**; hafta sonu zirvesinde yaklaşık %2 aşağı okunmalı. Bu, marj hedeflerinin ondalık payının içinde kalıyor ve modele geri yazılması gerekmiyor. Ayrıca hesap kuyruğun bütün dilim boyunca ortalama seviyede olduğunu varsayıyor; gerçekte kuyruk dilimin ikinci yarısında oluşuyor, yani %6,1 kötümser tarafta.

**Karar E: sabır sayacı yalnızca boş beklemede işler; pişirme süresi `cookPatienceWeightBp = 2500` ağırlığıyla sayılır. Sıfır yükte minimum bekleme 2.500 puan (oturan, ızgara karmaşıklık 1), zirvede ortalama 11.288, azami 17.575. Bütün arketipler sıfır yükte servis edilebiliyor; zirve diliminde kafa payının %10,2'si kaybediliyor, gün ortalaması %6,1, haftalık ciro etkisi %2,05. `content/archetypes/*.json` içindeki `patienceMs` değerleri değişmiyor.**

---

## 7. Türetilen sabitler, tek tabloda

`tools/balance/timing.py` çıktısı.

| Sabit | Değer | Kaynak |
|---|---|---|
| `TICK_MS` | 100 | [23](23-cekirdek-sozlesmesi.md) §1.2 |
| `SERVICE_DAY_MS` | 480.000 | Karar A |
| `DAY_TICKS` | 6.000 | 4.800 servis + 1.200 sabah/akşam |
| `KITCHEN_MS` | 17.143 | gün ÷ 28 |
| `GARSON_MS` | 19.200 | gün ÷ 25 |
| `BULASIKCI_MS` | 10.435 | gün ÷ 46 |
| `KASIYER_MS` | 7.273 | gün ÷ 66 |
| `SALON_MS` | 36.908 | üçünün toplamı |
| `T_SEAT` | 3.200 | garson bölüşümü |
| `T_ORDER` | 7.000 | garson bölüşümü |
| `T_SERVE` | 9.000 | garson bölüşümü |
| `T_BUS` | 4.435 | bulaşıkçı bölüşümü, masayı bloke eder |
| `T_WASH` | 6.000 | bulaşıkçı bölüşümü, masayı bloke etmez |
| `T_PAY` | 7.273 | kasiyer |
| `T_EAT_PARTY` | 38.000 | mutfak parametresi, fast food |
| `COOK_WAIT_REF_MS` | 20.000 | ızgara karmaşıklık 2 |
| `TABLE_TURN_MS` | 125.525 | 2,1847 × 30.908 + 20.000 + 38.000 |
| `UNIT_BUSY_MS` | 5.600 | 17.143 ÷ 3,0584 |
| `COOK_PATIENCE_BP` | 2500 | Karar E |
| `GROUP_SIZE_SEATED` | 2,1847 | `content/archetypes` ölçümü |
| `DISHES_PER_CUSTOMER` | 2,2 | fiş tutarından doğrulandı |
| Azami dilim payı | %28,2 | 0,25 ÷ 0,888 |

---

## 8. İçeriğe ve dokümana çıkan işler

Bu dosya hiçbir içerik dosyasına dokunmadı. Aşağıdakiler ayrı bir işte yapılacak.

| Dosya | Ne değişecek | Neden |
|---|---|---|
| `content/dishes/fastfood.json` | 32 `prepMs` bölüm 4.2 formülünden üretilecek | Ara durum kapasiteden +%20,7 sapıyor ve istasyon boyutunu düşürüyor |
| `content/dishes/turk.json` | 32 `prepMs` bölüm 4.2 formülünden üretilecek | Aynı |
| `content/economy.json` | `serviceMs: 120000` → `tableTurnMs: 125525` | Bölüm 2.4 |
| `content/economy.json` | Yeni alan `cookPatienceWeightBp: 2500` | Karar E |
| `content/economy.json` veya yeni `content/stations.json` | İstasyon başına `attendBp` ve yuva sayısı | Karar D |
| `content/archetypes/fastfood.json` | `arrivalWeightsBp` %20/%30/%20/%30'a normalleştirilecek | Karar F |
| `content/archetypes/turk.json` | `arrivalWeightsBp` %15/%30/%30/%25'e normalleştirilecek | Karar F |
| `content/staff-roles.json` | İsteğe bağlı `msPerCustomer` alanı (17143 / 19200 / 10435 / 7273) | Bölüm 2.1, çekirdek türetebilir de |
| `tools/balance/export.py` | `timing.py`'yi kullanacak; `prepMs` üretimi formülden | Bölüm 4.2 |
| [23-cekirdek-sozlesmesi.md](23-cekirdek-sozlesmesi.md) §1.3 | "Bir müşteri servisi ~1.200 tick" → "Bir masa devri 1.255 tick, bir müşteri 575 tick" | Bölüm 2.4 |
| [23-cekirdek-sozlesmesi.md](23-cekirdek-sozlesmesi.md) karar bekleyen madde 1 | Silinecek, kapandı | Karar A |
| [12-ekonomi.md](12-ekonomi.md) §5.2 | Sabır tanımı yeniden yazılacak | Karar E |
| [12-ekonomi.md](12-ekonomi.md) §5.6 | Dilim tablosu %30 tavanına çekilecek | Karar F |
| [14-personel-sistemi.md](14-personel-sistemi.md) | Kapasite tablosuna müşteri başına ms sütunu | Bölüm 2.1 |

**`content/archetypes/*.json` içindeki `patienceMs` değerleri değişmiyor.** Karar E'nin varlık sebebi bu.

---

## 9. Doğrulama

`python tools/balance/timing.py --check` — 47 test, altı grup.

| Grup | Ne kontrol ediyor |
|---|---|
| A (3) | Gün uzunluğu, kampanya süresi, dokunuş aralığı |
| B (14) | Görev süreleri toplamları kapasiteyi birebir geri veriyor; her rolün ms'si × kapasitesi = servis günü |
| C (8) | `prepMs` türetimi, tamsayılık, doğrusallık, fiş çapraz doğrulaması, içerikle sapma |
| D (4) | Eş zamanlılık tutarlılığı, istasyon yuvası |
| E (8) | Sabır: sıfır yük, zirve, kayıp payı, eski tanımın kırık olduğunun ispatı |
| F (10) | Havuz doluluğu, dilim fizibilitesi, hafta içi/hafta sonu ayrımı |

Ayrıca `python tools/balance/timing.py` bütün türetilmiş sabitleri ve ara hesapları düz metin olarak yazıyor; `--md` markdown tablolarını üretiyor.

**Çapraz doğrulama:** `timing.py` içindeki `CAP_ASCI`, `CAP_GARSON`, `CAP_BULASIKCI`, `CAP_KASIYER`, `SALON_LOAD` ve `OWNER_WORK` değerleri `model.py` ile birebir aynı. İkisi ayrıldığı anda B grubu testleri düşer.

---

## Karar bekleyen ayrıntılar

1. `T_EAT_PARTY` mutfak başına parametre olacak. Fast food 38.000 ms. İtalyan "uzun oturur, masa devri düşük" ([07-mutfak-sistemi.md](07-mutfak-sistemi.md)); değeri masa doluluğunu %100'ün altında tutacak şekilde çözülmeli
2. Türk mutfağının servis günü kısaltılsın mı (esnaf lokantası öğlen açılır), yoksa dilim profili mi yatıştırılsın; ikincisi seçildi ama birincisi kimliği daha güçlü taşır
3. `attendBp` ekipman kademesiyle düşecek mi, yoksa yükseltme yalnızca yuva mı eklesin
4. Kurye dışında masa işgal etmeyen arketip olacak mı (paket servis kanalı)
5. `cookPatienceWeightBp` 2500 doğru ağırlık mı; oynanabilirlik testi söyleyecek
