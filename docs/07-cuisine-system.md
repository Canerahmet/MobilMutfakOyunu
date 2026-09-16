# Cuisine System and Revenue Model

**Last updated:** 9 September 2026
**Status:** Decided, on three conditions. The conditions are below.

---

## The decision

At the start of the game the player picks a restaurant style. The choice is permanent for that save and does not change until the game ends.

| Cuisine | Access |
|---|---|
| Fast food | Free |
| Turkish cuisine | In-app purchase |
| Italian | In-app purchase |
| Japanese cuisine | In-app purchase |

Each cuisine changes not just the dish list but the **customer profile, the economy and the decision mechanics**.

**Different on Steam:** Steam players react badly to buying content. The Steam version is sold at a single price and includes every cuisine. The port boundary already carries that difference; no extra architectural work is needed.

---

## Why this structure is right

1. **The fifth most hated thing in the research was an empty endgame.** Four cuisines give a real reason to play again after finishing the game.
2. **The revenue model is honest.** We are not selling power, we are selling content. The research's hate list had energy, timers and level locks on it. None of those are here.
3. **It solves the problem of picking one theme.** A specific identity proposal stays valid, but we are not trapped in a single identity.
4. **Turkish cuisine genuinely does not exist in the market.** A distinctive hook.

---

## Its risk: content quadruples

For a one-person project this is a real danger. Scope explosion is the most common cause of death for solo projects.

**What makes the risk bearable is the architecture.** Cuisines share the same systems and differ only as data. The core code is written once, the cuisines live in JSON files. That makes the cost of the second cuisine content, not code.

**But it is not zero.** Every cuisine wants new dish models, new decor and new dialogue. That is why condition one below exists.

---

## Three conditions

### Condition 1: the first release will not have four cuisines

**At launch: free fast food plus one paid cuisine.** The other two arrive as updates.

**Turkish cuisine** is proposed as the first paid one. The reasons:
- The most original of them, nothing like it in the market
- The closest one to you, so references are easy to find and verify
- The furthest from fast food mechanically, so it shows the value of the purchase most clearly

Cuisines that arrive as updates also serve retention. In the research, Dave the Diver's strongest side was that it kept opening new systems.

### Condition 2: the free cuisine will be a whole game, not a demo

If fast food feels incomplete, the reviews will punish it. The harshest complaints in the research went to games that hid behind a paywall.

Fast food will have a full campaign, a real ending and its own signature mechanic. A player who never pays anything should be able to finish the game and be satisfied. Paid cuisines will not be "the real game", they will be "another game".

### Condition 3: there will be more than one save slot

Locking the style is good design. It makes the choice meaningful and turns every cuisine into a separate game.

**But with a single save slot the lock becomes a trap.** The player buys Turkish cuisine and cannot start it without losing their current fast food game. That produces refunds and bad reviews.

The fix is simple: more than one save slot. Each slot is locked to its own cuisine, and the slots are independent of each other.

The choice should also be freely resettable during the first few in-game days. A player who chose wrong should not be trapped twenty minutes later.

---

## A dependency was born: the end of the game has to be defined

The sentence "let us continue with that style until the game ends" requires there to be a point at which the game ends. That now makes register item **A14 endgame** mandatory.

Proposal: every game ends with a defined ending. Reaching a certain reputation, completing a story arc, or handing the restaurant over and retiring.

A defined ending also closes the empty-endgame complaint from the research, and makes switching cuisine and replaying feel natural.

---

## What the cuisines differ by

Every cuisine changes seven variables. The shared systems stay the same: the day cycle, the market, the menu and prices, staff stations, service, weekly rent, the bankruptcy ladder, reputation, venue expansion.

| Variable | What it means |
|---|---|
| Rhythm | The balance of volume and margin |
| Customer profile | Who comes, their patience, their spend, group size, peak hours |
| Menu structure | How many items, preparation complexity, course structure |
| Ingredient economy | Spoilage speed, price volatility, supply |
| **Signature mechanic** | The one mechanic that exists only in that cuisine |
| Venue and decor | The visual set |
| Staff | Station types |

**The signature mechanic is the most important row.** It is the thing that shows the purchase is another game rather than a repaint.

---

## The four cuisines

### Fast food (free)

- **Rhythm:** High volume, low margin
- **Customer:** Impatient, fast, young and families. Lunch and evening peaks are sharp
- **Menu:** Few items, fast preparation
- **Ingredients:** Long-lived, little spoilage, low price volatility
- **Signature mechanic: combo and flow.** You tie the menu into combos. The right combo design raises the average ticket but increases the kitchen's load. The order queue never stops; the game is continuous flow management.

Because it is the system with the fewest variables it is also the right place for the tutorial. That is why being free is natural for it.

### Turkish cuisine, the tradesman's lokanta (first paid)

- **Rhythm:** Medium volume, low to medium margin, but high loyalty
- **Customer:** Mostly regulars. Tradespeople and workers. A very strong lunch peak
- **Menu:** A dish-of-the-day rotation, pot dishes, batch cooking
- **Ingredients:** Portion management, cost per pot
- **Signature mechanic: tab (credit) and regulars.** You open a tab for regulars. It disrupts cash flow but raises loyalty and reputation. Who will pay is uncertain. There is also a constant trade-off between tea service, portion generosity and margin.

The tab mechanic collides directly with weekly rent pressure. This is the cuisine that makes the game's main tension bite hardest.

### Italian

- **Rhythm:** Medium volume, high margin
- **Customer:** Sits for a long time, couples and families. Evening-weighted. Patient but with high expectations
- **Menu:** Courses. Starter, main, dessert. Wine pairing
- **Ingredients:** Fresh and expensive, medium spoilage
- **Signature mechanic: table time and course timing.** The customer sits for a long time, table turnover drops. Timing the courses correctly raises tips and reputation; wrong timing blocks the table. A game about earning a lot with few tables.

### Japanese cuisine, the ramen shop

- **Rhythm:** Very high turnover, medium margin
- **Customer:** Fast, mostly single-seat, sits at the counter. Strong lunch peak
- **Menu:** Ramen-weighted, fast service, limited sides
- **Ingredients:** Fresh ingredients and a broth boiled every morning
- **Signature mechanic: broth and running out.** In the morning you decide how many portions of broth to boil. Make too little and it runs out mid-day and you close the shop early; you lose customers but nothing is wasted. Make too much and the leftover broth does not keep until the next day, which is a straight loss.

This mechanic comes from how real ramen shops work: when the broth is gone, the shop closes. A single morning decision determining the whole day creates the game's sharpest moment of prediction and separates it completely from the other three cuisines.

---

## Pricing

- Every cuisine can be bought separately.
- A bundle containing all of them, discounted against buying them one by one.
- Before buying, the player should be able to see what they are getting. Not just the name — the signature mechanic and the customer profile difference should be shown.
- The exact figures are in register item D2, not written yet.

---

## The register items this decision closes

| Item | Old status | New status |
|---|---|---|
| D1 Revenue model | Awaiting decision | ✅ Content purchase, not power |
| D3 First release scope | Awaiting decision | ✅ Venue expansion included, second branch excluded, two cuisines |
| D4 Theme and cuisine identity | Awaiting decision | ✅ Four cuisines, two of them at launch |
| A14 Endgame | Not written | ⚠️ Now mandatory, dependent on this decision |
