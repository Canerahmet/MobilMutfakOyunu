# 37 — Four agent reviews and the gaps that were closed

12 September 2026. Four independent reviews were run: **simulation/economy**, **view/animation**, **player experience**, **release readiness**. All of them read the code, none of them changed a file; every finding was verified in the code before it was acted on.

This document records not the findings but **what was closed**: what was wrong, how it was measured, what was done.

---

## 1. CRITICAL — The price had no ceiling: unlimited profit

**How it was found:** the review wrote a probe against `Lokanta.Core` and took 60-day runs with 5 seeds.

| scenario | till | reputation | satisfaction | served |
|---|---:|---:|---:|---:|
| baseline | 16,894 | 33.4 | 68.91 | 856 |
| extras x200 | 343,154 | 12.5 | 65.12 | 666 |
| extras x2000 | **3,368,594** | **12.5** | **65.12** | **666** |

The last two rows are the same: the price goes up tenfold, the till goes up 9.8-fold, **the reputation and the satisfaction do not budge**. The cause: satisfaction is clamped into `[0, 10000]` and demand never sees the price — so past the point where the price of an item drives the satisfaction of the customer buying it to zero, **every extra zero is free**.

**The fix:** `overpriceCeilingBp = 25000` (2.5 times the market) and `SetPrice` rejects a command that exceeds it (reason code 13). The floor already existed (`underpriceFloorBp`); the missing ceiling was a symmetry error.

## 2. CRITICAL — With the combo open, the price of the side and the drink never entered satisfaction at all

With a combo on, `WeightedPriceDiffBp` was measuring **only the main course**. Its reasoning was right ("the combo carries its own discount, the player should not be punished for opening the signature mechanic") but its implementation was wrong: the combo receipt adds up the price of **three items** and the combo is forced on every party.

| scenario | till | satisfaction |
|---|---:|---:|
| combo, normal price | 16,565 | 67.66 |
| combo, extras x2000 | **24,829,724** | **66.80** |

**1470 times** the baseline till, with the satisfaction almost undamaged.

**The fix:** with a combo the whole receipt is measured too — the market total of the three items x the combo discount is compared against the player's combo price. The discount is not punished, the inflation is measured.

**The second half:** `ComboSellable()` was not looking at `CanMake`. Measured: even when the ingredients for the side and the drink were never bought at all, **325 sides + 318 drinks** were sold in 24 days — at full price, at zero ingredient cost. The combo was a door to free production without stock.

### Why the balance tool did not catch it

The gap sat exactly **at the intersection of two bots**: `pahali_ekstra` raises the price of the extras but does not open the combo; `imzaci` opens the combo but does not touch the price. There was no bot that combined the two.

Now there is: **`kombo_sismesi`**. And both bots write a price just **under** the ceiling — a bot that writes above the ceiling would be rejected and would quietly turn into "the reasonable player", that is, it would test nothing.

After the fix (5 seeds, 60 days, fast food):

| bot | before | after |
|---|---:|---:|
| `makul` | 25,035 | 28,689 |
| `pahali_ekstra` | 25,035 (no effect) | **1,886, reputation 0, in debt on day 56** |
| `kombo_sismesi` | 24,829,724 | **2,149, reputation 0** |

---

## 3. HIGH — While the food was cooking the customer's patience never ran down (87%)

When `DispatchKitchen` started a job it marked the party with `_pInTask`, and `DrainRateBp` returned **0** on that flag. But `TimingConfig`'s own comment writes the intent: *"it never drains while the waiter is at the table"* — so the flag was supposed to mean "the waiter is at the table". **The cook being at the stove is not attending to the customer; the customer is waiting precisely then.**

The measurement (every table on every tick, 30 days x 3 seeds):

| cuisine | "waiting for food" ticks | patience running | **frozen** |
|---|---:|---:|---:|
| fastfood | 236,794 | 13.3% | **86.7%** |
| turk | 239,631 | 12.3% | **87.7%** |

So `DrainWaitingFoodBp = 3500` was effectively working as ~465 — 7.5 times too weak. The price of that: `prepMs`, the equipment tier and the combo's kitchen load were **almost invisible** on the customer's side. The tension docs/27 Decision D promised was there with nothing behind it.

**The fix:** kitchen work moved to a separate flag (`_pKitchenTask`). Task dispatch reads both; only **patience** was separated out.

### The second hole the fix opened

On the first run every reference bot went bankrupt — 78 people served in 60 days (1920 before). The cause was not patience but the **satisfaction penalty**: `_pWaitedMs` was counting every tick **in full**, and that was harmless as long as patience was frozen. Once the freeze was lifted, a minute of cooking saturated the `(waited/patience) x 6000` penalty and everybody left with zero satisfaction.

**The right answer is the same measure:** the waiting is counted weighted by the degree of irritation too. Waiting for a table counts in full, waiting for food counts less — both in the same unit.

`drainWaitingFoodBp` 3500 -> **500**: it keeps the old *effect* but it is now **tied** to the cooking time.

### Recalibration

`calibrate.py` ran the whole chain (solve -> model -> export -> simulation) and found the fixed point again:

| | before | after |
|---|---|---|
| realisation rate | 6500 | **7000** |
| penalty (32 seeds) | 5 | **6** |
| rents | 650/1550/2250/4000 | 850/1950/2900/5000 |

### The equipment test's measure changed

`A_slot_upgrade_makes_a_measurable_difference` went red: parties served 96 ≤ 98. But the reason was that the test was right — **the mechanism changed**. In a four-table shop the party count is noise; the equipment's payoff now shows up exactly where docs/27 promised it would:

| | without equipment | with equipment |
|---|---:|---:|
| average satisfaction | 83.3 | **88.8** |
| revenue | 10,099 | **10,258** |

The test now measures satisfaction and revenue, and for the party count it only says "it did not fall".

---

## 4. Other simulation fixes

- **The name of a member of staff who resigned was passing to the staff who stayed.** `RemoveStaff` shifts xp/trait/morale but **was not shifting the name array**, and its only caller is a resignation. Because the name is written to the save, the bug was permanent. The reason this mechanic exists is *"losing a number is abstract, an employee whose name you know resigning is concrete"* — when the names lie the mechanic turns inside out.
- **There was no save version migration.** On a missing key the reader **threw an exception**: after release, a single balance patch would have made every player's sixty-day campaign "corrupt". `IStateReader.Has(key)` was added and the rule was written down: *new fields are read through `Has`, and left at their default if absent.* Version 15; it was also recorded that 9–14 are undocumented.

---

## 5. The view layer

| finding | measure | fix |
|---|---|---|
| `StreetLife` touches a destroyed object | an exception per frame on every return to the main menu | `Clear()` clears the street too + a body check |
| `CookRoutine` **leaks 3 materials per plate** + `Shader.Find` | thousands of material instances in 60 days | a shared material + `MaterialPropertyBlock` |
| a crew change only looks at the **total** | on a waiter<->cook swap the new cook never cooks | the stamp is `Cooks x 1000 + Hall` |
| pausing does not stop the kitchen or the street | `Mathf.Max(0.25f, GameSpeed)` was swallowing the zero | an early exit when the speed is zero |
| a teleporting cook says "I have arrived" | the pan was being put on an empty stove | arrival **by distance** + a stage timeout + `Cancel` cuts the walk too |
| 56 chair transform writes per frame | the value does not change until the table fills | written only when it changes |
| `MovingCount` allocates an array on every read | the tour reads it for 1500 frames | counted from the lists already in hand |
| `Quality.Restore()` is never called | in the editor `renderScale` stays at 1.0 and **is written to disk** | hooked to `OnDisable` |
| `Destroy` in editor mode | the colliding box stays in the scene | the pattern was fixed in four places |

### Opaque in the build, transparent in the editor

This is the most important one: **the walls, the door leaves and the oven glass were being drawn opaque in the real build.** URP does not put the shader variant of the transparent pass into the build unless there is an **asset** that refers to it; a material built at runtime with `new Material(...)` does not get that variant.

There is no sign of it in the editor. The same class of bug had happened before with URP/Unlit (the badges).

**The fix:** `ArtPrefabs` now produces the `custom_wall`, `custom_door`, `custom_glass` and `custom_lightpool` materials as **.mat assets** and `BuildGameScene` binds them into the scene. If one is missing, `Debug.LogError` — no silent fallback.

---

## 6. Player experience

### The crisis is on screen now

The game's one promise is *"during service you only intervene in crises"*, and the only signal for a crisis was a ~50x8 dp badge above the table. The "Walkouts 1" at the top **never changes colour under any condition**; no sound, no vibration. The player could not see who to spend their four interventions on.

A **crisis strip** was added: a chip for each table whose patience has dropped below the threshold ("Table 3 18%"), sorted by the least patient, at most five. Touching a chip **selects** that table — which also solves the target-selection problem. When a new crisis appears, a one-off warning sound.

The strip **is not built at all when there is no crisis**: its appearing is itself the signal.

> **The check is not circular.** Asking "did the crisis appear" would be self-confirming. The inference is this: if a party left angry at the end of the day, that party must have dropped into the critical band at some point — so the strip should have been on screen at that moment.

### The measurement window was on the wrong screen

The tour was running at **1280x720**; the target phone is **873x393 dp**. So every width measurement, including the single window through which the team sees the interface, was being taken **on a screen the player never sees**.

The tour now runs at 873x393. On the first run it was confirmed that **a single dish card** was visible on the menu screen — in a list of 32 dishes.

**And the hints:** the tour was not clearing `PlayerPrefs`, so the screenshots taken were not the game's *shown to a new player* state but its *already played once* state. The tour now calls `Hints.Reset()` and asks "is there a hint on the morning of day 1". (`Cap` was missing from the `Hints.All` array — "show the hints again" was not bringing it back.)

### The menu list

It was in raw content order: on day one, one open dish and immediately below it **six locked rows in a row**. Three changes, zero new mechanics:

1. **Order:** On the menu -> Open -> Coming soon (two of them) -> Locked (N) collapsed.
2. **Compression:** only the dish that is touched opens as a card; the others are a single row. **The row itself is the button** — it comes down to 44 dp and the touch target becomes an 873 dp strip.
3. **Heading:** each section says at the top what it is.

Four dishes fit on the screen instead of one.

### The result of the decision is visible

The game had **no day-against-previous-day comparison at all**. A player who changed the price on the fifth day saw a number on the sixth and did not know whether it was good or bad. When the learning stops there is no reason left to come back for a second day.

- In the evening strip, **the difference against yesterday** beside the profit and the people served.
- **The day report became primary**, "Next day" secondary. In a sixty-day campaign the player pressed orange sixty times and never saw the waste, the demoralised staff or the regulars' scenes.

### A preparation summary before opening the service

The morning's one **irreversible** decision was the widest and only orange button on the screen, and it worked with one tap and no confirmation. Now there is a line above it — "6 dishes on the menu · 6 dishes in stock · 1 person in the kitchen" — and whichever item is missing is **red**. When there is red, the first tap asks and the second opens.

The confirmation only when something is missing: asking "are you sure" every morning would turn the confirmation into a reflex nobody reads.

### The sound control

10 boxes x 52 dp base width does not fit a 480 dp panel, the row wrapped, and because of `flexGrow` the two boxes underneath turned into two empty boxes half a screen wide — in a screenshot it reads as a **bug**. **5 steps**: it fits and it is enough.

---

## 7. Release readiness

| finding | status |
|---|---|
| **Licences** | **NO** release blocker. The Kenney packs are CC0 1.0, Rubik is SIL OFL 1.1; the attribution requirement is met (`Resources/licenses/` goes into the build and the Licences screen shows the full text) |
| **Unsigned AAB** | The package in hand was signed **with the debug key**. The AAB is now **not produced unsigned** — the build stops instead of warning and carrying on. For the APK a warning is enough (to sideload onto a device and try it) |
| **Symbols** | `androidCreateSymbols` is **Public**. Crash reports without symbols show bare addresses, and Android vitals is a **store visibility threshold** |
| **Content sync** | `SyncContent.Run()` is now part of the build. A build taken with a stale copy would go to the store with **different balance numbers** from the ones that were tested, and no test would break |
| **Identity conflict** | `ProjectSetup` and `BuildPlayer` were writing the package name, the company, the orientation and the target SDK in **opposite directions**. The package name locks forever on the first upload. One source: `BuildPlayer.Package/Company/Product` |
| **Low memory** | `Application.lowMemory` -> save. Android can kill the app while it is in the foreground too; a day's play would go that way |
| **Splash screen** | The logo-less blank dark screen was turned off |
| **Licence audit** | **New:** `tools/check_licenses.py`. In every folder under Art/ it looks for `License.txt`, a recognised licence and a row in the ATTRIBUTION ledger; if `Resources/audio/` fills up it demands the same of the sounds |

### Why the licence audit is a machine's job

The project's own log says *"the biggest risk: an asset produced with the free tier of an AI tool slipping through"*. But there was **zero friction** on that path: dropping an `.ogg` into a folder needs no code change, no licence text, and triggers no audit. Google Play will not catch this; the copyright holder will.

On its first run the audit found **three gaps** (no licence in the Icon folder, two packs not matching in the attribution ledger) — so it proved its own testability on its own first pass. A machine-readable **folder mapping** table was added to ATTRIBUTION.md.

---

## Verification

| what | result |
|---|---|
| `tools/check.py` | **13/13** (the licence audit was added) |
| core tests | 220 |
| the automatic tour, **873x393, a real phone** | **71/71**, in two consecutive runs |
| the placement audit, both cuisines | 0 overlaps, 15/15 facing the right way |
| balance, 5 seeds | `makul` 28,689 · `planci` grows · the exploit bots go bankrupt |
| calibration, 32 seeds | realisation 7000, penalty 6 |
| APK | 0 warnings |
| AAB | **not produced unsigned** (a deliberate gate) |

## What is left before release — the user's job

These depend on accounts, not on code:

1. Generate an **upload key** (`keytool`) and set the four environment variables. If the key is lost the application can never be updated again — **back it up in two separate places**.
2. A **privacy policy** (one paragraph is enough: no data is collected — there is no user-visible permission in the AAB manifest, analytics/ads/crash reporting are off, there is not a single network call).
3. The **Data Safety form**: "not collected".
4. The **IARC questionnaire**: **"not directed at children"** must be selected — if "children" gets ticked because of the cartoon style, the Families Policy obligations open up and they are hard to undo.

## Not decided — a design question

**The game is Turkish only** but `docs/21` D5 says English soft launch and a global release. One of the two has to be chosen: either `en.json` is added and a device-language key is put into `Loc.Load`, or D5 is rewritten as "TR closed test". Because `gen_loc.py` derives the keys from the content, a second language is cheap **now** — before the store texts and the screenshots are produced.
