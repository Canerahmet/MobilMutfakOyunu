# Review 1: Game Design and Balance

**Perspective:** senior game designer and economy balancer with fifteen years in management and tycoon games
**Items owned:** A4, A5, A6, A7, A8
**Date:** 9 September 2026

---

## Overall assessment

The structure of the plan is right and argued for unusually well: eight principles tied back to the research, every number labelled a "hypothesis", the test questions written before the answers. But the numeric layer does not hold together. The growth table in 12 §6 does not follow from the customer formula in 12 §5.1; the crew table in 14 does not follow from the capacity numbers in 14; demand (a formula) and capacity (staff) are nowhere reconciled with each other. On top of that, three of the four decisions in the daily loop (price, assignment, combo) turn into buttons that are solved on day one and never touched again. Verdict: the Phase 0 table must start from the formulas, not from these tables; the tables should be output, not input.

## The three strongest things

1. **The reputation formula brakes itself** (12 §5.1, §5.5). Because of the 0.5 floor, even at zero reputation 8 customers come to 4 tables; at low volume the daily swing shrinks as well. Recovery is slow but never impossible. This is the right maths against the "hard collapse" trap in the research.
2. **Honest information design.** Traits are visible before you hire (14, Hiring); the signature mechanic is bound to the year-end score as an axis (08, The cuisine-specific axis). The sentence "a management game, not a gamble" has made it into the numbers.
3. **The weekly lump payment plus expansion raising the rent** (02 §4, 12 §2). This is the only mechanism that can stop the economy from becoming irrelevant, and the question in 12 §7 — "by which week does money stop being a problem" — is exactly the right test.

## The five riskiest problems

### 1. The growth table does not follow from its own formula; the economy either becomes irrelevant or hits a ceiling (A4, A7)

12 §5.1's own examples say "7 tables, reputation 55 → 29", "10 tables, reputation 70 → 48", "14 tables, reputation 85 → 76". The §6 table in the same file writes 21, 34, 48 for almost the same inputs. Recomputed:

- Week 1: 13 × 50 × 7 = 4,550; ×0.68 = 3,094; −980 −1,800 = **+314**. That holds. But the 1.25 weekend factor has never been applied.
- Week 2: 15 × 52 × 7 = 5,460; ×0.68 = 3,713; −2,780 = **+933**. The table says +560; it does not hold.
- Week 3, by the formula: 28 × 1.02 = 29 customers. 29 × 56 × 7 = 11,368; ×0.68 = 7,730; −2,520 −3,400 = **+1,810**. The table says −322. The story that "the expansion week costs you money, growth makes you pay first" is not a design decision, it is the result of an arithmetic error.
- Week 5, by the formula: 40 × 1.18 = 47 customers → net **+4,964** (table: +1,066). Week 8: 56 × 1.38 = 77 → 40,425 revenue, net **+12,789**, margin 31.6% (table: +6,006, 19.8%).
- Week 7 wages: 2×980 + 3×770 + 700 + 630 = **5,600**; the table says 5,740.
- Cash: 8,000 +314 +560 −2,500 −322 +982 −4,500 +1,066 +2,808 = **6,408**. The last expansion costs 8,000; the player is 1,592 short. The line in 12 §6, "it takes your till down to 1,462", only comes out if week 7's profit is added — but that row already assumes 14 tables and 7,200 rent. Circular.

There are two ways out and both are bad. If the customer column of the table is demand, the economy becomes irrelevant by week 5 (see also problem 4, price). If the column is "served", i.e. a capacity ceiling, then at 14 tables 19 customers are turned away every day and 38 at the weekend; reputation only settles where demand = capacity: 56 × (0.5 + r/100) = 58 → **r ≈ 54**. The 88 reputation in the table is never reachable, and the year-end Reputation axis is closed not by the player's skill but by the staff cap.

**Fix:** delete the §6 table and regenerate it from the formula, with the weekend factor. Define the customer column as "demand" and write a separate reputation penalty for overflow (demand − capacity). Rescale weekly rent and wages against formula-derived revenue so that net margin does not exceed 20% by week 8. Add a cuisine term: the difference in "rhythm" that 07 promises is not in the formula at all, and the Turkish and fast food example margins sit in the same band (63-80%).

### 2. The capacity model contradicts the crew table; a cap of 8 is a trap (A8)

The base capacities in 14 are waiter 16, cook 20, dishwasher 26, cashier 34. What 58 customers need: 3 cooks, 4 waiters, 2 cashiers, 3 dishwashers = **12 people**. Even if the owner takes 12 of them, that is 11. The cap is 8. The sentence "the cap was tuned exactly to the need" is wrong by its own numbers. And the dishwasher, at 26, is the hardest bottleneck, not the first one; the claim that it comes "third" does not hold. In row 21 of the crew table the owner covers the till and the washing-up alone, but his capacity is 12. The compensation written down as "+30% with experience" is unreachable within the campaign: 1 point a day, a level every 30 points → the first employee reaches level 2 on day 60, at +20%.

The morale side is one-directional too: "−3 for consecutive busy days" is undefined and at 14 tables every day is busy. That is −21 + 5 = **−16** a week; morale falls from 70 to 15 in three and a half weeks, while the day-off mechanic is still "pending decision". Refusing a raise costs nothing: dropping to 50 morale with −20 only means "a slight chance of mistakes", and +5 a week brings it back. There is no "a raise or a cheaper new hire" decision — always refuse.

**Fix:** derive the crew table from the capacities; either raise the capacities (suggested: waiter 20, cook 24, dishwasher 40, cashier 50) or lower demand (tables × 3). Make the cap 3/5/7/9 and leave one empty slot at every tier. Make the owner's 12 "assigned to a single role each morning"; that adds a real decision to the morning counter. Define "busy" as 90% of capacity and add a balancing term to morale (suggested: +1 on a normal day). Stop experience gain on a refused raise. Make the day-off decision a "yes": on that day that role's capacity is zero, which makes it a planning decision.

### 3. Service scale, patience and "rare" archetypes do not fit each other (A7)

12 §5.2: service ≈ 120 seconds, patience 8-40 seconds. 58 customers in 120 seconds means one customer every 2 seconds; in Turkish cuisine 60% fall into the lunch slot (12 §5.6), and if the slots are equal that is 35 people in 30 seconds, more than one a second. A patience of 8-12 seconds means a queue of a handful of customers. With 3-5 interventions this is unreadable; 02's promise of "see the bottleneck and open it" turns into a particle system. It is also unclear whether a customer is a person or a party: a Family is 3-5 people while the bill, 50-75, looks per person.

11's claim that "rare, 5%, comes a few times over the campaign" is wrong as well: at 58 customers that is ~3 a day, about 100 rare visits over roughly 2,000 customers in the campaign. If a critic with a weight of 8 turns up every fourth day, the reputation formula swings on him alone.

**Fix:** scale service time with tier (inside 02's 90-180 s band: 120 at 4 tables, 180 at 14). Count customers as parties for throughput and keep the bill per person. Take the rare tier off a percentage and bind it to a calendar (suggested: 8-12 events per campaign), announced the evening before on the books screen. This is also the cure for problem 4.

### 4. The loop goes on autopilot by day 20: three decisions are solved on day one (A4, A6)

From 12 §5.3 and §5.4: price 20% above, average sensitivity −20 → satisfaction 80 → above the threshold of 60, satisfied. Tolerant is −8. Only the bargain-hunter (middle tier) leaves. Early on, with waiting near zero, pricing 20-25% above is the dominant strategy; that stacks another 20% on top of the revenue in the table and defeats 02 §6's goal that "the price decision must still matter in chapter 6". The Supermarket Simulator example from the research — "7% on everything, zero complaints" — is exactly this solvedness. Station assignment does not change during the day and, because the demand mix does not change, it is set once. Whether the combo is a setting made once or a daily decision is not written down. Dishes have no parameter other than margin (09's "Next step" promised a prep time, 12 did not deliver it); choosing among 12 burgers comes down to sorting by margin. And the Market contradicts two principles: 02 principle 2 says "ingredient ordering becomes automatic" while 02 §3.1 makes the Market "the second loop"; because 80% of fast food ingredients do not spoil, the Market becomes a game of "stock up on a cheap day" — and in the free, tutorial cuisine at that. If 26+12 ingredient lines are bought one by one, the 40-60 touch budget (principle 6) is spent before service even starts.

**Fix:** make the price penalty non-linear, accelerating above 10%, and tie tolerance to reputation (a high reputation carries a higher price) so that price becomes a decision again whenever reputation moves. Give every dish four parameters: prep time, station, at least one perishable ingredient, archetype pull. Make the Market an automatic baseline order plus 3-5 manual "opportunity/risk" lines a day. Make the combo a daily decision tied to that day's market price.

### 5. The fourth season is a plateau, and the signature mechanics are half reskin (A5, A6)

09, Progression curve: on days 46-60 there is no new system, 5 dishes, the crew at its cap, the last expansion bought (or, per problem 1, unbuyable). 45-90 minutes of flat play; that contradicts research §3 item 6 and the Supermarket Simulator late-game complaint head on. 08 defends it as "mastery", but mastery needs something that changes.

The mechanics: the Japanese soup stock really is a different shape of decision (estimating the quantity in the morning). The Turkish tab, as it stands, is a tax: the amount, the term, the collection calendar and the chance of non-payment are not written down; with 8,000 capital and a 2,780 weekly payment, credit on a 55 dish is noise. The combo is a button. Italian course timing wants a per-table decision, which conflicts with a budget of 3-5 interventions.

**Fix:** an event chain for season 4: the critic finale on a countdown, a rival restaurant pulled into season 3, one festival week (demand rises, the cap loosens temporarily). If that is not going to happen, cut the campaign to 45 days — 08 is already asking. Tie tab collection to a calendar out of phase with rent day (the civil-servant payday, the 1st and the 15th); make the book visible; let a meaningful share of weekly revenue go on the tab. Grow the menu slots with the table tier (suggested 4/5/6/8) so that the research's Cat Cafe complaint — "it unlocks but you cannot use it" — is not repeated; 32 dishes are only meaningful with parameters, otherwise v1's 20 were enough.

## Item decisions

| Item | Decision | Reasoning |
|---|---|---|
| A4 Economy numbers | REVISE | Price, wage, rent and loan structure are a fine starting point. The §6 table will be regenerated from §5.1 with the weekend factor; week 2's net (933 ≠ 560), week 7's wages (5,600 ≠ 5,740) and the cash ordering of the final expansion will be corrected; a cuisine term will be added to the formula. |
| A5 Content inventory | REVISE | 32 dishes may stay, but they are not approved until every dish carries four parameters (prep time, station, perishable ingredient, archetype pull); menu slots should grow with the tier. |
| A6 Progression curve | REVISE | The signature mechanic arriving on day 16 is right. If no event chain comes for season 4, cut the campaign to 45 days; move the last expansion to the end of week 7. |
| A7 Customer system | REVISE | The archetype and frequency structure is good. Settle customer = person/party, bind the rare tier to a calendar, scale service time, and write the overflow model and the non-linear price penalty. |
| A8 Staff system | REVISE | The trait design and the decision to keep the AI simple are right. Derive the crew table from the capacities, either accept the dishwasher bottleneck or raise the capacity, assign the owner's 12 to a single role per day, define "busy day", make refusing a raise cost something, day off "yes". |

## Unanswered questions

1. Is the customer count people or parties? Is the bill per person? Which unit are the formula and the capacity in?
2. Is expansion gated by season or by cash? With 8,000 capital the first 2,500 expansion can be bought on day 1.
3. What happens when demand exceeds capacity: does the customer turn away at the door, join a queue, and what is the reputation penalty?
4. Does downsizing (tier 5) lower the rent as well? Is voluntary downsizing possible? Which tier is "12 tables → 6" in 02 §5; the tiers are 4/7/10/14.
5. Are the owner's 12 customers in one role, or a total across three roles?
6. Is service a fixed 120 seconds, are the four slots equal, does it lengthen with the tier?
7. The 8-week loan term runs past the end of the campaign. How does the year-end Assets axis count outstanding debt? Will there be a 4-week term?
8. The tab: cap amount, term, collection calendar, chance of non-payment, the numeric value of loyalty.
9. A player who never expands stays in profit at 4 tables forever (+314 to +933 a week). Is the collapse ladder only going to bite the ambitious, or will there be a pressure such as a seasonal rent rise?
10. Is the combo a daily decision or a setting made once? With 20% spoilage in fast food, what will the Market actually force?
