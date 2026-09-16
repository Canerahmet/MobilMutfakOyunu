# 38 — The layout of the street, the interior lighting, the removal of the doors

*12 September 2026.* The user's four requests and the measurement that came out from under each of them.

> **Sokak ışıkları başta ortada ve sonda olsun. Şu an simetrik değil, aralarındaki
> mesafe o kötü görünüyor. Ayrıca karakterler sokakta yürürken birbirlerinin
> içinden geçiyor. Akşam olunca restoranın içi karanlık oluyor; iç ışıklandırma
> olsun fakat lambalar fiziksel olarak gözükmesin, tavanda olacakları için.
> Giriş ve mutfak kapısı dışındaki kapıları kaldıralım.**
>
> *("Put the street lights at the start, the middle and the end. Right now it is
> not symmetrical, and the distance between them is what looks bad. Also, the
> characters walk through each other on the street. When evening comes the inside
> of the restaurant goes dark; let there be interior lighting, but the lamps must
> not be physically visible, since they will be on the ceiling. Let us remove the
> doors other than the entrance and the kitchen door.")*

---

## 1. Street lamps: equal spacing is a *measurement*, not an impression

The old code divided the plot equally between four posts, and because one of them
fell in front of the door it was **shifted by 2.2 m**. The gaps came out as
2.3 / 4.5 / 4.5 m. The shift was a single line in the code and it looked
innocent; what it broke could only be seen on screen.

Three posts, equally spaced. But the first fix was wrong too, and **the render
showed it**:

| attempt | posts | what is visible at the opening tier |
|---|---|---|
| old | 4.45 / 6.75 / 11.25 / 15.75 | four posts, gaps 2.3 / 4.5 / 4.5 |
| fix 1 | 0 / 9 / 18 (relative to the plot) | **two** posts, leaning to the left |
| **fix 2** | **0 / *open width*/2 / *open width*** | three posts, symmetrical |

The camera frames **not the plot but the open rooms** (`CameraFit.OpenBounds`).
At the opening the player sees between 0 and 13.4 m, so the post at 18 m is not
on screen. A layout that was symmetrical on paper was asymmetrical on screen —
exactly the complaint that was to be fixed.

Its measurable form is `RestaurantView.LampSpacingError`: the largest of the
distances between neighbouring posts minus the smallest. In a symmetrical row,
**0.00**. The tour asks this on every run, because this is precisely the kind of
thing that can be broken again by "an innocent one-line shift".

### 1b. Not a post, a lantern *(added on the evening of 12 September)*

The user brought a reference image — a classic street lantern with an octagonal
base, tapering as it rises, with a curving arm — and said:

> **"ışık konusunda geliştirme yapmamız lazım, sokak için şu tarz bir model daha
> iyi olabilir"**
>
> *("we need to improve the lighting; a model in this style might be better for
> the street")*

The old lamp was **three boxes**: a 0.09 m post, an arm, and a 0.26 m white cube
on the end of it. At phone scale this read not as "a lamp" but as "a white dot on
the end of a thin stick".

The new lamp, with the reference's parts and in **a single mesh**:

| part | measurement |
|---|---|
| base plate + conical foot | r 0.185 -> 0.088, y 0 – 0.36 |
| body (the post) | r 0.052 -> 0.042, y 0.41 – 1.62 |
| two collars | y 0.95 and 1.585 |
| lantern | **on top of the post**: seat + skirt + glass + collar + conical cap + finial, y 1.60 – 2.26 |

Fifteen separate boxes would have meant fifteen draw calls; all of it merges into
**two meshes** (metal + glass) and the three lamps share the same two meshes —
**2 draws** per lamp.

In the first version the lantern was **on the end of a curving arm** (the middle
model in the reference) and it hung out over the pavement. The user wanted it on
top — the rightmost model in the reference — and the game's camera proved them
right: the arm reached towards +Z, that is, **towards the camera**, and at the
fixed angle it foreshortened away completely. An invisible arm made the lantern
look "stuck through the post"; a lantern on top is a lantern from every angle.

The light moved with the lamp as well (`LampHeadZ = 0`). The pool is 2.9 m deep
and the post is on the kerb, so the pavement's walking lane
(`Paths.PavementZ ± 0.35`) stays **inside** the pool — the light goes on lighting
the place people walk.

**The scale was chosen on screen, not on paper.** At the first measurement the
model was correct, but in the game's real frame the lamp is 40 pixels and the
lantern a fifth of that — an eight-pixel smudge. The lantern radii were scaled up
by x1.25; the decision was made *by looking at the render*.

**The bottom of the lantern is measured, not hard-coded.** The lantern is
directly above the pavement and 1.10 m figures pass underneath it. Somebody
scaling the model up could bring the lantern down to walking height and nothing
would warn them — the figure would pass through the lamp and the game would go on
working. `LampLanternBottom` reads the number **from the mesh itself** and the
tour asks it on every run. The measure's definition changed once too: at first it
was picking out "the vertices near the lantern on the z axis", and while the
lantern was **on the end of the arm** that was a correct distinction; once the
lantern went up onto the post, the same condition selected the whole lamp —
including the base — and the measure returned 0.00 m. In its place, the
**silhouette**: the post radius is 0.052, and anything wider than that and above
waist height is the lantern body. *The definition of a measure has to change with
the geometry of the thing it measures.*

### 1c. Night light: three layers

In URP the additional lights are off (`m_AdditionalLightsRenderingMode: 0`) and
**HDR is off too** (`m_SupportsHDR: 0`) — so there is neither a point light nor a
bloom. Emission above 1 is simply clipped to white. Night light is therefore
something that is **drawn**:

| layer | what it does |
|---|---|
| **the beam** | the cone dropping from the lantern to the ground. It is only through this that the light is seen *leaving its source* |
| **the halo** | the glow around the lantern — it stands in for bloom |
| **the pool** | the ellipse lying on the pavement; long along the street, because a circle read as "a circle lying on the ground" |

All three use the existing `custom_lightpool` material: **the transparent shader
variant only enters the build if a `.mat` asset uses it** (docs/37) — writing a
new material would have been walking into that trap again. The beam reads the
middle row of the texture: bright at the lantern (u=0.62), dim at the ground
(u=0.98).

Two bugs, and both came out by looking at the picture:

1. **The halo was not visible at all.** Unity's Quad faces **−Z**; handing it the
   camera's angle directly flips the panel over and, because the back face is
   culled, the panel is never drawn. The halo was there, with its back turned.
2. **The beam was not visible.** Same material and same strength as the pool: a
   lying panel is wide at a glance, an upright cone is razor thin. The beam's
   strength is written separately.

The glass of the unlit lamp changed too: when off it was dark grey (0.24), so by
day the glass and the iron were the same colour and the lantern read as "a post
with a thickened tip". Real lantern glass is white by day as well.

---

## 2. The pedestrians were walking through each other — two layers, four measurements

### The solution is in two layers

1. **The lane.** If two figures walking in opposite directions are on the same
   line, a collision is inevitable. Two separate lanes (`Paths.PavementLane`)
   remove it **structurally**: the one going right walks on the kerb side, the
   one going left on the building side.
2. **The shove.** For the ones catching up with each other in the same lane and
   for the ones stopping to chat, a small correction at the **end** of every frame
   (`LateUpdate`) that separates the bodies. Not pathfinding — it only stops two
   bodies overlapping.

The two have to be separate: with only the shove, two figures coming towards each
other would brake their way past (the pavement jams up); with only the lane, a
fast figure going the same way would pass through a slow one.

The shove is **at least 62% lateral**. The reason: for somebody catching up from
behind in the same lane, the line separating the two bodies is almost entirely
along the walking axis; pushing along that direction speeds the one in front up
and slows the one behind down — the two never come alongside each other, so
nobody can **overtake**. Tilt it sideways and the one behind slips past.

### Where the lane spacing comes from: three times the wrong thing was measured

The lane spacing wants a **distance**, and choosing that distance by eye would
have been making the very mistake that was being fixed. The measurement measured
the wrong thing three times:

| measurement | result | why it is wrong |
|---|---|---|
| bounding box | 0.60 x **1.14 m** | a footprint that is impossible for a one-metre figure — it is the **arm span** |
| hip band (18%–50%) | **1.08 m** | at those proportions the **hands** are at that height too |
| shoulder band | 0.58 m | correct but insufficient: the **head** is wider than the shoulders |
| **the whole profile** | **0.67 m** (the head) | the number in use |

Both of them were a band picked with a "reasonable" justification, and both of
them were measuring an arm. The only way to find the right band was **to print
the whole profile**:

```
PROFILE y 0.00-0.10  widest diameter 0.44 m   feet
PROFILE y 0.10-0.20  widest diameter 0.95 m   hands
PROFILE y 0.20-0.31  widest diameter 1.08 m   hands (widest)
PROFILE y 0.31-0.41  widest diameter 0.98 m
PROFILE y 0.41-0.51  widest diameter 0.76 m
PROFILE y 0.51-0.61  widest diameter 0.58 m   shoulders
PROFILE y 0.61-0.72  widest diameter 0.67 m   HEAD  <- in use
PROFILE y 0.72-0.82  widest diameter 0.65 m
PROFILE y 0.82-0.92  widest diameter 0.54 m
PROFILE y 0.92-1.02  widest diameter 0.50 m
```

At this pack's proportions the head is a third of the body, so **the head is
wider than the shoulders**. `PlacementAudit.Profile` is permanent now: next time
the figure scale or the pack changes, these lines are where to look.

**The arms are deliberately left out.** Two lanes 1.08 m apart would make the
pavement half as deep as the restaurant. Two bodies passing separately is enough;
one hand passing through another hand is a few pixels at this camera distance.

### Its price was measured: the street in frame goes from 1.10 to 1.94 m

The pavement had to be 1.40 m (0.70 lane spacing + half of each body). On the
first attempt the pavement was widened but **the frame was not**: the kerb and the
asphalt fell entirely outside the frame and the street turned into a strip of
pavement with no road. `CameraFit.StreetInFrame` was widened too and the touch
target was measured again — the detail is in [docs/31](31-rooms-and-camera.md),
**59 dp** (the minimum is 48).

The same measurement showed that the **71 dp in `docs/31` was stale**: that number
had been taken when there was no street at all, and the street itself had already
brought it down to ~64. Nobody had measured it again.

### The bug the tour caught on its first run

The new check came back red on its first run: **"at worst 4 pairs"**.

The cause was neither the lane nor the shove. When the time multiplier goes past
`TeleportAbove` (4.5), `Walker` stops drawing the walk and **teleports** to the
end of the path. On the street the end of the path is only two points — the exit
for those going right and the exit for those going left — so at high speed five
pedestrians piled up on those two points. Three plus two, exactly **four
overlapping pairs**.

The solution: the street does not follow game time. `Walker.SpeedCap` was added
and set to 4 for the street (just under the teleport threshold). The reason is not
mechanical but fictional: **the people passing on the street have no counterpart
in the simulation**, and "I fast-forwarded" should not mean "the pavement
emptied".

Pedestrians also no longer bump into the lamp posts (`PostClear`, with a separate
and smaller threshold for the post — the outer lane is 0.45 m from the post, and
if the threshold were as large as `Personal` every pedestrian in the outer lane
would be pushed inwards constantly).

### The next three reds

Once the pile-up was fixed, the check became **unstable**: 0 in one run, "worst
case 1 pair" in another. Three separate causes came out.

**1. The shove had a fixed speed.** A *customer* crossing the pavement diagonally
pushed a pedestrian, and that pedestrian walked into another — a chain. A
fixed-speed correction arrives a frame behind at the end of the chain. The fix was
to make it **proportional**: the more two bodies have interpenetrated, the further
apart they are pushed (when they are just grazing, the correction is near zero, so
there is no jitter), and the speed is only a ceiling. Plus two relaxation passes
per frame.

**2. The measurement was measuring a frame that was never drawn.** In Unity
`yield return null` wakes a coroutine **between Update and LateUpdate**. The shove
runs in `LateUpdate`, so the tour was reading the positions *after Walker had moved
them but before the shove corrected them*. There was no interpenetration in the
frame the player saw; the frame that was measured never reached the screen.

The number is now written and stored at the end of `LateUpdate`, after the
correction has finished — whoever asks gets the number for **the frame that was
drawn**.

This is the familiar shape of this project's measurement traps: the metric was
right, *when* it was read was wrong.

**3. The order of the corrections was wrong.** Once the moment of measurement was
fixed the red *continued* — so this time there really was interpenetration in the
drawn frame. The cause: for each person, mutual separation was done first and
obstacle pushing second. A person's **last operation** was "move away from the
customer / the post", and that push could put them back inside a neighbour they
had already been separated from; nobody looked again.

The right order: one-sided constraints first (obstacles, the lane), mutual
separation **last** — and that step repeats until a pass corrects no pair at all.
Whatever has the last word in the drawn frame became the thing the check measures.

The common lesson of the three: *"there is a correction"* and *"the corrected
state is what gets drawn"* are two separate claims. The second one has to be
measured.

### Removing the doors broke the tour — the measurement itself

When the interior doors went (leaves 8 -> 2) the tour's **liveliness window**
started running the full forty seconds on every run: the condition `opened doors >
0` can now only be satisfied by the entrance and the kitchen door, whereas before
one of eight doors was always near somebody. Because the window could not exit
early, the **separate 25-second table wait** that followed it was already past the
busy part of the service.

Once the diagnostic line was added the cause was visible at a glance:

```
DIAG liveliness window: 40.0 s, service 65% -> 100%
```

The window was starting at **65% into the day** and pushing up against **100%** —
so every check after it was measuring an empty hall. Two separate causes:

1. **The x240 speed was not being turned off.** That speed was only for the "let
   the hall fill" wait, but the camera section under it waits
   `WaitForSeconds(MoveSeconds)` twice, and at x240 one real second is four
   service minutes. The camera checks have nothing to do with speed — they are all
   interface and camera. From there on the speed is now x4.
2. **Choosing a table was a separate window.** It is now in the same window, chosen
   while the hall is *full*.

And a second ceiling was put on the window: the duration (40 s) **and** the day
(80%). The duration ceiling alone was not enough, because the ratio of those forty
seconds to the day varies from run to run. If it hits the day ceiling the
diagnostic writes `(DAY CEILING)`.

On the next tour the diagnostic pointed at a **third** source: the same tour was
entering the window at **15%, 18% and 50%** into the day across three runs. The
difference came from the "let the hall fill" wait before the window — that runs at
x240 too, and twelve seconds of it can amount to half the service day. It got a
day ceiling as well (35%).

The fourth was narrower: the chosen table was **latched once** in the window, and
if the window ran a few seconds longer that party could get up — the window came
out green while the table was empty at the point of use. It is now refreshed every
frame and re-read at the moment of use. *The thing that is held and the thing that
is used have to be valid at the same moment* — a lesson this tour had already
learned twice.

What is worth recording is this: a change **the user asked for** broke the
**measurement infrastructure**, and the red pointed not at the broken place but at
somewhere else entirely ("Work is being done in the kitchen"). Without the
diagnostic line, the kitchen code is where the search would have started.

---

## 3. Interior lighting: the lamps are not visible, their light is

The user's reasoning is right: the camera looks down on a building that **has no
ceiling**. A ceiling fitting, if drawn, would cover the very place it lights — and
there is no ceiling there anyway, so the fitting would hang in mid-air.

**Only the light pool.** The same technique as the street lamps (an unlit panel,
additively blended), because additional lights are **off** in the URP asset
(`m_AdditionalLightsRenderingMode: 0`, the mobile budget) and a spot placed in the
scene does nothing — without a warning.

A grid sized to the room (2.90 m spacing target, the pools overlapping each other
by 55%). A single large pool gives a rectangular room a smudge that is bright in
the middle and dark at the edges — that is not "ceiling lighting", it is "a
lantern on the floor".

The colour is **different** from the street's: the street lamp is sodium yellow,
the interior is warm white. If the two were the same colour, "inside" and
"outside" would read as a continuation of the same place.

### Bounce: it exists in reality, not here

What the user asked in the second round:

> *"içerideki ışıklar yeterli değil, biraz daha şiddetini artırmak mı lazım.
> Normalde ışık yansıyarak diğer kısımları da aydınlatmaz mı gerçekte"*
>
> *("the lights inside are not enough, do we need to raise the intensity a bit.
> Normally, does light not bounce and light up the other parts as well, in
> reality")*

They are right, and the answer is technical: there is **no bounce at all** in this
scene. Global illumination is off and a lightmap **cannot even be baked** — the
whole restaurant is built at runtime (`RestaurantView` generates the geometry from
the table count), so there is no fixed scene to bake. Two things can stand in for
bounce: **ambient light** (uniform) and **a shadowless fill from the opposite
direction** (it lifts the shadowed faces — and that is largely what bounce visibly
does).

Both of them were set the wrong way for night. The fill light was **blue** at night
(0.42 / 0.46 / 0.70) at an intensity of 0.22: every surface the warm key did not
reach was being filled with a *cold* light — the exact opposite of what bounce does
inside a warm dining room.

### The measurement: the inside was darker than the outside

I turned what the eye called "not enough" into a number (`scratchpad/brightness.py`,
the render's luma distribution region by region):

| region | before | after | by day |
|---|---|---|---|
| hall **median** | 46.3 | **66.8** | 87.4 |
| hall mean | 65.6 | 81.5 | 88.1 |
| kitchen median | 55.9 | 71.9 | 81.7 |
| **street mean** | **51.1** | **32.3** | 143.6 |

The first column tells the problem in a single line: **the hall's median pixel is
46, the street's mean is 51** — the inside of the restaurant was darker than the
pavement in front of it. Somebody looking at the mean could not have seen this
(65.6 vs 51.1); the gap between the median and the mean also tells you the
distribution: bright under the pools, dark between them.

### Why raising the intensity alone is not enough

Two reasons, both measured:

1. **sRGB compression.** Raising the light by 20% raises the brightness by about
   8% (`20%^(1/2.2)`). The first attempt measured exactly that: +20% light -> +7%
   brightness. Taking the median from 50 to 75 wants **2.5 times** the light, and
   that would blow out the area under the pools.
2. **Every lever I have is global.** Ambient, fill, the warm key — all of them
   light the pavement as much as the restaurant, because additional (local) lights
   and light layers are off in URP. Raising the intensity did not change the
   *difference* at all.

The solution was twofold: **grow the uniform component** (ambient 0.21 -> 0.66,
the fill warm and 0.22 -> 0.75) and **darken the outside locally** — at night the
street panels drop to 28% of their own materials (`RestaurantView.TintStreet`).
In reality that is right too: at night a pavement is exactly as bright as the
light falling on it. By day nothing changes (the tint is 1.00).

The tour asks the two **together** — a check that only looked at the interior light
would stay green while the street brightened by the same amount.

### The real cause was not the pools

The pools only light the **floor**: the tables, the chairs and the figures get
nothing from them. That was the real cause of the "the inside of the restaurant is
dark" complaint — the floor was being lit while everything standing on it stayed in
the dark. The warm fill light (`DayLight.Warm`) was strengthened: 1.55 -> 2.25 ->
**3.20**, its direction almost from overhead. (The third number came after the
second measurement; the document sat at 2.25 for a while and trailed the code.)

### The interior lights come on **before** the street lamps

`RoomThreshold` is 0.62, `LampThreshold` 0.76. If the two were on the same switch
the hall would stay dark in the early evening; the window the user was seeing was
exactly that gap. A restaurant turns its own lights on before the street lamps
anyway.

---

## 4. A hinged door on only two of them

The user's decision is right and there is a reason for it: in a real restaurant
there is no door between one dining room and another, there is an open passage. **A
leaf only marks a threshold** — street to hall, hall to kitchen. On top of that,
eight transparent leaves constantly opening and closing under a 34-degree view had
turned the movement itself into noise: on every frame the eye goes to the most
agitated place in the scene, and that place is not the game's information.

**A gap at every passage, a leaf on only two.** `Connect()` goes on saying which
room opens onto which and that rule was not broken (the store only from the
kitchen). The only thing that changed is whether a leaf is put in that gap.

This change would have made the "door count ≥ 5" check red, and deleting the check
would have been the easy road. But what would have been deleted was the thing that
really needs measuring: **that a room is not sealed off**. It became two separate
numbers:

| check | what it measures |
|---|---|
| `DoorCount == 2` | exactly two leaves: the entrance + the kitchen |
| `GapCount ≥ LinkCount + 1` | a passage for every adjacency, plus the main door |

---

## New measurements (the tour)

```
ok   : Only the entrance and the kitchen have a swinging door (2)
ok   : There is a gap for every neighbouring pair (6 gaps / 5 neighbouring pairs)
ok   : The pedestrians do not pass through each other (at worst 0 pairs)
ok   : The pedestrians do not walk into a lamp post (at worst 0 people, 3 posts known)
ok   : The street lamps are evenly spaced (3 posts, deviation 0.00 m)
ok   : Inside ceiling lights were built (16)
ok   : The inside lights come on only as the day goes on
ok   : The evening inside fill is lit (2.76)
ok   : The inside lights come on before the street lamps (0.62 < 0.76)
```

`PlacementAudit`: 0 overlapping pairs, 15/15 objects facing the right way, body
band 0.67 m — the pedestrians' minimum distance is 0.68, the lane spacing 0.70,
**the distance is enough**.

One more hole was closed in the post check: `PostOverlaps` was returning **0** even
when no posts had been loaded at all, so "the shove works" and "there is nothing to
measure" were both green. In that case it now returns 1, and the tour also asks how
many posts are known.
