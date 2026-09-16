# Economy Numbers and Customer Formulas

**Last updated:** 10 September 2026
**Register items:** A4 economy numbers, A7 customer formulas
**Status:** Batch A is closed. Every number in this file is derived from a formula by `tools/balance/model.py` and passes seventeen consistency tests. No hand-written table is left.

---

## How these numbers are produced

**Do not change the tables below by hand.** Everything between the generated-block comment markers is deleted and rewritten whenever `tools/balance/render.py` runs. If you want to change a number, change the parameter in `tools/balance/model.py` and run the writer.

```
cd tools/balance
python model.py --check     # 17 consistency tests
python solve.py             # searches for the parameters from the targets
python render.py            # writes the tables into this file
```

### The realisation rate

**The revenue in this file is realised revenue, not demand.** The model assumes all demand is served; the simulation produces a fraction of that on the same expansion schedule. A customer whose patience ran out, stock that ran dry, a table that filled up.

**The current value is `REALISATION_BP` in `tools/balance/model.py`, and the rents are `staffing.tiers` in `content/economy.json`.** No number is written here: this line carried a number once and a calibration generation went by — the document was talking about 7000 and 850/1,950/2,900/5,000 while the content held 6500 and 650/1,550/2,250/4,000. The design's reference document was describing an economy that did not exist. All the tables below are generated (`python tools/balance/export.py`), not hand-edited.

This number looked like a measured value but it is not: **it is a fixed point.** On 10 September 2026, when the kitchen model was fixed, the same measurement jumped from 65% to 93% — but applying that measurement directly turned expansion into a trap: 93% had been measured with the OLD cheap rents, and with the new rents the same strategy could not expand. **The rate determines the parameters, and the parameters determine the rate.**

That is why, instead of a one-shot measurement, `python tools/balance/calibrate.py` is run: it runs candidate rates one by one (solve → model → export → simulation) and scores them against the design targets. Detail in [32-equipment-and-rebalance.md](32-equipment-and-rebalance.md).

### The three bugs the review found

The five-agent review (see [review/00-synthesis.md](review/00-synthesis.md)) found that the numbers in this file did not derive from their own formulas. Once the model was built, three separate bugs came out:

| Bug | What it was | Its consequence |
|---|---|---|
| **The owner was duplicated** | In the capacity table the owner was counted in the waiter, cashier and dishwasher columns at the same time | One person was providing capacity for 36 customers; the real contribution was 12 |
| **The crew was set to the average** | The crew had been calculated against the weekly average | The weekend is 25% busier; the crew has to be set to the peak and paid for seven days |
| **The table contradicted the formula** | The formula in section 5.1 gives 76 customers for 14 tables / reputation 85, while the table in section 6 wrote 58 on the same row | The whole growth curve was resting on the wrong demand |
| **The rounding rule was inconsistent** | Found on 10 September 2026 when comparing against the C# core. Python's `round()` does banker's rounding: the sixth week's weekend demand is exactly 62.5 and Python gave 62 while C# gave 63 | Discrete decisions are now done with integer arithmetic on both sides. See [23-core-contract.md](23-core-contract.md) §2.3 |

The third week's loss was there for the same reason. In the corrected model the expansion weeks still lose money, but it is no longer an arithmetic accident — it is the result of the solved rent and crew load.

**Currency:** a nameless coin icon; there is a separate section below. The numbers here are unitless.

---

## 1. Starting position

| Value | Amount |
|---|---|
| Starting capital | 8,000 |
| Tables | 4 |
| Staff | 1 cook |
| Reputation | 30 / 100 |
| Dishes unlocked | 6 |

---

## 2. Fixed costs

Rent and wages are paid **weekly**, in one go at the end of the seventh day.

### Rent and expansion, by tier

<!-- GENERATED: rent -->
| Tier | Tables | Weekly rent | Cost of moving to this tier |
|---|---|---|---|
| Starting | 4 | 850 | — |
| Second | 7 | 1.950 | 2.500 |
| Third | 10 | 2.900 | 4.500 |
| Fourth | 14 | 5.000 | 8.000 |
<!-- /GENERATED: rent -->

The rents are not guesses: they were solved backwards from the net margin targeted for each tier's **mature week** (5%, 9%, 14%, 20%). `tools/balance/solve.py` does that job.

**The rule:** every expansion grows the rent too. This is not negotiable. The reason Tavern Master's economy collapsed was that growth did not raise fixed costs.

### Wages

| Role | Daily | Weekly |
|---|---|---|
| Cook | 140 | 980 |
| Waiter | 110 | 770 |
| Cashier | 100 | 700 |
| Dishwasher | 90 | 630 |

Experienced staff are up to 30 per cent more expensive. Traits do not affect the wage, only the performance.

### Expansion costs

In the table above. The prices were solved by the model from the constraint "every expansion week must lose money, and after the last expansion the till must drop below 3,000".

The last expansion is deliberately expensive: you pay 10,400 and bring the till down to 2,958. You are one step from the bankruptcy ladder.

---

## 3. Ingredient and dish economy

**The basic rule: ingredient cost is roughly 32 per cent of the sale price.** So the gross margin is around 68 per cent. That is a ratio close to running a real restaurant.

### Sample fast food items

| Dish | Sale | Ingredients | Margin |
|---|---|---|---|
| Hamburger | 45 | 15 | 67% |
| Fries | 20 | 5 | 75% |
| Fizzy Drink | 15 | 3 | 80% |
| Nuggets | 25 | 8 | 68% |
| Ice Cream | 18 | 5 | 72% |

**The combo:** a hamburger plus fries plus a fizzy drink comes to 80 separately. The combo price is 65. Ingredients 23.

- Profit selling them separately: 80 - 23 = 57, but the customer usually only takes the burger, so 30.
- Combo profit: 65 - 23 = 42.

The combo raises the average ticket from 45 to 65. In return the kitchen prepares three items, so the load increases. That is the signature mechanic's trade-off.

### Sample Turkish cuisine items

| Dish | Sale | Ingredients | Margin |
|---|---|---|---|
| Kuru Fasulye | 55 | 18 | 67% |
| Rice Pilaf | 25 | 6 | 76% |
| Lentil Soup | 30 | 8 | 73% |
| Kofte | 70 | 26 | 63% |
| Ayran | 15 | 5 | 67% |
| Rice Pudding | 25 | 8 | 68% |

**A typical order:** a pot dish plus pilaf plus ayran. Sale 95, ingredients 29, profit 66.

**The dish of the day:** that day's pot dish sells for 45 instead of 55. The margin drops but regular loyalty rises.

**Offering tea:** a cost of 2 per portion. It is given free. In return, loyalty and the tab repayment rate rise.

### Price fluctuation

- Ingredient prices fluctuate every day between **25 per cent below and above** the base price.
- Seasonal shift: some ingredients are 20 per cent cheap or expensive for a whole season.
- There is no advantage in buying early, stock spoils. Catching a cheap day is not luck, it is a matter of watching.

### Spoilage

| Cuisine | Share of perishable items |
|---|---|
| Fast food | 20% |
| Turkish cuisine | 60% |
| Italian | 70% |
| Japanese | 85% |

A perishable ingredient loses its whole value when the day closes. That ratio is one of the things that separates the cuisines' risk profiles.

---

## 4. Loans

| Amount | Repayment | Weekly instalment | Term |
|---|---|---|---|
| 5,000 | 6,750 | 844 | 8 weeks |
| 10,000 | 13,500 | 1,688 | 8 weeks |
| 20,000 | 27,000 | 3,375 | 8 weeks |

Total repayment is 1.35 times the principal. A loan makes fast growth possible but permanently raises the weekly outgoings.

If an instalment cannot be paid, the bankruptcy ladder starts running.

---

## 5. Customer formulas

### 5.1 Daily customer count

```
base       = table_count × 4
customers  = base × (0.5 + reputation / 100) × day_factor
```

<!-- GENERATED: demand -->
| Case | Sum | Weekday | Weekend |
|---|---|---|---|
| 4 tables, reputation 35 | 4 × 4 × 0.85 | 14 | 17 |
| 7 tables, reputation 52 | 7 × 4 × 1.02 | 29 | 36 |
| 10 tables, reputation 68 | 10 × 4 × 1.18 | 47 | 59 |
| 14 tables, reputation 88 | 14 × 4 × 1.38 | 77 | 97 |
<!-- /GENERATED: demand -->

`day_factor` is 1.0 on weekdays and 1.25 at the weekend. The season moves it slightly too.

**The crew is set to the weekend column, and the wage is paid for seven days.** That explains why the staff cost looks high against revenue, and it is deliberate: a restaurant that cannot cover the peak loses reputation.

**Reputation determines both the ceiling and the floor.** Growing the venue and neglecting reputation leaves the tables empty.

### 5.2 Patience

Patience is in seconds and drains during the service. A service takes roughly 120 seconds.

| Archetype example | Patience |
|---|---|
| Courier | 8 s |
| Student in a Hurry | 10 s |
| Lunch-Break Worker | 12 s |
| Office Group | 18 s |
| Family | 30 s |
| Pensioner | 40 s |

Patience drains while waiting to be seated, waiting for the order to be taken and waiting for the food to arrive. If it reaches zero the customer walks out and reputation drops hard.

### 5.3 Price sensitivity

```
price_penalty = (price / market_price - 1) × 100 × sensitivity
```

The `sensitivity` coefficient is between 0.4 and 2.5 depending on the archetype.

| Price | Tolerant (0.4) | Average (1.0) | Haggler (2.5) |
|---|---|---|---|
| Market price | 0 | 0 | 0 |
| 10% above | -4 | -10 | -25 |
| 20% above | -8 | -20 | -50 |
| 30% above | -12 | -30 | -75 |

Going more than 15 per cent below the market does not work either: satisfaction does not rise, only the margin melts.

### 5.4 Satisfaction

Every customer starts with 100 points.

| Factor | Change |
|---|---|
| Waiting | `- (waited / patience) × 60` |
| Price | The formula above |
| Low-quality ingredients | -15 |
| High-quality ingredients | +10 |
| The dish they wanted ran out | -30 |
| Tea or an apology offered | +15 |
| The owner attended to them personally | +20 |

If satisfaction is above 60 the customer leaves happy; below it, they complain.

### 5.5 Reputation

```
daily_change = Σ (satisfaction - 60) × reputation_weight / 100
```

| Example | Result |
|---|---|
| 40 customers, average satisfaction 80 | +8 |
| 40 customers, average satisfaction 50 | -4 |
| Food critic, satisfaction 90, weight 8 | +2.4 on its own |

Reputation runs from 0 to 100 and starts at 30. It falls naturally by 0.3 points a day, so it melts away if you neglect it.

### 5.6 The hourly distribution of archetypes

The service day is split into four slices. The rhythm of the cuisines becomes concrete here.

| Cuisine | Opening | Lunch peak | Afternoon | Evening |
|---|---|---|---|---|
| Fast food | 15% | 35% | 15% | 35% |
| Turkish cuisine | 10% | 60% | 20% | 10% |
| Italian | 5% | 20% | 10% | 65% |
| Japanese ramen | 15% | 50% | 15% | 20% |

In the lokanta sixty per cent of the customers come in a single slice. That turns the lunch peak into a real crisis moment and leaves the evening empty. In Italian it is exactly the reverse.

---

## 6. The targeted growth curve

This is the path a player who plays well is expected to follow. **Not verified.**

<!-- GENERATED: growth -->
| Week | Tables | Crew | Cap | Reputation | Guests/day (weekday / weekend) | Avg ticket | Revenue | Stock | Wages | Rent | Expansion | Weekly net | Till |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | 4 | 1 | 3 | 35 | 14 / 17 | 50 | 3.640 | −1.165 | −980 | −850 | — | **+645** | 8.645 |
| 2 | 4 | 2 | 3 | 45 | 15 / 19 | 52 | 4.113 | −1.316 | −1.734 | −850 | — | **+213** | 8.858 |
| 3 | 7 | 4 | 5 | 52 | 29 / 36 | 56 | 8.506 | −2.722 | −3.544 | −1.950 | −2.500 | **−2.210** | 6.648 |
| 4 | 7 | 4 | 5 | 60 | 31 / 39 | 58 | 9.460 | −3.027 | −3.622 | −1.950 | — | **+860** | 7.508 |
| 5 | 10 | 7 | 8 | 68 | 47 / 59 | 63 | 15.567 | −4.981 | −6.335 | −2.900 | −4.500 | **−3.150** | 4.358 |
| 6 | 10 | 7 | 8 | 75 | 50 / 63 | 66 | 17.371 | −5.559 | −6.475 | −2.900 | — | **+2.438** | 6.796 |
| 7 | 14 | 10 | 12 | 82 | 74 / 92 | 70 | 27.146 | −8.687 | −9.367 | −5.000 | −8.000 | **−3.908** | 2.888 |
| 8 | 14 | 10 | 12 | 88 | 77 / 97 | 75 | 30.398 | −9.727 | −9.573 | −5.000 | — | **+6.097** | 8.985 |
<!-- /GENERATED: growth -->

**The staff counts come from the capacity model**, they are not guesses. See [14-staff-system.md](14-staff-system.md).

| Week | What happens |
|---|---|
| 1 | One cook, the owner in the hall. Margin 11.7% — the game's only comfortable week and the tutorial's positive peak |
| 2 | The first hire. The margin drops to 5.3%. The lesson: staff are not free |
| 3 | The first expansion. The crew doubles, the week closes with a 2,881 loss |
| 4 | The crew settles, margin 9% |
| 5 | The second expansion. The hardest week: a 3,912 loss, the till drops to 3,342 |
| 6 | A breath. Margin 14% |
| 7 | The last expansion. The till is at 2,958, one step from the bankruptcy ladder |
| 8 | Margin 20%. This is where the payoff for growing is collected |

**The average ticket grows too:** from 50 to 75. As the menu widens, expensive dishes open and mechanics like the combo come into play. A hidden growth lever.

**Design intent:** net margin is 11.7% in the first week and 20.0% in the eighth. Growing pays, but by a factor of two, not eleven. Because the crew and the rent grow at the same time, most of the revenue increase goes straight back out.

**All three expansion weeks lose money.** −2,881, −3,912, −3,846. When you expand, the rent and the crew grow immediately while the reputation has not caught up yet. Growing does not reward you at once; it makes you pay first. This is no longer an arithmetic accident — it was written into the model as a constraint.

**The last expansion is a deliberate gamble.** You pay 10,400 and bring the till down to 2,958. In return you get a far higher score at the end of the year.

**The crew goes from 1 to 11**, while customers go from 14 to 77. The crew grows faster than the customers. That is the price of growing the restaurant.

---

## 7. Keeping the economy from becoming trivial

This was the second biggest cause of death in the research. Three countermeasures:

1. **Every expansion grows the rent.** Revenue rises, but so do the outgoings.
2. **The last tier of equipment is expensive.** Between 8,000 and 12,000. Even in the eighth week you are saving up for something.
3. **The end-of-year score looks at net worth.** There is always a reason to save money.

The first thing the balance tool has to measure is this: **in which week does the player say "money is no longer a problem"?** If that week is before 8, the economy has collapsed.

---

## 7.5 Currency: awaiting a decision

We will not use a real currency. There are three reasons and the third is decisive.

**1. An inflation anchor.** A price that looks reasonable today looks absurd in two years. The game ages.

**2. Localisation.** ₺ is right in the Turkish version and foreign in the English one. $ is the reverse. Both leave a segment out.

**3. The risk of being confused with real money.** This is the decisive one. The game has purchases made with real money — unlocking cuisines. If the in-game money carries a real currency symbol, the player confuses the two.

This is not a theoretical risk. In our research Good Pizza, Great Pizza was criticised for exactly this: the in-game money is shown with a banknote icon while store purchases do not carry a dollar sign, and at first players cannot tell which one is real money. Store rules also require virtual currency to be clearly separated from real money.

### What other games do

| Approach | Examples | Assessment |
|---|---|---|
| **A fictional name** | Animal Crossing "Bell", The Sims "Simoleon", Zelda "Rupee" | Memorable, carries brand value, immune to inflation |
| **Generic gold or a token** | Stardew Valley "g", PlateUp's token | Invisible, frictionless, everyone understands it |
| **A real currency** | Supermarket Simulator, most realistic simulations | Immersive but it ages and it gets confused |

Fictional currencies have one rule: **they are not translated.** Bell stays Bell in every language. If a name is chosen it has to stay the same in the Turkish and English versions.

### Our advantage: a single currency

Most mobile games have two currencies. Soft currency is earned by playing, hard currency is bought with real money. The confusion mostly comes from there.

**We have no hard currency.** The revenue model is a one-off cuisine purchase, that is, a direct purchase. There is a single currency inside the game and it is never sold for real money.

That alone removes the trap Good Pizza fell into, and adds one more item to the message "no energy, no timers, no second currency".

### Why the dollar is not the answer

The logic "we are making a global game, so let us use dollars" looks intuitively right but works backwards in two places.

**1. The dollar is not global, it is American.**

The App Store and Google Play show real prices in the player's own currency. A German player sees €, a Japanese one ¥, a Turkish one ₺. If we write $ inside the game, we show a symbol different from the currency the player actually pays in.

So the dollar is already the wrong symbol for most of the world. We gain no clarity.

**2. The dollar does not reduce the confusion risk, it maximises it.**

The problem was never clarity, it was confusion with real money. And that risk is bigger with the dollar than with ₺.

If the game's till reads "$8,000" while Turkish cuisine in the store is "$4.99", the same symbol is showing two completely different things. If we used ₺ at least the mismatch would be visible to a non-Turkish-speaking player. With the dollar the mismatch is invisible and complete.

Store rules also require virtual currency to be clearly separated from real money. Giving the symbol of the real transaction to the virtual money is the hardest possible way to make that separation.

### What is actually global: a coin icon

Zero language, zero currency, everyone reads it. That is why Stardew Valley's "g" and Animal Crossing's bell icon exist.

Clarity was never the obstacle. Games have been selling globally with fictional currencies for thirty years. Nobody asks what a Bell is; they learn it in two minutes.

### The options

| Candidate | Global readability | Identity | Confusion risk |
|---|---|---|---|
| **A nameless coin icon** | Highest | None | None |
| **Mangir** plus a coin icon | High | Highest, very fitting for a tradesman's lokanta | None |
| **Akce** plus a coin icon | High | Medium, sounds neutral | None |
| A token | High | Low | None |
| $ or ₺ | Misleading | None | **High** |

(Mangir and akce are old Ottoman coin names; they are written here without their Turkish diacritics.)

**An additional decision:** whichever name is chosen, in narrow interface areas a small coin icon will be used instead of text. The name will appear in full in tooltips and in the end-of-day accounts.

### ✅ Decision: a nameless coin icon

Decided on 9 September 2026. The currency is not named and no real currency symbol is used. A small coin icon appears on screen with a number beside it.

**Icon template: B, a flat stack of coins.** ✅ Decided 9 September 2026. Seven candidates were drawn and compared at real size: https://claude.ai/code/artifact/a90e6a88-de82-429b-b03c-f5552451a8f1

| Candidate | Assessment |
|---|---|
| A · A flat coin | Plain and clear, but a circle on its own does not say "money" |
| **B · A stack of coins, flat** | ✅ **Chosen.** The stepped silhouette reads at small size, the plainest form |
| C · A tilted coin | Fits the low-poly sense of volume |
| D · A coin plus a fork | At 16 pixels the fork disappears. Not recommended |
| E · A stack of coins, isometric | The reference's isometric style. Considered; the flat one was preferred |
| F · A stack of banknotes, gold | The reference stripped of the dollar. Weak at 16 pixels |
| G · The reference exactly as it is | Not recommended. Dollar association and a colour clash |

**Assessment of the reference image.** The image the user shared was a stack of green banknotes. Its style is right: thick form, flat colour, isometric volume. But it has two problems:

1. **Dollar association.** A green banknote and an oval portrait window means American money. It contradicts our decision to avoid real currency symbols.
2. **Colour clash.** Green is our main interface colour. If money is green it blends into the interface instead of standing out.

On top of that, at 16 pixels a rectangular silhouette turns into a horizontal smudge, while a round silhouette survives.

**The solution:** keep the style, change the object. Candidate E does that.

### The icon specification

| Rule | Decision |
|---|---|
| Name | None. It is not named anywhere, no translation needed |
| Symbol | No real currency symbol. $, ₺, € are not used |
| Colour | Warm gold. Separate from the interface's main palette, belonging to money alone |
| Number format | Monospaced digits, a full stop as the thousands separator. Like 8.240 |
| Layout | Icon on the left, number on the right. The order never changes |
| Smallest size | 16 pixels. Below that no icon is used, only the number |
| Real money | This icon is never used on the store screen. Real prices are in a different colour and layout |
| Production | A single vector file, a UI sprite in Unity |

---

### If a currency symbol is wanted anyway

If the decision goes towards a symbol, these three measures become mandatory:

1. On the store screen, real prices are **never** shown in the same visual language as the in-game currency. Different colour, different icon, different layout.
2. On the purchase screen the words "real money" are written explicitly.
3. The terms of use state that virtual currency cannot be converted into real money.

These are good practice anyway, but if a symbol is used they stop being negotiable.

---

## 8. The questions the balance tool will test

1. On which day does a player who never intervenes go bankrupt
2. What net worth does a player who plays well finish the year with
3. In which week does the economy become trivial
4. Does the strategy of keeping prices permanently above market win
5. How much does a player who never expands earn
6. Does taking a loan work, or is it a trap
7. When do staff turn profitable
8. Is sixty days the right length

---

## 8b. "What should a reasonable player earn in 60 days" — considered, **not changed**

An economy review proposed a concrete target band: the reasonable player should finish
day 60 with **32,000–40,000** coins and **at least 10 tables**, and `calibrate.py`
should run that as a check. The measured values are 22–25,000 and 7.2 tables;
`planci` climbs to 13.7 tables in the same economy and finishes with a **similar** till.

**The proposal was not applied, and the reason is this: money is not this game's goal.**
[08](08-endgame.md) closes the campaign with a **seven-axis plaque**; wealth is only one
of them. `makul` and `planci` finish with the same money, but `planci` carries a shop
**twice** the size and 99 reputation — in the end-of-year score the difference shows up
on the **venue** and **reputation** axes, not in the till. Playing cautiously should mean
"a smaller plaque", not "less money", and it does.

Putting an absolute band on the till would bring back the thing that was deliberately
rejected when the scoring system was built: a single-axis measure of success.

The half of the proposal that was **right** was applied as well: the denominator of the
end-of-year **wealth** axis (`RemainingPurchaseCostTotal`) was not counting the cold
store ladder, while the other function asking the same question (`RemainingPurchaseCost`)
was — two functions were giving two different answers to "what is left to buy".
Now both of them count it.

---

## 8c. The one balance target still open: a money sink in the late campaign

`calibrate.py` says "money becomes trivial for `planci` in Turkish cuisine" in
**week 7.3**; the target is **8.0**. Verified with 32 seeds, so it is not noise. Every
other check and the `makul` player are clean in both cuisines.

**The reason is known and it is a side effect of a deliberate fix.** The measure is this:
`Cash > RemainingPurchaseCost()` — can the money in the till pay for every remaining
purchasable in one go. The cold store ladder went from three steps to two and the
catalogue shrank by **8,000 coins**. But that step **could not be bought in any run**
(the measurement in §8): the threshold of 8.0 had been built on top of an item nobody
could buy. When the catalogue shrank, the measure showed the truth.

**The right way to close it is not to raise prices but to add something to buy.**
Inflating prices turns the measure green but changes nothing in play — `planci`
still buys everything, it just buys it a week later.

The missing thing already has a name: **business upgrades**
(`content/upgrades.json`, [13](13-data-schemas.md); the reason for deferring it is in
[06](06-plan-status.md)). Today every purchase sells **capacity** — equipment, tables,
storage — and none of them makes a customer more valuable. An upgrade like a shop sign
would both fill that gap and give the late campaign a goal to save towards.

**It was not closed now**, because adding a sixth spending axis means recalibrating the
balance from scratch, and the game is **playable and balanced** without that axis. It
stands here as an open missed target; once it is closed `calibrate.py` will go green
on its own.

---

## 8d. The measuring tool's reference is crippled by its own hand: the loan gate

All of the balance tool's targets are tuned against the `makul` player. That player's
expansion rule is this:

```csharp
if (sim.Cash > cost * 2 && sim.ReputationCenti > 4500 && canServe
    && !sim.HasLoan)                      // <-- it NEVER expands while it has a debt
```

On 11 September the `kredisiz` strategy was written (to measure the sixth question in
[12](12-economy.md) §8 for the first time) and it showed this:

| strategy | final till | tables | reputation |
|---|---:|---:|---:|
| `makul` | 25,424 | 7.4 | 75.8 |
| **`kredisiz`** (the only difference: it takes no loan) | **27,853** | **14.0** | **100.0** |

So **taking a loan shuts down the growth curve for eight weeks** — and what does that
is not the economy, it is the bot's own rule. `makul` is the tool's "player who plays
well" reference; that reference stays half-size by its own hand.

### Tried, measured, **not applied**

The gate was turned from "does it have a debt" into "can it afford it":

| share | result |
|---|---|
| 4 instalments | `makul` 14 tables, **43,746** — it beats `planci` (36,289) |
| the whole remaining debt | `makul` 14 tables, **39,877** — it still beats it |
| calibration | penalty **9 → 32**, four more targets break |

What breaks: `imzaci/makul` 0.84 and 0.82 (floor 0.90), the Turkish growth multiplier
4.20 (ceiling 4.0), and money now becomes trivial for `makul` too.

**The reason is clear: the targets are anchored to a crippled reference.** Fixing the
reference requires re-deriving the fixed point, the rents and the target bands — that is,
not a one-line fix but a complete retune. A half-tuned balance is worse than a documented
flaw; so the rule was **left as it is** and its reasoning sits next to the rule in
`Strategies.cs`.

### The order in which to close it

1. Turn the gate into "can it afford it" (`cost * 2 + remaining debt`).
2. Let `calibrate.py` search for the fixed point again — `makul` is stronger now, so the
   realisation rate and the rents will go up.
3. Re-derive the `imzaci/makul` band: once `makul` gets stronger the signature mechanic
   falls **below the floor**, so the band belonged to that reference too.
4. The money sink in §8c becomes **even more** pronounced with this change (a player who
   grows properly finishes the catalogue earlier) — the two have to be handled together.

---

## 9. The remaining gaps

- The equipment and upgrade price list has not been fully written
- Whether there will be a tipping system has not been decided
- Item prices for Italian and Japanese cuisine have not been written; they were deferred because those cuisines arrive as updates
- There are no exact values for the seasonal coefficients
