# Üretim Planı: Ne, Ne İle Yapılacak

**Son güncelleme:** 9 Eylül 2026
**Amaç:** "Şunu ne ile yapacağız" sorusunun her kalem için cevabını yazmak. Uygulamaya geçmeden önce boşluk kalmasın.

---

## Önce sınır: ben neyi üretebilirim, neyi üretemem

Bu, planın en önemli maddesi. "Tamamen yapay zeka ile yapalım" dediğinde bunun ne anlama geldiğini net koymak gerekiyor.

### Doğrudan üretebildiklerim

Ben metin üretirim. Bu göründüğünden çok daha geniş bir alan:

- **Bütün C# kodu.** Çekirdek simülasyon, Unity tarafı, editör araçları, testler.
- **Blender Python betikleri.** Bir betik çalıştırıldığında low-poly mobilya üretir. Aşağıda ayrıntısı var.
- **Unity editör araçları.** Sahne yerleştirme, prop dizme, veri içe aktarma.
- **Shader kodu.** Düz gölgeleme, kenar çizgisi, gün batımı ışığı.
- **İçerik verisi.** Tarifler, malzemeler, fiyatlar, personel arketipleri, müşteri tipleri. JSON olarak.
- **Metin içeriği.** Diyaloglar, müşteri yorumları, karakter hikayeleri, arayüz metinleri, yerelleştirme dosyaları.
- **Denge aracı ve analizi.** Simülasyonu çalıştırıp sonucu yorumlamak.
- **Mağaza metinleri, gizlilik politikası taslağı, yardım dokümanları.**

### Doğrudan üretemediklerim

- **3B model dosyası.** Mesh'i doğrudan çıkaramam. Ama onu üreten betiği yazabilirim.
- **Görsel.** Karakter portresi, doku, ikon, kapak görseli.
- **Ses.** Müzik, efekt, seslendirme.
- **Web arayüzü kullanmak.** Meshy, Tripo, Mixamo gibi servislere ben giremem. Onları sen çalıştırırsın.
- **Gerçek cihazda test.** Telefonda çalıştırıp bakmak sende.
- **Mağazaya yükleme.** Hesap ve imzalama işlemleri sende.

**Sonuç:** kod ve tasarım tarafı bende, görsel ve ses üretimi araç artı senin operasyonun. Aşağıdaki plan bu ayrıma göre kurgulandı.

---

## 3B modeller

Restoran iç mekânı şanslı bir konu: eşyaların çoğu kutu ve silindir. Bu, üç yollu bir strateji sağlıyor.

### Yol 1 — Prosedürel üretim (birincil öneri)

Blender Python betikleri yazarım. Betik, tek bir stil yapılandırmasından bütün mobilya setini üretir.

**Kapsadıkları:** masa, sandalye, tabure, tezgâh, raf, dolap, ocak, buzdolabı, tabak, bardak, tencere, tepsi, kapı, pencere çerçevesi, lamba, tabela, sandık.

**Neden bu yol birincil:**
- **Stil tutarlılığı garanti.** Hepsi aynı kenar yumuşatma, aynı ölçek, aynı palet.
- **Ücretsiz.** Tekrarlayan abonelik yok.
- **Sürüm kontrolüne girer.** Model bir dosya değil, bir betik. Stili değiştirmek istersen betiği değiştirip hepsini yeniden üretirsin.
- **Ölçek serbest.** Otuz çeşit sandalye lazımsa parametre değiştirip üretirsin.

**Kapsamadıkları:** insan karakterleri, bitkiler, organik yemek detayı.

### Yol 2 — CC0 hazır paketler (prototip ve boşluk doldurma)

Kenney ve Quaternius gibi kaynaklar binlerce low-poly modeli CC0 lisansıyla veriyor. Ticari kullanım serbest, atıf gerekmiyor.

**Kullanımı:** dikey dilimde hız, ve prosedürel yolun kapsamadığı organik nesneler.
**Riski:** tanınabilir bir görünüm. Çok sayıda oyun aynı paketleri kullanıyor. Nihai sürümde kimlik sorunu yaratabilir.

### Yol 3 — Ücretli paketler ve yapay zeka üretimi (seçili nesneler)

- **Synty POLYGON** serisinin Shops ve Town paketleri konumuza yakın. Profesyonel ve tutarlı, ücretli.
- **Tripo** aylık yaklaşık 12 dolar, **Meshy Pro** aylık 20 dolar. Metinden veya görselden model üretir.

**Kritik uyarı:** Meshy ve Tripo'nun **ücretsiz katmanları ticari kullanıma kapalı.** Oyunda kullanacaksan ücretli plan şart. Ayrıca üretilen mesh'lerin topolojisi genelde yoğun çıkıyor ve oyun için sadeleştirme istiyor.

**Kullanımı:** benzersiz olması gereken tekil nesneler. Dükkânın tabelası, özel bir ocak, hikayeye ait bir eşya.

---

## Karakterler ve animasyon

**Bu, projenin en zor üretim kalemi.** Mobilya betikle çözülür, insan çözülmez.

### Tasarımla sorunu küçültmek

Önerim, karakterleri baştan animasyon maliyetini düşürecek şekilde tasarlamak:

- Basit oranlar, ayrı parmak yok
- Yüz detayı minimum, nokta gözler
- Az sayıda paylaşılan animasyon klibi: yürüme, oturma, yeme, bekleme, konuşma, sevinme, sinirlenme

**Çeşitlilik modelden değil giydirmeden gelir.** Tek gövde modeli üstünde renk, saç, şapka, gözlük, çanta değişimiyle otuz farklı müşteri elde edilir. Bu, hem üretimi hem hafızayı düşürür ve mobilde önemli bir kazanç.

### İskeletleme ve animasyon yolları

| Yol | Durum | Not |
|---|---|---|
| **Mixamo** | Ücretsiz, ticari kullanım serbest | Otomatik iskelet ve geniş animasyon kütüphanesi. **Risk:** Adobe yıllardır güncellemiyor, 2025'te çok günlük kesinti yaşandı ve destek "artık desteklenmiyor" diyor. Yedek plan şart. |
| **Tripo otomatik iskeletleme** | Ücretli planla | Model üretimiyle aynı serviste |
| **Synty karakterleri** | Ücretli | İskeleti hazır gelir, Mixamo ile uyumlu |
| **Unity insansı sistemi** | Ücretsiz | İskelet varsa animasyon paylaşımı zaten çalışır |

**Karar:** Mixamo'ya tek başına bel bağlanmaz. Karakter modelleri standart insansı iskelete uygun üretilir, böylece animasyon kaynağı değişse de model değişmez.

---

## Malzeme ve doku

Low-poly'nin en büyük kolaylığı burada. **Neredeyse hiç doku gerekmiyor.**

- Tek bir palet atlası kullanılır. Bütün nesneler bu atlastan renk alır.
- Bu atlası kod ile üretebilirim. Palet bir veri dosyasında yaşar, betik görüntüyü üretir.
- Alternatif olarak vertex color kullanılır ve doku tamamen ortadan kalkar.

**Sonuç:** doku üretimi bir sorun değil, çözülmüş kabul edilebilir.

**Görsel kimlik buradan gelir:** araştırmada low-poly'nin tek zayıf tarafı jenerik görünme riskiydi. Kapatma yolu palet, ışık ve gölge. Bunların hepsi kod ve ayar işi, sanat yeteneği işi değil. Bu bizim lehimize.

---

## Ses

| Kalem | Yol | Maliyet |
|---|---|---|
| Müzik | ElevenLabs Music veya Suno, ücretli plan | Aylık abonelik |
| Ses efekti | ElevenLabs SFX, ücretli planda telifsiz ve ticari kullanım serbest | Aylık abonelik |
| Alternatif | Freesound ve benzeri CC0 kaynaklar | Ücretsiz |
| Konuşma | Yok. Anlamsız hece sesi önerilir | Çok ucuz, karakter katar |

**Uyarı:** Suno tarafında sahiplik dili değişti. Yazarlığı Suno'da tutup sana kalıcı ticari lisans veriyor. Ücretsiz katmanlar ticari kullanıma uygun değil. Yayın öncesi kullandığın planın şartlarını doğrula.

Seslendirme yerine anlamsız hece kullanmak hem maliyeti sıfırlar hem yerelleştirmeyi kolaylaştırır.

---

## Arayüz, ikon ve yazı tipi

- **Arayüz düzeni:** kod, bende.
- **İkonlar:** Kenney'nin CC0 arayüz paketleri veya üretilmiş ikonlar.
- **Yazı tipi:** Türkçe karakter desteği şart. Google Fonts üzerindeki SIL Open Font License lisanslı aileler ticari kullanıma uygun. Seçim yaparken ı, İ, ğ, ş, ç, ö, ü karakterlerinin varlığı kontrol edilir.
- **Karakter portreleri:** pixel art düşünülüyorsa görsel üretim aracı ile, senin operasyonun.

---

## Kod, altyapı ve araçlar

| Kalem | Yol | Maliyet |
|---|---|---|
| Motor | Unity Personal | Ücretsiz, yıllık 200 bin dolar gelir sınırına kadar |
| Açılış ekranı | Unity 6 ile Personal'da kaldırılabiliyor | Ücretsiz |
| Sürüm kontrolü | Git, ikili dosyalar için Git LFS | Ücretsiz |
| Depo | GitHub özel depo | Ücretsiz |
| Derleme | Başta yerel derleme | Ücretsiz |
| Test | Çekirdek için birim testleri, bende | Ücretsiz |
| Cihaz testi | En az bir düşük seviye Android telefon | Donanım |

Git LFS ayarı baştan yapılmalı. Model ve ses dosyaları normal Git'e girerse depo kısa sürede şişer ve geri dönüşü zahmetli olur.

---

## Servisler

Bunların hepsi port arayüzlerinin arkasında duracak, yani seçim mimariyi etkilemiyor.

| Kalem | Mobil | Steam |
|---|---|---|
| Analitik | Unity Analytics veya Firebase | İsteğe bağlı |
| Reklam | Gelir modeline bağlı | Yok |
| Satın alma | Unity IAP | Steam üzerinden |
| Bulut kayıt | iCloud, Google Play | Steam Cloud |

---

## Yerelleştirme

- Unity Localization paketi kullanılır.
- En az Türkçe ve İngilizce. Metinler baştan dosyalarda tutulur, koda gömülmez.
- Sonradan dil eklemek, baştan yapıya uymaktan çok daha pahalıdır.
- Çeviri metinlerini ben üretebilirim.

---

## Mağaza ve yayın

| Kalem | Yol |
|---|---|
| Ekran görüntüleri | Oyundan alınır, senin operasyonun |
| Tanıtım videosu | Kayıt ve kurgu, senin operasyonun |
| Uygulama ikonu | Görsel üretim aracı |
| Mağaza metinleri | Bende |
| Sürüm imzalama | Sende |

---

## Hukuki

- **Gizlilik politikası** zorunlu. Analitik veya reklam varsa mutlaka. Taslağını yazabilirim, ama nihai metni bir hukukçuya okutmak doğru olur.
- **KVKK ve GDPR** uyumu. Veri toplanıyorsa açık rıza akışı gerekir.
- **Yapay zeka ile üretilen varlıkların lisans denetimi.** Yayın öncesi her varlığın kaynağı ve lisansı bir tabloda listelenmeli. Ücretsiz katmanla üretilmiş bir modelin oyuna girmesi ciddi risk.
- **Yaş derecelendirmesi** başvuruları.

---

## Maliyet tablosu

### Zorunlu

| Kalem | Tutar |
|---|---|
| Google Play geliştirici hesabı | 25 dolar, tek seferlik |
| Apple geliştirici programı | Yıllık 99 dolar |
| Unity Personal | 0 |
| Git ve GitHub | 0 |
| **Mobil için ilk yıl toplamı** | **124 dolar** |

### Steam eklenirse

| Kalem | Tutar |
|---|---|
| Steam Direct | Oyun başına 100 dolar, 1000 dolar gelirden sonra iade ediliyor |

### İsteğe bağlı üretim araçları

| Kalem | Tutar |
|---|---|
| Tripo | Aylık yaklaşık 12 dolar |
| Meshy Pro | Aylık 20 dolar |
| ElevenLabs ses | Aylık abonelik |
| Synty model paketleri | Paket başına değişken |
| Kenney, Quaternius, Mixamo | 0 |

**Not:** komisyon oranları ayrı. Apple ve Google yıllık 1 milyon doların altında yüzde 15, üstünde yüzde 30 alıyor. Steam ilk 10 milyon dolara kadar yüzde 30 alıyor.

**En düşük senaryo:** CC0 paketler ve prosedürel üretimle, ilk yıl 124 dolarla mobilde yayına çıkmak mümkün.

---

## Bu dosyada karar bekleyenler

1. Modellerde birincil yol prosedürel mi, hazır paket mi olacak
2. Ses için ücretli yapay zeka aracı mı, CC0 kaynak mı
3. Karakter portreleri pixel art olacak mı
4. Test için hangi cihaz alınacak

---

## 9 Eylül 2026 güncellemesi: cevaplar sonrası

Değerlendirmenin sorularına verilen cevaplar ([22-answers-and-direction.md](22-answers-and-direction.md)) bu dosyanın üç varsayımını değiştirdi:

| Varsayım | Eski | Yeni |
|---|---|---|
| Platform | Android + iOS | **Sadece Android.** Mac yok, iOS Mac olunca |
| İlk yıl maliyeti | 124 $ | **25 $** (Google Play). Apple 99 $ ödenmiyor |
| Blender kullanımı | "Sen çalıştırırsın" | **Ben çalıştırırım**, Claude Code uzaktan kontrolle. Render'ı da ben görüyorum. Bkz. [24-art-pipeline.md](24-art-pipeline.md) |

"Doğrudan üretemediklerim" listesindeki **3B model dosyası** maddesi yumuşadı: mesh'i hâlâ elle çizemem, ama betiğin ürettiğini render edip görebiliyorum ve düzeltebiliyorum. Döngü kapalı. Makinede Blender 5.2 LTS, Unity 6000.5.8f1, Python 3.13 ve .NET SDK kurulu.
