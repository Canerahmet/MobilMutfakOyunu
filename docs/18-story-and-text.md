# Story and Text

**Last updated:** 9 September 2026
**Register item:** A13
**Status:** Written, awaiting decision

---

## Why it matters

In the research, one of the strongest retention tools on mobile turned out to be regulars with character. Players said this about Hungry Hearts Diner: **"Most people come for the food but stay for the stories."**

But the same research also gave a warning. Cat Cafe Manager's story was very short and was criticised as "two days of content". Little story is not worse than no story, but raising an expectation and not meeting it is.

---

## Five sources of text

| Source | How it is written | Volume |
|---|---|---|
| Regulars' arcs | By hand | High |
| Named staff scenes | By hand | Medium |
| Customer reviews | A template plus variables | Low but very visible |
| The critic's piece | By hand, branching on the score | Medium |
| Event texts | By hand | Low |

---

## Regulars' arcs

Ten people per cuisine, three to four scenes per person.

**The condition for a scene opening depends on two things:** how many times they have come and their average satisfaction. So the story opens not just with time but by **serving them well**.

```
Scene 1:  3 visits,  70 satisfaction
Scene 2:  8 visits,  75 satisfaction
Scene 3: 15 visits,  80 satisfaction
Scene 4: 25 visits,  85 satisfaction
```

The fourth scene is optional; only some characters have one.

**A scene is three to five sentences long.** Nobody reads long text on mobile. Short and frequent beats long and rare.

### Tone

- Warm, but no emotional exploitation.
- There is humour but no mockery. We laugh with the characters, not at them.
- Hungry Hearts Diner's warmth is the target, but with less melodrama.
- Nobody tells you their tragedy in the first scene. Trust is built over time.

### The connection to Turkish cuisine

The people you open a tab for are these ten. So the story and the mechanic meet in the same characters. Somebody who does not pay their debt is not just a number, it is somebody whose story you know.

This is the game's strongest design intersection. The mechanic and the narrative do not sit in separate places.

---

## Customer reviews

A few short reviews appear on the accounts screen every day. These are not written by hand; they are generated from **a template plus variables**.

```
"The {dish} was good but {complaint}."
"I waited {duration}. {comment}"
"{price_comment} Still, {positive}."
```

The variables come from the satisfaction components:

| Component | The phrase it triggers |
|---|---|
| A long wait | "I waited a long time", "The service was slow" |
| A high price | "It came out a bit salty", "The prices have gone up" |
| Low quality | "The ingredients were old", "It did not taste the way it should" |
| A dish that ran out | "They did not have what I wanted" |
| Something offered free | "They offered tea, that was kind" |
| High satisfaction | "I will come again", "The best on the street" |

> *"Biraz tuzlu geldi"*
>
> *("it came out a bit salty" — in Turkish a bill that is "salty" means an
> expensive one, so this line reads as a complaint about the price, not the
> seasoning; the English table above keeps both readings side by side.)*

**Roughly 120 template fragments** are enough. The number of combinations produces thousands of reviews, and every one of them reflects that day's real data.

This is the way to build a reactive system without hand-writing it. When the player reads a review they say "yes, I really was late today".

---

## The critic's piece

The text of the end-of-year evaluation. Separate per cuisine, five tiers by score.

**The piece refers to your game.** It does not just say "it was good"; it touches on:

- Your highest-covers day
- How many regulars you won
- Whether or not you fell onto the bankruptcy ladder
- How you did on the cuisine-specific axis

So the summary of thirty hours of play turns into a single newspaper piece. That is what creates the emotional counterpart of the plaque.

Roughly 150 words per tier. Five tiers, four cuisines.

---

## Event texts

- The five rungs of the bankruptcy ladder, the landlord's messages
- The entrances of the rare archetypes: the food critic, the health inspector, the bulk order
- A staff resignation, a raise demand
- Cuisine opening and closing texts

Short, functional, with character. Each one is a sentence or two.

---

## The volume budget

| Item | Words | Scope |
|---|---|---|
| Regulars' arcs | ~2,100 | Per cuisine |
| Named staff scenes | ~450 | Per cuisine |
| Customer review templates | ~1,800 | Shared |
| Critic's pieces | ~1,500 | The two cuisines at launch |
| Event texts | ~800 | Shared |
| Interface and tutorial | ~1,200 | Shared |
| **Total at launch** | **~10,400** | Two cuisines |
| **In two languages** | **~20,800** | Turkish and English |

Twenty thousand words is a quarter of an average novel. Bearable for a one-person project, especially considering that the reviews are generated from templates.

Every extra cuisine brings roughly 2,550 words, plus the critic's piece.

---

## Localisation decisions

| Topic | Decision |
|---|---|
| Languages | Turkish and English |
| Currency | Nameless, no translation needed |
| Character names | Not translated. Hasan Usta is Hasan Usta in every language |
| Turkish dish names | **Not translated, explained** |

**The decision on dish names matters.** In the English version the name "Kuru Fasulye" is kept, with "white bean stew" in small type underneath. That is both accurate and full of character. Writing "White Bean Stew" makes the dish generic and erases the cuisine's identity.

The same principle will hold for Italian and Japanese cuisine. Ramen stays ramen.

---

## Details awaiting a decision

1. Should it be four scenes or three per person
2. Are 120 review templates enough
3. Should the critic's piece have five tiers
4. Should a third language be added, and which one
