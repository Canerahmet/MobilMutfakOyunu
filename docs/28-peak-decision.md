# The Peak Decision

**Last updated:** 10 September 2026
**Register items:** A4 economy numbers, A5 cuisine identity, A7 customer formulas, A8 staff capacity
**Status:** Batch B. This file is binding and it **supersedes** [27-time-model.md](27-time-model.md) Decision F.

**Verification:** `python tools/balance/timing.py --peak` — every number in this file comes from there. `python tools/balance/timing.py --check` — 47 consistency tests, all passing; none of them were altered.

---

## 1. The contradiction

### 1.1 Two files, two claims

[12-economy.md](12-economy.md) §5.6 gives every cuisine a signature hourly profile and declares it the heart of the identity:

> "In the restaurant sixty per cent of the customers come in a single slice. That turns the lunch peak into a real crisis moment and leaves the evening empty."

[27-time-model.md](27-time-model.md) Decision F finds that same profile physically impossible and demands that the `arrivalWeightsBp` values be renormalised so that no slot exceeds 30%. The profile it proposes for Turkish cuisine is **15% / 30% / 30% / 25%**. That erases the 60% lunch peak.

### 1.2 The two sides' numbers

| Source | Fast food | Turkish cuisine |
|---|---|---|
| The [12](12-economy.md) §5.6 table (written) | 15% / 35% / 15% / 35% | 10% / 60% / 20% / 10% |
| `content/archetypes/*.json` (measured) | 11.05% / **37.49%** / 16.20% / 35.26% | 9.37% / **60.63%** / 17.53% / 12.47% |
| [27](27-time-model.md) Decision F (proposed) | 20% / 30% / 20% / 30% | 15% / 30% / 30% / 25% |

The written table does not match the content's measurement either; §5.6 was written by hand, the content was generated with `tools/content/gen_archetypes.py`. That is a second contradiction and it is closed below.

### 1.3 The arithmetic Decision F rests on

The peak day (`model.py`, the eighth week's weekend): 97 customers, 4 cooks, 7 hall staff + the owner's 1.4 work-days, 14 tables.

| Pool | Ms needed | Ms available | Day utilisation |
|---|---|---|---|
| Kitchen | 97 × 17,143 = 1,662,871 | 4 × 480,000 = 1,920,000 | 86.6% |
| Hall | 97 × 36,908 = 3,580,076 | 8.4 × 480,000 = 4,032,000 | **88.8%** |
| Tables | 43.25 × 125,525 = 5,428,923 | 14 × 480,000 = 6,720,000 | 80.8% |

A slot is a quarter of the day, which means a quarter of the capacity too. For no pool to exceed 100% within a slot:

```
maximum slot share = 0.25 ÷ 0.8879 = 28.16%
```

Turkish cuisine's 60.63% is **2.15×** that. Growing the crew does not rescue it either: Decision F shows that serving 60.63% with zero queue needs 26 staff and 28 tables, while the ceilings are 12 and 14. That calculation was redone and confirmed in section 4.2.

### 1.4 The real size of the contradiction

**The physical ceiling is correct. The damage estimate is wrong.**

Decision F calculates the slot-share ceiling (28.16%) correctly. But it calculates what happens when the ceiling is exceeded with an **open-loop** queue: assuming nobody walks out, it grows the backlog without limit. That is why it says "72.8% of the day walks out" for fast food's current profile.

In reality a customer whose patience runs out **does not join the queue** and shortens the wait for the person behind them. The queue is a closed loop and it saturates at the marginal archetype's patience. The same profile, the same crew, with a closed-loop model:

| Profile | Decision F's model | Closed-loop model | Difference |
|---|---|---|---|
| Fast food, content (37.49%) | daily loss 72.8% | daily loss **13.36%** | 5.4× |
| Turkish, content (60.63%) | daily loss 60.6% | daily loss **30.47%** | 2.0× |
| Fast food, Decision F (30%) | daily loss 6.1% | daily loss **1.80%** | 3.4× |

So the contradiction is smaller than Decision F thought, but it is not zero. **30.47% is still unacceptable.** Turkish cuisine's current profile is not feasible under either the open loop or the closed loop. But fast food's profile is **close to feasible** under the closed loop, and that widens the solution space.

The model itself is in section 3.

---

## 2. The physical quantity is not the slot share, it is the density

### 2.1 Definition

Decision F's sentence "the slot share cannot exceed 28.2%" hides the assumption that the slots are of equal length. If the slot length is variable, the right quantity is **density**:

```
density_i = customer_share_i ÷ duration_share_i
```

A slot's pool utilisation:

```
slot_utilisation_i = density_i × day_utilisation
```

The zero-queue condition is `slot_utilisation ≤ 1`, that is:

```
maximum density = 1 ÷ 0.8879 = 1.1262
```

With equal slots (duration share 0.25) that gives a slot share of 0.25 × 1.1262 = **28.16%**; Decision F's number. But if the duration share is 0.48 the same density allows a slot share of **54.06%**. The ceiling did not change; what changed is which quantity the ceiling applies to.

### 2.2 Slot capacity, in customers

With equal slots (120,000 ms), how many customers each pool can take:

| Pool | Calculation | Customers/slot |
|---|---|---|
| Kitchen | 4 × 120,000 ÷ 17,143 | 28.00 |
| Hall | 8.4 × 120,000 ÷ 36,908 | **27.31** |
| Tables | 14 × 120,000 ÷ 125,525 = 13.38 parties × 2.1847 ÷ 0.9741 | 30.01 |

The binding pool is the hall: 27.31 customers per slot. The total that can be served over the day is 97 ÷ 0.8879 = **109.25 customers**; a slot takes as much of that as its duration share.

Turkish cuisine's lunch slot brings 0.6063 × 97 = **58.82 customers** with equal slots. 58.82 ÷ 27.31 = 2.15. So the lunch slot carries more than twice the demand of its capacity.

### 2.3 Which levers move the density ceiling

| Lever | New ceiling | Equal-slot equivalent | Note |
|---|---|---|---|
| Weekend, crew 11 (current) | 1.1262 | 28.16% | Decision F's number |
| Weekend, crew 12 (the ceiling) | 1.1546 | 28.87% | One extra hall staff member |
| Weekday, 77 customers | 1.4188 | 35.47% | The crew is set to the weekend, the weekday is empty |

**A twelfth staff member grows the density ceiling by only 0.7 points.** The "the ceiling is one above what is needed at every tier" slack in [14-staff-system.md](14-staff-system.md) is an error margin; it is not a lever for buying your way through the peak. That is the short version of why option B does not work.

---

## 3. The queue model correction

### 3.1 Decision F's model is open loop

`slot_queue` inside `timing.py` computes the backlog at the end of a slot like this:

```
wait = max(0, work_needed − work_available) ÷ server_count
```

This assumes **everyone** who arrives during the slot joins the queue. For the Turkish profile the result is 192,598 ms of average idle wait: 4.8× the patience of the most patient archetype (the pensioner, 40,000 ms). That idle wait physically cannot occur, because before the queue reaches that length the arrivals start turning away at the door and the backlog stops growing.

### 3.2 The closed loop

`flow_day()` was added to `timing.py`. For every pool a backlog `B_p` (in ms-of-work) is kept; a new arrival's idle wait is `W = Σ B_p / rate_p`. A customer whose patience cannot bear `W` **does not join** the queue:

```
dB_p/dt = remaining(W) × arrival_rate_p − rate_p          (B_p ≥ 0)
```

`remaining(W)` is the share of heads of the archetypes whose wait score `W + cooking × 0.25` does not exceed their patience. Decision E's patience definition and `cookPatienceWeightBp = 2500` are used exactly as they are; nothing was altered.

Equilibrium settles at the point where the arrival rate falls to capacity. The backlog **saturates at the marginal archetype's patience**: 11,004 ms in fast food, 17,003 ms in Turkish. Those are not coincidences, they are the patience steps in the pool themselves.

### 3.3 A direct comparison of the two models

The fast food content profile, peak slot (37.49%), peak day:

| Quantity | Open loop | Closed loop |
|---|---|---|
| Slot utilisation, hall | 133.2% | 133.2% (the same, utilisation is measured by demand) |
| Idle wait at the end of the slot | 101,137 ms | **11,004 ms** |
| Slot-average idle wait | 50,568 ms | **10,167 ms** |
| Lost within the slot | 100% | **20.5%** |
| Daily loss | 72.8% | **13.36%** |

The utilisation percentages are the same in both models; the only thing that differs is how far the backlog grows. **Decision F's physics finding (the slot capacity is exceeded) is correct; its conclusion finding (the day collapses) is an artefact of the model.**

The closed-loop model's assumptions, written out openly:

| Assumption | What it means | Direction |
|---|---|---|
| A customer who leaves consumes no capacity | They leave the queue, they never entered service | **Lowers** the loss |
| Arrivals are spread evenly within the slot | In reality there is a peak inside the slot too | **Lowers** the loss |
| The waits of the three pools are added | Same as Decision F, sequential waiting | Neutral |
| Spillover between slots is free | The queue flows into the next slot | **Lowers** the loss |
| The queue remaining at the end of the day counts as lost | The people at the door at closing time | Raises the loss |

Three assumptions pull the loss down. So the numbers below are **on the optimistic side** and may rise in playability testing. This is the opposite of Decision F's "6.1% is on the pessimistic side" note, and deliberately so: there is a band between the two models, and the truth is inside it.

---

## 4. Four options

### 4.1 Option A: renormalise the profiles

Clip every slot to ~30%. Decision F's proposal.

| Criterion | Result |
|---|---|
| Fast food daily loss | 1.80% |
| Turkish daily loss | 2.21% (profile 15%/30%/30%/25%) |
| Files changed | `content/archetypes/fastfood.json`, `content/archetypes/turk.json` — the `arrivalWeightsBp` field of 24 archetypes |
| Code changed | None |

**The cost.** Turkish cuisine's lunch peak drops from 60.63% to 30%; the density from 2.425 to 1.200. Fast food's peak from 1.500 to 1.200. The two cuisines' density profiles become **identical to each other**: 1.20 / 1.20 in two slots for fast food, 1.20 / 1.20 in two slots for Turkish. "Rhythm", one of the seven differentiation variables in [07-cuisine-system.md](07-cuisine-system.md), and [10-cuisine-identity.md](10-cuisine-identity.md)'s "the rhythm of the crowd" line are erased from the content entirely. On top of that, Turkish cuisine's evening rises from 12.5% to 25%, so the "quiet evening" identity goes too.

This option works, it is cheap, and it **destroys one of the four identity pillars.**

### 4.2 Option B: grow the ceilings

The crew and tables needed to serve the current shares with zero queue:

| Cuisine | Slot share | Density | Hall work-days | Hall staff | Cooks | Total crew | Tables |
|---|---|---|---|---|---|---|---|
| Fast food | 37.49% | 1.500 | 11.18 | 10 | 6 | **16** | **17** |
| Turkish | 60.63% | 2.425 | 18.09 | 17 | 9 | **26** | **28** |
| Ceiling | — | — | — | — | — | **12** | **14** |

In the closed-loop model less crew is enough, because an 8-9% loss is accepted. Even then:

| Cuisine | Target peak loss | Cooks | Hall | Total crew | Tables |
|---|---|---|---|---|---|
| Fast food | 3.4% | 5 | 9 | 14 | 17 |
| Turkish | 9.9% | 8 | 14 | 22 | 26 |

**The cost.** It breaks in three separate places.

| What breaks | Why |
|---|---|
| The economy | The rents and expansion costs are solved from tier capacity to a target margin inside `tools/balance/solve.py`. Tables 14 → 17 shifts the rent curve, crew 12 → 16 shifts the wage curve; the eight-week growth table gets solved from scratch |
| The scene | 17 tables do not read on a 2.5D scene under a fixed forty-degree camera; [19-technical-setup.md](19-technical-setup.md) wants under 100 draw calls and under 100 thousand triangles per frame, and the table and customer counts feed straight into that budget |
| Touches | [16](16-screens-and-tutorial.md) sets a daily ceiling of 40-60 touches; the morning station assignment for 16 staff eats that budget on its own |

And the 26 crew / 28 tables Turkish needs is more than twice the ceiling. **Option B is expensive for fast food and impossible for Turkish.**

### 4.3 Option C: make the loss deliberate

Let losing customers at the peak be the rule; let the skill be reducing the loss, not preventing it.

**It costs nothing, but it is not enough on its own.** Two reasons:

**First, there is no tuning range.** With equal slots the loss share is a linear function of the overload, and because a slot is 120,000 ms the coefficient is very large. Peak slot share against loss:

| Slot share | Density | Hall utilisation | Fast food slot loss | Turkish slot loss |
|---|---|---|---|---|
| 28.0% | 1.120 | 99.4% | 0.0% | 0.0% |
| 30.0% | 1.200 | 106.5% | 3.0% | 2.2% |
| 32.5% | 1.300 | 115.4% | 9.4% | 9.7% |
| 35.0% | 1.400 | 124.3% | 15.5% | 15.8% |
| 37.5% | 1.500 | 133.2% | 20.5% | 20.5% |
| 45.0% | 1.800 | 159.8% | 33.6% | 32.7% |
| 60.6% | 2.425 | 215.3% | 50.4% | 49.6% |

Above 33% the loss is bound to **arithmetic**: roughly `1 − capacity ÷ demand`. The patience distribution, that is, the content, no longer has any effect — the two cuisines' columns stick together after 35%. So the question "which loss is dramatic" can only be asked in the 28-33% band; above that the question is reduced to the arithmetic of "how many customers turn away at the door".

**Second, reputation does not spiral anyway.** Calculated in section 5.6: for the sign of a day's reputation change to flip, the loss has to exceed **52.8% (fast food) or 55.2% (Turkish)**. In the proposed profile the loss is 3-5%. So the answer to the question "should the reputation damage be softened during an announced rush" is **no**: there is no death spiral to soften. Adding a softener would make the loss invisible and weaken principle 1 of [02-design-proposal.md](02-design-proposal.md) ("every decision will have a visible consequence").

Option C is not a solution, it is an **acceptance**. Part of the decision, not the decision itself.

### 4.4 Option D: reshape the day

Let the slot durations not be equal. Let the lunch slot be long in ticks.

**The cost is three items.**

| Item | What |
|---|---|
| Arithmetic | [27](27-time-model.md) §5.3's sentence "maximum slot share 28.2%" generalises: maximum **density** 1.1262 |
| Schema | A per-cuisine `slotDurationsBp` joins `arrivalWeightsBp`; four numbers, summing to 10,000, each dividing exactly into ticks |
| The core | The `slotTicks = ServiceTicks / SlotCount` line inside `Simulation.BuildArrivalPlan` becomes an array; `TimingConfig`, `ContentSet`, `ContentDto` and `ContentSetLoader` carry one more field |

**There is no fictional cost.** A tradesman's restaurant serves lunch between 11:30 and 15:00; really about half of an eight-hour service day. Assuming the slots are equal was fictionally wrong to begin with.

**But D is not enough on its own either.** Even if the lunch slot were 45% of the length, a 60.63% share gives a density of 1.347 against a ceiling of 1.1262. Lengthening the duration **lowers** the density, it does not zero it.

### 4.5 Comparison

| | A: renormalise | B: grow the ceiling | C: accept the loss | D: reshape the day |
|---|---|---|---|---|
| Turkish lunch share | 30% | 60.63% | 60.63% | 60.63% |
| Turkish lunch density | 1.200 | 1.000 | 2.425 | 1.263 |
| Daily loss | 2.21% | 0% | 30.47% | 5.08% |
| Crew needed | 11 | 26 | 11 | 11 |
| Tables needed | 14 | 28 | 14 | 14 |
| Does the economy get re-solved | No | **Yes** | No | No |
| Does the content change | **24 archetypes** | No | No | 4 numbers per cuisine |
| The identity pillar | **Disappears** | Preserved | Preserved | Changes shape |
| Is it enough on its own | Yes | No (the ceiling) | No (no tuning range) | No (density is still 1.347) |

---

## 5. Decision G: the recommendation

### 5.1 What

**Slot durations become cuisine-specific content. The archetypes' `arrivalWeightsBp` values stay exactly as they are. The remaining overload is accepted as deliberate loss and is not softened.**

That is, D is the primary mechanism, C is the accepted residue, and A only as far as D cannot absorb — and with the numbers below, A is not needed at all.

| Cuisine | Slot shares (unchanged) | Slot durations (new) | Ticks |
|---|---|---|---|
| Fast food | 11.05% / 37.49% / 16.20% / 35.26% | 20% / 30% / 20% / 30% | 960 / 1,440 / 960 / 1,440 |
| Turkish | 9.37% / 60.63% / 17.53% / 12.47% | 12% / 48% / 25% / 15% | 576 / 2,304 / 1,200 / 720 |

Both sequences sum exactly to 4,800 ticks; no slot asks for a fractional tick.

### 5.2 Fast food, peak day (97 customers, 4 cooks, 7 hall, 14 tables)

| Slot | Share | Duration | Density | Kitchen | Hall | Tables | Avg. idle wait | Maximum | Loss |
|---|---|---|---|---|---|---|---|---|---|
| 1 Opening | 11.05% | 20% | 0.552 | 47.9% | 49.1% | 44.6% | 0 ms | 0 ms | 0.0% |
| 2 Lunch | 37.49% | 30% | **1.250** | 108.2% | **111.0%** | 101.0% | 4,567 ms | 5,006 ms | **7.2%** |
| 3 Afternoon | 16.20% | 20% | 0.810 | 70.2% | 71.9% | 65.4% | 353 ms | 5,004 ms | 0.0% |
| 4 Evening | 35.26% | 30% | **1.175** | 101.8% | **104.4%** | 95.0% | 3,560 ms | 5,001 ms | **1.3%** |

At the end of the day 5,000 ms of queue is left, that is, there are a few people at the door at closing. **The daily loss is 3.15%**, 3.06 people out of 97.

Slot 2's arithmetic is plain: capacity 0.30 × 109.25 = 32.78 customers, arrivals 0.3749 × 97 = 36.37 customers, overload 3.59. Of that, 2.62 is lost, the rest spills into slot 3's slack (capacity 21.85, demand 15.71, slack 6.14).

### 5.3 Turkish cuisine, peak day (the same crew)

| Slot | Share | Duration | Density | Kitchen | Hall | Tables | Avg. idle wait | Maximum | Loss |
|---|---|---|---|---|---|---|---|---|---|
| 1 Opening | 9.37% | 12% | 0.781 | 67.6% | 69.3% | 62.8% | 0 ms | 0 ms | 0.0% |
| 2 Lunch | 60.63% | 48% | **1.263** | 109.4% | **112.2%** | 101.6% | 6,537 ms | 7,010 ms | **8.4%** |
| 3 Afternoon | 17.53% | 25% | 0.701 | 60.7% | 62.3% | 56.4% | 451 ms | 6,996 ms | 0.1% |
| 4 Evening | 12.47% | 15% | 0.831 | 72.0% | 73.8% | 66.9% | 0 ms | 0 ms | 0.0% |

At the end of the day there is no queue. **The daily loss is 5.08%**, 4.93 people out of 97.

Slot 2's arithmetic: capacity 0.48 × 109.25 = 52.44 customers, arrivals 0.6063 × 97 = 58.82, overload 6.38; 4.93 of it lost, the rest spilling into slot 3.

**60.63% stands.** [12](12-economy.md) §5.6's sentence is still true: in the restaurant sixty percent of the customers arrive in a single slot. What changed is that the slot is not a quarter of the day but **nearly half** of it.

### 5.4 Across all the tiers

Each of `model.py`'s PLAN rows, with its own crew and tables:

| Week | Tables | Reputation | Weekday | Weekend | Cooks | Hall | FF wd | FF we | TR wd | TR we |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | 4 | 35 | 14 | 17 | 1 | 0 | 0.00% | 5.89% | 0.00% | 7.68% |
| 2 | 4 | 45 | 15 | 19 | 1 | 1 | 0.00% | 0.00% | 0.00% | 0.00% |
| 3 | 7 | 52 | 29 | 36 | 2 | 2 | 0.00% | 0.00% | 0.00% | 0.25% |
| 4 | 7 | 60 | 31 | 39 | 2 | 2 | 0.00% | 2.39% | 0.00% | 4.58% |
| 5 | 10 | 68 | 47 | 59 | 3 | 4 | 0.00% | 0.55% | 0.00% | 1.78% |
| 6 | 10 | 75 | 50 | 63 | 3 | 4 | 0.00% | 3.46% | 0.00% | 5.52% |
| 7 | 14 | 82 | 74 | 92 | 4 | 6 | 0.00% | **7.24%** | 0.00% | **8.75%** |
| 8 | 14 | 88 | 77 | 97 | 4 | 7 | 0.00% | 3.15% | 0.00% | 5.08% |

Three things can be read off this.

**The weekday is zero at every tier.** The crew is set to the weekend peak, and on a weekday 20% fewer customers arrive; no queue forms at all. This is the directly visible counterpart of [14](14-staff-system.md)'s decision that "the crew is set to the peak and paid for seven days".

**The hardest week is week seven, and the hardest moment is right after the last expansion.** 92 customers are met with 4 cooks and 6 hall staff; the `ceil()` rounding is tightest here. [12](12-economy.md) §6 already describes week seven as "a deliberate gamble". The loss curve confirms the narrative.

**One customer is lost on the first week's weekend.** 5.89% × 17 = 1.00 people (7.68% × 17 = 1.31 in Turkish). This is not an accident, it is the tutorial's missing piece: [review/05-player-experience.md](review/05-player-experience.md) said "put the first hire on the morning after the first waiting loss the player sees with their own eyes." The capacity model already produces that loss on the sixth or seventh day. So the reason for hiring the first waiter comes from the arithmetic rather than from the script.

### 5.5 The effect on weekly revenue

| Cuisine | Weekday | Weekend | Customers lost per week | Total | Revenue effect |
|---|---|---|---|---|---|
| Fast food | 0.00% | 3.15% | 6.1 | 579 | **1.06%** |
| Turkish | 0.00% | 5.08% | 9.9 | 579 | **1.70%** |

The calculation: `(5 × 77 × weekday + 2 × 97 × weekend) ÷ (5 × 77 + 2 × 97)`.

`model.py`'s revenue remains an **upper bound** and in the peak week should be read 1.06% lower for fast food and 1.70% lower for Turkish. Decision F wrote the same correction as 2.05%; the new number is smaller than that.

The effect on the eighth week's net margin. A lost customer does not place an order, so they do not consume their ingredients either; wages and rent are fixed:

| | Revenue | Ingredients (32%) | Wages + rent | Net | Margin |
|---|---|---|---|---|---|
| `model.py` | 43,425 | −13,896 | −20,857 | 8,672 | **20.0%** |
| Fast food, −1.06% | 42,965 | −13,749 | −20,857 | 8,359 | **19.5%** |
| Turkish, −1.70% | 42,687 | −13,660 | −20,857 | 8,170 | **19.1%** |

**It stays inside the decimal slack of the margin targets; it does not need writing back into `model.py`.**

### 5.6 Reputation: is there a death spiral

A day's reputation change ([12](12-economy.md) §5.5): `Σ (satisfaction − 60) × reputation_weight / 100`. The satisfaction of a customer who walks out, at the moment their patience runs out exactly, is `100 − (patience ÷ patience) × 60 = 40`.

The peak day, the proposed profile:

| Cuisine | Day | Reputation change | If there were no queue at all | The peak's cost | Walked out |
|---|---|---|---|---|---|
| Fast food | Weekend | +14.93 | +28.57 | **13.64 points (48%)** | 3.06 people |
| Fast food | Weekday | +22.68 | +22.68 | 0 | 0 |
| Turkish | Weekend | +17.27 | +35.29 | **18.03 points (51%)** | 4.93 people |
| Turkish | Weekday | +28.02 | +28.02 | 0 | 0 |

The peak takes roughly **half** of the day's reputation gain. But it does not flip the sign. The sign-flip threshold:

```
one satisfied customer  = (avg. satisfaction − 60) × weight / 100
one customer walking out = (40 − 60) × weight / 100

fast food : +0.2946 and −0.2628  →  threshold 52.8%
turkish   : +0.3639 and −0.2956  →  threshold 55.2%
```

**For a day's reputation to turn negative, more than half of the customers have to be lost.** In the proposed profile the loss is 3-5%, and at option C's worst (Turkish 60.63% on equal slots) it is 30.47%. Neither passes the threshold.

That ratio is independent of the reputation formula's scale: the numerator and the denominator are multiplied by the same weight. If the formula is later normalised by volume (section 8, open item 3), the threshold stays the same.

**Conclusion: the answer to "should the reputation damage be softened during an announced rush" is no.** There is no spiral that needs softening; adding one would kill the visibility of the loss.

### 5.7 Who walks out at the peak

The peak slot's average wait score is 9,567 in fast food and 11,542 in Turkish; the maximum scores are 10,006 and 12,010 (idle wait + cooking × 0.25).

| Cuisine | Archetype | Patience | Share of the peak slot | At the average | At the end of the slot |
|---|---|---|---|---|---|
| Fast food | `kurye` | 8,000 | 3.60% | Stays, takeaway | Stays |
| Fast food | `sikayetci_musteri` | 9,000 | 1.06% | **Walks out** | Walks out |
| Fast food | `aceleci_ogrenci` | 10,000 | 12.27% | Stays | **Walks out** |
| Fast food | `cocuklu_ebeveyn` | 13,000 | 4.03% | Stays | Stays |
| Turkish | `kurye` | 8,000 | 2.22% | Stays, takeaway | Stays |
| Turkish | `uzun_yol_soforu` | 11,000 | 1.39% | **Walks out** | Walks out |
| Turkish | `ogle_molasi_calisani` | 12,000 | 16.47% | Stays | **Walks out** |
| Turkish | `cocuklu_ebeveyn` | 13,000 | 2.48% | Stays | Stays |

The courier survives in both cuisines: they do not join the table or kitchen queue, only the hall queue. Decision E's finding is preserved.

**The face that is lost differs per cuisine.** In fast food it is the complaining customer and the hurried student; in the restaurant it is the long-haul driver and the worker on their lunch break. The lunch-break worker is 16.47% of the lunch slot and, by definition, the person who cannot wait. That the one lost in the lunch rush is the civil servant on their lunch break is where the fiction and the arithmetic meet on their own.

### 5.8 Is there an archetype that becomes unservable

**No.**

| Check | Result |
|---|---|
| The most impatient archetype at zero load (`kurye`, 8,000) | Wait score 2,500, stays — Decision E, unchanged |
| The `patienceMs` values | Unchanged |
| The task durations (`T_SEAT`, `T_ORDER`, `T_SERVE`, `T_BUS`, `T_WASH`, `T_PAY`) | Unchanged |
| The `prepMs` formula | Unchanged |
| Weekday, every tier | No archetype is lost (no queue) |
| Weekend, the peak slot | 2 archetypes at risk in fast food, 2 in Turkish; both at the end of the slot, not at the average |

No archetype becomes **permanently** unservable. All four of the at-risk ones are served in the first half of the slot and lost in the second half. The `timing.py --check` group C and E tests (zero load, the courier, the brokenness of the old definition) were not altered and continue to pass.

### 5.9 The other two cuisines

[12](12-economy.md) §5.6 defines four cuisines. The Italian and Japanese archetype pools have not been written yet; their durations are derived by the same rule, purely from the density ceiling:

| Cuisine | Shares | Durations | Ticks | Densities | Status |
|---|---|---|---|---|---|
| Italian | 5% / 20% / 10% / 65% | 10% / 20% / 15% / 55% | 480 / 960 / 720 / 2,640 | 0.50 / 1.00 / 0.67 / **1.18** | Provisional |
| Japanese | 15% / 50% / 15% / 20% | 15% / 40% / 20% / 25% | 720 / 1,920 / 960 / 1,200 | 1.00 / **1.25** / 0.75 / 0.80 | Provisional |

Both are below the ceiling. The Italian's loss figure cannot be computed because `T_EAT_PARTY` will vary by cuisine ([27](27-time-model.md) pending decision item 1) and will lengthen for the Italian; a long sitting could make the table pool the binding one. **These two rows are not binding**; they are the proof that the slot-duration mechanism can carry all four cuisines.

---

## 6. Identity: what shrinks, and what comes in its place

This section has to be written without shrinking it.

### 6.1 What shrinks: peak severity stops being an identity axis

The density ceiling is the same for all cuisines, because the capacity model is cuisine-independent. In the proposed profiles:

| Cuisine | Peak density | Peak hall utilisation | Peak slot loss |
|---|---|---|---|
| Fast food | 1.250 | 111.0% | 7.2% |
| Turkish | 1.263 | 112.2% | 8.4% |

**The difference is 1.2 points.** The player **cannot feel** that the lunch peak is harder in the restaurant than in fast food, because it is not. If [12](12-economy.md) §5.6's sentence "turns the lunch peak into a real moment of crisis" is read to mean "into a bigger crisis than fast food's", it is **wrong**, and this decision does not make it right either. It could not have been made right by any option: option B bought it with 26 staff and 28 tables, which is twice the ceilings.

The "harder" claim that sixty percent was thought to carry was a claim never verified in the content. Measurement tested it for the first time and it did not hold.

### 6.2 What comes in its place: the duration and continuity of the pressure

Not the peak's severity, but the peak's **shape** varies by cuisine, and that is measurable:

| Criterion | Fast food | Turkish cuisine |
|---|---|---|
| Time spent above density 1.0 | 2,880 ticks (60% of the day) | 2,304 ticks (48% of the day) |
| In how many pieces | **Two** (1,440 + 1,440) | **One** (2,304) |
| Longest uninterrupted pressure, 1x speed | 144,000 ms = 2.4 min | 230,400 ms = 3.8 min |
| Breathing room between pressures | 960 ticks, density 0.810 | None |
| After the peak | A second peak | 1,920 ticks of uninterrupted slack (0.701 and 0.831) |
| The emptiest slot | Opening: 11.05% share, density 0.552 | Afternoon: 17.53% share, density 0.701 |
| The evening dining room | Full (104.4%) | Empty (73.8%) |

Three differences translate into what the player sees on the screen.

**First, the difference between a sprint and a marathon.** The fast food day is two short sprints; after each one there is a recovery window and a mistake can be corrected. The restaurant day is a single four-minute uninterrupted stress; there is no recovery inside it and mistakes accumulate. The same density, a completely different management problem.

**Second, the emptying of the evening.** The restaurant's fourth slot is 15% long and takes 12.47% of the customers; the hall is at 73.8%. Fast food's fourth slot is 30% long, takes 35.26% of the customers, and the hall is at 104.4%. The hall visibly emptying on screen is the exact counterpart of [10](10-cuisine-identity.md)'s "the rhythm of the crowd" line, and one of the six things that read from a distant camera.

**Third, the face turning away at the door.** Section 5.7: the complaining customer and the hurried student in fast food, the long-haul driver and the lunch-break worker in the restaurant. A different silhouette, different clothing, a different voice.

### 6.3 An honest summary

Of [07](07-cuisine-system.md)'s seven differentiation variables, the "rhythm" row **narrows but is not erased**. The part that narrows is severity, the part that is preserved is duration and continuity. Option A erased this row entirely; Decision G saves two thirds of it.

In exchange, part of the identity load shifts to other pillars. In [07](07-cuisine-system.md)'s own list, below rhythm, there is the **signature mechanic**, marked as "the most important row": the combo and the flow in fast food, the tab and the regular customer in Turkish. That is where the distinction rhythm cannot carry has to be carried, and [review/01-design-and-balance.md](review/01-design-and-balance.md) had already said that the tab in its current state is "a tax", that the amount, the term, the collection schedule and the probability of non-payment had not been written. **Because rhythm has shrunk, writing the tab is no longer optional.** That is a dependency and it is this decision's cost.

---

## 7. What will change, and where

### 7.1 Question-by-question answers

| File | Does it change | What and why |
|---|---|---|
| `content/archetypes/fastfood.json` | **No** | `arrivalWeightsBp` stays exactly as it is. The measured 11.05%/37.49%/16.20%/35.26% profile is feasible with 20%/30%/20%/30% durations (section 5.2). The work Decision F pushed onto this file is **cancelled** |
| `content/archetypes/turk.json` | **No** | Same. The 9.37%/60.63%/17.53%/12.47% profile is feasible with 12%/48%/25%/15% durations (section 5.3). The work Decision F pushed onto this file is **cancelled** |
| `content/archetypes/shared.json` | **No** | No shared archetype changes, the courier included |
| `content/economy.json` | **No** | The slot duration is one value per cuisine; `economy.json` has no cuisine dimension and the file is generated by `export.py`. See section 7.2 |
| The crew ceilings (3/5/8/12) | **No** | Growing the ceiling is only option B's requirement. Decision G uses the ceilings as they are; in section 2.3 the twelfth staff member's contribution to the density ceiling is 0.7 points, negligible |
| The table ceilings (4/7/10/14) | **No** | The same reason |
| `tools/balance/model.py` | **No** | There is no concept of a slot in `model.py`; demand, crew, rent and margin are computed over the day total, and the day total did not change. The loss is a **reading correction**: week 8's revenue reads 1.06% lower in fast food, 1.70% lower in Turkish. Decision F wrote the same correction as 2.05%; the new number is smaller than that and inside the decimal slack of the margin targets |
| `tools/balance/solve.py` | **No** | The rents and expansion costs are solved from tier capacity; the capacity did not change |

**The answer to all four questions is no.** That is the decision's strongest side and where it parts from options A and B: A would rewrite twenty-four archetypes, B the entire economy.

### 7.2 What will change

| File | What | Why |
|---|---|---|
| **New** `content/cuisines/fastfood.json` and `turk.json` | `slotDurationsBp: [2000,3000,2000,3000]` and `[1200,4800,2500,1500]` | The shape of the day, per cuisine. `economy.json` is cuisine-independent, `archetypes/*.json` is a list of archetypes; neither is the right home for this field. The same file will also carry the per-cuisine `T_EAT_PARTY` from [27](27-time-model.md) pending decision item 1 |
| `tools/content/gen_archetypes.py` | Generate and validate the duration array: four values, summing to 10,000, each × 4,800 ÷ 10,000 an integer | The same pattern as the generator's existing `arrivalWeightsBp` validation (lines 288-291) |
| `tools/balance/export.py` | Generation of `content/cuisines/*.json` | The rule of leaving no hand-written number |
| `unity/.../Core/Sim/TimingConfig.cs` | A `SlotTicks` array next to `SlotCount`; its sum must be `ServiceTicks` | The equal-slot assumption goes away |
| `unity/.../Core/Sim/Simulation.cs` | Inside `BuildArrivalPlan`, `slotTicks = _timing.ServiceTicks / _timing.SlotCount` → an array lookup and a pre-summed slot-start offset | A one-line breaking point |
| `unity/.../Core/Content/ContentSet.cs`, `Content/ContentDto.cs`, `Content/ContentSetLoader.cs` | The `slotDurationsBp` field and its validation | The same pattern as `arrivalWeightsBp` |
| `tools/balance/timing.py` | **Done.** Added: `PATIENCE_HEADS_TURK`, `GROUP_SIZE_SEATED_TURK`, `SLOT_SHARE_CONTENT_FF/TR`, `SLOT_DUR_EQUAL/FF/TR`, `slot_ticks()`, `pool_needs()`, `retain_share()`, `flow_day()`, `max_density()`, `peak_report()`, `--peak` | No existing constant, function or decision was altered; the 47 tests pass unchanged |
| [12-economy.md](12-economy.md) §5.6 | The table will have two columns: share **and** duration. The written values will be replaced with the measured ones (section 1.2). The "sixty percent in a single slot" sentence stays, "the slot is 48% of the day" is added | The written table did not match the content |
| [27-time-model.md](27-time-model.md) §5.3, §5.4, §5.5, Decision F | Decision F is **superseded**. "Maximum slot share 28.2%" → "maximum density 1.1262; 28.2% on equal slots". The renormalisation proposals in §5.4 option 3 and §5.5 are cancelled | This file |
| [27-time-model.md](27-time-model.md) §8 | The `arrivalWeightsBp` jobs in the `content/archetypes/*.json` rows will be deleted | Same |
| [27-time-model.md](27-time-model.md) §7 | The `Maximum slot share 28.2%` row becomes `Maximum density 1.1262` | Same |
| [27-time-model.md](27-time-model.md) §9 | "46 tests" → 47 | The number was already out of date; `--check` prints 47 checks |
| [23-core-contract.md](23-core-contract.md) | That the slots are not equal will be written into the core contract | The determinism contract |
| [13-data-schemas.md](13-data-schemas.md) | The `cuisines/*.json` schema; the slot duration sum will be added to the validation list in §260 | So the schema list does not stay incomplete |
| [16-screens-and-tutorial.md](16-screens-and-tutorial.md) | The day progress bar will **not** draw the slots equal | [review/05](review/05-player-experience.md) problem 4 already wanted a progress bar with slot markers; the bar now also shows the cuisine's identity |
| [10-cuisine-identity.md](10-cuisine-identity.md) | The "daily rhythm" row will be rewritten in terms of duration and continuity (the section 6.2 table) | The rhythm pillar narrowed, its definition should be updated |

**None of this was done in this job.** This file carries only the decision and its arithmetic.

---

## 8. What is left open

1. **`T_EAT_PARTY` per cuisine.** [27](27-time-model.md) pending decision item 1. If the sitting time in Turkish cuisine is longer than fast food's 38,000 ms, the table turn lengthens, table pool utilisation rises above 101.6% and **the binding pool could move from the hall to the tables.** If it does, the lunch slot's duration has to be pulled above 48%. This file's Turkish numbers assume `T_EAT_PARTY = 38,000` and that assumption is probably wrong.
2. **Italian and Japanese.** The durations in section 5.9 are provisional; once the archetype pools are written, the loss calculation has to be done. The Italian's 65% evening, together with the long sitting, could make the table pool binding.
3. **The reputation formula does not normalise by volume.** [12](12-economy.md) §5.5's example gives +8 at 40 customers; the same formula gives +28.57 at 97. A weekend day without a peak carries reputation from 0 to 100 on its own. Section 5.6's ratios (48%, 51%, threshold 52.8%) are independent of that scale and stay the same even if the formula is fixed, but **the formula itself is outside this file's scope and it is broken.** It has to be solved in a separate job.
4. **`docs/14` carries two different crew ceiling tables.** "How many people can you employ" says 2/4/6/8, "Crew ceiling" says 3/5/8/12. `model.py` and this file use the second. The first should be deleted.
5. **Playability verification of the closed-loop model.** Three of the five assumptions in section 3.3 pull the loss down. The real number is in the band between the open loop (13.36% fast food) and the closed loop (3.15%). The C# core already simulates customer by customer; a batch run over `Simulation` will say which of the two models it comes out closer to. **Until that measurement is made, section 5's loss figures should be treated as a lower bound.**
6. **The arrival distribution within a slot.** The model assumes a uniform distribution within a slot, and `BuildArrivalPlan` does the same (`_rngArrival.NextInt(slotTicks)`). A real lunch rush has a peak inside the slot too. Raising the slot count from four to eight would push that detail into the content; not opened up for now, because eight slots do not read on [16](16-screens-and-tutorial.md)'s day progress bar.
7. **The `serviceMs` field.** `content/economy.json` still carries `serviceMs: 120000`. [27](27-time-model.md) §2.4 said it should be `tableTurnMs: 125525`; it still has not been done and this file does not touch that change.
