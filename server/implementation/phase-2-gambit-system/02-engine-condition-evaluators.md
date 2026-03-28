# Step 02 — Engine: Condition Evaluators

## What to do

Create the `ConditionEvaluator` interface and first batch of implementations.

1. Define the interface in `domain/condition/`:
   ```kotlin
   fun interface ConditionEvaluator {
       suspend fun isMet(context: TickContext): Boolean
   }
   ```

2. Implement starting set (port from old system):

   | Class | Condition | Uses Query? |
   |-------|-----------|-------------|
   | `AlwaysCondition` | Always true | No |
   | `InCombatCondition` | Player is in combat | No (from state) |
   | `PlayerHealthBelowCondition(threshold)` | Player HP < X% | No (from state) |
   | `TargetExistsCondition` | Target exists | No (from state) |
   | `HasNotBuffCondition(unit, buff)` | Unit missing buff | Yes (QUERY) |
   | `TargetHasNotDebuffCondition(debuff)` | Target missing debuff | Yes (QUERY) |
   | `SpellReadyCondition(spell)` | Spell off cooldown | Yes (QUERY) |
   | `BossCastingCondition` | Any boss is casting | No (from state) |

3. Each condition is a small composable class. No inheritance.

## Files to create

- `server/.../domain/condition/ConditionEvaluator.kt` (interface)
- `server/.../domain/condition/AlwaysCondition.kt`
- `server/.../domain/condition/InCombatCondition.kt`
- `server/.../domain/condition/PlayerHealthBelowCondition.kt`
- etc. (one file per condition, or group small ones)

## How to verify

Unit tests with mock `TickContext`:
- `AlwaysCondition().isMet(ctx)` returns true.
- `InCombatCondition().isMet(ctx)` returns true when `player.inCombat = true`.
- `HasNotBuffCondition("player", "Earth Shield").isMet(ctx)` calls `query("HasBuff", ...)`,
  returns true when query result is false.
