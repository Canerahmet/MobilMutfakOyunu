# 45 — Design review and measurements

*13 September 2026.* Five agents assessed the game along five design axes:
economy/progression, moment-to-moment decision quality, the first session and
readability, cuisine identity, endgame and motivation. None of them changed a
file; all of them gave evidence (`file:line`) and a concrete proposal. This
document writes up the **measured** and **closed** part.

The rule, as everywhere in this project: *a finding's diagnosis can be right and
its remedy wrong.* Every claim was put either to the code or to a measurement
before it was accepted — and one of them was **refuted** by measurement.

---

## 1. The biggest hole: raising prices at the reputation ceiling was free

Price had **no direct channel to demand at all**. `DemandModel.CustomersPerDay`
took no price parameter; the only route price had was satisfaction → reputation.
And reputation is **hard-clamped** to the table tier's ceiling (55/75/90/100). So
for a player pressed against the ceiling, a loss of satisfaction bought nothing.

The balance bots sat at 8500 (cheap), 13000, 23000 and 24000 bp — **there was no
measurement at all between 10000 and 13000**, and the hole was exactly there. A
bot was placed inside the band (`orta_fiyat`, 11000 bp):

| strategy | end cash | rep | tabl | crew |
|---|---:|---:|---:|---:|
| makul | 18,670 | 79.3 | 7.9 | 4.3 |
| planci | 25,092 | 94.6 | 11.8 | 7.4 |
| **orta_fiyat** | **27,849** | 75.0 | 7.0 | 4.0 |
| yuksek_fiyat (+30%) | 380 | 0.0 | 4.0 | sinking |

A button pressed once in the morning was beating the game's most sophisticated
strategy **with fewer tables and a smaller crew**. The penalty existed only
outside the band.

**The fix:** `priceElasticityBp` (9000) — a price deviation now affects demand
directly, bounded by a floor and a ceiling.

| strategy | before | after |
|---|---:|---:|
| orta_fiyat | 27,849 | **19,393** |
| makul | 18,670 | 18,670 |
| planci | 25,092 | 25,092 |
| ucuz_fiyat | 12,498 | **15,059** |

**Every** strategy that plays at the market price stayed exactly the same: the
channel only engages on a deviation. Raising prices is now a trade —
`orta_fiyat` is 4% ahead but serves 1,659 people (instead of 2,034), its
reputation is 72.8 and it stays at 6.9 tables.

An unexpected gain: **cheap pricing became a real strategy** (12,498 → 15,059,
2,044 → 2,335 people). The volume game and the margin game are two separate
legitimate paths; cheap pricing used to be simply bad.

### Weights and a single gate

The menu deviation is weighted by the order probabilities — **exactly the same**
weights as `RecommendedRestock`. With two separate sets of weights the market
screen and the demand would contradict each other.

Demand was being computed in six separate places (today's crew, tomorrow's crew,
peak crew, recommended stock, expected people, arrival plan). They all go through
a single helper: missing one of them would mean the market screen recommending
stock **for customers who will not actually come**.

A **structural** test protects this
(`PricingTests.Demand_passes_through_a_single_gate`): inside `Simulation`, only
`ExpectedCustomers` may call `CustomersPerDay`. I first tried to measure it by
behaviour and it did not work — the crew and the stock are integers and on day 1
are already at the floor, so the difference rounds away and the test stayed green
in a vacuum. The liveliness check caught it. That the scan can be broken was
verified too: without the exclusion it finds exactly 1 call.

---

## 2. One of the three verbs was dead: the tea

The tea was below the owner's attention on every axis — satisfaction 900 against
2,400, patience ×1 against ×2, it does not speed up the kitchen — and **on top of
that it took money out of the till**; attention is free. Since they burn the same
intervention budget, there was no day on which to press the tea. The project's own
balance bot never pressed the tea either; that was one of the pieces of evidence
for the finding.

**The fix:** the tea now goes to **the room** — to everyone waiting. The two
answer different questions:

| verb | to whom | when |
|---|---|---|
| Attention | one table, deep (×2 patience + moving the kitchen job forward) | the single table in crisis |
| Tea | everyone waiting, shallow | when the room as a whole is impatient |

Its cost comes from the same place: the tea costs as many people as there are
waiting in the room — in a crowd it is both the most valuable and the most
expensive. If nobody is waiting, the button is disabled.

Two bugs were caught by the test: the tea no longer needs a target, but the branch
was **below** the party validity check, so the `-1` the interface sends when
nothing is selected was being silently rejected — the button did nothing.

---

## 3. A claim REFUTED by measurement: "the bot times its interventions badly"

The finding was this: the bot burns its interventions every 20 sim-seconds, so
four interventions a day are gone in the first ~80 seconds of a 480-second day,
while the peak is in the second slot. So the question "does intervening pay" could
have been measuring the player's **one real decision** ("now, or at the peak")
held fixed — and fixed at its worst value. The diagnosis was plausible.

The experiment: `sabirli_mudahale` — the same verbs, the same order, the only
difference being that it spends nothing until a table drops below the warning
threshold. It is asked on every tick, because the crisis window is ~3 seconds and
a bot that looks every twenty seconds would miss it; then the measurement would
say "saving them up does not help" but what it measured would be its own blink.

| strategy | end cash | interventions spent |
|---|---:|---:|
| makul (none at all) | 18,670 | 0 |
| mudahaleci (immediately) | 18,869 | 5,784 |
| sabirli_mudahale (saves for the peak) | 18,777 | 1,186 |

**Saving them up did not pay.** The problem is not how the bot plays but the
mechanic itself.

The real information is inside the number: the patient arm made 1,186
interventions over 1,440 days — **0.8 a day**. Because in a restaurant with a
proper crew a table almost never drops below the warning threshold. The
intervention is a rescue tool for a situation the player almost never gets into
while playing properly; that is why its value is neutral.

**This contradicts the game's promise.** The store text (docs/44) says
"SERVİS SIRASINDA SEN VARSIN" ("SERVICE IS WHERE YOU ARE") and sells the four
interventions as one of the main mechanics. The measurement says the mechanic is a
safety net today.

An open decision — there are three paths, none of them a one-liner:
1. Make the crisis more widespread (make the game harder),
2. Give the intervention a job **outside** a crisis (tips, regular closeness,
   table turnover rate),
3. Fit the promise to the measurement and change the store text.

The number of interventions was meanwhile tied to the number of tables (4 tables
4, 8 tables 5, 12 tables 6): a fixed four was erasing the mechanic **exactly where
it is most needed**. On its own it did not change the result (18,859 → 18,869) but
it fixed something that was wrong.

---

## 4. Measures that lie

**The event's name wrote the wrong dish.** The `StockOut` event was broadcasting
the PARTY index in field `A`, while the interface took `A` for a dish index and
printed a name — since party slots are handed out from low numbers, most of the
time a **valid but wrong** dish name. Nothing raised an error. The event was not
"ran out of stock" in the first place: the customer turns away at the door because
there is no main dish on the menu that can be made. It is `TurnedAway` now and its
text tells the truth.

**Four dead commands.** `SetDailySpecial`, `AssignStation` and `RefillBroth` were
defined, were never sent from anywhere, and were not in `Apply`'s `switch` either
— the enum made three mechanics look as though they were promised. They were
deleted; the numbers were not renumbered, because the command type is stored in
saves as a number. `InterventionKind.Apology` was worse: if it had been sent, it
would have given **the tea's effect without paying for the tea**. Now 0 is `None`
and an unknown type is explicitly rejected.

**`StockDaysLeft()` counted dishes, not days**, and the warning text promised days
by saying "Stok bugünü çıkarmaz" ("Stock will not last the day"). The tick turned
green at `>= 1`: a player with one portion each of six dishes looked "ready", opened
service, and ran out of goods in the first ten minutes.

The name was fixed (`MakeableDishCount`) and the line now measures the day
(`StockCoverageBp`): **"Stok 8 / 13 kişiye yetiyor"** ("Stock covers 8 of 13
guests"). The yardstick is **the scarcest ingredient**, not the total — a kitchen
with twenty plentiful ingredients and one exhausted one looks full in total, but
every order wanting that ingredient turns away at the door. The requirement comes
from the market screen's own calculation, so the two screens speak from the same
source.

My first version of the text was **clipped** in both languages, and the tour's
clipping check caught it.

---

## 5. The campaign's goal was invisible

There was not a single line in the game saying "sixty days"; the seven-axis
evaluation only opened on day 61. The player ran a shop with no end date for
2.5 hours and then met a report card they had never heard of — a surprise, not a
goal.

The HUD now says **"40 / 60. gün"** ("Day 40 of 60") next to the phase; in free
play it turns into "serbest oyun" ("free play").

---

## 6. The same label, three different numbers

`ui.hud.angry` was used in three places: on the service card (TOTAL lost), on the
evening strip (TOTAL), and in the day report (only those who LEFT A TABLE). On the
same day, under the same word, the player saw two different numbers. The numbers
had already been separated; what was missing was separating the **names** — now
"Kaybedilen" / "Masadan kalkan" / "Kapıdan dönen" ("Lost" / "Left the table" /
"Turned away").

Customers turned away at the door are also visible **during service** now. It is
the commonest way to die in the first week and the number was only in the day
report, so the player only saw it when it was impossible to fix.

---

## 8. The tab became a book, not a bonus button

Whoever paid, paid **112%** of the bill, and the chance was a fixed 85% for
everybody (95% with tea). The expected cash was **1.064 × bill** — that is, a tab
was *more profitable* than a cash sale. There was no day on which to say "no".

The deeper defect was this: **the book did not record whose debt it was**
(`_tabAmount`, `_tabDueDay`, `_tabTea` — no link to a regular). Since the
collection chance was the same for everyone, the question "who should I write it
for" **could not even arise**.

The fix:

| | before | after |
|---|---:|---:|
| base chance | 8500 | **6000** |
| trust (per visit) | — | **400 bp, capped at 3000** |
| tea bonus | 1000 | 1000 |
| chance ceiling | 10000 (certainty) | **9500** |
| payment bonus | 1200 | **800** |

Somebody you have just met is at 60%, somebody who has been coming for years is
at 90%, with tea +10%. The ceiling is not full certainty — a risk-free book is
again a bonus button that produces no decision.

**Measurement (24 seeds, 60 days, turk):**

| strategy | end cash | tab | year-end score |
|---|---:|---:|---:|
| makul (never opens the book) | 20,822 | — | 61 |
| imzaci (writes for everyone) | 18,408 | 2,501 | 71 |
| **secici_veresiye (only 5+ visits)** | **19,289** | 2,519 | 71 |

Two nested decisions came out: *should I use the book at all* (cash ↔ year-end
score) and *who should I write it for* (being selective is **+881 coins** at the
same score). There used to be one answer: yes, to everyone.

The selective arm (`secici_veresiye`) was deliberately written as a separate
strategy — the same pattern had been used on the intervention and there it said
**the opposite** of what was expected, so this was not guessed, it was measured.

Two silent traps were closed: the default of the `_tabRegular` array was 0, and 0
is a valid regular index — an empty account would have used the trust of somebody
it had never met. And `content/cuisines/turk.json` is a **generated** file; if I
had edited it by hand it would have been silently reverted on the first audit.

`SaveVersion` 17 → 18.

---

## 9. The wealth axis was punishing investment

It was `worth = cash + tab`: the equipment and the tables owned
were **not counted at all**. So the more equipment you bought, the shorter your
"Wealth" bar got — the thing the game encourages came back as a penalty on the
report card, and the player could not see on any screen why their score was
falling as they played well.

The value of what is owned is found by **subtracting what remains** from the whole
catalogue. Writing a separate sum was deliberately avoided: two separate
calculations will drift apart one day and it will be impossible to tell which is
right — this file has already lived that mistake once (of two "what is left"
functions, one counted the cold store and the other did not).

| strategy | wealth (before) | wealth (after) | total score |
|---|---:|---:|---:|
| genislemeyen | 12 | 22 | 46 |
| makul | 25 | **54** | 64 |
| planci | 32 | **76** | 73 |
| imzaci | 27 | **57** | **78** |

The axis now discriminates: someone who grows and invests scores higher than
someone who keeps the money under the mattress. A side effect: the top plaque's
threshold is 80 and no measured strategy could get near it — meaning the summit
the player would chase was probably empty. `imzaci` reached 78; the summit now
exists and is hard.

---

## 10. An axis left UNTOUCHED: the combo target

It is true that the combo axis is also a participation badge: `imzaci` scores
**100** with a 17.0% share, the target is 15%, so it is at the ceiling and there
is no slope.

But the target's reasoning is **already written** inside `export.py`: *"the combo
also increases the kitchen load, so closing it at the peak is a legitimate way to
play and the axis must not punish it."* Raising the target breaks exactly that
decision — as long as the axis looks at the combo **share**, "keep it open" and
"close it at the peak" point in opposite directions.

Fixing it requires changing not the target but **the thing being measured** (for
example combo revenue per angry customer). That is a design decision, not a number
tweak; it was left untouched so as not to break a written rationale on my own
taste.

> **14 September addendum — the rationale was refuted.** This section was
> protecting a written rationale: "closing it at the peak is a legitimate way to
> play". §18 measured that way of playing, and `zirvede_kapat` and `imzaci` served
> **exactly the same 2,016 parties**. After [48](48-day-sharpness.md) sharpened
> the day it was measured a second time: 19,048 against 19,268, served 1,923
> against 1,919 — the same game again.
>
> Two separate measurements, on two differently shaped days: **there is no
> alternative strategy to protect.** The axis has been left flat in order to
> protect a way of playing that does not exist.
>
> The same measurement also said that the combo itself is sound: in fastfood
> `makul` is 17,909, `imzaci` 19,268 — the combo is **worth +1,359**. The mechanic
> works; the defect is only in what the axis measures.

The tab axis, on the other hand, **fixed itself**: because collection now depends
on trust, the bot that writes for everyone scores 72 rather than 100. The axis
asks "did you use it well", not "did you use it".

---

## 12. An overflow vessel for the reputation ceiling

The ceiling is a correct idea — a four-table shop cannot be the restaurant the
neighbourhood talks about — but **erasing** the overflow did one more thing: for a
player at the ceiling there was **no measurable difference at all** between a
perfect day and a day of muddling through. The measurement (docs/06): a good
player hits 75 at seven tables and stays there for thirty-two days — more than
half the campaign unrewarded.

This is the reverse direction of the rule in §1. There, *the price paid above the
ceiling was free*; here, *what was earned above the ceiling was being given away
free too.*

Overflow reputation now accumulates in `_reputationOverflowCenti` and **is paid
out on the day you expand**. The vessel holds one tier's worth: unlimited
accumulation would fling reputation straight to the ceiling on expansion day and
make the new tier's own effort meaningless.

**Measurement (24 seeds, 60 days, fast food):**

| strategy | cash (before → after) | rep | tabl |
|---|---|---:|---:|
| makul | 18,670 → 18,820 | 79.3 → 79.4 | 7.9 |
| planci | 25,092 → **23,921** | 94.6 → 95.0 | 11.8 → **12.0** |
| imzaci | 20,427 → 20,595 | 79.4 | 7.9 |

The direction is as expected: when reputation jumps, demand jumps too and the crew
and stock costs follow it — `planci` grows more but its cash falls. **A side effect
worth recording:** the `kredisiz` arm (the bot that refuses to take a loan) goes
into debt on day 56; growing faster, it overreaches. It was not tuned, it was
recorded — tuning it wants a measurement round of its own.

### I got the test wrong three times while writing it

1. The shop was playing without a crew → reputation **froze at 3,459** in 40 days.
2. Crew + expansion added → **froze at 3,730** in 120 days, ceiling 7,500.

The cause was not the service but the **scale**: at four tables seven parties are
served a day, the daily reputation gain balances the daily decay and the
equilibrium comes out at ~34.6 — while the ceiling is 55. So at that tier the
ceiling is **not binding at all** and no overflow forms. To press against the
ceiling, the test would have had to play as well as the harness bot, that is,
**write a bot inside the test**.

The right solution turned out to be lowering the ceiling **from the content**
(`EconomyConfig.WithTiers`, an addition that fits the file's own `With...`
family): the rule is the same rule, only the threshold at which it becomes visible
was brought closer. No back door, no setter.

`SaveVersion` 18 → 19.

---

## 13. Two items not done, and why

Two open items turned out to be **groundless** when the code was looked at. Both
are of the same class: the code had already made that decision and written down
its reasoning.

**The season's effect on demand.** The finding said "docs/34 says winter is the
busiest, the code only changes ingredient prices". Reading docs/34 §2: the season
is already implemented as a **decision** — buy cheap in autumn, store it in the
cold room, carry it into winter — and it has been measured (`planci` gains 9%,
17,722 → 19,327). The sentence "winter is the busiest period" is not a seasonal
multiplier but the fact that the shop is already large at the end of the campaign;
next to it is written "a structural side effect and **deliberately left**".

Adding a seasonal multiplier to demand would not be closing a gap, it would be
**inventing a new mechanic**.

**The equipment line in `model.py`.** The comment inside `week_pnl` already says:
*"EQUIPMENT IS NOT IN THIS LEDGER, and that is a deliberate decision. It was tried
and it sank: … the model went 73,000 coins into debt. The right place is the
simulation."* Equipment prices are tuned with harness measurement, not with the
closed-form model.

### But there were two real items inside it

**`REALISATION_BP` was two values in two files** — `model.py` 7000 (the one
written into the game's content), `solve.py` 9335. The calibration was not broken:
`solve_for()` patches `solve.py` for each candidate during the sweep, and when the
sweep ends the **last candidate tried** is left in there. But in the manual flow
docs/12 describes (`python solve.py`) this would have produced **the wrong rent**.
`calibrate.py` now writes the chosen rate back into `solve.py` too, and where the
value comes from is written down in the file.

**`MARGIN_TARGETS` was being read as "net margin".** What it is was written next to
its name: the margin **before capital expenditure**. The equipment ladder is
~37,600 coins at 14 tables and it is not in this ledger.

---

## 15. Demand now plays — but only the realised kind

Demand was entirely deterministic: every Tuesday at the same reputation and table
count brought **exactly the same** number of customers. The consequence was that
the morning stock decision was a button rather than a judgement — the market's
recommendation was always exactly right and the newly added line "Stok 8 / 13
kişiye yetiyor" ("Stock covers 8 of 13 guests") never turned red.

The whole mechanic is **in the distinction**:

| | what it gives | who uses it |
|---|---|---|
| `ExpectedCustomers` | the expectation | crew recommendation, market recommendation, expected people |
| `ActualCustomers` | the reality | **only** the arrival plan |

If the deviation were reflected in the expectation too, the player would again
have exact knowledge and the volatility would be decoration.

Three things were preserved: because the draw comes from the `_rngEvent` stream
(it already existed, it went into the save, it was never used), **a replay is
bit-identical**; the golden week test measures `WeeklyPlanner` so it was
unaffected; and the deviation is drawn with integer arithmetic (floating point is
banned in the core).

**The measurement has to be read honestly.** A ±10% deviation stays inside the
market recommendation's **20% safety margin** — `makul` 18,820 → 19,085, spoilage
4,960 → 4,829, so the difference is within the noise. The decision it creates is
not for the "buy what is recommended" player but for the player who **cuts** the
stock: cutting used to be a computable bet, now it is a real one. If it is wanted
to bite harder, the lever is not the volatility but the safety margin.

A small footnote: `RecommendedRestock`'s comment already said *"demand
fluctuates"* — that sentence **was not true** until today.

---

## 16. The intervention now works in the room too

This was the decision §3 left open: the measurement said the mechanic is a safety
net (0.8 interventions a day in a properly staffed restaurant), while the store
text sells it as a main mechanic. Of the three paths, **"give the intervention a
job outside a crisis"** was chosen, because on its own it does not touch the
difficulty curve and it preserves the promise.

What was missing was the **hall side**. Attention extended patience and sped up
*the kitchen* (`HurryPartyJob`) but never touched the hall — whereas the
bottleneck is usually there. "The owner attends to it himself" means exactly that
he takes the order or the payment.

Now the attended table's **next hall job is halved** (`attendWorkCutBp = 5000`)
and the mark is consumed once it is used: attention is a **step**, not a permanent
state — otherwise a table attended once would be privileged for the whole day.

**Measurement (24 seeds, 60 days, fast food):**

| strategy | before | after | parties served |
|---|---:|---:|---:|
| baskili (short crew, no interventions) | 21,665 | 21,612 | 1955 |
| **baskili_mudahale** | 21,685 | **22,195** | 1978 |
| **difference** | **+20** | **+583** | **+23** |

For a player with a short crew, the intervention's sixty-day return went from +20
to +583.

**For someone playing with a comfortable crew it still does not pay** (`makul`
19,085, `mudahaleci` 18,979) and that is right: you paid money for a crew and
bought away the need. A real trade appeared — *work one person short, and run the
service yourself* — and `baskili_mudahale` is now the best strategy after
`planci`.

**The store text must be updated** (docs/44): the sentence "you get four
interventions a day" is now wrong in two places — the number grows with the tables
and the tea goes to the room, not to a single table.

---

## 18. The combo axis: the saturation is a symptom, the cause is elsewhere

In §10 I had not touched the axis because the target's rationale was written down.
This time I measured, and **the cause came out**.

### First a measurement error was fixed

`_mainOrders` was counting unconditionally, whereas the combo opens on **day 16**:
the denominator included the fifteen days on which the numerator was structurally
zero. The axis was showing real usage a third short.

The denominator now counts only while the mechanic is open. The measure tells the
truth by its name: *what percentage of the orders that could have become combos
did.* The always-open bot's share: **17.0% → 20.8%**.

### Then I tried to measure the target and found the mechanic

To set the target, the way of playing the design calls **legitimate** had to be
measured ("closing it at the peak is a legitimate way to play and the axis must
not punish it"). There was no bot that played that way, so the target could only
have been invented — and the code's own warning forbids that.

The `zirvede_kapat` arm was written: when the room is half full the combo closes,
when it drops the combo opens.

| strategy | combo share | end cash | parties served |
|---|---:|---:|---:|
| imzaci (always open) | 20.8% | 21,157 | 2016 |
| zirvede_kapat | 20.0% | 20,836 | **2016** |

**The two are the same game.** The share drops 0.8 points, the parties served are
**exactly the same**, the cash falls slightly.

### The cause: the combo's kitchen load does not bite

A combo produces three jobs per order (the expectation for a single main dish is
1.7) and lengthens each job by 35% with `kitchenLoadBp = 13500`. On paper the time
the cook is tied up is ~2.4×. But **the number of parties served is 2,016 in both
arms** — that is, there is slack in the kitchen and the extra load is absorbed. The
bottleneck is in the hall (which is also where §16's intervention fix made its
gain).

That is why the target was **left alone**: the measured range is 20.0–20.8 and
whatever target is set, both legitimate ways of playing score the same. **The axis
cannot measure skill because there is no skill difference to measure.**

Fixing it requires changing not the target but the balance — making the combo's
kitchen load genuinely hurt (e.g. raising `kitchenLoadBp` or piling the combo's
jobs onto a single station). That is a **difficulty decision** and belongs to the
user.

### I fell into the same trap a second time this round

My first `zirvede_kapat` threshold was **75% occupancy** and it never fired —
occupancy does not reach that level in practice (8–10 of 14 tables full = 57–71%).
The arm gave **exactly the same** result as `imzaci`, and the only thing that told
me so was two identical lines.

*A measurement arm that does not run and "it ran, it made no difference" look the
same from the outside.* When the threshold was lowered to 50% the difference
appeared (20.0 / 20.8) — and that difference gave the real answer.

---

## 19. Not closed

The five agents gave ~40 findings; this document closes the load-bearing ones.
What is left open, in order of value:

1. ~~The intervention's promise~~ — **CLOSED** (§16): it takes on the hall work
   too, and for a player with a short crew its return went from +20 to +583.
2. ~~The tab book is not on screen~~ — **CLOSED.** `LedgerScreen` was written: for
   every account, who, how much, when it is due, and the two numbers that are the
   decision itself (the payment chance *if you wait* / *if you chase it now*). The
   chance is the very number the simulation uses (`TabChanceBp` in one place).
   `CollectCredit` is in the game for the first time. It is measured in the tour.
3. **The combo axis** — the measurement error is fixed (§18: the denominator now
   counts while the mechanic is open, 17.0% → 20.8%). The saturation, though, is a
   symptom: it was measured that "close at the peak" and "keep it always open" are
   **the same game** (2,016 parties in both) because the combo's kitchen load does
   not bite. Fixing it is not the target but the BALANCE — a difficulty decision.
4. ~~The reputation ceiling's overflow vessel~~ — **CLOSED** (§12).
5. ~~The season's effect on demand~~ — **GROUNDLESS**, the finding had misread the
   document (§13).
6. ~~The equipment line in `model.py`~~ — **A DELIBERATE DECISION**, it was tried
   and it sank and that is written down (§13). The two real items inside it were
   closed.

~~Demand is entirely deterministic~~ — **CLOSED** (§15).

~~**Left:** the combo axis and the store text~~ — **both closed.** The store text
was updated in [44](44-store-texts.md) (the intervention sentence had gone stale in
three places). The combo axis was measured in §18: the target **deliberately** did
not change, because the measurement showed the problem is not in the axis but in
the balance.

**The only thing left is a difficulty decision:** making the combo's kitchen load
genuinely hurt. Without it the axis cannot measure skill — there is no skill
difference to measure. It belongs to the user.
