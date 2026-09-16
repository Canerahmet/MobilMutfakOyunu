# Game Design Proposal v0.1

**Date:** 9 September 2026
**Basis:** [Market research](research/01-market-research.md). Every design decision here is tied to a concrete piece of player feedback in that research.

---

## 1. The concept in one sentence

You are not the cook, you are the **owner**: you set the menu, the prices, the supply and the crew, during service you only step in when something breaks, and every week you have to meet the rent day.

The "owner, not cook" distinction in that sentence is what separates this game from every competitor on mobile. Cooking Fever and Diner Dash make you cook. Eatventure makes you grow a number. We make you **decide**.

---

## 2. Eight design principles derived from the research

These are not up for debate, because each one comes from a pattern that repeats across the reviews.

1. **Every decision will have a visible consequence.** What killed Cat Cafe Manager was the feeling that "nothing matters". Raise your prices and a customer's face changes, a table stays empty, a review drops. Immediately and visibly.
2. **Automation takes the boring part, never the interesting one.** This was an explicit request from Travellers Rest players. Ingredient ordering, dishwashing and cleaning get automated. The menu, the prices, the crew line-up and the crisis moments always stay yours.
3. **The economy will never become irrelevant.** Tavern Master died of "more money than you can spend". Every growth step also grows the fixed cost, so the pressure stays fresh.
4. **Failure will not erase progress, but its price will be visible.** The PlateUp model. No Game Over screen; you shrink instead.
5. **No timers, no energy, no waiting.** Good Pizza's real-time garden timers are the most complained-about mechanic there is. Nothing will tick while the game is closed.
6. **There will be a tap budget.** In Good Pizza, orders with many ingredients became a source of dread. A day should be at most 40-60 taps. More than that is a punishment, not a reward.
7. **A new system will keep opening.** Dave the Diver's strongest weapon. Just as the player is about to get bored, a new layer arrives.
8. **The theme will not drift.** The Travellers Rest forum complaint: "it is not a tavern game any more". Whatever we add, the game stays a restaurant management game.

---

## 3. The core loop: the anatomy of one day

Target length: **3-6 minutes.** That is the mobile session. The game can be closed at every stage and picks up where it left off.

### Stage 1 — Morning: the market (45-75 s)

This is our **second loop**, the counterpart to the dive in Dave the Diver. It will be single and good; we will not add another mini-game, so that we do not fall into Dave the Diver's "too many mini-games" complaint.

- Ingredient prices fluctuate daily. Tomatoes are cheap today, fish is expensive.
- Stock is limited. Arrive late and someone else takes the good goods.
- There are quality tiers: cheap goods mean low satisfaction, expensive goods mean a high review score.
- There is spoilage. Buy too much and it goes in the bin. Buy too little and you run out in the middle of service and lose customers.
- Relationships with suppliers grow: regular buying unlocks discounts and priority stock.

This single screen carries the price-and-supply trade-off that is the most-loved element of Supermarket Simulator.

### Stage 2 — Before opening: the counter (45-75 s)

- **Menu:** which 4-8 dishes are on sale today? According to what you have and the season. Dave the Diver's daily menu choice lives here.
- **Price:** you set the price of every dish. The market average is shown. Go above it and profit rises, satisfaction falls.
- **Crew:** who is at which station? Cook, waiter, till? Staff have character traits: fast but messy, slow but meticulous, panics in a crowd.

### Stage 3 — Service (90-180 s, active but pausable)

This is the heart of the game and the most critical design decision. **You do not cook.**

- Customers arrive, the staff work, the system runs itself.
- Your job is **to see the bottleneck and open it.** Dishes have piled up, the kitchen is jammed, a table has waited too long, a supply has run out.
- You have a limited number of **owner interventions** (3-5 per day). Speed up a station, send a complimentary item to a waiting table, greet a VIP customer yourself.
- It can be paused at any moment. Interventions can be made while paused, too. That guarantees we are not a reflex game.
- Difficulty comes not from reflexes but from **how right your preparation was.** That is exactly PlateUp's "close to engineering" feeling.

This design is also the right one for a mobile touchscreen: drag-and-drop and a single tap are enough, no precise aiming required. It heads off the biggest complaint about Supermarket Simulator's mobile adaptations — touch camera and aiming — from the start.

### Stage 4 — Closing: the books (45-60 s)

- **Income statement:** revenue, ingredient cost, wages, the day's share of rent, net profit. One screen, readable.
- **Customer reviews:** short, with character, signed by name.

  > *"Fiyatlar biraz tuzlu ama köfte harikaydı."*
  >
  > *("The prices are a bit steep but the köfte was wonderful.")*

  This is the visible face of the reputation system.
- **Reputation change:** the star score moves and sets tomorrow's customer count. Dave the Diver's Cooksta system.
- **Spending:** you put what you earned into equipment, layout, staff, or paying off the loan.

---

## 4. The weekly rhythm: the metronome of pressure

Daily pressure is exhausting, monthly pressure is not felt. **Weekly is the right scale.**

Every 7th day: **rent + wages + the loan instalment if there is one** leave the till in one go.

This single mechanic carries the "grow without going under" tension by itself. From day 4 onward the player starts thinking about Friday. Growth decisions — buying new equipment, hiring staff — get planned against that calendar.

---

## 5. Going under: a soft but toothed ladder

The clearest lesson in the research: a hard failure produces frustration, no consequence produces indifference. The middle is this:

| Step | Trigger | Consequence |
|---|---|---|
| 1. Warning | The till is barely meeting the weekly outgoings | A message from the landlord. A visual warning. |
| 2. Squeeze | A payment could not be made | You are forced to take a loan or sell equipment |
| 3. Morale loss | Wages were late | Staff morale drops, someone may resign |
| 4. Ultimatum | Failed to pay a second time | The landlord gives you 7 days. A visible countdown. |
| 5. Shrinking | Time ran out | You lose part of the restaurant. Tables go, the hall gets smaller. |

**No Game Over screen. The save is never deleted.** But what you lose at step 5 is something you see with your own eyes: yesterday you had 12 tables, today you have 6. That hurts far more than losing a number, and it is far fairer.

Recovering after shrinking has to be possible — there should even be a "recovery" achievement. PlateUp's idea of turning a loss into a permanent gain finds its answer here.

---

## 6. Opening depth in stages

Cuisineer sat at 76% because the management layer was shallow from the start and stayed that way. Dave the Diver got 96% because it kept opening new systems. Our plan:

| Chapter | System opened | Approximate time |
|---|---|---|
| 1 | Menu, price, basic service | First 30 min |
| 2 | Hiring staff and stations | 30-90 min |
| 3 | Supplier relationships, ingredient quality | 1.5-3 hours |
| 4 | Layout editing and expanding the venue | 3-5 hours |
| 5 | Reputation, critic visits, rival restaurants | 5-8 hours |
| 6 | A second branch, spreading through the district | 8+ hours |

Each new system does not invalidate the previous one, it sits on top of it. The price decision must still matter in chapter 6.

---

## 7. Mobile user experience decisions

- **Landscape.** Expanding the venue is a main mechanic, so screen width is needed. The menu and price screens will still be designed to be reachable with one hand.
- **Drag-and-drop as the basic interaction.** No precise aiming, no double taps.
- **Minimum 44 pt touch target.** Every button must be comfortable to press with a finger.
- **Pause at any moment and quit at any moment.** If the app closes in the middle of the day it resumes from the same second.
- **Tracking the tap budget.** The number of taps in a day will be measured in the prototype. Any design that goes over 60 is out.
- **Numeric information will be turned into a graphic.** A table cannot be read on a small screen. Satisfaction will be a facial expression, stock will be a full-empty bar.

---

## 8. Art direction proposal

**Proposal: a soft low-poly 3D scene, a fixed camera at roughly 40 degrees, pixel-art character portraits in dialogue.**

Reasons:

- Layout and expansion are the main mechanic. A 3D scene lets the camera rotate and zoom. Pixel sprites want a redraw for every angle, and in an expanding venue that cost compounds.
- Small-screen readability. In low-poly the silhouette and the colour block stay clear. Dense pixel detail turns into noise on a 6-inch screen.
- Production cost. For one person or a small team, low-poly modelling scales faster than quality pixel animation.
- Pixel portraits carry the story and the warmth, give the feeling of Hungry Hearts Diner, and are cheap.

**Alternative:** full pixel HD-2D (the Dave the Diver and Discounty line). More distinctive and proven in the market, but the animation cost is high and camera flexibility is low.

The research's warning applies here: **art style is not a saviour, it is a multiplier.** Even the reviews of Cat Cafe Manager that said "everything is adorable" did not recommend the game.

---

## 9. Engine proposal

**Proposal: Unity.**

- The mobile ads, in-app purchase and analytics SDK ecosystem is mature and ready.
- 3D low-poly scene and mobile optimisation tools are built in.
- Prototype speed is high thanks to the Asset Store.

**When Godot 4 would be the right call:** if we decide on fully pixel 2D and monetisation stays minimal. Godot's 2D pipeline and smaller build size turn into an advantage in that scenario. (Blog sources that give comparison numbers may be biased and should be treated as unverified.)

This decision also depends on your existing experience. Whichever engine you are more comfortable in, prototype speed matters more than anything.

---

## 10. Monetisation proposal

The research is very clear: energy, timers and level locks are hated. Leaving all of them out is a marketing message in itself.

- **Free to download**, the first chapter fully playable (roughly 1-2 hours of real play).
- **A one-time full-version purchase**, in the 5-8 dollar band.
- **Rewarded ads only as an option** and only where they do not cut the loop: something like "double today's profit" on the end-of-day books screen. Hungry Hearts Diner's mistake was not that ads existed, it was that they appeared in the middle of the game.
- **Cosmetic decor packs.**
- **Absolutely not:** energy, real-time waiting, level locks, pay-to-win.

---

## 11. Roadmap

| Phase | Length | Output | Passing criterion |
|---|---|---|---|
| 0. Paper and spreadsheet | 2-3 weeks | Economy table, the maths of one day | Is a 30-day simulation balanced on the spreadsheet? |
| 1. Vertical slice | 4-6 weeks | One restaurant, 6 dishes, 3 staff, 10 days, placeholder graphics | Is it playable and understandable? |
| 2. Fun test | 4 weeks | A test with 10-15 people | Is anyone bored on day 10? |
| 3. Content and art | 8-12 weeks | Real art, the systems of chapters 1-4 | Has the visual identity settled? |
| 4. Soft launch | 4 weeks | Release in a limited market | Day 1 and day 7 retention measurement |
| 5. Global release | - | - | - |

**Phase 0 is the most critical phase.** If the economy does not work on the spreadsheet it will never work in the game. Skipping this phase is the road to Tavern Master's "more money than you can spend" outcome.

---

## 12. Risks

1. **If the management layer stays shallow**, Cuisineer's fate awaits us (76%). Countermeasure: the fun test in Phase 2, measuring boredom on day 10.
2. **If the interface is unreadable on mobile** the game cannot be played. Countermeasure: the principle of turning numeric data into graphics, early testing on a real device.
3. **If the economy becomes irrelevant in the mid-game** the game dies. Countermeasure: every growth step also grows the fixed cost.
4. **Scope explosion.** The second-branch idea in chapter 6 has to be kept out of the first release for a one-person team. We should ship with chapters 1-4 first.
5. **If the staff AI is bad**, trust collapses. The shared wound of Cat Cafe Manager and Tavern Keeper. Countermeasure: keep staff behaviour simple and predictable, avoid complex pathfinding.

---

## 13. Open questions awaiting a decision

Your answers to these sharpen the plan. I have written my recommendation for each one.

1. **Art style:** soft low-poly 3D or full pixel HD-2D? *My recommendation: low-poly, reasons in section 8.*
2. **Engine:** Unity or Godot? *My recommendation: Unity. But your experience should be the deciding factor.*
3. **Team:** are you on your own, or is there support on the art or the code side? The roadmap durations change accordingly.
4. **Scope of the first release:** ship with chapters 1-4, or a smaller 1-3 scope? *My recommendation: 1-4, because expanding the venue is one of the best-loved mechanics and without it the game is incomplete.*
5. **Theme and setting:** a generic city restaurant, or a specific culture and cuisine (a Turkish tradesman's lokanta, for instance)? *My recommendation: a specific identity, because the most praised thing about Discounty and Hungry Hearts Diner is the sense of character and of place.*
6. **Monetisation:** free plus a full-version unlock, or paid up front? *My recommendation: free plus a one-time unlock.*
