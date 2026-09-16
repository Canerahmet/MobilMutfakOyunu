# Sanat Hattı: Mesh'i Kim Değerlendirecek

**Son güncelleme:** 10 Eylül 2026
**Kütük maddeleri:** C1 model üretim yolu, C2 karakter ve animasyon
**Durum:** Yeniden tasarlandı. Kapsam değerlendirmesinin en büyük endişesine cevap.
**Değerlendirme kaynağı:** [review/03-scope-realism.md](review/03-scope-realism.md)

---

## Sorun, açıkça

Kapsam değerlendirmesi şunu yazdı: "Karakter hattı geliştiricinin yetenek boşluğuna oturuyor. Ağırlık boyama görsel yargı ister."

Sen bugün doğruladın: Blender'ı bilmiyorsun ve kullanımını yapay zekaya devrediyorsun.

Bu, hattı şöyle bırakıyordu:

| Kim | Ne yapabilir | Ne yapamaz |
|---|---|---|
| Ben | Blender Python betiği yazmak | Ürettiği mesh'i görmek |
| Sen | Betiği çalıştırmak | Mesh'in yanlış olduğunu anlamak |

**Döngüde mesh'e bakan kimse yoktu.** Bu, "prosedürel birincil" kararını ([20-production-decisions.md](20-production-decisions.md)) çalışmaz hâle getiriyordu.

---

## Çözüm: başsız render döngüsü

Blender komut satırından açılır, betik çalışır, PNG üretir. **PNG'yi ben okuyabiliyorum.** Döngü kapanıyor:

```
betik yaz → blender --background → PNG → ben bakarım → not → betik düzelt
```

Bugün doğrulandı, `tools/art/gen_table.py`:

| Ölçüm | Değer |
|---|---|
| Üretilen | Lokanta masası, iki sandalye, örtü, tabak |
| Nesne | 19 |
| Üçgen | 2.004 |
| Render süresi | 4 saniye, üç açı |
| Motor | EEVEE |
| Blender | 5.2 LTS, makinede kurulu |

Ve ürettiğim ilk düzeltme notları, görüntüye bakarak:

1. Sandalye sırtı oturağın üstünde bir iki santim boşlukta duruyor; sırt 0,70'ten 0,68'e insin
2. Beyaz tabak beyaz örtüde kayboluyor; örtü kirli bej olsun ya da tabağa koyu kenar gelsin
3. Sandalyeler masadan on iki santim uzak; boş masada içeri çekilmiş dursun

Bu üç not, "modeli kim değerlendirecek" sorusunun cevabı. Değerlendirecek benim.

### Döngü uçtan uca çalıştı: 10 Eylül 2026

`tools/art/gen_fastfood_props.py` sekiz prop üretti: masa seti, sandalye, tezgah, ocak, dolap, raf, çöp, tepsi. Toplam 2.736 üçgen, hepsi bütçesinde.

Üç tur döndü ve her turda render'a bakmak gerçek hata yakaladı:

| Tur | Bakınca görülen | Düzeltme |
|---|---|---|
| 1 | Sandalye sırtı oturağın üstünde boşlukta; beyaz tabak beyaz örtüde kayboluyor; sandalyeler masadan uzak | Sırt oturağa oturdu, örtü kirli bej oldu, sandalyeler içeri çekildi |
| 2 | Ocağın davlumbazı ve tezgahın menü panosu boşlukta asılı | İkisi de duvara monte parça; temas sayfasına zemin ve duvar eklendi |
| 3 | Duvarlı proplar tamamen gri çıkıyor | Duvar +Y'de, kameraların ikisi arkasından bakıyordu; duvarlı proplar için açılar −Y yarısına alındı |
| 4 | Ocak duvara iki metre uzakta, bacası hiçbir yere ulaşmıyor | Ocak duvara yaslandı, baca tavana çıkıyor |

Hiçbiri kodu okuyarak bulunamazdı. Üçüncü tur özellikle öğretici: betik hatasız çalıştı, üçgen bütçesi tuttu, rapor "TAMAM" dedi ve çıktı tamamen boş gri bir kareydi. **Sayısal rapor doğru olabilir ve görüntü yine de kırık olabilir.**

### Döngünün sınırı

Ben görüyorum ama sanat yönetmeni değilim. Görebildiklerim: siluet okunuyor mu, parçalar birbirine oturuyor mu, oran bozuk mu, renk kontrastı var mı, üçgen bütçesi tutuyor mu, ağırlık boyama çökmüş mü. Göremediklerim: "güzel mi." Onu sen görürsün, sen de Blender bilmeden görebilirsin: PNG'ye bakarsın.

**Yani iş bölümü:** ben teknik doğruluğu, sen beğeniyi değerlendirirsin. İkisi de PNG'ye bakarak, ikisi de Blender açmadan.

### Karakter hattı kanıtlandı: 10 Eylül 2026

`tools/art/gen_character.py` ve `tools/art/lib/rig.py`. Kanıtlanması gereken şey 96 mesh sorununun gerçekten yok olup olmadığıydı.

| Ölçüm | Değer |
|---|---|
| Gövde parçası | 19 mesh, 836 üçgen |
| Takılabilir (önlük, şapka, saç) | 3 mesh, 132 üçgen |
| **Toplam** | **22 mesh** |
| 16 kıyafet × 3 vücut tipi için gereken ek mesh | **0** |
| Ağırlık boyama gereken yer | **yok** |
| Test pozu | 6, hepsi doğru render ediliyor |

### Kararın değişen yeri: skinning yerine katı parçalı

Doküman tek gövde mesh'i artı otomatik ağırlık öngörüyordu. **Denendi ve başarısız oldu.** Birleştirilmiş ama kaynaklanmamış kutulara otomatik ağırlık uygulanınca her kutu tek kemiğe bağlandı; oturma pozunda bacaklar kalçadan ayrıldı, eklemlerde boşluk açıldı.

Düzeltmenin yolu mesh'i kaynaklayıp eklem bölgesine kenar döngüsü eklemek ve ağırlık boyamaktı. **O iş tam olarak bu dokümanın tespit ettiği yetenek boşluğuna giriyor.**

Onun yerine her parça tek kemiğe **katı** bağlandı. Skinning yok, ağırlık boyama yok, eklemler çakışan geometriyle kapatılıyor.

**Bedeli açık:** karakter kâğıt gibi bükülmüyor, tahta bebek gibi dönüyor. Bu bir tarz kararı ve low-poly mobil oyunlarda yaygın. Oyunun yumuşak kenarlı yönüyle uyumlu, ve en önemlisi görsel yargı gerektiren tek adımı ortadan kaldırıyor.

### Beş tur baktım, beşi de bir şey yakaladı

| Tur | Sayısal rapor | Render ne gösterdi |
|---|---|---|
| 1 | Bütçeler tamam | Pozlar yanlış eksende; kollar aşağı inmiyor, öne arkaya salınıyor |
| 2 | Bütçeler tamam | Eklemlerde boşluk: oturma pozunda bacaklar kalçadan ayrıldı |
| 3 | Bütçeler tamam | Katı bağlamada parçalar sahneye dağıldı; kemik ebeveynliği kuyruğu başlangıç alıyor, ben başını varsaymıştım |
| 4 | Bütçeler tamam | Kollar aşağı yerine yukarı kalktı; dönüş işareti ters |
| 5 | Bütçeler tamam | Kamera arkadan bakıyor, önlük görünmüyor; saç kafanın üst yarısını yutuyor |

**Beş turun beşinde de sayısal rapor "TAMAM" dedi.** Üçgen bütçesi tuttu, betik hatasız çalıştı, hiçbir uyarı çıkmadı. Hataların hepsi yalnızca bakınca göründü.

### Kemik yapısı ve klip uyumu

On dokuz kemik, Unity insansı eşlemesine uygun adlarla: hips, spine, chest, neck, head, shoulder/upperarm/forearm/hand ve thigh/shin/foot, sol ve sağ. Quaternius Universal Animation Library klipleri bu yapıya yeniden hedeflenebiliyor.

Vücut tipi kemik **kalınlık** eksenlerinde ölçekleniyor, uzunlukta değil. Uzunluk değişirse iskelet oranları bozulur ve klip yeniden hedeflemesi kayar.


---

## Üç kademe, riske göre

### Kademe 1: Mobilya, ekipman, ortam

**Yol:** prosedürel Blender betiği + render döngüsü. Tamamen bizde.

Bugün doğrulanan yol bu. Masa, sandalye, tezgah, ocak, fırın, dolap, kasa, duvar panelleri, zemin karoları, tabela, saksı, lamba: hepsi küp, silindir ve pah kırma ile üretilir. Low-poly'nin avantajı tam burada: doku yok, sadece düz renk malzeme.

Üretim betiği standardı:

```
tools/art/
  lib/
    prim.py        küp, silindir, pah, düz renk malzeme yardımcıları
    stage.py       ışık, üç açılı kamera, temas sayfası render'ı
    export.py      glTF dışa aktarma + manifest
  props/
    table_2.py     iki kişilik masa
    table_4.py
    counter.py
    stove.py
    ...
  out/             PNG temas sayfaları (depoya girmez)
```

Her betik `python props/x.py` ile üç şey üretir: temas sayfası PNG (üç açı, tel kafes, üçgen sayısı), `Assets/Art/Generated/x.glb`, ve `x.json` manifest (üçgen, sınır kutusu, kaynak betik özeti). Unity içe aktarma manifesti okur; elle sürükleme yok.

### Kademe 2: Karakterler

**Yol:** tek gövde mesh + otomatik iskelet + kemik ölçeğiyle vücut tipi + takılabilir kıyafet parçaları. Render döngüsü altı test pozuyla ağırlık çöküşünü yakalar.

Değerlendirmenin matematiği: 16 kıyafet × 3 vücut tipi = 96 mesh. **Bu sayı yok oluyor**, çünkü:

| Değişken | Nasıl |
|---|---|
| Vücut tipi | Tek mesh, üç kemik ölçeği ön ayarı. Mesh çoğalmıyor |
| Kıyafet rengi | Malzeme değişimi. Mesh çoğalmıyor |
| Kıyafet parçası | Önlük, şapka, atkı, ceket: **skinsiz** ayrı mesh, kemiğe bağlı. Ağırlık boyama yok |
| Saç | 8 skinsiz mesh, kafa kemiğine bağlı |
| Cilt tonu | Malzeme |

Kıyafet seti = gövde malzemesi + 0-3 takılabilir parça. On altı set, sıfır ek skinning. Gövde bir kez ağırlıklanır ve bir daha dokunulmaz.

Ağırlıklama nasıl: Blender otomatik ağırlık (`parent_set(type='ARMATURE_AUTO')`) tek gövdeye. Sonra render döngüsü altı pozda (T, yürüme ortası, oturma, eğilme, uzanma, dönüş) render alır; dirsek ve diz çöküşünü ben görürüm. Çöküş varsa iki yol: kemik ekle ya da mesh'te o bölgeye kenar döngüsü ekle. İkisi de betik.

Animasyon klipleri üç kaynaktan (karar 10 Eylül 2026):

| İhtiyaç | Kaynak | Kim |
|---|---|---|
| Yürü, dur, taşı, otur, ye | **Quaternius Universal Animation Library 1 ve 2.** 250'den fazla klip, tek insansı iskelet, Unity'de yeniden hedeflenebilir, CC0, indirilebilir paket | Ben indirir, betikle bağlarım |
| Pişir, sil, kasa, servis | Blender'da prosedürel: üst gövde döngüleri, üç dört anahtar kare, render döngüsüyle doğrulanır | Ben |
| Eksik kalan tek bir klip | **Mixamo**, yedek. 2026'da açık ve ücretsiz, lisansı sınırsız ticari kullanım; ama 2015'ten beri güncellenmiyor. İndirilen klip bizde kalır | Sen, uygulama aşamasında, on beş dakika |

Mixamo'nun kapanma riski yalnızca ileride yeni klip indirmeyi etkiler; bu yüzden birincil değil, yedek.

Yedek plan: karakter hattı iki haftalık zaman kutusunda kanıtlanamazsa (bir karakter, iskelet, dört klip Unity'de cihazda oynuyor), Quaternius'un CC0 iskeletli low-poly karakterlerine geçilir. Kıyafet farkı sadece malzemeyle yapılır. Daha az kimlik, sıfır risk.

### Kademe 3: Benim üretemediklerim

| Şey | Çözüm | Bütçe |
|---|---|---|
| Doku | Yok. Low-poly düz renk, vertex color. Doku olmadığı için üretilmesi de gerekmiyor | 0 |
| Arayüz ikonları | SVG. Ben yazarım, sikke ikonunu zaten yazdım. Unity'ye PNG olarak Blender ya da Inkscape ile basılır | 0 |
| Karakter portreleri | Gerekmiyor. Karakterler 3B; portre, kameranın kafaya yaklaşmasıyla çalışma anında alınır | 0 |
| Kapak görseli, mağaza ekran görüntüleri | Oyunun kendisinden, kurulu sahne ve iyi ışıkla. Blender'da yüksek çözünürlük render | 0 |
| Ses | [17-audio-design.md](17-audio-design.md) kaynağı; değerlendirme onayladı (C4) | 0 |
| Logo, yazı tipi | Google Fonts açık lisans; logo tipografik | 0 |

Sıfır bütçeyle kapanmayan tek şey: bir gün profesyonel kapak görseli istersen. Gelirden.

---

## Ücretsiz kaynaklar, lisansıyla

| Kaynak | Ne | Lisans | Ne için |
|---|---|---|---|
| Kenney | Low-poly mobilya, yemek, karakter paketleri | CC0 | Prototip, prosedürel bitene kadar yer tutucu; bazıları kalıcı olabilir |
| Quaternius | Universal Animation Library 1-2 (250+ klip), iskeletli low-poly karakterler, yemek | CC0 | **Birincil animasyon kaynağı**; karakter yedek planı |
| Poly Haven | HDRI | CC0 | Blender render ışığı, oyunda kullanılmaz |
| Mixamo | İnsansı animasyon klipleri, otomatik iskelet | Adobe, oyunda ücretsiz; bakımsız | Yedek klip kaynağı, sen indirirsin |
| Google Fonts | Yazı tipleri | OFL | Arayüz |

Lisans denetim tablosu [21-business-and-release.md](21-business-and-release.md)'de; her varlık oraya kaynak ve lisansıyla yazılır. CC0 dışı hiçbir şey sorulmadan girmez.

---

## Üçgen bütçeleri

[19-technical-setup.md](19-technical-setup.md) sahne bütçesini veriyor. Varlık başına:

| Sınıf | Üçgen | Örnek |
|---|---|---|
| Küçük eşya | ≤ 150 | Tabak, bardak, saksı |
| Mobilya | ≤ 600 | Masa, sandalye, dolap |
| Ekipman | ≤ 900 | Ocak, fırın, tezgah |
| Karakter gövdesi | ≤ 2.000 | Tek mesh |
| Kıyafet parçası | ≤ 250 | Önlük, şapka |
| Saç | ≤ 300 | |

Temas sayfası üçgen sayısını basar; bütçe aşılırsa betik uyarır. Bugünkü masa seti 2.004 üçgen; iki sandalyeyle mobilya sınıfında üç parça, yani sınır içinde ama pah kırma segmentleri düşürülerek yarıya iner. İlk not bu.

---

## Kim ne yapar

| İş | Kim | Nasıl |
|---|---|---|
| Betik yazmak | Ben | Doğrudan |
| Blender çalıştırmak, render almak | Ben | Claude Code uzaktan kontrolle komut satırından |
| PNG'ye bakıp teknik not vermek | Ben | Read |
| PNG'ye bakıp "beğendim / beğenmedim" demek | Sen | Telefondan bile |
| Mixamo'dan yedek klip indirmek | Sen | Uygulama aşamasında, gerekirse, on beş dakika |
| Unity'ye içe aktarmak | Ben | Manifestle otomatik |
| Cihazda bakmak | Sen | APK kurulumu |

Sen Blender'ı hiç açmıyorsun. Bu, planın varsayımı değil, bugün doğrulanmış durumu.

---

## Zaman kutusu

| Kanıt | Süre | Başarı ölçütü | Başarısızsa |
|---|---|---|---|
| Mobilya hattı | ✅ Bitti | Sekiz proplu fast food seti üretildi, hepsi üçgen bütçesinde | — |
| Ortam seti | 1 hafta | Fast food dükkânı: 12 prop, tek sahne, Unity'de cihazda 60 fps | Kenney paketi kalıcı olur |
| Karakter hattı | ✅ Modelleme tarafı bitti | Gövde, iskelet, iki kıyafet parçası, saç ve altı poz doğrulandı. Kalan: klipleri yeniden hedefleyip Unity'de cihazda göstermek | Quaternius karakterleri, malzeme farkı |
| Yemek tabakları | 3 gün | 6 taban × 8 üst malzeme, 32 yemeği kapsıyor | Yemekler ikon olur, tabak boş |

Zaman kutusu dolunca yedek plana geçilir, tartışılmaz. Değerlendirme bunu istedi; doğru istedi.

---

## Yemek: 32 yemek, 32 mesh değil

Sen yemek çeşidinin çok olmasını istedin; tasarımcı "parametreliyse olur" dedi ([23-core-contract.md](23-core-contract.md) §8.3). Sanat tarafında da aynı mantık: **modüler tabaklama.**

```
tabak = taban + 0-3 üst parça
```

| Taban (6) | Üst parça (8) |
|---|---|
| Yuvarlak tabak | Küre yığını (köfte, pilav, nugget) |
| Oval tabak | Dilim (ekmek, pizza) |
| Kâse | Silindir (bardak, kutu) |
| Tepsi | Yaprak (salata, marul) |
| Kağıt (fast food) | Şerit (patates, makarna) |
| Fincan | Sos lekesi |
| | Çubuk (pipet, kürdan, çubuk) |
| | Buhar (sadece çorba ve ramen, partikül) |

Renk malzemeden. Hamburger = kağıt + dilim + küre + yaprak, hepsi kahverengi-yeşil tonlarında. Ramen = kâse + şerit + küre + buhar. 32 yemek, 14 mesh, sonsuz kombinasyon. `dishes/*.json` içindeki `plating` alanı bunu söylüyor.

Oyuncu tabağı 40 piksel boyunda görüyor. Bu ayrıntı yeter.

---

## Unity tarafında render döngüsü

Blender döngüsü kanıtlandıktan sonra aynı şeyin Unity'de de olması gerekiyordu; yoksa görünüm katmanı hiç görülmeden yazılırdı.

`tools/unity/shot.ps1` ve `Assets/Lokanta/Editor/SceneShot.cs`. Toplu kipte sahne kuruluyor, `RenderTexture`'a çiziliyor, PNG yazılıyor. Ekran gerekmiyor.

### İki tuzak, ikisi de ölçülerek bulundu

**1. `-nographics` bayrağı render'ı kapatıyor.** `run.ps1` onu kullanıyor çünkü orada yalnızca ayar uygulanıyor. Görüntü alan çalıştırıcı o bayrağı kullanmıyor.

**2. Unity, `-executeMethod`'u derleme bitmeden çalıştırabiliyor.** Günlükte "Requested script compilation" yazıyor, derlenen DLL'in içinde yeni kod var, ama çalışan sürüm eski. `shot.ps1` bu yüzden önce bir **ısınma turu** yapıyor: birinci tur yalnızca derliyor, ikinci tur çalıştırıyor.

### Sınır: toplu kipte URP Lit çalışmıyor

Malzemeler doğru atanıyor, URP etkin, `_BaseColor` yazılıp geri okunuyor. Buna rağmen **ışık alan her yüzey aynı rengi** veriyordu.

Ölçüm şöyle daralttı:

| Ölçüm | Sonuç |
|---|---|
| Kamera arka plan pikseli | Birebir doğru |
| Malzeme rengi, CPU tarafında geri okuma | Birebir doğru |
| Etkin boru hattı | LokantaURP |
| Işık alan yüzeylerin pikseli | Hepsi aynı, atanan renkten bağımsız |
| Aynı sahne, **Unlit** shader ile | Bütün pikseller birebir doğru |

Sebep: **editör toplu kipinde URP Lit'in shader varyantları derlenmiyor** ve yüzeyler tek bir geri dönüş rengine düşüyor. Unlit'in varyant sayısı çok daha az olduğu için etkilenmiyor.

### Bunun pratik anlamı

| Doğrulanabilen | Doğrulanamayan |
|---|---|
| Yerleşim, ölçek, oran | Gölgeleme ve gölgeler |
| Kamera açısı ve çerçeveleme | Işık rengi ve yoğunluğu |
| Renk paleti (birebir) | Son görünüm |
| Nesnelerin birbirine göre konumu | Malzeme parlaklığı |

Yani Unity görüntüsü **kompozisyon denetimi** için kullanılıyor, son görünüm denetimi için değil. Son görünüm iki yerde denetlenecek: Blender render'ları ve gerçek cihaz.

Doğrulama sahneleri Unlit malzeme kullanıyor; oyunun kendisi Lit kullanmaya devam ediyor.

