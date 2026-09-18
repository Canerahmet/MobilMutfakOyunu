# 32 — The equipment system and rebalancing the economy

10 September 2026. Phase 0's one remaining balance warning is closed: **for the reasonable player, money was ceasing to be a problem in week 5.5, and the target was for that not to happen before week eight.** [29-phase0-simulation.md](29-phase0-simulation.md) had written that this was not a balance problem but missing content. That was right, but what was missing was not only content.

---

## 1. What was written

[27-time-model.md](27-time-model.md) took **Decision D** back in March, but the simulation had never implemented it:

```
prepMs      the dish's WALL-CLOCK duration, the number the player sees
cookBusyMs  prepMs × attendBp / 10000, the number that consumes the cook pool
```

The simulation was keeping the cook busy for the dish's **entire wall-clock time**. So there was no difference at all between an oven and a grill, the `station` field was nothing but an icon picker, and the equipment had no story to tell.

Now there are three things:

| Concept | What | Where |
|---|---|---|
| **Station slot** | How many plates a station takes at once | `Simulation._stationBusy` |
| **`attendBp`** | What percentage of the wall-clock time is spent in the cook's hands | `content/equipment.json` |
| **Equipment tier** | Adds a slot or lowers `attendBp`, and **does not touch** `prepMs` | `CommandKind.BuyEquipment` |

An order is split into station jobs: the main course to the grill, the drink to the drinks station. Items going to the same station merge into one job. A job decides how many plates to cook at once by the smallest of three limits:

```
take = min(the party's plate count, free slots, the plates a cook can attend to at once)
```

**The third limit was added later and it had to be.** Without it, a party of four held four slots at once while the cook attended to them one after another; the slots looked full while doing nothing. The measurement caught this: the satisfaction of a kitchen that bought equipment was **falling** from 87 to 81. The limit is `ceil(10000 / attendBp)` and it gives the same number as [27-time-model.md](27-time-model.md) §3.3: at the tier-4 peak, 8.66 simultaneous plates and 4 cooks, so 2.2 per cook.

### The equipment ladder

`content/equipment.json` is a **generated file**; `tools/balance/export.py` writes it. The prices are not set by hand, they are derived **from the rent of the tier at which the item becomes necessary** — and since the rent was already solved from the mature-week margin target, it carries the economy's scale.

<!-- GENERATED: equipment -->
| Station | `attendBp` | Tier | Slots | `attendBp` | Price | Needed at tables |
|---|---|---|---|---|---|---|
| Stove | 3500 | t0 | 1 | 3500 | — | 4 |
|  |  | t1 | 2 | 3500 | 2,340 | 7 |
|  |  | t2 | 3 | 3500 | 3,480 | 10 |
|  |  | t3 | 4 | 2800 | 10,000 | 14 |
| fritoz | 3500 | t0 | 1 | 3500 | — | 4 |
|  |  | t1 | 2 | 3500 | 2,340 | 7 |
|  |  | t2 | 3 | 3500 | 3,480 | 10 |
|  |  | t3 | 4 | 2800 | 10,000 | 14 |
| Grill | 5600 | t0 | 1 | 5600 | — | 4 |
|  |  | t1 | 2 | 5600 | 2,340 | 7 |
|  |  | t2 | 3 | 5600 | 3,480 | 10 |
|  |  | t3 | 4 | 3500 | 10,000 | 14 |
| Oven | 2000 | t0 | 1 | 2000 | — | 4 |
|  |  | t1 | 2 | 2000 | 6,000 | 14 |
| Cold | 10000 | t0 | 1 | 10000 | — | 4 |
|  |  | t1 | 1 | 8000 | 3,480 | optional |
| Drinks | 10000 | t0 | 1 | 10000 | — | 4 |
|  |  | t1 | 1 | 6500 | 3,480 | optional |
| Desserts | 8000 | t0 | 1 | 8000 | — | 4 |
|  |  | t1 | 1 | 6000 | 3,480 | optional |

The whole ladder is **63,900 coins**.
<!-- /GENERATED: equipment -->

The last tier is 10,000 coins — inside the 8,000-12,000 band §7 of [12-economy.md](12-economy.md) asks for. The whole ladder is **48,080 coins**; the reasonable player's sixty-day net is **42,627**. **So they cannot buy everything and have to choose** — and the mandatory ones (the ones that add slots) are 37,640, the optional ones (the ones that release the cook early) 10,440. That was exactly the target.

---

## 2. The second problem this change opened

Once the cook stayed busy only for `attendBp` rather than for the whole dish, **the kitchen stopped being the bottleneck.** Every strategy's service rate went up.

And that led to a very familiar bug: the most expensive bug on record in [06-plan-status.md](06-plan-status.md) was the closed-form model assuming that all demand is served and the rents being solved from that assumption. The measured realisation rate had come out at 65%, and the rents had been lowered because they were 35% too high.

Once the kitchen was fixed, **the same measurement gave 93.35%.** So the rents were no longer too high, they were **too low**.

### Why a one-shot measurement is wrong

I applied that rate directly and had the rents re-solved. The result was a disaster:

| Strategy | With the rent at 65% | With the rent at 93% |
|---|---|---|
| reasonable | +32,398 | **−33,958** |
| planner | +10,526 | +1,084, in debt on day 32 |
| non-expanding | +12,537 | +11,155 |

Expanding turned into a trap. The reason is simple and important:

> **The rate determines the parameters, and the parameters determine the rate.** The 93.35% measurement had been taken with the OLD cheap rents. Once the new rents were applied the same strategy could not expand and the rate collapsed.

This is a fixed-point problem, and it cannot be solved in one shot.

### `tools/balance/calibrate.py`

It was written for this. For a candidate realisation rate it runs the whole chain:

```
solve.py  →  model.py  →  export.py  →  the simulation  →  the design targets
```

and scores the result. The targets are not absolute numbers but an **ordering**: the passive player must go under, the reckless expander must go under, the reasonable player must win, growing must be rewarded by 1.8-4.0×, the planner must be able to meet the schedule, and money must not become irrelevant before week eight.

```
 6500  rent [650, 1550, 2250, 4000]   penalty 11
 7000  rent [850, 1950, 2900, 5000]   penalty  0   CLEAN
 7500  rent [1050, 2350, 3600, 5250]  penalty 14
 8000  rent [1250, 2750, 4250, 6250]  penalty 14
 8500  rent [1400, 3150, 4900, 7300]  penalty 24
 9000  rent [1600, 3550, 5600, 8350]  penalty 36
 9335  rent [1700, 3800, 6050, 9050]  penalty 36
```

**The fixed point is 7000.** Neither the old 65% nor the measured 93%. Everything in between was eliminated by measurement.

The new parameters: rent **850 / 1,950 / 2,900 / 5,000**, capacity scale 1.00 (cook 30, waiter 26, dishwasher 48, cashier 70), the owner's contribution 1.3 person-days, expansion prices 0 / 2,500 / 4,500 / 8,000 — that is, the capacity and the expansions did not change, **only the rent went up by 31%.**

---

## 3. The yardstick itself was wrong too

The yardstick for "money stopped being a problem" was this: *the till exceeds three times the most expensive expansion.*

Before the equipment was written it was right. Afterwards it was wrong twice:

1. It did not count the equipment at all. The player had something to save up for, but the yardstick said "it became irrelevant".
2. The equipment was added but "three times" stayed arbitrary: if you have 30,000 in hand and there are two 20,000 pieces of equipment left, money is still important.

The right definition: **if the till can pay for everything that is left to buy in one go**, then there is nothing left to save up for. `Simulation.RemainingPurchaseCost()` gives that: the remaining expansion tiers plus the remaining equipment steps.

With that definition the measurement **does not fire in any strategy.** Across the sixty days there is always something left to buy.

---

## 4. The result

| strategy | final till | reputation | tables | served | lost | first debt | irrelevant |
|---|---:|---:|---:|---:|---:|---:|---:|
| `pasif` | -6,638 | 0.0 | 4 | 13 | 0 | **35** | — |
| `sadece_hal` | 4,753 | 66.0 | 4 | 976 | 58 | — | — |
| **`makul`** | **32,398** | 98.4 | 7 | 2,161 | 35 | — | — |
| `genislemeyen` | 12,537 | 90.4 | 4 | 1,122 | 41 | — | — |
| `atilgan` | -70,952 | 0.0 | 14 | 274 | 7 | **7** | — |
| `planci` | 8,892 | 75.2 | 14 | 2,966 | 30 | — | — |
| `yuksek_fiyat` | 7,009 | 0.0 | 4 | 510 | 6 | — | — |
| `fazla_kadro` | 1,838 | 80.5 | 4 | 1,118 | 49 | **56** | — |

**Every design target holds, the penalty is zero:**

- Neglect has a price: the passive player goes under on day 35.
- Reckless growth is punished: the bold one falls into debt on day 7.
- Growing is rewarded by **2.58×** (12,537 → 32,398), inside [12-economy.md](12-economy.md)'s "three times, not eleven" band.
- The planner's schedule can now be **met**: 14 tables, 94% service, 2,966 people. Before the equipment it was jamming at 7.4 tables.
- Overstaffing sinks you on day 56.
- A high price survives but earns a fifth of what good play earns, and its reputation is zero.
- **Money does not become irrelevant in any strategy, in any week.**

Model consistency checks 20/20, tests 106/106.

---

## 5. The three bugs the measurement caught in this round

Not one of them was visible by reading the code; all three came out of the numbers.

| Bug | How it showed up | Why it mattered |
|---|---|---|
| **The job was queueing on a single slot** | Buying equipment changed nothing | If the number of slots does not affect the duration, there is nothing in return for selling equipment |
| **A job was holding more slots than the cook could attend to** | The satisfaction of a kitchen that bought equipment was **falling** 87 → 81 | The upgrade was punishing the player |
| **Average satisfaction was the wrong yardstick** | A kitchen with equipment serves more parties but looked like it had "low satisfaction" | That average is only over the customers who **were served**; a narrow kitchen never serves the hard cases and its average comes out high |

The third one is the most insidious: because the metric was wrong, a system behaving correctly looked wrong. The right yardstick is the number of parties served and the revenue.

A test had also been lying: it said `NewSim(cooks: 3, salon: 4)`, but at four tables the crew cap is three, so the fourth hall worker was being silently rejected and "a strong crew" actually meant nothing but two extra cooks. The test now verifies its own setup explicitly.

---

## 7. The store: cold storage and `spoilDays`

The store room had no job, and [31-rooms-and-camera.md](31-rooms-and-camera.md) said "if it cannot be given a job it should come out of the layout". The job was found and it had been there all along.

**`spoilDays` was in the content but the simulation never read it.** Perishable ingredients' shelf lives range from 1 to 45 days. The code was this:

```csharp
if (_content.Ingredients[i].Perishable) _stockGrams[i] = 0;
```

An onion that keeps for twenty days and minced meat that keeps for one were going in the bin on the same night.

This is not a bug, it is **a designed foundation**: §3 of [12-economy.md](12-economy.md) says "a perishable ingredient loses its whole value when the day closes". That is why cold storage is not a *fix* but an upgrade that **changes** that foundation.

### The ladder

<!-- GENERATED: storage -->
| Tier | `keepBp` | Ingredients saved | Price |
|---|---:|---:|---:|
| t0 | 0 | 0 / 36 | — |
| t1 | 2500 | 13 / 36 | 2,340 |
| t2 | 10000 | 33 / 36 | 3,480 |

The whole ladder is **5,820 coins**. Perishable ingredients: **36** items.

A tier only saves an ingredient if it can raise its life to **2 days**: a life of 1 and a life of 0 go in the bin the same night.
<!-- /GENERATED: storage -->

**Two steps, not three.** The ladder had three steps for a while and the
third one **did not work at any price**: at 8,000 coins it was never
bought (the cautious rule wants a till of 32,000, and the reasonable
player peaks at 25,000), and when it was lowered to 4,500 it does get
bought and **loses 3,700**.

The reason is not the price but the **calendar**. After the second step there is about 4,700
coins of annual spoilage left, and the third step saves part of that; in a
sixty-day campaign no price can make that pay. Lowering it further is not
a solution either — at that point it stops being a **decision** and turns into
an automatic purchase.

To even out the steps, the first step had been lowered too (keepBp 1500)
and it got **worse**: what it gained the reasonable player fell from +2,720 to −234.
The first step had been well tuned all along; the only broken ones were the upper two.

Age is kept per ingredient and a purchase takes a **weighted average**. A simple "reset on purchase" rule opened an exploit: you could buy one gram a day and keep the clock at zero forever.

### Why this job is more than decor

Menu width is a **two-way** axis. Because everything perishable dies at night, every dish left on the menu has to be restocked every day and whatever is left over goes in the bin — so a wide menu is expensive. But a narrow menu is not free either: of the main courses that are open, a customer **asks** for one that is not on the menu and their satisfaction drops when they cannot find it (`Simulation.Awaited`).

That second half was **missing** for a long time, and it was the game's deepest balance bug: the penalty only applied to dishes *whose equipment you did not have*, and a dish taken off the menu was never counted as having been asked for. It was measured: a player keeping a single main course on the menu was beating the reasonable player by 12% in fast food and by 38% in Turkish cuisine. So a narrow menu was a **strictly dominant strategy**, cold storage's second reward was worthless, and the reason for a thirty-two dish inventory to exist had vanished.

The penalty is **proportional**: if half of the open main courses are not on the menu the penalty is half, if none of them are it is full. An absolute number was tried and it was excessive — it put a player who narrowed their menu reasonably in the same basket as one keeping a single dish, and bankrupted the second on the ninth day. The `tek_yemek` strategy stands in the balance tool as an **acceptance test**.

Cold storage loosens that constraint, which means it **buys menu width** — and it becomes the reason the thirty-two dishes of [09-content-inventory.md](09-content-inventory.md) exist. The content inventory, the market stage and the equipment ladder were disconnected from one another without this piece.

Its measured effect: the `planci` strategy went from 8,892 to **16,880**, its reputation hit 100, and it served 3,114 people instead of 2,966.

### The store room

The store is a small **13.4 m²** back room stuck to the kitchen's right edge; inside it are the cold room and the dry shelf, and the two correspond to the perishable / non-perishable split in the ingredient list. The fridge in the kitchen was removed — cold storage is the store's job now, and showing both would have been a lie.

---

## 8. Open items
- **The store is still close to the touch limit:** 51 dp in the general view against a 48 minimum. It cannot be made any smaller.
- **There is no equipment interface.** [16-screens-and-tutorial.md](16-screens-and-tutorial.md) screen 14 says "Upgrades and equipment" but its content was never written. Touching the kitchen in the room view should open onto this.
- ~~The second cuisine (Turkish) was not measured.~~ **It was measured and it came out broken**; the cause was not the equipment but the menu groups. See [33-second-cuisine.md](33-second-cuisine.md). After the fix the same equipment ladder works without adjustment.
- **`attendBp` falls with the equipment tier but does not fall with staff experience.** [27-time-model.md](27-time-model.md) §595 left that open, and it is still open.
