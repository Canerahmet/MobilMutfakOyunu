# 31 — Odalar ve iki kademeli kamera

10 Eylül 2026. Bu belge tek bir soruyu kapatıyor: **oyuncu neye dokunuyor?**

Soru tasarımdan değil ölçümden çıktı. [16-ekranlar-ve-ogretici.md](16-ekranlar-ve-ogretici.md) sonunda açık salon yerleşimi Unity'de kurulup gerçek telefon oranında render edildi ve masanın ekranda **15–20 dp** olduğu görüldü. Google'ın asgari dokunma hedefi 48 dp, Apple'ınki 44 pt. Yani "on dört masa ekrana sığıyor mu" sorusunun cevabı evetti ama soru yanlıştı: sığıyor, dokunulamıyor.

Ölçüm dört tur sürdü. Her turda render edilip **bakıldı**; sayıya güvenip bakmamak bu projede her seferinde yanlış çıktı.

---

## 1. Önce şu soru kapansın: masa hiç 48 dp olabilir mi?

Hayır, ve bunu ölçmeye gerek yok, aritmetik yetiyor.

Yatay 2400 piksellik bir telefon, yoğunluk 2,75 → **873 dp genişlik**. On dört masalık restoran en dar hâliyle 18 m eninde. Restoran kareyi kenar payı bırakmadan, perspektifsiz doldurduğunda bile:

```
873 dp × 0,86 m / 18 m = 41,7 dp
```

Ve bu **üst sınır**: perspektif payı, kenar payı ve derinlik ekseninin kısalması bu sayıyı ancak düşürür. Derinlik yönü 34 derecelik bakışta 0,56 kat kısalıyor, yani masanın dikey ekran boyu daha da küçük.

**Bütün restoranı gösteren hiçbir kamerada masa birincil dokunma hedefi olamaz.** Yerleşimi değiştirmek, kamerayı sıkılaştırmak, masayı büyütmek — hiçbiri 48 dp'ye ulaştırmıyor. Bu bir tasarım tercihi değil, ekranın ölçüsü.

---

## 2. Öneri: restoran modüler olsun

> "Restoran modüler yapıda olabilir yani odalar şeklinde çünkü sonuçta bulaşık yıkanılan yer, yemek yapılan mutfak gibi bölümler olması lazım. Restoran genişletildiğinde de sanki yeni bir oda eklenmiş gibi masalar gelir."

İki ayrı kazancı var ve ikisi de ölçülebilir:

1. **Anlam kazancı.** [14-personel-sistemi.md](14-personel-sistemi.md) zaten salonu iki havuza ayırıyor: aşçı havuzu ve salon havuzu (garson + bulaşıkçı + kasiyer). Oda, bu havuzun mekândaki karşılığı. Bulaşıkçıyı işe aldığında bir sayı değil **bulaşıkhane** doluyor.
2. **Dokunma kazancı.** Açık salonda "bütün restoran" ile "tek masa" arasında dokunulabilecek hiçbir nesne yok. Oda, tam olarak o boşluğu dolduran ara hedef.

## 3. Öneri: iki kademeli kamera

> "Tüm her şey aynı anda görüldüğü durumda oyuncu dokunarak kamerayı o modüler kısma yaklaştırmış olur."

Bu, §1'in kapattığı yoldan sonra kalan seçeneklerden birini seçiyor. Ölçüm doğruluyor — ama tek başına yetmiyor; §7'deki uzlaştırma gerekiyor.

---

## 4. Dört tur

| Tur | Yerleşim | Sonuç |
|---|---|---|
| 1 | Açık salon (`RestaurantScene.cs`) | Masa her kademede 15–20 dp. **Başarısız.** |
| 2 | Odalar tek sıra | Restoran 25 × 4,6 m bir koridora dönüştü; 20:9 karenin yarısı boş kaldı; kamera her kademede geri çekildiği için **dokunma hedefi büyümeyle küçülüyordu.** Oda fikri doğru, dizilim yanlış. |
| 3 | Odalar 2×2 eşit ızgara | Sayılar tuttu, **render yapay durdu.** Bütün odalar aynı ölçüde, bütün ayrım çizgileri hizalı. |
| 4 | **Farklı ölçüde dikdörtgenler** | Kabul edildi. |

Üçüncü turdaki kusuru sayı göstermiyordu, bakış gösterdi:

> "Restoran yerleşimi tamamen kare olmak zorunda değil, hatta şu anki görünüm biraz yapay duruyor, dikdörtgenlerden oluşabilir. Odaların boyutu birbirinden farklı olabilir."

## 5. Kabul edilen kat planı

Arsa **18,0 × 9,6 m**, sekiz oda, 172,8 m². Odalar arsayı boşluksuz kaplıyor ama ölçüleri farklı ve ayrım çizgileri hizalı değil: sol yarının yatay çizgisi z = 4,4, sağ yarının z = 5,0.

```
   0        5,2   8,4          13,4        18,0
9,6 +--------+-----+------------+-----------+
    |        |DEPO |            |           |
    | MUTFAK |3,2x |  SALON 2   |  SALON 4  |
    | 5,2x5,6| 4,2 |  5,0x5,2   |  4,6x4,6  |
5,4 |        +-----+            |           |
4,4 |        |     |            |           |
4,0 +--------+BULA-+------------+-----------+ 5,0
    | GİRİŞ  | ŞIK |            |           |
    | 5,2x4,0|3,2x |  SALON 1   |  SALON 3  |
    |        | 5,4 |  5,0x4,4   |  4,6x5,0  |
  0 +--------+-----+------------+-----------+

Depo mutfağın sağ kenarına yapışık: teslimat arkadan girer, depoya
iner, mutfağa çıkar. Bulaşık önde, çünkü kirli tabak salondan geliyor.
```

Servis odaları her zaman var. Salonlar kademeyle açılıyor: Salon 1 (4 masa) → Salon 2 (+3) → Salon 3 (+3) → Salon 4 (+4) = **4 / 7 / 10 / 14 masa**, [12-ekonomi.md](12-ekonomi.md)'nin kademe tablosuyla birebir.

**Arsa sabit, bina büyüyor.** Kamera hiçbir zaman geri çekilmiyor. Yapılmamış odalar çıplak zemin olarak duruyor ve onlara bakan kenarlar **alçak geçici duvar** alıyor — tam duvar denendi ve genişleme alanını tamamen gizledi, oyuncu nereye büyüyeceğini göremiyordu. Arsanın dış sınırındaki duvarlar tam yükseklikte.

İç bölmeler **0,85 m**. 2,6 m tam duvar denendi ve arka sıradaki sandalyelerin sırtını kesti; 34 derecelik bakışta tavana kadar duvar arkasını kapatıyor.

Duvarlar elle konmuyor, **odanın kenarından üretiliyor**: komşusu yapılmışsa alçak bölme (uzunsa ortasında geçit), yapılmamışsa geçici duvar, arsa sınırıysa tam duvar, kameraya bakan ön ve sağ kenarsa hiçbir şey. Bu yüzden oda ölçüleri değiştiğinde duvarları elden geçirmek gerekmiyor.

Kat planının arsayı boşluksuz kapladığı ve her odaya istenen masanın sığdığı **kodda kontrol ediliyor**. Bu kontrol ilk koşuşta bir hata yakaladı: `4,6 − 0,9 = 3,6999998` çıkıyor, `1,85`'e bölününce `1,99999` oluyor, tabanı alınca iki yerine bir sütun. Salon 3'e üç masa sığmıyordu ve render'da bu görülmeyebilirdi.

---

## 6. Ölçüm sonucu

Her sayı hedefin ekrandaki **iki ekseninin küçüğü**. Yalnızca yatay ölçmek hedefi olduğundan büyük gösteriyor.

> **Bu bölüm 11 Eylül 2026'da yeniden ölçüldü ve sayılar değişti.** Sebebi iki hataydı; ikisi de ölçüm aracındaydı, oyunda değil:
>
> 1. **Ölçüm kamerayı kendi eliyle kuruyordu.** `RoomLayout.Shoot` içinde `32f`, `Euler(34, -12, 0)` ve kapalı bir mesafe formülü yazılıydı. Oyun ise `CameraFit`'in ikili aramasını kullanıyordu ve o, en küçük mesafeyi buluyordu. Formül her terimde güvenli tarafa yanılıp **%30 fazla mesafe** veriyordu — yani ölçüm, oyunun gösterdiğinden küçük bir hedef bildiriyordu. `CameraFit` zaten tam bunun için yazılmıştı ve ölçüm aracı ona hiç bağlanmamıştı.
> 2. **Arayüz çubukları hesaba katılmıyordu.** Oyunda üst şerit ve eylem çubuğu ekranın ~%30'unu alıyor ve kamera kalan şeride sığdırıyor. Çubuksuz ölçmek hedefi olduğundan büyük gösteriyordu.
>
> İkisi düzeltilince eski kamera ayarının (32°, −12°) gerçek tabanı **48 dp** çıktı — Google'ın asgarisine tam tamına değiyor, payı yok. Aşağıdaki tablolar yeni ayarın (22°, 0°) sayıları.

### Genel görünüm — açık odalar karede, arayüz çubukları yerinde

| Oda | dp (20:9) | 48 dp |
|---|---|---|
| Bulaşık | 121 | ✓ |
| Salon 1 | 102 | ✓ |
| Mutfak | 99 | ✓ |
| Giriş | 94 | ✓ |
| Salon 2 | 91 | ✓ |
| **Depo** | **71** | ✓ |
| *masa* | *23* | ✗ |

**Taban 71 dp**, %48 payla. Masa geçmiyor ve §1'e göre hiç geçemez — genel görünümde dokunma hedefi oda.

Dar kare oranında (16:9) taban **81–89 dp**; yani en kötü durum 20:9 ve o da 71. Kapalı odalar tabloda yok: artık çizilmiyorlar, dokunulamıyorlar.

> **Yukarıdaki 71, sokak eklenmeden önceki ölçüm.** 12 Eylül 2026'da sokak
> genişletilirken yeniden ölçüldü ve arada iki basamak kaybedilmiş olduğu
> görüldü — kimse ölçmediği için. Kamera çerçevesi açık odalara **artı
> `CameraFit.StreetInFrame`** kadar sokağa yayılıyor ve o sayı sonradan
> eklendi:
>
> | sokak çerçevede | taban 20:9 | taban 16:9 | ne zaman |
> |---|---|---|---|
> | yok (0,00 m) | 71 dp | — | ilk ölçüm, sokaktan önce |
> | 1,10 m | ~64 dp | — | sokak eklendi, **yeniden ölçülmedi** |
> | **1,94 m** | **59 dp** | **74 dp** | iki yaya şeridi, ölçüldü |
>
> 1,10 → 1,94 genişlemesinin sebebi kaldırımda **iki yaya şeridi**
> gerekmesi: karşı yönde yürüyen figürler birbirinin içinden geçiyordu.
> Şerit aralığı figürün ölçülen en geniş gövde bandından geliyor (baş,
> 0,67 m — `PlacementAudit` PROFIL satırları). 59 dp, Google'ın 48 dp
> asgarisinin **%23 üstünde**.
>
> Ders 71'in yanlış olması değil, **doğruyken bırakılıp bir daha
> sorulmaması**. Çerçeveye bir metre ekleyen değişiklik ölçümü de
> çalıştırmalı.

### Oda görünümü — kamera bir salona yaklaşmış

| Hedef | dp | 48 dp |
|---|---|---|
| masa + sandalyeler | 96 | ✓ |
| masa tablası | 44 | ✗ |

**Dört kademede de aynı sayılar.** 4, 7, 10, 14 masa — hiç değişmiyor. Gerekçesi artık "arsa sabit" değil: kamera açık odaları çerçeveliyor, ama çerçeveyi bağlayan şey **en değil derinlik** ve mutfak bloğu arsanın bütün derinliğini birinci günden kaplıyor.

Dokunma bölgesi **masa tablası değil masa takımı**: masa artı iki sandalye, yerde 1,86 × 1,86 m. Masa aralığı da 1,85 m, yani bölgeler çakışmadan döşeniyor.

### Kamera açısı: −12° dönme neye mal oluyordu

Kullanıcı ekran görüntüsüne bakıp *"boş odalar yer kaplamasın, restoran tam ekran olan yerler gözüksün"* deyince kamera açıları ilk kez **ölçüldü**. O zamana kadar 32°/34°/−12° bir tercihtı, bir ölçüm sonucu değil.

| ayar | karenin ne kadarı restoran (açılış / büyümüş) | taban dokunma hedefi |
|---|---|---|
| 32°, −12° | %27 / %33 | 52 → **48 dp** |
| 32°, −6° | %34 / %39 | — |
| **22°, 0°** | **%48 / %43** | **71 → 71 dp** (sokaktan önce; bugün 59) |

Dönmenin sıfırlanması karenin dolgusunu %27'den %41'e çıkarıyor; görüş açısının 32'den 22'ye inmesi perspektifi düzleyip kalanını kazanıyor. Eğim 34'te kaldı — derinlik hissi oradan geliyor ve artırmak dolguyu **düşürüyor** (42°'de %18, 50°'de %17).

Kaybedilen şey o hafif "2,5D çevrilmişlik". Render'a bakınca duruyor: eğim ve mobilyaların yan yüzleri derinliği zaten veriyor, çevirme yalnızca kareyi yiyordu.

### Boş odalar artık çizilmiyor

Birinci kademede arsanın 172,8 m²'sinin yalnızca 102,6'sı açık; yani ekranın **%41'i** "henüz senin olmayan" koyu gri levhaydı. Levhaların rengi (0,16) bir zamanlar üç parlaklık ölçülerek dengelenmişti — sayılar doğruydu, **soru yanlıştı**.

Şimdi açılmamış oda hiç kurulmuyor: ne zemin, ne dokunma çarpışanı. Genişleme artık gerçekten bir *açılış* — oda yokken beliriyor. Kamera da (`CameraFit.OpenBounds`) yalnızca açık odaları çerçeveliyor.

### Gölge mesafesi: yanlış birimde bir "iyileştirme"

Aynı turda ikinci bir hata çıktı. Performans turu URP gölge mesafesini 25'ten **14 m**'ye indirmişti, gerekçe: *"arsa 18 × 9,6 m, yani her şey gölge haritasının içinde."*

Gerekçe yanlış birimdeydi. URP bu mesafeyi **kameradan** ölçüyor, sahne boyundan değil — ve kamera 26–35 m uzakta duruyor. Yani değer bütün düşen gölgeleri sessizce kapatmıştı. Hiçbir test yakalamadı; ekran görüntüsüne bakınca görüldü.

Yeni değer **45 m** = en kötü durumda (dar kare oranı, kalın çubuklar) sahnenin en uzak köşesi 39,4 m + pay. Gölge haritası 512 → **1024**: 45 m tek kademede 512 harita 18 cm/texel demek ve gölgeler tanınmaz oluyor.

Ayrıca `ProjectSetup.ConfigureUrp` bütün bu sayıların **ikinci bir kopyasını** tutuyordu ve performans turundan haberi yoktu — `ApplyAll` koşturan biri MSAA'yı 1'den 4'e, gölge mesafesini 45'ten 25'e geri alıyordu. İkisi eşitlendi ve `tools/check_urp.py` ayrışmayı bundan sonra yakalıyor (12. denetim).

---

## 7. Araştırmayla uzlaştırma

[30-mekan-yerlesimi.md](30-mekan-yerlesimi.md) yirmi altı oyunu tarayıp farklı bir sonuca vardı: **odalar sanat ve genişleme metaforu olarak benimsensin, etkileşim modeli olarak benimsenmesin.** Servis sırasında kamera sabit kalsın, birincil dokunma hedefi alt çubuktaki sabır çipleri olsun, oda çerçeveli kamera yalnızca yerleşim düzenleme ekranında yaşasın.

En sert itirazı sayısal ve ciddiye alınmalı:

> Kademe 4'te dört salon odası var. Bir tam tur = 3 oda değişimi. Oyuncu her dilimde bir kez restoranı taramak isterse **12 dokunuş** — servis aşamasının bütün bütçesinin %120'si, karşılığında sıfır karar.

**Bu hesap bir varsayıma dayanıyor: oyuncunun ne olup bittiğini görmek için odaları gezmesi gerektiği.** Ölçüm o varsayımı ortadan kaldırıyor.

Genel görünümde **oda 51–95 dp**. Yani rozet odanın üstünde durabilir ve dokunulabilir. Oyuncu bir odada ne olduğunu görmek için oraya gitmiyor; genel görünüm zaten taramanın kendisi.

### Uzlaştırılmış karar

| Kamera | Ne görünüyor | Birincil hedef | Gezinme dokunuşu |
|---|---|---|---|
| **Genel** (varsayılan, servis boyunca burada kalınabilir) | bütün restoran | **oda rozeti** (51–95 dp) | 0 |
| **Oda** (isteğe bağlı) | bir salon ve komşuları | **masa takımı** (65 dp) | eylem başına 1 |

**Yakınlaştırma zorunlu değil.** Oyun genel görünümden baştan sona oynanabiliyor: rozet odanın üstünde, dokunmak müdahaleyi açıyor. Odaya yaklaşmak bakmak için, mecburiyetten değil.

Bu, araştırmanın en sert kısıtını (sıfır zorunlu gezinme dokunuşu) kabul ederken kendi saydığı **en büyük bedeli ödemiyor**: salon dekora düşmüyor, çünkü dokunulan şey alt çubuktaki bir şerit değil, sahnenin içindeki oda. `research/01` §3'ün en sevilen mekanikler sıralamasında 2. ve 3. sıradaki "yerleşim tasarımı" ve "görünür büyüme" servis boyunca ekranda kalıyor.

Alt çubuk çipleri yine de yazılabilir ve `review/05`'in "sabır uyarısı üç kanaldan gelsin" maddesini karşılar — ama **ikinci kanal** olarak, birincil hedef olarak değil.

### Araştırmanın diğer dört maddesi kabul edildi

| # | Karar | Durum |
|---|---|---|
| 1 | Mekân koylardan kurulsun, kademe yeni bir salon koyu ekler | ✓ Kabul, uygulandı |
| 4 | Yerleşim düzenleme ekranı Kairosoft kalıbıyla: kareye dokun → hayalet ızgara → onay | ✓ Kabul, [16](16-ekranlar-ve-ogretici.md) ekran 11'e yazılacak |
| 5 | **Koylar mühürlü oda olmasın: kapı geçerliliği, koridor, yol bulma yok.** Garsonun yürüyüşü görsel | ✓ Kabul. [14-personel-sistemi.md](14-personel-sistemi.md) zaten "karmaşık yol bulma yok" diyor |
| — | Restaurant Renovation kaynak listesinden düşsün (eşleştirme bulmacası) | ✓ Kabul |

5. maddenin sayısal gerekçesi yeni kat planında da geçerli: mutfak merkezi (2,6; 6,8), en uzak salon merkezi (15,7; 7,3), arası **13,1 m**. 1,2 m/s'lik yürüyüşle tek yön 10.900 ms; [27-zaman-modeli.md](27-zaman-modeli.md) garsona müşteri başına 9.000 ms veriyor. Gerçek yürüyüş servis bütçesini aşıyor. Yeni plan eski şeride göre çok daha derli toplu (20,8 m → 13,1 m) ama **yine de yürüyüş simüle edilmemeli.**

### Araştırmanın açık bıraktıkları, kapananlar

| # | Soru | Durum |
|---|---|---|
| 1 | `RoomLayout.cs` konsol satırı dosyaya yazılsın; oradaki oda dp'leri hesaptı | ✓ Kapandı, §6'daki sayılar ölçüm |
| 2 | Kamera sığdırması düzeltilirse açık salon kaç dp'ye çıkar | ✓ Kapandı, §1: üst sınır 41,7 dp, 48'e hiç ulaşmıyor |
| 8 | Restaurant Renovation listeden düşsün | ✓ Kapandı |
| 3 | **Depo odasının işi ne** | Açık — ekipman şemaları yazılınca belli olacak |
| 4 | Kaç salon koyu çeşidi gerekiyor | Kapandı sayılabilir: dört salonun **dördü de farklı ölçüde**, kopyala-yapıştır okunmuyor |
| 5 | Kamera durumu kaydedilmiyor ([23](23-cekirdek-sozlesmesi.md)) | Açık — genel görünüm varsayılan olduğu için etkisi küçük |
| 6 | Çip çubuğu üst çubukla çatışıyor mu | Açık — çip artık ikinci kanal, baskı azaldı |
| 7 | İkinci şube odalı düzende ne demek | Açık, ilk sürüm dışı |

---

## 8. Bunun gerektirdiği üç şey

1. **Rozet odanın üstünde, masanın üstünde değil.** Masa genel görünümde 16 dp; oraya rozet koymanın anlamı yok. Odanın üstünde duran "3 müşteri bekliyor" rozeti hem görünür hem dokunulabilir.
2. **Yakınlaştırma kademeli, sürekli değil.** İki kademe: genel ve oda. Serbest zum (parmakla yakınlaştır) eklenirse ara kademelerde masa 16 ile 30 dp arasında kalıyor, yani dokunma o aralıkta çalışmıyor. Kademeli olunca dokunma kuralı her kademede belirli.
3. **Oda görünümü bir oda değil bir ZUM KADEMESİ.** 20:9 karede 5,0 × 4,4 m'lik bir odayı dikey olarak sığdırmak, yatayda zorunlu olarak ~12 metre göstermek demek. Oyuncu bir odaya yaklaştığında komşuların bir kısmını da görüyor. Bu iyi: bağlam kaybolmuyor.

## 9. Oda sözlüğü

| Oda | Ölçü | Alan | İçinde | Bağlı olduğu sistem |
|---|---|---|---|---|
| Mutfak | 5,2 × 5,6 m | 29,1 m² | ocak, tezgâh, davlumbaz | aşçı havuzu ([14](14-personel-sistemi.md)), istasyon yuvaları ([32](32-ekipman-ve-yeniden-denge.md)) |
| Giriş / kasa | 5,2 × 4,0 m | 20,8 m² | kasa, kapı | salon havuzu, kasiyer |
| Bulaşıkhane | 3,2 × 5,4 m | 17,3 m² | evye, raf | salon havuzu, bulaşıkçı |
| **Depo** | 3,2 × 4,2 m | **13,4 m²** | soğuk hava odası, kuru raf, sandık | **stok ve bozulma** ([32](32-ekipman-ve-yeniden-denge.md) §7) |
| Salon ×4 | 4,6–5,0 × 4,4–5,2 m | 20–26 m² | 3–4 masa takımı | masa kapasitesi ([12](12-ekonomi.md)) |

### Deponun işi bulundu

Bu belgenin ilk hâli şöyle diyordu: *"Depo boş ve bu bir risk. Arkasında simülasyon olmayan bir odaya sanat harcanmamalı; görünür bir iş verilemezse yerleşimden çıkarılmalı."*

**İş bulundu ve zaten oradaydı.** İçerikteki `spoilDays` alanı yazılmıştı ama simülasyon onu hiç okumuyordu: 77 malzemenin 44'ü bozulabilir, raf ömürleri 1 ile 45 gün arasında, ve hepsi her gece siliniyordu. Soğan da kıyma gibi bir gecede çöpe gidiyordu. Depo, o alanın var olma sebebi. Ayrıntı [32-ekipman-ve-yeniden-denge.md](32-ekipman-ve-yeniden-denge.md) §7.

**Depo mutfağa yapışık küçük bir arka oda.** Teslimat arkadan girer, depoya iner, mutfağa çıkar. Bulaşıkhane öne alındı, çünkü kirli tabak salondan geliyor ve bulaşıkçı salon havuzunda. Mutfaktaki buzdolabı kaldırıldı: soğuk saklama artık deponun işi ve orada görünür bir yükseltme merdiveni var; ikisini birden göstermek yalan olurdu.

**Ölçüm bir sınır çizdi.** Depo önce 3,2 × 3,8 m yapıldı ve genel görünümde **46 dp** ölçtü — asgari 48'in hemen altında. 4,2 m derinlikte **51 dp**. Yani "küçük oda" isteğinin bir tabanı var: 2,5B bakışta derinlik 0,56 kat kısaldığı için sığ oda dokunulamaz hâle geliyor. Depo bugün mutfağın yarısından küçük ama hâlâ dokunulabilir.

---

## Yerleşim: modeller birbirine giriyor mu

Kullanıcı 11 Eylül'de *"restorana yerleşen modeller üst üste binmiş gibi"* dedi. Göz kararı yetmiyor — 34 derecelik bir bakışta arkadaki bir nesne öndekinin üstüne biniyormuş gibi görünebilir, gerçekten binen ikisi de masum durabilir. `unity/Assets/Lokanta/Editor/PlacementAudit.cs` soruyu ölçüye çeviriyor: sahnedeki her nesnenin **gerçek pozdaki** kutusunu çıkarıp kesişen çiftleri yazıyor.

```powershell
tools\unity\run.ps1 -Method "Lokanta.EditorTools.PlacementAudit.Run"
```

**Neden BakeMesh:** `Renderer.bounds` derili bir mesh'te yalan söylüyor — Unity onu kök kemikten türetiyor ve poz değiştikçe güncellemiyor. İlk ölçümde **oturan** bir figür 1,68 m boyunda ve 1,66 m eninde göründü; ikisi de imkânsız.

**Araç iki kez yanlış ölçtü ve ikisi de kendi doğrulama satırıyla yakalandı.** `BakeMesh` mesh'i *kemiklere* göre deforme ediyor ve kemikler zaten ölçekli kökün altında duruyor — yani çıktının içinde ölçek **var**. `useScale` bayrağı yalnızca renderer'ın kendi ölçeğini ekliyor. Hem `true` + `localToWorldMatrix` hem `false` + `localToWorldMatrix` ölçeği iki kez uyguluyor. Doğrusu: bayrak kapalı ve dönüşümden ölçek çıkarılmış. Araç her koşuda ayakta bir figürün boyunu ölçüp `ArtPrefabs` hedefiyle karşılaştırıyor; tutmazsa sayılar çöp.

### Bulunanlar ve sonuç

| | önce | sonra |
|---|---|---|
| çakışan çift | **68** | **0** |
| figür × figür | 67 | 0 |
| figür × mobilya | 0,45 m | 0 |
| en büyük | 0,54 m | — |

- **Dört kişi bu masaya hiçbir makul ölçekte sığmıyor.** Hücre 1,85 × 1,70 m, masa çapı 0,88 m, oturan figürün ayak izi 0,90 × 1,01 m. Dört oturağı doldurmak için figürün eni ≤ 0,545 m olmalı — yani **0,66 m boyunda bir insan**. Hesap her ölçekte aynı çıkıyor. Ekranda en fazla **iki** misafir çiziliyor (`RestaurantView.VisibleGuests`); simülasyon etkilenmiyor, grup yine dört kişilik ve fişi de öyle.
- **Oturak yarıçapı 0,48 m**, iki kısıtın kesişimi: iki misafir birbirine girmemeli (2r ≥ 0,90) ve takım komşu takıma taşmamalı (r + en/2 ≤ 0,925).
- **Karakter 1,28 → 1,10 m.** İlk indirimin ölçütü "figür/sandalye **boy** oranı" idi ve yanlış soruyu soruyordu: bu paketin figürleri boylarından çok **enleriyle** büyük (kafa gövdenin üçte biri).

> **Bu üç madde ARTIK GEÇERSİZ — sayılar [35-canlandirma-ve-kamera.md](35-canlandirma-ve-kamera.md)'te güncellendi.** Masa altıgen (`tableRound`, çap 0,88) idi ve dört oturak 60°'lik kenarlarla hizalanamadığı için misafirler köşeye düşüyordu; artık **kare** (`Mobilya/table`, 0,82 m, kurulumda kareleniyor). Oturak yarıçapı **0,58**, karakter **0,95 m**, masa **0,55**, sandalye **0,68**, `SitLift` **0,26**. Dört kişinin sığmaması ve ekranda iki misafir çizilmesi kuralı aynı kalıyor.
- **Mutfakta buzdolabı ilk ocağın içindeydi** (0,30 m) — ikisi de arka sol köşedeydi. Buzdolabı arka sağa alındı, ocak sırası ona yer bırakıyor.
- **Personel birbirine giriyordu** (0,13 m): adım 0,95 m, figür eni 0,80 m. Adım 1,10, ikinci sıra 1,30 oldu. Ayrıca aşçılar `Z0 + 1,1`'de duruyordu ve ön tezgâhlar `Z0 + 0,55`'te — aşçı tezgâhın **içinde** duruyordu. Artık odanın orta şeridinde.

### Mobilya ile karakter ZIT yöne bakıyor

Kullanıcının ikinci cümlesi: *"Sandalyeler ters."* Doğruydu, ama sandalye yalnızca en görünen örneğiydi — **bütün mobilya 180° ters duruyordu**.

Ölçüldü (`Lokanta/Figur olcek goruntusu`, her nesne yaw 0'da, kırmızı küp +Z'de, mavi küp −Z'de, yedi mobilya tek karede):

| | yaw 0'da "ön" yönü |
|---|---|
| karakter | **+Z** (yüzü) |
| sandalye, ocak, tezgâh, lavabo, buzdolabı, raf | **−Z** (minder, fırın kapağı, kulp, musluk) |

Kod bu farkı bilmiyordu ve bir açı yazarken "karakter gibi" düşünüyordu. Sonuç: ocaklar duvara, tezgâhlar dışarıya, sandalyeler masaya **sırtını** dönüyordu; misafir sırtlığın içine gömülmüş görünüyordu.

Düzeltme tek sabitte: `RestaurantView.PropYaw = 180`. Çağrı yerlerindeki açılar **karakter kuralında** yazılıyor (0 = +Z'ye bak) ve sabit farkı kapatıyor — böylece her yeni eşyada ayrı bir 180 hatırlamak gerekmiyor.

**Kural:** yeni bir model eklerken yerel "ön" yönünü **varsayma, ölç**. İki modelin aynı yöne baktığını varsaymak bu hatanın kendisiydi.

Ayrı bir doğrulama görüntüsü de var — `Lokanta/Figur olcek goruntusu`: tek figür, tek sandalye, 1 m'lik ızgaranın üzerinde, **yandan**. "Oturuyor mu, yoksa sandalyenin önünde ayakta mı duruyor" sorusu dolu bir salonda ve tepeden bakışta cevaplanamıyor; bu görüntüde belirsizlik kalmıyor. Oturma pozunun gerçekten uygulandığı böyle doğrulandı — sorun poz değil ölçekti.

---

## Nasıl yeniden ölçülür

```powershell
tools\unity\shot.ps1 -Method "Lokanta.EditorTools.RoomLayout.Capture"
```

Çıktı `tools/art/out/unity/kat_*.png` ve günlükte `OLCUM` satırları. Bakılacak satır **TABAN**: en küçük *açık* odanın, arayüz çubukları yerindeyken ekranda kapladığı kısa kenar. Masa boyutu, oda ölçüsü, kamera açısı veya kademe yerleşimi değişirse **yeniden ölç ve bak**.

Kamera açılarını değiştiren `unity/Assets/Lokanta/Game/CameraFit.cs`'e dokunur; ölçüm aracı aynı sabitleri okuyor, yani ikisi ayrışamaz.
