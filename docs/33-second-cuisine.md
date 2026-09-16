# 33 — The second cuisine: the Turkish restaurant was run for the first time

10 September 2026. The whole of Phase 0's balance solution, all of its tests and all of its measurements had been done with **one cuisine**: fast food. The second cuisine had never been run.

The result of the first run:

| strategy | final till | served | at the door |
|---|---:|---:|---:|
| `pasif` | -7,271 | **0** | 100% |
| `sadece_hal` | -7,893 | **0** | 100% |
| `makul` | -7,434 | **0** | 100% |
| `genislemeyen` | -7,434 | **0** | 100% |
| `atilgan` | -41,187 | **0** | 100% |
| `planci` | -23,981 | **0** | 100% |
| `yuksek_fiyat` | -7,434 | **0** | 100% |
| `fazla_kadro` | -20,123 | **0** | 100% |

**All eight strategies went under with zero customers.** Not a single party was served, not a single coin of revenue was made.

---

## 1. The cause

Dish groups are **cuisine-specific**. [13-data-schemas.md](13-data-schemas.md) designed it that way deliberately: the example dish is `kuru_fasulye` and its group is `sulu`, and the example archetype's preference is `{ "sulu": 0.6, "pilav": 0.25, "corba": 0.15 }`.

The simulation, on the other hand, had **hard-coded** fast food's dictionary:

```csharp
private const string GroupMain = "ana";
private const string GroupSide = "yan";
private const string GroupDrink = "icecek";
```

The Turkish restaurant has no `ana` group at all. Its groups are: `sulu` (11), `corba` (4), `pilav` (5), `izgara` (5), `meze` (3), `tatli` (3), `icecek` (1).

So every customer came to the door, looked for a main dish on the menu, could not find one, and turned away. Forever.

**This bug is not visible by looking at the code.** Both files are correct in themselves: the dish content matches the schema, the simulation compiles and works flawlessly on fast food. The contract between them was never written down.

---

## 2. The fix

`content/cuisines/<id>.json` now carries the **menu roles**: which group is the main dish, which is the side, which is the drink, which stands in for dessert.

| Cuisine | main | side | drink | dessert |
|---|---|---|---|---|
| fast food | `ana` | `yan` | `icecek` | `tatli` |
| Turkish | `sulu`, `izgara` | `corba`, `pilav`, `meze` | `icecek` | `tatli` |

The core no longer knows group names, it knows **roles**. `ContentSet.MainGroups` comes from the content.

### The validation is hard, and it is in two places

So that the same bug cannot come back silently:

**At generation time** (`tools/balance/export.py`) — every group in the dish file must fall into exactly one role, no group may fall into two roles at once, no role may be given to a group that does not exist, and there must be at least one main dish open on the first day.

**At load time** (`ContentSetLoader`) — the same four checks, because [23-core-contract.md](23-core-contract.md) §9.1 requires that the game **not open** if the content is invalid. No silent defaults.

**At test time** (`tests/CuisineTests.cs`) — five tests, run separately for the two cuisines: does every group fall into exactly one role, can an order be placed on the first day, does a service day actually serve customers, **is an order placed from all four roles**, do the slot durations divide exactly into ticks. **When a new cuisine is added it has to be added to the list.**

---

## 3. After the fix

The same parameters, the same rents, the same equipment ladder — nothing was tuned per cuisine:

| strategy | final till | reputation | tables | served | lost | first debt |
|---|---:|---:|---:|---:|---:|---:|
| `pasif` | -6,297 | 0.0 | 4 | 13 | 0 | **35** |
| `sadece_hal` | 5,094 | 74.3 | 4 | 1,008 | 59 | — |
| **`makul`** | **30,745** | 99.6 | **8.8** | 2,209 | 45 | — |
| `genislemeyen` | 13,661 | 95.2 | 4 | 1,184 | 46 | — |
| `atilgan` | -66,054 | 0.0 | 14 | 247 | 14 | **7** |
| `planci` | 19,331 | 100.0 | 14 | 3,104 | 32 | — |
| `yuksek_fiyat` | 12,468 | 0.8 | 4 | 545 | 4 | — |
| `fazla_kadro` | 4,466 | 93.8 | 4 | 1,190 | 48 | — |

**All the design targets hold in Turkish cuisine too:** the passive player goes under on day 35, the reckless expander on day 7, growing pays 2.25× (13,661 → 30,745), the planner hits the schedule, and high prices and an oversized crew are punished.

This is an important result: **the economy is not over-fitted to a single cuisine.** The rents, capacities and equipment prices were solved with fast food and they work on the second cuisine with no tuning.

### The differences between the two cuisines

| | fast food | Turkish |
|---|---|---|
| Slot ticks | 960 / 1440 / 960 / 1440 | 576 / **2304** / 1200 / 720 |

*These two rows are what was decided on the day. The content has moved since:
`python tools/balance/export.py` prints `[1200, 864, 1680, 1056]` for fast food and
`[576, 1344, 2160, 720]` for Turkish (measured 17 September 2026). The table is left
as it was written - it is the record of a decision, not a reading of today's content -
but do not quote it as a current number.*
| Tables the `makul` player reaches | 7.0 | **8.8** |
| The `makul` player's final till | 33,251 | 30,745 |
| Growth multiplier | 2.41× | 2.25× |
| Oversized crew | **goes under on day 56** | does not go under, finishes with 4,466 |

All the differences come out of the sharpness of the peak. The Turkish restaurant's lunch slot is 48% of the day; in that window an oversized crew **pays off**, while on fast food's flatter day it is just wages. This is the first measured evidence of the "the cuisines should play differently" goal [10-cuisine-identity.md](10-cuisine-identity.md) asks for.

---

## 4. The second piece of dead content the same sweep turned up: dessert

Once the menu roles were written, the `tatli` role was defined too, and at that moment it became visible that **dessert was never being ordered.**

`PickOrder` was picking three items: the main dish for certain, 30% a side, 40% a drink. It never looked at dessert. So:

- **6** dessert dishes in fast food and **3** in the Turkish restaurant could never be sold
- [27-time-model.md](27-time-model.md) §3.3's peak table gives dessert 0.08 concurrent plates — that row was running for nothing
- the **dessert station's equipment upgrade** that I wrote in [32-equipment-and-rebalance.md](32-equipment-and-rebalance.md) could be bought but did nothing at all

A fourth item was added: `dessertChanceBp = 1800`, lower than the drink because dessert comes at the end and not everyone takes one. The maximum number of jobs per party rose from 3 to **4** — four items can go to four different stations.

The measured effect (five days, two cuisines):

| | main | side | drink | dessert |
|---|---:|---:|---:|---:|
| fast food | 70 | 12 | 45 | **6** |
| Turkish | 62 | 15 | 21 | **6** |

The effect on revenue is visible too: `planci` rose from 16,880 to **20,720**. `CuisineTests.An_order_is_placed_from_every_role` now verifies for each cuisine separately that all four roles are ordered from.

That the Turkish restaurant's drink count is half of fast food's is no coincidence: there is **only one drink** (tea) and stock limits it. The open item below is exactly what will solve this.

---

## 5. Left open: `orderPreference` still has not been written

[13-data-schemas.md](13-data-schemas.md) designed an order preference per archetype:

```json
"orderPreference": { "sulu": 0.6, "pilav": 0.25, "corba": 0.15 }
```

The simulation does not read it. Instead it uses a fixed model: **the main dish for certain, 30% a side, 40% a drink** (the `order` block in [12-economy.md](12-economy.md)). The menu roles made that model work on the second cuisine, but they did not implement the designed model.

The cost of that, visible today: the Turkish restaurant has **a single drink** (tea) and 40% of the customers take it. With an archetype preference, the tradesman neighbour could pick ayran and the student a cola.

A second cost: because `sulu` and `izgara` fall into the same role, they are picked with equal probability. In a real restaurant the stews dominate; docs/13's own example says 60%.

**This is the next slice's job.** The roles do not block it; on the contrary, they are the ground it will be built on.

---

## How to run it

```powershell
dotnet run --project src/Lokanta.Harness -- --cuisine turk
dotnet run --project src/Lokanta.Harness -- --cuisine fastfood
```

**Every run that changes the balance has to run both.** This document is the record of what measuring with a single cuisine cost.
