# 59 — The kitchen you can see: equipment, models and animation

18 September 2026.

> *"Mesela fast food için patates kızartma yapılan yerin modeli var mı?
> Patateslerin modeli var mı? Yağ nasıl olacak … Türk mutfağında kebap varsa
> onun için bir ızgara kısmı olabilir … Tabi bunları satın alınabilir ekipman
> şeklimde yapmak ve seviyeye göre yapmak lazım. Ayrıca yeni alınan ekipmanlara
> göre restoran nasıl olacak?"*
>
> *("For fast food, is there a model of the place where the chips are fried? Is
> there a model of the chips? What about the oil … in the Turkish kitchen, if
> there is kebab there could be a grill section … And of course these should be
> made as purchasable equipment, by level. Also, how will the restaurant look
> as new equipment is bought?")*

---

## 1. The thing that already exists, and the thing that does not

**The mechanic is built.** [32](32-equipment-and-rebalance.md) put the whole
equipment ladder in: seven shared stations, cuisine-specific ones on top,
`slots`, `attendBp`, a price derived from the rent of the tier at which the
item becomes necessary, and `CommandKind.BuyEquipment` to buy it. The
simulation knows at every instant which station is loaded, how many slots it
has and what tier it is at.

| | stations |
|---|---|
| shared | `ocak` · **`fritoz`** · `izgara` · `firin` · `soguk` · `icecek` · `tatli` |
| fast food | `milkshake_makinesi` · `waffle_makinesi` |
| Turkish | `tas_firin` · `doner_ocagi` · `pide_firini` |

**The kitchen draws none of it.** What stands in there is three copies of one
stove prefab, a fridge and two prep counters. `UpdateStoves` spreads sixteen
possible stations over those three objects by `station % 3`, so stove 0 shows
the sum of stations 0, 3, 6, 9… There is no model for a grill, an oven, a
drinks station, a döner spit or a stone oven, and **buying a piece of equipment
changes nothing you can see.**

That is the gap this document closes. The measurement to beat is blunt and
honest: **buy a station, and count how many pixels of the frame change.**
Today the answer is zero.

## 2. One content change: fast food has no fryer

The user asked the right question, and the answer was worse than expected.
**All eight** of fast food's hob dishes are deep fried:

| dish | what it is |
|---|---|
| `patates_kizartma` | chips |
| `baharatli_patates` | seasoned chips |
| `nugget` | nuggets |
| `sogan_halkasi` | onion rings |
| `mozzarella_cubuk` | mozzarella sticks |
| `crispy_tavuk` | fried chicken |
| `acili_kanat` | hot wings |
| `balik_burger` | the fish patty |

So the cuisine does not have a hob at all. It has an **unlabelled fryer**, and
has had one since the menu was written.

The station `fritoz` is added between `ocak` and `izgara`, and all eight dishes
move to it. **With the hob's numbers, exactly**: same `conc` (3.33), same
`attendBp` (3500 → 2800 at the top), therefore the same ladder, the same prices
and the same pressure at the same table counts. This is a naming and a MODEL
correction, not a balance change — and the fact that nothing about the work
changed is why the campaign numbers do not move.

Turkish keeps the hob for its thirteen dishes and never sees the fryer:
`Simulation.IsStationUsed` is per cuisine, so a station nothing cooks on is
never compulsory and never priced.

Everything here is generated, so the change goes into `tools/balance/model.py`
and `tools/content/gen_dishes.py`, never into `content/`.

**Inserting in the middle renumbered every station after it.** The content and
the loader are safe, because `ContentSetLoader.StationIds` and the generator
carry the same order and are regenerated together. **The save file is not
regenerated**, and that is where the renumbering actually bites: a file written
before the fryer stores tiers and job assignments against the OLD numbering, so
loading it would have moved every station's tier one place and sent every cook
to the wrong station. `SaveVersion` goes to 23 and `Restore` migrates a file
below it - the old array is read at its old length, index 1 is opened up for
the fryer at tier 0, and every `jobStation` at or above 1 is pushed up one.
`SaveTests` reads the migrated file back and asserts it against
`ContentSetLoader.StationIds`, so the migration and the loader cannot drift
apart silently. Three more tests were
holding their own copy of that order and said so when it moved — a station
count written as `6`, a station named by index `0`, and a list of ids typed out
a second time. All three now ask the loader.

The Turkish side needs nothing: `izgara` already carries `adana`, `kofte`,
`tavuk_sis` and `kuzu_pirzola`, and `doner_ocagi`, `tas_firin` and
`pide_firini` are already in the ladder. What Turkish is missing is not a
station, it is the MODELS — see below.

## 3. Every station gets a model, and the model shows its tier

The rule is one object per station the cuisine actually uses, built from the
`Modeler` like everything else in this project — no new asset, no licence row,
no download. The tier is visible in the object itself, because "I bought a
thing and the thing appeared" is the whole point.

| station | the object | what a tier adds |
|---|---|---|
| `ocak` | steel range, back splash, knobs along the front | **burners**: 1 → 4 rings on the top plate |
| `izgara` | char grill: nine slatted bars over an ember bed, drip tray | **embers**: 2 → 5 lit elements. The bed and the bars are a fixed size |
| `firin` | oven with a glass door and a lamp behind it | **a second chamber**: one door at tier 0, two at tier 1. The ladder has two rungs, so there is no tier 2 |
| `fritoz` | fryer: oil wells with a still amber surface, baskets of chips on a rail | **wells**: 1 → 4, one per rung of the ladder it inherited from the hob |
| `soguk` | cold counter: doors, a handle rail, a temperature panel per door | **a second door**, and a second panel with it |
| `icecek` | drinks tower: three nozzles, a cup rest, a lit panel | a fourth nozzle |
| `tatli` | dessert case: two glass shelves with cakes on them | a second shelf fills |
| `milkshake_makinesi` | three spindles over a cup rest | a fourth spindle |
| `waffle_makinesi` | two hinged round plates, a ready lamp | a second iron |
| `tas_firin` | domed stone oven, arched mouth, fire glow inside, chimney | the mouth widens |
| `doner_ocagi` | a tapered cone of meat on a spindle, a radiant panel behind | a second spit |
| `pide_firini` | wide low oven, wide mouth, a peel leaning beside it | a deeper mouth |

**What is lit and what is not.** The ember bed, the oven lamp, the fryer's heat
glow, the döner's radiant panel and the drinks panel are all *emissive*, which
in this project means an UNLIT material and a colour written by a property
block — the lesson `Appliance` already carries: with a lit material the lamp
inside an oven is drawn as a dark panel, because no light reaches in there.
Everything else is ordinary geometry on the shared lit material.

**The load drives the glow, one station to one object.** The `station % 3`
fan-out goes. Station *i*'s object shows station *i*'s load against station
*i*'s slots, which is what the player is deciding about.

**How finely it reads depends on how many lit elements the station has.** A
four-burner range shows four steps; a stone oven has one fire and therefore
reads on/off. That is honest for the object - an oven either has something in
it or it does not - but it is worth saying, because "load against slots"
suggests a meter everywhere and it is a meter only where there is a row of
things to light.

## 4. Where they stand, and every bound is measured

The kitchen is **5.2 × 5.6 m** at (0, 4.0). Three constraints bound every
placement and none of them is negotiable:

- `Paths.KitchenPost` put working cooks in a fixed row at z = 8.25. A cook
  works in front of ITS OWN station now (`CookRoutine.Post`), derived from the
  station's transform, because the stations no longer all stand on the back
  wall. `KitchenPost` itself **remains as the fallback** for a station with no
  object, and it still carries the old `station % 3` spread; it is reached only
  when `StoveOf` returns null.
- `Paths.CookHome` puts idle cooks in the middle of the room.
- `Paths.BackDoor` opens the wash-room gap at **x = 0.72** on the front edge,
  and a figure needs 0.44 m of width through it.

So the middle of the room stays clear and the equipment lines three walls:

```
 z=9.6  ┌────────────────────────────────────────┐   back wall: the hot line,
        │ [ocak/fritoz][izgara][firin ][ soguk ] │   filled greedily by measured
        │                                        │   width. 4.20 m usable.
 [left] │[tatli ]                       [icecek] │
        │                                        │   left wall: from z = 5.35, so
 z=6.5  │      (clear: idle cooks, 2 rows)       │   a cook coming through the
        │                                        │   gap is clear of it.
        │[tas_fi]                       [pide_f] │
 z=4.6  │      [pass / service counter]          │   right wall: the stretches the
 z=4.0  └──[gap]─────────────────────────────────┘   two doorways leave.
        x=0                                  x=5.2
```

**Greedy by measured width, not a fixed pitch.** A 1.05 m pitch is right for a
0.90 m range and wrong for a 1.16 m stone oven: the first run put the Turkish
stone oven and pide oven **0.86 m inside each other** and the placement audit
said so. Each station reports the width of its own built geometry (including
the peel leaning on it), and the layout packs by that.

**Every station stands on the floor.** The first version sorted them into
"floor" and "bench" and stood the small ones on the prep counters. Turkish ran
out of room — a stone oven, a döner spit and a pide oven were left with nowhere
to go, and the audit found all three piled at the origin, 0.58 m outside the
wash room, three rooms away. The small appliances bring their own stand
(`KitchenStation.Plinth`), which is what a burger bar actually does with a
milkshake machine, and the layout has one list to solve instead of two.

**A station with nowhere to stand is switched off and said out loud.** It used
to be left at (0, 0, 0). The guard is in the tour, not just the log:
`StationsMissing` counts the stations the simulation uses and the kitchen does
not show, and `StationsStale` counts the objects showing a tier other than the
one that was bought.

### 4b. Three things the rebuild had to fix on the way

- **The doorway was behind the service counter.** Moving the wash-room gap to
  x = 0.72 put it under the pass, which spans 0.75 → 4.45. The audit does not
  see it because the counter is part of `Decor`.
- **The extractor hood was sized for three stove prefabs**, `r.W - 1.9`, with
  its mouth at 1.44 m — and an oven is 1.52 m tall, so the hood cut through it.
  It is measured off the line that was actually laid, and hangs a hand's width
  above the tallest thing in it.
- **The idle cooks did not fit.** Both side walls carry equipment now and a
  figure is 1.14 m across the arms, so three abreast in the clear band wanted
  3.42 m and had 3.0. They stand in two rows.

## 5. The animation, and the rule that the character does the moving

> *"tüm bu geçişler karakterler tarafından yapılsın yani modelle bir anda oraya
> ışınlanmasın veya orada oluşmasın"*
>
> *("let all these transitions be done by the characters — the model should not
> teleport there or appear there")*

Three pieces:

**The dishwasher holds a plate and a sponge, and the sponge moves.** Both were
already there and both were on the same side, so it read as a figure holding a
plate with something stuck to it. The plate moves to the left hand and the
sponge stays right; the scrub is a per-frame offset rather than a clip, because
this character pack ships no scrubbing animation and adding one would mean a
new asset, a licence row and a retarget. What reads at this camera is that one
hand MOVES while the other holds still.

**The basin fills.** The foam used to float in an empty steel box, which reads
as litter rather than as washing up. There is a still surface a few
centimetres under the rim now, its height measured off the sink like everything
else in this project that sits on a surface. It is on the LIT material with a
pale colour, not on the tap's transparent one: a 2 cm slab of transparent
material over dark steel is invisible, and a still surface reads by catching
the light.

**The flame is under the pan.** The pan anchors are created AT the burner
rings, in the same loop, so ring *i* is under pan *i* — the alignment is by
construction rather than by two lists agreeing.

**Nothing teleports.** `Walker.Warp` now records how far it moved a figure, and
`Walker.Appear` is the same call for a figure that was not on screen a moment
ago — a guest arriving on the pavement, or the editor's single preview frame.
The difference is visible in the distance: the legitimate warps are
zero-distance and a teleport is metres. The tour reads one number.

It found one immediately: **the whole kitchen crew slid to new posts in a
single frame** whenever anybody was hired or left. They walk now, unless they
are a figure `Instantiate` made a few lines above and that has never been
anywhere.

## 6. The backdrop, on the third attempt

> *"ekranın en üstünde bulunan o gölge gibi kısımlar görselliği bozuyor onları
> da kaldıralım veya daha güzel bir arka plan yapalım"*
>
> *("those shadow-like parts at the top of the screen spoil the look, let us
> remove them or make a nicer background")*

Third report. The skyline was fixed twice and each fix answered a real cause —
first it was receiving the near scene's shadows, then it was lit where it
should have been unlit. Two correct fixes that still leave the user looking at
something they cannot name is the signal to stop fixing and change the thing.

Silhouettes are gone. What replaces them is what [58](58-visual-review.md) §10
proposed for the flat backdrop in the first place, and it is a better answer
than shapes: **a sky gradient**, eight unlit bands, lighter at the horizon and
deeper above, with nothing in it that can be mistaken for an object.

**It follows the day.** The bands are painted from the same colour `DayLight`
gives the camera's clear (`DayLight.Apply` → `RestaurantView.TintSky`), so
morning, noon, evening and night carry it without a second curve to keep in
step — which is the drift this project has been bitten by five times.

## 7. How this is measured

Nothing here is finished because it looks finished.

| what | how | state |
|---|---|---|
| every station the cuisine uses has an object | the tour counts `StationObjectCount` against `StationsMissing` | green |
| buying changes the picture | `StationsStale` compares each object's built tier with `StationTier` | **green, and worth less than it looks - see below** |
| the objects fit | the placement audit: 0 clashing pairs, 0 props out through a wall, both cuisines | green |
| nothing teleports | `Walker.WorstWarp` under 0.30 m, with the offender named | green |
| a station with nowhere to stand is not silent | it is switched off and logged by name and width | proven by the Turkish pide oven, which was the first thing it caught |
| an old save still loads | `SaveTests` migrates a version-22 file and asks the loader where the fryer went | green |
| the fryer changed nothing it should not | `check.py`: dish balance, content generation and 251 core tests | green |
| the frame still fits the budget | `GameShot` fails the run above 360 renderers / 70k triangles / 220 unbatched | 297 renderers and 61k triangles at full expansion |

**The staleness guard is self-satisfying as it stands.** `StationsStale` is
recomputed from the same `StationTier` the view rebuilt from, and the tour
never buys a station, so the guard has never been asked a question it could
answer wrong. It is left in because it costs nothing and it will catch a
rebuild that skips a station - but "buying changes the picture" is, today,
proven by hand and by the tier renders, not by the tour. Making the tour buy
its way up the ladder and re-count is the honest version and it is not written
yet.

**What is not measured, and is worth saying.** Whether the equipment READS at
the game's own camera — whether a player can tell a fryer from a grill at 34° —
is a judgement, not a number. `render/kitchen_*_line.png` was added for it: the
room shot is taken at the game's angle, which is the right test for "can the
player read it" and the wrong one for "is the model correct", where a stone
oven's dome is forty pixels.

## 8. What four agents found when they were pointed at it

The work above was checked by four independent readers with no stake in it:
one on the models and the placement, one on the paths and the animation, one on
the guards themselves, one on the economy. They were asked for defects, not for
approval. **Every finding in this section was reproduced before it was
believed and re-measured after it was fixed.**

The ones that were changing what the player sees:

| finding | why it mattered |
|---|---|
| the pans were built in station-local space | a pot placed on a station standing on the left wall landed over the pavement; the guard counted the MARKER, which was in the right place, so it was green |
| guests sat 0.41 m into the chair | `SitLift` was applied to a body the seating code then overwrote. Only in play - the editor preview never runs the walk, so every frame I had judged from was blind to it |
| `soguk` had no lamp | the one station whose load reads on a light had no light |
| `fritoz` tiers 2 and 3 were the same object | `Mathf.Clamp(tier + 1, 1, 3)` on a four-rung ladder: the player paid for a tier that changed nothing |
| `Own` radius excused an intruder by 3 cm | the clash guard used 0.78 m where the chair's outer edge is 0.68 m |
| the wash post leaked | a washer promoted to cook kept its sink, so the second sink could never be claimed |
| `UpdateScrub` ran while paused | the sponge kept scrubbing behind the pause menu |

The ones that were about the guards, which are the more expensive kind:

| finding | why it mattered |
|---|---|
| `GameShot` was never invoked by `check.py` | the screenshot and the whole scene budget were only ever taken by hand |
| `PlacementAudit` had **no failure path** | it counted clashes and printed them; nothing read the number. It logs `PROBLEMS:` now, which `run.ps1` treats as fatal |
| `run.ps1` and `shot.ps1` grepped for `HATA  :` and `SORUNLAR:` | spellings that had not existed since the repository was translated. The fatal-pattern list matched nothing |
| `check_licenses` searched the whole ledger for "own work" | one row saying it made every asset exempt |
| `check.py` toured only Turkish | the fryer is fast food's station and fast food was never played |

The last five are all the same defect wearing different clothes, and it is the
one CLAUDE.md rule 4 exists for: **a check that does not run looks exactly like
one that passes.** Four of the five had been green for weeks.

### And what was done about them

| finding | what changed |
|---|---|
| `Intruding` only knows table sets, so a cook could walk through the stove run | the kitchen stations are in it now, as RECTANGLES from their measured width and depth - a circle round a 1.16 x 0.70 m oven is wrong at both ends |
| pedestrians walked through the terrace rail, and stood inside the cafe tables | the street was in the wrong physical order. See below |
| `check_grade.py` read values but not `active:` or `m_OverrideState` | both switches are read, and a CLOSED list refuses an override nobody has classified. Proved by switching `ColorAdjustments` off and watching it go red |
| the `NoteIf(x, x)` family reported UNMEASURED where it should report RED | the condition is now something other than the answer: `HasCombo`, `HasCredit`, `BadgesEarned > 0` - and the rows that belong to every run are plain `Note` |
| `tools/store/compose.py` was in no script | `tour.ps1 -Store` runs it straight after copying the frames. Shooting them and making them Play-legal is one action |
| `ArtCheck` had no failure path | it counts problems and logs `PROBLEMS:`. It also checks the thing it never did: the figures are 1.10 m and every distance in the game is measured against that |
| `check_licenses` skipped `Mesh/`, `Prefab/`, `Materials/`, `Animator/` as "generated" | two of them are not ours - `Mesh/` holds geometry extracted from the Kenney FBX files. All four have a ledger row now. Kenney is CC0, so nothing was ever at risk; skipping a folder is not a judgement about its licence, it is the absence of one |
| the thirteen sound keys were Turkish | renamed to English. Both lists said "the names stay Turkish because they are the literal strings `Sfx.cs` looks for" - which is a reason for them to MATCH, not a reason for them to be Turkish |

### The one that mattered: "not a balance change" was not true

Section 2 argues that moving eight dishes to a station with the hob's exact
numbers cannot change the campaign, and cites `check.py` as the evidence.
**`check.py` has no campaign step.** It validates the content, generates it and
runs the core tests; it never plays a sixty-day season. The guard cited for
neutrality could not see the thing it was cited for - CLAUDE.md rule 4, and
this time in a document that had just been written.

Run properly - the harness at `3ddab61^` against `3ddab61`, same seeds, same
machine - the fast-food report **moved on 252 lines**, and two of docs/12's
design questions changed their ANSWER, not their number: putting somebody on
the sink went from losing to `planci` to beating it, and the player who never
expands went from 12,750 to 17,735. The growth multiplier at the project's own
32-seed verification fell from **1.60 to 1.14**, against a floor of 1.80.

**The content was innocent; the cause was one line and it predates the fryer.**
`Simulation.BuyEquipment` learned to refuse a station the cuisine does not cook
on - reason 4, added when the six-thousand-coin oven turned out to be for sale
to a Turkish player who could never use it. `NextEquipmentPrice` was left
quoting a price for that same purchase. So everything that asks "what does the
next tier cost" believed there was one:

- the harness bot's optional-purchase loop spent its one purchase a day on a
  command the simulation threw away, and `return`ed as though it had bought
  something;
- the morning equipment screen drew a live **Upgrade** button with a price on
  it, and pressing it produced the generic rejection notice, which names no
  reason and moves no money.

This was dormant while every cuisine used every shared station. Inserting
`fritoz` at index 1 made `ocak` dead for fast food and put it at **index 0** -
the first thing the loop tries - so from that commit the fast-food bots never
bought another optional upgrade at all.

`NextEquipmentPrice` returns -1 for an unused station now, which is how that
method already says "there is nothing to buy", and the equipment screen filters
on the same flag the kitchen has always filtered on. Measured after: `makul`
21,955, `genislemeyen` 12,750, `bulasikci` 24,927 below `planci`'s 26,163 -
the pre-fryer numbers, to the coin.

**And it uncovered something that was already true.** The growth multiplier was
**1.60 before any of this**, against docs/12's floor of 1.80. The fryer did not
break that gate; it was failing, and the campaign had not been run often enough
for anybody to notice. That is a balance question, not a bug, and it is open.

### Two more the same audit found

- `EquipmentTests.The_docs27_peak_slot_table_holds` skipped any station its
  table does not name. That is right for the cuisine's own equipment and wrong
  for a shared one: `fritoz` reached the content with no row in docs/27 and the
  skip made it invisible - drifting the fryer's top slots from 4 to 5 left the
  test PASSING, while the same drift on `ocak` failed it. The closed list of
  shared stations is checked first now, so a station the table has never heard
  of is the failure rather than the exemption.
- `gen_dishes.STATION_TIERS` had no `fritoz` row, so `max_tier` fell through to
  2 and the generator believed the fryer's ladder ended at tier 1. It did not
  bite only because none of the eight dishes ranked that high - which is luck.
  The comment above that table records the identical failure one station
  earlier, with three fast-food desserts locked for sixty days.

### The street was in the wrong order, and that cost frame depth

From the building outwards it ran: **near pedestrian lane (-0.37)**, terrace
rail (-0.42), planters (out to -0.65), cafe tables (-0.95), far lane (-1.07),
kerb. The near lane was INSIDE the terrace - 0.37 m from a wall with a rail 5
cm beyond it - so a pedestrian's torso went through that rail along the whole
length of the building every time anybody walked left.

The cafe tables were worse and more visible. They stood on the pavement and
were declared obstacles for the passers-by to push against, with **one push
radius for every obstacle: 0.40 m**. A table is 0.36 m across and a body 0.29,
so the push needed 0.65 and the figures stood inside the tables instead
(`render/zoom/before-pedestrians-in-the-terrace.png`).

The pavement is not wide enough for a terrace AND two pedestrian lanes: a
0.72 m cafe set plus 1.37 m of lanes plus the kerb comes to 2.5 m and there
was 1.94. So the terrace moved behind the rail and got shallower - a bench
0.30 m deep with its back to the building - and the lanes moved outside it:

    wall 0 | bench -0.19 | rail + planters -0.42 (out to -0.65)
          | lane -0.97 | lane -1.67 | kerb -2.02 | tarmac -2.34

**The price is real and it was measured, not waved through.**
`CameraFit.StreetInFrame` goes 1.94 -> 2.66, and every metre at the front
makes the restaurant smaller on screen. The touch-target floor
(`Editor/RoomLayout`) was re-taken at every tier, same build, one constant
changed - one tier is not the answer, because the floor falls as the
restaurant grows:

| tables | 4 | 7 | 10 | 14 |
|---|---|---|---|---|
| 1.94 m | 43 | 43 | 42 | 42 |
| **2.66 m** | **41** | **41** | **40** | **40** |

So the street costs **2 dp, flat**, and the worst tier goes 42 -> 40 against a
red line at 40. That spends the whole margin, which is why the trade is
written down rather than waved through: what it buys is in every frame along
the whole front of the building, what it costs is two dp on a number whose
subject is a room and not a button, and it is one constant to reverse.

**And re-taking it found a stale number, again.** `CameraFit` said the floor
was 59 dp; it was 43, and had been since before this work started. docs/41 had
re-measured the same line on 13 September, got 42-43 and argued the price -
what falls under Google's 48 is not a button but the SHORT edge of a room whose
long edge is twice it. That argument stands; two documents held the number and
one of them was updated. What the street change really costs, then, is not the
gate but the MARGIN under it: `RoomLayout` went red below 40, so an accepted 43
had three dp of room and 41 has one. The red line is a ratchet at 41 now.

`StreetLife.PostClear` went 0.40 -> 0.48 with the rest, which is the post's
0.185 plus a body's 0.29 - the shortfall that made the tour report a pedestrian
inside a lamp post on some runs and not others.

### Still open, written down rather than quietly dropped

- **The tour never buys a station**, so `StationsStale` has never been asked a
  question it could answer wrong. See the note under the table in section 7.
- **The merged decor group is invisible to the placement audit.** `Decor` is
  one mesh per colour covering the whole building, so its bounding box overlaps
  everything; the service counter, the shelves, the hood and the tray station
  are inside it and cannot be measured pair-by-pair. An axis-aligned box over a
  merged mesh cannot answer the question either way - splitting the group would
  answer it and would cost renderers the budget does not have.
- **Turkish string literals in C# are not checked.** `check_english` reads
  identifiers, file names and prose, not literals, which is why the sound keys
  survived this long. A blanket rule would fire on the content ids (`ocak`,
  `izgara`), which are deliberately Turkish, so it needs an allow-list built
  from the content - worth doing, not done.
- **The colour grade's cost on a device is still unmeasured.** Post-processing
  forces the camera through an intermediate target and no desktop run can
  measure that bandwidth. docs/21 carries it.

