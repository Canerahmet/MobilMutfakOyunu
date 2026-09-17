# 58 — A visual review: the icon was a blank square and the sky outshone the hall

*17 September 2026.* The user's request:

> *"Oyunu görsel açıdan daha iyi bir hale getirmek istiyorum. Bunun için
> agentlar ile oyunu bir eleştirmen ve arayüz tasarımcısı gözüyle değerlendir.
> Nereleri geliştirebiliriz?"*
>
> *("I want to make the game better visually. Assess it with agents, through
> the eyes of a critic and a UI designer. Where can we improve?")*

Four reviewers ran: an art director on the 3D scene, a product designer on the
interface as a design system, a mobile producer on the phone frame and the store
listing, and a genre critic against shipped management games. All four read the
rendered screenshots **as images** and then the code behind what they saw. None
of them changed a file.

Every claim below was re-verified here before anything was done about it. Two of
the four reports contained a premise that turned out to be wrong, and both are
noted where they arise.

---

## 0. The finding that outranks the rest: the app icon was a blank grey square

```
store-icon-512.png   512 x 512    262,144 pixels, ALL (205,205,205,255)
app-icon.png        1024 x 1024  1,048,576 pixels, ALL (205,205,205,205)
```

One distinct colour in each file. `app-icon.png` is wired into
`ProjectSettings.asset` as the **Android adaptive-icon foreground at all six
densities**, so the shipped launcher icon was a translucent grey square and the
store icon an opaque one. Every store impression passes through that asset.

[21](21-business-and-release.md)'s release audit listed it under **Ready**, with
the evidence *"`Art/Icons/store-icon-512.png`, 512×512 **RGBA**, 5 KB"*. All
three of those facts are true. **5 KB for a 512×512 PNG was itself the tell** —
that is what a blank square compresses to.

### The generator was never the problem

`IconShot.Run` builds a table, four chairs, a seated guest and a plate, lights
them with a key and a fill, and renders. The code is careful and its comments
argue every decision, including a correction about Play wanting a 32-bit PNG
*and* no transparency.

It was run through `tools/unity/run.ps1`, which passes **`-nographics`**
(`run.ps1:38`). That flag turns rendering off. `tools/unity/shot.ps1` exists for
exactly this reason and says so in its own header:

> *"How it differs from run.ps1: there is NO -nographics flag. That flag turns
> [rendering off]."*

The icon was run through the wrong one, and `Flatten` then did the rest: it takes
the ground colour from the image's own top-left pixel, so a uniform buffer
flattens to a uniform square. Re-run through `shot.ps1`, the icon is **9,776
colours** with the table set inside the adaptive mask's safe area.

### The check that should have existed

`tools/art/check_icons.py` opens the file. It fails on fewer than 64 distinct
colours, on one colour covering more than 92%, on a flat middle 66% (the subject
missing the adaptive mask's safe area, which ships as a blank circle on the
player's home screen), and on any transparency in the store copy.

The bar is deliberately low. This check's job is to catch **nothing**, not to
have opinions about art. Dimensions, colour mode and byte count are all proxies
for "there is a picture here" and all three passed — which is
[CLAUDE.md](../CLAUDE.md) rule 4 failing on the one asset every store impression
passes through. Seventeen checks now.

---

## 1. Nothing inside the restaurant received a shadow

Not "few". None.

`Modeler.Build` set `receiveShadows = false` on every mesh it made, and
`FloorPattern` — the planks and tiles laid 6 mm above the room slab, i.e. **the
visible top surface of every floor in the building** — goes through it. The
characters cast (`ArtPrefabs.cs` turns casting on for people and off for props,
with a defensible argument about chair shadows at 34°) and there was nothing
inside to catch it.

The pavement outside is a plain slab and does receive. That is exactly what the
pixels show: the pedestrians and the lamp posts are planted on the ground, and
six metres further in the dining tables float.

Two more settings were working against it:

- **The shadow distance was measured at the wrong tier.** 45 m came from "the
  furthest corner is 39.4 m, plus a margin" — and that corner is the *opening*
  restaurant. The plot grows; by tier 4 the back wall is at ~46 m, past the
  distance entirely. Worse, URP fades the last cascade over `m_CascadeBorder`
  (0.2), so the fade begins at 0.8 × 45 = **36 m** and the dining room was inside
  it at every tier. Now 70 m, which costs 2.2 → 3.4 cm per texel on the 2048 map;
  the note in `ProjectSetup` records that 9 cm was not enough, and 3.4 is a long
  way from that.
- **Soft shadows were being refused.** `BuildGameScene` asks the sun for
  `LightShadows.Soft`; `m_SoftShadowsSupported: 0` made every shadow in the game
  hard-edged whatever the light said. A request that is silently denied is worse
  than one never made — the code reads as though the decision had been taken. It
  is now managed in `ProjectSetup`, so `check_urp` watches it: **15 fields**.

**Not measured on a device.** Soft shadows are a per-pixel cost on a phone and
there is no test phone. Flagged rather than assumed.

---

## 2. In daylight the subject was the darkest thing in the frame

The measurement, and it is the one that explains why the night frame looks like
a game and the day frames look like a plan. Median luminance of everything that
is not a flat field, over the background's luminance:

| frame | subject | background | ratio |
|---|---:|---:|---:|
| `10-evening` | 51 | 34 | **1.5×** — subject brighter |
| `07-service-busy`, before | 62 | 132 | **0.47×** — ground twice as bright |

A figure/ground inversion. The restaurant — the subject, the thing the player is
asked to watch — was the darkest region of the frame, wrapped on three sides by a
field twice its brightness. The eye goes to the bright field and slides off the
building.

Two causes, and both had a reason behind them at the time:

- the midday sky stop was `(0.38, 0.51, 0.65)`, a *light* blue, brighter than
  anything in a room lit only by ambient;
- `RoomColor` gave the dining room `FloorDark`, **the darkest tone in the
  palette** — so the one room the player is asked to watch reflected less light
  than the store room.

Both are fixed, and the back of house still reads as the back of house: the
kitchen and wash-room lerps still start from `FloorDark`, so they stay colder and
darker than the hall. What changes is that the hall is now the brightest floor
rather than the dimmest.

**This took two passes, and that is the point.** The first attempt was reasoned
rather than measured: it moved the ratio from 0.47 to 0.71 and the ground was
still brighter. The second took it to **0.84**.

**And then the metric ran out of usefulness, which is worth saying plainly.**
"The median luminance of everything that is not a flat field" counts the sunlit
pavement, which *should* be bright, along with the interior, which should not.
It was the right instrument for catching a 0.47 and the wrong one for judging a
0.84. Looking at the frame settles it: the restaurant now reads as the subject,
because value is not the only channel - a warm, saturated, detailed object
against a desaturated neutral field separates on chroma and detail as well.

Tuning further against this number would have been tuning against the
instrument. What the frame still wants is not a darker ground but a ground that
is *something* - see the backdrop row in section 10.

---

## 3. Three numbers in the interface that could not be read

**The reputation number was illegible at every value.** `Kit.Pill` sets the ink to
`Theme.PlateInk`; `GameScreen` overwrote it with `ReputationColor`, whose four
returns were all chosen for the **dark panel**. Measured on the cream plate:

| state | contrast on the plate |
|---|---:|
| `InkDim` (the starting band) | 1.84 : 1 |
| `Good` | 2.00 : 1 |
| `Warn` | 1.52 : 1 |
| `Bad` | 2.22 : 1 |

The WCAG floor for text is 4.5. There was no value of reputation at which that
number could be read — and faint grey on cream reads as *disabled*, which is
worse than merely faint on one of the two numbers the top strip exists to show.

`Kit.CountRow` had already learnt this and its comment states the rule: *"as long
as the colour choice is left to the caller this class of bug reopens; the
decision now lives here."* `Kit.Pill` still let the caller decide, and this is
the caller it reopened in. `Theme.ReputationPlateColor` keeps the same bands —
30.0 is where a campaign starts, so the starting band has to stay quiet or the
first thing the player learns is a false alarm — in inks that hold on cream.

**Satisfaction showed a red 0,0 the moment service opened**: a second
hand-written threshold table three lines from the shared one, reopening the
false alarm `ReputationColor` was rewritten to prevent. An em dash now, until
somebody has been served.

**The list panels ghosted the 3D scene.** `Bg` at alpha 0.97 — the only colour in
that file with no comment on it, which is what a value typed while testing looks
like. Three per cent of a lit scene is visible against a near-black panel: the
scroll body measured brighter than its own head band, and the rows, already
1.19 : 1 against their background, lost most of what separated them. It read as
an unfinished screen. `ErrorScreen.Backdrop` has always used opaque `Bg`; two
backdrops in one codebase disagreeing is the proof that 0.97 was never a
decision.

---

## 4. The most destructive button in the game was the biggest, unguarded, and painted disabled

`Kit.Cta` **never calls `SetEnabled`**. `ready` changes the face colour, the
arrow colour and the title colour; the click handler is wired unconditionally. So
through the whole of service — the longest phase of the day — "Close the day" was
**live** while wearing the game's own disabled treatment, and one press ran
`Simulation.CloseDay`, which walks the hall and sends every seated party away
angry. Revenue lost, reputation hit, no undo, no question asked.

The *morning* CTA, whose action is far less destructive, already guards itself,
and its comment carries the rule that matters: ask only when there **is**
something to ask about, or the confirmation becomes a reflex nobody reads. Closing
a finished service asks nothing; closing a hall with guests in it asks once.

---

## 5. The dish rows were under the touch floor, on the most-tapped list in the game

`CompactRow` is built with `new Button(...)` rather than `Theme.Btn`, so it never
received `minHeight = Theme.Touch` and came out at 5 + 17 + 5 = **44 dp**. The
comment justified that: *"With the whole row touchable the row comes down to
44 dp AND the touch target GROWS — a strip 873 dp wide."*

That is the wrong shape of argument. The 48 dp floor is a floor on **both** axes;
width does not buy height. And with `marginBottom: 2` the centres of two rows were
46 dp apart, so a finger aiming at one dish opened its neighbour — the exact
failure `Theme.Level`'s comment records catching for the volume steps, and
missing here. The menu has 32 dishes.

Three rows per screen becomes four at 52 dp. That is the honest cost.

---

## 6. Hiding the scrollbar removed the only sign that content continues

`Theme.Mobile` hides both scrollbars, and the reasoning is right: Unity's default
is a thick light-grey desktop bar with arrow buttons, it looked foreign in a
screenshot, and nobody uses it on a touchscreen. But it was also the only thing
on screen saying *"there is more below"*, and **nothing replaced it.**

What that cost, at 873 × 393:

| screen | below the fold |
|---|---|
| **settings** | the hints button, the "Language" heading, **all five language buttons**, and Back |
| **cuisine choice** | the second cuisine and Back |
| **main menu** | with a save present, a fourth button puts the content 47 dp past the viewport |
| **slot choice** | slots 3 and 4, and Back |

A five-language game with no reachable way to change language, and a screen that
sells two cuisines while showing one. All four are draggable; none of them says
so. The Android back key gets the player out, so it is not a trap — but a control
nobody can find has not shipped.

The fix is one place: a bottom edge fade on every `Theme.Mobile` scroller, shown
only when the content really is taller than the viewport. It is **stacked strips,
not a gradient** — UI Toolkit's C# style API has no gradient fill, and this
project has a standing rule against a drawing API that "worked in the editor and
did not show up in the build"; `Icons.cs` builds every icon in the game out of
rectangles for that reason.

---

## 7. In self service, the cleaner was wearing a dinner jacket

[51](51-self-service.md) removed the waiter from fast food outright and put a
cleaner in their place. It is the sharpest mechanical difference between the two
cuisines — volume **+43%**, ticket **−22%**. The interface renames the role and
the simulation runs a different hall loop for it.

`Wardrobe.Role` had `Cook`, `Dishwasher` and `Waiter`, and the view fell through
to `Waiter` for every non-dishwasher in the hall. So a self-service burger bar had
a figure in a black dinner jacket, a white shirt and a burgundy bow tie standing
in it — the picture contradicting the mechanic that is supposed to separate the
cuisines.

`Role.Cleaner` is built out of parts that already exist: the dishwasher's apron
and the yellow gloves (chosen because yellow is the one colour that reads from a
distance), plus a cap to separate the two from each other. The silhouette does
the work, because at the default camera a figure is ~32 dp tall and detail does
not survive.

---

## 8. What the reviewers got wrong

Both worth recording, because the corrections are the same shape as the findings.

**"There is no 873 × 393 render on this machine."** True when it was said, and my
fault: `tour.ps1 -Store` writes into `%TEMP%\lokanta_tour_1` and then copies, so
the store run I kicked off had overwritten the measurement run's phone-size
frames. Every dp figure in that report is a pixel measurement of a 2.5× frame
divided by 2.5 — which is sound, because `Autopilot.MatchPhoneDp` sets
`referenceDpi = Screen.dpi / scale`, so it is one build and one layout. But the
hint layer is genuinely invisible in those files: store mode calls
`Hints.MarkAllSeen()`, so **nobody has looked at the first-run frame** since the
last store render.

**"The hero shot reads Ciro 25 / Ağırlanan 1."** True of the file that was on
disk when the reviewer read it, and not of the mechanism. The store capture waits
for half the tables to be occupied, revenue above zero and no notice bubbles on
screen; the run it was quoting had caught a thin moment. A fresh run reads **day
40, 14/14 tables, revenue 568, satisfaction 79.7**. The gate is real; its floor
is too low, and raising it is on the list below.

---

## 8b. A red that was nobody's fault, and what it cost to find out

A fast food tour came back with **five reds at once**, all saying the same
thing: nothing was happening in the hall.

```
FAIL : Service produces occupied tables (at most 0)
FAIL : There is movement in the hall (0 figures on their way)
FAIL : An approaching figure opened the door (0 doors)
FAIL : The guest comes in from the street (0 figures outside)
FAIL : Work is being done in the kitchen (0 people; the simulation gave work 0 times)
```

The previous fast food run had been 184 passed / 0 failed, so it looked exactly
like a regression from this round's changes.

**It was not.** The same binary then passed twice: once at phone scale
(179/0) and once at store scale (184/0).

My first hypothesis was the render resolution — the store tour draws 6.25× the
pixels, `GameApp`'s tick budget discards accumulated time beyond 40 ticks a
frame, so a lower frame rate really does make the simulation advance more slowly
in wall-clock seconds, and this round's shadow work made rendering more
expensive. It is a sound mechanism and it is **not what happened**: the store
run passes. Recorded because it was checked, not because it was right.

What it actually is: the liveliness window closes after a flat 40 real seconds,
and a day-one fast food hall is four tables and twelve guests. The window can
close before any of it happens. The code comment above that loop **already
describes this exact failure** from an earlier round — *"The same build PASSED on
a second run. So both the red and the green were the result not of the
measurement but of SAMPLING LUCK"* — and the fix made then covered one check,
the cook's, with a 75 s extension that fires only while the kitchen is unseen.

Everything else still closed at 40 s. The extension now covers every headline
check in the window: it keeps looking, to the same ceiling, while anything it is
meant to see is still unseen, and closes at 40 s once everything has been seen —
so the table checks that follow still find the peak. **It lowers no bar.**

A check that comes back red on one run in three is worse than no check, because
it teaches the reader to re-run until it is green, and after that it can never
fail for a real reason again.

---

## 9. Also fixed on the way past

**`tour.ps1 -Store` wrote every cuisine's screenshots into the same folder**, so
the second run silently replaced the first one's. The two cuisines look different
on purpose and the Play listing wants both; what was actually sitting in
`render/store/` was whichever cuisine happened to be run last, with nothing in
the name to say which. It is `render/store/<cuisine>/` now.

**And `render/store/` was gitignored.** `.gitignore` carried

```
render/*
!render/magaza/
```

The English rename ([56](56-english-repository.md)) moved `render/magaza` to
`render/store` and that line did not follow, so the un-ignore pointed at a
folder that no longer existed and the store images — which the comment three
lines above calls *a release asset* — quietly stopped being committed. **An
ignore rule fails by doing nothing**: no error, no warning, no output.

The reason nothing caught it is the same shape as everything else here.
`check_english.py` decides what to open by file **extension**, and the files that
configure a repository have none. `.gitignore` had been Turkish since the first
commit — four section headings and three paragraphs of reasoning — and every run
reported the repository fully English because it never opened the file. It reads
`.gitignore`, `.gitattributes` and `.editorconfig` by exact name now, and the
first run after that change immediately found `.gitattributes`, which was
Turkish too.

**One of the commits in this round swept in files that should never have been
there**: Unity's Performance Testing package writes `PerformanceTestRunInfo.json`
and `PerformanceTestRunSettings.json` into `Assets/Resources` on every run that
loads the test framework, and a `git add -A` taken while a tour was running
caught them. The next tour deleted them, which is how a generated file announces
that it does not belong in the repository. They are ignored now.

---

## 9b. The check that was missing, and the four screens it found

Section 6 fixed the *signal* — a fade where the scrollbar used to be. It did
not fix the *layouts*, and nothing measured them: `ClippedButtons`,
`OverlappingButtons` and `StripHeight` all walk `GameScreen`'s own strips, so no
menu, list, settings or dialog screen had ever been measured by anything.

`Autopilot.CheckScreenLayout` walks the whole visible tree of whatever screen the
tour is standing on and asks two things of it: is every visible button at least
48 dp in **both** axes, and does every button and label stay inside the panel. It
names the worst offender, because "3 controls are too small" sends the reader
hunting and `[Language] 873x44` does not.

It went red on its first run, on exactly the four screens section 6 predicted:

```
FAIL : layout main menu      [Quit] 36 dp past the edge
FAIL : layout cuisine choice [Back] 316 dp past the edge, 6 elements
FAIL : layout slot choice    [Back] 506 dp past the edge, 8 elements
FAIL : layout settings       [Back] 189 dp past the edge, 8 elements
```

### What it took to make them green

Four attempts, and three of them are worth recording because each was wrong in
a way that looked right.

**One.** A wrapping column: `flexWrap` so the content spills into a second
column instead of off the bottom. The rebuild came back with **byte-identical**
numbers. `height: 100%` inside a ScrollView resolves against the *content*
container, and that grows with its content — a column asking to be as tall as
the box its own content defines has no bound at all. The bound is
`contentViewport`, which is the window the content scrolls behind. Identical
numbers were the tell: a real change that achieves nothing usually moves
something.

**Two.** With a real bound the four screens went green — and the cuisine screen
had lost its title and its Back button off the **right** edge, where the check
was not looking. It only measured the bottom, because that is where the hidden
scrollbar used to speak. **A check that watches one edge teaches you the other
three are safe.** It measures all four now, and that immediately re-reddened two
screens that had just been declared clean.

**Three.** Generic wrapping is the wrong tool where items have different roles.
It packs purely by height and cannot know that a title belongs above *all* the
cards and Back below *all* of them, so it pushed both into a third column. The
cuisine screen's two cards are a **comparison** and are now an explicit row; the
four save slots are a **set** and are now one row of four. Both screens put the
title and Back on a shared header line, which costs nothing — the title was
never using the width — and saves the 52 dp that was the whole overflow.

Then `maxWidth: 100%` on an auto-width column turned out to be the same trap as
the height, one property along: the column sizes to its content and the cap
never binds.

Final: **189 passed, 0 failed.** Both cuisines, all five language buttons, all
four save slots and every Back button are on screen.

---

## 10. Found, verified, and not yet done

Listed so they are not lost, and ordered by what they cost against what they buy.

| | what | why it is not done yet |
|---|---|---|
| ~~The screenshot aspect ratio~~ | **Done.** `tools/store/compose.py` pads every store frame to 2183 × 1120 = **1.949 : 1**. See §13.6 | Nothing is resampled and nothing is cropped, so the pixels Play shows are the pixels the game drew. The upload attempt is still worth making: this satisfies the published rule, and only Play can confirm the rule is the published one |
| ~~The store screenshots are in Turkish~~ | **Done.** `tour.ps1 -Lang en`. It uses `Loc.UseLanguage`, which applies without persisting — a tour run must not leave the developer's own language changed behind it — and with no flag nothing happens at all, so the measurement runs are untouched | The store copy goes to `render/store/<cuisine>/<lang>/`, so five listings can have five sets |
| ~~The back wall stands over a hole of sky~~ | **Done.** At tier 1 the open rooms bound a 13.4 × 9.6 m box and Hall2 (5.0 × 5.2 m) is closed, so a rectangle of sky sat inside the building with a 2.6 m wall over it. Closed rooms are now built as a bare concrete **shell**: no wall, no door, no furniture, and the collider destroyed so it is genuinely not touchable rather than merely untagged | It costs nothing in framing — `CameraFit.OpenBounds` is built from the open rooms and the street — and it shows the player the space expansion buys, which the campaign's main progression path had never had on screen |
| ~~The table badge is ~4 dp tall~~ | **Done.** The overview badge now sits on the ROOM, as [31](31-rooms-and-camera.md) §8.1 decided - see §13.2. What follows is the earlier half. **Half done.** Thickness 0.18 → 0.36, and `LeftAngry` no longer hides the badge — it shows a full-width dark red mark, darker than the "running out" red so that *about to go* and *gone* do not read alike. Patience is zero by then, so the meter would have drawn nothing even after it stopped being hidden | The badge is legible now, not solved. [31](31-rooms-and-camera.md) §8.1 decided the real answer — in the OVERVIEW the badge belongs on the room, not the table — and that is still unbuilt |
| ~~The kitchen is a lamp, not a meter~~ | **Done.** How many hobs are lit is the load; their colour is whether the station is coping — blue at or under capacity, amber over, red at twice. Colour alone would not carry it: the stove is a handful of pixels at the default camera, so the count of lit hobs is the channel that survives at that size | `Simulation.StationSlotCount` was added because a load without a capacity has no scale: three plates on a one-slot hob is a jam, three on a four-slot range is a quiet morning |
| **The backdrop is still one flat colour** | It is darker and more neutral now, so it stops fighting the building - but 37% of the hero frame is still a single value with no sky, no horizon and nothing behind the restaurant. `DayLight` already drives that colour through four day stops, so a two-stop gradient quad could be driven from the same curve | The reasoning on record is [19](19-technical-setup.md)'s fill budget, and it is worth testing rather than inheriting: a quad at the far plane replaces the camera clear instead of stacking on it, so it adds no overdraw. `CameraFit.OpenBounds` is built from the room plan and the street only, so decor placed BEHIND the building does not push the camera back either - which means a row of neighbouring facades is free in framing terms |
| ~~No colour grading~~ | **Done, with one part still open.** Contrast +8, saturation +6, white balance +6, and `tools/check_grade.py` refuses nine off-tile effects by name. See §13.3 | The FRAME COST on a phone is still unmeasured and cannot be measured here: the pass forces the camera through an intermediate target, which is bandwidth. [21](21-business-and-release.md) carries it as a device item, and it is a one-line revert |
| ~~The main menu has no game in it~~ | **Done.** There is a real restaurant behind it now - fast food, four tables, the cook in a toque, the street with pedestrians - and one flat colour is **39.3%** of the frame where it was 80.9%. Nothing new had to be built: `RestaurantView.Source` already falls back to `Preview` when no campaign is loaded, and the editor's screenshot tool has driven that path for weeks, so the menu shows the game's own scene code. It is a live simulation, not a still, on a fixed seed so the menu looks the same every time it opens | The layout went with it: it was five buttons of equal weight in a wrapping column and at 873x393 the set did not fit, so "Quit" landed in a second column beside the others. One primary - Continue if there is a save, New Game if not - and a quiet row for the rest. The tagline, which is the whole pitch and was 17 dp grey under a 42 dp wordmark, is now full-strength ink |
| ~~The customers are twelve stock prefabs~~ | **Done.** `tools/art/gen_crowd.py` recolours the pack's colormap per cuisine - the clothing swatches only, with the four skin tones asserted byte-identical. See §13.1 | It cost nothing at draw time, as expected: still one material for every figure in the scene, and the renderer count did not move |
| ~~No UI layout check exists~~ | **Done, and it earned its place on the first run** — see §9b. It walks the whole visible tree of whatever screen the tour is on and reports the worst offender by name | It did not need a mutation to prove it fires: it went red immediately on the four screens §6 predicted, and red again when the first fix moved the overflow sideways |

---

## The state after the round

```
python tools/check.py                17/17 clean, 249 core tests
tour.ps1 -Cuisine turk               189 passed, 0 failed
tour.ps1 -Cuisine fastfood -Runs 2   186/0 and 185/0
tour.ps1 -Store -Lang en             194 passed, 0 failed
tour.ps1 -Build windows-il2cpp       191 passed, 0 failed
BuildPlayer.Android                  90.5 MB, 0 warnings
```

The tour is 194 checks where it was 176 at the start of the day. Eighteen of
those are new: the torn-save arms from [57](57-end-to-end-audit.md), and the
layout pass across four screens that had never been measured.

**Two numbers deliberately not compared.** The Turkish run and the fast food run
are different check counts because the cuisines have different mechanics, and
this project has written down more than once that comparing them is the mistake.
The pairs above are same-cuisine, same-build.

---

## 10b. The bottom strip

> *"Su an altta duran kisim biraz kaba gibi."*
>
> *("The part at the bottom looks a bit crude at the moment.")*

It was, and the reason is worth naming: the strip held four different KINDS of
thing - the clock, the three verbs, the signature mechanic and the phase advance
- in five dark boxes of five different sizes, all the same colour, with nothing
saying which of them was the game.

The three verbs were the worst of it. They are what the player DOES during
service, and they were plain text buttons sized by the length of their own
words, so "Tea" was a third the width of "Speed up" and the set read as three
accidental rectangles. The four charges that gate all three sat at the far end
of the box past a grey `> impatient`, so the shape of the decision - *three ways
to spend four charges* - was not on screen anywhere.

**What it is now.** One group with a copper edge along its top - the accent is
spent there and nowhere else in the strip, because that group is the one the
player acts through. Inside it, the four charges sit centred directly above
three equal-width buttons, each with a drawn icon saying WHERE the verb lands:
a flame for the kitchen, a glass for the room, a speech bubble for one table.
A spent charge is an empty socket rather than a dim dot.

**Four things were tried and thrown away on the way**, all of them measured
rather than argued:

| attempt | what the measurement said |
|---|---|
| `flexGrow` on the three buttons | They stayed at their 52 dp minimum and the labels broke into "Spe/ed/up". A button whose siblings are a fixed-width pip row does not get an equal third |
| A width cap on the CTA, to free room | Its own title wrapped to two lines, which made the button taller, which made the STRIP taller - 176 dp against a 154 dp budget - and the tour's camera check went red by 0.76 m. **The strip's height is the hall's.** |
| Moving the pips into a column | Right idea, wrong axis: in a column `alignItems` is the HORIZONTAL one, and `Center` left the verb row content-sized so it grew past the group and landed on the Tab button. The overlap detector caught it at 26 dp |
| Dropping the `> impatient` readout | Kept. It cost ~75 dp of the one group with none to spare, and it is a number the player has to match against a table anyway |

That last one has a condition attached. The readout was the only textual answer
to "which table is selected", so removing it makes the WORLD the only channel -
and the selection frame there was `+0.10 / +0.06` metres against a badge
measuring 4.4 dp, which is under a dp of edge. One reviewer compared the tour's
"a table is selected" screenshot against the one before it and could find no
difference in the picture at all. The frame is `+0.22 / +0.18` now. **Removing a
weak channel is only safe once the one it leaves behind is strong.**

Strip height after all of it: **162 dp in every language**, against 154 before.
Eight dp of hall for a group that says what it is.

---

## 12. Three things the user saw that no check was watching

> *"Karakterler mobilyalarin icinden geciyor bunu duzelt. Ayrica mutfak
> yerlesimini vs daha gercekci yap. Kapi vs onlarin yerlerini de kontrol et.
> Ayrica musteri ve karakterlerin ismi secilen dile gore degissin."*
>
> *("Characters are walking through the furniture, fix it. Also make the kitchen
> layout more realistic. Check the doors and their positions too. And the
> customers' and characters' names should change with the chosen language.")*

### Walking through the furniture

`Paths.Lane` said why in one line: `if (targetRoom == sourceRoom) return;`.
Between rooms it routed through doors; **inside** a room it added no waypoints at
all, so a waiter crossing a dining room went in a straight line through every
table and chair.

**There is no gap to walk between tables, and that is measured.** A chair sits
`SeatRadius` 0.58 m from the table's centre and is about 0.40 m deep, so a table
set is roughly 1.56 m across. The grid cell is 1.85 × 1.70, which leaves 0.29 m
between sets in x and 0.14 m in z — against a figure about 0.45 m wide. Routing
*between* them was never available at this table density, and no amount of
cleverness in the path code would have found a way through.

What **is** available is the perimeter: `RoomPlan.TableSpots` centres the grid in
the room, leaving 0.50–0.65 m on all four sides, and every table in these rooms
touches it. The route is now out to the nearer side lane, along it, and in —
three waypoints and no pathfinding, which is the rule this layer was built on.

**Mutation-verified**, because a claim about furniture needed a number:

| | worst a walking figure gets inside a table set |
|---|---:|
| with the fix | **0.00 m** |
| with the routing removed again | **0.50 m**, and the check goes red |

### The placement audit had been reporting noise

Asked to check the doors and the furniture, the answer came back: **93 clashing
pairs** — and every single one was against `Decor`, the merged mesh `Modeler`
builds one of per colour, whose axis-aligned box covers most of the building.
Not one real object-to-object overlap in the scene.

A list where every line is noise is a list nobody reads, and the one real clash
it exists to catch would have been invisible in it. With the merged group
skipped it reports something usable — and it earned that the first time it was
asked, catching the prep counters below.

### The kitchen had no middle

A small restaurant kitchen is a sequence — cold store, **prep**, cooking line,
pass — and this one went straight from the fridge to the stoves. The pass exists
(the service counter), the fridge is in the back-right corner, the stoves line
the back wall, so prep belongs on the left wall, which turns the line into an L.

**The middle has to stay clear**, and not as a matter of taste: `Paths.CookHome`
puts the idle cook at the room's centre and `Paths.KitchenPost` puts the working
cook in front of the stoves, so an island would be something to walk through —
and the in-room routing above covers dining rooms only.

The first attempt put three counters where two fit. The counter prefab is 0.92 m
and the run was 1.94 m, so they stood inside each other: the audit reported
0.23 m between neighbours and 0.38 m against the leftmost stove. Two counters,
a run starting further forward, 0.76 m clear of the cooking line — **0 clashing
pairs**.

### Names follow the language

Nine of the twenty named regulars carry a Turkish register marker. They are
kept here as quoted terms because the whole point is the word itself:
"Usta" (master craftsman), "Teyze" (auntie), "Bey" and "Hanım" (formal),
"Abla" (elder sister), "Dede" (grandad), "Abi" (elder brother), "Hoca"
(coach or teacher), "Şoför" (driver). That marker is what says how the owner
knows this person. It is the point of a named regular,
so this is not transliteration:

| | what it maps to |
|---|---|
| **es** | *Don* / *Doña* for the elders, *Profe* for the coach — the same warm-formal register, used the same way |
| **zh** | X 师傅 is the exact counterpart of *Usta*, X 阿姨 of *Teyze*, X 爷爷 of *Dede*, 老 X of the familiar elder. Almost one to one |
| **ar** | Arabic has *usta* (الأسطى) from the same Ottoman root, plus عم, خالة and الحاج — and several of the Turkish names are Arabic in origin anyway |
| **en** | the least of this, so plain first names with Auntie / Uncle / Grandad / Coach where the register really carries |

The 96 staff names became `name.staff.N` in all five tables, index-aligned with
`content/names.json`. **The core hands out an index and the view resolves it**, so
a row is the same *person* in every language and a save keeps its meaning across
a language change: switching language renames the cook rather than replacing
them. `gen_loc` checks the length, because a short list would leave the last few
names untranslated without a word.

And the tour was building the expected tenure notice from `Sim.StaffName` while
`Notices.cs` had moved to `Loc.StaffName` — a Turkish name compared against a
translated one, which would have turned that check quietly UNMEASURED. The
comment directly above it already records that exact lesson from an earlier
round.

---

## 11. The release build was blocked, and the cause was a compiler nobody used

Re-running the two pre-release gates after this round found something that has
nothing to do with the round: **neither of them can run any more.**

```
tour.ps1 -Build windows-il2cpp   ->  Fatal error in Unity CIL Linker
BuildPlayer.Android              ->  Fatal error in Unity CIL Linker
```

Both go through the same `UnityLinker.exe`, and the linker dies on the same
line:

```
System.IO.FileLoadException: Could not load file or assembly
'...Editor/Data/il2cpp/build/deploy/Analytics.Api.Output.dll'.
An Application Control policy has blocked this file. (0x800711C7)
```

`0x800711C7` is the Smart App Control signature this project already has a
memory about. What is new is **where** it is landing. This is not the project's
own output: it is a file inside the Unity Editor installation, and

```
Analytics.Api.Output.dll   11,264 bytes   17 August 2026
Signature status : NotSigned
```

it is **unsigned**. Smart App Control blocks unsigned binaries by HASH, and a
shipped Editor file has a fixed hash, so unlike the project's own DLLs there is
no rebuild that changes it. Tried twice; identical failure. The machine's policy
state is `VerifiedAndReputablePolicyState = 1`, enforced.

**What still works:** everything else. The Mono Windows build compiles and the
tour runs on it, so the whole measurement layer, every check and every
screenshot in this document is unaffected. Both IL2CPP paths are blocked, which
is precisely the release path: the Android package and the stripping exam.

**It worked this morning.** A Windows-IL2CPP build passed at 02:20 and an
Android APK at 02:29, both from this machine. Nothing in the project changed
between then and 11:48 that touches the linker. SAC takes its verdicts from a
cloud reputation service, so the policy moved underneath the machine.

**It is not mine to resolve, and the rule stands:** [CLAUDE.md](../CLAUDE.md) 5,
*never turn Smart App Control off - it is a one-way switch.* It stays on.

### CORRECTION, later the same day: it WAS mine to resolve

The paragraph above was wrong, and the way it was wrong is worth keeping.

SAC then went further and blocked `Burst.Backend` as well, which killed the
**Mono** build too - at that point the machine could not build anything at all.
Chasing that one turned up the question nobody had asked:

```
grep -rn "BurstCompile" unity/Assets/   ->  nothing
grep -n  "burst" unity/Packages/manifest.json  ->  nothing
```

**The game does not use Burst.** Not one `[BurstCompile]` anywhere, and it is not
a dependency the project asked for - it arrives underneath URP and the input
system. So an AOT compilation step that nothing in the game benefits from was a
hard build failure, and turning it off is less a workaround than the removal of
something that should never have been in the build:
`ProjectSettings/BurstAotSettings_*.json`, `EnableBurstCompilation: false`.

With it off, **both** gates came back:

```
BuildPlayer.Android              90.5 MB, 0 warnings
tour.ps1 -Build windows-il2cpp   191 passed, 0 failed
```

and the package lost a native library it had been carrying for nothing:
`lib_burst_generated.so` is gone, six `.so` files where there were seven.

**Honest about the causation.** The Windows-Mono failure was Burst and the fix
is certain there. The IL2CPP failure named a different file -
`Analytics.Api.Output.dll`, refused inside `UnityLinker.exe` - and I cannot
fully separate two explanations: that disabling Burst removed the linker plugin
whose attribute scan was reaching that assembly, which the stack trace makes
plausible, or that SAC's cloud reputation simply changed its mind in the hours
between. Both gates pass now; only the first of the two is proven.

**What I got wrong:** I looked at a blocked binary, recognised a trap the
project already had a memory about, and handed it back as the user's problem
without asking whether the blocked thing was needed at all. The rule about not
turning SAC off was right and is untouched. The conclusion drawn from it - that
there was nothing left to do - was not.

Recorded because several of these were nearly collateral damage.

- **`10-evening`.** The one frame in the set that already looks like a game, and
  everything above was steered toward it rather than away. Four channels move at
  once — sun elevation, light colour, background, lamps — which is why it reads
  as "evening has come" rather than "someone turned the lamp down".
- **The two cuisine palettes.** Real, procedural, zero-asset differentiation that
  genuinely reads side by side: cold slate, chequered tile and red neon against
  warm plank, wainscot and burgundy rug.
- **The light plate under the till number**, measured at 12.98 : 1. The best-made
  object on the screen. §3 extends it rather than diluting it.
- **`Theme.RowFlow` and `SetGap`'s physical-edge rule.** The Arabic layout mirrors
  correctly because of them, including the part that is easy to get wrong.
- **Drawn icons, never font glyphs**, after a real bug where Rubik's missing star
  showed the player an empty box.
- **The world layer never lies.** A plate appears only when the core says the
  party is eating; the terrace is empty because the simulation does not serve
  outside. Most shipped tycoons fake the world layer freely. That discipline is
  the only reason any indicator added later will be worth trusting.

---

## 13. The last three, and the four bugs they turned over

The items §10 left open that were mine to do — the crowd, the badge, the grade,
and the store frames' aspect ratio. All four landed. Three of the findings came
from the same place: **something was wired, the measurement said yes, and the
picture did not change.**

### 13.1 The crowd wears the cuisine

The staff have a wardrobe and the furniture has a per-cuisine material copy.
The customers had neither, so the one part of the frame that is mostly PEOPLE
said nothing about where the player was.

It could not be a tint. All twelve figures share **one** material and **one**
512×512 texture, so there is no shirt submesh to paint — a `_BaseColor` would
multiply the whole figure, skin included, and [10](10-cuisine-identity.md) is
explicit that identity here comes from the environment, the light, the
silhouette and the clothes and **never** from a face.

What the texture turned out to be settled the method: an indexed PNG holding a
grid of seventeen flat swatches, 64 px wide and 128 tall, each one a flat tone
beside a vertical ramp. So "recolour the clothes and leave the skin alone" is
not image processing, it is picking rectangles. `tools/art/gen_crowd.py`
recolours ten of them and copies the rest through, and then **asserts the kept
ones are byte-identical** rather than trusting that it did not write to them —
an off-by-one in the swatch table would look exactly like a correct run.

The two crowds differ by figure/ground, not by decoration:

| | the room | the crowd |
|---|---|---|
| fast food | cold and dark (wall 0.173, 0.184, 0.212) | chroma ×1.30, value ×1.06, hue +10° — louder and brighter than the room, which is also what a burger bar's crowd is. The pack's palette is already near the ceiling, so this moves least |
| Turkish | warm brown wood and copper (wall 0.290, 0.196, 0.137) | chroma ×0.62, value ×1.02, hue −12° — washed and slightly cool. Saturated warm clothes would **disappear** into that wall, the same trap the daytime sky fell into in §5 |

Neither transform touches the hue order, so twelve characters stay twelve
different people in both.

**And it did nothing, twice.** The first run reported every figure recoloured
and rendered an identical frame. The cause was not in any of the new code:
`ArtPrefabs.FindTexture` returned the first texture whose **path contained**
`"colormap"`, in whatever order the asset database handed them over — so the
moment `colormap-crowd-fastfood.png` landed in that folder it became the first
match, and the tool silently re-pointed the pack's own material at it and
rewrote all twelve prefabs to suit. Every figure in the Turkish restaurant was
wearing the fast-food palette, and the game rendered perfectly.

Two things came out of that, and both are worth more than the crowd:

- **The lookup is now an exact file name**, and says so when there are near
  misses instead of picking one. A substring match over an unordered list is
  not a lookup; it is a race with the file system.
- **The guard was green the whole time.** It asked whether the material carried
  *a* crowd texture, and a fast-food map is a crowd texture. It now asks
  whether it carries **this cuisine's** map. A guard that accepts the wrong
  answer is worse than no guard, because it is evidence pointing away from the
  bug — it cost an hour of looking in the wrong place.

### 13.2 The badge moved to the room

[31](31-rooms-and-camera.md) §8.1 decided this and nobody built it. It is not
decoration: [30](30-venue-layout.md) had costed the room camera at twelve taps
a service slice, "120% of the entire budget … in return for zero decisions",
and §7 of [31](31-rooms-and-camera.md) answered by refusing the premise — the
player does not travel to see, **the overview is the scan**. That answer only
holds if the overview carries the state, and it did not: the readout was on the
table, and a table is 16 dp from up there.

Now: one badge per open dining room, showing the **worst** table in it, with a
touch target that selects that table. The ordering is not a preference, it is
what can still be acted on — left angry, then waiting by patience left, then
waiting to pay, then eating. One readout at a time: in the overview the table
badges are hidden and in the room view the room badges are, because two
readouts of the same state at two sizes make the reader pick, which is the cost
the room badge existed to remove.

Two things were wrong in the first screenshot and both were about size:

- **It landed on the room behind.** The camera looks down at 34°, so raising a
  point by *h* slides it up the screen by *h*/tan 34° = 1.48*h* of ground —
  4.5 m for this badge, a whole room. Every badge was at the exact centre of
  its room, and that was the bug. The height is now cancelled out against the
  **live** camera, because the player can swing the view and a constant worked
  out at build time would be right only at the default angle.
- **It was a black slab.** The table badge's track is nearly black, which reads
  as a rim around a coloured sliver at 18 dp and as a *hole* at 60. The room
  meter has a light track — which also says the right thing about what it is: a
  readout laid over the scene, not a sign standing in it.

Sized off the frame rather than by eye: 3.00 m measured 175 px, i.e. 119 dp on
the 873 dp phone frame — wider than its own room. 2.20 m lands at about 87 dp,
still well over the 48 dp floor, which is the entire point of putting the badge
on the room instead of on a 16 dp table.

### 13.3 The colour grade, and the camera that was not looking

[19](19-technical-setup.md) has always permitted "at most a light colour
grading table" and it had never been spent. It is spent now: contrast +8,
saturation +6, white balance +6, on a renderer that finally has a
`postProcessData` and a camera that asks for post-processing.

`tools/check_grade.py` is the interesting half. Every post-processing override
already lives in the one shared asset with `m_OverrideState: 1` set on all of
it, so turning bloom on is not adding a feature — it is typing a number into a
field that is already there. Nine off-tile effects are refused **by name with
the reason next to each**, and the check fails on any of them moving.

**And this one did nothing either, in exactly the same way.** The renders came
out ungraded and looked entirely correct, because they looked like the day
before. The screenshot tool builds its own camera — this file already carries
that scar about the lights — and a camera built in code has
`renderPostProcessing` false. It was caught by measuring, not by looking:
contrast was driven to −100, the frame re-rendered, and the mean colour of the
two images was **identical to a tenth of a level**. A grade you cannot see in
the picture and cannot see in the numbers is a grade that is not running. With
the flag added, −100 renders a flat grey field, which is the proof.

**What is still open:** the frame cost on a phone. Post-processing forces the
camera through an intermediate target and that is bandwidth; no desktop run
says anything about it. It is a device item in [21](21-business-and-release.md)
and a one-line revert.

### 13.4 The figures were still walking through the furniture

The tour went red at 0.51 m on a run with nothing about walking changed, having
been green at 0.00 m twice. It was nearly written off as flaky. It was not.

The in-room routing added last round covers the case where both ends are in the
**same** room. A waiter coming from the kitchen is not that case: it reached the
front corridor at the target's own x and then walked straight up that column,
through every table set standing between the corridor and the table. **Which**
tables those are depends on which table the party was seated at — so the
failure appeared and disappeared with the seating, which is the exact shape of
a bug that gets called flaky.

It was only diagnosable once the check was made to **name its offender**, the
rule this project already applies to the layout check:

```
FAIL : Nobody walks through the furniture (worst 0,50 m inside a table set;
       character-male-c(Clone) at (10,5, 1,4) is inside table 0 at (10,0, 1,4),
       heading for (10,5, 3,1))
```

That is a walk to the back table of a column, through the front one. The
approach now uses the same side lane the in-room walk does. Measured against
the real grid rather than assumed: the lanes sit 0.28 m off the wall and the
nearest table centre is 1.10–1.30 m away, over the 1.00 m a figure needs.

The measurement itself was also wrong, and it took a second tour run to see the
other half: a figure standing where it had just served is inside those chairs
by definition, and so is one sitting down. **Two tables are excused and no
others** — the one you came from and the one you are going to. Everything
between them is somebody else's, and crossing a third still fails: proven by
putting the old straight-up approach back with the exclusion in place, which
went red again immediately.

The measurement itself was also wrong, twice over. A figure standing where it
had just served is inside those chairs by definition, and so is one sitting
down. **Two tables are excused and no others** — the one you came from and the
one you are going to. Everything between them is somebody else's, and crossing
a third still fails: proven by putting the old straight-up approach back with
the exclusion in place, which went red again immediately.

**And then the corridor itself turned out to clip the chairs.** With the route
fixed the tour still reported 0.20 m, on a figure walking along the front lane.
The note on `Paths.LaneZ` had justified 0.55 m against the wrong edge — it used
the chair *radius*, where what matters is the chair's outer edge. A chair
centre is 0.58 m from the table and a chair is about 0.40 m deep, so the front
row's sets reach down to z = 1.35 − 0.78 = **0.57**, and a figure is 0.44 m
across. At 0.55 the walker's near side was at 0.77 — a fifth of a metre inside
the front chairs, on every trip, since the game began.

The aisle is 0.57 m wide and the figure is 0.44, so there are **13 cm of slack
in total**. The lane is no longer chosen: it is centred in the only gap it has,
at 0.30. That is a layout fact worth having on record rather than buried — a
hall at this table density has room for exactly one aisle and no more, which is
also why the in-room routing uses the perimeter.

### 13.5 Every material in the project still had a Turkish name inside it

Not visual, found on the way past. The English rename moved
`Mobilya_wood.mat` to `Furniture_wood.mat` and twenty-two others with it, and
every one of them still said `m_Name: Mobilya_wood` **inside**.
`check_english.py` had reported the repository fully English for weeks because
`.mat` was not in its suffix list — the same failure as `.gitignore` in rule 1
of [CLAUDE.md](../CLAUDE.md), in a different place.

It was not cosmetic. `m_Name` is what `Material.name` returns at runtime and
`RestaurantView.Tinted` decides what colour a thing is painted by reading it;
the furniture tinting survived only because the rename had changed the PREFIX
and the branch matches the suffix. `Mobilya_wood` still contains "wood". The
next rename would not have been so lucky.

The new pass does not sniff for Turkish — `Mobilya` is a single token and
`turkish_text` needs two words on a line before it calls something a sentence,
so a one-word asset name is invisible to the prose pass **by design**. It asks
a question with an exact answer instead: an asset's internal name must be its
file name. Twenty-three were wrong, including `Ui.asset` calling itself
`Arayuz`, and two `[Header]` labels in the inspector.

### 13.6 The store screenshots were the wrong shape, and re-shooting was the wrong fix

The tour shoots at the phone frame, 2183 × 983 — **2.2208 : 1**. Play's
long-standing rule is that no side of a screenshot may be more than twice the
other, and 2183 is 217 px over 2 × 983. The listing is rejected at upload, so
this was a genuine release blocker sitting in a table of "not yet done".

The obvious fix is the wrong one. **The frame is not a picture, it is a
measurement**: 873 × 393 dp is what every UI number in
[41](41-ui-and-venue.md) was taken against — the 48 dp touch floor, the bottom
strip's budget, the layout check's four edges. Re-shooting at 2 : 1 changes the
layout and invalidates all of it, to fix a container.

Cropping is also wrong: 217 px off the sides takes the ends of the top and
bottom strips with it — the day and rent plate on one side, a verb button on
the other. Cropping a UI screenshot removes UI.

So `tools/store/compose.py` pads, and **the padding is the image's own edge
rows** rather than a bar in some chosen colour. The top row and the bottom row
are repeated outwards; both are the flat card of a HUD strip, so the result
reads as the strips being slightly taller rather than as letterboxing — and
there is no colour to pick, which means no colour to get wrong the next time
the theme moves. Nothing is resampled and nothing is cropped, so the pixels
Play shows are the pixels the game drew.

2183 × 1120 = **1.949 : 1**. The exact 2 : 1 height is 1091.5, so 1092 would
pass by half a pixel; 1120 leaves a real margin against a rounding rule nobody
published. The composed frames go to `render/store/<cuisine>/[<lang>/]play/`,
beside the raw ones rather than over them — the raw frames are the record the
UI measurements were taken from.

Seventy-five images, three sets. The upload attempt is still worth making, but
what it now tests is whether the published rule is the real rule, not whether
the file breaks it.

---

## 14. Eight more things the user saw, and the one check that was missing

> *"restoranin arka tarafinda, oyun ekraninin ustunde golgeler bulunuyor bunlar
> ne anlamadim … restoran giris kapisini bir sagdaki odaya alalim bulasikci da
> en soldaki odaya gecsin … iki karakter ayni lavaboda ic ice gecmis sekilde
> bulasik yikiyor, yanlis … mutfakta sol duvara konulan esyalarin arkasi
> duvardan tasmis gibi … restoranin onunde bulunan bitki veya saksilarin
> icerisinden cit geciyor gibi"*
>
> *("there are shadows behind the restaurant at the top of the game screen, I
> do not understand what they are … let us move the restaurant's entrance door
> to the room one to the right and let the dishwasher move to the leftmost room
> … two characters are washing up at the same sink, inside each other, wrong …
> the things put against the left wall in the kitchen look as if their backs
> stick out through the wall … there is a fence going through the plants or
> pots in front of the restaurant")*

Every one of these was in a frame the tour had already photographed and every
check had already passed on. The pattern is the same one §12 records: the
checks measure what they were pointed at.

### 14.1 The shadows behind the restaurant were the skyline, failing twice

The row of distant buildings §10 added was built into the same mesh group as
the FLOOR, and that group is built with `receive: true` - the floor has to take
the figures' shadows or nothing in the building is attached to the ground. So
blocks nine metres behind the back wall were receiving the near scene's
shadows, with nothing visible to cast them. Bands of dark across a shape you
cannot name is exactly what the user described.

Splitting it into its own mesh fixed the bands and the row was still dark, and
the second cause is the more interesting one: **the lighting was never the
colour's problem**. The sun comes from behind the building (52° elevation,
208° yaw), so every face these blocks turn towards the camera is in shade and
lit by ambient alone. No albedo survives that - which is the same thing
`TableBadge` learned when a green badge on a lit material measured 1.47 : 1
contrast against a floor it should have had 7.57 : 1 on.

On URP/Unlit the authored colour is what is drawn, and a hazy backdrop is
meant to be flat. Mixed towards each cuisine's own sky (dimmed to 0.72, after
the Turkish row came out a bright tan band brighter than the restaurant it
stands behind), and shortened from 3.6–6.0 m to 3.0–4.4 m: the tall version ran
off the TOP of the frame, and a row of rectangles with no tops is a wall, not a
skyline.

### 14.2 The entrance and the wash room changed places

Two rectangles swapped in `RoomPlan`. The names, the array order and every
lookup by name stayed exactly as they were, which is the whole benefit of the
plan being name-driven rather than index-driven.

It is not only where the user wants the door. `BuildDishStation`'s own comment
recorded what the old room cost: *"ONE SINK, NOT TWO - the room is 3.2 m wide
and each of the four objects is ~0.84 m"*. The simulation can put two people on
the washing up, and with one sink between them they walked to the same spot and
stood inside each other. At 5.2 m a counter, two sinks and a counter fit.

The adjacencies improve as well: the wash room now shares its whole 5.2 m edge
with the kitchen instead of 1.4 m of corner, and the store no longer touches
the entrance at all - which is the rule `Connect()` had been enforcing by hand.

**What the swap broke, and what caught it.** The placement audit went red on
the first run afterwards: two pairs of staff overlapping by 0.41 m. `HallHome`
spread the idle waiters along the room's WIDTH, which was fine at 5.2 m and
gives three of them 0.53 m each at 3.2. The new room is narrow and deep, so the
queue of idle staff turned through ninety degrees - two columns 1.5 m apart,
stepping back between the front corridor and the till counter. `QueueSpot` had
the same problem and the same answer: a queue that grows towards the camera
reads as a queue anyway, where one that grows sideways reads as people standing
about.

### 14.3 Two sinks, and a post each

One standing spot per sink, claimed when the walk starts and **held** until the
task ends. The obvious version picks the nearest free sink every frame, and two
washers then swap basins whenever one drifts a centimetre - which looks worse
than the bug it replaces. The tap and the suds are per basin too: running both
taps because one person is washing hides how many people are on it and leaves a
tap running over an empty sink.

### 14.4 The kitchen door opened behind a sink

> *"mutfaktan bulasikhaneye gecerken kapiyi lavabonun arkasindan aciyor ve
> lavabonun icinden geciyor"*
>
> *("when going from the kitchen to the wash room it opens the door behind the
> sink and goes through the sink")*

The doorway's position was computed in **two places**: `RestaurantView.Shared`
put the gap at the middle of the shared edge, and `Paths.BackDoor`
independently returned the middle of the room. They agreed by coincidence, and
`Lane`'s own comment at the top of the file had already written down what
happens when they stop agreeing - *"had it been written in two places, the door
would be in one place and the way through in another"*. What neither of them
knew is that the middle of that wall is now a sink.

Both sides ask `Paths.DoorAlong` now, and for the kitchen-to-wash-room edge it
answers with the LEFT END, where the user asked for it. The four-unit run
starts clear of the far jamb, so the cook comes through with 0.42 m to spare.
The leaf went with it: nothing inside the building has one any more, which is
the decision docs already recorded for the dining rooms, carried one room
further.

### 14.5 The counters in the kitchen stuck out through the wall

They were placed at `r.X0 + 0.34` on the reasoning that a counter is about
0.6 m deep. The prefab's pivot is not its centre, so both of them stood 0.12 m
out through the left wall into the street.

A depth written in a comment stops being true the day the art pack changes, so
`WallFlush` asks the object instead: it measures the placed prop's bounds and
slides it until its back sits on the wall's inner surface.

**And the audit now asks the question that was never asked.** The clash pass
compares objects with each other and has never looked at a wall, so it reported
"0 clashing pairs" the whole time those counters were sticking out. A second
pass finds the room each prop's centre is in and measures how far its box
reaches past that room's rectangle. Put the guessed number back and it says so
by name: `kitchenCabinet is 0.12 m past the wall of Kitchen`.

### 14.6 The railing went through the planters

The pots sit on the front line and the rail runs at z = −0.42; the pot is
0.46 m deep, so the rail passed through every one of them. There is nowhere to
move them to - the band between the shopfront and the rail is 0.36 m, and in
front of the rail are the two pedestrian lanes.

So the pots stay and **the rail breaks around them**, exactly as it already
breaks around the door. The breaks are built from the same list the pots are
placed from; two lists would drift apart the first time the spacing changed. A
planter set into a railing run is a real streetscape detail rather than a
compromise, and it gives the long front a rhythm it did not have.

### 14.7 A piece of furniture nobody could name

> *"bulasikhanein altinda bulunan mobilya ne onu anlamadim"*
>
> *("what is that furniture at the bottom of the wash room, I did not
> understand it")*

A lone counter in the middle of the wash room, left over from the days when
that room was the entrance and it was the till. The question is the whole
argument against it: a prop the player cannot name is not decoration, it is a
question mark - and the floor it stood on is the only clear ground between the
kitchen gap and the sinks. Deleted.

### 14.8 Left standing on purpose

The fast-food service counter still sits on the kitchen's front edge, which
after the swap faces the wash room rather than the hall. It is decorative -
nothing routes a guest to it, and the tray station in the entrance carries the
self-service furniture - so it now reads as the kitchen's pass into the back of
house. Recorded rather than quietly moved: if it should face the guests, the
only frontage the kitchen has onto the entrance is the 1.4 m corner at
x = 5.2, z ∈ [4.0, 5.4].
