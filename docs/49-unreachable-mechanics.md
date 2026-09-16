# 49 — Mechanics that never reach the player

*15 September 2026.* The richest vein of this round turned out to be hunting for
"the thing that never fires" ([48](48-day-sharpness.md)). I ran the same sweep
**systematically**: each of twenty commands was checked on four axes — the
balance bot, the automatic tour, the unit tests, and **the screen**.

| command | harness | tour | test | screen |
|---|:--:|:--:|:--:|:--:|
| `SetQuality` | yes | — | yes | **NO** |
| `CollectCredit` | — | — | — | yes |
| `SetDishwashers` | — | — | yes | yes |

All three are different faces of the same sentence: *a mechanic can be complete
in the code and non-existent in the game.*

---

## 1. `SetQuality`: a three-tier decision axis that was on no screen at all

Ingredient quality was fully written in the core, the balance bot measured it
(`ucuz_malzeme`), it had tests — and **there was no button for it on any
screen.** Tab collection had been found in exactly the same way
([45](45-design-review.md)).

Three tiers, and the effect grows with how expensive the ingredient is:

| tier | price | satisfaction |
|---|---|---|
| Low | 15-25% cheaper | **−5.0 … −20.0** |
| Standard | — | 0 |
| High | 15-35% dearer | **+3.0 … +15.0** |

And the mechanic is not dead, quite the opposite — it has got harsher: after the
day was sharpened, the `ucuz_malzeme` bot went 17,179 / reputation 44 → **7,447 /
reputation 8.1**. Once the two pressures (waiting and low satisfaction) combine,
cheap ingredients are close to fatal.

**The selector was put on the market screen** because the choice changes that
screen itself: `IngredientPriceToday` already computes from `_quality`, so
pressing a tier makes every price below it move instantly. The result does not
have to be explained somewhere else — it is seen.

The invisible side (satisfaction) is in words, and **not as a made-up average**:
the per-ingredient effect splits into three separate patterns, and a single
average would mislead. Instead the real effect of the dishes on the player's
**own menu** is computed — with the simulation's own `DishQualityCentiOf`
measure, so the number the screen prints is the very number service uses.

The tour verifies not the existence of the button but **that the command reaches
the simulation** (1 → 2 → back). "The button is there but the command does not
go" has come up twice in this project; an existence test could not see that bug.

---

## 2. `CollectCredit`: the button was there, nobody pressed it

The "Şimdi kovala" ("Chase now") button sat in `LedgerScreen` and the tour
verified that it was **on the screen** — but nothing pressed it. An existence
test cannot see the "the button is there but the command does not go" bug.

The tour presses it now and its yardstick is **the number of tabs**: chasing
closes the tab whether or not it is collected, whereas the change in amount
depends on the dice. Tying the check to the dice would have produced a tick that
changed colour from run to run.

---

## 3. Collecting early was measured: it is a trap

No bot was sending `CollectCredit`, so **the second half** of the decision the
design sells as "collection depends on trust" **had never been measured**.

The `erken_tahsilat` bot was written: every evening it chases the whole ledger.

| strategy | end cash | rep | debt day |
|---|---:|---:|---:|
| `imzaci` (patient) | **15,079** | 71.6 | — |
| `erken_tahsilat` | 9,655 | 48.9 | day 56 |

Halving the odds costs more than getting the money seven days early is worth.
There is a real decision in the mechanic and **patience wins**.

---

## 4. The bot itself was silently doing nothing

On its first run `erken_tahsilat` gave a result **byte for byte identical** to
`makul`: 122,724 revenue, no ledger, signature axis 0. The bot looked like it was
running.

The reason: `DuringService` **was not on the interface**. `Program.cs` dispatched
it through a chain of type checks:

```csharp
if (strategy is SignaturePlayer sp) sp.DuringService(sim);
else if (strategy is PickyCreditor pc) pc.DuringService(sim);
else if (strategy is PeakCloser pk) pk.DuringService(sim);
```

A new strategy that was not in the chain **silently** did nothing — and nothing
said so. The measuring tool's own measurement was coming back empty.

Instead of adding one more line to the chain, the class was closed:
`DuringService` is now on the interface, **with an empty default body**. A new
strategy joins simply by writing the method; there is no registration point left
to forget. Every number for the existing strategies stayed identical — the fix
did not change behaviour, it only stopped new behaviour from disappearing.

*Adding a new arm to a measuring tool is not finished until you have proved that
arm runs.*

---

## 5. `SetDishwashers`: the question can be asked now, the answer is mixed

Assigning a dishwasher existed on the screen and in the tests but **no balance
bot used it** — the plate bottleneck's decision had never been measured. The
`bulasikci` bot was written.

**I measured it wrong twice, and the same signature caught both.**

The first threshold was "hall ≥ 2" and it split the crew in the small shop too;
what was being measured was not "does a dishwasher pay" but "does splitting early
cost". I raised the threshold to 4 — and this time the result came out **byte for
byte identical** to `makul`, because `ReasonablePlayer` never reaches four hall
staff. The threshold never triggered, and the only thing that said so was two
identical rows (exactly the same thing had happened with `PeakCloser` last
round).

The bot was moved on top of `PlannerSchedule` — a dedicated dishwasher is already
**the big shop's** question.

| | end cash | net | tabl | waited, no plate | max dirty |
|---|---:|---:|---:|---:|---:|
| `planci` | 23,375 | 15,375 | 12.0 | 263 | 14.6 |
| `bulasikci` | **24,850** | **16,850** | 11.5 | **349** | 13.6 |

**The till is in favour but the measurement is not clean and the real yardstick
points the other way.** The arm that assigns a dishwasher finishes with a smaller
shop (11.5 tables) and invests less — the difference in the till could come from
there too. On top of that, waiting with no plate **goes up** (263 → 349), so the
mechanic is not achieving its own purpose.

### The reason is in the code, and it is a design question

```csharp
private bool WashNeeded()
{
    if (_platesDirty <= 0) return false;
    if (_dishwashers > 0) return false;      // with a dishwasher on, the hall does not get involved
```

A dedicated dishwasher **switches off the rest of the hall washing when needed**
— that is, they are not added to the capacity, they *replace* it. One person
washes less than "everybody runs when needed" behaviour.

The rule's comment records that this is deliberate ("once you take on a
dishwasher everybody does their own job" — the user's own sentence). So it was
**not changed**: this is not a measurement result, it is a design decision. The
question can be asked now and it has numbers; the decision is the user's.

---

## 6. The specialist really does wash faster — but it is not enough

The user's sentence:

> *"bulaşıkçının yıkama hızının diğerlerine göre çok daha fazla olması lazım,
> çünkü o işi yapan kişi o."*
>
> *("the dishwasher's washing speed has to be much higher than the others', because
> they are the person whose job it is.")*

The content already said so and the simulation was not using it: in
`staff-roles.json` the dishwasher role's daily capacity is **48**, the waiter's
**26**. The dedicated dishwasher and the waiter running to the rescue were
washing with the same `WashMs`.

It was wired up, and the ratio was **not invented, it was derived from the role
table** (26/48 = 5417 bp):

```
waiter 2000 ms  ->  bulasikci 1083 ms
```

A test holds that link; if the multiplier turns back into "no difference" it
breaks.

**But the measurement said speed alone is not enough:** waiting with no plate
349 → **351**. The cause is structural, not numerical — with a dedicated
dishwasher **exactly one person** washes, without one **everybody** in the hall
runs at the moment of crisis. For the multiplier to be enough it would have to be
as large as the hall crew, and the crew grows with the shop: **a fixed multiplier
cannot follow it.**

### Tried and reverted

An exception was written — "when clean plates are about to run out let the hall
come to the rescue": **351 → 351**, nothing changed. The reason: washing is
looked at *after* customer work, and at the peak the hall is already full — the
exception has no free person to fire it.

Because the measurement did not answer back, it was **reverted**. Weakening the
user's design rule on an unmeasured pretext would have been the very thing this
document complains about.

### Open decision

The dedicated dishwasher is still a dominated option today. The way to turn it
into a real trade-off is not speed but the **exclusion rule**: if the dishwasher
is *added* to capacity (if the hall still washes at the critical moment) the
button becomes meaningful. The rule is the user's; the numbers are here.

---

## 7. The auditor was committing the very error it catches

This section is the most expensive one, because it is the tool everything else
rests on.

`check.py` was saying **"core tests OK"** — and **zero** of the 239 tests had
run. `dotnet test` returns exit code **0** when the test assembly cannot be
loaded and only prints "Skipping: ... blocked"; the audit was looking only at the
exit code.

Three faults at once:

| fault | fix |
|---|---|
| The exit code was the only yardstick | **Evidence** is now required: no `Passed!`/`Failed!` summary line means red |
| The block was only looked for on the last line | It is looked for in the whole output — xUnit writes the block **at the top** |
| The retry never worked at all for `dotnet test` | `--no-incremental` is not accepted (MSB1001). A separate build + `--no-build`, and the retry was tied **to the evidence, not to the cause** |

The third one also taught this: the block does not always print `0x800711C7` —
sometimes the test host silently finds zero tests and returns 0. A retry tied to
the cause could not have seen that state.

### A permanent way out for SAC

The harness was blocked on all ten attempts. The reason: `dotnet run` launches a
freshly written `.exe`, and SAC blocks unsigned exes far harder than DLLs. The
solution:

```
dotnet build ... -p:UseAppHost=false
dotnet bin/Release/net10.0/Lokanta.Harness.dll --cuisine turk
```

No exe is produced at all, the DLL is loaded from the signed `dotnet` host — it
**passed on the first attempt.**
