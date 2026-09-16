# Customer System: Archetypes

**Last updated:** 9 September 2026, v2
**Register item:** A7 customer system detail, first half
**Status:** The archetypes are written. The formulas will come with the economy work.

**The v2 change:** the archetype count per cuisine was raised from 8 to **20**. A frequency tier was added to stop them blurring together, and some of them were made shared.

---

## What an archetype is

**An archetype is not a person, it is a behaviour template.**

When a customer comes through the door the game does this:

1. Picks an archetype according to the hour, the cuisine and the frequency tier.
2. Copies that archetype's parameters.
3. Generates an appearance for them from the wardrobe subset the archetype points at.
4. Puts them on stage.

The archetype is invisible. What the player sees is a uniquely dressed human generated from that template.

**How it differs from a named regular:** a named customer is one single person, hand-written, with a story, and always the same person. An archetype produces thousands of customers.

---

## The eight parameters

| Parameter | What it does |
|---|---|
| **Patience** | How long they wait before they get angry and walk out |
| **Spending tendency** | How many items they order, how much they leave |
| **Price sensitivity** | How much it bothers them if the price is above the market |
| **Group size** | How big a table is needed |
| **Arrival hour** | When during the day they are likely to come |
| **Order preference** | Which group on the menu they head for |
| **Reputation weight** | How much their satisfaction or complaint moves reputation |
| **Tendency to become a regular** | The chance of coming back and turning into a regular |

**A critical connection:** the archetype also tells the dress-up system which clothes and accessories to pick. The construction worker archetype pulls a hard hat and work clothes. That way, when you look at the hall, you can tell who is sitting there from the silhouette. Behaviour and appearance are tied to each other, and that tie is what creates the cuisine's identity.

---

## The frequency tier: why twenty archetypes do not blur together

As variety increases, the archetypes risk resembling each other. The **frequency tier** solves that.

| Tier | Count | Share of traffic | Its function |
|---|---|---|---|
| **Frequent** | 8 | Roughly 70% | The backbone of the day. The player learns these and plans around them |
| **Medium** | 8 | Roughly 25% | Texture. A few come every day and keep the day from being uniform |
| **Rare** | 4 | Roughly 5% | An event. They come a few times across the campaign and change the shape of that day |

Rare archetypes are, by design, **events**. The day the food critic comes, the game plays completely differently. The day the bulk order comes, the kitchen locks up. These produce remembered moments.

---

## The eight shared archetypes

These work in every cuisine; only their clothes change with the cuisine. Written once.

| Archetype | Tier | Standout feature |
|---|---|---|
| Solo Diner | Frequent | One person, medium patience, fast turnover |
| Couple | Frequent | Two people, medium patience, medium spend |
| Family | Frequent | 3-5 people, high patience, high spend |
| Courier, takeaway | Frequent | Very low patience, one item, does not occupy a table |
| Parent with Child | Medium | Low patience, a dessert order is certain, long table time |
| Traveller | Medium | One-off, never becomes a regular, low reputation weight |
| Sharing Group | Medium | Triple reputation weight, both the praise and the complaint grow |
| Food Critic | Rare | Very high quality expectation, the highest reputation weight |

---

## Twelve archetypes specific to fast food

| Archetype | Tier | Standout feature |
|---|---|---|
| Student in a Hurry | Frequent | Low patience, tight budget, buys the combo |
| Office Group | Frequent | 2-4 people, lunch peak, expects fast service |
| Post-Workout | Frequent | High spend, portion-focused, evening |
| Shopping Break | Frequent | Medium patience, adds a dessert and a drink |
| Haggler | Medium | Very high price sensitivity, never comes if you are above market |
| Late-Night Diner | Medium | High patience, near closing, medium spend |
| Match-Day Crowd | Medium | Crowded, loud, high spend, sits for a long time |
| Dieter | Medium | Looks for salad and vegetarian; if there is no option they walk out |
| Night Shift | Medium | At closing time, one person, can become loyal |
| Birthday Party | Rare | Very crowded, dessert-heavy, large revenue in one go |
| Complainer | Rare | Very hard to please, carries a reputation risk |
| Bulk Order | Rare | A huge order in one go, locks up the kitchen |

---

## Twelve archetypes specific to Turkish cuisine

| Archetype | Tier | Standout feature |
|---|---|---|
| Neighbouring Shopkeeper | Frequent | The highest tendency to become a regular. The prime candidate for the tab book |
| Lunch-Break Worker | Frequent | Very low patience, fast service is essential, a sharp lunch peak |
| Construction Worker | Frequent | High portion expectation, medium spend, lunch |
| Civil Servant | Frequent | Waits for the dish of the day, high patience, becomes a regular |
| Pensioner | Medium | Very high patience, low spend, very sensitive to being offered tea |
| Student | Medium | Tight budget, portion matters, high price sensitivity |
| Weekend Family | Medium | High spend, long table time |
| Long-Haul Driver | Medium | One-off, fast, looking for something filling |
| Fussy Diner | Medium | High satisfaction threshold, high reputation weight |
| Neighbourhood Gathering | Rare | A very large group, announces itself in advance, big volume in a single day |
| Health Inspector | Rare | Inspects cleanliness and order, the result moves reputation hard |
| Old Regular | Rare | Someone who has not come for a long time. Treated well, they turn back into a regular |

---

## How archetypes carry cuisine identity

The same twenty slots fill differently by cuisine, and the frequency distribution changes too.

- **Fast food:** the crowd is young and in a hurry. Average patience low, table time short, turnover high. Two sharp peaks.
- **The Turkish lokanta:** weighted towards regulars. Average patience high, a very hard lunch peak, the evening almost empty.
- **Italian:** they come in the evening and sit for a long time. Table turnover low, spend high.
- **The Japanese ramen shop:** single diners and very fast. Patience low, but table time is very short too.

Changing the menu alone does not give you this. What determines how the day feels to play is the archetype distribution.

---

## Production cost

An archetype is **data, not a model.** Eight parameters and a wardrobe marker. So adding a new archetype is cheap.

But it is not free: making twenty archetypes visually distinguishable is what widened the wardrobe.

| Item | v1 | v2 |
|---|---|---|
| Archetypes, per cuisine | 8 | 20 |
| The shared part of that | 0 | 8 |
| New ones to write per cuisine | 8 | 12 |
| Clothing sets, per cuisine | 12 | **16** |

Raising the clothing sets to sixteen is what makes the archetypes recognisable from the silhouette. The new combination count: 3 × 8 × 6 × 16 × 10, over twenty-three thousand.

---

## The part not yet written

The parameters have no numerical value. "Low patience" is currently just a word.

The following will be written together with the economy work:

1. How much patience is in seconds, and how it drains
2. The price sensitivity formula: how many per cent above market, how far satisfaction falls
3. Converting satisfaction into reputation
4. How many customers reputation brings the next day
5. The hour-by-hour distribution table for twenty archetypes
6. The real probabilities of the frequency tiers

These are the second half of A7 and will be written together with the A4 economy numbers.
