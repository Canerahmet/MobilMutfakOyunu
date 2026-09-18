# Third-party assets — source and licence record

This file is **mandatory**. Where every external file that enters the game came
from, and under which licence it is used, has to be written down here.

The reason is commercial: the pre-release licence review can only be done this
way. A file with no answer to "where did we download this from?" is a file that
cannot be published.

## The rule

1. Every downloaded package lands under `vendor/` first, **as the zip**.
2. The `License.txt` inside the zip is **never deleted** and travels into the project with the asset.
3. A row is added to this table: what, from where, which licence, when.
4. If the licence is anything other than CC0 / CC-BY / MIT it is **not used** — ask first.

## The table

| Package | Version | Source | Licence | Downloaded | Used for |
|---|---|---|---|---|---|
| Kenney Food Kit | 2.0 | https://kenney.nl/assets/food-kit | CC0 1.0 | 2026-09-11 | Plates, food and kitchen objects |
| Kenney Furniture Kit | 2.0 | https://kenney.nl/assets/furniture-kit | CC0 1.0 | 2026-09-11 | Tables, chairs, cabinets, counters |
| Kenney Mini Characters | 1.0 | https://kenney.nl/assets/mini-characters | CC0 1.0 | 2026-09-11 | Customer and staff figures **and 32 animation clips**, **recoloured per cuisine** |
| Kenney Modular Characters | 1.0 | https://kenney.nl/assets/modular-characters | CC0 1.0 | 2026-09-11 | **NOT USED** — the package turned out to be 2D sprites, not a 3D wardrobe |

There are two records and **both must be right**: this file and `vendor/ATTRIBUTION.md`. They drifted apart for a while — this one said the Furniture Kit was 1.0 (the truth: 2.0, `Art/Furniture/License.txt`) and showed Modular Characters as used, when that package was never used at all because it turned out to be 2D sprites. The pre-release asset licence audit ([21](../../../../docs/21-business-and-release.md) D6) reads this table; one wrong row invalidates the audit.

## What CC0 means

Creative Commons Zero: released into the public domain. Personal, educational
and **commercial** use are free, attribution is **not required**. From Kenney's
own licence text: *"You can use this content for personal, educational,
and commercial purposes."*

Attribution is not required but it **will be given** — Kenney's name appears on
the credits screen. Skipping a thank-you that costs nothing would be cheap.

## Audio: no files, synthesis instead — and the slot is ready

Today there are **no audio files at all**; all thirteen sound effects are synthesised
inside `Sfx.cs`. That is a deliberate start but **not the finished state**:
simple waveforms sound cheap in a restaurant game, and sounds like a door bell,
a kitchen sizzle or the murmur of a crowd are not convincing when synthesised.

`Sfx.Init` now **looks at the files first**: if there is a clip under
`Resources/audio/<name>` it plays that, otherwise it falls back to the
synthesised tone. So adding an audio file **requires no code change** — putting
it in the folder is enough, and as long as nothing is put there the game runs
complete.

The files to be added (folder: `unity/Assets/Lokanta/Resources/audio/`). The
names are the literal strings `Sfx.cs` looks for, so this table and the code
have to agree - `tools/check_licenses.py` reads both and says so when they do
not:

| File name | When it plays | What to look for |
|---|---|---|
| `click` | Every button | Very short, soft interface click |
| `confirm` | Opening service, buying | Short positive two-tone |
| `cancel` | Rejected command | Short negative tone |
| `coin` | Bill paid | Till / coin |
| `door-bell` | Customer came in | Shop door bell |
| `sizzle` | Work started in the kitchen | Grill sizzle |
| `pour` | Drink prepared | Liquid pouring |
| `upset` | Customer left angry | Grumble / negative accent |
| `level-up` | Staff member levelled up | Short success phrase |
| `day-turn` | Day closed | Soft, low transition |
| `alarm` | A guest's patience is running out | Short rising warning, NOT a failure tone |
| `empty` | A dish was asked for and is not in stock | Dull, short - a shelf coming up empty |
| `combo` | A combo was sold | Short bright lift, quieter than `coin` |

Unity reads `.ogg`, `.wav` and `.mp3`; **`.ogg` should be preferred** (smallest
in the APK). The extension does not matter, the file **name** does.

**The licence rule does not change:** only **CC0** or sources that are open to
commercial use with a clear licence, even if that licence requires attribution.
Kenney was used for the visuals and Kenney **has CC0 audio packs too**
(Interface Sounds, UI Audio, Impact Sounds) — same licence, same source, same
consistent style. If another source is used, its licence text must be added as
`Art/<folder>/License.txt` **and** copied under `Resources/licenses/`; the
in-game licence screen shows the text itself, as it does for Kenney and Rubik.

Music has no file path **yet**; the same pattern can be applied to music, but
sound effects come first.


## Our own work

The following are produced in code: no external source, no licence question.

| What | Where |
|---|---|
| Sound effects (as long as no file is put in) | `unity/Assets/Lokanta/Game/Sfx.cs` — the waveform is synthesised in code |
| Music | `unity/Assets/Lokanta/Game/Music.cs` — continuous synthesis, no file |
| Floor plan and room geometry | `unity/Assets/Lokanta/Game/RoomPlan.cs` |
| The whole interface | `unity/Assets/Lokanta/Game/Ui/` — UI Toolkit, built in code |
| All content (dishes, ingredients, archetypes, customers) | `tools/content/`, `tools/balance/` |

## Typeface

**Rubik**, SIL Open Font License 1.1 — `Art/Fonts/Rubik.ttf`. Open to commercial
use and complete for the Turkish characters (`tools/art/check_font.py` verifies
it by scanning every piece of text in the game).

OFL asks for the licence text to be **distributed with the product**: the text
goes into the build as `Resources/licenses/rubik-ofl.txt` and the **full text**
can be read on the in-game licence screen — writing "Rubik — SIL OFL 1.1" does
not satisfy the licence.

For a while this section said "Unity's default theme, Liberation Sans, no
separate typeface was downloaded", and that was **wrong**.

### The second typeface: Chinese

**Noto Sans SC**, SIL Open Font License 1.1 — `Art/Fonts/NotoSansSC-Lokanta.ttf`.

Rubik carries Latin, Cyrillic, Hebrew and **Arabic** (shaping tables included)
but it does not carry CJK: it failed to cover 765 characters in the Chinese
table. This font was added by subsetting the full version (10.5 MB) down to
**the characters the game can actually show while the language is
Chinese** — 229732 bytes. That set is measured, not guessed: `tools/art/subset_font.py`
builds the subset from `check_font.cjk_characters()` and `check_font.py` audits
the same set, so the number moves when the game's text moves.

The subset carries the Latin letters, the digits and the currency symbol as
well: when the game is in Chinese the whole interface is drawn with this font.

The licence text goes into the build as a separate file
(`Resources/licenses/noto-sans-sc-ofl.txt`) and the full text can be read on the
in-game Licences screen. **Separate copyright holder, separate notice** — saying
"there is already an OFL" does not satisfy the licence.

### A derived file is still the original's licence

`Characters/Textures/colormap-crowd-fastfood.png` and `-turk.png` are the
pack's own `colormap.png` with the clothing swatches recoloured
(`tools/art/gen_crowd.py`). CC0 1.0 permits modification and redistribution
without condition, so the derived files ship under the same row — but they are
listed anyway, because a file in this tree with no row is a file nobody can
account for a year from now, and the release audit ([21](../../../../docs/21-business-and-release.md) D6)
reads this table, not the folder.

## Folder mapping (read by a machine)

`tools/check_licenses.py` reads this table. Every asset folder under Art/ must
have a row here; a folder that has none turns the check red. "Somebody looked at
it" is true once — when a new folder is added nobody looks again.

**Including the folders this project writes itself.** `Mesh`, `Prefab`,
`Materials` and `Animator` are produced by `Editor/ArtPrefabs.cs`, so no
`License.txt` is looked for inside them — but they were skipped ENTIRELY until
18 September, and two of them are not ours: `Mesh/` holds body meshes extracted
from the Kenney FBX files and `Prefab/` wraps those same models. The container
is the project's; the geometry is Kenney's. CC0 permits all of it, which is why
this was never a legal risk — but skipping a folder is not a judgement about
its licence, it is the absence of one, and the next pack to arrive this way may
not be CC0.

| Folder | Package | Licence |
|---|---|---|
| Characters | Kenney Mini Characters 1.0 | CC0 1.0 |
| Characters/Textures/colormap-crowd-*.png | Derived from the above by `tools/art/gen_crowd.py` | CC0 1.0 |
| Furniture | Kenney Furniture Kit 2.0 | CC0 1.0 |
| Food | Kenney Food Kit 2.0 | CC0 1.0 |
| Fonts | Rubik (Hubert & Fischer) + Noto Sans SC (Google) | SIL OFL 1.1 (both) |
| Icons | The project's own work (Editor/IconShot.cs) | — |
| Mesh | Body meshes extracted from the Kenney Mini Characters FBX files by `Editor/ArtPrefabs.cs` | CC0 1.0 |
| Prefab | The project's own prefabs, wrapping the Kenney models above | CC0 1.0 |
| Materials | The project's own work (`Editor/ArtPrefabs.cs`); the character material samples the pack's `colormap.png` | CC0 1.0 |
| Animator | The project's own work (`Art/Animator/Character.controller`), driving clips from the packs above | CC0 1.0 |
