# 37 — Dört agent incelemesi ve kapatılan açıklar

12 Eylül 2026. Dört bağımsız inceleme koştu: **simülasyon/ekonomi**, **görünüm/canlandırma**, **oyuncu deneyimi**, **yayına hazırlık**. Hepsi kodu okudu, hiçbiri dosya değiştirmedi; bulguların her biri uygulanmadan önce kodda doğrulandı.

Bu belge bulguları değil **kapatılanları** kaydediyor: ne yanlıştı, nasıl ölçüldü, ne yapıldı.

---

## 1. KRİTİK — Fiyatın tavanı yoktu: sınırsız kâr

**Nasıl bulundu:** inceleme, `Lokanta.Core`'a bağlanan bir sonda yazıp 60 günlük, 5 tohumlu koşular aldı.

| senaryo | kasa | itibar | memnuniyet | ağırlanan |
|---|---:|---:|---:|---:|
| taban | 16.894 | 33,4 | 68,91 | 856 |
| ekstralar ×200 | 343.154 | 12,5 | 65,12 | 666 |
| ekstralar ×2000 | **3.368.594** | **12,5** | **65,12** | **666** |

Son iki satır aynı: fiyat 10 katına çıkıyor, kasa 9,8 katına çıkıyor, **itibar ve memnuniyet kıpırdamıyor**. Sebep: memnuniyet `[0, 10000]` arasına kırpılıyor ve talep fiyatı hiç görmüyor — yani bir kalemin fiyatı, o kalemi alan müşterinin memnuniyetini sıfıra indirdiği noktadan sonra **her ek sıfır bedava**.

**Düzeltme:** `overpriceCeilingBp = 25000` (piyasanın 2,5 katı) ve `SetPrice` aşan komutu reddediyor (sebep kodu 13). Taban zaten vardı (`underpriceFloorBp`); tavanın olmaması simetri hatasıydı.

## 2. KRİTİK — Kombo açıkken yan ve içeceğin fiyatı memnuniyete hiç girmiyordu

`WeightedPriceDiffBp` kombo varken **yalnızca ana yemeği** ölçüyordu. Gerekçesi doğruydu ("kombo kendi indirimini taşıyor, oyuncu imza mekaniğini açtığı için cezalandırılmasın") ama uygulaması yanlıştı: kombo fişi **üç kalemin** fiyatını topluyor ve kombo her gruba dayatılıyor.

| senaryo | kasa | memnuniyet |
|---|---:|---:|
| kombo, normal fiyat | 16.565 | 67,66 |
| kombo, ekstralar ×2000 | **24.829.724** | **66,80** |

Tabanın **1470 katı** kasa, memnuniyet neredeyse hasarsız.

**Düzeltme:** komboda da fişin tamamı ölçülüyor — üç kalemin piyasa toplamı × kombo indirimi ile oyuncunun kombo fiyatı karşılaştırılıyor. İndirim cezalandırılmıyor, şişkinlik ölçülüyor.

**İkinci yarısı:** `ComboSellable()` `CanMake` bakmıyordu. Ölçüldü: yan ve içeceğin malzemesi hiç alınmadığında bile 24 günde **325 yan + 318 içecek** satıldı — tam fiyattan, sıfır malzeme maliyetiyle. Kombo, stoksuz bedava üretim kapısıydı.

### Denge aracı neden yakalamadı

Açık tam **iki botun kesişiminde** duruyordu: `pahali_ekstra` ekstraları pahalılaştırıyor ama komboyu açmıyor; `imzaci` komboyu açıyor ama fiyata dokunmuyor. İkisini birleştiren bot yoktu.

Artık var: **`kombo_sismesi`**. Ve iki bot da tavanın hemen **altında** fiyat yazıyor — tavanın üstünde yazan bir bot reddediliyor ve sessizce "makul oyuncu"ya dönüşürdü, yani hiçbir şeyi sınamazdı.

Düzeltme sonrası (5 tohum, 60 gün, fast food):

| bot | önce | sonra |
|---|---:|---:|
| `makul` | 25.035 | 28.689 |
| `pahali_ekstra` | 25.035 (etkisiz) | **1.886, itibar 0, 56. günde borç** |
| `kombo_sismesi` | 24.829.724 | **2.149, itibar 0** |

---

## 3. YÜKSEK — Yemek pişerken müşterinin sabri hiç işlemiyordu (%87)

`DispatchKitchen` bir iş başlatırken grubu `_pInTask` ile işaretliyordu ve `DrainRateBp` o bayrakta **0** dönüyordu. Ama `TimingConfig`'in kendi yorumu niyeti yazıyor: *"garson masadayken hiç tükenmez"* — yani bayrak "garson masada" demek olmalı. **Aşçının ocakta olması müşteriyle ilgilenmek değil; müşteri tam da o sırada bekliyor.**

Ölçüm (her tick'te her masa, 30 gün × 3 tohum):

| mutfak | "yemek bekliyor" tick | sabır işliyor | **donmuş** |
|---|---:|---:|---:|
| fastfood | 236.794 | %13,3 | **%86,7** |
| turk | 239.631 | %12,3 | **%87,7** |

Yani `DrainWaitingFoodBp = 3500` fiilen ~465 olarak çalışıyordu — 7,5 kat zayıf. Bedeli: `prepMs`, ekipman kademesi ve kombonun mutfak yükü müşteri tarafında **neredeyse hiç görünmüyordu**. docs/27 Karar D'nin vaat ettiği gerilim vardı ama karşılığı yoktu.

**Düzeltme:** mutfak işi ayrı bir bayrağa taşındı (`_pKitchenTask`). Görev dağıtımı ikisini de okuyor; yalnızca **sabır** ayrıldı.

### Düzeltmenin açtığı ikinci delik

İlk koşuda bütün referans botlar iflas etti — 60 günde 78 kişi ağırlandı (önce 1920). Sebep sabır değil **memnuniyet cezası**ydı: `_pWaitedMs` her tick'i **tam** sayıyordu ve bu, sabır donmuş olduğu sürece zararsızdı. Donma kalkınca bir dakikalık pişme, `(waited/sabır) × 6000` cezasını doyuruyor ve herkes sıfır memnuniyetle çıkıyordu.

**Doğrusu aynı ölçü:** bekleme de sinir derecesiyle ağırlıklı sayılıyor. Masa beklemek tam, yemek beklemek az — ikisi de aynı birimde.

`drainWaitingFoodBp` 3500 → **500**: eski *etkiyi* koruyor ama artık pişme süresine **bağlı**.

### Yeniden kalibrasyon

`calibrate.py` tam zinciri koştu (solve → model → export → simülasyon) ve sabit noktayı yeniden buldu:

| | önce | sonra |
|---|---|---|
| gerçekleşme oranı | 6500 | **7000** |
| ceza (32 tohum) | 5 | **6** |
| kiralar | 650/1550/2250/4000 | 850/1950/2900/5000 |

### Ekipman testinin ölçüsü değişti

`Yuva_yukseltmesi_olculebilir_fark_yaratiyor` kırmızıya düştü: servis edilen grup 96 ≤ 98. Ama sebep testin haklı olmasıydı — **mekanizma değişti**. Dört masalık bir dükkânda grup sayısı gürültü; ekipmanın karşılığı artık tam da docs/27'nin söz verdiği yerde görünüyor:

| | ekipmansız | ekipmanlı |
|---|---:|---:|
| ortalama memnuniyet | 83,3 | **88,8** |
| ciro | 10.099 | **10.258** |

Test artık memnuniyet ve ciro ölçüyor, grup sayısı için yalnızca "düşmedi" diyor.

---

## 4. Diğer simülasyon düzeltmeleri

- **İstifa eden personelin adı kalan personele geçiyordu.** `RemoveStaff` xp/huy/moral kaydırıyor ama **ad dizisini kaydırmıyordu**, ve tek çağıranı istifa. Ad kayda yazıldığı için hata kalıcıydı. Bu mekaniğin var olma sebebi *"sayı kaybetmek soyut, adını bildiğin bir çalışanın istifa etmesi somut"* — adlar yalan söyleyince mekanik tersine dönüyordu.
- **Kayıt sürüm göçü yoktu.** Eksik anahtarda okuyucu **istisna atıyordu**: yayından sonra tek bir denge yaması, her oyuncunun altmış günlük kampanyasını "bozuk" yapardı. `IStateReader.Has(key)` eklendi ve kural yazıldı: *yeni alanlar `Has` ile okunur, yoksa varsayılanda bırakılır.* Sürüm 15; 9–14'ün belgesiz olduğu da kaydedildi.

---

## 5. Görünüm katmanı

| bulgu | ölçü | düzeltme |
|---|---|---|
| `StreetLife` yok edilmiş nesneye dokunuyor | ana menüye her dönüşte kare başına istisna | `Clear()` sokağı da temizliyor + gövde kontrolü |
| `CookRoutine` her tabakta **3 malzeme sızdırıyor** + `Shader.Find` | 60 günde binlerce malzeme örneği | paylaşılan malzeme + `MaterialPropertyBlock` |
| kadro değişimi yalnızca **toplama** bakıyor | garson↔aşçı takasında yeni aşçı hiç pişirmiyor | damga `Cooks × 1000 + Salon` |
| duraklatma mutfağı ve sokağı durdurmuyor | `Mathf.Max(0.25f, GameSpeed)` sıfırı yutuyordu | hız sıfırken erken çıkış |
| ışınlanan aşçı "vardım" diyor | tava boş ocağa konuyordu | varış **mesafeyle** + aşama zaman aşımı + `Cancel` yürüyüşü de kesiyor |
| 56 sandalye transform yazımı/kare | değer masa dolmadıkça değişmiyor | yalnızca değişince yazılıyor |
| `MovingCount` her okumada dizi ayırıyor | tur onu 1500 kare boyunca okuyor | elde olan listelerden sayılıyor |
| `Quality.Restore()` hiç çağrılmıyor | editörde `renderScale` 1,0'da kalıp **diske yazılıyor** | `OnDisable`'a bağlandı |
| editör kipinde `Destroy` | çarpışan kutu sahnede kalıyor | dört yerde kalıp düzeltildi |

### Yapıda opak, editörde saydam

En önemlisi bu: **duvarlar, kapı kanatları ve fırın camı gerçek yapıda opak çiziliyordu.** URP, saydam geçişin gölgelendirici varyantını ona başvuran bir **varlık** yoksa yapıya koymuyor; çalışma anında `new Material(...)` ile kurulan malzeme o varyantı almıyor.

Editörde hiçbir belirti yok. Aynı sınıf hata daha önce URP/Unlit'te yaşanmıştı (rozetler).

**Düzeltme:** `ArtPrefabs` artık `custom_wall`, `custom_door`, `custom_glass`, `custom_lightpool` malzemelerini **.mat varlığı** olarak üretiyor ve `BuildGameScene` onları sahneye bağlıyor. Eksikse `Debug.LogError` — sessiz düşüş yok.

---

## 6. Oyuncu deneyimi

### Kriz artık ekranda

Oyunun tek vaadi *"servis sırasında yalnızca krizlere müdahale edersin"* ve kriz için tek sinyal masa üstündeki ~50×8 dp'lik rozetti. Üstteki "Kızgın 1" **hiçbir koşulda renk değiştirmiyor**; ses yok, titreşim yok. Oyuncu dört müdahale hakkını kime harcayacağını göremiyordu.

**Kriz şeridi** eklendi: sabrı eşiğin altına inen masalar için çip ("Masa 3 %18"), en sabırsızdan sıralı, en fazla beş. Çipe dokunmak o masayı **seçiyor** — hedef seçme sorunu da böylece çözülüyor. Yeni bir kriz görününce tek seferlik uyarı sesi.

Şerit **kriz yokken hiç kurulmuyor**: ortaya çıkmasının kendisi sinyal.

> **Denetim dairesel değil.** "Kriz göründü mü" diye sormak kendi kendini onaylardı. Çıkarım şu: gün sonunda kızgın ayrılan bir grup varsa, o grup bir noktada kritiğe inmiş olmalı — yani şerit o an ekranda olmalıydı.

### Ölçüm penceresi yanlış ekrandaydı

Tur **1280×720**'de koşuyordu; hedef telefon **873×393 dp**. Yani bütün genişlik ölçümleri, ekibin arayüzü gördüğü tek pencere dahil, **oyuncunun görmediği bir ekranda** yapılıyordu.

Tur artık 873×393'te koşuyor. İlk koşuda menü ekranında **tek yemek kartı** göründüğü doğrulandı — 32 yemeklik bir listede.

**Ayrıca ipuçları:** tur `PlayerPrefs`'i temizlemiyordu, yani alınan görüntüler oyunun *yeni oyuncuya gösterdiği* hali değil *bir kez oynanmış* haliydi. Tur artık `Hints.Reset()` çağırıyor ve "1. gün sabahında ipucu var mı" diye soruyor. (`Hints.All` dizisinde `Cap` eksikti — "ipuçlarını yeniden göster" onu geri getirmiyordu.)

### Menü listesi

Ham içerik sırasındaydı: birinci gün bir açık yemek ve hemen altında **art arda altı kilitli satır**. Üç değişiklik, sıfır yeni mekanik:

1. **Sıra:** Menüde → Açık → Yakında (iki tane) → Kilitli (N) katlanmış.
2. **Sıkıştırma:** yalnızca dokunulan yemek kart olarak açılıyor; diğerleri tek satır. **Satırın kendisi düğme** — hem 44 dp'ye iniyor hem dokunma hedefi 873 dp'lik bir şerit oluyor.
3. **Başlık:** her bölümün üstünde ne olduğu yazıyor.

Ekrana bir yemek yerine dört yemek giriyor.

### Kararın sonucu görünüyor

Oyunda **tek bir gün-önceki-gün karşılaştırması yoktu**. Beşinci günde fiyatı değiştiren oyuncu altıncı günde bir sayı görüyor ve iyi mi kötü mü bilmiyordu. Öğrenme durunca ikinci güne dönmek için sebep kalmıyor.

- Akşam şeridinde kâr ve ağırlanan kişinin yanında **düne göre fark**.
- **Gün raporu birincil oldu**, "Ertesi gün" ikincil. Altmış günlük bir kampanyada oyuncu altmış kez turuncuya basıyordu ve çöpü, moralsiz personeli, müdavim sahnelerini hiç görmüyordu.

### Servisi açmadan önce hazırlık özeti

Sabahın **geri dönüşü olmayan** tek kararı, ekranın en geniş ve tek turuncu düğmesi, tek dokunuşla ve onaysız çalışıyordu. Artık üstünde bir satır var — "Menüde 6 yemek · Stokta 6 yemek · Mutfak 1 kişi" — ve eksik olan kalem **kırmızı**. Kırmızı varken ilk dokunuş soruyor, ikincisi açıyor.

Onay yalnızca eksik varken: her sabah "emin misin" sormak, onayı okunmayan bir refleks hâline getirirdi.

### Ses kumandası

10 kutucuk × 52 dp taban genişlik 480 dp'lik panele sığmıyor, satır sarıyor ve alttaki iki kutucuk `flexGrow` yüzünden yarım ekran genişliğinde iki boş kutuya dönüşüyordu — ekran görüntüsünde bir **hata** gibi okunuyor. **5 kademe**: hem sığıyor hem yeterli.

---

## 7. Yayına hazırlık

| bulgu | durum |
|---|---|
| **Lisanslar** | Yayın engeli **YOK**. Kenney paketleri CC0 1.0, Rubik SIL OFL 1.1; atıf şartı karşılanıyor (`Resources/licenses/` derlemeye giriyor ve Lisanslar ekranı tam metni gösteriyor) |
| **İmzasız AAB** | Elde duran paket **hata ayıklama anahtarıyla** imzalıydı. Artık AAB **imzasız üretilmiyor** — uyarıp devam etmek yerine yapı duruyor. APK'da uyarı yeterli (cihaza atıp denemek için) |
| **Semboller** | `androidCreateSymbols` **Public**. Sembolsüz çökme raporları çıplak adres gösteriyor ve Android vitals bir **mağaza görünürlük eşiği** |
| **İçerik senkronu** | `SyncContent.Run()` artık yapının parçası. Bayat bir kopyayla alınan yapı, test edilenden **farklı denge sayılarıyla** mağazaya giderdi ve hiçbir test kırılmazdı |
| **Kimlik çelişkisi** | `ProjectSetup` ile `BuildPlayer` paket adı, şirket, yön ve hedef SDK'yı **ters yönde** yazıyordu. Paket adı ilk yüklemede sonsuza kadar kilitleniyor. Tek kaynak: `BuildPlayer.Package/Company/Product` |
| **Düşük bellek** | `Application.lowMemory` → kaydet. Android ön plandayken de öldürebiliyor; o yolda bir günlük oynanış giderdi |
| **Açılış ekranı** | Logosuz boş koyu ekran kapatıldı |
| **Lisans denetimi** | **Yeni:** `tools/check_licenses.py`. Art/ altındaki her klasörde `License.txt`, tanınan bir lisans ve ATIF defterinde bir satır arıyor; `Resources/audio/` dolarsa aynısını seslerde de istiyor |

### Lisans denetimi neden makine işi

Projenin kendi kütüğü *"en büyük risk: yapay zekâ araçlarının ücretsiz katmanıyla üretilmiş bir varlığın gözden kaçması"* diyor. Ama o yolda **sıfır sürtünme** vardı: bir `.ogg`'yi klasöre atmak kod değişikliği istemiyor, lisans metni istemiyor, hiçbir denetçiyi tetiklemiyordu. Google Play bunu yakalamaz; telif sahibi yakalar.

Denetim ilk koşusunda **üç eksik** buldu (Simge klasöründe lisans yok, iki paket atıf defterinde eşleşmiyor) — yani sınanabilirliği kendi ilk turunda kanıtlandı. ATTRIBUTION.md'ye makine okunabilir bir **klasör eşlemesi** tablosu eklendi.

---

## Doğrulama

| ne | sonuç |
|---|---|
| `tools/check.py` | **13/13** (lisans denetimi eklendi) |
| çekirdek testleri | 220 |
| otomatik tur, **873×393 gerçek telefon** | **71/71**, iki ardışık koşuda |
| yerleşim denetimi, iki mutfak | 0 çakışma, 15/15 doğru doğrultu |
| denge, 5 tohum | `makul` 28.689 · `planci` büyüyor · istismar botları iflas |
| kalibrasyon, 32 tohum | gerçekleşme 7000, ceza 6 |
| APK | 0 uyarı |
| AAB | **imzasız üretilmiyor** (kasıtlı kapı) |

## Yayın öncesi kalan — kullanıcı işi

Bunlar koda değil hesaba bağlı:

1. **Yükleme anahtarı** üret (`keytool`) ve dört ortam değişkenini ayarla. Anahtar kaybedilirse uygulama bir daha güncellenemez — **iki ayrı yerde yedekle**.
2. **Gizlilik politikası** (tek paragraf yeter: veri toplanmıyor — AAB bildiriminde kullanıcıya görünen tek izin yok, analitik/reklam/çökme raporu kapalı, tek ağ çağrısı yok).
3. **Veri Güvenliği formu**: "toplanmıyor".
4. **IARC anketi**: **"çocuklara yönelik değil"** seçilmeli — çizgi film üslubu yüzünden "çocuklar" işaretlenirse Families Policy yükümlülükleri açılır ve geri alması zordur.

## Kararı verilmemiş — tasarım sorusu

**Oyun yalnızca Türkçe** ama `docs/21` D5 İngilizce soft launch ve global çıkış diyor. İkisinden biri seçilmeli: ya `en.json` eklenip `Loc.Load`'a cihaz dili anahtarı konur, ya da D5 "TR kapalı test" olarak yeniden yazılır. `gen_loc.py` anahtarları içerikten çıkardığı için ikinci dil **şimdi** ucuz — mağaza metinleri ve ekran görüntüleri üretilmeden önce.
