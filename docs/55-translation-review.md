# 55 — Translation review: the cast had split in two, the Chinese screen was broken

*16 September 2026.* The user's request:

> *"Agentlar oluşturup çevirilerin doğru olup olmadığını kontrol et. Ayrıca mağaza
> sayfası metinlerini de diğer diller için oluştur. Proje klasör yapısını ona göre
> düzenli ve anlaşılır hale getir."*
>
> *("Create agents and check whether the translations are correct. Also produce
> the store page texts for the other languages. And make the project folder
> structure tidy and understandable accordingly.")*

Four agents ran — Spanish, Chinese, Arabic and **English** (the default language
and the source the other three were translated from: a mistake there is a
mistake copied four times). Each of them walked all 635 keys one by one.

**All four reports pointed at the same place, and they were right.** What this
document is about is less what they found than *how* they found it: all four
produced a claim that could be measured, and I put each one to the code or to
the typeface.

---

## 1. The regulars' cast had split in two

While adding three languages in [54](54-five-languages.md), I did not **translate**
the regulars' occupation lines and story beats — **I made them up again**.
Thirteen of the twenty regulars became somebody else in es/zh/ar:

| person | archetype in the content | tr/en | es/zh/ar (wrong) |
|---|---|---|---|
| Rasim Amca | `insaat_iscisi` | building-site foreman | taxi driver |
| Tolga | `gece_vardiyasi` | night-shift security guard | fitness instructor |
| Sevda | `diyet_yapan` (favourite: yeşil salata) | dietitian | nurse |
| Ozan | `mac_grubu` (4-6 people, evening) | amateur footballer | intern |

This is not a difference of style: `Simulation.BindRegularsToPlan()` binds a
regular to a group that arrives with `archetypeBase`. Ozan's Chinese beat said
*"he is the first in at the start of lunch, always alone"*; the game brings him
**in the evening, with a party of four to six**. The text was being contradicted
in front of the player's eyes.

All twenty regulars in each of the three tables were rewritten against the tr/en
cast.

**There is no check that would catch this, and I do not want to invent one.** The
key set was correct, the placeholders were correct, there was no number to fail.
"Does this line describe this person?" is a question only a reader asks — and on
this pass the agents asked it.

---

## 2. The Chinese build was broken in three places while the font check was green

`check_font.py` said *"everything is covered"*. It could say so because it was
asking the wrong question: **"which characters are in the Chinese table"**. But
`UiRoot.FontForLanguage` draws **the whole tree** with Noto Sans SC when the
language is Chinese — not only the Chinese text.

It was measured, and all three were real:

1. **Staff names.** `content/names.json` is not in the localisation table.
   **Sixteen** of ninety-six names (Ayşe, İbrahim, Yağmur, Sıla…) carry letters
   that are not in the subset. Someone playing in Chinese saw one in every six
   staff members as `Ay□e`.
2. **Symbols embedded in the code.** The minus sign in the evening report,
   `U+2212`, and the menu bullet, `U+2022`, are directly inside C#. They are in
   no table and they were not in the subset either — **every expense line** came
   out as `□1,200 ¤`.
3. **The Arabic button in the language picker.** In the Chinese interface,
   `العربية` was seven empty boxes. Someone who reads Arabic could not find his
   own language.

### The fix for the three is not in the same place

`ğ İ ı Ş ş` **are not in the source font either** (measured: Noto Sans SC carries
30,890 code points, and no Latin Extended-A). So they cannot be fixed by adding
them to the subset. `Loc.PersonName` was written: when the language is Chinese it
folds those five letters — the same rule the Chinese content table already
applies to proper nouns.

`U+2212` and `U+2022` **did** go into the subset. Arabic cannot (the font does
not carry it), so the button is given Rubik locally — the same fix as on the
Chinese button.

### The subset is now a tool

`tools/art/subset_font.py` was written. The first subset had been extracted by
hand and **nowhere did it say which characters were wanted** — that was the cause
of the three gaps. The character set is defined in one place
(`check_font.cjk_characters()`): the tool generates from it, the checker looks
for it. With two lists they would have drifted apart, and drift again means
empty boxes.

The check went red on its first run — a new character brought in by my Chinese
fixes (`齐`) was not in the subset.

---

## 3. Seven strings that contradicted the simulation

All of them were verified by asking the code:

| key | what it said | what the code does |
|---|---|---|
| `badge.short_peak` | "one short" (exactly one person missing) | `_cooks < required.Cooks \|\| _hall < required.Hall` — **any** shortfall |
| `ui.morning.days_keep` | "days left" (stock remaining) | `KeepDays` = **shelf life** |
| `ui.menu.subtitle` | "Everything on the menu is kept in stock" (a reassurance) | the Turkish warns: a wide menu ties up money |
| `ui.service.target_auto` | "worst" | `MostImpatientParty()` |
| `notice.plates_out_busy` | "the dishwasher" | there is **no** dishwasher to hire |
| `ui.staff.fire_confirm` | "experience resets" | `Fire()` **deletes** the record |
| `notice.tenure_zincir` | "ask **for** {0}" | the Turkish "ona soruyor" = they **consult** them |

`days_keep` was the most expensive: a player reading *"3 days left"* understands
"I have three days of stock" and **does not buy**; the string means "it keeps for
three days", that is, *do* buy. It is the only line that sells the cold store.

### The inherited waiter said "the cook you inherited"

`TraitText` writes `ui.staff.inherited` for a person who has **no** trait. The
inherited hall staff member has no trait either (`Simulation.cs:800`,
`_hallTraitA[0] = -1`). So the waiter's card read *"Huysuz — devraldığın aşçı"*
(*"Bad-tempered — the cook you inherited"*), in five languages at once. A
separate key was added (`ui.staff.inherited_hall`).

### The intervention hint was wired to the wrong cable

`Hints.cs` was reading `InterventionsPerDay` (the base, 4); the daily allowance
is `InterventionsToday` = base + the table increase. The hint was right on day 1
and, after the first expansion, told the player his allowance was **smaller than
it was**. The text was right, the wiring was wrong.

---

## 4. The language-specific ones

**Spanish — a language that inflects, where English and Turkish do not.** Half
the station names are feminine (`Parrilla`, `Bebidas`) and half the staff names
are women's. `{0} mejorado` was printing "Parrilla **mejorado**". Seven strings
were rewritten into a form that needs no agreement (`Mejora: {0} — nivel {1}`).
Also `Dejar ir` = *let them go*; on a red button it could read as "let him go
home" → `Despedir`.

**Arabic — number-noun agreement.** Arabic wants a separate inflection for 1 /
2 / 3-10 / 11-99 / 100+, while the code drops in a bare integer. **Sixteen
strings** were correct in only one number band: `{0} أيام` was printing
"1 أيام". All of them were rewritten into the partitive form, which is correct
for every number (`{0} من الأيام`). Also, pairs such as `{0} / {1}` with only a
neutral character between them get mirrored by the bidirectional algorithm — a
strong Arabic word was put in between.

**Chinese — 厨房 means a kitchen *room*, not a *type* of cuisine.** Under the
question *"你先开哪种厨房？"* ("which kitchen room are you opening") stood the
`快餐` and `土耳其餐馆` cards; the question did not match its own answers. Also,
`毛利` was labelling both a sum of money and a percentage, and the two **ratio**
labels on the ledger screen were written in the imperative, which made them look
like copies of the button next to them.

---

## 5. The generator could not count a placeholder

`compare()` was comparing placeholders as a **set**, so `"{0} ... {0}"` was the
same as `"{0}"`. The English tenure notice was printing the staff member's name
twice and the gate said nothing. It is counted now: verified by mutation in both
directions.

```
placeholder differs: notice.tenure_zincir  reference={0} {1}  here={0}x2 {1}
```

---

## 6. Store texts in five languages

The short description, the full description and the privacy policy were written
for Spanish, Chinese and Arabic ([44](44-store-texts.md)). The character counts
**had been written by hand and two of them were wrong** (it said 60, it was 61).
`tools/content/check_store_texts.py` now measures: short description ≤ 80, full
description ≤ 4000, and whether each language has both.

| language | short | full |
|---|---:|---:|
| TR | 61 | 1,609 |
| EN | 68 | 1,707 |
| ES | 66 | 1,850 |
| ZH | 21 | 599 |
| AR | 57 | 1,446 |

---

## 7. The folder layout

Five languages put two files per language into `tools/content/` — ten files
among six generators and two checkers. `tools/content/languages/` was created;
the naming pattern (`loc_<language>[_ui].py`) stayed the same.

A **"what lives where in the repository"** map was added to the root `README.md`,
and a topical entry point to `docs/README.md` — fifty-two numbered documents are
a chronological log and that order must not be broken, but it was not navigable
either.

I did **not try** moving the three checkers (`audit_content.py`,
`check_licenses.py`, `check_urp.py`) into topic folders: it would have staled
fifteen document references for no gain.

### Instead, a check that makes moving safe

`tools/check_docs.py`: does every relative link point at a file that exists, and
is every numbered document mentioned in the index? On its first run it found
**nine broken links** — one was a wrong number I had written in
[54](54-five-languages.md), and the other eight had broken when the documents
were renumbered and had been like that for months.

`docs/README.md` was also contradicting itself: the register at the bottom said
*"all thirty-seven items are closed"* while thirteen rows of the table still said
"Open".

---

## What is not closed

- **The SHOULD FIX sections of the reports were only partly applied.** Every item
  that changed meaning or contradicted the simulation went in; some of the
  stylistic suggestions did not. All four left complete lists.
- **Still no language has been read by a native speaker.** An agent review does
  not replace that — but it left a far better starting point than a blank page.
- **Three dead keys** — `role.bulasikci`,
  `role.kasiyer`
  and `ui.hud.angry`: they are maintained in five tables and drawn nowhere.
  `loc_tarama.py` looks for dead `ui.*` keys but does not look at the `role.*`
  family.
