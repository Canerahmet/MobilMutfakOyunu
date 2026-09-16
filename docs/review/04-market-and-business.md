# Review 4: Market and Business

**Perspective:** mobile publisher and product lead who has scaled a dozen simulation and tycoon games from soft launch, and who also knows the Steam indie market
**Items owned:** D2, D5, D6, and D1 in an advisory capacity
**Date:** 9 September 2026

---

## Overall assessment

As a design the plan is solid; as a business model it is weak. The research (01) rests almost entirely on Steam data; the audience that responds to "no timers, no energy" lives on Steam, but the plan puts reaching that audience last (21 §D5, "Steam: later") and expects the money to come from a satisfied mobile player who has finished the free game paying $4.99 for a second campaign. That conversion runs low in this genre, and with a zero budget there is not even the install volume to carry it. In the current order my estimate is that the mobile side produces a few thousand dollars a year; real money only arrives if Steam is moved forward and a real purchase trigger is put inside the free campaign. The plan is not "it will not make money", it is "it will not make money in the order it is planned".

## The three strongest things

1. **An honest model, and a real differentiator.** A single currency, no power for sale, no timers (07 "Why this structure is right", 12 §7.5). This profile protects the store rating, generates no refunds or complaints, and is the kind Apple's editorial featuring likes. Good Pizza's currency confusion (01 §2) is already closed on paper.
2. **The "a cuisine = a separate game" thesis and the plaque/legacy screen** (07, signature mechanic; 08, "The replay hook"). Selling content only works if the buyer feels it is "another game, not a repaint"; the tab and soup-stock mechanics can carry that promise. The goal of collecting four plaques makes the second and third purchases natural.
3. **A near-zero cost base, and an identity story that earns free press.** $124 in the first year (05, cost table); break-even is almost the first sale, and the real cost is your time. The story "a Turkish solo developer, a neighbourhood restaurant, from the country that made Supermarket Simulator" spreads by itself in local press and communities.

## The five riskiest problems

### 1. The audience is on Steam, the business model is on mobile, and Steam has been left for last (the biggest commercial risk)

**The problem:** nine of the ten references in 01 are Steam games; how Good Pizza, My Cafe and Cooking Fever make money is not in the research at all. The player who wants decisions to have weight and hates timers is already on Steam; the mobile audience downloads free sims, tolerates timers and does not pay for content. By saying "once all the cuisines are finished", the plan defers Steam by at least a year.

**The consequence (an estimate, not data):** with a zero budget, 10-50 thousand organic mobile installs in the first year at $0.05-0.12 of revenue per install, i.e. a few thousand dollars. A Steam release launched with 5-10 thousand wishlists can reach tens of thousands of dollars in its first year; modest, but several times the mobile figure.

**The fix:** open the Steam page as soon as the vertical slice is done (02 §11, end of Phase 1). The free fast food campaign is already a ready demo; enter Steam Next Fest with it. Early Access at $9.99 with two cuisines, $12.99-14.99 at 1.0 with four. The genre accepts EA (Tavern Keeper is at 94% in EA, 01 §1). Put at most 1-2 months between mobile global and Steam EA. This is also the answer to the question of the highest-leverage free channel: Steam Next Fest and wishlists. The streamers who made Supermarket Simulator a hit (01 §1, 4.2 million Twitch hours) played it on PC. On mobile the only free discovery channel is Apple's editorial featuring; submit the featuring form 6-8 weeks before launch — "no ads, no timers, cultural identity" is exactly the profile they look for.

### 2. The free campaign kills the conversion trigger

**The problem:** 07 Condition 2 says the player "must be able to finish and be satisfied without paying anything"; per 08 that is 4-6 hours. A purchase only becomes meaningful once the campaign is over, and an estimated 5-10% of installs get there. Because of the lock (15, "Four slots"), the purchase adds nothing to the existing save: "buy now, start from zero". A single paid SKU does not look "thin" on mobile — the player counts screenshots, not SKUs; what is thin is an $11.99 bundle selling two cuisines that do not exist.

**The consequence:** in premium-unlock models, conversion per install is already a low single-digit percentage (approximate, from experience); with the trigger placed 4-6 hours in, getting past a 1-2% band (an estimate) is hard. 30 thousand installs, 300-600 sales.

**The fix:** (a) turn the three-day reset window (15, line 33) into a **free taste** of the paid cuisine: the first three days of the Turkish restaurant free in a new slot, with the lock and the purchase screen at the end of day three. The player has seen what he is buying and has invested three days; refund risk drops, and 07's condition that "the player should see what he is buying before he buys" is satisfied by itself. (b) Make the year-end evaluation screen (08) the main sales window: the critic's write-up ends, and the offer of "the next plaque" arrives. (c) Let the purchase add something small to the existing save, for instance a "guest dish" week from the new cuisine. (d) Do not put the bundle in the store until the second cuisine ships. Keep Turkish cuisine as the first paid one: the lowest production risk, the strongest press hook, and mechanically the furthest from fast food. But make the first update Italian, not Japanese; that is the most legible cuisine on a global mobile storefront. The purchase screen should not sell "a neighbourhood restaurant", it should sell the mechanic "open a tab for your regulars, put rent day at risk"; the Turkish dish names with subtitles (18, "The dish-name decision") can stay. The precursor of a second purchase is the completion rate of the paid cuisine; the plaque system helps here, and it must be measured.

### 3. With a zero budget a "soft launch" produces no data; the metric definitions are wrong

**The problem:** a soft launch is a paid user acquisition instrument; 21 §D5 says "cheap user acquisition" but the budget is zero. A few hundred organic installs over six weeks in a small market cannot tell a 1% purchase rate from a 3% one (roughly 1-2 thousand installs are needed). The table in 21 confuses the calendar day with the in-game day: "day 7 retention = is the first rent day being passed" — but the first rent is on the game's day 7, and with 3-6 minute days that is 30-40 minutes of play, passed in the first session. "Day 30 = is the campaign being finished" is the same error; the campaign finishes in a week. Google Play requires a mandatory closed test before production for new individual accounts (a set number of testers, 14 uninterrupted days; the number has changed from time to time, verify it in the console), and 20 §C8 left the question "where will the testers come from" open. Analytics — "none or minimal" (21, open question 4) — is undecided; without analytics, none of the seven metrics can be measured.

**The consequence:** six weeks go by and what is left is noise; the global launch is made blind.

**The fix:** use Turkey for the closed test; it is the easiest place to find testers and the closed-test rating does not reach the store. Open test in the Philippines (volume, cheap Android device variety, English) plus Canada (payment behaviour similar to the US). Android first, iOS at global launch; that defers the iOS device question in 20. Budget a minimum acquisition spend of $300-500; for retention alone it buys 1-2 thousand installs. Fix the metrics: calendar D1/D7/D30 separately, "percentage reaching the game's day 7" separately; add to the funnel store page → install, purchase screen → purchase, paid-cuisine completion rate and crash rate (exceeding Play Console's approximate 1.1% crash / 0.5% ANR thresholds lowers visibility). The targets, as approximate experience thresholds for the genre: D1 ≥ 35%, D7 ≥ 15%, D30 ≥ 6%; campaign completion ≥ 10% of installs; first-month purchase ≥ 2%, or ≥ 25% of those who complete; crash-free sessions ≥ 99%. Below that, postpone the global launch.

### 4. Zero revenue from the 98% who do not pay; the price ladder has one rung

**The problem:** 02 §10 proposed an optional rewarded ad on the end-of-day screen and cosmetic décor packs; 07 and 21 quietly dropped them, and in 05 ads are "none". The research says the problem is not the existence of the ad but its placement (01 §4, item 7). Good Pizza, My Cafe and Cooking Fever all three earn with a dual currency, timers and ads; premium mobile (Stardew on mobile, the Kairosoft series) charges up front on the strength of a brand earned on PC or a catalogue of dozens of games. A brandless solo game cannot get installs at an up-front price, so a free entry is right; but then you have to monetise the free audience somehow. $4.99 is a high threshold as a single first purchase, and there is no SKU below it. There is no regional pricing decision: $4.99 at the raw exchange rate kills purchases in Turkey, which 21 §D5 calls "one of the main markets". A 20% bundle discount is weak; an $11.99 mobile bundle sitting just under a $12.99 Steam price creates the perception of "PC money for a phone game".

**The consequence:** all of the revenue depends on a 1-2% slice; a single bad variable zeroes it.

**The fix:** put back on the end-of-day books screen (02 §3, Stage 4) only the rewarded ad the player presses himself, "double today's profit"; it breaks neither the single-currency principle nor the message "no energy, no timers, no second currency". The cost: an ad SDK brings a consent burden (see 5). Add $1.99-2.99 décor packs; 10 already defines environment sets. Keep a cuisine at $4.99 and fill in Apple's and Google's regional price tables. The bundle at $9.99 once three cuisines are done (about 33%). Steam at $12.99-14.99 with a 10-15% launch discount (standard on Steam, and it has a visibility effect); do not run a launch discount on mobile, it leaves a low anchor. Note: Turkey is now priced in dollars on Steam; the identity market pays full price there.

### 5. There are concrete gaps in the store-compliance and legal list

**The problem:** "restore purchases" appears in none of the documents; Apple guideline 3.1.1 makes it mandatory for non-consumable products, and it is a rejection reason. There is no refund clawback: Google refunds by itself within 48 hours, and a refunded cuisine has to be re-locked in the app (Google voided purchases, Apple server notifications), otherwise a "buy, play, refund" leak comes straight out of revenue in a single-SKU model. Under the EU Digital Services Act, a developer selling in the EU files a "trader" declaration; name, address, email and telephone are publicly visible on the store page, and if you publish as an individual your home address is visible. Steam asks for a declaration of AI-generated content on the store page; the pipeline in 05 (Tripo, Meshy, ElevenLabs, Suno) makes that mandatory and carries a backlash risk with the cozy audience; D6's licence check does not cover it. "A separate application for each store" (21) is in practice Google's IARC questionnaire, Apple's own questionnaire and, optionally, Steam's; the wine pairing in Italian cuisine (07) pulls the rating into the 12/13+ band as an alcohol reference, which is good: it makes it possible not to declare a child-directed audience and to stay out of the COPPA/Google Families burden. There are no loot boxes and no gambling; "no randomised payments" is enough on the questionnaires. The claim that "no personal data is collected" (21, "The data collection principle") is not true: retention requires an install identifier, which under GDPR is pseudonymised personal data; a privacy policy, an Apple privacy label and a Google data-safety form are needed in any case; if an ad SDK is added, a Google-certified consent platform is mandatory for the EEA/UK. The game's name is not in the documents, and there is no name-search or registration step; 01 §2 records that Supermarket Simulator was cloned within weeks.

**The consequence:** an Apple rejection, a refund leak, your personal address exposed, no defence against clones.

**The fix:** add to D6: restore purchases (entitlement comes from the store and is not confused with the cloud save in 15), refund-revocation handling, a business address for the DSA (a sole proprietorship or a virtual office), the Steam AI declaration, a 13+ audience declaration, a minimum consent flow for the install identifier (opt-in in the EEA; ask a lawyer), name registration at the Turkish Patent Office and the store IP complaint process, an application to Apple's Small Business Program and Google's 15% (05 assumes it is automatic — verify), and an accountant for foreign income. The Steam refund window is two hours: pull 08's goal that "all systems open in the first three hours" down to two.

## Item decisions

| Item | Decision | Reasoning |
|---|---|---|
| D1 Revenue model (advisory) | REVISE | Selling content and not selling power is right; but the optional end-of-day ad and the cosmetic packs from 02 §10 must come back, and the three-day taste trigger must be built. Otherwise the model takes nothing from the 98% who do not pay and the trigger stays 4-6 hours in. |
| D2 Price | REVISE | A cuisine at $4.99 is fine. The bundle only when there are ≥2 cuisines, and at $9.99. Steam EA $9.99, $12.99-14.99 at 1.0, a 10-15% launch discount. A regional price table and a $1.99-2.99 décor tier must be added. |
| D5 Launch plan | REVISE | Reverse the order: the Steam page at the end of Phase 1, the Next Fest demo = the free campaign, Steam EA at most 1-2 months after mobile global. Closed test Turkey, open test Philippines + Canada, Android first; a $300-500 acquisition budget; metric definitions and thresholds as in problem 3. |
| D6 Legal | REVISE | The list is right but incomplete: restore purchases, refund revocation, the DSA trader address, the Steam AI declaration, the 13+ declaration, analytics consent, name registration, commission-programme applications. With those added, APPROVE. |

## Unanswered questions

1. No conversion model can be built without measuring the rate at which the free campaign is finished. Does the Phase 2 fun test (02 §11) ask for the "finished the 60 days" and "wanted the second cuisine after finishing" rates?
2. How will two cuisines, 8-12 hours of content and a touch UI get past the "mobile port" perception on Steam? Is there a desktop scaling and keyboard/mouse plan in 19?
3. Will you cap the share of AI-generated assets? The Steam declaration and the community reaction depend on it.
4. Is Turkey a revenue market or a press market? Given purchasing-power pricing and Steam's dollar pricing, what sales are expected from there?
5. Is "no budget" really zero? Even $500 of test acquisition and the $100 Steam Direct fee contradict that sentence.
6. The analytics decision (21, open question 4) is a precondition of D5: which provider, which consent flow, which retention period?
7. What is the game called? Is it free on the three stores and as a domain, has a trademark search been done?
