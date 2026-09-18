# 31 — Rooms and a two-step camera

10 September 2026. This document closes a single question: **what is the player touching?**

The question came out of a measurement, not out of design. At the end of [16-screens-and-tutorial.md](16-screens-and-tutorial.md) the open-hall layout was built in Unity and rendered at a real phone's aspect ratio, and the table turned out to be **15-20 dp** on screen. Google's minimum touch target is 48 dp, Apple's is 44 pt. So the answer to "do fourteen tables fit on the screen" was yes, but the question was the wrong one: they fit, they cannot be touched.

The measurement took four rounds. Each round was rendered and then **looked at**; trusting a number without looking has come out wrong every single time on this project.

---

## 1. First let this question be closed: can a table ever be 48 dp?

No, and there is no need to measure it, arithmetic is enough.

A 2,400-pixel landscape phone at density 2.75 → **873 dp of width**. A fourteen-table restaurant is 18 m wide at its narrowest. Even if the restaurant filled the frame with no margin and no perspective:

```
873 dp × 0.86 m / 18 m = 41.7 dp
```

And that is an **upper bound**: the perspective margin, the edge margin and the foreshortening of the depth axis can only push the number down. The depth direction shortens by a factor of 0.56 at a 34-degree view, so the table's vertical extent on screen is smaller still.

**On no camera that shows the whole restaurant can a table be the primary touch target.** Changing the layout, tightening the camera, enlarging the table — none of it reaches 48 dp. This is not a design preference, it is the size of the screen.

---

## 2. The proposal: make the restaurant modular

> *"Restoran modüler yapıda olabilir yani odalar şeklinde çünkü sonuçta bulaşık yıkanılan yer, yemek yapılan mutfak gibi bölümler olması lazım. Restoran genişletildiğinde de sanki yeni bir oda eklenmiş gibi masalar gelir."*
>
> *("The restaurant could be modular, that is, made of rooms, because in the end there have to be sections like the place where the washing-up is done and the kitchen where the food is made. And when the restaurant is expanded, tables arrive as if a new room had been added.")*

It has two separate gains and both are measurable:

1. **A gain in meaning.** [14-staff-system.md](14-staff-system.md) already splits the hall into two pools: the cook pool and the hall pool (waiter + dishwasher + cashier). A room is that pool's counterpart in space. When you hire a dishwasher it is not a number that fills up, it is the **sink room**.
2. **A gain in touch.** In the open hall there is no object at all to touch between "the whole restaurant" and "one table". A room is precisely the intermediate target that fills that gap.

## 3. The proposal: a two-step camera

> *"Tüm her şey aynı anda görüldüğü durumda oyuncu dokunarak kamerayı o modüler kısma yaklaştırmış olur."*
>
> *("With everything visible at once, the player would touch to bring the camera closer to that modular part.")*

This picks one of the options left after §1 closed the other road. The measurement confirms it — but on its own it is not enough; the reconciliation in §7 is needed.

---

## 4. Four rounds

| Round | Layout | Result |
|---|---|---|
| 1 | The open hall (`RestaurantScene.cs`) | The table is 15-20 dp at every tier. **Failed.** |
| 2 | Rooms in a single row | The restaurant turned into a 25 × 4.6 m corridor; half of the 20:9 frame stayed empty; and because the camera pulled back at every tier, **the touch target was shrinking as the business grew.** The room idea is right, the arrangement is wrong. |
| 3 | Rooms on an equal 2×2 grid | The numbers held, **the render looked artificial.** Every room the same size, every dividing line aligned. |
| 4 | **Rectangles of different sizes** | Accepted. |

The flaw in the third round was not shown by a number, it was shown by looking:

> *"Restoran yerleşimi tamamen kare olmak zorunda değil, hatta şu anki görünüm biraz yapay duruyor, dikdörtgenlerden oluşabilir. Odaların boyutu birbirinden farklı olabilir."*
>
> *("The restaurant layout does not have to be completely square — in fact the current view looks a bit artificial; it could be made of rectangles. The rooms could be different sizes from each other.")*

## 5. The accepted floor plan

The plot is **18.0 × 9.6 m**, eight rooms, 172.8 m². The rooms cover the plot with no gaps, but their sizes differ and the dividing lines are not aligned: the left half's horizontal line is at z = 4.4, the right half's at z = 5.0.

```
   0        5.2   8.4          13.4        18.0
9.6 +--------+-----+------------+-----------+
    |        |STORE|            |           |
    | KITCHEN|3.2x |   HALL 2   |  HALL 4   |
    | 5.2x5.6| 4.2 |  5.0x5.2   |  4.6x4.6  |
5.4 |        +-----+            |           |
4.4 |        |     |            |           |
4.0 +--------+SINK +------------+-----------+ 5.0
    | ENTRY  |3.2x |            |           |
    | 5.2x4.0| 5.4 |   HALL 1   |  HALL 3   |
    |        |     |  5.0x4.4   |  4.6x5.0  |
  0 +--------+-----+------------+-----------+

The store is stuck to the kitchen's right edge: a delivery comes in from
the back, goes down into the store and up into the kitchen. The sink is
at the front, because dirty plates come from the hall.
```

The service rooms are always there. The halls open with the tiers: Hall 1 (4 tables) → Hall 2 (+3) → Hall 3 (+3) → Hall 4 (+4) = **4 / 7 / 10 / 14 tables**, one to one with the tier table in [12-economy.md](12-economy.md).

**The plot is fixed, the building grows.** The camera never pulls back. Rooms that have not been built stand as bare floor and the edges facing them get a **low temporary wall** — a full wall was tried and it hid the expansion area completely, so the player could not see where they were going to grow. The walls on the plot's outer boundary are at full height.

The interior partitions are **0.85 m**. A 2.6 m full wall was tried and it cut off the backs of the chairs in the back row; at a 34-degree view a wall that goes up to the ceiling hides what is behind it.

The walls are not placed by hand, they are **generated from the room's edges**: if the neighbour is built, a low partition (with a gap in the middle if it is long); if it is not built, a temporary wall; if it is the plot boundary, a full wall; and if it is the front or right edge facing the camera, nothing at all. That is why the walls do not have to be reworked when the room sizes change.

That the floor plan covers the plot with no gaps, and that the intended tables fit in every room, **is checked in code**. That check caught a bug on its first run: `4.6 − 0.9 = 3.6999998`, which divided by `1.85` gives `1.99999`, and taking the floor gives one column instead of two. Three tables did not fit in Hall 3, and that might not have been visible in the render.

---

## 6. The measurement result

Every number is the **smaller of the target's two axes** on screen. Measuring only the horizontal makes the target look bigger than it is.

> **This section was re-measured on 11 September 2026 and the numbers changed.** The cause was two bugs; both of them were in the measuring tool, not in the game:
>
> 1. **The measurement was setting up the camera with its own hands.** `32f`, `Euler(34, -12, 0)` and a closed-form distance formula were written inside `RoomLayout.Shoot`. The game, meanwhile, was using `CameraFit`'s binary search, and that finds the smallest distance. The formula erred on the safe side in every term and gave **30% too much distance** — that is, the measurement was reporting a smaller target than the game actually shows. `CameraFit` had been written for exactly this, and the measuring tool had never been wired to it.
> 2. **The interface bars were not being accounted for.** In the game the top strip and the action bar take about 30% of the screen and the camera fits into the strip that is left. Measuring without the bars made the target look bigger than it is.
>
> Once both were fixed, the old camera setting (32°, −12°) came out with a real floor of **48 dp** — touching Google's minimum exactly, with no margin. The tables below are the numbers for the new setting (22°, 0°).

### The general view — the open rooms in frame, the interface bars in place

| Room | dp (20:9) | 48 dp |
|---|---|---|
| Sink | 121 | ✓ |
| Hall 1 | 102 | ✓ |
| Kitchen | 99 | ✓ |
| Entry | 94 | ✓ |
| Hall 2 | 91 | ✓ |
| **Store** | **71** | ✓ |
| *table* | *23* | ✗ |

**The floor is 71 dp**, with 48% of margin. The table does not pass and by §1 it never can — in the general view the touch target is the room.

At the narrower aspect ratio (16:9) the floor is **81-89 dp**; so the worst case is 20:9 and even that is 71. Closed rooms are not in the table: they are no longer drawn and cannot be touched.

> **The 71 above is the measurement from before the street was added.** On 12 September 2026 it was
> re-measured while the street was being widened, and it turned out that two notches
> had been lost in the meantime — because nobody measured. The camera frame spreads
> out over the street by the open rooms **plus `CameraFit.StreetInFrame`**, and that
> number was added later:
>
> | street in frame | floor 20:9 | floor 16:9 | when |
> |---|---|---|---|
> | none (0.00 m) | 71 dp | — | the first measurement, before the street |
> | 1.10 m | ~64 dp | — | the street was added, **not re-measured** |
> | 1.94 m | ~~59 dp~~ **43 dp** | ~~74 dp~~ **54 dp** | two pedestrian lanes — **the 59 was wrong, see below** |
> | **2.66 m** | **41 dp** | **51 dp** | the terrace moved off the walking lanes, 18 September 2026 |
>
> Those are the four-table readings. **The floor falls as the restaurant
> grows**, so one tier is not the answer — every tier, same build, one
> constant changed:
>
> | tables | 4 | 7 | 10 | 14 |
> |---|---|---|---|---|
> | 1.94 m | 43 | 43 | 42 | 42 |
> | **2.66 m** | **41** | **41** | **40** | **40** |
>
> Two dp, flat. (2.50 m was measured too and reads the same 41/40, so the
> extra 0.16 m goes on the tarmac strip rather than into the budget: at 0.16 m
> of road in frame the street stops reading as a street.)
>
> The reason for the 1.10 → 1.94 widening is that the pavement needs
> **two pedestrian lanes**: figures walking in opposite directions were passing
> through each other. The lane spacing comes from the figure's measured widest
> body band (the head, 0.67 m — the `PlacementAudit` PROFILE lines). The reason
> for 1.94 → 2.66 is that the terrace — the rail, the planters and the cafe
> tables — stood **on those lanes**, so passers-by walked through the railing
> along the whole front of the building ([59](59-kitchen-equipment-and-models.md) §8).
>
> ### And it happened again, to the row above
>
> **The 59 dp was stale in exactly the way the 71 was.** Re-measured on 18
> September with the same build and one constant changed, the floor at 1.94 m
> is **43 dp**, not 59. The sentence this table used to end with — "59 dp is
> 23% above Google's 48 dp minimum" — was never true of the build it was
> written in.
>
> **This is not a gate that was secretly failing.** [41](41-ui-and-venue.md)
> §"10 degrees was chosen" re-measured the same line on 13 September, got
> **42–43 dp**, and argued the price: what falls below 48 is not a button but
> the SHORT edge on screen of a room whose long edge is more than twice that,
> and the player can zoom in with two fingers. That argument stands, and this
> document simply had not been told. Two documents holding the same number, one
> of them updated.
>
> **What the street change cost is 2 dp**, and what it bought is in
> [59](59-kitchen-equipment-and-models.md) §8. What it also cost is **all of
> the margin**: `RoomLayout` goes red below 40, the worst tier was 42, and it
> is now 40.
>
> **It is still the right trade, and it is one constant to reverse.** The two
> dp buy a defect that is in every frame along the whole front of the building,
> permanently — passers-by walking through the terrace railing and standing
> inside the cafe tables. They cost two dp on a number whose subject is a ROOM,
> not a button. Anybody who disagrees sets `CameraFit.StreetInFrame` back and
> re-runs `RoomLayout.Capture`.
>
> **Buying the margin back is a LAYOUT change.** At this pitch no lens setting
> widens a 3.2 m room; `Store` is the floor at every tier. That is open.
>
> The floor is the short edge of the **narrowest open room**, `Store` at 3.2 m.
> Widening the margin again is a LAYOUT change and not a camera one: at this
> pitch no lens setting widens a 3.2 m room.
>
> The lesson is not that 71 was wrong, it is that it **was left standing while it
> was right and never asked again** — and then 59 did the same thing, in a
> paragraph written to warn about it. A number that is cheap to re-take and is
> not re-taken is a number nobody knows.

### The room view — the camera has moved in on one hall

| Target | dp | 48 dp |
|---|---|---|
| table + chairs | 96 | ✓ |
| the table top | 44 | ✗ |

**The same numbers at all four tiers.** 4, 7, 10, 14 tables — they never change. The reason is no longer "the plot is fixed": the camera frames the open rooms, but what binds the frame is **the depth, not the width**, and the kitchen block covers the plot's whole depth from day one.

The touch region is **not the table top but the table set**: the table plus two chairs, 1.86 × 1.86 m on the floor. The table spacing is also 1.85 m, so the regions tile without overlapping.

### The camera angle: what the −12° rotation was costing

When the user looked at a screenshot and said:

> *"boş odalar yer kaplamasın, restoran tam ekran olan yerler gözüksün"*
>
> *("do not let the empty rooms take up space, let the parts where the restaurant is full-screen show")*

the camera angles were **measured** for the first time. Until then 32°/34°/−12° was a preference, not a measurement result.

| setting | how much of the frame is restaurant (opening / grown) | floor touch target |
|---|---|---|
| 32°, −12° | 27% / 33% | 52 → **48 dp** |
| 32°, −6° | 34% / 39% | — |
| **22°, 0°** | **48% / 43%** | **71 → 71 dp** (before the street; today 59) |

Zeroing the rotation raises the frame's fill from 27% to 41%; dropping the field of view from 32 to 22 flattens the perspective and wins the rest. The tilt stayed at 34 — the sense of depth comes from there, and increasing it **lowers** the fill (18% at 42°, 17% at 50°).

What was lost is that slight "2.5D turned-ness". Looking at the render it holds up: the tilt and the side faces of the furniture already give the depth, and the rotation was only eating the frame.

### Empty rooms are no longer drawn

At the first tier only 102.6 of the plot's 172.8 m² is open; so **41%** of the screen was a dark grey slab of "not yours yet". The slabs' colour (0.16) had once been balanced by measuring three brightnesses — the numbers were right, **the question was wrong**.

Now an unopened room is not built at all: no floor, no touch collider. Expansion is now genuinely an *opening* — the room appears where there was none. The camera (`CameraFit.OpenBounds`) also frames only the open rooms.

### Shadow distance: an "improvement" in the wrong unit

A second bug came out in the same round. A performance pass had lowered the URP shadow distance from 25 to **14 m**, on the grounds that *"the plot is 18 × 9.6 m, so everything is inside the shadow map."*

The reasoning was in the wrong unit. URP measures that distance **from the camera**, not from the size of the scene — and the camera stands 26-35 m away. So the value had quietly switched off every cast shadow. No test caught it; it was seen by looking at a screenshot.

The new value is **45 m** = in the worst case (the narrow aspect ratio, thick bars) the scene's furthest corner at 39.4 m plus margin. The shadow map went from 512 → **1024**: 45 m in a single cascade with a 512 map means 18 cm per texel, and the shadows become unrecognisable.

Also, `ProjectSetup.ConfigureUrp` was holding a **second copy** of all these numbers and knew nothing about the performance pass — anyone running `ApplyAll` was putting MSAA back from 1 to 4 and the shadow distance back from 45 to 25. The two were made equal, and `tools/check_urp.py` catches the divergence from now on (the 12th check).

---

## 7. Reconciling this with the research

[30-venue-layout.md](30-venue-layout.md) scanned twenty-six games and reached a different conclusion: **adopt rooms as an art and expansion metaphor, do not adopt them as the interaction model.** Keep the camera fixed during service, make the primary touch target the patience chips in the bottom bar, and let the room-framed camera live only on the layout-editing screen.

Its harshest objection is numerical and has to be taken seriously:

> At tier 4 there are four hall rooms. A full tour = 3 room changes. If the player wants to scan the restaurant once per slice, that is **12 taps** — 120% of the entire budget for the service stage, in return for zero decisions.

**That calculation rests on an assumption: that the player has to walk the rooms in order to see what is going on.** The measurement removes that assumption.

In the general view **a room is 51-95 dp**. So a badge can sit on top of a room and be touched. The player does not go to a room to see what is happening in it; the general view is the scan itself.

### The reconciled decision

| Camera | What is visible | Primary target | Navigation taps |
|---|---|---|---|
| **General** (the default; you can stay here throughout service) | the whole restaurant | **the room badge** (51-95 dp) | 0 |
| **Room** (optional) | one hall and its neighbours | **the table set** (65 dp) | 1 per action |

**Zooming is not compulsory.** The game can be played from the general view from start to finish: the badge sits on the room, touching it opens the intervention. Moving in on a room is for looking, not out of necessity.

This accepts the research's harshest constraint (zero compulsory navigation taps) while **not paying the biggest cost the research itself counted**: the hall is not demoted to decoration, because what is touched is not a strip in the bottom bar but a room inside the scene. "Layout design" and "visible growth", 2nd and 3rd in `research/01` §3's list of best-loved mechanics, stay on screen throughout service.

The bottom-bar chips can still be written and would satisfy `review/05`'s item "let the patience warning come through three channels" — but as a **second channel**, not as the primary target.

### The research's other four items were accepted

| # | Decision | Status |
|---|---|---|
| 1 | Build the venue out of bays; a tier adds a new hall bay | ✓ Accepted, implemented |
| 4 | The layout-editing screen in the Kairosoft pattern: tap a tile → ghost grid → confirm | ✓ Accepted, to be written into [16](16-screens-and-tutorial.md) screen 11 |
| 5 | **The bays are not sealed rooms: no door validity, no corridors, no pathfinding.** The waiter's walk is visual | ✓ Accepted. [14-staff-system.md](14-staff-system.md) already says "no complex pathfinding" |
| — | Restaurant Renovation should come off the source list (a match puzzle) | ✓ Accepted |

Item 5's numerical justification holds in the new floor plan too: the kitchen's centre is at (2.6; 6.8), the furthest hall's centre at (15.7; 7.3), **13.1 m** apart. At a walk of 1.2 m/s that is 10,900 ms one way; [27-time-model.md](27-time-model.md) gives the waiter 9,000 ms per customer. A real walk exceeds the service budget. The new plan is far more compact than the old strip (20.8 m → 13.1 m) but **the walk still must not be simulated.**

### What the research left open, and what has closed

| # | Question | Status |
|---|---|---|
| 1 | Write `RoomLayout.cs`'s console line to a file; the room dp values there were a calculation | ✓ Closed, the numbers in §6 are measurements |
| 2 | If the camera fit is corrected, how many dp does the open hall reach | ✓ Closed, §1: the upper bound is 41.7 dp, it never reaches 48 |
| 8 | Restaurant Renovation comes off the list | ✓ Closed |
| 3 | **What is the store room's job** | Open — it will become clear once the equipment schemas are written |
| 4 | How many hall bay variants are needed | Can be counted as closed: **all four** halls are different sizes, it does not read as copy-paste |
| 5 | The camera state is not saved ([23](23-core-contract.md)) | Open — the effect is small because the general view is the default |
| 6 | Does the chip bar conflict with the top bar | Open — the chip is a second channel now, the pressure is off |
| 7 | What does a second branch mean in a room layout | Open, out of scope for the first release |

---

## 8. The three things this requires

1. **The badge sits on the room, not on the table.** A table is 16 dp in the general view; there is no point putting a badge there. A "3 customers waiting" badge sitting on the room is both visible and touchable. **Built on 17 September 2026** ([58](58-visual-review.md) §13.2): one badge per open dining room showing the room's worst table, 2.20 m wide which measures about 87 dp in the overview frame, with a touch target that selects that table. The table badges are hidden while the overview is up and the room badges while a room is - one readout at a time, because two of them at two sizes make the reader choose, which is the cost this decision existed to remove.
2. **The zoom is stepped, not continuous.** Two steps: general and room. If a free zoom (pinch) is added, the table stays between 16 and 30 dp in the intermediate steps, so touch does not work in that range. With steps, the touch rule is definite at each step.
3. **The room view is not a room, it is a ZOOM STEP.** Fitting a 5.0 × 4.4 m room vertically into a 20:9 frame necessarily means showing about 12 metres horizontally. When the player moves in on a room they also see part of its neighbours. That is good: the context is not lost.

## 9. The room glossary

| Room | Size | Area | Inside it | The system it is tied to |
|---|---|---|---|---|
| Kitchen | 5.2 × 5.6 m | 29.1 m² | stove, counter, extractor hood | the cook pool ([14](14-staff-system.md)), station slots ([32](32-equipment-and-rebalance.md)) |
| Entry / till | 3.2 × 5.4 m | 17.3 m² | the till, the door, the tray station | the hall pool, the cashier |
| Wash room | 5.2 × 4.0 m | 20.8 m² | **two** sinks, two plate counters | the hall pool, the dishwasher |

**These two swapped places on 18 September 2026**, at the user's request: the
door moved one room to the right and the dishwasher to the leftmost room. Only
the rectangles changed - the names, the array order in `RoomPlan` and every
lookup by name stayed as they were. The wash room needed the extra width for a
reason the code had already written down: at 3.2 m a counter, two sinks and a
counter do not fit, so two people washing up shared one basin and stood inside
each other. See [58](58-visual-review.md) §14.2.
| **Store** | 3.2 × 4.2 m | **13.4 m²** | cold room, dry shelf, crates | **stock and spoilage** ([32](32-equipment-and-rebalance.md) §7) |
| Hall ×4 | 4.6-5.0 × 4.4-5.2 m | 20-26 m² | 3-4 table sets | table capacity ([12](12-economy.md)) |

### The store's job was found

The first version of this document said: *"The store is empty and that is a risk. Art should not be spent on a room with no simulation behind it; if it cannot be given a visible job it should come out of the layout."*

**The job was found and it had been there all along.** The `spoilDays` field in the content had been written but the simulation never read it: 44 of the 77 ingredients are perishable, their shelf lives run from 1 to 45 days, and all of them were being deleted every night. An onion was going in the bin overnight just like minced meat. The store is the reason that field exists. Detail in [32-equipment-and-rebalance.md](32-equipment-and-rebalance.md) §7.

**The store is a small back room stuck to the kitchen.** A delivery comes in from the back, goes down into the store and up into the kitchen. The sink room was brought to the front, because dirty plates come from the hall and the dishwasher is in the hall pool. The fridge in the kitchen was removed: cold storage is the store's job now and there is a visible upgrade ladder there; showing both would have been a lie.

**A measurement drew a boundary.** The store was first made 3.2 × 3.8 m and measured **46 dp** in the general view — just under the 48 minimum. At 4.2 m of depth it is **51 dp**. So the "small room" request has a floor: because depth foreshortens by a factor of 0.56 at a 2.5D view, a shallow room becomes untouchable. The store today is less than half the kitchen and still touchable.

---

## Layout: are the models running into each other

On 11 September the user said:

> *"restorana yerleşen modeller üst üste binmiş gibi"*
>
> *("the models placed in the restaurant look as if they are overlapping")*

The eye is not enough — at a 34-degree view an object behind another can look as though it is on top of it, while two that really do overlap can both look innocent. `unity/Assets/Lokanta/Editor/PlacementAudit.cs` turns the question into a measurement: it takes every object in the scene's box **in its real pose** and prints the intersecting pairs.

```powershell
tools\unity\run.ps1 -Method "Lokanta.EditorTools.PlacementAudit.Run"
```

**Why BakeMesh:** `Renderer.bounds` lies on a skinned mesh — Unity derives it from the root bone and does not update it as the pose changes. In the first measurement a **seated** figure appeared to be 1.68 m tall and 1.66 m wide; both are impossible.

**The tool measured wrong twice, and both times it was caught by its own self-verifying line.** `BakeMesh` deforms the mesh according to the *bones*, and the bones already sit under the scaled root — so the scale is **already in** the output. The `useScale` flag only adds the renderer's own scale. Both `true` + `localToWorldMatrix` and `false` + `localToWorldMatrix` apply the scale twice. The right way: the flag off and the scale taken out of the transform. On every run the tool measures the height of a standing figure and compares it with the `ArtPrefabs` target; if it does not match, the numbers are rubbish.

### What was found, and the result

| | before | after |
|---|---|---|
| overlapping pairs | **68** | **0** |
| figure × figure | 67 | 0 |
| figure × furniture | 0.45 m | 0 |
| the largest | 0.54 m | — |

- **Four people do not fit at this table at any reasonable scale.** The cell is 1.85 × 1.70 m, the table's diameter 0.88 m, a seated figure's footprint 0.90 × 1.01 m. To fill four seats the figure's width would have to be ≤ 0.545 m — that is, **a human being 0.66 m tall**. The arithmetic comes out the same at every scale. At most **two** guests are drawn on screen (`RestaurantView.VisibleGuests`); the simulation is not affected, the party is still four people and so is the bill.
- **The seat radius is 0.48 m**, the intersection of two constraints: two guests must not run into each other (2r ≥ 0.90) and the set must not spill over into the neighbouring set (r + width/2 ≤ 0.925).
- **The character went from 1.28 to 1.10 m.** The yardstick for that first reduction was the "figure/chair **height** ratio" and it was asking the wrong question: this pack's figures are large in their **width** rather than their height (the head is a third of the body).

> **These three items are NO LONGER VALID — the numbers were updated in [35-animation-and-camera.md](35-animation-and-camera.md).** The table was a hexagon (`tableRound`, 0.88 diameter) and because four seats cannot be aligned with 60-degree edges the guests were landing on the corners; it is now **square** (`Furniture/table`, 0.82 m, squared at setup). The seat radius is **0.58**, the character **0.95 m**, the table **0.55**, the chair **0.68**, `SitLift` **0.26**. The rules that four people do not fit and that two guests are drawn on screen stay the same.
- **In the kitchen the fridge was inside the first stove** (0.30 m) — both of them were in the back left corner. The fridge was moved to the back right and the stove row leaves room for it.
- **Staff were running into each other** (0.13 m): the step was 0.95 m, the figure's width 0.80 m. The step became 1.10 and the second row 1.30. Also, the cooks were standing at `Z0 + 1.1` and the front counters at `Z0 + 0.55` — the cook was standing **inside** the counter. They are in the room's middle strip now.

### The furniture and the characters face OPPOSITE ways

The user's second sentence:

> *"Sandalyeler ters."*
>
> *("The chairs are backwards.")*

That was true, but the chair was only the most visible example — **all of the furniture was standing 180° backwards**.

It was measured (`Lokanta/Figure scale screenshot`, every object at yaw 0, a red cube at +Z, a blue cube at −Z, seven pieces of furniture in one frame):

| | the "front" direction at yaw 0 |
|---|---|
| character | **+Z** (its face) |
| chair, stove, counter, sink, fridge, shelf | **−Z** (the cushion, the oven door, the handle, the tap) |

The code did not know about that difference and was thinking "like a character" whenever it wrote an angle. The result: the stoves were turning their **backs** to the wall, the counters to the outside, the chairs to the table; the guest looked buried inside the backrest.

The fix is a single constant: `RestaurantView.PropYaw = 180`. The angles at the call sites are written **in the character's convention** (0 = face +Z) and the constant closes the difference — so there is no separate 180 to remember for every new item.

**The rule:** when adding a new model, do not **assume** its local "front" direction, **measure** it. Assuming that two models face the same way was the bug itself.

There is a separate verification image too — `Lokanta/Figure scale screenshot`: one figure, one chair, on a 1 m grid, **from the side**. The question "is it sitting, or standing in front of the chair" cannot be answered in a full hall seen from above; in this image no ambiguity is left. That is how it was verified that the sitting pose really was being applied — the problem was not the pose, it was the scale.

---

## How to re-measure

```powershell
tools\unity\shot.ps1 -Method "Lokanta.EditorTools.RoomLayout.Capture"
```

The output is `tools/art/out/unity/floor_*.png` plus the `MEASURED` (measurement) lines in the log. The line to look at is **`FLOOR`** (the floor): the short edge, on screen and with the interface bars in place, of the smallest *open* room. If the table size, a room size, a camera angle or a tier layout changes, **re-measure and look**.

Anyone changing the camera angles touches `unity/Assets/Lokanta/Game/CameraFit.cs`; the measuring tool reads the same constants, so the two cannot diverge.
