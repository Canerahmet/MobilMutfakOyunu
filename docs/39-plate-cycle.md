# 39 — The plate cycle, the washing-up shift and the crew you inherit

*12 September 2026.* The user's request:

> **Oyun ilk başladığında aşçı ve garson olsun. Bulaşıklar çok biriktiği zaman
> garson bulaşıkları yıkamaya geçsin. Bulaşıkçı alınca herkes kendi işini yapar.**
>
> **Bulaşıkçının orada boş ve kirli tabaklar biriksin, bulaşıkçı onları lavaboda
> eliyle yıkasın ve temiz tabakları diğer tarafa dizsin. Aşçı oradan tabağı alıp
> yemek koysun, garson ise yemek konulan tabağı servis etsin. Oyunda belirli
> sayıda tabak olsun ki gerçekçi olsun — yani temiz tabak bitince bulaşıkçının
> yıkaması beklensin. Ayrıca garson yemek yenilen tabakları alıp bulaşıkçının
> kirli tabak kısmına bıraksın.**
>
> *("When the game first starts there should be a cook and a waiter. When too
> many dirty dishes pile up the waiter should go and wash them. Once you hire a
> dishwasher everybody does their own job.*
>
> *Empty, dirty plates should pile up at the dishwasher's place, the dishwasher
> should wash them by hand at the sink and stack the clean ones on the other
> side. The cook takes a plate from there and puts food on it, and the waiter
> serves the plate the food went onto. There should be a fixed number of plates
> in the game so that it is realistic — that is, when the clean plates run out
> you have to wait for the dishwasher to wash. Also the waiter should pick up
> the plates that have been eaten off and leave them in the dishwasher's dirty
> plate area.")*

This is not an invention: [docs/14](14-staff-system.md) describes the dishwasher
as one of four roles (wage 90, capacity 48, 28% of the hall load) and it wrote
down the reason as well — ***"if the plates run out the service stops. An
invisible bottleneck, but one you notice the moment it blocks. It teaches the
player 'do not neglect the thing that looks unimportant'."*** It had been
designed and not implemented: the simulation collapsed the four roles into two
pools and the washing-up was buried inside the hall capacity.

---

## 1. The crew you inherit: one cook, one waiter

Before, the hall was **empty** and on day one the owner did the entire service on
their own. Wrong for two reasons: the game says "you are the owner, not the
chef", but what the player sees at the opening is a one-person shop — themselves;
and without seeing what a waiter does, the decision "I hired a waiter" does not
say what it was good for.

As with the cook: the morale is the **starting morale** (otherwise they are born
below the resignation threshold and the hall empties out on day two), the trait
is **absent** (inherited staff are ordinary; character comes with the people
*you choose*), but they **have a name**.

| | before | after |
|---|---|---|
| `makul` end cash | 19,441 | 19,797 |
| reputation | 77.6 | 77.8 |
| served | 2,018 | 2,033 |

The passive bot and every exploit bot still go bankrupt on day 56. **No
recalibration was needed.**

---

## 2. The plate cycle

```
clean  ──(the cook plates up)──▶  in use  ──(the waiter clears the table)──▶  dirty
  ▲                                                                            │
  └────────────────────────(washed at the sink)────────────────────────────────┘
```

**14 / 20 / 26 / 34** plates by tier, coming from the content
(`tools/balance/export.py`). When the clean plates run out the cook cannot plate
up what has finished cooking (`PlateUp`) and the kitchen waits; the waiting is
**counted** (`PlateBlockedTicks`) — that is the measure of the sentence "the
dishwasher was neglected".

### Conservation is an invariant

`clean + in use + dirty = total` is tested **on every tick**
(`PlateTests.The_plate_count_is_conserved`), not at the end of the day: a counter
that breaks in an intermediate state and recovers by the end would pass an
end-of-day check. A leaking plate would be a bug that slows the service down day
by day with its cause visible nowhere.

### The plate count: measured three times, wrong all three times

| multiplier | tier 0 | result |
|---|---|---|
| x6 | 24 | **0 ticks** of waiting in 14 days — no bottleneck, the mechanic is decorative |
| x4 | 16 | still 0 ticks |
| x3 | 12 | **the automatic tour locked the restaurant up on day 1** |
| **x2 + 6** | **14** | measured pressure: minimum clean 1.3 / 34 |

The lesson in between has two parts:

**Why x6 and x4 did not bite:** the number of plates in simultaneous use is
limited by the **number of tables** (at most four people per table), so if the
plate count is above `tables x 4` it can never run out by definition. The balance
tool hid this because its bots grow fast and the averages swallow the first tier.

**Why x3 locked up:** twelve plates for four tables; two parties of four consume
the stock, a third party sits down, orders, the kitchen cannot find a plate,
their patience runs out, they leave angry — and the day closed with nobody
served. **The tour caught this, not the balance tool.**

### The line that broke the death spiral

```csharp
if (_platesClean < _pSize[best]) return;   // SeatWaitingParties
```

If there are no clean plates a new party is **not seated**. In a real restaurant
that is also the right call: if you have no plates you do not seat people and
leave them hungry. The waiting party waits at the door and comes in when a clean
plate appears — **the pressure is visible and the shop does not lock up.** With
this line the number could be brought down to 14 and the bottleneck became real.

The multiplier is small and the buffer is large (`tables x 2 + 6`), and that is
deliberate: the bottleneck should be in the **big** shop, not the **small** one.
A four-table restaurant has no plate crisis; a fourteen-table one does, because
the plate count grows linearly with tables while service speed grows with tables
x turnover.

---

## 3. The dishwasher: not a separate pool, a hall worker assigned to the sink

docs/14 sets the hall up as **"waiter + dishwasher + cashier, a single work
pool"** and the wage comes out of that blend. Opening a separate staff pool would
mean counting the same person in two wage tables — and on top of that it would
want its own trait/morale/resignation/candidate arrays, its own save fields and
its own interface.

The player's decision is the same either way: they **assign one person's worth of
crew to the sink** (`CommandKind.SetDishwashers`). If they do not, the waiter
switches to the sink by themselves once the dirty plates pile up and the service
stutters.

The whole hall crew cannot be given to the sink — then nobody would serve and the
game would lock itself up.

### Hysteresis is compulsory

With a single threshold the waiter washed one plate, went back to service, and
came back on the next frame: it read as **"going back and forth"**, not
"washing". The threshold for starting and the threshold for stopping are
separate.

### Why the washing *time* was not increased

The first thought was "make the washing slow so the pile builds up" and it was
**wrong**: `ClearMs` (clearing a table, 6,000 ms) is 31% of the total hall time
and in docs/14 the dishwasher's share is 28% — that is, clearing tables already
carries that share. Increasing the time would have counted the same work
**twice**. The pressure comes from the **count**, not from the time.

---

## 4. What is on screen

The dish station is built from a single function (`BuildDishStation`), because
all of it derives from each other's position: the washing figure in front of the
sink, the **dirty pile on the left, the clean pile on the right**. Computing them
in separate places would mean writing the same number in two places — in this
project that has drifted apart silently five times.

**One sink, not two** — and it was the *placement audit* that said so: two sinks
plus two counters overlapped by 0.16 m. The reason is arithmetic: the room is
3.2 m and each of the four objects is ~0.84 m, so 3.36 m is needed. Counter +
sink + counter is 2.52 m and fits comfortably.

The height of the piles is taken **by measurement** from the counter (`TopOf`),
not by writing it down: the first version guessed 0.92 m and the plates hung in
mid-air.

The waiter walks to the sink **carrying the dirty plates** and puts them down on
arrival — the counterpart of the user's sentence "the waiter should pick up the
plates that have been eaten off and leave them in the dishwasher's dirty plate
area".

The piles are placed **according to the flow**: dirty on the right (the halls are
at x > 8.4), clean on the left (the kitchen is at x < 5.2). The first version had
it the other way round and every plate crossed the room one extra time for
nothing.

### The cook picks up the plate

Two stages were added to the `CookRoutine` sequence — **TabagaGit** and
**TabagiAl** — and both are at the **very start** of the sequence, not in the
middle of cooking: a cook who abandons a hot pan and walks out of the room does
not read as "fetching a plate", it reads as "went off somewhere". In a real
kitchen the plate is prepared beforehand too.

That route goes through the Kitchen–Dish Station passage and **that passage is by
a hair's breadth**: the shared edge is exactly 1.40 m and the threshold is also
exactly 1.40 (`DoorWidth + MinJamb x 2`). It was measured — the passage *exists*
(the link count is 5; without it it would be 4), because `5.4f − 4.0f` in
floating point is 1.4000001 and the threshold is 1.4000000. So this edge rests
not on a safe margin but on the **last digit**. If the floor plan changes this is
the first place to look: the kitchen's neighbourhood with the dish station is the
game's most used passage.

### The tap runs, the plate is scrubbed with a sponge, there is foam

The washing figure has a **plate** in one hand and a **sponge** in the other;
**water runs** from the tap and it only runs while somebody is washing — a tap
that runs all the time does not say "being washed", it says "forgotten".

The water has to be a **material asset** (`custom_water.mat`): URP does not put
the shader variant of the transparent pass into the build unless an asset refers
to it, and a transparent material built at runtime is drawn **opaque** on the
device — with no sign of it in the editor. This project lived through that bug
once already, on the walls.

The foam is inside the basin: five overlapping pieces of different sizes. My
first attempt was four equal, evenly spaced panels and it read not as foam but as
**tiling**.

Seeing this needed a separate close-up (`dishwashing_*.png`): in editor mode
nobody is washing, the staff are built idle. `PreviewWash()` puts a figure at the
sink and makes it wash. The answer to *"is the water running, is there a sponge"*
is not in a number but in the picture — here, measuring means **looking**.

### But the tap never turned on in the game

The setup was right, the behaviour was wrong, and the diagnostic line had been
saying so for three runs: **`seen at the sink 0`**. The core was washing the
plates; on screen nobody arrived at the sink.

The cause is the same class as the cook's: the core's washing task is **2,000 ms**
and the game runs at x4 — half a second. Walking to the sink takes a few seconds,
so the figure finishes the task **before arriving**, `HallWashing` goes false and
the view sends them off somewhere else halfway down the path.

My first fix did not work either, and the reason was instructive: I had started
the hold counter **in the arrival callback** — but the arrival never happened, so
that line never ran. The counter now starts **when they set off** (with a safety
time; leaving it without a ceiling would lock a figure that got stuck once at the
sink for the whole day) and drops to the real waiting time on arrival.

The core reports "washed"; **the view tells the duration** — the same pattern as
`CookRoutine`.

### And the measure changed

Sampling `WashingCount` in a narrow window gave almost always zero. The new
measure is cumulative: `WashSeenFrames` — *did the player see somebody at the
sink today*. That is the tour's new question, and before the fix it was **0**,
after it **53 frames**.

What is worth recording: **the simulation doing something and the player seeing
it are two separate claims.** The check "the plates get washed (6 of them)" was
green and it was true — but nothing was happening on screen.

### The bottleneck is told to the player

When the clean plates ran out the kitchen stopped but **nothing was said** to the
player: the service slows down for no reason, and that reads as a **bug**, not a
mechanic. docs/14 describes the dishwasher as a bottleneck that is *"invisible,
but noticed the moment it blocks"* — the "noticed" part only happens if you say
so.

`SimEventKind.PlatesOut` was added:

> *Temiz tabak bitti — mutfak bekliyor. Lavaboda 6 kirli tabak var.*
>
> *("The clean plates have run out — the kitchen is waiting. There are 6 dirty
> plates at the sink.")*

If a dishwasher has been assigned the sentence changes (*"the dishwasher cannot
keep up"*): telling them to "hire a dishwasher" would be sending them to look for
a button that is not there. **The cause and the remedy in the same line.**

Not on every tick, **once per new blockage**: announcing it constantly would fill
the notification strip with a single sentence, and staying silent was the problem
itself. The flag drops when a plate comes out.

### The washing-up shift button is exercised in the tour

A new interface path, and in this project every path that is not tested has
broken silently at least once. The tour presses the button and verifies that
**the core's number** changed — by looking at the state itself, not at the text
on screen. Then it puts it back: the tour has to play the rest of the day with
the crew it *inherited*, not the one it changed itself, otherwise the later
measurements measure a different game.

---

## 5. Walking: the feet were sliding along the floor

`Anim.speed` was **not set anywhere**. The figure travels at `Walker.Speed x game
speed` but the walk clip always played at 1x:

| case | ground speed | clip tempo | deviation |
|---|---|---|---|
| x1 | 1.15 m/s | 1.28 m/s | 11% |
| x4 | 4.60 m/s | 1.28 m/s | **260%** |

The pedestrians on the street have randomised speeds on top of that (0.92–1.34)
and they did not match either.

### The clip's natural speed was measured, not guessed

The clip marks time (there is no root motion), so "how many metres per second
does this clip correspond to" is written nowhere. `PlacementAudit.WalkSpeed`
measures it:

```
stride length = the WIDEST the two feet open across the cycle
one cycle     = two steps
natural speed = 2 x stride / clip length
```

The feet are found **from the mesh, not from a bone** — this skeleton has no foot
bone (one bone per leg plus a knee added later). Among the vertices each leg
influences, the lowest one counts as that leg's foot.

The result: **the clip is 0.67 s, the stride 0.426 m -> 1.28 m/s.**
`Figure.WalkClipSpeed` is that number; if there is a deviation the audit writes
*"DEVIATION"*.

Now `Walker` calls `Body.SetGroundSpeed(Speed x multiplier)` on every frame and
the clip turns at the ground speed. When the walk ends (`Arrive`/`Stop`/`Warp`)
the tempo returns to normal — the speed of the sitting or the chopping clip has
nothing to do with ground speed.

**The tour's new question** is `WalkSlipWorst`: for every walking figure, the
relative deviation between `Anim.speed x WalkClipSpeed` and the ground speed. A
bug that is hard to notice by eye — and for exactly that reason it stood for
months. Measured: **0%**.

---

## 6. New measurements

**Core tests (223):**
- `The_plate_count_is_conserved` — on every tick,
  `clean + in use + dirty = total`
- `A_dishwasher_rescues_the_hall_from_the_sink` — the crew is held **equal**,
  otherwise what is measured is not the dishwasher but one extra person
- `The_plate_pressure_is_measured_at_the_peak` — it does not assert, it prints
  the numbers

**The balance tool** prints a new table (`plate pressure`): waiting with no
plate, minimum clean, maximum dirty. It is not enough for a bottleneck "to
exist", it has to be **felt** somewhere; a mechanic that is not felt is
decoration.

**The automatic tour:** the plates get dirty / the plates get washed / the plate
count is conserved — three separate checks, because all three can break silently
in three different ways.

### The checks were in the wrong place twice

These three checks were put in the wrong place **twice** and the diagnostics
showed both.

The first version put the three of them in the **liveliness window** and the
check became unstable: in a run where the window opened at 9% of the day nobody
had finished their food yet, so no table had been cleared and "the plates get
dirty" went red. What was being measured was not whether the cycle works but
**when the window opened**.

The second mistake was from the same family: the counters were **instantaneous**.
"How many plates are at the sink right now" does not say whether the cycle is
working — at the start of the day it is zero. The counters became cumulative
(`PlatesDirtiedToday`) and the checks moved to the **end of the day**.

The third was also something the diagnostic line showed: in one run the hall-fill
wait hit its own ceiling at 43% and only 1.4 seconds were left for the window.
The wait's ceiling came down from 45% to **25%** — the waiting itself must not
consume the day that is to be measured.

**And the fourth:** the block was written as *"asked at the end of the day"* but
in fact it sat before the camera section, **in the middle of the service**. In
one run the whole measurement section went by inside the first 12% of the day and
the counters came out zero again. The comment being right and the code being
wrong — the shape this project falls into most often. Now it really is after
`Close the day`.

### The foot-slide measurement misread once as well

`WalkSlipWorst` was at first **recomputing** `Speed x GameSpeed` and in one run it
reported a 123% deviation. There was no problem on the street: `GameSpeed` is
written by `GameApp.Update` and its order within the frame relative to
`Walker.Update` is not guaranteed, so on the frame where the game speed changed
the clip tempo was being measured against **the previous speed**. The figure had
covered the right distance on that frame and the next frame was already
correcting it.

Now `Walker.LastGroundSpeed` is read — *the distance actually covered on that
frame*. That was the thing that needed measuring in the first place: "did the
clip tempo match the distance covered".

| | value |
|---|---|
| `makul` end cash | 19,071 (19,558 before the plates, −2.5%) |
| `makul` minimum clean plates | **1.3 / 34** |
| `planci` waiting with no plate | 52 ticks |
| tour | **88/88**, in three consecutive runs |
| somebody seen washing at the sink | **53 frames** (0 before) |
| foot slide while walking | **0%** (260% at x4 before) |
| core tests | 223 |
| placement audit | 0 overlaps, 26/26 facing the right way |
