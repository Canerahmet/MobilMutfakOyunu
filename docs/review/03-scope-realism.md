# Review 3: Scope Realism

**Perspective:** senior independent developer who has shipped three games alone, two of them mobile, and has watched plenty of solo projects die of scope
**Items owned:** C1, C2, C4, C5, C8, A13, A15
**Date:** 9 September 2026

---

## Overall assessment

One person can ship this game, but not this plan. The plan reads like the content list of a three-person studio; the roadmap (02 §11) was never rescaled after the "one person" decision (03) was made. 03 itself says "the durations in the roadmap must be re-evaluated for one person", and 02 is still v0.1 and asks in §13 "are you on your own?". The code side really is carried by AI. What will kill this are the items that need visual judgement: 64 dish models, 32 outfit sets, 26 portraits. There is no eye on the team: the AI cannot see a mesh and the developer cannot fix one. If the content is halved it ships; as it stands, in month seven this becomes a project with its art half done.

## Asset count and time estimate

The release scope gathered from the documents (two cuisines):

| Item | Count | Source |
|---|---|---|
| Dish models | 32 × 2 = **64** | 09. Note: the 20 C1 table says "dish visuals come from outside", i.e. not procedural |
| Ingredients | 26 × 2 + 12 = 64 icons | 09. Whether they need to be 3D is not written |
| Cuisine-specific equipment | 10 × 2 = 20 | 09 |
| Shared furniture | 24 | 09 |
| Architectural shell | 2, each with 4 expansion tiers (4/7/10/14 tables) = 8 layouts | 09, 10 |
| Character base | 1 body, 3 body types, 8 hairstyles, 6 skins, 10 accessories, 7 clips | 09, 10, 05 |
| Outfit sets | 16 × 2 = **32** | 10 |
| Portraits | 13 × 2 = 26 | 10 |
| Screens | 18 | 16 |
| Audio | 8 music tracks (+1 year-end piece), 7 ambience layers, ~72 short sounds (40+12+20) | 17 |
| Text | ~10,400 × 2 = ~20,800 words, 120 template fragments, 26 × 3-4 = 78-104 beats | 18 |

The time estimate. The ratios are my assumption; they are not in the documents:

| Work | Assumption | Person-months |
|---|---|---|
| Code and Unity integration: 18 screens, core, save, IAP, tutorial | The AI writes it; wiring, on-device debugging and UI layout are on the developer. ~2 days per screen plus ~8 weeks for core/save/IAP | 4-5 |
| Procedural furniture: 24 + 20 + plates/glasses | A script plus a loop until it looks good | 1 |
| 64 dish models | 1.5-2 hours per piece for generation, simplification and fitting to the palette | 1-1.5 |
| Characters: body, rig, clips, 8 hairstyles, 32 outfits, weight transfer | Half a day to a day per set; ×3 if body types are separate meshes | 1.5-2.5 |
| 2 shells, 8 layouts, cuisine-specific décor | 2-3 weeks per cuisine | 1-1.5 |
| 26 portraits | 2-3 hours per portrait for consistency | 0.5 |
| UI icons | An editor tool that renders icons from 3D | 0.25-0.5 |
| Audio: 9 music tracks, 7 ambiences, ~72 effects | Generation, loop points, mixer | 0.5-0.75 |
| Text: 20,800 words of editing, template testing, wiring the beats | ~300 words/hour of editing | 0.75 |
| Phase 0 balance harness, tests, store, legal, video | | 1-1.5 |
| **Total** | | **11.5-15.5 person-months, midpoint 13-14** |

The roadmap (02 §11): 2-3 + 4-6 + 4 + 8-12 + 4 = 22-29 weeks, i.e. 5-7 months. A realistic estimate is roughly twice that, and that assumes full time. The real break is in Phase 3: the art rows of the table alone are 6-8 person-months and the plan has allotted 8-12 weeks for them. Phase 1's vertical slice of "6 dishes, 3 staff, 10 days" is right; the release content is ten times that.

## The five riskiest problems

**1. The roadmap was written before the solo decision and never corrected.** What kills solo projects is not bad code, it is the morale and the money running out with the art half done. In month seven the developer looks at it thinking "I should have shipped by the plan" and walks away. Fix: rewrite 02 §11 on 13-14 person-months, stretch Phase 3 to 5-6 months, and put a measurable gate on "art done": every release asset in LFS and 30 fps on the lowest device.

**2. The character pipeline sits right on top of the skill gap.** The fallback plan in 20 C2 saves the rig, not the clips: where "eating" and "sitting at a table" come from if Mixamo shuts down is not written. Each of the 16 outfit sets has to change the silhouette (10, "what actually reads"), which means mesh, not colour; every mesh wants a weight transfer onto the rig, and if the 3 body types are separate meshes then 32 sets become 96 meshes. Weight transfer is work verified by visual judgement in Blender — exactly what the developer has said he cannot do. Fix: download every clip today and put it in LFS; do body types with bone scaling, one mesh; cut outfits to 8-10 per cuisine, because the only ones that must read from the silhouette are the 8 archetypes in the "frequent" tier (70% of the traffic, per 11); as insurance, budget a paid character pack that comes rigged.

**3. The 64 dish models have fallen into the gap between two documents.** When 09 raises the dish count to 32 it uses the argument that "dish models can be generated procedurally"; 20 C1 puts the same dishes in the "comes from outside" column. Karnıyarık, mantı and işkembe çorbası are in no public-domain pack; what is left is paid AI generation plus simplification, i.e. 1-1.5 person-months of hidden work. Fix: script the stews, soups, rice and meatballs (a bowl plus a coloured surface, a heap, an ellipsoid); save Meshy for 5-10 hero dishes such as the burger; or ship with 09's v1 number of 20 dishes.

**4. Content was grown while there was no playable prototype.** On 9 September, in a single day, dishes went 20→32, archetypes 6 or 8→20 (09 and 11 do not agree), outfits 12→16, named characters 8→13; 12 still says "not balanced". 09's own sentence: "the real cost is not the model, it is the balance." 64 dish rows, 32 archetypes and a 60-day curve will be tuned on top of an economy nobody has touched. This is scope explosion that began in the planning phase. Fix: keep the v2 numbers as a schema ceiling, freeze the release at the v1 numbers and give the rest as free post-release content; better for retention too.

**5. The validation loop cannot be measured.** 20 C8 asks 5-15 people "is anyone bored on day ten", but where those people come from is the file's own open question. Measuring boredom with five people is noise; friends do not tell the truth. Reaching day ten costs at least 10 × 3-6 minutes per person, over several sessions. And an iOS device is said to be "if possible" while there is no Mac in 05's cost table; 21 says "cheap user acquisition" and the table says $124. Fix: measure boredom instead of asking about it; record an "abandoned on day N" event from the start of the closed test (21, 2 weeks); add the question "does the best strategy change after day ten" to the balance harness, which is automatic plateau detection; find testers through indie Discords and Turkish indie communities with an open test of 20-30 people; decide that the first release is Android.

## Item decisions

| Item | Decision | Reasoning |
|---|---|---|
| C1 Model production path | REVISE | The order is backwards: do the vertical slice and the fun test with a ready-made pack (02 §11 already says "placeholder"), and box the scripting into two weeks. Realistic for boxes and cylinders; chairs and a "soft" feel can eat weeks. If the developer cannot debug Blender Python, the fallback is Kenney/Quaternius plus a selected paid pack. The script should emit a contact-sheet PNG on every run so the AI can see the result. Take the dishes out of the "from outside" column |
| C2 Characters and animation | REVISE | The fallback plan does not cover the clips; body types by bone scaling; 16 sets → 8-10; 7 clips → 5 (walk, sit, eat, idle, one reaction). A script plus a visual check step must be written for the weight transfer |
| C4 Audio source | APPROVE | The cheapest item, and 20 is right. Two conditions: it must be verified that the tool can produce stems for the layered service music (17), and if it cannot, ship with two layers (base and full) and a crossfade; the monthly budget line is empty and must be filled |
| C5 UI and typeface | APPROVE | The most finished item in the plan: OFL, a Turkish test sentence, tabular figures, the 16/24/48 pixel rule. Addition: an editor tool that renders dish and ingredient icons from 3D; Turkish strings are longer than English, so all 18 screens should be tried in both languages on the lowest device |
| C8 Test plan | REVISE | The unit-test and balance-harness layers are right; the human layer has no source and no measure. Turn the boredom question into telemetry and a harness plateau metric; write down where the people come from; if there is no Mac, take iOS out of the first release |
| A13 Story and text | REVISE | The volume is carriable by AI; the bottleneck is editing and beat testing. Drop the fourth beat (18 already calls it optional); a threshold of 25 visits over 60 days means almost every other day, and most players will never see it, so the thresholds should be tuned with the balance harness; regulars 10 → 6, staff 3 → 2; the critic 5 → 3 tiers; in Turkish templates the variable must stay in the bare nominative ("{yemek}'i" and other suffixed forms break vowel harmony) |
| A15 Cuisine identity | REVISE | The principle is right and strong: not faces but shell, light, silhouette, rhythm. But the "production cost" table in 10 writes "16 outfits" and hides the mesh, weight and body-type multiplier; the combination count appears twice in the same file as 23 thousand and 17 thousand (17,280 = the number that comes out of v1's 12 sets). Outfits down to 8-10, and settle whether the uniform is inside the 16 or outside it |

## Three things to cut first, one thing never to cut

**To cut, in order:**

1. **The wardrobe.** 16 sets → 8-10 sets, 3 body types → one mesh plus bone scaling. On its own that is 1-1.5 person-months and it removes the most dangerous skill gap. The crowd still produces thousands of appearances.
2. **The dish count.** 32 → 20 per cuisine (09's v1 number). For a 4-8 dish menu, 20 is still 2.5-5 times the capacity; the choice stays meaningful. The remaining 12 dishes become a free post-release update.
3. **Named characters.** 13 → 8 per cuisine (6 regulars, 2 staff), beats 3-4 → 3. Portraits 26 → 16, beats ~91 → 48; the word budget and the unlock testing halve.

If that is not enough, the last resort is to ship a single cuisine with fast food and make Turkish cuisine the first update; because it breaks the revenue model (D1, D3) it is the last thing to cut, not the first.

**Never to cut:** the intersection of the tab book and the named regular in Turkish cuisine. In 18's words "the game's strongest design intersection", the strongest retention tool in the research, and the only real justification for the 4.99 purchase. The count comes down to six, the system stays. On the process side the Phase 0 balance harness is untouchable too; every content number depends on it.

## Unanswered questions

1. Is the developer full time or working evenings? It is in no document; this is the multiplier between person-months and calendar months.
2. Is there a Mac? Without one there is no iOS build; 05's cost table and 20's device matrix do not see this.
3. Has the developer ever opened Blender, can he see that a mesh is "wrong"? What is the time box on the scripting path?
4. Are the 3 body types separate meshes or bone scaling? That is the difference between 32 and 96 outfit meshes.
5. Is the staff uniform inside the 16 sets or extra? (09 says "the uniform changes with the cuisine", and 10's table counts "staff clothing" separately.)
6. If Mixamo shuts down, where do the sitting and eating clips come from?
7. Which audio tool, how much a month, can it produce stems?
8. On the Market screen, are ingredients 3D or icons?
9. Where do the playtesters and the soft-launch user acquisition budget come from?
10. Inconsistencies: 23,040 vs 17,000 combinations (10), 8 vs 9 music tracks (17), v1 archetypes 6 vs 8 (09 vs 11). Small, but the trace of a plan that grew several times in a single day.
