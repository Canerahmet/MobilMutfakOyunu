# The Art Pipeline: Who Is Going to Judge the Mesh

**Last updated:** 10 September 2026
**Register items:** C1 the model production route, C2 characters and animation
**Status:** Redesigned. An answer to the scope review's biggest worry.
**Review source:** [review/03-scope-realism.md](review/03-scope-realism.md)

---

## The problem, plainly

The scope review wrote this: "The character pipeline sits right on the developer's skill gap. Weight painting demands visual judgement."

You confirmed it today: you do not know Blender and you delegate its use to the AI.

That left the pipeline like this:

| Who | What they can do | What they cannot do |
|---|---|---|
| Me | Write a Blender Python script | See the mesh it produces |
| You | Run the script | Tell that the mesh is wrong |

**Nobody in the loop was looking at the mesh.** That made the "procedural first" decision ([20-production-decisions.md](20-production-decisions.md)) unworkable.

---

## The solution: a headless render loop

Blender opens from the command line, the script runs, a PNG comes out. **I can read the PNG.** The loop closes:

```
write script → blender --background → PNG → I look → notes → fix the script
```

Verified today, `tools/art/gen_table.py`:

| Measurement | Value |
|---|---|
| Produced | A restaurant table, two chairs, a cloth, a plate |
| Objects | 19 |
| Triangles | 2,004 |
| Render time | 4 seconds, three angles |
| Engine | EEVEE |
| Blender | 5.2 LTS, installed on the machine |

And the first correction notes I produced, by looking at the image:

1. The chair back is floating a centimetre or two above the seat; the back should come down from 0.70 to 0.68
2. The white plate disappears on the white cloth; either the cloth should be a dirty beige or the plate should get a dark rim
3. The chairs are twelve centimetres away from the table; at an empty table they should sit tucked in

Those three notes are the answer to the question "who is going to judge the model". I am.

### The loop ran end to end: 10 September 2026

`tools/art/gen_fastfood_props.py` produced eight props: the table set, chair, counter, stove, cupboard, shelf, bin, tray. 2,736 triangles in total, all within budget.

Three rounds went by, and in every round looking at the render caught a real bug:

| Round | What looking showed | The fix |
|---|---|---|
| 1 | The chair back floating above the seat; the white plate disappearing on the white cloth; the chairs far from the table | The back sat down on the seat, the cloth became a dirty beige, the chairs were tucked in |
| 2 | The stove's hood and the counter's menu board hanging in mid-air | Both became wall-mounted parts; a floor and a wall were added to the contact sheet |
| 3 | The props with walls come out entirely grey | The wall is at +Y and two of the cameras were looking from behind it; for props with walls the angles were moved into the −Y half |
| 4 | The stove is two metres from the wall and its flue reaches nowhere | The stove was pushed against the wall, the flue goes up to the ceiling |

None of these could have been found by reading the code. The third round is especially instructive: the script ran without an error, the triangle budget held, the report said "OK" and the output was a completely blank grey square. **A numeric report can be correct and the image can still be broken.**

### The limit of the loop

I can see, but I am not an art director. What I can see: does the silhouette read, do the parts sit together, is a proportion off, is there colour contrast, does the triangle budget hold, has the weight painting collapsed. What I cannot see: "is it beautiful." You see that, and you can see it without knowing Blender: you look at the PNG.

**So the division of labour:** I judge technical correctness, you judge taste. Both of us by looking at a PNG, neither of us by opening Blender.

### The character pipeline was proved: 10 September 2026

`tools/art/gen_character.py` and `tools/art/lib/rig.py`. What had to be proved was whether the 96-mesh problem really disappears.

| Measurement | Value |
|---|---|
| Body parts | 19 meshes, 836 triangles |
| Attachments (apron, hat, hair) | 3 meshes, 132 triangles |
| **Total** | **22 meshes** |
| Extra meshes needed for 16 outfits × 3 body types | **0** |
| Places needing weight painting | **none** |
| Test poses | 6, all rendering correctly |

### Where the decision changed: rigid parts instead of skinning

The document envisaged a single body mesh plus automatic weights. **It was tried and it failed.** When automatic weights were applied to boxes that were joined but not welded, every box bound to a single bone; in the sitting pose the legs came away from the hips and gaps opened at the joints.

The way to fix it was to weld the mesh, add an edge loop at the joint region, and paint weights. **That work falls exactly into the skill gap this document identified.**

Instead, every part was bound **rigidly** to a single bone. No skinning, no weight painting, the joints are covered with overlapping geometry.

**The cost is plain:** the character does not bend like paper, it turns like a wooden doll. That is a style decision and a common one in low-poly mobile games. It fits the game's soft-edged direction, and most importantly it removes the one step that demands visual judgement.

### I looked five rounds, and all five caught something

| Round | The numeric report | What the render showed |
|---|---|---|
| 1 | Budgets fine | The poses are on the wrong axis; the arms do not come down, they swing forward and back |
| 2 | Budgets fine | Gaps at the joints: in the sitting pose the legs came away from the hips |
| 3 | Budgets fine | With rigid binding the parts scattered across the scene; bone parenting takes the tail as the origin, and I had assumed the head |
| 4 | Budgets fine | The arms went up instead of down; the rotation sign is inverted |
| 5 | Budgets fine | The camera is looking from behind, the apron is not visible; the hair swallows the top half of the head |

**In all five rounds the numeric report said "OK".** The triangle budget held, the script ran without an error, not one warning came out. All of the bugs only showed up when looked at.

### The bone structure and clip compatibility

Nineteen bones, with names matching Unity's humanoid mapping: hips, spine, chest, neck, head, shoulder/upperarm/forearm/hand and thigh/shin/foot, left and right. Quaternius Universal Animation Library clips can be retargeted onto this structure.

The body type is scaled on the bones' **thickness** axes, not their length. If the length changes, the skeleton's proportions break and clip retargeting slips.


---

## Three tiers, by risk

### Tier 1: Furniture, equipment, environment

**Route:** a procedural Blender script + the render loop. Entirely ours.

This is the route verified today. Table, chair, counter, stove, oven, cupboard, till, wall panels, floor tiles, sign, planter, lamp: all produced with cubes, cylinders and bevels. This is exactly where low-poly's advantage lies: no textures, just flat-colour materials.

The production script standard:

```
tools/art/
  lib/
    prim.py        cube, cylinder, bevel, flat-colour material helpers
    stage.py       lights, three-angle camera, contact sheet render
    export.py      glTF export + manifest
  props/
    table_2.py     a table for two
    table_4.py
    counter.py
    stove.py
    ...
  out/             PNG contact sheets (not committed)
```

Every script produces three things with `python props/x.py`: a contact sheet PNG (three angles, wireframe, triangle count), `Assets/Art/Generated/x.glb`, and an `x.json` manifest (triangles, bounding box, source script digest). The Unity import reads the manifest; there is no dragging by hand.

### Tier 2: Characters

**Route:** a single body mesh + automatic rigging + body type through bone scale + attachable clothing parts. The render loop catches a weight collapse with six test poses.

The review's arithmetic: 16 outfits × 3 body types = 96 meshes. **That number disappears**, because:

| Variable | How |
|---|---|
| Body type | One mesh, three bone-scale presets. The mesh does not multiply |
| Clothing colour | A material swap. The mesh does not multiply |
| Clothing part | Apron, hat, scarf, jacket: a separate **skinless** mesh, bound to a bone. No weight painting |
| Hair | 8 skinless meshes, bound to the head bone |
| Skin tone | A material |

An outfit set = the body material + 0-3 attachments. Sixteen sets, zero extra skinning. The body is weighted once and never touched again.

How the weighting works: Blender automatic weights (`parent_set(type='ARMATURE_AUTO')`) on the single body. Then the render loop renders six poses (T, mid-walk, sitting, bending, reaching, turning); I see the elbow and knee collapse. If there is a collapse there are two routes: add a bone, or add an edge loop to that region of the mesh. Both are script work.

The animation clips come from three sources (decision 10 September 2026):

| Need | Source | Who |
|---|---|---|
| Walk, idle, carry, sit, eat | **Quaternius Universal Animation Library 1 and 2.** More than 250 clips, a single humanoid rig, retargetable in Unity, CC0, a downloadable pack | I download it and wire it up with a script |
| Cook, wipe, till, serve | Procedural in Blender: upper-body loops, three or four keyframes, verified with the render loop | Me |
| A single clip that is still missing | **Mixamo**, the backup. Open and free in 2026, its licence is unlimited commercial use; but it has not been updated since 2015. A downloaded clip stays with us | You, during implementation, fifteen minutes |

Mixamo's risk of shutting down only affects downloading new clips later on; that is why it is the backup, not the primary.

The fallback plan: if the character pipeline cannot be proved within a two-week timebox (one character, a rig, four clips playing on a device in Unity), we move to Quaternius's CC0 rigged low-poly characters. The clothing difference is made with materials alone. Less identity, zero risk.

### Tier 3: The things I cannot produce

| Thing | Solution | Budget |
|---|---|---|
| Textures | None. Low-poly flat colour, vertex colour. Because there are no textures, none need to be produced | 0 |
| Interface icons | SVG. I write them; I already wrote the coin icon. Stamped into Unity as PNG with Blender or Inkscape | 0 |
| Character portraits | Not needed. The characters are 3D; a portrait is taken at runtime by moving the camera close to the head | 0 |
| Cover art, store screenshots | From the game itself, with a dressed scene and good lighting. A high-resolution render in Blender | 0 |
| Sound | [17-audio-design.md](17-audio-design.md) is the source; the review approved it (C4) | 0 |
| Logo, typeface | Google Fonts open licence; the logo is typographic | 0 |

The only thing a zero budget does not close: if one day you want professional cover art. Out of revenue.

---

## Free sources, with their licences

| Source | What | Licence | What for |
|---|---|---|---|
| Kenney | Low-poly furniture, food and character packs | CC0 | Prototype, a placeholder until the procedural work is done; some of it may stay |
| Quaternius | Universal Animation Library 1-2 (250+ clips), rigged low-poly characters, food | CC0 | **The primary animation source**; the character fallback plan |
| Poly Haven | HDRI | CC0 | Blender render lighting, not used in the game |
| Mixamo | Humanoid animation clips, auto-rigging | Adobe, free in a game; unmaintained | Backup clip source, you download it |
| Google Fonts | Typefaces | OFL | The interface |

The licence audit table is in [21-business-and-release.md](21-business-and-release.md); every asset is written there with its source and licence. Nothing outside CC0 goes in without being asked about.

---

## Triangle budgets

[19-technical-setup.md](19-technical-setup.md) gives the scene budget. Per asset:

| Class | Triangles | Example |
|---|---|---|
| Small item | ≤ 150 | Plate, glass, planter |
| Furniture | ≤ 600 | Table, chair, cupboard |
| Equipment | ≤ 900 | Stove, oven, counter |
| Character body | ≤ 2,000 | One mesh |
| Clothing part | ≤ 250 | Apron, hat |
| Hair | ≤ 300 | |

The contact sheet prints the triangle count; if the budget is exceeded the script warns. Today's table set is 2,004 triangles; with its two chairs that is three pieces in the furniture class, so it is within the limit, but it halves if the bevel segments are reduced. That is the first note.

---

## Who does what

| Job | Who | How |
|---|---|---|
| Writing the script | Me | Directly |
| Running Blender, taking renders | Me | From the command line through Claude Code remote control |
| Looking at the PNG and giving technical notes | Me | Read |
| Looking at the PNG and saying "I like it / I don't" | You | Even from a phone |
| Downloading a backup clip from Mixamo | You | During implementation, if needed, fifteen minutes |
| Importing into Unity | Me | Automatic, with the manifest |
| Looking at it on a device | You | Installing the APK |

You never open Blender. That is not an assumption of the plan, it is the situation verified today.

---

## Timebox

| Proof | Time | Success criterion | If it fails |
|---|---|---|---|
| Furniture pipeline | ✅ Done | An eight-prop fast food set was produced, all within triangle budget | — |
| Environment set | 1 week | A fast food shop: 12 props, one scene, 60 fps on a device in Unity | The Kenney pack becomes permanent |
| Character pipeline | ✅ The modelling side is done | Body, rig, two clothing parts, hair and six poses verified. Remaining: retarget the clips and show them on a device in Unity | Quaternius characters, difference by material |
| Food plates | 3 days | 6 bases × 8 toppings covering 32 dishes | The dishes become icons, the plate is empty |

When the timebox runs out we move to the fallback plan, no discussion. The review asked for this; it was right to.

---

## Food: 32 dishes, not 32 meshes

You wanted a lot of variety in the dishes; the designer said "it works if they are parameterised" ([23-core-contract.md](23-core-contract.md) §8.3). The same logic applies on the art side: **modular plating.**

```
plate = base + 0-3 toppings
```

| Base (6) | Topping (8) |
|---|---|
| Round plate | A pile of spheres (meatballs, rice, nuggets) |
| Oval plate | A slice (bread, pizza) |
| Bowl | A cylinder (glass, can) |
| Tray | A leaf (salad, lettuce) |
| Paper (fast food) | A strip (fries, pasta) |
| Cup | A sauce smear |
| | A stick (straw, toothpick, skewer) |
| | Steam (only for soup and ramen, particles) |

The colour comes from the material. Hamburger = paper + slice + sphere + leaf, all in brown-green tones. Ramen = bowl + strip + sphere + steam. 32 dishes, 14 meshes, endless combinations. The `plating` field inside `dishes/*.json` says which.

The player sees the plate at 40 pixels tall. That level of detail is enough.

---

## The render loop on the Unity side

Once the Blender loop was proved, the same thing had to exist in Unity too; otherwise the view layer would be written without ever being seen.

`tools/unity/shot.ps1` and `Assets/Lokanta/Editor/SceneShot.cs`. In batch mode the scene is built, drawn into a `RenderTexture`, and a PNG is written. No display is needed.

### Two traps, both found by measuring

**1. The `-nographics` flag turns rendering off.** `run.ps1` uses it because all it does there is apply settings. The runner that takes images does not use that flag.

**2. Unity can run `-executeMethod` before compilation has finished.** The log says "Requested script compilation", the compiled DLL contains the new code, but the running version is the old one. That is why `shot.ps1` does a **warm-up round** first: the first round only compiles, the second round runs.

### The limit: URP Lit does not work in batch mode

The materials are assigned correctly, URP is active, `_BaseColor` is written and read back. Despite that, **every lit surface** was giving the same colour.

The measurement narrowed it down like this:

| Measurement | Result |
|---|---|
| The camera's background pixel | Exactly correct |
| The material colour, read back on the CPU side | Exactly correct |
| The active pipeline | LokantaURP |
| The pixels of lit surfaces | All the same, independent of the assigned colour |
| The same scene with the **Unlit** shader | Every pixel exactly correct |

The reason: **URP Lit's shader variants are not compiled in editor batch mode** and the surfaces fall back to a single fallback colour. Unlit is unaffected because its variant count is far smaller.

### What this means in practice

| Can be verified | Cannot be verified |
|---|---|
| Layout, scale, proportion | Shading and shadows |
| Camera angle and framing | Light colour and intensity |
| Colour palette (exactly) | The final look |
| The objects' positions relative to each other | Material glossiness |

So the Unity image is used for **composition checking**, not for checking the final look. The final look will be checked in two places: the Blender renders and a real device.

The verification scenes use Unlit materials; the game itself continues to use Lit.
