# Değerlendirme 2: Teknik Mimari

**Bakış açısı:** Birden çok Unity mobil oyunu çıkarmış, en az bir Steam portu yönetmiş baş mühendis
**Sorumlu kalemler:** B3, B4, B5, B6, B7, A11
**Tarih:** 9 Eylül 2026

---

## Genel değerlendirme

Plan doğru yerden başlıyor: derleyicinin zorladığı çekirdek sınırı, Faz 0 olarak konsolda denge aracı ve "manuel kayıt yok" gibi kararlar tek kişilik bir projede en çok geri dönüşü olan kararlar. Ancak planın taşıyıcı iddiası olan determinizm (04 §Çekirdeğin dış yüzü; 15 §Tohum) henüz bir tasarım değil, bir dilek: `Tick(deltaTime)` imzası, RNG'nin nerede yaşadığı, kayan noktalı durum ve tr-TR kültürü yazılmamış ve bunların her biri altıncı ayda kayıt sistemiyle denge aracını birlikte geçersiz kılar. Veri şemaları dört imza mekaniğinin üçünü ve personel kapasite modelini ifade edemiyor. Render tarafında GPU Resident Drawer bu sahne için yanlış araç. Aşağıdaki beş sorun şu an ucuz; Faz 0'dan önce kapatılmalı.

## En güçlü üç yön

1. **Derleyicinin zorladığı sınır ve Faz 0.** `Lokanta.Core` asmdef'inde Unity referanslarının kapatılması (04 §Derleyicinin katmanları zorlaması) ve Unity'ye dokunmadan önce denge aracının yazılması (04 §Headless denge aracı), bu türün en sık çürüme biçimi olan arayüz-simülasyon karışmasını baştan engelliyor.
2. **Kayıt ilkeleri.** Manuel kayıt yok, atomik yazma, `.bak`, sağlama, silinmeyen göç fonksiyonları, birleştirme yerine oyuncuya sorma, şifreleme yok (15 §Bozulmaya karşı koruma, §Sürüm göçü, §Bulut kaydı; 19 B6). Hepsi doğru ve gerekçeli.
3. **İçerik ve ölçüm disiplini.** Değişmez `id`, yerelleştirme anahtarı, açılışta reddeden doğrulayıcı (13 §İlke, §Doğrulama); profil yalnız cihazda, bütçe aşımı özellikten önce çözülür (19 B5 §Ölçüm); basit personel yapay zekâsı (14 §Personel yapay zekâsı) determinizmin de dostudur.

## En riskli beş sorun

### 1. Determinizm tasarlanmamış; kayıt ve denge aracı buna yaslanıyor

**Sorun.** `sim.Tick(deltaTime)` (04 §Çekirdeğin dış yüzü) Unity'nin değişken kare süresini çekirdeğe sokuyor. Aynı tohumla iki oynayış farklı `deltaTime` dizisi görür; `sabır -= dt` gibi her birikim ayrışır ve "durum artı tohum günü birebir üretir" (15 §Tohum) iddiası düşer. Dört yazılmamış tuzak daha var: (a) RNG belirsiz; Unity'nin Mono/IL2CPP BCL'i ile aracın .NET 8'inin `System.Random`'ının aynı diziyi vereceğine güvenilemez, "tüketilen adım sayısı" ile ileri sarmak da iç durumu kaydetmenin kötü bir vekili. (b) Üç farklı kod üreteci (IL2CPP ARM64, Mono, RyuJIT) kayan noktada aynı sonucu garanti etmez; `Math.Sin/Exp` libm'e bağlıdır. (c) `Dictionary`/`HashSet` sırası ve `List.Sort`'un kararsızlığı. (d) tr-TR kültürü: `"izgara".ToUpper()` Türkçe kültürde "İZGARA", `double.Parse("0.15")` farklı sonuç verir; geliştirme makinesi Türkçe.

**Neden önemli.** Servis ortası kayıt, hata ayıklama tekrarı ve Faz 0'ın çıktıları aynı varsayıma bağlı. Ayrışma altıncı ayda "kayıt yükleyince farklı gün" hatası olarak çıkar ve sebebi bulunamaz.

**Çözüm.** Sabit adım: parametresiz `Tick()`, 100 ms'lik adım; Uygulama katmanı gerçek zamanı biriktirip adım çağırır (kare başına üst sınırla), kayıt `tickIndex` tutar. Durum tamamen tamsayı: para zaten tamsayı, süreler milisaniye, memnuniyet ve itibar 1/100 puan, miktarlar gram (13 §Karar bekleyen 1'in cevabı). 12 §5'teki formüllerin hepsi toplama-çarpma-bölme; üstel veya trigonometrik yok, sabit noktaya geçiş bedava, yuvarlama tek yerde tanımlanır. Kendi RNG'si Core'da: xoshiro128** veya PCG32, durumu serileştirilir, alt sisteme ve güne göre ayrı akış (`SplitMix64(kampanyaTohumu, gün, akışId)`); böylece bir sisteme eklenen çağrı diğerini kaydırmaz ve tek gün 59 gün sarılmadan tekrar oynatılır. `string.GetHashCode` yasak; sıralama `id` ile kırılır; Core csproj'unda CA1304/1305/1307/1309 hata seviyesinde; açılışta `CultureInfo.DefaultThreadCurrentCulture = InvariantCulture`. CI'da aynı tohumu .NET 8 ve Android IL2CPP'de koşturup gün sonu durum hash'ini karşılaştıran test; hash kayıt zarfına da yazılır.

### 2. Çekirdek sınırının üç dikişi boş: serileştirme, olaylar, kompozisyon

**Sorun.** Grafik (04 §Derleyicinin katmanları zorlaması) JSON kütüphanesinin nerede yaşadığını söylemiyor; Core'da `JsonUtility` yok, `System.Text.Json` Unity'de yerleşik değil. "Çekirdek olay yayar" (04 §Olay akışı tek yönlü) için mekanizma yok; sınıf tipli `event Action<T>` her olayda çöp üretir, abonelik ömrü hataları doğurur. `Platform.Mobile → Ports` olan bir birim kendini `App`'e bağlayamaz; kompozisyon kökü tanımsız. `Ports → Core` bağımlılığı gereksiz. Aracın Core kaynaklarını nasıl derleyeceği yazılmamış: .NET 8 C# 12 kabul eder, Unity 6 C# 9'da kalır; kod bir gün Unity'de derlenmez.

**Çözüm.** Core: tipler, kurallar, DTO'lar (`SimEvent`, komutlar, `DayReport`, anlık durum), sıfır bağımlılık. Content: JSON→Core yükleyici ve doğrulayıcı, Newtonsoft (`com.unity.nuget.newtonsoft-json`, araçta aynı NuGet). Save: zarf ve göçler, Newtonsoft. App ve araç her ikisini referans alır. Olaylar: `readonly struct SimEvent { kind, tick, id1, id2, value }` bir listede birikir, App her adım sonrası `DrainEvents(buffer)` ile boşaltır; sıfır ayırma, tekrar için loglanabilir. İçerik id'leri JSON'da string, yüklemede `DishId(int)` struct'ına çevrilir; kayıt string yazar. Ports yalnız `byte[]`, string ve kendi küçük DTO'larını kullanır, Core'a bağlanmaz. `Lokanta.Boot.Mobile`/`Boot.Steam` (App + Platform.* referanslı, asmdef `includePlatforms` ile filtrelenmiş) kompozisyon kökü olur. Araç tarafında `Lokanta.Core.csproj` aynı `.cs` dosyalarını `<Compile Include>` ile alır; `netstandard2.1` ve `LangVersion 9` sabitlenir.

### 3. Şemalar imza mekaniklerini ve kapasite modelini ifade edemiyor

**Sorun.** Ücretsiz mutfağın imza mekaniği kombo (07 §Fast food; 12 §3) için hiçbir şema yok. `signatureMechanic: "veresiye"` tek string (13 §cuisines.json); veresiyenin geri ödeme olasılığı, gecikmesi, tavanı (07 §Türk mutfağı, "kimin ödeyeceği belirsiz"), Japon çorba suyunun porsiyon maliyeti ve aralığı, İtalyan kurs ve masa süresi yok. "Mutfaklar sadece veri olarak farklılaşır" (07 §Riski) iddiası imza mekanikleri için yanlış; her biri koddur ve parametre bloğu yok. 14 §Kapasite modeli'nin 16/20/26/34 kapasitesi, %70/%45 azalan verim, %50 rol dışı, patron 12, kademe başına azami personel, 30 puan/seviye, zam talebi, tazminat: hiçbiri `staff-roles`, `expansions` veya `economy.json`'da değil. Huy koşulları ("yoğun dilimlerde", "son çeyrekte") düz `effects` sayısıyla yazılamaz; anahtarlar serbest string ve doğrulayıcı (13 §Doğrulama) denetlemiyor. `hourSplit` (cuisines) ile `arrivalWeights` (archetypes) aynı şeyin iki kaynağı; 12 §5.6 mutfak bazlı. Günün yemeği indirimi ve çay maliyeti (12 §3) yok. `spoilDays` gün tanecikli; 15'teki "tazelik sayaçları" ve Japon taze malzemesi gün içi eğri ister.

**Çözüm.** `combos.json`; `cuisines.signature: { kind, params }` ayrımlı birleşim, her `kind` için Core'da tipli parametre sınıfı; `staff-roles.dailyCapacity`, `expansions.maxStaff`, `economy.staffing { stationDiminishing, offRoleEfficiency, ownerCapacity, xpPerLevel, raiseDemand, severanceWeeks }`; `effects` için `TraitEffect` enum'u ve `condition` alanı; `conflictsWith` simetri denetimi; `hourSplit` tek kaynağa iner. `id` kuralına "silinmez, `deprecated: true` ile emekli olur" eklenir. Her şema için JSON Schema dosyası (yapay zekâyla içerik üretirken editörde anında hata) ve doğrulayıcı CI'da `dotnet test`. Açılışta reddetme yerel içerik için doğru, indirilen paket için yanlış: paket karantinaya alınır, oyun açılır.

### 4. Servis ortası kayıt: on saniyede tam anlık görüntü, ana iş parçacığında

**Sorun.** 15 §Ne kaydediliyor hem "durum artı tohum yeter" diyor hem masaların ve siparişlerin anlık durumunu istiyor. Anlık görüntü yolu seçilirse her 10 s'de (15 §Ne zaman) 200 KB'a kadar JSON üretmek, gzip'lemek, sağlama almak ve fsync yapmak ana iş parçacığında 10-30 ms takılma demektir; flash aşınması sorun değil, takılma sorundur. Yazma sırası (19 B6 §Dosya düzeni) `Flush(true)` söylemiyor; fsync'siz rename güç kesintisinde sıfır uzunluklu "geçerli" dosya bırakır. Zarf (15 §Sürüm göçü) `savedAtUtc`, cihaz adı, oyun süresi, `tickIndex`, içerik sürümü ve durum hash'i taşımıyor; çakışma diyaloğu (15 §Bulut kaydı) bunlarla kurulur. iCloud anahtar-değer deposu toplam 1 MB; 4 yuva × (`.save` + `.bak`) sığmayabilir.

**Çözüm.** Servis ortası kayıt = servis başı anlık görüntüsü + komut günlüğü + `tickIndex`. Gün içinde istasyon değişmiyor (14 §İstasyon ataması), müdahale günde en fazla 4 (13 `interventionsPerDay`); günlük birkaç yüz bayt. Yüklemede tekrar oynatılır, hedef tick'teki hash zarftakiyle karşılaştırılır; uyuşmazsa tam anlık görüntüye düşülür (1. sorun kanıtlanana kadar ikisi de yazılır). Ana iş parçacığı yalnız byte dizisini üretir; gzip + CRC32 + `Flush(true)` + `File.Replace` bir `Task`'ta. Arka plana atılmada `OnApplicationPause(true)` içinde eşzamanlı ve küçük yazım. Çakışmada kaybeden kayıt `slot_N.conflict` olarak saklanır. iOS'ta CloudKit veya iCloud Documents, Android'de Play Games Saved Games (3 MB/anlık görüntü); bulut yuva başına tek gzip blobu taşır. Yuva açılışında `IStoreFront` sahiplik denetimi; sahiplik kayıt dosyasında tutulmaz.

### 5. GPU Resident Drawer yanlış araç; pişirilmiş ışık yerleşim moduyla çelişiyor; bütçeler ölçülebilir değil

**Sorun.** GPU Resident Drawer (19 B4 §Ayarlar) URP'de Forward+ ister, `BatchRendererGroup` üstünde çalışır ve OpenGL ES'te yoktur; yani yedek GLES 3.1 cihazlarda, tam da 30 fps garantisi verilen en düşük cihazlarda, devre dışı kalır. `SkinnedMeshRenderer` (müşteri, personel) ve UI kapsam dışı. Faydası binlerce örnekte görülür; 100 çizim çağrısında ölçülemez, karşılığında GPU belleği ve Forward+ kümeleme maliyeti alır. "Mobilya pişirilmiş" (19 §Işıklandırma) ama B7'de Layout haritası ve 15'te "yerleşim düzeni" var: masa taşınıyorsa ışığı pişirilemez; genişleme kademesi × mutfak ortamı kadar lightmap seti 200 MB'ı yer. Bütçeler sayım cinsinden, takılmanın sebebi milisaniye ve çöp: kare başına GC, Canvas yeniden inşası (her kare değişen TMP sayıları), UI overdraw yok; "30 dakika ısınma yok" hedefi var (19 B5) ama aracı (Adaptive Performance) yok; bellek ölçüsü (PSS mi Unity reserved mi) belirsiz. Linear + MSAA ile 60 fps orta cihazı ısıtır.

**Çözüm.** GRD kapalı; Forward, SRP Batcher + statik batching + sandalye/tabak malzemesinde GPU instancing yeter; profil render iş parçacığında darboğaz gösterirse yeniden değerlendirilir. HDR kapalı, MSAA 4x (tile GPU'da ucuz), Render Scale ayarı. Yalnız duvar-zemin pişirilir, mobilya prob + tek yönlü ışık. Bütçeler en düşük cihazda ms cinsinden: ana iş parçacığı ≤ 10 ms, render ≤ 6 ms, GPU ≤ 14 ms; serviste 0 B/kare GC; dinamik metin ayrı Canvas'ta; bellek `dumpsys meminfo` PSS ≤ 500 MB. 60 fps yalnız üst kademede, `Application.targetFrameRate` + Adaptive Performance (ADPF). ARM64-only derleme. Test matrisine bir GLES 3.1 cihaz zorunlu. Addressables ilk sürümde yerel gruplar; ölçülen boyut 200 MB'ı aşmadıkça uzak CDN yükü (hosting, katalog sürümü, indirme hatası → iade) alınmaz (19 §Karar bekleyen 4).

## Kalem kararları

| Kalem | Karar | Gerekçe |
|---|---|---|
| B3 Veri şemaları | DÜZELT | Kombo, veresiye, çorba suyu, kurs parametreleri ve 14'ün kapasite modeli ifade edilemiyor; `hourSplit` çift kaynak; huy koşulları yok; birimler tamsayıya (gram, ms, 1/100 puan) çevrilmeli; JSON Schema ve CI doğrulaması eklenmeli (3. sorun). `nameKey` yaklaşımı doğru. |
| B4 Unity sürümü ve render hattı | DÜZELT | 6.3 LTS, URP, Linear, Vulkan + GLES 3.1, Metal, ASTC, SRP Batcher onay. Linear, GLES 3.0+ tabanında (Android 10) güvenli. GRD kapatılmalı, Forward kalmalı, HDR kapalı, pişirilmiş ışık taşınabilir mobilyayı kapsamamalı (5. sorun). |
| B5 Performans hedefleri | DÜZELT | Sayım bütçeleri ms/iş parçacığı bütçesine çevrilmeli; GC, Canvas, overdraw, PSS tanımı, Adaptive Performance eklenmeli; 60 fps üst kademeye sınırlanmalı; uzak Addressables ilk sürümde ertelenmeli (5. sorun). |
| B6 Kayıt dosyası formatı | ONAYLA | JSON + gzip + sağlama + şifresiz + `.bak` doğru. Şartlar: sağlama algoritması yazılsın (CRC32 veya xxHash32), `Flush(true)` + `File.Replace`, zarfa meta alanları, Newtonsoft, göçler tipsiz JSON ağacı üstünde, her sürüm için test fikstürü. |
| B7 Girdi eylem haritası | DÜZELT | Desktop'ta Esc hem Pause hem Cancel, sol tık hem Select hem Intervene (19 B7 §Eylemler); harita önceliği tanımsız. 04 §Girdi katmanı Desktop'a oyun kolunu dahil ediyor, 19 B7 "ilk sürümde yok" diyor; çelişki. Oyun kolu şeması `.inputactions`'a şimdi eklenmeli: Steam Deck "Verified" tam kol desteği ister ve 16:10 (1280×800), "16:9 ve daha geniş" (04 §Ekran oranı) varsayımını kırar. Hover karşılığı `Inspect`/uzun basma yok. `IInputSource` port olmamalı: şema derleme hedefi değil çalışma zamanı yeteneğidir (Deck'te dokunmatik, Android'de fare) ve Input System zaten şema değiştirir. "Hassas nişan yok" doğru; "sürükle"ye "seç-onayla" eşdeğeri eklenmeli ki kol ve dokunmatik aynı akışı izlesin. |
| A11 Kayıt sistemi tasarımı | DÜZELT | Yuva, kilit, atomik yazma, göç, çakışmada sorma onay. Servis ortası kayıt komut günlüğü + tekrar oynatma olmalı, yazma arka iş parçacığına alınmalı, zarf meta alanları, kaybeden kayıt saklanmalı, bulut arka ucu boyut limitleriyle seçilmeli, sahiplik mağazadan (4. sorun). 15 §Karar bekleyen 4: sormaya devam, yeni olan önseçili. |

## Cevapsız sorular

1. Hangi JSON kütüphanesi, hangi derleme biriminde? Araç hangi .NET sürümünde, Core hangi C# sürümüne sabitleniyor? 04 §Derleyicinin katmanları zorlaması bunu söylemiyor.
2. `IAdProvider` "ödüllü reklam" (04 §Portlar) neyi ödüllendiriyor? Okuduğum dokümanlarda tanım yok; "güç satılmıyor, sayaç yok" (07 §Neden bu yapı doğru) ile çelişmeyen bir kullanım yoksa port kaldırılmalı, 04 §Aşırı mühendislik uyarısı'nın gereği. Varsa ATT/UMP rıza akışı da bir port ister.
3. Mobilya oyuncu tarafından taşınıyor mu (19 B7 Layout; 15 "yerleşim düzeni")? Cevap pişirilmiş ışığı ve lightmap bütçesini belirler.
4. Servis yalnız gerçek zamanlı 120 s mi, hız düğmesi var mı? Duraklat haritada var, hız yok; sabit adım tasarımını etkiler.
5. Yerelleştirmenin tek kaynağı `localization/tr.json` mi (13 §Dosya düzeni) yoksa Unity Localization tabloları mı (19 §Paketler)? JSON kaynak + editörde içe aktarma öneriyorum; Türkçe ek uyumu için Smart String gerekecek.
6. `hourSplit` mi `arrivalWeights` mi kanonik? 12 §5.6 mutfak bazlı yazıyor.
7. İndirilen mutfak paketleri kural JSON'u da taşıyor mu, yalnız sanat mı? Cevap "açılışta reddet" politikasını ve kayıt-paket sürüm uyumunu belirler.
8. Tarif birimi (13 §Karar bekleyen 1): tamsayı gram veya porsiyon önerisi kabul mü? Sabit nokta kararı buna bağlı.
9. `IAnalytics` 04 §Portlar'da var, 06 B2 özetinde yok; hangi liste geçerli?
