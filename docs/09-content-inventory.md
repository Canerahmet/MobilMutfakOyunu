# Content Inventory and Progression Curve

**Last updated:** 9 September 2026, v2
**Register items:** A5 content inventory, A6 progression curve
**Status:** Proposal ready, awaiting decision

**9 September 2026:** 32 is settled. The condition shared by the designer review and the scope review is binding: **every dish carries four parameters** (`prepMs`, `station`, `complexity`, `ingredients[].grams`), see [23-core-contract.md](23-core-contract.md) §8.3. The art cost is held constant by modular plating: 32 dishes, 14 meshes, see [24-art-pipeline.md](24-art-pipeline.md).

**The v2 change:** the dish count per cuisine was raised from 20 to 32. Ingredient, archetype and regular counts were resized accordingly. Character and environment differentiation was moved into a separate file: [10-cuisine-identity.md](10-cuisine-identity.md).

---

## The sizing principle

1. **The daily menu decision has to be meaningful.** Every day you put 4 to 8 dishes on the menu. The wider the pool, the more meaningful the choice. Thirty-two dishes is four times the menu's capacity.
2. **One person has to be able to carry the production.** Because dish models are low-poly and can be generated procedurally, this increase is bearable. The real cost is not the model, it is the balance.

**A critical design decision: a shared base.** Some of the ingredients, the staff and the business upgrades are common to every cuisine. The cost of the second cuisine is not a set from scratch, only the difference.

---

## The inventory in numbers

### Per cuisine

| Item | v1 | v2 | Note |
|---|---|---|---|
| Dishes | 20 | **32** | Opened gradually across the campaign |
| Cuisine-specific ingredients | 18 | **26** | On top of the shared pantry |
| Customer archetypes | 6 | **20** | 8 shared, 12 cuisine-specific |
| Named regulars | 8 | **10** | Each with a 3-4 scene story |
| Cuisine-specific equipment | 8 | **10** | Cooking stations |
| Wardrobe sets | — | **16** | Cuisine-specific clothing, new |
| Decor and architectural shell | 1 | **1** | The cuisine's visual identity |

### The shared base, produced once

| Item | Count |
|---|---|
| Basic pantry ingredients | 12 |
| Staff roles | 4 |
| Staff traits | 12 |
| Business upgrades | 10 |
| Furniture and venue pieces | 24 |
| Venue expansion steps | 4 |
| Character body and skeleton | 1 |
| Hair models | 8 |
| Skin tones | 6 |
| Accessories | 10 |

**The basic pantry:** salt, black pepper, olive oil, sunflower oil, flour, onion, garlic, tomato, sugar, milk, egg, butter.

---

## The progression curve: four seasons

The campaign is 60 days, 15 days per season. Every system opens within the first three seasons.

| Season | Days | System that opens | Dishes |
|---|---|---|---|
| **1. Learning** | 1-15 | Menu, price, service. The first staff member. The basic market | 6 → 13 |
| **2. Crew** | 16-30 | Station layout, staff traits, supplier relationship. The first expansion. **The signature mechanic** | 13 → 21 |
| **3. Growth** | 31-45 | Layout editing, reputation, the critic's visit. The signature mechanic deepens | 21 → 27 |
| **4. Mastery** | 46-60 | A rival restaurant, the last expansion. No new system arrives | 27 → 32 |

**Tempo:** a new dish every two days on average, one big system per season.

**The signature mechanic arrives at the start of the second season.** Putting it in the first season makes the tutorial load far too heavy, because the player is already learning the menu and prices.

---

## The fast food menu, 32 dishes

Built as mains, sides, drinks and desserts for the combo mechanic.

| Group | Count | Dishes |
|---|---|---|
| Main | 12 | Hamburger, Cheeseburger, Double Burger, Spicy Burger, Chicken Burger, Crispy Chicken, Fish Burger, Veggie Burger, Hot Dog, Chicken Wrap, Beef Wrap, Cheese Toastie |
| Side | 8 | Fries, Spicy Fries, Onion Rings, Nuggets, Hot Wings, Mozzarella Sticks, Green Salad, Coleslaw |
| Drink | 6 | Fizzy Drink, Cola, Lemonade, Milkshake, Ayran, Iced Tea |
| Dessert | 6 | Ice Cream, Apple Pie, Chocolate Cake, Donut, Brownie, Waffle |

**The opening menu:** Hamburger, Fries, Fizzy Drink, Hot Dog, Nuggets, Ice Cream.

**The combo logic:** a main plus a side plus a drink makes a combo. The combo price is lower than the sum of the parts but it raises the average ticket. In return the kitchen's load increases.

---

## The Turkish cuisine menu, 32 dishes

Built on the logic of a tradesman's lokanta and the dish-of-the-day rotation.

| Group | Count | Dishes |
|---|---|---|
| Pot dish | 12 | Kuru Fasulye (bean stew), Chickpea Stew, Meat & Vegetable Stew, Karniyarik (stuffed aubergine), Imambayildi (braised aubergine), Green Bean Stew, Moussaka, Aubergine Kebab, Okra with Lamb, Spinach with Mince, Barbunya (borlotti bean stew), Stuffed Courgette |
| Soup | 4 | Lentil, Ezogelin, Yayla, Tripe |
| Pilaf and pastry | 5 | Rice Pilaf, Bulgur Pilaf, Baked Pasta, Manti, Borek |
| Grill | 5 | Kofte (meatballs), Chicken Shish, Lamb Chops, Adana, Chicken Wings |
| Meze and salad | 3 | Shepherd's Salad, Cacik, Piyaz |
| Dessert | 3 | Rice Pudding, Kadayif, Revani |

**The opening menu:** Kuru Fasulye, Rice Pilaf, Lentil Soup, Shepherd's Salad, Kofte, Rice Pudding.

**The dish of the day:** every day one of the pot dishes becomes the dish of the day and is sold at a discount. Regulars wait for it. Putting the same dish up two days running lowers loyalty.

**Tea:** not an item sold on the menu, a service decision. Offering it creates an ingredient cost but raises loyalty and the tab repayment rate.

---

## The customer structure

### Twenty archetypes, per cuisine

An archetype is a behaviour pattern, not a person. Eight are shared across every cuisine, twelve are cuisine-specific. They split into three frequency tiers: frequent, medium, rare. For the full list see [11-customer-system.md](11-customer-system.md).

### Ten named regulars, per cuisine

Each has a name, a face, a job and a three to four scene story. It opens up as you serve them well enough.

In the research this came out as one of the strongest retention tools on mobile. Regulars also feed one axis of the end-of-year evaluation, and in Turkish cuisine they are the people you open a tab for.

---

## Staff

| Role | Job |
|---|---|
| Cook | The cooking station |
| Waiter | Taking orders and serving |
| Cashier | Payment |
| Dishwasher | Cleaning and the plate cycle |

Each staff member draws two of **twelve traits**: fast but messy, slow but meticulous, panics in a crowd, gets on well with customers, tires quickly, raises the crew's morale, and the like.

The uniform changes with the cuisine. See [10-cuisine-identity.md](10-cuisine-identity.md).

---

## Venue expansion

| Tier | Tables | Opens |
|---|---|---|
| Starting | 4 | Day 1 |
| Second | 7 | Season 2 |
| Third | 10 | Season 3 |
| Fourth | 14 | Season 4 |

Every expansion also raises the rent. The reason Tavern Master's economy collapsed was that growth did not raise fixed costs.

---

## Details awaiting a decision

1. Are thirty-two dishes enough, or should it go higher still
2. Is ten named regulars reasonable as a story-writing load
3. Should venue expansion stay at four tiers
4. Are the dishes on the two menus approved

---

## Next step

The next job is **the economy numbers and the customer formulas.** Ingredient costs, sale prices and preparation times for thirty-two dishes; wages, rent, starting capital and patience formulas. The balance tool can only run once those are written.
