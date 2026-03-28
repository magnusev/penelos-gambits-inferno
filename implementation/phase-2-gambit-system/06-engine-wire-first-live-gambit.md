# Step 06 — Engine: Wire It All Together — First Live Gambit

## What to do

This is the integration step. Connect everything and run a real gambit in-game.

1. Create a simple `DefaultGambitSetPicker`:
   ```kotlin
   class DefaultGambitSetPicker : GambitSetPicker {
       override fun pick(mapId: Int): GambitSet = defaultGambitSet
   }
   ```

2. Create a starter gambit set for your spec (e.g. Holy Paladin):
   ```kotlin
   val defaultGambitSet = GambitSet(
       name = "Default",
       before = emergencyGambits,     // interrupt, emergency heal
       gambits = listOf(
           GambitRule(1, "Holy Shock low ally",
               conditions = listOf(/* ally < 50% */),
               selector = FilterPipelineSelector(
                   unitProvider = { ctx -> ctx.allGroupUnits() },
                   filters = listOf(IsNotDeadFilter, LowestHealthUnderThresholdFilter(50))
               ),
               action = ActionIntent.Cast("Holy Shock")
           ),
           // ... more gambits
       ),
       fallback = fillerDamageGambits  // Crusader Strike, Judgment, etc.
   )
   ```

3. In the main tick loop (after receiving STATE_UPDATE):
   ```kotlin
   val tickState = mapper.toTickState(stateUpdate)
   val context = TickContext(tickState, queryPort)
   val gambitSet = picker.pick(tickState.mapId)
   val result = evaluator.evaluate(gambitSet, context)
   val command = result.toCommandDto()
   botConnection.send(Json.encodeToString(command))
   ```

4. Handle EXECUTION_RESULT: log success/failure.

## Files to create / change

- `server/.../domain/gambit/DefaultGambitSetPicker.kt` (new)
- `server/.../domain/gambit/GambitSets.kt` (new, define gambit sets)
- `server/.../service/Application.kt` (wire the tick loop)
- `server/.../api/websocket/BotConnection.kt` (add tick processing)

## Manual test — Checkpoint (Full Phase 2 Verification)

This is the big one. Full loop in-game:

1. Start bot, start engine.
2. Enter combat with a target dummy or easy mob.
3. Observe engine logs:
   - STATE_UPDATE received
   - Gambit evaluation: which gambit matched and why
   - COMMAND sent (e.g. `CAST Holy Shock on target`)
4. Observe bot logs:
   - COMMAND received
   - Spell cast attempted
   - EXECUTION_RESULT sent (success/failure)
5. Verify the character actually performs the action in-game.
6. Try different scenarios:
   - Low health ally (if healing spec) triggers heal gambit
   - Boss casting triggers interrupt gambit
   - Map change triggers GambitSetPicker swap
7. Stop engine mid-combat: bot safe-idles (does nothing).
8. Restart engine: resumes casting within seconds.

**Pass**: the bot plays the game autonomously using gambit rules defined in Kotlin.

This completes Phase 2. You now have a working Gambit System.
