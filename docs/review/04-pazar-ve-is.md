# Değerlendirme 4: Pazar ve İş

**Bakış açısı:** Bir düzine simülasyon ve tycoon oyununu soft launch'tan ölçeklendirmiş, Steam bağımsız pazarını da bilen mobil yayıncı ve ürün lideri
**Sorumlu kalemler:** D2, D5, D6, ve danışma niteliğinde D1
**Tarih:** 9 Eylül 2026

---

## Genel değerlendirme

Tasarım olarak plan sağlam, iş modeli olarak zayıf. Araştırma (01) neredeyse tamamen Steam verisine dayanıyor; "sayaç yok, enerji yok" mesajına tepki veren kitle Steam'de yaşıyor, ama plan o kitleye ulaşmayı en sona atıp (21 §D5, "Steam: Sonra") parayı, ücretsiz oyunu bitirip memnun kalmış mobil oyuncunun ikinci bir kampanyaya 4,99 dolar vermesinden bekliyor. Bu dönüşüm türde düşük seyreder ve sıfır bütçeyle onu taşıyacak kurulum hacmi de yok. Mevcut sırayla mobil taraf tahminimce yılda birkaç bin dolar üretir; anlamlı para ancak Steam öne alınır ve ücretsiz kampanyaya gerçek bir satın alma tetikleyicisi konursa gelir. Plan "para kazanmaz" değil, "planlandığı sırayla para kazanmaz."

## En güçlü üç yön

1. **Dürüst model, gerçek bir farklılaşma.** Tek para birimi, güç satmama, sayaçsızlık (07 "Neden bu yapı doğru", 12 §7.5). Bu profil mağaza puanını korur, iade ve şikâyet üretmez ve Apple editör vitrininin sevdiği türdendir. Good Pizza'nın para birimi karışıklığı (01 §2) daha kâğıt üstünde kapatılmış.
2. **"Mutfak = ayrı oyun" tezi ve plaket/miras ekranı** (07 imza mekaniği; 08 "Tekrar oynama kancası"). İçerik satışı ancak alıcı "yeniden boyama değil başka oyun" hissederse çalışır; veresiye ve çorba suyu mekanikleri bu vaadi taşıyabilir. Dört plaket toplama hedefi ikinci ve üçüncü satın almayı doğal kılıyor.
3. **Maliyet tabanı sıfıra yakın, kimlik hikâyesi bedava basın getiriyor.** İlk yıl 124 dolar (05 maliyet tablosu); başabaş neredeyse ilk satışta, asıl maliyet senin zamanın. "Türk solo geliştirici, esnaf lokantası, Supermarket Simulator'ün ülkesinden" hikâyesi yerel basın ve topluluklarda kendi kendine yayılır.

## En riskli beş sorun

### 1. Kitle Steam'de, iş modeli mobilde; Steam en sona atılmış (en büyük ticari risk)

**Sorun:** 01'deki on referansın dokuzu Steam oyunu; Good Pizza, My Cafe ve Cooking Fever'ın nasıl para kazandığı araştırmada yok. Karar ağırlığı isteyen, sayaçtan nefret eden oyuncu zaten Steam'de; mobil kitle ücretsiz sim indirir, sayaca katlanır, içeriğe ödeme yapmaz. Plan Steam'i "bütün mutfaklar tamamlanınca" diyerek en az bir yıl erteliyor.

**Sonuç (tahmin, veri değil):** Sıfır bütçeyle ilk yıl 10-50 bin organik mobil kurulum, kurulum başına 0,05-0,12 dolar gelir, yani birkaç bin dolar. 5-10 bin istek listesiyle çıkan bir Steam sürümü ilk yılda on binlerce dolara ulaşabilir; mütevazı ama mobilin birkaç katı.

**Çözüm:** Steam sayfasını dikey dilim biter bitmez aç (02 §11 Faz 1 sonu). Ücretsiz fast food kampanyası zaten hazır bir demo; Steam Next Fest'e onunla gir. İki mutfakla 9,99 dolardan Erken Erişim, dört mutfakla 1.0'da 12,99-14,99. Tür EA'yı kabul ediyor (Tavern Keeper EA'da %94, 01 §1). Mobil global ile Steam EA arasına en fazla 1-2 ay koy. En yüksek kaldıraçlı ücretsiz kanal sorusunun cevabı da bu: Steam Next Fest ve istek listesi. Supermarket Simulator'ü hit yapan yayıncılar (01 §1, 4,2 milyon Twitch saati) onu PC'de oynadı. Mobilde tek bedava keşif kanalı Apple editör vitrini; çıkıştan 6-8 hafta önce öne çıkarma formunu doldur, "reklamsız, sayaçsız, kültürel kimlik" tam onların aradığı profil.

### 2. Ücretsiz kampanya dönüşüm tetikleyicisini öldürüyor

**Sorun:** 07 Şart 2 "hiç para vermeden bitirip memnun kalabilmeli" diyor; 08'e göre bu 4-6 saat. Satın alma ancak kampanya bitince anlam kazanıyor; oraya kurulumların tahminen %5-10'u ulaşır. Kilit (15 "Dört yuva") yüzünden satın alma mevcut kayda hiçbir şey eklemiyor: "şimdi al, sıfırdan başla." Mobilde tek ücretli SKU "ince" görünmez, oyuncu SKU değil ekran görüntüsü sayar; ince olan şey 11,99'luk paketin var olmayan iki mutfağı satması.

**Sonuç:** Premium kilit açma modellerinde kurulum başına dönüşüm zaten düşük tek haneli yüzdedir (yaklaşık, deneyim); tetikleyici 4-6 saat sonraya konunca %1-2 bandını (tahmin) aşmak zor. 30 bin kurulum, 300-600 satış.

**Çözüm:** (a) Üç günlük sıfırlama penceresini (15, satır 33) ücretli mutfağın **ücretsiz tadımına** çevir: Türk lokantasının ilk üç günü yeni bir yuvada bedava, üçüncü gün sonunda kilit ve satın alma ekranı. Oyuncu ne aldığını görmüş ve üç gün yatırım yapmış olur; iade riski düşer, 07'nin "satın alma öncesi neyi aldığını görsün" şartı kendiliğinden sağlanır. (b) Yıl sonu değerlendirme ekranını (08) ana satış vitrini yap: eleştirmen yazısı biter, "sıradaki plaket" teklifi gelir. (c) Satın alma mevcut kayda küçük bir şey eklesin, örneğin yeni mutfaktan bir "konuk yemek" haftası. (d) Paketi ikinci mutfak çıkana kadar mağazaya koyma. Türk mutfağı ilk ücretli olarak kalsın: üretim riski en düşük, basın kancası en güçlü, mekanik olarak fast food'a en uzak. Ama ilk güncelleme Japon değil İtalyan olsun; mobil global vitrinde en okunur mutfak o. Satın alma ekranı "esnaf lokantası" satmasın, "düzenli müşterilere veresiye aç, kira gününü riske at" mekaniğini satsın; alt yazılı Türkçe yemek adları (18 "Yemek isimleri kararı") kalabilir. İkinci satın almanın öncülü ücretli mutfağın tamamlanma oranıdır; plaket sistemi burada yardım eder, ölçülmesi şart.

### 3. Sıfır bütçeyle "soft launch" veri üretmez; metrik tanımları hatalı

**Sorun:** Soft launch bir ücretli kullanıcı edinimi aracıdır; 21 §D5 "ucuz kullanıcı edinimi" diyor ama bütçe sıfır. Küçük bir pazarda altı haftada birkaç yüz organik kurulum %1 ile %3 satın alma oranını ayırt edemez (kabaca 1-2 bin kurulum gerekir). 21'deki tablo takvim günüyle oyun gününü karıştırıyor: "7. gün tutundurma = ilk kira günü aşılıyor mu" ama ilk kira oyunun 7. günü, 3-6 dakikalık günlerle 30-40 dakikalık oynanış, ilk oturumda geçilir. "30. gün = kampanya bitiriliyor mu" da aynı hata; kampanya bir haftada biter. Google Play yeni bireysel hesaplardan üretim öncesi zorunlu kapalı test istiyor (belli sayıda test kullanıcısı, kesintisiz 14 gün; sayı dönem dönem değişti, konsoldan doğrula) ve 20 §C8 "test kullanıcısı nereden bulunacak" sorusunu açık bırakmış. Analitik "hiç mi asgari mi" (21 açık soru 4) kararsız; analitik yoksa yedi metriğin hiçbiri ölçülemez.

**Sonuç:** Altı hafta gider, elde gürültü kalır; global çıkış körlemesine yapılır.

**Çözüm:** Kapalı test için Türkiye'yi kullan; test kullanıcısı bulmak en kolay orası ve kapalı test puanı mağazaya yansımaz. Açık test Filipinler (hacim, ucuz Android cihaz çeşitliliği, İngilizce) artı Kanada (ABD'ye benzer ödeme davranışı). Android önce, iOS global çıkışta; 20'deki iOS cihaz sorusu böylece ertelenir. 300-500 dolarlık asgari edinim harcamasını bütçele; yalnızca tutundurma için 1-2 bin kurulum alır. Metrikleri düzelt: takvim D1/D7/D30 ayrı, "oyunun 7. gününe ulaşan yüzde" ayrı; huniye mağaza sayfası → kurulum, satın alma ekranı → satın alma, ücretli mutfak tamamlama oranı ve çökme oranı ekle (Play Console'un yaklaşık %1,1 çökme / %0,5 ANR eşiklerini aşmak görünürlüğü düşürür). Hedefler, tür için yaklaşık deneyim eşikleri: D1 ≥ %35, D7 ≥ %15, D30 ≥ %6; kampanya tamamlama ≥ kurulumların %10'u; ilk ay satın alma ≥ %2 veya tamamlayanların ≥ %25'i; çökmesiz oturum ≥ %99. Altındaysa global çıkışı ertele.

### 4. Ödemeyen %98'den sıfır gelir, fiyat merdiveni tek basamaklı

**Sorun:** 02 §10 gün sonu ekranında isteğe bağlı ödüllü reklam ve kozmetik dekor paketleri öneriyordu; 07 ve 21 bunları sessizce düşürdü, 05'te reklam "Yok". Araştırma sorunun reklamın varlığı değil yeri olduğunu söylüyor (01 §4 madde 7). Good Pizza, My Cafe ve Cooking Fever'ın üçü de çift para birimi, sayaç ve reklamla kazanıyor; premium mobil (Stardew mobil, Kairosoft serisi) ise PC'de kazanılmış markayla ya da onlarca oyunluk katalogla peşin fiyat alıyor. Markasız solo oyun peşin fiyatla kurulum alamaz, ücretsiz giriş doğru; ama o zaman ücretsiz kitleyi de bir şekilde paraya çevirmen gerekir. 4,99, tek ve ilk satın alma olarak yüksek bir eşik; altında SKU yok. Bölgesel fiyat kararı yok: ham kurdan 4,99, "ana pazarlardan biri" denen Türkiye'de (21 §D5) satın almayı öldürür. %20 paket indirimi zayıf; 11,99 mobil paket 12,99 Steam'in dibinde durunca "telefon oyununa PC parası" algısı doğar.

**Sonuç:** Gelirin tamamı %1-2'lik dilime bağlı; tek kötü değişkenle sıfırlanır.

**Çözüm:** Gün sonu hesap ekranına (02 §3 Aşama 4) yalnızca oyuncunun bastığı "bugünkü kârı ikiye katla" ödüllü reklamını geri koy; tek para birimi ilkesini de "enerji yok, sayaç yok, ikinci para yok" mesajını da bozmuyor. Bedeli: reklam SDK'sı rıza yükü getirir (bkz. 5). 1,99-2,99 dolarlık dekor paketleri ekle; 10 zaten ortam setleri tanımlıyor. Mutfak 4,99 kalsın, Apple ve Google bölgesel fiyat tablolarını doldur. Paket üç mutfak tamamlanınca 9,99 (yaklaşık %33). Steam 12,99-14,99, %10-15 lansman indirimi (Steam'de standart, görünürlük etkisi var); mobilde lansman indirimi yapma, düşük çapa bırakır. Not: Steam'de Türkiye artık dolarla fiyatlanıyor; kimlik pazarı orada tam fiyat öder.

### 5. Mağaza uyumu ve hukuk listesinde somut boşluklar

**Sorun:** Dokümanların hiçbirinde "satın almaları geri yükle" yok; Apple yönergesi 3.1.1 tüketilemeyen ürünler için bunu zorunlu tutar, reddedilme sebebidir. İade geri alma yok: Google 48 saat içinde kendi kendine iade veriyor; iade edilen mutfağın uygulamada yeniden kilitlenmesi gerekir (Google voided purchases, Apple sunucu bildirimleri), yoksa "al, oyna, iade et" sızıntısı tek SKU'lu modelde doğrudan gelirden gider. AB Dijital Hizmetler Yasası gereği AB'de satan geliştirici "tacir" beyanı verir; ad, adres, e-posta ve telefon mağaza sayfasında herkese açık görünür, şahıs olarak yayınlarsan ev adresin görünür. Steam, yapay zekâ üretimli içeriğin mağaza sayfasında beyanını istiyor; 05'teki hat (Tripo, Meshy, ElevenLabs, Suno) bunu zorunlu kılar ve cozy kitlede tepki riski taşır; D6 lisans denetimi bunu kapsamıyor. "Her mağaza için ayrı başvuru" (21) pratikte Google IARC anketi, Apple'ın kendi anketi, Steam isteğe bağlı; İtalyan mutfağındaki şarap eşleştirme (07) alkol referansı olarak derecelendirmeyi 12/13+ bandına çeker, bu iyi: çocuk hedefli beyan vermeme ve COPPA/Google Aile yükünü dışarıda bırakma imkânı. Loot box ve kumar yok; anketlerde "rastgele ödeme yok" beyanı yeter. "Kişisel veri toplanmıyor" (21 "Veri toplama ilkesi") iddiası doğru değil: tutundurma için kurulum kimliği gerekir, bu GDPR'da takma adlı kişisel veridir; gizlilik politikası, Apple gizlilik etiketi ve Google veri güvenliği formu her hâlde gerekir; reklam SDK'sı eklenirse AEA/BK için Google onaylı rıza platformu şart. Oyunun adı dokümanlarda yok, ad taraması ve tescil adımı yok; 01 §2 Supermarket Simulator'ün haftalar içinde kopyalandığını yazıyor.

**Sonuç:** Apple reddi, iade sızıntısı, kişisel adresin ifşası, klonlara karşı savunmasızlık.

**Çözüm:** D6'ya ekle: geri yükleme (yetki mağazadan gelir, 15'teki bulut kayıtla karıştırılmaz), iade iptali işleme, DSA için iş adresi (şahıs şirketi veya sanal ofis), Steam yapay zekâ beyanı, 13+ hedef kitle beyanı, kurulum kimliği için asgari rıza akışı (AEA'da opt-in, hukukçuya sor), Türk Patent'te ad tescili ve mağaza IP şikâyet süreci, Apple Küçük İşletme Programı ve Google %15 için başvuru (05 otomatik sanıyor, doğrula), yurt dışı gelir için muhasebeci. Steam iade penceresi iki saat: 08'deki "bütün sistemler ilk üç saatte açılır" hedefini iki saate çek.

## Kalem kararları

| Kalem | Karar | Gerekçe |
|---|---|---|
| D1 Gelir modeli (danışma) | DÜZELT | İçerik satışı ve güç satmama doğru; ama 02 §10'daki isteğe bağlı gün sonu reklamı ve kozmetik paketler geri gelmeli, üç günlük tadım tetikleyicisi kurulmalı. Aksi hâlde model ödemeyen %98'den hiçbir şey almıyor ve tetikleyici 4-6 saat sonrada kalıyor. |
| D2 Fiyat | DÜZELT | Mutfak 4,99 tamam. Paket ancak ≥2 mutfak varken ve 9,99. Steam EA 9,99, 1.0'da 12,99-14,99, %10-15 lansman indirimi. Bölgesel fiyat tablosu ve 1,99-2,99 dekor kademesi eklenmeli. |
| D5 Lansman planı | DÜZELT | Sıra tersine: Steam sayfası Faz 1 sonunda, Next Fest demosu = ücretsiz kampanya, Steam EA mobil globalden en fazla 1-2 ay sonra. Kapalı test Türkiye, açık test Filipinler + Kanada, Android önce; 300-500 dolar edinim bütçesi; metrik tanımları ve eşikler 3. sorundaki gibi. |
| D6 Hukuki | DÜZELT | Liste doğru ama eksik: geri yükleme, iade iptali, DSA tacir adresi, Steam yapay zekâ beyanı, 13+ beyanı, analitik rızası, ad tescili, komisyon programı başvuruları. Eklenince ONAYLA. |

## Cevapsız sorular

1. Ücretsiz kampanyayı bitiren oran ölçülmeden dönüşüm modeli kurulamaz. Faz 2 eğlence testi (02 §11) "60 günü bitiren" ve "bitirince ikinci mutfağı isteyen" oranını soruyor mu?
2. Steam'de iki mutfak, 8-12 saat içerik ve dokunmatik arayüz "mobil port" algısını nasıl aşacak? 19'da masaüstü ölçekleme ve klavye/fare planı var mı?
3. Yapay zekâ üretimli varlık oranına üst sınır koyacak mısın? Steam beyanı ve topluluk tepkisi buna bağlı.
4. Türkiye gelir pazarı mı, basın pazarı mı? Alım gücü fiyatı ve Steam'in dolar fiyatlaması karşısında oradan beklenen satış nedir?
5. "Bütçe yok" gerçekten sıfır mı? 500 dolarlık test edinimi ve 100 dolarlık Steam Direct bile bu cümleyle çelişiyor.
6. Analitik kararı (21 açık soru 4) D5'in ön şartı: hangi sağlayıcı, hangi rıza akışı, hangi saklama süresi?
7. Oyunun adı ne? Üç mağazada ve alan adında boş mu, marka taraması yapıldı mı?
