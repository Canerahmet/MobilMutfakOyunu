# Veri Şemaları

**Son güncelleme:** 9 Eylül 2026
**Kütük maddesi:** B3
**Durum:** Yazıldı, karar bekliyor

---

## İlke

İçerik **JSON dosyalarında** yaşar, kodda değil. Mimari kararı buydu ve sebebi şu: konsolda çalışan denge aracı ile Unity aynı dosyaları okur. ScriptableObject kullanılsaydı denge aracı onları okuyamazdı.

**Metin gömülmez.** Her görünen isim bir yerelleştirme anahtarı taşır, metnin kendisi ayrı dosyalarda durur. Yerelleştirme kararı bunu gerektiriyor.

**Kimlikler değişmez.** Bir `id` yayınlandıktan sonra asla değiştirilmez, çünkü kayıt dosyaları ona referans verir.

---

## Dosya düzeni

```
content/
  economy.json            genel sabitler
  ingredients.json        malzemeler
  dishes/
    fastfood.json
    turk.json
  archetypes/
    shared.json
    fastfood.json
    turk.json
  staff-roles.json
  staff-traits.json
  equipment.json
  upgrades.json
  expansions.json
  regulars/
    fastfood.json
    turk.json
  cuisines.json
localization/
  tr.json
  en.json
```

---

## Şemalar

### economy.json

Oyunun bütün genel sabitleri tek dosyada. Denge aracı en çok bu dosyayı değiştirecek.

```json
{
  "startingCash": 8000,
  "startingReputation": 30,
  "campaignDays": 60,
  "seasonDays": 15,
  "rentDayInterval": 7,
  "reputationDecayPerDay": 0.3,
  "satisfactionNeutral": 60,
  "customerBasePerTable": 4,
  "weekendMultiplier": 1.25,
  "serviceSeconds": 120,
  "interventionsPerDay": 4,
  "loanMultiplier": 1.35,
  "loanWeeks": 8,
  "loanOptions": [5000, 10000, 20000],
  "priceVolatility": 0.25,
  "underpriceFloor": 0.85
}
```

### ingredients.json

```json
{
  "id": "kiyma",
  "nameKey": "ingredient.kiyma",
  "shared": false,
  "cuisines": ["turk", "fastfood"],
  "basePrice": 42,
  "unit": "kg",
  "perishable": true,
  "spoilDays": 1,
  "seasonModifier": { "ilkbahar": 1.0, "yaz": 1.05, "sonbahar": 0.95, "kis": 1.15 },
  "qualityPriceMultiplier": { "dusuk": 0.75, "standart": 1.0, "yuksek": 1.35 },
  "qualitySatisfaction": { "dusuk": -15, "standart": 0, "yuksek": 10 }
}
```

`shared: true` olan malzemeler temel kilere aittir ve her mutfakta bulunur.

### dishes/*.json

```json
{
  "id": "kuru_fasulye",
  "cuisine": "turk",
  "nameKey": "dish.kuru_fasulye",
  "group": "sulu",
  "unlockDay": 1,
  "marketPrice": 55,
  "recipe": [
    { "ingredient": "fasulye_tane", "amount": 0.15 },
    { "ingredient": "sogan", "amount": 0.05 },
    { "ingredient": "salca", "amount": 0.03 }
  ],
  "prepSeconds": 4.5,
  "station": "ocak",
  "batchSize": 12,
  "tags": ["gunun_yemegi_adayi", "vejetaryen"]
}
```

**`marketPrice`** piyasa referansı. Oyuncunun koyduğu fiyat bununla karşılaştırılıp fiyat cezası hesaplanır.

**`batchSize`** tencere yemekleri için. Bir kez pişirilir, o kadar porsiyon çıkar. Fast food'da bu değer 1.

**`station`** hangi ekipmanı işgal ettiği. Mutfak darboğazı buradan doğar.

### archetypes/*.json

```json
{
  "id": "esnaf_komsu",
  "cuisine": "turk",
  "shared": false,
  "nameKey": "archetype.esnaf_komsu",
  "tier": "sik",
  "patienceSeconds": 22,
  "spendTendency": 1.0,
  "priceSensitivity": 0.9,
  "partySize": { "min": 1, "max": 2 },
  "arrivalWeights": { "acilis": 0.10, "ogle": 0.70, "ogleden_sonra": 0.15, "aksam": 0.05 },
  "orderPreference": { "sulu": 0.6, "pilav": 0.25, "corba": 0.15 },
  "reputationWeight": 1.4,
  "regularChance": 0.35,
  "wardrobeTags": ["onluk", "esnaf"]
}
```

`tier` değeri `sik`, `orta` veya `nadir`. Trafik payları economy.json'da değil, sıklık kademesinden türetilir.

`wardrobeTags` giydirme sistemine hangi kıyafet alt kümesinin çekileceğini söyler. Davranış ve görünüm bağını kuran alan bu.

### staff-roles.json

```json
{
  "id": "asci",
  "nameKey": "role.asci",
  "dailyWage": 140,
  "stations": ["ocak", "izgara", "firin"],
  "baseSpeed": 1.0
}
```

### staff-traits.json

```json
{
  "id": "hizli_ama_dagilnik",
  "nameKey": "trait.hizli_ama_dagilnik",
  "effects": { "speed": 0.18, "cleanliness": -0.15 },
  "conflictsWith": ["yavas_ama_titiz"]
}
```

### equipment.json ve upgrades.json

```json
{
  "id": "ikinci_ocak",
  "cuisine": "turk",
  "nameKey": "equipment.ikinci_ocak",
  "cost": 3200,
  "station": "ocak",
  "capacityBonus": 1,
  "unlockDay": 18
}
```

```json
{
  "id": "tabela",
  "shared": true,
  "nameKey": "upgrade.tabela",
  "cost": 1500,
  "effects": { "customerBaseBonus": 0.12 },
  "unlockDay": 12
}
```

### expansions.json

```json
{
  "tier": 2,
  "tables": 7,
  "cost": 2500,
  "weeklyRent": 3400,
  "unlockSeason": 2
}
```

### regulars/*.json

```json
{
  "id": "hasan_usta",
  "cuisine": "turk",
  "nameKey": "regular.hasan_usta.name",
  "jobKey": "regular.hasan_usta.job",
  "archetypeBase": "esnaf_komsu",
  "favouriteDish": "kuru_fasulye",
  "arrivesFromDay": 3,
  "veresiyeEligible": true,
  "story": [
    { "beat": 1, "requiresVisits": 3, "requiresSatisfaction": 70, "textKey": "regular.hasan_usta.beat1" },
    { "beat": 2, "requiresVisits": 8, "requiresSatisfaction": 75, "textKey": "regular.hasan_usta.beat2" },
    { "beat": 3, "requiresVisits": 15, "requiresSatisfaction": 80, "textKey": "regular.hasan_usta.beat3" }
  ]
}
```

Düzenli müşteri bir arketipi taban alır ve üstüne kendi özelliklerini ekler. Böylece davranış kodu tek bir yol izler.

### cuisines.json

```json
{
  "id": "turk",
  "nameKey": "cuisine.turk",
  "free": false,
  "signatureMechanic": "veresiye",
  "perishableRatio": 0.57,
  "dailySpecial": true,
  "teaService": true,
  "scoreAxis": "veresiye_tahsilat",
  "hourSplit": { "acilis": 0.10, "ogle": 0.60, "ogleden_sonra": 0.20, "aksam": 0.10 },
  "palette": "turk",
  "wardrobeSet": "turk"
}
```

### `perishableRatio` bir GIRDI degil, bir OLCUM

Simulasyon bu alani okumuyor; `ingredients.json`'daki `perishable`
alanlarindan sayiliyor (`tools/audit_content.py`). Semada durmasinin
sebebi bir **tasarim vaadini** gorunur tutmak:

| mutfak | bozulabilir / toplam | oran |
|---|---:|---:|
| fastfood | 20 / 48 | **0.42** |
| turk | 30 / 53 | **0.57** |

Bu satirlar bir zamanlar 0,20 ve 0,60 yaziyordu ve **ikisi de yanlisti**:
olculdugunde iki mutfak da 0,58 cikti. Yani iki mutfagin en somut
oynanis farki oldugu soylenen sey, hic var olmamisti.

Fark simdi gercek ve bir KURALDAN geliyor: fast food'a ozel bir malzeme
gercek bir lokantada dondurulmus ya da kavanozda geliyorsa bozulmuyor
(kanat, fileto, sosis, mozzarella, dondurma karisimi, jalapeno, tursu,
vejetaryen kofte). Taze kalan yalnizca burgerin **ustune** konanlar:
ekmek, marul, lahana, elma.

Sonuc oyunda su: **fast food affediyor, Turk mutfagi affetmiyor.** Yanlis
hesaplanmis bir gunu fast food'da atlatirsin; Turk mutfaginda odersin, ve
soguk hava deposu orada cok daha erken bir zorunluluk.

Uzun raf omurlu kiler malzemeleri (sogan 20 gun, sarimsak 30, patates 25)
bilerek **bozulabilir birakildi**. Sogutma yokken her seyin gece olmesi
bir kaza degil, [12-economy.md](12-economy.md) 3'un koydugu taban - ve
soguk hava merdiveninin satin aldigi sey tam olarak o tabandan cikmak.
Onlari bozulmaz yapmak absurtlugu degil, yukseltmeyi kaldirirdi.

---

## Doğrulama

Çekirdek açılışta bütün içeriği doğrular ve hata varsa **çalışmayı reddeder.** Sessizce devam etmek en pahalı hata türüdür.

Kontrol edilenler:

1. Her `id` benzersiz mi
2. Her referans var olan bir kayda mı işaret ediyor
3. Her `nameKey` yerelleştirme dosyalarında var mı, hem TR hem EN
4. Tarif malzemeleri o mutfakta mevcut mu
5. `arrivalWeights` ve `orderPreference` toplamları 1,0 mı
6. `unlockDay` değerleri kampanya süresini aşıyor mu
7. Her mutfakta yeterli yemek var mı, açılış menüsünü doldurabiliyor mu

Bu doğrulayıcı denge aracının da ilk adımı olacak.

---

## Karar bekleyen ayrıntılar

1. Malzeme miktarları kilogram mı porsiyon mu olsun
2. Kalite kademesi üç mü kalmalı
3. Mevsim isimleri gerçek mevsimler mi olsun, yoksa sadece numara mı

---

## Parti B eklemeleri

[23-core-contract.md](23-core-contract.md) §8 bu şemalara dört ekleme getiriyor ve bağlayıcı:

1. `staff-roles.json` içine `pool` ve `capacityPerDay`; `economy.json` içine `staffing` bloğu (patron iş gücü, kademeler, tavanlar)
2. `cuisines.json` içine mutfak başına `signature` bloğu: combo, credit, courses, broth
3. `dishes/*.json` içine dört zorunlu parametre: `prepMs`, `station`, `complexity`, `ingredients[].grams`; artı görünüm için `plating`
4. **Bütün sayılar tamsayı:** santi-sikke, baz puan, milisaniye. Doğrulayıcı ondalık görürse dosyayı reddeder

Bu dosyadaki örnek şemalar Faz 0'da §8'e göre yeniden yazılır; sayısal değerler `tools/balance/render.py` tarafından üretilir, elle girilmez.
