# Technical Decisions and Production Conditions

**Last updated:** 9 September 2026

This file is the record of the decisions that have been made. If a decision changes, it is updated here and the reason is written down.

---

## Decisions made

### Engine: Unity ✅ decided

**Reason (from the user):** the animation quality and the visual result are better. Thanks to cross-platform support, one source can produce releases for the App Store, Google Play and later Steam.

**Consequences:**
- The mobile ads, in-app purchase and analytics libraries come ready.
- A Steam release is a realistic second target. That is the market where the most successful examples in the research (Dave the Diver, Supermarket Simulator) live.
- If Steam is a target, touch and mouse/keyboard input must be designed as separate layers from the start. Adding it later is expensive.

### Team: one person, entirely AI-assisted ✅ decided

**Reason (from the user):** there is no team, the game will be produced entirely using AI.

**Consequences:**
- This turns the art style decision into a technical decision. See the section below.
- The durations in the roadmap have to be re-evaluated for one person. The art production phase gets shorter with AI, but integration and consistency checking get longer.
- Scope discipline becomes critical. Scope explosion, the biggest risk item in the research, is the most common cause of death in a one-person project.
- The licensing of AI-generated assets has to be cleared up before release. The free output of some tools carries an attribution requirement.

---

### Art style: soft low-poly ✅ decided

After the comparison, the recommended direction was chosen. A hybrid approach is valid: the scene and the characters low-poly, the dialogue portraits and the interface icons possibly pixel art.

### Steam release: a planned target ✅ decided

**Reason (from the user):** the game will also be released on Steam later. For that reason the project will be planned in layers, so that making changes later is advantageous in both cost and time.

**Consequences:**
- The input layer is set up with two schemes from the start: touch and desktop.
- Every platform-dependent capability goes behind port interfaces.
- All of the architectural decisions are in [04-architecture.md](04-architecture.md).
- The Steam implementations are not written in the first three steps, only the ports are kept defined.

---

## The reasoning for the art style

Comparison page: https://claude.ai/code/artifact/0e98e412-5ec7-4c99-95a8-d532384f161e

The same restaurant scene was drawn in two styles and can be compared at phone size.

### The finding that decided it

In AI-assisted production, **2D sprite games are harder than 3D.** The reason is that in a sprite-based game there is no shared object. Every frame is an independently generated pixel map. The second frame of a walk cycle can come out as a different character from the first, the light source moves between frames, proportions drift.

In 3D there is a single model and it is consistent from every angle.

### The comparison

| Dimension | Pixel art | Soft low-poly |
|---|---|---|
| Production with AI | Weak, every sprite independent | Strong, a clean mesh ready for texturing |
| Animation | Six states × 8-16 frames, hand correction mandatory | Automatic rigging and a ready animation library |
| New furniture | Redraw for every angle | Put the model in the scene |
| Camera | A fixed angle is mandatory | Rotation and zoom are free |
| Small screen | Detail can turn into noise | The silhouette is clear at every scale |
| Distinctiveness | High | Medium, compensated with palette and light |
| Fit with Unity | Wants pixel alignment settings | Direct |

### The direction chosen: soft low-poly

Expanding the venue is one of the game's main mechanics. Drawing every new piece of furniture by hand for every angle is a cost that compounds over time in a one-person project.

**The hybrid solution:** the scene low-poly, the dialogue portraits and the interface icons possibly pixel art. Because a portrait is a single fixed image, it raises no animation consistency problem. That way the warmth of pixel art is kept exactly where the storytelling is.

### How distinctiveness is achieved

Low-poly's only weak side is the risk of looking generic. The way to close it:
- Warm evening light and strong shadow contrast
- A limited and steady colour palette
- Slightly thickened, readable silhouettes
- Hand-prepared character portraits

---

## Draft production pipeline (if low-poly is chosen)

1. **Scene and furniture.** Table, chair, counter, stove, shelf. Text-to-3D tools or ready low-polygon packs. All tied to a single style guide.
2. **Characters.** Generate a humanoid base model, upload it to an automatic rigging service, take the ready animations. Walking, sitting, serving, waiting.
3. **Light and palette.** This is where distinctiveness comes from. It decides more than the models do.
4. **Portraits and interface.** No consistency problem, because they are single frames.
5. **Licence audit.** Before release, the right to use every generated asset is verified.

**Warning:** the names and the capabilities of AI generation tools change fast. The current state has to be verified before this pipeline is built.

---

## Questions still awaiting an answer

1. **How far the first release's scope goes.** Recommendation: menu, staff, supply and venue expansion included; a second branch excluded.
2. **Theme and cuisine identity.** Recommendation: a specific identity, for example a tradesman's lokanta.
3. **Revenue model.** Recommendation: on mobile, free plus a one-time unlock; on Steam, sold up front. Thanks to the port boundary this decision does not affect the architecture.
