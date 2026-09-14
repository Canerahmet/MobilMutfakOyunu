# Değerlendirme 1: Oyun Tasarımı ve Denge

**Bakış açısı:** Yönetim ve tycoon oyunlarında on beş yıllık kıdemli oyun tasarımcısı ve ekonomi dengeleyicisi
**Sorumlu kalemler:** A4, A5, A6, A7, A8
**Tarih:** 9 Eylül 2026

---

## Genel değerlendirme

Planın yapısı doğru ve nadir görülecek kadar iyi gerekçelendirilmiş: sekiz ilke araştırmaya bağlı, her sayı "hipotez" diye etiketlenmiş, test soruları önceden yazılmış. Ama sayısal katman kendi içinde tutmuyor. 12 §6'daki büyüme tablosu 12 §5.1'deki müşteri formülünden türemiyor; 14'teki kadro tablosu 14'teki kapasite sayılarından türemiyor; talep (formül) ile kapasite (personel) hiçbir yerde birbiriyle uzlaştırılmamış. Bunun üstüne günlük döngünün dört kararından üçü (fiyat, atama, kombo) birinci gün çözülüp bir daha dokunulmayan düğmelere dönüşüyor. Verdikt: Faz 0 tablosu bu tablolardan değil, formüllerden başlamalı; tablolar sonuç olmalı, girdi değil.

## En güçlü üç yön

1. **İtibar formülünün kendini frenlemesi** (12 §5.1, §5.5). 0,5 tabanı sayesinde itibar sıfırda bile 4 masaya 8 müşteri geliyor; düşük hacimde günlük değişim de küçülüyor. Toparlanma yavaş ama hiçbir zaman imkânsız değil. Bu, araştırmadaki "sert batma" tuzağına karşı doğru matematik.
2. **Dürüst bilgi tasarımı.** Huylar işe almadan önce görünür (14 İşe alım), imza mekaniği yıl sonu puanına eksen olarak bağlı (08 Mutfağa özel eksen). "Yönetim oyunu, kumar değil" cümlesi sayılara yansımış.
3. **Haftalık toplu ödeme artı genişlemenin kirayı büyütmesi** (02 §4, 12 §2). Ekonominin önemsizleşmesini engelleyebilecek tek mekanizma bu ve 12 §7'deki "kaçıncı haftada para sorun değil" sorusu tam doğru test.

## En riskli beş sorun

### 1. Büyüme tablosu kendi formülünden türemiyor; ekonomi ya önemsizleşiyor ya tavana çarpıyor (A4, A7)

12 §5.1'in kendi örnekleri "7 masa, itibar 55 → 29", "10 masa, itibar 70 → 48", "14 masa, itibar 85 → 76" diyor. Aynı dosyanın §6 tablosu neredeyse aynı girdilerle 21, 34, 48 yazıyor. Yeniden hesap:

- Hafta 1: 13 × 50 × 7 = 4.550; ×0,68 = 3.094; −980 −1.800 = **+314**. Tutuyor. Ama hafta sonu katsayısı 1,25 hiç uygulanmamış.
- Hafta 2: 15 × 52 × 7 = 5.460; ×0,68 = 3.713; −2.780 = **+933**. Tablo +560 diyor, tutmuyor.
- Hafta 3, formülle: 28 × 1,02 = 29 müşteri. 29 × 56 × 7 = 11.368; ×0,68 = 7.730; −2.520 −3.400 = **+1.810**. Tablo −322 diyor. "Genişleme haftası zarar ettiriyor, büyümek önce bedelini ödetiyor" anlatısı bir tasarım kararı değil, aritmetik hatasının sonucu.
- Hafta 5, formülle: 40 × 1,18 = 47 müşteri → net **+4.964** (tablo +1.066). Hafta 8: 56 × 1,38 = 77 → 40.425 ciro, net **+12.789**, marj %31,6 (tablo +6.006, %19,8).
- Hafta 7 maaşı: 2×980 + 3×770 + 700 + 630 = **5.600**, tablo 5.740.
- Nakit: 8.000 +314 +560 −2.500 −322 +982 −4.500 +1.066 +2.808 = **6.408**. Son genişleme 8.000; oyuncu 1.592 açıkta. 12 §6'daki "kasayı 1.462'ye indiriyorsun" ancak 7. haftanın kârı eklenirse çıkıyor, ama o satır zaten 14 masa ve 7.200 kira varsayıyor. Döngüsel.

İki çıkış var, ikisi de kötü. Tablo müşteri sütunu talepse ekonomi 5. haftada önemsizleşiyor (üstüne bkz. sorun 4, fiyat). Sütun "servis edilen"se, yani kapasite tavanıysa, 14 masada her gün 19, hafta sonu 38 müşteri kapıdan dönüyor; itibar ancak talep = kapasite olduğu yerde dengeleniyor: 56 × (0,5 + i/100) = 58 → **i ≈ 54**. Tablodaki 88 itibara hiç ulaşılamıyor ve yıl sonu İtibar ekseni oyuncunun becerisiyle değil personel tavanıyla kapanıyor.

**Düzeltme:** §6 tablosunu silip formülden hafta sonu katsayısıyla yeniden üret. Müşteri sütununu "talep" olarak tanımla, taşma (talep − kapasite) için ayrı bir itibar cezası yaz. Haftalık kira ve ücretleri, formül kaynaklı ciroya göre 8. haftada net marj %20'yi geçmeyecek şekilde yeniden ölçekle. Mutfak terimi ekle: 07'nin vaat ettiği "ritim" farkı formülde yok, Türk ve fast food örnek marjları da aynı bantta (%63-80).

### 2. Kapasite modeli kadro tablosuyla çelişiyor; tavan 8 bir tuzak (A8)

14'teki taban kapasiteler garson 16, aşçı 20, bulaşıkçı 26, kasiyer 34. 58 müşteri için gereken: 3 aşçı, 4 garson, 2 kasiyer, 3 bulaşıkçı = **12 kişi**. Patron 12 alsa 11. Tavan 8. "Tavan tam olarak ihtiyaca göre ayarlandı" cümlesi kendi sayılarıyla yanlış. Ayrıca bulaşıkçı 26 ile ilk değil, en sert darboğaz; "üçüncü sırada" iddiası tutmuyor. Kadro tablosunun 21 satırında patron kasa ve bulaşığı tek başına tutuyor ama kapasitesi 12. Deneyimle +%30 diye yazılan telafi kampanyada ulaşılamıyor: günde 1 puan, 30 puanda seviye → ilk çalışan 60. günde 2. seviyede, +%20.

Moral tarafı da tek yönlü: "üst üste yoğun gün −3" tanımsız ve 14 masada her gün yoğun. Haftada −21 + 5 = **−16**; 70'ten 15'e 3,5 haftada iniyor, izin günü mekaniği ise hâlâ "karar bekliyor". Zam reddi ise bedelsiz: moral −20 ile 50'ye düşmek sadece "hafif hata şansı", haftada +5 ile geri geliyor. "Zam mı yeni ucuz biri mi" kararı yok, her zaman reddet.

**Düzeltme:** Kadro tablosunu kapasitelerden türet, ya kapasiteleri yükselt (öneri: garson 20, aşçı 24, bulaşıkçı 40, kasiyer 50) ya talebi düşür (masa × 3). Tavanı 3/5/7/9 yapıp her kademede bir yuva boşluk bırak. Patronun 12'sini "her sabah tek bir role atanır" yap; bu, sabah tezgâhına gerçek bir karar ekler. "Yoğun"u kapasitenin %90'ı olarak tanımla, morale bir denge terimi ekle (öneri: normal gün +1). Reddedilen zamda deneyim kazanımı dursun. İzin günü kararı "evet" olsun; o gün o rolün kapasitesi sıfır, dolayısıyla bir planlama kararı.

### 3. Servis ölçeği, sabır ve "nadir" arketipler birbirine uymuyor (A7)

12 §5.2: servis ≈ 120 saniye, sabır 8-40 saniye. 58 müşteri 120 saniyede 2 saniyede bir müşteri demek; Türk mutfağında %60 öğle dilimine düşüyor (12 §5.6), dilimler eşitse 30 saniyede 35 kişi, saniyede birden fazla. 8-12 saniyelik sabır, birkaç müşterilik kuyruk demek. 3-5 müdahale hakkıyla bu okunmaz; 02'nin "dar boğazı gör ve aç" vaadi partikül sistemine dönüşür. Ayrıca müşteri kişi mi grup mu belli değil; Aile 3-5 kişiyken fiş 50-75 kişi başı görünüyor.

11'deki "nadir %5, kampanya boyunca birkaç kez gelir" iddiası da yanlış: 58 müşteride günde ~3, kampanyada yaklaşık 2.000 müşteri üstünden ~100 nadir ziyaret. Ağırlığı 8 olan eleştirmen dört günde bir gelirse itibar formülü tek başına ondan sallanır.

**Düzeltme:** Servis süresi kademeyle ölçeklensin (02'nin 90-180 sn bandında: 4 masada 120, 14 masada 180). Hız için müşteri grup sayılsın, fiş kişi başı kalsın. Nadir kademe yüzdeden çıkıp takvime bağlansın (öneri: kampanyada 8-12 olay), bir akşam önce hesap ekranında haber verilsin. Bu aynı zamanda sorun 4'ün ilacı.

### 4. Döngü 20. günde otomatikleşiyor: üç karar birinci gün çözülüyor (A4, A6)

12 §5.3 ve §5.4 ile: fiyat %20 üstü, ortalama duyarlılık −20 → memnuniyet 80 → eşik 60'ın üstünde, memnun. Toleranslı −8. Sadece pazarlıkçı (orta kademe) gider. Erken oyunda bekleme sıfıra yakınken %20-25 üstü fiyat baskın strateji; bu, tablodaki cironun üstüne bir %20 daha bindiriyor ve 02 §6'daki "fiyat kararı 6. bölümde de önemli olmalı" hedefini boşa çıkarıyor. Araştırmadaki Supermarket Simulator "her şeye %7, şikayet sıfır" örneği tam bu çözülmüşlük. İstasyon ataması gün içinde değişmiyor ve talep karışımı değişmediği için bir kez kuruluyor. Kombo bir kez kurulan ayar mı, günlük karar mı, yazılmamış. Menüde yemeklerin marj dışında parametresi yok (09 "Sonraki adım" hazırlık süresi vaat etti, 12 vermedi); 12 burgerin arasından seçim marj sıralamasına iner. Hal ise iki ilkeyle çelişiyor: 02 ilke 2 "malzeme siparişi otomatikleşir" derken 02 §3.1 halı "ikinci döngü" yapıyor; fast food'da %80 malzeme bozulmadığı için hal "ucuz günde stokla" oyununa dönüyor, üstelik ücretsiz ve öğretici mutfakta. 26+12 malzeme kalemi tek tek alınırsa 40-60 dokunuş bütçesi (ilke 6) daha servis başlamadan biter.

**Düzeltme:** Fiyat cezası doğrusal olmaktan çıksın, %10 üstünde hızlansın ve toleransı itibara bağlansın (yüksek itibar daha yüksek fiyat taşır) ki fiyat itibar hareket ettikçe yeniden karar olsun. Her yemek dört parametre taşısın: hazırlık süresi, istasyon, en az bir bozulabilir malzeme, arketip çekimi. Hal otomatik taban sipariş artı günde 3-5 elle "fırsat/risk" satırı olsun. Kombo, o günkü hal fiyatına bağlı günlük karar olsun.

### 5. Dördüncü mevsim plato, imza mekanikleri yarı reskin (A5, A6)

09 İlerleme eğrisi: 46-60. günlerde yeni sistem yok, 5 yemek, kadro tavanda, son genişleme alınmış (ya da sorun 1'e göre alınamamış). 45-90 dakika düz oyun; araştırma §3 madde 6 ve Supermarket Simulator geç oyun şikayetiyle doğrudan çelişiyor. 08 bunu "ustalık" diye savunuyor ama ustalık için değişen bir şey lazım.

Mekanikler: Japon çorba suyu gerçekten farklı bir karar şekli (sabah miktar tahmini). Türk veresiye şu hâliyle vergi: tutar, vade, tahsilat takvimi, ödememe olasılığı yazılmamış; 8.000 sermaye ve 2.780'lik haftalık ödemede 55'lik bir yemeğin veresiyesi gürültü. Kombo bir düğme. İtalyan kurs zamanlaması masa başına karar ister, 3-5 müdahale bütçesiyle çelişir.

**Düzeltme:** 4. mevsime olay zinciri: eleştirmen finali geri sayımlı, rakip restoran 3. mevsime çekilsin, bir festival haftası (talep artar, tavan geçici gevşer). Bu yapılmayacaksa kampanya 45 güne insin, 08 zaten soruyor. Veresiye tahsilatı kira gününden farklı fazda bir takvime bağlansın (memur maaş günü, 1 ve 15); defter görünür olsun; haftalık cironun anlamlı bir payı veresiyeye gidebilsin. Menü yuvası masa kademesiyle büyüsün (öneri 4/5/6/8) ki araştırmadaki Cat Cafe "açılıyor ama kullanamıyorsun" şikayeti tekrarlanmasın; 32 yemek ancak parametrelerle anlamlı, aksi hâlde v1'in 20'si yeterliydi.

## Kalem kararları

| Kalem | Karar | Gerekçe |
|---|---|---|
| A4 Ekonomi sayıları | DÜZELT | Fiyat, ücret, kira, kredi yapısı başlangıç için uygun. §6 tablosu §5.1'den hafta sonu katsayısıyla yeniden üretilecek; 2. hafta neti (933 ≠ 560), 7. hafta maaşı (5.600 ≠ 5.740), son genişleme nakit sırası düzeltilecek; formüle mutfak terimi eklenecek. |
| A5 İçerik envanteri | DÜZELT | 32 yemek kalabilir, ama her yemek dört parametre (hazırlık süresi, istasyon, bozulabilir malzeme, arketip çekimi) taşımadan onaylanmaz; menü yuvası kademeyle büyüsün. |
| A6 İlerleme eğrisi | DÜZELT | İmza mekaniğinin 16. günde gelmesi doğru. 4. mevsime olay zinciri gelmezse kampanya 45 güne insin; son genişleme 7. hafta sonuna alınsın. |
| A7 Müşteri sistemi | DÜZELT | Arketip ve sıklık yapısı iyi. Müşteri = kişi/grup netleşsin, nadir kademe takvime bağlansın, servis süresi ölçeklensin, taşma modeli ve doğrusal olmayan fiyat cezası yazılsın. |
| A8 Personel sistemi | DÜZELT | Huy tasarımı ve basit yapay zekâ kararı doğru. Kadro tablosu kapasitelerden türetilsin, bulaşıkçı darboğazı kabul edilsin veya kapasite yükselsin, patronun 12'si günlük tek role atansın, "yoğun gün" tanımlansın, zam reddi bedelli olsun, izin günü "evet". |

## Cevapsız sorular

1. Müşteri sayısı kişi mi, grup mu? Fiş tutarı kişi başı mı? Formül ve kapasite hangi birimde?
2. Genişleme mevsime mi nakde mi kilitli? 8.000 sermayeyle 2.500'lük ilk genişleme 1. gün alınabiliyor.
3. Talep kapasiteyi aşınca ne oluyor: müşteri kapıdan mı dönüyor, kuyruğa mı giriyor, itibar cezası kaç?
4. Küçülme (kademe 5) kirayı da düşürüyor mu? Gönüllü küçülme var mı? 02 §5'teki "12 masa → 6" hangi kademe; kademeler 4/7/10/14.
5. Patronun 12 müşterisi tek rolde mi, üç rolde toplam mı?
6. Servis 120 saniye sabit mi, dört dilim eşit mi, kademeyle uzuyor mu?
7. Kredi vadesi 8 hafta kampanya sonunu aşıyor. Yıl sonu Varlık ekseni açık borcu nasıl sayıyor? 4 haftalık vade olacak mı?
8. Veresiye: tavan tutar, vade, tahsilat takvimi, ödememe olasılığı, sadakatin sayısal karşılığı.
9. Hiç genişlemeyen oyuncu 4 masada sonsuza kadar kârda kalıyor (+314 ile +933/hafta). Batma merdiveni sadece hırslıya mı işleyecek, yoksa mevsimlik kira artışı gibi bir baskı olacak mı?
10. Kombo günlük karar mı, bir kez kurulan ayar mı? Hal fast food'da %20 bozulmayla neyi zorlayacak?
