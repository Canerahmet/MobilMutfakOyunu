# Review 5: Player Experience

**Perspective:** UX lead and onboarding specialist with a background in accessibility and small-screen interaction on mobile
**Items owned:** A9, A10, A12
**Date:** 9 September 2026

---

## Overall assessment

The plan carries the right reflexes for mobile: no timers and no energy, pause at any moment, a 44pt target, a ban on modals that force reading, and the rule that "every sound has a visual twin". But the four stages on paper break in three places on a six-inch phone: the Market stage burns the touch budget on its own, and "ingredient ordering becomes automatic" in 02 §2 principle 2 still contradicts "the second loop" in 02 §3; service's most critical warning lives only in red and only inside the scene, and the free cuisine's palette is red and yellow (10); and the tutorial's first staff lesson contradicts the capacity model (14) and the economy table (12 §6). The time arithmetic does not quite hold either: the sum of the stages' lower bounds is 3 min 45 s and of the upper bounds 6 min 30 s, and the hiring and upgrade screens (16, screens 11-17) are outside that. All three items are structurally sound and none of them should be approved before the details are fixed.

## The touch count of one day

The scenario: the third week, a six-dish menu, three staff, three interventions, the report, one upgrade. Because the documents do not define the component level, these are my assumptions: quantity and price take two touches on average with plus/minus buttons, quality (02 §3, Stage 1) one touch per line, a drag one touch, and roughly ten ingredients for six dishes.

| Stage | Operation | Raw flow | With sticky defaults* |
|---|---|---|---|
| Market | 10 ingredients × (select 1 + quality 1 + quantity 2) | 40 | 6 |
| Counter | pick 6 dishes, 6 prices × 2, drag 3 staff, start | 22 | 5 |
| Service | 3 interventions × (table 1 + option 1), pause and resume 2, camera 2 | 10 | 10 |
| Books | report forward, swipe the review, upgrade screen, category, item, buy, back, tomorrow | 8 | 8 |
| **Total** | | **80** | **29** |

*Sticky defaults: yesterday's menu, prices and station assignment come back unchanged; in the Market a "repeat yesterday's order" fills the basket in one touch and the player corrects two lines. None of this is written in 16.

The conclusion: the raw flow exceeds the budget by thirty-three per cent, and the blow-up point is the Market. The second blow-up is the Counter: with a pool of 32 dishes (09) and eight price sliders it passes thirty on its own by the third season. The budget only holds with the default "whatever you did yesterday, you do today"; that is not something to be measured in the prototype, it is a design decision to be made now. One more note: the price needs a market ±5% stepped button, not a slider; in 12 §5.3 the difference between ten and twenty per cent above is ten points of satisfaction, and at six inches a slider will not hold that precision.

## The five riskiest problems

### 1. The Market burns the budget on its own, and the "automatic ordering" contradiction is open

**The problem.** 02 §2 principle 2 says "ingredient ordering becomes automatic"; 02 §3 Stage 1 makes the same thing the daily main screen, with price swings, quality, spoilage and supplier relationships. 16 fixes the budget at 40-60 but never defines the Market's components.

**What the player lives.** Thirty or forty touches on small plus/minus buttons across ten lines, every morning. By day ten the Market becomes a source of dread, like Good Pizza's many-ingredient orders — the very thing principle 6 was written to prevent.

**The fix.** The basket should arrive filled with yesterday's order, and "top up what is missing" should be one touch. Orders should be placed in dish portions, not ingredients, and the game should convert them. Quality should be a single choice for the day. The basket should stay editable until you leave the Market. Target ten touches for the Market; and the contradiction between 02 §2 and §3 should be closed in the text.

### 2. The patience warning is colour only, and inside the scene only

**The problem.** 17 "Event sounds": patience critical → "the table edge turns red". 10 "The identity of the four cuisines": the fast food palette is "high saturation, red and yellow". Characters are 60-120 pixels, with 6-20 people on screen (10). There is camera panning and zooming (19 B7), so a table can move off screen. 16's top bar has neither the intervention allowance nor any warning.

**What the player lives.** A player playing silently (17's own assumption) with weak red-green discrimination will not see a red table edge in a red-and-yellow shop; the courier leaves in eight seconds (12 §5.2), reputation drops sharply, and the player does not understand what he lost. Also, the "satisfaction facial expression" in 16 and 02 §7 contradicts 10's finding that "a face does not read at that scale".

**The fix.** A three-channel warning: on the table, colour plus shape plus motion (a closing ring); an arrow at the screen edge; and, under the top bar, touchable "patience queue" chips showing the three worst tables. Touching a chip should fire 19's Intervene action; that also solves the 44pt target problem in a fourteen-table dining room. The warning colour should be chosen per cuisine palette, as the most distant tone in that palette. The satisfaction indicator should be a UI glyph above 24 pixels, not the model's face, and should only appear on tables below the threshold. A haptic twin should be added (A12).

### 3. The first staff lesson contradicts the economy; the first ten minutes are punishment from beginning to end

**The problem.** 16 "Minute by minute": the first staff member at minute 7-10, i.e. on the fourth or fifth day. 14 "The crew needed": for thirteen customers, one cook plus the owner is enough. 12 §6: a single staff member for the first two weeks. 02 §6: the staff section at 30-90 minutes. 09: station layout in the second season. Four documents give four times. With 8,000 capital (12 §1), the 2,780 first bill on day seven is not an exam; per 12 §6 the first real squeeze is in week three. "No collapse in the first three days" (16, rule 3) protects nothing, because the ladder's first rung hangs on the weekly payment (02 §5) and cannot fire before day seven.

**What the player lives.** Because the game says "hire staff" he hires a waiter, sees minus 770 on day seven, and cannot see the capacity gain because the customers already fitted. All five of the things he learns are costs: price angers people, stock runs out, staff take money, rent arrives. There is not one positive peak in the first ten minutes. The D1 risk is exactly here; 21's metric "day 1 retention: is the tutorial working" will measure it, but late.

**The fix.** Bind the four documents to a single calendar. Put the first hire on the morning after the first waiting loss the player has seen with his own eyes (the pattern of the day-three stock lesson: pain first, then the tool) and make the candidate an apprentice (14: wage minus 25 per cent). Rename day seven "the first bill you can pay" and week three "the first exam". Add a positive event before minute five: a new dish unlocking (17 already has the celebration sound) or the first visit from a named regular. Remove the collapse protection or make it "until the first rent day". "Pause, look, intervene" should be taught explicitly on the second day; it is nowhere in the table.

### 4. Service's 120 seconds are bimodal; there is no speed, no counter and no time indicator

**The problem.** 02 §3 Stage 3 gives 3-5 interventions a day; 14 forbids changing stations, rightly. In 12 §5.6, sixty per cent of customers in Turkish cuisine arrive in one slot. 16's top bar has no interventions remaining, no position within the day and no sign of when the peak will come; speed control is in no document at all.

**What the player lives.** In the first week thirteen customers spread over two minutes: ninety seconds of watching a single cook. At the peak three tables go red within thirty seconds, the interventions run out, and then he watches again. Neither "there is nothing to do" nor "I cannot keep up" alone; the two back to back.

**The fix.** A 1x/2x speed button, not a skip. A day progress bar marking the four slots, with the peak visible in advance. Intervention dots on the top bar. A free verb in the quiet slots: "the owner attended personally +20" from 12 §5.4 is already in the formula, so make it visible as a service verb. An app closed mid-service should reopen paused and with a "resume" layer; 15 saves every ten seconds but does not write down how the return is presented, and coming back to eight seconds of patience is not fair.

### 5. No undo, a dead back button, no yesterday

**The problem.** 16 "Flow rules": no going back between stages, the Market decision is binding, and the Android back button "does nothing" in all four stages. Modals appear in only three situations, and spending on ingredients or an upgrade is not one of them. Screen 17 opens only from the Books; why a customer left is reported nowhere.

**What the player lives.** A mistouch on a 44pt button means twenty fish that will spoil (12 §3: you lose the whole value of a spoiled ingredient). A wrong "Go to the Counter" locks the day. Nothing happening on the back button feels "broken". Opening the game the next day there is no report from yesterday, and no reason for the customer who was lost. Principle 1, "every decision has a visible consequence", is left half done when no cause is shown.

**The fix.** Editing until you confirm within a stage, plus one-touch undo; that is not going back between stages, so it obeys the rule. The stage exit button should show the total and give a one-second non-modal "undo" notice. The back button should open the pause and settings layer in every stage, and should never do "nothing". A one-touch "Yesterday" card in the Market; screen 17 reachable "from anywhere" like 15. A lost-customer line in the report: waiting, sold out, price.

## Item decisions

| Item | Decision | Reasoning |
|---|---|---|
| A9 Screen list and flow | REVISE | Eighteen screens and the modal ban are right. What is missing: in-stage undo, the back button's "does nothing" behaviour, access to "Yesterday", a top bar that changes with the stage (interventions and day progress during service), and the reason for a loss. Surfaces not on the list: the pause menu, the loan (02 §5, rung 2), the landlord's message and the ultimatum (02 §5, rungs 1 and 4), the supplier relationship (02 §3). |
| A10 Tutorial and the first ten minutes | REVISE | The pacing is right: one concept a day, one sentence, by doing. The content is wrong: the time of the first hire contradicts four documents, day seven is not an exam, the first-three-days protection is a dead rule, there is no positive peak, and pausing is not taught. "The tutorial does not appear in a second playthrough" (16, rule 6) is wrong for a player sharing a device or returning weeks later; it should default to off but be switchable on with one touch when a slot is opened. |
| A12 Audio design | REVISE | Five layers, "every sound has a job", layered service music and the use of silence are right; four tracks and a 60-90 second loop are a reasonable cost. The fix is small but mandatory: the visual twin must be a "twin independent of colour"; the rule must apply to the continuous layers too, and the crowd murmur has no visual twin; a haptic twin must be added for two critical events; and the claim that "most people play silently" does not appear in research/01 and should go onto 21's metric list. |

## Unanswered questions

1. **Is landscape settled (16, question 1)?** For: venue expansion and floor-plan editing want width (02 §7), the same interaction as Steam (19 B7), one orientation for a one-person team. Against: a three-minute pocket session is a portrait, one-handed habit, one-handed play is nearly impossible in landscape, and research/01 has no data on orientation at all. Suggestion: keep v1 landscape, but pin the primary actions in the Market, the Counter and the Books to the bottom-right thumb zone, mirror them with a left-handed setting, and add the rotation abandonment rate at first launch to 21's metric list. Because the core is behind ports (04), the three management screens can be moved to portrait later; do not close that door.
2. **What do the accessibility settings cover?** 16 has screen 5 saying "accessibility" and no document writes its contents. The minimum list: a text size scale (20 C5 defines only the family and the Turkish glyphs, with no smallest body size), a colour-blind palette, reduced motion (a static alternative to the flashing stock bar and the glow effects, flashing below three hertz), haptics on/off, left-handed mirroring, and an "auto-pause when patience goes critical" option. Screen-reader support in a Unity UI is not realistic for one person in v1; instead of promising it, the baseline "no information by colour alone, no input that depends on timing alone" should be the written goal.
3. **Is four pieces of information too many in the top bar (16, question 4)?** Not too many — wrong during service. Till and "N days to rent" are constant; "Day" and "Reputation" only in the Market, the Counter and the Books; during service their places should go to the intervention allowance and day progress. The coin is "warm gold" (12 §7.5) and the Turkish cuisine palette is "brass" (10); the top bar should sit on its own opaque plate, not over the scene.
4. **The hiring pool refreshes every three days (14).** Who tells the player that? Without a badge on the Counter, the player forgets the pool.
5. **Is the Market a "second loop" or automation?** Until this is answered — the question between 02 §2 principle 2 and §3 Stage 1 — the touch budget cannot be computed.
