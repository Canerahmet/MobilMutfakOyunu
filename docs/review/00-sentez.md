# Beş Ajanlı Değerlendirme: Sentez

**Tarih:** 9 Eylül 2026
**Girdi:** Beş bağımsız değerlendirme, her biri farklı bir uzman bakış açısıyla ve sadece kendi alanındaki dokümanlarla. Ham raporlar bu klasörde 01'den 05'e.
**Kapsam:** Kütükteki onay bekleyen 24 kalem.

---

## Hüküm

| Karar | Kalem |
|---|---|
| ONAYLA | 3 |
| DÜZELT | 21 |
| REDDET | 0 |

**Hiçbir kalem reddedilmedi. Neredeyse hiçbiri de olduğu gibi onaylanmadı.** Beş ajanın ortak teşhisi aynı: yapı doğru, gerekçeler araştırmaya bağlı, ilkeler sağlam. Ama sayısal ve ayrıntı katmanı kendi içinde tutmuyor.

İki ajanın tek cümlelik hükümleri planın durumunu özetliyor:

- Kapsam gerçekçisi: **"Tek kişi bu oyunu çıkarabilir, ama bu planı çıkaramaz."**
- Yayıncı: **"Plan para kazanmaz değil, planlandığı sırayla para kazanmaz."**

Onaylanan üç kalem: kayıt dosyası formatı (B6), ses kaynağı (C4), arayüz ve yazı tipi (C5). Üçü de şartlı onay.

---

## Beş ajanın birleştiği yerler

Bunlar birden fazla ajanın bağımsız olarak, farklı dokümanlardan gelerek bulduğu sorunlar. En güvenilir bulgular bunlar.

### 1. Sayılar formülden türemiyor

**Üç ajan, üç açıdan.** Tasarımcı ekonomi tablosunu yeniden hesapladı: ikinci hafta neti 560 değil 933, üçüncü hafta −322 değil +1.810. "Genişleme haftası zarar ettiriyor" anlatısı tasarım kararı değil, aritmetik hatasının sonucu. Kapasite modelinde 58 müşteri için 8 değil 12 personel gerekiyor; "tavan ihtiyaca göre ayarlandı" cümlesi kendi sayılarıyla yanlış. Mimar aynı kapasite modelinin şemalarda hiç olmadığını buldu. UX uzmanı öğreticinin ilk personel dersinin kapasite modeliyle çeliştiğini buldu.

**Sonuç:** Faz 0 denge aracı bu tablolardan değil, formüllerden başlamalı. Tablolar sonuç olmalı, girdi değil.

### 2. Hal aşaması çözülmemiş

**İki ajan.** UX uzmanı bir günün dokunuşlarını saydı: ham akışta 80, bütçe 60, ve hal tek başına 40. Tasarımcı aynı aşamada bir ilke çelişkisi buldu: 02 ilke 2 "malzeme siparişi otomatikleşir" derken 02 §3 halı "ikinci döngü" yapıyor. Fast food'da malzemenin %80'i bozulmadığı için hal "ucuz günde stokla" oyununa dönüyor, üstelik ücretsiz ve öğretici mutfakta.

**Sonuç:** Hal ya otomatik taban sipariş artı günde 3-5 elle karar olacak, ya da gerçek bir ikinci döngü. İkisi birden olamaz.

### 3. Kapsam, prototip yokken şişti

**İki ajan.** Kapsam gerçekçisi varlıkları saydı ve süreyi tahmin etti: 13-14 kişi-ay, yol haritası 5-7 ay diyor. Tek günde yemek 20→32, arketip 8→20, kıyafet 12→16, isimli karakter 8→13 olmuş, ekonomi hâlâ dengelenmemiş. Tasarımcı aynı noktaya farklı yoldan geldi: 32 yemek ancak her yemek dört parametre taşırsa anlamlı, aksi hâlde v1'in 20'si yeterliydi.

**Sonuç:** v2 sayıları şema tavanı olarak kalsın, çıkış v1 sayılarında dondurulsun.

### 4. Dokümanlar birbiriyle çelişiyor

**Üç ajan.** Kombinasyon sayısı bir dosyada hem 17.280 hem 23.040. Müzik parçası 8 ve 9. Arketip v1 sayısı 6 ve 8. `hourSplit` ile `arrivalWeights` aynı şeyin iki kaynağı. İlk personel zamanı dört dokümanda dört farklı. `IAnalytics` bir listede var bir listede yok.

**Sonuç:** Planın aynı gün içinde birkaç kez büyüdüğünün izi. Tek geçişte temizlenmeli.

### 5. Öğretici takvimi tutmuyor

**UX uzmanı ana bulgu, tasarımcı destek.** İlk on dakikada öğrenilen beş şeyin beşi de ceza: fiyat kızdırır, stok biter, personel para götürür, kira gelir. Tek olumlu doruk yok. "İlk üç gün batma yok" kuralı ölü, çünkü merdiven haftalık ödemeye bağlı ve yedinci günden önce zaten tetiklenemez. Tasarımcı ise döngünün 20. günde otomatikleştiğini, fiyatın birinci gün çözülüp bir daha dokunulmadığını buldu.

---

## Tek ajanın bulduğu ama kritik olanlar

Bunlar yalnızca bir uzmanın alanına giriyor ama her biri tek başına Faz 0'ı engelleyecek ağırlıkta.

| Bulgu | Ajan | Neden kritik |
|---|---|---|
| **Determinizm tasarlanmamış** | Mimar | `Tick(deltaTime)` değişken kare süresini çekirdeğe sokuyor. RNG belirsiz, kayan nokta platformlar arası ayrışır, tr-TR kültürü `ToUpper` ve `Parse`'ı bozar. Kayıt sistemi ve denge aracı bu varsayıma yaslanıyor |
| **Steam sırası ters** | Yayıncı | Araştırmadaki on referansın dokuzu Steam. Sayaçtan nefret eden kitle orada. Plan Steam'i en sona atıyor |
| **Dönüşüm tetikleyicisi 4-6 saat sonrada** | Yayıncı | Satın alma ancak ücretsiz kampanya bitince anlam kazanıyor. Oraya kurulumların %5-10'u ulaşır |
| **Sabır uyarısı sadece renk** | UX | Kırmızı masa kenarı, kırmızı-sarı fast food paletinde. Sessiz ve renk körü oyuncu görmüyor |
| **GPU Resident Drawer yanlış araç** | Mimar | Forward+ ister, GLES'te çalışmaz, 100 çizim çağrısında faydası ölçülemez |
| **Karakter hattı yetenek boşluğunda** | Kapsam | Ağırlık aktarımı görsel yargı ister. 16 set × 3 vücut tipi = 96 mesh. Mixamo yedeği iskeleti kurtarıyor, klipleri değil |
| **Hukuki boşluklar somut** | Yayıncı | Satın alma geri yükleme (Apple 3.1.1 reddi), iade iptali, AB DSA tacir adresi, Steam yapay zekâ beyanı, oyunun adı yok |

---

## Ajanların çeliştiği yerler

Gerçek çelişki tek: **yemek sayısı.** Tasarımcı "32 kalabilir, ama her yemek parametreliyse" diyor. Kapsam gerçekçisi "20'ye in" diyor. İkisi de 32'nin parametresiz hâlinin yanlış olduğunda hemfikir.

Diğer görünür çelişkiler aslında uyumlu:

- Yayıncı gün sonu ödüllü reklamı geri istiyor. Mimar `IAdProvider`'ın tanımsız olduğunu, tanım yoksa kaldırılması gerektiğini söylüyor. Reklam tanımlanırsa ikisi de memnun.
- Kapsam gerçekçisi isimli karakteri 13'ten 8'e indiriyor. Yayıncı veresiye ile düzenli müşteri kesişimini "satın almanın tek gerçek gerekçesi" sayıyor. Kapsam gerçekçisi zaten "sayı iner, sistem kalır, bu asla kesilmez" diyor.

---

## Kalem kalem kararlar

| Kalem | Konu | Karar | Ajan | Öz |
|---|---|---|---|---|
| A4 | Ekonomi sayıları | DÜZELT | Tasarım | Tablo formülden türesin, hafta sonu katsayısı uygulansın |
| A5 | İçerik envanteri | DÜZELT | Tasarım | Her yemeğe dört parametre, menü yuvası kademeyle büyüsün |
| A6 | İlerleme eğrisi | DÜZELT | Tasarım | 4. mevsime olay zinciri ya da kampanya 45 güne |
| A7 | Müşteri sistemi | DÜZELT | Tasarım | Kişi/grup netleşsin, nadir takvime bağlansın, fiyat cezası doğrusal olmasın |
| A8 | Personel sistemi | DÜZELT | Tasarım | Kadro kapasiteden türesin, tavan 3/5/7/9, izin günü evet |
| A9 | Ekranlar ve akış | DÜZELT | UX | Aşama içi geri alma, geri tuşu duraklatma açsın, "Dün" kartı |
| A10 | Öğretici | DÜZELT | UX | Tek takvim, ilk kayıptan sonra ilk işe alım, olumlu doruk |
| A11 | Kayıt sistemi | DÜZELT | Mimar | Servis ortası kayıt komut günlüğü olsun, yazma arka iş parçacığında |
| A12 | Ses tasarımı | DÜZELT | UX | Görsel ikiz renkten bağımsız olsun, titreşim eklensin |
| A13 | Hikaye ve metin | DÜZELT | Kapsam | Düzenli 10→6, personel 3→2, eleştirmen 5→3 kademe |
| A15 | Mutfak kimliği | DÜZELT | Kapsam | Kıyafet 16→8-10, kombinasyon sayısı tutarlı olsun |
| B3 | Veri şemaları | DÜZELT | Mimar | İmza mekaniği parametreleri, kapasite bloğu, tamsayı birimler |
| B4 | Unity ve render | DÜZELT | Mimar | GRD kapalı, HDR kapalı, taşınabilir mobilya pişirilmesin |
| B5 | Performans | DÜZELT | Mimar | Bütçeler milisaniye cinsinden, sıfır GC, PSS tanımlı |
| **B6** | **Kayıt formatı** | **ONAYLA** | Mimar | Şartlı: CRC32, Flush(true), zarf meta alanları |
| B7 | Girdi haritası | DÜZELT | Mimar | Oyun kolu şeması şimdi, 16:10 desteği, IInputSource port olmasın |
| C1 | Model yolu | DÜZELT | Kapsam | Prototip hazır paketle, betik iki haftalık zaman kutusuyla |
| C2 | Karakter | DÜZELT | Kapsam | Tek mesh artı kemik ölçeği, klipler bugün indirilsin |
| **C4** | **Ses kaynağı** | **ONAYLA** | Kapsam | Şartlı: stem verebildiği doğrulansın, bütçe satırı dolsun |
| **C5** | **Arayüz ve yazı tipi** | **ONAYLA** | Kapsam | Planın en tamam kalemi |
| C8 | Test planı | DÜZELT | Kapsam | Sıkılma sorusu telemetriye, Mac yoksa iOS ertelensin |
| D2 | Fiyat | DÜZELT | Yayıncı | Paket 9,99 ve ancak iki mutfakla, bölgesel fiyat, dekor kademesi |
| D5 | Lansman | DÜZELT | Yayıncı | Steam sayfası Faz 1 sonunda, Next Fest, kapalı test Türkiye |
| D6 | Hukuki | DÜZELT | Yayıncı | Geri yükleme, iade iptali, DSA adresi, AI beyanı, ad tescili |

Danışma niteliğinde: **D1 gelir modeli** (karar verilmişti) için yayıncı DÜZELT diyor: üç günlük sıfırlama penceresi ücretli mutfağın bedava tadımına çevrilsin, gün sonu ödüllü reklam geri gelsin.

---

## Düzeltme planı

Yirmi bir düzeltme yedi partiye ayrıldı. İlk ikisi Faz 0'ın ön koşulu.

### Parti A — Sayısal tutarlılık *(Faz 0 öncesi zorunlu)*

1. Büyüme tablosunu formülden, hafta sonu katsayısıyla yeniden üret
2. Kapasite modelini kadro tablosu ve tavanla uzlaştır: ya kapasite yüksel, ya talep düş, tavan 3/5/7/9
3. Servis süresi kademeyle ölçeklensin: 4 masada 120 sn, 14 masada 180 sn
4. Müşteri birimi netleşsin: hız için grup, fiş için kişi
5. Nadir arketipler yüzdeden çıkıp takvime bağlansın: kampanyada 8-12 olay
6. Fiyat cezası doğrusal olmaktan çıksın, %10 üstünde hızlansın, tolerans itibara bağlansın
7. Her yemeğe dört parametre: hazırlık süresi, istasyon, bozulabilir malzeme, arketip çekimi
8. Veresiye tanımlansın: tavan, vade, tahsilat takvimi, ödememe olasılığı

### Parti B — Çekirdek mimari *(Faz 0 öncesi zorunlu)*

1. Sabit adım: parametresiz `Tick()`, 100 ms, `tickIndex` kayıtta
2. Durum tamamen tamsayı: milisaniye, gram, 1/100 puan
3. Çekirdekte kendi RNG'si, alt sisteme ve güne göre ayrı akış
4. Invariant kültür zorunlu, `string.GetHashCode` yasak
5. JSON kütüphanesi, olay mekanizması ve kompozisyon kökü tanımlansın
6. Şemalara imza mekaniği parametre blokları, `combos.json`, kapasite bloğu
7. Servis ortası kayıt komut günlüğü artı tekrar oynatma
8. CI'da .NET ile Android IL2CPP hash karşılaştırması

### Parti C — Kapsam kesintisi

1. Yemek 32 → 20, kalan 12 çıkış sonrası ücretsiz
2. Kıyafet 16 → 8-10, üç vücut tipi tek mesh artı kemik ölçeği
3. İsimli karakter 13 → 8, sahne 3
4. Yol haritası 13-14 kişi-ay üstünden yeniden yazılsın
5. Sulu yemek, çorba, pilav betiğe alınsın; yapay zeka üretimi 5-10 kahraman yemeğe
6. Bütün animasyon klipleri bugün indirilip depoya konsun

### Parti D — Oyuncu deneyimi

1. Hal: dünkü sipariş hazır gelsin, porsiyon üstünden sipariş, hedef on dokunuş
2. Sabır uyarısı üç kanallı: masada renk artı şekil artı hareket, ekran kenarında ok, üst çubukta dokunulabilir çipler
3. Öğretici tek takvimde, ilk işe alım ilk görünür kayıptan sonra, beşinci dakikadan önce olumlu doruk
4. Servis: 1x/2x hız, gün ilerleme çubuğu, müdahale sayacı
5. Aşama içi geri alma, geri tuşu duraklatma açsın, "Dün" kartı, kayıp müşteri sebebi

### Parti E — İş ve yayın

1. Steam sayfası Faz 1 sonunda, Next Fest demosu ücretsiz kampanya, Erken Erişim 9,99
2. Üç günlük sıfırlama ücretli mutfağın bedava tadımına dönüşsün
3. Gün sonu ödüllü reklam geri gelsin, sadece oyuncu basınca
4. Bölgesel fiyat tabloları, 1,99-2,99 dekor kademesi, paket 9,99 ve ancak iki mutfakla
5. Kapalı test Türkiye, açık test Filipinler artı Kanada, Android önce, 300-500 dolar edinim bütçesi
6. Metrikler takvim günü ile oyun gününü ayırsın; D1 ≥ %35, D7 ≥ %15, D30 ≥ %6
7. Hukuk: geri yükleme, iade iptali, DSA adresi, Steam AI beyanı, ad tescili

### Parti F — Teknik ayarlar

1. GPU Resident Drawer kapalı, HDR kapalı, MSAA 4x
2. Sadece duvar ve zemin pişirilsin
3. Bütçeler milisaniye: ana ≤ 10, render ≤ 6, GPU ≤ 14; serviste sıfır GC; PSS ≤ 500 MB
4. Oyun kolu şeması şimdi, 16:10 desteği, `IInputSource` port olmaktan çıksın
5. Uzak Addressables ilk sürümde yok

### Parti G — Doküman temizliği

1. 17.280 ile 23.040, 8 ile 9 müzik, 6 ile 8 arketip, `hourSplit` ile `arrivalWeights`, ilk personel zamanı, `IAnalytics`

---

## Senin cevaplaman gereken sorular

Ajanların sorduğu soruların çoğu tasarım işiyle kapanır. Ama şu dokuzu senin bilgin veya kararın olmadan kapanmaz.

| # | Soru | Soran | Neden önemli |
|---|---|---|---|
| 1 | **Tam zamanlı mı, akşamları mı?** | Kapsam | Kişi-ay ile takvim ayı arasındaki çarpan bu |
| 2 | **Mac var mı?** | Kapsam | Yoksa iOS derlemesi yok, ilk sürüm Android |
| 3 | **Blender'ı hiç açtın mı, mesh'in yanlış olduğunu görebiliyor musun?** | Kapsam | Prosedürel yolun zaman kutusu buna bağlı |
| 4 | **Bütçe gerçekten sıfır mı?** | Yayıncı | 300-500 dolar test edinimi olmadan soft launch veri üretmiyor |
| 5 | **Oyunun adı ne?** | Yayıncı | Marka taraması ve tescil, klonlara karşı tek savunma |
| 6 | **Yemek 20 mi 32 mi?** | Tasarım ve Kapsam çelişiyor | 32 sadece parametreliyse anlamlı |
| 7 | **Gün sonu ödüllü reklam geri gelsin mi?** | Yayıncı, verilmiş kararı sorguluyor | Ödemeyen %98'den tek gelir kaynağı |
| 8 | **Steam öne alınsın mı?** | Yayıncı, verilmiş kararı sorguluyor | Kitle orada, mobil dönüşüm düşük |
| 9 | **Yatay mod kesin mi?** | UX | Öneri: yatay kalsın ama başparmak bölgesi düzeni ve sol el aynalama |

---

## Sonraki adım

Bu dokuz soru cevaplanınca Parti A ve B yazılır. İkisi bitmeden Faz 0 başlamaz, çünkü denge aracı tutarsız sayılar ve deterministik olmayan bir çekirdek üstünde anlamsız sonuç üretir.

Parti C'den G'ye kadar olanlar Faz 0 ile paralel yürütülebilir.
