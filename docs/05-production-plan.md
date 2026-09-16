# Production Plan: What Gets Made, and With What

**Last updated:** 9 September 2026
**Purpose:** to write down the answer to "what are we going to make this with" for every single item. No gap left before we move to implementation.

---

## First the boundary: what I can produce and what I cannot

This is the most important item in the plan. When you said:

> *"Tamamen yapay zeka ile yapalım"*
>
> *("let's make it entirely with AI")*

what that actually means has to be set out clearly.

### What I can produce directly

I produce text. That is a much wider field than it sounds:

- **All of the C# code.** The core simulation, the Unity side, editor tools, tests.
- **Blender Python scripts.** Run a script and it produces low-poly furniture. Details below.
- **Unity editor tools.** Scene placement, prop laying-out, data import.
- **Shader code.** Flat shading, outlines, sunset light.
- **Content data.** Recipes, ingredients, prices, staff archetypes, customer types. As JSON.
- **Text content.** Dialogue, customer reviews, character stories, interface text, localisation files.
- **The balance tool and its analysis.** Running the simulation and interpreting the result.
- **Store texts, a draft privacy policy, help documents.**

### What I cannot produce directly

- **A 3D model file.** I cannot output a mesh directly. But I can write the script that produces it.
- **Images.** Character portraits, textures, icons, cover art.
- **Sound.** Music, effects, voice acting.
- **Using a web interface.** I cannot log in to services like Meshy, Tripo or Mixamo. You run those.
- **Testing on a real device.** Running it on a phone and looking at it is on you.
- **Uploading to a store.** Account and signing operations are on you.

**Conclusion:** the code and design side is on me, the visual and sound production is a tool plus your operation. The plan below is built around that split.

---

## 3D models

A restaurant interior is a lucky subject: most of the objects are boxes and cylinders. That gives us a three-way strategy.

### Route 1 — Procedural generation (the primary recommendation)

I write Blender Python scripts. The script produces the whole furniture set from a single style configuration.

**What it covers:** table, chair, stool, counter, shelf, cabinet, stove, fridge, plate, glass, pot, tray, door, window frame, lamp, sign, crate.

**Why this route is primary:**
- **Style consistency guaranteed.** All of them share the same edge softening, the same scale, the same palette.
- **Free.** No recurring subscription.
- **It goes into version control.** A model is not a file, it is a script. If you want to change the style you change the script and regenerate all of them.
- **Scale is free.** If you need thirty kinds of chair you change a parameter and generate them.

**What it does not cover:** human characters, plants, organic food detail.

### Route 2 — CC0 ready-made packs (prototyping and gap filling)

Sources like Kenney and Quaternius give away thousands of low-poly models under a CC0 licence. Commercial use is free, attribution is not required.

**Use:** speed in the vertical slice, and the organic objects the procedural route does not cover.
**Risk:** a recognisable look. A lot of games use the same packs. It could create an identity problem in the final release.

### Route 3 — Paid packs and AI generation (selected objects)

- The Shops and Town packs in the **Synty POLYGON** series are close to our subject. Professional and consistent, paid.
- **Tripo** is roughly 12 dollars a month, **Meshy Pro** 20 dollars a month. It generates models from text or from an image.

**Critical warning:** the **free tiers of Meshy and Tripo are closed to commercial use.** If you are going to use it in the game, a paid plan is mandatory. Also, the topology of generated meshes usually comes out dense and wants simplifying for a game.

**Use:** single objects that have to be unique. The shop's sign, a special stove, an object that belongs to the story.

---

## Characters and animation

**This is the hardest production item in the project.** Furniture is solved with a script; humans are not.

### Shrinking the problem through design

My recommendation is to design the characters from the start in a way that lowers the animation cost:

- Simple proportions, no separate fingers
- Minimal facial detail, dot eyes
- A small number of shared animation clips: walking, sitting, eating, waiting, talking, cheering, getting annoyed

**Variety comes from the wardrobe, not from the model.** On a single body model, thirty different customers are obtained by changing colour, hair, hat, glasses and bag. That lowers both production and memory, and on mobile that is a significant gain.

### Rigging and animation routes

| Route | Status | Note |
|---|---|---|
| **Mixamo** | Free, commercial use allowed | Automatic rigging and a wide animation library. **Risk:** Adobe has not updated it for years, there was a multi-day outage in 2025 and support says it is "no longer supported". A backup plan is mandatory. |
| **Tripo automatic rigging** | With a paid plan | In the same service as the model generation |
| **Synty characters** | Paid | They come rigged, compatible with Mixamo |
| **Unity humanoid system** | Free | If there is a rig, animation sharing already works |

**Decision:** we do not lean on Mixamo alone. Character models are produced to fit the standard humanoid rig, so that even if the animation source changes, the model does not.

---

## Materials and textures

This is low-poly's greatest convenience. **Almost no textures are needed.**

- A single palette atlas is used. All objects take their colour from that atlas.
- I can generate that atlas with code. The palette lives in a data file, a script produces the image.
- Alternatively, vertex colour is used and textures disappear completely.

**Conclusion:** texture production is not a problem, it can be treated as solved.

**This is where the visual identity comes from:** in the research, low-poly's only weak side was the risk of looking generic. The way to close it is palette, light and shadow. All of those are a matter of code and settings, not of artistic skill. That works in our favour.

---

## Sound

| Item | Route | Cost |
|---|---|---|
| Music | ElevenLabs Music or Suno, paid plan | Monthly subscription |
| Sound effects | ElevenLabs SFX, royalty-free and commercially usable on a paid plan | Monthly subscription |
| Alternative | Freesound and similar CC0 sources | Free |
| Speech | None. Nonsense-syllable sounds recommended | Very cheap, adds character |

**Warning:** on the Suno side the ownership language changed. It keeps authorship at Suno and grants you a perpetual commercial licence. The free tiers are not suitable for commercial use. Verify the terms of the plan you are using before release.

Using nonsense syllables instead of voice acting both zeroes the cost and makes localisation easier.

---

## Interface, icons and typeface

- **Interface layout:** code, that is on me.
- **Icons:** Kenney's CC0 interface packs, or generated icons.
- **Typeface:** Turkish character support is mandatory. Families on Google Fonts licensed under the SIL Open Font License are suitable for commercial use. When choosing, the presence of these characters is checked:

  > ı, İ, ğ, ş, ç, ö, ü

- **Character portraits:** if pixel art is being considered, with an image generation tool — your operation.

---

## Code, infrastructure and tools

| Item | Route | Cost |
|---|---|---|
| Engine | Unity Personal | Free, up to a 200 thousand dollar annual revenue limit |
| Splash screen | Can be removed on Personal with Unity 6 | Free |
| Version control | Git, Git LFS for binary files | Free |
| Repository | A private GitHub repository | Free |
| Builds | Local builds at first | Free |
| Tests | Unit tests for the core, on me | Free |
| Device testing | At least one low-end Android phone | Hardware |

The Git LFS setting has to be made from the start. If model and sound files go into ordinary Git the repository swells quickly and undoing it is laborious.

---

## Services

All of these will sit behind port interfaces, which means the choice does not affect the architecture.

| Item | Mobile | Steam |
|---|---|---|
| Analytics | Unity Analytics or Firebase | Optional |
| Ads | Depends on the revenue model | None |
| Purchases | Unity IAP | Through Steam |
| Cloud save | iCloud, Google Play | Steam Cloud |

---

## Localisation

- The Unity Localization package is used.
- At least Turkish and English. Text is kept in files from the start, not embedded in code.
- Adding a language later is far more expensive than conforming to the structure from the beginning.
- I can produce the translated text.

---

## Store and release

| Item | Route |
|---|---|
| Screenshots | Taken from the game, your operation |
| Trailer | Recording and editing, your operation |
| App icon | Image generation tool |
| Store texts | On me |
| Release signing | On you |

---

## Legal

- **A privacy policy** is mandatory. Definitely so if there is analytics or advertising. I can write the draft, but having a lawyer read the final text is the right thing to do.
- **KVKK and GDPR** compliance. If data is collected, an explicit consent flow is required.
- **Licence audit of AI-generated assets.** Before release, the source and the licence of every asset must be listed in a table. A model generated on a free tier getting into the game is a serious risk.
- **Age rating** applications.

---

## Cost table

### Mandatory

| Item | Amount |
|---|---|
| Google Play developer account | 25 dollars, one-time |
| Apple Developer Program | 99 dollars a year |
| Unity Personal | 0 |
| Git and GitHub | 0 |
| **First-year total for mobile** | **124 dollars** |

### If Steam is added

| Item | Amount |
|---|---|
| Steam Direct | 100 dollars per game, refunded after 1000 dollars of revenue |

### Optional production tools

| Item | Amount |
|---|---|
| Tripo | Roughly 12 dollars a month |
| Meshy Pro | 20 dollars a month |
| ElevenLabs sound | Monthly subscription |
| Synty model packs | Varies per pack |
| Kenney, Quaternius, Mixamo | 0 |

**Note:** commission rates are separate. Apple and Google take 15 percent under a million dollars a year and 30 percent above it. Steam takes 30 percent up to the first 10 million dollars.

**The lowest scenario:** with CC0 packs and procedural generation, releasing on mobile in the first year for 124 dollars is possible.

---

## Awaiting a decision in this file

1. Whether the primary route for models will be procedural or ready-made packs
2. For sound, a paid AI tool or a CC0 source
3. Whether the character portraits will be pixel art
4. Which device to buy for testing

---

## 9 September 2026 update: after the answers

The answers given to the review's questions ([22-answers-and-direction.md](22-answers-and-direction.md)) changed three of this file's assumptions:

| Assumption | Old | New |
|---|---|---|
| Platform | Android + iOS | **Android only.** There is no Mac, and iOS means a Mac |
| First-year cost | $124 | **$25** (Google Play). The Apple $99 is not being paid |
| Using Blender | "You run it" | **I run it**, Claude Code by remote control. I see the render too. See [24-art-pipeline.md](24-art-pipeline.md) |

The **3D model file** item in the "What I cannot produce directly" list has softened: I still cannot draw a mesh by hand, but I can render what the script produced, see it, and correct it. The loop is closed. On the machine there are Blender 5.2 LTS, Unity 6000.5.8f1, Python 3.13 and the .NET SDK.
