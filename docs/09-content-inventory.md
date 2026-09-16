# İçerik Envanteri ve İlerleme Eğrisi

**Son güncelleme:** 9 Eylül 2026, v2
**Kütük maddeleri:** A5 içerik envanteri, A6 ilerleme eğrisi
**Durum:** Öneri hazır, karar bekliyor

**9 Eylül 2026:** 32 kesinleşti. Tasarımcı ve kapsam değerlendirmesinin ortak şartı bağlayıcı: **her yemek dört parametre taşır** (`prepMs`, `station`, `complexity`, `ingredients[].grams`), bkz. [23-core-contract.md](23-core-contract.md) §8.3. Sanat maliyeti modüler tabaklamayla sabit: 32 yemek, 14 mesh, bkz. [24-art-pipeline.md](24-art-pipeline.md).

**v2 değişikliği:** Yemek sayısı mutfak başına 20'den 32'ye çıkarıldı. Malzeme, arketip ve düzenli müşteri sayıları buna göre yeniden boyutlandırıldı. Karakter ve ortam farklılaşması ayrı bir dosyaya alındı: [10-cuisine-identity.md](10-cuisine-identity.md).

---

## Boyutlandırma ilkesi

1. **Günlük menü kararının anlamlı olması.** Her gün menüye 4 ile 8 yemek koyuyorsun. Havuz ne kadar genişse seçim o kadar anlamlı. Otuz iki yemek, menü kapasitesinin dört katı.
2. **Tek kişilik üretimin altından kalkabilmesi.** Yemek modelleri low-poly ve prosedürel üretilebilir olduğu için bu artış taşınabilir. Asıl maliyet model değil, denge.

**Kritik tasarım kararı: paylaşılan taban.** Malzemelerin, personelin ve iş yükseltmelerinin bir kısmı bütün mutfaklarda ortak. İkinci mutfağın maliyeti sıfırdan bir set değil, sadece farkı oluyor.

---

## Sayılarla envanter

### Mutfak başına

| Kalem | v1 | v2 | Not |
|---|---|---|---|
| Yemek | 20 | **32** | Kampanya boyunca kademeli açılır |
| Mutfağa özel malzeme | 18 | **26** | Paylaşılan kilerin üstüne |
| Müşteri arketipi | 6 | **20** | 8'i paylaşılan, 12'si mutfağa özel |
| İsimli düzenli müşteri | 8 | **10** | Her birinin 3-4 sahnelik hikayesi |
| Mutfağa özel ekipman | 8 | **10** | Pişirme istasyonları |
| Gardırop seti | — | **16** | Mutfağa özel kıyafet, yeni |
| Dekor ve mimari kabuk | 1 | **1** | Mutfağın görsel kimliği |

### Paylaşılan taban, bir kez üretilir

| Kalem | Adet |
|---|---|
| Temel kiler malzemesi | 12 |
| Personel rolü | 4 |
| Personel huyu | 12 |
| İş yükseltmesi | 10 |
| Mobilya ve mekân parçası | 24 |
| Mekân genişleme adımı | 4 |
| Karakter gövde ve iskelet | 1 |
| Saç modeli | 8 |
| Cilt tonu | 6 |
| Aksesuar | 10 |

**Temel kiler:** tuz, karabiber, zeytinyağı, ayçiçek yağı, un, soğan, sarımsak, domates, şeker, süt, yumurta, tereyağı.

---

## İlerleme eğrisi: dört mevsim

Kampanya 60 gün, mevsim başına 15 gün. Bütün sistemler ilk üç mevsimde açılır.

| Mevsim | Gün | Açılan sistem | Yemek |
|---|---|---|---|
| **1. Öğrenme** | 1-15 | Menü, fiyat, servis. İlk personel. Temel hal | 6 → 13 |
| **2. Ekip** | 16-30 | İstasyon dizilimi, personel huyları, tedarikçi ilişkisi. İlk genişleme. **İmza mekaniği** | 13 → 21 |
| **3. Büyüme** | 31-45 | Yerleşim düzenleme, itibar, eleştirmen ziyareti. İmza mekaniği derinleşir | 21 → 27 |
| **4. Ustalık** | 46-60 | Rakip restoran, son genişleme. Yeni sistem gelmez | 27 → 32 |

**Tempo:** ortalama iki günde bir yeni yemek, mevsim başına bir büyük sistem.

**İmza mekaniği ikinci mevsimin başında gelir.** Birinci mevsime konursa öğretici yükü çok ağırlaşır, çünkü oyuncu zaten menü ve fiyatı öğreniyor.

---

## Fast food menüsü, 32 yemek

Kombo mekaniği için ana, yan, içecek ve tatlı olarak kurgulandı.

| Grup | Adet | Yemekler |
|---|---|---|
| Ana | 12 | Hamburger, Çizburger, Duble Burger, Acılı Burger, Tavuk Burger, Crispy Tavuk, Balık Burger, Vejetaryen Burger, Hot Dog, Tavuk Dürüm, Et Dürüm, Kaşarlı Tost |
| Yan | 8 | Patates Kızartması, Baharatlı Patates, Soğan Halkası, Nugget, Acılı Kanat, Mozzarella Çubuk, Yeşil Salata, Coleslaw |
| İçecek | 6 | Gazoz, Kola, Limonata, Milkshake, Ayran, Buzlu Çay |
| Tatlı | 6 | Dondurma, Elmalı Turta, Çikolatalı Kek, Donut, Brownie, Waffle |

**Açılış menüsü:** Hamburger, Patates Kızartması, Gazoz, Hot Dog, Nugget, Dondurma.

**Kombo mantığı:** ana artı yan artı içecek bir kombo yapar. Kombo fiyatı tek tek toplamından düşük ama ortalama fiş tutarını yükseltir. Karşılığında mutfak yükü artar.

---

## Türk mutfağı menüsü, 32 yemek

Esnaf lokantası mantığı ve günün yemeği rotasyonu üstüne kurgulandı.

| Grup | Adet | Yemekler |
|---|---|---|
| Sulu yemek | 12 | Kuru Fasulye, Nohut, Etli Türlü, Karnıyarık, İmambayıldı, Taze Fasulye, Musakka, Patlıcan Kebabı, Etli Bamya, Kıymalı Ispanak, Barbunya, Kabak Dolma |
| Çorba | 4 | Mercimek, Ezogelin, Yayla, İşkembe |
| Pilav ve hamur | 5 | Pirinç Pilavı, Bulgur Pilavı, Fırın Makarna, Mantı, Börek |
| Izgara | 5 | Köfte, Tavuk Şiş, Kuzu Pirzola, Adana, Tavuk Kanat |
| Meze ve salata | 3 | Çoban Salata, Cacık, Piyaz |
| Tatlı | 3 | Sütlaç, Kadayıf, Revani |

**Açılış menüsü:** Kuru Fasulye, Pirinç Pilavı, Mercimek Çorbası, Çoban Salata, Köfte, Sütlaç.

**Günün yemeği:** her gün sulu yemeklerden biri günün yemeği olur ve indirimli satılır. Düzenli müşteriler onu bekler. Aynı yemeği üst üste koymak sadakati düşürür.

**Çay:** menüde satılan bir kalem değil, servis kararı. İkram etmek malzeme maliyeti yaratır ama sadakati ve veresiye geri dönüş oranını yükseltir.

---

## Müşteri yapısı

### Yirmi arketip, mutfak başına

Arketip davranış kalıbıdır, kişi değil. Sekizi bütün mutfaklarda paylaşılır, on ikisi mutfağa özeldir. Üç sıklık kademesine ayrılırlar: sık, orta, nadir. Tam liste için bkz. [11-customer-system.md](11-customer-system.md).

### On isimli düzenli müşteri, mutfak başına

Her birinin adı, yüzü, mesleği ve üç ile dört sahnelik hikayesi var. Yeterince iyi hizmet verdikçe açılır.

Araştırmada bu, mobilde en güçlü tutundurma araçlarından biri çıkmıştı. Düzenli müşteriler ayrıca yıl sonu değerlendirmesinin bir eksenini besliyor ve Türk mutfağında veresiye açılan kişiler de bunlar oluyor.

---

## Personel

| Rol | İş |
|---|---|
| Aşçı | Pişirme istasyonu |
| Garson | Sipariş alma ve servis |
| Kasiyer | Ödeme |
| Bulaşıkçı | Temizlik ve tabak döngüsü |

**On iki huy** havuzundan her personele iki tane düşer: hızlı ama dağınık, yavaş ama titiz, kalabalıkta panikleyen, müşteriyle iyi anlaşan, çabuk yorulan, ekip moralini yükselten, ve benzerleri.

Üniforma mutfağa göre değişir. Bkz. [10-cuisine-identity.md](10-cuisine-identity.md).

---

## Mekân genişlemesi

| Kademe | Masa | Açılma |
|---|---|---|
| Başlangıç | 4 | Gün 1 |
| İkinci | 7 | 2. mevsim |
| Üçüncü | 10 | 3. mevsim |
| Dördüncü | 14 | 4. mevsim |

Her genişleme kirayı da artırır. Tavern Master'ın ekonomisinin çökme sebebi büyümenin sabit gideri artırmamasıydı.

---

## Karar bekleyen ayrıntılar

1. Otuz iki yemek yeterli mi, daha da artmalı mı
2. On isimli düzenli müşteri hikaye yazım yükü olarak makul mü
3. Mekân genişlemesi dört kademe mi kalmalı
4. İki menüdeki yemekler onaylanıyor mu

---

## Sonraki adım

Sıradaki iş **ekonomi sayıları ve müşteri formülleri.** Otuz iki yemeğin malzeme maliyetleri, satış fiyatları, hazırlık süreleri; maaşlar, kira, başlangıç sermayesi ve sabır formülleri. Denge aracı ancak onlar yazıldıktan sonra çalışabilir.
