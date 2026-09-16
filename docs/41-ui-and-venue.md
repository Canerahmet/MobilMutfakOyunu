# 41 — Redesigning the interface, and the identity of the venue

*12 September 2026.* The user brought three reference images, and the request
grew in stages:

> **"Oyun arayüzünü baştan tasarlamanı istiyorum, referanstaki görsel gibi bir
> arayüz olsun."**
>
> *("I want you to redesign the game's interface from scratch; make it an
> interface like the one in the reference image.")*

> **"Farklı mutfaklar için şöyle bir referans tasarım var."** (dört mutfak yan
> yana: burger, Türk, İtalyan, Japon)
>
> *("Here is a reference design for the different cuisines." — four cuisines
> side by side: burger, Turkish, Italian, Japanese)*

> **"Sadece buton ve şeyler değil; tüm restoran, karakterler, mutfak, mobilyalar,
> modeller, ışıklar — kısacası her şey referanstaki gibi olsun. Gerekirse sıfırdan
> modelleri vs. her şeyi yap. Oyun arka plan mantığı kalsın."**
>
> *("Not just the buttons and such; the whole restaurant, the characters, the
> kitchen, the furniture, the models, the lights — in short, make everything
> like the reference. Redo the models and everything from scratch if you have
> to. Keep the game's background logic.")*

The last sentence also draws the boundary of the work: **the core simulation is
untouchable, the view layer may be rebuilt.** This document writes up the first
and second slices of that work.

---

## 1. What the reference says

Put the four frames side by side and what stays the same separates cleanly from
what changes:

| unchanging | changing |
|---|---|
| camera angle, interface layout, colour roles | the colour and material of the wall |
| badge + bar, capsules, four buttons, green action | the light of the sign |
| the place and shape of the cards | the rug on the floor, the planters, the sign colour |

So **the interface is fixed, the venue carries the identity.**
[docs/10](10-cuisine-identity.md) had already written this down as a rule:
*cuisines are differentiated by environment, light, silhouette and clothing.*
The reference is a picture of that rule.

---

## 2. The interface: cards, not strips

The old game screen was two **full-width dark strips** with equivalent grey
buttons sitting side by side inside them. Measured: morning 179 dp, service
152 dp, evening 199 dp.

The new layout is the reference's layout — cards and capsules floating in the
corners:

```
[12] Service ▓▓▓▓░░░   Rent in 6 days      (🪙 8,000)(💎 30.0/55)  (⚙)
┌ Today ────────┐                                    ┌ Takings    ┐
│ ● Served    12│                                    │ 1,006 ¤    │
│ ● Walkouts   0│                                    ├ Satisfact. ┤
│ ● Tables  2/4 │                                    │ 94.7       │
└───────────────┘                                    └────────────┘
[⏸][×1] [ Speed up | Tea | Attention ●●●● ]            [▶▶ Close the day]
```

**Re-measured (13 September 2026, the same in both languages): morning 154 dp,
service 154 dp, evening 172 dp.**

| phase | old | new | difference |
|---|---:|---:|---:|
| morning | 179 | 154 | **+25 dp** to the hall |
| service | 152 | 154 | **−2 dp** |
| evening | 199 | 172 | **+27 dp** to the hall |

So the gain is in the morning and the evening; **two dp are lost in service**,
and at that phase the screen says more. This document said "156 dp in all three
phases" and "23–43 dp more room for the hall" for a while; both had fallen
behind the measurement, and the service row **was already a loss back then too**
(152 → 156). Collecting the three winning phases into a single sentence hid the
losing phase inside the average.

### Colour is now three roles

There used to be one accent colour (copper) doing eight separate jobs. The
reference's distinction was adopted:

| colour | meaning | where |
|---|---|---|
| **blue** | "go in here" | Market, Menu, Crew, Equipment, Day report |
| **green** | "advance the game" | Open service, Close the day, Next day |
| **copper** | "attention / value" | interventions left, reputation, day bar |

### Things that were not invented

The reference has a level bar, gems and "Chapter 3"; this game has none of
those. In their place the game's **own** numbers were put, and all of them
already existed — they simply were not visible:

| reference | its counterpart in Lokanta |
|---|---|
| level badge + XP bar | **day number + the progress of the service day** |
| gold + green "+" | cash + the button that **opens the loan screen** |
| gem | reputation (with its ceiling: 30.0 / 55) |
| "Daily Goals" list | morning **opening checklist**, in service **today's flow** |
| "Hourly Income / Customer Satisfaction" | **revenue and average satisfaction** — both of which only appeared in the evening report |
| "Chapter 3 → new customers" | the phase button and its subtitle ("Day 1 begins") |

**The progress of the service day** is the most striking one: this number
existed in the game (`ServiceProgressBp` — the direction of the shadows, the
colour of the light and the street lamps are all read from it) but was never
shown to the player. The answer to "how much is left" lived only in the colour
of the sky.

### The icons are drawn

Not a single icon comes from a font. This is not a preference but a lesson: the
star character does not exist in Rubik and appeared to the player as an **empty
box** (`tools/art/check_font.py` caught it). They are all built from rectangles,
circles and rotation (`Icons.cs`). UI Toolkit's own drawing API (Painter2D) is
not used either — every "it worked in the editor, it did not show up in the
build" story in this project came out of trusting something that reaches the
build indirectly.

### Two things the tour found

1. **The tour could not find "Servisi aç"** ("Open service"). The new buttons
   have an empty `text` field; the words are in a label inside them. The tour
   was clicking by text, and when it could not find it, it **silently carried
   on** — service never opened, and every later check started measuring the
   morning screen. The fix: the tour, and the clipping measurement, now both
   look at the button's **visible** text. *A player looks at the button's
   words, not at its area.*
2. **`Pause stopped the game`** **was red**, and the
   reason was not the game but the lookup: pause is an icon now. Mode buttons
   are found by their names (`ClickNamed`).

---

## 3. The venue: back curtain, pendants, sign, rug

A procedural model builder was added to the view layer (`Modeler.cs`): boxes and
many-sided prisms, gathered into a single mesh **per colour**. The whole
decoration, however many pieces it is made of, is a handful of draws.

| piece | what it does |
|---|---|
| **back curtain** | a solid 2.60 m wall + two side returns. The room walls are 1.15 m and transparent (the inside of the hall has to be visible); the back was empty, and the venue read like a "floor plan" |
| **sign** | above the door, a **neon frame** + emblem |
| **menu board** | on the back wall of the hall, its lines in light strips |
| **extractor hood** | above the row of stoves; stainless in every cuisine |
| **planters** | along the frontage, the area in front of the door left clear |
| **rug** | only in the Turkish cuisine |

### Pendant lamps were tried and reverted

In [docs/38](38-street-and-interior-light.md) the user had said *"lambalar
fiziksel olarak gözükmesin, tavanda olacakları için"* (*"the lamps should not be
physically visible, since they will be on the ceiling"*), and the interior
lighting was done only with pools of light on the floor. In the reference,
however, the pendant lamps are the most prominent element of the venue; in the
first writing of this document that decision was reverted and pendants were
added.

**Then they were removed again** (below, seventh slice) and docs/38's decision
stood: the reference's camera is lower, and there a pendant is half the venue;
ours looks at a ceilingless building from 34 degrees. The pendants were covering
the very place they lit.

So not everything that is in the reference works with our camera — **the angle
comes before the list.** There is not even an empty-bodied `Pendants()` method
left in the code; the reasoning sits at the top of `BuildDecor`.

### Why the sign has no writing

Drawing text in the world needs a separate package (TextMeshPro) and this
project does not have it. The sign speaks with **colour and light** instead. On
the first attempt the whole face of the board was lit and it read on screen as
*"a lit yellow plank"* — a lamp, not a sign. On real signs what lights up is the
lettering and the **frame**; a neon frame does the same job.

### The lamp light is separate from the sign

On the first attempt the mouths of the pendants also lit in the sign's colour,
and the fast food hall filled with a **pink** light. The sign is the colour of
the identity (neon red); the lamp is the same thing in every restaurant: warm
white.

### Second slice: floor, counter, shelf, banquette, railing, clothing

| piece | what it does |
|---|---|
| **floor pattern** | long wooden boards in Turkish, square tiles (checkerboard) in fast food. A single flat colour read as an **empty** area at a 34-degree look; the pattern also gives scale |
| **service counter** | a hot display counter in front of the kitchen: stainless top, trays in a row, glass guard. The reference's most characteristic kitchen piece |
| **wall shelves** | in the kitchen and the store, with pots on them — "a place where work is done" |
| **banquette** | a bench along the back wall of the halls |
| **terrace railing** | between the frontage and the pavement; the area in front of the door is open |
| **clothing** | a white toque + white apron for the cook, a dark apron for the waiter |

**The clothing silently did nothing twice**, and both cases are instructive:

1. The toque was placed relative to the bone with **fixed numbers** (0.105 m up,
   0.17 m wide) and nothing appeared on screen: the toque stayed **inside** the
   head. The reason is the bone chain's own scale (the head bone's `lossyScale`
   is 1.19) and the fact that the head bone sits at the neck, not in the middle
   of the head. The measurement is now read **from the figure**: the top of the
   skin's world box and the height of the figure.
2. The second writing was looking for a `SkinnedMeshRenderer`; the package's
   character is built from **two separate pieces** (`body-mesh`, `head-mesh`).
   When the component could not be found the method returned at the top — no
   clothing, and no error either. Now the boxes of all the renderers are merged.

Both are the same class: *a thing not appearing and it not raising an error can
happen at the same time.* What made the diagnosis was not the log but the
**render**.

### Third slice: the shop-window frame and the terrace seating

**The frontage is now a shop front:** a bottom kerb, mullions and a thin beam.
No glass was added (a transparent material can be drawn opaque in the build,
[docs/37]); what was added is the **frame** — the glass is the gap between.
On the first attempt the beam was at 2.05 m and came out in the image as a
**dark band** passing in front of the hall's front row: it was covering the place
where the player sees the tables. The room walls are already 1.15 m; the
frontage should not rise above that line either. The beam came down to 1.34 m.

**There are two tables on the terrace** and they are **empty** — the simulation
does not serve outside. An empty terrace table still says "this is a
restaurant"; a full one would be lying. The tables were registered in
`StreetObstacles`: the pavement is also where the pedestrians walk, and without
registering them the passers-by would walk through the table. *Putting something
in the scene means making it part of the road.*

### Fourth slice: the furniture carries identity too

The package's furniture materials have **no texture** — they are all a flat
`_BaseColor` (`Furniture_wood`, `Furniture_carpet`, …). So colouring them per
cuisine does not mean fighting with textures: a **copy** of the material is made
and its colour is written from the palette.

**One copy per material.** The alternative was writing a property block onto
every renderer, and that throws the renderers **out** of SRP batching (this
project learned it on the floor slabs): a hundred pieces of furniture would have
meant a hundred separate draws. A single copy paints all the furniture sharing
that material at once.

Result: **red chairs** in fast food, dark burgundy cushions and warm wood on the
Turkish side.

**Metal is not painted.** On the first attempt the palette's metal (brass in
Turkish) went to every metal piece and the kitchen turned **gold**: the sink,
the counter, the fridge. What was learned on the extractor hood holds here too —
equipment is stainless in every restaurant; brass is a decorative colour
(railing, lantern) and stays there.

### Fifth slice: food on the table

In all four frames of the reference there are plates and food on the tables;
our tables were **empty even while being served** — the only place the player
could get the "that table is eating" information from was the badge.

The condition comes **from the core**: only a table whose food has arrived
(`Eating` / `WaitingToPay`) has a plate. Putting a plate on every table would
have been easier and would have been a lie — a waiting table and an eating table
would look the same on screen. The plates are pooled (built once, shown and
hidden) and the food comes from the package's ingredient models, so they enter
batching with their own materials.

**On the first attempt the plates sat 2.4 m above the table.** I was measuring
the tabletop height from all of the carrier's renderers; the carrier also
contains the chairs (0.9 m) and the **badge** (2.45 m). The measurement is now
taken while the table is being built, before the chairs are added — at that
moment there is only the table inside the carrier.

The tour now asks for both at once: *"An eating table has a plate IN THE SAME FRAME"* — "the core is eating" and "the player sees it" are separate
claims (the lesson learned on the dishwashing).

### Sixth slice: readability

As the scene was decorated two things accumulated, and both of them **washed the
image out**:

1. **The milky white of the walls.** With 0.16 alpha, the transparent walls laid
   a haze over the whole hall; the colours beneath them (rug, wood, red chair)
   were fading. The room division is already read from the floor and the
   furniture — the wall's job is to **draw the boundary**, not to paint the
   area. It went down to 0.10.
   *Note: `RestaurantView` had a `WallColor` field that was not read from
   anywhere; the real colour is in the `custom_wall.mat` asset, because a
   transparent material has to be a `.mat` asset ([docs/37]). The dead field was
   deleted — but **its summary had not been deleted**: the orphaned comment
   stood there a while longer, and moreover described an even older value (0.20).
   Leaving the comment that describes a field when you delete the field turns
   the comment from **documentation** into **legend**.*
2. **The floor colour was disconnected from the palette.** The room floors were
   three fixed colours (hall / kitchen / service) and showed through the pattern:
   a warm brown base in fast food, a cold grey base in Turkish. It now comes from
   the palette; the service rooms are still separated (the kitchen and the
   dishwashing colder, the store darker) because the player needs to be able to
   make the "this is the back" distinction.

### Seventh slice: shadows and ceiling lamps

> **"Dükkân içi ışık ve gölgelerde problem var, odalardaki gölgeler başka
> odalara kayıyor. Başka problemler de var, düzelt. Tavandaki ışıklar
> gözükmesin, onların gözükmesine gerek yok."**
>
> *("There is a problem with the light and shadows inside the shop, the shadows
> in the rooms are sliding into other rooms. There are other problems too, fix
> them. The ceiling lights should not be visible, there is no need for them to
> be visible.")*

**The shadow overspill had two causes and both are measurable:**

1. **The transparent walls were casting shadows.** The walls are glass (alpha
   0.10) but solid in the shadow map: a 1.15 m panel leaves a dark band **6.5 m**
   long when the sun is at 10 degrees, and that band covered half of the
   neighbouring room. The error is double — a glass partition does not cast
   a shadow in the first place, and the shadow it cast fell exactly where the
   player needs to look.
2. **The sun's elevation was far too low.** The elevation was 26 degrees in the
   morning and 10 in the evening; shadow length is `h / tan(angle)`, so a 1.8 m
   fridge leaves a **10 metre** shadow in the evening. The elevation is now in
   the 44–66 degree band: the longest shadow is ~1.9 m, the narrowest room is
   3.2 m. **The time of day is not lost** — the direction (azimuth) keeps
   turning from 148 to 268, and that is what actually tells the time; what is
   read is the *direction*, not the length. The evening shadow strength also
   comes down to 0.35: what lights the night is not the directional sun but the
   interior lights.

**The ceiling lamps were removed.** This piece existed once, added on top of the
reference images; the user said the same thing twice. The reference's camera is
lower and there a pendant is half the venue; ours looks at a **ceilingless**
building from 34 degrees — the pendants were turning into objects that covered
the place they lit. The light stays: the pools on the floor and the evening's
warm fill.

**"Other problems" turned out to be two, both in close-up:**

- **The service counter was standing in the same place as the package's
  counters** — the counter boxes came out through the inside of the service
  counter and the trays hung in the air. The front-row counters were removed;
  the service counter is already a better counter (top, trays in a row, glass
  guard) and the cook's work points derive from the stoves.
- **Irregular dark blotches on the floor**: the underside of the pattern slabs
  was on **the same plane** as the top face of the floor slab (both at y=0) —
  z-fighting. From a distance it read like "shadow crumbs" and would have sent
  me hunting through the shadow settings. The pattern was raised 6 mm.

---

## 4. The identity difference so far

| | fast food | Turkish |
|---|---|---|
| wall | charcoal grey | warm brick |
| accent | neon red | copper |
| sign | red neon | amber |
| floor | no rug | kilim (burgundy + copper border) |
| metal | steel | brass |
| floor pattern | square tiles (checkerboard) | long wooden boards |
| service trays | red | amber |
| chair cushion | red | dark burgundy |
| frontage mullion | red | copper |

---

## 5. The camera angle: the reference's three-quarter view and its price

> **"Kamera açısı da referanstaki gibi olsa daha iyi değil mi? Referans
> noktasından çok uzaktayız şu an."**
>
> *("Wouldn't it be better if the camera angle were like the reference too?
> Right now we are very far from the reference point.")*

Correct. In all four frames of the reference the building stands at **three
quarters** — two faces are visible at once. Ours was looking straight on
(`CameraFit.Yaw = 0`) and that had been chosen **deliberately**:
[docs/31](31-rooms-and-camera.md) had measured that a −12 degree rotation drops
the touch-target floor from 71 dp to 48.

So it was **measured** first (`RoomLayout.Capture`, FLOOR = the striped short
edge of the smallest open room):

| rotation | FLOOR 20:9 | FLOOR 16:9 |
|---|---|---|
| 0° | 53 dp | 66 dp |
| 10° | 45 dp | 57 dp |
| 18° | 41 dp | 51 dp |
| 30° | 39 dp | 49 dp |

**And it was also looked at on a phone.** 20 degrees looked marvellous in the
imaging tool (1280×560); in the real build, at 873×393 and with the strips in
place, **the building shrank** — a rotated rectangle wants more room on the
screen, and the fit pulls the camera back. So rotation alone does not bring us
closer to the reference, it takes us *further away*. The tool's frame and the
phone's frame give two different answers to the same question; the decision is
the phone's.

**10 degrees was chosen.** The reference's three-quarter view arrives and the
loss stops at the smallest step. Going below 48 dp is a known price — and what
falls below the floor is not a button but the **short** edge on screen of a
five-metre room; its long edge is more than twice that and the player can zoom
in with two fingers.

**Re-measured on 13 September and the number came out 42–43 dp, not 45**
(20:9; 52–54 at 16:9). The game did not change — the *measurement* was fixed:
the tool was counting the strips as taking 40% of the screen, the real worst
phase is 43.8%. The accepted price stands in the same place, but it now stands
with the right number. `RoomLayout` prints this as a **warning** on every run
(the 48 dp industry floor) and goes **red if it drops below 40 dp**: reporting an
accepted price as an error on every run would be losing a real regression inside
the noise.

The measuring tool itself was fixed too: the strip ratios were written as
`0.13 / 0.17` (30% in total) and were said to be an "upper bound". The tour
**actually** measures the strip and that day it came out 156 dp / 393 dp =
**40%**. So the number was not an upper bound but a lower one — the measurement
was saying the target was larger than it really is.

**This copy silently went stale again.** In the 13 September measurement the
worst phase (evening) is **172 dp**, that is 43.8%; the tool was still computing
against 40% and was again reporting the touch target as larger than it is. The
band was set to 172 dp and `RoomLayout` now compares its copy against the value
the tour measures and goes **red** if it falls below it. Having the same number
in two places was unavoidable (the tool cannot build the interface in editor
mode); what was avoidable was the two of them diverging without anyone ever
seeing it.

### And that "real solution" turned out to be wrong

I had written a sentence here: *"the reference's building is square, ours is a
long strip; if the floor plan gets deeper, both the rotation and the size come
back."* **The measurement refuted it.**

When a frame without the interface was taken (873×393) it became visible that
the building **already fills** the screen: the width is at the limit, there is a
little slack in depth. The 34-degree tilt compresses the depth by
`sin(34°) = 0.56`, so an 18 × 9.6 m plot stands on screen at a ratio of
**3.3:1**; the band between the strips is 3.7:1. If the plot were square the
building would have **shrunk**, not grown.

The real bottleneck was the interface itself: the strips were taking 156 of the
393 dp (40%). The icon buttons came down from 62 to 54 dp, the capsule from 42
to 38 dp; the menu button stayed at the touch floor (48 dp).

*Writing a guess into a document does not make it true — these lines are what
taking one frame before starting to redraw the plan bought.*

---

## 6. What is next

This document covers the first and second slices. What remains in order to get
closer to the reference:

1. **Furniture models** — the package models are staying and now **take the
   cuisine's tone**; rewriting them procedurally (wooden top + dark frame) is
   still open but its payoff has shrunk
2. ~~Characters~~ ✅ cook's toque and apron; customer variety is next
2b. ~~Food on the table~~ ✅
3. ~~Terrace~~ ✅ railing and planters; outdoor seating is next
4. ~~Service counter~~ ✅
5. ~~Floor texture~~ ✅
6. ~~Deepening the floor plan~~ ❌ **the measurement showed it was unnecessary** (§5)
