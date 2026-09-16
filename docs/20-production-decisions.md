# Üretim Kararları

**Son güncelleme:** 9 Eylül 2026
**Kütük maddeleri:** C1 model üretim yolu, C2 karakter ve animasyon, C4 ses kaynağı, C5 arayüz ve yazı tipi, C8 test planı
**Durum:** Yazıldı, karar bekliyor

Bu dosya [05-production-plan.md](05-production-plan.md) dosyasında açık bırakılan seçenekleri karara bağlıyor.

---

## C1. Model üretim yolu

**Karar: üç katmanlı, sırayla denenen bir yol.**

| Sıra | Yol | Ne için |
|---|---|---|
| 1 | Prosedürel Blender betiği | Mobilya, ekipman, tabak, mimari parçalar |
| 2 | Kamu malı paketler | Betiğin kapsamadığı organik nesneler ve prototip hızı |
| 3 | Yapay zeka üretimi veya ücretli paket | Sadece benzersiz olması gereken tekil nesneler |

### Neden bu sıra

Prosedürel önce geliyor çünkü **stil tutarlılığını garanti eden tek yol.** Bütün mobilya tek bir yapılandırmadan çıkınca aynı kenar yumuşatması, aynı ölçek ve aynı palet garanti oluyor.

Ayrıca model bir dosya değil bir betik olduğu için sürüm kontrolüne giriyor. Stili beğenmezsen betiği değiştirip hepsini yeniden üretiyorsun. Elle modellenmiş kırk parçayı yeniden yapmak günler alır, betiği değiştirmek dakikalar alır.

### Kapsam

| Prosedürel üretiliyor | Dışarıdan geliyor |
|---|---|
| Masa, sandalye, tabure, tezgâh | Bitkiler |
| Raf, dolap, ocak, buzdolabı | Yemek görselleri |
| Tabak, bardak, tencere, tepsi | Karakterler |
| Kapı, pencere çerçevesi, lamba | Tabela ve hikayeye ait tekil eşyalar |

### Kural

Yapay zeka ile üretilen hiçbir varlık **ücretsiz katmanla** üretilmiyor. O araçların ücretsiz çıktıları ticari kullanıma kapalı. Yayın öncesi bütün varlıkların kaynağı ve lisansı bir tabloda listeleniyor.

---

## C2. Karakter ve animasyon

**Karar: tek paylaşılan gövde, standart insansı iskelet, giydirme ile çeşitlilik.**

### Üretim hattı

1. Tek bir insansı temel gövde üretilir.
2. **Standart insansı iskelete** uygun olarak hazırlanır.
3. Otomatik iskeletleme servisinden geçirilir.
4. Animasyon kütüphanesinden klipler alınır: yürüme, oturma, yeme, bekleme, konuşma, sevinme, sinirlenme.
5. Unity'nin insansı animasyon sistemi bütün karakterlerde aynı klipleri paylaşır.

### Mixamo riski ve yedek plan

Mixamo ücretsiz ve ticari kullanıma açık, ama Adobe yıllardır güncellemiyor ve destek artık desteklenmediğini söylüyor.

**Yedek plan: modeller standart insansı iskelete uygun üretiliyor.** Böylece animasyon kaynağı değişse bile modeller değişmiyor. Mixamo kapanırsa başka bir otomatik iskeletleme servisine geçiliyor ve model tarafında hiçbir şey yapılmıyor.

Bu, tek bir servise bağımlılığı ortadan kaldıran yapısal karar.

### Çeşitlilik

Model değil giydirme. Üç vücut tipi, sekiz saç, altı cilt tonu, mutfak başına on altı kıyafet, aksesuarlar. Yirmi üç binden fazla kombinasyon.

Ayrıntı: [10-cuisine-identity.md](10-cuisine-identity.md).

---

## C4. Ses kaynağı

**Karar: ücretli yapay zeka araçları, kamu malı yedekle.**

| Kalem | Kaynak |
|---|---|
| Ses efekti | Ücretli yapay zeka aracı. Ücretli planlar telifsiz ve ticari kullanıma açık |
| Müzik | Ücretli yapay zeka aracı |
| Yedek | Kamu malı ses kütüphaneleri |
| Karakter hecesi | Yapay zeka veya kendi kaydın |

### Gerekçe

Ses üretimi bu projede en ucuz kalem. Aylık abonelik, bütün oyunun sesini üretmeye yetiyor. Kamu malı kütüphanelerden toplamak ücretsiz ama tutarlı bir palet kurmak çok daha zor.

### Uyarı

Kullanılan aracın **o ay geçerli** şartları yayın öncesi doğrulanacak. Bu araçların lisans dili hızla değişiyor. Üretilen her sesin hangi araçla ve hangi planla üretildiği kaydedilecek.

---

## C5. Arayüz, ikon ve yazı tipi

### Yazı tipi

**Karar: Google Fonts üzerinde SIL Open Font License lisanslı bir aile.**

Zorunlu kontrol listesi:

| Karakter | Neden |
|---|---|
| ı ve İ | Türkçe noktasız ı ve noktalı İ. En sık atlanan hata |
| ğ Ğ | |
| ş Ş | |
| ç Ç, ö Ö, ü Ü | |

**Test yöntemi:** "İstanbul'da çiğ köfte ve şalgam" cümlesi bütün yazı boyutlarında kontrol edilir. Bir karakter bile eksikse yazı tipi elenir.

Eş genişlikli rakam desteği de şart, çünkü kasa değeri değişince sayının zıplamaması gerekiyor.

### İkonlar

- Kamu malı arayüz ikon setleri temel alınıyor.
- Para ikonu özel çiziliyor, karar verildi: düz sikke yığını.
- Her ikon 16, 24 ve 48 pikselde test ediliyor. En küçük boyutta okunmayan ikon kullanılmıyor.

### Arayüz düzeni

Kod, yani bende. Ayrıntı [16-screens-and-tutorial.md](16-screens-and-tutorial.md) dosyasında.

---

## C8. Test planı

### Üç katman

| Katman | Ne test ediyor | Kim |
|---|---|---|
| Birim testleri | Çekirdek kuralları | Otomatik, her derlemede |
| Denge aracı | Ekonomi ve ilerleme | Otomatik, gecelik |
| Oynanabilirlik | Eğlence ve anlaşılırlık | İnsan |

### Birim testleri

Çekirdek saf C# olduğu için normal .NET test altyapısıyla çalışıyor, Unity gerekmiyor.

Kapsanan alanlar: ekonomi hesapları, memnuniyet ve itibar formülleri, sabır azalması, kapasite hesabı, batma merdiveni geçişleri, kayıt göçü, içerik doğrulama.

**Kural: bulunan her hata için önce bir test yazılıyor.**

### Denge aracı

Faz 0'da yazılıyor. Farklı oyuncu stratejileriyle yüzlerce oyunu simüle edip sonucu tablo olarak veriyor.

Cevaplaması gereken sekiz soru [12-economy.md](12-economy.md) dosyasında.

### Oynanabilirlik testi

| Aşama | Kişi | Ne ölçülüyor |
|---|---|---|
| Dikey dilim | 5 kişi | Anlaşılıyor mu |
| İçerik sonrası | 10-15 kişi | Onuncu günde sıkılan var mı |
| Soft launch | Gerçek oyuncular | Birinci ve yedinci gün tutundurma |

**En kritik soru: onuncu günde sıkılan var mı?** Araştırmadaki en yaygın ölüm sebebi orta oyun platosuydu. Bu soru cevaplanmadan içerik üretimine devam edilmiyor.

### Cihaz matrisi

| Sınıf | En az |
|---|---|
| Üst seviye | Bir adet güncel telefon |
| En düşük | Bir adet asgari şartları karşılayan telefon |
| iOS | **İlk sürümde yok.** Mac yok, derleme yapılamıyor. Mac olunca eklenir |

Emülatörde performans testi yapılmıyor. Sadece gerçek cihaz.

---

## Karar bekleyen ayrıntılar

1. Prosedürel yol birincil kalsın mı, yoksa hazır paketle başlayıp sonra mı geçelim
2. Ses için hangi araç, aylık bütçe ne kadar
3. Oynanabilirlik testi için kişi nereden bulunacak
4. ~~iOS test cihazı ilk sürümde şart mı~~ Kapandı: ilk sürüm sadece Android. Bkz. [22-answers-and-direction.md](22-answers-and-direction.md)
