# Plan Durumu ve Eksikler Kütüğü

**Son güncelleme:** 9 Eylül 2026
**Amaç:** Planın tamamında neyin kararlı, neyin açık, neyin henüz yazılmamış olduğunu tek yerde görmek. Uygulamaya bu liste bitmeden geçilmeyecek.

## Durum işaretleri

- ✅ **Karar verildi.** Yazılı ve gerekçeli.
- ⏳ **Seçenekler hazır, karar bekliyor.** Analiz yapıldı, senin seçimin gerekiyor.
- ❌ **Henüz yazılmadı.** Üzerinde hiç çalışılmadı.
- 🔍 **Değerlendirme kararı.** 9 Eylül 2026'da beş bağımsız ajan tarafından verildi: ONAYLA, DÜZELT veya REDDET. Ayrıntı [review/00-sentez.md](review/00-sentez.md).

## Özet

| Alan | ✅ | ⏳ | ❌ | Toplam |
|---|---|---|---|---|
| A. Tasarım | 5 | 11 | 0 | 16 |
| B. Teknik | 2 | 5 | 0 | 7 |
| C. Üretim | 3 | 5 | 0 | 8 |
| D. İş | 3 | 3 | 0 | 6 |
| **Toplam** | **13** | **24** | **0** | **37** |

**Parti A ve B kapandı, Faz 0 açık.** Dokuz sorunun altısı cevaplandı ([22-cevaplar-ve-yon.md](22-cevaplar-ve-yon.md)). Ekonomi tabloları artık `tools/balance` tarafından üretiliyor; çekirdek sözleşmesi [23-cekirdek-sozlesmesi.md](23-cekirdek-sozlesmesi.md) bağlayıcı; sanat hattı render döngüsüyle [24-sanat-hatti.md](24-sanat-hatti.md) yeniden kuruldu. Açık karar: oyun adı. Ödüllü reklam ve Steam sayfası yayın öncesi aşamaya ertelendi (10 Eylül 2026). Unity 6.3 LTS kararlaştırıldı. Parti C-G Faz 0 ile paralel.

**Değişim kaydı**

- 9 Eylül 2026: mutfak seçimi kararı D1, D3 ve D4'ü kapattı. Aynı karar A14'ü zorunlu hale getirdi ve A11'in kapsamını genişletti. Bkz. [07-mutfak-sistemi.md](07-mutfak-sistemi.md).
- 9 Eylül 2026: oyun sonu için öneri yazıldı, A14 karar bekler duruma geçti. Bkz. [08-oyun-sonu.md](08-oyun-sonu.md).
- 9 Eylül 2026: oyun sonu modeli onaylandı. İçerik envanteri ve ilerleme eğrisi yazıldı, A5 ve A6 karar bekler duruma geçti. Bkz. [09-icerik-envanteri.md](09-icerik-envanteri.md).
- 9 Eylül 2026: yemek sayısı mutfak başına 32'ye çıkarıldı. Yeni kalem A15 mutfak kimliği eklendi, kütük 36 kaleme çıktı. Bkz. [10-mutfak-kimligi.md](10-mutfak-kimligi.md).
- 9 Eylül 2026: dördüncü mutfak Japon oldu, imza mekaniği çorba suyu ve tükenme. İsimli personel önerisi kabul edildi. Müşteri arketipleri yazıldı, A7 karar bekler duruma geçti. Bkz. [11-musteri-sistemi.md](11-musteri-sistemi.md).
- 9 Eylül 2026: arketip sayısı 20'ye çıkarıldı, sıklık kademeleri eklendi. Ekonomi sayıları ve müşteri formülleri yazıldı, A4 karar bekler duruma geçti. Faz 0 önündeki son büyük tasarım işi bitti. Bkz. [12-ekonomi.md](12-ekonomi.md).
- 9 Eylül 2026: Parti 1-5 tamamlandı, 37 kalemin hepsi yazıldı.
- 10 Eylül 2026: **Faz 0 ikinci dilim: simülasyon ve denge aracı.** Unity projesi kuruldu ve ayarları kodla uygulanıyor; sabit adımlı simülasyon bir servis gününü baştan sona koşuyor; denge aracı beş stratejiyle altmış günlük kampanyayı simüle ediyor. 88 test geçiyor. Simülasyon altı tasarım hatası buldu: sabır tek hızda tükenemez, hazırlama süreleri kapasiteyle çelişiyordu, aceleci müşteri ağır yemek sipariş edemez, ortalama fiş kola fiyatına iniyordu, malzeme maliyeti kasadan hiç düşülmüyordu, itibar bir haftada tavana vuruyordu. Ayrıntı [29-faz0-simulasyon.md](29-faz0-simulasyon.md).
- 10 Eylül 2026: **zaman modeli çözüldü** ([27-zaman-modeli.md](27-zaman-modeli.md), 47 kontrol). Servis günü 4.800 tick kalıyor, görev süreleri kapasiteden türüyor, aşçı eş zamanlı istasyon yürütüyor. Aynı çalışma mutfak saat dağılımının fiziksel olarak servis edilemez olduğunu buldu.
- 11 Eylül 2026: **Unity görünüm katmanı, ilk dilim** ([34](34-ilerleme-ve-kilit.md) §22). Altmış günlük ekonomi, iki mutfak, iki imza mekaniği, isimli müşteriler ve huylu personel vardı ve **hiçbiri ekranda değildi**. İçerik yükleme `IContentSource` portunun arkasına alındı (Android'de dosya yolu yok; Resources seçildi çünkü her platformda **senkron** okunuyor ve doğrulama yapan bir yükleyici asenkron olamaz), kat planı editör betiğinden **çalışma zamanına** taşındı ve ilk kez **testlendi** — odaların arsayı tam kapladığı, masa sayılarının kademe tablosuyla aynı olduğu (4/7/10/14) ve masaların birbirine değmediği bugüne kadar yalnızca Unity açılınca sınanıyordu. Plan çekirdek derlemesine girince `float` yasağının yansıma testi onu yakaladı; kural gevşetilmedi, **kapsamı yazıldı**: determinizm simülasyona ait, kat planı ekranda nerede durduğunu söylüyor. Yazılanlar: `GameHost` (sabit adımlı tick döngüsü — Unity'nin değişken deltaTime'ı çekirdeğe hiç girmiyor), `RestaurantView`, iki kademeli `CameraRig`, `HudView` ve sahneyi **kuran** bir editör aracı. Görünüm katmanı simülasyonu okur, ona yazmaz; yazan tek şey komut. **Bu katman burada derlenmedi** — `UnityEngine`'e bağlı, ilk derleme Unity açıldığında. **203 test.**
- 11 Eylül 2026: **dört yayımcı seviyesi inceleme — ve iki tanesi bugün yazdığım kodda hata buldu.** Dört ajan ayrı ayrı baktı: ticari yeşil ışık, sistem/denge, teknik risk, içerik/ses. Her bulgu koda karşı doğrulandıktan sonra uygulandı.

  **Çöken sınıf hatalar:**
  - **Kampanya sonu ekranı her karede yeniden açılıyordu.** `SeasonJustEnded` ekran KAPANANA kadar true kalıyor ve `CheckSeasonEnd` koşulsuz çağrılıyordu — 30 fps'te dakikada 1800 `EndScreen`, her biri `sim.Score()` çağırıp oyunun en ağır panelini kurarak. Oyuncu için sonucu daha kötüydü: "Serbest oyuna devam" tek bir `Pop()` yapıyor ve altından **aynı ekran** çıkıyordu. Altmış günlük kampanya, çıkılamayan bir ekranda bitiyordu. Tek satırla düzeldi; tur artık **yığın derinliğini** ölçüyor ve düzeltme kaldırılınca kırmızı döndüğü sınandı (yığın 6, beklenen 2).
  - **`link.xml` yoktu ve `Dto.cs:7` var olduğunu söylüyordu.** Android IL2CPP `High` budamayla derleniyor ve bütün içerik yükleme yansıma; budanan şey DTO'ların yazıcıları, sonuç çökme değil **sessiz varsayılan** → açılışta hata ekranı. Editörde ve Windows yapısında (Mono) görünmez — yani projedeki **hiçbir testin ulaşamadığı** tek yapılandırmada yaşıyordu.
  - **`URP/Unlit` yapıya hiç girmiyordu.** Bugün rozeti okunur yapmak için `Shader.Find` ile aldığım gölgelendiriciye projede tek bir varlık bile başvurmuyor; cihazda `null` dönecek, kod sessizce ışıklı malzemeye düşecek ve rozet **aynı gün düzelttiğim 1,47:1 kontrasta** geri kaçacaktı. `Shader.Find` editörde her zaman başarılı olduğu için görünmezdi. Gölgelendirici "her zaman dahil" listesine eklendi, sessiz düşüş kaldırıldı, ve tur artık **gerçek yapıda** malzemenin ışıksız olduğunu sorguluyor.
  - **Kayıt özeti sürüm taşımıyordu.** Sürüm 14, sıfır göç. Güncelleme sonrası yuva kartı sağlıklı bir kampanya gösteriyor (gün 43, kasa, itibar), oyuncu "Devam"a basıyor ve `Restore` fırlatıyor — yuva bir sonraki açılışta yine sağlıklı görünüyor. Özet artık sürümü taşıyor ve yuva **dürüstçe** bozuk görünüyor. (Göç ayrı ve daha büyük bir iş.)

  **Denge:** **32 yemeğin 20'sinin fiyatı müşteriye hiç yansımıyordu.** `ComputeSatisfaction` yalnızca ana yemeğe bakıyor, `OrderPrice` dört kalemi birden yazıyordu ve `SetPrice`in üst sınırı yok. Türk mutfağında tek içeceği (ayran, 16) on katına çıkarmak **kampanyanın bütün kârının altı katını** getiriyordu — sıfır risk, sıfır geri bildirim. Ceza artık her kalemin **fişteki payıyla** ağırlıklandırılmış sapması üzerinden. Fiyatlar içerik değerindeyken her sapma sıfır, yani referans botlar **birebir aynı** (25.678 / 31.378 / 835 / 301) ve kalibrasyon **ceza 5, 32 tohum** — değişmedi. Yeni kalıcı nöbetçi `pahali_ekstra` artık **8 sikkeyle batıyor**.

  **Android:** `runInBackground` Android'de kapandı (müzik telefon görüşmesinin üzerine çalıyordu), yön `AutoRotation` oldu (telefon ters çevrilince oyun baş aşağıydı ve düzeltmenin yolu yoktu), `installLocation` dahili (kayıt çıkarılabilir karta düşebiliyordu), mağaza simgesinden saydamlık kalktı (en düşük alfa 205; Play reddediyor) — ve üreteci de düzeltildi, yoksa geri gelirdi.

  **İçerik:** 12 huyun **açıklaması yoktu** — hıza ve ücrete dokunmayan dördü işe alım kartında "normal / normal" görünüyor ve oyuncu +8 puanlık biriyle hiçbir şey yapmayanı ayırt edemiyordu. "{0} bunu seviyor" metni tabloda **yazılıydı ve hiçbir kod okumuyordu**; yerine dokunmatik ekranda hiç görünmeyen bir tooltip vardı — hikâye ile mekaniğin buluştuğu tek menü sinyali fiilen görünmezdi. **İtibar tavanı** görünür oldu: `ReputationCapCenti` "arayüz bunu göstermeli" yorumuyla yazılmış ve hiçbir ekran okumuyordu; iyi oynayan bir oyuncu yedi masada 75'e dayanıp **32 gün** orada kalıyor ve sebebini hiçbir yerden öğrenemiyordu.

  **Arayüz:** `Accent` dokuz iş yapıyordu, üçünü bıraktı — "bu açık" yeşil yazı, "paran yetiyor" etkinlik, "ekranı kapat" düz düğme. Bölüm başlıkları renk yerine **ağırlık** taşıyor (`Theme.Head`); ekipman ekranında iki kardeş kartın başlığı farklı renkteydi.

  **Ayrıca iki denetim yanlış şeyi ölçüyordu:** `gen_loc.py`'nin yineleme taraması dosya genelini tarıyordu ve iki ayrı sözlüğün aynı anahtar adını kullanması on iki yanlış alarm verdi — kapsam sözlük içine çekildi (gerçek yinelemeyi hâlâ yakaladığı sınandı). Ve aynı muafiyet listesi `LocTests.cs`'te ikinci kez yazılıydı; ayrıştı.

  **Sonradan çıkan iki madde.** AAB'de iki yeni uyarı belirdi — "Round/Legacy simgeler kullanımdan kalktı" — ve sebebi benim eklediğim bir hata değildi: üç simge türü de doluydu ve `AndroidManifestPatch` değişikliğim bildirim üretimini yeniden koşturduğu için yüzeye çıktı. Ama peşinden gerçek bir yayın maddesi geldi: **`minSdk 25`, docs/19'un yazdığı "Android 10, 3 GB RAM, GLES 3.1" tabanıyla çelişiyordu** — yani yapı, oyunu koşturamayacak 2017 model bir telefona kurulmaya izin veriyordu ve o telefonun bırakacağı yorum geri alınamaz. Yeni bir karar değil, yazılı kararı koda geçirmek: **API 29**. Yan faydası, uyarlanabilir simgenin API 26'dan itibaren yeterli olması — eski ve yuvarlak yuvalar artık hiç doldurulmuyor ve uyarılar düştü.

  `check.py` 12/12, 220 test, tur **43/43**, kalibrasyon 6500 / ceza 5, AAB **31,1 MB / 0 uyarı** (link.xml koruması +0,5 MB). Doğrulandı: bildirimde `installLocation=internalOnly`, `preferExternal` yok; `defaultScreenOrientation` AutoRotation; `runInBackground` kapalı; `AndroidMinSdkVersion 29`.

- 11 Eylül 2026: **"sandalyeler ters" — sandalye değil, bütün mobilya.** Ölçüldü (her nesne yaw 0'da, kırmızı küp +Z'de, mavi küp −Z'de): **karakter +Z'ye, mobilyanın tamamı −Z'ye bakıyor.** Kod bu farkı bilmiyor ve açı yazarken "karakter gibi" düşünüyordu; ocaklar duvara, tezgâhlar dışarıya, sandalyeler masaya sırtını dönüyordu. Tek sabitte düzeldi (`RestaurantView.PropYaw`): çağrı yerleri karakter kuralında yazılıyor, sabit farkı kapatıyor. Kural [31](31-oda-ve-kamera.md)'e yazıldı — **yeni bir modelin "ön" yönünü varsayma, ölç**.

- 11 Eylül 2026: **68 çakışan model, 0'a indi — ve ölçüm aracı kendi hatasını iki kez yakaladı.** Kullanıcı "modeller üst üste binmiş gibi, karakterler hâlâ çok büyük" dedi. Göz kararı yetmediği için `Editor/PlacementAudit.cs` yazıldı: sahnedeki her nesnenin **gerçek pozdaki** kutusu ve kesişen çiftler ([31](31-oda-ve-kamera.md)).
  - **Araç kendi ölçtüğü şeyi yanlış ölçtü, iki kez.** `BakeMesh` mesh'i kemiklere göre deforme ediyor ve kemikler zaten ölçekli kökün altında — yani çıktının içinde ölçek var. Hem `useScale=true` hem `false`, `localToWorldMatrix` ile birleşince ölçeği iki kez uyguluyor. Her koşuda ayakta bir figürün boyunu `ArtPrefabs` hedefiyle karşılaştıran bir **doğrulama satırı** koyuldu; ikisini de o yakaladı (2,41 m / hedef 1,28). Bu araç zaten "yanlış şeyi ölçmek"e karşı yazılmıştı ve ilk iş tam onu yaptı.
  - **Dört kişi bu masaya hiçbir makul ölçekte sığmıyor.** Hücre 1,85 × 1,70 m, oturan figürün ayak izi 0,90 × 1,01 m; dört oturak için figürün eni ≤ 0,545 m olmalı, yani 0,66 m boyunda bir insan. Ekranda en fazla **iki** misafir çiziliyor — simülasyon etkilenmiyor, grup yine dört kişilik.
  - **Karakter 1,28 → 1,10 m.** Önceki indirimin ölçütü "figür/sandalye **boy** oranı" idi ve yanlış soruyu soruyordu: bu paketin figürleri boylarından çok **enleriyle** büyük.
  - Mutfakta **buzdolabı ilk ocağın içindeydi** (0,30 m); **aşçı ön tezgâhın içinde duruyordu**; personel birbirine giriyordu (0,13 m). Üçü de düzeldi. Sonuç: **0 çakışma**, iki mutfakta da.
  - Ayrıca `Lokanta/Figur olcek goruntusu`: tek figür, tek sandalye, 1 m'lik ızgara, **yandan**. "Oturuyor mu ayakta mı" sorusu tepeden bakışta cevaplanamıyordu; oturma pozunun çalıştığı böyle doğrulandı — sorun poz değil ölçekti.
  - **Menü listesi**: kilitli yemekler tam karttan tek satıra indi, "Menüde" anahtarı başlık satırına taşındı ve turuncu olmaktan çıktı. Hedef telefonda görünen yemek sayısı 1 kart + 2 satırdan 1 kart + 7 satıra çıktı; vurgu renginin dokuz işinden biri de gitti.

- 11 Eylül 2026: **arayüz incelemesi — iki ajan, biri piyasayı taradı biri kodu okudu.** Kullanıcı "arayüz daha ilgi çekici ve renk paleti daha uygun olabilir mi, geçiş/buton animasyonları eklenebilir mi, ayrıca karakterler biraz büyük" dedi. Her bulgu koda karşı doğrulandıktan sonra uygulandı:
  - **Hiçbir düğmenin basılı hâli yoktu.** `Theme.Btn` satır içi stil yazıyor ve UI Toolkit'te satır içi stil `:active` kuralını her zaman yeniyor — yani hazır tema hiç görünmüyordu. Ses vardı ama telefonda ses genelde kapalı, yani bir dokunuşun karşılığı çoğu oyuncu için **hiçbir şeydi**. Tek yerde düzeltildi, 59 çağrı yeri birden: koyulaşma + %3 küçülme, 90/150 ms.
  - **Kabul ile red aynı renkteydi.** Kıt bir müdahale hakkı harcanıyor ve gidip gitmediği yalnızca metinden anlaşılıyordu; `DidIntervene` bunu metinde çözmüş, görsel kanalda bırakmıştı. Beş red yolu artık `Bad`.
  - **Yıkıcı onaylarda `primary` olan taraf yıkıcı olandı** — 60 günlük bir kaydı silen ya da seviye atlamış bir personeli kovan düğme, en tıklanabilir görünen düğmeydi. `Theme.Bad` tanımlıydı ve oyunda hiçbir düğmede kullanılmıyordu. Üç yerde: "Vazgeç" birincil ve **önce**, yıkıcı olan kırmızı.
  - **Birinci gün üst şeritteki en doygun renk yanlış bir alarmdı.** Kampanya 30,0 itibarla başlıyor, eşik 40'tı — yani oyuncunun ilk öğrendiği şey kırmızı bir uyarıydı. Eşikler 70/45/25 oldu; başlangıç bandı **nötr**, renk artık durum değil **yön** bildiriyor.
  - **Akşam şeridi kendi bütçesini aşıyordu ve o aşama hiç ölçülmüyordu.** `CheckStrips` yalnızca sabah ve servis için koşuyordu; bütçeyi aşan tek aşama akşamdı (228 dp, sınır 220, salona kalan 165 dp ve taban 173). Altı sayının altısı da 26 dp kalındı ve paletin en parlak rengi `Warn` olduğu için "Bozulan", açıklaması gereken kârdan çok çarpıyordu. Şimdi iki manşet büyük, dört ikincil sayı 14 dp, hepsi tek satırda: **199 dp**. Ara bir denemede şerit 269 dp'ye çıktı ve **aynı turda yeni eklenen denetim onu yakaladı**.
  - **Sabır rozeti 1,47:1 kontrastla neredeyse görünmezdi.** Kodun kendi yorumu "emisyon kapalı olduğu için renkler ışıktan bağımsız okunuyor" diyordu; doğrunun tam tersi — emisyon kapalıysa renk tamamen ışığa bağlı. Yazılı yeşil aynı zeminde 7,57:1 verirdi, ışık beşte dördünü yiyordu. Rozet artık **ışıksız** bir malzeme kullanıyor, kalınlık 0,11 → 0,18 m.
  - **Servis şeridi yedi eş değer gri düğmeydi** — oyunun bütün fiil kümesi duraklat tuşundan ayırt edilemiyordu, ve hepsini besleyen kıt kaynak ("Hak 4") ekrandaki en az göze çarpan yazıydı. Üç müdahale ortak bir koyu tepsiye alındı, kalan hak **nokta dizisi** oldu, duraklat ve hız kip düğmesi boyutuna indi.
  - **Kasa açık bir kapsülde ve sayarak değişiyor.** Benzer oyunların taranmasında çıkan en yaygın kural: chrome koyu olsa bile sayı açık bir plakanın üzerinde durur (GPGP, My Cafe, Cooking Diary, Cooking Fever, Idle Restaurant Tycoon). Bir vurgu rengi harcamadan en çok okunan sayıyı ekrandaki en yüksek kontrastlı nesne yapıyor. Sayaç 360 ms yavaşlayarak gidiyor; 1.630 ¤'luk kira ile 12 ¤'luk satış artık aynı şey değil.
  - **Ekran geçişleri** tek karelik kesmeydi. Giriş 220 ms yavaşlayarak (saydamlık + 12 px), çıkış alttaki ekranın girişiyle. Yalnızca dönüşüm ve saydamlık — Unity'nin kendi belgesi yerleşim özelliklerinin geçişte yerleşimi yeniden hesaplattığını söylüyor, ve 30 fps'te bir kare 33 ms olduğu için Material'in ölçeğinde bir basamak yukarı çıkıldı.
  - **Karakterler 1,45 → 1,28 m.** Üç ölçek render edilip bakıldı: 1,45'te mutfak personeli tezgâhın üzerine eğilmiş gibi (figür/sandalye oranı 1,82), 1,15'te yetişkinler çocuk gibi (1,44), 1,28'de tezgâhın arkasına sığıyor (1,61). Ama yakın plan asıl sebebin **boy değil en** olduğunu gösterdi: paketin figürleri omuzdan ~0,8 m ve iki oturak arası 0,88 m. İki kişilik bir grup artık **karşılıklı** oturuyor (1,24 m) ve çift **X ekseninde** — ilk denemede Z eksenindeydi ve kamera 34° eğimle oraya baktığı için iki figür ekranda üst üste biniyordu.
  - Tur bir de **kararsız** çıktı: müdahale kontrolü x240 hızda koşuyordu ve seçilen grup kalkabiliyordu. O blok artık simülasyonu **duraklatıyor**; iki ardışık koşu 41/41.

  **Yapılmayanlar, gerekçesiyle:** `Accent` hâlâ dokuz ayrı iş yapıyor (birincil eylem, yıkıcı onay, açık anahtar, "parası yetiyor", ekranı kapat, bölüm başlığı, oyun adı, kendi eyleminin balonu, favori yemek noktası) — bunu bölmek 24 çağrı yerine dokunuyor ve tek turda yapılacak iş değil. Liste kartları hedef telefonda 232 dp ve görünür alan 219 dp, yani **bir yemek bile sığmıyor**; 32 yemeklik bir listede bu gerçek bir sorun ama tek satırlık açılır satırlara geçmek ayrı bir tur. Bildirim balonları hâlâ salonun önünü kapatıyor.

- 11 Eylül 2026: **ekranın %41'i "henüz senin olmayan" boş levhaydı — ve düşen gölgeler iki gün önce sessizce kapanmıştı.** Kullanıcı oyun içi görüntüye bakıp *"boş odalar yer kaplamasın, restoran tam ekran olan yerler gözüksün"* dedi. Peşinden gidilen her adım bir ölçüm bulgusu çıkardı ([31](31-oda-ve-kamera.md) §6):
  - **Açılmamış odalar artık çizilmiyor.** Birinci kademede arsanın 172,8 m²'sinin yalnızca 102,6'sı açık. Levhaların rengi bir zamanlar üç parlaklık ölçülerek dengelenmişti; sayılar doğruydu, **soru yanlıştı**. Ne zemin kuruluyor ne dokunma çarpışanı; genişleme artık gerçekten bir *açılış*.
  - **Kamera açık odaları çerçeveliyor** (`CameraFit.OpenBounds`). Tek başına beklenen kazancı vermedi — yatay ekranda çerçeveyi bağlayan şey **en değil derinlik** ve mutfak bloğu arsanın bütün derinliğini birinci günden kaplıyor. İlk hesabım ×1,34 demişti; o saf en oranıydı, **gerçek ölçüm ×1,07**.
  - **Asıl kaldıraç kamera açısıydı ve hiç ölçülmemişti.** 32°/34°/−12° bir tercihti. Ölçüldü: −12°'lik çevirme karenin yarısından fazlasını yiyor. **22°/34°/0°** ile karenin dolgusu %27 → %48 (açılış), dokunma hedefi tabanı **48 → 71 dp**. Kaybedilen şey hafif "2,5D çevrilmişlik"; render'a bakınca eğim ve mobilya yan yüzleri derinliği zaten veriyor.
  - **Ölçüm aracı oyunu ölçmüyordu.** `RoomLayout.Shoot` kamerayı kendi eliyle kuruyordu: `32f`, `Euler(34,-12,0)` ve kapalı bir mesafe formülü. `CameraFit` tam bunun için yazılmıştı ve araç ona hiç bağlanmamıştı — formül **%30 fazla mesafe** veriyor, üstelik arayüz çubukları hesaba katılmıyordu. İkisi düzeltilince **eski ayarın gerçek tabanı 48 dp** çıktı: Google'ın asgarisine tam tamına değiyor, payı yok. Belgedeki "81 dp" o yüzden yanlıştı. Araç artık `CameraFit`'i çağırıyor ve **en dar kare oranını da** ölçüyor.
  - **Gölge mesafesi yanlış birimdeydi.** Performans turu onu 25 → 14 m yapmıştı, gerekçe *"arsa 18 × 9,6 m"*. URP bu mesafeyi **kameradan** ölçüyor ve kamera 26–35 m uzakta: değer bütün düşen gölgeleri kapatmıştı. Hiçbir test görmedi, ekran görüntüsü gördü. 45 m (en uzak köşe 39,4 + pay), harita 512 → 1024.
  - **`ProjectSetup.ConfigureUrp` bütün bu sayıların ikinci kopyasıydı** ve performans turundan haberi yoktu: `ApplyAll` koşturmak MSAA'yı 1'den 4'e, gölge mesafesini 45'ten 25'e geri alıyordu. Eşitlendi; `tools/check_urp.py` ayrışmayı yakalıyor — **12. denetim**.
  - Bir de sessiz bir görüntü hatası: `RestaurantView.Clear` editör kipinde `Destroy` çağırıyordu ve o silme **hiç gerçekleşmiyordu** (toplu kipte kare sonu gelmiyor). İki mutfak da aynı masa sayısıyla kurulduğu için üst üste binen geometri yıllarca aynı görüntüyü verdi; "4 masa" yazan bir görüntü istenince ortaya çıktı.

  Sonuç: `check.py` **12/12**, **220 test**, tur **36/36**.

- 11 Eylül 2026: **düşük seviye bir telefonda 30 fps tutmuyordu — ve kaybın çoğu beş satırlık ayardaydı.** Bir performans incelemesi sahneyi saydı: en büyük kademede 14 masa, 70 mobilya, 28 rozet ve **46-67 figür**, her biri 1 Animator + 2 SkinnedMeshRenderer taşıyor → opak geçiş ~265 çizim çağrısı, gölge geçişi ~142, **toplam ~400**; düşük seviye bir Adreno 610 için pratik sınır 150-200. Düzeltilenler:
  - **URP ayarları:** 4x MSAA → 1, render ölçeği 1 → 0,8, gölge haritası 2048² → 512, gölge mesafesi 25 → 14 m (~~arsa 18×9,6 m, yani her şey gölge haritasının içindeydi~~ — **bu gerekçe yanlış birimdeydi ve aynı gün geri alındı: URP bu mesafeyi kameradan ölçüyor, değer bütün gölgeleri kapatmıştı; bir sonraki maddeye bak**), ek ışık PerPixel → kapalı (dolgu ışığı ikinci bir piksel geçişi açıyordu). Ara doku `Always` → `Auto`: o ayar tek başına her karede tam ekran bir kopyalama ekliyordu, **~1,2 GB/s bant genişliği, hiçbir şey çizmeden**.
  - **`gpuSkinning: 0` → `1`.** 134 iskelet mesh CPU'da deriliyordu; kemik sayısı 7 ve `TwoBones` zaten ayarlı, yani GPU tarafında en ucuz yol açıktı ve kullanılmıyordu.
  - **vSync çelişkisi.** `BuildPlayer` `vSyncCount = 1` yazıyor, `ProjectSetup` `= 0` yazıyordu — ve ikisi de **yanlış kalite seviyesine**: `ProjectSetup` editörde aktif olana (Ultra), Android ise seviye 2'yi kullanıyor. `vSyncCount != 0` iken Android `targetFrameRate`'i **yok sayıyor**, yani 120 Hz'lik ucuz bir telefonda oyun 120 fps hedefliyordu — hedef 30 fps tabanıyken. Ayar artık çalışma zamanında, `GameApp.Awake`'te: vSync kapalı, hedef **30**.
  - **67 Animator tek bir duruşu sonsuza kadar tekrarlıyordu.** Oturan müşteri kıpırdamıyor ama Animator'ı her kare değerlendiriliyordu: ~45 µs × 67 ≈ **3 ms/kare**, karşılığı sıfır. Geçiş bitince Animator kapanıyor (`Figure.Update`), duruş değişince geri açılıyor; havuza dönen figür `Release()` ile unutuyor.
  - **Tick bütçesi 400 → 40.** `Time.deltaTime` 0,333 sn ile sınırlı, yani en yüksek hızda tek karede 213 tick birikebiliyordu ve bütçe buna izin veriyordu — 22 ms'lik tek bir kare, ve **kendini besleyen** bir sarmal (uzun kare → daha büyük birikim → daha uzun kare). Ayrıca `AdvanceTasks` her tick **1024** iş yuvasını koşulsuz tarıyordu; aktif iş sayısı 56'yı geçemez, tarama artık grup başına.
  - **Her karede dize üretimi.** Üst şeridin akış satırı `string.Format` + üç `int` kutulaması yapıyordu, değerler hiç değişmese bile: ~150-200 bayt/kare, sekiz dakikalık serviste **~3 MB çöp**. Bileşenler ayrı tutuluyor, dize ancak sayı değişince üretiliyor. (`OccupiedTables` de bir özellik değil döngüydü.)
  - **Kayıt iki kez yazılıyordu.** Android arka plana atarken `OnApplicationPause(true)` **ve** `OnApplicationFocus(false)` ikisi birden tetikleniyor; kayıt ~12.500 alan ve ana iş parçacığında 30-60 ms — tam da işletim sisteminin uygulamayı öldürmeye hazırlandığı anda, iki kat.
  - **Ses sentezi.** Örnekleme hızı 44100 → **22050** (tık ve para sesinde fark duyulmuyor, bellek ve süre yarılanıyor), ve sentez artık **tembel**: önce dosyaya bakılıyor. Önce on klibin hepsi koşulsuz üretilip sonra dosya varsa çöpe atılıyordu.

  Tur değişikliklerden sonra **36/36 temiz**, 220 test geçiyor. Ölçüm yapılamadı (cihaz yok); incelemenin bütün sayıları "şu kadar nesne × şu kadar iş" tahmini, ama 400 çizim çağrısı + 134 CPU-skinned mesh + 4x MSAA + tam çözünürlük + 2048² gölge haritasının 33 ms'ye sığmayacağı ölçüm gerektirmiyordu.

- 11 Eylül 2026: **ölçüm aracının referansı kendi eliyle sakatmış.** `kredisiz` stratejisi ([12](12-ekonomi.md) §8'in altıncı sorusunu ilk kez ölçmek için yazıldı) şunu gösterdi: krediye hiç dokunmayan aynı oyuncu **14 masaya ve 100 itibara** çıkıp 27.853 ile bitiriyor, `makul` ise 7,4 masada kalıp 25.424 ile — çünkü botun kuralı borcu varken genişlemeyi **tamamen** kapatıyor. Yani aracın "iyi oynayan oyuncu" referansı yarı boyutta kalıyor ve bütün kalibrasyon hedefleri o referansa demirlenmiş. Düzeltme denendi ve **uygulanmadı**: kapı "karşılayabiliyor mu"ya çevrilince `makul` 39.877'ye çıkıp `plancı`'yı geçiyor ve kalibrasyon cezası **9'dan 32'ye** fırlıyor (imza bandı tabanın altına düşüyor, Türk büyüme çarpanı tavanı aşıyor). Tam bir yeniden ayarlama gerekiyor; yarım ayarlanmış bir denge belgelenmiş bir kusurdan kötü. Gerekçe kuralın yanında ve kapatma sırası [12](12-ekonomi.md) §8d'de.

- 11 Eylül 2026: **üç yeni strateji, üç yeni cevap.** `tek_yemek` dar menünün kabul testi; `kredisiz` yukarıdaki bulguyu açtı; `ucuz_fiyat` ([12](12-ekonomi.md) §8'in dördüncü sorusunun karşıtı — araç aşırı fiyatlamayı ölçüyor, indirim kırmayı ölçmüyordu) **sağlıklı bir tuzak** çıktı: %15 indirim daha çok müşteri (2036 vs 1916) ve biraz daha yüksek itibar getiriyor ama kasayı %40 düşürüyor (15.288 vs 25.424).

- 11 Eylül 2026: **servis şeridi ekrana sığmıyordu — 1103 dp / 873 dp.** Bir metin incelemesi Rubik'in gerçek harf genişliklerinden ölçtü: "Mutfağı hızlandır › Milkshake Makinesi" tek başına 319 dp, yedi öğeli satır 1103 dp, ekran 873 — **230 dp taşma**, ve en kısa istasyon adıyla bile sığmıyor. Sebebi bugün eklediğim iki şeydi: hedef adını düğmeye yazmak. Taşan yazı **kırpılıyor**, yani ekran çalışır görünüyor ve turdaki yükseklik bütçesi yeşil kalıyordu. İstasyon adı düğmeden çıktı (bildirim zaten söylüyor), etiketler kısaldı, ve tur artık **genişliği de** ölçüyor — istenen genişliği, kaplananı değil, çünkü kırpılmış bir düğme zaten sığmış görünür.

- 11 Eylül 2026: **iki metin sessizce kayboluyordu.** `gen_loc.py`'de `ui.evening.wages` ve `ui.evening.rent` **ikişer kez** yazılmıştı; Python son değeri alıp ilkini atıyor. Araç eksik anahtarı, boş metni ve fazlalığı yakalıyordu ama yinelenmeyi **göremiyordu** — çünkü dosya yüklendiğinde yineleme zaten kaybolmuş oluyor. Denetim artık sözlüğü değil **kaynağı** tarıyor. Ayrıca `ui.service.intervene` hiçbir yerde kullanılmıyordu, silindi. Metin düzeltmeleri: "Kâr payı" (temettü demek) → "Kâr marjı", "Malzeme sonrası" → "Brüt kâr", "Soğuk hava olmadan" → "Soğuk hava deposu olmadan", `notice.equipment`/`notice.storage` fiilsizdi, Ücret/Maaş ve yandı/battı ikilikleri tekleşti, düğme yazıları tek harf düzenine ("Servisi aç", "Günü kapat") geçti, ve `ui.hint.menu` işaret ettiği ekranı yanlış adla çağırıyordu.

- 11 Eylül 2026: **yıl sonu değerlendirmesi tek atışlıktı — telefonu kilitlemek 60 günün ödülünü kalıcı siliyordu.** Sıra şuydu: işaretle → **kaydet** → ekranı aç. Yani "görülmüş" bilgisi, oyuncu tek satır okumadan diske yazılıyordu; o anda telefon kilitlenirse `OnApplicationPause` bir kez daha kaydediyor, Android uygulamayı öldürüyor ve `SeasonJustEnded` bir daha `true` dönmüyordu. Kaybedilen şey küçük değil: yedi eksenli değerlendirme, plaket, altmış günün tek kapanışı — ve `EndScreen`'i açan başka hiçbir çağrı yeri yoktu. Bayrak artık ekran **kapanınca** konuyor, ve değerlendirme duraklatma menüsünden **yeniden açılabiliyor**.

- 11 Eylül 2026: **~728. günde kalıcı, geri dönüşsüz kilit.** Haftalık ücret `weeklyXpWageGrowthBp: 220` ile **bileşik** büyüyordu ve tavanı yoktu; gelir ise masa (en fazla 16) ve itibar (en fazla 100) ile tavanlı. 200. günde ücret +%80, 365'te 3,03 kat, **104. haftada `PowNano` `long`'u taşıyor**. İstisna `CloseDay`'in **ortasında** atıyordu: itibar düşmüş, stok yaşlanıp çöpe gitmiş, deneyim artmış ama aşama `Evening`'e geçmemiş. Unity düğme geri çağrısındaki istisnayı yutuyor, yani oyuncu "Günü Kapat"a basıyor, hiçbir şey olmuyor, tekrar basıyor — ve **her basışta aynı zarar bir kez daha işliyor**. Zam artık **iki katta** duruyor; 200 günlük bir koşu teste bağlandı.

- 11 Eylül 2026: **`Validate()` altı alanı kaçırıyordu ve içinde bir kayma hatası vardı.** `_stationTier[i] <= Tiers.Length` bir fazlaydı (`MaxTier = Length-1`); soğuk hava kademesi, kalite, masa→grup eşlemesi, varış planı ve sipariş edilen yemekler hiç kontrol edilmiyordu. Hepsi kaydı **sağlam** gösterip oyunun **içinde** çöküyordu — yuva açılıyor, salon çiziliyor, sonra ilk "Günü Kapat"ta `IndexOutOfRange`. `Validate`'in kendi yorumu "hata OYUNCUYA, oyunun içine girmeden önce söyleniyor" diyor; artık öyle.

- 11 Eylül 2026: **akşam raporu yüklemeden sonra yalan söylüyordu.** `_dayWages`, `_dayRent`, `_daySpoiled` ve `_revenueAll` kaydedilmiyordu. Kira gününü kapat, telefonu kilitle, geri dön: "Günün kârı" **haftanın en büyük giderini yok sayıp** büyük bir artı gösteriyor, kasadaki sayı ise düşmüş. Bu, `DayReport.WageCost`'un yorumunda anlatılan hatanın kayıt yolundan aynen geri gelmesiydi. Kayıt sürümü **14**.

- 11 Eylül 2026: **`HurryPartyJob` iki indis uzayını karıştırıyordu — patron ilgisi yanlış masanın yemeğini hızlandırıyordu.** `_kitchenTaskTarget` bir grup değil bir **iş** tutuyor (`job = party * 4 + k`); ham karşılaştırma yüzünden 7 numaralı grubun işi hiç hızlanmıyor, onun yerine **iş indisi 7 olan iş** hızlanıyordu. Üstelik kısaltılan süre aşçının bağlı kalma süresiydi, müşterinin beklediği duvar saati değil. Oyuncu hakkını harcıyor, "ilgi gösterildi" balonunu okuyor, mutfakta hiçbir şey değişmiyordu. Bir fonksiyon aşağıdaki `CancelTasksFor` bölmeyi **doğru** yapıyor — aynı hata sınıfı arayüzde de bulunmuştu (`PartyAtTable`).

- 11 Eylül 2026: **kayıt yazılamadığında tek kelime edilmiyordu.** `SaveStore.Save` her istisnayı yutup `false` dönüyordu ve o `false`'u **hiçbir çağrı yeri okumuyordu**. Depolaması dolu bir oyuncu otuz gün oynar, uygulamadan çıkar ve hiçbir şey bulamazdı. Artık günde bir kez bildirim çıkıyor.

- 11 Eylül 2026: **"Önerilen stoğu al" günün tamamını kilitleyebiliyordu.** Arayüz malzeme başına bir komut gönderiyordu; içerikte 77 malzeme var, günlük komut sınırı 256. Beş kez basmak günün bütçesini bitiriyor ve sonrasında fiyat, menü, işe alım, ekipman, genişleme, müdahale ve veresiye dahil **her** komut sessizce reddediliyordu. Parası yetmeyen oyuncu için daha kolay: alım reddedilse bile komut günlüğe yazılıyor. Yeni `OrderRecommended` komutu hepsini **tek komutta** yapıyor, ve red bildirimi artık **sebebini** söylüyor.

- 11 Eylül 2026: **kredi çekirdekte eksiksiz yazılıydı ve hiçbir ekranda düğmesi yoktu.** `TakeLoan`, `loanOptions`, sekiz haftalık taksit, `HasLoan`/`LoanWeeksLeft`/`LoanInstallment` — hepsi vardı, `Game/` altında `TakeLoan` geçen tek satır yoktu. Sonuç: oyuncunun nakit sıkışıklığına karşı hiçbir aracı yok; kasa eksiye düştüğü an batma merdiveni **kendiliğinden** işliyor, ekipman satılıyor, dükkân küçülüyor, sağlamlık ekseninden 30 puan gidiyor — **seçim sunulmadan**. Kredi ekranı yazıldı (yalnızca gerektiğinde beliriyor), ve `kredisiz` stratejisi [12](12-ekonomi.md) §8'in altıncı sorusunu ilk kez ölçüyor.

- 11 Eylül 2026: **soğuk hava merdiveni iki basamağa indi.** Üçüncü basamak hiçbir fiyatta çalışmıyordu: 8.000'de hiç satın alınmıyor (temkinli kural 32.000 kasa istiyor, oyuncu 25.000'de zirve yapıyor), 4.500'e indirilince alınıyor ve **−3.700 kaybettiriyor**. Sebep fiyat değil **takvim** — ikinci basamaktan sonra geriye yılda ~4.700 sikkelik zayiat kalıyor ve altmış günde hiçbir fiyat bunu ödetemez. İkinci basamağın `keepBp`'si 6000→7000 oldu (eşik 4 günden 3'e, korunan değer %67'den %83'e) ve **aynı fiyata iki buçuk kat fayda** verdi. Basamakları eşitlemek için birinciyi de düşürmek denendi ve **daha kötü** oldu (+2.720 → −234): birinci basamak zaten iyi ayarlıymış. Merdivenin tamamı 12.560'tan **4.560** sikkeye indi ve iki basamak da kendini ödüyor.

- 11 Eylül 2026: **Smart App Control'ün gerçek mekanizması bulundu.** Engel dosyanın **karmasına** bağlı ve çekirdek `<Deterministic>true</Deterministic>` ile derlendiği için aynı kaynak her zaman aynı ikiliyi üretiyor — yani engellenen bir derleme **yeniden derlemekle düzelmiyor**, sonsuza kadar kalıyor. "Bir gün Debug, ertesi gün Release" görüntüsünün sebebi buydu. Çözüm `-p:Deterministic=false`: derleyici her seferinde yeni bir modül kimliği gömüyor. Hem `Debug` hem `Release` engelliyken bu bayrakla ilk denemede geçti. `tools/check.py` ve `calibrate.py` bunu kendiliğinden yapıyor. Önceki iki teşhis ("netstandard2.1 engelleniyor", "yapılandırma dönüşümlü") **yanlıştı** ve ikisi de saatler kaybettirdi.

- 11 Eylül 2026: **yayın denetimi — paket içeriden açıldı.** İmza bloğu okundu (hata ayıklama sertifikası), ikili bildirim çözüldü, `lib/` sıkıştırması ve lisans metinlerinin derlemeye girip girmediği doğrulandı. Düzeltilenler: **yerel kütüphane paketleme** — `BuildPlayer`'ın yorumu yıllarca "sıkıştırılmış kalsın" diye söz verip kodda hiç yapmıyordu ve Unity 6'da o API yok; Gradle yaması kurulum ayak izini **108 MB'dan 84 MB'a** indirdi ve **AAB 30,8 MB kaldı**. Ayrıca ilk kez **AAB üretildi**, sürüm kodu ve imzalama parametreli hale geldi (parola ortam değişkeninde, depoda değil), mağaza simgesi **512×512 RGBA** olarak üretiliyor (eskisi alfasızdı ve Play reddederdi), Apache-2.0 **tam metni** ve üç Kenney paketinin lisansı derlemeye girdi, atıf tablosundaki iki yanlış satır düzeltildi. Kalanlar ve gerekçeleri: [21](21-is-ve-yayin.md) "Yayın denetimi".

- 11 Eylül 2026: **dar menü baskın stratejiydi — oyunun en derin denge hatası.** `Awaited()` yalnızca *ekipmanı olmayan* yemekleri "soruldu ama yok" sayıyordu; menüden çıkarılan yemek hiçbir zaman sorulmuyordu, yani menü daraltmanın talep tarafında **sıfır bedeli** vardı. Ölçüldü: tek ana yemek tutan oyuncu makul oyuncuyu fast food'da %12, Türk mutfağında %38 geçiyordu. İki sonucu vardı — soğuk hava deposunun ikinci ödülü (menü genişliği taşımak) **değersizdi**, ve otuz iki yemeklik envanterin var olma sebebi ortadan kalkmıştı. Ceza artık **oranlı** (açık ana yemeklerin yarısı menüde değilse yarım); mutlak sayıyla denendi ve aşırıydı — makul daraltan ile tek yemek tutanı aynı kefeye koyup ikincisini dokuzuncu günde iflas ettiriyordu. `tek_yemek` stratejisi denge aracında bir **kabul testi** olarak duruyor.

- 11 Eylül 2026: **açılış stoğu gizli bir sübvansiyondu.** `RestockAll(6000)` yetmiş yedi malzemenin **hepsine** altışar kilo koyuyordu — menüde olmayanın, kilidi açılmamışın malzemesi dahil. Kırk dördü bozulabilir olduğu için birinci gece **14.713 sikke** çöpe gidiyordu (başlangıç kasasının üç katı), dayanıklı olanlar ise günlerce süren ücretsiz bir başlangıç hediyesiydi. Görünmediği için zararsız sanıldı; akşam raporuna "çöpe giden" satırı eklenince ortaya çıktı. Stok artık **menüye göre bir günlük**. Makul oyuncunun zayiatı malzemenin %57'sinden **%13'üne** indi.

- 11 Eylül 2026: **tatlılar hiç sipariş edilmiyordu.** Sipariş modeli tatlıyı istiyordu (`DessertChanceBp`) ama hal modeli malzemesini **hiç almıyordu**: her iki mutfağın tatlıları açılış stoğu bitince ulaşılamaz oluyor, `CanMake` false dönüyor, sipariş sessizce düşüyordu. Blanket açılış stoğu bunu örtüyordu. Ayrıca yan ve içecek stoku sabit `/4` ile bölünüyordu ("yaklaşık dört seçenek var") — menüde tek içecek varken ihtiyacın dörtte biri stoklandığı için müşteri kapıdan dönüyordu. Sayım artık gerçek.

- 11 Eylül 2026: **servis 8 dakika değil 8 SANİYE sürüyordu.** `TimeScale` varsayılanı 60'tı: 480.000 sim-ms, saniyede 60.000 sim-ms — sekiz gerçek saniye. [16](16-ekranlar-ve-ogretici.md) servis için **90-180 sn** diyor. Altında kalan her şey çöküyordu: en sabırsız müşterinin **tüm sabrı 0,17 saniye**, yani masa rozetinin mavi-sarı-kırmızı geçişi göz kırpmasından kısa; kamera geçişi tek yön 0,35 sn, yani masa seçmek servisin %10'u; bildirim balonu 3,5 sn, yani servisin %44'ü. Servis aşamasının bütün arayüzü insan tepki süresine göre yazılmıştı ve hiçbiri yetişmiyordu. Varsayılan **4** (120 sn); eski 60 artık hız düğmesinin en üstü, "×16".

- 11 Eylül 2026: **müdahale hedefi artık oyuncunun kararı.** Önceden "Çay ikram" her zaman `MostImpatientParty`'ye gidiyordu; servis sırasında oyuncunun tek kararı "şimdi mi, sonra mı" idi ve **kime** sorusunu oyun cevaplıyordu. Masaya dokunmak artık hedefi seçiyor (ikinci kamera kademesinin işi de bu oldu); seçim yoksa eski davranış duruyor, yani zorunluluk değil incelik. **Masa indisi ile grup indisi karışıyordu** ve ikram bambaşka bir masaya gidiyordu (`PartyAtTable` çevirisi eklendi); üç düğme de koşulsuz "oldu" diyordu, oysa çekirdek dört ayrı yerde sessizce reddediyor.

- 11 Eylül 2026: **müdahalenin takası ölçüldü.** Sabır uzatması masayı daha uzun işgal ediyor, yani kurtarılan grup hizmet edilebilecek başkasının yerini alıyor: müdahale eden bot hiç müdahale etmeyenden **daha az** müşteri ağırlıyordu. Üç varyant 16 tohumla koşuldu; ödül memnuniyete kaydırılıp sabır katı 3'ten 2'ye indirilince takas düzeldi (Türk mutfağında +159 müşteri, +1.859 kasa). Bu üç sayı **kodda** duruyordu — üstelik `EconomyConfig.WithMorale()` içinde, adı bile yanlış bir yerde — yani denge aracı için yoktular; artık içerikte ([23](23-cekirdek-sozlesmesi.md) §8.2).

- 11 Eylül 2026: **denge aracının sekiz tohumu bazı kontroller için gürültüydü.** Aynı ayar, aynı içerik: 8 tohumda müdahale oranı %95,1 (kontrol kırılıyor), 16'da %98,4, 32'de %98,6 (geçiyor). Yani araç, geçmesi gereken bir dengeyi kırmızı gösteriyordu ve o kırmızıyı kovalamak var olmayan bir sorunu kovalamak olurdu. Ucuz **arama**, titiz **doğrulama**: sweep sekizle, kazanan otuz ikiyle. Ayrıca gelir tablosunun mutabakatı Türk `imzaci` satırında **−2.441** sapmıştı ve hiçbir uyarı çıkmıyordu; açık veresiye kasa hareketine ekleniyor ama cirosu hiç kaydedilmiyordu. Ciro artık kümülatif okunuyor ve fark sıfır dışıysa **uyarı çıkıyor**.

- 11 Eylül 2026: **fast food ile Türk mutfağı bozulabilirlik ekseninde tıpatıp aynıydı.** [13](13-veri-semalari.md) 0,20 ve 0,60 vaat ediyordu; ölçüldü, ikisi de **0,58**. Yani iki mutfağın en somut oynanış farkı olduğu söylenen şey hiç var olmamıştı. Fark artık bir **kuraldan** geliyor: fast food'a özel bir malzeme gerçek bir lokantada dondurulmuş ya da kavanozda geliyorsa bozulmuyor. Taze kalan yalnızca burgerin **üstüne** konanlar. Sonuç 0,42 / 0,57 — fast food affediyor, Türk mutfağı affetmiyor.

- 11 Eylül 2026: **ilk beş dakika yazıldı.** [16](16-ekranlar-ve-ogretici.md) dakika dakika bir öğretici tarif ediyordu ve kodda tek satır karşılığı yoktu. Yedi ipucu, her biri **bir kez**, anlattığı şeyin yanında, tek cümle; cihazda saklanıyor, kayıtta değil. Ayarlardan geri getirilebiliyor. Şerit **bildirim alanında** duruyor — alt şeride konulunca üst+alt 226 dp'ye çıkıyordu, yani 393 dp'lik bir telefonun %58'i; turdaki **şerit bütçesi ölçümü** bunu yakaladı ve artık 144 dp.

- 11 Eylül 2026: **iki test yanlış şeyi doğrulamaya başlamıştı.** Kalite testi iki farklı yemeği karşılaştırıyordu ve açılış stoğu menüye bağlanınca mekaniği değil **stok harmanını** ölçmeye başladı (iki etin de değeri tam −2000 olduğu hâlde −1965 ve −1939 çıkıyordu); artık her yemek **kendi** malzemeleriyle ölçülüyor. Veresiye testi "sık gelen arketip" kuralını doğruluyordu — o kural düzenli müşteri içeriği yokken doğruydu; içerik yazılınca kural `veresiyeEligible`'a geçti, test geçmedi ve **yedek** kuralı sınamış oldu. İçerik bunu bilerek böyle kurdu: veresiye **sıklığa değil tanışıklığa** açılıyor, mahallenin emeklisi kapının önünde bırakılmıyor.

- 11 Eylül 2026: **ses katmanı dosya kabul ediyor.** Hepsi kodla sentezleniyordu ve gerekçesi ticariydi; gerekçe yarım doğru — lisans gerçek bir risk ama basit dalga biçimleri bir lokanta oyununda ucuz duyuluyor. `Sfx.Init` artık **önce `Resources/ses/` altına bakıyor**, yoksa senteze düşüyor: dosya eklemek kod değişikliği istemiyor, klasör boşken oyun eksiksiz çalışıyor ve APK'ya sıfır bayt ekliyor. On dosyanın adı, işi ve lisans kuralı `Art/ATIF.md`'de; tur kaçının dosyadan geldiğini **raporluyor** (şu an 0/10), ki "sesler tamam" cümlesi sentezin üzerini örtmesin.

- 11 Eylül 2026: **tasarımı kapatan iki sistem yazıldı** ([34](34-ilerleme-ve-kilit.md) §19-20). **İsimli düzenli müşteriler**: mutfak başına on kişi, arketibini taban alan davranış, sevdiği yemek, kampanyaya giriş günü ve hikâye sahneleri. En önemli değişmez, isimli müşterinin talebi **şişirmemesi** — günün planına eklenmiyor, planın içinden yer alıyor; aksi halde her yeni isim ekonomiyi büyütür ve kalibrasyon her içerik eklemesinde kayardı. Veresiye uygunluğu artık "sık gelen arketip" türetiminden çıkıp **isimli müşteriye** taşındı ([13](13-veri-semalari.md) baştan böyle yazmıştı); aday kitle daraldığı için isteme oranı %12'den %40'a çıkarıldı. **Personel huyları ve moral**: on iki huy, çakışmalar simetri açısından yüklemede denetleniyor, hız artık tek çarpanda toplanıyor (deneyim + huy − moral − yoğunluk − yorgunluk). Üretecin denge kuralı **iki huyu yakaladı** (müşteriyle iyi anlaşan, ekip moralini yükselten — ikisi de bedelsiz görünüyor); bedelleri huyun içinde değil **havuzun** içinde: her birinin çakıştığı bir kötü ikizi var, yani iyi huy seçilen bir avantaj değil bir şans. Moral iki kez yanlış kuruldu ve ikisini de ölçüm söyledi: docs/14'ün tablosu bir **olay listesi**, sürüklenme modeli değil — sadece olayları uygulayınca iyi yönetilen bir dükkânda bile bütün kadro bir ayda istifa ediyordu; ve "yoğun gün" mutlak eşik değil **kadroya göre** eşik (aynı müşteri iki aşçıyla sakin, bir aşçıyla yorucu). Ayrıca bir kurucu hatası: başlangıç kadrosu huy ve moral **almıyordu**, ilk aşçının morali 0 ile başlıyordu — istifa eşiğinin altında — ve restoran **ikinci gün aşçısız** kalıyordu. Huy sistemi dengeyi bozdu (fast food iyi oyuncunun itibarı 96,5 → 87) ve sebep üçüncü kez de **bot** çıktı: her etki tek tek kapatılarak ölçüldü ve tek suçlu `wageBp` idi — kendisi zararsız (kişi başı +%1,2) ama **oynaklığı** eşiğe bağlı kararları tek yönlü geriletiyordu. Eksik olan şey mekanik değil **karar**dı: [14](14-personel-sistemi.md)'ün **üç adaylı işe alım havuzu** yazıldı (üç günde bir yenileniyor, alınan adayın yeri hemen dolmuyor) ve denge aracı hem aday seçiyor hem kötü personeli değiştiriyor. Sonuç huysuz tabanı da **geçti**: `makul` 24.540/96,5 → **28.617/99,3**. Başlangıç aşçısı huysuz başlıyor, çünkü onu oyuncu seçmiyor — kampanyanın ilk gününde görünmez bir zar atmak olurdu. Bir kalibrasyon hedefi de tavana çarptı: `makul` 99,3'e çıkınca “müdahale itibarı artırmalı” ölçüsü anlamsızlaştı (aynı koşuda Türk mutfağında müdahaleci 100,0, makul 96,1 — boşluk olan yerde çalışıyor) ve hedef “hiçbir zaman daha kötü değil”e çevrildi. **196 test, iki mutfakta da kalibrasyon cezası sıfır.**
- 10 Eylül 2026: **küçük borçlar kapatıldı.** Üç şey: (1) bu belgenin **durum tablosu eskiydi ve fark edilmemişti** — "88 test", "hal aşaması ❌", "komut günlüğü ❌" yazarken üçü de yanlıştı; tablo yeniden yazıldı ve her satıra onu doğrulayan komut kondu. (2) Denetleyicinin 4. kontrolü 63 alan sayıyordu ve **çoğu sahteydi**: [13](13-veri-semalari.md) şeması Faz 0'dan önce yazıldı, [23](23-cekirdek-sozlesmesi.md) §2.2 sonradan bütün birimleri tamsayıya çevirdi, yani `serviceSeconds` gibi adların çoğu uygulanmış alanların eski adı. Artık üç gruba ayrılıyor — **adı değişti** (31 alan, uygulanmış), **türetiliyor** (8 alan, [34](34-ilerleme-ve-kilit.md) §11 gereği içeriğe yazılmıyor), **yazılmamış** (24 alan, gerçek kuyruk: düzenli müşteri, personel huyu, iş yükseltmesi, yıl sonu ekseni, sanat hattı). `tools/audit_content.py` **ilk kez çıkış 0 veriyor**. (3) **Kayıt göç zinciri bilinçli erteleme olarak yazıldı**: kayıt sürümü bir haftada 1'den 10'a çıktı, bugün zincir yazmak onu da atılacak on göç yazmak demek; şu anki davranış sessiz değil, `Restore` farklı sürümde açık hata veriyor. Zincir ilk oynanabilir sürüm dışarı çıktığında yazılacak — bir başkasının kaydı ilk kez var olduğunda.
- 10 Eylül 2026: **imza mekanikleri yazıldı** ([34](34-ilerleme-ve-kilit.md) §18). [07](07-mutfak-sistemi.md) imza mekaniğini "en önemli satır" diye yazmıştı, [23](23-cekirdek-sozlesmesi.md) §8.2 şemasını da vermişti — **blok hiç yoktu ve mutfaklar sorunsuz yükleniyordu**, yani iki mutfak farklı menü taşıyan aynı oyundu. Fast food'a **kombo** (ana+yan+içecek kesin geliyor, indirimli tek fiyat, aşçıyı %20 daha uzun bağlıyor), Türk'e **veresiye** yazıldı; ikisi de [09](09-icerik-envanteri.md)'un dediği gibi **ikinci mevsimin ilk günü** açılıyor. Veresiye üç kez yanlış yazıldı ve üçünde de ölçüm söyledi: prim düğmesi olarak yazılınca işe yaramadı (**iyi oynayan zaten itibar tavanında**), sadakat talebi olarak yazılınca da yaramadı (**masalar zaten dolu, %97 servis**), doğrusu ters yöndeydi — veresiye teklif edilen bir prim değil **istenen** bir şey: sık gelen müşteri istiyor, vermezsen memnuniyet düşüyor, verirsen nakit bağlanıyor ve hesabını kapatan üstüne %12 koyuyor. Deftere yazılana çay konuyor ve çay tahsilat şansını %85'ten %95'e çıkarıyor (`teaCostCenti` böylece okunmaya başladı). Ölçüm: itibarı tavanda olmayan küçük lokantada veren, geri çevirenin **%8 önünde**; tavandaki oyuncuda **nötr** — personel deneyimiyle aynı şekil, ve doğrusu bu. Kalibrasyona yeni hedef: `imzacı`, `makul`'ün %90-%130'u arasında kalmalı (alt sınır tuzak olmadığını, üst sınır mecburiyet olmadığını sınıyor), ölçü **kasa + defter**. **171 test, iki mutfakta da kalibrasyon cezası sıfır.**
- 10 Eylül 2026: **döner ve pide yazıldı — ve üretecin kilit sistemini sildiği bulundu** ([34](34-ilerleme-ve-kilit.md) §17). Türk menüsü 32'de kalarak dört yemek değişti: `doner` ve `iskender` **döner ocağına**, `kiymali_pide` ve `lahmacun` **pide fırınına** bağlı; dördü de ana rolde. Asıl bulgu bunu yazarken çıktı: `content/dishes/*.json` üretilen dosyalar ama `requiresStationTier`, `unlockReputationCenti` ve adlandırılmış istasyon adları oraya **elle** yazılmıştı ve `gen_dishes.py` onları bilmiyordu — yani içerik üreticisini çalıştırmak, itibar + ekipman kilidinin tamamını tek komutta siliyordu, hiçbir test kırılmadan. Üçü de artık üretiliyor, ekipmanın `opens` listesi de elle tutulmak yerine yemeklerin kendi `station` alanından **türetiliyor**, ve üreteci iki kez çalıştırmak bit bit aynı içeriği veriyor. Yeni içerik dengeyi bozdu ve iki ihlalin ikisi de yine **bot kararıydı**: `Equipment.Upgrade` kredi varken hiçbir isteğe bağlı ekipman almıyordu, dolayısıyla genişlemeyen oyuncu döner ocağını hiç alamıyor ve her gün sorulan yemeğe "yok" diyordu (itibar 85,1 → 50,7). Kredi varken yalnızca **menü açan** ekipman, iki haftalık sabit gider korunarak: `makul` 30.364 → **33.208**, itibar 64,2 → **100,0**, büyüme çarpanı 4,14 → **3,80**. **153 test, iki mutfakta da kalibrasyon cezası sıfır.**
- 10 Eylül 2026: **denetleyicinin kuyruğu kapandı: dört sistem yazıldı, bir alan silindi** ([34](34-ilerleme-ve-kilit.md) §13-16). **İstasyon hızlandırma** ([02](02-tasarim-onerisi.md)'nin yazılmamış üçüncü müdahalesi): patron pişirmiyor, o istasyondaki işlerin kalan duvar saatinin %40'ını siliyor; boş istasyon hakkı yakmıyor. **Hal fiyat oynaklığı** ([12](12-ekonomi.md) §3): `priceVolatilityBp` artık okunuyor, gün açılışında her malzemeye ±%25 bandında günlük çarpan atılıyor — sabahki stok kararı bugünün fiyatını görüyor. Oynaklık dengeyi bozmadı, **düzeltti** (kalibrasyon cezası 8 → **0**): soğuk hava merdivenine bir karşılık yarattı. **Personel deneyimi** ([14](14-personel-sistemi.md)): çalışılan her gün 1 puan, 30 puanda seviye, azami 3, seviye başına +%10 — ve kısalan şey `attendMs`, `prepMs` değil ([27](27-zaman-modeli.md) Karar D). Merdiven abartılarak ölçüldü: etki **dar kadroda** çıkıyor (`genislemeyen` itibarı 64,7 → 89,4), büyük işletmede doygun. Kalan üç alan üç farklı kaderi aldı: `weeklyWageMultiplierBp`, `unlockSeason` ve `ownerPool` **değişmeze** çevrildi (üçü de üretecine karşı açılışta doğrulanıyor), `Crew.SalonWorkMicro` **silindi** — dört kurucusundan ikisi ona `0` yazıyordu, yani ölü değil *tuzak*tı. Denetleyicinin 1., 2. ve 3. kontrolü artık **hiçbir şey bulmuyor**. Ayrıca `calibrate.py` sessizce yanlış cevap veriyordu: harness'ın Debug çıktısı bu makinede engelleniyor, hata yutuluyor, yedi adayın hepsi eşitleniyordu — artık `-c Release` ile koşuyor ve boş tabloyu cezaya çevirmek yerine hata fırlatıyor. **150 test, iki mutfakta da kalibrasyon cezası sıfır.**
- 10 Eylül 2026: **sipariş tercihi türetilerek yazıldı** ([34](34-ilerleme-ve-kilit.md) §11). docs/13'ün `orderPreference` şeması 24 arketip için elle ağırlık tablosu isterdi ve o tablolar uydurma olurdu; onun yerine **zaten yüklü** olan `priceSensitivityBp` ve `tipChanceBp` alanlarından türetildi. Fiyata duyarlı müşteri artık gerçekten ucuz seçiyor: fast food'da pazarlıkçının ort. fişi 29,1, yemek eleştirmeninin 43,2 — **%48 fark, sıfır yeni içerikle**. İyi oyuncu 26.434, itibar 92,4. **İki mutfak da temiz.** 131 test.
- 10 Eylül 2026: **adlandırılmış ekipman** ([34](34-ilerleme-ve-kilit.md) §10). docs/09'un "mutfak başına 10 özel pişirme istasyonu" planı uygulandı: payılaşılan altının ardına, başlangıçta OLMAYAN, satın alınana kadar yemeklerini kilitli tutan ekipman. Türk'te **taş fırın** (7 yemek), fast food'da milkshake ve waffle makinesi. Döner ve pide içerikte olmadığı için bağlanamadı; mekanizma hazır, kalan iş içerik. Ekipman gelince `plancı` fast food'da battı ve sebep içerik değil **strateji** çıktı: maaş parasını ekipmana yatırıyordu. Haftalık ödemeyi koruyan kural onu 17.501'den **21.468**'e çıkardı. **İki mutfak da temiz, kalibrasyon cezası sıfır.** 129 test.
- 10 Eylül 2026: **malzeme kalitesi yazıldı** ([34](34-ilerleme-ve-kilit.md) §9). 77 malzemenin üç kademelik kalite tablosu okunmuyordu. İçerik kararı zaten taşıyordu: en hassas altı malzemenin **hepsi et**, tuz ile karabiber duyarsız — bu yüzden tek küresel ayar yetiyor ve yemeğe göre farklı sonuç veriyor. İlk uygulamada yemeğin kalite etkisi malzemelerin **ortalaması** alındı ve Türk mutfağı yakaladı: ucuz soğan ucuz eti gizliyordu, `ucuz_malzeme` iyi oyunu geçiyordu. Kural **en belirleyici malzeme**ye çevrildi; artık iki mutfakta da tuzak (9.117 ve 11.604'e karşı ~25.000). 127 test, kalibrasyon cezası sıfır.
- 10 Eylül 2026: **ilerleme sistemi: mevsim, kilit, karmaşıklık, soran müşteri** ([34-ilerleme-ve-kilit.md](34-ilerleme-ve-kilit.md)). `tools/audit_content.py` yazıldı ve **beş ölü içerik** buldu; dördü canlandırıldı. Yemek kilidi artık takvim değil **itibar arti ekipman** istiyor, yani ekipman almak menü açıyor. Kilit pasifliği ödüllendirince (kilitli yemek = masrafsız yemek) **soran müşteri** mekaniği eklendi ve deliği kapattı. İki mutfak farkının sebebi mutfakta değildi: itibar sönümlemesi tepeye yakın bıçak sırtıydı, 3,5 puanlık memnuniyet farkını 20 puanlık itibar farkına çeviriyordu; **sönümlemeye taban** kondu ve fark 4,6 puana indi. **Talep eğrisi 20 puanın altında dikleştirildi** ve denge aracına "boşaldı" sütunu eklendi: itibarın o eşiğin altına indiği gün, yani dükkânın görünür biçimde boşaldığı an. Hiçbir şey yapmayanın dükkânı **5. günde** boşalıyor, kötü yöneten 32. güne kadar sürünüyor, iyi oynayan hiç çökmüyor. **Kalibrasyon cezası sıfır, iki mutfakta da.** Ayrıca denetleyicinin bulduğu **19 sessiz ayrışma** kapatıldı ve ikisi gerçek hataydı: yemek yeme süresi (içerik 38 sn, kod 45 sn — masa devir hızında %18) ve servis günü uzunluğu (içerik 120.000 ms, kod 480.000 — burada içerik eskiydi). `TierIndex` ölü alan olmaktan çıkıp **yükleme değişmezine** dönüştü. 120 test, 20/20 model kontrolü.
- 10 Eylül 2026: **tatlı ölü içerikti, canlandırıldı** ([33](33-ikinci-mutfak.md) §4). `PickOrder` üç kalem seçiyordu ve tatlıya hiç bakmıyordu: fast food'da 6, Türk'te 3 tatlı yemeği, docs/27'nin zirve tablosundaki tatlı satırı ve **tatlı istasyonunun ekipman yükseltmesi** — hepsi boşa çalışıyordu. Dördüncü kalem eklendi (%18 olasılık), grup başına iş sayısı 3'ten 4'e çıktı. `plancı` 16.880 → **20.720**. 120 test.
- 10 Eylül 2026: **ikinci mutfak ilk kez koşturuldu ve kırık çıktı** ([33-ikinci-mutfak.md](33-ikinci-mutfak.md)). Türk lokantasında sekiz stratejinin hepsi **sıfır müşteriyle** batıyordu: yemek grupları mutfağa özel (docs/13 `sulu`, `corba`, `pilav`, `izgara`, `meze`) ama simülasyon fast food sözlüğünü (`ana`, `yan`) sabit kodlamıştı, yani hiç kimse ana yemek bulamıyordu. Çözüm: **menü rolleri** içerikten geliyor, üç yerde doğrulandı (üretim, yükleme, test). Düzeltmeden sonra Türk mutfağı **hiçbir ayar gerektirmeden** bütün tasarım hedeflerini tutuyor — ekonomi tek mutfağa aşırı uydurulmamış. 118 test.
- 10 Eylül 2026: **depoya iş verildi: soğuk hava ve `spoilDays`** ([32](32-ekipman-ve-yeniden-denge.md) §7). İçerikteki `spoilDays` alanı yazılmıştı ama simülasyon onu hiç okumuyordu: 44 bozulabilir malzemenin raf ömrü 1 ile 45 gün arasında değişiyor, hepsi her gece siliniyordu. Soğuk hava merdiveni o alanı canlandırdı ve **menü genişliği satın aldıran** yeni bir eksen açtı — otuz iki yemeğin var olma sebebi. `plancı` 8.892'den **16.880**'e çıktı, itibar 100. Depo artık mutfağa yapışık 13,4 m² bir arka oda; genel görünümde 51 dp ile dokunulabilir kalıyor. 110 test.
- 10 Eylül 2026: **ekipman sistemi yazıldı ve ekonomi yeniden dengelendi** ([32-ekipman-ve-yeniden-denge.md](32-ekipman-ve-yeniden-denge.md)). [27-zaman-modeli.md](27-zaman-modeli.md) Karar D nihayet uygulandı: asçı yemeğin bütün duvar saati boyunca değil yalnızca `attendBp` kadar meşgul, istasyonun yuva sayısı ayrı bir kısıt. Mutfak darboğaz olmaktan çıkınca gerçekleşme oranı %65'ten %93'e fırladı ve kiralar eksik kaldı; ama o ölçümü doğrudan uygulamak genişlemeyi tuzağa çevirdi. **Oran parametreleri, parametreler oranı belirliyor**; sabit noktayı `tools/balance/calibrate.py` aradı ve **7000**'de buldu. Kira 850/1950/2900/5000. Sonuç: **bütün tasarım hedefleri tutuyor, ceza sıfır** — Faz 0'ın açık kalan tek denge uyarısı (paranın 5,5. haftada önemsizleşmesi) kapandı. 106 test, 20/20 model kontrolü.
- 10 Eylül 2026: **oda tabanlı yerleşim ve iki kademeli kamera** ([31-oda-ve-kamera.md](31-oda-ve-kamera.md)). Modulüler oda önerisi üç turda ölçüldü; açık salon başarısız, odalar tek sıra başarısız (restoran koridora dönüşüyor ve kamera kademeye göre geri çekiliyor), **odalar 2x2 ızgarada başarılı**. Arsa sabit, bina büyüyor: dört kademede de genel görünümde oda 81 dp, oda görünümünde masa takımı 67 dp — ikisi de 48 dp asgarinin üstünde ve **kademeden bağımsız**. Dokunma hedefi masa tablası (31 dp) değil masa takımı.
- 10 Eylül 2026: **Unity render döngüsü ve restoran yerleşimi.** Toplu kipte ekran görüntüsü alınıyor; iki tuzak ölçülerek bulundu (`-nographics` render'ı kapatıyor, Unity `-executeMethod`'u derleme bitmeden çalıştırabiliyor) ve editör toplu kipinde URP Lit'in çalışmadığı, doğrulama sahnelerinin Unlit kullanması gerektiği saptandı. Dört kademe telefon oranında render edildi ve **masanın dokunma hedefi olamayacağı ölçüldü**: 14 masada masa ~15 dp, asgari 48 dp. Bkz. [16-ekranlar-ve-ogretici.md](16-ekranlar-ve-ogretici.md) sonu.
- 10 Eylül 2026: **karakter hattı kanıtlandı.** 22 mesh, 16 kıyafet × 3 vücut tipi için ek mesh sıfır, ağırlık boyama yok. Skinning yerine katı parçalı yapıya geçildi; sebebi ve bedeli [24-sanat-hatti.md](24-sanat-hatti.md)'de.
- 10 Eylül 2026: **iki model uzlaştırıldı, ekonomi çalışıyor.** Kapalı form modelin talebin tamamının ağırlandığı varsayımı ölçüldü: gerçekleşme oranı %65. Kiralar o varsayımdan çözüldüğü için %35 fazlaydı. `REALISATION_BP` eklendi, `solve.py` yeniden çözdü, kiralar 1.950-10.450'den 650-4.000'e indi. Sonuç: genişlemeyen oyuncu 11.719, genişleyen 27.916 ile bitiriyor, yani **büyümek 2,4 kat ödüllendiriyor**; pasif oyuncu 42. günde batıyor; pervasız genişleyen 7. günde. Bütün tasarım niyetleri doğrulandı. Kalan tek uyarı paranın 5,5. haftada önemsizleşmesi ve sebebi ekipmanın henüz yazılmamış olması.
- 10 Eylül 2026: **çifte malzeme ödemesi bulundu ve düzeltildi.** Hal aşaması yokken konan geçici satır kaldırılmamıştı; her tabağın malzemesi hem halde hem kasada düşülüyordu. Bu tek satır ondan önceki bütün denge ölçümlerini geçersiz kılıyordu. Düzeltmeden sonra **Faz 0'ın iki açık denge sorunu da kapandı**: pasif oyuncu 21. günde batıyor, makul oyuncu artıda bitiriyor ve talebin %88'ini ağırlıyor. Ayrıca üç strateji hatası düzeltildi: yanlış sinyalden işe alım, körlemesine genişleme, kullanılmayan kredi. Kredi mekaniği yazıldı. Ayrıntı [29-faz0-simulasyon.md](29-faz0-simulasyon.md) §5b.
- 10 Eylül 2026: **hal aşaması ve stok yazıldı.** Pasif oyuncu artık doğru biçimde batıyor: altmış günde 13 kişi, talebin %98'i kapıdan dönüyor. Menüde bir şey bulamayan müşteri masaya oturmuyor. Menü genişliği ile bozulma arasındaki gerilim modelden kendiliğinden çıktı. Kira arayıcısı otuz iki kombinasyon denedi ve **kiranın sorun olmadığını kanıtladı**; kalan sorun ölüm sarmalı ve kullanılmayan kredi mekaniği. Ayrıntı [29-faz0-simulasyon.md](29-faz0-simulasyon.md) §5b.
- 10 Eylül 2026: **zirve kararı uygulandı.** Dilim süreleri mutfak içeriğinden geliyor; fast food 960/1440/960/1440 tick, Türk 576/2304/1200/720. Müşteri kaybı düştü.
- 10 Eylül 2026: **zirve kararı** ([28-zirve-karari.md](28-zirve-karari.md)). Dilim **payları** değil dilim **süreleri** mutfağa göre değişecek. Türk mutfağının %60 öğle payı olduğu gibi kalıyor; öğle dilimi günün %48'ini kaplıyor. Günlük kayıp fast food %3,15, Türk %5,08, haftalık ciroya etkisi %1,7 altında. Mutfak kimliğinin zirve **yoğunluğu** ekseni ölüyor, yerine **süre ve süreklilik** geçiyor. Bu, yükü imza mekaniğine kaydırdığı için veresiyenin düzgün yazılması artık zorunlu.
- 10 Eylül 2026: **Faz 0 birinci dilim yazıldı.** `Lokanta.Core` (netstandard2.1, Unity referansı yok), `Lokanta.Content` (JSON yalnızca burada), 68 test. `Fx` tamsayı aritmetiği, `Rng` xoshiro128** sekiz akış, talep ve kadro modelleri, içerik doğrulayıcı. C# çekirdek Python modeliyle eşleşiyor. Karşılaştırma bir yuvarlama hatası yakaladı: Python'un `round()`'u bankacı yuvarlaması yapıyordu, altıncı haftada 62,5 değerinde ayrışıyorlardı.
- 10 Eylül 2026: **reklam, Steam ve klip kararı.** Ödüllü reklam ilk sürümde yok, oyun bitip yayına çıkmadan önceki son adımda eklenecek ve biçimi o zaman kararlaştırılacak; analiz not olarak [22-cevaplar-ve-yon.md](22-cevaplar-ve-yon.md) §5'te. Steam sayfası ve sürümü oyun bittikten sonra. Animasyon: Quaternius birincil, restoran hareketleri prosedürel, Mixamo yedek (kullanıcı indirir). Unity 6.3 LTS kurulacak.
- 9 Eylül 2026: **cevaplar ve Parti A-B.** Mac yok → sadece Android; Blender bilinmiyor → başsız render döngüsü doğrulandı; bütçe sıfır → 25 $; 32 yemek parametre şartıyla; yatay kesin. Ekonomi modeli yazıldı (`tools/balance`), üç aritmetik hata düzeltildi, 17 test geçiyor. Çekirdek sözleşmesi, sanat hattı ve ad araştırması yazıldı. Bkz. [22-cevaplar-ve-yon.md](22-cevaplar-ve-yon.md).
- 9 Eylül 2026: **beş ajanlı değerlendirme.** Tasarım, mimari, kapsam, pazar ve oyuncu deneyimi bakış açılarıyla 24 kalem incelendi. 3 onay, 21 düzelt, 0 ret. Ekonomi tablosunun formülden türemediği, kapasite modelinin kadro tablosuyla çeliştiği, determinizmin tasarlanmadığı, kapsamın 13-14 kişi-ay olduğu ve Steam sırasının ters olduğu bulundu. Ham raporlar [review/](review/) klasöründe.

---

## A. Tasarım

| # | Konu | Durum | Not |
|---|---|---|---|
| A1 | Çekirdek döngü, günün dört aşaması | ✅ | [02-tasarim-onerisi.md](02-tasarim-onerisi.md) |
| A2 | Batma merdiveni, beş kademe | ✅ | Yumuşak başarısızlık, kayıt silinmez |
| A3 | Haftalık kira ritmi | ✅ | Baskının metronomu |
| A4 | **Ekonomi sayıları** | ✅ Parti A | Bütün tablolar `tools/balance/model.py` tarafından üretiliyor, 17 tutarlılık testi geçiyor. Üç aritmetik hata bulunup düzeltildi. Bkz. [12-ekonomi.md](12-ekonomi.md) |
| A5 | **İçerik envanteri** | ⏳ 🔍 DÜZELT | v2 hazır: mutfak başına 32 yemek, 26 malzeme, 8 arketip, 10 düzenli müşteri, artı paylaşılan taban. Bkz. [09-icerik-envanteri.md](09-icerik-envanteri.md) |
| A6 | **İlerleme eğrisi** | ⏳ 🔍 DÜZELT | Öneri hazır: dört mevsim, 15'er gün, sistemler ilk üç mevsimde açılır. Aynı dosyada |
| A7 | **Müşteri sistemi detayı** | ⏳ 🔍 DÜZELT | Tamamlandı: 20 arketip artı sabır, fiyat duyarlılığı, memnuniyet, itibar ve saat dağılımı formülleri. Dengelenmedi. Bkz. [11-musteri-sistemi.md](11-musteri-sistemi.md) ve [12-ekonomi.md](12-ekonomi.md) |
| A8 | **Personel sistemi detayı** | ✅ Parti A | İki havuzlu kapasite modeli, kapasiteler 28/25/46/66, patron 1,4 iş-günü, tavan 3/5/8/12. Kadro 1'den 11'e. Bkz. [14-personel-sistemi.md](14-personel-sistemi.md) |
| A9 | **Ekran listesi ve akış** | ⏳ 🔍 DÜZELT | Yazıldı: 18 ekran, geri tuşu kuralları, modal politikası, üst çubuk hiyerarşisi. Bkz. [16-ekranlar-ve-ogretici.md](16-ekranlar-ve-ogretici.md) |
| A10 | **Öğretici ve ilk on dakika** | ⏳ 🔍 DÜZELT | Yazıldı: dakika dakika plan, günde bir kavram, ilk üç gün batma koruması. Aynı dosyada |
| A11 | **Kayıt sistemi tasarımı** | ⏳ 🔍 DÜZELT | Yazıldı: 4 yuva, durum artı tohum, atomik yazma, sürüm göçü, bulut çakışması. Bkz. [15-kayit-sistemi.md](15-kayit-sistemi.md) |
| A12 | **Ses tasarımı** | ⏳ 🔍 DÜZELT | Yazıldı: beş katman, katmanlı servis müziği, her sesin görsel karşılığı kuralı. Bkz. [17-ses-tasarimi.md](17-ses-tasarimi.md) |
| A13 | **Hikaye ve karakterler** | ⏳ 🔍 DÜZELT | Yazıldı: beş metin kaynağı, şablonlu yorum sistemi, 20.800 kelimelik hacim bütçesi, yerelleştirme kararları. Bkz. [18-hikaye-ve-metin.md](18-hikaye-ve-metin.md) |
| A14 | Son oyun ve kalıcılık | ✅ | Altmışıncı günde puanlanan yıl sonu değerlendirmesi, sonrasında serbest oyun. Yedi eksen, biri mutfağa özel. Bkz. [08-oyun-sonu.md](08-oyun-sonu.md) |
| A16 | Para birimi | ✅ | İsimsiz sikke ikonu artı sayı. İsim yok, gerçek para sembolü yok. İkon şablonu seçimi sürüyor. Bkz. [12-ekonomi.md](12-ekonomi.md) bölüm 7.5 |
| A15 | **Mutfak kimliği** | ⏳ 🔍 DÜZELT | Karakter giydirme sistemi ve mutfağa özel ortam farklılaşması. Yüz yerine mekân, ışık, siluet ve ritim. Bkz. [10-mutfak-kimligi.md](10-mutfak-kimligi.md) |

---

## B. Teknik

| # | Konu | Durum | Not |
|---|---|---|---|
| B1 | Katmanlı mimari | ✅ | [04-mimari.md](04-mimari.md) |
| B2 | Port listesi | ✅ | Girdi, kayıt, bulut, satın alma, reklam, başarım |
| B3 | **Veri şemaları** | ✅ Parti B | Eklemeler [23-cekirdek-sozlesmesi.md](23-cekirdek-sozlesmesi.md) §8'de bağlayıcı olarak yazıldı: kapasite bloğu, imza mekaniği, yemek parametreleri, tamsayı birimler. Şema dosyası Faz 0'da yeniden yazılır |
| B4 | **Unity sürümü ve render hattı** | ⏳ 🔍 DÜZELT | Unity 6.3 LTS, URP, Linear, GPU Resident Drawer açık. Bkz. [19-teknik-kurulum.md](19-teknik-kurulum.md) |
| B5 | **Performans hedefleri** | ⏳ 🔍 DÜZELT | 60 fps hedef, 30 fps taban, 100 çizim çağrısı, 200 MB indirme eşiği. Aynı dosyada |
| B6 | **Kayıt dosyası formatı** | ⏳ 🔍 ONAYLA | JSON artı gzip, sağlama toplamı, şifreleme yok. Aynı dosyada |
| B7 | Girdi eylem haritası | ⏳ 🔍 DÜZELT | Dört eylem haritası, iki kontrol şeması, eylem bazlı eşleme. Aynı dosyada |

---

## C. Üretim

| # | Konu | Durum | Not |
|---|---|---|---|
| C1 | **Model üretim yolu** | ✅ Yeniden tasarlandı | Başsız render döngüsü bugün doğrulandı: betik → Blender → PNG → ben görürüm. Üç kademe, zaman kutuları, sıfır bütçe kaynakları. Bkz. [24-sanat-hatti.md](24-sanat-hatti.md) |
| C2 | **Karakter ve animasyon** | ⏳ Zaman kutusu | Tek gövde + kemik ölçeği + skinsiz takılabilir parçalar; 96 mesh sorunu yok oldu. İki haftalık kanıt bekliyor, yedek Quaternius. Bkz. [24-sanat-hatti.md](24-sanat-hatti.md) |
| C3 | Malzeme ve doku | ✅ | Palet atlası veya vertex color. Çözülmüş kabul |
| C4 | **Ses kaynağı** | ⏳ 🔍 ONAYLA | Ücretli yapay zeka aracı, kamu malı yedekle. Aynı dosyada |
| C5 | Arayüz, ikon, yazı tipi | ⏳ 🔍 ONAYLA | OFL lisanslı aile, Türkçe karakter test cümlesi, ikon boyut kuralı. Aynı dosyada |
| C6 | Sürüm kontrolü | ✅ | Git artı Git LFS, baştan kurulur |
| C7 | Yerelleştirme | ✅ | Unity Localization, Türkçe ve İngilizce, metin koda gömülmez |
| C8 | **Test planı** | ⏳ 🔍 DÜZELT | Cihaz matrisinden iOS çıktı, Android tek platform. Determinizm hattı [23-cekirdek-sozlesmesi.md](23-cekirdek-sozlesmesi.md) §9'da. Sıkılma telemetrisi bekliyor |

---

## D. İş

| # | Konu | Durum | Not |
|---|---|---|---|
| D1 | Gelir modeli | ✅ | Mutfak içerik satın alması. Güç satılmıyor. Steam'de tek fiyat, hepsi dahil |
| D2 | **Fiyat** | ⏳ 🔍 DÜZELT | Mutfak 4,99 bandı, paket 11,99, Steam 12,99 hepsi dahil. Bkz. [21-is-ve-yayin.md](21-is-ve-yayin.md) |
| D3 | İlk sürüm kapsamı | ✅ | Mekân genişletme dahil, ikinci şube hariç, çıkışta iki mutfak |
| D4 | Tema ve mutfak kimliği | ✅ | Dört mutfak: fast food ücretsiz, Türk ücretli, İtalyan ve Japon mutfağı güncelleme |
| D5 | **Lansman planı** | ✅ 10 Eylül 2026 | Android tek platform, organik kapalı test, Steam sayfası ve sürümü oyun bittikten sonra, ödüllü reklam yayın öncesi son adım. Bkz. [22-cevaplar-ve-yon.md](22-cevaplar-ve-yon.md) §5 |
| D6 | **Hukuki** | ⏳ 🔍 DÜZELT | Zorunlular listesi, varlık lisans denetim tablosu, asgari veri ilkesi. Aynı dosyada |

---

## Kalan planlama işi

Uygulamaya geçmeden önce kapatılacak 24 kalem. Beş partiye ayrıldı. Parti 1 yazıldı, onay bekliyor.

### Parti 1 — Çekirdek kodun ön koşulu ✅ yazıldı

| # | Konu | Dosya |
|---|---|---|
| B3 | Veri şemaları | [13-veri-semalari.md](13-veri-semalari.md) |
| A8 | Personel sistemi detayı | [14-personel-sistemi.md](14-personel-sistemi.md) |
| A11 | Kayıt sistemi tasarımı | [15-kayit-sistemi.md](15-kayit-sistemi.md) |

Üçü de onay bekliyor.

### Parti 2 — Oyuncu deneyimi ✅ yazıldı

| # | Konu | Durum |
|---|---|---|
| A9 | Ekran listesi ve akış | ✅ [16-ekranlar-ve-ogretici.md](16-ekranlar-ve-ogretici.md) |
| A10 | Öğretici ve ilk on dakika | ✅ Aynı dosyada |
| A12 | Ses tasarımı | ✅ [17-ses-tasarimi.md](17-ses-tasarimi.md) |
| A13 | Hikaye ve karakter metinleri | ✅ [18-hikaye-ve-metin.md](18-hikaye-ve-metin.md) |

### Parti 3 — Teknik kurulum

| # | Konu |
|---|---|
| B4 | Unity sürümü ve render hattı |
| B5 | Performans hedefleri |
| B6 | Kayıt dosyası formatı |
| B7 | Girdi eylem haritası |

### Parti 4 — Üretim kararları

| # | Konu |
|---|---|
| C1 | Model üretim yolu |
| C2 | Karakter ve animasyon |
| C4 | Ses kaynağı |
| C5 | Arayüz, ikon, yazı tipi |
| C8 | Test planı |

### Parti 5 — İş ve yayın

| # | Konu |
|---|---|
| D2 | Fiyat |
| D5 | Lansman planı |
| D6 | Hukuki |

### Onay bekleyen yazılmış kalemler

Bunlar yazıldı, sadece onay gerekiyor: A4, A5, A6, A7, A15, artı Parti 1'in üçü (B3, A8, A11). Toplam sekiz kalem karar bekleyen sütununda.

---

## Not

Uygulamaya geçilmeden önce bu kütüğün tamamı kapanacak. Anlaşma bu: önce plan, sonra uygulama.

Faz 0'ın denge aracı için asgari şart Parti 1'in bitmesi. Ama parti 2'den 5'e kadar olan kalemler de kod yazılmadan önce yazılacak, çünkü sonradan keşfedilen bir eksik en pahalı eksiktir.

---

## Faz 0 durumu

**Birinci dilim bitti (10 Eylül 2026).** Ekonomik omurga: içerik JSON'u, tamsayı aritmetiği, rastgelelik, talep ve kadro modelleri, testler.

| Bileşen | Durum | Nerede |
|---|---|---|
| `Fx` tamsayı aritmetiği | ✅ | `src/Lokanta.Core/Fx.cs` |
| `Rng` xoshiro128**, 8 akış | ✅ | `src/Lokanta.Core/Rng.cs` |
| Talep modeli | ✅ | `src/Lokanta.Core/Economy/DemandModel.cs` |
| Kadro modeli, iki havuz | ✅ | `src/Lokanta.Core/Economy/StaffingModel.cs` |
| Haftalık plan modeli | ✅ | `src/Lokanta.Core/Economy/WeeklyPlanner.cs` |
| İçerik yükleyici ve doğrulayıcı | ✅ | `src/Lokanta.Content/` |
| `economy.json`, `staff-roles.json` | ✅ üretilen | `content/` |
| 68 test | ✅ hepsi geçiyor | `tests/Lokanta.Core.Tests/` |
| Tick simülasyonu | ❌ | İkinci dilim |
| Komut günlüğü ve kayıt | ❌ | İkinci dilim |
| Kalan dokuz şema | ❌ | İkinci dilim |

**Çalıştırma:**

```
dotnet test                              203 test
python tools/audit_content.py            icerik-kod sozlesme denetimi (cikis 0 = temiz)
python tools/content/gen_dishes.py       malzeme ve yemekleri uretir
python tools/balance/model.py --check    tasarim kontrolleri
python tools/balance/export.py           content/ ve altin veriyi uretir
python tools/balance/calibrate.py        gerceklesme sabit noktasini arar, ceza 0 olmali
python tools/balance/render.py           tablolari dokumanlara yazar
python tools/balance/rng_reference.py    bagimsiz RNG referansini uretir
dotnet run --project src/Lokanta.Harness -c Release -- --mutfak turk
```

> Bu makinede Windows "Application Control" politikası taze yazılan derlemeleri engelliyor, ve **hangi yapılandırmayı engellediği zamanla değişiyor** (bir gün Debug bloke, ertesi gün Release). `calibrate.py` bu yüzden ikisini de deniyor. Elle koşarken `-c Release` takılırsa `-c Release` olmadan dene, ya da tersi.

**Faz 0 bitti (10 Eylül 2026).** Sabit adım `Tick()`, müşteri gelişi, sabır, servis döngüsü, gün raporu, haftalık kira ve maaş, hal aşaması, kayıt, imza mekanikleri, denge aracı. Bkz. [29-faz0-simulasyon.md](29-faz0-simulasyon.md) ve [34-ilerleme-ve-kilit.md](34-ilerleme-ve-kilit.md).

> Bu tablo bir kez eskidi ve fark edilmedi: "88 test", "hal aşaması ❌", "komut günlüğü ❌" yazarken üçü de yanlıştı. **Bir sayı yazacaksan onu üreten komutu da yaz** — aşağıdaki her satırın karşılığı çalıştırılabilir.

| Bileşen | Durum | Nerede / nasıl doğrulanır |
|---|---|---|
| Unity projesi, Android hedefli | ✅ | `unity/`, ayarlar `ProjectSetup.cs` ile |
| Sabit adımlı simülasyon | ✅ | `unity/Assets/Lokanta/Core/Sim/` |
| İçerik: mutfak başına 32 yemek, 77 malzeme, 20 arketip | ✅ üretilen | `python tools/content/gen_dishes.py` |
| Zaman modeli | ✅ | [27-zaman-modeli.md](27-zaman-modeli.md), 47 kontrol |
| **Hal aşaması** | ✅ | Sabah peşin alım, raf ömrü, soğuk hava, günlük fiyat oynaması |
| **Malzeme kalitesi** | ✅ | Üç kademe, en belirleyici malzeme karar veriyor ([34](34-ilerleme-ve-kilit.md) §9) |
| **Yemek kilidi** | ✅ | İtibar + ekipman; müşteri eksik yemeği soruyor ([34](34-ilerleme-ve-kilit.md) §4-6) |
| **Ekipman ve istasyon yuvası** | ✅ | Paylaşılan 6 + mutfağa özel adlandırılmış ekipman |
| **Patron müdahalesi** | ✅ | Dört tür, günlük sayılı hak ([34](34-ilerleme-ve-kilit.md) §12-13) |
| **Personel deneyimi** | ✅ | 30 günde seviye, azami 3 ([34](34-ilerleme-ve-kilit.md) §15) |
| **İmza mekanikleri** | ✅ | Fast food kombo, Türk veresiye ([34](34-ilerleme-ve-kilit.md) §18) |
| **Komut günlüğü ve kayıt** | ✅ | Sürüm 10, `StateHash`, 60 günlük kesinti testi |
| Denge aracı, on bir strateji | ✅ | `dotnet run --project src/Lokanta.Harness -c Release` |
| İçerik-kod sözleşme denetimi | ✅ **temiz** | `python tools/audit_content.py` (çıkış 0) |
| **219 test** | ✅ | `dotnet test` — altısı `RobustnessTests`: ücret tavanı, 200 günlük koşu, gün içi sayacın kaydı, yükleme sonrası duyuru seli, bozuk kayıt |
| **Kalibrasyon** | ✅ ceza 0 | `python tools/balance/calibrate.py`, iki mutfakta da |
| **İsimli düzenli müşteriler** | ✅ | Mutfak başına 10 kişi, hikâye sahneleri ([34](34-ilerleme-ve-kilit.md) §19) |
| **Personel huyları ve moral** | ✅ | 12 huy, moral eşikleri, istifa ([34](34-ilerleme-ve-kilit.md) §20) |
| İş yükseltmeleri | ⏸ **bilinçli erteleme** | Aşağıya bak |
| **Yıl sonu puanlaması** | ✅ | Yedi eksen, plaket 0-3; `scoreAxis` içerikten okunuyor (`Simulation.SignatureAxis`) |
| Kayıt göç zinciri | ⏸ **bilinçli erteleme** | Aşağıya bak |
| **Unity görünüm katmanı** | 🟡 ilk dilim | `unity/Assets/Lokanta/Game/` — kamera, kat planı, HUD ([34](34-ilerleme-ve-kilit.md) §22) |

**Sıradaki iş:** tasarımı kapatan iki sistem (isimli düzenli müşteriler, personel huyları ve moral), sonra Unity görünüm katmanı.

### İş yükseltmeleri: bilinçli erteleme

`content/upgrades.json` ([13](13-veri-semalari.md)) bir **eksik değil, bir
özellik**: tabela gibi, kapasite eklemeden müşteri tabanını yükselten
satın almalar. Güzel bir tycoon mekaniği ve oyunda gerçekten bir boşluk
dolduruyor — bugün bütün satın almalar *kapasite* satıyor (ekipman,
masa, depo), hiçbiri bir müşteriyi daha değerli yapmıyor.

**Yine de şimdi yazılmayacak.** Sebep: altıncı bir harcama ekseni eklemek,
dengeyi baştan kalibre etmek demek. Gerçekleşme oranı bir **sabit nokta**
([32](32-ekipman-ve-yeniden-denge.md)) ve her yeni para musluğu o noktayı
kaydırıyor; bugün eklemek, yayına hazır duran iki mutfağın dengesini
yeniden açmak olur. Oyun bu eksen olmadan **oynanabilir ve dengeli**.

Yazılma anı: ilk sürüm dışarı çıktıktan ve gerçek oyuncuların 60 günü
nasıl geçirdiği görüldükten sonra — o zaman "hangi satın alma eksik"
sorusunun cevabı tahmin değil **ölçüm** olur.

`tools/audit_content.py` bu iki alanı (`capacityBonus`, `customerBaseBonus`)
kuyrukta tutmaya devam ediyor; sessizce düşmüyorlar.

---

### Kayıt göç zinciri: bilinçli erteleme

[23-cekirdek-sozlesmesi.md](23-cekirdek-sozlesmesi.md) §7.6 iki doğrulama istiyor. **Kesinti testi yazıldı** ve geçiyor. **Sürüm testi** — eski anlık görüntünün göç zinciriyle yüklenip tekrar oynatılması — yazılmadı, ve bu bir eksiklik değil bir karar:

Kayıt sürümü bir hafta içinde 1'den 10'a çıktı. Bugün göç zinciri yazmak, on tanesi de atılacak on göç yazmak demek. Şu anki davranış **sessiz değil**: `Restore` farklı bir sürüm görünce açık bir hata fırlatıyor, yani eski kayıt bozulmuyor, açılmıyor.

**Zincir ne zaman yazılacak:** ilk oynanabilir sürüm dışarı çıktığında — yani bir başkasının kaydı ilk kez var olduğunda. O ana kadar sürüm numarasını artırmak bedava; o andan sonra her artış bir göç borcu.

**Üçüncü dilim:** Unity görünüm katmanı.
