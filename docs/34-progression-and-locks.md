# 34 — Progression: locks, complexity, the seasons and "the customer who asks"

10 September 2026. This document describes **five pieces of dead content** found in a single day and the two holes that opened up while bringing them back to life.

---

## 1. First this question: is there something behind everything the content promises?

Four separate bugs were found on the same day and all four were of one class:

| What was promised | What the code was doing |
|---|---|
| `spoilDays` — the shelf life of 44 ingredients, 1 to 45 days | It deleted all of them every night |
| The `tatli` group — 9 dessert dishes | `PickOrder` never looked at desserts |
| Dish groups are cuisine-specific | The fast food dictionary was hard-coded |
| `attendBp` — [27](27-time-model.md) Decision D | The kitchen had never applied it |

None of them breaks the build, none of them fails a test. All of them are **silently dead.** This class of bug is not found by guessing, it is swept for.

`tools/audit_content.py` was written for that. It asks three questions:

1. Is there a key in `content/*.json` that no DTO binds?
2. Is there a property on a core type that is read **nowhere**?
3. Is there a field in [13-data-schemas.md](13-data-schemas.md) that is not in the generated content?

The second question has to be asked carefully: a field not being read *in the core* is not on its own a bug — the slot durations are handed to `TimingConfig` outside the core, the capacities are validated in the loader. The real finding is a field read **nowhere**.

### The two whole systems it found on its first run

**Seasonal prices.** All 77 ingredients have a multiplier for each of the four seasons and **35 of them have real movement**: tomatoes are 14% cheaper in summer and 20% dearer in winter. The simulation was always paying the base price.

**Ingredient quality.** Three tiers (`dusuk` / `standart` / `yuksek`), affecting both the price and the satisfaction. It was never read; **it was written in §9.**

Also, `seasonDays` and `unlockSeason` are **bound to the DTO but never reach the core** — a third class that slips between the auditor's two checks. The auditor cannot see it; it should be added on the next pass.

---

## 2. The seasons were implemented

The campaign is 60 days, a season 15: spring, summer, autumn, winter. The price of an ingredient bought at the market changes with the season.

**On its own this is not a decision, only a swing in the expenses.** For it to be a decision you have to be able to buy cheap and keep it — and that is exactly the **cold storage** ladder written in [32](32-equipment-and-rebalance.md) §7. The two pieces complete each other:

```
onions 10% cheaper in autumn  +  a cold room (60% of the shelf life)
      = cheap stock that lasts until winter
```

When the `planci` strategy used it in the balance tool it **gained 9%** (17,722 -> 19,327). So the seasons are no longer decoration.

There is a structural side effect too, and it was left in deliberately: winter is both the **dearest** season and the **busiest** stretch of the campaign. The cost curve rises towards the end.

---

## 3. The dish lock: from a calendar to an achievement

### What the situation was

The dishes were not all open from day one already: **6** of the 32 dishes (2 main courses) were open on day 1, the rest arriving one by one from day 3 to day 57.

But the opening was a **calendar**. On the day `unlockDay` arrived, the dish opened; the player did nothing for it.

### What happened

The lock now wants three conditions at once:

| Condition | What it means |
|---|---|
| `unlockDay` | The pacing floor — the earliest it can happen ([09](09-content-inventory.md)) |
| `unlockReputationCenti` | Something **earned** |
| `requiresStationTier` | Something **bought** |

The third one matters: **buying equipment now opens menu items.** The station ladder written in [32](32-equipment-and-rebalance.md) was no longer selling only speed; it sells content.

### `complexity` came alive — but not as a reward

The `complexity` field (1..3) existed in the content and was read nowhere. The data was already meaningful: complexity-3 dishes cost **twice as much and take twice as long to prepare** as complexity-1 ones.

The first implementation scaled satisfaction by complexity **in both directions**. The measurement caught it immediately:

> 17 of the Turkish menu are complexity 3. The `sadece_hal` player, who does nothing at all, jumped from 5,094 to **33,488**.

The reason is simple: most customers are satisfied anyway, so in practice the magnifier only worked in one direction and it inflated the reputation.

**The reward is already in the price.** Complexity is now only **risk**: if you take a skilled dish late the customer gets angrier, and if you take it well there is no extra reward.

---

## 4. The hole the lock system opened

Once the lock was written the measurement showed a second problem:

| | before the lock | after the lock |
|---|---:|---:|
| `sadece_hal` (does nothing at all) | 3,386 | **18,583** |
| `fazla_kadro` | 1,921 | **14,403** |

**A locked dish = a dish not stocked = a dish with no cost.** The gain from narrowing the menu was handed free of charge to the player who never manages the menu at all. A designed tension had been turned inside out.

### The solution: the customer comes and asks

> "Yeni yemek kilitleri açılınca müşteriler onu gelip sorsun; böylece gerekli masrafı yapmazsa müşteri memnuniyeti düşer."
>
> *("When new dish locks open, let the customers come and ask for it; that way, if they do not make the necessary outlay, customer satisfaction drops.")*

If there is a main course whose day and reputation have come but whose **equipment has not been bought**, the customer asks for it. Not finding it, their satisfaction drops.

Two details matter:

- **Only a gap the player could have closed is asked about.** A dish the calendar has not brought yet is not asked for; that would be unfair.
- **The penalty scales with the number of pending dishes.** Skipping one dish is a small gap; skipping eight is neglect, and the neighbourhood talks about it.

| | after the lock | after the asking customer |
|---|---:|---:|
| `sadece_hal` | 18,583 | **14,424** |
| `fazla_kadro` | 14,403 | **5,106** |

It closed the hole. And because the mechanic turns buying equipment from an abstract speed gain into **a disappointment at the table**, it lines up with [02-design-proposal.md](02-design-proposal.md)'s "visible growth" principle.

---

## 5. Once again I wrote the rule for one cuisine

The lock rule was absolute at first: *complexity 3 -> station tier 2.*

In fast food **2** of the 32 dishes are complexity 3. In the Turkish restaurant it is **17**. So the rule locked half of the Turkish menu at a stroke, and the measurement showed it: `sadece_hal` was earning **89%** of what good play earns, because a locked menu means a cheap menu.

The rule now goes by **the cuisine's own distribution**: the dishes are sorted by complexity and price and split into three — the bottom 45% need no equipment, the next 35% tier 1, the top 20% tier 2. Both cuisines come out 17 / 9 / 6.

This is a repeat of [33-second-cuisine.md](33-second-cuisine.md)'s lesson, and it was repeated by the person who wrote that document. `tools/balance/calibrate.py` now runs **both cuisines**; calibrating with one cuisine is therefore no longer possible.

---

## 6. The state this pass left behind

Four new systems together made the economy **harder**: the seasonal cost, the lock condition, the complexity risk and the asking customer. The calibration was re-solved with both cuisines and **the realisation rate went back to 6,500**, the rents 650 / 1,550 / 2,250 / 4,000.

At an intermediate point the balance was marginal: on some settings the player who did nothing beat the one who followed the full plan. The reputation floor in §7 closed that too.

**The result, in both cuisines:**

| strategy | fast food | Turkish | emptied |
|---|---:|---:|---:|
| does nothing at all | −4,943 | −4,722 | **day 5** |
| buys ingredients only | 12,311 | 14,920 | 32 / 36 |
| **good player** | **24,876** (reputation 91.0) | **26,211** | never |
| does not expand | 11,519 | 12,859 | never |
| expands recklessly | −51,604 (in debt on day 7) | −5,433 (day 23) | 8 / 11 |
| **planner** | **17,501** (reputation **100.0**) | **20,419** (reputation **87.5**) | never / 54 |
| high price | 3,790 | 3,145 | 9 / 6 |
| overstaffed | 7,947 | 13,110 | 52 / never |

**The calibration penalty is ZERO, in both cuisines.** Every design target passes:

- Neglect has a price: the shop of the one who does nothing **empties on day 5** and goes under on day 42
- The bad manager **scrapes through, but only just**: they crawl until day 32 without going under
- Reckless growth puts you in debt on day 7
- Growing is rewarded **2.3-fold**
- **Whoever invests on time has a reputation above 80 in both cuisines** (92.1 and 87.5)
- Overstaffing and high prices are punished
- Money never becomes irrelevant in any week

120 tests, 20/20 model checks.

### The first-week rule: the demand floor turned out to be too high

> "Hiç iş yapmayan biri ilk haftayı geçemesin. Kötü yönettiği durumda zor da olsa geçebilsin."
>
> *("Somebody who does no work at all should not get through the first week. If they manage badly they should get through, but only with difficulty.")*

**It was tried with the starting cash and the measurement rejected it.** Bringing it from 8,000 down to 1,725 (85% of the first week's outgoings) did make the do-nothing player die early, but `planci` fell to −18,976, the good player's reputation dropped to 16.2 and their table count stuck at 4. The reason is structural: **the buffer a real player needs in order to run the place is the same buffer that keeps a dead restaurant standing for weeks.**

Two more things came out and both of them changed the direction.

**The first:** the demand multiplier was `0.5 + reputation`, that is, even with zero reputation the restaurant got **half** the base demand. A shop nobody talks about was behaving as if it were half full; the death spiral of neglect did not actually exist.

**The second:** [08-endgame.md](08-endgame.md) had already rejected the closure — *"The save is still never deleted, the game still does not end."* Going under is not an ending, it is a **ladder**. So "not getting through the first week" cannot mean the game ending.

And the do-nothing player's **revenue is zero** (they have no ingredients, they turn everybody away at the door), so even if demand falls the day they go under does not change: cash ÷ weekly outgoings = day 42. Pure arithmetic.

### The solution that was accepted: not the day you go under, the day you EMPTY OUT

The demand curve was steepened below 20 points — at zero, 10% of the base demand remains. The breaking point is **below** the starting reputation (30), and that matters: it was put at 30 first and the test caught it — because the reputation starts eroding from day one, every player entered the steep region on the second day already and the opening week collapsed for everyone.

An **"emptied"** column was added to the balance tool: the day the reputation drops below 20 points, that is, the moment the shop visibly empties out.

| strategy | emptied | first debt |
|---|---:|---:|
| **does nothing at all** | **day 5** | 42 |
| expands recklessly | 8 | 7 |
| prices badly | 9 | — |
| manages badly | 32 | — |
| overstaffed | 45 | — |
| **good player** | **never** | — |
| **planner** | **never** (54 in the Turkish cuisine) | — |

The rule holds now, when it is read correctly: **the shop of the one who does nothing empties on the fifth day.** The player sees within the first week that it is over; the money in the till only delays the funeral. The bad manager crawls to day 32 — "gets through, but only just". Whoever plays well never collapses.

It is right fictionally too: a restaurant with money does not close immediately, it drags on.

**This change does not touch good play at all.** Everybody who keeps their reputation above 20 gets exactly the same numbers as before; only the collapsing player is punished.

## 7. The cause of the difference between the cuisines was not the cuisine

Even after complexity was made relative, the difference remained: the good player's reputation was 94.2 in fast food and 74.5 in the Turkish restaurant. Twenty points.

The measurement put the cause somewhere else:

| | avg. satisfaction | served | final reputation |
|---|---:|---:|---:|
| fast food | 86.7 | 3,062 | **94.2** |
| Turkish | 83.2 | 2,860 | **74.5** |

**The satisfaction difference is 3.5 points, the reputation difference is 20 points.** So the problem is not in the cuisine, it is in the reputation curve.

The cause is the damping rule. In [29](29-phase0-simulation.md) the gain had started being multiplied by the remaining headroom, to stop the reputation hitting the ceiling in nine days:

```
gain = gain × (10000 − reputation) / 10000
```

That job is being done. But because the multiplier is **linear**, the region near the top is a knife edge: once the reputation reaches 90 the gain drops to a tenth and the daily 0.3-point erosion beats it. In that region a 3.5-point difference in satisfaction turns into a 20-point difference at the equilibrium.

**The solution: a floor under the damping.** Even if the remaining headroom drops below 30 points, the multiplier is computed as though it were 30 points. The early damping stays exactly as it was — the reputation still does not hit the ceiling in nine days — but the region at the top stops being a knife edge.

| reputation (`planci`) | before the floor | after the floor |
|---|---:|---:|
| fast food | 94.2 | 92.1 |
| Turkish | 74.5 | **87.5** |
| **difference** | **20 points** | **4.6 points** |

As a side effect, in the Turkish restaurant `planci` (19,912) now beats `sadece_hal` (16,931); the ordering violation was closed by this change too.

**The lesson:** looking for the difference between the two cuisines inside the cuisines was wrong. The difference had grown in the non-linear region of a curve both of them pass through.

---

## 8. The silent drift between content and code was closed

The auditor's third check found **19 fields**: bound by the DTO but never reaching the core. All of them were decided on and **two of them were real bugs.**

### Real bug 1: the eating time

The content said **38 seconds**, `TimingConfig` used **45**. An 18% difference in table turnover, drifted for years and nobody had seen it. After the fix, `planci`'s reputation went from 92.1 to **100.0** and its losses fell from 25 to 22.

### Real bug 2: the length of the service day

The content said **120,000 ms**, the code used **480,000** — four times as much. Here **the code was right**: when [27-time-model.md](27-time-model.md) raised the day length to 480,000 the content was not updated. The content was fixed; the two now come from one source.

### The silent but harmless ones

The loan terms (1.35x / 8 weeks / three options), the rent day, the satisfaction neutral threshold (6000), the low-price floor — all of them were written in the content, constant in the code, and their values were the same **by coincidence**. They are read from the content now.

Verification: after the rework the balance tool gave **exactly the same** numbers; then it moved in the expected direction with the eating-time fix.

### Turning a dead field into an invariant

`TierIndex` (frequent / medium / rare) was read nowhere. [13-data-schemas.md](13-data-schemas.md) §144 says "the traffic shares are derived from the frequency tier" and the content complies — frequent 550–1300, medium 220–400, rare 100–150, with no overlap at all — but nothing was enforcing it.

It is checked at load time now: if an archetype's tier is changed and its weight forgotten, **the game does not open**. A dead field turned into an invariant that stops two pieces of data drifting apart silently. This is the third way of dealing with dead fields: implement it, delete it, or **turn it into an invariant**.

---

## 9. Ingredient quality: the content had already decided

The last big dead system the auditor found. All 77 ingredients had a three-tier table and the simulation never read it:

```json
"qualityPriceMultiplierBp": { "dusuk": 8000, "standart": 10000, "yuksek": 12500 },
"qualitySatisfactionCenti": { "dusuk": -1200, "standart": 0, "yuksek": 800 }
```

### The design decision was written in the data

There are three sensitivity classes and all six of the most sensitive are **meat**:

| class | cheap price | cheap satisfaction | how many ingredients | example |
|---|---:|---:|---:|---|
| **meat** | −25% | **−20 points** | 6 | `kiyma`, `tavuk_gogus`, `balik_filetosu`, `dana_kusbasi` / `kuzu_kusbasi`, `kuzu_pirzola_et` |
| medium | −20% | −12 points | 38 | tomato, milk, egg, sausage, bread |
| insensitive | −15% | −5 points | 33 | salt, black pepper, flour, sugar, pasta |

So the rule **going cheap is free on salt and catastrophic on meat** is already written into the content.

### This is why a single global setting is enough

If quality were chosen per ingredient it would be 77 decisions; [16-screens-and-tutorial.md](16-screens-and-tutorial.md)'s budget of 40–60 taps a day would not carry that. But thanks to the sensitivity distribution, **even a single setting gives a different result per dish**: a meat-heavy menu is punished hard, a pasta menu barely notices.

The stock quality blends by weighted average — somebody who buys cheap and then buys dear cannot get rid of the cheap goods on their hands straight away.

### Taking an average was wrong and the measurement caught it

In the first implementation a dish's quality effect was the **average** of its ingredients. In fast food it looked right; in the Turkish cuisine it came out inverted:

> `ucuz_malzeme` was earning 35,200 — **more** than good play's 26,211.

The cause was the average itself: the cheap onion thrown into the pot was **hiding the cheap meat**. A stew with six ingredients divides the penalty by six, a hamburger with three by three. A comment I had written myself said the opposite — "the customer says *the meat is cheap*" — but the code was not behaving that way.

The right rule: **whatever the most decisive ingredient says.** The customer is not counting the onions on the side.

| | fast food | Turkish |
|---|---:|---:|
| with the **average** | 9,117 | **35,200** <- not a trap, a strategy |
| with the **most decisive** | 9,117 | **11,604** <- a trap |

`QualityTests.Cheap_meat_cannot_hide_behind_a_crowded_recipe` protects against that regression: a dish with many ingredients cannot be punished less than one with few.

### The result

| | `makul` | `ucuz_malzeme` |
|---|---:|---:|
| end cash (fast food) | 24,876 | **9,117** |
| end cash (Turkish) | 26,211 | **11,604** |
| reputation | 91.0 / 58.0 | 16.3 / 18.2 |
| tables reached | 7.9 / 8.0 | 4.4 / 4.4 |

The chain is readable: the unit cost falls -> the reputation collapses -> the customers thin out -> growth stops. Cutting the ingredients is a trap, and **why** it is a trap is visible.

---

## 10. Named equipment

> "Özel ekipmandan kastım mesela pide yapmak istersek taş fırın gereksin. Döner için döner takılan tezgâh gereksin. Veya İtalyan makarna için parmesan peyniri gereksin."
>
> *("What I mean by special equipment is, for example, if we want to make pide then a stone oven should be needed. For doner, a counter with a doner spit on it should be needed. Or for Italian pasta, parmesan cheese should be needed.")*

[09-content-inventory.md](09-content-inventory.md) had already planned this: **10 special cooking stations per cuisine**. It had not been done; §3's "station tier ≥ N" rule was its pale stand-in.

`equipment.json` now carries `cuisineStations`: cuisine-specific, **named** equipment added **after** the six shared stations.

| cuisine | equipment | price | what it opens |
|---|---|---:|---|
| Turkish | **`tas_firin`** (Stone Oven) | 2,700 | `borek`, `kadayif`, `revani`, `firin_makarna`, `musakka`, `karniyarik`, aubergine kebab |
| fast food | **`milkshake_makinesi`** (Milkshake Machine) | 1,860 | milkshake |
| fast food | **`waffle_makinesi`** (Waffle Iron) | 2,700 | waffle |

The only difference from the shared six is that **they are not there at the start**: until one is bought the dishes tied to it are locked, even if their day and their reputation have come. The difference goes from abstract to concrete — not "grill tier 2 required" but **"buy a stone oven, and borek opens"**.

**Doner and pide could not be given, because they are not in the content.** The Turkish menu has kofte, chicken shish, adana, wings and chops; there is no doner. The mechanism is ready, and when those dishes are written, `doner_ocagi` and `pide_firini` can be defined and wired up in a single line each. The ten-strong lineup is a content job.

### What is written in two places has to be cross-checked

`equipment.json` lists the dishes each piece of equipment opens, but the dishes also state their own station. After watching something written in two places drift apart silently five times today, the list was not left as documentation: it is cross-checked at load time. If a dish is moved to another station and the list is not updated, **the game does not open**.

### The cause of the regression was not the content but the bot's decision

After the equipment was added, the measurement showed `planci` going under in fast food: −1,896, reputation 35.6, crew down from 11 to 6.9. The daily data gave the reason at a glance:

| day | till |
|---|---:|
| 55 | 19,649 |
| **56** (pay day) | **330** |
| 57 | 163 -> cannot buy ingredients, 4 customers |
| 58–60 | zero service, reputation from 100 to 31 |

At fourteen tables the weekly fixed costs are ~19,000 and the restaurant accumulates exactly that much a week. The two new pieces of equipment (4,560) pushed the knife edge down. So the content was not wrong — **the strategy was spending the wage money.**

The fix is in the strategy: an equipment purchase cannot eat the weekly payment. A real player does not buy a waffle iron as pay day approaches.

| | before the equipment | after the equipment | with the payment guard |
|---|---:|---:|---:|
| `planci` fast food | 17,501 | **−1,896** | **21,468** (reputation 100) |
| `planci` Turkish | 20,419 | — | **23,581** (reputation 100) |

The fix did not just close the regression, it made the bot **more competent than it was before**. That is the balance tool's second job: while it measures the economy it also shows up the strategies' incompetence.

---

## 11. Order preference: deriving instead of inventing

[13-data-schemas.md](13-data-schemas.md) had designed an order preference per archetype:

```json
"orderPreference": { "sulu": 0.6, "pilav": 0.25, "corba": 0.15 }
```

The simulation was not reading it — **every customer was ordering from the same distribution.** Implementing the schema literally would have meant hand-writing a weight table for 24 archetypes, and those tables would have been made up: there is no basis on which to say whether a given archetype likes rice at 25% or 30%.

**Instead it was derived from the character fields that were already loaded.**

### Dish choice: `priceSensitivityBp`

This field existed in the content, varied between 4,000 and 25,000, and was used only in the *satisfaction penalty* — it played no part at all in **choosing** a dish.

Now dish choice from a role is not equiprobable: 10,000 sensitivity is neutral, above it leans cheap, below it leans expensive. No dish is excluded entirely (there is a base weight), the distribution just shifts.

| cuisine | most sensitive | avg. receipt | least sensitive | avg. receipt |
|---|---|---:|---|---:|
| Turkish | student (23,000) | 51.5 | food critic (4,000) | 60.7 |
| fast food | haggler (25,000) | **29.1** | food critic (4,000) | **43.2** |

In fast food there is a **48% receipt difference** between the stingiest and the most generous customer — with zero new content.

### Extras: `tipChanceBp`

The dessert probability varies by archetype too. The tipping tendency is a proxy for the tendency to spend: somebody who tips a lot also takes a dessert, somebody who never tips does not. The inspector, whose tip is 0, gets half the dessert probability, and the neighbourhood group meal, at 3,200, gets twice.

### Why this is better

Writing a made-up table would have meant writing the data **twice**: the character in one place (price sensitivity), the preference in another. After watching something written in two places drift apart silently five times today, going down that road would have been wrong.

In its derived form, changing an archetype's price sensitivity changes its ordering behaviour **by itself**. And `PriceSensitivityBp` and `TipChanceBp` now do double duty.

**The measured side effect:** the good player went from 23,636 to **26,434** and their reputation from 86.5 to **92.4**. The reason is that customers who are insensitive to price now really do choose the expensive dish.

---

## 12. The owner's intervention: there was an effect, there was no constraint

[02-design-proposal.md](02-design-proposal.md) §10 defines the owner's role during service in a single sentence: *"during service you only intervene in crises"*. §59 sets the limit as well: **3–5 uses per day**.

In the simulation the **effect** of intervening was written — an apology and a freebie +15 points, the owner's attention +20, and the customer being attended to waits a little longer. But two constraints were missing:

| What was missing | Its consequence |
|---|---|
| **The number of uses** — `interventionsPerDay: 4` is written in the content, nothing enforces it | Every angry customer could be rescued for free; crisis management was not a resource but an unlimited button |
| **The cost of the freebie** — [12-economy.md](12-economy.md) §3 says "2 cost per portion" | The tea was a free satisfaction tap |

Both were added. And one more bug came out, through a test: I was only setting up the allowance at the day's opening, but the simulation **starts with day one already open** — so the first day went by with zero allowance. It was put in the constructor too.

### Measured: intervening pays off, but in reputation, not money

The `mudahaleci` strategy plays like the reasonable player and, in addition, gives the owner's attention to the table with the least patience left during service.

| | `makul` | `mudahaleci` |
|---|---:|---:|
| end cash | 26,434 | **27,019** (+2.2%) |
| reputation | 92.4 | **99.5** |
| tables reached | 7.8 | 8.1 |
| served | 2,216 | 2,288 |

The difference in the till is small, the difference in reputation is large. That is the right shape: docs/02 defines the owner's service role as **crisis management**, not as a money tap. In a restaurant whose reputation is near the ceiling anyway the gain looks small; its real value shows up in a restaurant that is struggling.

### I tied the target to the wrong thing, and the second cuisine corrected it

The target put into the calibration at first was: *intervening cannot be worse than not intervening* — measured by the end cash. The Turkish cuisine rejected it: `mudahaleci` 24,058, `makul` 28,638.

But in the same run:

| | `makul` | `mudahaleci` |
|---|---:|---:|
| reputation | 56.5 | **81.3** |
| tables reached | 8.4 | **11.0** |
| served | 1,998 | **2,152** |
| end cash | 28,638 | 24,058 |

The interventionist is running not a smaller but a **bigger** business. Its till is lower because the rising reputation pushed the strategy into more expansion and more crew — it did not lose the money, it **invested** it.

So the measure was wrong. What the intervention directly affects is **reputation and customers served**; the till depends on what the strategy does with that reputation and cannot be this mechanic's measure. The target was switched to those two, plus "the interventionist must not fall into debt".

**The general lesson:** measuring a mechanic by a number it does not directly affect can convict it unjustly. Something that raises the reputation **lowers** the end cash in the hands of a strategy that grows on that reputation.

### The fourth kind that was never written

docs/02 gives three examples: *"speed up a station, send a freebie to a waiting table, greet a VIP customer yourself."* The second and third exist. **The first — speeding up a station — does not**, because `InterventionKind` carries three values and that is a closed list in [23-core-contract.md](23-core-contract.md). Adding one is a contract change; a separate decision.

---

## 13. Speeding up a station: it was worth opening the contract

The end of §12 left this kind as "a separate decision". The decision was taken: **it was written.**

Because `InterventionKind` is a closed list in [23-core-contract.md](23-core-contract.md), adding a fourth value is a contract change. It was worth it, because the other two interventions both target **a waiting table**; neither of them can touch the kitchen. During service the place that jams is usually the kitchen, and the owner had no button at all for the kitchen.

### The owner does not cook

[14-staff-system.md](14-staff-system.md) is clear: the owner does not work in the kitchen. So the mechanic cannot be "the owner steps up to the counter". Instead, `RushStation` **erases 40% of the remaining wall-clock time of the jobs cooking at that station** — the owner goes into the kitchen and says "that table is waiting", and one plate is moved up. It changes not the cook's capacity but **the priority of the queue**.

This does not break [27-…](27-time-model.md) Decision D either: `prepMs` stays fixed on the content side, and what is shortened is the *remaining* time at that moment — not a permanent speed-up like the equipment gives, but a one-off allowance usable a few times a day.

### Speeding up an empty station does not burn the allowance

The intervention allowance is daily and counted. If pressing an empty station spent an allowance, the mechanic would punish the player **for misreading the interface** — which station is busy is not always visible at every zoom level. An empty station is rejected and the allowance stands. Two tests protect this.

---

## 14. Market price volatility: there has to be something to keep track of

[12-economy.md](12-economy.md) §3 said this: *"There is no advantage in buying early, stock spoils. Catching a cheap day is not luck, it is a matter of watching."* `priceVolatilityBp: 2500` was written in the content. **It was not being read.** So the market gave the same price every day and there was nothing to keep track of.

Now a separate daily multiplier is rolled for every ingredient at the day's opening (`AdvanceToNextDay`) — from the `Market` stream, in a ±25% band. When you go to the market in the morning you see today's price; the order is billed at that price.

### Why at the day's opening and not at the service opening

When the player makes the morning stock decision they must see **today's** price. If the multiplier were rolled at the service opening, the player would buy looking at yesterday's price and the price would move afterwards — that is not a decision, it is a gamble.

### Volatility on its own is not a decision

Even if the price of forty-four ingredients moves every day, if you cannot buy what is cheap today and keep it for tomorrow it is just noise in the expenses. What makes it a decision is **the cold storage ladder**: `CanKeep` + `KeepDays`. The harness's `stockAhead` logic was already doing this (if today's price is below 92% of the year's average, stock up to four days ahead) — that comparison had until now been measuring only the **season**, because there was no daily volatility. The same line now reads two signals at once: the season (slow, predictable) and the market (fast, unpredictable).

The average does not include the volatility — the average of the volatility is 1.0 — so the comparison is not broken.

### The measurement: the balance improved

| | before | after |
|---|---:|---:|
| calibration penalty (both cuisines) | 8 | **0** |

The volatility did not break the balance, it **fixed** it. The reason is probably this: a fixed price made the strategy that cuts the ingredients too predictable; volatility adds risk to that same strategy but adds no risk to a player who has invested in cold storage. So the volatility creates **the payoff** for a ladder that was already being sold.

### Determinism

The swing is random but tied to the seed: the same seed gives the same price sequence, and `_rngMarket` and `_marketBp` go into the save (version 8). `The_market_prices_swing_every_day` protects both at once — the price swings **and** a twin simulation gives the same first price.

### A footnote: the calibration could give a wrong answer silently

While taking this measurement, `calibrate.py` gave all seven candidates the same penalty (1998) and wrote the worst one down as "the best". The cause was not in the balance: the harness's **Debug** output gets caught by the "Application Control" policy on this machine and never runs at all. `run()` was swallowing the error, the table came back empty, and every candidate came out equal.

Two things were fixed: the harness now runs with `-c Release`, **and** if the table is completely empty `calibrate.py` no longer assigns a penalty and carries on, it throws. The penalty mechanism measures a *design* violation; turning a *run* error into a penalty makes a broken measurement look like a valid result.

---

## 15. Staff experience: how long who has been here

[14-staff-system.md](14-staff-system.md) had written this: *1 point for every day worked, a level at 30 points, at most 3 levels, +10% speed per level.* `staff-roles.json` carried the `xpSpeedBp: [10000, 11000, 12000, 13000]` ladder on each of the four roles. **None of it was being read.**

### Experience cannot be counted

The simulation keeps the staff as **numbers**: `_cooks`, `_hall`. Experience does not fit that model — the answer to "how many cooks are there" is a number, but the answer to "how long have they been here" belongs to a *person*. So two arrays were added: `_cookXpDays[]`, `_hallXpDays[]`. Hiring appends at the **end**, removing takes from the **end**.

Taking from the end is deliberate: otherwise a nonsensical decision — "fire the most experienced" — would arise. Somebody hired back is a new person too — cutting the crew and hiring back is not free.

### What gets shorter is not the cooking time

Experience divides `attendMs`, not `prepMs`. So **an experienced cook is freed up sooner, the food does not cook faster.** Whatever [27-…](27-time-model.md) Decision D says for equipment applies to experience as well; `Experience_does_not_touch_a_dishs_cooking_time` protects it. The same on the hall side: the waiter's job gets shorter, the customer's eating time does not.

The owner (element zero of the hall array) does not gain experience — they already have a separate multiplier through `OwnerAdjusted`.

### The measurement: experience helps a THIN CREW most

To find out whether the mechanic binds at all, the ladder was exaggerated (+100/200/300% instead of +10/20/30%) and the fast food run repeated:

| strategy | the real ladder | exaggerated |
|---|---:|---:|
| `genislemeyen` reputation | 64.7 | **89.4** |
| `genislemeyen` served | 1,091 | 1,160 |
| `fazla_kadro` reputation | 30.1 | 36.9 |
| `makul` reputation | 99.1 | 99.5 |
| `planci` reputation | 100.0 | 100.0 |

In big, well-staffed businesses the effect is **saturated** — they are at the ceiling already. The difference shows up in `genislemeyen`, running four tables with two people. So experience works exactly where it should: **it gives the business that is too small to take on somebody new a payoff for holding on to who it has.**

With the real ladder the effect stays measured (the calibration penalty is 0 again, the measured realisation 10,196 -> 10,207). It is not a strategy but a reward accumulating in the background — and it is paid for by a compounding 2.2% weekly wage rise anyway. **Hold on to what you have, they get faster; the cost of holding on rises too.**

### There is no third level in 60 days

30 days = level 1, 60 days = level 2, 90 days = level 3. So the ceiling cannot be reached within the campaign; the third level belongs to [29-…](08-endgame.md)'s free play. That is not a gap but a consequence of the time structure — but it is written down explicitly in a test, because it is a number that could be changed without anyone noticing.

---

## 16. The remaining three dead fields: a different fate for each of them

[The fifth lesson](#) said a dead field has three fates: **implement it, delete it, or turn it into an invariant.** The three items left in the queue illustrate all three.

### `weeklyWageMultiplierBp` -> **an invariant**

`economy.json` carried both `weeklyXpWageGrowthBp: 220` and its ten weekly compounded values. The same fact in two places. The table was not deleted — the balance tool and the interface read it — but `ContentLoader` now validates it against its generator at start-up, with a one-basis-point tolerance (the table is computed in Python, the validation in C#).

### `unlockSeason` -> **an invariant**

Each of the 32 dishes carries both `unlockDay` and `unlockSeason`, and the second can be derived from the first: `(day - 1) / 15 + 1`. The loader now validates that and carries it as `DishDef.UnlockSeason`; the progression screen will group the dishes by it. On top of that, [09-…](09-content-inventory.md)'s progression curve was tied to a test as well: 13/8/6/5 dishes per season, in both cuisines.

This is why `ContentSetLoader` reads the season length from `economy.json` — writing a second constant would have opened exactly the bug that had just been closed.

### `ownerPool` -> **an invariant**

The content says `"hall"` and the code already **assumes** hall: `StaffingModel` takes the owner's working day off the hall load, `DispatchHall` runs the owner as waiter number zero, and docs/14 forbids the owner from working in the kitchen. The field is not a *choice*, it is *an assumption written down*. Deleting it would make the assumption invisible; reading it would require writing a second pool, and the design does not want one. If another pool is written the game no longer opens.

### `Crew.SalonWorkMicro` -> **deleted**

The only real deletion. It carried the peak day's hall workload and was read nowhere — and **two of its four constructors were writing `0` to it.** So if somebody did go to read it, they would read the wrong value. The load can be recomputed from `peakCustomers` anyway.

**The lesson:** a field not being written does not make it deletable; but **if some of its call sites lie to it**, that field is no longer dead, it is a *trap*. Deleting is the only right fate.

The auditor's checks 1, 2 and 3 now find **nothing at all.**

---

## 17. Doner and pide — and the lock system the generator was deleting

The state the user wanted was this: *"pide yapmak istersek taş fırın gereksin. Döner için döner takılan tezgah gereksin."* (*"if we want to make pide then a stone oven should be needed. For doner, a counter with a doner spit on it should be needed."*) The stone oven was written in §10; doner and pide **were not in the content**, which is why they could not be wired up.

### The menu stays at 32: four in, four out

| Out | Why |
|---|---|
| `tavuk_kanat_izgara` | it reads as fast food; the `tavuk_kanat` ingredient left the Turkish list too |
| `firin_makarna` | it belongs to Italian cuisine; `kasar` was rebound to the new pide instead |
| `kiymali_ispanak` | the weakest of the eleven stews; the `ispanak` ingredient was deleted too |
| `kabak_dolma` | the same |

| In | Equipment | Season |
|---|---|---|
| `doner` | **`doner_ocagi`** | 2 |
| `iskender` | `doner_ocagi` | 4 |
| `kiymali_pide` | **`pide_firini`** | 3 |
| `lahmacun` | `pide_firini` | 4 |

All four are in the `izgara` group, that is, in the **main** role: they touch the backbone of the menu, not its edge. The only new ingredient is `doner_eti`. The season spread was not disturbed (13/8/6/5) and the progression curve stayed the same: 6 -> 13 -> 21 -> 27 -> 32.

The Turkish cuisine now carries three named pieces of equipment (stone oven 2,700, doner grill 1,860, pide oven 4,800 coins) with **eleven dishes** in total behind them. docs/09 wants ten stations per cuisine; we are somewhere between three and five.

### What was really found while writing this: the generator was deleting the lock system

`content/dishes/*.json` are **generated** files. But `requiresStationTier`, `unlockReputationCenti` and the named station names had been written straight into them — the generator `tools/content/gen_dishes.py` **did not know about them**.

So running `python tools/content/gen_dishes.py`:
- turned seven Turkish dishes' station back from `tas_firin` to `firin`,
- set `requiresStationTier` to 0 on **all** 64 dishes,
- deleted `unlockReputationCenti`.

So running the content generator **destroyed the biggest design job of that day — the reputation + equipment lock — in a single command.** No test broke, because the tests read the content as it stands at that moment. I only saw this by actually running the generator.

**Three things were fixed:**

1. The named stations are now in `gen_dishes.py`: the dish row writes its own station, and `CUISINE_STATIONS` says which equipment belongs to which cuisine.
2. The lock gates are **generated** inside `unlock_gates()`: the dishes are sorted (by complexity, by price) and split (the bottom 53% with no equipment, the next 28% tier 1, the top 19% tier 2 — 17/9/6 out of 32 dishes), the ones tied to named equipment are exactly tier 1, the opening menu is always 0; the reputation threshold is 0.9 points per day.
3. **The equipment's `opens` list is now derived**: `model.py` no longer keeps it by hand, `export.py` scans the dish files and extracts it. Which dish wants which piece of equipment is already written in the dish's own `station` field; keeping a second list was in this project exactly the thing that had just drifted apart.

`ContentTests.The_unlock_gates_follow_the_generators_rule` tests the same rule from the outside, and running the generator twice now gives **bit-for-bit identical** content.

### Balance: twice the bot was guilty, not the economy

The new content broke the calibration (penalty 0 -> 16) and both violations were the strategy's decision:

**The first — `genislemeyen`'s reputation fell from 85.1 to 50.7.** The cause: it had a loan, and `Equipment.Upgrade` bought **no** optional equipment while a loan was outstanding, so it could never buy the doner grill — and the customers were asking for doner every day (the "asking customer" mechanic from §6). It spent half of the sixty days saying "we don't have it". The rule changed: while a loan is outstanding, **only equipment that opens menu items** can be bought, with two weeks of fixed costs kept in reserve.

The second half of that rule came from measurement too: one week of reserve was not enough, `planci` missed its expansion schedule (12.5 tables, target 13) because it put the money into the grill and delayed the tier. At two weeks both were fixed.

**The result:** `makul` 30,364 -> **33,208** and its reputation 64.2 -> **100.0**; the growth multiplier 4.14 -> **3.80** (target band 1.8–4.0). **Both cuisines clean, penalty 0.** 153 tests.

**The lesson (a continuation of the seventh):** every field hand-written into a generated file dies on the generator's next run. This is the most insidious form of the "the same thing is written in two places" bug — because the two places do not drift apart, one of them **deletes** the other.

---

## 18. The signature mechanics: the only thing separating one cuisine from another

[07-cuisine-system.md](07-cuisine-system.md) had written this down as **"the most important line"**: *"this is what shows that buying one is a different game, not a repaint."* [23-…](23-core-contract.md) §8.2 had written the schema as well — a `signature` block inside `cuisines/*.json`, *"if the kind is unknown the validation rejects it, if the block is missing the cuisine does not load."*

**The block did not exist at all and the cuisines were loading without a problem.** So the two cuisines were the same game carrying different menus.

| Cuisine | Mechanic | What it does |
|---|---|---|
| fast food | **combo** | A party that chooses the combo's main course **definitely** takes the side and the drink too, pays one discounted price for all three, and that job ties the cook up **20% longer** |
| Turkish | **credit** | A frequent customer **asks** to have the bill put on the tab; if you open it your cash flow suffers, if you do not your satisfaction drops |

The mechanic is in the code, the numbers in the data. If the block is absent, if the `kind` is not recognised, if the combo **relieves** the kitchen (`kitchenLoadBp < 10000` — a free gain) or if the combo discount is not a discount, the game does not open.

### The mechanic arrives in the second season

[09-…](09-content-inventory.md): *"The signature mechanic arrives at the start of the second season. If it is put in the first, the tutorial load gets far too heavy, because the player is already learning the menu and the prices."* `SignatureDef.FromDay` carries that (season length + 1 = day 16) and the loader audits the combo's three items against that day: a combo that is locked when the mechanic arrives could never be sold.

### The tab was written wrong three times, and each time the measurement said so

**First attempt — a bonus button.** Opening a tab gave a satisfaction bonus. The measurement: `imzaci` 29,151, `makul` 32,676. The reason is simple and instructive: **the player who plays well is already at the reputation ceiling.** At 100/100 an +8-point satisfaction bonus buys nothing; all that is left is the cost of the bad debts.

**Second attempt — loyalty demand.** Every bill collected adds a permanent 0.6% to demand (capped at 15%). The measurement: customers served 2,331 -> 2,334. **Three people.** The reason is from the same class again: the player who plays well is **at the capacity limit**, the service rate is 97%, the share turned away at the door is 1%. Raising demand gives nothing to a restaurant whose tables are full.

**Third attempt — the right direction.** The mechanic was set up backwards: the tab is **not a bonus you offer, it is something that is asked of you.** A frequent customer (`TierIndex 0`) asks to have the bill put on the tab with 12% probability; if you do not give it, their satisfaction drops. Exactly the same idea as the "customer who asks for a locked dish" mechanic (§6), which was the user's own. The gain side was tied to money as well: whoever settles their bill **adds 12% on top**, and the person on the tab is given tea — tea raises the collection chance from 85% to 95% and its cost is the content's `teaCostCenti` (a field never read until now).

### The measurement: the mechanic is neutral at the ceiling, it pays in a TIGHT spot

| | gives the tab | turns it down |
|---|---:|---:|
| till | 19,270 | 17,999 |
| tab | 169 | — |
| reputation | 17.8 | 17.1 |
| loyalty | 12.0% | — |

This is a small restaurant **whose reputation is not at the ceiling**. The one who gives is 8% ahead of the one who turns it down. Across a full sixty-day campaign, against a `makul` saturated at 100 reputation, `imzaci` gets 30,465 + 1,883 on the tab = 32,348 against 32,987 — that is, **neutral**.

This is the same shape as what was found with staff experience (§15): **the mechanic helps the player who is squeezed; it does not touch the player at the ceiling.** And that is right — if it paid off for the player at the ceiling too, the mechanic would be an obligation, not a decision.

In fast food the combo pays off directly: the ticket 55.5 -> **56.2**, the till 26,167 -> **26,698**, but the reputation 99.2 -> 97.7 — the kitchen really is more tired. The trade is visible.

### A new target for the calibration

`imzaci` must stay **between 90% and 130% of** `makul` and must not fall into debt. The lower bound tests that the mechanic is not a trap, the upper bound that it is not an obligation. The measure is **till + tab**, because on the sixtieth day an uncollected tab is not lost money ([08](08-endgame.md), net assets). A **`tab`** column was added to the harness table for this.

**The lesson (the ninth's sibling): tying a mechanic's reward to an axis the player is already SATURATED on makes the mechanic invisible.** Reputation at 100, tables full. Two designs that paid out on those two axes were tried and both measured zero. The third was tied to money and to a PENALTY — and it worked. The first question when writing a new mechanic should not be "what does it give" but **"is there room in the player for what it gives"**.

---

## 19. Named regular customers

[11-customer-system.md](11-customer-system.md) had drawn the distinction clearly: *"a named customer is a single person, hand-written, with a story, and always the same person. An archetype produces thousands of customers."* [09](09-content-inventory.md) wants ten per cuisine, each with a three-or-four-beat story. The `content/regulars/` folder **did not exist at all.**

Now it does: `tools/content/gen_regulars.py`, ten people per cuisine. On the Turkish side Hasan Usta (the shopkeeper neighbour, `kuru_fasulye`, day 3), Nazife Teyze, Selim Bey, Rasim Amca…; on the fast food side Deniz, Burak, Elif…

### Their behaviour comes from the archetype, their identity from themselves

A regular customer **takes an archetype as its base** ([13](13-data-schemas.md)): patience, party size, price sensitivity, arrival time — all from there. That way the behaviour code follows a single path. There are three things that are their own:

| Field | What it does |
|---|---|
| `favouriteDish` | If it is on the menu they order it; if not, their satisfaction drops |
| `arrivesFromDay` | When they enter the campaign (spread from day 3 to day 47) |
| `veresiyeEligible` | Whether they can go on the tab in the Turkish cuisine |

### The most important invariant: it does NOT INFLATE the demand

A named customer is **not added** to the day's plan — they **take a place** inside it. `BindRegularsToPlan` binds an arriving regular to a plan row in their own archetype; failing that, it writes the earliest empty row in their name.

This is deliberate: otherwise every new name would grow the economy and **the calibration would shift with every content addition.** `RegularTests.They_DO_NOT_INFLATE_the_demand` compares the first three days — because the first regular arrives on day 3, days 1 and 2 are exactly identical, therefore day 3's reputation is identical too, and its plan must come out identical as well. On the following days the numbers diverge and **their divergence is correct**: a customer who finds their favourite dish leaves happier and the reputation works out differently. That difference is the mechanic itself.

### The tab found its identity

[13](13-data-schemas.md) had put the `veresiyeEligible` field in the regular customer's file, and that was right: **a tab is opened for somebody whose name you know.** In §18 this rule was being derived from "a frequent archetype" — an approach that works but has no identity. Now it comes from the content if the content is there; if not, the old derivation stands as a fallback (the unit tests run without the regular customer file).

A side effect of this was measured: because the candidate pool narrowed (seven people, each dropping in on half the days), the 12% asking rate meant it barely operated at all — three tabs in sixty days. The rate was raised to **40%**; it is now a daily decision.

---

## 20. Staff traits and morale

[14-staff-system.md](14-staff-system.md) had written twelve traits and a morale table. There was **no such thing as a trait** in the core.

### The traits: two of them looked free, the measurement asked

The generator enforces a balance rule — docs/14's own sentence: *"no trait is purely good or purely bad."* When the rule was first written it **caught two traits**: gets on well with customers (+8 satisfaction) and lifts the team's morale (+10 morale). Neither has any cost at all in the effect table.

But they **do** have costs, only not inside the trait — inside the **pool**: each of them has an evil twin (sullen, cantankerous) and the two conflict. Hiring draws one of the two — so a good trait is not an advantage you choose, it is a **roll of the dice**. The rule was written accordingly: *either a trait has a cost within itself, or a trait it conflicts with is its mirror.*

The conflicts are also audited at load time for **symmetry**: if A conflicts with B then B must conflict with A. A conflict written one-way would leave a rule silently not working in the hiring code — that is, a member of staff carrying two conflicting traits could be generated.

### Speed is now collected into a single multiplier

Experience + trait − morale − busyness − fatigue. All in the same place, because all of them say the same thing: how quickly does this person finish this job. The floor is 20% — so that three bad traits stacking up do not stop the person entirely.

**The busy slot is derived from the cuisine**, not a fixed "second slot": [28](28-peak-decision.md) Decision G changed not the shares but the **durations**, so the kitchen's peak is written in the slot length. Writing it fixed would have forced the Turkish restaurant's lunch peak onto fast food too.

### Morale: the table is an EVENT list, not a drift model

The first implementation took docs/14's table literally and the measurement rejected it: **even in a well-run shop the whole crew resigned within a month.** The cause was not a gap in the table but **what the table is**: docs/14 gives an event list (wages +5 / −25, a busy day −3, a day off +10, a raise +15), and half of those are things the player has to *do*. Applying only the events makes the ladder go one way: down.

Two fixes:

1. **A quiet day recovers morale** (+2), but it does not go **above** the starting morale. So quiet days do not make the staff happy, they only bring them back to normal; going above that needs the player to do something.
2. **"A busy day" is not an absolute threshold but a threshold relative to the crew.** The first definition was "three parties per table" and in a well-run shop every day counted as busy. The right one: it is busy if the kitchen worked above its **design capacity**. The same number of customers is calm with two cooks and exhausting with one.

### And a constructor bug

The starting crew was **not getting** a trait or morale — `RollTraits` was only called from inside `Hire`, whereas the game starts with one cook. That cook's morale started at **0**, that is, below the resignation threshold: the restaurant was left **without a cook on the second day**. Without the daily trace this bug would have been misdiagnosed as "the morale system is too harsh".

**The lesson:** if a field's default is `0` and `0` is a meaningful value (here, "zero morale"), every path that constructs that field has to be asked about separately. The `Hire` path was right; the constructor path did not exist at all.

---

## 21. The trait system broke the balance, and three times the cause turned out to be the BOT

When the traits were coded, the good player's reputation in fast food dropped **from 96.5 to 87** and in some runs the shop emptied out. The loss of balance was real; its cause was not the economy.

### How the measurement was narrowed down

Instead of guessing, I **turned each effect off one at a time** and ran it:

| Only this effect on | `makul` reputation |
|---|---:|
| none | 96.5 |
| `speedBp` | 95.7 |
| `peakPenaltyBp` | 95.5 |
| `fatiguePenaltyBp` | 98.6 |
| `cleanlinessBp` | 96.5 |
| `satisfactionCenti` | 97.0 |
| `xpBp` | 99.9 |
| **`wageBp`** | **86.9** |

A single field. And `wageBp` itself is harmless — the wage per person is **+1.2%** on average. What does the damage is the **variance**: threshold-based decisions (taking a loan, buying equipment, expanding) are one-way, so a bad draw sets you back permanently while a good draw cannot put you ahead of schedule.

### What was missing was not a mechanic but a DECISION

docs/14 had already written the answer and I had coded half of it: *"Three candidates appear at once on the hiring screen. The candidates are generated: role, two traits, appearance, name. The candidate pool refreshes every three days. You can reject a candidate you do not like, but a new one does not arrive straight away."*

Without a pool, a trait is a **lottery**: you draw an expensive crew and you live with it. With a pool it is a **decision**: you look at three candidates and you choose.

The pool was written (`CandidateSlots = 3`, refreshing every three days, the slot of a hired candidate not filled immediately) and two things were added to the balance tool:

1. **`Hiring.Pick`** — the highest speed + satisfaction − wage among the candidates.
2. **`Hiring.ReplaceWorst`** — if the person you have is markedly worse than the candidate at the door, replace them. The swap has a price: the leaver's experience is reset.

The second one **never fired** in its first version — I had set the threshold at 3,000, while the total spread of the trait scores is ~4,700. Once the threshold came down to 1,200:

| | traitless baseline | traits on, no bot | traits + **selection** |
|---|---:|---:|---:|
| `makul` till | 24,540 | 20,886 | **28,617** |
| `makul` reputation | 96.5 | 87.5 | **99.3** |
| `genislemeyen` reputation | — | 61.0 | **71.3** |

So the trait system does not break the balance when the player **manages their crew** — it **improves** it.

### The starting cook has no trait

One more thing came out: the player does not **choose** the starting cook, the game gives them one. Assigning them a random trait means throwing an invisible die on the campaign's first day — and in the measurement, a run that drew a badly-traited starting cook leaked satisfaction from 9,000 to 5,000, the shop stayed at four tables and could not recover in sixty days. The cook you inherit is **ordinary**; character comes with the people **you choose**.

### And a target started measuring wrongly, because it hit the ceiling

The calibration said "intervening must raise the reputation". Once `makul` learned to manage its own crew and climbed to **99.3**, there was nowhere left for the intervention to raise and the mechanic looked like it "does not work" — while in the same run, in the Turkish cuisine, the interventionist was at 100.0 and `makul` at 96.1, that is, **it works where there is headroom.**

The target was changed from "always better" to "never worse" (a 3-point tolerance). This is the same lesson as §18's, this time on the *measuring* side: **measuring a mechanic on an axis the player is already saturated on convicts it unjustly.**

---

## 22. The Unity view layer: the first slice

A sixty-day economy, two cuisines, two signature mechanics, named customers and staff with traits — all of it existed and **none of it was on screen**. Under `unity/Assets/Lokanta/` there were only four editor scripts.

This slice does not make the game playable; it makes it **visible**. The aim is to open up the biggest unknown: is this fun?

### The content was moved behind the platform

The loader was reading a file path. On Android there is no such thing as a file path: the content is inside the APK. The `IContentSource` port was written ([26](04-architecture.md)'s "the platform sits behind a port" rule) and it has two implementations:

| Source | Where |
|---|---|
| `DirectoryContentSource` | tests, the balance tool, the editor |
| `ResourcesContentSource` | the game; Unity Resources, **synchronous** on every platform |

StreamingAssets was not chosen because on Android it is only read through `UnityWebRequest` and **asynchronously**; content loading is a synchronous operation (it validates, it throws) and making it asynchronous would infect the whole chain. The price is that `content/` is copied under `Assets/Resources/content/` — **Lokanta > Copy the content to Resources**, and beside it an **Is the content copy up to date** check. A stale copy makes the balance look as though it has changed, and finding out why takes hours.

### The floor plan moved to runtime — and was tested for the first time

The plan lived only inside `Editor/RoomLayout.cs`, that is, **the runtime did not know the plan**. Writing the same numbers in two places has drifted apart silently four times in this project; the plan was moved to `Lokanta.Game.RoomPlan` and the editor tool now **derives** from it (the only thing left there is the colour).

Something else happened when it moved: the plan entered `Lokanta.Core.csproj` and became **testable**. `RoomPlanTests` asks six things and all of them pass:

- the rooms cover the plot **exactly** (172.80 m² = 172.80 m²)
- the rooms do not overlap and do not spill outside the plot
- the table counts are **the same as the tier table**: 4 / 7 / 10 / 14 — the plan and the economy have to say the same thing, otherwise the player cannot see in the hall the table they bought
- every hall room gets its tables (without `Fit()`'s epsilon a column disappears)
- the table points are inside their own room
- the nearest two tables are 1.70 m apart — the touch targets do not overlap

All of this used to be tested **only when Unity was opened**.

### And the scope of the `float` ban was written down

When the plan entered the core assembly, [23](23-core-contract.md) §2.5's reflection test caught it: `PlotW : Single`. The rule is right and it was not loosened — **its scope was written down**: the scan now only walks the `Lokanta.Core.*` namespace.

The distinction is this: determinism belongs to the **simulation**, because the same command sequence has to give the same result on a phone and in the balance tool. The floor plan does not enter the result, it says where something stands on screen; writing an 18.0 x 9.6 m plot in integers would mean converting the measurement to centimetres and dividing everywhere, and would gain nothing. **Unless this distinction is written down**, one day somebody will loosen this test in order to get a float into the simulation.

### What was written

| File | Job |
|---|---|
| `GameHost` | Loads the content, sets up `Simulation`, drives the **fixed-step** tick loop |
| `RestaurantView` | Builds the rooms and the tables, updates them every frame by **reading** from the simulation |
| `CameraRig` | A two-tier camera: the overview <-> zooming into a room |
| `HudView` | Day, till, reputation, phase buttons |
| `Editor/BuildGameScene` | **Builds** the scene — a hand-built scene turns into a file where nobody remembers who bound what to where |

**The fixed step is the most important line.** Unity's variable `deltaTime` never enters the core; there is an accumulator in `GameHost` and that is all. It is no longer **discarded**, it is accumulated — discarding would shorten the day on a slow frame. At most 400 ticks are processed in one frame, so that a late frame does not produce a "death spiral".

The view layer **reads** the simulation, it does not write to it; the only thing that writes is a command. Saving and replay are built on that.

### What was not done

This slice is a box-prism placeholder. There is no customer figure, no animation, no real interface (the HUD is deliberately IMGUI — its job is not design but visibility). Nor was the touch target turning into the **table set** when you zoom into a room written; the camera zooms but the target is still the room. [16](16-screens-and-tutorial.md)'s fourteen screens and [24](24-art-pipeline.md)'s mesh pipeline are ahead of us.

**This layer was not compiled here.** Because it depends on `UnityEngine`, `dotnet build` does not see it; the first compile happens when Unity is opened.

---

## 23. What is next

- ~~Ingredient quality~~ **written** (§9).
- ~~`orderPreference` is dead~~ **written, by derivation** (§11).
- ~~The special equipment should be named~~ **the mechanism was written** (§10). What is left is **content**: dishes like doner and pide have to be added to the menu so that `doner_ocagi` and `pide_firini` can be bound to them. docs/09 plans 10 stations per cuisine; right now the Turkish cuisine has 1 and fast food 2.
- ~~The auditor's blind spot~~ **closed**: the third check was written and it found 19 fields (§8).
- ~~The owner's intervention~~ **its constraints were written** (§12), and the fourth kind too (§13).
- ~~Speeding up a station~~ **written** (§13); `InterventionKind` carries four values.
- ~~Market price volatility~~ **written** (§14).
- ~~Staff experience~~ **written** (§15).
- ~~`weeklyWageMultiplierBp`, `unlockSeason`, `ownerPool`, `SalonWorkMicro`~~ **closed** (§16).
- **The remaining queue is now content and interface work**, not dead systems:
  - ~~doner/pide~~ **written** (§17). The number of named pieces of equipment is 3 in the Turkish cuisine and 2 in fast food; docs/09 wants 10 per cuisine, so there is still content work.
  - the auditor's 4th check counts 63 fields that are in the docs/13 schema and not in the generated content. These are not dead fields but **content that has not been written**: the schema describes a wider game than the one the balance tool produces.
  - ~~the room view~~ **its first slice was written** (§22): a two-tier camera and the floor plan in the scene. The layout screen ([16](16-screens-and-tutorial.md) screen 14) and the touch target turning into the table set when you zoom into a room are still not written.
  - ~~the signature mechanics~~ **written** (§18); the Italian (`courses`) and Japanese (`broth`) blocks are ready on the schema side and will be coded when those cuisines are written.
  - ~~named regular customers~~ **written** (§19).
  - ~~staff traits and morale~~ **written** (§20).
  - **business upgrades** (`content/upgrades.json`, [13](13-data-schemas.md)) and **the year-end scoring** (`scoreAxis`, [08](08-endgame.md)) are still not written.
  - **The Unity view layer** — the next big job.
- **`SalonWorkMicro`** is read nowhere in the core; it must either be used or deleted.

