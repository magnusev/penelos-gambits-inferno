# Step 04 — Engine: GambitRule + GambitSet + Evaluation Loop

## What to do

1. Define `GambitRule` in `domain/gambit/`:
   ```kotlin
   data class GambitRule(
       val priority: Int,
       val name: String,
       val conditions: List<ConditionEvaluator>,
       val selector: TargetSelector,
       val action: ActionIntent,
       val canExecuteCheck: (suspend (TickContext, UnitState) -> Boolean)? = null
   )

   sealed class ActionIntent {
       data class Cast(val spell: String) : ActionIntent()
       data class Macro(val macro: String) : ActionIntent()
       data object None : ActionIntent()
   }
   ```

2. Define `GambitSet`:
   ```kotlin
   data class GambitSet(
       val name: String,
       val gambits: List<GambitRule>,
       val before: GambitSet? = null,
       val fallback: GambitSet? = null
   )
   ```

3. Define `GambitSetPicker`:
   ```kotlin
   interface GambitSetPicker {
       fun pick(mapId: Int): GambitSet
   }
   ```

4. Implement evaluation in `domain/gambit/GambitEvaluator.kt`:
   ```kotlin
   suspend fun evaluate(gambitSet: GambitSet, context: TickContext): EvaluationResult {
       // 1. Check before-chain
       // 2. Iterate gambits by priority, check conditions + canExecute
       // 3. Check fallback chain
       // 4. Return NONE if nothing matched
   }
   ```

5. Create a hardcoded test gambit set for verification:
   ```kotlin
   val testGambitSet = GambitSet(
       name = "Test",
       gambits = listOf(
           GambitRule(1, "Always do nothing",
               conditions = listOf(AlwaysCondition()),
               selector = PlayerSelector(),
               action = ActionIntent.None
           )
       )
   )
   ```

## Files to create

- `server/.../domain/gambit/GambitRule.kt`
- `server/.../domain/gambit/ActionIntent.kt`
- `server/.../domain/gambit/GambitSet.kt`
- `server/.../domain/gambit/GambitSetPicker.kt`
- `server/.../domain/gambit/GambitEvaluator.kt`

## How to verify — Checkpoint (unit tests)

Write BehaviorSpec tests:
- Given a gambit set with 3 gambits at priority 1, 2, 3
  When priority-1 conditions are met and action is executable
  Then priority-1 gambit is selected.

- Given priority-1 conditions met but canExecute fails
  When evaluated
  Then priority-2 is selected instead.

- Given a gambit set with before-chain
  When before-chain has a matching gambit
  Then before-chain gambit wins over main gambits.

- Given no gambits match
  When evaluated
  Then result is NONE.
