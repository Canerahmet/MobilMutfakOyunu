# Technical Setup

**Last updated:** 9 September 2026
**Register items:** B4 version and render pipeline, B5 performance targets, B6 save file format, B7 input action map
**Status:** Written, awaiting decision

---

## B4. Unity version and render pipeline

### Version: Unity 6.3 LTS

> **Note, 9 September 2026.** The version installed on the machine is 6000.5.8f1, that is the Unity 6.5 tech stream, not LTS. **Decision 9 September 2026:** 6.3 LTS will be installed from the Hub and the project opened with it. See [22-answers-and-direction.md](22-answers-and-direction.md) §2. Also, the first release is Android only; the iOS and Metal rows apply once there is a Mac.

| Why | Explanation |
|---|---|
| LTS is mandatory | We are making a long-lived game. A live game is not kept on an experimental version |
| Support period | Supported until December 2027. Covers development plus the first live year |
| Afterwards | Unity 6.7 LTS arrives at the end of 2026. The move is assessed after the first release ships |

**The rule: the version does not change once production has started.** Only security and store-compatibility patches are taken.

### Render pipeline: URP

| Option | Decision |
|---|---|
| **URP** | ✅ Chosen. Designed for mobile, more than enough for a low-poly stylised scene |
| Built-in | No. Unity's direction of development is no longer there |
| HDRP | No. For desktop and console, unusable on mobile |

### Settings

| Setting | Value | Reasoning |
|---|---|---|
| Colour space | Linear | Needed for soft light and shadow |
| Android graphics API | Vulkan first, OpenGL ES 3.1 as fallback | Coverage of older devices |
| iOS graphics API | Metal | The only option |
| Texture compression | ASTC | The standard on mobile |
| Additional light shadows | Off | Saves GPU and memory |
| Store Actions | Auto or Discard | Saves bandwidth on low-end devices |
| SRP Batcher | On | Lowers the CPU cost of draw calls |
| **GPU Resident Drawer** | **On** | A Unity 6 feature. In object-dense scenes it can halve the CPU cost of rendering |

**The GPU Resident Drawer matters especially for us.** The restaurant scene is object-dense: tables, chairs, plates, customers, decor. Exactly the case that feature targets.

### Lighting

- The light on static objects is **baked**. Walls, floor, furniture.
- Only one directional light is real-time.
- Characters are lit by light probes.
- No heavy post-processing. At most a light colour grading table.
  **Spent, on 17 September 2026:** contrast +8, saturation +6, white balance
  +6, and nothing else. `tools/check_grade.py` enforces the 'light' half of
  that sentence - it refuses nine off-tile effects by name, because every
  override already sits in the shared volume profile with its override state
  enabled, so turning bloom on is typing a number rather than adding a
  feature. The frame cost on a device is still unmeasured; see
  [21](21-business-and-release.md).

The warm evening light that carries the cuisine's identity is provided by baked light and colour grading. There is no need for real-time shadows.

### Packages

| Package | Job |
|---|---|
| Input System | Two input schemes |
| Localization | Turkish and English |
| Addressables | Content loading and download size management |
| TextMeshPro | Type with Turkish character support |
| Unity IAP | The cuisine purchase |
| Analytics or Firebase | Behind a port |

---

## B5. Performance targets

### Supported devices

| Platform | Minimum |
|---|---|
| Android | Android 10, 3 GB RAM, Vulkan or OpenGL ES 3.1 |
| iOS | iOS 16, iPhone SE 2nd generation and above |

Devices below this threshold are not supported. Widening the coverage slows everything down, and that segment is small anyway.

### Frame rate

| Device class | Target |
|---|---|
| Mid and high end | 60 fps |
| The lowest supported | 30 fps **guaranteed** |

30 fps is the guaranteed floor. Because the game is not a reflex game, 30 fps does not break playability. But drops do, so the target is a stable frame rate.

### Budgets

| Item | Target | On the lowest device |
|---|---|---|
| Draw calls | Under 100 per frame | Under 60 |
| Visible triangles | Under 100 thousand | Under 60 thousand |
| Memory | Under 700 MB | Under 500 MB |
| Download size | **Under 200 MB** | The same |
| Continuous play | 30 minutes with no thermal throttling | The same |

**200 MB is a critical threshold.** Both Google Play and the App Store show a warning when an app above that size is downloaded over cellular data. Seeing that warning lowers the install rate.

To stay under it: the cuisine assets are separated with Addressables and pulled down when a purchased cuisine is downloaded. That way the first download contains only fast food.

### Measured (13 September 2026)

Measured after the scene was redesigned (the back wall, the floor pattern, the
service counter, the terrace, food on the tables, pots on the stove). The
previous table had been taken **before** the pendant lamps and the front-row
counters were removed, and it was still carrying the "measurement" label — that
is, it was not measuring today's scene.

| scene | renderers | triangles | outside batching |
|---|---:|---:|---:|
| Turkish, opening (4 tables) | 175 | 25,088 | 65 |
| Turkish, general (10 tables) | 243 | 38,205 | 83 |
| Fast food, opening | 196 | 28,308 | 64 |
| Fast food, general | 272 | 44,372 | 84 |

**The third column is new.** A renderer that has a `MaterialPropertyBlock`
written to it cannot enter SRP batching; a review estimated that this could be
around 160 (badges, clothing, stove tops, foam, trays). Measured: **84**. The
gap between the estimate and the measurement is a factor of two — and the
estimate leaned in the direction that would have justified an unnecessary
optimisation.

**A renderer ≠ a draw call:** SRP batching merges the ones that share the same
material, so the real call count is below this. This table is an **upper bound**
and its actual job is to catch regressions — a change that quietly adds a
hundred objects to the scene shows up here.

### Where the thresholds are and why they differ from the budget

These two numbers are measured by **two separate tools**, and for a long time
neither of them was breaking anything:

| tool | what it measures | threshold |
|---|---|---:|
| the smoke tour (`Autopilot`) | the **floor** scene, 4 tables — the tour never expands | 400 renderers / 80 thousand triangles |
| `GameShot` | the **ceiling** scene, every room open | 360 renderers / 70 thousand triangles / 220 outside batching |

The thresholds are **larger** than the budget row above (100 / 60 thousand), and
that is deliberate: the budget row counts *draw calls*, these two tools count
*renderers*, and batching closes the gap between the two. The thresholds were
chosen to leave headroom over today's measurement — so that small additions do
not break them and a **silent bloat** is caught.

**The third number is new:** *renderers outside batching*. A renderer that has a
`MaterialPropertyBlock` written to it cannot enter SRP batching; the project had
written this down repeatedly and designed the floor and the room light around it,
but three new systems (badges, clothing, stove tops) went on paying the same cost
and were **never measured**. And because they were not measured, nobody noticed.

The ceiling threshold is in `GameShot`, not in the tour, because the tour stays
at four tables: if a fourteen-table scene went over budget, the threshold in the
tour could never be broken. *A threshold that stands where it cannot be broken is
not a threshold.*

The tour measures the same two numbers, but **at four tables**: it plays sixty
days and does not expand. When it was first written the comment said "the
restaurant is at its biggest at the end of the campaign" and the diagnosis
disproved it — *the name of a measure should say what it measures.*

### Measurement

- A profile is taken on a real device every release, not in the editor.
- Tested on at least two devices: one high end, one at the minimum.
- A budget overrun is solved before a feature is added.

---

## B6. Save file format

### The format

| Decision | Value |
|---|---|
| Format | JSON |
| Compression | gzip |
| Integrity | A checksum, at the end of the file |
| Encryption | **None** |
| Target size | Under 200 KB per save |

### Why JSON

Readable, easy to debug, easy to write version migrations for. After compression, size is not a problem.

A binary format would be slightly smaller, but writing the migration function and inspecting a player's corrupt save would be far harder.

### Why there is no encryption

This is a single-player game. There is no leaderboard, no multiplayer, no competition. Somebody who edits their save affects only their own game.

What encryption gives in return: support gets harder, debugging gets harder, and a determined player breaks it anyway. **No gain, and a cost.**

### File layout

```
saves/
  slot_1.save      current
  slot_1.bak       the previous one
  slot_2.save
  slot_2.bak
  ...
```

The write order: write to a temporary file, verify the checksum, move the current file to the backup, make the temporary file the current one. That order makes a half-written file impossible.

---

## B7. Input action map

Unity's Input System is used. **The game code never reads raw device input**, only intent.

### Two control schemes

| Scheme | Device | When |
|---|---|---|
| Touch | Touchscreen | Mobile, the default |
| Desktop | Mouse and keyboard | The Steam version |

A gamepad scheme can be added later, but it is not in the first release.

### Action maps

| Map | When it is active |
|---|---|
| UI | In menus and screens |
| Service | In the service phase |
| Layout | In layout editing |
| Camera | In service and layout, together with UI |

More than one map can be active at once. For example, in service both Service and Camera are open.

### Actions

| Action | Touch | Desktop |
|---|---|---|
| Point | Finger position | Mouse position |
| Select | A single tap | Left click |
| Drag | Press and drag | Left click and drag |
| Intervene | Touching the table | Right click or left click |
| Pan | Two-finger swipe | Middle-click drag or WASD |
| Zoom | Two-finger pinch | Mouse wheel |
| Pause | The pause button | Space or Esc |
| Cancel | The back button | Esc |
| Confirm | The confirm button | Enter |

**Precise aiming is in neither scheme.** An ability that does not exist on touch is not used on desktop either, because there should be no gameplay difference between the two versions.

### Why two schemes from the start

Steam is targeted later. If the input abstraction is added afterwards, all the interaction code has to be rewritten. The cost of building it now is close to zero.

---

## The development machine's trap: Smart App Control

On this machine **Smart App Control is in enforced mode** (Windows 11, CI policy
`{0283ac0f-fff1-49ae-ada1-8a933130cad6}`). It can block a freshly built, unsigned
assembly from *loading*:

```
System.IO.FileLoadException: Could not load file or assembly
'...\Lokanta.Core.dll'. An Application Control policy has blocked
this file. (0x800711C7)
```

**Which configuration is blocked changes over time:** one day `Debug` is blocked,
the next day `Release`. On the morning of 11 September `Debug` worked first, and
a few hours later the same command was blocked in `Debug` and passed in
`Release`. The block depends on the file's **hash**: as long as the core does not
change there is no problem; the moment it changes there can be.

The symptom is misleading. On one occasion 206 of 213 tests broke together and **pure
maths** tests like `Fx.MulDiv_throws_on_division_by_zero` gave a `FileLoadException`
instead of a `DivideByZeroException` — all for the same reason. There is nothing
to look for in the code.

### Why it sometimes passes and sometimes does not

The block depends on the file's **hash**. The core and the content are compiled
with `<Deterministic>true</Deterministic>`, that is, **the same source always
produces the same binary**. The consequence: a build that is blocked once is
**not fixed** by rebuilding — same file, same block, forever. That target stays
locked until the source changes.

That is why it looks like "Debug blocked one day, Release the next": what changes
is not the day but when that configuration's binary last changed.

### The right response

1. Look at the `Microsoft-Windows-CodeIntegrity/Operational` log (events
   3033/3077/3118). Anything that does not appear there is not SAC. **Do not go
   looking in the code.**

2. Run it with **`python tools/dotnet_retry.py <args>`**. The tool does three
   things at once and all three are necessary — each was tried on its own and was
   not enough:

   | what | why it is necessary |
   |---|---|
   | `-p:Deterministic=false` | so the compiler embeds a new module id every time |
   | `--no-incremental` | **the flag does nothing on its own**: if the source has not changed, MSBuild considers the build up to date, skips it and hands back the same blocked binary. Measured — six attempts, six identical blocks |
   | the retry | a fresh hash can be blocked too; the block is probabilistic |

3. **For `dotnet run` the build has to be a SEPARATE step.** `run` does not
   recognise the `--no-incremental` flag and **passes it to the application**;
   the harness then rightly rejects it as an "unknown flag". First
   `dotnet build … --no-incremental`, then `dotnet run --no-build`.

`tools/check.py` and `tools/balance/calibrate.py` do this by themselves, and both
of them **throw an error** instead of returning an empty table — returning empty
silently levels all the candidates and the calibration was picking one of them
at random as "the best".

Determinism is switched off only for the **local run**; it stays on in the
project.

**Smart App Control was not turned off and must not be:** on Windows it is a
one-way door — once it is off, turning it back on requires reinstalling the
operating system. This is a development machine setting, not a project decision.

### Three wrong diagnoses, in order

This trap was misdiagnosed three times on 11 September and each one cost hours.
It is written down so that there is not a fourth:

1. **"netstandard2.1 is blocked, net10.0 passes."** Wrong — the same net10.0 DLL
   was blocked a few hours later. (The multi-target build was kept anyway, but
   for a different reason; see below.)
2. **"The configuration alternates: Debug one day, Release the next."** Wrong —
   what changes is not the day but when that configuration's binary last changed.
3. **"`-p:Deterministic=false` solves it."** Incomplete — MSBuild is not
   rebuilding.

### Two targets: a separate decision

`Lokanta.Core` and `Lokanta.Content` carry
`<TargetFrameworks>netstandard2.1;net10.0</TargetFrameworks>`. This is **not an
SAC workaround** — it was thought to be for a while and that was wrong. The real
reasoning:

- `netstandard2.1` is Unity's API surface. If the core steps outside it, the
  build breaks **here**. It is never loaded, only compiled.
- `net10.0` is the assembly the tests and the balance tool load.

That guard only works when it is compiled, and because nobody loads it, it does
not get compiled on its own: `tools/check.py` runs both targets as a separate
check.

---

## Details awaiting a decision

1. Should the minimum Android version be 10, or should it go lower
2. Is a 30 fps floor enough
3. Should gamepad support go into the first release
4. Should downloadable cuisine assets be built in the first release
