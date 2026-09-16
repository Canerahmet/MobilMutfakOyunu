# 40 — The second language: English

*12 September 2026.* The user's request:

> **"İngilizce de ekle."**
>
> *("add English too")*

The game was single-language, and that is more than a gap: on the Play Store,
Turkish on its own limits the audience the game can reach to one country.
[docs/21](21-business-and-release.md) said in the release plan "the first release
is Turkish, English right after"; it closes here.

---

## 1. One generator, two tables

The English strings live in two separate modules
(`tools/content/languages/loc_en.py`, `tools/content/languages/loc_en_ui.py`)
but they go through **the same generator** (`tools/content/gen_loc.py`). Writing
a separate tool was the easy road and the wrong one: two tables drift apart
quietly, and drift in the text means **a button on screen reading
`[ui.staff.hire]`** — a bug the player sees and I do not.

`compare(tr, en)` insists on three things, and all three produce silent failures:

| what is checked | what happens if it is missing |
|---|---|
| the **key set** is the same | a missing key -> the key in square brackets on screen |
| the **placeholder** set is the same (`{0}`, `{1}`) | text that has `{0}` in Turkish and not in English does **not** raise a formatting error, it silently never prints the number |
| the English text is **not empty** | an empty string, an empty button on screen |

If there is drift the generator exits **without writing** (return code 1).
565 keys x 2 languages.

## 2. The translation policy

The rule is one sentence: **what needs translating is translated, what is a name
stays a name.**

| kind | decision | example |
|---|---|---|
| ingredient | translated in full | `ingredient.kiyma` -> "Minced Meat" |
| a Turkish dish known worldwide | **kept** | `lahmacun`, `doner`, `iskender`, `manti` |
| a Turkish dish whose name is a recipe | translated | `mercimek_corbasi` -> "Lentil Soup" |
| kept but not recognised | name + a short explanation | `karniyarik` -> the name, then "(Stuffed Aubergine)" |
| a regular's name | **kept** | Burak, Nermin |
| a story line | re-voiced, not translated word for word | — |

The strings themselves, as the English table holds them:

> *"Lahmacun", "Döner", "İskender", "Mantı", "Karnıyarık (Stuffed Aubergine)"*
>
> *(a name is a name in every language; the one nobody outside Turkey would
> recognise carries a short gloss in brackets, and only that one)*

41 keys have **the same** text in both languages and every one of them is
deliberate: proper nouns and international words (hamburger, mozzarella,
bulgur). `compare` does not count this as an error — the ones that should be the
same really must be the same.

## 3. The language is a choice

`Loc` now knows both tables. Three decisions:

- **The order is saved, not the code.** `Languages = { "tr", "en" }` and an
  **index** is written to `PlayerPrefs`. A new language is appended at the
  **end**, never inserted — inserting one would also change the choice already
  made on somebody's device.
- **It is not asked on first launch, it is guessed.** If the device language is
  Turkish then Turkish, otherwise English. A wrong guess is fixed from Settings
  with one tap; a screen that asks for a language at startup is an obstacle
  everybody walks through on every install.
- **Every language writes its OWN name.** Somebody looking for a line that says
  "Turkish" already reads English.

> *"Türkçe", "English"*
>
> *(the two entries in the language list, each written in its own language)*

Formatting depends on the language too: `tr-TR` / `en-GB`. The same money is
"8.000 ¤" and "8,000 ¤". If the culture does not change, the numbers are read
wrongly in one of the languages.

## 4. Two real bugs the tour found

The tour now **pins the language to Turkish** (it clicks buttons by their text;
on an English machine it would have stopped at the first step) and tests the
language switch **separately**.

**a. The strip budget overflows in English.** The strip height is now measured
**in both languages** (`CheckStripsAllLanguages`). The evening strip is 199 dp in
Turkish and **230 dp** in English — the budget is 220. A check that measured one
language stayed green. The fix was to shorten the labels ("Walkouts",
"Satisfaction"): the strip is a cramped summary, and the labels have to be short
**in both languages**.

**b. Nobody is visible at the sink.** `Someone was seen washing at the sink (0
frames)`. The simulation had washed 12 plates that day — so the core was working
correctly. The cause: the **owner** was taking the washing-up shift, and **the
owner is not drawn**. Hall server number 0 is the owner; when they wash, the
player sees nothing. The fix is not to give the shift to the owner:

```csharp
bool patron = s == 0 && _hall > 0;
if (!patron && WashNeeded()) { ... }
```

This also completes the claim in [docs/39](39-plate-cycle.md): the bottleneck was
going to be **visible**; work done by a member of staff who is not drawn left the
bottleneck invisible.

## 5. A new check can eat its neighbours

Until English was added, the strip check was a single measurement and it was
cheap. With two languages it **loads the language twice and rebuilds the
interface twice** — and those seconds were running at x16 speed: one real second
is 3% of a service day.

The result was six checks going red at once in the turZ1 run:

```
DIAG hall fill wait:      1.0 s, service 25%, occupied tables 1
DIAG liveliness window:  11.3 s, service 70% -> 80% (DAY CEILING)
FAIL : The service produces occupied tables (0)
FAIL : The customer comes in off the street (0 figures outside)
FAIL : Someone was seen washing at the sink (0 frames)
```

The liveliness window opened at **70% of the day**, that is, it measured a hall
that was emptying out. None of them was a real bug; all of them were the
measurement window opening late.

Two fixes:

1. The strip measurement now runs in a **paused** world (like the camera
   section) — languages and the camera both work in a stopped world, so pausing
   is free.
2. The "second table" wait now has a **day ceiling** (60%). A check that waits
   must not eat the thing it is waiting for.

After the fix the window opens at 10% of the day:

```
DIAG hall fill wait:      0.3 s, service  7%, occupied tables 2
DIAG second table wait:   0.0 s, service 10%, occupied tables 3
DIAG liveliness window:  18.8 s, service 10% -> 34%
```

The rule is the continuation of the measurement lesson in
[docs/39](39-plate-cycle.md): **in a tour the measurements share a common
resource — the service day. Adding a new check can consume what the neighbouring
checks measure.**

## 6. The same rule produced a third bug as well

While hardening the tour, the only remaining red was this:

```
FAIL : If there is an angry customer the crisis strip appeared (1 angry, 0 critical tables)
```

The check's inference was: *if a party left angry at the end of the day, that
party must have dropped into the critical band at some point — so the "LOSING
PATIENCE" strip should have been on screen.* The inference was rotten in two
places at once.

**a. The ones turned away at the door were being counted.** `AngryParties` also
counts a party that could not find a table and turned back; that party **never
sat down**, so it could not show up in a crisis strip that is a list of tables.
On day one two tables were full and the arriving party turned back. The
simulation now also reports `AngrySeatedParties` (left the table angry) and the
inference is built on that alone.

**b. The check was asking the simulation, not the screen.** `CrisisTables` is
computed from the core — "is there a critical table". Whether the strip **was
built on screen** is a separate question, and when it was asked, out it came:

> The bottom bar was **never refreshed during service**. `BuildBottom()` only
> runs when the phase changes, when service ends, or when a button is pressed.
> So a player who touches nothing **never sees** the crisis strip — and the
> strip's own warning sound does not play either (`Sfx.Upset` plays while the
> strip is being built). The sound came when a customer left angry; there was no
> channel that warned **beforehand**.

The game's only urgent warning channel depended on the moments the player was
already touching the screen. The fix is to rebuild the bar inside `Tick`, **when
the number changes** (not on every frame). The measure changed too:
`GameScreen.CrisisBuilds` increments on exactly the line where the strip is
built — that is the thing the player sees.

This third bug is the same sentence as [docs/39](39-plate-cycle.md)'s:
***"the simulation does this" and "the player sees this" are two separate
claims.*** At the sink the owner was washing and was not drawn; here there was a
crisis and it was not drawn. In both cases the core was flawless.

---

## Summary

| | |
|---|---|
| strings | 565 keys x 2 languages |
| drift check | key, placeholder, empty text — all three in the generator |
| default | the device language; changed from Settings, kept in `PlayerPrefs` |
| formatting | `tr-TR` / `en-GB` |
| what the tour found | the strip is 230 dp in English; nobody is visible when the owner washes; the crisis strip is never refreshed during service |
