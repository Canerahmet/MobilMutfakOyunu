# 36 — The street, the time of day, doors and the kitchen's stages

12 September 2026. This round takes the restaurant from being a **floor plan** and makes it a **place**: there is an outside, time passes, the rooms have doors, and food is really being cooked in the kitchen.

Five jobs hang together and all of them answer the same question: *does what I see on screen tell me what the simulation knows?*

---

## 1. Doors between the rooms

The walls arrived in the previous round but the figures were walking through them — what a wall is for only becomes clear once it has a door.

### Walls line by line, doors from room pairs

Two versions were tried and both were wrong:

| version | the bug |
|---|---|
| each room's four edges separately | shared lines were being drawn **twice** and the alpha stacked; that wall came out darker than the others and invented a meaning, "there is a thicker wall there" |
| **one** door per line | on the x = 5.2 line there are **three separate adjacencies** (Entry-Sink, Kitchen-Sink, Kitchen-Store) and a single door only served one of them |

The right way: walls line by line (the **union** of the intervals, in one piece), doors **from room pairs**. One door for every pair that is adjacent and opens onto each other, in the middle of the shared edge — or, if the shared edge contains the front lane (`Paths.LaneZ`), there, because that is where the crossing happens anyway.

**The gap is clamped, not discarded.** The first rule was "leave at least 15 cm of wall on both sides", and that discarded the three most-used doors at once: the lane is at the very start of the wall, so there is no wall at all on the left. An opening flush with the corner is itself already correct.

### The rooms' logic: the store only from the kitchen

The user's rule is written in one place, inside `Connect()`:

```
Depo       ->  only Mutfak
all others ->  open if they are neighbours
```

(`Depo` is the store and `Mutfak` is the kitchen — the room ids.) In a real lokanta the larder is behind the kitchen too; you do not get into it directly from the hall or from the sink room. The sink room is already next to the kitchen (on the x = 5.2 line, the shared edge z 4.0-5.4).

**The rule is tested** (`RestaurantView.AccessOk`): the door graph is walked and two things are verified — is every open room reachable from the entry, and *with the kitchen closed off*, can the store still be entered. The second is the machine's version of the "only from the kitchen" rule. The automatic tour asks it on every run.

### The doors open for whoever comes near

`Door.cs`. The hinge is at the **edge** of the leaf (rotating from the model pivot buries the leaf in the wall — exactly what had happened with the oven door). The detection radius is **1.10 m**: the walking speed is 1.15 m/s and the door opens in 0.28 s, so the leaf is fully open about 0.8 m before the figure arrives.

No collider: the touch target is the room floor (docs/31's measurement), and a ray hitting a door would break the room selection. The door does **not stop** the figure either — it is image only.

---

## 2. The street

Customers were appearing at the bottom edge of the frame, and that place was nothing. What gives the feeling of "they came from outside" is the place they came from **existing**.

Three slabs: pavement, kerb, asphalt. Four street lamps. Customers now appear at `Paths.Street(i)` — a few metres from the door — and come walking along the pavement.

> **This section's numbers changed in [docs/38](38-street-and-interior-light.md).** Four
> lamps became three and they are placed against the *open rooms* rather than the plot; the
> pavement widened enough to take two pedestrian lanes (pedestrians were passing
> through each other) and the walking line moved off the asphalt onto the
> pavement — `PavementZ` used to be −1.05 while the pavement ran between −0.62 and
> −0.02, so everybody was walking in the road.

**The frame opened 1.10 m forwards, and that was chosen by measuring.** The camera frame depends on the depth; every metre added at the front shrinks the restaurant on screen. The touch-target floor fell **71 → 64 dp**; Google's minimum is 48 dp.

> Later it became **1.94 m** ([docs/38](38-street-and-interior-light.md)), the floor **59 dp**.
> Also, the 64 above was *calculated but not measured* that day; and the 71 in docs/31
> had not been updated when the street was added either.

### Passers-by

`StreetLife.cs`. The user's sentence:

> *"sokaktan geçen karakterlerin tamamı lokantaya gitmesin, bazıları yola devam etsin veya kendi aralarında konuşup sonra yola devam etsinler."*
>
> *("do not let all the characters passing along the street go into the lokanta; let some of them carry on down the road, or talk among themselves and then carry on.")*

Five people, in two directions along the pavement. A pair drawing level stops with a 35% chance and talks for 2.2-4.5 seconds, **turned towards each other**, then carries on. There is an 8-second cooldown so that the same pair does not immediately talk again — without it, it reads as "a jam" rather than "a conversation".

**They do not touch the simulation at all:** they are not customers, the core does not know about them, none of them sits down at a table. The randomness is on its own seed (fixed), with no contact with the core's RNG — the tour has to see the same street on every run.

---

## 3. The time of day

> *"Oyun içerisinde sabah öğle ve akşam ayırımı belli değil."*
>
> *("In the game the distinction between morning, midday and evening is not clear.")*

That was true: the phase of the day could only be read from a line of text in the interface.

`DayLight.cs` changes **four channels at once**, because a single channel (light intensity alone, say) reads not as "evening has come" but as "somebody turned the lamp down":

| channel | morning | midday | afternoon | evening |
|---|---|---|---|---|
| the sun's angle | 26° / 148° | 62° / 208° | 34° / 246° | 10° / 268° |
| the light's colour | cool-warm | neutral | warm | purple-blue |
| intensity | 1.05 | 1.55 | 1.25 | **0.32** |
| background (the sky) | pale blue | light blue | warm | almost black |
| lamps | — | — | — | **lit** |

The source is a single number: `Simulation.ServiceProgressBp`. The values are interpolated — no hard cut, otherwise a frame jumps and says "it is 14:00 now".

> **A core ratio has to be an INTEGER.** The first version was named `ServiceProgress01` and returned a `float`, and the floating-point ban test rightly went red (docs/23). The ratio is given in basis points; converting it to floating point is the view's job.

### The street lamp: a light that stands in the scene and is never drawn

In the first version a **point light** was put on each lamp and it did nothing: in the URP asset additional lights are **off** (`m_AdditionalLightsRenderingMode: 0`, docs/19's mobile budget). A light that stands in the scene and never enters the draw — it means looking at a "lit lamp" and seeing nothing, and nothing warns you.

Instead: a slab lying on the pavement, **unlit and additively blended**, with a soft circle texture generated at runtime on it (64×64, with no file put on disk). Free, and genuinely visible.

A flat-coloured slab gave a **square** pool of light; a circle with a soft edge turns the same slab into a real pool of light.

---

## 4. The kitchen's stages

> *"Aşçı yemek pişirirken malzemeleri alsın, onları yıkasın, doğrasın, pişirsin; ocağa tava koysun, piştikten sonra tavadaki yemeği tabağa aktarsın."*
>
> *("While the cook is cooking, let them pick up the ingredients, wash them, chop them, cook them; let them put a pan on the stove, and once it is cooked transfer the food from the pan to a plate.")*

`CookRoutine.cs` — a nine-stage sequence:

```
ToFridge -> Take (ingredient in hand) -> ToCounter -> Wash -> Chop
   -> ToStove (the PAN on the stove) -> Cook -> Plate -> ToPass
```

The clips come from the pack's ready-made ones; no new animation was produced: `attack-melee-right` → chopping (an arm coming down from above), `interact-left` → washing, `pick-up` → picking up an ingredient. The pack carries no pan, so it is made out of three boxes (body, handle, the food inside it).

### Why a sequence, why it does not look at the simulation every frame

It was measured (the automatic tour, `"simulasyon is verdi 145 kez"`): **the core holds the cook's job open in only about 10% of the frames** — a job lasts a few hundred milliseconds. When the view followed that directly the cook took two steps towards the fridge and turned back; **they never reached any station**, and so never entered any working pose. The kitchen was completely empty and that was why.

The solution: when the core gives a job, the view **starts a sequence** and plays it to the end. The economy does not change — nothing here writes anything to the core, it only tells what it reads at human speed.

### Two more traps

- **Cooks have to be asked EVERY FRAME.** The staff loop was checking "has the job changed" and was **missing** a job shorter than one tick: the simulation gave a job ten times, the view saw it zero times. The cook branch is on its own loop now, every frame.
- **A working figure's Animator has to stay on.** `Figure` switches the Animator off about 1 s after a transition — correct for a seated customer (they do not move), a disaster for a working cook: they were freezing on the **first frame** of the chopping clip. "The pose was set" was green, the image was dead.

### The cook turns to face what they are looking at

On arrival the angle was written as a fixed **180°**; whatever the cook did they faced the same way, and they were using the stove with their back to it. The angle now comes from the target's position — and the target comes **from the stage**: the counter while washing, the stove while cooking. The check had first assumed "a working cook faces the stove" and measured a 176° deviation; it was right, the cook was looking at the right place, and the check was asking about the wrong target.

Idle staff no longer stand at a fixed angle either: they stay facing the way they walked.

---

## 5. The waiter's tray

> *"Garson her seferinde belirli sayıda yemek ve içecek taşıyabilsin; tepsi kullanıp kullanmaması da bu sayıyı etkilesin."*
>
> *("Let the waiter be able to carry a certain number of dishes and drinks each time; and let whether or not they use a tray affect that number.")*

The rule: **one plate in the hand, two or more with a tray.** The tray's capacity is 3 — a fourth spills past the figure's width. The number is read from the simulation (how many tables are waiting for food), so it is not made up.

**In the view layer, an image and not a constraint.** The core's hall capacity is a person-day model; adding a per-trip carrying constraint changes the economy and would require the sixty-day balance to be solved again. If a real mechanic is wanted, that is its cost, and a calibration run would be needed as well.

---

## 6. The objects' orientation — measured

> *"Restoran içerisindeki eşyaların yerleşimini ve doğrultularını kontrol et."*
>
> *("Check the placement and the orientation of the objects inside the restaurant.")*

The rule can be measured: **an object set against a wall must face into the room.** A stove's mouth cannot face the wall, a counter's front cannot face the wall. An object's "front" comes from the `RestaurantView.PropYaw` rule, so there is no second assumption.

The result: **15 objects set against walls, 0 of them backwards.** (The doormat was excluded: a horizontal slab has no "front face" — the check was counting it as backwards, and that was not a bug but the rule not applying to that object.)

Why measure: "somebody looked at it" is true once; when the floor plan or the layout changes, nobody looks again.

---

## Verification

| what | result |
|---|---|
| `tools/check.py` | 12/12 |
| core tests | 220 |
| the automatic tour (a real Windows build) | **69/69** |
| the placement audit, both cuisines | 0 overlaps, 15/15 correct orientations |
| the touch-target floor (20:9) | 64 dp (minimum 48) — *today 59, see docs/38* |
| the room access rules | pass (the store only from the kitchen) |

The questions the tour added in this round: the room walls were built / the walls are transparent and have no colliders / the doors were built / an approaching figure opened the door / the customer comes from the street / there are passers-by on the street / the room access rules / work is being done in the kitchen / a working figure's animation runs / the cook faces their target / the morning and evening backgrounds differ / the sun's angle changes through the day / the sun weakens in the evening / the street lamps are lit only in the evening.

### The measurement itself was breaking the thing it measured

Worth recording: the liveliness checks (a walking figure, a door opening, a customer on the street, a working cook) were at first waiting **one by one**, and the tour passes service at ×240 — four of them in a row comes to about twenty-five seconds, during which the service window closes and the last checks were measuring an **empty** hall and going red. The speed was brought back to normal and all of them are sampled **in a single loop**.

A second one of the same class: the table-selection check was waiting for the "number of occupied tables" and then looking for an "active table" in a separate loop; the table could empty in the few frames in between. **The thing you wait for and the thing you look for have to be the same.**
