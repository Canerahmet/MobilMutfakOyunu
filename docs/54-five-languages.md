# 54 — Five languages: English, Spanish, Chinese, Arabic

*16 September 2026.* The user's request:

> *"Oyuna İngilizce, İspanyolca, Çince ve Arapça ekle, seçeneklerden dil
> değiştirilebilsin. Ona göre de kullanılan kelimeler vs. ortak anlaşılır olsun.
> Default olarak oyun İngilizce başlasın."*
>
> *("Add English, Spanish, Chinese and Arabic to the game; let the language be
> changed from the options. And make the words used, and so on, commonly
> understood accordingly. By default let the game start in English.")*

The game was bilingual ([40](40-two-languages.md)). It became five. **The real work was not the
translation** — translation was something the generator had already solved. The
real work was the two writing systems themselves: Chinese wants thousands of
glyphs, Arabic joins its letters up and flows right to left. Both are outside
the job of "translate the text".

---

## 1. Five tables, one generator

`tools/content/gen_loc.py` extracts the keys **by scanning the content**: there
is no hand-maintained key list. `build_en()`, written when the second language
was added, was generalised (`build_from(mod)`) and three languages went through
the same gate:

| file | language | note |
|---|---|---|
| `loc_es.py` + `loc_es_ui.py` | Spanish | **neutral** Spanish; no *voseo*, singular "tú" |
| `loc_zh.py` + `loc_zh_ui.py` | Simplified Chinese | mainland usage, full-width punctuation |
| `loc_ar.py` + `loc_ar_ui.py` | Modern Standard Arabic | no regional dialect |

Every language goes through **the same drift gate**: if one language's table
carries a different key set from another's, the generator fails. The result:
`637 keys × 5 languages`, "all strings complete.".

The request that it be "commonly understood" came down to word choice. The most
concrete example: the waiter role is *camarero* in Spain and *mesero* in Latin
America. Both are understood in either region; **mesero** was chosen as the natural one
for the wider audience, and the reasoning sits at the top of the file.

---

## 2. English by default — and a check that measures it

The device's language used to be guessed (`Application.systemLanguage`). The
user's decision deleted that: the game **opens in English on every device**, and
someone who is going to play in Turkish picks it once in Settings and the choice
is saved.

That decision lives in `Loc.Preferred()` as **a single `return 1;` line**, and
above it sits the place where the device-guessing code I deleted used to be. It
could come back by accident — and when it does, nothing will fail: *on the
developer's own Turkish phone it looks right.*

The tour now runs the real path: it deletes the saved preference, calls the
preference logic again, looks at the language that comes out, and then puts the
save back exactly as it was.

```
ok   : A device with nothing saved opens in English (en)
```

---

## 3. The tour itself was changing the player's language

The strip budget was measured in two languages, and the measurement called
`Loc.SetLanguage()` — that is, the gate that **writes to disk**. The tour was
touring five languages and saving the last one as the player's setting.

`Loc.UseLanguage()` was added: it applies, it does not save. **Trying and
choosing are different things; now they have different gates.**

---

## 4. Chinese: a second typeface

Rubik carries Latin, Cyrillic, Hebrew and Arabic — it does not carry CJK.
`check_font.py` counted **765 uncovered characters** in the Chinese table. One
more typeface came in:

- `vendor/noto-sans-sc/NotoSansSC-Regular.ttf` — **10,540,644 bytes**
- `unity/Assets/Lokanta/Art/Fonts/NotoSansSC-Lokanta.ttf` — **229,836 bytes**,
  a subset of the **876 code points** the game actually uses

The subset carries the Latin letters, the digits and the currency symbol as
well: when the language is Chinese, **the whole tree** is drawn with this
typeface, or else "12 ¤" would be empty boxes.

Two things came out of this:

**The web build of Noto Sans SC has no Latin Extended-A** (ğ İ ı Ş). The Turkish
marks were stripped from the Latin proper nouns in the Chinese table — the
reasoning for that decision is written at the top of `loc_zh.py`.

**A second typeface = a second licence text.** Saying "there is a Rubik OFL and
this one is OFL too" on the licence screen does not satisfy a licence:
`licenses/noto-sans-sc-ofl` was added as a separate file, and the credits line
and the `ATTRIBUTION.md` ledger were updated. The tour counts the licences
(3 → 4).

---

## 5. Arabic: if the letters do not join, the text is rubbish

This was the **only real technical obstacle** in the job, and it had been written
down as such from the start: *"there is no RTL handling anywhere for Arabic and
there is no separate text setting either — this can only be understood by
drawing it."*

In Arabic the same letter takes **a different shape** at the beginning, in the
middle and at the end of a word. Unity's **standard** text generator does not do
this: it lays the letters out one by one and left to right. What comes out is
not Arabic, it is a list of Arabic letters.

The solution is Unity 6's **Advanced Text Generator** (ATG): joining,
bidirectional ordering, line breaking. Three places were touched:

1. **A project setting** (`ProjectSettings/UIToolkitProjectSettings.asset`,
   `m_EnableAdvancedText: 1`). A checkbox — which means it can be off on a
   machine that has just cloned the repository. `AdvancedText.Set(true)` became
   part of the build; the same lesson the content sync taught, in the same
   words.
2. **The root element**: `UiRoot.ApplyLanguage()` sets the typeface, the text
   direction and the text generator together — all three are properties of the
   language. ATG is turned on **only in Arabic**: there is nothing to be gained
   by risking the four working languages for the fifth.
3. **The language button**: the Arabic language name is written joined even when
   the interface is in another language. Otherwise someone who reads Arabic
   would see scattered letters on the very button where he is looking for his
   language.

### The setting being off breaks nothing — which is why it is measured

With the setting off the text is drawn, the letters are visible, and every check
stays green. Only someone who reads Arabic sees that the writing is broken. In
this project the lesson *"a check that does not run looks exactly like one that
passes"* had been learned in precisely this form.

There is no API to ask about joining. But joining leaves a trace: **joined
letters make a word shorter**, because connected forms are narrower than
isolated ones. The tour writes the same Arabic word twice — once with the
standard generator, once with the advanced one — and compares the widths:

```
ok   : The Arabic letters join up (isolated 60.0 dp -> connected 40.0 dp)
```

A 33% narrowing. If the two numbers had come out equal, there would have been no
joining.

---

## 6. A half-translated interface is worse than one never translated

The text flowed correctly but **the layout did not**: the checkbox stayed to the
left of the text, the primary button at the bottom right, the day badge at the
top left. In a right-to-left language all of those are on the other side.

The mirroring was done in **one place** — there are thirty-two places that set a
row up, and forgetting one of them would mean that row flows the wrong way:

- `Theme.RowFlow` → `FlexDirection.RowReverse` in Arabic. `Theme.Row()` and the
  nine places that build a row directly all ask it.
- `Theme.SetGap()` gives the gap to `marginRight` instead of `marginLeft`. If it
  did not, the gaps would shift by one: an extra gap at the left end of the row,
  and no gap at all between the first two elements.
- `Icons.Play()` is mirrored horizontally. **It is the only icon that states a
  direction** ("open the day", "next day"); for someone reading right to left,
  forward is to the left. Pause, book and plate have no direction — those are
  not flipped.

Number pairs (`30.0 / 55`, `8,000 ¤`) look reversed in Arabic. That is not a
bug: it is the correct output of the Unicode bidirectional algorithm, and
someone reading right to left reads them **in the right order**.

---

## 7. The strip budget is now measured in five languages

It used to be measured in two. Once there were five, *"it fits in Turkish and
English"* no longer proves anything: the strip overflows on **the longest
string**, and we do not know which language that string is in.

| stage | tr | en | es | zh | ar | budget |
|---|---:|---:|---:|---:|---:|---:|
| morning | 154 | 154 | 154 | 159 | 154 | 220 |
| service | 154 | 154 | 154 | 159 | 154 | 220 |
| evening | 172 | 172 | **202** | 174 | 172 | 220 |

**The longest language is Spanish**, and on the evening strip it is 18 dp under
the budget. It could not have been guessed; it was measured. The measurement
calls `ApplyLanguage()` — calling only `Refresh()` would have measured Chinese
with Rubik, that is, measured the width of empty boxes.

A screenshot is taken in each of the five languages as well (`05-language-*.png`). An
empty box is a character too, and a row laid out backwards is still a row — some
things can only be understood by looking.

---

## What is not closed

- **The Arabic strings are not machine translation, but no native speaker has
  seen them either.** The same is true for Chinese and Spanish. Before release,
  one reading pass per language is cheap and valuable work.
- **The store page texts are still in one language.** The game opens in five
  languages; the Play Console listing does not.
