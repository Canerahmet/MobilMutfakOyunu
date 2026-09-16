# Oyunun Sonu

**Son güncelleme:** 9 Eylül 2026
**Kütük maddesi:** A14
**Durum:** ✅ Onaylandı, 9 Eylül 2026

---

## Soru

Oyunun tanımlı bir sonu mu olsun, yoksa sonsuza kadar mı devam etsin?

Mutfak kilidi kararı bu soruyu zorunlu hale getirdi. "Oyun bitene kadar o tarz ile devam edelim" cümlesi, oyunun bittiği bir noktanın var olmasını gerektiriyor.

---

## Diğer oyunlar ne yapıyor

| Oyun | Model | Sonuç |
|---|---|---|
| **Stardew Valley** | Üçüncü yılda puanlanan değerlendirme, sonra serbest oyun | En başarılı model. Oyun bitmiyor, sadece değerlendiriliyor |
| **Dave the Diver** | Hikaye sonu, jenerikten sonra devam edilebiliyor | İyi ama post-game içeriği zayıf bulunmuş |
| **PlateUp!** | On beş günlük koşular, her koşu bitiyor | Roguelite. Kayıp kalıcı bonusa dönüşüyor |
| **Two Point Hospital** | Bölüm bazlı, her bölümde üç yıldız hedefi | Net hedefler, ama bizim yapımıza uymuyor |
| **Supermarket Simulator** | Sonsuz. Tanımlı son yok | **Uyarı vakası.** Aşağıda ayrıntısı var |
| **Tavern Master** | Sonsuz, boş son oyun | Araştırmada "harcayamayacağın kadar para" şikayeti |
| **Cat Cafe Manager** | Kısa hikaye, sonrası yok | "Hikaye bitince tekrar oynanabilirlik yok" |

---

## Kanıt: sonsuz modelin tuzağı

Supermarket Simulator bize en yakın ekonomik döngüye sahip oyun ve sonsuz modeli seçti. Forumlarındaki geç oyun şikayetleri çok net:

- İki yüz saat oynayan bir oyuncu 90. seviyede ve hâlâ beş genişleme ile altı lisans kalmış. **Orta ve geç oyunda tek bir genişlemeyi almak otuz saat gerçek zaman alıyor.**
- Oyun "depo simülatörüne" dönüşüyor. İki üç kasiyer ve raf görevlisi alındıktan sonra oyuncu mağazada neredeyse hiç vakit geçirmiyor, açılıştan önceki bir saati deponun dolu olduğundan emin olarak harcıyor.
- Ödül tarafı boş: "birkaç ürün lisansı dışında hiçbir ödül almıyoruz."
- Oyuncular günü kısaltmak için mod kullanıyor. Yirmi beş dakikalık gün beş dakikaya iniyor.

Aynı forumda "450 saat sonunda oyunu bitirdim" başlığı da var. Yani sonsuz model bir kesim için çalışıyor. Ama şikayetler baskın ve hepsi aynı yere işaret ediyor: **hedef yoksa büyüme anlamını yitiriyor.**

Öte yandan sert son da kötü. Stardew Valley'nin tasarımcısı eski Harvest Moon oyunlarının oyuncuyu daha iyi bir son için baştan başlamaya zorlamasını bilinçli olarak reddetmiş.

**İki uç da kötü. Ortası çalışıyor.**

---

## Öneri: puanlanan final, sonrası serbest oyun

Oyun tanımlı bir noktada **değerlendirilir**, ama **bitmez**.

### Finalin yapısı

**Birinci yılın sonu.** Dört mevsim, mevsim başına yaklaşık on beş gün, toplam altmış gün.

Yıl dolduğunda semtin yemek eleştirmeni yıllık değerlendirmesini yazar. Bu bir gazete yazısı olarak sunulur ve başlığı senin puanına göre değişir. Ardından restorana bir plaket asılır.

**Plaket duvarda kalır ve serbest oyunda görünür.** Ne yaptığının kalıcı ve görünür kanıtı olur.

### Değerlendirmeden sonra

- Kayıt silinmez, hiçbir şey elinden alınmaz.
- Restoran çalışmaya devam eder. Serbest oyun modu açılır.
- Yeni hikaye gelmez, ama oyun oynanabilir kalır.
- İstersen daha iyi bir puan için ikinci yıla devam edip yeniden değerlendirilebilirsin. Stardew Valley'nin yeniden değerlendirme fikri.

### Tekrar oynama kancası

Puan, mutfak satın almanın değerini yaratan şey. Farklı bir mutfakla yeni bir kayıt açıp daha iyi bir sonuç hedeflersin.

**Miras ekranı:** kayıtlar üstü kalıcı bir vitrin. Her mutfağın en iyi sonucunu gösterir. Dört plaketi de toplamak uzun vadeli hedef olur.

---

## Puanlama eksenleri

Değerlendirme tek sayı değil, çok eksenli olmalı. Böylece farklı oyun tarzları farklı yollardan iyi sonuç alabilir.

| Eksen | Ne ölçer |
|---|---|
| Varlık | Yıl sonundaki net değer |
| İtibar | Son itibar seviyesi |
| Düzenli müşteriler | Kaç düzenli müşteri kazandın ve elinde tuttun |
| Ekip | Kalan personel ve morali |
| Mekân | Ne kadar büyüdün |
| **Sağlamlık** | Batma merdivenine hiç düştün mü, ne kadar derine |
| **Mutfağa özel eksen** | Her mutfakta farklı |

### Mutfağa özel eksen

| Mutfak | Eksen |
|---|---|
| Fast food | Ana yemeklerin kaçı komboya döndü |
| Türk mutfağı | Veresiye tahsilat oranı |
| İtalyan | Ortalama fiş tutarı |
| Japon mutfağı | Çorba suyu isabet oranı |

Bu eksen mutfakları birbirinden ayıran şeyi sonuçta da görünür kılıyor.

> **Ölçüm notu (13 Eylül 2026).** Bu satır *"imza mekaniğini doğrudan
> ödüllendirir"* diyor ve bir süre fast food için **doğru değildi**: ekseni
> `peakCovers` (en yüksek günlük kuver) idi ve ölçüldü ki komboyu her sabah açan
> bot ile hiç açmayan bot aynı puanı alıyor (38/38), en yüksek puanlar ise en
> çok genişleyenlerde (plancı 73). Yani eksen "Mekân" ekseninin kopyasıydı.
>
> **Eksen değiştirildi** (`comboShare`, hedef %15 — ölçümden). Artık komboyu
> kullanan bot 100, kullanmayan 0 alıyor ve genişleme bu ekseni hiç
> etkilemiyor. İmzacı bot her iki mutfakta da en yüksek puanlı strateji.
> Ayrıntı [docs/42](42-crew-and-intervention.md) 5.
>
> *Bir belgedeki "doğrudan ödüllendirir" cümlesi, ölçülene kadar bir tahmindi;
> ölçülünce yanlış çıktı ve düzeltilen şey belge değil kod oldu.*

---

## Batma merdiveni ile bağlantı

Bu tasarımın en değerli yan etkisi burada.

Şu ana kadar batma merdiveninin sadece kısa vadeli sonucu vardı: itibar kaybı, ekipman satışı, küçülme. **Sağlamlık ekseni ona uzun vadeli bir sonuç ekliyor.**

Merdivene hiç düşmeden yılı tamamlamak yüksek puan getirir. Küçülmeye kadar inmek kalıcı olarak puanı düşürür. Kayıt hâlâ silinmez, oyun hâlâ bitmez, ama yaptığın hatanın izi yıl sonunda karşına çıkar.

Böylece "yumuşak ama dişli başarısızlık" tasarımı tamamlanmış oluyor. Ceza anlık değil, birikimli.

---

## Kampanya uzunluğu

| Değer | Tahmin |
|---|---|
| Kampanya | 60 oyun günü, dört mevsim |
| Gün başına süre | 3 ile 6 dakika |
| Mutfak başına toplam | Yaklaşık 4 ile 6 saat |
| Çıkıştaki iki mutfak | Yaklaşık 8 ile 12 saat |

**Bunlar hipotez, ölçülmedi.** Faz 0'daki denge aracının doğrulaması gereken ilk şeylerden biri bu. Altmış gün çok uzunsa sistemlerin açılma temposu sıkışır, çok kısaysa ekonomi anlamsızlaşır.

Sistem açılma temposu da buna uymalı. Bütün sistemler ilk üç saatte açılmalı ki son iki saat ustalık ve optimizasyona kalsın.

---

## Neden bu model bizim için doğru

1. **Mutfak kilidini mümkün kılıyor.** "Oyun bitene kadar" artık tanımlı: yıl sonu değerlendirmesi.
2. **Supermarket Simulator tuzağından kaçınıyor.** Hedefsiz sonsuz büyüme yok.
3. **Sert sondan da kaçınıyor.** Hiçbir şey elinden alınmıyor, isteyen devam ediyor.
4. **Mutfak satın almasını değerli kılıyor.** Puan ve plaket, dört mutfağı da oynamak için sebep veriyor.
5. **Araştırmadaki beşinci şikayeti kapatıyor.** Boş son oyun sorunu, plaket ve miras ekranıyla çözülüyor.
6. **Batma merdivenini tamamlıyor.** Başarısızlığın artık birikimli bir bedeli var.

---

## Karar bekleyen ayrıntılar

1. Kampanya altmış gün mü olmalı, yoksa daha kısa mı
2. Serbest oyunda yeni hedefler olacak mı, yoksa sadece işletme mi devam edecek
3. İkinci yıla devam edip yeniden değerlendirilme olacak mı
4. Plaket kaç kademeli olacak
