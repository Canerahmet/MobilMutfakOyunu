# Katmanlı Mimari

**Son güncelleme:** 9 Eylül 2026
**Amaç:** Oyunu, ileride değişiklik yapmanın ucuz olduğu katmanlara ayırmak. Özellikle mobilden Steam'e geçişin küçük ve öngörülebilir bir iş olması.

Çalışma adı olarak `Lokanta` kullanıldı. Gerçek isim sonra belirlenecek.

---

## Temel kural

**Simülasyon Unity'yi bilmez.**

Oyunun bütün kuralları, ekonomisi, müşterileri, personeli ve batma mantığı saf C# ile yazılır. İçinde tek bir `UnityEngine` referansı olmaz.

Bu tek kural aşağıdakilerin hepsini bedava getirir:

- Ekonomiyi Unity açmadan, konsolda binlerce gün simüle ederek dengeleyebilirsin.
- Kuralları birim testleriyle doğrulayabilirsin.
- Steam sürümü, arayüz ve platform katmanını değiştirir; kuralların tek satırına dokunmaz.
- İleride motoru değiştirmek istersen oyunun beyni elinde kalır.

Bu kuralı kağıt üstünde tutmak işe yaramaz. Aşağıda derleyicinin bunu zorlaması anlatılıyor.

---

## Katmanlar

Bağımlılık her zaman aşağı doğrudur. Üst katman alt katmanı bilir, alt katman üstü asla bilmez.

| Katman | Ne yapar | Unity referansı |
|---|---|---|
| **Sunum** | 3B sahne, karakter animasyonu, kamera, ses, efekt | Var |
| **Arayüz** | Hal, tezgâh, servis göstergesi, hesap ekranları | Var |
| **Uygulama** | Gün akışını yürütür, kayıt yükler, komutları çekirdeğe iletir | Var |
| **Çekirdek** | Bütün oyun kuralları ve simülasyon | **Yok** |
| **İçerik** | Tarifler, malzemeler, ekipman, personel, müşteri tipleri | Yok |
| **Portlar** | Platform yeteneklerinin arayüz tanımları | Yok |
| **Platform** | Portların mobil ve Steam gerçeklemeleri | Var |

---

## Derleyicinin katmanları zorlaması

Unity'de bunu sağlayan mekanizma **Assembly Definition** dosyalarıdır. Her katman kendi derleme birimidir ve sadece izin verilen katmanlara referans verebilir.

```
Lokanta.Core            → hiçbir şeye referans vermez, Unity API'si kapalı
Lokanta.Content         → Core
Lokanta.Ports           → Core
Lokanta.App             → Core, Content, Ports
Lokanta.View            → App, Core
Lokanta.UI              → App, Core
Lokanta.Platform.Mobile → Ports        (sadece mobil derlemeye dahil)
Lokanta.Platform.Steam  → Ports        (sadece Steam derlemeye dahil)
Lokanta.Core.Tests      → Core
```

`Lokanta.Core` derleme birimini oluştururken **Unity referanslarını kapatmak** kritik adımdır. Bunu yaptığında, çekirdeğe yanlışlıkla bir `GameObject` sızdırmaya çalıştığın an proje derlenmez. Disiplin senden değil derleyiciden gelir.

Bu tek ayar, katmanlı mimarinin kağıt üstünde kalmasını engelleyen şeydir.

---

## Portlar: mobil ile Steam arasındaki tek fark

Platforma göre değişen her şey bir arayüzün arkasına konur. Oyun kodu sadece arayüzü çağırır, gerçeklemeyi bilmez.

| Port | Mobil gerçeklemesi | Steam gerçeklemesi |
|---|---|---|
| `IInputSource` | Dokunmatik, sürükle bırak | Fare, klavye, oyun kolu |
| `ISaveStore` | Cihaz belleği | Yerel dosya |
| `ICloudSave` | iCloud, Google Play | Steam Cloud |
| `IStoreFront` | Uygulama içi satın alma | Yok, oyun peşin satılır |
| `IAdProvider` | Ödüllü reklam | Boş gerçekleme, hiçbir şey yapmaz |
| `IAchievements` | Game Center, Play Games | Steam Achievements |
| `IAnalytics` | Mobil analitik | İsteğe bağlı |

**Neden bu kadar önemli:** Reklam ve satın alma çağrıları oyun kodunun içine dağılırsa Steam sürümü kâbusa döner. Portun arkasındaysa, Steam derlemesi sadece boş bir reklam gerçeklemesi verir ve konu kapanır.

Gelir modeli kararı da bu yüzden mimariyi etkilemiyor. Mobilde ücretsiz artı kilit, Steam'de peşin satış olabilir. İkisi de aynı çekirdeğin üstünde çalışır.

---

## Girdi katmanı: baştan iki şemalı

Steam hedefi olduğu için girdi baştan soyutlanmalı. Unity'nin Input System paketi bunu **control scheme** kavramıyla zaten destekliyor. Baştan iki şema tanımla:

- **Touch:** sürükle bırak, tek dokunuş
- **Desktop:** fare, klavye kısayolları, oyun kolu

Oyun kodu `IInputSource` üzerinden niyeti okur. Örneğin "şu masaya şu müşteriyi yerleştir" komutu, parmakla mı fareyle mi geldiğini bilmez.

Sonradan eklemek pahalıdır çünkü tüm etkileşim kodunu yeniden yazmak gerekir.

---

## Ekran oranı

Mobil yatay yaklaşık 19.5:9, Steam 16:9 ve daha geniş. Arayüz en dar güvenli alana göre tasarlanır, geniş ekranda nefes alır.

Pratik kural: bilgi yoğun paneller kenarlara sabitlenir, oyun alanı ortada esner. Sabit piksel konumu kullanma.

---

## İçerik veri olarak tutulur, kod olarak değil

Tarifler, malzemeler, ekipman fiyatları, personel arketipleri ve yükseltme maliyetleri **veri dosyalarında** yaşar.

Öneri: kaynak gerçeklik JSON dosyaları olsun, Unity tarafında bunları okuyan bir yükleyici bulunsun.

**Neden ScriptableObject değil:** ScriptableObject Unity editöründe çok rahattır ama seni Unity'ye bağlar. Konsolda çalışan denge aracın onu okuyamaz. JSON ise hem oyun hem denge aracı tarafından okunur. İstersen editör kolaylığı için üstüne ince bir sarmalayıcı yazarsın.

Denge değişikliği yapmak için kod derlemek zorunda kalmamak, tek kişilik bir projede çok zaman kazandırır.

---

## Olay akışı tek yönlü

Çekirdek olay yayar, sunum dinler. Sunum çekirdeğin durumunu **asla doğrudan değiştirmez**, sadece komut gönderir.

```
Oyuncu dokunur
  → Arayüz komut üretir
    → Uygulama komutu çekirdeğe iletir
      → Çekirdek durumu değiştirir ve olay yayar
        → Sunum ve arayüz olayı dinleyip kendini günceller
```

Bu kural olmazsa arayüz ile simülasyon zamanla birbirine karışır ve hata ayıklamak imkânsızlaşır. Yönetim oyunlarında en sık görülen çürüme budur.

---

## Çekirdeğin dış yüzü

Kabaca şuna benzeyen bir yüzey hedefleniyor. Detay değişir, şekil değişmez.

```csharp
var day = sim.BeginDay();

sim.Market.Buy(ingredientId, quantity);
sim.Menu.Set(dishId, price);
sim.Staff.Assign(staffId, Station.Kitchen);

sim.OpenService();
sim.Tick(deltaTime);          // deterministik
sim.Intervene(tableId, InterventionKind.Apology);

DayReport report = sim.CloseDay();
```

`Tick` deterministik olmalı. Aynı başlangıç durumu ve aynı rastgelelik tohumu, aynı sonucu vermeli.

**Bunun getirileri:** kayıt dosyası durum artı tohumdan ibaret olur ve platformlar arasında taşınabilir. Hata ayıklarken bir günü tekrar oynatabilirsin. Denge aracı güvenilir sonuç üretir.

---

## Headless denge aracı

`Lokanta.Core` referans veren küçük bir konsol uygulaması. Farklı oyuncu stratejileriyle yüzlerce oyun simüle eder ve sonucu CSV olarak yazar.

Ölçülecekler:

- Kaç oyuncu kaçıncı günde batıyor
- Ne zaman ekonomi önemsizleşiyor, yani para birikip anlamını yitiriyor
- Fiyatı sürekli yüksek tutan bir oyuncu kazanıyor mu
- Hiç müdahale etmeyen bir oyuncu ne kadar dayanıyor

Bu araç, araştırmadaki iki büyük ölüm sebebini erkenden yakalar: batmanın hüsran vermesi ve ekonominin kolaylaşıp anlamsızlaşması.

**Faz 0 budur.** Unity'ye dokunmadan önce bu yazılır.

---

## Sanat da katmanlı olmalı

Görsel varlıklar da değiştirilebilir kalmalı.

- Her nesne kendi prefab'ında, ortak eksen ve ölçek kuralına uyar.
- Dikey dilim **ilkel kutularla** yapılır. Masa bir küptür, müşteri bir kapsüldür.
- Gerçek model geldiğinde prefab içeriği değişir, oyun kodu değişmez.

Bu, tek kişilik bir projede sanatın oyun tasarımını rehin almasını engeller. Araştırmadaki en net ders buydu: sanat kurtarıcı değil çarpandır. Önce çarpılacak sayının doğru olması gerekir.

---

## Aşırı mühendislik uyarısı

Katmanlama, gerçekten önemli olan dikişlerde yapılır. Her sınıfa arayüz yazmak tek kişilik bir projeyi boğar.

**Soyutlanacak dikişler:**

1. Çekirdek ile Unity arasındaki sınır
2. Platform portları
3. İçerik verisi ile kod arasındaki sınır

**Soyutlanmayacaklar:** geri kalan her şey. Bir masa sınıfının arayüzüne ihtiyacın yok. İhtiyaç doğduğunda çıkarırsın.

---

## İlk üç adım

1. **Çekirdek ve denge aracı.** Unity yok. Bir günün matematiği konsolda çalışsın, otuz günlük simülasyon dengeli çıksın.
2. **Unity iskeleti.** Derleme birimleri kurulur, portlar tanımlanır, mobil gerçeklemeleri yazılır. Sahne ilkel kutulardan oluşur.
3. **Dikey dilim.** Tek restoran, altı yemek, üç personel, on gün. Oynanabilir ve test edilebilir.

Steam gerçeklemeleri bu üç adımda yazılmaz. Sadece portlar tanımlı olduğu için, sırası geldiğinde eklenmesi küçük bir iş olur.
