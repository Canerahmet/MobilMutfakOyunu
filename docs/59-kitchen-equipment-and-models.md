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
equipment ladder in: six shared stations, cuisine-specific ones on top,
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

**Inserting in the middle renumbered every station after it**, which is safe
only because the closed list in `ContentSetLoader.StationIds` and the generator
both carry the same order and both are regenerated together. Three tests were
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
| `izgara` | char grill: slatted bars over an ember bed, drip tray | **bars and width**: the bed grows and the ember row with it |
| `firin` | oven with a glass door, a lamp behind it, racks | **racks**: 1 → 2, and a second door at tier 2 |
| `fritoz` | fryer: oil wells with a still amber surface, baskets on a rail | **wells**: 1 → 3 |
| `soguk` | the cold counter (the existing fridge model) | a second door |
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

## 4. Where they stand, and every bound is measured

The kitchen is **5.2 × 5.6 m** at (0, 4.0). Three constraints bound every
placement and none of them is negotiable:

- `Paths.KitchenPost` put working cooks in a fixed row at z = 8.25. **That went
  too**: a cook works in front of ITS OWN station now (`CookRoutine.Post`),
  derived from the station's transform, because the stations no longer all
  stand on the back wall.
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
| buying changes the picture | `StationsStale` compares each object's built tier with `StationTier` | green |
| the objects fit | the placement audit: 0 clashing pairs, 0 props out through a wall, both cuisines | green |
| nothing teleports | `Walker.WorstWarp` under 0.30 m, with the offender named | green |
| a station with nowhere to stand is not silent | it is switched off and logged by name and width | proven by the Turkish pide oven, which was the first thing it caught |
| the fryer changed nothing it should not | `check.py`: dish balance, content generation and 249 core tests | green |
| the frame still fits the budget | `MEASURED scene budget` | 296 renderers and 61k triangles at full expansion, against 400 and 80k |

**What is not measured, and is worth saying.** Whether the equipment READS at
the game's own camera — whether a player can tell a fryer from a grill at 34° —
is a judgement, not a number. `render/kitchen_*_line.png` was added for it: the
room shot is taken at the game's angle, which is the right test for "can the
player read it" and the wrong one for "is the model correct", where a stone
oven's dome is forty pixels.
