# The End of the Game

**Last updated:** 9 September 2026
**Register item:** A14
**Status:** ✅ Approved, 9 September 2026

---

## The question

Should the game have a defined ending, or should it go on forever?

The cuisine lock decision made this question mandatory. The sentence "let us continue with that style until the game ends" requires there to be a point at which the game ends.

---

## What other games do

| Game | Model | Outcome |
|---|---|---|
| **Stardew Valley** | A scored evaluation in the third year, then free play | The most successful model. The game does not end, it is only evaluated |
| **Dave the Diver** | A story ending, you can continue after the credits | Good, but the post-game content was found weak |
| **PlateUp!** | Fifteen-day runs, every run ends | Roguelite. Loss turns into a permanent bonus |
| **Two Point Hospital** | Level-based, a three-star target in every level | Clear goals, but it does not fit our structure |
| **Supermarket Simulator** | Endless. No defined ending | **A cautionary case.** The detail is below |
| **Tavern Master** | Endless, empty endgame | The "more money than you can spend" complaint in the research |
| **Cat Cafe Manager** | A short story, nothing after it | "No replayability once the story is over" |

---

## Evidence: the trap in the endless model

Supermarket Simulator is the game with the economic loop closest to ours, and it chose the endless model. The late-game complaints on its forums are very clear:

- A player with two hundred hours is at level 90 and still has five expansions and six licences left. **In the mid and late game a single expansion takes thirty hours of real time to buy.**
- The game turns into a "warehouse simulator". After hiring two or three cashiers and a shelf stocker, the player spends almost no time in the shop; they spend the hour before opening making sure the warehouse is full.
- The reward side is empty: "apart from a few product licences we get no rewards at all."
- Players use mods to shorten the day. A twenty-five-minute day drops to five minutes.

On the same forum there is also a thread titled "I finished the game after 450 hours". So the endless model works for one segment. But the complaints dominate and they all point to the same place: **without a goal, growth loses its meaning.**

On the other hand a hard ending is bad too. Stardew Valley's designer deliberately rejected the way the old Harvest Moon games forced the player to start over for a better ending.

**Both extremes are bad. The middle works.**

---

## Proposal: a scored finale, free play afterwards

The game is **evaluated** at a defined point, but it does not **end**.

### The structure of the finale

**The end of the first year.** Four seasons, roughly fifteen days per season, sixty days in total.

When the year is up, the neighbourhood's food critic writes their annual review. It is presented as a newspaper piece and its headline changes according to your score. Then a plaque is hung in the restaurant.

**The plaque stays on the wall and is visible in free play.** It becomes permanent, visible proof of what you did.

### After the evaluation

- The save is not deleted, nothing is taken away from you.
- The restaurant keeps running. Free play mode opens.
- No new story arrives, but the game stays playable.
- If you want, you can continue into a second year for a better score and be evaluated again. Stardew Valley's re-evaluation idea.

### The replay hook

The score is what creates the value of buying a cuisine. You open a new save with a different cuisine and aim for a better result.

**The legacy screen:** a permanent showcase across saves. It shows the best result for every cuisine. Collecting all four plaques becomes the long-term goal.

---

## The scoring axes

The evaluation should not be a single number; it should be multi-axis. That way different playstyles can get a good result by different routes.

| Axis | What it measures |
|---|---|
| Wealth | Net worth at the end of the year |
| Reputation | Final reputation level |
| Regulars | How many regulars you won and kept |
| Crew | Remaining staff and their morale |
| Venue | How much you grew |
| **Robustness** | Did you ever fall onto the bankruptcy ladder, and how deep |
| **Cuisine-specific axis** | Different for every cuisine |

### The cuisine-specific axis

| Cuisine | Axis |
|---|---|
| Fast food | How many of the mains turned into a combo |
| Turkish cuisine | Tab collection rate |
| Italian | Average ticket |
| Japanese cuisine | Broth accuracy rate |

This axis makes the thing that separates the cuisines visible in the result as well.

> **Measurement note (13 September 2026).** This line says *"it rewards the
> signature mechanic directly"* and for a while that was **not true** for fast
> food: its axis was `peakCovers` (the highest daily covers) and it was measured
> that a bot which opens the combo every morning and a bot which never opens it
> get the same score (38/38), while the highest scores went to whoever expanded
> the most (`planci`, 73). So the axis was a copy of the "Venue" axis.
>
> **The axis was changed** (`comboShare`, target 15% — from measurement). Now a
> bot that uses the combo gets 100 and one that does not gets 0, and expansion
> does not affect this axis at all. The signature bot is the highest-scoring
> strategy in both cuisines. Detail in [docs/42](42-crew-and-intervention.md) 5.
>
> *The sentence "it rewards it directly" in a document was a guess until it was
> measured; once measured it turned out wrong, and what got fixed was the code,
> not the document.*

---

## The connection to the bankruptcy ladder

The most valuable side effect of this design is here.

Until now the bankruptcy ladder only had a short-term consequence: reputation loss, selling equipment, shrinking. **The robustness axis adds a long-term consequence to it.**

Finishing the year without ever falling onto the ladder brings a high score. Going all the way down to shrinking permanently lowers the score. The save is still not deleted, the game still does not end, but the trace of the mistake you made shows up at the end of the year.

That completes the "soft but toothed failure" design. The penalty is not instant, it is cumulative.

---

## Campaign length

| Value | Estimate |
|---|---|
| Campaign | 60 in-game days, four seasons |
| Time per day | 3 to 6 minutes |
| Total per cuisine | Roughly 4 to 6 hours |
| The two cuisines at launch | Roughly 8 to 12 hours |

**These are hypotheses, not measured.** This is one of the first things the balance tool in Phase 0 has to verify. If sixty days is too long the pace at which systems open gets squeezed; if it is too short the economy becomes meaningless.

The pace at which systems open has to fit this too. Every system should open in the first three hours so that the last two hours are left for mastery and optimisation.

---

## Why this model is right for us

1. **It makes the cuisine lock possible.** "Until the game ends" is now defined: the end-of-year evaluation.
2. **It avoids the Supermarket Simulator trap.** No goalless endless growth.
3. **It also avoids the hard ending.** Nothing is taken away from you; whoever wants to can carry on.
4. **It makes buying a cuisine worthwhile.** The score and the plaque give a reason to play all four cuisines.
5. **It closes the fifth complaint in the research.** The empty-endgame problem is solved by the plaque and the legacy screen.
6. **It completes the bankruptcy ladder.** Failure now has a cumulative cost.

---

## Details awaiting a decision

1. Should the campaign be sixty days, or shorter
2. Will there be new goals in free play, or will it just be the business carrying on
3. Will there be continuing into a second year and being re-evaluated
4. How many tiers the plaque will have
