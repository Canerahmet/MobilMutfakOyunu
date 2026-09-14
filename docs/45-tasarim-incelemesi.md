# 45 — Tasarım incelemesi ve ölçümler

*13 Eylül 2026.* Beş agent oyunu beş tasarım ekseninden değerlendirdi:
ekonomi/ilerleme, anlık karar kalitesi, ilk oturum ve okunabilirlik, mutfak
kimliği, son oyun ve motivasyon. Hiçbiri dosya değiştirmedi; hepsi kanıt
(dosya:satır) ve somut öneri verdi. Bu belge **ölçülen** ve **kapatılan**
kısmı yazıyor.

Kural, bu projenin her yerinde olduğu gibi: *bir bulgunun teşhisi doğru,
çaresi yanlış olabilir.* Her iddia kabul edilmeden önce ya koda ya ölçüme
soruldu — ve bir tanesi ölçümle **çürüdü**.

---

## 1. En büyük açık: itibar tavanındayken zam bedavaydı

Fiyatın talebe **hiçbir doğrudan kanalı yoktu**. `DemandModel.CustomersPerDay`
fiyat parametresi almıyordu; fiyatın tek yolu memnuniyet → itibar idi. İtibar
ise masa kademesinin tavanına (55/75/90/100) **sert kırpılıyor**. Yani tavana
dayanmış bir oyuncu için memnuniyet kaybı hiçbir şey satın almıyordu.

Denge botları 8500 (ucuz), 13000, 23000 ve 24000 bp'de duruyordu — **10000 ile
13000 arasında hiçbir ölçüm yoktu** ve açık tam orasıydı. Bandın içine bir bot
kondu (`orta_fiyat`, 11000 bp):

| strateji | son kasa | itibar | masa | kadro |
|---|---:|---:|---:|---:|
| makul | 18.670 | 79,3 | 7,9 | 4,3 |
| plancı | 25.092 | 94,6 | 11,8 | 7,4 |
| **orta_fiyat** | **27.849** | 75,0 | 7,0 | 4,0 |
| yuksek_fiyat (+%30) | 380 | 0,0 | 4,0 | batıyor |

Sabah bir kez basılan bir düğme, oyunun en gelişmiş stratejisini **daha az masa
ve daha az kadroyla** geçiyordu. Ceza yalnızca bandın dışında vardı.

**Düzeltme:** `priceElasticityBp` (9000) — fiyat sapması artık doğrudan talebi
etkiliyor, taban ve tavanla sınırlı.

| strateji | önce | sonra |
|---|---:|---:|
| orta_fiyat | 27.849 | **19.393** |
| makul | 18.670 | 18.670 |
| plancı | 25.092 | 25.092 |
| ucuz_fiyat | 12.498 | **15.059** |

Piyasa fiyatıyla oynayan **her** strateji birebir aynı kaldı: kanal yalnızca
sapmada devreye giriyor. Zam artık takas — `orta_fiyat` %4 önde ama 1659 kişi
ağırlıyor (2034 yerine), itibarı 72,8 ve 6,9 masada kalıyor.

Beklenmeyen kazanç: **ucuz fiyat gerçek bir strateji oldu** (12.498 → 15.059,
2044 → 2335 kişi). Hacim oyunu ile marj oyunu iki ayrı meşru yol; eskiden ucuz
fiyat sadece kötüydü.

### Ağırlıklar ve tek kapı

Menü sapması, sipariş olasılıklarıyla ağırlıklandırılıyor — `RecommendedRestock`
ile **birebir aynı** ağırlıklar. İki ayrı ağırlık olsaydı hal ekranı ile talep
birbirini yalanlardı.

Talep altı ayrı yerde hesaplanıyordu (bugünkü kadro, yarınki kadro, zirve
kadro, önerilen stok, beklenen kişi, geliş planı). Hepsi tek yardımcıdan
geçiyor: birini atlamak, hal ekranının **gerçekte gelmeyecek müşteriye göre**
stok önermesi demekti.

Bunu **yapısal** bir test koruyor (`PricingTests.Talep_tek_kapidan_geciyor`):
`Simulation` içinde `CustomersPerDay`'i yalnızca `ExpectedCustomers` çağırabilir.
Önce davranışla ölçmeyi denedim, olmadı — kadro ve stok tamsayı ve 1. günde
zaten tabanda, yani fark yuvarlanıp kayboluyor ve test vakumda yeşil kalıyordu.
Canlılık kontrolü yakaladı. Taramanın kırılabildiği de doğrulandı: dışlama
olmadan tam 1 çağrı buluyor.

---

## 2. Üç fiilden biri ölüydü: çay

Çay her eksende patron ilgisinin altındaydı — memnuniyet 900'e karşı 2400,
sabır ×1'e karşı ×2, mutfağı hızlandırmıyor — ve **üstelik kasadan para
çıkarıyordu**; ilgi bedava. Aynı müdahale hakkını yaktıkları için çaya basmak
için hiçbir gün yoktu. Projenin kendi denge botu da çaya hiç basmıyordu; bu,
tespitin kanıtlarından biriydi.

**Düzeltme:** çay artık **salona** gidiyor — bekleyen herkese. İkisi farklı
soruya cevap veriyor:

| fiil | kime | ne zaman |
|---|---|---|
| İlgi | bir masaya, derin (×2 sabır + mutfağı öne alma) | krizdeki tek masa |
| Çay | bekleyen herkese, sığ | salon toptan sabırsızken |

Bedeli de oradan: çay salondaki bütün bekleyenlerin kişi sayısı kadar tutuyor —
kalabalıkta hem en değerli hem en pahalı. Bekleyen yoksa düğme kapalı.

İki hatayı test yakaladı: çay artık hedef istemiyor ama dal parti geçerlilik
kontrolünün **altındaydı**, yani arayüzün seçim yokken yolladığı `-1` sessizce
reddediliyordu — düğme hiçbir şey yapmıyordu.

---

## 3. Ölçümle ÇÜRÜYEN iddia: "bot müdahaleleri kötü zamanlıyor"

Bulgu şuydu: bot haklarını her 20 sim-saniyede bir yakıyor, yani günde dört hak
480 saniyelik günün ilk ~80 saniyesinde bitiyor; zirve ise ikinci dilimde.
Yani "müdahale kazandırıyor mu" sorusu, oyuncunun verdiği **tek gerçek kararı**
("şimdi mi, zirvede mi") sabit tutarak, üstelik en kötü değerinde ölçüyor
olabilirdi. Teşhis makuldü.

Deney: `sabirli_mudahale` — aynı fiiller, aynı sıra, tek fark bir masa uyarı
eşiğinin altına inmeden hiçbir hak harcamaması. Her tik soruluyor, çünkü kriz
penceresi ~3 saniye ve yirmi saniyede bir bakan bir bot onu kaçırır; o zaman
ölçüm "saklamak işe yaramıyor" derdi ama ölçtüğü şey kendi göz kırpması olurdu.

| strateji | son kasa | harcanan müdahale |
|---|---:|---:|
| makul (hiç yok) | 18.670 | 0 |
| mudahaleci (hemen) | 18.869 | 5.784 |
| sabirli_mudahale (zirveye saklar) | 18.777 | 1.186 |

**Saklamak kazandırmadı.** Sorun botun oynayışı değil, mekaniğin kendisi.

Asıl bilgi sayının içinde: sabırlı kol 1440 günde 1186 müdahale yaptı — **günde
0,8**. Çünkü kadrosu düzgün bir lokantada bir masa uyarı eşiğinin altına
neredeyse hiç inmiyor. Müdahale, oyuncunun düzgün oynarken neredeyse hiç
girmediği bir durumun kurtarma aracı; değeri o yüzden nötr.

**Bu oyunun vaadiyle çelişiyor.** Mağaza metni (docs/44) "SERVİS SIRASINDA SEN
VARSIN" diyor ve dört müdahale hakkını ana mekaniklerden biri olarak satıyor.
Ölçüm, mekaniğin bugün bir emniyet ağı olduğunu söylüyor.

Açık karar — üç yol var, hiçbiri tek satırlık değil:
1. Krizi yaygınlaştır (oyunu zorlaştır),
2. Müdahaleye kriz **dışında** bir iş ver (bahşiş, müdavim yakınlığı, masa
   devir hızı),
3. Vaadi ölçüme uydur ve mağaza metnini değiştir.

Hak sayısı bu arada masa sayısına bağlandı (4 masa 4, 8 masa 5, 12 masa 6):
sabit dört, mekaniği **tam da en gerekli olduğu yerde** siliyordu. Tek başına
sonucu değiştirmedi (18.859 → 18.869) ama yanlış olan bir şeyi düzeltti.

---

## 4. Yalan söyleyen ölçüler

**Olayın adı yanlış yemeği yazıyordu.** `StockOut` olayı `A` alanına PARTİ
indisini yayıyordu, arayüz ise `A`'yı yemek indisi sanıp isim basıyordu — parti
yuvaları küçük numaralardan dağıtıldığı için çoğu zaman **geçerli ama yanlış**
bir yemek adı. Hiçbir şey hata vermiyordu. Olay zaten "malzeme bitti" de
değildi: müşteri menüde yapabileceği ana yemek bulamayınca kapıdan dönüyor.
Artık `TurnedAway` ve metni doğruyu söylüyor.

**Dört ölü komut.** `SetDailySpecial`, `AssignStation`, `RefillBroth` tanımlıydı,
hiçbir yerden gönderilmiyordu ve `Apply`'ın `switch`'inde de yoktu — enum üç
mekanik vaadini var gösteriyordu. Silindi; sayılar yeniden numaralanmadı, çünkü
komut türü kayıtlarda sayı olarak geçiyor. `InterventionKind.Apology` daha
kötüsüydü: gönderilseydi **çayın parasını ödemeden çayın etkisini** alıyordu.
Artık 0 `None` ve bilinmeyen tür açıkça reddediliyor.

**`StockDaysLeft()` gün değil yemek sayıyordu** ve uyarı metni "Stok bugünü
çıkarmaz" diyerek gün vaat ediyordu. Tik `>= 1` ile yeşile dönüyordu: altı
yemeğin her birinden birer porsiyonu olan oyuncu "hazır" görünüp servisi
açıyor, ilk on dakikada malı bitiyordu.

Ad düzeldi (`MakeableDishCount`) ve satır artık günü ölçüyor
(`StockCoverageBp`): **"Stok 8 / 13 kişiye yetiyor"**. Ölçüt **en kıt malzeme**,
toplam değil — yirmi malzemesi bol biri bitmiş bir mutfak toplamda dolu görünür
ama o malzemeyi isteyen her sipariş kapıdan döner. İhtiyaç hal ekranının kendi
hesabından geliyor, yani iki ekran aynı kaynaktan konuşuyor.

İlk metnim iki dilde de **kırpıldı** ve turun kırpma denetimi yakaladı.

---

## 5. Kampanyanın hedefi görünmüyordu

Oyuna "altmış gün" diyen tek bir satır yoktu; yedi eksenli değerlendirme
yalnızca 61. günde açılıyordu. Oyuncu 2,5 saat boyunca bitiş tarihi olmayan bir
dükkân işletip hiç duymadığı bir karneyle karşılaşıyordu — hedef değil, sürpriz.

HUD'da artık aşamanın yanında **"40 / 60. gün"** yazıyor; serbest oyunda
"serbest oyun"a dönüyor.

---

## 6. Aynı etiket, üç farklı sayı

`ui.hud.angry` üç yerde kullanılıyordu: servis kartında (TOPLAM kayıp), akşam
şeridinde (TOPLAM), gün raporunda (yalnızca MASADAN kalkan). Oyuncu aynı gün,
aynı kelimenin altında iki farklı sayı görüyordu. Sayılar zaten ayrılmıştı;
eksik olan **adların** ayrılmasıydı — artık "Kaybedilen" / "Masadan kalkan" /
"Kapıdan dönen".

Kapıdan dönen müşteri ayrıca **servis sırasında** da görünüyor artık. İlk
haftanın en sık ölüm biçimi bu ve sayı yalnızca gün raporundaydı, yani oyuncu
onu ancak düzeltmesi imkânsızken görüyordu.

---

## 8. Veresiye bir defter oldu, prim düğmesi değil

Ödeyen fişin **%112'sini** ödüyordu ve şans herkes için sabit %85'ti (çayla
%95). Beklenen nakit **1,064 × fiş** — yani veresiye peşin satıştan *kârlı*.
"Hayır" demek için hiçbir gün yoktu.

Daha derin kusur şuydu: **defter kimin borcu olduğunu tutmuyordu**
(`_tabAmount`, `_tabDueDay`, `_tabTea` — müdavim bağı yok). Tahsilat şansı
herkes için aynı olduğundan "kime yazayım" diye bir soru **doğamıyordu bile**.

Düzeltme:

| | önce | sonra |
|---|---:|---:|
| taban şans | 8500 | **6000** |
| güven (ziyaret başına) | — | **400 bp, tavan 3000** |
| çay primi | 1000 | 1000 |
| şans tavanı | 10000 (kesinlik) | **9500** |
| ödeme primi | 1200 | **800** |

Yeni tanıştığın biri %60'ta, yıllardır gelen %90'da, çayla +%10. Tavan tam
kesinlik değil — risksiz bir defter yine karar üretmeyen bir prim düğmesidir.

**Ölçüm (24 tohum, 60 gün, türk):**

| strateji | son kasa | defterde | yıl sonu puanı |
|---|---:|---:|---:|
| makul (defteri hiç açmıyor) | 20.822 | — | 61 |
| imzacı (herkese yazıyor) | 18.408 | 2.501 | 71 |
| **seçici (yalnızca 5+ ziyaretli)** | **19.289** | 2.519 | 71 |

İki iç içe karar çıktı: *defteri kullanayım mı* (nakit ↔ yıl sonu puanı) ve
*kime yazayım* (seçici olmak aynı puanla **+881 sikke**). Eskiden tek cevap
vardı: herkese evet.

Seçici kol (`secici_veresiye`) bilerek ayrı bir strateji olarak yazıldı —
aynı kalıp müdahalede kullanılmış ve orada beklenenin **tersini** söylemişti,
o yüzden tahmin edilmedi, ölçüldü.

İki sessiz tuzak kapandı: `_tabRegular` dizisinin varsayılanı 0'dı ve 0
geçerli bir müdavim indisi — boş bir hesap hiç tanımadığı birinin güvenini
kullanırdı. Ve `content/cuisines/turk.json` **üretilen** bir dosya; elle
düzenleseydim ilk denetimde sessizce geri alınırdı.

`SaveVersion` 17 → 18.

---

## 9. Varlık ekseni yatırımı cezalandırıyordu

`worth = kasa + defter` idi: sahip olunan ekipman ve masalar **hiç
sayılmıyordu**. Yani ekipman aldıkça "Varlık" çubuğu kısalıyor — oyunun teşvik
ettiği şey karnede ceza olarak dönüyordu, ve oyuncu doğru oynadıkça puanının
neden düştüğünü hiçbir ekranda göremiyordu.

Sahip olunanların değeri, katalogun tamamından **kalanı çıkararak** bulunuyor.
Ayrı bir toplama yazılmadı bilerek: iki ayrı hesap bir gün birbirinden ayrılır
ve hangisinin doğru olduğu anlaşılmaz — bu dosyada aynı hata bir kez yaşandı
(iki "geriye ne kaldı" fonksiyonundan biri soğuk havayı sayıyor, öteki
saymıyordu).

| strateji | varlık (önce) | varlık (sonra) | toplam puan |
|---|---:|---:|---:|
| genişlemeyen | 12 | 22 | 46 |
| makul | 25 | **54** | 64 |
| plancı | 32 | **76** | 73 |
| imzacı | 27 | **57** | **78** |

Eksen artık ayırt ediyor: büyüyüp yatırım yapan, parayı yastık altında tutandan
yüksek alıyor. Yan etki: en üst plaketin eşiği 80 ve ölçülen hiçbir strateji
oraya yaklaşamıyordu — yani oyuncunun peşine düşeceği tepe muhtemelen boştu.
İmzacı 78'e çıktı; tepe artık var ve zor.

---

## 10. DOKUNULMAYAN bir eksen: kombo hedefi

Kombo ekseninin de katılım rozeti olduğu doğru: `imzaci` %17,0 payla **100**
alıyor, hedef %15, yani tavanda ve eğim yok.

Ama hedefin gerekçesi `export.py` içinde **zaten yazılmış**: *"kombo mutfak
yükünü de artırdığı için zirvede kapatmak meşru bir oyun ve eksen onu
cezalandırmamalı."* Hedefi yükseltmek tam da o kararı bozar — eksen kombo
**payına** baktığı sürece "açık tut" ile "zirvede kapat" zıt yönlerdir.

Düzeltmesi hedefi değil **ölçülen şeyi** değiştirmeyi gerektiriyor (örneğin
kızgın müşteri başına kombo cirosu). Bu bir tasarım kararı, bir sayı ayarı
değil; yazılı bir gerekçeyi kendi zevkimle bozmamak için dokunulmadı.

Veresiye ekseni ise **kendiliğinden düzeldi**: tahsilat artık güvene bağlı
olduğu için herkese yazan bot 100 değil **72** alıyor. Eksen "kullandın mı"
değil "iyi kullandın mı" diye soruyor.

---

## 12. İtibar tavanına taşma kabı

Tavan doğru bir fikir — dört masalık bir dükkân semtin konuştuğu lokanta
olamaz — ama taşan değeri **silmek** bir şey daha yapıyordu: tavandaki oyuncu
için mükemmel bir gün ile idare eden bir gün arasında **ölçülebilir hiçbir fark
kalmıyordu**. Ölçüm (docs/06): iyi oynayan yedi masada 75'e dayanıp otuz iki
gün orada duruyor — kampanyanın yarısından fazlası karşılıksız.

Bu, §1'deki kuralın ters yönü. Orada *tavanın üstünde ödenen bedel bedavaydı*;
burada *tavanın üstünde kazanılan da bedava veriliyordu.*

Taşan itibar artık `_reputationOverflowCenti` içinde birikiyor ve
**genişlendiğin gün ödeniyor**. Kap bir kademe kadar: sonsuz birikim,
genişleme gününde itibarı doğrudan tavana fırlatır ve yeni kademenin kendi
emeğini anlamsız kılardı.

**Ölçüm (24 tohum, 60 gün, fast food):**

| strateji | kasa (önce → sonra) | itibar | masa |
|---|---|---:|---:|
| makul | 18.670 → 18.820 | 79,3 → 79,4 | 7,9 |
| plancı | 25.092 → **23.921** | 94,6 → 95,0 | 11,8 → **12,0** |
| imzacı | 20.427 → 20.595 | 79,4 | 7,9 |

Yön beklenen: itibar sıçrayınca talep de sıçrıyor, kadro ve stok maliyeti onu
takip ediyor — plancı daha çok büyüyor ama nakdi düşüyor. **Kayda değer yan
etki:** `kredisiz` kolu (kredi almayı reddeden bot) 56. günde borca giriyor;
daha hızlı büyüyünce fazla uzanıyor. Ayarlanmadı, kaydedildi — ayarlamak kendi
ölçüm turunu ister.

### Testi yazarken üç kez yanıldım

1. Dükkân kadrosuz oynuyordu → itibar 40 günde **3459'da dondu**.
2. Kadro + genişleme eklendi → 120 günde **3730'da dondu**, tavan 7500.

Sebep servis değil **ölçek**: dört masada günde yedi grup ağırlanıyor, günlük
itibar kazancı günlük erimeyle dengeleniyor ve denge noktası ~34,6 çıkıyor —
tavan ise 55. Yani o kademede tavan **hiç bağlayıcı değil** ve taşma diye bir
şey oluşmuyor. Tavana dayanmak için testin harness botu kadar iyi oynaması,
yani **testin içine bir bot yazmak** gerekirdi.

Doğru çözüm tavanı **içerikten** düşürmek oldu (`EconomyConfig.WithTiers`,
dosyanın kendi `With...` ailesine uyan bir ekleme): kural aynı kural, yalnızca
görünür olduğu eşik yaklaştırıldı. Arka kapı yok, setter yok.

`SaveVersion` 18 → 19.

---

## 13. Yapılmayan iki madde ve nedenleri

İki açık madde, koda bakınca **dayanaksız** çıktı. İkisi de aynı sınıftan: kod
zaten o kararı vermiş ve gerekçesini yazmış.

**Mevsimin talebe etkisi.** Bulgu "docs/34 kış en yoğun diyor, kod yalnızca
malzeme fiyatını değiştiriyor" diyordu. docs/34 §2 okununca: mevsim zaten bir
**karar** olarak uygulanmış — sonbaharda ucuza alıp soğuk odada saklayıp kışa
taşımak — ve ölçülmüş (`plancı` %9 kazanıyor, 17.722 → 19.327). "Kış en yoğun
dönem" cümlesi mevsim çarpanı değil, kampanya sonunda dükkânın zaten büyük
olması; yanında "yapısal bir yan etki ve **kasıtlı bırakıldı**" yazıyor.

Talebe mevsim çarpanı eklemek boşluk kapatmak değil, **yeni bir mekanik icat
etmek** olurdu.

**`model.py`'de ekipman kalemi.** `week_pnl` içindeki yorum zaten şöyle
diyor: *"EKIPMAN BU LEDGERDE YOK, ve bu bilinçli bir karar. Denendi ve battı:
... model 73.000 sikke borca düşüyordu. Doğru yer simülasyon."* Ekipman
fiyatları kapalı form modelle değil harness ölçümüyle ayarlanıyor.

### Ama içinde iki gerçek madde vardı

**`REALISATION_BP` iki dosyada iki değerdi** — `model.py` 7000 (oyunun
içeriğine yazılan), `solve.py` 9335. Kalibrasyon bozuk değildi: `solve_for()`
taramada her aday için `solve.py`'yi yamıyor, tarama bitince orada **son
denenen** aday kalıyor. Ama docs/12'nin tarif ettiği elle akışta
(`python solve.py`) bu **yanlış kira** üretirdi. `calibrate.py` artık seçilen
oranı `solve.py`'ye de geri yazıyor, ve dosyada değerin nereden geldiği yazılı.

**`MARGIN_TARGETS` "net marj" diye okunuyordu.** Adının yanına ne olduğu
yazıldı: **sermaye gideri öncesi** marj. Ekipman merdiveni 14 masada ~37.600
sikke ve bu defterde yok.

---

## 15. Talep artık oynuyor — ama yalnızca gerçekleşen

Talep tamamen belirlenimciydi: aynı itibar ve masa sayısındaki her salı
**birebir aynı** sayıda müşteri getiriyordu. Sonucu, sabah stok kararının bir
yargı değil bir düğme olmasıydı — hal önerisi her zaman tam doğruydu ve yeni
eklenen "Stok 8 / 13 kişiye yetiyor" satırı hiçbir zaman kırmızıya dönmüyordu.

Mekaniğin tamamı **ayrımda**:

| | ne veriyor | kim kullanıyor |
|---|---|---|
| `ExpectedCustomers` | beklenti | kadro önerisi, hal önerisi, beklenen kişi |
| `ActualCustomers` | gerçek | **yalnızca** geliş planı |

Sapma tahmine de yansısaydı oyuncu yine kesin bilgiye sahip olurdu ve oynaklık
dekor kalırdı.

Üç şey korundu: çekiliş `_rngEvent` akışından (zaten vardı, kayda giriyordu,
hiç kullanılmıyordu) olduğu için **tekrar oynatma birebir aynı**; altın hafta
testi `WeeklyPlanner`'ı ölçtüğü için etkilenmedi; sapma tamsayı aritmetiğiyle
çekiliyor (çekirdekte kayan nokta yasak).

**Ölçüm dürüst okunmalı.** ±%10 sapma, hal önerisinin **%20 emniyet payının**
içinde kalıyor — `makul` 18.820 → 19.085, zayiat 4.960 → 4.829, yani fark
gürültü içinde. Yarattığı karar "önerileni al" oyuncusu için değil, stoktan
**kısan** oyuncu için: eskiden kısmak hesaplanabilir bir bahisti, artık gerçek
bir bahis. Daha sert ısırması istenirse kaldıraç oynaklık değil emniyet payı.

Küçük bir yan not: `RecommendedRestock`'un yorumu zaten *"talep dalgalanıyor"*
diyordu — o cümle bugüne kadar **doğru değildi**.

---

## 16. Müdahale artık salonda da çalışıyor

§3'ün açık bıraktığı karar buydu: ölçüm mekaniğin bir emniyet ağı olduğunu
söylüyordu (kadrosu düzgün lokantada günde 0,8 müdahale), mağaza metni ise onu
ana mekanik diye satıyor. Üç yoldan **"müdahaleye kriz dışında bir iş ver"**
seçildi, çünkü tek başına zorluk eğrisine dokunmuyor ve vaadi koruyor.

Eksik olan **salon tarafıydı**. İlgi sabrı uzatıyor ve *mutfağı* hızlandırıyordu
(`HurryPartyJob`) ama salona hiç dokunmuyordu — oysa darboğaz çoğu zaman orada.
"Patron kendi ilgileniyor" tam olarak siparişi/hesabı onun alması demek.

Artık ilgilenilen masanın **sıradaki salon işi yarıya iniyor**
(`attendWorkCutBp = 5000`) ve işaret kullanılınca tükeniyor: ilgi bir **adım**,
sürekli bir hâl değil — yoksa bir kez ilgilenilen masa gün boyu ayrıcalıklı
olurdu.

**Ölçüm (24 tohum, 60 gün, fast food):**

| strateji | önce | sonra | ağırlanan grup |
|---|---:|---:|---:|
| baskılı (kadro eksik, müdahale yok) | 21.665 | 21.612 | 1955 |
| **baskılı + müdahale** | 21.685 | **22.195** | 1978 |
| **fark** | **+20** | **+583** | **+23** |

Kadrosu eksik oyuncu için müdahalenin 60 günlük getirisi +20'den +583'e çıktı.

**Rahat kadroyla oynayan için hâlâ ödemiyor** (`makul` 19.085, `mudahaleci`
18.979) ve bu doğru: parayla kadro alıp ihtiyacı satın almışsın. Ortaya gerçek
bir takas çıktı — *bir kişi eksik çalış, serviste sen koş* — ve
`baskili_mudahale` artık plancıdan sonraki en iyi strateji.

**Mağaza metni güncellenmeli** (docs/44): "günde dört müdahale hakkın var"
cümlesi artık iki yerden yanlış — hak sayısı masayla büyüyor ve çay tek masaya
değil salona gidiyor.

---

## 18. Kombo ekseni: doygunluk semptom, sebep başka

§10'da eksene dokunmamıştım çünkü hedefin gerekçesi yazılıydı. Bu kez ölçtüm
ve **sebep çıktı**.

### Önce bir ölçüm hatası düzeldi

`_mainOrders` koşulsuz sayıyordu, oysa kombo **16. günde** açılıyor: payda,
payın yapısal olarak sıfır olduğu on beş günü de içeriyordu. Eksen gerçek
kullanımı üçte bir oranında eksik gösteriyordu.

Payda artık yalnızca mekanik açıkken sayıyor. Ölçü adıyla doğruyu söylüyor:
*komboya dönebilecek siparişlerin yüzde kaçı komboya döndü.* Hep-açık botun
payı **%17,0 → %20,8**.

### Sonra hedefi ölçmeye çalıştım ve mekaniği buldum

Hedefi koymak için tasarımın **meşru** dediği oyunu ölçmek gerekiyordu
("zirvede kapatmak meşru bir oyun ve eksen onu cezalandırmamalı"). O oyunu
oynayan bot yoktu, yani hedef ancak uydurulabilirdi — ve kodun kendi uyarısı
bunu yasaklıyor.

`zirvede_kapat` kolu yazıldı: salon yarısı dolunca kombo kapanıyor, düşünce
açılıyor.

| strateji | kombo payı | son kasa | ağırlanan grup |
|---|---:|---:|---:|
| imzacı (hep açık) | %20,8 | 21.157 | 2016 |
| zirvede_kapat | %20,0 | 20.836 | **2016** |

**İkisi aynı oyun.** Pay 0,8 puan düşüyor, ağırlanan grup **birebir aynı**,
kasa biraz azalıyor.

### Sebep: kombonun mutfak yükü ısırmıyor

Kombo sipariş başına üç iş üretiyor (tek ana yemekte beklenen 1,7) ve her işi
`kitchenLoadBp = 13500` ile %35 uzatıyor. Kâğıt üzerinde aşçının bağlı kaldığı
süre ~2,4 kat. Ama **servis edilen grup sayısı iki kolda da 2016** — yani
mutfakta boşluk var ve ek yük soğuruluyor. Darboğaz salonda (§16'daki müdahale
düzeltmesinin kazandığı yer de orası).

Bu yüzden hedefe **dokunulmadı**: ölçülen aralık 20,0–20,8 ve hangi hedef
konursa konsun iki meşru oyun da aynı puanı alır. **Eksen beceriyi ölçemiyor
çünkü ortada ölçülecek bir beceri farkı yok.**

Düzeltmesi hedefi değil dengeyi değiştirmeyi gerektiriyor — kombonun mutfak
yükünü gerçekten acıtmak (ör. `kitchenLoadBp` yükseltmek ya da kombo işlerini
tek istasyonda yığmak). Bu bir **zorluk kararı**, kullanıcıya ait.

### Bu turda ikinci kez aynı tuzağa düştüm

İlk `zirvede_kapat` eşiğim **%75 doluluk** idi ve hiç tetiklenmedi — doluluk o
seviyeye pratikte çıkmıyor (14 masanın 8-10'u dolu = %57-71). Kol `imzaci` ile
**birebir aynı** sonucu verdi ve bunu ancak iki satırın aynı olması söyledi.

*Bir ölçüm kolunun çalışmaması ile "çalıştı, fark etmedi" dışarıdan aynı
görünüyor.* Eşik %50'ye indirilince fark belirdi (20,0 / 20,8) — ve o fark
asıl cevabı verdi.

---

## 19. Kapatılmayanlar

Beş agent ~40 bulgu verdi; bu belge en taşıyıcı olanları kapatıyor. Açık
kalanlar, sırasıyla değeri yüksek olanlar:

1. ~~Müdahalenin vaadi~~ — **KAPANDI** (§16): salon işini de üstleniyor,
   kadrosu eksik oyuncu için getirisi +20'den +583'e çıktı.
2. ~~Veresiye defteri ekranda yok~~ — **KAPANDI.** `LedgerScreen` yazıldı:
   her hesap için kim, tutar, vade ve kararın kendisi olan iki sayı
   (*beklersen* / *şimdi kovalarsan* ödeme şansı). Şans simülasyonun kullandığı
   sayının ta kendisi (`TabChanceBp` tek yerde). `CollectCredit` ilk kez oyunda.
   Turda ölçülüyor.
3. **Kombo ekseni** — ölçüm hatası düzeldi (§18: payda artık mekanik açıkken
   sayıyor, %17,0 → %20,8). Doygunluk ise semptom: ölçüldü ki "zirvede kapat"
   ile "hep açık tut" **aynı oyun** (2016 grup, ikisinde de) çünkü kombonun
   mutfak yükü ısırmıyor. Düzeltmesi hedef değil DENGE — bir zorluk kararı.
4. ~~İtibar tavanı taşma kabı~~ — **KAPANDI** (§12).
5. ~~Mevsimin talebe etkisi~~ — **DAYANAKSIZ**, bulgu belgeyi yanlış okumuş (§13).
6. ~~`model.py`'de ekipman kalemi~~ — **BİLİNÇLİ KARAR**, denenmiş ve battığı
   yazılı (§13). İçindeki iki gerçek madde kapatıldı.

~~Talep tamamen belirlenimci~~ — **KAPANDI** (§15).

**Kalan:** kombo ekseni (§10) ve mağaza metninin güncellenmesi — müdahale
cümlesi iki yerden eskidi (hak sayısı masayla büyüyor, çay salona gidiyor).
