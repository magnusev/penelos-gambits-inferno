# Step 01 — Bot: Fix Stale Environment in CombatTick

## Problem

`rotation.cs` only creates `_environment` inside `OutOfCombatTick()` under a conditional branch.
`CombatTick()` calls `SendStateUpdate()` but never refreshes `_environment`, so every
STATE_UPDATE in combat sends the same stale snapshot.

## What to do

Call `RefreshEnvironment()` at the start of both `CombatTick()` and `OutOfCombatTick()`.
Remove the old `_environment = new Environment(...)` buried inside the Devotion Aura if-block.

## Files to change

- `PenelosGambits/rotation.cs`

## Manual test — Checkpoint

1. Load the rotation in the bot.
2. Connect a WebSocket client (e.g. `websocat ws://localhost:8082/`).
3. Stand still, observe STATE_UPDATE messages arriving with stable values.
4. Move your character, verify `player.isMoving` toggles to `true` in the next message.
5. Change target, verify `target.name` updates.
6. Enter combat, verify `combatTime` increments and `player.inCombat` is `true`.

**Pass**: fields update every tick reflecting live game state.
**Fail**: fields stay the same across ticks even when game state changes.
