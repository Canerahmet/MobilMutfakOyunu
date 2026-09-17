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
| **The screenshot aspect ratio** | 2183 × 983 = **2.2204 : 1**, which breaches Play's long-standing "no side more than twice the other" rule (2183 > 2 × 983 by 217 px) | The fix is to composite into a 1920 × 1080 canvas rather than re-shoot — re-shooting at 16:9 changes the dp layout and invalidates every measurement in [41](41-ui-and-venue.md). Confirm with one upload attempt first |
| ~~The store screenshots are in Turkish~~ | **Done.** `tour.ps1 -Lang en`. It uses `Loc.UseLanguage`, which applies without persisting — a tour run must not leave the developer's own language changed behind it — and with no flag nothing happens at all, so the measurement runs are untouched | The store copy goes to `render/store/<cuisine>/<lang>/`, so five listings can have five sets |
| ~~The back wall stands over a hole of sky~~ | **Done.** At tier 1 the open rooms bound a 13.4 × 9.6 m box and Hall2 (5.0 × 5.2 m) is closed, so a rectangle of sky sat inside the building with a 2.6 m wall over it. Closed rooms are now built as a bare concrete **shell**: no wall, no door, no furniture, and the collider destroyed so it is genuinely not touchable rather than merely untagged | It costs nothing in framing — `CameraFit.OpenBounds` is built from the open rooms and the street — and it shows the player the space expansion buys, which the campaign's main progression path had never had on screen |
| ~~The table badge is ~4 dp tall~~ | **Half done.** Thickness 0.18 → 0.36, and `LeftAngry` no longer hides the badge — it shows a full-width dark red mark, darker than the "running out" red so that *about to go* and *gone* do not read alike. Patience is zero by then, so the meter would have drawn nothing even after it stopped being hidden | The badge is legible now, not solved. [31](31-rooms-and-camera.md) §8.1 decided the real answer — in the OVERVIEW the badge belongs on the room, not the table — and that is still unbuilt |
| ~~The kitchen is a lamp, not a meter~~ | **Done.** How many hobs are lit is the load; their colour is whether the station is coping — blue at or under capacity, amber over, red at twice. Colour alone would not carry it: the stove is a handful of pixels at the default camera, so the count of lit hobs is the channel that survives at that size | `Simulation.StationSlotCount` was added because a load without a capacity has no scale: three plates on a one-slot hob is a jam, three on a four-slot range is a quiet morning |
| **The backdrop is still one flat colour** | It is darker and more neutral now, so it stops fighting the building - but 37% of the hero frame is still a single value with no sky, no horizon and nothing behind the restaurant. `DayLight` already drives that colour through four day stops, so a two-stop gradient quad could be driven from the same curve | The reasoning on record is [19](19-technical-setup.md)'s fill budget, and it is worth testing rather than inheriting: a quad at the far plane replaces the camera clear instead of stacking on it, so it adds no overdraw. `CameraFit.OpenBounds` is built from the room plan and the street only, so decor placed BEHIND the building does not push the camera back either - which means a row of neighbouring facades is free in framing terms |
| **No colour grading** | `postProcessData: {fileID: 0}`, `m_RendererFeatures: []`, an untouched default volume profile. [19](19-technical-setup.md) already permits *"at most a light colour grading table"* — this is an unimplemented allowance, not a rejected idea. In Unity 6.3 URP, tonemapping, colour adjustments, white balance, split toning and vignette run on-tile; bloom and depth of field do not, and bloom has a published Android benchmark taking a frame from 25 ms to 60.5 ms | Wants a device to verify. There is no test phone |
| **The main menu has no game in it** | 80.9% of that frame is one flat colour: a near-black field, an orange wordmark and three grey pills. It is the first thing a store visitor sees and the first thing a player opens | The build already renders the hall, and `08-pause` proves a scrim over the live scene composites correctly |
| **The customers are twelve stock prefabs** | The staff get a wardrobe and the furniture gets a per-cuisine material copy; the customers get neither, in both cuisines | The one-material-copy trick already ships for furniture and costs no draw calls |
| ~~No UI layout check exists~~ | **Done, and it earned its place on the first run** — see §9b. It walks the whole visible tree of whatever screen the tour is on and reports the worst offender by name | It did not need a mutation to prove it fires: it went red immediately on the four screens §6 predicted, and red again when the first fix moved the overflow sideways |

---

## What the reviewers said not to break

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
