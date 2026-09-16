# Zirve Kararı

**Son güncelleme:** 10 Eylül 2026
**Kütük maddeleri:** A4 ekonomi sayıları, A5 mutfak kimliği, A7 müşteri formülleri, A8 personel kapasitesi
**Durum:** Parti B. Bu dosya bağlayıcıdır ve [27-time-model.md](27-time-model.md) Karar F'yi **değiştirir**.

**Doğrulama:** `python tools/balance/timing.py --zirve` — bu dosyadaki bütün sayılar oradan çıkıyor. `python tools/balance/timing.py --check` — 47 tutarlılık testi, hepsi geçiyor; hiçbiri değiştirilmedi.

---

## 1. Çelişki

### 1.1 İki dosya, iki iddia

[12-economy.md](12-economy.md) §5.6 her mutfağa bir imza saat profili veriyor ve bunu kimliğin kalbi ilan ediyor:

> "Lokantada müşterilerin yüzde altmışı tek dilimde geliyor. Bu, öğle zirvesini gerçek bir kriz anına çeviriyor ve akşamı boş bırakıyor."

[27-time-model.md](27-time-model.md) Karar F aynı profili fizik olarak imkânsız buluyor ve `arrivalWeightsBp` değerlerinin hiçbir dilim %30'u aşmayacak şekilde yeniden normalleştirilmesini istiyor. Türk mutfağı için önerdiği profil **%15 / %30 / %30 / %25**. Bu, %60'lık öğle zirvesini siliyor.

### 1.2 İki tarafın sayıları

| Kaynak | Fast food | Türk mutfağı |
|---|---|---|
| [12](12-economy.md) §5.6 tablosu (yazılı) | %15 / %35 / %15 / %35 | %10 / %60 / %20 / %10 |
| `content/archetypes/*.json` (ölçülen) | %11,05 / **%37,49** / %16,20 / %35,26 | %9,37 / **%60,63** / %17,53 / %12,47 |
| [27](27-time-model.md) Karar F (önerilen) | %20 / %30 / %20 / %30 | %15 / %30 / %30 / %25 |

Yazılı tablo ile içeriğin ölçümü de birebir tutmuyor; §5.6 elle yazılmış, içerik `tools/content/gen_archetypes.py` ile üretilmiş. Bu ikinci bir çelişki ve aşağıda kapanıyor.

### 1.3 Karar F'nin dayandığı aritmetik

Zirve gün (`model.py` sekizinci hafta hafta sonu): 97 müşteri, 4 aşçı, 7 salon personeli + patron 1,4 iş-günü, 14 masa.

| Havuz | Gereken ms | Var olan ms | Gün doluluğu |
|---|---|---|---|
| Mutfak | 97 × 17.143 = 1.662.871 | 4 × 480.000 = 1.920.000 | %86,6 |
| Salon | 97 × 36.908 = 3.580.076 | 8,4 × 480.000 = 4.032.000 | **%88,8** |
| Masa | 43,25 × 125.525 = 5.428.923 | 14 × 480.000 = 6.720.000 | %80,8 |

Bir dilim günün dörtte biri, yani kapasitenin de dörtte biri. Hiçbir havuzun dilim içinde %100'ü aşmaması için:

```
azami dilim payı = 0,25 ÷ 0,8879 = %28,16
```

Türk mutfağının %60,63'ü bunun **2,15 katı**. Kadro büyütmek de kurtarmıyor: Karar F, %60,63'ü sıfır kuyrukla servis etmek için 26 personel ve 28 masa gerektiğini, tavanların ise 12 ve 14 olduğunu gösteriyor. Bölüm 4.2'de bu hesap yeniden yapıldı ve doğrulandı.

### 1.4 Çelişkinin gerçek boyutu

**Fiziksel tavan doğru. Hasar tahmini yanlış.**

Karar F, dilim payı tavanını (%28,16) doğru hesaplıyor. Ama tavanın aşılması hâlinde ne olacağını **açık döngü** bir kuyrukla hesaplıyor: kimse çıkıp gitmiyor varsayımıyla birikimi sınırsız büyütüyor. Bu yüzden fast food'un mevcut profili için "günün %72,8'i çıkıp gidiyor" diyor.

Gerçekte sabrı tükenen müşteri **kuyruğa girmiyor** ve arkasındakinin beklemesini kısaltıyor. Kuyruk kapalı döngüdür ve marjinal arketipin sabrında doyar. Aynı profil, aynı kadro, kapalı döngü modelle:

| Profil | Karar F'nin modeli | Kapalı döngü model | Fark |
|---|---|---|---|
| Fast food, içerik (%37,49) | günlük kayıp %72,8 | günlük kayıp **%13,36** | 5,4 kat |
| Türk, içerik (%60,63) | günlük kayıp %60,6 | günlük kayıp **%30,47** | 2,0 kat |
| Fast food, Karar F (%30) | günlük kayıp %6,1 | günlük kayıp **%1,80** | 3,4 kat |

Yani çelişki, Karar F'nin sandığından küçük ama sıfır değil. **%30,47 hâlâ kabul edilemez.** Türk mutfağının mevcut profili, açık döngüyle de kapalı döngüyle de fizibil değil. Ama fast food'un profili kapalı döngüyle **fizibile yakın**, ve bu, çözüm alanını genişletiyor.

Modelin kendisi bölüm 3'te.

---

## 2. Fiziksel büyüklük dilim payı değil, yoğunluk

### 2.1 Tanım

Karar F'nin "dilim payı %28,2'yi aşamaz" cümlesi, dilimlerin eşit uzunlukta olduğu varsayımını içinde saklıyor. Dilim uzunluğu değişkense doğru büyüklük **yoğunluk**:

```
yoğunluk_i = müşteri_payı_i ÷ süre_payı_i
```

Bir dilimin havuz doluluğu:

```
dilim_doluluğu_i = yoğunluk_i × gün_doluluğu
```

Sıfır kuyruk şartı `dilim_doluluğu ≤ 1`, yani:

```
azami yoğunluk = 1 ÷ 0,8879 = 1,1262
```

Eşit dilimde (süre payı 0,25) bu, 0,25 × 1,1262 = **%28,16** dilim payı verir; Karar F'nin sayısı. Ama süre payı 0,48 ise aynı yoğunluk **%54,06** dilim payına izin verir. Tavan değişmedi; tavanın hangi büyüklüğe uygulandığı değişti.

### 2.2 Dilim kapasitesi, müşteri cinsinden

Eşit dilimde (120.000 ms) her havuzun kaç müşteri kaldırdığı:

| Havuz | Hesap | Müşteri/dilim |
|---|---|---|
| Mutfak | 4 × 120.000 ÷ 17.143 | 28,00 |
| Salon | 8,4 × 120.000 ÷ 36.908 | **27,31** |
| Masa | 14 × 120.000 ÷ 125.525 = 13,38 grup × 2,1847 ÷ 0,9741 | 30,01 |

Bağlayıcı havuz salon: dilim başına 27,31 müşteri. Gün boyu servis edilebilen toplam 97 ÷ 0,8879 = **109,25 müşteri**; bir dilim bunun süre payı kadarını alıyor.

Türk mutfağının öğle dilimi eşit dilimde 0,6063 × 97 = **58,82 müşteri** getiriyor. 58,82 ÷ 27,31 = 2,15. Yani öğle dilimi, kapasitesinin iki katından fazla talep taşıyor.

### 2.3 Yoğunluk tavanı hangi kaldıraçlarla oynuyor

| Kaldıraç | Yeni tavan | Eşit dilim karşılığı | Not |
|---|---|---|---|
| Hafta sonu, kadro 11 (mevcut) | 1,1262 | %28,16 | Karar F'nin sayısı |
| Hafta sonu, kadro 12 (tavan) | 1,1546 | %28,87 | Fazladan bir salon personeli |
| Hafta içi, 77 müşteri | 1,4188 | %35,47 | Kadro hafta sonuna kurulu, hafta içi boş |

**On ikinci personel yoğunluk tavanını yalnızca 0,7 puan büyütüyor.** [14-staff-system.md](14-staff-system.md)'nin "tavan her kademede gerekenden bir fazla" boşluğu bir hata payı; zirve satın alma kaldıracı değil. Bu, seçenek B'nin neden çalışmadığının kısa hâli.

---

## 3. Kuyruk modeli düzeltmesi

### 3.1 Karar F'nin modeli açık döngü

`timing.py` içindeki `slot_queue`, dilim sonundaki birikimi şöyle hesaplıyor:

```
bekleme = max(0, gereken_iş − var_olan_iş) ÷ sunucu_sayısı
```

Bu, dilim boyunca gelen **herkesin kuyruğa girdiğini** varsayıyor. Türk profili için sonuç 192.598 ms ortalama boş bekleme: en sabırlı arketibin (emekli, 40.000 ms) sabrının 4,8 katı. O boş bekleme fiziksel olarak oluşamaz, çünkü kuyruk o uzunluğa gelmeden önce gelenler kapıdan dönmeye başlar ve birikim büyümeyi durdurur.

### 3.2 Kapalı döngü

`timing.py` içine `flow_day()` eklendi. Her havuz için birikim `B_p` (ms-iş cinsinden) tutuluyor; yeni gelenin boş beklemesi `W = Σ B_p / hız_p`. Sabri `W`'yi kaldırmayan müşteri kuyruğa **girmiyor**:

```
dB_p/dt = kalan(W) × geliş_hızı_p − hız_p          (B_p ≥ 0)
```

`kalan(W)`, bekleme puanı `W + pişirme × 0,25` değerini sabrı aşmayan arketiplerin kafa payı. Karar E'nin sabır tanımı ve `cookPatienceWeightBp = 2500` aynen kullanılıyor; değiştirilmedi.

Denge, geliş hızının kapasiteye düştüğü noktada kuruluyor. Birikim **marjinal arketibin sabrında doyuyor**: fast food'da 11.004 ms, Türk'te 17.003 ms. Bunlar tesadüf değil, havuzdaki sabır basamaklarının kendisi.

### 3.3 İki modelin doğrudan karşılaştırması

Fast food içerik profili, zirve dilim (%37,49), zirve gün:

| Büyüklük | Açık döngü | Kapalı döngü |
|---|---|---|
| Dilim doluluğu, salon | %133,2 | %133,2 (aynı, doluluk taleple ölçülüyor) |
| Dilim sonu boş bekleme | 101.137 ms | **11.004 ms** |
| Dilim ortalaması boş bekleme | 50.568 ms | **10.167 ms** |
| Dilimde kaybedilen | %100 | **%20,5** |
| Günlük kayıp | %72,8 | **%13,36** |

Doluluk yüzdeleri iki modelde de aynı; ayrılan tek şey birikimin nereye kadar büyüdüğü. **Karar F'nin fizik tespiti (dilim kapasitesi aşılıyor) doğru; sonuç tespiti (gün çöküyor) modelin artefaktı.**

Kapalı döngü modelin kabulleri, açık yazılıyor:

| Kabul | Ne demek | Yön |
|---|---|---|
| Çıkan müşteri kapasite tüketmez | Kuyruktan ayrılır, servise girmemiştir | Kaybı **azaltır** |
| Geliş dilim içinde düzgün dağılmış | Gerçekte dilim içinde de tepe olur | Kaybı **azaltır** |
| Üç havuzun beklemesi toplanır | Karar F ile aynı, sıralı bekleme | Nötr |
| Dilimler arası taşma serbest | Kuyruk sonraki dilime akar | Kaybı **azaltır** |
| Gün sonunda kalan kuyruk kayıp sayılır | Kapanışta kapıdakiler | Kaybı artırır |

Üç kabul kaybı aşağı çekiyor. Yani aşağıdaki sayılar **iyimser taraftadır** ve oynanabilirlik testinde yukarı çıkabilir. Bu, Karar F'nin "%6,1 kötümser tarafta" notunun tersi ve kasıtlı: iki model arasında bir bant var, gerçek onun içinde.

---

## 4. Dört seçenek

### 4.1 Seçenek A: profilleri yeniden normalleştir

Her dilimi ~%30'a kırp. Karar F'nin önerisi.

| Ölçüt | Sonuç |
|---|---|
| Fast food günlük kayıp | %1,80 |
| Türk günlük kayıp | %2,21 (profil %15/%30/%30/%25) |
| Değişen dosya | `content/archetypes/fastfood.json`, `content/archetypes/turk.json` — 24 arketibin `arrivalWeightsBp` alanı |
| Değişen kod | Yok |

**Bedeli.** Türk mutfağının öğle zirvesi %60,63'ten %30'a iniyor; yoğunluk 2,425'ten 1,200'e. Fast food'un zirvesi 1,500'den 1,200'e. İki mutfağın yoğunluk profili **birbirinin aynısı** oluyor: 1,20 / 1,20 iki dilimde fast food, 1,20 / 1,20 iki dilimde Türk. [07-cuisine-system.md](07-cuisine-system.md)'nin yedi farklılaşma değişkeninden biri olan "ritim" ve [10-cuisine-identity.md](10-cuisine-identity.md)'nin "kalabalığın ritmi" satırı içerikten tamamen siliniyor. Ayrıca Türk mutfağının akşamı %12,5'ten %25'e çıkıyor, yani "akşam sönük" kimliği de gidiyor.

Bu seçenek çalışıyor, ucuz, ve **dört kimlik direğinden birini imha ediyor.**

### 4.2 Seçenek B: tavanları büyüt

Mevcut payları sıfır kuyrukla servis edecek kadro ve masa:

| Mutfak | Dilim payı | Yoğunluk | Salon iş-günü | Salon personeli | Aşçı | Toplam kadro | Masa |
|---|---|---|---|---|---|---|---|
| Fast food | %37,49 | 1,500 | 11,18 | 10 | 6 | **16** | **17** |
| Türk | %60,63 | 2,425 | 18,09 | 17 | 9 | **26** | **28** |
| Tavan | — | — | — | — | — | **12** | **14** |

Kapalı döngü modelde daha az kadro yeter, çünkü %8-9 kayıp kabul ediliyor. O hâlde bile:

| Mutfak | Hedef zirve kaybı | Aşçı | Salon | Toplam kadro | Masa |
|---|---|---|---|---|---|
| Fast food | %3,4 | 5 | 9 | 14 | 17 |
| Türk | %9,9 | 8 | 14 | 22 | 26 |

**Bedeli.** Üç ayrı yerde kırılıyor.

| Kırılan | Neden |
|---|---|
| Ekonomi | Kiralar ve genişleme bedelleri `tools/balance/solve.py` içinde kademe kapasitesinden hedef marja çözülmüş. Masa 14 → 17 kira eğrisini, kadro 12 → 16 maaş eğrisini kaydırıyor; sekiz haftalık büyüme tablosu baştan çözülüyor |
| Sahne | 17 masa 2,5D sahnede kırk derecelik sabit kamerada okunmuyor; [19-technical-setup.md](19-technical-setup.md) kare başına 100 altı çizim çağrısı ve 100 bin altı üçgen istiyor, masa ve müşteri sayısı doğrudan bu bütçeye giriyor |
| Dokunuş | [16](16-screens-and-tutorial.md) günlük 40-60 dokunuş tavanı koyuyor; 16 personelin sabah istasyon ataması tek başına o bütçeyi yiyor |

Ve Türk için gereken 26 kadro / 28 masa, tavanın iki katından fazla. **Seçenek B fast food için pahalı, Türk için imkânsız.**

### 4.3 Seçenek C: kaybı bilinçli hâle getir

Zirvede müşteri kaybetmek kural olsun; beceri kaybı önlemek değil azaltmak olsun.

**Bedeli yok, ama tek başına yeterli değil.** İki sebep:

**Birincisi, ayar aralığı yok.** Eşit dilimde kayıp payı, aşırı yükün doğrusal bir fonksiyonu ve dilim 120.000 ms olduğu için katsayı çok büyük. Zirve dilim payı ile kayıp:

| Dilim payı | Yoğunluk | Salon doluluğu | Fast food dilim kaybı | Türk dilim kaybı |
|---|---|---|---|---|
| %28,0 | 1,120 | %99,4 | %0,0 | %0,0 |
| %30,0 | 1,200 | %106,5 | %3,0 | %2,2 |
| %32,5 | 1,300 | %115,4 | %9,4 | %9,7 |
| %35,0 | 1,400 | %124,3 | %15,5 | %15,8 |
| %37,5 | 1,500 | %133,2 | %20,5 | %20,5 |
| %45,0 | 1,800 | %159,8 | %33,6 | %32,7 |
| %60,6 | 2,425 | %215,3 | %50,4 | %49,6 |

%33'ün üstünde kayıp **aritmetiğe** bağlanıyor: kabaca `1 − kapasite ÷ talep`. Sabır dağılımının, yani içeriğin, etkisi kalmıyor — iki mutfağın sütunları %35'ten sonra birbirine yapışıyor. Yani "hangi kayıp dramatik" sorusu ancak %28-33 bandında sorulabiliyor; onun üstünde soru "kaç müşteri kapıdan dönüyor" aritmetiğine indirgeniyor.

**İkincisi, itibar zaten sarmala girmiyor.** Bölüm 5.6'da hesaplandı: bir günün itibar değişiminin işaretinin dönmesi için kaybın **%52,8'i (fast food) veya %55,2'yi (Türk)** aşması gerekiyor. Önerilen profilde kayıp %3-5. Yani "ilan edilmiş yoğunluk sırasında itibar hasarı yumuşatılsın mı" sorusunun cevabı **hayır**: yumuşatılacak bir ölüm sarmalı yok. Yumuşatma eklemek, kaybı görünmez yaparak [02-design-proposal.md](02-design-proposal.md) ilke 1'i ("her kararın görünür bir sonucu olacak") zayıflatır.

Seçenek C bir çözüm değil, bir **kabul**. Kararın parçası, kendisi değil.

### 4.4 Seçenek D: günü yeniden biçimlendir

Dilim süreleri eşit olmasın. Öğle dilimi tick olarak uzun olsun.

**Bedeli üç kalem.**

| Kalem | Ne |
|---|---|
| Aritmetik | [27](27-time-model.md) §5.3'ün "azami dilim payı %28,2" cümlesi genelleşiyor: azami **yoğunluk** 1,1262 |
| Şema | `arrivalWeightsBp` yanına mutfak başına `slotDurationsBp` geliyor; dört sayı, toplamı 10.000, her biri tick'e tam bölünüyor |
| Çekirdek | `Simulation.BuildArrivalPlan` içindeki `slotTicks = ServiceTicks / SlotCount` satırı diziye dönüyor; `TimingConfig`, `ContentSet`, `ContentDto`, `ContentSetLoader` bir alan taşıyor |

**Kurgu bedeli yok.** Bir esnaf lokantasının öğle servisi 11.30-15.00 arasıdır; sekiz saatlik bir servis gününün gerçekten yaklaşık yarısı. Dilimleri eşit varsaymak, kurgusal olarak zaten yanlıştı.

**Ama D tek başına da yetmiyor.** Öğle dilimi %45 uzunlukta olsa bile %60,63 payın yoğunluğu 1,347 çıkar, tavan 1,1262. Süre uzatmak yoğunluğu **düşürür, sıfırlamaz**.

### 4.5 Karşılaştırma

| | A: normalleştir | B: tavanı büyüt | C: kaybı kabul et | D: günü biçimlendir |
|---|---|---|---|---|
| Türk öğle payı | %30 | %60,63 | %60,63 | %60,63 |
| Türk öğle yoğunluğu | 1,200 | 1,000 | 2,425 | 1,263 |
| Günlük kayıp | %2,21 | %0 | %30,47 | %5,08 |
| Gereken kadro | 11 | 26 | 11 | 11 |
| Gereken masa | 14 | 28 | 14 | 14 |
| Ekonomi yeniden çözülüyor mu | Hayır | **Evet** | Hayır | Hayır |
| İçerik değişiyor mu | **24 arketip** | Hayır | Hayır | Mutfak başına 4 sayı |
| Kimlik direği | **Yok oluyor** | Korunuyor | Korunuyor | Biçim değiştiriyor |
| Tek başına yeterli mi | Evet | Hayır (tavan) | Hayır (ayar yok) | Hayır (yoğunluk hâlâ 1,347) |

---

## 5. Karar G: öneri

### 5.1 Ne

**Dilim süreleri mutfağa özel içerik olur. Arketiplerin `arrivalWeightsBp` değerleri olduğu gibi kalır. Kalan aşırı yük bilinçli kayıp olarak kabul edilir ve yumuşatılmaz.**

Yani D birincil mekanizma, C kabul edilen artık, A yalnızca D'nin emeyemediği kadar — ve aşağıdaki sayılarda A'ya hiç ihtiyaç kalmıyor.

| Mutfak | Dilim payları (değişmiyor) | Dilim süreleri (yeni) | Tick |
|---|---|---|---|
| Fast food | %11,05 / %37,49 / %16,20 / %35,26 | %20 / %30 / %20 / %30 | 960 / 1.440 / 960 / 1.440 |
| Türk | %9,37 / %60,63 / %17,53 / %12,47 | %12 / %48 / %25 / %15 | 576 / 2.304 / 1.200 / 720 |

Her iki dizi de 4.800 tick'e tam toplanıyor; hiçbir dilim kesirli tick istemiyor.

### 5.2 Fast food, zirve gün (97 müşteri, 4 aşçı, 7 salon, 14 masa)

| Dilim | Pay | Süre | Yoğunluk | Mutfak | Salon | Masa | Ort. boş bekleme | Azami | Kayıp |
|---|---|---|---|---|---|---|---|---|---|
| 1 Açılış | %11,05 | %20 | 0,552 | %47,9 | %49,1 | %44,6 | 0 ms | 0 ms | %0,0 |
| 2 Öğle | %37,49 | %30 | **1,250** | %108,2 | **%111,0** | %101,0 | 4.567 ms | 5.006 ms | **%7,2** |
| 3 Öğleden sonra | %16,20 | %20 | 0,810 | %70,2 | %71,9 | %65,4 | 353 ms | 5.004 ms | %0,0 |
| 4 Akşam | %35,26 | %30 | **1,175** | %101,8 | **%104,4** | %95,0 | 3.560 ms | 5.001 ms | **%1,3** |

Gün sonunda 5.000 ms kuyruk kalıyor, yani kapanışta kapıda birkaç kişi var. **Günlük kayıp %3,15**, 97 müşteride 3,06 kişi.

Dilim 2 aritmetiği açık: kapasite 0,30 × 109,25 = 32,78 müşteri, gelen 0,3749 × 97 = 36,37 müşteri, aşırı yük 3,59. Bunun 2,62'si kayboluyor, kalanı dilim 3'ün boşluğuna taşıyor (kapasite 21,85, talep 15,71, boşluk 6,14).

### 5.3 Türk mutfağı, zirve gün (aynı kadro)

| Dilim | Pay | Süre | Yoğunluk | Mutfak | Salon | Masa | Ort. boş bekleme | Azami | Kayıp |
|---|---|---|---|---|---|---|---|---|---|
| 1 Açılış | %9,37 | %12 | 0,781 | %67,6 | %69,3 | %62,8 | 0 ms | 0 ms | %0,0 |
| 2 Öğle | %60,63 | %48 | **1,263** | %109,4 | **%112,2** | %101,6 | 6.537 ms | 7.010 ms | **%8,4** |
| 3 Öğleden sonra | %17,53 | %25 | 0,701 | %60,7 | %62,3 | %56,4 | 451 ms | 6.996 ms | %0,1 |
| 4 Akşam | %12,47 | %15 | 0,831 | %72,0 | %73,8 | %66,9 | 0 ms | 0 ms | %0,0 |

Gün sonunda kuyruk yok. **Günlük kayıp %5,08**, 97 müşteride 4,93 kişi.

Dilim 2 aritmetiği: kapasite 0,48 × 109,25 = 52,44 müşteri, gelen 0,6063 × 97 = 58,82, aşırı yük 6,38; 4,93'ü kayıp, kalanı dilim 3'e taşıyor.

**%60,63 duruyor.** [12](12-economy.md) §5.6'nın cümlesi hâlâ doğru: lokantada müşterilerin yüzde altmışı tek dilimde geliyor. Değişen şey, o dilimin günün dörtte biri değil, **neredeyse yarısı** olması.

### 5.4 Bütün kademelerde

`model.py` PLAN satırlarının her biri, kendi kadrosu ve masasıyla:

| Hafta | Masa | İtibar | Hafta içi | Hafta sonu | Aşçı | Salon | FF içi | FF sonu | TR içi | TR sonu |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | 4 | 35 | 14 | 17 | 1 | 0 | %0,00 | %5,89 | %0,00 | %7,68 |
| 2 | 4 | 45 | 15 | 19 | 1 | 1 | %0,00 | %0,00 | %0,00 | %0,00 |
| 3 | 7 | 52 | 29 | 36 | 2 | 2 | %0,00 | %0,00 | %0,00 | %0,25 |
| 4 | 7 | 60 | 31 | 39 | 2 | 2 | %0,00 | %2,39 | %0,00 | %4,58 |
| 5 | 10 | 68 | 47 | 59 | 3 | 4 | %0,00 | %0,55 | %0,00 | %1,78 |
| 6 | 10 | 75 | 50 | 63 | 3 | 4 | %0,00 | %3,46 | %0,00 | %5,52 |
| 7 | 14 | 82 | 74 | 92 | 4 | 6 | %0,00 | **%7,24** | %0,00 | **%8,75** |
| 8 | 14 | 88 | 77 | 97 | 4 | 7 | %0,00 | %3,15 | %0,00 | %5,08 |

Üç şey okunuyor.

**Hafta içi her kademede sıfır.** Kadro hafta sonu zirvesine kurulu, hafta içi %20 daha az müşteri geliyor; kuyruk hiç oluşmuyor. [14](14-staff-system.md)'ün "kadro zirveye kurulur, ücreti yedi gün ödenir" kararının doğrudan görünür karşılığı bu.

**En sert hafta yedinci hafta, en sert an son genişlemenin ertesi.** 92 müşteri, 4 aşçı ve 6 salon personeliyle karşılanıyor; `ceil()` yuvarlaması burada en sıkı. [12](12-economy.md) §6 zaten yedinci haftayı "bilinçli bir kumar" diye tanımlıyor. Kayıp eğrisi anlatıyı doğruluyor.

**Birinci haftanın hafta sonunda bir müşteri kaybediliyor.** %5,89 × 17 = 1,00 kişi (Türk'te %7,68 × 17 = 1,31). Bu bir kaza değil, öğreticinin eksik parçası: [review/05-player-experience.md](review/05-player-experience.md) "ilk işe alımı oyuncunun gözle gördüğü ilk bekleme kaybının ertesi sabahına koyun" diyordu. Kapasite modeli o kaybı zaten altıncı veya yedinci günde üretiyor. İlk garsonun işe alınma sebebi böylece senaryodan değil aritmetikten geliyor.

### 5.5 Haftalık ciro etkisi

| Mutfak | Hafta içi | Hafta sonu | Haftalık kayıp müşteri | Toplam | Ciro etkisi |
|---|---|---|---|---|---|
| Fast food | %0,00 | %3,15 | 6,1 | 579 | **%1,06** |
| Türk | %0,00 | %5,08 | 9,9 | 579 | **%1,70** |

Hesap: `(5 × 77 × hafta_içi + 2 × 97 × hafta_sonu) ÷ (5 × 77 + 2 × 97)`.

`model.py` cirosu bir **üst sınır** olarak kalıyor ve zirve haftasında fast food'da %1,06, Türk'te %1,70 aşağı okunmalı. Karar F aynı düzeltmeyi %2,05 olarak yazmıştı; yeni sayı ondan küçük.

Sekizinci haftanın net marjına etkisi. Kaybedilen müşteri sipariş vermediği için malzemesini de tüketmiyor; maaş ve kira sabit:

| | Ciro | Malzeme (%32) | Maaş + kira | Net | Marj |
|---|---|---|---|---|---|
| `model.py` | 43.425 | −13.896 | −20.857 | 8.672 | **%20,0** |
| Fast food, −%1,06 | 42.965 | −13.749 | −20.857 | 8.359 | **%19,5** |
| Türk, −%1,70 | 42.687 | −13.660 | −20.857 | 8.170 | **%19,1** |

**Marj hedeflerinin ondalık payının içinde kalıyor; `model.py`'ye geri yazılması gerekmiyor.**

### 5.6 İtibar: ölüm sarmalı var mı

Bir günün itibar değişimi ([12](12-economy.md) §5.5): `Σ (memnuniyet − 60) × itibar_ağırlığı / 100`. Çıkıp giden müşterinin memnuniyeti, sabrı tam tükendiği an `100 − (sabır ÷ sabır) × 60 = 40`.

Zirve gün, önerilen profil:

| Mutfak | Gün | İtibar değişimi | Kuyruk hiç olmasa | Zirvenin bedeli | Çıkan |
|---|---|---|---|---|---|
| Fast food | Hafta sonu | +14,93 | +28,57 | **13,64 puan (%48)** | 3,06 kişi |
| Fast food | Hafta içi | +22,68 | +22,68 | 0 | 0 |
| Türk | Hafta sonu | +17,27 | +35,29 | **18,03 puan (%51)** | 4,93 kişi |
| Türk | Hafta içi | +28,02 | +28,02 | 0 | 0 |

Zirve, günün itibar kazancının yaklaşık **yarısını** götürüyor. Ama işaretini döndürmüyor. İşaret dönme eşiği:

```
bir memnun müşteri  = (ort. memnuniyet − 60) × ağırlık / 100
bir çıkan müşteri   = (40 − 60) × ağırlık / 100

fast food : +0,2946 ve −0,2628  →  eşik %52,8
türk      : +0,3639 ve −0,2956  →  eşik %55,2
```

**Bir günün itibarının eksiye dönmesi için müşterilerin yarıdan fazlasının kaybedilmesi gerekiyor.** Önerilen profilde kayıp %3-5, seçenek C'nin en kötü hâlinde (Türk %60,63 eşit dilimde) %30,47. Hiçbiri eşiği geçmiyor.

Bu oran, itibar formülünün ölçeğinden bağımsız: pay ve payda aynı ağırlıkla çarpılıyor. Formül ileride hacme göre normalleştirilse (bölüm 8, açık madde 3) eşik aynı kalır.

**Sonuç: "ilan edilmiş yoğunluk sırasında itibar hasarı yumuşatılsın mı" sorusunun cevabı hayır.** Yumuşatma gerektiren bir sarmal yok; eklemek kaybın görünürlüğünü öldürür.

### 5.7 Zirvede kim çıkıp gidiyor

Zirve dilim ortalama bekleme puanı fast food'da 9.567, Türk'te 11.542; azami puan 10.006 ve 12.010 (boş bekleme + pişirme × 0,25).

| Mutfak | Arketip | Sabır | Zirve dilimindeki payı | Ortalamada | Dilim sonunda |
|---|---|---|---|---|---|
| Fast food | kurye | 8.000 | %3,60 | Kalıyor, paket | Kalıyor |
| Fast food | şikayetçi_müşteri | 9.000 | %1,06 | **Çıkıyor** | Çıkıyor |
| Fast food | aceleci_öğrenci | 10.000 | %12,27 | Kalıyor | **Çıkıyor** |
| Fast food | çocuklu_ebeveyn | 13.000 | %4,03 | Kalıyor | Kalıyor |
| Türk | kurye | 8.000 | %2,22 | Kalıyor, paket | Kalıyor |
| Türk | uzun_yol_şoförü | 11.000 | %1,39 | **Çıkıyor** | Çıkıyor |
| Türk | öğle_molası_çalışanı | 12.000 | %16,47 | Kalıyor | **Çıkıyor** |
| Türk | çocuklu_ebeveyn | 13.000 | %2,48 | Kalıyor | Kalıyor |

Kurye her iki mutfakta da hayatta: masa ve mutfak kuyruğuna girmiyor, yalnızca salon kuyruğuna giriyor. Karar E'nin tespiti korunuyor.

**Kaybedilen yüz her mutfakta farklı.** Fast food'da şikayetçi müşteri ve aceleci öğrenci; lokantada uzun yol şoförü ve öğle molası çalışanı. Öğle molası çalışanı öğle diliminin %16,47'si ve tanım gereği bekleyemeyen kişi. Öğle yoğunluğunda kaybedilenin öğle molasındaki memur olması, kurgu ile aritmetiğin kendiliğinden buluştuğu yer.

### 5.8 Servis edilemez hâle gelen arketip var mı

**Hayır.**

| Kontrol | Sonuç |
|---|---|
| Sıfır yükte en sabırsız arketip (kurye 8.000) | Bekleme puanı 2.500, kalıyor — Karar E, değişmedi |
| `patienceMs` değerleri | Değişmiyor |
| Görev süreleri (`T_SEAT`, `T_ORDER`, `T_SERVE`, `T_BUS`, `T_WASH`, `T_PAY`) | Değişmiyor |
| `prepMs` formülü | Değişmiyor |
| Hafta içi, her kademe | Hiçbir arketip kaybedilmiyor (kuyruk yok) |
| Hafta sonu, zirve dilim | Fast food'da 2, Türk'te 2 arketip risk altında; ikisi de dilimin sonunda, ortalamada değil |

Bir arketip **kalıcı olarak** servis edilemez hâle gelmiyor. Risk altındaki dördü de dilimin ilk yarısında servis ediliyor, ikinci yarısında kaybediliyor. `timing.py --check` C ve E grubu testleri (sıfır yük, kurye, eski tanımın kırıklığı) değiştirilmedi ve geçmeye devam ediyor.

### 5.9 Diğer iki mutfak

[12](12-economy.md) §5.6 dört mutfak tanımlıyor. İtalyan ve Japon arketip havuzları henüz yazılmadı; süreleri aynı kuralla, yalnızca yoğunluk tavanından türetiliyor:

| Mutfak | Paylar | Süreler | Tick | Yoğunluklar | Durum |
|---|---|---|---|---|---|
| İtalyan | %5 / %20 / %10 / %65 | %10 / %20 / %15 / %55 | 480 / 960 / 720 / 2.640 | 0,50 / 1,00 / 0,67 / **1,18** | Geçici |
| Japon | %15 / %50 / %15 / %20 | %15 / %40 / %20 / %25 | 720 / 1.920 / 960 / 1.200 | 1,00 / **1,25** / 0,75 / 0,80 | Geçici |

İkisi de tavanın altında. İtalyan'ın kayıp sayısı hesaplanamıyor çünkü `T_EAT_PARTY` mutfağa göre değişecek ([27](27-time-model.md) karar bekleyen madde 1) ve İtalyan'da uzayacak; uzun oturma masa havuzunu bağlayıcı hâle getirebilir. **Bu iki satır bağlayıcı değil**, dilim süresi mekanizmasının dört mutfağın hepsini taşıyabildiğinin ispatı.

---

## 6. Kimlik: ne küçülüyor, yerine ne geliyor

Bu bölümü küçültmeden yazmak gerekiyor.

### 6.1 Küçülen: zirve şiddeti bir kimlik ekseni olmaktan çıkıyor

Yoğunluk tavanı bütün mutfaklar için aynı, çünkü kapasite modeli mutfaktan bağımsız. Önerilen profillerde:

| Mutfak | Zirve yoğunluğu | Zirve salon doluluğu | Zirve dilim kaybı |
|---|---|---|---|
| Fast food | 1,250 | %111,0 | %7,2 |
| Türk | 1,263 | %112,2 | %8,4 |

**Aradaki fark 1,2 puan.** Oyuncu, öğle zirvesinin lokantada fast food'dakinden daha sert olduğunu **hissedemez**, çünkü değil. [12](12-economy.md) §5.6'nın "öğle zirvesini gerçek bir kriz anına çeviriyor" cümlesi, "fast food'dakinden daha büyük bir krize" anlamında okunuyorsa **yanlış** ve bu kararla da doğru olmuyor. Hiçbir seçenekle doğru olamıyordu: seçenek B onu 26 personel ve 28 masayla satın alıyordu, o da tavanların iki katı.

Yüzde altmışın taşıdığı sanılan "daha zor" iddiası, içerikte hiç doğrulanmamış bir iddiaydı. Ölçüm onu ilk kez sınadı ve tutmadı.

### 6.2 Yerine gelen: baskının süresi ve sürekliliği

Zirve şiddeti değil ama zirvenin **şekli** mutfağa göre değişiyor, ve bu ölçülebilir:

| Ölçüt | Fast food | Türk mutfağı |
|---|---|---|
| Yoğunluğu 1,0 üstünde geçen süre | 2.880 tick (%60 gün) | 2.304 tick (%48 gün) |
| Kaç parçada | **İki** (1.440 + 1.440) | **Bir** (2.304) |
| En uzun kesintisiz baskı, 1x hız | 144.000 ms = 2,4 dk | 230.400 ms = 3,8 dk |
| Baskılar arası nefes | 960 tick, yoğunluk 0,810 | Yok |
| Zirve sonrası | İkinci zirve | 1.920 tick kesintisiz boşluk (0,701 ve 0,831) |
| En boş dilim | Açılış: %11,05 pay, yoğunluk 0,552 | Öğleden sonra: %17,53 pay, yoğunluk 0,701 |
| Akşam salonu | Dolu (%104,4) | Boş (%73,8) |

Üç fark oyuncunun ekranda gördüğü şeye çeviriliyor.

**Birincisi, sprint ile maraton farkı.** Fast food günü iki kısa sprint; her birinden sonra toparlanma penceresi var, hata düzeltilebilir. Lokanta günü tek bir dört dakikalık kesintisiz stres; içinde toparlanma yok, hata birikiyor. Aynı yoğunluk, tamamen farklı yönetim problemi.

**İkincisi, akşamın boşalması.** Lokantanın dördüncü dilimi %15 uzunlukta ve müşterilerin %12,47'sini alıyor; salon %73,8'de. Fast food'un dördüncü dilimi %30 uzunlukta, müşterilerin %35,26'sını alıyor, salon %104,4'te. Ekranda salonun görünür şekilde boşalması, [10](10-cuisine-identity.md)'un "kalabalığın ritmi" satırının tam karşılığı ve uzak kamerada okunan altı şeyden biri.

**Üçüncüsü, kapıdan dönen yüz.** Bölüm 5.7: fast food'da şikayetçi müşteri ve aceleci öğrenci, lokantada uzun yol şoförü ve öğle molası çalışanı. Farklı siluet, farklı kıyafet, farklı ses.

### 6.3 Dürüst özet

[07](07-cuisine-system.md)'nin yedi farklılaşma değişkeninden "ritim" satırı **daralıyor ama silinmiyor**. Daralan kısım şiddet, korunan kısım süre ve süreklilik. Seçenek A bu satırı tamamen siliyordu; Karar G iki üçünü kurtarıyor.

Buna karşılık, kimlik yükünün bir kısmı başka direklere kayıyor. [07](07-cuisine-system.md)'nin kendi listesinde ritmin altında **imza mekaniği** var ve "en önemli satır" olarak işaretli: fast food'da kombo ve akış, Türk'te veresiye ve düzenli müşteri. Ritmin taşıyamadığı ayrımı taşıyacak yer orası, ve [review/01-design-and-balance.md](review/01-design-and-balance.md) veresiyenin şu hâliyle "vergi" olduğunu, tutar, vade, tahsilat takvimi ve ödememe olasılığının yazılmadığını zaten söylemişti. **Ritim küçüldüğü için veresiyenin yazılması artık isteğe bağlı değil.** Bu bir bağımlılık ve bu kararın bedeli.

---

## 7. Ne değişecek, nerede

### 7.1 Soru soru cevaplar

| Dosya | Değişiyor mu | Ne ve neden |
|---|---|---|
| `content/archetypes/fastfood.json` | **Hayır** | `arrivalWeightsBp` olduğu gibi kalıyor. Ölçülen %11,05/%37,49/%16,20/%35,26 profili, %20/%30/%20/%30 süreleriyle fizibil (bölüm 5.2). Karar F'nin bu dosyaya çıkardığı iş **iptal** |
| `content/archetypes/turk.json` | **Hayır** | Aynı. %9,37/%60,63/%17,53/%12,47 profili, %12/%48/%25/%15 süreleriyle fizibil (bölüm 5.3). Karar F'nin bu dosyaya çıkardığı iş **iptal** |
| `content/archetypes/shared.json` | **Hayır** | Kurye dahil hiçbir paylaşılan arketip değişmiyor |
| `content/economy.json` | **Hayır** | Dilim süresi mutfak başına bir değer; `economy.json`'un mutfak boyutu yok ve dosya `export.py` tarafından üretiliyor. Bölüm 7.2'ye bakın |
| Kadro tavanları (3/5/8/12) | **Hayır** | Tavan büyütmek yalnızca seçenek B'nin gereği. Karar G tavanları olduğu gibi kullanıyor; bölüm 2.3'te on ikinci personelin yoğunluk tavanına katkısı 0,7 puan, ihmal edilebilir |
| Masa tavanları (4/7/10/14) | **Hayır** | Aynı sebep |
| `tools/balance/model.py` | **Hayır** | `model.py`'de dilim kavramı yok; talep, kadro, kira ve marj gün toplamı üzerinden hesaplanıyor ve gün toplamı değişmedi. Kayıp bir **okuma düzeltmesi**: hafta 8 cirosu fast food'da %1,06, Türk'te %1,70 aşağı okunur. Karar F aynı düzeltmeyi %2,05 olarak yazmıştı, yeni sayı ondan küçük ve marj hedeflerinin ondalık payının içinde |
| `tools/balance/solve.py` | **Hayır** | Kira ve genişleme bedelleri kademe kapasitesinden çözülüyor; kapasite değişmedi |

**Dört sorunun dördünün de cevabı hayır.** Bu, kararın en güçlü tarafı ve seçenek A ile B'den ayrıldığı yer: A yirmi dört arketibi, B ekonominin tamamını yeniden yazdırıyordu.

### 7.2 Değişecek olanlar

| Dosya | Ne | Neden |
|---|---|---|
| **Yeni** `content/cuisines/fastfood.json` ve `turk.json` | `slotDurationsBp: [2000,3000,2000,3000]` ve `[1200,4800,2500,1500]` | Mutfak başına gün biçimi. `economy.json` mutfaktan bağımsız, `archetypes/*.json` bir arketip listesi; ikisi de bu alanın doğru yeri değil. Aynı dosya [27](27-time-model.md) karar bekleyen madde 1'deki mutfak başına `T_EAT_PARTY`'yi de taşıyacak |
| `tools/content/gen_archetypes.py` | Süre dizisini üret ve doğrula: dört değer, toplam 10.000, her biri × 4.800 ÷ 10.000 tamsayı | Üreticinin mevcut `arrivalWeightsBp` doğrulaması (satır 288-291) ile aynı kalıp |
| `tools/balance/export.py` | `content/cuisines/*.json` üretimi | Elle yazılmış sayı bırakmama kuralı |
| `unity/.../Core/Sim/TimingConfig.cs` | `SlotCount` yanına `SlotTicks` dizisi; toplamı `ServiceTicks` olmalı | Eşit dilim varsayımı kalkıyor |
| `unity/.../Core/Sim/Simulation.cs` | `BuildArrivalPlan` içinde `slotTicks = _timing.ServiceTicks / _timing.SlotCount` → dizi araması ve dilim başlangıcı önceden toplanmış ofset | Tek satırlık kırılma noktası |
| `unity/.../Core/Content/ContentSet.cs`, `Content/ContentDto.cs`, `Content/ContentSetLoader.cs` | `slotDurationsBp` alanı ve doğrulaması | `arrivalWeightsBp` ile aynı kalıp |
| `tools/balance/timing.py` | **Yapıldı.** Eklenen: `PATIENCE_HEADS_TURK`, `GROUP_SIZE_SEATED_TURK`, `SLOT_SHARE_CONTENT_FF/TR`, `SLOT_DUR_EQUAL/FF/TR`, `slot_ticks()`, `pool_needs()`, `retain_share()`, `flow_day()`, `max_density()`, `peak_report()`, `--zirve` | Mevcut hiçbir sabit, fonksiyon veya karar değiştirilmedi; 47 test aynen geçiyor |
| [12-economy.md](12-economy.md) §5.6 | Tablo iki sütunlu olacak: pay **ve** süre. Yazılı değerler ölçülen değerlerle değiştirilecek (bölüm 1.2). "Yüzde altmışı tek dilimde" cümlesi kalacak, "dilim günün %48'i" eklenecek | Yazılı tablo içerikle tutmuyordu |
| [27-time-model.md](27-time-model.md) §5.3, §5.4, §5.5, Karar F | Karar F **değiştirildi**. "Azami dilim payı %28,2" → "azami yoğunluk 1,1262; eşit dilimde %28,2". §5.4 seçenek 3 ve §5.5'teki normalleştirme önerileri iptal | Bu dosya |
| [27-time-model.md](27-time-model.md) §8 | `content/archetypes/*.json` satırlarındaki `arrivalWeightsBp` işleri silinecek | Aynı |
| [27-time-model.md](27-time-model.md) §7 | `Azami dilim payı %28,2` satırı `Azami yoğunluk 1,1262` olacak | Aynı |
| [27-time-model.md](27-time-model.md) §9 | "46 test" → 47 | Sayı zaten eskiydi; `--check` 47 kontrol yazıyor |
| [23-core-contract.md](23-core-contract.md) | Dilimlerin eşit olmadığı çekirdek sözleşmesine yazılacak | Determinizm sözleşmesi |
| [13-data-schemas.md](13-data-schemas.md) | `cuisines/*.json` şeması; §260'taki doğrulama listesine dilim süresi toplamı eklenecek | Şema listesi eksik kalmasın |
| [16-screens-and-tutorial.md](16-screens-and-tutorial.md) | Gün ilerleme çubuğu dilimleri **eşit çizmeyecek** | [review/05](review/05-player-experience.md) sorun 4 zaten dilim işaretli bir ilerleme çubuğu istiyordu; çubuk artık mutfak kimliğini de gösteriyor |
| [10-cuisine-identity.md](10-cuisine-identity.md) | "Günlük ritim" satırı süre ve süreklilik cinsinden yeniden yazılacak (bölüm 6.2 tablosu) | Ritim direği daraldı, tanımı güncellenmeli |

**Hiçbiri bu işte yapılmadı.** Bu dosya yalnızca kararı ve aritmetiğini taşıyor.

---

## 8. Açık kalanlar

1. **`T_EAT_PARTY` mutfak başına.** [27](27-time-model.md) karar bekleyen madde 1. Türk mutfağında oturma süresi fast food'un 38.000 ms'sinden uzunsa masa devri uzar, masa havuzu doluluğu %101,6'dan yukarı çıkar ve **bağlayıcı havuz salondan masaya geçebilir.** Geçerse öğle dilimi süresi %48'den yukarı çekilmeli. Bu dosyanın Türk sayıları `T_EAT_PARTY = 38.000` varsayıyor ve bu varsayım muhtemelen yanlış.
2. **İtalyan ve Japon.** Bölüm 5.9'daki süreler geçici; arketip havuzları yazılınca kayıp hesabı yapılmalı. İtalyan'ın %65 akşamı, uzun oturma ile birlikte masa havuzunu bağlayabilir.
3. **İtibar formülü hacme göre normalleşmiyor.** [12](12-economy.md) §5.5 örneği 40 müşteride +8 veriyor; aynı formül 97 müşteride +28,57 veriyor. Zirvesiz bir hafta sonu günü itibarı tek başına 0'dan 100'e taşır. Bölüm 5.6'nın oranları (%48, %51, eşik %52,8) bu ölçekten bağımsız ve formül düzeltilse de aynı kalır, ama **formülün kendisi bu dosyanın kapsamı dışında ve kırık.** Ayrı bir işte çözülmeli.
4. **`docs/14` iki farklı kadro tavanı tablosu taşıyor.** "Kaç kişi çalıştırabilirsin" 2/4/6/8 diyor, "Kadro tavanı" 3/5/8/12 diyor. `model.py` ve bu dosya ikincisini kullanıyor. Birincisi silinmeli.
5. **Kapalı döngü modelin oynanabilirlik doğrulaması.** Bölüm 3.3'teki beş kabulden üçü kaybı aşağı çekiyor. Gerçek sayı, açık döngü (%13,36 fast food) ile kapalı döngü (%3,15) arasındaki bantta. C# çekirdeği zaten müşteri müşteri simüle ediyor; `Simulation` üstünde bir toplu koşu, iki modelin hangisine yakın çıktığını söyleyecek. **Bu ölçüm yapılana kadar bölüm 5'in kayıp sayıları alt sınır sayılmalı.**
6. **Dilim içi geliş dağılımı.** Model dilim içinde düzgün dağılım varsayıyor; `BuildArrivalPlan` da öyle yapıyor (`_rngArrival.NextInt(slotTicks)`). Gerçek öğle yoğunluğunun dilim içinde de tepesi vardır. Dilim sayısını dörtten sekize çıkarmak bu ayrıntıyı içeriğe taşırdı; şimdilik açılmadı, çünkü [16](16-screens-and-tutorial.md)'nın gün ilerleme çubuğunda sekiz dilim okunmaz.
7. **`serviceMs` alanı.** `content/economy.json` hâlâ `serviceMs: 120000` taşıyor. [27](27-time-model.md) §2.4 bunun `tableTurnMs: 125525` olması gerektiğini söylemişti; hâlâ yapılmadı ve bu dosya o değişikliğe dokunmuyor.
