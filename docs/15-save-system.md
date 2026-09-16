# Save System

**Last updated:** 9 September 2026
**Register item:** A11
**Status:** Written, awaiting decision

---

## Why it matters this much

Three decisions made this system mandatory:

1. **The cuisine lock.** Every save is locked to a cuisine and does not change until the game ends.
2. **The more-than-one-slot condition.** With a single slot, a player who bought the second cuisine would have to delete their current game. That would produce refunds and bad reviews.
3. **Mobile sessions.** The game can close at any moment. The phone rings, the app is pushed to the background, the battery dies. The player must never lose progress.

---

## Four slots

| Rule | Decision |
|---|---|
| Number of slots | 4 |
| Cuisine per slot | One, chosen when it is created, unchanged afterwards |
| Relationship between slots | None, completely independent |
| Deleting | Free, but it asks for confirmation |
| More than one slot with the same cuisine | Allowed |

On the slot screen, every slot shows: the cuisine, the day, the till, the reputation and the plaque if one has been won.

### When the lock becomes final

The cuisine choice can be reset freely **until the end of the third day**. A player who realises twenty minutes later that they chose wrong is not trapped.

After the third day the lock is permanent. Changing it means opening a new slot.

---

## What is saved

A save is made of two parts: **the state** and **the seed**.

### The state

| Group | Contents |
|---|---|
| Time | Day number, season, which phase of the day |
| Money | The till, the loan balance, the number of instalments left |
| Reputation | The current value, the last seven days' history |
| Venue | The expansion tier, the layout, the equipment and upgrades owned |
| Staff | Each worker's identity, role, traits, morale, experience, station |
| Stock | Ingredient amounts, quality tiers, freshness counters |
| Menu | Today's menu, the price of every dish, the dishes unlocked |
| Customers | Regulars' progress, story scenes, the tab book |
| Bankruptcy | Which rung of the ladder it is on, and the ultimatum countdown if there is one |
| Service | If it is mid-service, the instantaneous state of the tables and the orders |

### The seed

The randomness seed and the number of steps consumed. Because the core is deterministic, **the state plus the seed is enough to reproduce the day exactly.**

What that buys:
- The save file stays small
- While debugging, a day can be replayed
- It is portable between platforms

---

## When it is saved

**The rule: on every phase transition and after every meaningful decision.**

| Moment | Saved |
|---|---|
| The market phase ended | Yes |
| The counter phase ended | Yes |
| Service started | Yes |
| During service, every 10 seconds | Yes |
| The end-of-day accounts closed | Yes |
| A purchase was made | Yes |
| The app was pushed to the background | Yes, immediately |

Thanks to the periodic save during service, even if the app closes the player loses at most ten seconds.

**No manual saving.** The player should never have to think about saving. On mobile, manual saving is a design mistake.

---

## Protection against corruption

A power cut or the app being killed while a save is being written is a real risk. Three measures:

1. **Atomic writes.** It is written to a temporary file first, the completion is verified, and then it is moved over the real file. A half-written file is never created.
2. **A backup of the previous save.** Every slot keeps two files: the current one and the previous one. If the current one cannot be read, it falls back to the previous one and the player is told.
3. **A checksum.** A checksum of the contents sits at the end of every file. If it does not match, the file is treated as corrupt.

The recovery order: the current file, then the backup, then an error message. **It never silently returns to a reset game.** The player is told plainly what happened.

---

## Version migration

When the game updates, old saves must still open. In a mobile game this is not negotiable, because the player does not choose the update.

```json
{
  "saveVersion": 3,
  "gameVersion": "0.4.1",
  "cuisine": "turk",
  "state": { }
}
```

**The rule:** a migration function is written for every version increment. Migrations are applied in order, so a save coming from version 1 passes through the 1→2 and 2→3 functions.

**Migration functions are never deleted.** Even five versions later, a save from the first version must still open.

**New fields must have a default.** If a field that is not in the old save is added, the migration function gives it a sensible value. For example, when the tab book was added, old saves carry on with an empty book.

---

## Cloud saves

Cloud saving sits behind a port interface. iCloud and Google Play on mobile, Steam Cloud on Steam.

| Rule | Decision |
|---|---|
| When it uploads | At the end of the day and when the app is pushed to the background |
| Conflict | The player is asked; the day and the till of both saves are shown |
| Automatic merging | None. In a management game, merging produces a wrong result |
| If the cloud is off | The game runs normally, only the local save |

Not choosing automatically in a conflict is deliberate. In a scenario where the player plays on two devices, the game should not decide which progress is lost.

---

## Its place in the architecture

The save system **does not belong to the core.** The core produces and reads the state, but it is the application layer that writes the file. And where the file is written sits behind a port interface.

```
Core            produces the state, keeps it serialisable
Application     decides when to save, applies the migration
ISaveStore      knows where to write the file
ICloudSave      uploads to the cloud
```

Thanks to this separation, the balance tool can also load a save and start a simulation from a particular day.

---

## Details awaiting a decision

1. Are four slots enough
2. Should the free-reset period for the cuisine lock be three days
3. Should the save interval during service be ten seconds
4. Would it be better, on a cloud conflict, to pick the further-advanced one instead of asking the player
