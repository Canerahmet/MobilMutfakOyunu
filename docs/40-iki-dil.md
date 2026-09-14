# 40 — İkinci dil: İngilizce

*12 Eylül 2026.* Kullanıcının isteği: **"İngilizce de ekle."**

Oyun tek dilliydi ve bu bir eksiklikten fazlası: Play Store'da Türkçe tek başına
oyunun ulaşabileceği kitleyi ülkeyle sınırlıyor. [docs/21](21-is-ve-yayin.md)
yayın planında "ilk sürüm Türkçe, İngilizce hemen sonra" yazıyordu; burada
kapanıyor.

---

## 1. Tek üreteç, iki tablo

İngilizce dizeler ayrı iki modülde (`tools/content/loc_en.py`,
`tools/content/loc_en_ui.py`) ama **aynı üretecin** içinden geçiyor
(`tools/content/gen_loc.py`). Ayrı bir araç yazmak kolay yoldu ve yanlış yoldu:
iki tablo sessizce ayrışır, ve metinde ayrışma ekranda **`[ui.staff.hire]`
yazan bir düğme** demek — oyuncunun gördüğü, benim görmediğim bir hata.

`compare(tr, en)` üç şeyi şart koşuyor ve üçü de sessiz hata üretir:

| sınanan | eksikse ne olur |
|---|---|
| **anahtar kümesi** aynı | eksik anahtar → ekranda köşeli parantezli anahtar |
| **yer tutucu** kümesi aynı (`{0}`, `{1}`) | Türkçe'de `{0}` olup İngilizce'de olmayan metin biçimleme hatası **vermez**, sessizce sayıyı hiç yazmaz |
| İngilizce metin **boş değil** | boş dize, ekranda boş bir düğme |

Ayrışma varsa üreteç **yazmadan** çıkıyor (dönüş kodu 1). 565 anahtar × 2 dil.

## 2. Çeviri politikası

Kural tek cümle: **çevrilmesi gereken çevrilir, ad olan ad kalır.**

| tür | karar | örnek |
|---|---|---|
| malzeme | tamamen çevrilir | `ingredient.kiyma` → "Minced Beef" |
| dünyaca bilinen Türk yemeği | **korunur** | Lahmacun, Döner, İskender, Mantı |
| adı tarif olan Türk yemeği | çevrilir | `mercimek_corbasi` → "Lentil Soup" |
| korunan ama tanınmayan | ad + kısa açıklama | `karniyarik` → "Karnıyarık (Stuffed Aubergine)" |
| müdavim adı | **korunur** | Burak, Nermin |
| hikâye repliği | yeniden seslendirilir, birebir çevrilmez | — |

41 anahtarın iki dilde metni **aynı** ve hepsi bilinçli: özel adlar ve uluslararası
kelimeler (hamburger, mozzarella, bulgur). `compare` bunu hata saymıyor — aynı
olması gerekenler gerçekten aynı olmalı.

## 3. Dil bir seçim

`Loc` artık iki tabloyu da tanıyor. Üç karar:

- **Sıra kaydediliyor, kod değil.** `Languages = { "tr", "en" }` ve
  `PlayerPrefs`'e **dizin** yazılıyor. Yeni dil **sona** eklenir, araya değil —
  araya eklemek eski cihazlardaki seçimi de değiştirir.
- **İlk açılışta sorulmuyor, tahmin ediliyor.** Cihazın dili Türkçe ise Türkçe,
  değilse İngilizce. Yanlış tahmin Ayarlar'dan tek dokunuşla düzeliyor; açılışta
  dil soran bir ekran ise herkesin her kurulumda geçtiği bir engel.
- **Her dil KENDİ adıyla yazıyor** ("Türkçe", "English"). "Turkish" yazan bir
  satırı arayan kişi zaten İngilizce biliyordur.

Biçimleme de dile bağlı: `tr-TR` / `en-GB`. Aynı para "8.000 ¤" ve "8,000 ¤".
Kültür değişmezse sayılar bir dilde yanlış okunur.

## 4. Turun bulduğu iki gerçek hata

Tur artık **dili Türkçe'ye sabitliyor** (düğmelere metne göre tıklıyor; İngilizce
bir makinede ilk adımda dururdu) ve dil değişimini **ayrıca** sınıyor.

**a. Şerit bütçesi İngilizce'de taşıyor.** Şerit yüksekliği artık **iki dilde de**
ölçülüyor (`CheckStripsBothLanguages`). Akşam şeridi Türkçe'de 199 dp, İngilizce'de
**230 dp** — bütçe 220. Tek dilde ölçen bir kontrol yeşil kalıyordu. Çözüm
etiketleri kısaltmak oldu ("Walkouts", "Satisfaction"): şerit sıkışık bir özet,
etiketler **iki dilde de** kısa olmalı.

**b. Lavaboda kimse görünmüyor.** `Lavaboda yikayan goruldu (0 kare)`. Simülasyon
o gün 12 tabak yıkamıştı — yani çekirdek doğru çalışıyordu. Sebep: bulaşık
nöbetini **patron** alıyordu ve **patron çizilmiyor**. Salon sırasındaki 0
numaralı sunucu patrondur; o yıkayınca oyuncu hiçbir şey görmez. Düzeltme,
nöbeti patrona vermemek:

```csharp
bool patron = s == 0 && _salon > 0;
if (!patron && WashNeeded()) { ... }
```

Bu, [docs/39](39-tabak-dongusu.md)'un iddiasını da tamamlıyor: darboğaz
**görünür** olacaktı; görünmeyen bir personelin yaptığı iş darboğazı görünmez
bırakıyordu.

## 5. Yeni bir kontrol, komşu kontrolleri yiyebiliyor

Şerit kontrolü İngilizce eklenene kadar tek bir ölçümdü ve ucuzdu. İki dil olunca
**dili iki kez yüklüyor ve arayüzü iki kez baştan kuruyor** — ve o saniyeler ×16
hızda koşuyordu: bir gerçek saniye servis gününün %3'ü.

Sonuç, turZ1 koşusunda altı kontrolün birden kırmızıya düşmesi oldu:

```
TANI salon dolma beklemesi: 1,0 sn, servis 25%, dolu masa 1
TANI canlilik penceresi:   11,3 sn, servis 70% -> 80% (GUN TAVANI)
HATA : Servis dolu masa uretiyor (0)
HATA : Musteri sokaktan geliyor (0 figur disarida)
HATA : Lavaboda yikayan goruldu (0 kare)
```

Canlılık penceresi günün **%70'inde** açıldı, yani boşalmakta olan bir salonu
ölçtü. Hiçbiri gerçek bir hata değildi; hepsi ölçüm penceresinin geç açılmasıydı.

İki düzeltme:

1. Şerit ölçümü artık **duraklatılmış** dünyada koşuyor (kamera bölümü gibi) —
   diller de kamera da duran bir dünyada çalışıyor, duraklatmak bedava.
2. "İkinci masa" beklemesinin artık **gün tavanı** var (%60). Bekleyen kontrol,
   beklediği şeyi yiyemez.

Düzeltmeden sonra pencere günün %10'unda açılıyor:

```
TANI salon dolma beklemesi: 0,3 sn, servis  7%, dolu masa 2
TANI ikinci masa beklemesi: 0,0 sn, servis 10%, dolu masa 3
TANI canlilik penceresi:   18,8 sn, servis 10% -> 34%
```

Kural, [docs/39](39-tabak-dongusu.md)'daki ölçüm dersinin devamı: **bir turda
ölçümler ortak bir kaynağı — servis gününü — paylaşıyor. Yeni bir kontrol
eklemek, komşu kontrollerin ölçtüğü şeyi tüketebilir.**

## 6. Aynı kural üçüncü hatayı da çıkardı

Turu sağlamlaştırırken kalan tek kırmızı şuydu:

```
HATA : Kizgin musteri varsa kriz seridi gorundu (1 kizgin, 0 kritik masa)
```

Kontrolün çıkarımı şuydu: *gün sonunda kızgın ayrılan bir grup varsa, o grup bir
noktada kritiğe inmiş olmalı — yani "SABRI TÜKENİYOR" şeridi ekranda olmalıydı.*
Çıkarım iki yerden birden çürüktü.

**a. Kapıdan dönen sayılıyordu.** `AngryParties` masa bulamayıp geri döneni de
sayıyor; o grup **hiç oturmadı**, yani masa listesi olan kriz şeridinde
görünemezdi. Birinci gün iki masa dolu, gelen geri döndü. Simülasyon artık
`AngrySeatedParties` (masadan kızgın ayrılan) de veriyor ve çıkarım yalnızca onun
üzerinden kuruluyor.

**b. Kontrol ekrana değil simülasyona soruyordu.** `CrisisTables` çekirdekten
hesaplanıyor — "kritik masa var mı". Şeridin **ekranda kurulup kurulmadığı** ayrı
bir soru; sorunca da çıktı:

> Alt çubuk servis boyunca **hiç tazelenmiyordu**. `BuildBottom()` yalnızca aşama
> değişince, servis bitince ya da bir düğmeye basılınca koşuyor. Yani hiçbir şeye
> dokunmayan bir oyuncuya kriz şeridi **hiç görünmüyor** — ve şeridin kendi uyarı
> sesi de çalmıyor (`Sfx.Upset` şerit kurulurken çalıyor). Müşteri kızgın
> ayrılınca ses geliyordu; **önceden** uyaran kanal yoktu.

Oyunun tek acil uyarı kanalı, oyuncunun zaten ekrana dokunduğu anlara bağlıydı.
Düzeltme `Tick` içinde, **sayı değişince** (her karede değil) çubuğu yeniden
kurmak. Ölçü de değişti: `GameScreen.CrisisBuilds` tam şeridin kurulduğu satırda
artıyor — oyuncunun gördüğü şey budur.

Bu üçüncü hata da [docs/39](39-tabak-dongusu.md)'un cümlesinin aynısı:
***"simülasyon şunu yapıyor" ile "oyuncu şunu görüyor" iki ayrı iddiadır.***
Bulaşıkta patron yıkıyordu ve çizilmiyordu; burada kriz vardı ve çizilmiyordu.
İki durumda da çekirdek kusursuzdu.

---

## Özet

| | |
|---|---|
| dize | 565 anahtar × 2 dil |
| ayrışma denetimi | anahtar, yer tutucu, boş metin — üçü de üreteçte |
| varsayılan | cihaz dili; Ayarlar'dan değiştirilir, `PlayerPrefs`'te kalır |
| biçimleme | `tr-TR` / `en-GB` |
| turun bulduğu | şerit İngilizce'de 230 dp; patron yıkayınca kimse görünmüyor; kriz şeridi servis boyunca hiç tazelenmiyor |
