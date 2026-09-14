# Oyun Tasarım Önerisi v0.1

**Tarih:** 9 Eylül 2026
**Dayanak:** [Pazar Araştırması](research/01-pazar-arastirmasi.md). Buradaki her tasarım kararı, araştırmadaki somut bir oyuncu geri bildirimine bağlanmıştır.

---

## 1. Tek cümlelik konsept

Sen aşçı değil **patronsun**: menüyü, fiyatı, tedariki ve ekibi sen kurarsın, servis sırasında sadece krizlere müdahale edersin, ve her hafta gelen kira gününü karşılamak zorundasın.

Bu cümledeki "aşçı değil patron" ayrımı, oyunu mobildeki tüm rakiplerden ayıran şey. Cooking Fever ve Diner Dash sana yemek yaptırır. Eatventure sana sayı büyüttürür. Biz sana **karar** verdiririz.

---

## 2. Araştırmadan türetilen 8 tasarım ilkesi

Bunlar tartışmaya açık değil, çünkü her biri incelemelerde tekrarlanan bir örüntüden geliyor.

1. **Her kararın görünür bir sonucu olacak.** Cat Cafe Manager'ın ölüm sebebi "hiçbir şeyin önemi yok" hissiydi. Fiyatı yükseltirsen müşteri yüzü değişecek, masa boş kalacak, yorum düşecek. Anında ve gözle görülür.
2. **Otomasyon sıkıcı olanı alacak, ilginç olanı asla.** Travellers Rest oyuncularının açık talebi. Malzeme siparişi, bulaşık ve temizlik otomatikleşir. Menü, fiyat, ekip dizilimi ve kriz anları hep senin kalır.
3. **Ekonomi hiçbir zaman önemsizleşmeyecek.** Tavern Master "harcayamayacağın kadar para" yüzünden öldü. Her büyüme adımı sabit gideri de büyütecek, böylece baskı hep taze kalacak.
4. **Başarısızlık ilerlemeyi silmeyecek ama bedeli görünür olacak.** PlateUp modeli. Game Over ekranı yok, küçülme var.
5. **Sayaç, enerji ve bekleme yok.** Good Pizza'nın gerçek zamanlı bahçe sayaçları en çok şikayet edilen mekanik. Oyun kapalıyken hiçbir şey beklemeyecek.
6. **Tıklama bütçesi olacak.** Good Pizza'da çok malzemeli siparişler korku kaynağı oldu. Bir gün en fazla 40-60 dokunuş olmalı. Fazlası ödül değil ceza.
7. **Yeni sistem sürekli açılacak.** Dave the Diver'ın en güçlü silahı. Oyuncu tam sıkılacakken yeni bir katman gelecek.
8. **Tema kaymayacak.** Travellers Rest forumundaki "artık meyhane oyunu olmaktan çıkıyor" şikayeti. Ne eklersek ekleyelim, oyun restoran işletme oyunu kalacak.

---

## 3. Çekirdek döngü: bir günün anatomisi

Hedef süre: **3-6 dakika.** Mobil oturumu bu. Her aşamada oyun kapatılabilir ve kaldığı yerden devam eder.

### Aşama 1 — Sabah: Hal (45-75 sn)

Bu bizim **ikinci döngümüz**, Dave the Diver'daki dalışın karşılığı. Tek ve iyi olacak; Dave the Diver'ın "çok fazla mini oyun" şikayetine düşmemek için başka mini oyun eklemeyeceğiz.

- Malzeme fiyatları her gün dalgalanır. Domates bugün ucuz, balık pahalı.
- Stok sınırlı. Geç kalırsan iyi malı kaparlar.
- Kalite seviyeleri var: ucuz mal düşük memnuniyet, pahalı mal yüksek yorum puanı.
- Bozulma var. Fazla alırsan çöpe gider. Az alırsan servis ortasında tükenir ve müşteri kaybedersin.
- Tedarikçilerle ilişki büyür: düzenli alışveriş indirim ve öncelikli stok açar.

Bu tek ekran, Supermarket Simulator'ün en sevilen unsuru olan fiyat ve tedarik takasını taşıyor.

### Aşama 2 — Açılıştan önce: Tezgâh (45-75 sn)

- **Menü:** Bugün hangi 4-8 yemek satılacak? Elindeki malzemeye ve mevsime göre. Dave the Diver'ın günlük menü seçimi burada.
- **Fiyat:** Her yemeğin fiyatını sen belirlersin. Piyasa ortalaması gösterilir. Üstüne çıkarsan kâr artar, memnuniyet düşer.
- **Ekip:** Kim hangi istasyonda? Aşçı mı, garson mu, kasa mı? Personelin karakter özellikleri var: hızlı ama dağınık, yavaş ama titiz, kalabalıkta panikleyen.

### Aşama 3 — Servis (90-180 sn, aktif ama duraklatılabilir)

Burası oyunun kalbi ve en kritik tasarım kararı. **Sen yemek yapmıyorsun.**

- Müşteriler gelir, personel çalışır, sistem kendi kendine döner.
- Senin işin **dar boğazı görmek ve açmak.** Bulaşık birikti, mutfak tıkandı, bir masa çok bekledi, bir tedarik bitti.
- Sınırlı sayıda **patron müdahalesi** hakkın var (gün başına 3-5). Bir istasyonu hızlandır, bekleyen masaya ikram gönder, VIP müşteriyi bizzat karşıla.
- Her an duraklatılabilir. Duraklatınca da müdahale edilebilir. Bu, refleks oyunu olmadığımızı garanti eder.
- Zorluk refleksten değil, **hazırlığın doğruluğundan** gelir. PlateUp'ın "mühendisliğe yakın" hissi tam olarak bu.

Bu tasarım aynı zamanda mobil dokunmatik için doğru olan: sürükle-bırak ve tek dokunuş yeterli, hassas nişan gerekmiyor. Supermarket Simulator mobil uyarlamalarının en büyük şikayeti olan dokunmatik kamera ve nişan sorununu baştan bertaraf ediyor.

### Aşama 4 — Kapanış: Hesap (45-60 sn)

- **Gelir tablosu:** Ciro, malzeme maliyeti, maaşlar, günlük kira payı, net kâr. Tek ekranda, okunur.
- **Müşteri yorumları:** Kısa, karakterli, isimli. "Fiyatlar biraz tuzlu ama köfte harikaydı." Bu, itibar sisteminin görünür yüzü.
- **İtibar değişimi:** Yıldız puanı hareket eder ve yarınki müşteri sayısını belirler. Dave the Diver'ın Cooksta sistemi.
- **Harcama:** Kazandığını ekipmana, yerleşime, personele veya krediyi kapatmaya yatırırsın.

---

## 4. Haftalık ritim: baskının metronomu

Günlük baskı yorucu olur, aylık baskı hissedilmez. **Haftalık doğru ölçek.**

Her 7. gün: **kira + maaşlar + varsa kredi taksiti** tek seferde kasadan çıkar.

Bu tek mekanik, "batmadan büyüme" gerilimini tek başına taşır. Oyuncu 4. günden itibaren cuma gününü düşünmeye başlar. Büyüme kararlarını (yeni ekipman almak, personel işe almak) bu takvime göre planlar.

---

## 5. Batma: yumuşak ama dişli merdiven

Araştırmanın en net dersi: sert batma hüsran üretir, sonuçsuzluk kayıtsızlık üretir. Ortası şu:

| Kademe | Tetik | Sonuç |
|---|---|---|
| 1. Uyarı | Kasa haftalık gideri zor karşılıyor | Ev sahibinden mesaj. Görsel uyarı. |
| 2. Sıkışma | Ödeme yapılamadı | Kredi çekmek veya ekipman satmak zorunda kalırsın |
| 3. Moral kaybı | Maaş gecikti | Personel morali düşer, biri istifa edebilir |
| 4. Ültimatom | İkinci kez ödenemedi | Ev sahibi 7 günlük süre verir. Geri sayım görünür. |
| 5. Küçülme | Süre doldu | Restoranın bir bölümünü kaybedersin. Masalar gider, salon küçülür. |

**Game Over ekranı yok. Kayıt asla silinmez.** Ama 5. kademede kaybettiğin şey gözünle gördüğün bir şey: dün 12 masan vardı, bugün 6. Bu, sayı kaybetmekten çok daha acıtır ve çok daha adildir.

Küçülmeden sonra toparlanmak mümkün olmalı, hatta bir "toparlanma" başarımı olmalı. PlateUp'ın kaybı kalıcı kazanıma çevirme fikri burada karşılığını bulur.

---

## 6. Derinliğin kademeli açılması

Cuisineer %76'da kaldı çünkü yönetim katmanı baştan sığdı ve öyle kaldı. Dave the Diver %96 aldı çünkü sürekli yeni sistem açtı. Planımız:

| Bölüm | Açılan sistem | Yaklaşık süre |
|---|---|---|
| 1 | Menü, fiyat, temel servis | İlk 30 dk |
| 2 | Personel işe alma ve istasyonlar | 30-90 dk |
| 3 | Tedarikçi ilişkileri, malzeme kalitesi | 1,5-3 saat |
| 4 | Yerleşim düzenleme ve mekân genişletme | 3-5 saat |
| 5 | İtibar, eleştirmen ziyareti, rakip restoranlar | 5-8 saat |
| 6 | İkinci şube, semt yayılması | 8+ saat |

Her yeni sistem, önceki sistemi geçersiz kılmaz, üstüne biner. Fiyat kararı 6. bölümde de önemli olmalı.

---

## 7. Mobil kullanıcı deneyimi kararları

- **Yatay mod (landscape).** Mekân genişletme ana mekanik olduğu için ekran genişliği gerekli. Menü ve fiyat ekranları yine de tek elle erişilebilir tasarlanacak.
- **Sürükle-bırak temel etkileşim.** Hassas nişan yok, çift dokunuş yok.
- **Minimum 44pt dokunma alanı.** Her buton parmakla rahat basılabilir olacak.
- **Her an duraklatma ve her an çıkış.** Gün ortasında uygulama kapanırsa aynı saniyeden devam eder.
- **Tıklama bütçesi takibi.** Prototipte bir günün dokunuş sayısı ölçülecek. 60'ı geçen tasarım elenecek.
- **Sayısal bilgi grafiğe çevrilecek.** Küçük ekranda tablo okunmaz. Memnuniyet bir yüz ifadesi, stok bir dolu-boş çubuk olacak.

---

## 8. Sanat yönü önerisi

**Öneri: yumuşak low-poly 3D sahne, sabit yaklaşık 40 derece kamera, diyaloglarda pixel-art karakter portreleri.**

Gerekçe:

- Yerleşim ve genişleme ana mekanik. 3D sahne kamerayı döndürmeye ve yakınlaşmaya izin verir. Pixel sprite'lar her açı için yeniden çizim ister ve genişleyen bir mekânda bu maliyet katlanır.
- Küçük ekran okunabilirliği. Low-poly'de silüet ve renk bloğu net kalır. Yoğun pixel detayı 6 inçlik ekranda gürültüye dönüşür.
- Üretim maliyeti. Tek kişilik veya küçük ekip için low-poly modelleme, kaliteli pixel animasyonundan daha hızlı ölçeklenir.
- Pixel portreler hikayeyi ve sıcaklığı taşır, Hungry Hearts Diner'ın duygusunu verir ve maliyeti düşüktür.

**Alternatif:** Tam pixel HD-2D (Dave the Diver ve Discounty çizgisi). Daha ayırt edici ve pazarda kanıtlı, ama animasyon maliyeti yüksek ve kamera esnekliği düşük.

Araştırmanın uyarısı burada geçerli: **sanat tarzı kurtarıcı değil, çarpan.** Cat Cafe Manager'ın "her şey sevimli" diyen incelemeleri bile oyunu tavsiye etmedi.

---

## 9. Motor önerisi

**Öneri: Unity.**

- Mobil reklam, uygulama içi satın alma ve analitik SDK ekosistemi olgun ve hazır.
- 3D low-poly sahne ve mobil optimizasyon araçları yerleşik.
- Asset Store ile prototip hızı yüksek.

**Godot 4 ne zaman doğru olur:** Tamamen pixel 2D'ye karar verirsek ve monetizasyon minimal kalırsa. Godot'nun 2D hattı ve daha küçük build boyutu o senaryoda avantaja döner. (Karşılaştırma sayıları veren blog kaynakları taraflı olabilir, doğrulanmamış kabul edilmeli.)

Bu karar senin mevcut deneyimine de bağlı. Hangi motorda daha rahatsan, prototip hızı her şeyden önemli.

---

## 10. Monetizasyon önerisi

Araştırma çok net: enerji, sayaç ve seviye kilidi nefret ediliyor. Bunların hepsini dışarıda bırakmak tek başına bir pazarlama mesajı olur.

- **Ücretsiz indirilir**, ilk bölüm tam oynanır (yaklaşık 1-2 saat gerçek oynanış).
- **Tek seferlik tam sürüm satın alması**, 5-8 dolar bandı.
- **Ödüllü reklam sadece isteğe bağlı** ve döngüyü kesmeyen yerde: gün sonu hesap ekranında "bugünkü kârı ikiye katla" gibi. Hungry Hearts Diner'ın hatası reklamın varlığı değil, oyunun ortasında çıkmasıydı.
- **Kozmetik dekor paketleri.**
- **Kesinlikle yok:** enerji, gerçek zamanlı bekleme, seviye kilidi, pay-to-win.

---

## 11. Yol haritası

| Faz | Süre | Çıktı | Geçme kriteri |
|---|---|---|---|
| 0. Kağıt ve tablo | 2-3 hafta | Ekonomi tablosu, bir günün matematiği | 30 günlük simülasyon tabloda dengeli mi? |
| 1. Dikey dilim | 4-6 hafta | Tek restoran, 6 yemek, 3 personel, 10 gün, placeholder grafik | Oynanabilir ve anlaşılır mı? |
| 2. Eğlence testi | 4 hafta | 10-15 kişiyle test | 10. günde sıkılan var mı? |
| 3. İçerik ve sanat | 8-12 hafta | Gerçek sanat, bölüm 1-4 sistemleri | Görsel kimlik oturdu mu? |
| 4. Soft launch | 4 hafta | Sınırlı pazarda yayın | 1. gün ve 7. gün tutundurma ölçümü |
| 5. Global çıkış | - | - | - |

**Faz 0 en kritik faz.** Ekonomi tabloda çalışmıyorsa oyunda hiç çalışmaz. Bu fazı atlamak, Tavern Master'ın "harcayamayacağın kadar para" sonucuna giden yoldur.

---

## 12. Riskler

1. **Yönetim katmanı sığ kalırsa** Cuisineer'ın kaderi bizi bekler (%76). Karşı önlem: Faz 2'deki eğlence testi, 10. günde sıkılma ölçümü.
2. **Mobilde arayüz okunmazsa** oyun oynanamaz. Karşı önlem: sayısal veriyi grafiğe çevirme ilkesi, gerçek cihazda erken test.
3. **Ekonomi orta oyunda önemsizleşirse** oyun ölür. Karşı önlem: her büyüme adımının sabit gideri de büyütmesi.
4. **Kapsam patlaması.** Bölüm 6'daki ikinci şube fikri, tek kişilik ekip için ilk sürümün dışında tutulmalı. Önce bölüm 1-4 ile çıkılmalı.
5. **Personel yapay zekâsı kötü olursa** güven yıkılır. Cat Cafe Manager ve Tavern Keeper'ın ortak yarası. Karşı önlem: personel davranışını basit ve öngörülebilir tutmak, karmaşık yol bulmadan kaçınmak.

---

## 13. Karar bekleyen açık sorular

Bunlara cevabın planı netleştirir. Her biri için önerimi de yazdım.

1. **Sanat tarzı:** Yumuşak low-poly 3D mi, tam pixel HD-2D mi? *Önerim: low-poly, gerekçe 8. bölümde.*
2. **Motor:** Unity mi Godot mu? *Önerim: Unity. Ama senin deneyimin belirleyici olmalı.*
3. **Ekip:** Tek başına mısın, yoksa sanat veya kod tarafında destek var mı? Yol haritası süreleri buna göre değişir.
4. **Hedef ilk sürüm kapsamı:** Bölüm 1-4 ile çıkmak mı, yoksa daha küçük bir 1-3 kapsamı mı? *Önerim: 1-4, çünkü mekân genişletme en sevilen mekaniklerden ve onsuz oyun eksik kalır.*
5. **Tema ve mekân:** Genel bir şehir restoranı mı, yoksa belirli bir kültür ve mutfak mı (örneğin bir Türk esnaf lokantası)? *Önerim: belirli bir kimlik, çünkü Discounty ve Hungry Hearts Diner'ın en çok övülen tarafı karakter ve yer duygusu.*
6. **Monetizasyon:** Ücretsiz artı tam sürüm kilidi mi, yoksa doğrudan ücretli mi? *Önerim: ücretsiz artı tek seferlik kilit açma.*
