# 52 — Fast food's rent and the face of the hall

*15 September 2026.* The request came in two parts:

> *"Fast food diğer mutfaklara göre biraz daha oynaması kolay olabilir ama
> aradaki fark çok da büyük olmasın, ona göre zorluğunu biraz artırabilirsin.
> Görünüm kısmına devam et."*
>
> *("Fast food can be a bit easier to play than the other cuisines, but don't let
> the gap be too big — you can raise its difficulty a little accordingly. Carry on
> with the visual side.")*

---

## 1. Difficulty: raising the rent **raises** the till

`rentMultiplierBp` comes from the content (`cuisines/*.json`) and the simulation
rebuilds the tiers with it. It was swept — the `makul` bot, against Turkish's
17,351:

| rent multiplier | fast food cash | gap |
|---|---:|---:|
| none | 22,473 | +29.5% |
| **×1.15** | **22,263** | **+28.3%** |
| ×1.25 | 23,493 | +35.4% |

Intuition says "if rent goes up the till goes down" and **the measurement shows
the opposite.** The cause is the same one seen in the volume multiplier
([51](51-self-service.md) §5): the bot answers a cost by **not expanding**, not
expanding is already more profitable, and so every expense lever pushes it
towards a leaner and richer shop.

The direct consequence: **end cash is not a difficulty measure for this bot.** The
measure is the **gap between** the two cuisines — and that is narrowest at 11500.

For the well-playing bot (`planci`) the gap is already +13%. The large gaps that
remain come not from rent but from Turkish's two weaknesses (spoilage 14,121
against 5,487; tabs net negative) and those are a separate job — not things to be
closed with rent.

---

## 2. The imaging tool was not showing what the game shows

The self-service counter — menu panels, tills, drinks machine — had been written
in the previous session. Looking at the frame, **none of it was there.**

The cause: the flag was being read from the wrong end of the chain.

```
CuisineId   : App.Content.Cuisine  ->  PreviewCuisine  ->  "fastfood"
SelfService : App.Content.SelfService                     (tool: null)
```

There is no `App` in the editor tool, so self service was **always off**. The
palette went through `PreviewCuisine`, so the tool was drawing "fast food" in the
right colour but with the **wrong structure**.

The cure was not to write `"if fastfood then self service"` — that knowledge
belongs to the content, not to the view. `GameShot` was already building a
`ContentSet`; it now hands it to the view as `PreviewContent` and `SelfService`
follows the same chain.

*If the tool does not show what the game will show, saying "it has been added" is
not a measurement but a guess.* This time the guess was wrong.

---

## 3. Hanging menu panels: the same lesson for the third time

I had hung the panels **above** the counter (y = 2.00). When the frame opened
there were giant, empty, glowing boards standing in front of the cooks.

This is the **third** time for the same thing in this project: looking at a
ceilingless building from 34 degrees, **anything hung covers what is behind
it.** The user had said so twice about pendant lamps ("don't let the ceiling
lights show").

The solution was not to remove the panel but **to move it**: the menu board is now
**on the kitchen's back wall**. Under this camera the back wall sits directly
above the counter — so the player still reads it as "the menu above the counter",
but it covers nothing.

The board is not a blank sheet either: a dark face, lit rows, a price column on
the right — the same language as the hall's menu board.

---

## 4. The hall's largest surface had not been differentiated

Putting the two halls side by side (`render/salon_*_oda.png`), the floor and the
wall had diverged but **the tables were identical** — both the same brown wood.
The hall's largest surface by area is the tabletop, so half the distinction was
still missing.

| | furniture |
|---|---|
| Turkish | brown wood (unchanged) |
| fast food | **light laminate** `(0.686, 0.612, 0.510)` |

Dark floor + light top + red cushion is the arrangement in all three of the frames
the user brought. A single palette line; no new asset, no downloaded texture,
nothing added to the attribution ledger.

---

## 5. The tray drop-off station: the visible end of self service

The cleaner collects the trays from the tables ([51](51-self-service.md)) — but
**where they take them afterwards** was not in the hall. On the entrance room's
left wall, a waist-height cabinet: a stack of trays on top, a dark mouth at the
front, a small lit sign beside it.

**No queue rail was added**, and that is deliberate: the reference has a rail in
front of the counter, but in this simulation nobody queues at the counter — the
customer walks from the door to a table. An empty queue rail would promise a
mechanic that does not exist.

---

## 6. The counter was split in two

The tills were on top of the counter and **the full-length glass guard was
covering them**: in the frame all that was left were two white smudges on the
counter. The guard came down from `w` to `0.60·w`; the tills moved to the ends,
outside the guard, and got their own screens.

A real fast food counter is divided the same way: **the hot line in the middle,
the tills at the ends.**

---

## 7. The Turkish tour went red — and the cause was a previous commit

After the visual work was finished the Turkish tour ran and failed:

```
  DIAG liveliness window: 37.7 s, service 30% -> 61%
FAIL : Work is being done in the kitchen (0 people; the simulation gave work 156 times)
```

**The same build passed on the second run** (139/0). So both the red and the green
were the result of **sampling luck**, not of the measurement.

The root cause is not in this session: with [51](51-self-service.md) the Turkish
cuisine's peak had moved **from the 1st slice to the 2nd**
(`[1200,4800,2500,1500]` → `[1200,2800,4500,1500]`) and **the Turkish tour had
never been run after that commit.** With a 40-second real-time budget the
liveliness window only reaches 61% of the day; the peak is now at 50–75%. The
single cook also spends most of their time **walking** to the station — the
simulation gives the task, the pose is `Walk`. If the window happens to land in
that narrow gap it is green, if not it is red.

The cure is **not** to lengthen the window for everybody: that would bring back the
old `runs to 80% and eats the peak` behaviour, and that behaviour had already been
fixed once. The extension is now **conditional** — it keeps looking for up to 75
seconds only if the thing to be measured has not been seen yet *and* the
simulation really is handing out work. If the check is satisfied, the window
closes at 40 seconds as before, so the following checks find the peak exactly as
they used to.

Measured — two runs, both green:

| | result |
|---|---|
| Turkish run 1 | **141 passed, 0 failed, 0 unmeasured** |
| Turkish run 2 | 139 passed, 0 failed, 2 unmeasured — window **11.4 s** (30% → 39%) |
| fast food | 133 passed, 0 failed, 1 unmeasured — window **exactly 40.0 s** |

Then five more Turkish runs: all green, windows 11.4 / 19.9 / 28.3 / 32.4 seconds
— that is, **the extension never fired once.** The fast food window also closed at
exactly 40.0 seconds. The normal path is unchanged.

### Counting a branch that never fired as "fixed"

Five green runs do not show the extension *works*, only that it *was not needed*.
This project has written that same trap many times, so the branch was measured
**by mutation**: the condition was temporarily made `true`, so the extension was
free on every run.

```
mutation:   DIAG liveliness window 26.6 s  ->  summary: 140 passed, 0 failed
```

The window still closed under 40 seconds. The reason is illuminating: the loop
normally ends not on the time cap but on a conditional `break`. So the extension
cannot extend a run that sees what it came to see — there is no risk of eating the
peak, and that is now a measurement rather than a piece of reasoning.

What remains honest is this: the branch **itself** has still not fired, because
the condition it fires under shows up once in six runs. What it looks for and the
condition it fires under were taken verbatim from that one run; but if this goes
red once more, this is the first place to look.

*A time budget is a safety cap; the moment it becomes the measurement's definition
the measurement is lost.*
