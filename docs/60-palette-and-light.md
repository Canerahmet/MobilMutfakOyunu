# 60 — The palette and the light: a room that is comfortable to look at

18 September 2026.

> *"Görsel kısmı iyileştirmeye devam et. Tek renk olması oyunun sıkıcı olmasına
> neden olabilir. Biraz daha renk paletini genişletebiliriz. Ayrıca daha yumuşak
> renkleri kullanmak göze hitap etmesi açısından daha iyi olur."*
>
> *("Carry on improving the visual side. Being one colour may be what makes the
> game boring. We could widen the palette a little. And softer colours would be
> better for the eye.")*

and, a few minutes later, the sentence that aimed it:

> *"Mesela zemin rengi ve arka duvar rengi biraz göz yorucu gibi."*
>
> *("The floor colour and the back wall colour are a bit tiring to the eye, for
> instance.")*

---

## 1. The complaint was right, and it was measurable

A frame was sampled rather than judged: twelve-by-twelve pixel patches off
`render/hall_*_noon.png`, reported as a percentage of the value range.

| surface | fast food | Turkish |
|---|---|---|
| hall floor | 29% | 14% |
| back wall | 16% | 19% |
| table top | 24% | 32% |
| pavement (outside) | 43% | 43% |
| sky | 28% | 27% |

**Nothing indoors reached 35%.** The brightest thing in the frame was a flat
pavement, and the sky stood darker than the ground beneath it. A picture with
no mid-tones has nothing for the eye to rest on and has to be read by
squinting, which is precisely the tiring part.

The hue range was as narrow as the value range. Fast food was slate, black and
one signal red; Turkish was brown, darker brown and copper. Three tones of one
hue is a monochrome however many objects are cut from it - which is the whole
of the "boring" complaint, stated as a measurement.

## 2. Three levers, and the third one was the real one

### Lift the value, drop the chroma, keep the hue

Every colour in both palettes moved by the same rule. The hue is the identity
and it does not move: the Turkish room stays warm and coppery, the fast food
room stays cool with a red in it. What changes is that they stop being dark and
stop being pure.

The two that mattered most are the two the user named.

**The back wall.** Turkish went from 0.29/0.20/0.14 - a dark brown that made
the room read as a cellar - to a pale plaster. A traditional lokanta has timber
to chair height and light plaster above it, and that contrast is the entire
reason the wainscot exists; a dark wall above it threw the contrast away. Fast
food went from 0.17/0.18/0.21, which at this light level is not a dark wall but
a black one.

**A signal red is for a warning light.** `Accent` was 0.85/0.24/0.20, as
saturated as the colour gets, and it is on the sign, the counter, the cushions
and the boards - all of them in shot at once, for sixty days. The same hue at a
third less chroma still reads as the identity from across the room.

### Give the kitchen four materials instead of one

Twelve station models were built in [59](59-kitchen-equipment-and-models.md)
and the line still came out as a grey band with two warm dots in it. "Stainless"
had been read as "one colour", and a real kitchen line is stainless AND a
worktop AND a dark plinth AND a painted carcass. The family is four hues now,
and the carcass - the biggest single area in the run - is a soft slate rather
than the near-black it was.

One part, and only one, is painted by the cuisine: **the kick strip**, a 10 cm
band at floor level under the whole row. The project has been here before ("the
first attempt sent the palette's metal to every metal part and the kitchen
turned GOLD"), and the discipline is the point: a kick strip does for a kitchen
what a skirting board does for a room - it grounds the run and carries the
temperature of the place without being the place. The appliances stay stainless.

### And the light, which is what actually held the floor down

Lightening the Turkish floor from 0.40 to 0.60 moved it from **14% to 17%**.
That is the measurement that redirected the whole exercise: no palette change
can lift a surface the light does not reach.

The building has walls on three sides and no ceiling, so the key light rakes
across it and the rooms sit in their own shadow. Every indoor surface was
reading 17-23% whatever it was authored, against a pavement at 43% - the inside
was receiving something like 40% of the light the outside got.

The fill is the shadowless counter-light and it is the one control that reaches
into a roofless room. The ambient went up with it: with no global illumination
and no baked light map, the ambient is the only bounce this pipeline has.

**And then the fill had to come back down, because it went to 1.00 and the room
stopped being dark by becoming bleached.** The kitchen line returned from the
render as a pale grey mass with no form in it - the one light that reaches into
a roofless room is also the one that casts nothing, and lifting a room and
modelling it are two different jobs. 0.58 → 0.85 does the lift; the KEY light
(1.55 → 1.70 at midday) does the modelling, because the sun casts and that is
what gives an object its sides back. Neither is touched at night, where the
darkness is the point and the lamps do the work.

The same correction applied to the kitchen's own materials: at 0.68 / 0.82 /
0.48 the three of them read as one pale mass, because three light colours under
a strong fill are one light colour. The carcass dropped to 0.40 so that the
worktop has something to sit on. **The spread between colours matters as much
as where they sit.**

This spends nothing from [58](58-visual-review.md)'s figure/ground measurement.
That work darkened the background so the sky would stop outshining the subject;
raising the fill brightens the SUBJECT, which moves the same ratio the right
way.

### A fourth thing, found on the way

`RoomColor` mixed 65% of a near-black tint (0.22/0.24/0.27) into the kitchen and
wash-room floors to mark them as back of house. So those rooms came out at half
the value of the room next door whatever the palette said - a distinction made
with the one channel that also decides whether a surface is comfortable to look
at. The distinction survives at 0.48 and a 50% mix, and it is now carried by
HUE, which costs the eye nothing.

## 3. What it came to

Measured the same way, same build, one change at a time:

| surface | before | after |
|---|---|---|
| fast food back wall | 16% | **34%** |
| fast food hall floor | 29%, cold blue | **42%, warm** |
| fast food kitchen floor | 23% | **31%** |
| Turkish back wall | 19% | **42%** |

The scene budget did not move: 303 renderers and 60k triangles at the ten-table
overview, against ceilings of 360 and 70,000. Colour is free.

## 4. Two things the instruments got wrong, which is worth writing down

**A point sample answers the wrong question.** "What is that pixel" is not "is
this picture dark", and twice a sample landed on a rug or a table and sent the
work at the wrong surface. A value histogram over the whole frame is the right
instrument, and it was written (`tools/art/tone.py`) rather than guessed at.

**And then the histogram lied too.** It reported that 44% of every frame sat in
one colour bucket at 25% value, and that number is real - but the pixels are
`GameShot`'s own clear colour, which the game never draws. The diagnostic tool
builds its own camera; this project already knew that (docs/58 §13.3, where the
colour grade rendered nothing for the same reason) and the lesson had to be
learned a second time in a different shape.

The sky gradient was widened and raised anyway, and that change stands on its
own: its bright end was sitting BEHIND the building, so the only part of the
gradient the camera could ever see was its dark half. Eight bands, same cost,
starting at the roof line now.

## 5. The crowd was reasoning from a room that no longer exists

`tools/art/gen_crowd.py` recolours the clothing per cuisine, and its note
spelled out the reasoning - which made it obvious that the reasoning had just
been invalidated:

> *fast food - the room is cold and dark (wall 0.173, 0.184, 0.212)... chroma
> up. turkish - the room is warm brown wood... saturated warm clothes would
> DISAPPEAR into it, so the crowd goes washed and lighter.*

Both premises were the OLD walls. Fast food's crowd answered a black room with
chroma x1.30, as loud as the swatches go; against a mid-tone wall that makes
twelve figures the most saturated thing in the frame (x1.12 now). And the
Turkish case had **inverted**: washed and lighter was right against dark timber
and is the very trap the note warns about once the wall is pale plaster - a
pale subject on a pale ground. That crowd goes slightly deeper and a little
more coloured now (0.62 -> 0.80 chroma, 1.02 -> 0.92 value).

Separation is still by VALUE, which is what survives at the 32 dp a figure
occupies in the overview. Only the sign of the difference changed.

A palette is a system, and moving one number in it moves the ones that were
measured against it. The comment is what made that visible: a number with its
reasoning written next to it announces when its reasoning expires.

## 6. Still open

- **The scale.** The figures are about a metre and the furniture is at adult
  scale, so the dishwasher cannot put their hands in the basin and the plate is
  held at chest height. It is a chosen style, not a defect, but it caps how
  convincing the working animations can be.
- **The two cuisines still differ mainly by temperature**, not by silhouette.
  The kick strip and the four materials help; a Turkish kitchen that is visibly
  a different SHAPE from a fast food one would help more.
- The room badges read as floating progress bars in a wide shot.
- **The kitchen is still the most monochrome room in the building.** Four
  materials and a painted kick strip are an improvement on one grey, and it is
  legible now where it was dim before - but stainless is stainless, and the
  thing that would really separate the two kitchens is a different SHAPE, not
  more paint. A tiled splashback behind the line is the cheapest next step.
