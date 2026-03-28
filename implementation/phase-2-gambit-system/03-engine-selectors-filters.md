# Step 03 — Engine: Selectors + UnitFilter Pipeline

## What to do

1. Define interfaces in `domain/selector/`:
   ```kotlin
   fun interface TargetSelector {
       suspend fun select(context: TickContext): UnitState?
   }

   fun interface UnitFilter {
       fun filter(units: List<UnitState>): List<UnitState>
   }
   ```

2. Implement static selectors:
   - `PlayerSelector` — returns player as UnitState
   - `CurrentTargetSelector` — returns current target
   - `BossSelector(unitId)` — returns specific boss

3. Implement filter primitives (port from old system):
   - `IsNotDeadFilter`
   - `IsInRangeFilter(spell)` — needs query (SpellInRange)
   - `LowestHealthFilter` — sorts by health, returns first
   - `LowestHealthUnderThresholdFilter(threshold)` — filters HP < X, sorts, returns first
   - `HasDebuffFilter(debuff)` — keeps units with debuff
   - `HasNotBuffFilter(buff)` — keeps units without buff

4. Implement `FilterPipelineSelector`:
   ```kotlin
   class FilterPipelineSelector(
       private val unitProvider: (TickContext) -> List<UnitState>,
       private val filters: List<UnitFilter>
   ) : TargetSelector {
       override suspend fun select(context: TickContext): UnitState? {
           var units = unitProvider(context)
           for (filter in filters) {
               units = filter.filter(units)
               if (units.isEmpty()) return null
           }
           return units.firstOrNull()
       }
   }
   ```

5. Add logging to `FilterPipelineSelector` for debugging:
   log each filter step showing candidates before/after.

## Files to create

- `server/.../domain/selector/TargetSelector.kt`
- `server/.../domain/selector/UnitFilter.kt`
- `server/.../domain/selector/PlayerSelector.kt`
- `server/.../domain/selector/FilterPipelineSelector.kt`
- `server/.../domain/selector/filters/IsNotDeadFilter.kt`
- `server/.../domain/selector/filters/LowestHealthUnderThresholdFilter.kt`
- etc.

## How to verify

Unit tests:
- `IsNotDeadFilter` removes dead units.
- `LowestHealthUnderThresholdFilter(50)` with units at [80, 30, 45] returns [30].
- `FilterPipelineSelector` with [IsNotDead, LowestHealth] picks lowest alive unit.
