# 43 — A five-agent review: measurements that measure nothing

*13 September 2026.* At the user's request five agents audited the whole thing
end to end (core simulation, view layer, interface + localisation, tour +
measurement, document–code consistency). None of them changed a file; all of
them wrote findings with `file:line`. **Sixty findings, all of them worked
through.**

What came out was less a set of numbers than a *pattern*, and that pattern is
the subject of this document.

---

## 1. The most serious finding: the tour's 107 checks could not break anything

`Autopilot` wrote every check with `Debug.LogWarning` and **always** called
`Application.Quit(0)`. `run.ps1`'s fatal pattern was
`SORUNLAR:|Fatal Error|Aborting batchmode` — `HATA` was not on that list. On top
of that **there was not even a script** that ran the tour: `-lokanta-tour`
appeared in the whole project only inside `Autopilot.cs`.

So the sentence "the tour is 107/107 green" meant, in practice, **"nobody
looked"**.

| fix | where |
|---|---|
| `Application.Quit(kaldi > 0 ? 1 : 0)` | `Autopilot` |
| `HATA  :` added to the fatal pattern, report `-Context 0, 400` | `run.ps1` |
| `tour.ps1` written (build + N runs + summary) | new |
| `check.py --unity` lists the Unity auditors too | `check.py` |

**A crash and "checks left failing" are different things.** `Application.Quit(1)`
returned `0xC0000005` for a while in this Windows build — the exit code alone
cannot tell the two apart, and the reverse direction is more dangerous: a real
crash gets read as "checks left failing" and nobody looks. The tour now writes
`<output>/summary.txt` (passed / failed / unmeasured); if the file is missing,
it means the tour died before it finished.

**The cause of the crash was SOUND** and it took three attempts to find:

1. `Application.Quit` was being called from inside a coroutine — Unity enters
   its shutdown cleanup with a stack of coroutines still trying to run. Fixed
   (a flag is set and the call is made from `Update`), **the crash continued**.
2. The music's `OnAudioFilterRead` is called **from the audio thread** and can
   still be inside it while the clip is destroyed at shutdown. A `volatile` flag
   and `AudioSource.Stop()` were added, **the crash still continued**.
3. A flag is not enough because Unity **keeps calling** the callback: the
   component was switched off with `enabled = false` and
   `AudioListener.pause = true` was added. Exit code 0.

The difference between them: the first two attempts silenced the *inside* of the
callback, the third stopped the callback from **being called**.

**And here I built a hypothesis and was wrong.** After the sound fix two runs
closed cleanly with a zero code and the next run crashed with a non-zero code;
from that I concluded *"`Application.Quit(1)` is breaking the shutdown"* and
changed the tour so that it would not return a code. Of the next three runs
**two crashed again — with a zero exit code.** A pattern built out of three
observations fell apart on the fourth.

That is exactly the subject of this document, and I fell into the same trap:
*a pattern drawn from three observations is a guess, not a measurement.*

**Today's situation, as measured:** the crash is at shutdown, **intermittent**
(two runs out of three), outside managed code (Unity produces no crash dump) and
does not affect the tour's result. The sound fix reduced the frequency but did
not end it. It stands as an open item.

The decision to remove the exit code stayed **right** all the same, but for a
different reason: loading two separate things (how many checks were left failing
/ did the process close healthily) onto one number made both of them unreadable.
They were separated — the result is in `summary.txt`, and the exit code now says
only whether the shutdown was healthy. `tour.ps1` now reports the two
separately:

```
run 1 -> exit -1073741819
  summary: 112 passed, 0 failed, 3 unmeasured
  -> WARNING: the tour completed but the process closed with -1073741819
```

It mattered for the player too: a game that crashes on shutdown shows Windows'
"the program stopped working" window, and the game's pause menu has a "Save and
quit".

*But `summary.txt` stays: the exit code folds two different things into one
number, and that will happen again.*

---

## 2. A third result was needed: **UNMEASURED**

Most of the checks were in this shape:

```csharp
Note(toplam == 0 || oran > 0.6f, "klip ilerliyor");
```

If nothing happened at all, **green**. In two stability runs the line

```
ok   : Calisan personelin klibi ilerliyor (0%, 0 kare)
```

("the working staff's clip is advancing") stayed green exactly like that: the
work animation the user specifically asked for was never measured, and the tour
wrote 107/107.

`NoteIf(olctu, ok, ...)` produces a third result and at the end of the tour
**"how many checks could not be tested at all"** is printed. The vacuum is no
longer silent. The liveliness window does not exit without the work clip having
been measured either.

Three more fixes in the same family:

- **A violation is counted within the same frame.** "An eating table has a
  plate" was comparing the maxima of two *different* frames: an eating table
  without a plate on frame 10 was counted as "verified" by another table's plate
  on frame 500.
- **The rotation limit could not be broken.** The claim
  `YawOffset <= MaxYawLimit` was asking a value produced by
  `Mathf.Clamp(..., -MaxYaw, MaxYaw)` about **its own limit**. If `ApplyGesture`'s
  input were ignored entirely, this check *and* the "≈ 0" check that follows it
  would both have stayed green. It now asks "is it actually turning" first.
- **`TrayCount >= 0`** was always true for an `int` counter: zero information.
  It is now `TrayCount <= StaffCount`.

And two measurements went only into `Debug.Log`: `GameShot`'s scene budget (no
threshold) and `RoomLayout`'s 48 dp touch target (written out as a line of
**text**, never compared under any condition). Both are now tied to a threshold.

For the touch target the threshold had to be **two-tier**: the project had
measured and accepted going below 48 dp (docs/41), so making 48 red would mean
reporting a decision already taken as an error on every run. Below 48 warns,
below 40 is red.

And the moment it was wired up, something came out: the tool's copy of the strip
ratio had stayed at 40%, the real worst phase is **43.8%**. So the accepted
number was wrong too — the floor is not 45 but **42–43 dp**. The game did not
change, the measurement was fixed.

`GameShot`'s new third number (renderers outside batching) also refuted a guess:
the audit had estimated ~160, the measurement said **84**. The guess pointed in
the direction that would have justified an unnecessary optimisation.

---

## 3. The measurement window was eating the day it measured — for the third time

The tour never asked **where in the day** it opened the liveliness window. In two
consecutive runs the window opened at 11% and at 43%; in the run that opened at
43% the hall was **empty** and six checks went red at once — all of them were
showing the kitchen, the door, the street, and there was nothing wrong with any
of them.

There were two causes, both from the "the measurement consumes its own source"
family:

1. **A single screenshot** taken at x240 speed was spending a quarter of the day
   (half a second × 240 ≈ 120 sim-seconds). The pause came after the screenshot.
2. The "second table" wait was using its whole twenty seconds, still not finding
   the table, and carrying the day from 25% to 41%. The day cap was 60%.

The fix: the pause comes **before** the screenshot, the wait cap is 30%, and
**where the window opens is now a check**:

```
Note(gunBasi < 3500, "Canlilik penceresi gunun basinda basladi")
```

("the liveliness window started at the beginning of the day"). Without this line
the difference was invisible and next time it would have been read as "the hall
is empty" again.

Lowering the wait cap created **a new instability**: the "service produces a full
table" check was looking at the *instantaneous* occupancy on the last frame of
the wait, and the party could have got up on that frame. The claim is "service
**produces** a full table", that is, cumulative; it now asks for the highest
value the wait saw.

---

## 4. The core: two lines were **imitating** a guard

```csharp
int enFazla = _salon > 0 ? _salon - 0 : 0;   // == _salon, always
if (enFazla > _salon) enFazla = _salon;      // can never be true
```

The comment said "the whole hall crew cannot be put on the sink, or the game
locks itself up". The audit **correctly** found that both lines were no-ops —
but what was wrong belonged not to the code but to the **comment**:
`DispatchSalon` never puts server zero (the owner) on the sink, so there is no
such lock. On top of that, when there is a single hall worker on the first day,
putting them on the sink is **a legitimate decision**, and with a floor in place
the plate bottleneck could never be tried in the campaign's first days.

My first fix was to *enforce* the floor, and it broke the tour
(`Bulasik nobetine atama (0 -> 0)`, "assignment to dish duty 0 -> 0"). The
right fix was to delete the dead lines and pull the comment back to the truth.

**A finding's diagnosis can be right and the remedy it proposes wrong.**

### The bankruptcy ladder was not breaking the lock, it was repeating it forever

The third rung of the ladder wiped the debt and left the till at **zero**; but
`Buy` refuses with `cost > _cash` — with zero you cannot buy any ingredient
either. The evidence was in our own test's diagnostic output: from day 10 to day
40, **"0 parties, 4 tables" every single day**. Thirty-five days in a row without
a single customer, with rent and wages still being deducted and the ladder going
down again every week.

The ladder was written for exactly this soft lock.

The rung now leaves the shop **working**: a rescue allowance worth one day of
the recommended stock — not an invented number, `RecommendedRestock` already
knows the menu, the demand and the safety margin. It has a price: the rescue
value is written onto the year-end resilience axis.

### Downsizing was breaking the plate invariant

`Expand` **adds** as many clean plates as the difference; `Downsize` removed
nothing. The bug hid itself: at night `AdvanceToNextDay` rewrites the plates for
the tier, so the invariant was broken only for **that day** — and
`The_plate_count_is_conserved` never ran a downsize, so it never saw it. New
test: `A_shrinking_restaurant_loses_plates_too`.

### The crew decision is taken in the evening but affects **tomorrow**

`ReasonablePlayer.OnEvening` was asking `RequiredCrewToday()` and
`AdvanceToNextDay` comes **after** that call: on Friday evening a weekday crew
was set up and Saturday's peak was entered short-handed. The method's name was
right, **its caller was asking about the wrong day**. `RequiredCrewTomorrow()`
was added.

Its side effect was measured and recorded in docs/42: the intervention's gain
under pressure fell from +861 to +360. **A measurement that shrinks a number by
improving the thing it measures.**

### The balance tool's own flags

`--solve` was not in the `KnownFlags` list: the rent solver had been written and
**could never be run** (the tool said "Unknown flag" and exited with 2). `--tohum`
and `--strategy` were accepted but never read — someone typing `--strategy baskili`
got no error, got the full run, and thought they had filtered it. This was the
danger described in `CheckArgs`'s own comment, repeated **inside the list
itself**.

`Interventionist.Tried/Applied` was static and was never reset: the sum of the
two arms was printed on one line and the counters' reason for existing — "did an
intervention actually happen" per arm — could not be read.

And one warning was **mathematically impossible to fire**: `peopleLost` counted
PARTIES, `peopleServed` counted PEOPLE, and `Warnings()` compared the two
directly (2,042 people / 16 parties, threshold 408). The text said "a quarter",
the maths did "a fifth".

---

## 5. Tests comparing zero against zero

```
no dishwasher: plateless waiting 0 ticks, washed 15, served 7
with dishwasher: plateless waiting 0 ticks, washed 15, served 7
```

The two runs were **identical** and the only claim was `0 <= 0`: if the
dishwasher mechanic had been deleted entirely the test would still have stayed
green. In a quiet four-table shop there is nothing to measure.

The new test **builds pressure first** (twenty-five days of a healthily growing
shop), **asserts as a precondition** that the pressure really formed, and then
compares. What it measures changed too: not "plateless waiting" but **how many
times the hall dropped its work and ran to the sink** (`HallRushWashes`) — that
is where the dishwasher's whole value is, and the hall tending the dishes *in
idle time* is harmless anyway. Result: **31 runs against 3.**

Sibling finding: the "plate pressure is measured at the peak" test was measuring
*a dead shop* (see §4). The test bot now grows sensibly (one tier, if three times
the cost is in the till) and **that customers were served in the last ten days**
is asserted as a precondition.

`NoFloatTests` was also scanning only **signatures**; a
`double k = a / (double)b;` in a method body would have passed this guard without
trouble — and that is exactly what breaks determinism. A source scan was added
(with comments and string literals stripped out), and **how many files the scan
saw** is asserted too: if the path were wrong the loop would never turn and the
test would stay green with "no violations".

---

## 6. The interface: invisible numbers and Turkish left in the code

**The three numbers on the "Today" card were white on cream** — measured,
contrast **1.08:1**. That card is the only one the player looks at throughout
service. The `CheckRow` on the same card was doing it right (dark ink), so half
the card was legible and the fault was not caught by eye. It happened because the
colour choice **was left to the caller**; the decision is now inside
`Kit.CountRow` and dark ink on a light ground is mandatory.

The game's most-pressed button (`Cta`) was below the threshold too: white 19 dp
bold heading on `Go` at **2.54:1** (threshold 3:1) and an 82%-transparent
subtitle at ~2.2:1 (threshold 4.5:1) — and that subtitle is the one sentence
explaining an irreversible decision. The face went down to `GoDeep` (7.0:1) and
the transparency went away.

The loan button was **30 × 30 dp** — the project's own standard is 52, Google's
floor is 48 — and its meaning was given only by a `tooltip`; **on a touchscreen a
tooltip is never visible.** The visual stayed 30, the touch area became 52.

**Turkish embedded in the code: 42 strings.** `gen_loc.py`'s own docstring
*admitted* it ("about twenty-five interface strings are still embedded in the
code") but no check broke on it and the number had grown. The worst of them were
the service intervention feedbacks — the **only** feedback channel in the game's
main loop: somebody playing in English pressed a button and read
*"3. masaya çay ikram edildi"* ("tea was served to table 3"). Worse, `". masaya "`
is a Turkish ordinal suffix; even translated, the pattern would not have worked.

Three new checks were wired into `gen_loc.py` (`loc_tarama.py`):

| check | what it catches |
|---|---|
| constructor scan | when the first argument of `Theme.Text/Btn/Title/Head/Field` or `Toast` is a literal string |
| Turkish-letter scan | **every** literal string under `Ui/` carrying a Turkish-specific letter (including ones assembled piece by piece) |
| dead key scan | `ui.*` that is in the table and that no source asks for |

The third found **18 dead keys**: text maintained in two languages, translated,
never shown. Because `SCREEN_KEY` exempted the whole `ui.*` family from the
surplus check, this class was never visible.

Also:

- The position of the percent sign was embedded in the code (`"%" + n` → "Margin
  %62" in English). `Loc.Percent` looks at the culture's
  `PercentPositivePattern`.
- The same thing had two names: the gear button `ui.hud.menu` = "Menü" and the
  menu board `ui.morning.menu` = "Menü"; the gear is now "Duraklat" ("Pause").
  `ui.hud.angry` said a mood in Turkish ("Kızgın", "angry") and a count of people
  leaving in English ("Walkouts") — **the same number, two different things.**
- On the crisis chip the selection was given **by colour alone** (in red-green
  blindness both colours have similar luminance) — a marker and a border were
  added.
- The clipping measurement scanned only the **bottom strip** and returned zero if
  `_bottom == null`: "could not be measured" and "clean" gave the same result. The
  top strip and the cards are now scanned as well, and the unmeasurable case is
  −1.

---

## 7. The view: unowned meshes

`Modeler.Build` produces a `new Mesh()` on every call and **a Mesh does not
follow the GameObject when it is destroyed**. The worst path is the clothing:
every time the crew composition changes, all the staff are dressed again and
three or four meshes are created per person. Over a sixty-day campaign, thousands
of orphaned meshes.

The same class existed in the tint copies (`_tinted`): the dictionary was emptied
on every `Rebuild` but the copies were not destroyed.

The solution was to put ownership **on the object itself**: the `OwnedMesh`
component ties the mesh to the GameObject's lifetime, so `Clear()`, `Strip()`, a
scene change and a prefab deletion all go down the same path. (Disabled at
shutdown: Unity is already freeing everything and calling `Destroy` by hand at
that moment can crash the process.)

The others:

- **The lamp halo was being built at a fixed angle to the camera** and the comment
  next to it said "the camera's angle is fixed". That stopped being true when
  two-finger rotation was added: at ±35° the halo thins and the lantern body
  covers its middle. A silent degradation — it only shows up as "the night is a
  bit dim".
- **In play mode `Clear()` was leaving double geometry for one frame.** The
  visible price was one frame of double drawing; the insidious one was that
  `WallCount`, `WallsClear` and `AccessOk` **counted double** on that frame.
- `CookRoutine.Pan` was creating and destroying three objects per plate; the rest
  of the project pools everywhere and has written down why.
- `PanMaterial` could silently return `null` → a magenta pan on the device, no
  sign of it in the editor.
- `Figure.BendKnees` did a null check for three fields and not for `Legs`.
- The `_isKlip` measurement dictionary was not cleared in `Clear()`: new staff
  were being compared against the old index's progress value, and it **showed a
  bug that had been fixed as still broken**.
- `Wardrobe.Dress` had four silent early exits; they all print a warning now and
  the tour asks "staff dressed N/M".
- Dead code: `RoomDining`, `Find(string)`, `_props`, `_pots`, a twice-written
  `shadowCastingMode`, the empty-bodied `Pendants()` + `PendantY`, `Modeler`'s
  never-called bright branch and `ColorCount`, `Theme.Chip`, `Theme.MenuIcon`,
  `Icons.Clock`, `Icons.Trend`, and three labels whose text was produced on every
  `Tick` and then hidden unconditionally.

---

## 8. Document–code: the numbers fall behind the code

| document | said | reality |
|---|---|---|
| docs/41 | there are pendant lamps (§1 and §3) **and** they were removed (§7) | `Pendants()` is empty-bodied in the code |
| docs/41 | `[docs/25](25-mutfak-kimligi.md)` | that file does not exist; the right one is docs/10 |
| docs/38 | `Warm` 1.55 → **2.25** | 3.20 in the code |
| docs/40 | 524 keys | 572 |
| README | 88 tests | 225 |
| README | "two balance problems **open**" | docs/42 had closed both |
| docs/19 | the "measured" budget table | measured before two pieces were removed |
| MorningScreens | "4,300 coins, reputation drops a little" | docs/42 measured it: reputation **does not change** |

The last row is the most instructive: the "reputation drops" in the comment was
exactly the guess docs/42 §4 **measured and refuted**. When a sentence in a
document is corrected, the comment that copied it is not corrected — and the
comment outlives the document.

Same family: when the `WallColor` field was deleted, **its summary was not** —
the orphaned comment stood a while longer and moreover described an even older
value (0.20). Leaving the comment that describes a field when you delete the
field turns the comment from **documentation** into **legend**.

---

## 8b. A gap the audit did not see: it came out of looking at the renders

All five agents read the code and produced sixty findings, but one was missing:
**no check asked about the pots on the stove.** It was the user's request ("no
pots and pans are being put on the stove in the kitchen, there is no realistic
kitchen picture"), the code was written, the tour runs 112 checks — and there was
not a single line measuring that the pots existed. If the stove layout changed or
the `BuildPots` call were dropped, the kitchen would quietly empty again and only
the user would see it.

The way it was found is meaningful too: **by looking at the renders.** An audit
that reads source can say "that line is wrong", but to say "that line is missing
entirely" it needs to know what ought to be there — and that knowledge is not in
the code, it is in the request.

The same look fixed two more things, both by measuring:

- The hall **looked dark**; it was measured (median luma) and in the real build
  it came out at **78.9**, the street at 43.6 — that is, the inside is brighter
  than the outside, the situation docs/38 was aiming at. What was dark was the
  `GameShot` image, and that is the editor batch-mode artefact this project has
  already documented.
- The floor being dark slate is **not a regression but a palette decision**: grey
  tile in fast food, warm wood + rug in Turkish. Putting the two renders side by
  side also showed that the identity system had survived the `ClearTints`
  refactor.

In both cases my first reaction was "something has broken" and in both cases it
was wrong. **Looking with your eyes finds the question, measurement gives the
answer** — and the two do not substitute for each other.

---

## 8c. The store resolution opened one more class of bug

For the store screenshot the tour was run at 2.5× resolution (the same dp layout,
enlarged). That render showed two buttons **overlapping** — and the strip's two
existing measurements were both **green**: the height was within budget, no *text*
was clipped. Because instead of being clipped, it was overlapping.

The cause: in UI Toolkit the default for `flex-shrink` is **zero**, unlike CSS.
When a row does not fit, nothing narrows; the last items overflow and land on top
of the one before.

While writing the measurement **I looked at the wrong thing three times**, and all
three are the subject of this document:

| attempt | what it measured | why it stayed green |
|---|---|---|
| 1 | the **direct** children of `_bottom` | there is a single row container in there, no sibling to compare against |
| 2 | **all** elements, pairwise | `Kit.Cta`'s double arrow overlaps two triangles by 6 dp **on purpose**; icons are shape on shape too |
| 3 | only the **buttons** | — this is the right one: two buttons overlapping is always a bug, two triangles overlapping is an icon |

The third attempt named the bug:

```
ust uste binen dugme (servis/en): Attention > worst x Close the day (13 dp)
ust uste binen dugme (gun 40, 14 masa): Ilgi > sabirsiz x Veresiye ac (87 dp)
```

("overlapping button (service/en)…", "overlapping button (day 40, 14 tables)…")
The English one was happening **on the first day**, meaning every English player
saw it.

The fix did not come in one move and every step of it was steered by
measurement: once shrinking was allowed the overlap ended but **three buttons
started being clipped** (the bug turned from an invisible form into a measurable
one); the target suffix was taken out of two buttons' text and moved into its own
badge; "Mutfağı hızlandır" → "Hızlandır" ("Speed up the kitchen" → "Speed up");
the button padding went from 16 to 8 dp; "Veresiye aç" → "Veresiye" ("Open a tab"
→ "Tab"). The number dropped at every step: 3 → 3 → 2 → 0.

**Removing the target suffix was also a design fix:** the same information was
written on two buttons at once and the string's length depended on the
*language* — the strip had hit the same wall before, and the suffix had been cut
down from "en sabırsız" ("the most impatient") to a single word. The badge makes
the length language-independent.

### And then the measurement's own trap

The first version of the store image showed all the HUD numbers **empty**. There
is no bug in the game: when the screen is rebuilt the values are written on the
next `Tick()`, and this is a known, solved situation. But the image had been
taken **on the same frame** as `Refresh()` — and a screenshot is exactly one
single frame.

*A gap that lasts one frame is invisible to the player; it is completely visible
to a screenshot.*

---

## 8d. The last sweep before release: the moment the save breaks

Up to that point the tour's checks measured the game in its **working** state.
The last place looked at before release was the game in its **broken** state: if
the player's save cannot be read, what does the screen say?

The path was largely solid, and the reason for that was earlier tours:

* The write is genuinely atomic (`Flush(true)` + a single `File.Replace`), so
  there is no such thing as a half-written save.
* The version is **in the summary too**, so when `SaveVersion` increases the slot
  card does not go on showing a healthy campaign.
* An unreadable slot does not look silently "empty", it looks **broken** — a
  silent "empty" would mean the player overwrites it and really does lose it.
* If `LoadSlot` falls into the wrong branch, `ErrorScreen` opens; the game does
  not drop into an empty scene.

The one thing left was a **contradiction**: on a broken slot's card the badge at
the top said "broken save" while the disabled button right below it said "Empty".
The two halves of the same card were saying two different things, and this was
the one screen the player reads at the moment they cannot find their sixty-day
campaign. The disabled button now says `ui.slot.unloadable` ("Yüklenemiyor" /
"Cannot be loaded").

The real thing missing was not the label: **this path was never run.** The tour
always started with a clean slot, so the screen where the save breaks was the one
screen the sixty-day tour never visited. Five measurements were added to the tour
— a summary that is valid in form but has the wrong version is written into the
fourth slot (it is the version check itself that is tested, not the corrupt
file's `catch` branch) — and these are looked for:

| measurement | what it breaks |
|---|---|
| Continue on the main menu while a broken save exists | the player cannot see that their save exists |
| A broken slot looks broken | it looks silently "empty" and gets overwritten |
| Its button says "Cannot be loaded" | the two halves of the card contradict each other |
| A broken slot does not load | a pressable Continue button was left behind |
| A broken slot can be deleted | the player cannot rescue the slot |

The check count went from 119 to 124. The last two measurements could have been
green on their own in a vacuum (they would have passed even if the screen never
opened); they are not, because the first and third measurements prove that the
screen opened and that it is in **Load mode**.

*It is not enough for an error path to work properly; the error path also has to
speak consistently within itself, and that has to be MEASURED.*

---

## 8e. Two names, one picture

While listing the files coming out of the store tour, `12-kampanya-sonu.png` and
`13-degerlendirme.png` were **the same number of bytes**. Two different screens
can produce images of the same size — but `md5sum` showed the two were
**byte-for-byte identical**.

The cause is simple and so is the fix: the moment the campaign ends, the year-end
evaluation is already on top of the stack. So there is **no** separate frame
called "end of campaign"; `Shot("12-kampanya-sonu")` was capturing the evaluation
screen a second time. Because the file was written, nothing warned.

This is the same class as the rest of the document: **an image's name is the only
record of what it shows, and it can be wrong.** The tour was not auditing its own
screenshots with its own eyes.

Now it does. `RecordShot` writes an FNV-1a digest of every PNG into a ledger and
at the end of the tour a single check asks: was the same picture written under a
second name? If it was, it names **the pair** (`12-kampanya-sonu =
13-degerlendirme`) instead of saying "1 duplicate".

**When I first wrote the liveliness condition I got it wrong again.** It was
`_shot >= 2`: even if `RecordShot` were never called, `_shotDup` would stay null
and the check would come back green — it was measuring that a screenshot had been
taken, not that the measurement itself had run. The condition is now
`_shotHash.Count >= 2`, that is, **two distinct digests in the ledger**. The
measured result: `19 goruntu, 19 ayri resim` ("19 images, 19 distinct pictures").
With the removed image it would have been 20/19 — the check really can be broken.


---

## 8f. What 126 checks could not see: the picture itself

The store tour returned all 126 checks green. Then I **looked** at the PNG that
came out: three notice bubbles were piled up in the dead centre of the hall —
*"Soruldu ama yok: Musakka / Nohut / Kıymalı Pide"* ("Asked for but not
available: …") — and the restaurant itself was not visible. In the image destined
for the store listing, the game was buried under three negative sentences
overlaid on it.

In the run before that there had been no bubbles at all. So the bug was not
"always broken" but **nondeterministic**: the same code produced two different
store images and which one came out depended on the simulation at that moment.

The bubbles live for 3.5 seconds. The capture now waits for a frame where
`NoticeCount == 0` (at most 12 s) and writes that as a check:
`Magaza goruntusunde salonun ustu acik (0 balon)` ("in the store image the hall
is unobstructed (0 bubbles)"). The measurement is taken **after** the image is
captured — measuring first would miss a bubble that drops in one of the
intervening frames.

Result: day 40, 10 of 14 tables occupied, revenue 1,188, satisfaction 78.5,
reputation 96.8/100, and the hall unobstructed.

The real subject of this section is not the bubbles. **A check answers only the
question it asks.** The tour was measuring that the image was taken, that the HUD
was full, that the buttons did not overlap, that the text was not clipped — none
of them asked "does this picture show the game". That question was asked for the
first time by a human.


---

## 8g. Light and shadow: the figures were standing in mid-air

The user's question: "is there a problem with the in-game light and shadow". There
was.

**First the map had to be drawn — who casts a shadow in this game?**

| source | casts | receives |
|---|---|---|
| everything `Modeler` produces (floor covering, wall, counter, table, chair) | **no** | **no** |
| room floors (`CreatePrimitive` cube) | yes | yes |
| glass partitions, lamp posts, badges | no (each with a reason) | no |
| characters (Kenney prefab, default setting) | **yes** | yes |

So **the only shadow in the game is the character shadow.** `Modeler.Build`
switches it off on every mesh it produces and — in a file where every decision is
explained in a paragraph — **does not write a single line of reasoning**. That on
its own is not a bug (only moving things casting shadows is a defensible style),
but its consequence was this: if a figure's shadow is broken, there is no other
shadow beside it to compare against, so the breakage reads as "style".

**The defect.** Zoom into the screenshot and the figure's shadow was **detached
from its feet**: a large, blurry blot falling a fraction of the body's height
away. While the chair next to it sat flush on the floor, the human looked as if
it were hovering.

The arithmetic: shadow distance 45 m, map 1024 → **8.8 cm/texel**. Unity's default
`m_ShadowNormalBias` is 1.0 and it shifts the sample by **one texel** along the
normal — on a 1.10 m figure that is 8% of the body's height. Classic
*peter-panning*.

**Why no check caught it.** `check_urp.py` compares the `.asset` against
`ProjectSetup.cs` — but only the fields `ProjectSetup` **sets**.
`m_ShadowDepthBias` and `m_ShadowNormalBias` were not on that list at all, so they
were outside the audit's scope and had stayed at Unity's defaults. *An auditor
cannot see more than the list it is given — and it cannot tell you that the list
is incomplete.*

**The fix.** Map 1024 → 2048 (texel 4.4 cm), bias 1.0/1.0 → 0.6/0.4. Both levers
depend on the texel size, so they work together: the offset drops from 8.8 cm to
~1.8 cm. 2048 is cheap here, because there is almost nothing in the shadow pass —
the cost is clearing the map and 8 MB of memory, and the rasterised area is a
handful of small figures.

Both values were written **in two places at once** (the copy trap the document
itself warns about: `LokantaURP.asset` + `ProjectSetup.cs`) and they are audited
now — the number of compared fields went from 12 to **14**.

The measured result: the shadow stuck to the feet, its edges sharpened, no shadow
acne appeared indoors, tour 128/0.

**Two things that were measured but are not defects.** The same sweep also
measured pixel brightness:

| | kitchen | dining hall | street |
|---|---|---|---|
| evening | 79.3 | 48.6 | 47.3 |
| start of service | 57.5 | 46.0 | 145.3 |

The evening fix (`DayLight`) lit the kitchen, but the dining hall is still **the
same** brightness as the darkened street; in daytime the inside is a third of the
pavement. Both are readable, so the light was not rebalanced — but this went on
record: **the rule "the inside should be brighter than the outside" holds in the
kitchen and does not hold in the dining hall.** The tour's check on this subject
looks at the ambient light *setting*, not at the pixel on the screen.


---

## 8h. Chamfer: "the models look too sharp"

The user's request: let the furniture and the props have slightly more rounded
edges.

`Modeler.BoxAt` built the box out of six separate faces and its comment wrote
this up as a deliberate choice ("each with its OWN corners: flat shading, sharp
edge"). The right way to soften it is a **chamfer**: 6 inset faces + 12 edge strips
+ 8 corner triangles, that is **44 triangles instead of 12**.

**At this distance the chamfer's job is not to change the silhouette.** A 4.5 cm
chamfer is one or two pixels on screen in the pulled-back view. Its job is to
**break the light** along the edge: because the chamfer face sits at a different
angle from its two neighbours, a thin light strip appears along every edge. In a
flat-shaded scene, that strip is what gives softness.

**The first time I put the gate on the wrong axis.** The condition was "thinnest
edge ≥ 5 cm" and it filtered out the game's *most visible* piece: a tabletop is
0.80 × **0.04** × 0.80, so its thinnest edge is 4 cm. Tabletop, counter top, chair
seat, shelf — all flat slabs, all filtered out. The measurement said so too: the
triangle count rose only **12%**, because the chamfer had never touched the
furniture that matters.

Protecting the thin axis is not the gate's job anyway — the chamfer is capped at
28% of each axis, so on a 4 cm top the vertical chamfer comes down to 1.1 cm by
itself. The right rule is **how many edges are large**: pieces with two edges
≥ 10 cm (slabs and blocks) get chamfered, single-long-axis rods (railing batten,
table leg, post) go on being filtered out — on them a chamfer stays invisible but
would cost 44 triangles.

| | triangles (14 tables) | renderers |
|---|---:|---:|
| no chamfer | 37,182 | 254 |
| wrong gate (thinnest edge) | 41,854 | 254 |
| right gate (two large edges) | **44,638** | 254 |

The budget is 80 thousand; the renderer count does not change, because the chamfer
does not produce new objects, it adds triangles to the existing mesh. Increasing
the chamfer's **size** does not change the triangle count at all — the gate looks
at the dimensions, not at the chamfer itself.

**The winding direction is not written by hand, it is computed.** Writing the
winding of twenty-six surfaces correctly by hand would have meant one of them
coming out reversed and producing an inward-facing surface — and a reversed
surface becomes **invisible** without raising any error, so a hole would have
opened silently. Because the shape is convex and its centre is at the origin in
local space, the criterion is simple: the face's normal must point outwards from
its own centre, and if it does not, the order is reversed.

### And while the chamfer was being fixed, the store frame broke

The new frame was showing **6/14 tables**; but the capture waits for "half of them
to be full". The cause was the bubble wait I added in the previous section: the
two waits stood **back to back** and while the second was waiting, the first was
being undone — the guests got up and the frame fell below the threshold. **The
wait was consuming the thing it was waiting for** (the very same as the third
pattern in §9 of this document).

The three conditions are now looked for in a single loop, on the same frame; and
"the hall is full" is also written as a separate check after the image is taken.
Otherwise this would break silently and nobody would say so. Result: 9/14 tables,
revenue 1,377, no bubbles.


---

## 9. The pattern

The great majority of the sixty findings collapse into a single sentence:

> **A line that says a thing is measured does not prove that the thing is
> measured.**

It came out in three forms:

1. **The measurement cannot break anything** — the tour's exit code, the
   threshold-less `Debug.Log` in `GameShot`, the 48 dp written out as text and
   never compared, `--solve` being impossible to run.
2. **The measurement is green in a vacuum** — `Note(toplam == 0 || ...)`,
   `TrayCount >= 0`, `0 <= 0`, asking `Mathf.Clamp` about its own limit,
   comparing a threshold against a unit that cannot break it.
3. **The measurement consumes the source it measures** — the liveliness window,
   the second-table wait, the screenshot taken at x240.

And then there is the comment itself: `_salon - 0`, "reputation drops", "other
screens read it", "Excess accumulated time is THROWN AWAY", "the camera's angle is
fixed" — every one of them a sentence describing something the code does not do.
**A comment that describes what the code does not do is not a comment but
misinformation**, because it stops the next reader from checking.

It was deliberate that none of the five agents changed a single file, and it
worked: a finding's **diagnosis** and its **remedy** are separate jobs, and as §4
showed, the diagnosis can be right while the remedy is wrong.
