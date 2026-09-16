# Venue Layout: the Open Hall, Rooms, a Fixed View

**Last updated:** 10 September 2026
**Register items:** A9 the screen list and the flow, B7 the input-to-action map, C1 the art pipeline
**Status:** Research and a proposal. Awaiting a decision.

**Basis:** the touch-target measurement at the end of [16-screens-and-tutorial.md](16-screens-and-tutorial.md); the renders from `unity/Assets/Lokanta/Editor/RestaurantScene.cs` and `unity/Assets/Lokanta/Editor/RoomLayout.cs`; web research on twenty-six games.

**Warning:** some of the findings about games in this file rest on search-index excerpts of pages that cannot be fetched directly, such as Fandom, GameFAQs, TouchArcade and Gamezebo. Every item that could not be confirmed is explicitly marked **not verified**. No guesses.

---

## 1. The question

The developer proposes this: let the restaurant not be a single open hall but be made of **rooms**. Let the hall, the kitchen, the sink and the store be separate rooms; let expansion **add a new room** instead of adding tables to the same floor.

The question is urgent because on 10 September 2026 it was measured how many dp a table is on a phone screen, and the answer came out as **a table cannot be a touch target**. The room proposal is an answer aimed at the gap that measurement opened. Whether the answer works cannot be known without measuring it.

This file does three things: it completes the measurement, it collects what comparable games do, and it compares three candidate layouts with their costs and gives a recommendation.

---

## 2. The measured constraint

### 2.1 The standard

| Source | Minimum touch target | Additional rule |
|---|---|---|
| Google, Android accessibility | **48 × 48 dp**, physically about **9 mm** | At least **8 dp** of space between targets |
| Apple | 44 × 44 pt | — |
| Google, general advice | 7-10 mm for touchable objects | — |

The conversion `RoomLayout.cs` uses: a 960-pixel render → a 2,400-pixel phone (×2.5), density 2.75. So **a 2,400 × 1,080 pixel phone is 873 × 393 dp in landscape**. 48 dp means **5.5%** of the screen width.

### 2.2 Measured in the open hall (docs/16, from the Unity console)

| Tier | Tables | Hall | Table, on a 2,400-pixel phone | dp | Fraction of the minimum |
|---|---|---|---|---|---|
| 1 | 4 | 7.8 × 7.4 m | 55 pixels | ~20 dp | 42% |
| 2 | 7 | 9.6 × 7.4 m | 53 pixels | ~19 dp | 40% |
| 3 | 10 | 11.5 × 7.4 m | 50 pixels | ~18 dp | 38% |
| 4 | 14 | 13.3 × 9.1 m | 42 pixels | ~15 dp | **31%** |

### 2.3 What exactly is broken

Reading `RestaurantScene.cs` shows where the number comes from, and the diagnosis turns out to be something other than "fourteen tables is too many":

| Factor | Value | Its effect |
|---|---|---|
| The table top | 0.86 × 0.86 m | This is the thing being measured; with the chairs the set is 1.86 m |
| The grid step | 1.85 m across, 1.70 m in depth | 14 tables = 6 columns × 3 rows = 11.1 × 5.1 m |
| The kitchen strip and its margin | 2.4 m + 1.6 m | **44%** of the hall's depth is not tables |
| Wall height | 3.0 m | It dominates the vertical axis in the camera fit |
| Camera | 32° vertical field of view, 30° tilt, −16° rotation | The tilt foreshortens the depth |
| The fit formula | `distance = max(distV, distH) + maxZ + 4% margin`, then ×1.04 | The `+ maxZ` term pulls the camera **40% further back** than it needs to be |

The critical point: **the camera fits from the height, not the width.** A 20:9 frame is 2.22:1, while a box projected at a 30° tilt takes up more room vertically. The result is visible to the eye in the `oda_14` render: the restaurant strip fills about **45%** of the frame and the rest is empty background.

That means part of the number is **camera staging, not a law of nature.** By cropping the frame, leaving the wall out of the fit and lowering the tilt, roughly 1.5× can be gained: 15 dp → about 22-25 dp. **Still under 48.** The camera setting eases the problem, it does not solve it.

### 2.4 Why "let us make the target bigger than the table" does not work

docs/16 had claimed this, and the measurement confirms it. At tier 4 the column step is 1.85 m and the table top is 0.86 m. If the table is 15 dp then the column step is 15 × (1.85 / 0.86) = **32 dp**. The row step is narrower still on screen. Invisible 48 dp targets would overlap their neighbours and the 8 dp of space Google asks for would be gone entirely.

**The whole table set** (a table plus two chairs, 1.86 m) is about 32 dp at tier 4. That is not enough either.

---

## 3. A second measurement: a room-based layout was built

`unity/Assets/Lokanta/Editor/RoomLayout.cs` (10 September 2026, 10:57) built the room proposal and rendered it in two camera modes. The layout: the kitchen (4.2 × 4.6 m) and the sink (2.6 × 4.6 m) at the far left, the hall rooms to their right. Each hall room is 4.6 × 4.3 m and holds 4 or 3 tables.

| Tier | Tables | Hall rooms | Total strip width |
|---|---|---|---|
| 1 | 4 | 4 | 11.4 m |
| 2 | 7 | 4 + 3 | 16.0 m |
| 3 | 10 | 4 + 3 + 3 | 20.6 m |
| 4 | 14 | 4 + 3 + 3 + 4 | 25.2 m |

**Note:** our tier increments are +3, +3, +4. One hall room takes 3-4 tables. So the tier ladder and the room ladder **line up one to one.** That is not a coincidence; the tier counts are already the size of a room.

### 3.1 Hard evidence: the room frame is independent of the tier

When the MD5s of the renders are taken, `room_10_oneroom` and `room_14_oneroom` come out as **the same file bit for bit**. The same frame, the same table size, in a ten-table restaurant and in a fourteen-table one.

```
84afa4423b8f8c177fc3f08b8776f6cb  room_10_oneroom_105735.png
84afa4423b8f8c177fc3f08b8776f6cb  room_14_oneroom_105735.png
```

This is the room-based layout's one real structural claim: **if the camera frames a room, the touch target stops depending on the size of the restaurant.** In the open hall the table falls from 20 dp to 15 dp; in a room frame it does not fall, because the thing being framed is not growing.

### 3.2 The measurement table

| Tier | Tables | **Open hall** table | Room layout, **whole restaurant**: table | Room layout, **whole restaurant**: room | Room layout, **single room**: table | Room layout, **single room**: table + chairs |
|---|---|---|---|---|---|---|
| 1 | 4 | 20 dp | ~44 dp | ~235 dp | ~45 dp | ~100 dp |
| 2 | 7 | 19 dp | ~31 dp | ~167 dp | ~45 dp | ~100 dp |
| 3 | 10 | 18 dp | ~24 dp | ~130 dp | ~45 dp | ~100 dp |
| 4 | 14 | **15 dp** | ~20 dp | **~106 dp** | **~45 dp** | **~100 dp** |

**Where the numbers come from and how much they can be trusted.** The open-hall column is a number measured from the Unity console (docs/16). The room columns are values I calculated from `RoomLayout.cs`'s geometry and read off the renders; I could not get at `RoomLayout.cs`'s own `Debug.Log` output. The two methods agree to within about 15%. **The script has to be run and the console line written to a file**; the decision rests on the sign of these numbers, not on their decimals.

The fall in the "whole restaurant" column is inversely proportional to the strip width: between 11.4 and 25.2 m the table goes from 44 dp down to 20 dp.

### 3.3 Three conclusions

1. **No layout can make the table 48 dp while showing the whole restaurant in one frame.** Neither the open hall (15 dp) nor the room layout (20 dp). At tier 4 a table cannot be the primary touch target on any camera that shows the world.
2. **The room layout produces an intermediate target that does not exist at all in the open hall:** the room. At tier 4 a room is ~106 dp, twice the minimum. In the open hall there is no object at all to touch between "the whole hall" and "one table".
3. **In a single-room frame the table gets close to 48 dp (~45 dp) and the table set passes comfortably (~100 dp).** But that means the camera is showing a quarter of the restaurant.

At tier 4 in single-room mode a 20:9 frame shows about 15 m of world width: the kitchen, the sink, the first hall and part of the second. So "single room" mode in practice means **two rooms plus the service strip**, not one room out of four.

---

## 4. What comparable games do

Five questions for every game: (1) is the venue a single open floor, rooms, or a single fixed view; (2) how does expansion work; (3) the camera; (4) what is touched; (5) how is the target protected on mobile.

### 4.1 Mobile time-pressure service games

| Game | Venue | Expansion | Camera | Touched | Its mobile solution |
|---|---|---|---|---|---|
| **Diner Dash** (2004, PC) | One fixed screen per level | 2-6 tables per level; a new venue = a new scene. Level 1-1 two tables, 1-2 four tables; the Hometown Hero guide says "six tables is a lot" | Fixed, no panning and no zoom | **The table.** A table is tapped about 5 times for a single customer: seat, take the order, bring it, the bill, clear | The table count is limited by the level; there is no permanently growing hall |
| **Diner DASH Adventures** (Glu, 2019) | Separate, hand-designed fixed level scenes | The game: new levels and venues. The venue: **decor slots opened with stars**, with a few design options in each slot. The player never draws a layout | In-level camera control **not verified** | Customers, tables, the fixed strip of food stations | **The tables are colour-coded** — at a small size a table is recognised by its colour, not its silhouette. The decor was taken out of the live scene and moved into a slot picker |
| **Cook, Serve, Delicious! 2/3** | **No venue.** A queue of order tickets; the holding stations along the top of the screen, the queue on the left | What grows is the **interface slots**: the number of menu, prep and holding stations. CSD2 has a separate "Designer" screen that lets you place walls, floors and tables, but it is **purely cosmetic** | None, a static interface | Order tickets, prep buttons, stations. Never furniture, never customers | There are no targets in the world; every target is a large interface element in a fixed position. CSD2/3 have no mobile version; only CSD1 shipped, and TouchArcade wrote that the port **reduced** the input density |
| **Good Pizza, Great Pizza** | **A single fixed counter view.** No hall simulation, no seating logic, no camera | Ingredients, equipment, decor, the garden. Notable: **the "Wide Counter" upgrade physically enlarges the work area** | Fixed | Dough, ingredients, the oven, the cutter, the customer's ticket | The targets are close-up and in fixed positions, independent of the size of the business. When the ingredient shelf got crowded (no labels, the player remembers by appearance) the solution was **not to shrink the icons but to enlarge the counter** |
| **Cooking Fever** | **One fixed view, four customer slots** per restaurant | **48 separate restaurants**, all on the same four-slot template. The single floor never grows | Fixed, no panning | Ingredient bins, appliances, plates, the counter, customers. **Nothing spatial is ever touched: no tables, no chairs, no floor** | Simultaneous customers are **fixed at 4**; the difficulty is not the count but the speed and the complexity. Interior upgrades (an aquarium, a disco ball, stools) give stats, appear in the scene and are **never touched** |
| **Cooking Diary** (Mytona) | A restaurant per scene, with **a walking chef avatar** | 9 districts × ~6 restaurants, separate scenes | **Not verified** | Stations and objects; the avatar walks there by itself | What is touched is the **station**, not a distant target; no precise aiming is needed |
| **Cooking Madness** | A fixed single-view kitchen per level | 80+ themed restaurants, 3,000+ levels, a world map | Fixed (**not verified beyond the store description**) | Cooking stations; the orders are above the customers' heads | A new scene, not a growing scene |
| **Animal Restaurant** | **Separate areas:** Main, Kitchen, Courtyard, Concert, Garden, Buffet, Fishing Pond, Takeout, Terrace | "New rooms open up"; each area produces its own income | Fixed per area; the interface is along the screen edges | **Customer orders, money, rubbish** and the edge interface. Not tables | The commercially most successful room-based mobile restaurant game; the targets are customers and interface, not furniture |

### 4.2 Shop simulations

| Game | Venue | Expansion | Camera | Touched | Mobile |
|---|---|---|---|---|---|
| **Supermarket Simulator** (2024) | **A single open sales floor** + a single storage building next to it | 23 "Growth" sections, each **4×4 m**, all of them extending the same floor, $1,176,900 in total. Storage is a separate 15 sections on the same 4×4 m logic | First person | Physical objects up close + a **computer terminal** for ordering, pricing and hiring | No official mobile version; the listings on Google Play are copies. The storage is deliberately awkward: at tier 1 only the **street door** opens, the in-store door comes at tier 3 |
| **TCG Card Shop Simulator** (2024) | A single rectangular floor + an adjacent **Lot B** | Shop A: 30 tile expansions. Lot B: once at level 15 for $5,000, then 14 more expansions. Each expansion is roughly a 1×1 area | First person | Shelves, the card table, the till, boxes; buying an expansion happens **in the RENO BIGG phone app** | No mobile version. Players want "walls and partitions to be more creative" — **the game gives no rooms, and the players notice** |
| **Recettear** (2010) | **A single fixed shop room**, top-down. Recette cannot leave the counter | The same room grows three times: ML12 → 4 counters, ML20 → 6, ML26 → 10. A new room is **never** added; only the door moves | Fixed top-down, a single screen | A counter slot, then the haggling interface | No port |
| **Moonlighter** (2018) | **A single room**, top-down | 4 shop upgrades; the room grows and rearranges the furniture, at most 14 tables. It never becomes multi-room | Top-down, the shop fits on one screen | **Table** → inventory → **a price per table**; customers react to the price with an emoji | **The strongest touch evidence in this set.** iOS 2020 / Android 2021. Not a straight port: "the interface was designed from scratch for the move to touch", "tap where you want to go" instead of a virtual stick — "so that players do not cover the corners of the screen with their thumbs". And: **"the shop stocking and selling menus feel far more natural on touch"** |
| **Tavern Master** (2021) | A single freely drawn building; no pre-cut rooms | It grows by **erasing** walls ("draw a hole over that wall"). The kitchen, the guest room and the store open through **research**, and then the player builds them | Top-down/isometric, pan and zoom | Catalogue tab → furniture → drag to place | The "Tavern Master" on Google Play appears to be a different game; **a mobile version of the PC game could not be verified** |
| **Cat Cafe Manager** (2022) | **A single open floor, no interior walls.** The developer's own answer: to make rooms you leave "a row of empty tiles" | Buying tiles (the cost rises as the cafe grows); the walls place themselves. Temple research raises the chair and crew ceilings | Angled, zoom available, **no rotation** | Build mode (floor, wallpaper, windows) and decor mode (furniture, appliances, doors) | No mobile |
| **Chef Life** (2023) | **Separate rooms:** kitchen, dining room, office. Third person (not first) | You tap the plan on the dining-room wall and **swap in a ready-made layout.** Only during the daytime prep, and it needs a level and an "Interior Design" unlock. **The decorations reset** | Third-person follow | Stations and ingredients, the catalogue book in the office, the plan board | No mobile |
| **Discounty** (2025) | A top-down single floor | **Exactly twice**, each time **a new adjacent area**: a tea/coffee shop on the right, then one on the left. There is a separate storage room | Top-down | Shelves, the till, products; an adjacency mechanic (a "booster" makes the products on the adjacent shelf attractive) | No mobile |
| **Travellers Rest** (2020) | **Three floors** in a single building (cellar, tavern, rooms to let) | Build Mode opens at reputation 7: buy **individual floor tiles** → assign a zone (red for food, blue for production) → place a door, and the enclosed area becomes a **room to let**. The tile allowance and the maximum number of rooms grow with reputation | Top-down | The building table, tiles, doors | No mobile |
| **Dave the Diver** | A single fixed restaurant scene | **No physical room is added.** The Cooksta rank grows the crew and the menu; at the very end a **second branch**, that is, a separate venue | Fixed | Service taps; decoration from the menu at the bottom of the screen | Mobile version 17 September 2026, "fully optimised for phones" |
| **Restaurant Renovation** (ZYMobile) | **Not a management game.** A match puzzle plus decoration; there is no floor that is walked on | Scenes are renovated with puzzle winnings | None | Puzzle pieces and decor options | The name is misleading; this one should come off the reference list |

### 4.3 Two references for the grammar of rooms

| Game | Venue | Expansion | Camera | Touched | Touch |
|---|---|---|---|---|---|
| **Two Point Hospital / Campus** | Rooms the player **draws** inside a fixed building shell. Minimum between 2×3 and 4×5; **a door is a mandatory fitting**; room quality (1-5 prestige) comes out of the size plus the items in it | Buying adjacent **plots**, horizontally. No floors. Every building is a micro hospital | Free 3D: pan, **rotate**, tilt (up to about 45°), zoom. **There is no single-room mode**; instead, 12 colour layers ("Visualisation Modes") | Rooms, individual staff (up to 10 actions, picking a staff member up with "Pick Up" and dropping them in a room), individual patients, and six separate interface lists | **No touch version at all, across three games in seven years.** No iOS/Android, no Netflix; a "JUMBO Edition" console bundle. On Switch **touch is not supported at all**, and reviewers found that strange |
| **PlateUp!** | Real rooms enclosed by walls, a service window and **doors**. 5 procedural plan types | **It does not grow within a run.** The plan is fixed at the start of a run; the experience level opens larger plans (Extended 10, Huge 11). What grows between days is **density**: new appliances inside the same shell | Fixed top-down, **no player control**. No control is needed because the stock plans fit in a single frame | Appliances, blueprints | No mobile. On large maps the camera is not enough; the community mods (CameraPlus, Free Camera Control) exist for exactly that reason |

**Two Point's lesson about control.** The console port built a **virtual cursor** instead of touch: the left stick drives the cursor, the right stick the camera, the shoulder buttons the zoom and the list navigation; the menu was pinned to the bottom left corner. The reviews said "it settles in within fifteen minutes" but added that "selecting a particular item or person can get hard". The same problems came back in Two Point Museum's Switch 2 port: **overshooting the target** when placing with a stick, **no undo**, mode changes not being visible, menu depth, and **text that is far too small**. The one mitigation that works: **the cursor is held fixed in the middle of the screen and the world slides underneath it.**

**Two Point's lesson about pathfinding — the harshest one for us.** A corridor is not something that is built, it is the **complement of the rooms**: "every part of the hospital plot that is not a room is a corridor." The validity rule is one sentence: **"every room must be connected to a corridor by its door, and every room on the same plot must have a clear path to that door."** The cost of this is documented: stuck patients, "cannot find a path" and "invalid navigation" bug titles; the players' fix is always to break the layout and put it back. In the developers' own account, the hardest part was "the patients' queues and their movement in the corridors" and "the wall thickness and the cell width".

**PlateUp's generator contract** is the cleanest written form of the grammar of rooms: a property is added to every pair of adjacent tiles that are not in the same room, **a random door is placed for every pair of adjacent rooms**, and the system **guarantees a path through doors from the front door to every room.**

### 4.4 Working examples of room-based growth on a phone

| Game | Venue | Expansion | Camera | Touched | Lesson |
|---|---|---|---|---|---|
| **Fallout Shelter** | Rooms seen in cross-section, stacked floor by floor | Dig a new room; rooms of the same type placed side by side **merge automatically**, at most three merges, capacity from 2 to 6 | Pinch zoom, panning, and **automatic zoom**: touching a room or a dweller sends the camera there and centres it. "Designed for a mobile version played with a finger on a small screen" | Rooms, dwellers, the hammer button at the bottom left | **A documented failure.** From one player's write-up on touch: "to see a lot of rooms at once you have to zoom out so far that touching any of them is not easy" and "trying to select the radio room is equally likely to select the dweller inside it". The automatic camera is not liked either: "taking control of the camera away from the player" is a standing complaint |
| **Tiny Tower** | A vertical stack of floors, one business per floor | Build a new floor | Vertical scrolling; **four floors at a time** are visible in the in-game view | Floors, the lift, bitizens | A floor is a quarter of the screen height. The target size **does not change** with the number of floors, because the camera never shows the whole tower |
| **Hotel Empire Tycoon** | Separate rooms and areas; "you jump from one room to another" | New rooms and areas, then an entirely **new hotel** | Camera detail **not verified** | Tap an area → a performance and crew panel opens | An object in the world is a **selector**, not a manipulation tool |

### 4.5 The common answer from twenty-six games

There are three patterns, and **every successful mobile example uses at least one of them**:

| Pattern | Who uses it |
|---|---|
| **A fixed close-up station layout with a capped concurrency.** The targets are large and never move; the difficulty comes from the speed, not from the count | Cooking Fever (4 slots), Good Pizza (a single counter), CSD (the holding stations along the top of the screen) |
| **Growth = a new scene, not a growing scene.** The camera never has to pull back | Cooking Fever's 48 restaurants, Cooking Madness's 80+, Cooking Diary's 9 districts, Diner Dash's venues, Dave the Diver's second branch |
| **Layout and decoration exiled from the timed screen** onto a separate screen, with slot pickers | Diner DASH Adventures (decor slots with stars and keys), CSD2's Designer, Cooking Fever's interior menu, Good Pizza's shop and garden screen |

And two negative findings:

- **The only game that has you touch a table is Diner Dash**, and it keeps the table count at **2-6 per fixed screen**, makes the increase **between levels rather than inside one restaurant**, and in the 2019 mobile version also makes the table recognisable **by colour**. **There is no example in this genre of a fourteen-table hall in a single frame.**
- **No shop game that has shipped grows by opening pre-written discrete rooms.** The two closest (Supermarket Simulator, TCG Card Shop Simulator) extend a single floor with 4×4 m tiles and add **one** back room. Wherever a separate room exists (Supermarket's storage, TCG's Lot B, Travellers Rest's floors) it exists **to hide the back area** and walking distance has been put in deliberately — friction that reads as content in first person reads on a phone in 2.5D as **the camera going back and forth**.

---

## 5. Kairosoft: the only commercial example of room-based management on a phone

The brief flagged Kairosoft as "the closest commercial precedent". It flagged it correctly, and the answer is sharper than expected.

### 5.1 Four games, two different structures

| Game | Venue | Expansion | Camera |
|---|---|---|---|
| **Cafeteria Nipponica** | **No rooms.** Tables and facilities are placed on a grid inside an orange-framed plot. The manual: "To place a table, select an orange-framed area of the restaurant" | Staged: small → medium → large. Also relocation and three restaurants at once. **The grid dimensions are not verified** | A console port review says "you can zoom right in and back out" |
| **Hot Springs Story** | **Discrete rooms with fixed footprints.** The Large Bath is 2×3; the scenery bonus only applies to its left 2×2; its entrance has to be on a particular tile with a clear path from above or from the right. Combos work within a 2-tile radius | Buying **deeds**, directionally: Deed I 20,000 → +1 above, +2 to the left. Deed V 2,500,000 → +4 above | Pan + zoom + a direction wheel opened from a corner button |
| **Mega Mall Story** | **Rooms on floors.** Shops are 1, 2, 3 or 4 tiles wide | Bought as investments: "Mid Mall" = 4 columns on each side; "Basement" = down to BF3. **Stairs and escalators are explicit circulation facilities** and visibly affect sales | Drag with a finger, or direction arrows from a corner button |
| **Dream House Days** | Rooms with a furniture cap: small 16, medium 32, large 64 | Apartment size | — |

### 5.2 How Kairosoft solves the touch target

The answer is in a single sentence: **Kairosoft does not make the grid a touch target, it makes it a selection target.** The decision is taken in a menu, not in the frame.

| Technique | Evidence |
|---|---|
| **No direct manipulation.** You touch a tile and a menu opens | Kairobotica: "Tap an empty plot to open the build menu" |
| **There is always a second route:** the menu button in the corner | Sushi Spinnery: "tap the menu button and choose build from the drop-down list". Kairobotica: the menu button is at the bottom right, the menu opens on the left |
| **There is a confirmation step; its indicator is a ghost grid** | The Sushi Spinnery expansion: "you will see grids showing how big the expansion will be, and after selecting the area you **tap again** to confirm the purchase" |
| **Zone first, then tile.** The game highlights the valid zone | The Cafeteria Nipponica manual: "select the orange-framed area" |
| **Dragging only for an object that needs continuity** | The Sushi Spinnery conveyor: "tap and drag, make sure it connects to the existing belt" |
| **Redundant navigation input:** finger drag and pinch **plus** a direction wheel opened from a corner button | Pocket Academy: "you can zoom in and out with the pinch method... if you tap the pink down-arrow button at the bottom left, a navigation wheel appears" |
| **The game logic has its own cursor**, separate from the pointer | In Dungeon Village on Switch 2 you can see "two cursors doing different jobs on screen" |

**The complaint is real and Kairosoft themselves fixed it.** TouchArcade says of the older titles that "the interface feels lifted from a PC game"; of the newer ones, "finally an interface that feels designed for a smartphone rather than for feature phones and PCs, **placement is less of a chore, the menu buttons are bigger**". The complaint that remained in 2023 is **menu depth**: "getting to some submenus and commands is more roundabout than it needs to be, especially in staff management".

### 5.3 Three takeaways for us

1. **The restaurant-themed Kairosoft game (Cafeteria Nipponica) does not use rooms.** The ones that use rooms are the hotel, the shopping mall and the apartment block. So Kairosoft itself chose an open grid for the restaurant.
2. **The confirmation step is mobile's real answer.** Ghost grid → tap again → confirmed. That closes, directly, the trap Two Point fell into on console (overshooting the target + no undo).
3. **Redundant input is mandatory:** finger panning and pinch always, plus a direction control opened from a corner. Both do the same job; that redundancy is deliberate.

---

## 6. Three candidate layouts

| | **A. The open hall** | **B. Rooms** | **C. A single fixed view** |
|---|---|---|---|
| Definition | One floor, expanding by tier. Today's `RestaurantScene.cs` | Hall, kitchen, sink and store separate; a tier adds a new room. Today's `RoomLayout.cs` | The camera never moves, the hall is decor, the decisions are in the interface |
| Table at tier 4, in the whole-restaurant frame | 15 dp | ~20 dp | ~20 dp |
| Table at tier 4, in a room frame | No rooms | **~45 dp** (fixed) | Not applicable |
| Intermediate touch target | **None.** Nothing between the hall and a table | **A room, ~106 dp** | Bottom-bar chips, **as many dp as you like** |
| Camera work | Pan and zoom: 2 taps in the tap budget (review/05) | Changing rooms: a full tour at tier 4 is 3 taps | **Zero** |
| Visible growth | In a single frame, directly | Present in the far mode, absent in the room mode | In a single frame, directly |
| Pathfinding requirement | None; the tables are on one floor | Door and path consistency needed (the Two Point, PlateUp rule) | None |
| Art volume | 1 shell per cuisine × 4 tiers = 8 layouts (review/03's budget) | ~7 room modules per cuisine + a shared door/wall kit | 1 shell per cuisine |
| Precedent | **None.** No mobile example with 14 tables in a single frame could be found | Animal Restaurant, Fallout Shelter, Tiny Tower, Hot Springs Story | Cooking Fever, Good Pizza, CSD, Recettear |
| Its main risk | The table cannot be touched at any tier | The navigation tap and the loss of at-a-glance | The hall falls to decor |

---

## 7. What a room-based layout costs us

### 7.1 The navigation tap — the harshest constraint

| Source | Number |
|---|---|
| docs/16's daily tap budget | 40-60 |
| docs/27's derivation | a 480,000 ms service day ÷ 60 = 8,000 ms per tap |
| review/05's day count, **the whole service stage** | 10 taps, **2 of them camera** |
| docs/27's number of day slices | 4 |

At tier 4 there are four hall rooms. A full tour = 3 room changes. If the player wants to scan the restaurant once per slice, that is **12 taps.** That is **120%** of the entire budget for the service stage and **20%** of the day's total budget. In return it produces zero decisions: navigating is not, in itself, deciding anything.

For comparison: an alert chip in the bottom bar (review/05's "patience queue chips" proposal) asks for **zero** navigation taps, because the alert comes to the player, the player does not go to the alert.

Eight 96 dp chips fit across an 873 dp landscape screen. docs/02's budget of 3-5 owner interventions a day is below that; so a chip bar never strains the ceiling.

### 7.2 Pathfinding — a direct conflict with docs/14

docs/14 says it plainly: **"Decision: no complex pathfinding."** Staff are pinned to their station, the waiter's path is precomputed and short, there are no collisions, staff can walk through each other.

The grammar of rooms conflicts with that decision in three places:

| Conflict | Evidence |
|---|---|
| If there are rooms there are **doors**, and if there are doors there is **connection validity** | Two Point: "every room must be connected to a corridor by its door, and every room must have a clear path to its door". PlateUp: the generator guarantees a path through doors from the front door to every room |
| When connection validity breaks, **agents get stuck, and that is a class of bug** | Documented in Two Point: the "cannot find a path" and "invalid navigation" titles. docs/14 had already written this down as the "shared wound" of Cat Cafe Manager and Tavern Keeper |
| In a room layout, **walking time turns into an economic variable** | Hot Springs Story: the game calculates distance and duration; how many facilities a guest can consume in a day is set by the walking |

And the numerical conflict, from `RoomLayout.cs`'s geometry: at tier 4 the kitchen is at `x ∈ [0; 4.2]` and the furthest hall at `x ∈ [20.6; 25.2]`. Centre to centre is **20.8 m.** At a walk of 1.2 m/s, one way is **17,300 ms.** docs/27 gives the waiter **9,000 ms** of "service" per customer. So a real walk in the strip layout comes to roughly **four times** the service budget.

There are three ways out: the walk is not simulated (visual only), the rooms cluster compactly around the kitchen, or every hall room gets its own service point. **The first is the one that is already correct.**

### 7.3 There is no such thing as a venue in the core

Searching under `src/` shows that the core has no venue model: a tier is `TierConfig(Tables, Rent, Upgrade, StaffCap)`, so **the table count is a scalar.** No coordinates, no rooms, no seats. docs/23 also declares the camera and screen transitions "not commands": view state does not enter the simulation and **is not saved.**

That has two consequences:

1. A room-based layout is, today, **a pure presentation decision.** Nothing changes on the simulation side, nothing has to be added. That is good news.
2. If the rooms are wanted to mean something in the simulation (capacity per room, staff per room, routing customers) that needs **new state, a new save field and new pathfinding.** At that moment docs/14's decision breaks.
3. Because the camera state is not saved, **a player who leaves and comes back mid-day wakes up in the default room.** docs/16's "leave at any moment, resume from the second you left" rule does not hold for the camera.

### 7.4 Art volume — a smaller matter than it is assumed to be

review/03's output budget: "Architectural shell: 2, each with 4 expansion tiers = **8 layouts**", 2-3 weeks per cuisine, 1-1.5 person-months in total.

In a room layout the count changes:

| Item | Open hall | Rooms |
|---|---|---|
| Hand-built layouts per cuisine | 4 (one per tier) | 0; a tier is a combination of modules |
| Room modules per cuisine | 1 shell | Kitchen 1, sink 1, store 1, hall 3-4 variants = **6-7** |
| Shared | — | A door/wall/transition kit, once |
| Total for two cuisines | 8 layouts | 12-14 modules + 1 kit |

The module count goes up, but each module is smaller than a layout, and this is exactly the logic docs/24 has already adopted ("32 dishes, 14 meshes"). **Art volume is not the room layout's main cost; it is roughly break-even.**

There are two real art costs:

- **Four identical hall rooms read as copy-paste.** At least 3 variants are needed, otherwise tier 4 looks cheap. That takes back the "no new design per layout" saving.
- **The store is a new room with no counterpart in any document.** docs/14 has four roles and no storekeeper; docs/12 has stock but no physical store. Two Point's rule is the warning here: a room must have **mandatory fittings and a job.** A store room with no job is pure cost.

Also, in the form they have in the renders, what is being proposed is in fact **not rooms**: `RoomLayout.cs` places three walls, the front is open to the camera, there is no ceiling and there are no doors. These are not sealed rooms, they are **bays sharing a back wall.** That is a good thing — a bay gives the framing benefit of a room without paying a room's art cost. But then the word "room" is misleading the discussion.

### 7.5 The whole business at a glance

research/01 §3, listing the mechanics players love most, puts these in 2nd and 3rd place: **layout design and venue expansion**, and **visible growth** — "watching your tiny shop turn into a bustling hub."

Single-room mode shuts that down. Fallout Shelter's documented dead end is exactly this: zoom out and you cannot touch, zoom in and you cannot see, and the two targets (the room, or the person inside it) land on the same pixel.

The one thing in the room layout's favour is here, in the measurement: **in the far mode a room is 106 dp.** So "pull back, but do not lose touchability" is possible — as long as what is touched is the room and not the table.

---

## 8. Its effect on the "owner, not chef" fantasy

docs/02 §1: "You are not the cook, you are the owner." docs/14: the owner cannot cook, contributes 1.4 person-days in the hall, and **can only be in one place at a time.**

| Argument | Direction |
|---|---|
| An owner thinks in **departments**, a chef thinks in **stations**. Kitchen, hall and sink are already docs/14's role split | **For** rooms |
| Two Point is the genre's purest "owner" fantasy and it is entirely room-based | **For** rooms |
| But our decision units are the menu, prices, the crew and station assignment; every one of them already has its own screen (docs/16 screens 8, 11, 12, 13) | **Against** rooms: a second representation of the same decision |
| If the owner can only be in one place at a time (docs/14), locking the camera to one room is **consistent** with the fantasy | For rooms, **but by burning the service tap budget** |
| docs/16's own conclusion: "The hall turns into decor; but we are playing the owner anyway, not the waiter" | For the fixed view |

Conclusion: rooms do not conflict with the fantasy, they support it. But what the fantasy requires is **not walking around inside the rooms**, it is **making decisions about the rooms.** The second one is an interface job.

---

## 9. Recommendation

**Adopt the room layout — as an art and expansion metaphor. Do not adopt it as the interaction model and the service camera.**

Concretely, five items:

| # | Decision | Reason |
|---|---|---|
| 1 | **Build the venue out of bays** (kitchen, sink, hall bays). A tier adds a new hall bay | The tier increments (+3, +3, +4) are already bay-sized. In `RoomLayout.cs` a table at tier 1 is 44 dp against 20 dp in the open hall; laying them out in a strip uses the landscape screen properly |
| 2 | **During service the camera is fixed, the whole restaurant is in frame, and the player's camera work is zero** | review/05 budgeted the camera at 2 taps; room navigation would have asked for 12. Cooking Fever, Good Pizza, CSD and Recettear are all fixed |
| 3 | **During service the table is not a touch target.** The primary target is the patience-queue chips in the bottom bar; a chip highlights its table and triggers the intervention | No layout makes a table 48 dp at tier 4. A chip can be at any dp you like. review/05 already proposed this; it is docs/16's third way |
| 4 | **A room-framed camera exists only on the layout-editing screen** (docs/16 screen 11), in the Kairosoft pattern: tap a tile → menu, ghost grid → tap again → confirm, plus redundant navigation (drag + pinch + a corner direction control) | There a table is ~45 dp, a table set ~100 dp, and the tap budget is not under pressure. The confirmation step closes the "overshoot the target + no undo" trap Two Point fell into on console |
| 5 | **The bays are not sealed rooms: no doors, no corridors, no path validity.** The waiter's walk is visual, not simulated | docs/14 says "no complex pathfinding". There is no venue in the core anyway. There is no point in buying Two Point's documented class of bugs |

**Its biggest cost:** during service the hall **falls to decor.** The player's attention shifts from the 3D scene we spend most of the art budget on to a strip of interface in the bottom bar. "Layout design" and "visible growth", ranked 2nd and 3rd in research/01's list of best-loved mechanics, drop into the background throughout service and pay out only on the layout screen and in the end-of-day frame. We are buying touchability by pushing the very thing we are standing on into the background.

The second cost: **the store room and the door/wall kit**, new art work with no simulation behind it. If the store cannot be given a job, it should come out of the layout.

---

## 10. Open items

1. **`RoomLayout.cs`'s console line must be written to a file.** The room dp values in this file are my calculation and my reading of the renders; they are not measured numbers. The decision rests on the signs, but the numbers should go into the document in their measured form.
2. **If the camera fit is corrected, how many dp does the open hall reach?** The `+ maxZ` term and including the wall height in the fit drop the frame to roughly 45% full. With a cropping camera I expect 15 dp → 22-25 dp. It cannot be known without measuring, and even if it will not reach 48 it could change the difference between B and C.
3. **What is the store room's job?** If it has none it should go. Can docs/12's stock and spoilage mechanic be given a visible counterpart?
4. **How many hall bay variants are needed?** Four identical bays read as copy-paste. Is it 3, is it 4, how many variants look sufficient?
5. **The camera state is not saved** (docs/23). Which bay will a player who leaves the layout screen and comes back wake up in? Should an exception be written?
6. **Does the chip bar conflict with the top bar?** review/05 proposed putting the intervention allowance and the day's progress in the top bar; chips are coming to the bottom bar. The two strips' combined share of the 393 dp landscape height has to be measured.
7. **What does a second branch (docs/02 §6, section 6) mean in a room layout?** Does the strip keep getting longer, or does it become a separate scene in the Cooking Fever pattern? Out of scope for the first release, but the layout decision should look at it.
8. **Restaurant Renovation should come off the reference list.** Verified: it is a match puzzle, not a management game.

---

## 11. Sources

**Standards and measurement**
- Google, touch target size: https://support.google.com/accessibility/android/answer/7101858
- Material Design accessibility: https://m2.material.io/design/usability/accessibility.html
- In-project: `unity/Assets/Lokanta/Editor/RestaurantScene.cs`, `unity/Assets/Lokanta/Editor/RoomLayout.cs`, `tools/art/out/unity/oda_*.png`

**Diner Dash and mobile service games**
- https://en.wikipedia.org/wiki/Diner_Dash
- https://en.wikipedia.org/wiki/Diner_Dash:_Hometown_Hero
- https://dinerdash.fandom.com/wiki/Walkthrough:Flo's_Diner_(Diner_Dash) (index excerpt)
- https://apps.apple.com/us/app/diner-dash-adventures/id1380831764
- https://www.levelwinner.com/diner-dash-adventures-beginners-guide-tips-cheats-strategies-to-restore-dinertown/
- https://www.nowf.com/guides/diner-dash-adventures-guide
- https://www.touchtapplay.com/diner-dash-adventures-cheats-tips-guide-to-pass-all-levels/

**Cook, Serve, Delicious!**
- https://en.wikipedia.org/wiki/Cook,_Serve,_Delicious!_2 , .../Cook,_Serve,_Delicious!_3
- https://store.steampowered.com/app/386620/Cook_Serve_Delicious_2/
- https://steamcommunity.com/app/386620/discussions/0/1520386297697292960/ (menu and station slots)
- https://www.choicestgames.com/2023/08/cook-serve-delicious-2-review.html (the Designer is cosmetic)
- https://www.pocketgamer.com/cook-serve-delicious-mobile/warning-android-cook-serve-delicious-users-the-game-is-getting-delisted-but-dont/

**Good Pizza, Great Pizza / Cooking Fever / Cooking Diary / Cooking Madness / Animal Restaurant**
- https://en.wikipedia.org/wiki/Good_Pizza,_Great_Pizza
- https://store.steampowered.com/app/770810/Good_Pizza_Great_Pizza__Cooking_Simulator_Game/
- https://noodlearcade.com/cooking-fever-ultimate-strategy-guide (four customer slots)
- https://www.pocketgamer.com/cooking-fever/cooking-fever-tips-and-tricks-how-to-survive-hells-kitchen/
- https://en.wikipedia.org/wiki/Cooking_Fever
- https://cookingdiary.game/game-guide/game-tips/tips-and-tricks
- https://play.google.com/store/apps/details?id=droidhang.twgame.restaurant
- https://animalrestaurant.fandom.com/wiki/Animal_Restaurant (the area list, index excerpt)
- https://www.levelwinner.com/animal-restaurant-beginners-guide-tips-cheats-strategies-to-grow-your-restaurant-business-fast/

**Shop simulations**
- https://store.steampowered.com/app/2670630/Supermarket_Simulator/
- https://supermarket-simulator.fandom.com/wiki/Growth , .../Storage (index excerpt)
- https://theguidehall.com/supermarket-simulator-how-unlock-storage/
- https://store.steampowered.com/app/3070070/TCG_Card_Shop_Simulator/
- https://tcgcardshopsimulator.wiki.gg/wiki/RENO_BIGG
- https://steamcommunity.com/app/3070070/discussions/0/4849903998512913531/ (the request for walls and partitions)
- https://en.wikipedia.org/wiki/Recettear:_An_Item_Shop%27s_Tale
- https://recettear.fandom.com/wiki/Merchant_Level (index excerpt)
- https://www.thegamer.com/moonlighter-shop-upgrades/ , https://moonlighter.fandom.com/wiki/Shop_Upgrades
- https://www.pocketgamer.com/moonlighter/moonlighter-hands-on-innovative-controls-and-great-design-updated/ (the touch redesign)
- https://toucharcade.com/2020/12/01/moonlighter-review-iphone-ipad-android/
- https://store.steampowered.com/app/1525700/Tavern_Master/ , https://steamcommunity.com/app/1525700/discussions/0/3202621452558674963/
- https://catcafemanager.wiki.gg/wiki/Design_Mode , https://steamcommunity.com/app/1354830/discussions/2/3830914078559477875/ (no interior walls)
- https://steamcommunity.com/app/1122340/discussions/0/3825289852122217751 (Chef Life's layout swap)
- https://www.thegamer.com/chef-life-a-restaurant-simulator-upgrade-decorate-restaurant/
- https://store.steampowered.com/app/2274620/Discounty/ , https://steamcommunity.com/app/2274620/discussions/0/601914904286416715/
- https://travellersrest.wiki.gg/wiki/Construction_Mode
- https://dave-the-diver.fandom.com/wiki/Bancho_Sushi (index excerpt) , https://www.pockettactics.com/dave-the-diver/mobile
- https://play.google.com/store/apps/details?id=com.zymobile.restaurant (Restaurant Renovation, a match puzzle)

**Two Point and PlateUp!**
- https://en.wikipedia.org/wiki/Two_Point_Hospital (the platform list, development difficulties)
- https://two-point-hospital.fandom.com/wiki/Rooms , .../Corridor , .../Door (index excerpt)
- https://www.gamepressure.com/two-point-hospital/hospital-rooms/zbb405 (minimum room sizes)
- https://gamefaqs.gamespot.com/pc/230622-two-point-hospital/faqs/76595/room-prestige
- http://www.nintendoworldreport.com/review/52928/two-point-hospital-switch-review (the virtual cursor scheme)
- https://godisageek.com/reviews/two-point-hospital-switch-review-nintendo-sega/ (no touch)
- http://www.nintendoworldreport.com/review/73084/two-point-museum-switch-2-review-in-progress (overshooting the target, no undo, small text)
- https://steamcommunity.com/app/535930/discussions/0/1737715419898938140/ (pathfinding bugs)
- https://www.twopointstudios.com/en/post/creativity-tools-breakdown-two-point-campus
- https://wiki.plateupgame.com/gameplay/Restaurant , .../Modding/GameDataObjects/LayoutProfile (the generator contract)
- https://wiki.plateupgame.com/gameplay/Headquarters , .../Automation
- https://github.com/Karl-HeinzSchneider/PlateUp-CameraPlus
- https://en.wikipedia.org/wiki/PlateUp!

**Kairosoft**
- https://www.gamezebo.com/walkthroughs/pocket-academy-walkthrough/ (pinch + the direction wheel)
- https://www.gamezebo.com/walkthroughs/mega-mall-story-walkthrough/ (drag + direction arrows, investment expansion)
- https://www.gamezebo.com/walkthroughs/kairobotica-walkthrough/ (tap an empty tile → menu)
- https://www.gamezebo.com/walkthroughs/the-sushi-spinnery-walkthrough/ (ghost grid, tap again, confirm)
- https://kairosoft.wiki.gg/wiki/Transcript:Manual_(Cafeteria_Nipponica) (the orange-framed area)
- https://kairosoft.wiki.gg/wiki/Hot_Springs_Story , https://gamefaqs.gamespot.com/iphone/618271-hot-springs-story/faqs/61941 (deeds, footprints, walking time)
- https://kairosoft.wiki.gg/wiki/Special_Rooms_(Dream_House_Days)
- https://toucharcade.com/2015/06/05/biz-builder-delux-review-like-several-kairosoft-games-stapled-together/ (the interface improvement)
- https://toucharcade.com/2023/06/27/dream-town-island-mobile-kairosoft-game-review-iphone-ipad-android/ (the remaining menu-depth complaint)
- https://higherplaingames.com/mobile/cafeteria-nipponica-review/ (zoom)
- https://www.whatsitlike.com.au/game-dev-story-switch-2-review/ (the in-game cursor)

**Room-based growth on a phone**
- https://damonwakes.wordpress.com/2016/03/26/touchscreen-troubles/ (Fallout Shelter's touch dead end)
- https://steamcommunity.com/app/588430/discussions/0/1319962514593528480/ (the automatic zoom complaint)
- https://gamerant.com/fallout-shelter-how-to-merge-rooms/
- https://en.wikipedia.org/wiki/Tiny_Tower
- https://www.couchclicker.com/complete-guide-to-hotel-empire-tycoon/ , https://www.levelwinner.com/hotel-empire-tycoon-beginners-guide-tips-cheats-strategies-to-grow-your-hotel-empire-fast/
