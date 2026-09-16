# Production Decisions

**Last updated:** 9 September 2026
**Register items:** C1 model production route, C2 characters and animation, C4 audio source, C5 interface and typeface, C8 test plan
**Status:** Written, awaiting decision

This file settles the options left open in [05-production-plan.md](05-production-plan.md).

---

## C1. Model production route

**Decision: a three-layer route, tried in order.**

| Order | Route | What for |
|---|---|---|
| 1 | A procedural Blender script | Furniture, equipment, plates, architectural pieces |
| 2 | Public-domain packs | Organic objects the script does not cover, and prototyping speed |
| 3 | AI generation or a paid pack | Only for individual objects that have to be unique |

### Why this order

Procedural comes first because it is **the only route that guarantees stylistic consistency.** When all the furniture comes out of a single configuration, the same edge bevel, the same scale and the same palette are guaranteed.

Also, because a model is a script and not a file, it goes into version control. If you do not like the style you change the script and regenerate all of it. Redoing forty hand-modelled pieces takes days; changing the script takes minutes.

### Scope

| Generated procedurally | Comes from outside |
|---|---|
| Table, chair, stool, counter | Plants |
| Shelf, cupboard, stove, fridge | Food visuals |
| Plate, glass, pot, tray | Characters |
| Door, window frame, lamp | The sign and individual story props |

### The rule

No asset generated with AI is produced on a **free tier**. The free outputs of those tools are closed to commercial use. Before release, the source and licence of every asset are listed in a table.

---

## C2. Characters and animation

**Decision: one shared body, a standard humanoid skeleton, variety through dress-up.**

### The production line

1. A single humanoid base body is produced.
2. It is prepared to fit a **standard humanoid skeleton**.
3. It is passed through an auto-rigging service.
4. Clips are taken from an animation library: walking, sitting, eating, waiting, talking, cheering, getting angry.
5. Unity's humanoid animation system shares the same clips across all the characters.

### The Mixamo risk and the fallback plan

Mixamo is free and open to commercial use, but Adobe has not updated it for years and support now says it is no longer supported.

**The fallback plan: the models are produced to fit a standard humanoid skeleton.** That way, even if the animation source changes, the models do not. If Mixamo shuts down we move to another auto-rigging service and nothing is done on the model side.

This is the structural decision that removes the dependency on a single service.

### Variety

Not the model, the dress-up. Three body types, eight hairstyles, six skin tones, sixteen outfits per cuisine, accessories. More than twenty-three thousand combinations.

Detail: [10-cuisine-identity.md](10-cuisine-identity.md).

---

## C4. Audio source

**Decision: paid AI tools, with public domain as the fallback.**

| Item | Source |
|---|---|
| Sound effects | A paid AI tool. The paid plans are royalty-free and open to commercial use |
| Music | A paid AI tool |
| Fallback | Public-domain sound libraries |
| Character syllables | AI, or your own recording |

### Reasoning

Audio production is the cheapest item in this project. A monthly subscription is enough to produce the sound for the whole game. Collecting from public-domain libraries is free, but building a consistent palette that way is far harder.

### Warning

The terms of the tool used, **as valid that month**, will be verified before release. The licence language of these tools changes fast. Which tool and which plan produced every sound will be recorded.

---

## C5. Interface, icons and typeface

### Typeface

**Decision: a family on Google Fonts licensed under the SIL Open Font License.**

The mandatory checklist — every one of these letters must have a glyph:

> | Character | Why |
> |---|---|
> | ı and İ | The Turkish dotless i and dotted capital I. The most commonly missed bug |
> | ğ Ğ | |
> | ş Ş | |
> | ç Ç, ö Ö, ü Ü | |

**The test method:** the sentence below is checked at every text size. If even one character is missing, the typeface is cut.

> *"İstanbul'da çiğ köfte ve şalgam"*
>
> *("raw köfte and turnip juice in Istanbul" — the sentence is chosen because
> it contains every Turkish-only letter at once.)*

Monospaced digit support is also essential, because the number must not jump when the till value changes.

### Icons

- Public-domain interface icon sets are taken as the base.
- The money icon is drawn specially; it is decided: a flat stack of coins.
- Every icon is tested at 16, 24 and 48 pixels. An icon that does not read at the smallest size is not used.

### Interface layout

Code, so it is mine to do. Detail in [16-screens-and-tutorial.md](16-screens-and-tutorial.md).

---

## C8. Test plan

### Three layers

| Layer | What it tests | Who |
|---|---|---|
| Unit tests | The core rules | Automatic, on every build |
| The balance tool | Economy and progression | Automatic, nightly |
| Playability | Fun and comprehension | Human |

### Unit tests

Because the core is pure C#, it runs on normal .NET test infrastructure; Unity is not needed.

Areas covered: economy calculations, the satisfaction and reputation formulas, patience drain, the capacity calculation, bankruptcy ladder transitions, save migration, content validation.

**The rule: a test is written first for every bug found.**

### The balance tool

Written in Phase 0. It simulates hundreds of games with different player strategies and gives the result as a table.

The eight questions it has to answer are in [12-economy.md](12-economy.md).

### Playability testing

| Stage | People | What is measured |
|---|---|---|
| Vertical slice | 5 people | Is it understood |
| After the content | 10-15 people | Is anyone bored on the tenth day |
| Soft launch | Real players | Day one and day seven retention |

**The most critical question: is anyone bored on the tenth day?** The most common cause of death in the research was the mid-game plateau. Content production does not continue until that question is answered.

### The device matrix

| Class | At least |
|---|---|
| High end | One current phone |
| Minimum | One phone that meets the minimum requirements |
| iOS | **Not in the first release.** There is no Mac, so it cannot be built. It is added once there is a Mac |

No performance testing is done on an emulator. Real devices only.

---

## Details awaiting a decision

1. Should the procedural route stay primary, or should we start with a ready-made pack and move over later
2. Which tool for audio, and what the monthly budget is
3. Where the people for the playability testing will be found
4. ~~Is an iOS test device essential in the first release~~ Closed: the first release is Android only. See [22-answers-and-direction.md](22-answers-and-direction.md)
