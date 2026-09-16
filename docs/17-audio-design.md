# Audio Design

**Last updated:** 9 September 2026
**Register item:** A12
**Status:** Written, awaiting decision

---

## The basic rule

**Every sound will have a job.** No sound that carries no information will be added.

The reason is a mobile reality: most players play with the sound off. If sound is designed as decoration, nobody notices what they are missing. If sound carries information, the player with the sound off loses that information.

Hence the second rule: **every audio warning will have a visual counterpart.** Playing silently should not make the game harder, only less enjoyable.

---

## Five layers

| Layer | Job | Continuous |
|---|---|---|
| Ambience | The identity of the venue | Continuous |
| Music | The mood of the phase | Continuous |
| Event | A notification of game state | Momentary |
| Interface | Touch feedback | Momentary |
| Character | Nonsense syllables instead of speech | Momentary |

---

## Ambient sound

Cuisine-specific, and it **thickens with the intensity of the service.** As the hall fills, the murmur of the crowd rises. The player hears that things are getting busy without looking at the screen.

| Cuisine | Ambient layers |
|---|---|
| Fast food | The sizzle of the fryer, till beeps, the glass door, street traffic |
| The Turkish lokanta | The clink of tea glasses, the sound of a ladle, a radio, steam off the hot counter |
| Italian | Cutlery, glasses, the oven door, light acoustic |
| Japanese ramen | Noodles being drained, the cauldron bubbling, steam, short orders |

Three to four layers per cuisine. These are independent audio files and they mix according to the intensity.

---

## Music

It changes with the phase. Four tracks per cuisine plus a shared end-of-year track.

| Phase | Character |
|---|---|
| Market | Calm, morning, light. Decision-making music |
| Counter | Focused, rhythmic but calm |
| Service | **Layered.** As occupancy rises, instruments are added |
| Accounts | Soft, closing, a little tired |

**The service music being layered matters.** While the hall is empty a plain base plays. As the tables fill, new layers come in. The player feels the pressure through their ears. Overcooked does this and it works.

**Loop length 60 to 90 seconds.** Mobile sessions are short; long tracks are not needed.

---

## Event sounds

These carry information. No event not on the list makes a sound.

| Event | Tone | Visual counterpart |
|---|---|---|
| A customer arrived | Neutral, short | A door animation |
| An order was placed | Neutral | An icon over the table |
| Food is ready | Positive, short | A glow at the station |
| **Patience critical** | **A warning, distinct** | The edge of the table turns red |
| **An ingredient ran out** | **A warning, distinct** | The stock bar is empty and flashing |
| A customer walked out | Negative | A brief symbol over the table |
| Payment taken | Positive, a money chime | An increase on the coin icon |
| A new dish opened | Positive, celebratory | A badge on the menu screen |
| Rent day approaching | A reminder, daily | A countdown in the top bar |
| A staff member resigned | Negative, heavy | A notification card |

**The two in bold are the most critical.** Patience and stock are the two situations where the player has to intervene immediately. Those two take the most distinctive place in the sound palette.

---

## Interface sounds

Roughly twelve of them, shared across all cuisines. Tap, confirm, cancel, error, the start and end of a drag, a screen transition.

Short, soft, not irritating when repeated. The player will hear these fifty times a day.

---

## Character sound

**No voice acting.** Nonsense syllables instead.

- Every archetype carries a vocal tone: the one in a hurry is high and fast, the pensioner is low and slow.
- Around twenty syllable samples, varied by tone.
- The intonation changes with satisfaction.

Three reasons: the cost is close to zero, it needs no localisation, and it adds character. A solution in the Animal Crossing line.

---

## Silence

Silence is a tool too.

- When service ends there is a short gap, then the accounts music comes in.
- While the customer reviews are read at the end of the day the ambience is turned down. Focus on the text.
- The end-of-year evaluation opens in complete silence, then a single track comes in.

---

## Production load

| Item | Count | Scope |
|---|---|---|
| Ambient layer | 3-4 | Per cuisine |
| Music track | 4 | Per cuisine |
| End-of-year track | 1 | Shared |
| Event sound | ~40 | Shared |
| Interface sound | ~12 | Shared |
| Character syllable | ~20 | Shared, varied by tone |

Total for the two cuisines at launch: eight music tracks, seven ambient layers, plus roughly seventy shared short sounds.

The decision about the source stands open in the register as item C4: a paid AI tool, or a public-domain library.

---

## Details awaiting a decision

1. Should the service music be layered, or is a single track enough
2. Should the character syllables change with the cuisine
3. Are four music tracks per cuisine too many
