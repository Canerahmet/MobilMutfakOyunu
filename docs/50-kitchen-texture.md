# 50 — The kitchen's texture: wall, floor, outside

*15 September 2026.* The request:

> *"restoranın genel dokusu ve arka planı, duvarlar vs. üzerine çalışalım…
> internetten tipik bir fast food ve diğer mutfakların arka plan görüntüsünü al,
> bunları bizim oyuna uygula."*
>
> *("let's work on the restaurant's general texture and background, the walls and
> so on… take a typical fast food and other cuisines' background image from the
> internet and apply them to our game.")*

---

## 1. No image was downloaded from the internet — and none should have been

Two reasons, both binding:

**Licence.** The user's own rule is "asset licences must permit commercial
release" and `tools/check_licenses.py` looks for every asset in the attribution
ledger. Had a copyrighted photograph entered the game the audit would have gone
red — correctly.

**Technical.** A photographic texture sits wrong in a low-poly scene: under a
fixed 40-degree camera the wall is a few hundred pixels and a photograph whose
perspective does not match looks "pasted on".

There is a third, practical limit as well: a web search returns **text**. I
cannot see the image with my eyes, so I cannot say "apply this image".

Instead the internet was used as a **reference**: the characteristic materials of
those places were researched, and the look was built out of our own geometry.

### The distinction the research gave

| | characteristic surface |
|---|---|
| **Fast food** | durable and wipeable: stainless steel, laminate, ceramic; a bold warm accent over a neutral base |
| **Tradesman's restaurant** | "wooden wainscot, plain wall tile and simple decoration"; rustic — wood, stone, metal accessories |

---

## 2. The palette's identity was not touched

Fast food's wall is **dark**, and at first glance that contradicts the research
(QSRs use light neutral walls). Looking at the code gave the reason — the palette
was derived from reference images the user brought themselves:

> The first frame of the reference is exactly this — a red sign, a stainless
> counter, a black-and-red floor.

So this is a deliberate decision, a modern burger place. **It was not changed.**
The research was used to complete what was missing, not to break a design
decision.

---

## 3. What was missing: the surface of the wall

The wall was a single flat box — body + skirting + cornice. The two cuisines
showed **the same wall in a different colour**, so half of the "I have walked
into somewhere else" feeling was missing.

| | Turkish restaurant | Fast food |
|---|---|---|
| surface | **wooden wainscot** 1.05 m | panel + steel strip 1.15 m |
| upper band | copper | stainless |
| vertical joint | 0.85 m (board) | 1.15 m (sheet) |

The wainscot height is not arbitrary: **a seated person's back is at that
line.** Wainscot exists in real life to protect chair height too; it is not
decoration.

All of it is `Modeler` boxes — no new asset, no downloaded texture, nothing added
to the attribution ledger.

## 4. The outside came to belong to the cuisine too

The background stands in for the sky ([19](19-technical-setup.md)'s fill budget)
and was **identical in both cuisines** — so the distinction stopped at the hall's
four walls.

Now the cuisine's tone bleeds into it: fast food cold and urban, Turkish warm.
Measured, not eyeballed:

```
Turk: (93,123,157) -> (116,130,144)     blue fell, red rose
```

**Night was not touched.** Night being almost black is what carries the contrast
of the "an open restaurant" image; lighting it up for the sake of a tone would
have ruined the whole night.

---

## 5. The tour was picking a fixed cuisine

`Autopilot` always pressed **1** on the cuisine selection screen (Turkish). So
fast food's wall, palette, floor pattern, the colour of its outside — and the
**combo button** — were things the tour never saw.

A `-Cuisine fastfood` flag was added (default `turk`, so existing runs stay
identical). The first fast food run went red immediately:

```
FAIL : clipped text (day 40): 2 - Hızlandır | Kombo kapalı
```

("Hızlandır" = "Speed up", "Kombo kapalı" = "Combo off".)

---

## 6. The clipping measurement: three attempts, one lesson

Looking at the frame, "Kombo kapalı" **had wrapped to two lines and was fully
readable**. So the fault was not in the game, it was in the check itself.

| attempt | result |
|---|---|
| "measure the wrapping label by height" | 1 → **7** false alarms |
| "exempt it if it wrapped to two lines" | 7 → 1 |
| **"only measure a box that really clips"** | 0 |

Why the first attempt backfired: in UI Toolkit wrapping is **on by default**, so
every label fell into the new branch and obviously-fitting texts like "×4" and
"8.000 ¤" went red.

On the third I stopped patching and asked the real question, and the answer was:
**in UI Toolkit the `overflow` default is visible** — the text is drawn even if
it overruns the box, it is not cut. Across the whole interface `Overflow.Hidden`
appears in only two places (an icon box and a progress bar) and on no text label
at all.

So the measurement was looking for **a form of bug that does not exist** in this
interface, and could only produce false alarms. It never went off for years
because the Turkish labels happened to fit their narrow boxes.

The check now only measures the inside of a box that really clips: silent today,
but it will speak on the day somebody puts `Overflow.Hidden` on a text
container. The real protection is already in two neighbouring measurements —
**overlapping buttons** (that one caught the real bug in the Turkish cuisine) and
an element spilling off the screen.

*Patching a check three times is the price of never having asked what it
measures.*

### A green summary is not enough

One of the intermediate runs said "140 passed, 0 failed" and **proved nothing**:
fast food had failed to compile (`IResolvedStyle` has no `overflow`) and the
Turkish run had executed **the old binary** with `-SkipBuild`. What saved it was
looking at the build line at the top of the output, not at the end of the
summary.
