# 35 — The animation layer, working equipment and a two-finger camera

This document records the round in which the hall turned from **a static diorama** into a working restaurant. Three jobs hang together: the figures now **walk**, the equipment now **shows that it is working**, and the player now drives the camera **themselves**. What all three add up to: the things the simulation knows became visible on screen.

The state before: the simulation knew at every moment which table was at which stage and how many plates were cooking at which station; on screen the waiter and the cook **stood where they were**, customers **appeared out of nowhere** at their tables, and the oven and the stove **never** gave any sign of working.

---

## 1. The walking layer

`Game/Walker.cs` + `Game/Paths.cs`. Both new.

**No pathfinding, there is a lane.** The floor plan is fixed, the rooms are rectangles, and everybody goes back and forth between the same two things. A two-part path — first come out onto the front lane (`Paths.LaneZ = 0.55`), then go up in line with the target — both reaches every table and, when looked at, reads as *"they came in through the door and went to their table"*. A\* is unnecessary for this scene. There are no collisions either: figures can pass through each other, and that is better than having them jam up and spin on the spot.

| who | from | to | when |
|---|---|---|---|
| customer | `Paths.Outside` (z = −0.85, outside the door) | their table | when the party is seated |
| customer | their table | `Paths.Outside` | when the party gets up |
| waiting party | the door | `Paths.QueueSpot(i)` | when there is no table |
| waiter | `Paths.SalonHome` (beside the till) | `Paths.BesideTable` | order / service / bill |
| cook | `Paths.CookHome` | `Paths.Fridge` → `Paths.KitchenPost(station)` | when a job starts |

**The speed is tied to the game's speed.** `Walker.Speed = 1.15 m/s` at ×1. The simulation speeds up to ×16; if the walk stays in **real time**, the food arrives while the figure is still halfway along its path. The multiplier is written from `GameApp` every frame (`Paused ? 0 : TimeScale / BaseTimeScale`). But the multiplier has a ceiling: above ×4.5 a walk does not read, so the figure **teleports** — a walk too fast to see is not a walk, it is a flicker.

**Pausing stops the walking too.** `GameSpeed = 0` → `Walker.Update` returns early. Having the hall keep flowing while the pause screen is open raised the question of what the pause was for.

**The waiter carries the food.** `RestaurantView.ShowCarry` binds the plate to the waiter's hand and puts it down on the table when they arrive. The plate is the `Food/plate-deep` prefab; there is no separate model for carrying versus serving.

---

## 2. Working equipment

`Game/Appliance.cs`. New. One component per oven/stove.

**No particles.** docs/19 targets a low-end Adreno, and the particle system is a documented fill-rate bottleneck on that class of device. The flame and the lamp are both **boxes**: emissive-coloured, casting no shadows, with no collider. Six boxes for three stoves — immeasurable as draw calls.

Four separate bugs were fixed one after another, all of them found **by looking at an image**:

| symptom | cause | fix |
|---|---|---|
| the door sinks into the oven | the model's pivot is in the **middle** of the door | the hinge moved to the bottom edge and the door bound to it (`OpenAngle = −72°`) |
| the lamp turns with the door | the lamp was bound to the hinge | in a real oven the lamp stays in the body → but the pack's door mesh is **completely opaque**, nothing inside is visible through the glass → the lamp was moved **between** the glass and the door surface |
| the lamp has slid sideways and is burning between two stoves | the hinge is at the bottom edge and the door model's local x is 0.268 — the offsets were inheriting that shift | the position is computed **from the visual centre** with `pivot.InverseTransformPoint(b.center)` |
| the glass and the lamp are five times too small | the hinge is under the model's 0.204 scale | `Panel()` divides the size by the parent's `lossyScale` — the caller writes **metres** |
| the glass is opaque | in URP `_Surface = 1` is an **inspector** setting only | `_SrcBlend` / `_DstBlend` are written at runtime as well |
| the flame burns in front of the eyes | the eyes had been assumed to be at ±21% | **rendered from above and measured**: x is symmetric (±20.5%), z is **not** — the back row +20.4%, the front row −7.5% (the row of knobs on the front edge has pushed the hob backwards) |

The measurement image: `Lokanta/Figur olcek goruntusu` → `render/scale_stove_ustten.png`.

---

## 3. Two fingers: zoom and rotate

`Game/CameraRig.cs` + `Game/Quality.cs` (new).

| limit | value | why |
|---|---|---|
| zoom | 0.45 – 1.00 | 1.00 = docs/31's measured default frame. **You cannot go further out**: that frame already shows everything, and pulling back only shrinks the touch target. 0.45 ≈ ×2.2 magnification |
| rotation | ±35° | leaving it free breaks two things: the touch-target measurement was made at a particular angle, and seen from behind the kitchen gets in front of the hall |

**The field of view is not narrowed, the camera moves in.** Narrowing the FOV would magnify too, but it changes the perspective and invalidates the arithmetic the touch-target measurement uses.

**The resolution goes up when you move in.** The game is drawn at a 0.8 render scale (36% fewer pixels, unnoticeable in the default frame). At ×2.2 magnification that softness becomes **visible**; and with the camera moved in there is far less on screen, so there is budget for full resolution too. The threshold is 0.85 — markedly far from the default so that it does not switch on and off on a small drag.

**The general view resets both.** There has to be a way for the player to get back to a known place at any time.

### The finger and the tour going down the same road

`ApplyGesture(zoomDelta, twist)` is the **single** entry point: both `HandlePinch` and the automatic tour go through it. Leaving a separate entry route would have meant the check never measured the real code — the tour cannot produce touches, and everything to do with the limits would have gone unmeasured.

### The real bug the tour caught: a rotated camera did not come back

In its first version the tour asked this:

```csharp
Note(Mathf.Approximately(Rig.Zoom, 1f) && Mathf.Approximately(Rig.YawOffset, 0f), ...)
```

**It was green and it was wrong.** The sliding transition was carrying only the **position**; the angle was written only by the finger gesture. So when a player turned the camera and pressed "general view", the camera went to the right place but **kept looking sideways** — there was no cure for a camera you had turned. The fields (`Zoom`, `YawOffset`) were telling the truth, the camera was not.

The fix is two-sided:

- `CameraRig.Begin()` became the **only** way to start a transition and it stores `_fromRot` too; `Update` now applies `Quaternion.Slerp(_fromRot, LookRotation, k)`.
- The tour now asks **the camera itself**: `Quaternion.Angle(Rig.transform.rotation, CameraFit.Rotation) < 1°`.

Verified: when the fix is reverted, the check goes red with a **35.0-degree** deviation.

> The same lesson, for the umpteenth time on this project: a check being green does not mean it is measuring the right thing.

---

## 4. Furniture and character scales — again

Two sentences from the user:

> *"masa karakterlerin başına değiyor gibi"* and *"karakterler niye masaların köşesine oturuyor"*
>
> *("the table looks like it is touching the characters' heads" and "why are the characters sitting on the corners of the tables")*

Both were true and each had a different cause.

### The table was touching their heads

Measured: a seated figure's head was **0.37 m** above the table; a realistic ratio is 0.51. The root cause: **the furniture was at real scale and the characters were at half scale**. Enlarging the characters broke the toy-like look, so the furniture came down:

| | before | after |
|---|---|---|
| dining table | 0.74 m | **0.55 m** |
| chair | 0.92 m | **0.68 m** |
| `SitLift` | 0.35 | **0.26** |
| head-to-table clearance | 0.37 m | **0.47 m** |

The kitchen counters were **deliberately** left at 0.92: a surface you work at standing up, not a table you sit at.

Also, on the user's suggestion, it was **the head and not the body** that was shrunk (`ArtPrefabs.HeadScale = 0.80`, the `head` bone is scaled). This pack's figures are large in their **width** rather than their height — the head is a third of the body; and that is the thing to shrink.

### Sitting on the corner: the table was a hexagon

`tableRound` is a **hexagon** and its corners are at ±Z. The four seats stand at 90°; they cannot be aligned with 60-degree edges at any angle — the guests necessarily landed on the corners.

The user chose square. The `Furniture/table` prefab is used and it is **made square** at setup: `TableSquareZ()` measures the renderer bounds once and writes the `x/z` ratio into `localScale.z` (clamped between 0.25 and 4, cached). Writing a fixed number would have broken silently when the model pack changed.

**The same distance in all four directions:** `SeatRadius = 0.58 m`, a single constant.

```
0.58 = 0.41 (half width) + 0.17 clearance
upper bound, from the neighbouring table : 0.58 + figure width/2 (0.32) = 0.90 ≤ 0.925  (cell 1.85)
empty chairs on Z                        : 0.58 + 0.15               = 0.73 ≤ 0.85   (cell 1.70)
```

Visual verification:

- `render/scale_masa_ustten.png` (from above, four chairs at equal distance)
- `render/scale_masa_oyun.png` (the game angle)

Both of them call `RestaurantView.SeatAt()` — the measuring tool does **not rewrite** the arithmetic, it uses the game's own code.

**Two guests sit on the X pair** (`SeatOrder` = 1, 3, 2, 0). In the game camera's view the X pair spreads horizontally and both figures are fully visible; on the Z pair the back one was hiding behind the front one and the table. All four cannot be drawn: four seated figures do not fit in a 1.85 × 1.70 m cell at any reasonable scale (docs/31). The simulation is not affected — the party is still four people, and so is the bill.

### Sitting: the figure was 7.4 cm below the cushion

Two sentences from the user:

> *"sırt ve arka tarafları sandalyenin üstüne geliyor"* and *"dizleri sandalyenin içine girmiş gibi"*
>
> *("their back and their behind come out on top of the chair" and "their knees look as if they have gone inside the chair")*

These were two symptoms of **the same single bug**. `SitLift` had never been measured; it had been written by eye.

The measuring tool was written for this (`Editor/FigureShot` → the `OTURMA` lines). Three wrong methods were tried and all three were recorded, because each one is a separate trap:

| method | why it did not work |
|---|---|
| the bounding box | the seated figure's width came out as **1.01 m** — that is the **arms**; the bottom of the box can be a leg and its back a shoulder |
| vertex sampling | the pack's meshes have **Read/Write off** (`isReadable: 0`), `.vertices` comes back empty; and even if they were readable, on a **low-poly** box body there are only eight vertices, none of them in the middle |
| a ray — but the chair's collider was left in the scene | the measurement **silently read the chair**: it gave **the same number** even when the figure was lifted by 7.4 cm. A measurement that does not change is a sign that it is not measuring the thing |

The right way: the posed mesh is taken with `BakeMesh` (that mesh is **ours**, the pack's setting is no obstacle), attached to a temporary `MeshCollider`, and the surfaces are read with a ray — and **the chair's colliders are removed first**.

The numbers that came out:

| | before | after |
|---|---|---|
| cushion surface | 0.355 m | 0.355 m |
| the figure's underside of the pelvis | 0.281 m | **0.355 m** |
| the back's rear / the backrest's front | 0.108 m **inside** | 0.042 m **in front of** |
| feet | — | 0.14 m above the floor, in mid-air |
| `SitLift` | 0.26 | **0.316** |
| `SitForward` (new) | — | **0.15** |
| the radius of an occupied chair (new) | — | **0.65** (an empty chair is at 0.58, right up against the table) |

**An empty chair is up against the table, an occupied chair is further back.** In reality a chair is pulled back to sit down too; here it also makes room for the backrest.

### Bending at the knee is not possible — and is not needed

For one round, in the sitting pose the leg bones were written to the rest angle (downwards). The result was worse: **there is no knee in the skeleton** — there is one bone per leg (`root, leg-left, leg-right, torso, arm-left, arm-right, head`), so turning the leg down turns the thigh too, and the thigh cuts through the cushion's front edge.

The measurement made it definite: **if the hips are to sit on top of the cushion, a leg that cannot bend at the knee has to pass through it.** That is why the pack's own clip holds the thigh horizontal — the leg lies along the cushion and the feet dangle over the front edge. At chibi proportions that is the correct posture.

Adding a bone is technically possible (there are 22 separate vertex rings along the leg, so a new bone really would bend it) but it was not needed: the real bug was not in the leg, it was in the height.

### The character from 0.95 → 1.00 m

The user's request:

> *"çok çok az büyütelim"*
>
> *("let us make them very, very slightly bigger")*

The neighbouring-table margin still holds and the placement audit gives **0 overlaps** in both cuisines. The head is now **0.57 m** above the table (up from 0.47): because the figure has stopped being sunk into the cushion and is really sitting on top of it.

> The target height is now **in one place**: `ArtPrefabs.CharacterHeight`. The placement audit reads it — for a long time it said "target 1.28" while the target had changed long before.

### A knee bone was ADDED to the model

The user's question:

> *"Peki modele sen kemik ekleyip düzeltebilir misin?"*
>
> *("So can you add a bone to the model and fix it?")*

Yes — and it was needed.

The pack's skeleton: `root, leg-left, leg-right, torso, arm-left, arm-right, head`. **One bone per leg, no knee.** The consequence was measured: if the hips are to sit on top of the cushion, a single-piece leg descending from the hip has to pass through it. That is why the pack's own sitting clip holds the thigh horizontal and stretches the feet forward — it is not "a person sitting on a chair" but "a person sitting cross-legged on the floor".

The bone is added **in the production pipeline** (`ArtPrefabs.AddKnees`), not as a one-off edit: the next run of "produce the model prefabs" would have deleted it.

| step | what happens |
|---|---|
| readability | `isReadable` is turned on for the FBXs in the character folder — the pack comes with it off and `.vertices` was coming back empty |
| the split plane | **between the two rings** nearest the middle of the leg's own vertices. If it passed *through* a ring, two vertices at the same point would fall to different bones on a flat-shaded model and the surface would split open; passing between them stretches only a single quad |
| re-weighting | the 64 vertices below the plane are bound to the new bone, and a **copy** of the mesh is saved as an asset (the pack's file is not changed) |
| the bind matrix | `diz.worldToLocalMatrix * renderer.localToWorldMatrix` |

**The bind matrix tests itself:** the same formula is applied to the pack's *own* leg bone and compared against the matrix the model ships with. If it does not match, the generated bone would be wrong too — and that means a mesh that slides silently.

The pose is set up with `Figure.BendKnees`: **thigh forward, shin down**. No clip moves the knee bone, so all the old poses stay exactly as they were.

Three traps, all three found by measurement:

- **The bind angle cannot be read at runtime.** The first version read it on first use, and what it read was not the bind angle — the clip had already changed the pose. The angles are now recorded **during prefab production** (`Figure.LegRest` / `KneeRest`).
- **Aim at a direction instead of setting an angle.** The thigh's and the shin's bone axes are not the same: the thigh turned as expected while the shin went somewhere else entirely. `Aim()` finds the bone's "down" axis in its bind state and turns it towards the wanted direction; you do not have to know where the axis points.
- **A surface that does not touch had been measured.** The *middle* of the pelvis was being seated on the cushion and the number was green, but the surface that touches is **the underside of the thighs** — and that is 9.4 cm below the hip bone. `SitLift` 0.316 → **0.410**, the table 0.55 → **0.58**. The yardstick was corrected too.

The result: the pelvis deviation +0.094 (above the thighs, correct), **leg margin 0.000** (the thighs sit exactly on the cushion), back margin 0.005 (in front of the backrest). The feet are 0.23 m above the floor, in mid-air — this pack's legs are 32% of its height (in reality 52%) and a chair that put its feet on the floor would be 0.24 m.

### The cook does real work now and turns to face what they are looking at

Three separate bugs:

1. **On arrival the angle was written as a fixed 180°** — whatever the cook did they faced the same way, and they were using the stove with their back to it. The angle now comes **from the stove's own position** (`Paths.FaceFrom(target, lookAt)`), and the mapping is the same one `UpdateAppliances` uses — so the cook turns to the stove that is **lit**.
2. **Idle staff were also standing at a fixed angle.** `float.NaN` is passed now: they stay facing the way they walked.
3. **The Animator was switching off after ~1 s** — correct for a seated customer (they do not move), wrong for a working cook: they were freezing on the **first frame** of the chopping clip. A working figure now calls `HoldAwake()`. The pose count was green, the image was dead.

The kitchen jobs were built from the pack's ready-made clips; no new animation was produced: `attack-melee-right` → chopping (an arm coming down from above), `interact-left` → washing, `pick-up` → picking up an ingredient. They are distributed by station in a fixed way, so there are three different movements on three stoves but the image does not flicker.

### The transparent walls that separate the rooms

The floor plan read as a single slab of floor; the only things marking a room's boundary were a 4 cm gap and a difference in colour.

| decision | value | why |
|---|---|---|
| height | 1.15 m | the character is 1.00 m; it draws the room's line but at a 34-degree view you can see inside. Full height (2.4 m) would have hidden the front row completely |
| alpha | 0.20 | let it be understood that the wall is **there**, without hiding the table and the cook behind it |
| the front edge | not drawn | the door is there and the camera is looking from there |
| collider | none | the touch target is the room floor (docs/31); a ray hitting the wall would break the room selection |
| shadow | off | URP's shadow pass does not read alpha — a transparent wall was casting an **opaque** shadow and drawing black stripes across the hall |

**A shared edge is drawn once.** If it were drawn twice the alpha would stack and that wall would be darker than the others — and whoever looked at it would invent a meaning, "there is a thicker wall there". The key is the endpoints rounded to the centimetre.

The walls are **excluded** from the placement audit: they stand on a room boundary and by definition they intersect every counter set against that boundary.

---

## Verification

| what | result |
|---|---|
| `tools/check.py` | 12/12 |
| core tests | 220 |
| the automatic tour (a real Windows build) | **57/57** |
| the placement audit (`PlacementAudit`), both cuisines | **0 overlapping pairs** |
| head-to-table clearance | 0.63 m |
| sitting (`OTURMA SONUC`) | pelvis +0.094 / leg 0.000 / back 0.005 |
| AAB | 31.1 MB, **0 warnings** |

To re-measure:

```powershell
tools\unity\shot.ps1 -Method "Lokanta.EditorTools.FigureShot.Capture"     # scale images
tools\unity\shot.ps1 -Method "Lokanta.EditorTools.PlacementAudit.Run"     # the overlap audit
tools\unity\run.ps1  -Method "Lokanta.EditorTools.BuildPlayer.Windows"    # then the automatic tour
```
