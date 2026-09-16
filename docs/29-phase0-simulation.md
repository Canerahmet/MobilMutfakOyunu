# Phase 0, Second Slice: the Simulation and the Balance Tool

**Last updated:** 10 September 2026
**Status:** Working. 88 tests pass, the balance tool runs the sixty-day campaign.
**Previous slice:** the Phase 0 table at the end of [06-plan-status.md](06-plan-status.md)

---

## 1. What was done

| Part | Where | Status |
|---|---|---|
| The Unity project, targeting Android | `unity/` | Set up; the settings are applied by code |
| Fixed-step simulation | `unity/Assets/Lokanta/Core/Sim/Simulation.cs` | Runs a service day from start to finish |
| Command and event types | Same folder | 17 commands, 18 kinds of event |
| Timing configuration | `TimingConfig.cs` | Derived from the capacity model |
| Content loader, dishes and archetypes | `unity/Assets/Lokanta/Content/` | With validation |
| The balance tool | `src/Lokanta.Harness/` | Five strategies, multi-seed |
| Tests | `tests/Lokanta.Core.Tests/` | 88 tests |

**The Unity project is configured by code.** The `Lokanta/Proje ayarlarini uygula` menu item, or `tools/unity/run.ps1` in batch mode. No setting is clicked by hand; it repeats.

The settings applied: linear colour space; graphics API with Vulkan first and OpenGL ES 3 as the fallback; IL2CPP; ARM64; Android 10 as the floor; ASTC texture compression; AAB output; landscape orientation only; the accelerometer off; the URP asset created and assigned; HDR off; additional light shadows off; **the GPU Resident Drawer off**.

That last item was the review's correction, and it overrides the "On" row in the B4 table of [19-technical-setup.md](19-technical-setup.md).

---

## 2. The decision about where the sources live

The C# sources sit under `unity/Assets/Lokanta/`. The project files under `src/` compile them by **linking** them.

```
unity/Assets/Lokanta/Core/      the source lives here + Lokanta.Core.asmdef
src/Lokanta.Core/*.csproj       takes the ../../unity/Assets/... files with Compile Include
```

**Why under Assets:** the `noEngineReferences: true` flag in the asmdef file enforces the "the core does not know Unity" rule **at the compiler level**. That is a stronger guarantee than a folder arrangement; the core cannot even see a `UnityEngine` type.

**Why a csproj as well:** `dotnet test` runs the 88 tests in one second, while Unity takes minutes to open. Two compilers, one source.

**Why the intermediate outputs live under `src/`:** Unity produces a `.meta` for every file under Assets. Dropping `bin/` and `obj/` folders in there would pollute the project with hundreds of pointless files.

---

## 3. The simulation's model

**Staff are not agents, they are servers.** [14-staff-system.md](14-staff-system.md) says "no complex pathfinding". In the simulation a staff member is a server in a work pool that processes one job at a time. Daily capacity falls out of the job durations by itself; it is not coded separately.

**The customer unit was settled.** The review's finding, "make person versus party explicit": a slot is the **party** that occupies a table. The demand formula produces **people**, parties form until the people run out, and both work and the bill scale with party size.

**The chain of jobs:** wait for a table → be taken an order → be cooked → be served → eat → pay → have the table cleared. The hall pool does ordering, serving, payment and clearing; the kitchen pool does the cooking.

**The priority rule is one sentence:** every idle server attends to the customer with the **least patience left**. Ties go to the lower index. That single rule makes priority lists like "take payment first, then serve" unnecessary, and it is deterministic.

**The owner is the hall's zeroth server** and finishes jobs 1.4 times faster, because the capacity model counts the owner as 1.4 person-days.

---

## 4. The design bugs the simulation found

This section is the real value. Not one of them was found by reading; all of them came out when the simulation ran.

### 4.1 Patience cannot drain at a single rate

In the first run, **six parties out of six left angry.** The reason was that §5.2 of [12-economy.md](12-economy.md) contradicted itself: patience is given as 8-40 seconds, but the same section says a service takes about 120 seconds. At a single rate every customer would always walk out.

**The right reading:** patience is tolerance for being ignored.

| Stage | Drain rate | Reason |
|---|---|---|
| Waiting for a table | 100% | Nobody is attending to them |
| Waiting for the order to be taken | 100% | Seated but not seen |
| Waiter at the table | **0%** | A customer being attended to is not waiting |
| Food cooking | 35% | The order is placed, an expectation is set |
| Waiting for the bill | 50% | Full, but wants to leave |

### 4.2 The preparation times contradicted the capacity model

In the generated content a hamburger cooked in 75,000 ms. In a 480,000 ms service a cook could make six hamburgers a day; the capacity model says 28 people.

**The fix:** `prepMs` is no longer written by hand, it is derived from the capacity budget inside `tools/content/gen_dishes.py`. Complexity only sets the ratio between dishes, not the absolute value. The menu average is made equal to the budget, with a deviation of six parts per thousand.

### 4.3 A customer in a hurry must not order a heavy dish

A courier with 8 seconds of patience can never wait for a dish that takes 17 seconds to cook. That archetype was structurally impossible to serve.

**The fix:** the customer chooses from among the dishes they can wait for. Realistic and cheap.

### 4.4 The average ticket was sinking to the price of a cola

The balance tool's first run gave a strange result: **the passive player was not going under, the one playing well was.**

The reason: every customer picked **a single item** from the menu with **equal probability**, so most customers were buying nothing but a drink.

**The fix:** one **main course is certain** per person, with a side (30%) and a drink (40%) by chance. The average is 1.7 plates. This is the base form of the combo mechanic in [07-cuisine-system.md](07-cuisine-system.md). The preparation times were divided by it too, because the per-person budget is fixed.

### 4.5 The ingredient cost was never being paid

The simulation was calculating the ingredient cost but not taking it out of the till. That is why the passive player finished their 60 days with 32,348 coins; the **15,494-coin gap was exactly the ingredients that went unpaid.**

**Temporary fix:** the cost is deducted at the moment of sale. The right way is for it to be paid up front at the morning market stage; when the market is written, this line goes away.

### 4.6 Reputation hit the ceiling in a week

The formula in §5.5 of [12-economy.md](12-economy.md) gives +12 points a day. Reputation goes from 30 to 100 in nine days, which means the sixty-day campaign's main axis of progression is exhausted in the first week.

**The fix:** the gain is scaled against the remaining gap, **the loss is not scaled.** Reputation is hard to earn and easy to lose.

---

## 5. The balance tool's answers

Ten seeds, sixty days, five strategies.

<!-- GENERATED: harness -->
| Strategy | Final till | Reputation | Tables | Crew | Served | First debt |
|---|---|---|---|---|---|---|
| Passive | 16,900 | 41.1 | 4 | 1 | 824 | — |
| Reasonable | 12,788 | 97.1 | 11.2 | 8.2 | 2,414 | day 46 |
| Non-expanding | 9,275 | 50.6 | 4 | 3 | 907 | — |
| High price | 7,367 | 1.4 | 4 | 2.6 | 570 | — |
| Overstaffed | 8,196 | 49.7 | 4 | 3 | 910 | — |

| Strategy | Revenue | Ingredients | Wages | Rent | Net | Ticket per person |
|---|---|---|---|---|---|---|
| Passive | 48,419 | 15,448 | 8,471 | 15,600 | 8,900 | 58.7 |
| Reasonable | 145,738 | 46,401 | 38,589 | 43,740 | 17,008 | 60.4 |
| Non-expanding | 53,711 | 17,131 | 19,705 | 15,600 | 1,275 | 59.2 |
| High price | 42,852 | 10,614 | 17,270 | 15,600 | −633 | 75.2 |
| Overstaffed | 53,809 | 17,153 | 20,860 | 15,600 | 196 | 59.2 |
<!-- /GENERATED: harness -->

**Design intents confirmed:**

- **A high price loses money.** Reputation collapses to 1.4, revenue falls, and the sixty days close at a loss. The ticket may rise to 75 coins, but the customers do not come.
- **Overstaffing is punished.** Pushing the crew to the ceiling serves the same customers as a crew of three but pays 1,155 coins more in wages.
- **Money never becomes irrelevant in any strategy.** The warning threshold never fired.

**Two balance problems left open:**

1. **The passive player does not go under.** Starts with 8,000 and finishes with 16,900. Never intervening costs nothing; the bankruptcy ladder produces no threat. The design intent is the opposite of this.
2. **The reasonable player falls into debt in every run**, on day 46 on average. The third expansion (10,400 to buy, 10,450 weekly rent) cannot be covered. Expansion should have a price, but it should not be a reason to go under.

Both come from the same root: **the rent and the expansion prices had been solved from the closed-form weekly model, not from the simulation.** The next job is to wire `tools/balance/solve.py` to the simulation and search for the parameters there.

---

## 5b. The market stage and the death spiral

10 September, second session. The first of the two open problems in section 5 was taken up.

### A correction to the diagnosis: not the rent, the mechanic

"The passive player does not go under" had been taken for a parameter problem. Raising the rent would have been the wrong fix; it would have punished the good player to exactly the same degree. What the passive player is **really not doing is buying ingredients.** Because the market stage had not been written, the restaurant was running itself.

The stock system was written: ingredients are bought up front, consumed when the order is taken, and the perishable ones are zeroed at the end of the day.

**The result is unambiguous.** The passive player now serves 13 people in sixty days and 98% of the demand turns around at the door. Neglect has a price.

### If there is nothing on the menu, the customer turns around at the door

In the first implementation a customer with no stock available sat down at a table, waited until their patience ran out, and was then counted as "left angry". One stock mistake took the same reputation penalty as a customer kept waiting for forty minutes, and every strategy went into a death spiral.

The fix: a party that cannot find a main course does not come in. There is a reputation penalty, but a small one, and the table is not occupied. The angry-customer count fell from 271 to 0.

### The market stage's real tension: menu width

The stock check is made **per party**, while the suggestion was calculating a daily **average**. On a menu with twelve main courses, half an order a day falls to each dish; when a party of three wants that dish it needs three portions' worth of ingredients and there is half a portion in stock.

The fix is two-sided: the suggestion holds a floor that covers at least one party for each open dish, and the reasonable strategy now narrows the menu to match demand. **Carrying a wide menu is expensive, because perishable ingredients are zeroed every day.** This is the market stage's design tension, and it came out of the model by itself.

### The remaining problem was measured: not the rent, the death spiral

A rent solver was added to the tool: `dotnet run --project src/Lokanta.Harness -- --solve`. It cut the rent down to 25% and tried thirty-two combinations of rent and expansion.

**None of them worked.** Even with the rent at a quarter, the reasonable player goes under.

The service-rate column shows why:

| Strategy | Share of demand served | Turned away at the door |
|---|---|---|
| Passive | 2% | 98% |
| Market only | 47% | 47% |
| Reasonable | 32% | 64% |

The chain is this:

1. At the first tier the economy breaks even at best: rent 1,950 plus wages 980, against a weekly gross profit of roughly 2,856
2. Any dip melts the till
3. If the till melts, ingredients cannot be bought and customers turn around at the door
4. Reputation falls, demand falls, revenue falls
5. **The spiral cannot be reversed**, because recovering means buying ingredients and buying ingredients takes money

The first twelve days go healthily (7-15 people a day, reputation from 3,171 to 3,904). The collapse starts when the till runs dry.

**Two root causes, both measured:**

- **The closed-form model assumes 100% of demand is served**, while the simulation serves between 47% and 83%. The rents had been solved on that assumption.
- **No strategy uses credit.** §4 of [12-economy.md](12-economy.md) defines credit for exactly this situation: 5,000 coins, 1.35× repayment, eight weeks. That should be the floor of the spiral, and the strategy never tries it.

The next balance pass has to start with those two. Playing with the rent scale was tried and proved not to be enough.

### The decisive bug: the ingredients were being paid for twice

While there was no market stage, a temporary line had been put inside `CompletePayment`: `till += ticket − cost`. Its purpose was to have the ingredients paid for somewhere. Once the market was written the ingredients were bought up front in the morning, but that temporary line was never removed.

**The result: every plate's ingredients were being deducted both at the market and at the till.**

It came out when the days were traced one by one. Gross profit 476 coins a day, till increase 116 coins. The 360 in between was exactly the ingredients paid for a second time.

That single line invalidated every balance measurement taken before it. The rent solver failing across thirty-two combinations, raising demand not helping, credit not being enough: all of it was in the shadow of this bug.

### After the fix

| Strategy | Final till | Reputation | Service rate | First debt |
|---|---|---|---|---|
| Passive | −15,448 | 0 | 2% | day 21 |
| Market only | −8,440 | 0 | 63% | day 43 |
| **Reasonable** | **+2,914** | 60.3 | 88% | day 56 |
| Non-expanding | +1,503 | 51.1 | 86% | day 56 |
| High price | −379 | 0 | 87% | day 55 |
| Overstaffed | −18,249 | 0 | 54% | day 31 |

**Both of the open problems in section 5 are closed too:**

- **The passive player goes under on day 21.** 98% of the demand turns around at the door, 13 people are served in sixty days. Neglect has a price.
- **The reasonable player finishes in the black.** Service rate 88%, reputation 60.

The design intents also continue to hold: a high price loses money, overstaffing gives the worst result.

### Three strategy bugs fixed along the way

The balance tool's data is only as good as the quality of the strategy the tool plays. Three bugs were found, and each of them changed the numbers markedly.

| Bug | What it was | Its effect |
|---|---|---|
| **Hiring off the wrong signal** | It said "if there are angry customers two days running, hire". But an angry customer is not always a crew signal; stock may have run out, or a short-patience archetype may have waited at the peak | Two hall staff were being kept on four tables, 2,408 coins of wages a week. The capacity model wants zero at that volume; the owner is enough on their own. Wages fell from 19,647 to 10,446 |
| **Blind expansion** | It expanded whenever money and reputation were sufficient | A player who could not fill the shop they already had started paying rent on empty tables. A third condition was added: the service rate has to be above 85% |
| **Credit was never used** | The `TakeLoan` command sat in the enum but had not been implemented | §4 of [12-economy.md](12-economy.md) defines credit for exactly this situation. It was implemented, and the strategy draws on it when the till cannot cover two weeks of costs |

### The remaining balance decision

The rent solver (`--solve`) now finds combinations that work. Even at the current rent the reasonable player finishes in the black, so the rent is no longer a catastrophe — it is tight.

| Rent scale | Reasonable, final till | Reasonable, debt risk | Passive |
|---|---|---|---|
| 50% | 20,049 | 0% | goes under |
| 75% | 13,449 | 0% | goes under |
| 100% (current) | 3,511 | 25% | goes under |

**This is a design decision, not a technical problem.** Lowering the rent would require `tools/balance/model.py` to be re-derived as well, because the rents there were solved from the target margins. Reconciling the two models is a separate job.

One more thing to watch: the reasonable player reaches 5.5 tables on average over sixty days, while the closed-form model assumes 14 tables are reached. Whether the expansion gates are too tight or expansion genuinely cannot be afforded also has to be measured.

### Reconciling the two models: the realisation rate

The remaining balance decision was closed by measuring it.

Two more strategies were added. **Bold** expands the moment it sees the money; **planner** follows the closed-form model's schedule (days 15, 29 and 43).

| Strategy | Operating net | Served | Service rate | Final till |
|---|---|---|---|---|
| Bold | −60,803 | 297 | 15% | −64,158 |
| Planner | +47,052 | 2,396 | 93% | **−2,810** |

Bold eats itself: it reaches twelve tables and can neither stock them nor staff them. But **planner** was the decisive one. It produces the highest operating net, serves the most customers, has a 93% service rate — and still finished at a loss.

So the expansion gates were not too tight. **Even the best-working strategy could not cover the rent.**

The reason was measured: the closed-form model assumes 165,870 coins of revenue on the same schedule, the simulation produces 107,949. **The realisation rate is 65%.** A customer whose patience ran out, stock that ran dry, a table that filled up. Because the rents were solved from the model's unrealised revenue, they were roughly 35% too high.

**The fix:** the model now multiplies revenue by the realisation rate. The table shows what the player actually takes, the margins become real, the rents solve correctly. The C# side applies the same coefficient and the golden-table test keeps the two tied together.

`solve.py` was run again and found a new set with zero penalty:

| | Old | New |
|---|---|---|
| Rent, 4 → 14 tables | 1,950 → 10,450 | **650 → 4,000** |
| Expansion price | 3,250 / 5,850 / 10,400 | **2,500 / 4,500 / 8,000** |
| Capacity (cook/waiter/dishwasher/cashier) | 28/25/46/66 | 30/26/48/70 |
| Owner's labour | 1.4 | 1.3 |

### The result: the game works

| Strategy | Final till | Reputation | Tables | Service rate | First debt |
|---|---|---|---|---|---|
| Passive | −5,067 | 0 | 4 | 2% | day 42 |
| Market only | −1,797 | 13.2 | 4 | 76% | day 53 |
| **Reasonable** | **+27,916** | 88.3 | 7.3 | 92% | — |
| Non-expanding | +11,719 | 64.3 | 4 | 87% | — |
| Planner | +26,707 | 85.1 | 13.2 | 93% | day 42 |
| Bold | −64,158 | 0 | 14 | 15% | day 7 |
| High price | +7,918 | 0.1 | 4 | 91% | — |
| Overstaffed | −7,737 | 2.5 | 4 | 71% | day 44 |

**Every design intent is confirmed:**

- **Growing is rewarded.** Non-expanding 11,719, expanding 27,916. Roughly 2.4×. The document said "three times, not eleven"; that is inside the band.
- **Neglect has a price.** The passive player goes under on day 42.
- **Reckless growth is punished.** Bold falls into debt on day 7.
- **A high price does not pay.** It survives on 7,918 but earns a quarter of what good play earns, and its reputation is zero. A small expensive shop is a legitimate but weak business; the design intent was "it must not be the winning strategy", and that holds.
- **Overstaffing sinks you.**

### The single remaining warning and its cause — CLOSED

The tool was giving one warning: for the reasonable player, money stops being a problem in week 5.5, and the target was for it not to happen before week eight.

**This is not a balance problem, it is missing content.** §7 of [12-economy.md](12-economy.md) lists three measures, and the second of them is that the last-tier equipment should cost 8,000-12,000 coins. The equipment had not been written yet, so there was nothing for the player to save up for.

**Closed on the evening of 10 September 2026.** The equipment system was written, Decision D of [27-time-model.md](27-time-model.md) was implemented and the economy was rebalanced. The warning no longer fires in any strategy. The detail, including the three bugs the measurement caught in that round: [32-equipment-and-rebalance.md](32-equipment-and-rebalance.md).

The numbers under this section are from the run BEFORE that closure; the up-to-date table is in docs/32.


---

## 6. Test coverage

| Set | Count | What it protects |
|---|---|---|
| Arithmetic | 12 | The rounding rule, overflow, exponentiation precision |
| Randomness | 12 | An exact match with an independent Python implementation, stream independence |
| The floating-point ban | 4 | Not one `float` in the core, no Unity and no JSON reference |
| Content | 13 | Rejecting decimals, the id rule, cross-references |
| Golden table | 7 | The C# core matches the Python model |
| Simulation | 18 | The day's flow, determinism, frame independence, cuisine |
| Balance rules | 22 | `model.py --check` |

**The cross-validation principle.** A test taking its expected value from the code it is testing proves nothing. The randomness test holds to the independent Python implementation in `tools/balance/rng_reference.py`, and the weekly-table test holds to `model.py`'s output.

---

## 7. Running it

```
dotnet test                                    88 tests
dotnet run --project src/Lokanta.Harness       the balance tool
dotnet run --project src/Lokanta.Harness -- --seeds 20 --csv out.csv
dotnet run --project src/Lokanta.Harness -- --solve      the rent solver

python tools/balance/model.py --check          22 balance rules
python tools/balance/timing.py --check         46 timing-model rules
python tools/content/gen_dishes.py             dishes and ingredients
python tools/content/gen_archetypes.py         archetypes
python tools/balance/export.py                 content/ and the golden data
python tools/balance/render.py                 write the tables into the documents

.\tools\unity\run.ps1 -Method Lokanta.EditorTools.ProjectSetup.ApplyAll
```

Why `run.ps1` exists: in batch mode Unity **returns zero even when there is a compile error.** On 10 September the editor script gave four API errors, `-executeMethod` never ran, and the exit code was zero all the same. This script reads the log and returns the real result.

---

## 8. What comes next

| Order | Job | Why |
|---|---|---|
| 1 | **Implementing** the peak decision ([28-peak-decision.md](28-peak-decision.md)) | Decided: it is not the slice shares but the slice **durations** that change with the cuisine. Turkish cuisine's 60% lunch share stays as it is, and the lunch slice takes up 48% of the day. The daily loss is 3.15% for fast food, 5.08% for Turkish |
| 2 | Searching for the parameters from the simulation | The two balance problems in §5 |
| 3 | The market stage | Ingredients must be paid for up front, spoilage and stock have to come into play |
| 4 | The command log and the save | [23-core-contract.md](23-core-contract.md) §7 |
| 5 | The remaining schemas | Equipment, upgrades, regulars, cuisines |
| 6 | The Unity view layer | Showing the simulation on screen |
