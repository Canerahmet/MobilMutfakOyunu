# 64 — What the game is about, and where it contradicts itself

19 September 2026.

> *"oyunu filozofik incelemesini soylesem faydasi olur mu?"* ... *"olur devam et."*
>
> *("Would a philosophical review of the game help?" ... "Yes, go ahead.")*

Three independent readers were given the same two questions — *what is this
game about, and where does the design contradict itself?* — from three
angles: the thesis, the sixty-day arc, and an adversary whose only job was to
find how "saying more" would make the game worse and to hold every claim to
the harness. This document is the synthesis. Every claim below is marked as
one of three kinds:

- **measured** — a number from the harness or a test, reproducible;
- **read** — a fact about the code or the content, with a file reference;
- **held** — a judgement. Stated as one, so it can be argued with.

Where the three readers disagreed, the disagreement is kept, because in
[62](62-service-agency.md) that was where the answer came from.

---

## 1. The thesis, and what delivers it

**Read.** README and [02](02-design-proposal.md) §1: *"You are not the cook,
you are the owner: you set the menu, the prices, the supply and the crew;
during service you only step in when something breaks."*

**Measured.** The morning delivers it. The crew decision is a real trade:
running one waiter short and covering it yourself is the second-best strategy
in the harness ([42](42-crew-and-intervention.md) §4, [62](62-service-agency.md) §7).
The market is a real trade: this week's audit found the four-table Turkish
shop's whole margin sitting in what it bought and threw away
([63](63-calibration-audit.md) §10-12). Those are an owner's decisions.

**Read.** The service phase, which the README leads with (*"this is where you
are"*), is thinner than its billing. A table has no seat count in the core;
every table is interchangeable, so "which table" is a null choice ([62](62-service-agency.md) §4).
Staff have no location. The owner's presence during service is a
regenerating pool of three charges over interchangeable tables.

**Held (reader 1, and I agree):** the game is about *the morning*. The
marketing is about the service. The one-word tell: the year-end signature
axis is labelled `score.signature` = **"Kitchen"** — the owner-not-cook game
names its owner's signature after the room the owner is not in.

## 2. Does the tab say anything?

The Turkish signature is the running tab — *veresiye* — and the cuisine
screen makes a one-line claim: *"Running a tab: hurts your cash flow, builds
loyalty"* (`ui.cuisine.credit`).

**Measured, first half.** It hurts: `imzaci` (runs tabs) ended with 16,294
against `makul`'s 21,185 — 23% of the cash — at 8 seeds, and 16,092 against
19,561 at 32.

**Measured, second half — and it was false.** "Builds loyalty" is a mechanic
in the code: every settled account adds 60 bp to a demand bonus, capped at
15% (`_creditLoyaltyBp`). It was applied in exactly one place —
**inside the market recommendation**, after the demand gate had returned. So
the neighbourhood's trust raised the number of people the player *bought
stock for*, and not the number who *arrived*. The bot that ran tabs served
**fewer** people than the one that did not (1,829 against 1,846) and spoiled
2,700 coins more. The gate's own comment warned about *"recommending stock
for customers who would not actually arrive"* — one mechanic before it
happened.

Moved to the gate, where every other demand term lives:

| Turkish, 8 seeds | before | after |
|---|---:|---:|
| `imzaci` served | 1,829 | **1,921** (now above `makul`'s 1,846) |
| `imzaci` end cash | 16,294 | **18,051** |
| `imzaci` reputation | 70.9 | 73.2 |
| the tab's cash price against `makul` | −4,891 | −3,134 |

Guarded by `CreditLoyaltyTests`: once loyalty amounts to a whole person, the
stock bought for a main dish must be built from the same people the arrival
forecast reports. Proved red under the old placement (2,340 g bought for a
2,184 g forecast). And one thing the guard taught on the way: a settled tab
is 60 bp, and on a forecast of twenty people 0.6% rounds to nothing — the
bonus does not act until it has accumulated to a person. The first version of
the test stopped at the first settlement and its red proof *passed*.

**Read.** What the tab does *not* do. A default touches the shop's reputation
and the global bonus; it writes nothing to the regular who defaulted —
`_regVisits`, the story beats, the upset days are untouched. *You can stiff
Hasan Usta and he comes back tomorrow unchanged* (reader 1). No story beat
opens or closes on a tab; they gate on visits and satisfaction only. The
English tables rename the neighbourhood — Hasan Usta is *"Old Harry"*,
Nazife Teyze is *"Auntie Pat"* — so trust, memory and the neighbourhood
survive only in the Turkish table.

**Measured (adversary).** Selectivity buys almost nothing: `secici_veresiye`
(tabs only for regulars with five visits) 17,346 / signature 73 against
`imzaci` 16,294 / 74. The whole skill gap in the mechanic is **one axis
point** and a thousand coins.

**Held.** The tab is a receivables screen with a good name. The scoring
stance is the right one — the axis rewards *collecting what you lent*, i.e.
trust honoured, not profit — but nothing in the sixty days lets a player
*earn* that stance or *lose* it with a person. The adversary's rule from
[53](53-pending-decisions.md) applies: never write a sentence the simulation can
contradict, and "he knows you remember" would be one, today.

## 3. The employer

**Read.** [14](14-staff-system.md) promises severance of a week's wage, a
−10 morale hit to the rest on a firing, raises, days off. `Fire()` in the
core is an array shuffle: zero cost, no morale event. `CommandKind` has no
Raise and no DayOff. The morale code's own comment says climbing above 70
*"requires the player to DO something (a rise, a day off — the event list in
docs/14)"*, citing levers that do not exist. The firing dialogue frames the
loss as *yours*: *"Their experience goes with them."*

**Measured (adversary).** The crew axis is `(roster fill + average morale) / 2`.
`fazla_kadro` — crew inflated to the cap, a net loss of 3,350, end cash 4,650 —
gets the **best crew mark of all twenty-five arms**: 85, against `makul`'s 75
and `planci`'s 72. As scored, being a good employer means hiring to the cap;
as paid, [14](14-staff-system.md) says *"an extra person hired is written
straight to the loss"*; and a badge (`short_peak`) celebrates running the peak
short-handed. Three signals, three directions.

**Held.** The design has a stance on *capacity*, not on employment. That is
not a flaw in itself — it is the honest state of the code — but the score
claims a stance the economy contradicts.

## 4. The sixty days

**Measured (reader 2, `makul`, four seeds, Turkish, by week).**

| week | tables | crew | guests/day | revenue/day | reputation | cash delta |
|---:|---:|---:|---:|---:|---:|---:|
| 2 | 6.2 | 3.5 | 24 | 1,644 | 63 | −412 |
| 3 | 7 | 4 | 33 | 2,111 | 73.5 | +2,142 |
| 4 | 7 | 4 | 34 | 2,010 | **75.0** | +1,184 |
| 5 | 7 | 4 | 32 | 2,067 | 74.1 | +138 |
| 6 | 7 | 4 | 33 | 2,170 | **75.0** | +136 |
| 7 | 7 | 4 | 33 | 2,300 | 75.0 | +1,297 |
| 8 | 7 | 4 | 34 | 2,418 | 74.7 | +1,538 |

From day 22 to day 60 the reference player's campaign is the same day
thirty-eight times with a slowly larger till: identical crew, identical
covers, reputation pinned at exactly the tier ceiling from week 4. Where a
shape exists — expansion weeks that lose money and a week 8 that collects —
only `planci` produces it, and the player writes that shape by choosing to
expand; the game never imposes it.

**Read.** What is scheduled, and whether the player can tell:

| | player notices? |
|---|---|
| a dish unlock every ~2 days | yes — the one steady drip |
| named regulars from day 3 to 44, story beats on visits | yes |
| rent every seventh day, the weekly report card | yes — the metronome |
| **day 16: the tab / the combo opens** | **no notice, no hint, no string: a button silently appears** |
| **days 16, 31, 46: the seasons** | **invisible: an ingredient price multiplier and nothing else; no name, no line, `DemandModel` has no season term** |
| the critic's visit in season 3, the rival in season 4 ([09](09-content-inventory.md)) | **not implemented**; the critic is a random rare archetype |
| the landlord's five-step ladder ([02](02-design-proposal.md)) | the ladder fires in one call on a bad rent day; there is no warning rung and no landlord text |

**Held (reader 2):** *there is no point in the sixty days at which something
happens* to *the restaurant.* Every change is player-initiated or a per-day
dice roll. The last week is the fifth week with a bigger number.

**Held (adversary, disagreeing):** the project moved *away* from calendar
beats twice on evidence — [45](45-design-review.md) §13 found a seasonal
demand multiplier groundless, [34](34-progression-and-locks.md) §3 moved the dish lock from
a calendar to an achievement — and two of the seven axes (resilience,
regulars) no longer discriminate between anything a competent player does.
Content paid into a saturated axis is invisible. The disagreement is real and
it is the right one to have.

## 5. The year-end score

**Read.** Seven axes 0–100, *"none stands in for another; it should be
possible to score well by growing and equally by running a small but
well-loved shop"* (`Score()`, [08](08-endgame.md)).

**Measured.** Three of the seven barely respond to play. Resilience is 100 for
twenty of twenty-five arms. Regulars is 96 for `makul` and 100 for the
cheap-price bot. Place is 28 or 50 for everyone who does not expand. Wealth
and Place reward the same decision. The signature axis is 0 for a Turkish
player who never opens the book, so that player's total caps near 86
whatever else they do. The plaque is four headlines; the critic's piece that
was to *"refer to your game"* ([18](18-story-and-text.md)) does not exist,
so the evaluation lists numbers the player already saw last Sunday.

## 6. The contradiction to fix first

Reader 1 named it and the other two supplied the evidence: **the game tells
the player seven axes, and its designer's only instrument reads the till.**
Every failing check in [63](63-calibration-audit.md) is cash. [12](12-economy.md) §8b
says *"money is not this game's goal"*. Nothing in `calibrate.py` scores the
plaque; the harness prints the seven axes and nothing asserts on them.

That is why the crew axis contradicts the crew economy (nobody tuned the
axis), why the plaque's summit is empty (nobody tuned to it), and why the
short-handed badge exists (cash said it was fine). A critic cannot tell
whether Lokanta is a game about a *good* restaurant or a *profitable* one —
and neither, on the evidence, can its own tooling.

**Held.** Fix the instrument before the game. Until the calibration scores the
plaque, the seven axes are a preface to a one-number game, and every "say
more" proposal above would be tuned by feel, which is how [45](45-design-review.md) §10
protected a way of playing that turned out not to exist.

## 7. The adversary's case, kept whole

Not everything should say more. The evening report is a profit-and-loss with
reasons (*"nobody took the order"*, *"the food never came"*); reputation is
damped on gain and undamped on loss; tenure produces one line — *"Doesn't
need to ask any more"* — derived from thirty days the simulation counted.
Every text that shipped and held up was a label on a measured state; every
text that failed ([55](55-translation-review.md) found seven) was a claim
about the state. The day is already twice the length [02](02-design-proposal.md)
promised, five languages carry every string, and nothing measures taps or
reading time. *Meaning in a management game is in its systems.* Held, and
worth holding against every proposal in §8.

## 8. What follows, in order

Each item is either measurable with the existing harness or it is not, and
that decides its place in the queue.

1. **Score the plaque in the calibration** — assert on the seven axes, not
   only the till. Measurable today: the columns exist. This is the
   prerequisite for everything below; without it §3 and §5 cannot be fixed
   without being un-fixed by the next sweep.
2. **The crew axis must not be topped by the overstaffer** — `fazla_kadro`
   85 against `makul` 75 is a target, not a taste.
3. **The tab's loyalty reaches the door — done** (§2). Next, and measurable:
   a defaulter is remembered *by that regular*. The `secici` arm is the
   instrument; today its gap is one point.
4. **Announce what the calendar already does**: the tab and the combo arrive
   on day 16 with no string, and the seasons change prices with no name.
   These are labels on measured states — the kind of text that holds.
5. **Firing has the cost [14](14-staff-system.md) promised, or the document
   stops promising it.** Either is honest; the current state is neither.
6. The arc (§4) is the real design question and it is deliberately last:
   the two readers disagree, the measurement (a flat `makul` from week 4)
   supports the complaint, and the counter-evidence (two calendar mechanics
   removed on measurement) is also real. It needs an arm before it needs a
   beat.

## 9. What was done, in the order of §8

> *"devam et. tum sureci bitir. sonrasinda tekrar konusalim"*
>
> *("Go on. Finish the whole process. Then we talk again.")*

**1. The plaque is scored.** `calibrate.py` now parses the harness's score
table alongside the till, and its first axis assertion is the one the review
caught lying: the overstaffer must not top the crew axis.

**2. The crew axis measures the roster against the weekend peak's need**,
capped at 100, instead of against the cap. Hiring to the ceiling no longer
raises the mark. Guarded, and proved red.

**3. A defaulter is remembered by the regular who defaulted.** Two visits'
worth of trust off the *capped* value, per bad account, visible where the
ledger shows the chance. The first version took it off the raw visits and a
regular of twenty-two visits lost nothing: 8,800 less 800 is still over a
ceiling of 3,000 - the memory was invisible for exactly the long-standing
regular it was written for, and the guard said so. Save version 25.
Measured: `secici_veresiye` over `imzaci` widened from +1,052 to +1,219 and
one axis point to two; a small gap, now a real one.

**4. The calendar announces what it does.** The tab and the combo say so on
the day they open; the seasons turn with a name, in five languages. The
Chinese line for severance was reworded twice to stay inside the font
subset, and the subset was rebuilt from the strings (998 characters, all
covered) - the check that had found the Chinese build broken in three places
([55](55-translation-review.md)) found this one before it shipped. The score
label "Kitchen" is "Signature".

**5. Letting somebody go costs what [14](14-staff-system.md) said**: a week's
wage from the payroll's own table, and ten points off the morale of everyone
who stays. Proved red - 716 coins and 70 → 60 on the colleague.

### And the bots had to learn the price

The day severance arrived, the reasonable player's wages went from 25,848 to
**35,896** in fast food and its year from 21,546 to 15,489. It was not the
economy; it was the bot playing by the old price: a waiter hired for
Saturday sacked on Wednesday and hired back on Friday, a cook the same, and a
replace-the-worst rule tuned when a sacking was free.

The harness counts firings per strategy now, because the wages column rose
and the reason had to be inferred. Measured, 8 seeds:

| `makul`, per eight campaigns | let go | Turkish | fast food |
|---|---:|---:|---:|
| old rules under severance | 27 / 46 | 17,171 | 17,235 |
| surplus judged against the weekend peak, replace-the-worst at 1,200 | 27 / 46 | 17,171 | 17,235 |
| … at 2,000 | 20 / 33 | 17,491 | 19,000 |
| … at 3,000 | 3 / 18 | 18,413 | 19,115 |
| … switched off | 2 / 17 | 18,484 | 19,258 |

Under a real severance, replacing a slightly better waiter never pays at any
threshold; 3,000 is indistinguishable from off and keeps the mechanism for
the genuinely bad. This is the fourth bot this week to read as a broken
economy, and the first one caused by a change made the same day - which is
the argument for the counter.

**6. The arc is left as §4 states it**: a disagreement between two readers
and a flat `makul` from week 4, waiting for an arm before a beat.

### Verification, 32 seeds, both cuisines

```
penalty 12  fastfood: the overstaffer tops the crew axis (85 against 78);
            turk:     the overstaffer tops the crew axis (85 against 79)
```

Growth, the signature, the planner, cheap ingredients: all clean. The one
line left is the crew axis, and it is no longer the roster: with that half
fixed, the overstaffer wins **by morale**. An idle crew is a happy one; the
reasonable player's is busy and, two or three times a campaign, let go. The
axis is telling the truth.

What it exposes is §3's missing half: [14](14-staff-system.md) promises a
raise and a day off, and neither command exists, so the reference player has
no way to lift a busy crew's morale except to hire people it does not need.
That is not a fault the realisation rate can reach, and a penalty the sweep
cannot answer steers it toward whatever masks the symptom
([63](63-calibration-audit.md) §8) - so the check now weighs nothing and
prints on every run, until the levers exist and it can be turned back into a
penalty with a target.

**Open, in the order of §8:** the morale levers (a raise, a day off - the
measurable claim is that `makul` can reach the overstaffer's crew mark
without its roster), then §4's arc.
