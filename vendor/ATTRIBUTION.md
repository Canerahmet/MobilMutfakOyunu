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
| Kenney Mini Characters | 1.0 | https://kenney.nl/assets/mini-characters | CC0 1.0 | 2026-09-11 | Customer and staff figures **and 32 animation clips** |
| Kenney Modular Characters | 1.0 | https://kenney.nl/assets/modular-characters | CC0 1.0 | 2026-09-11 | **NOT USED** — the package turned out to be 2D sprites, not a 3D wardrobe |

## Components the engine itself puts into the build

These were not downloaded; Unity puts them into the APK when it builds. They
are **distributed software** nonetheless, so they have a place in the ledger —
this table did not exist at audit time, and not one of the third-party
components inside the APK was on record.

| Component | Licence | From where |
|---|---|---|
| Newtonsoft.Json | MIT (James Newton-King) | `com.unity.nuget.newtonsoft-json` |
| AndroidX (28 modules) | Apache-2.0 | Unity Android player |
| Kotlin stdlib + kotlinx-coroutines | Apache-2.0 | AndroidX dependency |
| libc++_shared | Apache-2.0 with LLVM Exception | Android NDK |
| Swappy (libswappywrapper) | Apache-2.0 | Unity frame pacing |
| Unity runtime (libunity, libil2cpp) | Unity Companion / EULA | The engine |

**The rule has been updated:** item 1 above used to say "anything other than
CC0/CC-BY/MIT is not used"; Apache-2.0 is unavoidably inside already and it is
open to commercial use. The rule now applies to licences other than **CC0 /
CC-BY / MIT / Apache-2.0 / SIL OFL**.

## Animation

The character animations are **not a separate download** — they come inside the
FBX files of the Mini Characters package. Every figure has 32 clips; the game
uses five: `idle`, `walk`, `sit`, `interact-right` (the cook at the counter),
`holding-both` (the waiter carrying a plate).

Because the skeleton is the same in every figure, the clips are driven from a
single controller (`Art/Animator/Character.controller`). The licence is the same
as the package's own: CC0.

Quaternius and Mixamo **turned out not to be needed**; no clip was downloaded
from outside.

## What CC0 means

Creative Commons Zero: released into the public domain. Personal, educational
and **commercial** use are free, attribution is **not required**. From Kenney's
own licence text: *"You can use this content for personal, educational,
and commercial purposes."*

Attribution is not required but it **will be given** — Kenney's name appears on
the credits screen. Skipping a thank-you that costs nothing would be cheap.

## Our own work

The following are produced in code: no external source, no licence question.

| What | Where |
|---|---|
| All sound effects | `unity/Assets/Lokanta/Game/Sfx.cs` — the waveform is synthesised in code |
| Music | `unity/Assets/Lokanta/Game/Music.cs` — continuous synthesis, no file |
| Floor plan and room geometry | `unity/Assets/Lokanta/Game/RoomPlan.cs` |
| Application icon | `unity/Assets/Lokanta/Editor/IconShot.cs` — rendered from the game's own scene (Kenney CC0 models) |
| Splash screen background | `IconShot.Apply()` — flat colour, no image |
| The whole interface | `unity/Assets/Lokanta/Game/Ui/` — UI Toolkit, built in code |
| All content (dishes, ingredients, archetypes, customers) | `tools/content/`, `tools/balance/` |

## Typeface

| Package | Version | Source | Licence | Downloaded | Used for |
|---|---|---|---|---|---|
| Rubik | variable (wght) | https://github.com/google/fonts/tree/main/ofl/rubik | SIL OFL 1.1 | 2026-09-11 | All interface text |
| Noto Sans SC | Regular | https://fonts.google.com/noto/specimen/Noto+Sans+SC | SIL OFL 1.1 | 2026-09-16 | Chinese interface text |

**Copyright line:** *Rubik — Copyright 2015 The Rubik Project Authors (https://github.com/googlefonts/rubik), SIL Open Font License 1.1.*

**Copyright line:** *Noto Sans SC — Copyright 2014-2021 Adobe (http://www.adobe.com/), SIL Open Font License 1.1.*

A SECOND TYPEFACE IS A SECOND LICENCE TEXT. Rubik carries Latin, Cyrillic,
Hebrew and Arabic but no CJK, so the Chinese table needed its own font. Saying
"the other one is OFL too" does not satisfy a licence: the full text ships
separately at `Resources/licenses/noto-sans-sc-ofl.txt` and appears on the
in-game Licences screen. This row was missing here for a day - the Art copy
of this ledger had it and this one did not, which is exactly the divergence
rule 5 below warns about.
OFL asks for both the licence name and the copyright notice; the in-game
**Open source licences** screen shows both.

SIL Open Font License 1.1 is open to commercial use; the only conditions are not
to **sell the typeface on its own** and to distribute a derived typeface under
the same licence. Embedded use inside the game is free.
Licence text: `unity/Assets/Lokanta/Art/Fonts/License.txt`.

**Why a separate typeface was downloaded:** Unity's default runtime theme works
in the editor but **could not resolve the typeface in a build** — in the first
desktop build the buttons were drawn with no text visible on them at all.

**Coverage is checked:** `python tools/art/check_font.py` compares every
character the game can display against the typeface. On its first run it found
four missing (₺, →, ≡, ★); three were turned into drawn elements, and the
currency sign became the generic currency sign (¤).
