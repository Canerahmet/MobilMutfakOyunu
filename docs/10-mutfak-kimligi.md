# Mutfak Kimliği: Karakter ve Ortam Farklılaşması

**Son güncelleme:** 9 Eylül 2026
**Kütük maddesi:** A5 ve A13 ile bağlantılı, yeni pillar
**Durum:** Öneri hazır, karar bekliyor. Dördüncü mutfak 9 Eylül 2026'da Japon olarak netleşti.

---

## Amaç

Mutfak değişince oyuncu **başka bir yere girmiş gibi hissetmeli.** Sadece menü değil, mekân, insanlar, ışık, ses ve günün ritmi değişmeli.

Bu doğru bir hedef ve satın alma kararını doğrudan besliyor. Mutfak satın alması bir renk değişimi gibi hissedilirse kimse ikincisini almaz. Ortam gerçekten değişirse her mutfak ayrı bir oyun olur.

---

## Yaklaşım düzeltmesi: farklılaşma yüzde olmaz

Karakterleri yüz hatlarıyla etnik olarak ayırmak bu oyunda yanlış araç. İki sebebi var.

**Birincisi teknik ve belirleyici.** Kırk derecelik sabit kamerada ve telefon ekranında bir karakter yaklaşık 60 ile 120 piksel yüksekliğinde görünüyor. Göz o ölçekte iki üç piksel eder. Yüz hattı zaten okunmuyor. Bu yolla farklılaşma denesek bile görsel olarak çalışmaz, sadece yakın plan diyalog portrelerinde görünür.

**İkincisi ticari.** Etnik karikatüre kayan tasvir mağaza içerik politikalarına takılabilir ve inceleme bombardımanı riski taşır. Global yayın hedefleyen bir oyunda bu, sonradan düzeltmesi çok pahalı bir hata.

**İyi haber:** o ölçekte gerçekten okunan araçlar zaten çok daha güçlü. Siluet, kıyafet, renk, mekân, ışık ve ritim. Aşağıdaki sistem bunların üstüne kuruldu.

---

## Ne gerçekten okunuyor

Uzak kamerada ve küçük ekranda oyuncunun ayırt edebildiği şeyler, önem sırasıyla:

1. **Mekânın kabuğu.** Duvar, zemin, tavan, mobilya dizilimi. Ekranın çoğunu bu kaplıyor.
2. **Işık ve palet.** Parlak floresan mı, sıcak öğleden sonra ışığı mı, akşam feneri mi.
3. **Siluet.** Baret, sırt çantası, şapka, çanta, önlük. Uzaktan tanınan şey bunlar.
4. **Kıyafet rengi ve deseni.**
5. **Kalabalığın ritmi.** Ne zaman doluyor, ne zaman boşalıyor.
6. **Ses.** Fritöz mü, çay bardağı mı, kaynayan çorba kazanı mı.

Yüz, bu listenin dışında. Yüz sadece diyalog portrelerinde iş görür ve orası zaten elle tasarlanan isimli karakterlerin alanı.

---

## Karakter görünüm sistemi

Tek bir paylaşılan gövde ve iskelet üstüne kurulu bir giydirme sistemi. Yapay zeka destekli üretimde tek modelin her açıdan tutarlı olması avantajını korur.

| Değişken | Adet | Kapsam |
|---|---|---|
| Gövde ve iskelet | 1 | Paylaşılan |
| Vücut tipi | 3 | Paylaşılan |
| Saç modeli | 8 | Paylaşılan |
| Cilt tonu | 6 | Paylaşılan, **her mutfakta tamamı kullanılır** |
| Kıyafet seti | 16 | **Mutfağa özel.** Yirmi arketibin siluetten tanınması için |
| Aksesuar | 10 paylaşılan + mutfağa özel | Baret, çanta, şapka, gözlük, önlük |

**Kombinasyon sayısı:** 3 × 8 × 6 × 16 × 10 ile yirmi üç binin üstünde farklı görünüm. Küçük bir varlık setinden kalabalık ve çeşitli bir restoran çıkıyor.

**Cilt tonu neden her mutfakta tam yelpaze:** gerçek restoranlarda müşteri karışıktır. Her mutfağı tek tip bir nüfusa indirgemek hem gerçek dışı olur hem de yukarıdaki karikatür riskine kapı açar. Çeşitlilik ücretsiz gelir, çünkü aynı altı ton zaten üretilmiş oluyor.

---

## Dört mutfağın kimliği

| | Fast food | Türk mutfağı | İtalyan | Japon mutfağı |
|---|---|---|---|---|
| **Mimari kabuk** | Plastik oturma grupları, tezgâh üstü sipariş, fayans zemin, neon menü panosu, cam cephe | Ahşap sandalye, masa örtüsü, girişte buharlı sıcak tezgâh, duvarda takvim, köşede televizyon, çay ocağı | Beyaz masa örtüsü, şarap rafı, tuğla duvar, sarkıt aydınlatma, ahşap bar | Ahşap lata duvar, kâğıt fener, girişte noren perdesi, tezgâh oturma düzeni, açık mutfakta kaynayan çorba kazanları ve erişte haşlama bölümü |
| **Palet** | Yüksek doygunluk, kırmızı ve sarı, beyaz zemin | Toprak tonları, pirinç, koyu ahşap, krem | Derin yeşil, krem, şarap kırmızısı | Koyu ahşap, kırmızı fener aksanı, serin gölgeler |
| **Işık** | Parlak floresan, gölge az, gün ortası | Sıcak öğleden sonra, pencereden eğik ışık | Mum sıcaklığı, akşam, güçlü kontrast | Akşam, fener aksanları, kazanlardan yükselen buharın içinden geçen ışık |
| **Personel kıyafeti** | Kep, renkli önlük, tişört | Beyaz ceket veya gömlek, uzun önlük | Yelek, kravat, uzun önlük | Kısa ceket, bandana |
| **Müşteri siluetleri** | Öğrenci sırt çantası, ofis çalışanı, kurye çantası, aile | Esnaf önlüğü, inşaat bareti, memur ceketi, emekli şapkası | Şık palto, çanta, çiftler | Tezgâhta tek kişilik oturanlar, öğrenci, öğle molası çantası |
| **Günlük ritim** | İki keskin zirve, öğle ve akşam | Çok güçlü öğle zirvesi, akşam sakin | Öğle sakin, uzun akşam | Güçlü öğle zirvesi, orta akşam |
| **Ortam sesi** | Fritöz cızırtısı, kasa bipleri, pop müzik | Çay bardağı şıngırtısı, kepçe sesi, radyo | Çatal bıçak, kadeh, hafif akustik | Erişte süzme sesi, kazan fokurtusu, buhar, kısa siparişler |

Bu tablo, oyuncunun mutfak değiştirdiğinde neyi fark edeceğinin listesi. Hiçbiri yüzle ilgili değil ve hepsi uzak kamerada okunuyor.

---

## Karakter sayımı: kaç kişi var

"On isimli karakter dışında kaç karakter var" sorusunun cevabı katmanlı. Karakterler üç gruba ayrılıyor.

### 1. Elle tasarlanan karakterler

Bunlar tek tek yazılıp tasarlanıyor. Adı, yüzü, mesleği ve hikayesi var.

| Grup | Mutfak başına | Not |
|---|---|---|
| İsimli düzenli müşteri | 10 | Her birinin 3-4 sahnelik hikayesi |
| İsimli personel | 3 | **Yeni öneri.** Aşağıda gerekçesi |
| **Mutfak başına toplam** | **13** | |

| Kapsam | Elle tasarlanan karakter |
|---|---|
| Çıkışta, iki mutfak | 26 |
| Artı oyuncunun kendisi | 27 |
| Dört mutfak tamamlandığında | 53 |

**İsimli personel önerisi:** araştırmada Tavern Keeper'ın en çok övülen tarafı personelin karakterli olmasıydı. Personel, oyuncunun her gün gördüğü ve bağ kurduğu kişi. Mutfak başına üç tanesinin elle tasarlanması, küçük bir maliyetle büyük duygusal getiri sağlıyor. Kalan personel üretiliyor.

### 2. Davranış şablonları

| Grup | Mutfak başına |
|---|---|
| Müşteri arketipi | 20, sekizi paylaşılan |
| Personel rolü | 4, paylaşılan |
| Personel huyu | 12, paylaşılan |

Arketip bir kişi değil bir kalıp. Sabrı, harcamayı, grup büyüklüğünü ve geliş saatini belirliyor. Görünüm giydirme sisteminden geliyor.

### 3. Üretilen kalabalık

Sayı sınırlı değil. Giydirme sistemi mutfak başına on yedi binden fazla farklı görünüm üretiyor, yani oyuncu pratikte hiç aynı kişiyi iki kez görmüyor.

| Ölçü | Değer |
|---|---|
| Ekranda aynı anda | 6 ile 20 karakter, masa sayısına bağlı |
| Bir kampanyadaki toplam müşteri ziyareti | Yaklaşık 2.300 |
| Aynı anda çalıştırılabilen personel | Başta 1-2, sonda 6-8 |
| İşe alım ekranında gösterilen aday | 3, yenilenir |
| Kampanya boyunca görülen aday | Yaklaşık 25-30 |

Üretilen personelin de adı, iki huyu ve görünümü oluyor. Yani isimsiz değiller, sadece elle yazılmış hikayeleri yok.

---

## Özgüllük kişide olur, grupta değil

İsimli düzenli müşteriler kültürel özgüllüğün doğru yeri.

**Neden:** bir grubu genelleyen tasvir hem klişe hem riskli. Ama adı, mesleği, alışkanlığı ve derdi olan tek bir kişi hem güvenli hem çok daha akılda kalıcı.

Türk mutfağında karşı dükkânın sahibi, her gün aynı saatte gelip aynı masaya oturan, kuru fasulyeyi seven, ay sonu sıkışınca veresiye defterine yazılan bir esnaf. Bu kişi bir karakter. "Türk müşteri" ise bir kategori ve kimsenin ilgisini çekmez.

Aynı ilke dört mutfakta da geçerli. On isimli karakterin her biri elle tasarlanır, kalan kalabalık yukarıdaki giydirme sisteminden üretilir.

---

## Kapandı: dördüncü mutfak Japon oldu

"Uzak Doğu" bir mutfak değil, bir kıta parçasıydı. Japon, Çin, Kore ve Tayland mutfakları mimari, yemek ve servis ritüeli olarak tamamen farklı.

**Karar: Japon mutfağı, ramen dükkânı olarak.** Bu seçim somut mimari, somut yemek listesi ve somut bir servis ritmi getiriyor. Tezgâh oturma düzeni, açık mutfak, tek kişilik müşteri ağırlığı ve çok hızlı devir hepsi oradan doğal olarak çıkıyor.

İmza mekaniği de bu seçimle keskinleşti: **çorba suyu ve tükenme.** Gerçek ramen dükkânlarında su bitince dükkân kapanır.

Aynı mantık İtalyan için de geçerli ve orada "trattoria" zaten yeterince spesifik bir çerçeve.

---

## Üretim maliyeti

| Kalem | Mutfak başına yeni iş |
|---|---|
| Mimari kabuk ve dekor | 1 set |
| Kıyafet | 16 parça |
| Aksesuar | Birkaç mutfağa özel parça |
| Palet ve ışık ayarı | 1 profil |
| Ortam sesi | 3-4 katman |
| İsimli karakter tasarımı | 13 kişi: 10 müşteri, 3 personel |

Gövde, iskelet, animasyon, saç ve cilt tonları paylaşılan tabandan geliyor. Yani mutfak başına ek karakter maliyeti model değil **kıyafet ve tasarım.** Bu, dört mutfak fikrini tek kişilik üretimde ayakta tutan şey.

---

## Karar bekleyen ayrıntılar

1. Mutfak başına on altı kıyafet yeterli mi
2. Ortam sesi katmanları bu kapsamda mı kalmalı
3. Elle tasarlanan personel karakteri önerisi kabul ediliyor mu
