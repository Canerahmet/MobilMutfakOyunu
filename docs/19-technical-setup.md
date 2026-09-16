# Teknik Kurulum

**Son güncelleme:** 9 Eylül 2026
**Kütük maddeleri:** B4 sürüm ve render hattı, B5 performans hedefleri, B6 kayıt dosyası formatı, B7 girdi eylem haritası
**Durum:** Yazıldı, karar bekliyor

---

## B4. Unity sürümü ve render hattı

### Sürüm: Unity 6.3 LTS

> **9 Eylül 2026 notu.** Makinede kurulu sürüm 6000.5.8f1, yani Unity 6.5 teknoloji akışı, LTS değil. **Karar 9 Eylül 2026:** Hub'dan 6.3 LTS kurulacak, proje onunla açılacak. Bkz. [22-answers-and-direction.md](22-answers-and-direction.md) §2. Ayrıca ilk sürüm sadece Android; iOS ve Metal satırları Mac olunca geçerli.

| Neden | Açıklama |
|---|---|
| LTS zorunlu | Uzun ömürlü bir oyun yapıyoruz. Deneysel sürüm üstünde canlı oyun tutulmaz |
| Destek süresi | Aralık 2027'ye kadar destekleniyor. Geliştirme artı ilk canlı yılı kapsıyor |
| Sonrası | Unity 6.7 LTS 2026 sonunda geliyor. Geçiş, ilk sürüm yayınlandıktan sonra değerlendirilir |

**Kural: sürüm üretim başladıktan sonra değişmez.** Sadece güvenlik ve mağaza uyumluluğu yamaları alınır.

### Render hattı: URP

| Seçenek | Karar |
|---|---|
| **URP** | ✅ Seçildi. Mobil için tasarlanmış, low-poly stilize sahne için fazlasıyla yeterli |
| Built-in | Hayır. Unity'nin gelişim yönü artık orada değil |
| HDRP | Hayır. Masaüstü ve konsol için, mobilde kullanılamaz |

### Ayarlar

| Ayar | Değer | Gerekçe |
|---|---|---|
| Renk uzayı | Linear | Yumuşak ışık ve gölge için gerekli |
| Android grafik API | Vulkan öncelikli, OpenGL ES 3.1 yedek | Eski cihaz kapsamı |
| iOS grafik API | Metal | Tek seçenek |
| Doku sıkıştırma | ASTC | Mobilde standart |
| Ek ışık gölgeleri | Kapalı | GPU ve bellek tasarrufu |
| Store Actions | Auto veya Discard | Düşük seviye cihazlarda bant genişliği tasarrufu |
| SRP Batcher | Açık | Çizim çağrısı CPU maliyetini düşürüyor |
| **GPU Resident Drawer** | **Açık** | Unity 6 özelliği. Nesne yoğun sahnelerde render CPU maliyetini yarıya kadar düşürebiliyor |

**GPU Resident Drawer bizim için özellikle önemli.** Restoran sahnesi nesne yoğun: masalar, sandalyeler, tabaklar, müşteriler, dekor. Tam olarak bu özelliğin hedeflediği durum.

### Işıklandırma

- Sabit nesnelerin ışığı **pişirilmiş** (baked). Duvar, zemin, mobilya.
- Sadece bir yönlü ışık gerçek zamanlı.
- Karakterler ışık probu ile aydınlanıyor.
- Ağır son işlem yok. En fazla hafif bir renk derecelendirme tablosu.

Mutfak kimliğini taşıyan sıcak akşam ışığı, pişirilmiş ışıkla ve renk derecelendirmesiyle sağlanıyor. Gerçek zamanlı gölgeye ihtiyaç yok.

### Paketler

| Paket | İş |
|---|---|
| Input System | İki girdi şeması |
| Localization | Türkçe ve İngilizce |
| Addressables | İçerik yükleme ve indirme boyutu yönetimi |
| TextMeshPro | Türkçe karakter desteği olan yazı |
| Unity IAP | Mutfak satın alması |
| Analytics veya Firebase | Port arkasında |

---

## B5. Performans hedefleri

### Desteklenen cihazlar

| Platform | En düşük |
|---|---|
| Android | Android 10, 3 GB RAM, Vulkan veya OpenGL ES 3.1 |
| iOS | iOS 16, iPhone SE 2. nesil ve üstü |

Bu eşiğin altındaki cihazlar desteklenmiyor. Kapsamı genişletmek her şeyi yavaşlatır ve o kesim zaten küçük.

### Kare hızı

| Cihaz sınıfı | Hedef |
|---|---|
| Orta ve üst seviye | 60 fps |
| En düşük desteklenen | 30 fps **garantili** |

30 fps taban garanti. Oyun refleks oyunu olmadığı için 30 fps oynanabilirliği bozmuyor. Ama düşüşler bozuyor, bu yüzden hedef sabit kare hızı.

### Bütçeler

| Kalem | Hedef | En düşük cihazda |
|---|---|---|
| Çizim çağrısı | Kare başına 100 altı | 60 altı |
| Görünür üçgen | 100 bin altı | 60 bin altı |
| Bellek | 700 MB altı | 500 MB altı |
| İndirme boyutu | **200 MB altı** | Aynı |
| Sürekli oynama | 30 dakika ısınma kısıtlaması olmadan | Aynı |

**200 MB kritik bir eşik.** Hem Google Play hem App Store, bu boyutun üstündeki uygulamaları hücresel veriyle indirirken uyarı gösteriyor. Uyarı görmek kurulum oranını düşürüyor.

Bunun altında kalmak için: mutfak varlıkları Addressables ile ayrılıyor, satın alınan mutfak indirilirken çekiliyor. Böylece ilk indirme sadece fast food içeriyor.

### Ölçülen (13 Eylül 2026)

Sahne yeniden tasarlandıktan sonra (arka duvar, zemin deseni, servis bankosu,
teras, masalarda yemek, ocak üstü kaplar) ölçüldü. Bir önceki tablo sarkıt
lambalar ve ön sıra tezgâhları **çıkarılmadan önce** alınmıştı ve "ölçüm"
etiketi taşımaya devam ediyordu — yani bugünkü sahneyi ölçmüyordu.

| sahne | çizici | üçgen | toplu çizim dışı |
|---|---:|---:|---:|
| Türk, açılış (4 masa) | 175 | 25.088 | 65 |
| Türk, genel (10 masa) | 243 | 38.205 | 83 |
| Hızlı yemek, açılış | 196 | 28.308 | 64 |
| Hızlı yemek, genel | 272 | 44.372 | 84 |

**Üçüncü sütun yeni.** `MaterialPropertyBlock` yazılan bir çizici SRP toplu
çizimine giremiyor; bir denetim bunun 160 civarında olabileceğini tahmin etti
(rozet, kıyafet, ocak üstü, köpük, tepsi). Ölçüldü: **84**. Tahmin ile ölçüm
arasındaki fark iki kat — ve tahmin, gereksiz bir iyileştirmeyi haklı
gösterecek yöndeydi.

**Çizici ≠ çizim çağrısı:** SRP toplu çizimi aynı malzemeyi paylaşanları
birleştiriyor, yani gerçek çağrı sayısı bunun altında. Bu tablo bir **üst sınır**
ve asıl işi regresyonu yakalamak — sahneye sessizce yüz nesne ekleyen bir
değişiklik burada görünür.

### Eşikler nerede ve neden bütçeden farklı

Bu iki sayıyı **iki ayrı araç** ölçüyor ve uzun süre ikisi de hiçbir şeyi
kırmıyordu:

| araç | neyi ölçüyor | eşik |
|---|---|---:|
| duman turu (`Autopilot`) | **taban** sahne, 4 masa — tur hiç genişlemiyor | 400 çizici / 80 bin üçgen |
| `GameShot` | **tavan** sahne, bütün odalar açık | 360 çizici / 70 bin üçgen / 220 toplu-çizim-dışı |

Eşikler yukarıdaki bütçe satırından (100 / 60 bin) **büyük**, ve bu kasıtlı:
bütçe satırı *çizim çağrısını* sayıyor, bu iki araç *çizici* sayıyor ve toplu
çizim ikisi arasındaki farkı kapatıyor. Eşikler bugünkü ölçümün üzerine pay
bırakacak şekilde seçildi — küçük eklemeler kırmasın, **sessiz bir şişme**
yakalansın.

**Üçüncü sayı yeni:** *toplu çizim dışı çizici*. `MaterialPropertyBlock` yazılan
bir çizici SRP toplu çizimine giremiyor; proje bunu defalarca yazıp zemini ve
oda ışığını ona göre tasarlamıştı, ama üç yeni sistem (rozet, kıyafet, ocak üstü)
aynı bedeli ödemeye devam ediyordu ve **hiç ölçülmüyordu**. Ölçülmediği için de
kimse fark etmiyordu.

Tavan eşiği turda değil `GameShot`'ta, çünkü tur dört masada kalıyor: on dört
masalık sahne bütçeyi aşsa turdaki eşik hiçbir zaman kırılamazdı. *Bir eşik,
kırılamayacağı yerde durursa eşik değildir.*

Tur da aynı iki sayıyı ölçüyor ama **dört masada**: altmış günü oynuyor,
genişlemiyor. İlk yazımında yorum "kampanya sonunda restoran en büyük" diyordu
ve tanı onu çürüttü — *ölçünün adı neyi ölçtüğünü söylemeli.*

### Ölçüm

- Her sürümde gerçek cihazda profil çıkarılıyor, editörde değil.
- En az iki cihazda test: bir üst seviye, bir en düşük seviye.
- Bütçe aşımı, özellik eklemeden önce çözülüyor.

---

## B6. Kayıt dosyası formatı

### Format

| Karar | Değer |
|---|---|
| Biçim | JSON |
| Sıkıştırma | gzip |
| Bütünlük | Sağlama toplamı, dosya sonunda |
| Şifreleme | **Yok** |
| Hedef boyut | Kayıt başına 200 KB altı |

### Neden JSON

Okunabilir, hata ayıklaması kolay, sürüm göçü yazması kolay. Sıkıştırıldıktan sonra boyut sorunu kalmıyor.

İkili biçim biraz daha küçük olurdu ama göç fonksiyonu yazmak ve bir oyuncunun bozuk kaydını incelemek çok daha zor olurdu.

### Neden şifreleme yok

Bu tek oyunculu bir oyun. Skor tablosu yok, çok oyunculu yok, rekabet yok. Kaydını düzenleyen biri sadece kendi oyununu etkiliyor.

Şifreleme karşılığında ne veriyor: destek zorlaşıyor, hata ayıklama zorlaşıyor, ve kararlı bir oyuncu zaten aşıyor. **Kazancı yok, maliyeti var.**

### Dosya düzeni

```
saves/
  slot_1.save      güncel
  slot_1.bak       bir önceki
  slot_2.save
  slot_2.bak
  ...
```

Yazma sırası: geçici dosyaya yaz, sağlamayı doğrula, güncel dosyayı yedeğe taşı, geçici dosyayı güncel yap. Bu sıra yarım dosya oluşmasını imkânsız kılıyor.

---

## B7. Girdi eylem haritası

Unity Input System kullanılıyor. **Oyun kodu ham cihaz girdisini asla okumuyor**, sadece niyeti okuyor.

### İki kontrol şeması

| Şema | Cihaz | Ne zaman |
|---|---|---|
| Touch | Dokunmatik ekran | Mobil, varsayılan |
| Desktop | Fare ve klavye | Steam sürümü |

Oyun kolu şeması ileride eklenebilir, ama ilk sürümde yok.

### Eylem haritaları

| Harita | Ne zaman aktif |
|---|---|
| UI | Menülerde ve ekranlarda |
| Service | Servis aşamasında |
| Layout | Yerleşim düzenlemede |
| Camera | Servis ve yerleşimde, UI ile birlikte |

Aynı anda birden fazla harita aktif olabiliyor. Örneğin serviste hem Service hem Camera açık.

### Eylemler

| Eylem | Touch | Desktop |
|---|---|---|
| Point | Parmak konumu | Fare konumu |
| Select | Tek dokunuş | Sol tık |
| Drag | Basılı tut ve sürükle | Sol tık ve sürükle |
| Intervene | Masaya dokunma | Sağ tık veya sol tık |
| Pan | İki parmak kaydırma | Orta tık sürükleme veya WASD |
| Zoom | İki parmak sıkıştırma | Fare tekerleği |
| Pause | Duraklat düğmesi | Boşluk tuşu veya Esc |
| Cancel | Geri düğmesi | Esc |
| Confirm | Onay düğmesi | Enter |

**Hassas nişan hiçbir şemada yok.** Dokunmatikte olmayan bir yetenek masaüstünde de kullanılmıyor, çünkü iki sürüm arasında oynanış farkı olmamalı.

### Neden baştan iki şema

Steam ileride hedefleniyor. Girdi soyutlaması sonradan eklenirse bütün etkileşim kodunu yeniden yazmak gerekir. Şimdi kurmanın maliyeti neredeyse sıfır.

---

## Geliştirme makinesi tuzagı: Smart App Control

Bu makinede **Smart App Control zorunlu modda** (Windows 11, CI politikası
`{0283ac0f-fff1-49ae-ada1-8a933130cad6}`). Taze yazılmış, imzasız bir
derlemenin *yüklenmesini* engelleyebiliyor:

```
System.IO.FileLoadException: Could not load file or assembly
'...\Lokanta.Core.dll'. An Application Control policy has blocked
this file. (0x800711C7)
```

**Hangi yapılandırmanın engellendiği zamanla değişiyor:** bir gün `Debug`
bloke, ertesi gün `Release`. 11 Eylül sabahı önce `Debug` çalışıyordu, birkaç
saat sonra aynı komut `Debug`'da bloke olup `Release`'de geçti. Engel dosyanın
**karmasına** bağlı: çekirdek değişmediği sürece sorun çıkmıyor, değiştiği
anda çıkabiliyor.

Belirti yanıltıcı. Bir kere 213 testin 206'sı birden kırıldı ve
`Fx.MulDiv_sifira_bolmede_atar` gibi **saf matematik** testleri
`DivideByZeroException` yerine `FileLoadException` verdi — hepsi aynı
sebepten. Kodda aranacak bir şey yok.

### Neden bazen geçiyor, bazen geçmiyor

Engel dosyanın **karmasına** bağlı. Çekirdek ve içerik
`<Deterministic>true</Deterministic>` ile derleniyor, yani **aynı kaynak her
zaman aynı ikiliyi** üretiyor. Sonuç: bir kez engellenen derleme, yeniden
derlemekle **düzelmiyor** — aynı dosya, aynı engel, sonsuza kadar. Kaynak
değişene kadar o hedef kilitli kalıyor.

Bu yüzden "bir gün Debug bloke, ertesi gün Release" görünüyor: aslında
değişen şey gün değil, o yapılandırmanın ikilisinin en son ne zaman
değiştiği.

### Doğru tepki

1. `Microsoft-Windows-CodeIntegrity/Operational` günlüğüne bak (olay
   3033/3077/3118). Orada görünmeyen bir şey SAC değildir. **Kodda arama.**

2. **`python tools/dotnet_retry.py <args>`** ile koş. Araç üç şeyi birden
   yapıyor ve üçü de gerekli — her biri ayrı ayrı denendi ve yetmedi:

   | ne | neden gerekli |
   |---|---|
   | `-p:Deterministic=false` | derleyici her seferinde yeni bir modül kimliği gömsün |
   | `--no-incremental` | **bayrak tek başına hiçbir şey yapmıyor**: kaynak değişmediyse MSBuild derlemeyi güncel sayıp atlıyor ve aynı engelli ikiliyi geri veriyor. Ölçüldü — altı deneme, altı aynı engel |
   | yeniden deneme | taze bir karma da engellenebiliyor; engel olasılıklı |

3. **`dotnet run` için derleme AYRI adım olmalı.** `run` `--no-incremental`
   bayrağını tanımıyor ve **uygulamaya geçiriyor**; harness de haklı olarak
   "bilinmeyen bayrak" diye reddediyor. Önce `dotnet build … --no-incremental`,
   sonra `dotnet run --no-build`.

`tools/check.py` ve `tools/balance/calibrate.py` bunu kendiliğinden yapıyor ve
ikisi de boş tablo döndürmek yerine **hata fırlatıyor** — sessizce boş dönmek
bütün adayları eşitliyor ve kalibrasyon rastgele birini "en iyi" seçiyordu.

Determinizm yalnızca **yerel koşu** için kapatılıyor, projede açık kalıyor.

**Smart App Control kapatılmadı ve kapatılmamalı:** Windows'ta tek yönlü bir
kapı — kapatıldıktan sonra yeniden açmak işletim sistemini yeniden kurmayı
gerektiriyor. Bu bir geliştirme makinesi ayarı, proje kararı değil.

### Üç yanlış teşhis, sırayla

Bu tuzak 11 Eylül'de üç kez yanlış teşhis edildi ve her biri saatler aldı.
Kayda geçiyor ki dördüncüsü olmasın:

1. **"netstandard2.1 engelleniyor, net10.0 geçiyor."** Yanlış — aynı net10.0
   DLL birkaç saat sonra engellendi. (Çok hedefli derleme yine de tutuldu,
   ama başka bir gerekçeyle; aşağıya bak.)
2. **"yapılandırma dönüşümlü: bir gün Debug, bir gün Release."** Yanlış —
   değişen şey gün değil, o yapılandırmanın ikilisinin en son ne zaman
   değiştiği.
3. **"`-p:Deterministic=false` çözer."** Eksik — MSBuild yeniden derlemiyor.

### İki hedef: ayrı bir karar

`Lokanta.Core` ve `Lokanta.Content`
`<TargetFrameworks>netstandard2.1;net10.0</TargetFrameworks>` taşıyor. Bu
**SAC geçişi değil** — bir süre öyle sanıldı ve yanlıştı. Gerçek gerekçe:

- `netstandard2.1` Unity'nin API yüzeyi. Çekirdek onun dışına çıkarsa
  derleme **burada** kırılır. Hiç yüklenmiyor, yalnızca derleniyor.
- `net10.0` testlerin ve denge aracının yüklediği derleme.

O koruma yalnızca derlendiğinde işe yarıyor ve kimse onu yüklemediği için
kendiliğinden derlenmiyor: `tools/check.py` her iki hedefi de ayrı bir
denetim olarak koşuyor.

---

## Karar bekleyen ayrıntılar

1. En düşük Android sürümü 10 mu olmalı, daha aşağı inilmeli mi
2. 30 fps tabanı yeterli mi
3. Oyun kolu desteği ilk sürüme girmeli mi
4. Mutfak varlıklarının indirilebilir olması ilk sürümde mi yapılmalı
