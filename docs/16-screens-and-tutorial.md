# Screens and Tutorial

**Last updated:** 9 September 2026
**Register items:** A9 screen list and flow, A10 tutorial
**Status:** Written, awaiting decision

---

## The screen list

Eighteen screens. Landscape, designed against the narrowest safe area.

### Outside the game

| # | Screen | Job |
|---|---|---|
| 1 | Title | Logo, continue, slots |
| 2 | Slot selection | Four slots, each showing cuisine, day, till, plaque |
| 3 | Cuisine selection | When opening a new game. Locked cuisines are visible with a preview |
| 4 | Cuisine store | Purchase with real money. **The in-game currency icon is never used here** |
| 5 | Settings | Sound, language, accessibility, data |
| 6 | Legacy showcase | Across saves. The best plaque for every cuisine |

### The day cycle

| # | Screen | Phase | Duration |
|---|---|---|---|
| 7 | Market | Morning | 45-75 s |
| 8 | Counter | Before opening | 45-75 s |
| 9 | Service | Daytime | 90-180 s |
| 10 | End-of-day accounts | Closing | 45-60 s |

These four are sequential and cyclical. The player passes through these four screens every day.

### Management screens

| # | Screen | Where it opens from |
|---|---|---|
| 11 | Layout editing | Counter or accounts |
| 12 | Hiring | Counter |
| 13 | Staff management | Counter. Morale, experience, raises, dismissal |
| 14 | Upgrades and equipment | Accounts |
| 15 | Customer book | From anywhere. Regulars and their stories |
| 16 | Tab book | In Turkish cuisine. Who owes how much |
| 17 | Reputation and reviews | Accounts |
| 18 | End-of-year evaluation | Automatic on day 60 |

---

## Flow rules

### The back button

Android has a hardware back button and its behaviour has to be defined.

| Position | Back button |
|---|---|
| A management screen | Returns to the screen above |
| The four phases of the day | **Does nothing.** You do not go back between phases |
| During service | Opens the pause menu |
| The main screen | Asks to confirm quitting |

Not going back between phases is deliberate. A decision made in the market phase is binding and is not undone.

### Use of modals

**The rule: no modal that forces the player to read.**

A modal is used in three situations only:
1. A destructive operation that needs confirmation, for example dismissing a staff member or deleting a save
2. Spending real money
3. The end-of-year evaluation

Every other piece of information is given inside the screen, without stopping the flow.

### Information hierarchy

On the service screen there are three things in the top bar and their order never changes:

```
[coin] 8.240      Day 23    Reputation 62    Rent in 4 days
```

**"Rent in 4 days" is deliberately permanent.** The metronome of the weekly pressure must be visible at all times. The player should not make a decision without seeing it.

---

## The tutorial: the first ten minutes

### The principle

The clearest lesson from the research: **no wall of text, teach by doing.** And one concept per day.

Fast food is the right ground for the tutorial, because it is the cuisine with the fewest variables. That is one of the reasons it is the free one.

### Minute by minute

| Minute | What happens | What is taught |
|---|---|---|
| 0-1 | A cold open. The shop is open, one customer comes, there is one dish. One tap | How service works |
| 1-3 | The first day is completed. The market and the counter in their plainest form | The four phases of the day |
| 3-5 | The second day. Price setting opens | Price affects satisfaction |
| 5-7 | The third day. A second dish on the menu. An ingredient runs out | The cost of running out of stock |
| 7-10 | The fourth and fifth days. The first staff member is hired | Crew and wages |

**The seventh day is the first rent day.** The tutorial runs up to there, and the first real test is there.

### Tutorial rules

1. **No step is skipped, but no step waits either.** If the player is ready, it moves on at once.
2. **Tutorial text is one sentence.** No hint runs past two sentences.
3. **No bankruptcy in the first three days.** Even if the till drops to zero the ladder does not run. The player learns first.
4. **A warning is given before the first rent day.** "Tomorrow is rent day, you have this much in the till."
5. **The tutorial can be turned off.** But the default is on.
6. **The tutorial does not appear in a second game.** The slot screen already knows you have played.

### The goal of the first ten minutes

By the end of the tenth minute the player should know:

- What the four phases of the day are
- That I set the price and that it has consequences
- That I lose customers when an ingredient runs out
- That staff cost money but give capacity
- That I have to pay rent every week

If they do not know this, the tutorial has failed.

---

## Interface constraints

These come from the mobile decisions and are not negotiable.

| Rule | Value |
|---|---|
| Orientation | Landscape |
| Smallest touch target | 44pt |
| Basic interaction | Drag and drop, and a single tap |
| Precise aiming | None |
| Double tap | None |
| Daily tap budget | 40-60. To be measured in the prototype |
| Numerical data | Turned into a graphic. Satisfaction is a face, stock is a full-empty bar |
| Pause | At any moment |
| Quitting | At any moment, resuming from the second it stopped |

**The tap budget will be measured.** In Good Pizza, orders with many ingredients became a source of dread, because the number of taps stopped being a reward and turned into a punishment. Any design that goes past sixty will be cut.

---

## Details awaiting a decision

1. ~~Is landscape final, should portrait be supported too~~ **Closed 9 September 2026: landscape, no portrait.** To be added in Batch D: thumb-zone layout, a left-handed mirroring option
2. Should the tutorial run until the seventh day, or be shorter
3. Is the length of the bankruptcy protection in the first three days right
4. Are four pieces of information in the top bar too many

---

## Measured: a table cannot be a touch target

10 September 2026. The restaurant layout was built in Unity and all four tiers were rendered at a real phone aspect ratio (`Assets/Lokanta/Editor/RestaurantScene.cs`). The camera fits the whole hall into the frame, the angle is 30 degrees, the orientation is landscape.

Then it was measured **how many pixels on screen** a table is. Until now that number had been talked about by guesswork.

| Tier | Hall | Table, in a 960-pixel frame | On a 2400-pixel phone | In dp |
|---|---|---|---|---|
| 4 tables | 7.8 × 7.4 m | 22 pixels | 55 pixels | ~20 dp |
| 7 tables | 9.6 × 7.4 m | 21 pixels | 53 pixels | ~19 dp |
| 10 tables | 11.5 × 7.4 m | 20 pixels | 50 pixels | ~18 dp |
| 14 tables | 13.3 × 9.1 m | 17 pixels | 42 pixels | ~15 dp |

For comparison: Google's minimum touch target is **48 dp**, Apple's is **44 pt**.

**Even at the smallest tier the table is less than half the minimum; at the largest tier it is a third of it.** This shows that the answer to "do fourteen tables fit on the screen" is yes, but that it was the wrong question. They fit; they cannot be touched.

### What this means

On a camera that shows the whole hall in a single frame, **a table cannot be the primary touch target.** There are three routes and a decision has to be made:

| Route | What it means | Its cost |
|---|---|---|
| **Let the camera zoom in** | The player pans and zooms, the table gets bigger | The tap budget goes up; docs/16 says 60 taps a day, and panning is inside that |
| **Let the target be bigger than the table** | An invisible, wider touch region around the table | At 14 tables the regions collide; 42-pixel tables overlap with 130-pixel targets |
| **Let the thing you touch not be the table** | The customer, the order or the warning is listed in the bottom bar; the player touches the list, not the hall | The hall becomes decor; but we are playing the owner anyway, not the waiter |

The third is the most consistent with [02-design-proposal.md](02-design-proposal.md)'s "the owner, not the chef" principle, and it overlaps with [review/05](review/05-player-experience.md)'s proposal of "a three-channel patience warning, chips in the top bar". But no decision has been made.

**Without this measurement all three looked reasonable.** The number weakens two of the options seriously.
