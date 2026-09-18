# Restaurant Management Game — Project Documents

Mobile (iOS/Android) and Steam later on. 2.5D, soft low-poly. Unity. A one-person, AI-assisted production.

**The concept:** you are not the chef, you are the owner. You set the menu, the prices, the supply and the crew; during service you only step in when there is a crisis; and every week you have to meet rent day.

**The approach:** implementation does not begin before the plan is complete. For the current state of the plan, see [06-plan-status.md](06-plan-status.md).

## By topic

The files are **chronological**: every number is the record of a piece of work.
The groups below are only a second door — the numbering and the order do not
change.

**Start here** — [06](06-plan-status.md) the state of the plan and of the work · [02](02-design-proposal.md) what the game is · [04](04-architecture.md) layers and the port boundary · [23](23-core-contract.md) the core's invariants

**Design** — [07](07-cuisine-system.md) two cuisines, signature mechanics · [08](08-endgame.md) day sixty and seven axes · [09](09-content-inventory.md) 32 dishes · [11](11-customer-system.md) customer archetypes · [34](34-progression-and-locks.md) locks, seasons, the customer who asks · [47](47-recognition-and-report-card.md) badges and the weekly report card · [53](53-pending-decisions.md) the dishwasher, the combo, the staff's voice

**Economy and balance** — [12](12-economy.md) numbers, rent, wages, formulas · [27](27-time-model.md) the day and the tick · [28](28-peak-decision.md) the shape of the peak · [29](29-phase0-simulation.md) the simulation and the balance tool · [32](32-equipment-and-rebalance.md) station slots · [42](42-crew-and-intervention.md) the crew cap, the intervention budget · [48](48-day-sharpness.md) sharpening the day · [52](52-split-and-rent.md) the kitchen split and the rent

**Core and data** — [03](03-technical-decisions.md) engine, platform, art style · [13](13-data-schemas.md) JSON schemas · [15](15-save-system.md) four slots, version migration · [19](19-technical-setup.md) project setup · [14](14-staff-system.md) roles, traits, morale · [39](39-plate-cycle.md) counted plates, the washing-up shift · [51](51-self-service.md) fast food's service model · [33](33-second-cuisine.md) Turkish cuisine

**Interface, art and venue** — [16](16-screens-and-tutorial.md) screen flow and the first ten minutes · [41](41-ui-and-venue.md) the card HUD, colour roles · [24](24-art-pipeline.md) the model pipeline · [10](10-cuisine-identity.md) wardrobe and environment identity · [30](30-venue-layout.md) the floor plan · [31](31-rooms-and-camera.md) the room view, the camera, URP · [35](35-animation-and-camera.md) the animation layer · [36](36-street-time-and-kitchen.md) the street and the time of day · [38](38-street-and-interior-light.md) night light · [50](50-kitchen-texture.md) texture generation · [17](17-audio-design.md) audio layers

**Text and language** — [18](18-story-and-text.md) story arcs and the word budget · [40](40-two-languages.md) two languages from one generator · [54](54-five-languages.md) five languages, Arabic joining, mirroring · [55](55-translation-review.md) a four-agent translation review · [56](56-english-repository.md) the repository written in English

**Review and measurement** — [22](22-answers-and-direction.md) questions and direction · [37](37-four-agent-review.md) the four-agent review · [43](43-review-and-measurement.md) where the measurement misled us · [45](45-design-review.md) the five-agent design pass · [49](49-unreachable-mechanics.md) mechanics that never reach the player · [57](57-end-to-end-audit.md) the end-to-end audit: the busy slot was the empty one · [58](58-visual-review.md) the visual round: a blank icon, a shadowless floor, an inverted sky · [59](59-kitchen-equipment-and-models.md) the kitchen you can see: equipment, models, animation · [46](46-shipped-binary.md) the shipped binary, link.xml

**Release** — [05](05-production-plan.md) tools, licences, cost · [20](20-production-decisions.md) production decisions · [21](21-business-and-release.md) the release audit and the work that is left · [44](44-store-texts.md) store texts, the privacy policy · [25](25-game-name.md) the name decision — OPEN

## Documents

| File | Contents | Status |
|---|---|---|
| [research/01-market-research.md](research/01-market-research.md) | Ten games examined, the mechanics that are loved and hated, market data | Done |
| [02-design-proposal.md](02-design-proposal.md) | Game mechanics, the daily loop, the collapse ladder, the roadmap | v0.1 |
| [03-technical-decisions.md](03-technical-decisions.md) | Engine, team, art style and platform decisions | Current |
| [04-architecture.md](04-architecture.md) | Layered architecture, the port boundary, the core's independence from the engine | Current |
| [05-production-plan.md](05-production-plan.md) | What will be made, and with what. Tools, licences, costs, the skill limit | Current |
| [06-plan-status.md](06-plan-status.md) | **The state of the 37 plan items and the five batches of remaining work** | Current |
| [07-cuisine-system.md](07-cuisine-system.md) | Cuisine choice, signature mechanics, the revenue model and three conditions | Current |
| [08-endgame.md](08-endgame.md) | The end of the game: a scored year-end evaluation | Approved |
| [09-content-inventory.md](09-content-inventory.md) | The content inventory (v2, 32 dishes), the shared base and the sixty-day progression curve | Current |
| [10-cuisine-identity.md](10-cuisine-identity.md) | The character wardrobe system, the cuisine-specific environment and the character count | Current |
| [11-customer-system.md](11-customer-system.md) | Customer archetypes (v2, 20 of them), frequency tiers and eight parameters | Current |
| [12-economy.md](12-economy.md) | Economy numbers, rent, wages, margin, credit and the customer formulas | Current |
| [13-data-schemas.md](13-data-schemas.md) | Eleven JSON schemas, the file layout and start-up validation | Current |
| [14-staff-system.md](14-staff-system.md) | Four roles, twelve traits, morale, experience and hiring | Current |
| [15-save-system.md](15-save-system.md) | Four slots, corruption protection, version migration, cloud saves | Current |
| [16-screens-and-tutorial.md](16-screens-and-tutorial.md) | Eighteen screens, the flow rules and the first-ten-minutes plan | Current |
| [17-audio-design.md](17-audio-design.md) | Five audio layers, layered service music, the production load | Current |
| [18-story-and-text.md](18-story-and-text.md) | Story arcs, templated reviews, the word budget, localisation | Current |
| [19-technical-setup.md](19-technical-setup.md) | Unity version, URP settings, performance budgets, the save format, the input map | Current |
| [20-production-decisions.md](20-production-decisions.md) | The model path, the character pipeline, the audio source, the typeface, the test plan | Current |
| [21-business-and-release.md](21-business-and-release.md) | Price, launch stages, the metrics to measure, the legal must-haves | Current |
| **[review/00-synthesis.md](review/00-synthesis.md)** | **The synthesis of the five-agent review: 3 approve, 21 revise, a seven-batch fix plan, nine open questions** | **Current** |
| [review/01-05](review/) | The five raw review reports: design, architecture, scope, market, player experience | Current |
| **[22-answers-and-direction.md](22-answers-and-direction.md)** | **Answers to the nine questions, the machine scan, the zero-budget plan, a recommendation for the two open decisions** | **Current** |
| **[23-core-contract.md](23-core-contract.md)** | **Batch B: determinism, integer state, RNG, culture, the command log, schema additions, acceptance criteria. Binding** | **Binding** |
| **[24-art-pipeline.md](24-art-pipeline.md)** | **The render loop, three tiers, the character plan, modular plating, time boxes** | **Current** |
| [25-game-name.md](25-game-name.md) | Ten name candidates, a store collision scan, a recommendation | Open |
| `../tools/balance/` | The balance model: `model.py` the formulas, `solve.py` the parameter search, `render.py` writing into the documents | Working |
| `../tools/art/` | Blender generation scripts; `gen_table.py` is the validation example | Working |
| `../tools/store/` | `compose.py`: pads every store screenshot with its own edge rows to 2183x1120 (1.949 : 1), because the shot frame is 2.22 : 1 and Play refuses anything over 2 : 1 | Working |
| `../src/Lokanta.Core/` | The pure C# core: `Fx` integer arithmetic, `Rng`, the demand and crew models. No Unity reference | Phase 0, slice 1 |
| `../src/Lokanta.Content/` | JSON loading and validation. Newtonsoft only here | Phase 0, slice 1 |
| `../tests/Lokanta.Core.Tests/` | 249 tests: arithmetic, RNG cross-validation, culture, the floating-point ban, the golden table, the simulation | All passing |
| **[29-phase0-simulation.md](29-phase0-simulation.md)** | **Phase 0, second slice: the simulation, the balance tool and the six design bugs the simulation found** | **Current** |
| [27-time-model.md](27-time-model.md) | The service day, task durations, deriving prep time, concurrency, peak occupancy | Current |
| [28-peak-decision.md](28-peak-decision.md) | The decision on the contradiction between the kitchen's hour distribution and capacity | Current |
| [30-venue-layout.md](30-venue-layout.md) | The plot, the room grid, tier unlocking and furniture placement | Current |
| **[31-rooms-and-camera.md](31-rooms-and-camera.md)** | **A two-tier camera, a measured touch target, the furniture/character orientation rule** | **Current** |
| [32-equipment-and-rebalance.md](32-equipment-and-rebalance.md) | Station slots, `attendBp` and the calibration of the realisation rate | Current |
| [33-second-cuisine.md](33-second-cuisine.md) | Turkish cuisine: dish groups come from the content, validated in three places | Current |
| [34-progression-and-locks.md](34-progression-and-locks.md) | The progression lock, complexity, seasons and bringing five pieces of dead content to life | Current |
| **[35-animation-and-camera.md](35-animation-and-camera.md)** | **The walking layer, a working oven and stove, two-finger zoom and the square table** | **Current** |
| **[36-street-time-and-kitchen.md](36-street-time-and-kitchen.md)** | **Doors between rooms, the street and passers-by, the time of day, the kitchen's stages, the tray** | **Current** |
| **[37-four-agent-review.md](37-four-agent-review.md)** | **The four-agent review: the price ceiling, the combo gap, patience freezing, the release gates, the crisis strip** | **Current** |
| **[38-street-and-interior-light.md](38-street-and-interior-light.md)** | **The layout of the street lamps and the classic lantern model, the three layers of night light, pedestrian collision and figure profile, interior ceiling lighting, removing the wings** | **Current** |
| **[39-plate-cycle.md](39-plate-cycle.md)** | **The inherited crew (cook + waiter), the counted-plate cycle, the washing-up shift, the seating threshold that breaks the death spiral** | **Current** |
| **[40-two-languages.md](40-two-languages.md)** | **English localisation: two tables from one generator, the translation policy, the default by device language, the two bugs the tour found** | **Current** |
| **[41-ui-and-venue.md](41-ui-and-venue.md)** | **Redesigning the game screen against the reference (cards, capsules, three colour roles), procedural decoration and kitchen identity** | **Current** |
| **[42-crew-and-intervention.md](42-crew-and-intervention.md)** | **The passive player now goes under; intervention pays off under pressure; the crew recommendation had the wrong name** | **Current** |
| **[43-review-and-measurement.md](43-review-and-measurement.md)** | **The five-agent review: the tour's 107 checks could not break anything; checks that stay green in a vacuum; a measurement consuming its own source** | **Current** |
| **[44-store-texts.md](44-store-texts.md)** | **The store's short/full description (in five languages), the privacy policy text and producing screenshots at store resolution** | **Current** |
| **[45-design-review.md](45-design-review.md)** | **The five-agent design pass: price had no channel to demand (a rise at the ceiling was free), intervention was wired to the hall as well, the saturation of the combo axis turned out to be a symptom** | **Current** |
| **[46-shipped-binary.md](46-shipped-binary.md)** | **Why the emulator does not help (an ARM64-only APK) and `link.xml`: trimming protection that had never been run was measured by mutation — with it removed, the game dies at start-up** | **Current** |
| **[47-recognition-and-report-card.md](47-recognition-and-report-card.md)** | **Not a daily quest but RECOGNITION: the weekly report card (one moment of success → nine) and seven badges; a unit error was handing out a badge on day one** | **Current** |
| **[48-day-sharpness.md](48-day-sharpness.md)** | **Why pressure never fired: the busiest moment of the day was 1.24 times the average; the durations were sharpened, and the value of intervention went from +505 to +4,953** | **Current** |
| **[49-unreachable-mechanics.md](49-unreachable-mechanics.md)** | **Twenty commands scanned on four axes: ingredient quality was on no screen at all, nobody pressed the "chase" button, and early collection turned out to be a trap** | **Current** |
| **[50-kitchen-texture.md](50-kitchen-texture.md)** | **Wall surfaces by cuisine (wood panelling / steel banding), the floor and the tone of the outside; the tour now plays fast food too, and the clipping measurement was hunting a bug that did not exist** | **Current** |
| **[51-self-service.md](51-self-service.md)** | **Fast food self-service: a cleaner instead of a waiter, the hall roles wired to the kitchen, volume +43% and the ticket −22%; volume is not a lever for the total till** | **Current** |
| **[52-split-and-rent.md](52-split-and-rent.md)** | **Fast food rent ×1.15 (raising the rent *raises* the bot's till — measured); the screenshot tool was not drawing self-service at all, the hanging menu panels were covering what was behind them, and the furniture had turned to laminate** | **Current** |
| **[53-pending-decisions.md](53-pending-decisions.md)** | **The open dishwasher decision was closed by measurement (two "fixes" were tried and both made it worse); the combo answer was reversed by self-service and the tour had never been pressing its button; the staff were given a voice for the first time (`trait.*.voice`)** | **Current** |
| **[54-five-languages.md](54-five-languages.md)** | **Five languages (tr/en/es/zh/ar), English by default; Arabic letter joining solved with the Advanced Text Generator and measured by the width difference (60 → 40 dp); the layout mirrored; the strip budget is measured in five languages — the longest is Spanish** | **Current** |
| **[55-translation-review.md](55-translation-review.md)** | **A four-agent translation review: the regulars' cast was a different cast in es/zh/ar; the Chinese build was broken in three places while the font check was green (the name pool + symbols embedded in code); seven strings contradicted the simulation** | **Current** |
| **[56-english-repository.md](56-english-repository.md)** | **The whole repository moved to English (253 names, 860 identifiers, 16,515 prose lines to zero) and `check_english.py` now measures it; the checker itself was wrong five times, and the renames exposed a pool token, a sound folder and an editor path that nothing was testing** | **Current** |
| **[57-end-to-end-audit.md](57-end-to-end-audit.md)** | **A five-agent audit against the fifty-six documents: the busy slot was the EMPTY slot (two staff traits had been running inverted since docs/48), an unused station raised the score, the migration test slid forward with every release, the save had no backup, `ComboOrdered` reached nobody, and the change log had stopped six days earlier** | **Current** |
| **[58-visual-review.md](58-visual-review.md)** | **A four-reviewer visual round: the app icon was a BLANK GREY SQUARE wired in at six densities and the release audit called it Ready on its file size; nothing inside the restaurant received a shadow; by day the sky was twice as bright as the subject; the reputation number was illegible at every value; hiding the scrollbar put the language picker below an invisible fold** | **Current** |
| **[59-kitchen-equipment-and-models.md](59-kitchen-equipment-and-models.md)** | **The kitchen you can see: docs/32 built a whole equipment ladder and the kitchen drew none of it - three stove prefabs stood for sixteen stations by `station % 3`, so buying a grill changed nothing on screen. One object per station, the tier in the geometry, a fryer for the eight fast-food dishes that were all deep fried on a hob, the dishwasher's two hands and a basin with water in it, and the backdrop's silhouettes replaced by a sky** | **Current** |
| `../unity/` | The Unity 6.3 LTS project, targeting Android. The settings are applied in code by `ProjectSetup.cs` | Set up |
| `../src/Lokanta.Harness/` | The balance tool: five strategies, a multi-seed sixty-day campaign | Working |

**The status column was corrected on 16 September 2026.** Thirteen rows still
said "Open"; this document's *own* subsection ("The plan register — closed")
says those same items were closed on 9 September and that implementation started
after that. In other words, the table was contradicting the section below it. The
one exception is [25-game-name.md](25-game-name.md): the name and the trademark
scan really are open.

*The column states the status of the document, not the status of the work. What
is left before release is in
[21-business-and-release.md](21-business-and-release.md).*

## Published readable versions

- Research and design proposal: https://claude.ai/code/artifact/409721e3-d115-4985-a498-f54b8720e47e
- Art style comparison: https://claude.ai/code/artifact/0e98e412-5ec7-4c99-95a8-d532384f161e
- Layered architecture: https://claude.ai/code/artifact/521351fd-1618-4372-a271-c9e72484bc42
- Production plan and the register of gaps: https://claude.ai/code/artifact/e338ccdd-44d2-4547-ac96-1134f43fb943
- The cuisine system and the revenue model: https://claude.ai/code/artifact/a1ae2487-ed9b-4bf0-bfa4-32d05df221af
- The end of the game: https://claude.ai/code/artifact/23501b45-3584-406c-a7a6-10fb0760b760
- Content inventory and progression: https://claude.ai/code/artifact/c1201234-32b7-4cde-8342-4d3715d166c0
- Cuisine identity and the character system: https://claude.ai/code/artifact/ee1d0bfa-4654-4087-ac42-cffc9ca6fa1b
- Customer archetypes: https://claude.ai/code/artifact/51069155-7773-40aa-ab89-5bcf0fa8b63f
- Economy and formulas: https://claude.ai/code/artifact/9cbe26ce-ddc4-4626-95a4-087d941eb13d
- Coin icon candidates: https://claude.ai/code/artifact/a90e6a88-de82-429b-b03c-f5552451a8f1
- The staff system: https://claude.ai/code/artifact/258d1c68-2b67-45b6-a3c7-922236b97004
- Player experience (tutorial, audio, text): https://claude.ai/code/artifact/7971d53e-7ba6-4c70-b7ee-7545d94a6990
- **Plan status and the review list: https://claude.ai/code/artifact/f4a38388-fa0f-44ac-b36e-fa3d65ce6a6b**
- **The five-agent review synthesis: https://claude.ai/code/artifact/27dd1ec2-42d8-4b52-aeb1-88b7fe1f7901**

## How complete the plan is

| Area | Decided | Open | Not written |
|---|---|---|---|
| A. Design | 5 | 11 | 0 |
| B. Technical | 2 | 5 | 0 |
| C. Production | 3 | 5 | 0 |
| D. Business | 3 | 3 | 0 |
| **Total** | **13** | **24** | **0** |

For the detail and the description of every item, see [06-plan-status.md](06-plan-status.md).

## Decisions made

| Topic | Decision |
|---|---|
| Engine | Unity |
| Team and production method | One person, AI-assisted |
| Art style | Soft low-poly, portraits may be pixel |
| Platform | Mobile first, Steam later, layered planning |
| Architecture | The core is pure C#, the platform sits behind ports |
| Texturing | A palette atlas or vertex colour |
| Version control | Git plus Git LFS |
| Localisation | Turkish and English, the text in files |
| Revenue model | Cuisine content purchases, no power for sale |
| Scope | Venue expansion included, a second branch excluded |
| Cuisines | Fast food free, Turkish paid, Italian and Japanese as updates |
| The end of the game | A scored evaluation on day 60, free play after it |
| Currency | A nameless plain stack-of-coins icon plus a number. No real currency symbol |

## Where we stand — 17 September 2026

**The game plays end to end.** The main menu, cuisine selection, four save
slots, the sixty-day campaign, the year-end evaluation and free play; two
cuisines, five languages, the animation layer, day-night lighting and an Android
package.

| measure | state |
|---|---|
| Core tests | 249, all passing |
| `tools/check.py` | 18 checks, all clean |
| Smoke tour (a real Windows build, 873×393 dp) | 196 checks, 0 failures (fast food; 1 has nothing to measure in that cuisine) |
| Balance tool | 25 strategies × 60 days, zero reconciliation gap |
| Android APK | 90.6 MB, 0 warnings; arm64 only, libraries uncompressed and 16 KB aligned |

*The numbers above were read off `tools/check.py`, `tools/unity/tour.ps1` and
`tools/balance` on the date in the heading, and they are copied here by hand —
so they go stale. The previous version of this block said they were "not written
by hand", which was not true, and by the time anyone looked it was claiming 225
tests, 13 checks and two languages. **Re-read the tools rather than trusting
this table**; it is a snapshot, not a measurement.*

### What is left before release

The technical side is ready ([21](21-business-and-release.md)'s release audit
verified it from inside the package: no permissions, no data collected, 64-bit,
16 KB aligned, licences in the build). Most of what is left is **the user's
decision** — a password, an account, a legal form:

1. Generate the upload key (keystore) and **back it up in two separate places**
2. The name decision + a trademark scan — the package name is locked on the
   first upload
3. A Play Console account, then a 14-day closed test
4. The privacy policy URL — the text is ready, [44](44-store-texts.md)
5. The Data Safety form — the answer is ready: "no data collected"
6. The IARC age rating
7. **The money model.** There is no purchase code; the game can be released today
   as free with both cuisines open. Going from free to paid is impossible, so
   this is an irreversible decision
8. The feature graphic (1024×500). Screenshots are automatic:
   `tools\unity\tour.ps1 -Store`

### What had to be known before the first update — **closed**

It used to say that if `Simulation.SaveVersion` went up, every player's sixty-day
campaign would be gone. **The migration path has been written and proven to run**
([53](53-pending-decisions.md) §8):

- `Restore` now accepts the range `MinReadableVersion`–`SaveVersion`.
- Fields added in a given version are read behind an `if (version >= N)` gate; in
  an older save they are skipped and left at their default.
- `SaveTests.An_old_version_save_opens` is a theory over **every** version in
  `MinReadableVersion`–`SaveVersion-1`: for each one it takes a real current save,
  deletes every field added after that version and loads it.
  `A_version_that_is_too_old_is_rejected` says that the gate is still a gate.

**For the next version:** read the fields with `if (version >= N)`, write a line
in the `SaveVersion` list, and add a row to `SaveTests.FieldsAddedIn` —
`Every_readable_version_is_covered` goes red until you do. The file's old rule (*"new
fields are read with `Has()`"*) is **void** — it had been applied in two of 126
reads and it was the wrong tool: `Has()` always treats a missing field as
legitimate, so it cannot tell a corrupt save from an old one.

### Open balance questions

1. Which market should the soft launch be in
2. Is the 4.99 band right for the price of a cuisine (depends on item 7)
3. Should there be no analytics at all, or a minimal one

---

## The plan register — closed

Thirty-seven items were written on 9 September 2026, all of them were closed, and
implementation started after that. The agreement *"implementation will not begin
before the whole register is closed"* was kept; this section stands as a
historical record.

1. ~~The scope and theme decision~~
2. ~~The end of the game~~ — a scored year-end evaluation
3. ~~The content inventory~~ — 32 dishes
4. ~~Cuisine identity~~ — the character and environment system
5. ~~Customer archetypes~~
6. ~~Economy numbers and the customer formulas~~ — balanced, see [42](42-crew-and-intervention.md)
7. ~~Batch 1~~ — data schemas, the staff system, the save system
8. ~~Batch 2~~ — screens, the tutorial, audio design, story texts
9. ~~Batches 3, 4 and 5~~ — technical setup, production decisions, business and release
