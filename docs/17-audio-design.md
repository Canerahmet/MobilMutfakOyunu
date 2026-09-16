# Ses Tasarımı

**Son güncelleme:** 9 Eylül 2026
**Kütük maddesi:** A12
**Durum:** Yazıldı, karar bekliyor

---

## Temel kural

**Her sesin bir işi olacak.** Bilgi taşımayan ses eklenmeyecek.

Bunun sebebi mobil gerçeği: oyuncuların büyük kısmı sessiz oynuyor. Ses süs olarak tasarlanırsa kimse kaçırdığını fark etmez. Ses bilgi taşırsa, sessiz oynayan oyuncu bilgiyi kaybeder.

Bu yüzden ikinci kural: **her ses uyarısının görsel karşılığı olacak.** Sessiz oynamak oyunu zorlaştırmamalı, sadece daha az keyifli yapmalı.

---

## Beş katman

| Katman | İş | Sürekli mi |
|---|---|---|
| Ortam | Mekânın kimliği | Sürekli |
| Müzik | Aşamanın ruh hali | Sürekli |
| Olay | Oyun durumu bildirimi | Anlık |
| Arayüz | Dokunma geri bildirimi | Anlık |
| Karakter | Konuşma yerine anlamsız hece | Anlık |

---

## Ortam sesi

Mutfağa özel ve **servis yoğunluğuna göre yoğunlaşıyor.** Salon dolunca kalabalık uğultusu artıyor. Oyuncu ekrana bakmadan da işlerin yoğunlaştığını duyuyor.

| Mutfak | Ortam katmanları |
|---|---|
| Fast food | Fritöz cızırtısı, kasa bipleri, cam kapı, sokak trafiği |
| Türk lokantası | Çay bardağı şıngırtısı, kepçe sesi, radyo, sıcak tezgâh buharı |
| İtalyan | Çatal bıçak, kadeh, fırın kapağı, hafif akustik |
| Japon ramen | Erişte süzme, kazan fokurtusu, buhar, kısa siparişler |

Mutfak başına üç ile dört katman. Bunlar bağımsız ses dosyaları ve yoğunluğa göre karışıyor.

---

## Müzik

Aşamaya göre değişiyor. Mutfak başına dört parça artı ortak bir yıl sonu parçası.

| Aşama | Karakter |
|---|---|
| Hal | Sakin, sabah, hafif. Karar verme müziği |
| Tezgâh | Odaklı, ritmik ama sakin |
| Servis | **Katmanlı.** Doluluk arttıkça enstrüman ekleniyor |
| Hesap | Yumuşak, kapanış, biraz yorgun |

**Servis müziğinin katmanlı olması önemli.** Salon boşken sade bir temel çalıyor. Masalar doldukça yeni katmanlar giriyor. Oyuncu baskıyı kulaktan hissediyor. Overcooked bunu yapıyor ve işe yarıyor.

**Döngü süresi 60 ile 90 saniye.** Mobil oturum kısa, uzun parçalara gerek yok.

---

## Olay sesleri

Bunlar bilgi taşıyor. Listede olmayan hiçbir olay ses çıkarmıyor.

| Olay | Ton | Görsel karşılığı |
|---|---|---|
| Müşteri geldi | Nötr, kısa | Kapı animasyonu |
| Sipariş verildi | Nötr | Masa üstü ikon |
| Yemek hazır | Olumlu, kısa | İstasyonda parlama |
| **Sabır kritik** | **Uyarı, belirgin** | Masa kenarı kırmızıya dönüyor |
| **Malzeme tükendi** | **Uyarı, belirgin** | Stok çubuğu boş ve yanıp sönüyor |
| Müşteri çıkıp gitti | Olumsuz | Masa üstünde kısa simge |
| Ödeme alındı | Olumlu, para tınısı | Sikke ikonunda artış |
| Yeni yemek açıldı | Olumlu, kutlama | Menü ekranında rozet |
| Kira günü yaklaşıyor | Hatırlatma, günlük | Üst çubukta geri sayım |
| Personel istifa etti | Olumsuz, ağır | Bildirim kartı |

**Kalın yazılanlar en kritik ikisi.** Sabır ve stok, oyuncunun anında müdahale etmesi gereken iki durum. Bu ikisi ses paletinde en ayırt edici yeri alıyor.

---

## Arayüz sesleri

Yaklaşık on iki adet, bütün mutfaklarda ortak. Dokunma, onay, iptal, hata, sürükleme başlangıcı ve bitişi, ekran geçişi.

Kısa, yumuşak, tekrarlandığında rahatsız etmeyen. Oyuncu bunları günde elli kez duyacak.

---

## Karakter sesi

**Seslendirme yok.** Bunun yerine anlamsız hece.

- Her arketip bir ses tonu taşıyor: aceleci yüksek ve hızlı, emekli alçak ve yavaş.
- Yirmi kadar hece örneği, tona göre değiştiriliyor.
- Memnuniyete göre tonlama değişiyor.

Üç sebep: maliyeti sıfıra yakın, yerelleştirme gerektirmiyor, ve karakter katıyor. Animal Crossing çizgisinde bir çözüm.

---

## Sessizlik

Sessizlik de bir araç.

- Servis bittiğinde kısa bir boşluk, sonra hesap müziği giriyor.
- Gün sonunda müşteri yorumları okunurken ortam sesi kısılıyor. Metne odak.
- Yıl sonu değerlendirmesi tamamen sessizlikle açılıyor, sonra tek bir parça giriyor.

---

## Üretim yükü

| Kalem | Adet | Kapsam |
|---|---|---|
| Ortam katmanı | 3-4 | Mutfak başına |
| Müzik parçası | 4 | Mutfak başına |
| Yıl sonu parçası | 1 | Paylaşılan |
| Olay sesi | ~40 | Paylaşılan |
| Arayüz sesi | ~12 | Paylaşılan |
| Karakter hecesi | ~20 | Paylaşılan, tonla çeşitleniyor |

Çıkıştaki iki mutfak için toplam: sekiz müzik parçası, yedi ortam katmanı, artı paylaşılan yetmiş kadar kısa ses.

Kaynak kararı kütükte C4 maddesi olarak açık duruyor: ücretli yapay zeka aracı mı, kamu malı kütüphane mi.

---

## Karar bekleyen ayrıntılar

1. Servis müziği katmanlı mı olmalı, yoksa tek parça mı yeterli
2. Karakter hecesi mutfağa göre değişmeli mi
3. Mutfak başına dört müzik parçası fazla mı
