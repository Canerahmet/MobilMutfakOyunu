# Teknik Kararlar ve Üretim Koşulları

**Son güncelleme:** 9 Eylül 2026

Bu dosya verilmiş kararların kaydıdır. Bir karar değişirse burada güncellenir ve gerekçesi yazılır.

---

## Verilmiş kararlar

### Motor: Unity ✅ karar verildi

**Gerekçe (kullanıcı):** Animasyon kalitesi ve görsel sonuç daha iyi. Çapraz platform desteği sayesinde App Store, Google Play ve ileride Steam için tek kaynaktan sürüm çıkarılabilir.

**Sonuçları:**
- Mobil reklam, uygulama içi satın alma ve analitik kütüphaneleri hazır geliyor.
- Steam sürümü gerçekçi bir ikinci hedef. Bu, araştırmadaki en başarılı örneklerin (Dave the Diver, Supermarket Simulator) bulunduğu pazar.
- Steam hedefi varsa dokunmatik ve fare/klavye girdisi baştan ayrı katman olarak tasarlanmalı. Sonradan eklemek pahalıdır.

### Ekip: tek kişi, tamamen yapay zeka destekli ✅ karar verildi

**Gerekçe (kullanıcı):** Ekip yok, oyun tamamen yapay zeka kullanılarak üretilecek.

**Sonuçları:**
- Bu, sanat tarzı kararını teknik bir karara dönüştürüyor. Bkz. aşağıdaki bölüm.
- Yol haritasındaki süreler tek kişiye göre yeniden değerlendirilmeli. Sanat üretimi fazı yapay zeka ile kısalır ama entegrasyon ve tutarlılık denetimi uzar.
- Kapsam disiplini kritik hale geliyor. Araştırmadaki en büyük risk maddesi olan kapsam patlaması, tek kişilik bir projede en sık görülen ölüm sebebi.
- Yapay zeka ile üretilen varlıkların lisansı yayın öncesi netleştirilmeli. Bazı araçların ücretsiz çıktıları atıf zorunluluğu taşıyor.

---

### Sanat tarzı: yumuşak low-poly ✅ karar verildi

Karşılaştırma sonrası önerilen yön seçildi. Melez yaklaşım geçerli: sahne ve karakterler low-poly, diyalog portreleri ve arayüz ikonları pixel art olabilir.

### Steam sürümü: planlanan hedef ✅ karar verildi

**Gerekçe (kullanıcı):** Oyun ileride Steam üzerinde de yayınlanacak. Bu yüzden proje katmanlar halinde planlanacak ki ileride değişiklik yapmak hem maliyet hem zaman olarak avantajlı olsun.

**Sonuçları:**
- Girdi katmanı baştan iki şemalı kurulur: dokunmatik ve masaüstü.
- Platforma bağlı her yetenek port arayüzlerinin arkasına konur.
- Mimari kararların tamamı [04-mimari.md](04-mimari.md) dosyasında.
- Steam gerçeklemeleri ilk üç adımda yazılmaz, sadece portlar tanımlı tutulur.

---

## Sanat tarzı gerekçesi

Karşılaştırma sayfası: https://claude.ai/code/artifact/0e98e412-5ec7-4c99-95a8-d532384f161e

Aynı restoran sahnesi iki tarzda çizildi ve telefon boyutunda karşılaştırılabiliyor.

### Belirleyici bulgu

Yapay zeka destekli üretimde **2D sprite oyunları 3D'den daha zordur.** Sebep, sprite tabanlı bir oyunda paylaşılan bir nesne olmamasıdır. Her kare bağımsız üretilmiş bir piksel haritasıdır. Yürüme döngüsünün ikinci karesi birinci karesinden farklı bir karakter çıkabilir, ışık kaynağı kareler arasında yer değiştirir, oranlar kayar.

3D'de tek bir model vardır ve her açıdan tutarlıdır.

### Karşılaştırma

| Boyut | Pixel art | Yumuşak low-poly |
|---|---|---|
| Yapay zeka ile üretim | Zayıf, her sprite bağımsız | Güçlü, temiz ve kaplaması hazır mesh |
| Animasyon | Altı durum × 8-16 kare, elle düzeltme şart | Otomatik iskelet ve hazır animasyon kütüphanesi |
| Yeni mobilya | Her açı için yeniden çizim | Modeli sahneye koy |
| Kamera | Sabit açı zorunlu | Döndürme ve yakınlaşma serbest |
| Küçük ekran | Detay gürültüye dönüşebilir | Silüet her ölçekte net |
| Ayırt edicilik | Yüksek | Orta, palet ve ışıkla telafi edilir |
| Unity uyumu | Piksel hizalama ayarı ister | Doğrudan |

### Seçilen yön: yumuşak low-poly

Mekân genişletme oyunun ana mekaniklerinden biri. Her yeni mobilyanın her açı için elle çizilmesi, tek kişilik bir projede zamanla katlanan bir maliyet.

**Melez çözüm:** Sahne low-poly, diyalog portreleri ve arayüz ikonları pixel art olabilir. Portre tek bir sabit görüntü olduğu için animasyon tutarlılığı sorunu doğurmaz. Böylece pixel sanatın sıcaklığı, hikaye anlatımının olduğu yerde korunur.

### Ayırt edicilik nasıl sağlanır

Low-poly'nin tek zayıf tarafı jenerik görünme riski. Kapatma yolu:
- Sıcak akşam ışığı ve güçlü gölge kontrastı
- Sınırlı ve kararlı bir renk paleti
- Hafif kalınlaştırılmış, okunur silüetler
- Elle hazırlanmış karakter portreleri

---

## Üretim hattı taslağı (low-poly seçilirse)

1. **Sahne ve mobilya.** Masa, sandalye, tezgâh, ocak, raf. Metinden 3D üreten araçlar veya hazır düşük poligonlu paketler. Hepsi tek bir stil kılavuzuna bağlı.
2. **Karakterler.** İnsansı temel model üret, otomatik iskeletleme servisine yükle, hazır animasyonları al. Yürüme, oturma, servis, bekleme.
3. **Işık ve palet.** Ayırt ediciliğin geldiği yer. Modellerden çok bu belirler.
4. **Portreler ve arayüz.** Tek kare oldukları için tutarlılık sorunu yok.
5. **Lisans denetimi.** Yayın öncesi tüm üretilmiş varlıkların kullanım hakkı doğrulanır.

**Uyarı:** Yapay zeka üretim araçlarının isimleri ve yetenekleri hızla değişiyor. Bu hattı kurmadan önce güncel durum doğrulanmalı.

---

## Hâlâ cevap bekleyen sorular

1. **İlk sürüm kapsamı nereye kadar.** Öneri: menü, personel, tedarik ve mekân genişletme dahil; ikinci şube hariç.
2. **Tema ve mutfak kimliği.** Öneri: belirli bir kimlik, örneğin bir esnaf lokantası.
3. **Gelir modeli.** Öneri: mobilde ücretsiz artı tek seferlik kilit açma, Steam'de peşin satış. Port sınırı sayesinde bu karar mimariyi etkilemiyor.
