# Data Schemas

**Last updated:** 9 September 2026
**Register item:** B3
**Status:** Written, awaiting decision

---

## The principle

Content lives in **JSON files**, not in code. That was the architectural decision and the reason is this: the balance tool that runs in a console and Unity read the same files. Had we used ScriptableObjects the balance tool could not read them.

**Text is not embedded.** Every visible name carries a localisation key; the text itself sits in separate files. The localisation decision requires this.

**Ids do not change.** Once an `id` has shipped it is never changed, because save files refer to it.

---

## File layout

```
content/
  economy.json            general constants
  ingredients.json        ingredients
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

## The schemas

### economy.json

All of the game's general constants in one file. The balance tool will change this file more than any other.

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

Ingredients with `shared: true` belong to the basic pantry and exist in every cuisine.

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

**`marketPrice`** is the market reference. The price the player sets is compared against it and the price penalty is calculated from the difference.

**`batchSize`** is for pot dishes. It is cooked once and yields that many portions. In fast food this value is 1.

**`station`** is which piece of equipment it occupies. The kitchen bottleneck is born here.

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

The `tier` value is `sik`, `orta` or `nadir`. The traffic shares are not in economy.json; they are derived from the frequency tier.

`wardrobeTags` tells the dress-up system which clothing subset to draw from. This is the field that makes the tie between behaviour and appearance.

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

### equipment.json and upgrades.json

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

A regular takes an archetype as its base and adds its own properties on top. That way the behaviour code follows a single path.

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

### `perishableRatio` is not an INPUT, it is a MEASUREMENT

The simulation does not read this field; it is counted from the `perishable`
fields in `ingredients.json` (`tools/audit_content.py`). The reason it stays in
the schema is to keep a **design promise** visible:

| cuisine | perishable / total | ratio |
|---|---:|---:|
| fastfood | 20 / 48 | **0.42** |
| turk | 30 / 53 | **0.57** |

These rows once read 0.20 and 0.60 and **both were wrong**: when measured, both
cuisines came out at 0.58. So the thing said to be the most concrete gameplay
difference between the two cuisines had never existed.

The difference is real now and it comes from a RULE: an ingredient specific to
fast food does not spoil if in a real restaurant it arrives frozen or in a jar
(wings, fillet, sausage, mozzarella, ice cream mix, jalapeño, pickles, veggie
patty). The only things that stay fresh are the ones that go **on top of** the
burger: bread, lettuce, cabbage, apple.

In play the result is this: **fast food forgives, Turkish cuisine does not.**
A day you miscalculated you can shrug off in fast food; in Turkish cuisine you
pay for it, and the cold store is a necessity far earlier there.

Long shelf-life pantry ingredients (onion 20 days, garlic 30, potato 25) were
deliberately **left perishable**. Everything dying overnight when there is no
refrigeration is not an accident; it is the floor set by
[12-economy.md](12-economy.md) 3 — and what the cold store ladder buys is exactly
getting off that floor. Making them non-perishable would not remove the
absurdity, it would remove the upgrade.

---

## Validation

The core validates all the content at startup and **refuses to run** if there is an error. Carrying on silently is the most expensive kind of mistake.

What is checked:

1. Is every `id` unique
2. Does every reference point at a record that exists
3. Does every `nameKey` exist in the localisation files, both TR and EN
4. Are the recipe's ingredients available in that cuisine
5. Do `arrivalWeights` and `orderPreference` sum to 1.0
6. Do the `unlockDay` values exceed the campaign length
7. Does every cuisine have enough dishes, can it fill the opening menu

This validator will also be the balance tool's first step.

---

## Details awaiting a decision

1. Should ingredient amounts be in kilograms or in portions
2. Should the quality tier stay at three
3. Should the season names be real seasons, or just numbers

---

## Batch B additions

[23-core-contract.md](23-core-contract.md) §8 brings four additions to these schemas, and they are binding:

1. `pool` and `capacityPerDay` into `staff-roles.json`; a `staffing` block into `economy.json` (the owner's labour, the tiers, the caps)
2. A `signature` block per cuisine into `cuisines.json`: combo, credit, courses, broth
3. Four mandatory parameters into `dishes/*.json`: `prepMs`, `station`, `complexity`, `ingredients[].grams`; plus `plating` for the appearance
4. **Every number is an integer:** centi-coins, basis points, milliseconds. If the validator sees a decimal it rejects the file

The example schemas in this file are rewritten in Phase 0 according to §8; the numerical values are produced by `tools/balance/render.py` and are not entered by hand.
