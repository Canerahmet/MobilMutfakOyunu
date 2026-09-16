# 48 — The sharpness of the day: why the pressure never fired

*14 September 2026.* Every run of the tour produced the same three lines:

```
UNMEASURED: Tea to the hall (0 waiting tables)
UNMEASURED: The crisis strip when someone leaves a table angry (0 angry)
UNMEASURED: The strip was built when a critical table was seen (0 critical tables)
```

All three measure **pressure** and all three were unmeasurable. The store text,
meanwhile, says "SERVİS SIRASINDA SEN VARSIN" ("SERVICE IS WHERE YOU ARE",
[44](44-store-texts.md)) — the intervention, the tea, the crisis strip and the
signature mechanics are all built on top of that pressure.

---

## 1. Root cause: the day was almost flat

The arrival **shares** sit in the archetypes, the slice **durations** sit in the
cuisine. A slice's intensity is the ratio of the two — and once weighted by
traffic:

| | lunch share | lunch duration | intensity |
|---|---:|---:|---:|
| fastfood | 37.6% | 30% | **1.25×** |
| turk | 59.3% | 48% | **1.24×** |

The busiest moment of the day is only a quarter above the average. The crew,
meanwhile, is built from the day's **total** work (`StaffingModel.Required`) — so
a 1.25× peak is absorbed comfortably. No queue forms, no patience runs out,
nobody gets angry.

**In the Turkish cuisine the content contradicted its own description.**
`ui.cuisine.turk_desc` says "Sert öğle zirvesi" ("Hard lunch peak"); but lunch
was the **longest** slice, covering 48% of the day. The promised peak was a flat
spread over half a day.

---

## 2. The durations changed, the weights did not

The distinction is deliberate: because the arrival shares stayed fixed, **the
daily customer total did not change** — the same customers were simply squeezed
into a narrower window.

Why it matters: the rent and margin calibration
([solve.py](../tools/balance/solve.py)) is solved over the daily total. Had I
changed the weights, the whole economy would have had to be re-solved; playing
with the durations left the calibration valid.

Besides, the arrival shares are the archetypes' **identity** — "the tradesman
comes at lunch, the student in the evening". Changing them wholesale would have
flattened the character of twenty archetypes.

| | new durations (bp) | new intensities |
|---|---|---|
| fastfood | 2500 / 1800 / 3500 / 2200 | 0.52 / **2.09** / 0.46 / **1.50** |
| turk | 1200 / 2800 / 4500 / 1500 | 0.88 / **2.12** / 0.40 / 0.82 |

The two cuisines now differ **in rhythm** as well: fast food gets crowded twice
during the day (lunch and evening), Turkish explodes at lunch and calms down in
the afternoon. Cuisine identity is not only in the menu, it is in the shape of
the day.

---

## 3. The game changed — and it was measured

Harness, Turkish cuisine, 8 seeds × 60 days:

| strategy | before | after |
|---|---:|---:|
| `baskili` — short crew, passive | **22,888** | 13,314 |
| `baskili_mudahale` — short crew, **playing** | 23,393 | **18,267** |
| `makul` — full crew | 21,968 | 17,351 |

Two things got better at once:

**The value of intervening went from +505 to +4,953.** The mechanic is no longer
scenery.

**The most profitable game changed.** Before, "build a short crew, do nothing"
was best (22,888) — that is, the exact opposite of what the game sells was being
rewarded. Now working short-handed and **playing actively** (18,267) beats both
the passive short crew and the full crew.

Nobody goes under: `makul` 17,351 / reputation 75, `planci` 23,375 / reputation
94.8. The game got harder, not unfair.

---

## 4. My hypothesis was refuted: the combo is a separate matter

I thought the combo came from the same root — "the bottleneck is not in the
kitchen but in the hall". I measured after sharpening and it was **wrong**:
`imzaci` 19,268, `zirvede_kapat` 19,048, service 1919 against 1923. Still the
same game.

But the same measurement said something more valuable: in fastfood `makul` is
17,909 and `imzaci` 19,268 — so **the combo is worth +1,359 and the mechanic
works.**

The problem is not in the mechanic but in the **assumption**
[45](45-design-review.md) §18 protects: "closing at the peak is a legitimate game
and the axis should not punish it". The measurement said twice, under two
different shapes of day, that **that game does not exist**. There is no
alternative to protect.

*An axis left flat in order to protect a strategy that does not exist.*

---

## 5. The test that broke was right

`The_weekend_is_busier_than_a_weekday` broke: 6 on a weekday, 6 at the weekend.

The reason was legitimate. The test was running a starving restaurant (one cook,
no stock refresh); when the day sharpened, waiting got longer, satisfaction fell
and reputation dropped **from 33 to 11** in seven days. Falling demand swallowed
the weekend multiplier — 9 parties on day 6, 4 on day 7.

So while asking "is the weekend busier", the test was really asking "is the
reputation spiral faster than the multiplier". With the stock refreshed it
measures what it claims: reputation 33 → 50, 7 on a weekday, 10 at the weekend.

---

## 6. The tour was tuned to the new rhythm too

The tour dropped two checks on its first run and the reason was instructive:

```
UNMEASURED: No occupied table was found during service (service 70%, today 12 people were expected)
FAIL : Someone was seen washing at the sink (0 frames)
```

There was a contradiction inside the tour: the liveliness window could run as far
as **80%** of service, but the table hunt gave up after **70%**. As long as the
day was flat it stayed invisible — the tables were full all day, so the order did
not matter. Once the day sharpened, the window ate the peak and left the table
hunt an empty hall.

The window now stops at the halfway point of the day (the real-time budget is the
same, only where in the day it stops has changed).

*A tool's assumption stays invisible until the thing it measures changes.*

### Two pressure checks fired for the first time

After the tuning the tour passed 134, failed 0 — and **two** of the ones that had
been unmeasurable for months now run:

```
ok   : Tea to the hall (1 waiting table)          previously "0 waiting tables"
ok   : The intervention allowance went down       previously never measurable
  DIAG waiting for the hall to fill: 0.6 s, service 14%, occupied tables 2
```

The hall fills at **14%** of service. The tea button found someone to be sent to
for the first time; the intervention allowance was spent for the first time.

### The crisis strip: the measurement itself was in the wrong place

The remaining two checks ("angry customer", "critical table") still did not fire,
and the reason was not the shape of the day: **the measurement was inside day 1's
inspection block** — four tables, twelve people. A crisis there is structurally
impossible. So the check looked like it was running and measured nothing for
sixty days.

Two changes: the measurement was moved across the whole campaign, and on days
20-21 (the weekend) the tour **deliberately works one waiter short** — which is
also the game the game rewards ([47](47-recognition-and-report-card.md)'s "You
ran the peak short-handed" badge recognises exactly this).

When I first wrote the total I put it in the morning, and because
`AdvanceToNextDay` had just zeroed the counter the total always stayed zero — the
check would run and measure nothing. The same class of error, the same day, for
the third time.

The result, the **first verification** of the crisis strip:

```
  DIAG campaign crisis: 30 angry at their tables, the strip 186 times
ok   : The crisis strip was built when guests got angry in the campaign (30 angry, the strip 186 times)
```

The two old checks on day one were **deleted**. A check that can never run is
worse than one that does not exist at all: the second says it is missing, the
first produces an illusion of coverage — and it produced one for months.

---

## 7. The same class of error three times in this round

1. Treating `link.xml` as correct by reasoning ([46](46-shipped-binary.md))
2. Adding the `badge.` family only to the generator and not to the test
   ([47](47-recognition-and-report-card.md))
3. Putting the crisis measurement somewhere it could never run (this document)

All three confirm the same sentence: **a check that does not run looks, from
outside, exactly like one that passes.** The cure was the same for all three —
forcing the check to break: removing `link.xml` and making the tour fail,
deleting the family and making generation refuse, deliberately cutting the crew
and creating the crisis.
