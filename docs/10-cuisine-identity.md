# Cuisine Identity: Character and Environment Differentiation

**Last updated:** 9 September 2026
**Register item:** connected to A5 and A13, a new pillar
**Status:** Proposal ready, awaiting decision. The fourth cuisine was settled as Japanese on 9 September 2026.

---

## The aim

When the cuisine changes the player should **feel they have walked into somewhere else.** Not just the menu: the venue, the people, the light, the sound and the rhythm of the day should change.

This is the right goal and it feeds the purchase decision directly. If buying a cuisine feels like a colour change, nobody buys the second one. If the environment really changes, every cuisine becomes a separate game.

---

## A correction to the approach: differentiation does not happen in the face

Separating characters ethnically by facial features is the wrong tool in this game. There are two reasons.

**The first is technical and decisive.** On a fixed forty-degree camera and a phone screen a character appears roughly 60 to 120 pixels tall. An eye is two or three pixels at that scale. Facial features simply do not read. Even if we tried to differentiate this way it would not work visually; it would only show in close-up dialogue portraits.

**The second is commercial.** Depiction that slides into ethnic caricature can catch on store content policies and carries a review-bombing risk. In a game aiming at a global release this is a mistake that is very expensive to fix afterwards.

**The good news:** the tools that really do read at that scale are far stronger anyway. Silhouette, clothing, colour, venue, light and rhythm. The system below is built on those.

---

## What actually reads

The things a player can tell apart on a distant camera and a small screen, in order of importance:

1. **The shell of the venue.** Walls, floor, ceiling, furniture layout. This covers most of the screen.
2. **Light and palette.** Bright fluorescent, warm afternoon light, or an evening lantern.
3. **Silhouette.** A hard hat, a backpack, a cap, a bag, an apron. These are what is recognised from a distance.
4. **Clothing colour and pattern.**
5. **The rhythm of the crowd.** When it fills up, when it empties.
6. **Sound.** A fryer, a tea glass, or a boiling soup cauldron.

The face is outside this list. The face only does its work in dialogue portraits, and that is already the territory of the hand-designed named characters.

---

## The character appearance system

A dress-up system built on a single shared body and skeleton. In AI-assisted production it keeps the advantage of one model staying consistent from every angle.

| Variable | Count | Scope |
|---|---|---|
| Body and skeleton | 1 | Shared |
| Body type | 3 | Shared |
| Hair model | 8 | Shared |
| Skin tone | 6 | Shared, **all of them used in every cuisine** |
| Clothing set | 16 | **Cuisine-specific.** So that twenty archetypes are recognisable by silhouette |
| Accessory | 10 shared + cuisine-specific | Hard hat, bag, cap, glasses, apron |

**Combination count:** 3 × 8 × 6 × 16 × 10, over twenty-three thousand different appearances. A crowded and varied restaurant comes out of a small asset set.

**Why skin tone is the full range in every cuisine:** in real restaurants the customers are mixed. Reducing each cuisine to a single population would be both unrealistic and an open door to the caricature risk above. The variety comes free, because the same six tones are already produced.

---

## The identity of the four cuisines

| | Fast food | Turkish cuisine | Italian | Japanese cuisine |
|---|---|---|---|---|
| **Architectural shell** | Plastic seating groups, counter ordering, tiled floor, a neon menu board, a glass front | Wooden chairs, tablecloths, a steaming hot counter at the entrance, a calendar on the wall, a television in the corner, a tea stove | White tablecloths, a wine rack, a brick wall, pendant lighting, a wooden bar | Wooden slat walls, paper lanterns, a noren curtain at the entrance, counter seating, boiling soup cauldrons and a noodle-blanching section in the open kitchen |
| **Palette** | High saturation, red and yellow, white floor | Earth tones, brass, dark wood, cream | Deep green, cream, wine red | Dark wood, a red lantern accent, cool shadows |
| **Light** | Bright fluorescent, few shadows, midday | Warm afternoon, light slanting through the window | Candle warmth, evening, strong contrast | Evening, lantern accents, light passing through the steam rising off the cauldrons |
| **Staff clothing** | A cap, a coloured apron, a t-shirt | A white jacket or shirt, a long apron | A waistcoat, a tie, a long apron | A short jacket, a bandana |
| **Customer silhouettes** | A student's backpack, an office worker, a courier bag, a family | A tradesman's apron, a construction hard hat, a civil servant's jacket, a pensioner's cap | A smart coat, a handbag, couples | People sitting alone at the counter, students, a lunch-break bag |
| **Daily rhythm** | Two sharp peaks, lunch and evening | A very strong lunch peak, a quiet evening | A quiet lunch, a long evening | A strong lunch peak, a medium evening |
| **Ambient sound** | The sizzle of the fryer, till beeps, pop music | The clink of tea glasses, the sound of a ladle, a radio | Cutlery, glasses, light acoustic | The sound of noodles being drained, the cauldron bubbling, steam, short orders |

This table is the list of what the player will notice when they change cuisine. None of it has anything to do with the face, and all of it reads on a distant camera.

---

## The character count: how many people are there

The answer to "apart from the ten named characters, how many characters are there" is layered. The characters fall into three groups.

### 1. Hand-designed characters

These are written and designed one by one. They have a name, a face, a job and a story.

| Group | Per cuisine | Note |
|---|---|---|
| Named regulars | 10 | Each with a 3-4 scene story |
| Named staff | 3 | **A new proposal.** The reasoning is below |
| **Total per cuisine** | **13** | |

| Scope | Hand-designed characters |
|---|---|
| At launch, two cuisines | 26 |
| Plus the player themselves | 27 |
| When all four cuisines are done | 53 |

**The named staff proposal:** in the research, the most praised side of Tavern Keeper was that the staff had character. Staff are the people the player sees every day and bonds with. Hand-designing three of them per cuisine gives a big emotional return for a small cost. The rest of the staff are generated.

### 2. Behaviour templates

| Group | Per cuisine |
|---|---|
| Customer archetype | 20, eight of them shared |
| Staff role | 4, shared |
| Staff trait | 12, shared |

An archetype is a pattern, not a person. It determines patience, spend, group size and arrival time. The appearance comes from the dress-up system.

### 3. The generated crowd

The number is not limited. The dress-up system produces more than seventeen thousand different appearances per cuisine, so in practice the player never sees the same person twice.

| Measure | Value |
|---|---|
| On screen at once | 6 to 20 characters, depending on the table count |
| Total customer visits in a campaign | Roughly 2,300 |
| Staff that can work at the same time | 1-2 at the start, 6-8 at the end |
| Candidates shown on the hiring screen | 3, refreshed |
| Candidates seen across the campaign | Roughly 25-30 |

Generated staff also get a name, two traits and an appearance. So they are not nameless; they just have no hand-written story.

---

## Specificity lives in a person, not in a group

Named regulars are the right place for cultural specificity.

**Why:** depiction that generalises a group is both a cliché and risky. But a single person with a name, a job, a habit and a problem is both safe and far more memorable.

In Turkish cuisine, the owner of the shop across the street: a tradesman who comes at the same time every day, sits at the same table, likes kuru fasulye, and gets written into the tab book when money is tight at the end of the month. This person is a character. "The Turkish customer" is a category, and interests nobody.

The same principle holds for all four cuisines. Each of the ten named characters is hand-designed; the rest of the crowd is generated by the dress-up system above.

---

## Closed: the fourth cuisine became Japanese

"The Far East" was not a cuisine, it was a piece of a continent. Japanese, Chinese, Korean and Thai cuisines are completely different in architecture, food and service ritual.

**Decision: Japanese cuisine, as a ramen shop.** This choice brings concrete architecture, a concrete dish list and a concrete service rhythm. Counter seating, an open kitchen, a weighting towards single customers and very fast turnover all follow naturally from it.

The signature mechanic sharpened with this choice too: **broth and running out.** In real ramen shops, when the broth is gone the shop closes.

The same logic applies to Italian, and there "trattoria" is already a specific enough frame.

---

## Production cost

| Item | New work per cuisine |
|---|---|
| Architectural shell and decor | 1 set |
| Clothing | 16 pieces |
| Accessories | A few cuisine-specific pieces |
| Palette and light setup | 1 profile |
| Ambient sound | 3-4 layers |
| Named character design | 13 people: 10 customers, 3 staff |

The body, the skeleton, the animation, the hair and the skin tones come from the shared base. So the extra character cost per cuisine is not model work, it is **clothing and design.** This is what keeps the four-cuisine idea standing up in one-person production.

---

## Details awaiting a decision

1. Are sixteen clothing sets per cuisine enough
2. Should the ambient sound layers stay at this scope
3. Is the hand-designed staff character proposal accepted
