# Değerlendirme 3: Kapsam Gerçekçiliği

**Bakış açısı:** Üç oyunu tek başına çıkarmış, ikisi mobil, kapsamdan ölen pek çok solo proje görmüş kıdemli bağımsız geliştirici
**Sorumlu kalemler:** C1, C2, C4, C5, C8, A13, A15
**Tarih:** 9 Eylül 2026

---

## Genel değerlendirme

Tek kişi bu oyunu çıkarabilir, ama bu planı çıkaramaz. Plan üç kişilik bir stüdyonun içerik listesi gibi yazılmış; "tek kişi" kararı (03) verildikten sonra yol haritası (02 §11) hiç yeniden ölçeklenmemiş. 03'ün kendisi "yol haritasındaki süreler tek kişiye göre yeniden değerlendirilmeli" diyor, 02 hâlâ v0.1 ve §13'te "tek başına mısın?" diye soruyor. Kod tarafı yapay zekayla gerçekten taşınır. Öldürecek olan, görsel yargı isteyen kalemler: 64 yemek modeli, 32 kıyafet seti, 26 portre. Ekipte göz yok: yapay zeka mesh'i göremiyor, geliştirici de düzeltemiyor. İçerik yarıya inerse çıkar; bu haliyle yedinci ayda sanatı yarım kalmış bir proje olur.

## Varlık sayımı ve süre tahmini

Dokümanlardan toplanan çıkış kapsamı (iki mutfak):

| Kalem | Sayım | Kaynak |
|---|---|---|
| Yemek modeli | 32 × 2 = **64** | 09. Dikkat: 20 C1 tablosu "yemek görselleri dışarıdan geliyor" diyor, yani prosedürel değil |
| Malzeme | 26 × 2 + 12 = 64 ikon | 09. 3B gerekip gerekmediği yazılmamış |
| Mutfağa özel ekipman | 10 × 2 = 20 | 09 |
| Paylaşılan mobilya | 24 | 09 |
| Mimari kabuk | 2, her biri 4 genişleme kademesiyle (4/7/10/14 masa) = 8 yerleşim | 09, 10 |
| Karakter tabanı | 1 gövde, 3 vücut tipi, 8 saç, 6 cilt, 10 aksesuar, 7 klip | 09, 10, 05 |
| Kıyafet seti | 16 × 2 = **32** | 10 |
| Portre | 13 × 2 = 26 | 10 |
| Ekran | 18 | 16 |
| Ses | 8 müzik (+1 yıl sonu parçası), 7 ortam katmanı, ~72 kısa ses (40+12+20) | 17 |
| Metin | ~10.400 × 2 = ~20.800 kelime, 120 şablon parçası, 26 × 3-4 = 78-104 sahne | 18 |

Süre tahmini. Oranlar benim varsayımım, dokümanlarda yok:

| İş | Varsayım | Kişi-ay |
|---|---|---|
| Kod ve Unity entegrasyonu: 18 ekran, çekirdek, kayıt, IAP, öğretici | Yapay zeka yazar; bağlama, cihazda ayıklama ve arayüz yerleşimi geliştiricide. Ekran başına ~2 gün artı çekirdek/kayıt/IAP ~8 hafta | 4-5 |
| Prosedürel mobilya: 24 + 20 + tabak/bardak | Betik artı beğenilene kadar döngü | 1 |
| 64 yemek modeli | Parça başına 1,5-2 saat üretim, sadeleştirme, palete oturtma | 1-1,5 |
| Karakter: gövde, iskelet, klipler, 8 saç, 32 kıyafet, ağırlık aktarımı | Set başına yarım-bir gün; vücut tipi ayrı mesh ise ×3 | 1,5-2,5 |
| 2 kabuk, 8 yerleşim, mutfağa özel dekor | Mutfak başına 2-3 hafta | 1-1,5 |
| 26 portre | Tutarlılık için portre başına 2-3 saat | 0,5 |
| Arayüz ikonları | 3B'den ikon render eden editör aracı | 0,25-0,5 |
| Ses: 9 müzik, 7 ortam, ~72 efekt | Üretim, döngü noktası, mixer | 0,5-0,75 |
| Metin: 20.800 kelime düzelti, şablon testi, sahne bağlama | ~300 kelime/saat düzelti | 0,75 |
| Faz 0 denge aracı, testler, mağaza, hukuk, video | | 1-1,5 |
| **Toplam** | | **11,5-15,5 kişi-ay, orta 13-14** |

Yol haritası (02 §11): 2-3 + 4-6 + 4 + 8-12 + 4 = 22-29 hafta, yani 5-7 ay. Gerçekçi tahmin bunun yaklaşık iki katı, ve bu tam zamanlı varsayımıyla. Asıl kırılma Faz 3'te: tablonun sanat satırları tek başına 6-8 kişi-ay, plan bunun için 8-12 hafta ayırmış. Faz 1'in "6 yemek, 3 personel, 10 gün" dikey dilimi doğru; çıkış içeriği onun on katı.

## En riskli beş sorun

**1. Yol haritası solo kararından önce yazılmış ve hiç düzeltilmemiş.** Solo projeleri öldüren şey kötü kod değil, sanat yarıdayken biten moral ve para. Geliştirici yedinci ayda "plana göre çıkmış olmalıydım" hissiyle bakar ve bırakır. Düzeltme: 02 §11'i 13-14 kişi-ay üstünden yeniden yaz, Faz 3'ü 5-6 aya çıkar, "sanat tamam" için ölçülebilir bir kapı koy: bütün çıkış varlıkları LFS'te ve en düşük cihazda 30 fps.

**2. Karakter hattı, yetenek boşluğunun tam üstünde.** 20 C2'deki yedek plan iskeleti kurtarıyor, klipleri kurtarmıyor: Mixamo kapanırsa "yeme" ve "masada oturma" klipleri nereden gelecek, yazılmamış. 16 kıyafet setinin her biri siluet değiştirmek zorunda (10, "ne gerçekten okunuyor"), yani renk değil mesh; her mesh iskelete ağırlık aktarımı ister ve 3 vücut tipi ayrı mesh ise 32 set 96 mesh olur. Ağırlık aktarımı Blender'da görsel yargıyla doğrulanan bir iş, tam da geliştiricinin yapamadığını söylediği şey. Düzeltme: bütün klipleri bugün indirip LFS'e koy; vücut tipini kemik ölçeğiyle yap, tek mesh; kıyafeti mutfak başına 8-10'a indir, çünkü siluetten okunması şart olan sadece "sık" kademedeki 8 arketip (11'e göre trafiğin %70'i onlar); sigorta olarak iskeleti hazır ücretli bir karakter paketini bütçeye yaz.

**3. 64 yemek modeli iki doküman arasındaki boşluğa düşmüş.** 09, yemek sayısını 32'ye çıkarırken "yemek modelleri prosedürel üretilebilir" gerekçesini kullanıyor; 20 C1 aynı yemekleri "dışarıdan geliyor" sütununa koyuyor. Karnıyarık, mantı, işkembe çorbası hiçbir kamu malı pakette yok; kalan yol ücretli yapay zeka üretimi artı sadeleştirme, yani 1-1,5 kişi-ay saklı iş. Düzeltme: sulu yemek, çorba, pilav ve köfteyi betiğe al (kâse artı renkli yüzey, yığın, elipsoit); Meshy'yi burger gibi 5-10 kahraman yemeğe sakla; ya da 09'un v1 sayısı olan 20 yemekle çık.

**4. İçerik, oynanabilir bir prototip yokken büyütülmüş.** 9 Eylül'de tek günde yemek 20→32, arketip 6 veya 8→20 (09 ile 11 birbirini tutmuyor), kıyafet 12→16, isimli karakter 8→13 olmuş; 12 hâlâ "dengelenmedi". 09'un kendi cümlesi: "asıl maliyet model değil, denge." 64 yemek satırı, 32 arketip ve 60 günlük eğri, hiç dokunulmamış bir ekonomi üstünde ayarlanacak. Bu, kapsam patlamasının planlama aşamasında başlamış hali. Düzeltme: v2 sayılarını şema tavanı olarak tut, çıkışı v1 sayılarında dondur, kalanını çıkış sonrası ücretsiz içerik olarak ver; tutundurma için de daha iyi.

**5. Doğrulama döngüsü ölçülemiyor.** 20 C8 "onuncu günde sıkılan var mı" sorusunu 5-15 kişiye soruyor ama kişilerin nereden geleceği dosyanın kendi açık sorusu. Beş kişiyle sıkılma ölçümü gürültüdür; arkadaşlar doğruyu söylemez. Onuncu güne varmak kişi başına en az 10 × 3-6 dakika, birden çok oturumda. Ayrıca iOS cihaz "mümkünse" deniyor ama 05'in maliyet tablosunda Mac yok; 21 "ucuz kullanıcı edinimi" diyor, tablo 124 dolar. Düzeltme: sıkılmayı sormak yerine ölç; kapalı testte (21, 2 hafta) "terk edilen gün numarası" olayını baştan kaydet; denge aracına "en iyi strateji onuncu günden sonra değişiyor mu" sorusunu ekle, bu otomatik plato tespiti; test kişilerini indie Discord'ları ve Türk indie topluluklarından 20-30 kişilik açık testle bul; ilk sürümü Android'e karar ver.

## Kalem kararları

| Kalem | Karar | Gerekçe |
|---|---|---|
| C1 Model üretim yolu | DÜZELT | Sıra tersine: dikey dilim ve eğlence testi hazır paketle (02 §11 zaten "placeholder" diyor), betik iki haftalık zaman kutusuyla. Kutu ve silindir için gerçekçi; sandalye ve "yumuşak" his için haftalar yer. Geliştirici Blender Python'u ayıklayamıyorsa yedek Kenney/Quaternius artı seçili ücretli paket. Betik her çalışmada kontak sayfası PNG üretsin ki yapay zeka sonucu görebilsin. Yemekleri "dışarıdan" sütunundan çıkar |
| C2 Karakter ve animasyon | DÜZELT | Yedek plan klipleri kapsamıyor; vücut tipi kemik ölçeğiyle; 16 set → 8-10; 7 klip → 5 (yürüme, oturma, yeme, bekleme, tek tepki). Ağırlık aktarımı için betik artı görsel kontrol adımı yazılmalı |
| C4 Ses kaynağı | ONAYLA | En ucuz kalem, 20 haklı. İki şart: katmanlı servis müziği (17) için aracın stem verebildiği doğrulanmalı, veremiyorsa çıkışta iki katman (temel ve dolu) ile çapraz geçiş; aylık bütçe satırı boş, doldurulsun |
| C5 Arayüz ve yazı tipi | ONAYLA | Planın en tamam kalemi: OFL, Türkçe test cümlesi, eş genişlikli rakam, 16/24/48 piksel kuralı. Ek: yemek ve malzeme ikonlarını 3B'den render eden editör aracı; Türkçe metinler İngilizceden uzun, 18 ekran iki dilde de en düşük cihazda denensin |
| C8 Test planı | DÜZELT | Birim testi ve denge aracı katmanları doğru; insan katmanı kaynaksız ve ölçüsüz. Sıkılma sorusunu telemetriye ve harness plato metriğine çevir; kişi kaynağını yaz; Mac yoksa iOS'u ilk sürümden çıkar |
| A13 Hikaye ve metin | DÜZELT | Hacim yapay zeka için taşınır, darboğaz düzelti ve sahne testi. Dördüncü sahne kalksın (18 zaten isteğe bağlı diyor); 25 ziyaret eşiği 60 günde neredeyse gün aşırı geliş demek, çoğu oyuncu göremez, eşikler denge aracıyla ayarlansın; düzenli 10 → 6, personel 3 → 2; eleştirmen 5 → 3 kademe; Türkçe şablonlarda değişken yalnız yalın hâlde kalsın ("{yemek}'i" gibi ek alan biçim ünlü uyumunu kırar) |
| A15 Mutfak kimliği | DÜZELT | İlke doğru ve güçlü: yüz değil kabuk, ışık, siluet, ritim. Ama 10'daki "üretim maliyeti" tablosu "16 kıyafet" yazıp mesh, ağırlık ve vücut tipi çarpanını gizliyor; kombinasyon sayısı aynı dosyada 23 bin ve 17 bin olarak iki farklı yerde geçiyor (17.280 = v1'in 12 setiyle çıkan sayı). Kıyafet 8-10'a insin, üniformanın 16'nın içinde mi dışında mı olduğu netleşsin |

## Önce kesilecek üç şey, asla kesilmeyecek bir şey

**Kesilecek, sırayla:**

1. **Gardırop.** 16 set → 8-10 set, 3 vücut tipi → tek mesh artı kemik ölçeği. Tek başına 1-1,5 kişi-ay ve en tehlikeli yetenek boşluğunu ortadan kaldırır. Kalabalık yine binlerce görünüm üretir.
2. **Yemek sayısı.** Mutfak başına 32 → 20 (09'un v1 sayısı). 4-8 kişilik menü için 20 hâlâ kapasitenin 2,5-5 katı, seçim anlamlı kalır. Kalan 12 yemek çıkış sonrası ücretsiz güncelleme.
3. **İsimli karakter.** Mutfak başına 13 → 8 (6 düzenli, 2 personel), sahne 3-4 → 3. Portre 26 → 16, sahne ~91 → 48; kelime bütçesi ve unlock testi yarıya iner.

Bunlar yetmezse son çare fast food ile tek mutfak çıkıp Türk mutfağını ilk güncelleme yapmaktır; gelir modelini (D1, D3) kırdığı için ilk değil, en son kesilecek şeydir.

**Asla kesilmeyecek:** Türk mutfağında veresiye defteri ile isimli düzenli müşterinin kesişimi. 18'in deyişiyle "oyunun en güçlü tasarım kesişimi", araştırmadaki en güçlü tutundurma aracı ve 4,99'luk satın almanın tek gerçek gerekçesi. Sayısı altıya iner, sistemi kalır. Süreç tarafında Faz 0 denge aracı da dokunulmaz; bütün içerik sayıları ona bağlı.

## Cevapsız sorular

1. Geliştirici tam zamanlı mı, akşamları mı çalışıyor? Hiçbir dokümanda yok; kişi-ay ile takvim ayı arasındaki çarpan bu.
2. Mac var mı? Yoksa iOS derlemesi yok; 05 maliyet tablosu ve 20 cihaz matrisi bunu görmüyor.
3. Geliştirici Blender'ı hiç açtı mı, bir mesh'in "yanlış" olduğunu görebiliyor mu? Betik yolunun zaman kutusu ne?
4. 3 vücut tipi ayrı mesh mi, kemik ölçeği mi? 32 ile 96 kıyafet mesh'i arasındaki fark bu.
5. Personel üniforması 16 setin içinde mi, ek mi? (09 "üniforma mutfağa göre değişir", 10 tablosu ayrıca "personel kıyafeti" sayıyor.)
6. Mixamo kapanırsa oturma ve yeme klipleri nereden gelecek?
7. Hangi ses aracı, aylık ne kadar, stem verebiliyor mu?
8. Hal ekranında malzemeler 3B mi, ikon mu?
9. Oynanabilirlik testi kişileri ve soft launch kullanıcı edinim bütçesi nereden?
10. Tutarsızlıklar: 23.040 ile 17.000 kombinasyon (10), 8 ile 9 müzik parçası (17), v1 arketip 6 ile 8 (09 ile 11). Küçük ama planın aynı gün içinde birkaç kez büyüdüğünün izi.
