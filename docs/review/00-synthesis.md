# The Five-Agent Review: Synthesis

**Date:** 9 September 2026
**Input:** five independent reviews, each from a different expert perspective and each with only the documents in its own field. The raw reports are in this folder, 01 to 05.
**Scope:** the 24 items in the register awaiting approval.

---

## The verdict

| Decision | Items |
|---|---|
| APPROVE | 3 |
| REVISE | 21 |
| REJECT | 0 |

**No item was rejected. Almost none was approved as it stood, either.** The five agents share a diagnosis: the structure is right, the reasoning is tied to the research, the principles are sound. But the numeric and detail layer does not hold together.

Two agents' one-sentence verdicts sum up the state of the plan:

- The scope realist: **"One person can ship this game, but not this plan."**
- The publisher: **"It is not that the plan makes no money; it makes no money in the order it is planned."**

The three approved items: the save file format (B6), the audio source (C4), and the UI and typeface (C5). All three are conditional approvals.

---

## Where the five agents converge

These are the problems more than one agent found independently, arriving from different documents. They are the most reliable findings.

### 1. The numbers do not follow from the formulas

**Three agents, from three angles.** The designer recomputed the economy table: week two's net is 933, not 560, and week three's is +1,810, not −322. The story that "the expansion week costs you money" is not a design decision but the result of an arithmetic error. In the capacity model, 58 customers need 12 staff, not 8; the sentence "the cap was tuned to the need" is wrong by its own numbers. The architect found that the same capacity model is nowhere in the schemas. The UX expert found that the tutorial's first staff lesson contradicts the capacity model.

**Conclusion:** the Phase 0 balance harness must start from the formulas, not from these tables. The tables should be output, not input.

### 2. The Market stage is unresolved

**Two agents.** The UX expert counted the touches in a day: 80 in the raw flow, against a budget of 60, with the Market alone at 40. The designer found a contradiction of principle in the same stage: 02 principle 2 says "ingredient ordering becomes automatic" while 02 §3 makes the Market "the second loop". Because 80% of fast food ingredients do not spoil, the Market turns into a game of "stock up on a cheap day" — and in the free, tutorial cuisine at that.

**Conclusion:** the Market will either be an automatic baseline order plus 3-5 manual decisions a day, or a real second loop. It cannot be both.

### 3. The scope grew while there was no prototype

**Two agents.** The scope realist counted the assets and estimated the time: 13-14 person-months, against a roadmap that says 5-7 months. In a single day dishes went 20→32, archetypes 8→20, outfits 12→16 and named characters 8→13, and the economy is still unbalanced. The designer arrived at the same point by a different road: 32 dishes are only meaningful if every dish carries four parameters; otherwise v1's 20 were enough.

**Conclusion:** keep the v2 numbers as a schema ceiling and freeze the release at the v1 numbers.

### 4. The documents contradict each other

**Three agents.** The combination count is both 17,280 and 23,040 in one file. Music tracks are 8 and 9. The v1 archetype count is 6 and 8. `hourSplit` and `arrivalWeights` are two sources for the same thing. The time of the first staff member is four different things in four documents. `IAnalytics` is in one list and not in another.

**Conclusion:** the trace of a plan that grew several times in a single day. It should be cleaned up in one pass.

### 5. The tutorial calendar does not hold

**The UX expert's main finding, with the designer in support.** All five things learned in the first ten minutes are punishments: price angers people, stock runs out, staff take money, rent arrives. There is no single positive peak. The rule "no collapse in the first three days" is dead, because the ladder hangs on the weekly payment and cannot fire before day seven. The designer, for his part, found that the loop goes on autopilot by day 20 and that price is solved on day one and never touched again.

---

## Found by a single agent, but critical

These fall within only one expert's field, but each one on its own is heavy enough to block Phase 0.

| Finding | Agent | Why it is critical |
|---|---|---|
| **Determinism is not designed** | Architect | `Tick(deltaTime)` lets variable frame time into the core. The RNG is undefined, floating point diverges across platforms, and the tr-TR culture breaks `ToUpper` and `Parse`. The save system and the balance harness both lean on this assumption |
| **The Steam order is backwards** | Publisher | Nine of the ten references in the research are Steam games. The audience that hates timers is there. The plan puts Steam last |
| **The conversion trigger is 4-6 hours in** | Publisher | A purchase only becomes meaningful once the free campaign is over. An estimated 5-10% of installs get there |
| **The patience warning is colour only** | UX | A red table edge, in a red-and-yellow fast food palette. A silent player and a colour-blind player do not see it |
| **The GPU Resident Drawer is the wrong tool** | Architect | It wants Forward+, does not work on GLES, and its benefit cannot be measured at 100 draw calls |
| **The character pipeline sits in the skill gap** | Scope | Weight transfer needs visual judgement. 16 sets × 3 body types = 96 meshes. The Mixamo fallback saves the rig, not the clips |
| **The legal gaps are concrete** | Publisher | Restore purchases (an Apple 3.1.1 rejection), refund revocation, the EU DSA trader address, the Steam AI declaration, and the game has no name |

---

## Where the agents disagree

There is only one real conflict: **the dish count.** The designer says "32 can stay, but only if every dish is parameterised". The scope realist says "come down to 20". Both agree that 32 without parameters is wrong.

The other apparent conflicts are actually compatible:

- The publisher wants the end-of-day rewarded ad back. The architect says `IAdProvider` is undefined and that without a definition it should be removed. If the ad is defined, both are satisfied.
- The scope realist cuts named characters from 13 to 8. The publisher counts the intersection of the tab and the regular customer as "the only real justification for the purchase". The scope realist already says "the count comes down, the system stays, this is never cut".

---

## Item-by-item decisions

| Item | Topic | Decision | Agent | In short |
|---|---|---|---|---|
| A4 | Economy numbers | REVISE | Design | Derive the table from the formula, apply the weekend factor |
| A5 | Content inventory | REVISE | Design | Four parameters per dish, menu slots grow with the tier |
| A6 | Progression curve | REVISE | Design | An event chain for season 4, or cut the campaign to 45 days |
| A7 | Customer system | REVISE | Design | Settle person/party, bind rares to a calendar, make the price penalty non-linear |
| A8 | Staff system | REVISE | Design | Derive the crew from capacity, cap 3/5/7/9, day off yes |
| A9 | Screens and flow | REVISE | UX | In-stage undo, the back button opens pause, a "Yesterday" card |
| A10 | Tutorial | REVISE | UX | One calendar, the first hire after the first loss, a positive peak |
| A11 | Save system | REVISE | Architect | Mid-service saving as a command log, writing on a background thread |
| A12 | Audio design | REVISE | UX | The visual twin must not depend on colour, add haptics |
| A13 | Story and text | REVISE | Scope | Regulars 10→6, staff 3→2, the critic 5→3 tiers |
| A15 | Cuisine identity | REVISE | Scope | Outfits 16→8-10, make the combination count consistent |
| B3 | Data schemas | REVISE | Architect | Signature mechanic parameters, a capacity block, integer units |
| B4 | Unity and render | REVISE | Architect | GRD off, HDR off, do not bake movable furniture |
| B5 | Performance | REVISE | Architect | Budgets in milliseconds, zero GC, PSS defined |
| **B6** | **Save format** | **APPROVE** | Architect | Conditional: CRC32, Flush(true), envelope meta fields |
| B7 | Input map | REVISE | Architect | The gamepad scheme now, 16:10 support, IInputSource should not be a port |
| C1 | Model path | REVISE | Scope | Prototype with a ready-made pack, the script on a two-week time box |
| C2 | Characters | REVISE | Scope | One mesh plus bone scaling, download the clips today |
| **C4** | **Audio source** | **APPROVE** | Scope | Conditional: verify it can give stems, fill in the budget line |
| **C5** | **UI and typeface** | **APPROVE** | Scope | The most finished item in the plan |
| C8 | Test plan | REVISE | Scope | The boredom question into telemetry, defer iOS if there is no Mac |
| D2 | Price | REVISE | Publisher | The bundle at 9.99 and only with two cuisines, regional pricing, a décor tier |
| D5 | Launch | REVISE | Publisher | The Steam page at the end of Phase 1, Next Fest, closed test in Turkey |
| D6 | Legal | REVISE | Publisher | Restore purchases, refund revocation, the DSA address, the AI declaration, name registration |

In an advisory capacity: for **D1, the revenue model** (which had been decided), the publisher says REVISE: turn the three-day reset window into a free taste of the paid cuisine, and bring back the end-of-day rewarded ad.

---

## The fix plan

The twenty-one revisions are split into seven batches. The first two are preconditions of Phase 0.

### Batch A — Numeric consistency *(mandatory before Phase 0)*

1. Regenerate the growth table from the formula, with the weekend factor
2. Reconcile the capacity model with the crew table and the cap: either raise capacity or lower demand, cap 3/5/7/9
3. Scale service time with the tier: 120 s at 4 tables, 180 s at 14
4. Settle the customer unit: parties for throughput, people for the bill
5. Take the rare archetypes off percentages and bind them to a calendar: 8-12 events per campaign
6. Make the price penalty non-linear, accelerating above 10%, with tolerance tied to reputation
7. Four parameters per dish: prep time, station, perishable ingredient, archetype pull
8. Define the tab: cap, term, collection calendar, chance of non-payment

### Batch B — Core architecture *(mandatory before Phase 0)*

1. Fixed step: a parameterless `Tick()`, 100 ms, `tickIndex` in the save
2. State entirely integer: milliseconds, grams, 1/100 points
3. Its own RNG in the core, a separate stream per subsystem and per day
4. Invariant culture mandatory, `string.GetHashCode` banned
5. Define the JSON library, the event mechanism and the composition root
6. Signature-mechanic parameter blocks in the schemas, `combos.json`, a capacity block
7. Mid-service saving as a command log plus replay
8. A .NET vs Android IL2CPP hash comparison in CI

### Batch C — Scope cut

1. Dishes 32 → 20, the remaining 12 free after release
2. Outfits 16 → 8-10, three body types as one mesh plus bone scaling
3. Named characters 13 → 8, beats 3
4. Rewrite the roadmap on 13-14 person-months
5. Script the stews, soups and rice; AI generation for 5-10 hero dishes
6. Download every animation clip today and put it in the repository

### Batch D — Player experience

1. The Market: yesterday's order arrives ready, orders placed in portions, target ten touches
2. A three-channel patience warning: colour plus shape plus motion on the table, an arrow at the screen edge, touchable chips on the top bar
3. The tutorial on a single calendar, the first hire after the first visible loss, a positive peak before minute five
4. Service: 1x/2x speed, a day progress bar, an intervention counter
5. In-stage undo, the back button opens pause, a "Yesterday" card, the reason a customer was lost

### Batch E — Business and release

1. The Steam page at the end of Phase 1, the Next Fest demo as the free campaign, Early Access at 9.99
2. Turn the three-day reset into a free taste of the paid cuisine
3. Bring back the end-of-day rewarded ad, only when the player presses it
4. Regional price tables, a 1.99-2.99 décor tier, the bundle at 9.99 and only with two cuisines
5. Closed test Turkey, open test the Philippines plus Canada, Android first, a $300-500 acquisition budget
6. The metrics must separate the calendar day from the in-game day; D1 ≥ 35%, D7 ≥ 15%, D30 ≥ 6%
7. Legal: restore purchases, refund revocation, the DSA address, the Steam AI declaration, name registration

### Batch F — Technical settings

1. GPU Resident Drawer off, HDR off, MSAA 4x
2. Bake only the walls and the floor
3. Budgets in milliseconds: main ≤ 10, render ≤ 6, GPU ≤ 14; zero GC during service; PSS ≤ 500 MB
4. The gamepad scheme now, 16:10 support, `IInputSource` should stop being a port
5. No remote Addressables in the first release

### Batch G — Document clean-up

1. 17,280 vs 23,040, 8 vs 9 music tracks, 6 vs 8 archetypes, `hourSplit` vs `arrivalWeights`, the time of the first staff member, `IAnalytics`

---

## The questions you have to answer

Most of the agents' questions close with design work. But these nine do not close without your knowledge or your decision.

| # | Question | Asked by | Why it matters |
|---|---|---|---|
| 1 | **Full time, or evenings?** | Scope | This is the multiplier between person-months and calendar months |
| 2 | **Is there a Mac?** | Scope | Without one there is no iOS build, and the first release is Android |
| 3 | **Have you ever opened Blender, can you see that a mesh is wrong?** | Scope | The time box on the procedural path depends on it |
| 4 | **Is the budget really zero?** | Publisher | Without $300-500 of test acquisition, a soft launch produces no data |
| 5 | **What is the game called?** | Publisher | A trademark search and registration are the only defence against clones |
| 6 | **20 dishes or 32?** | Design and Scope disagree | 32 is only meaningful if they are parameterised |
| 7 | **Should the end-of-day rewarded ad come back?** | Publisher, questioning a decision already made | The only revenue from the 98% who do not pay |
| 8 | **Should Steam be moved forward?** | Publisher, questioning a decision already made | The audience is there, and mobile conversion is low |
| 9 | **Is landscape settled?** | UX | Suggestion: stay landscape, but with a thumb-zone layout and left-handed mirroring |

---

## The next step

Once these nine questions are answered, Batches A and B get written. Phase 0 does not start until both are done, because the balance harness produces meaningless results on top of inconsistent numbers and a non-deterministic core.

Batches C through G can run in parallel with Phase 0.
