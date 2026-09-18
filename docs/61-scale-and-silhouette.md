# 61 — One scale, and two kitchens that are not the same shape

18 September 2026.

> *"1 ve 2 yi yap. Bence karakterler aynı kalsın mobilyaları ölçeklendir."*
>
> *("Do 1 and 2. I think the characters should stay as they are - scale the
> furniture.")*

The two items were the ones [60](60-palette-and-light.md) ended on: the two
kitchens held the same shapes, and the figures and the furniture were drawn to
different scales.

---

## 1. The scale was half-fixed, and the unfinished half was the kitchen

The furniture was never uniformly at "real scale". `Editor/ArtPrefabs` carries
the record of the hall being brought down years-of-commits ago:

> *Measured: at a 0.74 m top, the head of a seated figure was only 0.37 m above
> the table (on a real person it is 0.51). The furniture is at real scale and
> the characters at half of it - and that inconsistency shows up most at the
> table, because that is where the player is looking.*

The table went to 0.58 and the chairs to 0.68. **Everything through the kitchen
door kept its real-world height**: a 0.92 m counter, a 1.80 m fridge, a 0.86 m
worktop on every station built in code. So one building contained furniture
drawn to two different scales, and the visible results were a cook standing at a
counter level with their shoulders and a dishwasher holding a plate at chest
height because the basin was above their reach.

**The ratio was not invented, it was read off the hall.** This file already
states the method, in the note beside the door:

> *2.10 m is a real door size, but the figures in this package are 1.10 m; next
> to them the door turned into a triumphal arch. At the real ratio
> (door/person = 1.17) the equivalent is 1.29 m; 1.45 leaves a little margin
> and still reads as a "door".*

Real size, divided by a real person, times ours, plus a margin so the object
still reads as itself. The hall lands at about **0.75 of real** (0.74 → 0.58,
0.88 → 0.68). Every kitchen number is now its real size times 0.75, which puts
the two rooms on one scale for the first time.

A 0.69 m worktop against a 1.00 m figure is 69% of its height. That is still
taller than life - a real counter is 54% of a real person - and it is **exactly
the hall's own exaggeration**, which was measured and kept on purpose: at this
camera, furniture drawn dead to scale reads as too low.

### One line, not forty numbers

The stations built in code are scaled on the ROOT transform rather than by
editing every dimension inside them. That is not laziness, it is the only
version that cannot drift:

- `Width` is MEASURED off the built bounds **after** the scale is applied, so
  the wall packing adapts on its own;
- `PotSpots` are children, so the pans follow;
- the plinth under a bench appliance follows;
- and a model added next year inherits it without being told.

The same constant, `KitchenStation.FurnitureScale`, is what the hood, the
splashback, the wall shelves and the code-built service counter now ride. Each
of those had its own written number before, and a number written in two places
disagrees with itself - which this project has paid for five times.

The service counter and the tray station are scaled **vertically only**. Their
length comes from the room and the queue stands at their depth; what was wrong
was their height against a figure.

### The link nobody would have looked for

The wash room's four units were spread evenly between the doorway and the far
wall. That was right for units 0.84 m wide and wrong the moment they were not:
at 0.63 the row opened into four islands with a third of a metre of daylight
between each. **A run of units is a run - they touch.**

The pitch comes off the measured prefab now, with 0.10 m of air, and it closes
the gaps rather than running through the wall if the room is short. A number in
the view that claims to be a furniture width is a copy of one that lives in
`ArtPrefabs`, and copies drift.

## 2. The two kitchens differ above the line now

They already differ in WHICH stations they own - a domed stone oven and a
turning spit against a shake machine and a waffle press. But those stand down
the SIDE walls, because the back wall is the hot line and the shared stations
land there in content order. So the wall the camera looks straight at was a row
of stainless boxes under a stainless hood in both, and docs/60's splashback and
kick strip changed its COLOUR without changing its outline.

`OverTheLine` is the outline. It sits in the band between the worktop and the
hood - the strip of wall a 34 degree camera sees most of - and it is a
different OBJECT in each cuisine, not the same object painted twice:

| | what it is | why that cuisine |
|---|---|---|
| fast food | a **heat-lamp pass**: a steel shelf on two brackets with a row of amber lamps along its front | every burger bar has one, where the food waits to be carried out. It is the only warm light in that room and a lokanta has none |
| Turkish | a **hanging pot rail**: copper pans of three sizes on a rail on two drops | the hook rail of a tradesman's kitchen, and its silhouette is ROUND against the line's rectangles |

Three sizes in rotation, because a rail of identical pans reads as a fence.

**The lamps went under the shelf first, where the camera cannot see them.** A
heat lamp does hang underneath - and this camera looks DOWN at 34 degrees. The
shelf appeared and the light did not: the thing was built, it was the right
colour, and it was facing away from the only viewpoint there is. They are on
the shelf's front face now, which is the face the room sees.

They are amber (1.00/0.62/0.26) rather than the sign's coral. A heat lamp is an
infrared bulb; and after docs/60 softened the accent, coral does not read as
heat at midday. It is also a hue this room does not otherwise contain, which is
half of what the object is there to do.

## 3. What it cost

Nothing that matters: the scale change moves geometry that already existed, and
the two new objects are about 300 triangles between them. The ten-table
overview sits at 306 renderers and 61,110 triangles against ceilings of 360 and
70,000.

## 3b. What the tour caught, and it was not the kitchen

The run after all this went red once and passed on the re-run - the shape of
failure this project distrusts most, because the evidence is gone by the time
anybody looks. Two separate things were going on.

**The "crash" was the filter, not the game.** A `-Runs 3` tour reported "the
tour did not run to the end (no summary file)" on its second run. PowerShell's
`Select-Object -First` terminates the upstream pipeline once it has its count,
and that killed `tour.ps1` halfway. Nothing was wrong with the build.

**The real failure was the lamp post, and the arithmetic was mine.** Run four
times, it appears about one time in three:

    FAIL : The pedestrians do not walk into a lamp post (at worst 1 people)

docs/59 §8 moved the post out to clear the new walking lanes and computed the
limit: the outer lane is at -1.67, `StreetLife.PostClear` is 0.48, so the post
cannot come closer than -2.15. **It was put at -2.16.** One centimetre of
margin is not margin: a pedestrian on that lane stands exactly on the push
boundary, so the speed variation or the mutual push when two of them meet puts
them inside for a frame - and the measurement samples the frame that was DRAWN.

-2.26 leaves 0.11, and the kerb widens to -2.46 so the base plate still sits on
stone. Four runs clean afterwards.

**A limit computed to the centimetre and then met to the centimetre is a limit
that will be crossed.** That is the second time in this session: the touch
target ratchet was set from one tier's reading and failed on a tier it had
never looked at ([31](31-rooms-and-camera.md)).

### And the evidence will exist next time

`Player.log` is overwritten by the next run and `check.py` prints only the last
line of the tour script, so an intermittent failure used to leave nothing
behind. `tour.ps1` copies the log of any run that crashes or leaves a check
failing. A flaky failure with no evidence is the worst kind, because the only
way to study it is to reproduce it and reproducing it is the part that does not
work.

### One more the scale opened, found by reading rather than by failing

`Intruding` gained the kitchen stations in docs/59 and compares a figure's
position against `Width` and `Depth`, which are MEASURED off world bounds.
`InverseTransformPoint` divides by the station's `localScale` - and the
stations carry one now. So the box was 1/0.75 too small and the guard had
quietly stopped seeing a third of what it was built for, without ever going
red. It un-rotates by hand now, which keeps both sides in world units whatever
the scale becomes.

**Adding a scale breaks every measurement that does not account for it, and
the breakage looks green.**

## 4. Still open

- **The figures are still chibi against realistic proportions.** Scaling the
  furniture fixes the RELATIONSHIP - a counter at chest height, a chair a
  figure can sit on - but the two styles are still two styles. That was the
  user's call and it is the right one: the characters are the game's face.
- The room badges read as floating progress bars in a wide shot. They are
  signal rather than decoration and their legibility is measured in
  [41](41-ui-and-venue.md), so they are not a palette question.
