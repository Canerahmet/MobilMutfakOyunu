# Faz 0, İkinci Dilim: Simülasyon ve Denge Aracı

**Son güncelleme:** 10 Eylül 2026
**Durum:** Çalışıyor. 88 test geçiyor, denge aracı altmış günlük kampanyayı koşuyor.
**Önceki dilim:** [06-plan-durumu.md](06-plan-durumu.md) sonundaki Faz 0 tablosu

---

## 1. Ne yapıldı

| Parça | Nerede | Durum |
|---|---|---|
| Unity projesi, Android hedefli | `unity/` | Kuruldu, ayarlar kodla uygulanıyor |
| Sabit adımlı simülasyon | `unity/Assets/Lokanta/Core/Sim/Simulation.cs` | Bir servis gününü baştan sona koşuyor |
| Komut ve olay tipleri | Aynı klasör | 17 komut, 18 olay türü |
| Zaman ayarı | `TimingConfig.cs` | Kapasite modelinden türüyor |
| İçerik yükleyici, yemek ve arketip | `unity/Assets/Lokanta/Content/` | Doğrulamalı |
| Denge aracı | `src/Lokanta.Harness/` | Beş strateji, çok tohumlu |
| Testler | `tests/Lokanta.Core.Tests/` | 88 test |

**Unity projesi kodla yapılandırılıyor.** `Lokanta/Proje ayarlarını uygula` menüsü ya da toplu kipte `tools/unity/run.ps1`. Elle tıklanan ayar yok, tekrarlanabilir.

Uygulanan ayarlar: Linear renk uzayı, Vulkan öncelikli ve OpenGL ES 3 yedekli grafik API, IL2CPP, ARM64, Android 10 tabanı, ASTC doku sıkıştırma, AAB çıktısı, yalnızca yatay yön, ivmeölçer kapalı, URP asseti oluşturulup atanmış, HDR kapalı, ek ışık gölgeleri kapalı, **GPU Resident Drawer kapalı**.

Son madde değerlendirmenin düzeltmesiydi ve [19-teknik-kurulum.md](19-teknik-kurulum.md) B4 tablosundaki "Açık" satırını geçersiz kılıyor.

---

## 2. Kaynak yerleşimi kararı

C# kaynakları `unity/Assets/Lokanta/` altında duruyor. `src/` altındaki proje dosyaları onları **bağlayarak** derliyor.

```
unity/Assets/Lokanta/Core/      kaynak burada + Lokanta.Core.asmdef
src/Lokanta.Core/*.csproj       ../../unity/Assets/... dosyalarını Compile Include ile alıyor
```

**Neden Assets altında:** asmdef dosyasındaki `noEngineReferences: true` bayrağı, "çekirdek Unity'yi bilmez" kuralını **derleyici seviyesinde** zorluyor. Klasör yerleşiminden daha güçlü bir garanti; çekirdek `UnityEngine` tipini göremiyor bile.

**Neden ayrıca csproj:** `dotnet test` 88 testi bir saniyede koşuyor, Unity dakikalarca açılıyor. İki derleyici, tek kaynak.

**Neden ara çıktılar `src/` altında:** Unity, Assets altındaki her dosya için `.meta` üretiyor. `bin/` ve `obj/` klasörlerini oraya bırakmak projeyi yüzlerce gereksiz dosyayla kirletirdi.

---

## 3. Simülasyonun modeli

**Personel ajan değil, sunucu.** [14-personel-sistemi.md](14-personel-sistemi.md) "karmaşık yol bulma yok" diyor. Simülasyonda personel, iş havuzunda aynı anda tek görev işleyen bir sunucu. Günlük kapasite görev sürelerinden kendiliğinden çıkıyor, ayrıca kodlanmıyor.

**Müşteri birimi karara bağlandı.** Değerlendirmenin "kişi/grup netleşsin" bulgusu: bir yuva, bir masayı işgal eden **gruptur**. Talep formülü **kişi** üretir, gruplar kişiler bitene kadar oluşur, iş ve hesap grup büyüklüğüyle ölçeklenir.

**Görev zinciri:** masa bekle → sipariş alınsın → pişsin → servis edilsin → ye → öde → masa toplansın. Salon havuzu sipariş, servis, ödeme ve toplamayı; mutfak havuzu pişirmeyi yapıyor.

**Öncelik kuralı tek cümle:** her boştaki sunucu, **sabrı en az kalan** müşteriye bakar. Eşitlikte küçük indeks kazanır. Bu tek kural, ayrıca "önce ödeme al, sonra servis yap" gibi öncelik listeleri yazmayı gereksiz kılıyor ve deterministik.

**Patron salonun sıfırıncı sunucusu** ve görevleri 1,4 kat hızlı bitiriyor, çünkü kapasite modeli patronu 1,4 iş-günü sayıyor.

---

## 4. Simülasyonun bulduğu tasarım hataları

Bu bölüm asıl değer. Hiçbiri okuyarak bulunmadı; hepsi simülasyon koşunca ortaya çıktı.

### 4.1 Sabır tek hızda tükenemez

İlk koşuda **altı gruptan altısı kızgın ayrıldı.** Sebep [12-ekonomi.md](12-ekonomi.md) §5.2'nin kendi içinde çelişmesiydi: sabır 8-40 saniye veriliyor, ama aynı bölüm bir servisin yaklaşık 120 saniye sürdüğünü söylüyor. Tek hızla her müşteri her zaman çıkıp giderdi.

**Doğru okuma:** sabır, ilgilenilmeme toleransıdır.

| Aşama | Tüketme hızı | Gerekçe |
|---|---|---|
| Masa bekliyor | %100 | Kimse ilgilenmiyor |
| Sipariş alınmayı bekliyor | %100 | Oturdu ama görülmedi |
| Garson masada | **%0** | İlgilenilen müşteri beklemiş sayılmaz |
| Yemek pişiyor | %35 | Sipariş verildi, beklenti kuruldu |
| Hesap bekliyor | %50 | Karnı tok, ama gitmek istiyor |

### 4.2 Hazırlama süreleri kapasite modeliyle çelişiyordu

Üretilen içerikte hamburger 75.000 ms pişiyordu. 480.000 ms'lik serviste bir aşçı günde altı hamburger yapabilirdi; kapasite modeli 28 kişi diyor.

**Düzeltme:** `prepMs` artık elle yazılmıyor, `tools/content/gen_dishes.py` içinde kapasite bütçesinden türüyor. Karmaşıklık yalnızca yemekler arasındaki oranı belirliyor, mutlak değeri değil. Menü ortalaması bütçeye eşitleniyor, sapma binde altı.

### 4.3 Aceleci müşteri ağır yemek sipariş etmemeli

Sabrı 8 saniye olan kurye, 17 saniye pişen yemeği hiçbir zaman bekleyemez. Bu arketip yapısal olarak servis edilemezdi.

**Düzeltme:** müşteri, bekleyebileceği yemekler arasından seçiyor. Gerçekçi ve ucuz.

### 4.4 Ortalama fiş kola fiyatına iniyordu

Denge aracının ilk koşusu tuhaf bir sonuç verdi: **pasif oyuncu batmıyor, iyi oynayan batıyordu.**

Sebep: her müşteri menüden **eşit olasılıkla tek kalem** seçiyordu, yani müşterilerin büyük kısmı sadece içecek alıyordu.

**Düzeltme:** kişi başına bir **ana yemek kesin**, yan (%30) ve içecek (%40) olasılıklı. Ortalama 1,7 tabak. Bu, [07-mutfak-sistemi.md](07-mutfak-sistemi.md)'deki kombo mekaniğinin taban hali. Hazırlama süreleri de buna bölündü, çünkü kişi başına bütçe sabit.

### 4.5 Malzeme maliyeti hiç ödenmiyordu

Simülasyon malzeme maliyetini hesaplıyor ama kasadan düşmüyordu. Pasif oyuncunun 60 günü 32.348 sikkeyle bitirmesinin sebebi buydu; aradaki **15.494 sikke tam olarak ödenmeyen malzemeydi.**

**Geçici düzeltme:** maliyet satış anında düşülüyor. Doğrusu sabah hal aşamasında peşin ödenmesi; hal yazılınca bu satır kalkacak.

### 4.6 İtibar bir haftada tavana vuruyordu

[12-ekonomi.md](12-ekonomi.md) §5.5 formülü günde +12 puan veriyor. İtibar 30'dan 100'e dokuz günde çıkıyor, yani altmış günlük kampanyanın ana ilerleme ekseni ilk haftada tükeniyor.

**Düzeltme:** kazanç kalan boşluğa oranlanıyor, **kayıp oranlanmıyor.** İtibar zor kazanılıyor, kolay kaybediliyor.

---

## 5. Denge aracının cevapları

On tohum, altmış gün, beş strateji.

<!-- ÜRETİLEN: harness -->
| Strateji | Son kasa | İtibar | Masa | Kadro | Ağırlanan | İlk borç |
|---|---|---|---|---|---|---|
| Pasif | 16.900 | 41,1 | 4 | 1 | 824 | — |
| Makul | 12.788 | 97,1 | 11,2 | 8,2 | 2.414 | 46. gün |
| Genişlemeyen | 9.275 | 50,6 | 4 | 3 | 907 | — |
| Yüksek fiyat | 7.367 | 1,4 | 4 | 2,6 | 570 | — |
| Fazla kadro | 8.196 | 49,7 | 4 | 3 | 910 | — |

| Strateji | Ciro | Malzeme | Maaş | Kira | Net | Fiş/kişi |
|---|---|---|---|---|---|---|
| Pasif | 48.419 | 15.448 | 8.471 | 15.600 | 8.900 | 58,7 |
| Makul | 145.738 | 46.401 | 38.589 | 43.740 | 17.008 | 60,4 |
| Genişlemeyen | 53.711 | 17.131 | 19.705 | 15.600 | 1.275 | 59,2 |
| Yüksek fiyat | 42.852 | 10.614 | 17.270 | 15.600 | −633 | 75,2 |
| Fazla kadro | 53.809 | 17.153 | 20.860 | 15.600 | 196 | 59,2 |
<!-- /ÜRETİLEN: harness -->

**Doğrulanan tasarım niyetleri:**

- **Yüksek fiyat kaybettiriyor.** İtibar 1,4'e çöküyor, ciro düşüyor, altmış günü zararla kapatıyor. Fiş 75 sikkeye çıksa da müşteri gelmiyor.
- **Fazla kadro cezalandırılıyor.** Kadroyu tavana dayamak, üç kişilik kadroyla aynı müşteriyi ağırlıyor ama 1.155 sikke fazla maaş ödüyor.
- **Para hiçbir stratejide önemsizleşmiyor.** Uyarı eşiği hiç tetiklenmedi.

**Açık kalan iki denge sorunu:**

1. **Pasif oyuncu batmıyor.** 8.000 ile başlayıp 16.900 ile bitiriyor. Hiç müdahale etmemenin bedeli yok; batma merdiveni tehdit üretmiyor. Tasarım niyeti bunun tersi.
2. **Makul oyuncu her koşuda borca düşüyor**, ortalama 46. günde. Üçüncü genişleme (10.400 bedel, haftalık 10.450 kira) karşılanamıyor. Genişlemenin bedeli olmalı ama batma sebebi olmamalı.

İkisi de aynı kökten: **kira ve genişleme bedelleri kapalı form haftalık modelden çözülmüştü, simülasyondan değil.** Sıradaki iş `tools/balance/solve.py`'yi simülasyona bağlayıp parametreleri oradan aratmak.

---

## 5b. Hal aşaması ve ölüm sarmalı

10 Eylül, ikinci oturum. Bölüm 5'teki iki açık sorundan birincisi ele alındı.

### Teşhis düzeltmesi: kira değil, mekanik

"Pasif oyuncu batmıyor" bir parametre sorunu sanılmıştı. Kirayı yükseltmek yanlış çözüm olurdu; iyi oynayanı da aynı ölçüde cezalandırırdı. Pasif oyuncunun **gerçekte yapmadığı şey malzeme almak.** Hal aşaması yazılmadığı için restoran kendi kendini işletiyordu.

Stok sistemi yazıldı: malzeme peşin alınıyor, sipariş alınırken tüketiliyor, bozulabilir olanlar gün sonunda sıfırlanıyor.

**Sonuç kesin.** Pasif oyuncu artık altmış günde 13 kişi ağırlıyor ve talebin %98'i kapıdan dönüyor. İhmalin bedeli var.

### Menüde bir şey yoksa müşteri kapıdan döner

İlk uygulamada stoksuz müşteri masaya oturuyor, sabrı bitene kadar bekliyor, sonra "kızgın ayrıldı" sayılıyordu. Bir stok hatası, kırk dakika bekletilmiş müşteriyle aynı itibar cezasını alıyordu ve bütün stratejiler ölüm sarmalına giriyordu.

Düzeltme: ana yemek bulunamayan grup içeri girmiyor. İtibar cezası var ama küçük, ve masa meşgul edilmiyor. Kızgın müşteri sayısı 271'den 0'a düştü.

### Hal aşamasının gerçek gerilimi: menü genişliği

Stok kontrolü **grup başına** yapılıyor, öneri ise günlük **ortalamayı** hesaplıyordu. On iki ana yemekli menüde her yemeğe günde yarım sipariş düşüyor; üç kişilik bir grup o yemeği isteyince üç porsiyonluk malzeme gerekiyor ve stokta yarım porsiyon var.

Düzeltme iki taraflı: öneri her açık yemek için en az bir grubu karşılayacak tabanı tutuyor, ve makul strateji artık menüyü talebe göre daraltıyor. **Geniş menü taşımak pahalı, çünkü bozulabilir malzeme her gün sıfırlanıyor.** Bu, hal aşamasının tasarım gerilimi ve modelden kendiliğinden çıktı.

### Kalan sorun ölçüldü: kira değil, ölüm sarmalı

Araca bir kira arayıcısı eklendi: `dotnet run --project src/Lokanta.Harness -- --solve`. Kirayı %25'e kadar indirip otuz iki kira ve genişleme kombinasyonu denedi.

**Hiçbiri işe yaramadı.** Kira dörtte bire inse bile makul oyuncu batıyor.

Servis oranı sütunu sebebi gösteriyor:

| Strateji | Talebin ağırlanan payı | Kapıdan dönen |
|---|---|---|
| Pasif | %2 | %98 |
| Sadece hal | %47 | %47 |
| Makul | %32 | %64 |

Zincir şu:

1. Birinci kademede ekonomi en iyi ihtimalle başabaş: kira 1.950 artı maaş 980, haftalık brüt kâr ise yaklaşık 2.856
2. Herhangi bir düşüş kasayı eritiyor
3. Kasa erirse malzeme alınamıyor, müşteri kapıdan dönüyor
4. İtibar düşüyor, talep düşüyor, ciro düşüyor
5. **Sarmal geri döndürülemez**, çünkü toparlanmak için malzeme almak, malzeme almak için para gerekiyor

İlk on iki gün sağlıklı geçiyor (günde 7-15 kişi, itibar 3.171'den 3.904'e). Çöküş kasa tükendiğinde başlıyor.

**İki kök sebep, ikisi de ölçüldü:**

- **Kapalı form model talebin %100'ünün ağırlandığını varsayıyor**, simülasyon %47 ile %83 arasında ağırlıyor. Kiralar o varsayımla çözülmüştü.
- **Hiçbir strateji kredi kullanmıyor.** [12-ekonomi.md](12-ekonomi.md) §4 tam olarak bu durum için kredi tanımlıyor: 5.000 sikke, 1,35 kat geri ödeme, sekiz hafta. Sarmalın tabanı bu olmalı ve strateji onu hiç denemiyor.

Sıradaki denge geçişi bu ikisiyle başlamalı. Kira ölçeğiyle oynamak denendi ve yetmediği kanıtlandı.

### Belirleyici hata: malzemenin parası iki kez ödeniyordu

Hal aşaması yokken `CompletePayment` içine geçici bir satır konmuştu: `kasa += fiş − maliyet`. Amacı malzemenin bir yerde ödenmesiydi. Hal yazılınca malzeme artık sabah peşin alınıyor, ama o geçici satır kaldırılmadı.

**Sonuç: her tabağın malzemesi hem halde hem kasada düşülüyordu.**

Gün gün iz sürünce ortaya çıktı. Brüt kâr günde 476 sikke, kasa artışı 116 sikke. Aradaki 360 tam olarak ikinci kez ödenen malzemeydi.

Bu tek satır, ondan önceki bütün denge ölçümlerini geçersiz kılıyordu. Kira arayıcısının otuz iki kombinasyonda başarısız olması, talep artırmanın işe yaramaması, kredinin yetmemesi: hepsi bu hatanın gölgesindeydi.

### Düzeltmeden sonra

| Strateji | Son kasa | İtibar | Servis oranı | İlk borç |
|---|---|---|---|---|
| Pasif | −15.448 | 0 | %2 | 21. gün |
| Sadece hal | −8.440 | 0 | %63 | 43. gün |
| **Makul** | **+2.914** | 60,3 | %88 | 56. gün |
| Genişlemeyen | +1.503 | 51,1 | %86 | 56. gün |
| Yüksek fiyat | −379 | 0 | %87 | 55. gün |
| Fazla kadro | −18.249 | 0 | %54 | 31. gün |

**Bölüm 5'teki iki açık sorun da kapandı:**

- **Pasif oyuncu 21. günde batıyor.** Talebin %98'i kapıdan dönüyor, altmış günde 13 kişi ağırlanıyor. İhmalin bedeli var.
- **Makul oyuncu artıda bitiriyor.** Servis oranı %88, itibar 60.

Tasarım niyetleri de doğrulanmaya devam ediyor: yüksek fiyat kaybettiriyor, fazla kadro en kötü sonucu veriyor.

### Yol boyunca düzeltilen üç strateji hatası

Denge aracının verisi, aracın oynattığı stratejinin kalitesi kadar iyi. Üç hata bulundu ve her biri sayıları belirgin biçimde değiştirdi.

| Hata | Neydi | Etkisi |
|---|---|---|
| **Yanlış sinyalden işe alım** | "İki gün üst üste kızgın müşteri varsa işe al" diyordu. Ama kızgın müşteri her zaman kadro sinyali değil; stok tükenmiş ya da sabrı kısa arketip zirvede beklemiş olabilir | Dört masada iki salon personeli tutuluyordu, haftalık 2.408 sikke maaş. Kapasite modeli o hacimde sıfır istiyor; patron tek başına yetiyor. Maaş 19.647'den 10.446'ya düştü |
| **Körlemesine genişleme** | Para ve itibar yeterliyse genişliyordu | Mevcut dükkânı dolduramayan oyuncu boş masalara kira ödemeye başlıyordu. Üçüncü şart eklendi: servis oranı %85 üstünde olmalı |
| **Kredi hiç kullanılmıyordu** | `TakeLoan` komutu enum'da duruyor ama uygulanmamıştı | [12-ekonomi.md](12-ekonomi.md) §4 tam bu durum için kredi tanımlıyor. Uygulandı ve strateji kasa iki haftalık gideri karşılamayınca çekiyor |

### Kalan denge kararı

Kira arayıcısı (`--solve`) artık çalışan bileşimler buluyor. Mevcut kirayla bile makul oyuncu artıda bitiyor, yani kira artık felaket değil, sıkı.

| Kira ölçeği | Makul son kasa | Makul borç riski | Pasif |
|---|---|---|---|
| %50 | 20.049 | %0 | batıyor |
| %75 | 13.449 | %0 | batıyor |
| %100 (mevcut) | 3.511 | %25 | batıyor |

**Bu bir tasarım kararı, teknik bir sorun değil.** Kirayı düşürmek `tools/balance/model.py`'nin de yeniden türetilmesini gerektirir, çünkü oradaki kiralar hedef marjlardan çözülmüştü. İki modelin uzlaştırılması ayrı bir iş.

Ayrıca dikkat: makul oyuncu altmış günde ortalama 5,5 masaya çıkıyor, kapalı form model ise 14 masaya çıkıldığını varsayıyor. Genişleme kapıları fazla mı sıkı, yoksa genişleme gerçekten mi karşılanamıyor, bu da ölçülmeli.

### İki modelin uzlaştırılması: gerçekleşme oranı

Kalan denge kararı ölçülerek kapatıldı.

İki strateji daha eklendi. **Atılgan** parayı görür görmez genişliyor; **plancı** kapalı form modelin takvimine uyuyor (15, 29 ve 43. günler).

| Strateji | İşletme neti | Ağırlanan | Servis oranı | Son kasa |
|---|---|---|---|---|
| Atılgan | −60.803 | 297 | %15 | −64.158 |
| Plancı | +47.052 | 2.396 | %93 | **−2.810** |

Atılgan kendini yiyor: on iki masaya çıkıp ne stoklayabiliyor ne kadrolayabiliyor. Ama **plancı** belirleyici oldu. En yüksek işletme netini üretiyor, en çok müşteriyi ağırlıyor, servis oranı %93 — ve yine de zararla bitiriyordu.

Yani genişleme kapıları fazla sıkı değildi. **En iyi işleyen strateji bile kirayı çıkaramıyordu.**

Sebep ölçüldü: kapalı form model aynı takvimde 165.870 sikke ciro varsayıyor, simülasyon 107.949 üretiyor. **Gerçekleşme oranı %65.** Sabrı biten müşteri, tükenen stok, dolan masa. Kiralar modelin gerçekleşmeyen cirosundan çözüldüğü için yaklaşık %35 fazlaydı.

**Düzeltme:** model artık ciroyu gerçekleşme oranıyla çarpıyor. Tablo oyuncunun gerçekten aldığını gösteriyor, marjlar gerçek oluyor, kiralar doğru çözülüyor. C# tarafı aynı katsayıyı uyguluyor ve altın tablo testi ikisini bağlı tutuyor.

`solve.py` yeniden çalıştırıldı ve sıfır cezayla yeni bir set buldu:

| | Eski | Yeni |
|---|---|---|
| Kira, 4 → 14 masa | 1.950 → 10.450 | **650 → 4.000** |
| Genişleme bedeli | 3.250 / 5.850 / 10.400 | **2.500 / 4.500 / 8.000** |
| Kapasite (aşçı/garson/bulaşıkçı/kasiyer) | 28/25/46/66 | 30/26/48/70 |
| Patron iş gücü | 1,4 | 1,3 |

### Sonuç: oyun çalışıyor

| Strateji | Son kasa | İtibar | Masa | Servis oranı | İlk borç |
|---|---|---|---|---|---|
| Pasif | −5.067 | 0 | 4 | %2 | 42. gün |
| Sadece hal | −1.797 | 13,2 | 4 | %76 | 53. gün |
| **Makul** | **+27.916** | 88,3 | 7,3 | %92 | — |
| Genişlemeyen | +11.719 | 64,3 | 4 | %87 | — |
| Plancı | +26.707 | 85,1 | 13,2 | %93 | 42. gün |
| Atılgan | −64.158 | 0 | 14 | %15 | 7. gün |
| Yüksek fiyat | +7.918 | 0,1 | 4 | %91 | — |
| Fazla kadro | −7.737 | 2,5 | 4 | %71 | 44. gün |

**Bütün tasarım niyetleri doğrulandı:**

- **Büyümek ödüllendiriyor.** Genişlemeyen 11.719, genişleyen 27.916. Yaklaşık 2,4 kat. Doküman "üç kat, on bir kat değil" diyordu; bandın içinde.
- **İhmalin bedeli var.** Pasif oyuncu 42. günde batıyor.
- **Pervasız büyüme cezalandırılıyor.** Atılgan 7. günde borca düşüyor.
- **Yüksek fiyat kazandırmıyor.** 7.918 ile hayatta kalıyor ama iyi oyunun dörtte birini kazanıyor ve itibarı sıfır. Küçük ve pahalı bir dükkân meşru ama zayıf bir iş; tasarım niyeti "kazanan strateji olmasın"dı, o tutuyor.
- **Fazla kadro batırıyor.**

### Kalan tek uyarı ve sebebi — KAPANDI

Araç bir uyarı veriyordu: makul oyuncuda para 5,5. haftada sorun olmaktan çıkıyor, hedef sekizinci haftadan önce olmaması.

**Bu bir denge sorunu değil, eksik içerik.** [12-ekonomi.md](12-ekonomi.md) §7 üç önlem sayıyor ve ikincisi son kademe ekipmanların 8.000-12.000 sikke olması. Ekipman henüz yazılmamıştı, yani oyuncunun biriktireceği bir şey yoktu.

**10 Eylül 2026 akşamı kapandı.** Ekipman sistemi yazıldı, [27-zaman-modeli.md](27-zaman-modeli.md) Karar D uygulandı ve ekonomi yeniden dengelendi. Uyarı artık hiçbir stratejide tetiklenmiyor. Ayrıntı, bu turda ölçümün yakaladığı üç hata dahil: [32-ekipman-ve-yeniden-denge.md](32-ekipman-ve-yeniden-denge.md).

Bu bölümün altındaki sayilar kapanmadan ÖNCEKI koşudan; güncel tablo docs/32'de.


---

## 6. Test kapsamı

| Küme | Adet | Ne koruyor |
|---|---|---|
| Aritmetik | 12 | Yuvarlama kuralı, taşma, üsleme hassasiyeti |
| Rastgelelik | 12 | Bağımsız Python uygulamasıyla birebir eşleşme, akış bağımsızlığı |
| Kayan nokta yasağı | 4 | Çekirdekte tek bir `float` yok, Unity ve JSON referansı yok |
| İçerik | 13 | Ondalık reddi, kimlik kuralı, çapraz referans |
| Altın tablo | 7 | C# çekirdek Python modeliyle eşleşiyor |
| Simülasyon | 18 | Gün akışı, determinizm, kare bağımsızlığı, kültür |
| Denge kuralları | 22 | `model.py --check` |

**Çapraz doğrulama ilkesi.** Bir testin beklenen değerini test ettiği koddan alması hiçbir şey kanıtlamaz. Rastgelelik testi `tools/balance/rng_reference.py` içindeki bağımsız Python uygulamasını, haftalık tablo testi `model.py` çıktısını tutturuyor.

---

## 7. Çalıştırma

```
dotnet test                                    88 test
dotnet run --project src/Lokanta.Harness       denge aracı
dotnet run --project src/Lokanta.Harness -- --seeds 20 --csv out.csv
dotnet run --project src/Lokanta.Harness -- --solve      kira arayicisi

python tools/balance/model.py --check          22 denge kuralı
python tools/balance/timing.py --check         46 zaman modeli kuralı
python tools/content/gen_dishes.py             yemek ve malzeme
python tools/content/gen_archetypes.py         arketipler
python tools/balance/export.py                 content/ ve altın veri
python tools/balance/render.py                 tabloları dokümanlara yaz

.\tools\unity\run.ps1 -Method Lokanta.EditorTools.ProjectSetup.ApplyAll
```

`run.ps1` neden var: Unity toplu kipte **derleme hatası olsa bile sıfır döndürüyor.** 10 Eylül'de editör betiği dört API hatası verdi, `-executeMethod` hiç çalışmadı ve çıkış kodu yine de sıfırdı. Bu betik günlüğü okuyup gerçek sonucu döndürüyor.

---

## 8. Sıradaki iş

| Sıra | İş | Neden |
|---|---|---|
| 1 | Zirve kararının **uygulanması** ([28-zirve-karari.md](28-zirve-karari.md)) | Karar verildi: dilim payları değil dilim **süreleri** mutfağa göre değişiyor. Türk mutfağının %60 öğle payı olduğu gibi kalıyor, öğle dilimi günün %48'ini kaplıyor. Günlük kayıp fast food %3,15, Türk %5,08 |
| 2 | Parametreleri simülasyondan aratmak | §5'teki iki denge sorunu |
| 3 | Hal aşaması | Malzeme peşin ödenmeli, bozulma ve stok devreye girmeli |
| 4 | Komut günlüğü ve kayıt | [23-cekirdek-sozlesmesi.md](23-cekirdek-sozlesmesi.md) §7 |
| 5 | Kalan şemalar | Ekipman, yükseltme, düzenli müşteri, mutfaklar |
| 6 | Unity görünüm katmanı | Simülasyonu ekranda göstermek |
