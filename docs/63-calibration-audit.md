# 63 — What the calibration was actually measuring

18-19 September 2026.

The instruction was three words: *"kalibrasyonu kovala"*

*("chase the calibration")*

The sweep had reported an improvement earlier that day and the job was to run
the full candidate list and take the result.

The result is that there is no change to make. Getting to that answer took
finding two bugs, one of which was introduced by the fix for the other.

---

## 1. The sweep was measuring a cook who could not exist

`model.py` says a cook serves **28** guests a day. That number is not a
preference: `timing.py` derives the cook's time per guest from the content's
own `prepMs` and `attendBp` and gets **17,127 ms**, and the service day is
480,000 ms. At 30 the model claims a cook serves more guests than the menu
allows, and `timing.py` reports the contradiction as a 7.04% deviation.

`solve.py` carried its own copy of the capacities reading `cook=30`, and
`calibrate.py` writes `model.py`'s `CAP_*` **from that dict**. So every sweep
copied the stale 30 back over the corrected 28.

It is the bug described in a comment twelve lines above the line that caused
it — *"Two files carried the same constant with different values"* — repeated
one line lower, for a different constant.

**What it cost:** the sweep of the morning of the 18th ranked nine candidates
and picked 6500 with a penalty of 11. Every row of that table was measured on
the wrong cook. It also explains an attribution that had not added up: a
single-candidate run had reported the fast food penalty falling 10 to 6, and
the improvement could not be traced to anything anybody had chosen. It was
this.

## 2. The obvious repair was worse

Reading the capacity from `model.py` instead of restating it looks like the
textbook fix. It closes a loop.

`calibrate.py` **writes** `model.py`, and it writes the capacities scaled by
the staffing factor `s` that the solver searches:

```python
caps = dict((k, int(round(v * s))) for k, v in solve.BASE_CAP.items())
```

So with `BASE_CAP` reading from `model.py`: 28 to 27 to 26, one notch per
sweep at a scale of 0.95. It was seen in the wild — a sweep interrupted by a
dropped connection left the tree standing on `CAP_COOK = 27`.

**The old copy was stale but stable. The repair was fresh and unstable, which
is the harder of the two to notice** — a stale constant is wrong the same way
every time and eventually somebody compares two files, while a drifting one is
correct on the run you happen to check.

## 3. What it is now

The cook capacity is read from **the derivation itself** — the dish mix, the
attend fractions, the busy time — which nothing in the sweep writes. And
`calibrate.py` no longer writes `CAP_COOK` at all.

That last part is a design statement, not tidiness. The other three capacities
are knobs: how much of a hall role's day one guest costs is a balance choice.
The cook's is a claim about the menu, so scaling it by a staffing factor does
not tune the game — it asserts something about the dishes that the dishes do
not say.

## 4. And the guard existed the whole time

`timing.py --check` runs **52 assertions** with a correct exit code, and `C2`
is precisely *"the derived cook time agrees with the declared capacity"*.

`check.py` had never called it.

This is the pattern this project keeps finding in the game — complete in the
code, reachable from nowhere ([49](49-unreachable-mechanics.md)) — in the
toolchain that checks the game. Fifty-two green assertions nobody runs are
worth nothing, and the one that mattered would have gone red the first time a
sweep touched the capacity.

Proved by setting the capacity to 30 and watching `C2` and `F3` go red, then
setting it back. It now runs **first** in `check.py`, because everything after
it is derived from those numbers: with the day, the capacities and the prep
times disagreeing, a red dish table is a symptom and this is the cause.

## 5. The corrected sweep

Nine candidates, both cuisines, 8 seeds each. `s` is the staffing scale, `o`
the owner's work factor, `e` the equipment-price factor.

| realisation | penalty | measured | what it fails |
|---:|---:|---:|---|
| 5500 | **16** | 12,495 | cheap ingredients beat good play; money stops mattering in week 7 (both cuisines) |
| 6000 | 28 | 12,290 | growth 1.54; cheap ingredients beat good play; signature 0.87 |
| 6500 | 17 | 12,412 | cheap ingredients beat good play; money stops mattering; signature 0.89 |
| **7000** | **16** | 11,891 | **growth 1.65; the planner cannot keep to the calendar (Turkish)** |
| 7500 | 28 | 9,498 | growth 1.28; signature 0.89; the planner, both cuisines |
| 8000 | 62 | 9,228 | growth 0.42 in Turkish; debt; reputation 62.7; the shop empties |
| 8500 | 44 | 8,148 | growth 1.24 / 1.06; signature 0.52; debt |
| 9000 | 64 | 6,494 | growth 0.98 / 0.78; reputation 75.0 / 71.9; debt |
| 9335 | 32 | 6,760 | growth 0.99; reputation 78.1; the planner, both cuisines |

**The winner is 7000, and 7000 is what the repository already had.** Applying
it produced an empty `git status`. The corrected calibration re-derived the
setting already in the tree, independently of it.

Two things this table settles that the previous one could not:

**The bracket is no longer at its edge.** 5500 and 6000 were added because the
previous winner sat on the lowest value in the list, and an optimum at the
boundary is the search saying the bracket is wrong rather than an answer. They
do not keep improving — 6000 scores 28 — so the range now contains the answer
instead of ending at it.

**The penalty surface is jagged, and that is new.** The previous, invalid
sweep was monotone: 11, 16, 28, 38, 54, 64, 72. This one runs 16, 28, 17, 16,
28, 62, 44, 64, 32. A surface that reverses direction three times has noise in
it comparable to the differences being ranked, and the top three candidates
are separated by **one penalty point**.

That matters because `calibrate.py` justifies its eight seeds in as many
words: *"the penalty difference between candidates is tens of points and the
noise does not disturb it"*. **That justification no longer holds.** It was
true of the surface the tool used to produce; it is not true of this one.

### The tie was re-measured, and eight seeds had been hiding the answer

5500 and 7000 both score 16. The tool breaks that tie on the higher rent, and
picked 7000. So 5500 was re-run on its own at 32 seeds to find out whether the
tie was real.

It is real on the scale — both still score 16 — and the scale is the thing
that turns out to be wrong.

| | 8 seeds | 32 seeds |
|---|---:|---:|
| 5500, cheap ingredients | 29,084 | 34,802 |
| 5500, good play | 28,101 | 25,291 |
| **the cynical strategy's advantage** | **+3.5%** | **+37.6%** |

At eight seeds that failure looks like a rounding error between two ways of
playing. At thirty-two it is a **dominant strategy**: buying the cheapest
ingredients beats playing well by more than a third of the campaign's money,
in a game about running a restaurant. The sweep very nearly recorded it as a
close call.

**And two failures worth 16 points each are not the same failure.** 7000's is
that growth comes out at 1.60 against a target of 1.8 — a magnitude short by
11%. 5500's is that the cynical strategy wins by 38% and the economy stops
mattering in week 7 of nine, in both cuisines. The penalty function scores
"this number is a bit low" and "this strategy defeats the design" identically,
and then breaks the tie on rent. It reached the right answer here by an
argument that does not generalise.

## 6. The growth multiplier is not a calibration problem

This is the finding the chase was for.

Verified at 32 seeds on the winner: **fast food's growth multiplier is 1.60
against a target of 1.8-4.0.** The same 1.60 measured before the intervention
work of [62](62-service-agency.md), and before either sweep.

Across all nine candidates the multiplier is at or below 1.65 wherever it is
reported. The two settings where it passes — 5500 and 6500 — buy that by
making **cheap ingredients beat good play**, which is worse than a tuning gap:
it means the cynical purchase dominates the considered one, in a game whose
premise is that you are running a restaurant. And they buy it at rents of
300 / 750 / 2800 / 3850, when the rent is supposed to be the dominant fixed
cost and the metronome of pressure ([12](12-economy.md) §2). A game nobody can
lose has no growth problem, because it has no problem.

So: **the ratio between expanding and not expanding cannot be fixed by
choosing a realisation rate.** Nine of them were tried. What the calibration
can do it has done, and it has left the tree exactly where it found it. The
growth multiplier is a design question now, not a balance one.

## 7. Still open

- **The fixed point does not converge.** `REALISATION_BP` is meant to be the
  rate at which the model's theoretical revenue actually materialises, and the
  sweep measures what it gets. At a set point of 7000 the measured rate is
  **11,891** — the player realises 119% of plan against a model that assumes
  70%. Reading across the table, set and measured cross at about **8700**, and
  at 8500 the penalty is 44 and Turkish falls into debt. **The
  self-consistent rate and the playable rate are not the same number**, and
  the rent is solved from the closed-form model at whichever one is written
  down. Either the model under-describes where the revenue comes from, or
  `REALISATION_BP` is not the quantity the rent solve should be keyed on.
- **Eight seeds no longer separate the candidates** (§5), and on the one
  candidate that was re-measured they understated a dominant strategy by a
  factor of ten. The sweep needs more seeds near the top of the ranking.
- **The penalty function cannot tell a shortfall from a defeat** (§5). Ten
  points for "growth is 11% under target" and a comparable score for "the
  cynical strategy wins by 38%" are not commensurable, and the tie-break that
  currently resolves them is about rent. A failure that says *a player who
  ignores the design does better* should not be tradeable against a number
  being a little low.
- ~~**"The planner cannot keep to the calendar"** appears in seven of the nine
  rows, in both cuisines, at every rate.~~ **Answered the same day — and the
  answer was a third option this list did not offer.** See §8.

## 8. The complaint that never went away was the bot

§7 offered two explanations for *"planci cannot keep to the calendar"*
appearing at every rate in both cuisines: a real design fault, or a check
measuring the wrong thing. It was **neither**, and the missing third option is
worth naming because this project has now met it three times.

`PlannerSchedule` expands on days 15, 29 and 43:

```csharp
sim.Apply(new Command(sim.TickIndex, CommandKind.Expand, t));
break;
}
_next++;          // ran whether or not the Expand went through
```

**A refused expansion burnt the slot for good.** A planner a few hundred coins
short on the morning of day 43 never asked again, and spent the remaining
seventeen days earning money it had no way to spend: Turkish finished the
campaign at **11.0 tables holding 23,622 coins**, when the step it had missed
costs 8,000. Ending rich and small is the signature, and it was on the table
the whole time.

It is the lesson already written beside `Interventionist.Tried`, in a
different bot: *"a bot that gets refused is not a bot"*. The price ceiling
taught it once, when `yuksek_fiyat` silently became a copy of the reasonable
player as soon as its prices started bouncing.

### What the fix changed

The slot is consumed only when the table count actually moves, and the planner
retries each morning until it does.

| Turkish `planci` | before | after |
|---|---:|---:|
| tables | 11.0 | **13.8** |
| served | 2,439 | 2,721 |
| reputation | 91.7 | 97.3 |

Fast food does not move: 14.0 either way, because it was never refused.

### And the refusals are counted now

"The schedule is unaffordable" and "the bot asked on the wrong day" produced
the same row, so the difference is now measured rather than inferred:
**15 refusals across 16 Turkish seeds** — about one morning late per campaign.

That is a different sentence from the one the calibration had been printing.
The model's calendar is not unaffordable in Turkish; it arrives roughly a day
early for the money. What the fix then exposes is real and was hidden behind
the bot giving up: **Turkish reaches the calendar by borrowing**, showing a
debt day of 49.

### The cost of the bug was not one line in a report

That complaint was worth **6 penalty points on nearly every candidate** of a
nine-candidate sweep, in a penalty function whose top three settings are
separated by one point (§5). A constant offset does not change a ranking, but
this one was not constant - it applied to seven rows of nine, and it pushed
the search toward whatever eases a Turkish cashflow that was never actually
that tight. The direction that eases it is cheap rent, which is exactly where
5500 and 6500 sit, and cheap rent is how *cheap ingredients beat good play*
arrives. **A penalty for a fault that does not exist steers just as firmly as
one for a fault that does.**

The sweep was therefore re-run once more.

## 9. The sweep with the planner fixed, and what is left

Same nine candidates, same seeds, the only change being a bot that asks again
when it is turned down.

| realisation | penalty before | **penalty after** | what it fails now |
|---:|---:|---:|---|
| 5500 | 16 | 16 | cheap ingredients beat good play; money stops mattering in week 7 |
| 6000 | 28 | 22 | growth 1.54; cheap ingredients; signature 0.87 |
| 6500 | 17 | 17 | cheap ingredients; money stops mattering; signature 0.88 |
| **7000** | **16** | **10** | **growth 1.72 — and nothing else** |
| 7500 | 28 | 32 | growth 1.28; signature 0.87; reputation 73.8; the planner |
| 8000 | 62 | 58 | growth; the hall-only bot beats the planner; reputation 57.5 |
| 8500 | 44 | 74 | as above, worse; Turkish reputation 39.0 |
| 9000 | 64 | 84 | growth 1.00 / 0.88; reputation 32.3 / 21.0 |
| 9335 | 32 | 92 | growth 1.08 / 0.49; reputation 29.7 / 13.4; the shop empties |

**The surface has a shape again.** It was 16, 28, 17, 16, 28, 62, 44, 64, 32 —
reversing direction three times, with the top three separated by one point.
It is now 16, 22, 17, **10**, 32, 58, 74, 84, 92: a single minimum with a
**six-point** margin, and monotone above it. The ranking no longer depends on
which seeds were drawn.

That is the measure of what the bug was doing. It was not adding noise, it was
adding a *slope* — six points on seven of nine rows, pushing the search toward
whatever eased a Turkish cashflow that turned out to be about one morning
short.

Two other things the fixed planner makes visible, both of which were being
hidden by a bot that gave up before it could hit them:

- **"The hall-only bot beats the planner"** appears from 8000 upward. At those
  rents, expanding on the model's calendar is actively worse than standing
  still. That is a real and useful signal about where the rent stops being
  pressure and starts being a wall; the old planner never got far enough into
  the campaign to trip it.
- **"planci cannot keep to the calendar" now appears only at 7500 and above** —
  where it is true. Below that the schedule is affordable, which is what the
  refusal count had already said.

### What is left is one complaint

The winner is 7000 with a penalty of **10**, and the growth multiplier is
worth exactly 10. Verified at 32 seeds: **1.60 against a target of 1.8-4.0,
and nothing else fails.**

Applying it produced an empty `git status` for the second sweep running. The
calibration is at its optimum, the tree is on it, and the one thing still
failing is the one thing nine realisation rates could not move.

## 10. The one complaint left was the check reading the design backwards

§9 ended with exactly one failure standing: the fast food growth multiplier,
1.60 against a target of 1.8-4.0, unmoved by nine realisation rates and by
the intervention work before them. The check that produced it:

```python
g = rows["genislemeyen"]["cash"]
m = rows["makul"]["cash"]
ratio = m / g
bad(1.8 <= ratio <= 4.0, 10, "growth multiplier ...")
```

It divides **end cash**. The sentence it cites from [12](12-economy.md) is
*"Growing **pays**, but by a factor of two, not eleven"* — a statement about
what growing earns. Those are not the same number, because both bots start
with the same 8,000, and the till is `8,000 + earned`. A constant on both
sides of a ratio pulls it toward 1, and the constant has nothing to do with
growing.

Measured on 32 seeds, taking the stake off:

| | till ratio (the check) | earned ratio (the design) |
|---|---:|---:|
| fast food, `makul` / `genislemeyen` | 20,763 / 12,980 = **1.60** — fails | 12,763 / 4,980 = **2.56** — inside the band |
| Turkish | 18,702 / 8,305 = **2.25** — passes | 10,702 / 305 = **35.1** — "eleven", three times over |

**The check had both cuisines backwards at once.** Fast food meets the design
sentence and was being failed for it; Turkish violates the design's ceiling
threefold and was being passed, because the stake was hiding a non-expander
who earns nothing.

So the chase of the last week — the intervention pool, nine rates, three
sweeps — was aimed at a number that was already where the design asked for it.

### What the check measures now

The stake comes off both sides, and the band is applied to what the campaign
earns. Proved in both directions on the fast food rows: a grower earning 1.2x
fails low, one earning 6.0x fails high.

The Turkish case needed a second thought. On a base of 305 coins a ratio is
not a measurement, it is a division, and "35.1" points at nothing anybody can
fix. So when the non-expander's earnings are under **a quarter of the stake**
the complaint is *that* — *"growth is a no-brainer: standing still earns 305
on a stake of 8,000 in sixty days"* — and it points at the actual remedy,
which [52](52-split-and-rent.md) named a week ago and explicitly said was not
the rent's job: Turkish spoils **41%** of what it buys at four tables
(12,734 of 30,972) and its tabs net negative. The quarter is a judgment, drawn
where it is near neither cuisine (fast food's non-expander earns 62% of the
stake, Turkish's 4%).

### Three things this closes, and one it opens

- The fast food growth multiplier is **not a problem and never was**. §6's
  conclusion — that it is a design question — is withdrawn: it was a
  measurement question, and the memory of this project already says so in
  general terms (*measure the mechanic, not a proxy for it*). The proxy here
  was the till.
- The calibration's penalty at 7000 does not change in size — one complaint
  worth 10 — but it changes **cuisine and meaning**, from a fast food number
  that was fine to a Turkish fact that is not.
- The remedy is now the one [52](52-split-and-rent.md) already identified,
  and it is not a knob in this tool.
- **Opened:** every candidate's growth complaint in §5 and §9 was the till
  ratio. The surface has to be swept once more with the check measuring
  earnings, because some settings that "failed growth" (6000: 1.54) may pass
  on earnings, and the ranking is only as good as its evaluator.

### Where a four-table Turkish shop's money goes

The corrected check says the Turkish non-expander earns 305 coins in sixty
days. Before letting that stand it was worth asking whether the bot was doing
something foolish in one cuisine and not the other - the planner had just been
caught doing exactly that. It is not: `genislemeyen` is `ReasonablePlayer`
with expansion switched off, the same code in both cuisines. The money goes
where the design sends it. Same 32 seeds, both non-expanders, whole campaign:

| | fast food | Turkish | difference |
|---|---:|---:|---:|
| revenue | 60,262 | 56,996 | −3,266 |
| spoiled (of stock bought) | 8,183 (28%) | 12,734 (**41%**) | **+4,551** |
| wages | 8,346 | 13,947 | **+5,601** |
| rent | 7,820 | 6,800 | −1,020 |
| equipment and expansion | 9,956 | 6,581 | −3,375 |
| loan taken | 0 | 2,031 | +2,031 |
| **earned** | **4,980** | **305** | **−4,675** |

Two lines carry it, and both are the cuisine's identity rather than a fault:

- **Spoilage.** *"Fast food forgives, Turkish does not"* ([06](06-plan-status.md)):
  fast food's frozen and jarred ingredients do not spoil, so its factor is
  0.42 against Turkish's 0.57. At four tables, buying for a menu that has to
  be stocked every day, that rule throws away two fifths of what a Turkish
  shop buys.
- **A second pair of hands.** 13,947 over sixty days is 232 a day: a cook at
  140 and a hall worker most days. The `crew` column reads 1.0 for both
  cuisines because it is the crew **on day 60**, not the average - the
  harness accumulates `Cooks + HallStaff` once per run, at the end, beside
  the table count. Turkish has table service; fast food's counter *"carried
  half the hall's workload"* ([51](51-self-service.md)), so at four tables
  the fast food owner is enough and the Turkish owner is not.

The rest follows: with a waiter's wage and the bin taking 41%, the Turkish
shop borrows (2,031), buys a third less equipment, and finishes the year where
it started.

**What is a design decision and not a bug:** whether *zero* is the intended
magnitude of "does not forgive". The design says a non-expander should earn
little ([12](12-economy.md): a factor of two, not eleven, between growing and
not). It does not say nothing. A four-table Turkish lokanta that cannot pay
its owner in sixty days is either the correct price of the harder cuisine or
the free cuisine's unlock selling a game that does not work at its starting
size - and that is a question for the designer, which the calibration now
asks in those words instead of hiding it behind a passing ratio.

One small thing for the harness: `crew` sits next to columns that read as
campaign figures and is a day-60 snapshot. It cost three readings of that
table to notice, and it should say so in its header.

## 11. The sweep with the check measuring earnings

Same nine candidates, same seeds, the only change being §10's check.

| realisation | §9 penalty | **§11 penalty** | what it fails now |
|---:|---:|---:|---|
| 5500 | 16 | 36 | growth **4.18 / 7.46** — over the ceiling in both cuisines; cheap ingredients beat good play |
| 6000 | 22 | 22 | growth 7.20 in Turkish; cheap ingredients; signature 0.87 |
| 6500 | 17 | 37 | growth 5.21 in fast food; Turkish non-expander earns **−588**; cheap ingredients |
| **7000** | **10** | **10** | **Turkish non-expander earns 305 — and nothing else** |
| 7500 | 32 | 42 | growth 1.76 in fast food; Turkish non-expander earns 834; reputation 73.8 |
| 8000 | 58 | 48 | the hall-only bot beats the planner; reputation 57.5; Turkish 1,118 |
| 8500 | 74 | 74 | growth 1.79; the planner beaten; Turkish 1,536 against a grower at 2,201 |
| 9000 | 84 | 84 | growth 1.00; reputation 32.3 / 21.0; the Turkish grower earns **−364** |
| 9335 | 92 | 92 | growth 1.31; reputation 29.7 / 13.4; both Turkish players lose money |

**The winner is 7000 for the third sweep running, and for the third time
applying it left `git status` empty.** Its margin over the runner-up is now
twelve points. Verified at 32 seeds, one complaint: *Turkish: standing still
earns 305 on a stake of 8,000 in sixty days.* **Fast food has no complaint
at all.**

Two things the table says that no earlier table could:

**Cheap rent now fails for the right reason.** 5500 and 6500 used to pass the
growth check on the till ratio. On earnings they fail it from above - 4.18,
5.21, 7.46 - which is the design's *"not eleven"* finally being measured: at
that rent, not growing is not a choice anybody would make, and the cheap
ingredients that beat good play arrive by the same door.

**There is no realisation rate at which Turkish passes.** Below 6500 its
growth multiplier is over the ceiling; from 6500 up its non-expander earns
under a quarter of the stake, and from 9000 up its grower loses money too.
The rent moves the four-table Turkish shop between "growth is the only move"
and "nothing is a move" without passing through "growth pays, by a factor of
two". That is the measurement behind the sentence [52](52-split-and-rent.md)
wrote a week ago from intuition: Turkish's weaknesses are spoilage and tabs,
and *they are not things to be closed with rent*.

So the calibration is finished in the only sense that matters: it has one
knob, the knob is at its optimum, and the one failure left is provably not
that knob's to fix. What remains is a design decision about the free
cuisine's harder sibling at its starting size (§10), and it is now stated in
the calibration's own output in those words.

## 12. "Sıfır kazanır olmasın" — four levers, measured one at a time

> *"Sıfır kazanır olmasın"*
>
> *("Let it not earn zero.")*

The decision was about the outcome, not the means: a four-table Turkish shop
that never expands must earn something. The acceptance is §10's own check -
the non-expander earns at least a quarter of the stake, and the grower earns
1.8-4.0 times what it does. Four levers were measured, each alone, each
against the same 8-seed baseline (Turkish non-expander 338, grower 10,325;
fast food 4,750 / 13,955).

**1. A cold-store-aware stocking floor — measured and dropped.** Stock a
typical party (3) instead of a full one (4) for any ingredient that dies
tonight. Spoilage fell 2,367 and revenue fell 1,827: at four tables the floor
was not waste, it was **stock that sometimes sold**, and cutting it turned the
bin into stock-outs almost one for one. Turkish non-expander 338 → 669. And
it took 10% off the fast food opening day (102,050 → 91,570) and halved its
tail, because the opening stock comes from the same recommendation.
Reverted, test and all.

**2. A shelf life without a cold store — measured and dropped, twice.** Tier
0 keeps nothing: a thirty-day cheese dies overnight beside the mince. A small
tier-0 keep rate (10%, 15%) is physically honest, and it is guarded in three
places as the designed baseline of docs/12 §3. Bypassed for the experiment,
the first run came back **byte-identical** in all four cells - and
byte-identical means the arm never ran: `StorageKeepBp()` returns 0 at tier
0 without reading the content, so the number in the file is dead. Bypassed
properly, the result was a Turkish non-expander at **−492**: its spoilage
did not move (12,624), because the waste is not in the long-lived
ingredients at all. It is in the fresh ones, spoilDays 1-7 - nineteen of
Turkish's thirty perishables - and those die at every keep rate. Reverted.

**3. The bot was not playing a lokanta.** `NarrowMenu` says why a menu is
narrowed - *"every dish on the menu has to be restocked daily and the
leftovers go in the bin; that is why we want four customers per main
course"* - and then applied that rule to the mains only, switching every
side, drink and dessert ON every morning. In Turkish those are the fresh
ones: the salads, the milk puddings. The same arithmetic now runs per role,
favourites of regulars first, at least one dish per role.

| 8 seeds | before | after |
|---|---:|---:|
| Turkish non-expander | 338 | **1,954** |
| Turkish non-expander, spoiled | 12,596 | 8,452 |
| Turkish grower | 10,325 | 11,517 |
| fast food non-expander | 4,750 | 5,435 |

Two levers in the *game* were tried against a number the *bot* was producing.
It is the planner's bug in another bot (§8), and it is the third time this
week: a bot that does not do the obvious thing reads as a broken economy.

**4. The safety margin goes on the forecast, not on the floor.** The
recommendation added 20% to the larger of the forecast and the floor. The
floor is not a forecast; it is already a buffer, and a buffer times a margin
bought 4.8 portions of every perishable for a dish that sells one a day.
The margin now applies to the forecast alone and the floor stands as written.

| 8 seeds | after 3 | after 3 + 4 |
|---|---:|---:|
| Turkish non-expander | 1,954 | **3,628** |
| Turkish non-expander, spoiled | 8,452 | 6,541 |
| Turkish grower | 11,517 | 13,185 → ratio **3.63** |
| fast food non-expander / grower | 5,435 / 13,160 | 4,962 / 13,546 → ratio **2.73** |
| fast food opening day | 102,050, tail 7.3% | **102,050, tail 7.3%** — unchanged |

Guarded by `StockingMarginTests`: one low-selling dish alone on the menu,
and the recommended stock of each of its ingredients is exactly the floor.
Proved red under the old rule.

One number to keep an eye on: the Turkish non-expander's reputation fell from
47.3 to 42.2 with the thinner stock. Fewer portions in the fridge is a few
more "we are out of that" on a busy night. The grower's reputation did not
move.

### Acceptance

The calibration's own verification at 7000, **32 seeds, both cuisines**:

```
penalty 0  BOTH CUISINES CLEAN
```

The first clean verification in the calibration's history. Levers 3 and 4
did it; 1 and 2 were measured and reverted. The rate did not move, the rent
did not move, the cuisine's identity did not move: the bot plays a shorter
menu, and the recommendation no longer pads a buffer.

At 8 seeds the same run shows one Turkish complaint - the signature mechanic
at 0.86 against a floor of 0.90 - that is absent at 32. That is §5's
eight-seed noise showing up where a verdict is now close to a threshold; the
32-seed verification is the one that counts, and it is why the sweep
re-measures its winner.
