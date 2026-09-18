# The Time Model

**Last updated:** 10 September 2026
**Register items:** [23-core-contract.md](23-core-contract.md) pending decision item 1, A4 economy numbers, A8 staff capacity
**Status:** Batch B. This file is binding. Every duration is derived by `tools/balance/timing.py`; there is no hand-written millisecond.

**Verification:** `python tools/balance/timing.py --check` — 47 consistency tests.

---

## Why this file exists

Three files contradicted each other.

| Source | Claim |
|---|---|
| [23-core-contract.md](23-core-contract.md) §1.3 | A service day is 4,800 ticks = 480,000 ms |
| [14-staff-system.md](14-staff-system.md) | A cook's capacity is 28 customers/day |
| `content/dishes/fastfood.json` | Hamburger `prepMs` = 75,000 |

The arithmetic: 480,000 ÷ 28 = **17,143 ms**, the total cook-time a single customer takes. If one hamburger takes 75,000 ms, a cook makes 480,000 ÷ 75,000 = **6.4 hamburgers** a day, which is not 28 customers' worth of work. The generated `prepMs` values and the capacity model were **4.35×** apart on the menu average and **7.5×** apart on the hamburger.

The second contradiction: on the peak day there are 97 customers, 14 tables, 4 cooks, 7 hall staff. Nowhere had anyone calculated whether 97 customers fit into 480,000 ms.

The third contradiction: the courier archetype's patience is 8,000 ms. Seating + ordering + cooking + serving takes 26,000 ms at zero load. The courier walked out angry every single time.

This file binds the three to a single arithmetic.

---

## 1. The length of the service day

### 1.1 The length of the day does not change the utilisation

First a wrong instinct has to be closed off: "if the day feels short, let us make it longer."

Section 2 below defines all the task durations **as a fraction of the day**: the waiter's time is day ÷ 25, the cook's time is day ÷ 28. If the day doubles, all the durations double and the utilisation percentages **stay the same**. So the day's length neither fixes nor breaks feasibility.

There are three things the day's length really does determine:

| What | How it is bound |
|---|---|
| Session length | One day has to finish in one sitting |
| Campaign length | 60 days × the length of a day |
| The ratio to absolute content constants | `patienceMs` (8,000–30,000) and `prepMs` are absolute numbers; if the day grows, the derived durations grow, patience does not |

The third row is critical and it bounds the day's length **from both sides**.

### 1.2 The upper bound: patience

The average patience spent in the peak slot (section 6) is 11,288 ms on a 480,000 ms day. That number is directly proportional to the day's length: 11,288 ÷ 480,000 = **0.023517 × day**.

In the archetypes' patience steps, the step after 10,000 ms is 13,000 ms. For the crowd lost at the peak to stay in the 10,000 ms band:

```
0.023517 × day < 13,000  →  day < 552,800 ms
```

If the day exceeds 552,800 ms, the archetypes with 13,000 ms of patience (the parent with children, 3.8% of heads) also start being lost at the peak, and the peak slot's loss rises from 10.2% to 14.0%.

### 1.3 The lower bound: legibility

The simplest dish (grill, complexity 1) has a `prepMs` of 10,000 ms, that is, 0.0208333 × day. For a cooking animation to be legible, a lower bound of 8,000 ms was accepted:

```
0.0208333 × day ≥ 8,000  →  day ≥ 384,000 ms
```

Below that the grill animation becomes a flash and the `station` field becomes visually meaningless.

### 1.4 The decision

The band is **384,000 – 552,800 ms**. The current value of 480,000 ms is in the middle of the band and also meets these three criteria:

| Criterion | Value | Target | Result |
|---|---|---|---|
| Service day | 480,000 ms = 8.0 min | — | — |
| Day total (service + 1,200 ticks of morning/evening) | 6,000 ticks = 10.0 min | ≤ 12 min, one sitting | Passed |
| Touch interval (60 touches) | 480,000 ÷ 60 = 8,000 ms | 6,000–12,000 ms | Passed |
| Campaign, 1x | 60 × 6,000 ticks = 360,000 ticks = **10.00 hours** | 8–14 hours | Passed |
| Campaign, 2x | **5.00 hours** | — | — |

The campaign length comes out at exactly 10 hours because 60 × 600,000 ms = 36,000,000 ms.

**Decision A: the service day stays at 4,800 ticks (480,000 ms). The 60-day campaign gives the player 10 hours 0 minutes of play at 1x and 5 hours 0 minutes at 2x. Because the day's length did not change, nobody incurs a new cost. [23-core-contract.md](23-core-contract.md) pending decision item 1 is closed.**

---

## 2. Task durations

### 2.1 The derivation rule

One rule: **the total milliseconds a role spends on one customer = the service day ÷ that role's capacity.** That way the sentence "25 customers a day" and the sentence "19,200 ms per customer" become the same sentence.

| Role | Pool | Capacity | Day ÷ capacity | Ms used | Multiplied back | Residue |
|---|---|---|---|---|---|---|
| Cook | kitchen | 28 | 17,142.857 | **17,143** | 480,004 | +4 ms |
| Waiter | hall | 25 | 19,200.000 | **19,200** | 480,000 | **0** |
| Dishwasher | hall | 46 | 10,434.783 | **10,435** | 480,010 | +10 ms |
| Cashier | hall | 66 | 7,272.727 | **7,273** | 480,018 | +18 ms |

The rounding follows the [23-core-contract.md](23-core-contract.md) §2.3 rule: half away from zero. The residues are under 1 ms per customer; 4 ms over 28 customers, smaller than a ten-thousandth of the day. The verification tests accept this slack with a `± capacity` tolerance.

The hall pool total:

```
19,200 + 10,435 + 7,273 = 36,908 ms
480,000 × 0.0768907 (model.py HALL_LOAD) = 36,907.5 → 36,908, exactly
```

### 2.2 The customer's journey

| Task | Unit | Pool | ms | Does it block the table |
|---|---|---|---|---|
| Waiting for a table | — | none | 0 | No, they do not have a table yet |
| Greeting and seating | person | hall / waiter | 3,200 | Yes |
| Taking the order | person | hall / waiter | 7,000 | Yes |
| Waiting for the cooking | table | kitchen / cook | 20,000 | Yes |
| Serving | person | hall / waiter | 9,000 | Yes |
| Eating | table | none | 38,000 | Yes |
| Paying | person | hall / cashier | 7,273 | Yes |
| Clearing the table | person | hall / dishwasher | 4,435 | Yes |
| Washing up | person | hall / dishwasher | 6,000 | **No**, at the sink |

The totals:

```
waiter     : 3,200 + 7,000 + 9,000        = 19,200 = day ÷ 25   EXACT
dishwasher : 4,435 + 6,000                = 10,435 = day ÷ 46
cashier    : 7,273                        =  7,273 = day ÷ 66
hall       : 19,200 + 10,435 + 7,273      = 36,908 = day × 0.0768907
kitchen    : 2.2 items × average cooking  = 17,127 ≈ day ÷ 28   (section 4)
```

**Separating the washing-up from the table is deliberate.** Once the plates come off the table the table frees up, and the washing carries on in the back. This preserves the "if the plates run out, service stops" bottleneck from [14-staff-system.md](14-staff-system.md) while letting the table turn freely. The staff time that blocks the table:

```
36,908 − 6,000 = 30,908 ms/person
```

### 2.3 The table turn

A table hosts not a person but a **party**. The `content/archetypes/*.json` measurement (weight × average party size):

| Measurement | Value |
|---|---|
| Average party size, all archetypes | 2.1195 heads |
| Average party size, seated (excluding the courier) | **2.1847 heads** |
| The courier's share of heads (takes no table) | 2.59% |

The courier is defined in [11-customer-system.md](11-customer-system.md) as "takeaway, occupies no table"; it is subtracted from the table arithmetic.

```
table turn = 2.1847 × 30,908 + 20,000 + 38,000
           = 67,525 + 20,000 + 38,000
           = 125,525 ms  (1,255 ticks)
```

Table time per head is 125,525 ÷ 2.1847 = **57,457 ms**.

**Decision B: the task durations are in the table above. The waiter's time × 25, the dishwasher's × 46, the cashier's × 66 and the cook's × 28 give the service day back with an error of ±1 ms/customer. One table turn is 125,525 ms.**

### 2.4 What "~120 seconds of service" meant

[23-core-contract.md](23-core-contract.md) §1.3 says "one customer service ~1,200 ticks, 120 s" and `content/economy.json` writes `serviceMs: 120000`. That number is correct **per table turn, not per customer**:

| Reading | Value | Is it correct |
|---|---|---|
| Per customer | 57,457 ms (575 ticks) | 120 s is twice too much |
| Per table turn | 125,525 ms (1,255 ticks) | 4.6% off 120 s |

The line in §1.3 should be corrected to "one table turn ~1,255 ticks", and the `serviceMs` field should become `tableTurnMs: 125525`.

---

## 3. Concurrency

### 3.1 The decision point

Does a cook deal with one dish at a time, or can they chop while the oven bakes? This determines what `prepMs` is.

| Model | What `prepMs` is | Consequence |
|---|---|---|
| One dish | The cook's busy time | Average `prepMs` = 17,143 ÷ 2.2 = **7,792 ms** |
| Concurrent | Wall clock; busy time is separate | Average `prepMs` ≈ 21,306 ms, busy time 7,788 ms |

In the one-dish model every dish drops below 8 seconds. At that scale there is no visual difference left between an oven and a grill, the `station` field turns into nothing but an icon picker, and the equipment upgrade has no story left to tell.

### 3.2 The chosen model

```
cookBusyMs = prepMs × attendBp / 10000        the number that consumes the cook pool
prepMs     = the dish's wall-clock duration    the number the player sees
```

`attendBp` is the physical nature of the station: what percentage of the wall clock is spent **in the cook's hands**.

| Station | `attendBp` | Why |
|---|---|---|
| Drinks (`icecek`) | 10000 | They fill the glass, there is no gap |
| Cold (`soguk`) | 10000 | Chopping, entirely by hand |
| Dessert (`tatli`) | 8000 | Plating and decorating |
| Grill (`izgara`) | 5600 | Put it on, turn it, take it off; there are gaps in between |
| Fryer (`fritoz`) | 3500 | They lower the basket and leave it |
| Stove (`ocak`) | 3500 | A pot on the heat: stirred now and then, not watched |
| Oven (`firin`) | 2000 | They put it in, close it, walk away |

**The fryer row used to be the stove's, and the justification gave it away.**
"They lower the basket and leave it" describes a deep fryer, and it was
written under the stove because all eight of fast food's "stove" dishes are
deep fried — chips, nuggets, onion rings, wings, mozzarella sticks, fried
chicken, wings, the fish patty. That is what `fritoz` corrects
([59](59-kitchen-equipment-and-models.md) §2). The two share a number, but for
different reasons, and the Turkish hob now carries its own: thirteen soups,
stews and pilafs, stirred occasionally.

### 3.3 The consequences

**The `station` field carries load.** It now says three things at once: which equipment is needed, how much of the cook it ties up (`attendBp`), and how many plates it can take at a time (the slot count).

**The equipment upgrade works on two axes, and neither of them shortens `prepMs`:**

| Axis | What it does | Example |
|---|---|---|
| Slot | Raises the station's concurrent plate count | A second fryer |
| `attendBp` | Frees the cook earlier | A double-sided grill: no turning, 5600 → 3500 |

A dish's cooking time is physics; the upgrade buys parallelism, not time. This shuts down the "everything speeds up when you buy equipment" inflation from the start.

**The slot count needed at the peak** (slot share 30%, 97 customers):

| Station | Concurrent plates | Slots needed |
|---|---|---|
| Grill (`izgara`) | 3.23 | 4 |
| Fryer (`fritoz`) | 3.33 | 4 |
| Stove (`ocak`) | 3.33 | 4 |
| Oven (`firin`) | 1.13 | 2 |
| Drinks (`icecek`) | 0.68 | 1 |
| Cold (`soguk`) | 0.20 | 1 |
| Dessert (`tatli`) | 0.08 | 1 |

**This table is solved PER CUISINE, and the column of numbers is fast food's.**
It was derived from `content/dishes/fastfood.json` (§4.1), where the 3.33 now
belongs to the fryer and the hob carries nothing at all; the hob's row is the
Turkish menu's, which lands on the same figure. Read down a single cuisine and
the sum is unchanged either way — **8.65 for fast food, 7.52 for Turkish**. The
sum of the whole column, 11.98, is not a number any kitchen ever faces, and
nothing in the model computes with it: `slots_needed` is strictly per station
(`conc × tables / 14`).

Within one cuisine, then: 8.66 concurrent plates with 4 cooks, so each cook carries 2.2 plates on average; that is the visible counterpart of concurrency. The total busy time is 4.15 cook-units, matching the slot utilisation of 103.9% exactly.

**Decision D: one cook runs more than one station concurrently. `prepMs` is the wall clock, the cook's busy time is `prepMs × attendBp / 10000`. The `station` field carries `attendBp` and the slot count. An equipment upgrade adds a slot or lowers `attendBp`, it does not touch `prepMs`. At tier 4 the grill, the fryer and the stove each want 4 slots, the oven 2, the rest 1 each.**

`EquipmentTests.The_docs27_peak_slot_table_holds` holds the content to this
paragraph, and it checks the closed list of shared stations FIRST: a station
the table does not name is now the test's failure, not its exemption. It used
to skip what it did not recognise, which is how `fritoz` reached the content
with no row here and nothing said a word.

---

## 4. The prepMs formula

### 4.1 How many items per customer

The formula's input. The `content/dishes/fastfood.json` menu structure is 12 mains + 8 sides + 6 drinks + 6 desserts. The signature mechanic, the combo ([07-cuisine-system.md](07-cuisine-system.md), `economy.json` `combo.items`), is three items: main + side + drink. Not everyone takes the combo; the courier takes "one item", the lone customer takes 1–2 items.

The accepted mix:

| Group | Items/customer | Menu average complexity | Menu average price |
|---|---|---|---|
| Main | 1.0 | 1.6667 | 4,516.7 |
| Side | 0.6 | 1.1250 | 2,618.8 |
| Drink | 0.5 | 1.0000 | 1,883.3 |
| Dessert | 0.1 | 2.1667 | 3,925.0 |
| **Total** | **2.2** | — | — |

**The ticket total is what verifies it.** Multiplying the mix by the menu prices:

```
1.0 × 4,516.7 + 0.6 × 2,618.8 + 0.5 × 1,883.3 + 0.1 × 3,925.0
= 4,516.7 + 1,571.3 + 941.7 + 392.5
= 7,422.2 centi-coins = 74.2 coins
```

`model.py`'s eighth-week target ticket is 75 coins. The deviation is 1.0%. The combo discount (`priceBp` 8125) and the combo ticket premium (`ticketBonusBp` 1500) roughly cancel each other out. **2.2 items is not a guess, it is a number read off the ticket.**

### 4.2 The formula

The weighted complexity unit:

```
1.0 × 1.6667 + 0.6 × 1.1250 + 0.5 × 1.0000 + 0.1 × 2.1667 = 3.0584 units/customer
```

The cook's time is to be distributed across those units:

```
3.0584 × UNIT_BUSY = 17,143  →  UNIT_BUSY = 5,605.2
```

**`UNIT_BUSY = 5,600 ms` was chosen.** The reason: with this value `prepMs` comes out as an integer at every station (`5,600 × 10000 / attendBp` divides for every `attendBp`). The deviation is 5,600 × 3.0584 = 17,127 ms against a target of 17,143 ms, **0.09%**.

```
cookBusyMs(complexity) = 5,600 × complexity
prepMs(station, complexity) = 5,600 × complexity × 10000 / attendBp(station)
```

### 4.3 The result table

| Station | `attendBp` | Complexity 1 | Complexity 2 | Complexity 3 |
|---|---|---|---|---|
| Drinks (`icecek`) | 10000 | 5,600 | 11,200 | 16,800 |
| Cold (`soguk`) | 10000 | 5,600 | 11,200 | 16,800 |
| Dessert (`tatli`) | 8000 | 7,000 | 14,000 | 21,000 |
| Grill (`izgara`) | 5600 | **10,000** | **20,000** | **30,000** |
| Stove (`ocak`) | 3500 | 16,000 | 32,000 | 48,000 |
| Oven (`firin`) | 2000 | 28,000 | 56,000 | 84,000 |

The cook's busy time is independent of the station: 5,600 ms for complexity 1, 11,200 for 2, 16,800 for 3. That is why the capacity identity is unaffected by the menu's station distribution.

If a station-free reference value is wanted, the grill column should be used: that is fast food's main station, and by complexity it is **10,000 / 20,000 / 30,000 ms**.

**Decision C: `prepMs = 5,600 × complexity × 10000 ÷ attendBp`. 2.2 items per customer, verified from the ticket total. For complexity 1/2/3 on the grill: 10,000 / 20,000 / 30,000 ms.**

### 4.4 The gap against the content

The measurement that is the reason this file was written, `content/dishes/fastfood.json` on the morning of 10 September 2026:

| Measurement | The content that day | Derived | Ratio |
|---|---|---|---|
| The average `prepMs` of the 32 dishes | 92,656 ms | 21,306 ms | 4.35× |
| Hamburger | 75,000 ms | 10,000 ms | 7.5× |
| Chocolate cake (oven, c=3) | 260,000 ms | 84,000 ms | 3.1× |
| Fizzy drink (drink, c=1) | 20,000 ms | 5,600 ms | 3.6× |

**An interim state.** The content was regenerated while this analysis was going on. The new values depend only on complexity, and the station dimension has been dropped:

| Cuisine | Complexity 1 | Complexity 2 | Complexity 3 |
|---|---|---|---|
| Fast food | 8,000 | 11,500 | 17,500 |
| Turkish | 6,000 | 8,500 | 12,500 |

This interim state does not match the capacity model either. With the order mix, the cook's time per customer is:

```
1.0 × 10,333 + 0.6 × 8,438 + 0.5 × 8,000 + 0.1 × 12,917 = 20,688 ms
target 17,143 ms  →  deviation +20.7%
counted as cook time: 480,000 ÷ 20,688 = 23.2 customers/day, capacity 28
```

On top of that `prepMs` has become station-independent; the `attendBp` dimension Decision D rests on disappears and the oven and the drinks station cook at the same speed.

`content/dishes/fastfood.json` and `content/dishes/turk.json` **must be regenerated from the section 4.2 formula.** These files were not touched by hand; the generator `tools/balance/export.py` will call the formula.

---

## 5. Peak feasibility

### 5.1 Day-average utilisation

The peak day: `model.py`'s eighth week, weekend. 97 customers, 4 cooks, 7 hall staff + the owner's 1.4 work-days, 14 tables.

| Pool | Ms needed | Ms available | Utilisation |
|---|---|---|---|
| Kitchen | 97 × 17,143 = 1,662,871 | 4 × 480,000 = 1,920,000 | **86.6%** |
| Hall | 97 × 36,908 = 3,580,076 | 8.4 × 480,000 = 4,032,000 | **88.8%** |
| Tables | 43.25 turns × 125,525 = 5,428,923 | 14 × 480,000 = 6,720,000 | **80.8%** |

The table turn count: 97 × (1 − 0.0259) = 94.49 seated heads ÷ 2.1847 = 43.25 parties. Per table that is 43.25 ÷ 14 = 3.09 turns/day, and each turn takes 26.2% of the day.

**All three pools are below 100%.** On the day average, 97 customers fit into 14 tables, 4 cooks and 7 hall staff. The bottleneck is the hall, exactly as [14-staff-system.md](14-staff-system.md) wants.

### 5.2 Slot concentration blows the pools up

The day average is not enough, because customers are not spread evenly across the day's four slots. A slot is a quarter of the day, which means a quarter of the capacity too. If a slot's share exceeds 25%, work piles up in that slot.

The `content/archetypes/*.json` measurement (the realised version of [12-economy.md](12-economy.md) §5.6):

| Cuisine | Slot 1 | Slot 2 | Slot 3 | Slot 4 |
|---|---|---|---|---|
| Fast food | 11.1% | **37.5%** | 16.2% | **35.3%** |
| Turkish | 9.4% | **60.6%** | 17.5% | 12.5% |

Fast food's lunch slot, with the current content profile:

| Pool | Slot utilisation | Work piled up | Idle wait at the end of the slot |
|---|---|---|---|
| Kitchen | 129.9% | 143,577 ms | 35,894 ms |
| Hall | 133.2% | 334,528 ms | 39,825 ms |
| Tables | 121.2% | 355,846 ms | 25,418 ms |
| **Total** | — | — | **101,137 ms** (average 50,568) |

Slot 4 (35.3%) blows up the same way: 122% / 125% / 114%, 74,083 ms in total.

An average idle wait of 50,568 ms is 1.7× the patience of even the **most patient** archetype in the fast food pool (the family, 30,000 ms). With this profile **72.8%** of the day walks out. The economy model assumes all 97 customers are served; with a 72.8% loss the entire growth curve collapses.

> **CORRECTION, 10 September 2026.** The 72.8% figure here is an **open-loop** calculation and is larger than the real one. A customer whose patience runs out also frees their place in the queue on the way out, which shortens the wait for the person behind them. In a closed-loop calculation the same profile's loss is **13.36%**. For the Turkish profile it is 30.47%. The density ceiling (28.16% on equal slots) does not change; that is correct. The detail and the corrected table: [28-peak-decision.md](28-peak-decision.md). Decision F below was written before that correction; **document 28 supersedes it.**

### 5.3 The maximum slot share

For no pool to exceed 100% within a slot:

```
maximum slot share = 0.25 ÷ the highest pool utilisation
                   = 0.25 ÷ 0.888
                   = 28.2%
```

### 5.4 What has to change

There are three options, two of them impossible.

**Option 1: grow the crew and the tables.** To absorb 37.5%, every pool has to grow 1.5×:

| Resource | Needed | Ceiling | Result |
|---|---|---|---|
| Hall staff | 1.5 × 7.4584 − 1.4 = 9.79 → 10 | — | — |
| Cooks | 1.5 × 3.464 = 5.20 → 6 | — | — |
| Total crew | 16 | **12** | Impossible |
| Tables | 1.5 × 0.808 × 14 = 16.97 → 17 | **14** | Impossible |

The crew ceiling is 12, the table ceiling is 14. Option 1 is not reachable by any player decision.

**Option 2: lengthen the day.** Section 1.1 showed that every task duration is a fraction of the day, so a longer day scales the work and the capacity by the same factor and every utilisation percentage comes out identical. Nothing changes.

**Option 3: calm the arrival profile.** The only option that works. A slot-share ceiling of 30% is set.

| Profile | Kitchen | Hall | Tables | Avg. idle wait | Daily loss |
|---|---|---|---|---|---|
| Content (37.5%) | 129.9% | 133.2% | 121.2% | 50,568 ms | 72.8% (open loop; 13.36% closed loop) |
| Ceiling (28.2%) | 97.5% | 100.0% | 91.0% | 0 ms | 0% |
| **Proposed (30%)** | **103.9%** | **106.5%** | **96.9%** | **6,288 ms** | **6.1%** |

**30% is deliberately 1.8 points above the ceiling.** Exactly at the ceiling the lunch peak produces no queue at all and the "a real moment of crisis" that [12-economy.md](12-economy.md) wants disappears. At 30% the peak bites the impatient but does not wreck the day.

The proposed distribution for fast food is **20% / 30% / 20% / 30%**. The two sharp peaks are preserved, the sharpness of the peaks is clipped.

### 5.5 Turkish cuisine

60.6% is not possible with any crew: the pools go to 210% / 215% / 196%, and the hall wants 2.4× the crew, that is, 17 hall staff. The ceiling is 12.

Turkish cuisine's identity is "the lunch peak is very hard" ([10-cuisine-identity.md](10-cuisine-identity.md), [11-customer-system.md](11-customer-system.md)) but that identity can be carried by **which slots are full**, not by piling everything into one slot. The proposal: **15% / 30% / 30% / 25%**. Lunch and afternoon full, the evening quiet — the exact opposite of the Italian, and feasible.

There is another, more ambitious route: Turkish cuisine gets a shorter service day (a tradesman's restaurant opens at noon and closes in the evening) and daily demand falls by the same proportion. That carries the identity more strongly but makes the demand formula cuisine-dependent. Not opened up for now; see pending decision item 2.

**Decision F: the peak day's utilisation is 86.6% kitchen, 88.8% hall, 80.8% tables — all three below 100%, 97 customers fit. But the slot share cannot exceed 28.2%. The current content profiles (fast food 37.5%, Turkish 60.6%) are not feasible and cannot be rescued by adding crew or tables, because what is needed is 16 staff and 17 tables, above the ceilings. The `arrivalWeightsBp` values must be renormalised so that no slot exceeds 30%.**

---

## 6. Patience

### 6.1 The old definition is broken

[12-economy.md](12-economy.md) §5.2: "Patience drains while waiting to be seated, waiting for the order to be taken and waiting for the food to arrive." That is, patience is the **total wall clock** from the door to the food.

At zero load, that is, with the restaurant empty and no queue at all:

| Situation | Calculation | Duration |
|---|---|---|
| A seated customer (grill c=1) | 3,200 + 7,000 + 10,000 + 9,000 | **29,200 ms** |
| A courier, no table (grill c=1) | 7,000 + 10,000 + 9,000 | **26,000 ms** |

The courier's patience is 8,000 ms. Even at zero load that is **3.25×** shorter than the time required. The hurried student at 10,000 ms: 2.9× short. The complaining customer at 9,000 ms: 2.9× short.

These three archetypes would walk out angry **every single time**, even if the game were played perfectly. This is not a balance problem, it is a definition error.

There were two ways out:

| Route | The cost |
|---|---|
| Grow the patience values | The mandatory floor (29,200 ms) dominates; the 8,000–30,000 range compresses into 35,000–90,000, and the 1:3.75 spread between archetypes drops to 1:2.6. The distinctiveness is lost |
| Change what patience counts | The content does not change, the 1:3.75 spread is preserved |

The second was chosen.

### 6.2 The new definition

**The patience counter only runs during idle waiting.** Idle waiting = the time during which nobody is attending to the customer and their food has not started either:

- the table queue (no free table),
- the waiter queue (seated, nobody has come to take the order),
- the kitchen queue (order placed, no free cook, the dish has not started),
- the pass queue (the dish is ready, no waiter to carry it).

The cooking itself is not idle waiting — but it does not come free either:

```
wait_score = idle_wait_ms + cooking_ms × cookPatienceWeightBp / 10000
cookPatienceWeightBp = 2500
```

A dish's cooking time counts at a quarter weight against patience. This has three consequences:

1. **Nobody is lost at zero load.** Idle waiting is zero, all that is left is the cooking share.
2. **A heavy menu is still risky.** One complexity-2 dish on the stove (32,000 ms) burns 8,000 ms of patience by itself; the courier's entire patience. The [23-core-contract.md](23-core-contract.md) §8.3 intent, "if a heavy dish sells a lot the kitchen jams", is preserved and tied directly to the menu decision.
3. **Patience becomes a pure crew signal.** The "the penalty for an understaffed crew" loop in [14-staff-system.md](14-staff-system.md) (not enough staff → the wait grows → patience runs out → reputation drops) corresponds exactly to this counter. The player is punished for something they can fix by hiring, not for something they cannot fix.

The satisfaction formula in [12-economy.md](12-economy.md) §5.4 works as it is: `−(waited ÷ patience) × 60`, where "waited" is now the wait score.

### 6.3 The numbers

| Situation | Idle wait | Cooking share | Wait score | Courier (8,000) | Student (10,000) |
|---|---|---|---|---|---|
| Zero load, grill c=1 | 0 | 2,500 | **2,500** | Stays, satisfaction 81 | Stays, satisfaction 85 |
| Zero load, stove c=2 | 0 | 8,000 | **8,000** | Walks out | Stays, satisfaction 52 |
| Peak average (30% profile) | 6,288 | 5,000 | **11,288** | Walks out | Walks out |
| Peak maximum (30% profile) | 12,575 | 5,000 | **17,575** | Walks out | Walks out |
| Peak, courier (no table) | 3,930 | 2,500 | **6,430** | **Stays** | — |

The courier does not enter the table or kitchen queue, only the hall queue; that is why they survive the peak on average. It is no accident that the most impatient archetype is the most durable one: not occupying a table is what protects them.

### 6.4 How much is lost at the peak

In the fast food archetype pool the patience distribution, weighted by heads:

| Patience | Share of heads | At the peak (11,288 score) |
|---|---|---|
| 8,000 (courier) | 2.6% | No table, stays |
| 9,000 (complainer) | 1.0% | Walks out |
| 10,000 (hurried student) | 9.2% | Walks out |
| 13,000 and above | 87.2% | Stays |

The share lost in the peak slot is **10.2%** (complainer 1.0% + hurried student 9.2%), and the day average is **6.1%**, because a queue only forms in the two peak slots: 0.30 × 10.2% × 2 = 6.1%.

The loss is also only on weekend days:

| Day | Customers | Hall slot utilisation | Queue | Loss |
|---|---|---|---|---|
| Weekday | 77 | 84.6% | None | 0% |
| Weekend | 97 | 106.5% | Yes | 6.1% |

The effect on weekly revenue: 6.1% × (2 × 97) ÷ (5 × 77 + 2 × 97) = **2.05%**.

`model.py`'s revenue is therefore an **upper bound**; at the weekend peak it should be read about 2% lower. That stays inside the decimal slack of the margin targets and does not need writing back into the model. The calculation also assumes the queue is at its average level throughout the slot; in reality the queue forms in the second half of the slot, so 6.1% is on the pessimistic side.

**Decision E: the patience counter only runs during idle waiting; the cooking time counts with a weight of `cookPatienceWeightBp = 2500`. The minimum wait at zero load is 2,500 score (seated, grill complexity 1), the peak average is 11,288 and the maximum 17,575. Every archetype can be served at zero load; 10.2% of heads is lost in the peak slot, 6.1% on the day average, and the weekly revenue effect is 2.05%. The `patienceMs` values in `content/archetypes/*.json` do not change.**

---

## 7. The derived constants, in one table

The output of `tools/balance/timing.py`.

| Constant | Value | Source |
|---|---|---|
| `TICK_MS` | 100 | [23](23-core-contract.md) §1.2 |
| `SERVICE_DAY_MS` | 480,000 | Decision A |
| `DAY_TICKS` | 6,000 | 4,800 service + 1,200 morning/evening |
| `KITCHEN_MS` | 17,143 | day ÷ 28 |
| `WAITER_MS` | 19,200 | day ÷ 25 |
| `DISHWASHER_MS` | 10,435 | day ÷ 46 |
| `CASHIER_MS` | 7,273 | day ÷ 66 |
| `HALL_MS` | 36,908 | the sum of the three |
| `T_SEAT` | 3,200 | the waiter's split |
| `T_ORDER` | 7,000 | the waiter's split |
| `T_SERVE` | 9,000 | the waiter's split |
| `T_BUS` | 4,435 | the dishwasher's split, blocks the table |
| `T_WASH` | 6,000 | the dishwasher's split, does not block the table |
| `T_PAY` | 7,273 | the cashier |
| `T_EAT_PARTY` | 38,000 | a cuisine parameter, fast food |
| `COOK_WAIT_REF_MS` | 20,000 | grill complexity 2 |
| `TABLE_TURN_MS` | 125,525 | 2.1847 × 30,908 + 20,000 + 38,000 |
| `UNIT_BUSY_MS` | 5,600 | 17,143 ÷ 3.0584 |
| `COOK_PATIENCE_BP` | 2500 | Decision E |
| `GROUP_SIZE_SEATED` | 2.1847 | the `content/archetypes` measurement |
| `DISHES_PER_CUSTOMER` | 2.2 | verified from the ticket total |
| Maximum slot share | 28.2% | 0.25 ÷ 0.888 |

---

## 8. The work this pushes out to the content and the documents

This file touched no content file. The following will be done in a separate job.

| File | What will change | Why |
|---|---|---|
| `content/dishes/fastfood.json` | 32 `prepMs` values will be generated from the section 4.2 formula | The interim state deviates +20.7% from the capacity and drops the station dimension |
| `content/dishes/turk.json` | 32 `prepMs` values will be generated from the section 4.2 formula | Same |
| `content/economy.json` | `serviceMs: 120000` → `tableTurnMs: 125525` | Section 2.4 |
| `content/economy.json` | A new field `cookPatienceWeightBp: 2500` | Decision E |
| `content/economy.json` or a new `content/stations.json` | `attendBp` and the slot count per station | Decision D |
| `content/archetypes/fastfood.json` | `arrivalWeightsBp` renormalised to 20%/30%/20%/30% | Decision F |
| `content/archetypes/turk.json` | `arrivalWeightsBp` renormalised to 15%/30%/30%/25% | Decision F |
| `content/staff-roles.json` | An optional `msPerCustomer` field (17143 / 19200 / 10435 / 7273) | Section 2.1, though the core can derive it too |
| `tools/balance/export.py` | Will use `timing.py`; `prepMs` generation from the formula | Section 4.2 |
| [23-core-contract.md](23-core-contract.md) §1.3 | "One customer service ~1,200 ticks" → "One table turn 1,255 ticks, one customer 575 ticks" | Section 2.4 |
| [23-core-contract.md](23-core-contract.md) pending decision item 1 | To be deleted, it is closed | Decision A |
| [12-economy.md](12-economy.md) §5.2 | The patience definition will be rewritten | Decision E |
| [12-economy.md](12-economy.md) §5.6 | The slot table will be pulled down to the 30% ceiling | Decision F |
| [14-staff-system.md](14-staff-system.md) | A ms-per-customer column in the capacity table | Section 2.1 |

**The `patienceMs` values in `content/archetypes/*.json` do not change.** That is the reason Decision E exists.

---

## 9. Verification

`python tools/balance/timing.py --check` — 47 tests, six groups.

| Group | What it checks |
|---|---|
| A (3) | The day's length, the campaign length, the touch interval |
| B (14) | The task duration totals give the capacity back exactly; every role's ms × its capacity = the service day |
| C (8) | The `prepMs` derivation, integrality, linearity, the ticket cross-check, the deviation from the content |
| D (4) | Concurrency consistency, the station slot |
| E (8) | Patience: zero load, the peak, the loss share, the proof that the old definition was broken |
| F (10) | Pool utilisation, slot feasibility, the weekday/weekend split |

`python tools/balance/timing.py` also writes all the derived constants and intermediate calculations out as plain text; `--md` produces the markdown tables.

**Cross-validation — the sentence that was wrong from the day it was written, and
how it was closed.** This line used to say that `CAP_COOK`, `CAP_WAITER`,
`CAP_DISHWASHER`, `CAP_CASHIER`, `HALL_LOAD` and `OWNER_WORK` in `timing.py` were
identical to `model.py`'s, and that "the moment the two diverge, the group B
tests fail".

Neither half was true, and `git log -S` settles when it stopped being true:
**never**. Both files were born in the first commit with different numbers.

| constant | `timing.py` | `model.py` |
|---|---:|---:|
| `CAP_COOK` | 28 | 30 |
| `CAP_WAITER` | 25 | 26 |
| `CAP_DISHWASHER` | 46 | 48 |
| `CAP_CASHIER` | 66 | 70 |
| `OWNER_WORK` | 1.4 | 1.3 |
| `HALL_LOAD` (derived) | 0.0769 | 0.0736 |

The group B tests did **not** fail, because they test this file against *itself*:
`waiter_ms × CAP_WAITER` equals the service day whichever capacity you pick, so
long as the budget was derived from it. A cross-check written as a sentence and
never executed is not a cross-check — it is this project's oldest mistake, in
prose form.

### Which side was right — and the answer was neither, then both

`model.py` is what `export.py` writes into `content/`, what the harness is
calibrated against, and what the core tests run on. So it won the hall roles:
`timing.py` now **imports** them, and its hand-split parts (3200/7000/9000 for
the waiter, 4435/6000 for the dishwasher) were rescaled to keep their
proportions against the new totals. `B1` also stopped claiming the waiter's time
divides the service day *exactly* — that was only ever true because 480,000
happens to divide by 25. A property of the number, not of the model.

**The cook was the other way round.** `C2` derives the cook's time per guest from
the content's own `prepMs` and `attendBp`: **17,127 ms**, which is a capacity of
480,000 ÷ 17,127 = **28**. `model.py` had been declaring 30 since the first
commit, so the staffing model said a cook serves more guests than the dishes
allow. Three independent places said 28 — this tool, [14](14-staff-system.md)'s
prose, and the dish data itself; only `model.py` said 30.

Measured before changing it, fast food, 8 seeds:

| strategy | end cash at 30 | end cash at 28 |
|---|---:|---:|
| `makul` | 22,257 | 22,215 |
| `genislemeyen` | 12,750 | 12,750 |
| `atilgan` | 2,269 | 2,288 |
| `planci` | 25,094 | 25,568 |

It costs nothing. `model.py` moved to 28, the content regenerated, and the one
test that pinned 30 was updated with the reason.

**`timing.py --check` is now 52 checks, 0 failed** — including five new group G
checks that name each constant and both values. A red G line would mean the two
have drifted again; this time something runs it.

---

## Details awaiting a decision

1. `T_EAT_PARTY` will become a per-cuisine parameter. Fast food is 38,000 ms. The Italian "sits long, the table turn is low" ([07-cuisine-system.md](07-cuisine-system.md)); its value must be solved so that table utilisation stays below 100%
2. Should Turkish cuisine's service day be shortened (a tradesman's restaurant opens at noon), or should the slot profile be calmed instead; the second was chosen, but the first carries the identity more strongly
3. Will `attendBp` fall with the equipment tier, or should an upgrade only add slots
4. Will there be an archetype other than the courier that occupies no table (a delivery channel)
5. Is `cookPatienceWeightBp` 2500 the right weight; playability testing will say
