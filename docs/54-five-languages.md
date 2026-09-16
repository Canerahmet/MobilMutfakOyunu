# 54 — Beş dil: İngilizce, İspanyolca, Çince, Arapça

*16 Eylül 2026.* Kullanıcının isteği:

> *"Oyuna İngilizce, İspanyolca, Çince ve Arapça ekle, seçeneklerden dil
> değiştirilebilsin. Ona göre de kullanılan kelimeler vs. ortak anlaşılır olsun.
> Default olarak oyun İngilizce başlasın."*

Oyun iki dilliydi ([44](40-two-languages.md)). Beş oldu. **Asıl iş çeviri değildi** —
çeviri, üretecin zaten çözdüğü bir şeydi. Asıl iş iki yazı sisteminin
kendisiydi: Çince binlerce glif istiyor, Arapça harfleri birleştiriyor ve
sağdan sola akıyor. İkisi de "metni çevir" işinin dışında.

---

## 1. Beş tablo, tek üreteç

`tools/content/gen_loc.py` anahtarları **içeriği tarayarak** çıkarıyor: elle
tutulan bir anahtar listesi yok. İkinci dil eklerken yazılmış olan `build_en()`
genelleştirildi (`build_from(mod)`) ve üç dil aynı kapıdan geçti:

| dosya | dil | not |
|---|---|---|
| `loc_es.py` + `loc_es_ui.py` | İspanyolca | **nötr** İspanyolca; *voseo* yok, tekil "tú" |
| `loc_zh.py` + `loc_zh_ui.py` | Basitleştirilmiş Çince | anakara yazımı, tam genişlikte noktalama |
| `loc_ar.py` + `loc_ar_ui.py` | Modern Standart Arapça | bölgesel ağız yok |

Her dil **aynı ayrışma kapısından** geçiyor: bir dilin tablosu ötekinden farklı
bir anahtar kümesi taşıyorsa üreteç hata veriyor. Sonuç: `635 anahtar × 5 dil`,
"bütün metinler tam".

"Ortak anlaşılır olsun" isteği kelime seçimine döndü. En somut örnek: garson
rolü İspanya'da *camarero*, Latin Amerika'da *mesero*. İkisi de karşılıklı
anlaşılır; daha geniş kitle için doğal olan **mesero** seçildi ve gerekçe
dosyanın başında duruyor.

---

## 2. Varsayılan İngilizce — ve bunu ölçen bir kontrol

Önce cihazın dili tahmin ediliyordu (`Application.systemLanguage`). Kullanıcının
kararı bunu sildi: oyun **her cihazda İngilizce açılıyor**, Türkçe oynayacak kişi
Ayarlar'dan bir kez seçiyor ve seçim kaydediliyor.

Bu karar `Loc.Preferred()` içinde **tek bir `return 1;` satırında** yaşıyor ve
üstünde, sildiğim cihaz-tahmini kodunun yeri duruyor. Geri gelmesi bir
yanlışlıkla mümkün — ve geri geldiğinde hiçbir şey hata vermez: *geliştiricinin
kendi Türkçe telefonunda doğru görünür.*

Tur artık gerçek yolu koşuyor: kaydı siliyor, tercih mantığını yeniden
çağırıyor, çıkan dile bakıyor, sonra kaydı olduğu gibi geri koyuyor.

```
tamam : Kayitsiz cihaz Ingilizce aciliyor (en)
```

---

## 3. Turun kendisi oyuncunun dilini değiştiriyormuş

Şerit bütçesi iki dilde ölçülüyordu ve ölçüm `Loc.SetLanguage()` çağırıyordu —
yani **diske yazan** kapıyı. Tur beş dili gezip sonuncusunu oyuncunun ayarı
olarak kaydediyordu.

`Loc.UseLanguage()` eklendi: uygular, kaydetmez. **Denemek ile seçmek ayrı
şeyler; artık ayrı kapıları var.**

---

## 4. Çince: ikinci yazı tipi

Rubik Latin, Kiril, İbrani ve Arapça taşıyor — CJK taşımıyor. `check_font.py`
Çince tablosunda **765 karşılanmayan karakter** saydı. Bir yazı tipi daha girdi:

- `vendor/noto-sans-sc/NotoSansSC-Regular.ttf` — **10.540.644 bayt**
- `unity/Assets/Lokanta/Art/Fonts/NotoSansSC-Lokanta.ttf` — **227.116 bayt**,
  oyunun gerçekten kullandığı **876 kod noktasının** alt kümesi

Alt küme Latin harfleri, rakamları ve para simgesini de taşıyor: dil Çince'yken
**bütün ağaç** bu yazı tipiyle çiziliyor, yoksa "12 ¤" boş kutu olurdu.

İki şey buradan çıktı:

**Noto Sans SC'nin web yapısında Latin Extended-A yok** (ğ İ ı Ş). Çince
tablosundaki Latin özel adlardan Türkçe işaretler çıkarıldı — kararın gerekçesi
`loc_zh.py`'nin başında yazıyor.

**İkinci yazı tipi = ikinci lisans metni.** Lisans ekranında "Rubik OFL var, o
da OFL" demek lisansı karşılamıyor: `licenses/noto-sans-sc-ofl` ayrı bir dosya
olarak eklendi, künye satırı ve `ATTRIBUTION.md` kütüğü güncellendi. Tur lisans
sayısını sayıyor (3 → 4).

---

## 5. Arapça: harfler birleşmezse metin çöp

Bu, işin **tek gerçek teknik engeliydi** ve baştan öyle yazılmıştı: *"Arapça için
hiçbir yerde RTL işlemesi yok ve ayrı bir metin ayarı da yok — bu ancak
çizdirerek anlaşılır."*

Arapça'da aynı harf sözcüğün başında, ortasında ve sonunda **başka bir şekil**
alır. Unity'nin **ölçünlü** metin üreticisi bunu yapmıyor: harfleri tek tek ve
soldan sağa diziyor. Çıkan şey Arapça değil, Arap harflerinden bir liste.

Çözüm Unity 6'nın **Gelişmiş Metin Üreticisi** (ATG): birleştirme, iki yönlü
sıralama, satır sonu. Üç yere dokunuldu:

1. **Proje ayarı** (`ProjectSettings/UIToolkitProjectSettings.asset`,
   `m_EnableAdvancedText: 1`). Bir onay kutusu — yani depoyu yeni klonlayan bir
   makinede kapalı olabilir. `AdvancedText.Set(true)` yapının parçası oldu;
   içerik senkronunun öğrettiği dersin aynısı, aynı kelimelerle.
2. **Kök öğe**: `UiRoot.ApplyLanguage()` yazı tipini, yazı yönünü ve metin
   üreticisini birlikte kuruyor — üçü de dilin özelliği. ATG **yalnızca
   Arapça'da** açılıyor: çalışan dört dili beşincisi için riske atmanın bir
   karşılığı yok.
3. **Dil düğmesi**: Arapça dil adı, arayüz başka dildeyken de birleşik
   yazılıyor. Yoksa Arapça okuyan biri, dilini aradığı düğmede dağılmış harfler
   görürdü.

### Ayarın kapalı olduğu hiçbir şeyi bozmuyor — bu yüzden ölçülüyor

Ayar kapalıyken metin çiziliyor, harfler görünüyor, bütün kontroller yeşil
kalıyor. Yalnızca Arapça okuyan biri yazının bozuk olduğunu görüyor. Bu projede
*"koşmayan bir kontrol, geçen bir kontrolün tıpatıp aynısına benziyor"* dersi
tam bu biçimde öğrenilmişti.

Birleştirmeyi soracak bir API yok. Ama birleştirmenin bıraktığı bir iz var:
**birleşen harfler sözcüğü kısaltır**, çünkü bağlantılı şekiller yalıtık
şekillerden dardır. Tur aynı Arapça sözcüğü iki kez yazıyor — biri ölçünlü, biri
gelişmiş üreticiyle — ve genişlikleri karşılaştırıyor:

```
tamam : Arapca harfler birlesiyor (yalitik 60,0 dp -> baglantili 40,0 dp)
```

%33 daralma. İki sayı eşit çıksaydı birleştirme olmamış olurdu.

---

## 6. Yarım çevrilmiş arayüz, hiç çevrilmemişten kötü

Metin doğru akıyordu ama **yerleşim akmıyordu**: onay kutusu metnin solunda,
birincil düğme sağ altta, gün rozeti sol üstte kalıyordu. Sağdan sola bir dilde
bunların hepsi ters taraftadır.

Aynalama **tek yerde** yapıldı — otuz iki satır kurulum yeri var ve birinin
unutulması, o satırın ters akması demekti:

- `Theme.RowFlow` → Arapça'da `FlexDirection.RowReverse`. `Theme.Row()` ve
  doğrudan satır kuran dokuz yer buradan soruyor.
- `Theme.SetGap()` aralığı `marginLeft` yerine `marginRight`'a veriyor.
  Verilmeseydi boşluklar bir kayardı: satırın sol ucunda fazladan bir boşluk,
  ilk iki öğe arasında hiç boşluk.
- `Icons.Play()` yatay aynalanıyor. **Yön bildiren tek simge bu** ("günü aç",
  "ertesi gün"); sağdan sola okuyan biri için ileri soldadır. Duraklat, kitap,
  tabak yönsüz — onlar çevrilmiyor.

Sayı çiftleri (`30,0 / 55`, `8.000 ¤`) Arapça'da görsel olarak ters duruyor. Bu
bir hata değil: Unicode iki yönlü algoritmasının doğru çıktısı ve sağdan sola
okuyan biri onları **doğru sırada** okuyor.

---

## 7. Şerit bütçesi artık beş dilde ölçülüyor

Eskiden iki dilde ölçülüyordu. Beş dile çıkınca *"Türkçe ve İngilizce'de sığıyor"*
artık bir şey kanıtlamıyor: şerit **en uzun metne** göre taşar ve o metnin hangi
dilde olduğunu bilmiyoruz.

| aşama | tr | en | es | zh | ar | bütçe |
|---|---:|---:|---:|---:|---:|---:|
| sabah | 154 | 154 | 154 | 159 | 154 | 220 |
| servis | 154 | 154 | 154 | 159 | 154 | 220 |
| akşam | 172 | 172 | **202** | 174 | 172 | 220 |

**En uzun dil İspanyolca** ve akşam şeridinde bütçenin 18 dp altında. Tahmin
edilemezdi; ölçüldü. Ölçüm `ApplyLanguage()` çağırıyor — yalnızca `Refresh()`
çağırmak, Çince'yi Rubik ile ölçmek yani boş kutuların genişliğini ölçmek
olurdu.

Beş dilin her birinde bir de görüntü alınıyor (`05-dil-*.png`). Boş kutu da bir
karakterdir, ters dizilmiş bir satır da bir satırdır — bazı şeyler ancak
bakılarak anlaşılıyor.

---

## Kapanmayanlar

- **Arapça metinler makine çevirisi değil ama anadili konuşuru da görmedi.**
  Aynısı Çince ve İspanyolca için de geçerli. Yayından önce her dil için bir
  okuma turu, ucuz ve değerli bir iş.
- **Mağaza sayfası metinleri hâlâ tek dilde.** Oyun beş dilde açılıyor, Play
  Console listesi açılmıyor.
