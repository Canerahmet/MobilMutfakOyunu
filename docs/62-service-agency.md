# 62 — The service phase: what the owner does while the day runs

18 September 2026.

> *"kullaniciyi oyuna daha fazla dahil etmek ve mudahele etmesine olanak
> vermek icin ne yapilabilir onu da dusun? cunku su an biraz servis sirasinda
> oyuncu mudahelesi cok az gibi."*
>
> *("Think about what could be done to involve the player more and let them
> intervene, because at the moment player intervention during service seems a
> bit thin.")*

Four independent readers were put on the question - one on the genre, one on
what the simulation already exposes, one on the minute-by-minute rhythm, one
whose job was to find how any of it would make the game worse. Their reports
disagreed with each other, and the disagreement is what produced the answer.

---

## 1. The complaint was right; the obvious diagnosis was wrong

The reflex reading is "too few levers, add levers". Two of the four readers
said the opposite, with a measurement behind it:

> *In a well-run restaurant about 0.3 parties per day walk out, so there is
> nothing for an intervention to rescue.* — [42](42-crew-and-intervention.md)

and a patient bot, asked on every tick, found **0.8 uses a day** for its four
to six charges ([45](45-design-review.md) §3). On that evidence the budget is
not the binding constraint and more charges answer a scarcity that does not
exist.

**But that measurement is dated 13 September and the day was sharpened on the
14th** ([48](48-day-sharpness.md)), which took the value of an intervention
from +505 to +4,953. The number was right when it was taken and was never
taken again - the third time in one week that this project made a decision on
a stale figure (see [31](31-rooms-and-camera.md) for the other two).

Re-measured on the shipped content, 24 seeds x 60 days, fast food one waiter
short:

> **An eager player's whole day budget is gone at 0:45 on average.**

The lunch crest is at 2:00 and the evening crest at 6:14. So the budget IS
binding - it binds forty-five seconds into an eight-minute day, and the rest
of it really is spectating. Both readings were half right: there is little to
rescue, *and* the purse is empty long before the two moments when there is.

## 2. Spreading beats enlarging, and it is not close

Same method, same seeds:

| pool | rule | served | **lost at the table** | charges/day |
|---|---|---:|---:|---:|
| none | never intervene | 1150 | 71 | 0 |
| day budget 4 | eager | 1173 | 69 | 4.00 |
| day budget 4 | only on a crisis | 1163 | 60 | 3.47 |
| day budget 6 | only on a crisis | 1165 | 59 | 5.18 |
| **regen 2 / cap 3 / +1 per 120 s** | eager | **1184** | 66 | 5.00 |
| **regen 2 / cap 3 / +1 per 120 s** | only on a crisis | 1174 | **55** | **3.53** |

A **six**-charge day budget spends 5.18 charges and loses 59 parties. A
**three**-charge regenerating pool spends 3.08 and loses 53. **Fewer charges,
better day** - and the decision survives: under regeneration, eager play still
loses 66 where patient play loses 55, so "now, or at the crest" is still a
question worth getting right.

One more thing the table says that a day budget cannot express: in Turkish,
spending EARLY is correct, because that crest starts at **0:58**. In fast food
it is wrong. One number for the whole day cannot be right in both; a pool that
refills lets the arrival curve decide.

So: `interventionStart 2`, `interventionRegenMs 120000`, `interventionCap 3`,
the cap rising with the table count rather than the budget - because the note
that made the allowance scale in the first place said that growing "was not
increasing the player's agency but dissolving it", and that is still true of
the ceiling.

## 3. What shipped

### The attention pool refills

`InterventionsToday` is gone as the primary knob. `RegenerateInterventions`
runs in `TickService`, counts integer milliseconds off the tick - never a wall
clock, because the campaign has to replay from a seed - and stops at the cap.

**The cap is the cost.** Hoarding through a quiet stretch throws away
everything that would have regenerated; entering a 2.1x crest empty is the
other way to lose.

### Last orders

`CloseDay` ends the day by sending every seated party away angry, and the tail
after the arrival window carries **27% of a fast food day's revenue** - so
closing early was never right, on any day, in either cuisine. The game shipped
a button that is always wrong to press, with a confirmation toast standing in
for a design decision.

`LastOrders` (command 22) is the decision it was pretending to be: the door
shuts, nobody inside is touched, the day ends when the room empties. Plates
gone and one hand short with ninety seconds of arrivals to come, giving up the
revenue to keep the reputation is a real move.

### The sink, during service

`SetDishwashers` has never had a phase guard. `DispatchHall` re-reads it every
tick. It was reachable only from the morning crew screen - which is
[49](49-unreachable-mechanics.md)'s pattern exactly: complete in the code,
absent from the game.

As an all-day commitment the dedicated washer is close to dominated, because
`_dishwashers > 0` REPLACES the hall's crisis washing rather than adding to
it. **As a sixty-second loan it is the trade that was missing**: the
specialist's faster wash, for exactly as long as you are paying for it in
floor capacity. Zero core code; the plate count went on the card beside it,
because the reason to press it was only ever visible in the evening report.

### "Hurry the kitchen" gets a target

Tea and the owner's attention have honoured a selected table since `Target()`
was written - *"the game answered the WHO, and the whole mechanic of being the
owner had come down to a timing button"*. The third verb still answered it:
`BusiestStation()`, chosen for the player, every time.

The fix could not be a label. The note beside the button records why: the
station's name made that one button 319 dp and the strip 1,103 dp on an 873 dp
screen. So it uses the selection that is already there - tap the table that is
waiting, and the work hurried is the work THAT table is waiting on, which is
not always the busiest station. Select nothing and the old behaviour stands.

### The table asking for a tab is marked

`Simulation.AsksForCredit` has been public since the mechanic was written and
its own summary says *"the UI will mark it"*. It had **zero call sites** - not
the interface, not the tour, not the tests. The player pressed the button and
learned who had asked from the toast afterwards. A copper pip on that table's
badge now, in the world, which is where the target readout was deliberately
moved when it came off the strip.

### And a real bug, found on the way

The loan button's condition was evaluated **once per session**. `TopBar` runs
from `Build`, `GameScreen` is built once, `Pop` does not rebuild what is
underneath, and `Tick` refreshes only the cards and the strip. A player who
loaded a healthy save and went broke on day thirty **had no loan button for
the rest of the session** - the one screen that exists to rescue them, gone
because the strip had made up its mind an hour earlier. Same shape as the four
in docs/49.

## 4. What was refused, and why

- **Live staff reassignment.** `CommandKind` **9 was `AssignStation`**: it
  existed, was sent from nowhere, and was deleted as dead content. Staff have
  no per-person location in the core - `_cooks` and `_hall` are scalars.
- **Seating control.** `SeatWaitingParties` puts the most urgent waiting party
  at any free clean table, and **a table has no seat count anywhere in the
  core**. Every table is interchangeable, so "which table" is a null choice;
  making it real re-opens capacity, the rent solve and twenty bots.
- **A per-table prompt when a party is about to leave.** The crisis window is
  about three seconds, the speed control goes to **x16** (that window is then
  under 200 ms), and [02](02-design-proposal.md) promises in as many words
  that this is not a reflex game.
- **A second currency for the post-peak trough.** There is nothing for it to
  buy: in fast food slot 3 the hall is empty 13.7% of the time. The trough
  wants work that pays later, not a second purse.
- **Anything in the bottom strip.** It has been measured asking for 1,103 dp
  of an 873 dp screen, and "Attention" has already broken mid-word into
  "Attentio / n". Everything above went on a card.

## 5. How it is held

| claim | where it is asserted |
|---|---|
| the pool refills | `InterventionTests.The_attention_pool_refills_during_service` - proved by disabling the regeneration and watching it go red |
| it stops at the cap | `InterventionTests.The_pool_stops_at_the_cap` |
| last orders stops arrivals AND ejects nobody | `InterventionTests.Last_orders_stops_arrivals_and_ejects_nobody` - the two halves fail separately, so they are asserted separately |
| the resource is still limited | `SimulationTests.The_owners_intervention_is_a_limited_resource`, rewritten to follow the pool rather than deleted |
| the refill happens in the REAL BUILD | the tour watches the banked milliseconds rise |
| the sink button SENDS ITS COMMAND | the tour presses it and asserts `Dishwashers` changed, then changed back - docs/49 §2 is explicit that an existence check cannot see "the button is there but the command does not go" |

**The tour caught the first version of its own check.** It ran inside a block
that pauses the game to hold a selection still, so nothing ticked and the
refill check went red on a mechanic that works. The tour was right and the
check was wrong, which is the good way round.

## 6. Still open

- **A day is not eight minutes.** `ServiceComplete` needs `_serviceTick >=
  ServiceTicks` **and** `_partyCount == 0`, so a fast food day plays for about
  552 seconds and **27% of the money lands after the clock the design talks
  about**. docs/27's session arithmetic is ~13% optimistic. Either the
  documents should say "eight minutes of arrivals plus about seventy seconds
  of drain", or the drain should come inside the window.
- **Fast food fights its second crest with tired staff.** `InLastQuarter()`
  fires at 6:00 and that crest starts at 6:14, so `cabuk_yorulan` costs a fast
  food player 20% of a hand through their busiest 106 seconds and costs a
  Turkish player nothing. That asymmetry may be worth keeping, but it is
  currently an accident of two independent constants and nothing measures it.
- **Two readings of the tail, and they are not the same size.**
  `ServiceDayLengthTests` opens the game and plays day one: fast food overruns
  its window by **40.1 s carrying 7.3%** of the day, and Turkish overruns by
  **nothing at all** - its room is empty on the stroke of the window, because
  its last slot runs at 0.82 against fast food's 1.51. The 27% above was
  measured across 60-day campaigns at fourteen tables. Both can be true; what
  is NOT established is the sentence "closing early was never right, on any
  day, in either cuisine", which was written from the campaign figure alone.
  On an opening Turkish day there is no tail to protect.
- **The fast food slot intensity is quoted as 1.60 here and derives as 1.505**
  from the archetype weights. Turkish agrees to three places. The likely
  reason is that this document's figure came from the arrival plan, after the
  day of the week, the season and the regulars are applied - but that is a
  guess and nothing measures the two against each other.
- **Mise en place** - banking kitchen work during the trough - is the one
  proposal from the rhythm study not built. It is the only mechanic that
  attacks the dead stretch with an ANTICIPATORY decision, and it is also the
  one with real core work in it.
- `OrderIngredient` has no phase guard and no screen that sends it during
  service, while `OrderRecommended`, the same mechanic in bulk, is guarded.
  One of the two was written with a phase in mind.

## 7. What it measured once it had shipped

The change was re-measured on the settled build, 8 seeds x 60 days, fast food.
Two of the three readings are not what I expected before taking them.

### The growth multiplier did not move - and could not have

| | before | after |
|---|---:|---:|
| fast food `makul` / `genislemeyen` | 20,763 / 12,980 | 20,763 / 12,980 |
| growth multiplier | 1.60 | 1.60 |

Byte-identical output is this project's signature for *the arm never ran*
([49](49-unreachable-mechanics.md)), so it was worth chasing. It is innocent
here: **`ReasonablePlayer` and `Expansionless` never intervene.** Only
`Interventionist`, `PatientInterventionist` and the pressured arm call
`DuringService`. The growth gate is the ratio between two bots that were never
touched by this change, so an identical reading is the correct reading.

The consequence matters more than the number. When the calibration reported
fast food's penalty falling 10 -> 6 earlier the same day, it had also moved
`CAP_COOK` 28 -> 30 in the same step. **That improvement was the cook cap, not
this work.** The growth multiplier is still 1.60 against a 1.8-4.0 target and
is still the open balance problem.

### The pool really does flow

| arm | charges spent | per day |
|---|---:|---:|
| `mudahaleci` (eager) | 2,809 | **5.85** |
| `sabirli_mudahale` (patient) | 1,092 | **2.28** |

Against [45](45-design-review.md)'s **0.8 a day** - the figure that said the
allowance was not binding, taken before the day was sharpened - the eager arm
now spends seven times as much. `Applied == Tried` on every row, so none of it
is a refused bot quietly reporting success.

### Intervening costs money and buys satisfaction

Both intervention arms wrap `ReasonablePlayer` and delegate `OnMorning` and
`OnEvening` to it unchanged, so the only difference between these three rows is
the interventions themselves:

| arm | cash | satisfaction | stars | customers |
|---|---:|---:|---:|---:|
| `makul` (never intervenes) | **21,955** | 85.8 | 9.3 | 2,594 |
| `mudahaleci` | 20,972 | **88.1** | **9.6** | **2,657** |
| `sabirli_mudahale` | 21,331 | 86.1 | 9.3 | 2,642 |

The first reading of this was "intervening is dominated - it earns less than
doing nothing". It is not. It is a **priced trade**: 4.5% of the cash for 2.3
satisfaction points and 63 customers, and the patient arm buys most of the
customers for 2.8% of the cash at **39% of the charges**. A mechanic that cost
nothing would not be a decision, and cash alone is the wrong axis to judge it
on - satisfaction is what carries into the next day.

What it does mean is that the pool cannot be the answer to the growth gate: it
moves satisfaction, and growth is gated on cash. That is the calibration's
problem, and it is where this goes next.
