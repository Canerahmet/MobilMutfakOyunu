# Mutfak Sistemi ve Gelir Modeli

**Son güncelleme:** 9 Eylül 2026
**Durum:** Karar verildi, üç şartla. Şartlar aşağıda.

---

## Karar

Oyun başlarken oyuncu bir restoran tarzı seçer. Seçim o kayıt için kalıcıdır ve oyun bitene kadar değişmez.

| Mutfak | Erişim |
|---|---|
| Fast food | Ücretsiz |
| Türk mutfağı | Oyun içi satın alma |
| İtalyan | Oyun içi satın alma |
| Japon mutfağı | Oyun içi satın alma |

Her mutfak sadece yemek listesini değil, **müşteri profilini, ekonomiyi ve karar mekanizmalarını** değiştirir.

**Steam'de farklı:** Steam oyuncuları içerik satın almasına kötü tepki verir. Steam sürümü tek fiyatla satılır ve bütün mutfakları içerir. Port sınırı bu farkı zaten taşıyor, ek mimari iş gerekmiyor.

---

## Neden bu yapı doğru

1. **Araştırmadaki en çok nefret edilen beşinci şey boş son oyundu.** Dört mutfak, oyunu bitirdikten sonra yeniden oynamak için gerçek bir sebep veriyor.
2. **Gelir modeli dürüst.** Güç satmıyoruz, içerik satıyoruz. Araştırmadaki nefret listesinde enerji, sayaç ve seviye kilidi vardı. Bunların hiçbiri yok.
3. **Tek tema seçme sorununu çözüyor.** Belirli bir kimlik önerisi geçerli kalıyor, ama tek kimliğe hapsolmuyoruz.
4. **Türk mutfağı pazarda gerçekten yok.** Ayırt edici bir kanca.

---

## Riski: içerik dört katına çıkıyor

Bu, tek kişilik bir proje için gerçek bir tehlike. Kapsam patlaması, solo projelerin en sık ölüm sebebi.

**Riski taşınabilir kılan şey mimari.** Mutfaklar aynı sistemleri paylaşır, sadece veri olarak farklılaşır. Çekirdek kodu bir kez yazılır, mutfaklar JSON dosyalarında yaşar. Böylece ikinci mutfağın maliyeti kod değil içerik olur.

**Ama sıfır değil.** Her mutfak yeni yemek modelleri, yeni dekor ve yeni diyalog ister. Bu yüzden aşağıdaki birinci şart var.

---

## Üç şart

### Şart 1: İlk sürümde dört mutfak olmayacak

**Çıkışta: ücretsiz fast food artı bir ücretli mutfak.** Diğer ikisi güncelleme olarak gelir.

İlk ücretli mutfak olarak **Türk mutfağı** öneriliyor. Sebepleri:
- En özgün olanı, pazarda benzeri yok
- Sana en yakın olan, referans bulmak ve doğrulamak kolay
- Fast food'dan mekanik olarak en uzak olanı, yani satın almanın değerini en net gösteren

Güncelleme olarak gelen mutfaklar ayrıca tutundurmaya yarar. Araştırmada Dave the Diver'ın en güçlü tarafı sürekli yeni sistem açmasıydı.

### Şart 2: Ücretsiz mutfak tam bir oyun olacak, demo değil

Fast food eksik hissettirirse yorumlar bunu cezalandırır. Araştırmadaki en sert şikayetler ödeme duvarı arkasına saklanan oyunlara geliyordu.

Fast food'un tam bir kampanyası, gerçek bir sonu ve kendi imza mekaniği olacak. Oyuncu hiç para vermeden oyunu bitirip memnun kalabilmeli. Ücretli mutfaklar "asıl oyun" değil, "başka bir oyun" olacak.

### Şart 3: Birden fazla kayıt yuvası olacak

Tarzın kilitlenmesi iyi bir tasarım. Seçimi anlamlı yapıyor ve her mutfağı ayrı bir oyun haline getiriyor.

**Ama tek kayıt yuvası olursa kilit tuzağa dönüşür.** Oyuncu Türk mutfağını satın alır, mevcut fast food oyununu kaybetmeden başlayamaz. Bu iade ve kötü yorum üretir.

Çözüm basit: birden fazla kayıt yuvası. Her yuva kendi mutfağına kilitli, yuvalar birbirinden bağımsız.

Ayrıca ilk birkaç oyun gününde seçim serbestçe sıfırlanabilmeli. Yanlış seçim yapan oyuncu yirmi dakika sonra kapana kısılmamalı.

---

## Bir bağımlılık doğdu: oyunun sonu tanımlanmalı

"Oyun bitene kadar o tarz ile devam edelim" cümlesi, oyunun bittiği bir noktanın olmasını gerektiriyor. Bu, kütükteki **A14 son oyun** maddesini artık zorunlu hale getiriyor.

Öneri: her oyun tanımlı bir sonla biter. Belirli bir itibara ulaşmak, bir hikaye yayını tamamlamak veya restoranı devredip emekli olmak gibi.

Tanımlı son, araştırmadaki boş son oyun şikayetini de kapatır ve mutfak değiştirip yeniden oynamayı doğal hale getirir.

---

## Mutfaklar ne ile farklılaşır

Her mutfak yedi değişkeni değiştirir. Paylaşılan sistemler aynı kalır: gün döngüsü, hal, menü ve fiyat, personel istasyonları, servis, haftalık kira, batma merdiveni, itibar, mekân genişletme.

| Değişken | Ne demek |
|---|---|
| Ritim | Hacim ve marj dengesi |
| Müşteri profili | Kim geliyor, sabrı, harcaması, grup büyüklüğü, zirve saatleri |
| Menü yapısı | Kaç çeşit, hazırlık karmaşıklığı, kurs yapısı |
| Malzeme ekonomisi | Bozulma hızı, fiyat oynaklığı, tedarik |
| **İmza mekaniği** | Sadece o mutfakta olan tek mekanik |
| Mekân ve dekor | Görsel set |
| Personel | İstasyon tipleri |

**İmza mekaniği en önemli satır.** Satın almanın yeniden boyama değil başka bir oyun olduğunu gösteren şey bu.

---

## Dört mutfak

### Fast food (ücretsiz)

- **Ritim:** Yüksek hacim, düşük marj
- **Müşteri:** Sabırsız, hızlı, genç ve aile. Öğle ve akşam zirveleri keskin
- **Menü:** Az çeşit, hızlı hazırlık
- **Malzeme:** Uzun ömürlü, bozulma az, fiyat oynaklığı düşük
- **İmza mekaniği: Kombo ve akış.** Menüyü kombolara bağlarsın. Doğru kombo kurgusu ortalama fiş tutarını yükseltir ama mutfak yükünü artırır. Sipariş kuyruğu hiç durmaz, oyun sürekli akış yönetimidir.

En az değişkenli sistem olduğu için öğretici olarak da doğru yer. Ücretsiz olması bu yüzden doğal.

### Türk mutfağı, esnaf lokantası (ilk ücretli)

- **Ritim:** Orta hacim, düşük ve orta marj, ama sadakat yüksek
- **Müşteri:** Ağırlıklı olarak düzenli müşteri. Esnaf ve çalışanlar. Öğle zirvesi çok güçlü
- **Menü:** Günün yemeği rotasyonu, tencere yemekleri, toplu pişirme
- **Malzeme:** Porsiyon yönetimi, tencere başına maliyet
- **İmza mekaniği: Veresiye ve düzenli müşteri.** Düzenli müşterilere veresiye açarsın. Nakit akışını bozar ama sadakati ve itibarı yükseltir. Kimin ödeyeceği belirsizdir. Ayrıca çay servisi ve porsiyon cömertliği ile marj arasında sürekli bir takas vardır.

Veresiye mekaniği haftalık kira baskısıyla doğrudan çatışır. Bu, oyunun ana gerilimini en sert hissettiren mutfak.

### İtalyan

- **Ritim:** Orta hacim, yüksek marj
- **Müşteri:** Uzun oturan, çift ve aile. Akşam ağırlıklı. Sabırlı ama beklentisi yüksek
- **Menü:** Kurslar. Başlangıç, ana yemek, tatlı. Şarap eşleştirme
- **Malzeme:** Taze ve pahalı, orta bozulma
- **İmza mekaniği: Masa süresi ve kurs zamanlaması.** Müşteri uzun oturur, masa devir hızı düşer. Kursları doğru zamanlamak bahşişi ve itibarı yükseltir; yanlış zamanlama masayı tıkar. Az masayla çok kazanma oyunu.

### Japon mutfağı, ramen dükkânı

- **Ritim:** Çok yüksek devir, orta marj
- **Müşteri:** Hızlı, tek kişilik masa ağırlıklı, tezgâh önünde oturur. Öğle zirvesi güçlü
- **Menü:** Ramen ağırlıklı, hızlı servis, sınırlı yan ürün
- **Malzeme:** Taze malzeme ve her sabah kaynatılan çorba suyu
- **İmza mekaniği: Çorba suyu ve tükenme.** Sabah kaç porsiyonluk suyu kaynatacağına karar verirsin. Az yaparsan gün ortasında tükenir ve dükkânı erken kapatırsın; müşteri kaybedersin ama israf olmaz. Çok yaparsan artan su ertesi güne kalmaz, doğrudan zarardır.

Bu mekanik gerçek ramen dükkânlarının işleyişinden geliyor: su bitince dükkân kapanır. Tek bir sabah kararının bütün günü belirlemesi, oyunun en keskin tahmin anını yaratıyor ve diğer üç mutfaktan tamamen ayrılıyor.

---

## Fiyatlandırma

- Her mutfak ayrı satın alınabilir.
- Hepsini içeren bir paket, tek tek almaya göre indirimli.
- Satın alma öncesi oyuncu neyi aldığını görebilmeli. Sadece isim değil, imza mekaniği ve müşteri profili farkı gösterilmeli.
- Kesin rakamlar kütükteki D2 maddesinde, henüz yazılmadı.

---

## Bu kararın kapattığı kütük maddeleri

| Madde | Eski durum | Yeni durum |
|---|---|---|
| D1 Gelir modeli | Karar bekliyor | ✅ İçerik satın alması, güç değil |
| D3 İlk sürüm kapsamı | Karar bekliyor | ✅ Mekân genişletme dahil, ikinci şube hariç, iki mutfak |
| D4 Tema ve mutfak kimliği | Karar bekliyor | ✅ Dört mutfak, çıkışta iki tanesi |
| A14 Son oyun | Yazılmadı | ⚠️ Artık zorunlu, bu karara bağımlı |
