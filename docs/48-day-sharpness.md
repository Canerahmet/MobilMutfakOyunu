# 48 — Günün sivriliği: baskının neden hiç ateşlenmediği

*14 Eylül 2026.* Turun her koşusunda aynı üç satır çıkıyordu:

```
OLCULEMEDI: Salona cay (0 bekleyen masa)
OLCULEMEDI: Masadan kizgin ayrilan varsa kriz seridi (0 kizgin)
OLCULEMEDI: Kritik masa goruldugunde serit kurulmus (0 kritik masa)
```

Üçü de **baskı** ölçüyor ve üçü de ölçülemiyordu. Mağaza metni ise
"SERVİS SIRASINDA SEN VARSIN" diyor ([44](44-store-texts.md)) — müdahale,
çay, kriz şeridi ve imza mekanikleri hep bu baskının üstüne kurulu.

---

## 1. Kök sebep: gün neredeyse dümdüzdü

Geliş **payları** arketiplerde, dilim **süreleri** mutfakta duruyor. Bir
dilimin yoğunluğu ikisinin oranı — ve trafiğe göre ağırlıklandırılınca:

| | öğle payı | öğle süresi | yoğunluk |
|---|---:|---:|---:|
| fastfood | %37,6 | %30 | **1,25×** |
| turk | %59,3 | %48 | **1,24×** |

Günün en yoğun anı ortalamanın yalnızca çeyrek katı üstünde. Kadro ise
günlük **toplam** işe göre kuruluyor (`StaffingModel.Required`) — yani
1,25×'lik bir tepe rahatça soğuruluyor. Kuyruk oluşmuyor, sabır tükenmiyor,
kimse kızmıyor.

**Türk mutfağında içerik kendi tanımıyla çelişiyordu.** `ui.cuisine.turk_desc`
"Sert öğle zirvesi" diyor; ama öğle, günün %48'ini kaplayan **en uzun**
dilimdi. Vaat edilen zirve, yarım güne yayılmış bir düzlüktü.

---

## 2. Süre değişti, ağırlık değil

Bu ayrım kasıtlı: geliş payları sabit kaldığı için **günlük müşteri toplamı
değişmedi**, aynı müşteriler yalnızca daha dar bir pencereye sığıştı.

Önemi şurada: kira ve marj kalibrasyonu ([solve.py](../tools/balance/solve.py))
günlük toplam üzerinden çözülüyor. Ağırlıkları değiştirseydim bütün ekonomiyi
yeniden çözmek gerekirdi; süreyle oynamak kalibrasyonu geçerli bıraktı.

Ayrıca geliş payları arketiplerin **kimliği** — "esnaf öğle gelir, öğrenci
akşam". Onları toptan değiştirmek yirmi arketipin karakterini düzlerdi.

| | yeni süreler (bp) | yeni yoğunluklar |
|---|---|---|
| fastfood | 2500 / 1800 / 3500 / 2200 | 0,52 / **2,09** / 0,46 / **1,50** |
| turk | 1200 / 2800 / 4500 / 1500 | 0,88 / **2,12** / 0,40 / 0,82 |

İki mutfak artık **ritim olarak da** ayrışıyor: fast food gün içinde iki kez
kalabalıklaşıyor (öğle ve akşam), Türk öğle patlayıp öğleden sonra sakinliyor.
Mutfak kimliği yalnızca menüde değil, günün şeklinde de.

---

## 3. Oyun değişti — ölçüldü

Harness, Türk mutfağı, 8 tohum × 60 gün:

| strateji | önce | sonra |
|---|---:|---:|
| `baskili` — eksik kadro, pasif | **22.888** | 13.314 |
| `baskili_mudahale` — eksik kadro, **oynayan** | 23.393 | **18.267** |
| `makul` — tam kadro | 21.968 | 17.351 |

İki şey birden düzeldi:

**Müdahalenin değeri +505'ten +4.953'e çıktı.** Mekanik artık bir dekor değil.

**En kazançlı oyun değişti.** Önce "eksik kadro kur, hiçbir şey yapma" en
iyisiydi (22.888) — yani oyunun sattığı şeyin tam tersi ödüllendiriliyordu.
Şimdi eksik kadroyla çalışıp **aktif oynayan** (18.267) hem pasif eksik
kadroyu hem tam kadroyu geçiyor.

Kimse batmıyor: `makul` 17.351 / itibar 75, `planci` 23.375 / itibar 94,8.
Oyun zorlaştı, adaletsizleşmedi.

---

## 4. Hipotezim çürüdü: kombo ayrı bir konu

Kombonun aynı kökten olduğunu düşünüyordum — "darboğaz mutfakta değil
salonda". Sivriltmeden sonra ölçtüm ve **yanlıştı**: `imzaci` 19.268,
`zirvede_kapat` 19.048, servis 1919'a 1923. Hâlâ aynı oyun.

Ama aynı ölçüm daha değerli bir şey söyledi: fastfood'da `makul` 17.909,
`imzaci` 19.268 — yani **kombo +1.359 değerinde ve mekanik çalışıyor.**

Sorun mekanikte değil, [45](45-design-review.md) §18'in koruduğu
**varsayımda**: "zirvede kapatmak meşru bir oyun ve eksen onu
cezalandırmamalı". Ölçüm iki kez, iki farklı gün şeklinde, o oyunun
**var olmadığını** söyledi. Korunacak bir alternatif yok.

*Bir eksen, var olmayan bir stratejiyi korumak için düz bırakılmış.*

---

## 5. Kırılan test haklıydı

`Hafta_sonu_hafta_icinden_kalabalik` kırıldı: hafta içi 6, hafta sonu 6.

Sebep meşruydu. Test aç bir lokantayı koşuyordu (tek aşçı, stok
tazelenmiyor); gün sivrilince bekleme uzadı, memnuniyet düştü ve itibar yedi
günde **33'ten 11'e** indi. Düşen talep hafta sonu çarpanını yuttu — 6. gün
9 grup, 7. gün 4.

Yani test "hafta sonu kalabalık mı" diye sorarken aslında "itibar spirali
çarpandan hızlı mı" diye soruyordu. Stok tazelenince iddia ettiği şeyi
ölçüyor: itibar 33 → 50, hafta içi 7, hafta sonu 10.

---

## 6. Tur da yeni ritme göre ayarlandı

Tur ilk koşuda iki kontrol düşürdü ve sebebi öğreticiydi:

```
OLCULEMEDI: Servis sirasinda dolu masa bulunamadi (servis %70, bugun 12 kisi)
HATA: Lavaboda yikayan goruldu (0 kare)
```

Turun içinde bir çelişki vardı: canlılık penceresi servisin **%80'ine** kadar
koşabiliyordu ama masa avı **%70'ten** sonra pes ediyordu. Gün düz olduğu
sürece görünmedi — masalar gün boyu dolu kaldığı için sıra önemsizdi. Gün
sivrilince pencere zirveyi yiyip masa avını boş salona bıraktı.

Pencere artık günün yarısında bırakıyor (gerçek zaman bütçesi aynı, yalnızca
günün neresinde durduğu değişti).

*Bir aracın varsayımı, ölçtüğü şey değişene kadar görünmüyor.*

### İki baskı kontrolü ilk kez ateşlendi

Ayar sonrası tur 134 geçti, 0 kaldı — ve aylardır ölçülemeyenlerden **ikisi**
artık koşuyor:

```
tamam : Salona cay (1 bekleyen masa)      onceden "0 bekleyen masa"
tamam : Mudahale hakki dustu              onceden hic olculemiyordu
TANI  : salon dolma beklemesi 0,6 sn, servis %14, dolu masa 2
```

Salon servisin **%14'ünde** doluyor. Çay düğmesi ilk kez gönderilecek birini
buldu, müdahale hakkı ilk kez harcandı.

### Kriz şeridi: ölçümün kendisi yanlış yerdeydi

Kalan iki kontrol ("kızgın müşteri", "kritik masa") hâlâ ateşlenmiyordu ve
sebebi gün şekli değildi: **ölçüm 1. günün teftiş bloğundaydı** — dört masa,
on iki kişi. Kriz orada yapısal olarak imkânsız. Yani kontrol koşuyor gibi
görünüp altmış gün boyunca hiçbir şey ölçmüyordu.

İki değişiklik: ölçüm kampanyanın tamamına taşındı, ve tur 20-21. günlerde
(hafta sonu) **bilerek bir garson eksik** çalışıyor — ki bu aynı zamanda
oyunun ödüllendirdiği oyun ([47](47-recognition-and-report-card.md)'nin "Zirveyi eksik
kadroyla geçtin" nişanı tam da bunu tanıyor).

Toplamı ilk yazdığımda sabaha koymuştum ve `AdvanceToNextDay` sayacı yeni
sıfırladığı için toplam hep sıfır kalıyordu — kontrol koşar, hiçbir şey
ölçmezdi. Aynı sınıf hata, aynı gün, üçüncü kez.

Sonuç, kriz şeridinin **ilk doğrulaması**:

```
TANI kampanya krizi: 30 masadan kizgin, serit 186 kez
tamam : Kampanyada kizgin olunca kriz seridi kuruldu (30 kizgin, serit 186 kez)
```

Birinci gündeki iki eski kontrol **silindi**. Hiçbir zaman koşamayan bir
kontrol, hiç olmayan bir kontrolden kötüdür: ikincisi eksik olduğunu söyler,
birincisi kapsama yanılsaması üretir — nitekim aylarca üretti.

---

## 7. Bu turda aynı hata sınıfı üç kez

1. `link.xml`'i akıl yürütmeyle doğru sayma ([46](46-shipped-binary.md))
2. `badge.` ailesini yalnızca üretece ekleyip teste eklememe
   ([47](47-recognition-and-report-card.md))
3. Kriz ölçümünü hiç koşamayacağı yere koyma (bu belge)

Üçü de aynı cümleyi doğruluyor: **koşmayan bir kontrol, geçen bir kontrolle
dışarıdan aynı görünüyor.** Üçünün de çaresi aynı oldu — kontrolü kırılmaya
zorlamak: `link.xml`'i kaldırıp turu düşürmek, aileyi silip üretimi
reddettirmek, kadroyu bilerek eksiltip krizi yaratmak.
