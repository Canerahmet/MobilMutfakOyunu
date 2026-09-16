# Çekirdek Sözleşmesi

**Son güncelleme:** 10 Eylül 2026
**Kütük maddeleri:** B3 veri şemaları, B6 kayıt formatı, A11 kayıt sistemi, ve değerlendirmenin "determinizm tasarlanmamış" bulgusu
**Durum:** Parti B. Bu dosya bağlayıcıdır. Faz 0 kodu buradaki kurallara uymak zorunda.
**Değerlendirme kaynağı:** [review/02-technical-architecture.md](review/02-technical-architecture.md)

---

## Neden bu dosya var

Mimari değerlendirmesi şunu buldu: [04-architecture.md](04-architecture.md) determinizmi **istiyor** ama **tasarlamıyor**. `Tick(deltaTime)` değişken kare süresini çekirdeğe sokuyor, rastgelelik kaynağı belirsiz, kayan nokta üç derleyicide üç farklı sonuç verebiliyor, Türkçe kültür ayarı `ToUpper` çağrısını bozuyor.

Kayıt sistemi, denge aracı ve hata ayıklama üçü de "aynı girdi, aynı çıktı" varsayımına yaslanıyor. Bu varsayım kendiliğinden sağlanmaz. Aşağıdaki on kural onu sağlar.

Her kuralın yanında **nasıl doğrulanacağı** yazıyor. Doğrulanamayan kural yoktur.

---

## 1. Determinizm sözleşmesi

### 1.1 Tanım

**Aynı içerik, aynı tohum, aynı komut günlüğü → aynı durum, bayt bayt.** Platform, derleyici, işletim sistemi, kültür ayarı, kare hızı ve gerçek zamanın hiçbiri sonucu değiştiremez.

"Aynı durum" şu demek: bölüm 9'daki durum özeti (hash) eşit.

### 1.2 Sabit adım

```csharp
public sealed class Simulation
{
    public const int TickMs = 100;          // bir tick = 100 ms simülasyon zamanı
    public long TickIndex { get; private set; }

    public void Tick();                      // parametresiz. Gerçek zaman girmez.
    public void Apply(in Command c);         // oyuncu girdisi
    public IReadOnlyList<SimEvent> DrainEvents();
}
```

`Tick()` parametre almaz. Çekirdek gerçek zamanı hiç görmez.

Unity tarafı biriktirir:

```csharp
// Lokanta.App.SimDriver : MonoBehaviour
float _acc;
void Update()
{
    _acc += Time.unscaledDeltaTime * Speed;       // Speed = 1 veya 2, oyuncu seçer
    int n = 0;
    while (_acc >= 0.1f && n < MaxTicksPerFrame)  // MaxTicksPerFrame = 5
    {
        _sim.Tick();
        _acc -= 0.1f;
        n++;
    }
    if (n == MaxTicksPerFrame) _acc = 0f;         // kare düştüyse zaman yavaşlar, sapmaz
}
```

**Kare düşünce ne olur:** simülasyon yavaşlar, bozulmaz. Beş tick tavanı "ölüm sarmalını" engeller. Oyuncu bir saniye fazla bekler, ama kayıt dosyası bozulmaz.

**Hız 2x:** sürücü iki kat tick çağırır. Çekirdek hızdan habersizdir. Bu yüzden 2x oynayan ve 1x oynayan aynı komutları aynı tick'te verirse aynı sonucu alır.

### 1.3 Zaman birimleri

| Süre | Tick | Not |
|---|---|---|
| Bir tick | 1 | 100 ms |
| Bir müşteri servisi | ~1.200 | 120 s, [12-economy.md](12-economy.md) §5.2 |
| Bir servis günü | 4.800 | 8 dakika 1x hızda |
| Altmış günlük sezon | 288.000 | Denge aracı 50 µs/tick ile 15 saniyede biter |

Sabır değerleri ([12-economy.md](12-economy.md) §5.2) saniye cinsinden yazılı; çekirdekte ms olarak tutulur, 8 s = 8.000 ms = 80 tick.

### 1.4 Doğrulama

- **Tekrar oynatma testi:** aynı tohum ve komut günlüğüyle iki kez çalıştır, hash eşit olmalı.
- **Kare bağımsızlık testi:** aynı komutları `MaxTicksPerFrame = 1` ve `= 5` ile çalıştır, hash eşit olmalı.

---

## 2. Tamsayı durum

### 2.1 Kural

`Lokanta.Core` altındaki hiçbir tipte `float`, `double` veya `decimal` bulunmaz. Alan, parametre, dönüş değeri, yerel değişken, sabit: hiçbiri.

Sebep: IL2CPP (Android), Mono (editör) ve RyuJIT (denge aracı, .NET) kayan nokta işlemlerini farklı sırayla ve farklı hassasiyetle yapabilir. `0.1f + 0.2f` üç ortamda üç farklı bit deseni verebilir. Tamsayı toplama her yerde aynıdır.

### 2.2 Birim tablosu

| Büyüklük | Birim | Tip | Örnek |
|---|---|---|---|
| Zaman | milisaniye | `int` (gün içi), `long` (tickIndex) | 8 s sabır = 8000 |
| Para | **santi-sikke**, 1 sikke = 100 birim | `long` | 45 sikke = 4500 |
| Ağırlık | gram | `int` | 120 g köfte harcı |
| Memnuniyet, itibar, moral | **santi-puan**, 0..10000 | `int` | itibar 30 = 3000 |
| Oran, çarpan, yüzde | **baz puan (bp)**, 10000 = 1,0 | `int` | %32 malzeme = 3200; hız +%18 = 11800 |
| Kapasite | müşteri/gün | `int` | garson 25 |
| Patron iş gücü | santi-iş-günü | `int` | 1,4 iş-günü = 140 |
| Sayaçlar | adet | `int` | masa 14 |

Para `long`, çünkü 60 günlük toplam ciro santi-sikke cinsinden 2³¹'i aşabilir (43.425 sikke/hafta × 100 × 9 hafta ≈ 39 milyon, sınır 2,1 milyar; güvenli ama kredi ve yıl sonu puanı çarpımlarında `int` taşar).

### 2.3 Bölme ve yuvarlama

Tek yardımcı sınıf, başka yol yok:

Uygulanmış hâli `src/Lokanta.Core/Fx.cs`:

```csharp
public static class Fx
{
    public const int  One   = 10_000;         // 1,0 bp
    public const int  Micro = 1_000_000;      // 1 iş-günü
    public const long Nano  = 1_000_000_000L; // birikimli çarpanların iç hassasiyeti
    public const int  Coin  = 100;            // 1 sikke = 100 santi-sikke

    /// a * b / c, yarısı sıfırdan uzağa. Ara çarpım checked:
    /// sessiz taşma, yanlış sonuçtan daha kötüdür.
    public static long MulDiv(long a, long b, long c)
    {
        if (c == 0) throw new DivideByZeroException();
        long p; checked { p = a * b; }
        long q = p / c;
        long r = p - q * c;
        if (r == 0) return q;
        long ar = r < 0 ? -r : r;          // Math.Abs değil: long.MinValue atar
        long ac = c < 0 ? -c : c;
        long twice; checked { twice = ar * 2; }
        if (twice >= ac) q += ((p < 0) == (c < 0)) ? 1 : -1;
        return q;
    }

    public static long Bp(long value, int bp) => MulDiv(value, bp, One);
    public static int  CeilDiv(int a, int b);      // kadro hesabı
    public static long CeilDivL(long a, long b);
    public static long PowNano(long baseNano, int exp);   // birikimli zam
    public static long BpToNano(int bp);
}
```

`PowNano` neden var: deneyim zammını baz puanla üslemek sekizinci haftada yaklaşık %0,03 sapma, yani birkaç sikke üretiyordu. Birikimli çarpanlar nano ölçeğinde hesaplanır, sonuç santi-sikkeye tek seferde iner.

- `Math.Round`, `Math.Floor`, `(int)` ile kesme: **yasak.** Sadece `Fx`.
- Banker's rounding (yarıyı çifte yuvarlama) **yasak**; `MulDiv` yarıyı sıfırdan uzağa yuvarlar. Sebep: `Math.Round` varsayılanı banker's, ve iki geliştirici ikisini karıştırır.
- Formül dokümanlarındaki ondalıklar bp'ye çevrilir: `(0,5 + itibar/100)` → `5000 + itibar_santi / 1` yani `5000 + 3000 = 8000 bp = 0,8`.

### Bu kural uygulamada ne yakaladı

10 Eylül 2026, çekirdeğin ilk dilimi yazılırken. C# çekirdeği ile Python modeli sekizinci haftaya kadar aynı sonucu verirken **altıncı haftada ayrıştı**: hafta sonu talebi Python'da 62, C#'ta 63.

Sebep tam olarak bu bölümün yasakladığı şeydi.

| | Hesap | Sonuç |
|---|---|---|
| Ham değer | 10 masa × 4 × 1,25 × 1,25 | **62,5** tam ortada |
| Python `round(62,5)` | Bankacı yuvarlaması, yarıyı çifte götürür | 62 |
| C# `Fx.MulDiv` | Yarısı sıfırdan uzağa | 63 |

Daha kötüsü, Python **tutarlı bile değildi.** Dördüncü haftada ham değer 38,5 olması gerekirken kayan nokta gürültüsü 38,500000000000007 üretiyor ve `round()` bu kez yukarı, 39'a gidiyordu. Yani aynı betik bir satırda bankacı yuvarlaması, bir satırda gürültüye bağlı yuvarlama yapıyordu.

**Düzeltme:** ayrık kararlar (müşteri sayısı, kadro) Python tarafında da tamsayı aritmetiğine geçti. `tools/balance/model.py` içindeki `mul_div` fonksiyonu `Fx.MulDiv` ile aynı kuralı uyguluyor. Para alanları ondalık kalabilir; onların toleransı 1 sikke, çünkü sonuçta yuvarlanıp gösteriliyorlar.

**Ders:** bu hata iki bağımsız uygulama karşılaştırılmasaydı hiç görünmezdi. Tek uygulama kendi kendini onaylar. Bölüm 9'daki çapraz doğrulama hattının varlık sebebi bu.

### 2.4 Formül çevirisi örneği

[12-economy.md](12-economy.md) §5.1:

```
müşteri = masa × 4 × (0,5 + itibar/100) × gün_katsayısı
```

Çekirdekte:

```csharp
// src/Lokanta.Core/Economy/DemandModel.cs
public static int CustomersPerDay(int tables, int reputationCenti,
                                  int basePerTable, int dayFactorBp)
{
    long seats = (long)tables * basePerTable;
    long numerator = seats * (Fx.One / 2 + reputationCenti) * dayFactorBp;
    return (int)Fx.MulDiv(numerator, 1, (long)Fx.One * Fx.One);
}
```

**Ara adımda yuvarlama yok.** İlk taslak `Fx.Bp` çağrısını iki kez yapıyordu, yani iki kez yuvarlıyordu; Python modeli ise tek kez yuvarlıyor. Pay sonuna kadar bölünmeden taşınır, yuvarlama bir kere yapılır.

Denge aracı `tools/balance/model.py` artık aynı tamsayı yolunu kullanıyor (bkz. yukarıdaki bulgu). **Faz 0'ın ilk testi:** Python modeli ile C# çekirdeği aynı sekiz haftalık tabloyu üretmeli. Ayrık alanlar birebir, para alanları 1 sikke toleransında. `tests/Lokanta.Core.Tests/GoldenWeeklyTests.cs`.

### 2.5 Doğrulama

- **Yansıma testi:** `Lokanta.Core` derlemesindeki bütün tiplerin bütün alan, özellik, parametre ve dönüş tipleri taranır; `float`, `double`, `decimal` görülürse test başarısız.
- **Analizör (sonra):** Faz 1'de aynı kural bir Roslyn analizörüne taşınır, derleme hatası olur.

---

## 3. Rastgelelik

### 3.1 Üreteç: xoshiro128**

Seçildi. Sebepler: durumu dört `uint` (kayıt dosyasına doğrudan yazılır), ayırma yapmaz, 32 bit ARM'de 64 bit çarpma gerektirmez, referans uygulaması on satır, kalitesi oyun için fazlasıyla yeterli. PCG32 de olurdu; farkı yok, biri seçildi.

```csharp
public struct Rng
{
    uint s0, s1, s2, s3;

    public uint Next()
    {
        uint result = RotL(s1 * 5, 7) * 9;
        uint t = s1 << 9;
        s2 ^= s0; s3 ^= s1; s1 ^= s2; s0 ^= s3;
        s2 ^= t;
        s3 = RotL(s3, 11);
        return result;
    }

    /// [0, maxExclusive). Lemire çarp-kaydır. Çok küçük bir sapma var,
    /// deterministik olduğu için kabul edildi; oyun için önemsiz.
    public int NextInt(int maxExclusive) => (int)(((ulong)Next() * (ulong)maxExclusive) >> 32);
    public int NextBp() => NextInt(Fx.One + 1);                 // 0..10000
    public bool Chance(int bp) => NextInt(Fx.One) < bp;

    static uint RotL(uint x, int k) => (x << k) | (x >> (32 - k));
}
```

### 3.2 Akışlar

Her alt sistemin kendi akışı var. Bir sisteme çağrı eklemek diğerinin dizisini kaydırmaz.

```csharp
public enum RngStream
{
    Arrival,      // müşteri geliş zamanı
    Archetype,    // hangi arketip
    Order,        // ne sipariş eder
    StaffError,   // personel hata atar mı
    Market,       // günlük malzeme fiyatı
    Event,        // günlük olay zarı
    Hiring,       // aday havuzu
    ReviewText,   // yorum şablonu seçimi
    Count
}
```

Tohumlama:

```csharp
static Rng Seed(ulong master, RngStream stream)
{
    ulong z = master ^ ((ulong)(stream + 1) * 0x9E3779B97F4A7C15UL);
    // splitmix64, dört kez
    uint Mix() {
        z += 0x9E3779B97F4A7C15UL;
        ulong x = z;
        x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
        x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
        return (uint)(x ^ (x >> 31));
    }
    return new Rng { s0 = Mix(), s1 = Mix(), s2 = Mix(), s3 = Mix() };
}
```

Kayıt dosyası her akışın dört `uint` durumunu yazar. Çağrı sayacı tutulmaz: durumu geri yüklemek O(1), sayaçla ileri sarmak O(n).

### 3.3 Yasaklar

| Yasak | Sebep |
|---|---|
| `System.Random` | Tohumlanabilir ama .NET sürümleri arasında algoritması değişti |
| `Guid.NewGuid()` | Rastgele ve platforma bağlı |
| `string.GetHashCode()` | .NET Core'da her süreçte farklı; sözlük sırasını bile değiştirir |
| `Dictionary` üstünde sıraya bağlı döngü | Sıra garanti değil. Liste kullan, ya da `SortedDictionary` ordinal anahtarla |
| `HashSet` üstünde sıraya bağlı döngü | Aynı |
| `DateTime.Now`, `Environment.TickCount` | Gerçek zaman çekirdeğe girmez |
| `UnityEngine.Random` | Zaten erişilemez, asmdef engelliyor |

### 3.4 Doğrulama

- **Akış bağımsızlık testi:** `Event` akışına bin fazladan çağrı ekle, `Arrival` dizisi değişmemeli.
- **Bilinen değer testi:** tohum 0 için ilk beş `Next()` çıktısı sabitlenir ve referans uygulamayla karşılaştırılır.

---

## 4. Kültür ve metin

### 4.1 Tuzak

Türkçe kültürde `"ID".ToLower()` sonucu `"ıd"`, `"file".ToUpper()` sonucu `"FİLE"`. Ordinal olmayan her karşılaştırma, sıralama ve arama Türkçe cihazda farklı çalışır. Geliştirici Türk, cihazı Türkçe, oyuncuların çoğu değil: **hata sadece geliştiricinin makinesinde görünmez.**

### 4.2 Kurallar

| Kural | Nasıl |
|---|---|
| Kimlik karşılaştırma | `string.Equals(a, b, StringComparison.Ordinal)` veya `a == b` (zaten ordinal) |
| Kimlik sıralama | `StringComparer.Ordinal` |
| Küçük harfe çevirme | `ToLowerInvariant()`; çekirdekte zaten gerekmemeli, kimlikler küçük harf ASCII |
| Sayı ayrıştırma | Çekirdek sayı ayrıştırmaz. İçerik yükleyici `CultureInfo.InvariantCulture` ile |
| Sayı biçimleme | Çekirdek metin üretmez. Sayı ve anahtar döner, görünüm katmanı biçimler |
| Kimlik alfabesi | `[a-z0-9_]+`, doğrulama reddeder |

`Lokanta.Core` hiçbir yerde `CultureInfo` referansı taşımaz, çünkü kültüre duyarlı işlem yapmaz. Bu, kuraldan daha güçlü: ihtiyaç yok.

### 4.3 Doğrulama

- **Kültür testi:** aynı 60 günlük koşu `tr-TR`, `en-US` ve `de-DE` kültürlerinde (`CultureInfo.CurrentCulture` değiştirilerek) çalıştırılır, üç hash eşit olmalı.
- **Kimlik testi:** içerik yüklenirken `[a-z0-9_]+` dışında kimlik görülürse yükleme reddedilir.

---

## 5. Kayan noktanın sınırı

`float` şurada serbest: `Lokanta.View`, `Lokanta.UI`. Konum, animasyon, kamera, arayüz geçişleri. Bunlar görünüştür, simülasyon değil.

**Sınır kuralı:** portlardan ve `Simulation` yüzeyinden `float` geçmez. Görünüm katmanı tamsayı alır ve kendisi çevirir:

```csharp
// View tarafı
Vector3 pos = new Vector3(tableX * 1.0f, 0f, tableY * 1.0f);   // tamsayı ızgaradan
float fill = patienceMs / (float)patienceMaxMs;                  // çubuk için
```

Görünüm katmanı simülasyonu asla `float` ile etkilemez. Dokunuş konumu masa kimliğine çevrilir, komut olarak gönderilir; koordinat çekirdeğe girmez.

---

## 6. Kütüphane ve altyapı kararları

### 6.1 İçerik yükleme: Newtonsoft, sadece açılışta, sadece Content katmanında

Seçenekler değerlendirildi:

| Seçenek | Durum |
|---|---|
| Newtonsoft (`com.unity.nuget.newtonsoft-json`) | **Seçildi.** Unity'nin resmi paketi, IL2CPP'de çalışıyor, `link.xml` ile tip koruma gerekiyor |
| System.Text.Json kaynak üretimi | En doğru teknik cevap ama Unity'ye yedi DLL taşımak gerekiyor; Faz 0'da bu sürtünmeye değmez |
| Elle yazılmış ayrıştırıcı | Şema değiştikçe bakım yükü; on bir şema için fazla |

Kurallar:

- Newtonsoft yalnızca `Lokanta.Content` derlemesinde referanslanır. `Lokanta.Core` JSON bilmez; hazır DTO alır.
- Her DTO alanı `[JsonProperty("adı")]` ile açıkça işaretlenir. Yansıma adı türetmez.
- `link.xml` bütün DTO tiplerini korur. IL2CPP budaması yüzünden "alan boş geldi" hatası Faz 0'da bir kez yaşanır, sonra bir daha yaşanmaz.
- Yükleme açılışta bir kez. Servis sırasında JSON işlemi yok.
- Sayısal alanlar JSON'da **tamsayı** yazılır (santi-sikke, bp, ms). Ondalık görülürse doğrulama reddeder.

### 6.2 Kayıt dosyası: tek yürüyüş, iki çıktı

[15-save-system.md](15-save-system.md) JSON + gzip + sağlama toplamına karar verdi; değerlendirme bunu onayladı (B6). Bu karar korunuyor. Ama **serileştirme yansımayla değil, elle yazılmış yürüyüşle** yapılır:

```csharp
public interface IStateWriter
{
    void Begin(string key);  void End();
    void Int(string key, int v);
    void Long(string key, long v);
    void Str(string key, string v);          // sadece kimlikler
    void Arr(string key, int count);         // ardından count kadar eleman
}

public interface IStateReader { /* simetrik */ }

public interface ISerializable
{
    void Write(IStateWriter w);
}
```

Her durum tipi (`TableState`, `StaffState`, `CustomerState`, `Inventory`, `Loan`, ...) `Write` ve statik `Read` uygular. Alan sırası sabittir ve sınıfta yorumla numaralanır.

`IStateWriter`'ın iki uygulaması:

| Uygulama | Nerede | İş |
|---|---|---|
| `JsonStateWriter` | `Lokanta.App` | Kayıt dosyasını yazar |
| `HashStateWriter` | `Lokanta.Core` | FNV-1a 64 ile durum özeti üretir |

**Aynı yürüyüş** hem dosyayı hem özeti üretir. Bir alan kayıtta unutulursa özet de onu görmez ve determinizm testi yakalayamaz; bu yüzden her yeni alan için "yürüyüşe eklendi mi" kontrol listesi maddesi var (§10).

### 6.3 Olay mekanizması

Çekirdekten dışarı C# `event` veya `delegate` çıkmaz. Sebep: görünüm katmanı çekirdeğe abone olursa yaşam döngüsü karışır ve çekirdek görünümün istisnalarını yer.

```csharp
public readonly struct SimEvent
{
    public readonly long Tick;
    public readonly SimEventKind Kind;
    public readonly int A, B, C, D;     // yük yuvaları; anlamı Kind'a bağlı, enum'da belgelenir
}
```

- Çekirdek olayları 4.096 kapasiteli halka tampona yazar.
- Sürücü her tick partisinden sonra `DrainEvents()` çağırır, görünüm katmanına dağıtır.
- Tampon dolarsa: hata ayıklama derlemesinde assertion, yayın derlemesinde en eskisi düşer. Bir tick partisinde 4.096 olay üretilmesi zaten tasarım hatası.
- Olay yükü tamsayı. Metin yok. Görünüm, kimliği anahtara, anahtarı metne çevirir.

### 6.4 Kompozisyon kökü

Tek yer, sabit sıra. `Lokanta.App.Bootstrap`, ilk sahnedeki tek `MonoBehaviour`:

```
1. Platform portları oluşturulur   (ISaveStore, IStoreFront, IAnalytics ... Platform.Mobile)
2. İçerik yüklenir                 (Content.Loader → ContentSet)
3. İçerik doğrulanır               (Content.Validator; hata varsa oyun açılmaz, hata ekranı)
4. Kayıt yuvası seçilir / okunur   (ISaveStore → SaveEnvelope)
5. Simulation kurulur              (ContentSet + tohum + varsa anlık görüntü + komut günlüğü)
6. Komut günlüğü tekrar oynatılır  (§7)
7. View kurulur, olay tamponuna bağlanır
8. SimDriver başlar
```

Hiçbir `MonoBehaviour` kendi kendine `new Simulation()` yapmaz. Hiçbir `static` tekil yok. Test, 5. adımdan başlayıp 7 ve 8'i atlar.

---

## 7. Servis ortası kayıt: komut günlüğü

### 7.1 Model

Kayıt dosyası üç parçadır:

```
SaveEnvelope
├── header        sürüm, mutfak, gün, tickIndex, tohum, sağlama, yazılma zamanı (sadece bilgi)
├── snapshot      gün başındaki tam durum (§6.2 yürüyüşü)
└── commands[]    gün başından beri uygulanan komutlar, tick sırasıyla
```

Yükleme: anlık görüntü geri yüklenir, komutlar sırayla uygulanırken aradaki tick'ler azami hızda çalıştırılır. Deterministik olduğu için sonuç, kesintisiz oyunla bayt bayt aynıdır.

### 7.2 Komut

```csharp
public readonly struct Command          // 20 bayt
{
    public readonly long Tick;
    public readonly CommandKind Kind;   // int
    public readonly int A, B, C;
}

public enum CommandKind
{
    OpenService,          // sabah → servis
    CloseDay,             // servis → gün sonu
    SetPrice,             // A yemek, B santi-sikke
    SetMenuSlot,          // A yuva, B yemek (−1 boş)
    SetDailySpecial,      // A yemek
    OrderIngredient,      // A malzeme, B gram
    Hire,                 // A aday
    Fire,                 // A personel
    AssignStation,        // A personel, B istasyon
    Intervene,            // A masa, B müdahale türü (özür, ikram, patron ilgisi)
    Expand,               // A kademe
    BuyEquipment,         // A ekipman
    TakeLoan,             // A kredi
    ExtendCredit,         // A düzenli müşteri, B santi-sikke  (veresiye, Türk mutfağı)
    CollectCredit,        // A düzenli müşteri
    RefillBroth,          // (Japon mutfağı)
    Count
}
```

**Komut olmayanlar:** hız, duraklatma, kamera, ekran geçişi. Bunlar görünüm durumu; simülasyona girmez, kaydedilmez.

### 7.3 Sınırlar

| Büyüklük | Değer | Kaynak |
|---|---|---|
| Gün başına azami komut | 256 | Dokunuş bütçesi 60, [16-screens-and-tutorial.md](16-screens-and-tutorial.md); 4 kat pay |
| Günlük azami tick | 4.800 servis + 1.200 sabah/akşam | §1.3 |
| Günlük tekrar oynatma süresi | < 0,5 s | 6.000 tick × 50 µs |
| Komut günlüğü boyutu | ≤ 5 KB | 256 × 20 bayt |
| Anlık görüntü | ≤ 40 KB sıkıştırılmadan | 14 masa, 12 personel, 30 müşteri, 26 malzeme, 32 yemek, 10 düzenli |

256 aşılırsa komut reddedilir ve olay üretilir; pratikte ulaşılmaz.

### 7.4 Ne zaman yazılır

| An | Ne yazılır |
|---|---|
| `CloseDay` | Yeni anlık görüntü, günlük temizlenir. **Tam kayıt** |
| Her komuttan sonra | Sadece günlük eki. Ucuz; 20 bayt |
| Android `OnApplicationPause(true)` | Günlük; anlık görüntü zaten var |
| Her 30 s servis | Günlük; komut olmasa da tickIndex ilerlemiştir |

Yazma [15-save-system.md](15-save-system.md) ve [19-technical-setup.md](19-technical-setup.md) kurallarıyla: geçici dosyaya yaz, `Flush(true)`, yeniden adlandır, `.bak` tut, CRC32.

### 7.5 Gün sınırı

`CloseDay` uygulanınca: gün sonu hesabı yapılır, durum yeni güne geçer, anlık görüntü **yeni günün başlangıcı** olarak yazılır, günlük sıfırlanır. Böylece bir kayıt dosyası en fazla bir günlük komut taşır.

### 7.6 Doğrulama

- **Kesinti testi:** 60 günlük koşu; rastgele 200 noktada kaydet, yükle, devam et. Son hash kesintisiz koşuyla eşit olmalı.
- **Sürüm testi:** eski sürüm anlık görüntüsü + günlük, göç zinciriyle yüklenir ve tekrar oynatılır.

---

## 8. Şema eklemeleri

Değerlendirme [13-data-schemas.md](13-data-schemas.md)'de dört eksik buldu. Hepsi tamsayı birimlerle (§2.2) yazılır.

### 8.1 Personel kapasitesi ve havuz

[14-staff-system.md](14-staff-system.md) iki havuzlu modeli tanımlıyor. Şemaya:

```json
{
  "id": "garson",
  "nameKey": "role.garson",
  "pool": "salon",
  "dailyWage": 11000,
  "capacityPerDay": 25,
  "stations": ["salon"],
  "xpSpeedBp": [10000, 11000, 12000, 13000]
}
```

`economy.json` içine:

```json
"staffing": {
  "ownerWorkCentiDays": 140,
  "ownerPool": "salon",
  "tiers": [
    { "tables": 4,  "rent": 195000,  "upgrade": 0,       "staffCap": 3 },
    { "tables": 7,  "rent": 435000,  "upgrade": 325000,  "staffCap": 5 },
    { "tables": 10, "rent": 685000,  "upgrade": 585000,  "staffCap": 8 },
    { "tables": 14, "rent": 1045000, "upgrade": 1040000, "staffCap": 12 }
  ],
  "weeklyXpWageGrowthBp": 220
}
```

Bu sayılar `tools/balance/model.py` çıktısıdır; JSON elle değil, yazıcıyla üretilir (Faz 0 işi: `render.py`'ye JSON hedefi eklenir).

### 8.2 İmza mekaniği parametreleri

Mekanik kodda, sayılar veride. `cuisines.json` içine mutfak başına bir `signature` bloğu:

```json
"signature": { "kind": "combo",
  "combo": { "items": ["hamburger", "patates", "gazoz"], "priceBp": 8125, "kitchenLoadBp": 12000, "ticketBonusBp": 1500 } }

"signature": { "kind": "credit",
  "credit": { "maxPerRegular": 300000, "dueDays": 7, "collectChanceBp": 8500,
              "defaultRepPenaltyCenti": 300, "loyaltyBonusBp": 1500, "teaCostCenti": 200 } }

"signature": { "kind": "courses",
  "courses": { "gapMs": 45000, "toleranceMs": 15000, "onTimeBonusCenti": 800, "lateBonusCenti": -1200 } }

"signature": { "kind": "broth",
  "broth": { "potPortions": 40, "refillMs": 1800000, "soldOutPenaltyCenti": -3000, "freshBonusCenti": 500 } }
```

`kind` bilinmiyorsa doğrulama reddeder. Blok eksikse mutfak yüklenmez.

### 8.3 Yemek parametreleri

Değerlendirmenin ortak bulgusu: "32 yemek ancak her yemek parametreliyse anlamlı." Dört zorunlu alan:

```json
{
  "id": "hamburger",
  "nameKey": "dish.hamburger",
  "cuisine": "fastfood",
  "price": 4500,
  "prepMs": 90000,
  "station": "izgara",
  "complexity": 1,
  "unlockSeason": 1,
  "ingredients": [ { "id": "kofte_harci", "grams": 120 }, { "id": "ekmek", "grams": 80 } ],
  "plating": { "base": "bun", "toppings": ["patty", "lettuce"] }
}
```

| Alan | Ne yapar |
|---|---|
| `prepMs` | Aşçı kapasitesini yemek bazında ağırlıklandırır; ağır yemek çok satılırsa mutfak tıkanır |
| `station` | Hangi ekipman gerekli; yoksa yemek menüye konamaz |
| `complexity` | 1-3; personel hata olasılığı ve çırak cezası buna bağlı |
| `ingredients[].grams` | Malzeme maliyeti fiyattan değil gramajdan türer; piyasa dalgalanması buradan işler |
| `plating` | Görünüm katmanı için; simülasyon okumaz. Bkz. [24-art-pipeline.md](24-art-pipeline.md) |

### 8.4 Tamsayı birimler, her yerde

Mevcut şemalardaki bütün ondalık alanlar dönüştürülür:

| Eski | Yeni |
|---|---|
| `"baseSpeed": 1.0` | `"baseSpeedBp": 10000` |
| `"effects": { "speed": 0.18 }` | `"effects": { "speedBp": 1800 }` |
| `"dailyWage": 140` | `"dailyWage": 14000` (santi-sikke) |
| `"patienceSec": 8` | `"patienceMs": 8000` |
| `"priceSensitivity": 2.5` | `"priceSensitivityBp": 25000` |

Doğrulayıcı JSON'da ondalık nokta görürse dosyayı reddeder. İstisna yok.

---

## 9. Doğrulama ve sürekli tümleştirme

### 9.1 Açılış doğrulaması

İçerik yüklenirken, oyun açılmadan:

1. Bütün kimlikler `[a-z0-9_]+` ve dosya içinde tekil
2. Bütün çapraz referanslar çözülüyor (yemek → malzeme, rol → istasyon, düzenli → arketip)
3. Ondalık sayı yok
4. Her mutfakta `signature` var ve `kind` tanınıyor
5. Her yemekte dört zorunlu parametre var
6. `staffing.tiers` masa sayısına göre artan, `staffCap` artan
7. `nameKey` her dilde karşılık buluyor (uyarı, hata değil)

Hata varsa oyun açılmaz, hata ekranı gösterir, hangi dosya hangi satır. Sessiz varsayılan yok.

### 9.2 Durum özeti

```csharp
public static ulong Hash(Simulation sim)
{
    var w = new HashStateWriter();        // FNV-1a 64; her int 4 bayt little-endian, long 8 bayt
    sim.Write(w);
    return w.Result;
}
```

Özet, nesne `GetHashCode` değil; açık bayt yürüyüşü. Alan sırası `Write` içindeki sıradır.

### 9.3 Determinizm hattı

| Test | Nerede | Ne |
|---|---|---|
| Altın koşu | `dotnet test`, masaüstü | Tohum 20260909, "iyi oyuncu" betiği, 60 gün → hash sabitlenir ve depoya yazılır |
| Çapraz platform | Android IL2CPP derlemesi, hata ayıklama sahnesi | Aynı koşu cihazda, hash logcat'e yazılır, masaüstüyle karşılaştırılır |
| Kültür | `dotnet test` | tr-TR, en-US, de-DE |
| Kare bağımsızlık | `dotnet test` | MaxTicksPerFrame 1 ve 5 |
| Kesinti | `dotnet test` | 200 rastgele kayıt/yükleme |
| Python eşleşmesi | `dotnet test` | C# sekiz haftalık tablo, `model.py` tablosuyla ±1 |

Altın hash değişirse ya bir hata düzeltildi ya bir hata eklendi; ikisi de commit mesajında açıklanır.

### 9.4 Cihaz testi olmadan

Mac yok, ilk sürüm Android. Çapraz platform testi bir Android cihazla yapılır. Emülatör kabul edilmez ([20-production-decisions.md](20-production-decisions.md)); IL2CPP'nin gerçek ARM derlemesi test ediliyor.

---

## 10. Kabul ölçütleri: Parti B ne zaman bitti

- [ ] `Simulation.Tick()` parametresiz; `SimDriver` biriktiriyor, tavan 5
- [x] **`Lokanta.Core` yansıma testi geçiyor:** alan, özellik, imza ve yapıcılarda hiç `float`/`double`/`decimal` yok; Unity, Newtonsoft ve System.Text.Json referansı da yok
- [x] **`Fx` sınıfı var**, çekirdekte başka bölme/yuvarlama yok. `MulDiv`, `Bp`, `CeilDiv`, `PowNano`
- [x] **`Rng` xoshiro128**, sekiz akış.** Bilinen değer testi C# çıktısını değil, bağımsız bir Python uygulamasını (`tools/balance/rng_reference.py`) tutturuyor
- [ ] Yasak liste (§3.3) için kod arama testi: `System.Random`, `GetHashCode()`, `DateTime.Now`, `Guid.NewGuid` çekirdekte geçmiyor
- [x] **Kültür testi** tr-TR, en-US, de-DE ve ar-SA kültürlerinde aynı sonucu veriyor
- [ ] Newtonsoft sadece `Lokanta.Content`'te; `link.xml` DTO'ları koruyor
- [ ] `IStateWriter` iki uygulama; her durum tipi `Write`/`Read`; yeni alan kontrol listesi PR şablonunda
- [ ] `SimEvent` halka tamponu, çekirdekten `event` çıkmıyor
- [ ] `Bootstrap` sekiz adım, sırayla; `static` tekil yok
- [ ] Komut günlüğü: on altı komut türü, 256 sınırı, kesinti testi geçiyor
- [ ] Şemalar §8'e göre güncellendi; doğrulayıcı yedi kuralı uyguluyor
- [ ] Altın hash depoda; Android cihazda aynı hash bir kez görüldü ve kaydedildi
- [x] **C# çekirdek ile `tools/balance/model.py` sekiz haftalık tabloda eşleşiyor.** 10 Eylül 2026. Ayrık alanlar (müşteri, kadro) birebir; para alanları 1 sikke toleransında. 68 test geçiyor

Son madde Faz 0'ın kendisi: bu dosya onu mümkün kılıyor, yerine geçmiyor.

---

## Karar bekleyen ayrıntılar

1. Servis günü 4.800 tick (8 dakika 1x) doğru uzunluk mu; oynanabilirlik testi söyleyecek
2. `MaxTicksPerFrame = 5` düşük cihazda yeterli mi; cihaz testi söyleyecek
3. Roslyn analizörü Faz 1'de mi, daha erken mi
