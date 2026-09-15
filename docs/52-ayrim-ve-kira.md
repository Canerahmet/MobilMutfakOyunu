# 52 — Fast food'un kirası ve salonun yüzü

*15 Eylül 2026.* İstek iki parçalıydı:

> *"Fast food diğer mutfaklara göre biraz daha oynaması kolay olabilir ama
> aradaki fark çok da büyük olmasın, ona göre zorluğunu biraz artırabilirsin.
> Görünüm kısmına devam et."*

---

## 1. Zorluk: kirayı artırmak kasayı **artırıyor**

`rentMultiplierBp` içerikten geliyor (`cuisines/*.json`) ve simülasyon
kademeleri onunla yeniden kuruyor. Süpürüldü — `makul` botu, Türk 17.351'e
karşı:

| kira çarpanı | fast food kasa | fark |
|---|---:|---:|
| yok | 22.473 | +%29,5 |
| **×1,15** | **22.263** | **+%28,3** |
| ×1,25 | 23.493 | +%35,4 |

Sezgi "kira artarsa kasa düşer" diyor ve **ölçüm tersini gösteriyor.** Sebep
hacim çarpanında görülenin aynısı ([51](51-self-servis.md) §5): bot maliyete
**genişlemeyerek** cevap veriyor, genişlememek zaten daha kârlı, dolayısıyla
her masraf kolu onu daha yalın ve daha zengin bir dükkâna itiyor.

Bunun doğrudan sonucu şu: **bitiş kasası bu bot için bir zorluk ölçüsü
değil.** Ölçü, iki mutfak **arasındaki fark** — ve o, en dar 11500'de.

İyi oynayan botta (`planci`) fark zaten +%13. Kalan büyük farklar kiradan
değil Türk'ün iki zayıflığından geliyor (zayiat 14.121'e 5.487; veresiye net
negatif) ve onlar ayrı bir iş — kirayla kapatılacak şeyler değil.

---

## 2. Görüntü aracı oyunun gösterdiğini göstermiyordu

Self servis tezgâhı — menü panelleri, kasalar, içecek makinesi — bir önceki
oturumda yazılmıştı. Kareye bakınca **hiçbiri yoktu.**

Sebep: bayrak zincirin yanlış ucundan okunuyordu.

```
CuisineId   : App.Content.Cuisine  ->  PreviewCuisine  ->  "fastfood"
SelfServis  : App.Content.SelfService                      (arac: null)
```

Editör aracında `App` yok, dolayısıyla self servis **her zaman kapalıydı**.
Palet `PreviewCuisine` üzerinden geçiyordu, yani araç "fast food"u doğru
renkte ama **yanlış yapıda** çiziyordu.

Çare `"fastfood ise self servis"` yazmak değil — o bilgi içeriğin, görünümün
değil. `GameShot` zaten `ContentSet`i kuruyordu; onu `PreviewContent` olarak
görünüme veriyor ve `SelfServis` aynı zinciri izliyor.

*Araç oyunun göstereceğini göstermezse, "eklendi" demek bir ölçüm değil bir
tahmindir.* Bu kez tahmin yanlıştı.

---

## 3. Asılı menü panelleri: üçüncü kez aynı ders

Panelleri tezgâhın **üstüne** (y = 2,00) asmıştım. Kare açılınca ekranda
aşçıların önünde duran dev, boş, parlayan levhalar vardı.

Bu, bu projede **üçüncü** kez aynı şey: tavansız bir binaya 34 dereceden
bakarken **asılan her şey arkasını kapatır.** Kullanıcı bunu sarkıt lambalar
için iki kez söylemişti ("tavandaki ışıklar gözükmesin").

Çözüm paneli kaldırmak değil **yerini değiştirmek** oldu: menü panosu artık
**mutfağın arka duvarında**. Bu kamerada arka duvar tezgâhın tam üstünde
duruyor — yani oyuncu zaten "tezgâhın üstündeki menü" diye okuyor, ama
hiçbir şeyi kapatmıyor.

Pano da boş bir levha değil: koyu yüz, ışıklı satırlar, sağda fiyat sütunu —
salonun menü tahtasıyla aynı dil.

---

## 4. Salonun en büyük yüzeyi ayrışmamıştı

İki salonu yan yana koyunca (`render/salon_*_oda.png`) zemin ve duvar
ayrışmıştı ama **masalar birebir aynıydı** — ikisi de aynı kahverengi ahşap.
Salonun en çok yer kaplayan yüzeyi masa tablası, yani ayrımın yarısı hâlâ
eksikti.

| | mobilya |
|---|---|
| Türk | kahverengi ahşap (değişmedi) |
| fast food | **açık laminat** `(0.686, 0.612, 0.510)` |

Koyu zemin + açık tabla + kırmızı minder, kullanıcının getirdiği üç karenin
de düzeni. Tek bir palet satırı; yeni varlık, indirilen doku, atıf defterine
eklenen bir şey yok.

---

## 5. Tepsi bırakma istasyonu: self servisin görünen sonu

Temizlikçi masadaki tepsileri topluyor ([51](51-self-servis.md)) — ama
**topladıktan sonra nereye** götürdüğü salonda yoktu. Giriş odasının sol
duvarına, bel hizasında bir dolap: üstünde tepsi yığını, önünde koyu bir
ağız, yanında ışıklı küçük bir levha.

**Sıra bandı eklenmedi** ve bu bilinçli: referansta tezgâhın önünde bant var
ama bu simülasyonda kimse tezgâhta sıraya girmiyor — müşteri kapıdan masaya
yürüyor. Boş bir sıra bandı, olmayan bir mekaniği vaat ederdi.

---

## 6. Tezgâh ikiye bölündü

Kasalar tezgâhın üzerindeydi ve **boydan boya cam siper onları örtüyordu**:
karede geriye tezgâhın üzerinde iki beyaz leke kalmıştı. Siper `w`'den
`0,60·w`'ye indi; kasalar uçlara, siperin dışına çıktı ve kendi ekranlarını
aldı.

Gerçek bir hızlı yemek tezgâhı da böyle bölünür: **ortada sıcak hat, uçlarda
kasa.**

---

## 7. Türkçe tur kırmızı yandı — ve sebebi bir önceki işlemeydi

Görsel iş bittikten sonra Türkçe tur koştu ve kaldı:

```
TANI canlilik penceresi: 37,7 sn, servis %30 -> %61
HATA: Mutfakta is yapiliyor (0 kisi; simulasyon is verdi 156 kez)
```

**Aynı yapı ikinci koşuda geçti** (139/0). Yani hem kırmızı hem yeşil
ölçümün değil **örnekleme şansının** sonucuydu.

Kök sebep bu oturumda değil: [51](51-self-servis.md) ile Türk mutfağının
zirvesi **1. dilimden 2. dilime** taşınmıştı
(`[1200,4800,2500,1500]` → `[1200,2800,4500,1500]`) ve **Türkçe tur o
işlemeden sonra hiç koşturulmamıştı.** Canlılık penceresi 40 saniyelik gerçek
zaman bütçesiyle günün ancak %61'ine yetişiyor; zirve artık %50–75'te.
Tek aşçı da zamanının çoğunu istasyona **yürüyerek** geçiriyor — simülasyon
görev veriyor, duruş `Walk`. Pencere o dar aralığa denk gelirse yeşil,
gelmezse kırmızı.

Çare pencereyi herkes için uzatmak **değil**: o, `%80'e kadar koşup zirveyi
yiyen` eski davranışı geri getirirdi ve o davranış bir kez düzeltilmişti.
Uzatma artık **koşullu** — yalnızca ölçülecek şey henüz görülmediyse *ve*
simülasyon gerçekten iş veriyorsa 75 saniyeye kadar bakmaya devam ediyor.
Kontrol sağlandıysa pencere eskisi gibi 40 saniyede kapanıyor, yani sonraki
kontroller zirveyi aynen buluyor.

Ölçüldü — iki koşu, ikisi de yeşil:

| | sonuç |
|---|---|
| Türk koşu 1 | **141 geçti, 0 kaldı, 0 ölçülemedi** |
| Türk koşu 2 | 139 geçti, 0 kaldı, 2 ölçülemedi — pencere **11,4 sn** (%30 → %39) |
| fast food | 133 geçti, 0 kaldı, 1 ölçülemedi — pencere **tam 40,0 sn** |

Sonra beş Türkçe koşu daha: hepsi yeşil, pencereler 11,4 / 19,9 / 28,3 / 32,4
saniye — yani **uzatma bir kez bile ateşlenmedi.** Fast food penceresi de tam
40,0 saniyede kapandı. Normal yol değişmemiş.

### Ateşlenmeyen bir dalı "düzeldi" saymak

Beş yeşil koşu uzatmanın *çalıştığını* göstermiyor, yalnızca *gerekmediğini*.
Bu proje aynı tuzağı defalarca yazdı, o yüzden dal **mutasyonla** ölçüldü:
koşul geçici olarak `true` yapıldı, yani uzatma her koşuda serbest.

```
mutasyon: TANI canlilik penceresi 26,6 sn  ->  ozet: 140 gecti, 0 kaldi
```

Pencere yine 40 saniyenin altında kapandı. Sebep aydınlatıcı: döngü normalde
zaman kapağıyla değil **koşullu `break`** ile bitiyor. Yani uzatma, göreceğini
gören bir koşuyu uzatamıyor — zirveyi yeme riski yok, ve bu artık bir akıl
yürütme değil ölçüm.

Geriye dürüst kalan şey şu: dalın **kendisi** hâlâ ateşlenmiş değil, çünkü
ateşlendiği durum altı koşuda bir görülüyor. Aradığı şey ve ateşlenme koşulu
o tek koşudan birebir alındı; ama bir kez daha kırmızı yanarsa bakılacak ilk
yer burası olmalı.

*Zaman bütçesi bir emniyet kapağıdır; ölçümün tanımı olduğu an ölçüm
kaybolur.*
