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
- **"The planner cannot keep to the calendar"** appears in seven of the nine
  rows, in both cuisines, at every rate. A complaint that never goes away is
  either a real design fault or a check measuring the wrong thing, and nothing
  has told the two apart.
