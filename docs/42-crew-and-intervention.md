# 42 — The crew decision and the value of intervening

*12 September 2026.* The balance tool was run again after all the changes
(12 seeds, 60 days). Two findings came out; the second was not expected.

---

## 1. An old open item is closed: the passive player goes under

| question | answer |
|---|---|
| What happens to a player who never intervenes | **100%** of the runs fall into debt, on average on **day 56** |
| Permanently above-market prices | 75% go under |
| Combo inflation | 67% go under |
| A reasonable player | 19,155 in the till, 79 reputation |
| The planner | 24,255 in the till, 95 reputation |

The note that had sat in the `project-simulation` memory for ages — *"the
passive player still does not go broke"* — is no longer true.

---

## 2. Intervening looked neutral — because there was nothing to rescue

The interventionist bot was seating **the same number of people** as the one
that does not intervene (2021) and earning slightly less. The first suspicion
was in the right place: *"a bot whose commands are rejected is not a bot"* —
this project had already watched a bot turn quietly into a copy of another one
when the price ceiling arrived. So the first thing was to **count**:

```
=== interventions (per strategy) ===
  mudahaleci             2880 applied /   2880 tried
  baskili_mudahale       2880 applied /   2880 tried
```

*(For a while the counters were **static and never reset**: the sum of the two
arms was printed on a single line, and the very reason the counters exist — "did
an intervention actually happen" per arm — could not be read. Now they are per
strategy.)*

So the mechanic works. The problem was the ceiling: **in a well-run restaurant
about 0.3 parties per day walk out**, so there is nothing for an intervention to
rescue.

To separate that out, a **pressure pair** was written: two copies of the same
player running one waiter short, the only difference between them being the
intervention.

| bot | end cash | served | left the table angry |
|---|---:|---:|---:|
| `makul` (full crew) | 19,155 | 2,021 | 2 |
| `baskili` (1 short) | 22,153 | 1,960 | 13 |
| **`baskili_mudahale`** | **22,513** | 1,967 | 11 |

**Intervening pays off under pressure** (+360 coins) and lowers the number of
parties that leave the table angry (13 -> 11). So the mechanic is not weak; it
cannot be measured in a comfortable restaurant. And that was exactly the game's
promise: *"you are the owner, and when the place cannot keep up, you step in".*

**The numbers were measured again and came out smaller** (they used to be +861).
The reason is a known fix: the crew decision taken in the evening now looks at
**tomorrow's** day type through `RequiredCrewTomorrow()`. It used to look at the
type of the day that was ending, so a weekday crew was set on Friday evening and
Saturday's peak was entered short-handed — part of what the intervention was
rescuing was in fact the pressure that bug created. A number that shrinks because
the measurement *improved* the thing it measures: good news.

**The "lost" column now counts THOSE WHO LEFT THE TABLE ANGRY**, not those
turned away at the door. The two were being folded into a single number; one is
a service problem, the other a capacity problem.

---

## 3. The unexpected finding: the crew advice had the wrong name

The striking row in the table above is this one: **running one waiter short
earns roughly 3,000 more coins** (it was 5,300 in the first measurement; after
the `RequiredCrewTomorrow` fix the gap shrank, but the direction did not change).
Looking for the reason turned up `RequiredCrewToday()` — it says "today" in its
name but it was calculating every day with the **weekend** multiplier:

```csharp
int peak = DemandModel.CustomersPerDay(..., _economy.WeekendMultiplierBp);
```

Wages are paid **every day**, the peak is two days a week. The interface said
"you need 3 people today", the player hired them, and could not learn anywhere
why they were losing money.

**Two fixes:**

1. `RequiredCrewToday()` now really measures today (with the weekday / weekend
   distinction). The peak is a separate method: `RequiredCrewPeak()`.
2. The screen now says **"To keep up with everyone: 1 + 3 · weekend: 1 + 4"**.
   The number is a capacity calculation, not the best number for profit — and
   that is a **decision**: saying *"required"* was hiding the decision.

**Persistence** was added to the bot's firing rule as well: if it is short it
hires immediately, if it is over it waits for three days in a row of being over.
Firing resets experience; a bot that hires on Friday and fires on Monday was
paying the mechanic's price and never actually making the decision.

**The gap did not close, it shrank** (crew 4.6 -> 4.2 people, wages 26,284 ->
25,572).

---

## 4. "It loses the score" — I wrote that and I had not measured it

The first version of the section above ended like this: *"the short crew wins
the money and loses the score."* It sounded right and it was **unmeasured**.
This project's rule is clear: an unmeasured sentence is a guess sitting in a
document.

The year-end score (docs/08, seven axes) was added to the balance tool. The
measurement:

| strategy | score | wealth | rep | regulars | crew | place | resil | sig |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| `pasif` | 9 | 0 | 0 | 0 | 27 | 28 | 0 | 9 |
| `makul` | **65** | 25 | 79 | 86 | **73** | 55 | 100 | 38 |
| `baskili` | **64** | **29** | 78 | 86 | **64** | 55 | 100 | 38 |
| `baskili_mudahale` | 64 | 30 | 79 | 85 | 64 | 55 | 100 | 38 |
| `planci` | **77** | 32 | 95 | 86 | 72 | 86 | 100 | 73 |

**Half of the sentence was true:** the short crew wins on the *wealth* axis
(29 / 25) and loses on the *crew* axis (64 / 73). But **the reputation is the
same** (78 / 79) — I had written that it would fall — and **the totals are almost
equal: 64 to 65.**

The correct sentence is this: the short crew is a **trade**, not a mistake. The
player wins the money and the wealth score, and loses the crew score and 65
people; the scales balance to within one point. The tension is real, but it does
not say "you are playing it wrong" — it asks *"what do you care about"*. In a
management game that is exactly what you want.

The score table is now printed on every balance run: the next sentence of the
form "this strategy pays off" will not be able to look at the till and forget the
score.

---

## 5. The second question the table opened: the "signature" axis does not measure the signature

These rows sit side by side in the same table:

| strategy | sig axis |
|---|---:|
| `makul` (never opens the combo) | 38 |
| **`imzaci` (opens the combo every morning)** | **38** |
| `planci` (expands a lot) | 73 |
| `atilgan` (expands a lot, goes under) | 69 |

[docs/08](08-endgame.md) says of the cuisine-specific axis that *"this axis
rewards the signature mechanic **directly**"*. Fast food's signature mechanic is
the **combo**; its axis is `peakCovers` — **the highest daily cover count**. The
measurement shows the axis is tracking **expansion**, not the combo: the bot that
opens the combo and the bot that never opens it get the same score, and the
highest scores go to whoever opens the most tables.

The Turkish cuisine does not have the same problem: its axis is
`creditCollected`, the tab collection rate — the mechanic itself.

This is waiting on a **design decision** and both options are defensible:

- **Leave the axis, fix the sentence.** "Highest cover count" is fast food's
  identity itself (speed and volume); only docs/08's "rewards it directly"
  sentence is too bold a claim.
- **Change the axis.** What percentage of main-course orders became a combo —
  then the axis measures a *decision* (the combo also tires the kitchen out, so
  closing it at the peak may be the right move).

### The decision (13 September 2026): the axis changed

**The second option was chosen.** The reason is in the measurement: `peakCovers`
was tracking the same thing as the "Venue" axis — the `planci` bot scored 86 on
Venue and 73 on the signature axis, so two of the seven axes were rewarding a
single behaviour (expansion) twice. The Turkish cuisine's axis measures a
*decision*. The asymmetry is not in the design itself, it is in fast food's axis.

The target came **from the measurement**, it was not made up. First the raw rate
was printed (12 seeds, 60 days):

| bot | combo share |
|---|---:|
| `imzaci` (opens it every morning) | **17.4%** |
| `kombo_sismesi` (opens it but goes under) | 5.3% |
| the other seventeen bots | 0.0% |

The target is **15%**: "open it most days" gives full marks. Because the combo
also raises the kitchen load, closing it at the peak is a legitimate way to play
and the axis must not punish it.

The result:

| bot | sig (before -> after) | total (before -> after) |
|---|---|---|
| `imzaci` | 38 -> **100** | 65 -> **74** |
| `makul` | 38 -> 0 | 65 -> 60 |
| `planci` | 73 -> 0 | 77 -> 67 |

Using the combo is now worth **14 points** and the axis does not track expansion
at all. `imzaci` became the highest-scoring strategy in both cuisines (74) —
"play reasonably and use your cuisine's signature" was supposed to be the best
game, and now it is.

The `peakCovers` branch was **deleted**: no cuisine uses it and this project's
rule is clear — a branch with no call sites left is not wired up, it is deleted.

### And when the axis changed, an invisible cost surfaced

Running the Turkish cuisine with `--strategy imzaci` made the income statement
**not add up: 134 coins**. The cause: the cost of the tea offered when a tab is
opened was coming out of the till and **was not being written to any expense
line.** A player who opens tabs could never reconcile their books.

Why it stayed invisible is instructive too: the Turkish cuisine and the `imzaci`
bot **had never been run together**, because the `--strategy` flag was accepted
and never read ([docs/43](43-review-and-measurement.md) §4). Fixing a flag
uncovered a leak.

The tea now has its own column and the reconciliation is zero. It being visible
is right too: **a tab is not free**, and having its price nowhere made the
mechanic look cheaper than it is.
