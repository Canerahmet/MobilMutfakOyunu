# Mekân Yerleşimi: Açık Salon, Odalar, Sabit Görünüm

**Son güncelleme:** 10 Eylül 2026
**Kütük maddeleri:** A9 ekran listesi ve akış, B7 girdi eylem haritası, C1 sanat hattı
**Durum:** Araştırma ve öneri. Karar bekliyor.

**Dayanak:** [16-screens-and-tutorial.md](16-screens-and-tutorial.md) sonundaki dokunma hedefi ölçümü; `unity/Assets/Lokanta/Editor/RestaurantScene.cs` ve `unity/Assets/Lokanta/Editor/RoomLayout.cs` render'ları; yirmi altı oyunun web araştırması.

**Uyarı:** Bu dosyadaki oyun bulgularının bir kısmı, Fandom, GameFAQs, TouchArcade ve Gamezebo gibi doğrudan çekilemeyen sayfaların arama dizinindeki alıntılarına dayanıyor. Doğrulanamayan her madde açıkça **doğrulanmadı** diye işaretlendi. Tahmin yok.

---

## 1. Soru

Geliştirici şunu öneriyor: restoran tek bir açık salon olmasın, **odalardan** oluşsun. Salon, mutfak, bulaşık, depo ayrı odalar olsun; genişleme aynı zemine masa eklemek yerine **yeni bir oda eklesin**.

Soru şu yüzden acil: 10 Eylül 2026'da masanın telefon ekranında kaç dp olduğu ölçüldü ve **masanın dokunma hedefi olamayacağı** çıktı. Oda önerisi, bu ölçümün açtığı boşluğa gelen bir cevap. Cevabın işe yarayıp yaramadığı ölçülmeden bilinemez.

Bu dosya üç şeyi yapıyor: ölçümü tamamlıyor, karşılaştırılabilir oyunların ne yaptığını topluyor, ve üç aday yerleşimi bedelleriyle karşılaştırıp bir öneri veriyor.

---

## 2. Ölçülen kısıt

### 2.1 Standart

| Kaynak | Asgari dokunma hedefi | Ek kural |
|---|---|---|
| Google, Android erişilebilirlik | **48 × 48 dp**, fiziksel karşılığı yaklaşık **9 mm** | Hedefler arasında en az **8 dp** boşluk |
| Apple | 44 × 44 pt | — |
| Google, genel tavsiye | Dokunmatik nesneler için 7-10 mm | — |

`RoomLayout.cs`'in kullandığı dönüşüm: 960 piksellik render → 2.400 piksellik telefon (×2,5), yoğunluk 2,75. Yani **2.400 × 1.080 piksellik bir telefon, yatayda 873 × 393 dp**. 48 dp, ekran genişliğinin **%5,5'i** demek.

### 2.2 Açık salonda ölçülen (docs/16, Unity konsolundan)

| Kademe | Masa | Salon | Masa, 2.400 piksellik telefonda | dp | Asgarinin kaçta kaçı |
|---|---|---|---|---|---|
| 1 | 4 | 7,8 × 7,4 m | 55 piksel | ~20 dp | %42 |
| 2 | 7 | 9,6 × 7,4 m | 53 piksel | ~19 dp | %40 |
| 3 | 10 | 11,5 × 7,4 m | 50 piksel | ~18 dp | %38 |
| 4 | 14 | 13,3 × 9,1 m | 42 piksel | ~15 dp | **%31** |

### 2.3 Neyin bozulduğu, tam olarak

`RestaurantScene.cs` okunduğunda sayının nereden geldiği görünüyor ve teşhis "on dört masa çok fazla"dan farklı çıkıyor:

| Etken | Değer | Etkisi |
|---|---|---|
| Masa tablası | 0,86 × 0,86 m | Ölçülen şey bu; sandalyelerle birlikte set 1,86 m |
| Izgara adımı | 1,85 m yatay, 1,70 m derinlemesine | 14 masa = 6 sütun × 3 satır = 11,1 × 5,1 m |
| Mutfak şeridi ve pay | 2,4 m + 1,6 m | Salon derinliğinin **%44'ü** masa değil |
| Duvar yüksekliği | 3,0 m | Kamera sığdırmasında dikey ekseni domine ediyor |
| Kamera | 32° dikey görüş açısı, 30° eğim, −16° dönüş | Eğim derinliği kısaltıyor |
| Sığdırma formülü | `mesafe = max(distV, distH) + maxZ + %4 pay`, sonra ×1,04 | `+ maxZ` terimi kamerayı gereğinden **%40 daha geri** çekiyor |

Kritik nokta: **kamera genişlikten değil yükseklikten sığdırıyor.** 20:9'luk kare 2,22:1 iken, 30° eğimle yansıtılan kutu dikeyde daha çok yer kaplıyor. Sonuç, oda_14 render'ında gözle görülüyor: restoran şeridi karenin yaklaşık **%45'ini** dolduruyor, gerisi boş arka plan.

Bu, sayının bir kısmının **doğa kanunu değil kamera kurgusu** olduğu anlamına geliyor. Kareyi kırparak, duvarı sığdırmanın dışında bırakarak ve eğimi düşürerek kabaca 1,5 kat kazanılabilir: 15 dp → yaklaşık 22-25 dp. **Yine de 48'in altında.** Kamera ayarı sorunu hafifletiyor, çözmüyor.

### 2.4 "Hedefi masadan büyük yapalım" neden çalışmıyor

docs/16 bunu iddia etmişti, ölçüm doğruluyor. Kademe 4'te sütun adımı 1,85 m, masa tablası 0,86 m. Masa 15 dp ise sütun adımı 15 × (1,85 / 0,86) = **32 dp**. Satır adımı ekranda daha da dar. 48 dp'lik görünmez hedefler komşularıyla çakışır ve Google'ın istediği 8 dp boşluk hiç kalmaz.

**Masa setinin tamamı** (masa + iki sandalye, 1,86 m) kademe 4'te yaklaşık 32 dp. O da yetmiyor.

---

## 3. İkinci ölçüm: oda tabanlı yerleşim kuruldu

`unity/Assets/Lokanta/Editor/RoomLayout.cs` (10 Eylül 2026, 10:57) oda önerisini kurup iki kamera kipinde render etti. Yerleşim: en solda mutfak (4,2 × 4,6 m) ve bulaşık (2,6 × 4,6 m), onların sağında salon odaları. Her salon odası 4,6 × 4,3 m ve içinde 4 ya da 3 masa var.

| Kademe | Masa | Salon odaları | Toplam şerit eni |
|---|---|---|---|
| 1 | 4 | 4 | 11,4 m |
| 2 | 7 | 4 + 3 | 16,0 m |
| 3 | 10 | 4 + 3 + 3 | 20,6 m |
| 4 | 14 | 4 + 3 + 3 + 4 | 25,2 m |

**Not:** Kademe artışlarımız +3, +3, +4. Bir salon odası 3-4 masa alıyor. Yani kademe merdiveni ile oda merdiveni **birebir örtüşüyor.** Bu tesadüf değil, kademe sayıları zaten oda büyüklüğü kadar.

### 3.1 Sert kanıt: oda çerçevesi kademeden bağımsız

Render'ların MD5'i alındığında `oda_10_tekoda` ile `oda_14_tekoda` **bit bit aynı dosya** çıkıyor. Aynı kare, aynı masa boyutu, on masalık ve on dört masalık restoranda.

```
84afa4423b8f8c177fc3f08b8776f6cb  oda_10_tekoda_105735.png
84afa4423b8f8c177fc3f08b8776f6cb  oda_14_tekoda_105735.png
```

Bu, oda tabanlı yerleşimin tek gerçek yapısal iddiası: **kamera bir odayı çerçeveliyorsa, dokunma hedefi restoranın büyüklüğüne bağlı olmaktan çıkar.** Açık salonda masa 20 dp'den 15 dp'ye düşüyor; oda çerçevesinde düşmüyor, çünkü çerçevelenen şey büyümüyor.

### 3.2 Ölçüm tablosu

| Kademe | Masa | **Açık salon** masa | Oda düzeni, **tüm restoran**: masa | Oda düzeni, **tüm restoran**: oda | Oda düzeni, **tek oda**: masa | Oda düzeni, **tek oda**: masa + sandalye |
|---|---|---|---|---|---|---|
| 1 | 4 | 20 dp | ~44 dp | ~235 dp | ~45 dp | ~100 dp |
| 2 | 7 | 19 dp | ~31 dp | ~167 dp | ~45 dp | ~100 dp |
| 3 | 10 | 18 dp | ~24 dp | ~130 dp | ~45 dp | ~100 dp |
| 4 | 14 | **15 dp** | ~20 dp | **~106 dp** | **~45 dp** | **~100 dp** |

**Sayıların kaynağı ve güvenilirliği.** Açık salon sütunu Unity konsolundan ölçülmüş sayıdır (docs/16). Oda sütunları benim `RoomLayout.cs` geometrisinden hesapladığım ve render'lardan okuduğum değerlerdir; `RoomLayout.cs`'in kendi `Debug.Log` çıktısına erişemedim. İki yöntem yaklaşık %15 içinde örtüşüyor. **Betiği çalıştırıp konsol satırını dosyaya yazmak gerekiyor**; karar bu sayıların işaretine dayanıyor, ondalığına değil.

"Tüm restoran" sütunundaki düşüş şerit eniyle ters orantılı: 11,4 → 25,2 m arasında masa 44 dp'den 20 dp'ye iniyor.

### 3.3 Üç sonuç

1. **Hiçbir yerleşim, tüm restoranı tek karede gösterirken masayı 48 dp yapamıyor.** Ne açık salon (15 dp), ne oda düzeni (20 dp). Kademe 4'te dünyayı gösteren hiçbir kamerada masa birincil dokunma hedefi olamaz.
2. **Oda düzeni, açık salonda hiç var olmayan bir ara hedef üretiyor:** oda. Kademe 4'te oda ~106 dp, yani asgarinin iki katı. Açık salonda "bütün salon" ile "tek masa" arasında dokunulabilecek hiçbir nesne yok.
3. **Tek oda çerçevesinde masa 48 dp'ye yaklaşıyor (~45 dp) ve masa seti rahatça geçiyor (~100 dp).** Ama bu, kameranın restoranın dörtte birini gösterdiği anlamına geliyor.

Kademe 4'te tek oda kipinde 20:9 kare yaklaşık 15 m dünya genişliği gösteriyor: mutfak, bulaşık, birinci salon ve ikinci salonun bir kısmı. Yani "tek oda" kipi pratikte **iki oda artı servis şeridi** demek, dört odanın biri değil.

---

## 4. Karşılaştırılabilir oyunlar ne yapıyor

Beş soru her oyun için: (1) mekân tek açık zemin mi, odalar mı, sabit tek görünüm mü; (2) genişleme nasıl; (3) kamera; (4) ne dokunuluyor; (5) mobilde hedef nasıl korunuyor.

### 4.1 Mobil zaman baskılı servis oyunları

| Oyun | Mekân | Genişleme | Kamera | Dokunulan | Mobil çözümü |
|---|---|---|---|---|---|
| **Diner Dash** (2004, PC) | Seviye başına tek sabit ekran | Seviye başına 2-6 masa; yeni mekân = yeni sahne. Bölüm 1-1 iki masa, 1-2 dört masa; Hometown Hero kılavuzu "altı masa çok" diyor | Sabit, kaydırma ve yakınlaştırma yok | **Masa.** Tek müşteri için masaya ~5 kez basılıyor: otur, sipariş al, getir, hesap, topla | Masa sayısı seviyeyle sınırlı; kalıcı büyüyen bir salon yok |
| **Diner DASH Adventures** (Glu, 2019) | Ayrı, elle tasarlanmış sabit seviye sahneleri | Oyun: yeni seviye ve mekân. Mekân: **yıldızla açılan dekor yuvaları**, her yuvada birkaç tasarım seçeneği. Oyuncu asla yerleşim çizmiyor | Seviye içi kamera kontrolü **doğrulanmadı** | Müşteri, masa, sabit yemek istasyonu şeridi | **Masalar renk kodlu** — küçük boyutta masa siluetten değil renkten tanınıyor. Dekor canlı sahneden çıkarılıp yuva seçicisine alınmış |
| **Cook, Serve, Delicious! 2/3** | **Mekân yok.** Sipariş fişi kuyruğu; bekletme istasyonları ekranın üstünde, kuyruk solda | Büyüyen şey **arayüz yuvaları**: menü, hazırlık ve bekletme istasyonu sayısı. CSD2'nin ayrı "Designer" ekranı duvar, zemin, masa koyduruyor ama **tamamen kozmetik** | Yok, statik arayüz | Sipariş fişi, hazırlık düğmeleri, istasyonlar. Asla mobilya, asla müşteri | Dünyada hedef yok; bütün hedefler sabit konumlu büyük arayüz öğeleri. CSD2/3'ün mobil sürümü yok; sadece CSD1 çıktı ve TouchArcade portun girdi yoğunluğunu **düşürdüğünü** yazdı |
| **Good Pizza, Great Pizza** | **Tek sabit tezgâh görünümü.** Salon simülasyonu, oturma mantığı, kamera yok | Malzeme, ekipman, dekor, bahçe. Dikkat çekici: **"Wide Counter" yükseltmesi çalışma alanını fiziksel olarak büyütüyor** | Sabit | Hamur, malzeme, fırın, kesici, müşteri fişi | Hedefler yakın plan ve sabit konumlu, işletme büyüklüğünden bağımsız. Malzeme rafı kalabalıklaşınca (etiket yok, oyuncu görünüşten hatırlıyor) çözüm **ikonu küçültmek değil tezgâhı büyütmek** oldu |
| **Cooking Fever** | Restoran başına **tek sabit görünüm, dört müşteri yuvası** | **48 ayrı restoran**, hepsi aynı dört yuvalı şablon. Tek zemin hiç büyümüyor | Sabit, kaydırma yok | Malzeme kabı, cihaz, tabak, tezgâh, müşteri. **Hiçbir mekânsal şeye dokunulmuyor: masa yok, sandalye yok, zemin yok** | Eş zamanlı müşteri **4'te sabit**; zorluk sayı değil hız ve karmaşıklık. İç mekân yükseltmesi (akvaryum, disko topu, tabure) istatistik veriyor, sahnede görünüyor, **asla dokunulmuyor** |
| **Cooking Diary** (Mytona) | Sahne başına restoran, **yürüyen şef avatarı** | 9 semt × ~6 restoran, ayrı sahneler | **Doğrulanmadı** | İstasyon ve nesne; avatar oraya kendi yürüyor | Uzaktaki hedefe değil **istasyona** dokunuluyor; hassas nişan gerekmiyor |
| **Cooking Madness** | Seviye başına sabit tek görünüm mutfak | 80+ tematik restoran, 3.000+ seviye, dünya haritası | Sabit (**mağaza tanımı dışında doğrulanmadı**) | Pişirme istasyonları; siparişler müşterinin başının üstünde | Yeni sahne, büyüyen sahne değil |
| **Animal Restaurant** | **Ayrı alanlar:** Main, Kitchen, Courtyard, Concert, Garden, Buffet, Fishing Pond, Takeout, Terrace | "Yeni odalar açılıyor"; her alan ayrı gelir üretiyor | Alan başına sabit; arayüz ekran kenarlarında | **Müşteri siparişi, para, çöp** ve kenar arayüzü. Masa değil | Ticari olarak en başarılı oda tabanlı mobil restoran oyunu; hedefler müşteri ve arayüz, mobilya değil |

### 4.2 Dükkân simülasyonları

| Oyun | Mekân | Genişleme | Kamera | Dokunulan | Mobil |
|---|---|---|---|---|---|
| **Supermarket Simulator** (2024) | **Tek açık satış zemini** + bitişikte tek depo binası | 23 "Growth" bölümü, her biri **4×4 m**, hepsi aynı zemini uzatıyor, toplam 1.176.900 $. Depo ayrı 15 bölüm, aynı 4×4 m mantığı | Birinci şahıs | Fiziksel nesneler yakın planda + sipariş, fiyat, işe alım için **bilgisayar terminali** | Resmî mobil sürüm yok; Google Play'deki listelemeler kopya. Depo kasıtlı olarak zahmetli: kademe 1'de sadece **sokak kapısı** açılıyor, mağaza içi kapı kademe 3'te geliyor |
| **TCG Card Shop Simulator** (2024) | Tek dikdörtgen zemin + bitişik **Lot B** | Dükkân A: 30 karo genişlemesi. Lot B: seviye 15'te 5.000 $'a bir kez, sonra 14 genişleme daha. Her genişleme kabaca 1×1 alan | Birinci şahıs | Raf, kart masası, kasa, kutu; genişleme satın alma **RENO BIGG telefon uygulamasında** | Mobil sürüm yok. Oyuncular "duvar ve bölme daha yaratıcı olsun" istiyor — **oyun oda vermiyor ve oyuncular bunu fark ediyor** |
| **Recettear** (2010) | **Tek sabit dükkân odası**, tepeden. Recette tezgâhtan ayrılamıyor | Aynı oda üç kez büyüyor: ML12 → 4 tezgâh, ML20 → 6, ML26 → 10. Yeni oda **hiç** eklenmiyor; sadece kapı yer değiştiriyor | Sabit tepeden, tek ekran | Tezgâh yuvası, sonra pazarlık arayüzü | Port yok |
| **Moonlighter** (2018) | **Tek oda**, tepeden | 4 dükkân yükseltmesi; oda büyüyüp mobilyayı yeniden diziyor, azamide 14 masa. Asla çok odalı olmuyor | Tepeden, dükkân tek ekrana sığıyor | **Masa** → envanter → **masa başına fiyat**; müşteri fiyata emoji ile tepki veriyor | **Bu setteki en güçlü dokunmatik kanıt.** iOS 2020 / Android 2021. Düz port değil: "arayüz dokunmatiğe geçiş için baştan tasarlandı", sanal çubuk yerine "gideceğin yere dokun" — "oyuncular başparmaklarıyla ekranın köşelerini kapatmasın diye". Ve: **"dükkân stoklama ve satış menüleri dokunmatikte çok daha doğal geliyor"** |
| **Tavern Master** (2021) | Serbest çizilen tek bina; hazır kesilmiş oda yok | Duvar **silerek** büyüyor ("o duvarın üstüne delik çiz"). Mutfak, misafir odası, depo **araştırma** ile açılıyor, sonra oyuncu inşa ediyor | Tepeden/izometrik, kaydır ve yakınlaştır | Katalog sekmesi → mobilya → sürükle yerleştir | Google Play'deki "Tavern Master" farklı bir oyun görünüyor; **PC oyununun mobil sürümü doğrulanamadı** |
| **Cat Cafe Manager** (2022) | **Tek açık zemin, iç duvar yok.** Geliştiricinin kendi cevabı: oda yapmak için "boş karolardan bir sıra" bırakıyorsun | Karo satın alma (maliyet kafe büyüdükçe artıyor); duvarlar otomatik yerleşiyor. Tapınak araştırması sandalye ve kadro tavanını yükseltiyor | Açılı, yakınlaştırma var, **döndürme yok** | İnşa modu (zemin, duvar kâğıdı, pencere) ve dekor modu (mobilya, cihaz, kapı) | Mobil yok |
| **Chef Life** (2023) | **Ayrı odalar:** mutfak, salon, ofis. Üçüncü şahıs (birinci şahıs değil) | Salon duvarındaki plana dokunup **hazır bir yerleşimle değiştiriyorsun.** Sadece gündüz hazırlıkta, seviye ve "İç Mimari" kilidi gerekiyor. **Dekorlar sıfırlanıyor** | Üçüncü şahıs takip | İstasyon ve malzeme, ofisteki katalog kitabı, plan panosu | Mobil yok |
| **Discounty** (2025) | Tepeden tek zemin | **Tam iki kez**, her biri **bitişik yeni bir alan**: sağda çay/kahve dükkânı, sonra solda. Ayrı bir depo odası var | Tepeden | Raf, kasa, ürün; komşuluk mekaniği ("booster" bitişik raftaki ürünleri çekici yapıyor) | Mobil yok |
| **Travellers Rest** (2020) | Tek binada **üç kat** (bodrum, meyhane, kiralık odalar) | İtibar 7'de İnşaat Modu açılıyor: **tek tek zemin karosu** satın al → bölge ata (kırmızı yemek, mavi üretim) → kapı koy, kapalı alan **kiralık oda** olur. Karo hakkı ve azami oda sayısı itibarla büyüyor | Tepeden | İnşaat masası, karo, kapı | Mobil yok |
| **Dave the Diver** | Tek sabit restoran sahnesi | **Fiziksel oda eklenmiyor.** Cooksta rütbesi kadro ve menüyü büyütüyor; en sonunda **ikinci şube**, yani ayrı bir mekân | Sabit | Servis dokunuşları; dekorasyon ekranın altındaki menüden | Mobil sürüm 17 Eylül 2026, "telefon için tamamen optimize" |
| **Restaurant Renovation** (ZYMobile) | **Yönetim oyunu değil.** Eşleştirme bulmacası + dekorasyon; yürünen zemin yok | Bulmaca kazancıyla sahneler yenileniyor | Yok | Bulmaca taşları ve dekor seçenekleri | Adı yanıltıcı; bu referans listeden düşmeli |

### 4.3 Oda dilbilgisinin iki referansı

| Oyun | Mekân | Genişleme | Kamera | Dokunulan | Dokunmatik |
|---|---|---|---|---|---|
| **Two Point Hospital / Campus** | Sabit bina kabuğunun içine oyuncunun **çizdiği** odalar. Asgari 2×3 ile 4×5 arası; **kapı zorunlu bir gereç**; oda kalitesi (1-5 prestij) boyut + içindeki eşyalardan çıkıyor | Bitişik **parsel** satın alma, yatay. Kat yok. Her bina bir mikro hastane | Serbest 3B: kaydır, **döndür**, eğ (yaklaşık 45°'ye kadar), yakınlaştır. **Tek oda kipi yok**; onun yerine 12 renk katmanı ("Visualisation Modes") | Oda, tek tek personel (10 eyleme kadar, "Pick Up" ile personeli kaldırıp odaya bırakma), tek tek hasta, ve altı ayrı arayüz listesi | **Hiç dokunmatik sürüm yok, yedi yılda üç oyunda.** iOS/Android yok, Netflix yok; "JUMBO Edition" konsol paketi. Switch'te **dokunmatik hiç desteklenmiyor**, incelemeciler bunu tuhaf buldu |
| **PlateUp!** | Duvar, servis penceresi ve **kapılarla** çevrili gerçek odalar. 5 prosedürel plan tipi | **Koşu içinde büyümüyor.** Plan koşu başında sabit; deneyim seviyesi daha büyük plan açıyor (Extended 10, Huge 11). Günler arası büyüyen şey **yoğunluk**: aynı kabuğun içine yeni cihaz | Sabit tepeden, **oyuncu kontrolü yok**. Stok planlar tek kareye sığdığı için kontrol gerekmiyor | Cihaz, blueprint | Mobil yok. Büyük haritalarda kamera yetmiyor; topluluk modları (CameraPlus, Free Camera Control) tam bu yüzden var |

**Two Point'in kontrol dersi.** Konsol portu dokunmatik yerine **sanal imleç** kurdu: sol çubuk imleci, sağ çubuk kamerayı, omuz düğmeleri yakınlaştırmayı ve liste gezinmesini sürüyor; menü sol alt köşeye sabitlendi. İncelemeler "on beş dakikada oturuyor" dedi ama "belirli eşya veya kişiyi seçmek zorlaşabiliyor" diye ekledi. Two Point Museum'un Switch 2 portunda aynı sorunlar tekrarlandı: çubukla yerleştirmede **hedefi aşma**, **geri alma yok**, kip değişimi görünmüyor, menü derinliği, ve **metin çok küçük**. Tek işe yarayan hafifletme: **imleç ekranın ortasında sabit tutuluyor, dünya imlecin altında kayıyor.**

**Two Point'in yol bulma dersi — bizim için en sert olanı.** Koridor inşa edilen bir şey değil, **odaların tümleyeni**: "hastane parselinin oda olmayan her parçası koridordur." Geçerlilik kuralı tek cümle: **"her oda kapısıyla koridora bağlı olmalı ve aynı parseldeki her odanın kapısına açık bir yolu olmalı."** Bunun bedeli belgeli: takılan hastalar, "yol bulamıyor", "geçersiz gezinme" hata başlıkları; oyuncu çözümleri hep yerleşimi bozup düzeltmek. Geliştiricilerin kendi anlatımında en zor kısım "hastaların kuyrukları ve koridorlarda hareketi" ile "duvar kalınlığı ve hücre genişliği" olmuş.

**PlateUp'ın üretici sözleşmesi**, oda dilbilgisinin en temiz yazılı hâli: aynı odada olmayan her bitişik karo çiftine bir özellik ekleniyor, **her bitişik oda çiftine rastgele kapı** konuyor, ve sistem **ön kapıdan her odaya kapılardan geçen bir yol olmasını garanti ediyor.**

### 4.4 Telefonda oda tabanlı büyümenin çalışan örnekleri

| Oyun | Mekân | Genişleme | Kamera | Dokunulan | Ders |
|---|---|---|---|---|---|
| **Fallout Shelter** | Kesitten görünen, kat kat dizili odalar | Yeni oda kaz; aynı tipten oda yan yana gelince **otomatik birleşiyor**, en fazla üç birleşme, kapasite 2'den 6'ya | Pinch yakınlaştırma, kaydırma, ve **otomatik yakınlaştırma**: bir odaya ya da sakine dokununca kamera oraya gidip ortalıyor. "Küçük ekranda parmakla oynanan mobil sürüm için tasarlandı" | Oda, sakin, sol alttaki çekiç düğmesi | **Belgelenmiş başarısızlık.** Bir oyuncunun dokunmatik yazısı: "çok sayıda odayı aynı anda görebilmek için o kadar uzaklaşmak gerekiyor ki hiçbirine dokunmak kolay olmuyor" ve "radyo odasını seçmeye çalışmak, içindeki sakini seçmekle eşit olasılıkta". Otomatik kamera da sevilmiyor: "kameranın kontrolünü oyuncudan almak" şikayet konusu |
| **Tiny Tower** | Dikey kat yığını, kat başına tek işletme | Yeni kat inşa et | Dikey kaydırma; oyun içi görüntüde **aynı anda dört kat** görünüyor | Kat, asansör, bitizen | Bir kat ekran yüksekliğinin dörtte biri. Hedef büyüklüğü kat sayısıyla **değişmiyor**, çünkü kamera hiçbir zaman kuleyi tümüyle göstermiyor |
| **Hotel Empire Tycoon** | Ayrı oda ve alanlar; "bir odadan diğerine atlıyorsun" | Yeni oda ve alan, sonra tümüyle **yeni otel** | Kamera ayrıntısı **doğrulanmadı** | Alana dokun → performans ve kadro paneli açılıyor | Dünyadaki nesne bir **seçici**, manipülasyon aracı değil |

### 4.5 Yirmi altı oyunun ortak cevabı

Üç örüntü var ve **her başarılı mobil örnek en az birini kullanıyor**:

| Örüntü | Kim kullanıyor |
|---|---|
| **Sabit yakın plan istasyon düzeni, tavanlı eş zamanlılık.** Hedefler büyük ve konumu hiç değişmiyor; zorluk sayıdan değil hızdan geliyor | Cooking Fever (4 yuva), Good Pizza (tek tezgâh), CSD (ekranın üstündeki bekletme istasyonları) |
| **Büyüme = yeni sahne, büyüyen sahne değil.** Kameranın hiç uzaklaşması gerekmiyor | Cooking Fever 48 restoran, Cooking Madness 80+, Cooking Diary 9 semt, Diner Dash mekânları, Dave the Diver ikinci şube |
| **Yerleşim ve dekorasyon, zamanlı ekrandan sürgün edilmiş** ayrı bir ekranda, yuva seçicilerle | Diner DASH Adventures (yıldız ve anahtarla dekor yuvası), CSD2 Designer, Cooking Fever iç mekân menüsü, Good Pizza dükkân ve bahçe ekranı |

Ve iki olumsuz bulgu:

- **Masaya dokunduran tek oyun Diner Dash**, ve masa sayısını **sabit ekran başına 2-6** tutuyor, artışı **tek restoranın içinde değil seviyeler arasında** yapıyor, 2019 mobil sürümünde de masayı **renkle** tanınır kılıyor. **Tek karede on dört masalı bir salonun bu türde örneği yok.**
- **Hiçbir sevkiyat yapmış dükkân oyunu, önceden yazılmış ayrık odaları açarak büyümüyor.** En yakın ikisi (Supermarket Simulator, TCG Card Shop Simulator) tek zemini 4×4 m'lik karolarla uzatıyor ve **tek bir** arka oda ekliyor. Ayrı oda nerede varsa (Supermarket deposu, TCG Lot B, Travellers Rest katları) **arka bölgeyi gizlemek için** var ve kasıtlı olarak yürüme mesafesi konmuş — birinci şahısta içerik gibi okunan bu sürtünme, telefonda 2.5D'de **kamera gidip gelmesi** olarak okunur.

---

## 5. Kairosoft: telefonda oda tabanlı yönetimin tek ticari örneği

Görev tanımı Kairosoft'u "en yakın ticari emsal" diye işaretledi. Doğru işaretlemiş, ve cevabı beklenenden keskin.

### 5.1 Dört oyun, iki farklı yapı

| Oyun | Mekân | Genişleme | Kamera |
|---|---|---|---|
| **Cafeteria Nipponica** | **Oda yok.** Turuncu çerçeveli arsa içinde ızgaraya masa ve tesis konuyor. El kitabı: "Masa koymak için restoranın turuncu çerçeveli bir alanını seçin" | Kademeli: küçük → orta → büyük. Ayrıca taşınma ve aynı anda üç restoran. **Izgara ölçüleri doğrulanmadı** | Konsol port incelemesi "içine iyice yakınlaşıp uzaklaşabiliyorsun" diyor |
| **Hot Springs Story** | **Ayrık odalar, sabit ayak izli.** Büyük Banyo 2×3; sahne bonusu sadece sol 2×2'sine işliyor; girişinin belirli bir karede ve üstten veya sağdan açık yolu olması gerekiyor. Kombolar 2 karelik yarıçapta çalışıyor | **Tapu** satın alma, yönlü: Tapu I 20.000 → +1 üst, +2 sol. Tapu V 2.500.000 → +4 üst | Kaydır + yakınlaştır + köşe düğmesinden açılan yön tekerleği |
| **Mega Mall Story** | **Katlarda odalar.** Dükkânlar 1, 2, 3 veya 4 kare genişliğinde | Yatırım olarak satın alınıyor: "Orta Mall" = her iki yana 4 sütun; "Bodrum" = BF3'e kadar aşağı. **Merdiven ve yürüyen merdiven açık birer dolaşım tesisi** ve satışı belirgin etkiliyor | Parmakla sürükle veya köşe düğmesinden yön okları |
| **Dream House Days** | Odalar, mobilya tavanlı: küçük 16, orta 32, büyük 64 | Daire boyutu | — |

### 5.2 Kairosoft dokunma hedefini nasıl çözüyor

Cevap tek cümlede: **Kairosoft ızgarayı dokunma hedefi yapmıyor, seçim hedefi yapıyor.** Karar karede değil menüde veriliyor.

| Teknik | Kanıt |
|---|---|
| **Doğrudan manipülasyon yok.** Kareye dokunursun, menü açılır | Kairobotica: "Boş bir arsaya dokunarak inşa menüsünü açın" |
| **İkinci yol her zaman var:** köşedeki menü düğmesi | Sushi Spinnery: "menü düğmesine dokunup açılır listeden inşayı seçin". Kairobotica: menü düğmesi sağ altta, menü sol tarafta açılıyor |
| **Onay adımı var; göstergesi hayalet ızgara** | Sushi Spinnery genişlemesi: "genişlemenin ne kadar büyük olacağını gösteren ızgaralar görürsün, alanı seçtikten sonra **tekrar dokunup** satın almayı kesinleştirirsin" |
| **Önce bölge, sonra kare.** Oyun geçerli bölgeyi vurguluyor | Cafeteria Nipponica el kitabı: "turuncu çerçeveli alanı seçin" |
| **Sürükleme sadece süreklilik gerektiren nesnede** | Sushi Spinnery konveyörü: "dokun ve sürükle, mevcut banda bağlandığından emin ol" |
| **Yedekli gezinme girdisi:** parmakla sürükleme ve pinch **artı** köşe düğmesinden açılan yön tekerleği | Pocket Academy: "pinch yöntemiyle yakınlaştırıp uzaklaştırabilirsiniz... sol altta pembe aşağı ok düğmesine dokunursanız bir gezinme tekerleği belirir" |
| **Oyun mantığında kendi imleci var**, işaretçiden ayrı | Switch 2'de Dungeon Village'da "ekranda farklı işler yapan iki imleç" görünüyor |

**Şikayet gerçek ve Kairosoft'un kendisi düzeltmiş.** TouchArcade eski başlıklar için "arayüz bir PC oyunundan çekilmiş gibi" diyor; yeni başlıklar için "sonunda tuş telefonları ve PC yerine akıllı telefon için tasarlanmış hisseden bir arayüz, **yerleştirme daha az zahmetli, menü düğmeleri daha büyük**". 2023'te kalan şikayet **menü derinliği**: "bazı alt menülere ve komutlara erişim olması gerekenden dolambaçlı, özellikle personel yönetiminde".

### 5.3 Bizim için üç çıkarım

1. **Restoran temalı Kairosoft oyunu (Cafeteria Nipponica) oda kullanmıyor.** Oda kullananlar otel, alışveriş merkezi ve apartman. Yani Kairosoft'un kendisi, restoran için açık ızgarayı seçmiş.
2. **Onay adımı mobilin asıl cevabı.** Hayalet ızgara → tekrar dokun → kesinleşir. Bu, Two Point'in konsolda düştüğü tuzağı (hedefi aşma + geri alma yok) doğrudan kapatıyor.
3. **Yedekli girdi zorunlu:** parmakla kaydırma ve pinch her zaman, artı köşeden açılan bir yön kontrolü. İkisi de aynı işi yapıyor; bu gereksizlik bilinçli.

---

## 6. Üç aday yerleşim

| | **A. Açık salon** | **B. Odalar** | **C. Sabit tek görünüm** |
|---|---|---|---|
| Tanım | Tek zemin, kademede genişliyor. Bugünkü `RestaurantScene.cs` | Salon, mutfak, bulaşık, depo ayrı; kademe yeni oda ekliyor. Bugünkü `RoomLayout.cs` | Kamera hiç hareket etmiyor, salon dekor, karar arayüzde |
| Kademe 4'te masa, tüm restoran karesinde | 15 dp | ~20 dp | ~20 dp |
| Kademe 4'te masa, oda karesinde | Oda yok | **~45 dp** (sabit) | Uygulanmıyor |
| Ara dokunma hedefi | **Yok.** Salon ile masa arasında hiçbir şey | **Oda, ~106 dp** | Alt çubuk çipleri, **istenen kadar dp** |
| Kamera işi | Kaydır ve yakınlaştır: dokunuş bütçesinde 2 dokunuş (review/05) | Oda değiştirme: kademe 4'te tam tur 3 dokunuş | **Sıfır** |
| Görünür büyüme | Tek karede, doğrudan | Uzak kipte var, oda kipinde yok | Tek karede, doğrudan |
| Yol bulma gerekliliği | Yok; masalar tek zeminde | Kapı ve yol tutarlılığı gerekiyor (Two Point, PlateUp kuralı) | Yok |
| Sanat hacmi | Mutfak başına 1 kabuk × 4 kademe = 8 yerleşim (review/03 bütçesi) | Mutfak başına ~7 oda modülü + paylaşılan kapı/duvar kiti | Mutfak başına 1 kabuk |
| Emsal | **Yok.** Tek karede 14 masalı mobil örnek bulunamadı | Animal Restaurant, Fallout Shelter, Tiny Tower, Hot Springs Story | Cooking Fever, Good Pizza, CSD, Recettear |
| Ana riski | Masa hiçbir kademede dokunulamıyor | Gezinme dokunuşu ve at-a-glance kaybı | Salon dekora düşüyor |

---

## 7. Oda tabanlı yerleşimin bize maliyeti

### 7.1 Gezinme dokunuşu — en sert kısıt

| Kaynak | Sayı |
|---|---|
| docs/16 günlük dokunuş bütçesi | 40-60 |
| docs/27 türetimi | 480.000 ms servis günü ÷ 60 = dokunuş başına 8.000 ms |
| review/05 gün sayımı, **servis aşamasının tamamı** | 10 dokunuş, bunun **2'si kamera** |
| docs/27 gün dilimi sayısı | 4 |

Kademe 4'te dört salon odası var. Bir tam tur = 3 oda değişimi. Oyuncu her dilimde bir kez restoranı taramak isterse **12 dokunuş.** Bu, servis aşamasının bütün bütçesinin **%120'si** ve günün toplam bütçesinin **%20'si.** Karşılığında sıfır karar üretiliyor: gezinme kendi başına hiçbir şeye karar vermek değil.

Karşılaştırma: alt çubuktaki uyarı çipi (review/05'in "sabır kuyruğu çipleri" önerisi) **sıfır** gezinme dokunuşu istiyor, çünkü uyarı oyuncuya geliyor, oyuncu uyarıya gitmiyor.

873 dp'lik yatay ekrana 96 dp'lik sekiz çip sığıyor. docs/02'nin gün başına 3-5 patron müdahalesi bütçesi bunun altında; yani çip çubuğu tavanı hiç zorlamıyor.

### 7.2 Yol bulma — docs/14 ile doğrudan çatışma

docs/14 açıkça yazıyor: **"Karar: karmaşık yol bulma yok."** Personel istasyonuna sabitleniyor, garsonun yolu önceden hesaplanmış ve kısa, çakışma yok, personel birbirinin içinden geçebiliyor.

Oda dilbilgisi bu kararla üç yerden çatışıyor:

| Çatışma | Kanıt |
|---|---|
| Oda varsa **kapı vardır**, kapı varsa **bağlantı geçerliliği** vardır | Two Point: "her oda kapısıyla koridora bağlı olmalı ve her odanın kapısına açık yolu olmalı". PlateUp: üretici, ön kapıdan her odaya kapılardan geçen bir yol garanti ediyor |
| Bağlantı geçerliliği bozulunca **ajanlar takılır ve bu bir hata sınıfıdır** | Two Point'te belgeli: "yol bulamıyor", "geçersiz gezinme" başlıkları. docs/14 zaten Cat Cafe Manager ve Tavern Keeper'ın "ortak yarası" olarak bunu yazmıştı |
| Odalı düzende **yürüme süresi ekonomik bir değişkene dönüşür** | Hot Springs Story: oyun mesafeyi ve süreyi hesaplıyor; misafirin gün içinde kaç tesis tüketebileceğini yürüme belirliyor |

Ve sayısal çatışma, `RoomLayout.cs` geometrisinden: kademe 4'te mutfak `x ∈ [0; 4,2]`, en uzak salon `x ∈ [20,6; 25,2]`. Merkezler arası **20,8 m.** 1,2 m/s'lik bir yürüyüşle tek yön **17.300 ms.** docs/27 garsona müşteri başına **9.000 ms** "servis" veriyor. Yani şerit yerleşiminde gerçek bir yürüyüş, servis bütçesinin yaklaşık **dört katı** eder.

Üç çıkış var: yürüyüş simüle edilmez (sadece görsel), odalar mutfağın etrafında kompakt kümelenir, ya da her salon odasının kendi servis noktası olur. **Birincisi zaten doğru olan.**

### 7.3 Çekirdekte mekân diye bir şey yok

`src/` altında arama yapıldığında çekirdeğin mekân modeli olmadığı görülüyor: kademe `TierConfig(Tables, Rent, Upgrade, StaffCap)`, yani **masa sayısı bir skaler.** Koordinat yok, oda yok, koltuk yok. docs/23 de kamerayı ve ekran geçişini "komut olmayan" ilan ediyor: görünüm durumu, simülasyona girmez, **kaydedilmez.**

İki sonucu var:

1. Oda tabanlı yerleşim bugün **saf sunum kararı.** Simülasyon tarafında hiçbir şey değişmiyor, hiçbir şey eklemek gerekmiyor. Bu iyi haber.
2. Odaların simülasyonda bir anlamı olsun istenirse (oda başına kapasite, oda başına personel, müşteri yönlendirme) **yeni durum, yeni kayıt alanı ve yeni yol bulma** gerekir. O anda docs/14'ün kararı bozulur.
3. Kamera durumu kaydedilmediği için, **gün ortasında çıkıp dönen oyuncu varsayılan odada uyanır.** docs/16'nın "her an çıkış, kaldığı saniyeden devam" kuralı kamerada tutmuyor.

### 7.4 Sanat hacmi — sanılandan küçük bir mesele

review/03'ün çıkış bütçesi: "Mimari kabuk: 2, her biri 4 genişleme kademesiyle = **8 yerleşim**", mutfak başına 2-3 hafta, toplam 1-1,5 kişi-ay.

Odalı düzende sayım değişiyor:

| Kalem | Açık salon | Odalar |
|---|---|---|
| Mutfak başına elle kurulan yerleşim | 4 (kademe başına bir) | 0; kademe modüllerin birleşimi |
| Mutfak başına oda modülü | 1 kabuk | Mutfak 1, bulaşık 1, depo 1, salon 3-4 çeşit = **6-7** |
| Paylaşılan | — | Kapı/duvar/geçiş kiti, 1 kez |
| İki mutfak için toplam | 8 yerleşim | 12-14 modül + 1 kit |

Modül sayısı artıyor ama her modül bir yerleşimden küçük, ve bu tam olarak docs/24'ün zaten benimsediği mantık ("32 yemek, 14 mesh"). **Sanat hacmi odalı düzenin ana maliyeti değil; kabaca başabaş.**

İki gerçek sanat maliyeti var:

- **Dört aynı salon odası kopyala-yapıştır gibi okunur.** En az 3 çeşit gerekiyor, yoksa kademe 4 ucuz görünür. Bu, "yerleşim başına yeni tasarım" tasarrufunu geri alır.
- **Depo, hiçbir dokümanda karşılığı olmayan yeni bir oda.** docs/14'te dört rol var, depocu yok; docs/12'de stok var, fiziksel depo yok. Two Point'in kuralı burada uyarı: bir odanın **zorunlu gereçleri ve bir işi** olmalı. İşi olmayan depo odası saf maliyet.

Ayrıca render'lardaki hâliyle önerilen şey aslında **oda değil**: `RoomLayout.cs` üç duvar koyuyor, ön taraf kameraya açık, tavan yok, kapı yok. Bunlar mühürlü oda değil, **arka duvarı paylaşan koylar.** Bu iyi bir şey — koy, odanın sanat maliyetini ödemeden odanın çerçeveleme faydasını veriyor. Ama o zaman "oda" kelimesi tartışmayı yanıltıyor.

### 7.5 Bir bakışta bütün işletme

research/01 §3, oyuncuların en sevdiği mekanikleri sıralarken 2. ve 3. sıraya şunu koyuyor: **yerleşim tasarımı ve mekân genişletme**, ve **görünür büyüme** — "minicik dükkânının hareketli bir merkeze dönüşmesini izlemek."

Tek oda kipi bunu kapatıyor. Fallout Shelter'ın belgelenmiş çıkmazı tam bu: uzaklaşınca dokunulamıyor, yaklaşınca görülmüyor, ve iki hedef (oda mı, içindeki kişi mi) aynı piksele düşüyor.

Odalı düzenin lehine olan tek şey burada, ölçümde: **uzak kipte oda 106 dp.** Yani "uzaklaş, ama dokunulabilirliği kaybetme" mümkün — dokunulan şey masa değil oda olduğu sürece.

---

## 8. "Patron, şef değil" fantezisine etkisi

docs/02 §1: "Sen aşçı değil patronsun." docs/14: patron pişiremez, salonda 1,4 iş-günü katkı verir, **aynı anda tek yerde olabilir.**

| Argüman | Yön |
|---|---|
| Patron **bölüm** düşünür, şef **istasyon** düşünür. Mutfak, salon, bulaşık zaten docs/14'ün rol ayrımı | Odalar **lehine** |
| Two Point, türün en saf "patron" fantezisi ve tamamen oda tabanlı | Odalar **lehine** |
| Ama bizim karar birimlerimiz menü, fiyat, kadro ve istasyon ataması; hepsinin zaten ayrı ekranı var (docs/16 ekran 8, 11, 12, 13) | Odalar **aleyhine**: aynı kararın ikinci bir temsili |
| Patron aynı anda tek yerde olabiliyorsa (docs/14), kamerayı bir odaya kilitlemek fantezi ile **tutarlı** | Odalar lehine, **ama servis dokunuş bütçesini yakarak** |
| docs/16'nın kendi çıkarımı: "Salon dekora dönüşür; ama zaten patron oynuyoruz, garson değil" | Sabit görünüm lehine |

Sonuç: odalar fantezi ile çelişmiyor, hatta destekliyor. Ama fantezinin gerektirdiği şey **odaların içinde gezinmek** değil, **odalar hakkında karar vermek.** İkincisi bir arayüz işi.

---

## 9. Öneri

**Odalı yerleşim benimsensin — sanat ve genişleme metaforu olarak. Etkileşim modeli ve servis kamerası olarak benimsenmesin.**

Somut olarak beş madde:

| # | Karar | Gerekçe |
|---|---|---|
| 1 | **Mekân koylardan kurulsun** (mutfak, bulaşık, salon koyları). Kademe yeni bir salon koyu ekler | Kademe artışları (+3, +3, +4) zaten koy büyüklüğü. `RoomLayout.cs`'te kademe 1'de masa 44 dp, açık salonda 20 dp; şeride dizmek yatay ekranı doğru kullanıyor |
| 2 | **Servis sırasında kamera sabit, bütün restoran karede, oyuncunun kamera işi sıfır** | review/05 kamerayı 2 dokunuşla bütçelemiş; oda gezinmesi 12 dokunuş isterdi. Cooking Fever, Good Pizza, CSD ve Recettear hepsi sabit |
| 3 | **Servis sırasında masa dokunma hedefi değil.** Birincil hedef alt çubuktaki sabır kuyruğu çipleri; çip masayı vurgular ve müdahaleyi tetikler | Hiçbir yerleşim kademe 4'te masayı 48 dp yapmıyor. Çip istenen dp'de olabilir. review/05 zaten bunu önerdi; docs/16'nın üçüncü yolu bu |
| 4 | **Oda çerçeveli kamera sadece yerleşim düzenleme ekranında** (docs/16 ekran 11) var olsun, Kairosoft kalıbıyla: kareye dokun → menü, hayalet ızgara → tekrar dokun → onay, artı yedekli gezinme (sürükle + pinch + köşe yön kontrolü) | Orada masa ~45 dp, masa seti ~100 dp ve dokunuş bütçesi baskı altında değil. Two Point'in konsolda düştüğü "hedefi aşma + geri alma yok" tuzağını onay adımı kapatıyor |
| 5 | **Koylar mühürlü oda olmasın: kapı, koridor, yol geçerliliği yok.** Garsonun yürüyüşü görsel, simüle edilmiyor | docs/14 "karmaşık yol bulma yok" diyor. Çekirdekte zaten mekân yok. Two Point'in belgeli hata sınıfını satın almanın anlamı yok |

**En büyük bedeli:** servis sırasında salon **dekora düşüyor.** Oyuncunun dikkati, sanat bütçesinin çoğunu harcadığımız 3B sahneden alt çubuktaki bir arayüz şeridine kayıyor. research/01'in en sevilen mekanikler sıralamasında 2. ve 3. sırada olan "yerleşim tasarımı" ve "görünür büyüme", servis boyunca arka plana iniyor ve ödülünü ancak yerleşim ekranında ve gün sonu karesinde veriyor. Dokunulabilirliği, üstünde durduğumuz şeyi geri plana atarak satın alıyoruz.

İkinci bedel: **depo odası ile kapı/duvar kiti**, arkasında hiçbir simülasyon olmayan yeni sanat işi. Depoya bir iş verilemiyorsa yerleşimden çıkarılmalı.

---

## 10. Açık kalanlar

1. **`RoomLayout.cs` konsol satırı dosyaya yazılmalı.** Bu dosyadaki oda dp değerleri benim hesabım ve render okumam; ölçülmüş sayı değil. Karar işaretlere dayanıyor ama sayılar dokümana ölçülmüş hâliyle girmeli.
2. **Kamera sığdırması düzeltilirse açık salon kaç dp'ye çıkar?** `+ maxZ` terimi ve duvar yüksekliğinin sığdırmaya katılması, kareyi kabaca %45 doluluğa düşürüyor. Kırpan bir kamerayla 15 dp → 22-25 dp bekliyorum. Ölçülmeden bilinmez, ve 48'e yetmeyecek olsa da B ile C arasındaki farkı değiştirebilir.
3. **Depo odasının işi ne?** İşi yoksa çıkmalı. docs/12'nin stok ve bozulma mekaniğine görünür bir karşılık verilebilir mi?
4. **Kaç salon koyu çeşidi gerekiyor?** Dört aynı koy kopyala-yapıştır okunur. 3 mü, 4 mü, kaç varyantla yeterli görünür?
5. **Kamera durumu kaydedilmiyor** (docs/23). Yerleşim ekranından çıkıp dönen oyuncu hangi koyda uyanacak? İstisna yazılmalı mı?
6. **Çip çubuğu üst çubukla çatışıyor mu?** review/05 üst çubuğa müdahale hakkı ve gün ilerlemesi koymayı önerdi; alt çubuğa çip geliyor. 393 dp'lik yatay yükseklikte iki şeridin toplam payı ölçülmeli.
7. **İkinci şube (docs/02 §6, bölüm 6) odalı düzende ne demek?** Şerit uzamaya devam eder mi, yoksa Cooking Fever kalıbıyla ayrı sahne mi olur? İlk sürüm kapsamı dışında ama yerleşim kararı buna bakmalı.
8. **Restaurant Renovation referans listesinden düşmeli.** Doğrulandı: eşleştirme bulmacası, yönetim oyunu değil.

---

## 11. Kaynaklar

**Standart ve ölçüm**
- Google, dokunma hedefi boyutu: https://support.google.com/accessibility/android/answer/7101858
- Material Design erişilebilirlik: https://m2.material.io/design/usability/accessibility.html
- Proje içi: `unity/Assets/Lokanta/Editor/RestaurantScene.cs`, `unity/Assets/Lokanta/Editor/RoomLayout.cs`, `tools/art/out/unity/oda_*.png`

**Diner Dash ve mobil servis oyunları**
- https://en.wikipedia.org/wiki/Diner_Dash
- https://en.wikipedia.org/wiki/Diner_Dash:_Hometown_Hero
- https://dinerdash.fandom.com/wiki/Walkthrough:Flo's_Diner_(Diner_Dash) (dizin alıntısı)
- https://apps.apple.com/us/app/diner-dash-adventures/id1380831764
- https://www.levelwinner.com/diner-dash-adventures-beginners-guide-tips-cheats-strategies-to-restore-dinertown/
- https://www.nowf.com/guides/diner-dash-adventures-guide
- https://www.touchtapplay.com/diner-dash-adventures-cheats-tips-guide-to-pass-all-levels/

**Cook, Serve, Delicious!**
- https://en.wikipedia.org/wiki/Cook,_Serve,_Delicious!_2 , .../Cook,_Serve,_Delicious!_3
- https://store.steampowered.com/app/386620/Cook_Serve_Delicious_2/
- https://steamcommunity.com/app/386620/discussions/0/1520386297697292960/ (menü ve istasyon yuvaları)
- https://www.choicestgames.com/2023/08/cook-serve-delicious-2-review.html (Designer kozmetik)
- https://www.pocketgamer.com/cook-serve-delicious-mobile/warning-android-cook-serve-delicious-users-the-game-is-getting-delisted-but-dont/

**Good Pizza, Great Pizza / Cooking Fever / Cooking Diary / Cooking Madness / Animal Restaurant**
- https://en.wikipedia.org/wiki/Good_Pizza,_Great_Pizza
- https://store.steampowered.com/app/770810/Good_Pizza_Great_Pizza__Cooking_Simulator_Game/
- https://noodlearcade.com/cooking-fever-ultimate-strategy-guide (dört müşteri yuvası)
- https://www.pocketgamer.com/cooking-fever/cooking-fever-tips-and-tricks-how-to-survive-hells-kitchen/
- https://en.wikipedia.org/wiki/Cooking_Fever
- https://cookingdiary.game/game-guide/game-tips/tips-and-tricks
- https://play.google.com/store/apps/details?id=droidhang.twgame.restaurant
- https://animalrestaurant.fandom.com/wiki/Animal_Restaurant (alan listesi, dizin alıntısı)
- https://www.levelwinner.com/animal-restaurant-beginners-guide-tips-cheats-strategies-to-grow-your-restaurant-business-fast/

**Dükkân simülasyonları**
- https://store.steampowered.com/app/2670630/Supermarket_Simulator/
- https://supermarket-simulator.fandom.com/wiki/Growth , .../Storage (dizin alıntısı)
- https://theguidehall.com/supermarket-simulator-how-unlock-storage/
- https://store.steampowered.com/app/3070070/TCG_Card_Shop_Simulator/
- https://tcgcardshopsimulator.wiki.gg/wiki/RENO_BIGG
- https://steamcommunity.com/app/3070070/discussions/0/4849903998512913531/ (duvar ve bölme talebi)
- https://en.wikipedia.org/wiki/Recettear:_An_Item_Shop%27s_Tale
- https://recettear.fandom.com/wiki/Merchant_Level (dizin alıntısı)
- https://www.thegamer.com/moonlighter-shop-upgrades/ , https://moonlighter.fandom.com/wiki/Shop_Upgrades
- https://www.pocketgamer.com/moonlighter/moonlighter-hands-on-innovative-controls-and-great-design-updated/ (dokunmatik yeniden tasarım)
- https://toucharcade.com/2020/12/01/moonlighter-review-iphone-ipad-android/
- https://store.steampowered.com/app/1525700/Tavern_Master/ , https://steamcommunity.com/app/1525700/discussions/0/3202621452558674963/
- https://catcafemanager.wiki.gg/wiki/Design_Mode , https://steamcommunity.com/app/1354830/discussions/2/3830914078559477875/ (iç duvar yok)
- https://steamcommunity.com/app/1122340/discussions/0/3825289852122217751 (Chef Life yerleşim değişimi)
- https://www.thegamer.com/chef-life-a-restaurant-simulator-upgrade-decorate-restaurant/
- https://store.steampowered.com/app/2274620/Discounty/ , https://steamcommunity.com/app/2274620/discussions/0/601914904286416715/
- https://travellersrest.wiki.gg/wiki/Construction_Mode
- https://dave-the-diver.fandom.com/wiki/Bancho_Sushi (dizin alıntısı) , https://www.pockettactics.com/dave-the-diver/mobile
- https://play.google.com/store/apps/details?id=com.zymobile.restaurant (Restaurant Renovation, eşleştirme bulmacası)

**Two Point ve PlateUp!**
- https://en.wikipedia.org/wiki/Two_Point_Hospital (platform listesi, geliştirme zorlukları)
- https://two-point-hospital.fandom.com/wiki/Rooms , .../Corridor , .../Door (dizin alıntısı)
- https://www.gamepressure.com/two-point-hospital/hospital-rooms/zbb405 (asgari oda ölçüleri)
- https://gamefaqs.gamespot.com/pc/230622-two-point-hospital/faqs/76595/room-prestige
- http://www.nintendoworldreport.com/review/52928/two-point-hospital-switch-review (sanal imleç şeması)
- https://godisageek.com/reviews/two-point-hospital-switch-review-nintendo-sega/ (dokunmatik yok)
- http://www.nintendoworldreport.com/review/73084/two-point-museum-switch-2-review-in-progress (hedefi aşma, geri alma yok, küçük metin)
- https://steamcommunity.com/app/535930/discussions/0/1737715419898938140/ (yol bulma hataları)
- https://www.twopointstudios.com/en/post/creativity-tools-breakdown-two-point-campus
- https://wiki.plateupgame.com/gameplay/Restaurant , .../Modding/GameDataObjects/LayoutProfile (üretici sözleşmesi)
- https://wiki.plateupgame.com/gameplay/Headquarters , .../Automation
- https://github.com/Karl-HeinzSchneider/PlateUp-CameraPlus
- https://en.wikipedia.org/wiki/PlateUp!

**Kairosoft**
- https://www.gamezebo.com/walkthroughs/pocket-academy-walkthrough/ (pinch + yön tekerleği)
- https://www.gamezebo.com/walkthroughs/mega-mall-story-walkthrough/ (sürükle + yön okları, yatırım genişlemesi)
- https://www.gamezebo.com/walkthroughs/kairobotica-walkthrough/ (boş kareye dokun → menü)
- https://www.gamezebo.com/walkthroughs/the-sushi-spinnery-walkthrough/ (hayalet ızgara, tekrar dokun, onay)
- https://kairosoft.wiki.gg/wiki/Transcript:Manual_(Cafeteria_Nipponica) (turuncu çerçeveli alan)
- https://kairosoft.wiki.gg/wiki/Hot_Springs_Story , https://gamefaqs.gamespot.com/iphone/618271-hot-springs-story/faqs/61941 (tapular, ayak izleri, yürüme süresi)
- https://kairosoft.wiki.gg/wiki/Special_Rooms_(Dream_House_Days)
- https://toucharcade.com/2015/06/05/biz-builder-delux-review-like-several-kairosoft-games-stapled-together/ (arayüz iyileşmesi)
- https://toucharcade.com/2023/06/27/dream-town-island-mobile-kairosoft-game-review-iphone-ipad-android/ (kalan menü derinliği şikayeti)
- https://higherplaingames.com/mobile/cafeteria-nipponica-review/ (yakınlaştırma)
- https://www.whatsitlike.com.au/game-dev-story-switch-2-review/ (oyun içi imleç)

**Telefonda oda tabanlı büyüme**
- https://damonwakes.wordpress.com/2016/03/26/touchscreen-troubles/ (Fallout Shelter dokunmatik çıkmazı)
- https://steamcommunity.com/app/588430/discussions/0/1319962514593528480/ (otomatik yakınlaştırma şikayeti)
- https://gamerant.com/fallout-shelter-how-to-merge-rooms/
- https://en.wikipedia.org/wiki/Tiny_Tower
- https://www.couchclicker.com/complete-guide-to-hotel-empire-tycoon/ , https://www.levelwinner.com/hotel-empire-tycoon-beginners-guide-tips-cheats-strategies-to-grow-your-hotel-empire-fast/
