# 47 — Recognition and the weekly report card

*14 September 2026.* The question was:

> *"oyun içi görev başarma duygusunu oyuncuya hissettirmek için günlük görevler
> mi olsa?"*
>
> *("should there be daily quests, so the player feels the sense of completing
> something in the game?")*

The diagnosis was right, the medicine was not. This document describes both.

---

## 1. The real gap: there is a score, there is no visibility

The game scores on seven axes ([08](08-endgame.md)) and the player saw them
**exactly once** — on the sixtieth day. You cannot feel progress in something
you cannot see, and you cannot play towards a yardstick you learn too late.

So the problem is not "there is no sense of achievement", it is **"achievement
is measured but not shown"**.

---

## 2. Why a daily quest was the wrong medicine

**It breaks the fiction.** The game's one-sentence promise is "You're the boss,
not the chef" (the first line of [44](44-store-texts.md)'s store text). Who
hands the boss a quest list every morning? A quest board makes the player an
employee of an invisible boss — the one thing the game promises not to be.

**The reward lands on a saturated axis.** This project's law: *a reward paid
into a saturated axis is invisible.* The classic daily-quest reward is money,
and by the harness a good player ends the sixtieth day with ~21,000 in the till
— the reward is not felt at exactly the moment it is supposed to be.

**It can punish playing well.** A "host 40 people today" quest turns the
player's **strategy into failure** if it lands on the day they deliberately work
one person short and bank the money.

Daily quests are also a **retention** tool: a problem for games with ads and
in-game purchases. Here there are neither.

---

## 3. What was done: recognition, not assignment

The distinction fits in one sentence: a *quest* says "do this tomorrow", a
*badge* says "you did this today". The second is retrospective, so it **never**
conflicts with the player's plan.

### A. The weekly report card

`Score()` is already a **pure function** of the current state — it works on any
day of the campaign. So no new calculation had to be written for the report
card; two separate calculations part company one day and then nobody can tell
which one is right.

At the close of the seventh day the seven axes are photographed and shown in the
evening report **as a delta against last week**. 60 days ≈ 9 weeks: the single
moment of achievement becomes **nine**, and the player learns what they are
measured by before it is too late.

Weekly, not daily: the axes do not stir in a single day, and the game's own
rhythm is already weekly (wages and rent are paid weekly, the peak is two days a
week).

**Week zero's photograph is taken in the constructor.** Without that line last
week would count as zero and on the seventh day the player would see a jump like
"Place +33" that **they did not make** — the score of the shop they inherited is
not their gain. A test holds this
(`The_first_report_cards_delta_does_not_count_what_was_inherited`).

### B. Seven badges

| badge | condition |
|---|---|
| Nobody left hungry | a peak day, zero turned away **and** zero angry |
| You ran the peak short-handed | a peak, crew below what was needed, no angry guests |
| The book is closed | a tab **was written** and all of it was collected |
| They know you now | a regular's first story beat |
| Ten thousand in the till | — |
| You made the place bigger | the first tier of expansion |
| The restaurant people talk about | reputation 90 |

All of them come from things the simulation already knows; the only new tracking
is a "was the book ever opened" flag. The reward is **not money**: it is being
seen.

Badges that have not been earned also stand there by name in
[`BadgeScreen`](../unity/Assets/Lokanta/Game/Ui/BadgeScreen.cs) — the player has
to see what is possible in order to pick **their own** goal. But it is not a
quest list: none of them says "do this today" and none of them has a deadline.

---

## 4. The measurement fixed three things

### A unit error: the badge was handed out on the first day

I had written `CashMilestone = 10000`. `_cash` is in **centi-coins** and the
starting till is 800,000 centi (8,000 coins) — so the threshold was 100 coins
and the badge was awarded **at the end of the first day**. Nothing broke; a
recognition called "the first ten thousand" was simply handed out without ever
being earned.

That is exactly the dangerous side of badges: **not that they break, but that
they are given without being deserved.** Without the measurement it would have
been invisible.

### Distribution: the achievement curve died in the first week

With the first five badges a reasonable player picks up three recognitions on
days 5, 6 and 8, and then there was **fifty-two days of silence**. "You made the
place bigger" and "The restaurant people talk about" were added for that — the
second comes late because reputation is clipped to the ceiling of the table tier
(`reputationCapCenti` 5500/7500/9000/10000), so **seeing 90 requires expanding
first.**

### The player doing the measuring must be measured too

In the first calibration no badge was earned at all, and that **did not show**
the badges were unreachable — it showed the bot doing the measuring played
badly. The passive bot meets the peak with one cook and one waiter; of course
nobody leaves happy. Once it was swapped for a player who understands stock and
crew, three badges came in the first eight days.

*A measuring tool's own skill looks like a property of the thing it measures.*

### The tour's player: 6 out of 7

The final verification tour (Turkish cuisine, 60 days, a real Windows build)
earned **six of the seven badges** across the campaign. So the set is neither
unreachable nor free: a well-played campaign collects most of them, and the one
that is left needs a separate **decision** — the tour's bot builds the crew it
needs every morning, so "run the peak short-handed" is a game it never plays.

The gap between the test bot (which only understands stock + crew) taking three
badges on days 5-6-8 and the tour's bot taking six shows that recognition
**changes with how you play**. That was the point.

---

## 5. The trap that fired for the third time — now closed

The text-family allow-list sat in **two languages**: `gen_loc.py:SCREEN_KEY`
(Python) and `LocTests.cs` (C#). `LocTests.cs`'s own comment already said so:

> THE SAME LIST also sits in tools/content/gen_loc.py:SCREEN_KEY and the two CAN
> DRIFT APART — and drift they did.

They drifted a third time, this time on me: I added the `badge.` family to the
generator and not to the test.

Now `gen_loc.py` **reads `LocTests.cs`** and compares the two lists, and refuses
to generate if they have drifted. Verified by mutation in both directions:

```
deleted from the generator: exit 1  ->  not in gen_loc.py  : badge.
deleted from the test     : exit 1  ->  not in LocTests.cs : badge.
```

The comment's warning that "whoever changes one must change the other" is no
longer a wish, it is a check.

The scan looks **only at the relevant block**: the first version scanned the
whole file and an `EndsWith("Key")` somewhere else in the test was taken for a
family — the check turned itself red.

---

## 6. Scope

- Save version 20 → **21** (`badges`, `badgesToday`, `creditEverOpened`,
  `weekAxis`, `weekAxisPrev`, `weekReportDay`).
- `Theme.AxisRow` is **a single implementation**: the year-end report card and
  the weekly report card use the same row format, so the player does not learn a
  new table on the sixtieth day. A second copy would have been an invitation to
  the thing that has happened repeatedly in this project.
- 6 new tests (233 → **239**).
- The tour's checks were put inside the evening report — that is the **only
  moment** the check can run, because the "earned today" mark is cleared in
  `AdvanceToNextDay`. If it cannot measure at all it says `UNMEASURED`: a check
  that does not run looks, from outside, exactly like one that passes.
