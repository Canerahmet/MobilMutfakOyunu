# Müşteri Sistemi: Arketipler

**Son güncelleme:** 9 Eylül 2026, v2
**Kütük maddesi:** A7 müşteri sistemi detayı, birinci yarısı
**Durum:** Arketipler yazıldı. Formüller ekonomi çalışmasıyla birlikte gelecek.

**v2 değişikliği:** Arketip sayısı mutfak başına 8'den **20'ye** çıkarıldı. Karışmalarını önlemek için sıklık kademesi eklendi ve bir kısmı paylaşılan hale getirildi.

---

## Arketip nedir

**Arketip bir kişi değil, bir davranış şablonudur.**

Kapıdan bir müşteri girdiğinde oyun şunu yapar:

1. Saate, mutfağa ve sıklık kademesine göre bir arketip seçer.
2. O arketipin parametrelerini kopyalar.
3. Arketipin işaret ettiği gardırop alt kümesinden ona bir görünüm üretir.
4. Sahneye koyar.

Arketip görünmez. Oyuncunun gördüğü şey, o şablondan üretilmiş benzersiz görünümlü bir insan.

**İsimli düzenli müşteriden farkı:** isimli müşteri tek bir kişidir, elle yazılmıştır, hikayesi vardır ve hep aynı kişidir. Arketip ise binlerce müşteri üretir.

---

## Sekiz parametre

| Parametre | Ne yapar |
|---|---|
| **Sabır** | Ne kadar bekleyince sinirlenip çıkıp gider |
| **Harcama eğilimi** | Kaç kalem sipariş eder, ne kadar bırakır |
| **Fiyat duyarlılığı** | Fiyat piyasa üstündeyse ne kadar rahatsız olur |
| **Grup büyüklüğü** | Kaç kişilik masa gerekir |
| **Geliş saati** | Gün içinde ne zaman gelme ihtimali yüksek |
| **Sipariş tercihi** | Menünün hangi grubuna yönelir |
| **İtibar ağırlığı** | Memnuniyeti veya şikayeti itibarı ne kadar oynatır |
| **Düzenli olma eğilimi** | Tekrar gelme ve düzenli müşteriye dönüşme ihtimali |

**Kritik bağlantı:** arketip aynı zamanda giydirme sistemine hangi kıyafet ve aksesuarların seçileceğini söyler. İnşaat işçisi arketipi baret ve iş kıyafeti çeker. Böylece salona baktığında kimin oturduğunu siluetten anlarsın. Davranış ve görünüm birbirine bağlı, ve mutfak kimliğini yaratan şey bu bağ.

---

## Sıklık kademesi: yirmi arketip neden karışmıyor

Çeşitlilik arttıkça arketiplerin birbirine benzeme riski doğar. Bunu **sıklık kademesi** çözüyor.

| Kademe | Adet | Trafik payı | İşlevi |
|---|---|---|---|
| **Sık** | 8 | Yaklaşık %70 | Günün omurgası. Oyuncu bunları tanır ve ona göre plan yapar |
| **Orta** | 8 | Yaklaşık %25 | Doku. Her gün birkaçı gelir, günü tekdüzelikten çıkarır |
| **Nadir** | 4 | Yaklaşık %5 | Olay. Kampanya boyunca birkaç kez gelir, o günün şeklini değiştirir |

Nadir arketipler tasarım olarak **olay** niteliğinde. Yemek eleştirmeni geldiği gün oyun bambaşka oynanır. Toplu sipariş geldiği gün mutfak kilitlenir. Bunlar hatırlanan anlar üretir.

---

## Paylaşılan sekiz arketip

Bunlar her mutfakta çalışır, sadece kıyafetleri o mutfağa göre değişir. Bir kez yazılır.

| Arketip | Kademe | Öne çıkan özellik |
|---|---|---|
| Yalnız müşteri | Sık | Tek kişi, orta sabır, hızlı devir |
| Çift | Sık | İki kişi, orta sabır, orta harcama |
| Aile | Sık | 3-5 kişi, sabır yüksek, harcama yüksek |
| Kurye, paket alan | Sık | Sabır çok düşük, tek kalem, masa işgal etmez |
| Çocuklu ebeveyn | Orta | Sabır düşük, tatlı siparişi kesin, masa süresi uzun |
| Yolcu | Orta | Tek seferlik, düzenli olmaz, itibar ağırlığı düşük |
| Paylaşımcı | Orta | İtibar ağırlığı üç katı, hem övgü hem şikayet büyür |
| Yemek eleştirmeni | Nadir | Kalite beklentisi çok yüksek, itibar ağırlığı en yüksek |

---

## Fast food'a özel on iki arketip

| Arketip | Kademe | Öne çıkan özellik |
|---|---|---|
| Aceleci öğrenci | Sık | Sabır düşük, bütçe dar, kombo alır |
| Ofis grubu | Sık | 2-4 kişi, öğle zirvesi, hızlı servis bekler |
| Antrenman sonrası | Sık | Yüksek harcama, porsiyon odaklı, akşam |
| Alışveriş molası | Sık | Orta sabır, tatlı ve içecek ekler |
| Pazarlıkçı | Orta | Fiyat duyarlılığı çok yüksek, piyasa üstünde hiç gelmez |
| Geç saat müşterisi | Orta | Sabır yüksek, kapanışa yakın, orta harcama |
| Maç grubu | Orta | Kalabalık, gürültülü, yüksek harcama, uzun oturur |
| Diyet yapan | Orta | Salata ve vejetaryen arar, seçenek yoksa çıkar gider |
| Gece vardiyası | Orta | Kapanış saatinde, tek kişi, sadık olabilir |
| Doğum günü grubu | Nadir | Çok kalabalık, tatlı ağırlıklı, tek seferde büyük gelir |
| Şikayetçi müşteri | Nadir | Memnun etmesi çok zor, itibar riski taşır |
| Toplu sipariş | Nadir | Tek seferde devasa sipariş, mutfağı kilitler |

---

## Türk mutfağına özel on iki arketip

| Arketip | Kademe | Öne çıkan özellik |
|---|---|---|
| Esnaf komşu | Sık | Düzenli olma eğilimi en yüksek. Veresiye defterinin ana adayı |
| Öğle molası çalışanı | Sık | Sabır çok düşük, hızlı servis şart, keskin öğle zirvesi |
| İnşaat işçisi | Sık | Porsiyon beklentisi yüksek, orta harcama, öğle |
| Memur | Sık | Günün yemeğini bekler, sabır yüksek, düzenli olur |
| Emekli | Orta | Sabır çok yüksek, harcama düşük, çay ikramına çok duyarlı |
| Öğrenci | Orta | Bütçe dar, porsiyon önemli, fiyat duyarlılığı yüksek |
| Hafta sonu ailesi | Orta | Harcama yüksek, masa süresi uzun |
| Uzun yol şoförü | Orta | Tek seferlik, hızlı, doyurucu arar |
| Titiz müşteri | Orta | Memnuniyet eşiği yüksek, itibar ağırlığı yüksek |
| Mahalle toplu yemeği | Nadir | Çok kalabalık grup, önceden haber verir, tek günde büyük hacim |
| Denetim görevlisi | Nadir | Temizlik ve düzeni denetler, sonucu itibarı sert oynatır |
| Eski müşteri | Nadir | Uzun süredir gelmemiş biri. İyi ağırlanırsa düzenliye döner |

---

## Arketipler mutfak kimliğini nasıl taşıyor

Aynı yirmi slot, mutfağa göre farklı doluyor ve sıklık dağılımı da değişiyor.

- **Fast food:** kalabalık genç ve aceleci. Sabır ortalaması düşük, masa süresi kısa, devir yüksek. İki keskin zirve.
- **Türk lokantası:** düzenli müşteri ağırlıklı. Sabır ortalaması yüksek, öğle zirvesi çok sert, akşam neredeyse boş.
- **İtalyan:** akşam gelir ve uzun oturur. Masa devri düşük, harcama yüksek.
- **Japon ramen dükkânı:** tek kişilik ve çok hızlı. Sabır düşük ama masa süresi de çok kısa.

Menü değişmesi tek başına bunu vermez. Günün oynanış hissini belirleyen şey arketip dağılımı.

---

## Üretim maliyeti

Arketip **veri, model değil.** Sekiz parametre ve bir gardırop işareti. Yani yeni arketip eklemek ucuz.

Ama sıfır değil: yirmi arketibi görsel olarak ayırt edilebilir kılmak için gardırop genişledi.

| Kalem | v1 | v2 |
|---|---|---|
| Arketip, mutfak başına | 8 | 20 |
| Bunun paylaşılan kısmı | 0 | 8 |
| Mutfak başına yeni yazılacak | 8 | 12 |
| Kıyafet seti, mutfak başına | 12 | **16** |

Kıyafet setinin on altıya çıkması, arketiplerin siluetten tanınabilmesi için gerekli. Yeni kombinasyon sayısı: 3 × 8 × 6 × 16 × 10 ile yirmi üç binin üstünde.

---

## Henüz yazılmayan kısım

Parametrelerin sayısal karşılığı yok. "Sabır düşük" şu an sadece bir kelime.

Ekonomi çalışmasıyla birlikte şunlar yazılacak:

1. Sabır saniye cinsinden ne kadar, ve nasıl azalıyor
2. Fiyat duyarlılığı formülü: piyasa üstü yüzde kaç, memnuniyet ne kadar düşüyor
3. Memnuniyetin itibara çevrilmesi
4. İtibarın ertesi gün kaç müşteri getirdiği
5. Yirmi arketibin saate göre dağılım tablosu
6. Sıklık kademelerinin gerçek olasılıkları

Bunlar A7'nin ikinci yarısı ve A4 ekonomi sayılarıyla birlikte yazılacak.
