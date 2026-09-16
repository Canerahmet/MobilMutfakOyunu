# Layered Architecture

**Last updated:** 9 September 2026
**Purpose:** to split the game into layers where making a change later is cheap. In particular, to make the move from mobile to Steam a small and predictable job.

`Lokanta` is used as the working name. The real name will be decided later.

---

## The basic rule

**The simulation does not know about Unity.**

All of the game's rules, its economy, its customers, its staff and its going-under logic are written in pure C#. There is not a single `UnityEngine` reference inside it.

This one rule brings all of the following for free:

- You can balance the economy by simulating thousands of days in a console, without opening Unity.
- You can verify the rules with unit tests.
- The Steam release changes the interface and the platform layer; it does not touch a single line of the rules.
- If you later want to change engine, the brain of the game stays in your hands.

Keeping this rule on paper does not work. Below is how the compiler is made to enforce it.

---

## The layers

The dependency always points downward. An upper layer knows the layer below it; a lower layer never knows the one above.

| Layer | What it does | Unity reference |
|---|---|---|
| **Presentation** | 3D scene, character animation, camera, sound, effects | Yes |
| **Interface** | Market, counter, service display, books screens | Yes |
| **Application** | Runs the flow of the day, loads the save, passes commands to the core | Yes |
| **Core** | All of the game rules and the simulation | **No** |
| **Content** | Recipes, ingredients, equipment, staff, customer types | No |
| **Ports** | The interface definitions of the platform capabilities | No |
| **Platform** | The mobile and Steam implementations of the ports | Yes |

---

## The compiler enforcing the layers

The mechanism that provides this in Unity is **Assembly Definition** files. Every layer is its own compilation unit and can only reference the layers it is allowed to.

```
Lokanta.Core            → references nothing, the Unity API is switched off
Lokanta.Content         → Core
Lokanta.Ports           → Core
Lokanta.App             → Core, Content, Ports
Lokanta.View            → App, Core
Lokanta.UI              → App, Core
Lokanta.Platform.Mobile → Ports        (included only in the mobile build)
Lokanta.Platform.Steam  → Ports        (included only in the Steam build)
Lokanta.Core.Tests      → Core
```

When creating the `Lokanta.Core` compilation unit, **switching the Unity references off** is the critical step. Once you have done that, the moment you try to leak a `GameObject` into the core by accident the project stops compiling. The discipline comes from the compiler, not from you.

That one setting is the thing that stops a layered architecture from staying on paper.

---

## Ports: the only difference between mobile and Steam

Everything that changes with the platform goes behind an interface. The game code only calls the interface; it does not know the implementation.

| Port | Mobile implementation | Steam implementation |
|---|---|---|
| `IInputSource` | Touch, drag and drop | Mouse, keyboard, gamepad |
| `ISaveStore` | Device storage | Local file |
| `ICloudSave` | iCloud, Google Play | Steam Cloud |
| `IStoreFront` | In-app purchase | None, the game is sold up front |
| `IAdProvider` | Rewarded ads | Empty implementation, does nothing |
| `IAchievements` | Game Center, Play Games | Steam Achievements |
| `IAnalytics` | Mobile analytics | Optional |

**Why this matters so much:** if ad and purchase calls get scattered through the game code, the Steam release turns into a nightmare. Behind a port, the Steam build simply hands over an empty ad implementation and the matter is closed.

That is also why the revenue model decision does not affect the architecture. It can be free plus an unlock on mobile and sold up front on Steam. Both run on top of the same core.

---

## The input layer: two schemes from the start

Because Steam is a target, input has to be abstracted from the start. Unity's Input System package already supports this with the **control scheme** concept. Define two schemes from the beginning:

- **Touch:** drag and drop, single tap
- **Desktop:** mouse, keyboard shortcuts, gamepad

The game code reads intent through `IInputSource`. For example, the command "seat this customer at that table" does not know whether it came from a finger or a mouse.

Adding it later is expensive, because all of the interaction code has to be rewritten.

---

## Screen ratio

Mobile landscape is roughly 19.5:9, Steam is 16:9 and wider. The interface is designed to the narrowest safe area and breathes on a wide screen.

The practical rule: information-dense panels are pinned to the edges, the play area stretches in the middle. Do not use fixed pixel positions.

---

## Content is held as data, not as code

Recipes, ingredients, equipment prices, staff archetypes and upgrade costs live in **data files**.

Recommendation: let the source of truth be JSON files, with a loader on the Unity side that reads them.

**Why not ScriptableObject:** ScriptableObject is very comfortable in the Unity editor but it ties you to Unity. Your balance tool, which runs in a console, cannot read it. JSON is read by both the game and the balance tool. If you want editor comfort you write a thin wrapper on top of it.

Not having to compile code to make a balance change saves a lot of time in a one-person project.

---

## The event flow is one-way

The core raises events, the presentation listens. The presentation **never changes the core's state directly**, it only sends commands.

```
The player taps
  → the interface produces a command
    → the application passes the command to the core
      → the core changes state and raises an event
        → the presentation and the interface listen to the event and update themselves
```

Without this rule the interface and the simulation get tangled together over time and debugging becomes impossible. This is the most common rot in management games.

---

## The outer face of the core

Roughly this kind of surface is the target. The details will change, the shape will not.

```csharp
var day = sim.BeginDay();

sim.Market.Buy(ingredientId, quantity);
sim.Menu.Set(dishId, price);
sim.Staff.Assign(staffId, Station.Kitchen);

sim.OpenService();
sim.Tick(deltaTime);          // deterministic
sim.Intervene(tableId, InterventionKind.Apology);

DayReport report = sim.CloseDay();
```

`Tick` must be deterministic. The same starting state and the same random seed must give the same result.

**What that buys:** the save file becomes nothing but state plus seed, and it is portable between platforms. When debugging you can replay a day. The balance tool produces trustworthy results.

---

## The headless balance tool

A small console application that references `Lokanta.Core`. It simulates hundreds of games with different player strategies and writes the result out as CSV.

What gets measured:

- How many players go under, and on which day
- When the economy becomes irrelevant, that is, when money piles up and loses its meaning
- Whether a player who keeps prices permanently high wins
- How long a player who never intervenes lasts

This tool catches the two big causes of death in the research early: failure being frustrating, and the economy getting easy and meaningless.

**This is Phase 0.** It gets written before Unity is touched.

---

## The art has to be layered too

Visual assets have to stay replaceable as well.

- Every object in its own prefab, obeying a shared axis and scale rule.
- The vertical slice is made with **primitive boxes**. A table is a cube, a customer is a capsule.
- When the real model arrives the contents of the prefab change; the game code does not.

In a one-person project this stops the art from holding the game design hostage. That was the clearest lesson in the research: art is not a saviour, it is a multiplier. First the number being multiplied has to be right.

---

## A warning about over-engineering

Layering is done at the seams that actually matter. Writing an interface for every class drowns a one-person project.

**The seams to abstract:**

1. The boundary between the core and Unity
2. The platform ports
3. The boundary between content data and code

**What not to abstract:** everything else. You do not need an interface for a table class. You extract one when the need arises.

---

## The first three steps

1. **The core and the balance tool.** No Unity. The maths of one day runs in a console, and a thirty-day simulation comes out balanced.
2. **The Unity skeleton.** The compilation units are set up, the ports are defined, the mobile implementations are written. The scene is made of primitive boxes.
3. **The vertical slice.** One restaurant, six dishes, three staff, ten days. Playable and testable.

The Steam implementations are not written in these three steps. Because the ports are defined, adding them when their turn comes is a small job.
