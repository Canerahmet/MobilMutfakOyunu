# Ekranlar ve Öğretici

**Son güncelleme:** 9 Eylül 2026
**Kütük maddeleri:** A9 ekran listesi ve akış, A10 öğretici
**Durum:** Yazıldı, karar bekliyor

---

## Ekran listesi

On sekiz ekran. Yatay mod, en dar güvenli alana göre tasarlanır.

### Oyun dışı

| # | Ekran | İş |
|---|---|---|
| 1 | Açılış | Logo, devam et, yuvalar |
| 2 | Yuva seçimi | Dört yuva, her biri mutfak, gün, kasa, plaket gösterir |
| 3 | Mutfak seçimi | Yeni oyun açarken. Kilitli mutfaklar önizlemeyle görünür |
| 4 | Mutfak mağazası | Gerçek parayla satın alma. **Oyun içi para ikonu burada asla kullanılmaz** |
| 5 | Ayarlar | Ses, dil, erişilebilirlik, veri |
| 6 | Miras vitrini | Kayıtlar üstü. Her mutfağın en iyi plaketi |

### Günün döngüsü

| # | Ekran | Aşama | Süre |
|---|---|---|---|
| 7 | Hal | Sabah | 45-75 sn |
| 8 | Tezgâh | Açılış öncesi | 45-75 sn |
| 9 | Servis | Gündüz | 90-180 sn |
| 10 | Gün sonu hesabı | Kapanış | 45-60 sn |

Bu dördü sıralı ve döngüseldir. Oyuncu her gün bu dört ekrandan geçer.

### Yönetim ekranları

| # | Ekran | Nereden açılır |
|---|---|---|
| 11 | Yerleşim düzenleme | Tezgâh veya hesap |
| 12 | İşe alım | Tezgâh |
| 13 | Personel yönetimi | Tezgâh. Moral, deneyim, zam, çıkarma |
| 14 | Yükseltme ve ekipman | Hesap |
| 15 | Müşteri defteri | Her yerden. Düzenli müşteriler ve hikayeleri |
| 16 | Veresiye defteri | Türk mutfağında. Kim ne kadar borçlu |
| 17 | İtibar ve yorumlar | Hesap |
| 18 | Yıl sonu değerlendirmesi | 60. günde otomatik |

---

## Akış kuralları

### Geri tuşu

Android'de donanım geri tuşu var ve davranışı tanımlı olmalı.

| Konum | Geri tuşu |
|---|---|
| Yönetim ekranı | Bir üst ekrana döner |
| Günün dört aşaması | **Hiçbir şey yapmaz.** Aşamalar arasında geri gidilmez |
| Servis sırasında | Duraklatma menüsünü açar |
| Ana ekran | Çıkış onayı sorar |

Aşamalar arasında geri gidilmemesi bilinçli. Hal aşamasında verilen karar bağlayıcıdır, geri alınmaz.

### Modal kullanımı

**Kural: oyuncuyu okumaya zorlayan modal yok.**

Modal sadece üç durumda kullanılır:
1. Onay gereken yıkıcı işlem, örneğin personel çıkarma veya kayıt silme
2. Gerçek para harcaması
3. Yıl sonu değerlendirmesi

Bunun dışındaki bütün bilgi ekranın içinde, akışı durdurmadan verilir.

### Bilgi hiyerarşisi

Servis ekranında üst çubukta üç şey var ve sırası hiç değişmez:

```
[sikke] 8.240      Gün 23    İtibar 62    Kiraya 4 gün
```

**"Kiraya 4 gün" bilinçli olarak sabit.** Haftalık baskının metronomu her an görünür olmalı. Oyuncu bunu görmeden karar vermemeli.

---

## Öğretici: ilk on dakika

### İlke

Araştırmadaki en net ders: **metin duvarı yok, yaparak öğret.** Ve günde bir kavram.

Fast food öğretici zemini olarak doğru yer, çünkü en az değişkeni olan mutfak. Ücretsiz olmasının bir sebebi de bu.

### Dakika dakika

| Dakika | Ne oluyor | Öğretilen |
|---|---|---|
| 0-1 | Soğuk açılış. Dükkân açık, tek müşteri geliyor, tek yemek var. Bir dokunuş | Servis nasıl işliyor |
| 1-3 | Birinci gün tamamlanıyor. Hal ve tezgâh en sade halleriyle | Günün dört aşaması |
| 3-5 | İkinci gün. Fiyat belirleme açılıyor | Fiyat memnuniyeti etkiliyor |
| 5-7 | Üçüncü gün. Menüde ikinci yemek. Malzeme bitiyor | Stok tükenmesinin bedeli |
| 7-10 | Dördüncü ve beşinci gün. İlk personel işe alınıyor | Kadro ve maaş |

**Yedinci gün ilk kira günü.** Öğretici oraya kadar sürüyor ve ilk gerçek sınav orada.

### Öğretici kuralları

1. **Hiçbir adım atlanmaz ama hiçbir adım da beklemez.** Oyuncu hazırsa hemen devam eder.
2. **Öğretici metni tek cümle.** İki cümleyi geçen hiçbir ipucu yok.
3. **İlk üç gün batma yok.** Kasa sıfıra inse bile merdiven işlemez. Oyuncu önce öğrenir.
4. **İlk kira günü öncesi uyarı verilir.** "Yarın kira günü, kasanda şu kadar var."
5. **Öğretici kapatılabilir.** Ama varsayılan açık.
6. **İkinci oyunda öğretici gelmez.** Yuva ekranı zaten oynadığını biliyor.

### İlk on dakikanın hedefi

Oyuncu onuncu dakikanın sonunda şunu bilmeli:

- Günün dört aşaması ne
- Fiyatı ben belirliyorum ve sonucu var
- Malzeme bitince müşteri kaybediyorum
- Personel para götürüyor ama kapasite veriyor
- Her hafta kira ödemem gerekiyor

Bunu bilmiyorsa öğretici başarısız olmuştur.

---

## Arayüz kısıtları

Bunlar mobil kararlarından geliyor ve pazarlık konusu değil.

| Kural | Değer |
|---|---|
| Yön | Yatay |
| En küçük dokunma alanı | 44pt |
| Temel etkileşim | Sürükle bırak ve tek dokunuş |
| Hassas nişan | Yok |
| Çift dokunuş | Yok |
| Günlük dokunuş bütçesi | 40-60. Prototipte ölçülecek |
| Sayısal veri | Grafiğe çevrilir. Memnuniyet yüz ifadesi, stok dolu-boş çubuk |
| Duraklatma | Her an |
| Çıkış | Her an, kaldığı saniyeden devam |

**Dokunuş bütçesi ölçülecek.** Good Pizza'da çok malzemeli siparişler korku kaynağı olmuştu, çünkü tıklama sayısı ödül olmaktan çıkıp cezaya dönüşüyordu. Altmışı geçen tasarım elenecek.

---

## Karar bekleyen ayrıntılar

1. ~~Yatay mod kesin mi, dikey de desteklenmeli mi~~ **Kapandı 9 Eylül 2026: yatay, dikey yok.** Parti D'de eklenecek: başparmak bölgesi düzeni, sol el aynalama seçeneği
2. Öğretici yedinci güne kadar mı sürmeli, daha kısa mı
3. İlk üç günde batma korumasının süresi doğru mu
4. Üst çubukta dört bilgi fazla mı

---

## Ölçüldü: masa dokunma hedefi olamıyor

10 Eylül 2026. Restoran yerleşimi Unity'de kuruldu ve dört kademe de gerçek telefon oranında render edildi (`Assets/Lokanta/Editor/RestaurantScene.cs`). Kamera bütün salonu çerçeveye sığdırıyor, açı 30 derece, yön yatay.

Sonra masanın **ekranda kaç piksel** olduğu ölçüldü. Bu sayı bugüne kadar tahminle konuşuluyordu.

| Kademe | Salon | Masa, 960 piksellik karede | 2400 piksellik telefonda | dp karşılığı |
|---|---|---|---|---|
| 4 masa | 7,8 × 7,4 m | 22 piksel | 55 piksel | ~20 dp |
| 7 masa | 9,6 × 7,4 m | 21 piksel | 53 piksel | ~19 dp |
| 10 masa | 11,5 × 7,4 m | 20 piksel | 50 piksel | ~18 dp |
| 14 masa | 13,3 × 9,1 m | 17 piksel | 42 piksel | ~15 dp |

Karşılaştırma: Google'ın asgari dokunma hedefi **48 dp**, Apple'ın **44 pt**.

**Masa, en küçük kademede bile asgarinin yarısından küçük; en büyük kademede üçte biri.** Bu, "on dört masa ekrana sığıyor mu" sorusunun cevabının evet, ama yanlış soru olduğunu gösteriyor. Sığıyor; dokunulamıyor.

### Bu ne demek

Bütün salonu tek karede gösteren bir kamerada **masa birincil dokunma hedefi olamaz.** Üç yol var ve karar verilmeli:

| Yol | Ne demek | Bedeli |
|---|---|---|
| **Kamera yaklaşsın** | Oyuncu kaydırıp yakınlaştırıyor, masa büyüyor | Dokunuş bütçesi artıyor; docs/16 günde 60 dokunuş diyor, kaydırma da onun içinde |
| **Hedef masadan büyük olsun** | Masanın etrafında görünmez, daha geniş bir dokunma bölgesi | 14 masada bölgeler çakışır; 42 piksellik masalar 130 piksellik hedeflerle örtüşür |
| **Dokunulan şey masa olmasın** | Müşteri, sipariş ya da uyarı alt çubukta listelenir; oyuncu listeye dokunur, salona değil | Salon dekora dönüşür; ama zaten patron oynuyoruz, garson değil |

Üçüncüsü [02-design-proposal.md](02-design-proposal.md)'nin "patron, şef değil" ilkesiyle en tutarlı olanı ve [review/05](review/05-player-experience.md)'in istediği "sabır uyarısı üç kanallı, üst çubukta çipler" önerisiyle örtüşüyor. Ama karar verilmedi.

**Bu ölçüm olmadan üçü de makul görünüyordu.** Sayı, seçeneklerden ikisini ciddi biçimde zayıflatıyor.
