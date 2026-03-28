# Step 01 — Engine: Domain Model

## What to do

Create the core domain types that the gambit system operates on. These have NO external
dependencies (no Ktor, no serialization annotations).

1. Create domain model classes in `domain/model/`:
   ```kotlin
   data class TickState(
       val timestamp: Long,
       val mapId: Int,
       val globalCooldown: Int,
       val combatTime: Int,
       val player: PlayerState?,
       val target: TargetState?,
       val group: GroupInfo?,
       val bosses: List<BossState>
   )

   data class PlayerState(val health: Int, val spec: String, val castingSpellId: Int, val inCombat: Boolean, val isMoving: Boolean)
   data class TargetState(val exists: Boolean, val name: String?, val health: Int, val castingSpellId: Int)
   data class GroupInfo(val type: String, val size: Int)
   data class BossState(val unitId: String, val name: String, val health: Int, val castingSpellId: Int)

   // Generic unit representation for selectors/filters
   data class UnitState(val unitId: String, val name: String?, val health: Int, val castingSpellId: Int, val isDead: Boolean = false)
   ```

2. Create port interfaces in `domain/port/`:
   ```kotlin
   interface GameQueryPort {
       suspend fun query(method: String, params: Map<String, Any> = emptyMap()): QueryResult
   }

   data class QueryResult(val success: Boolean, val data: Map<String, Any?> = emptyMap())
   ```

3. Create a mapper in the API layer to convert `StateUpdateDto` -> `TickState`.

4. Create `TickContext` which wraps state + query port + per-tick cache:
   ```kotlin
   class TickContext(
       val state: TickState,
       private val queryPort: GameQueryPort
   ) {
       private val cache = mutableMapOf<String, QueryResult>()

       suspend fun query(method: String, params: Map<String, Any> = emptyMap()): QueryResult {
           val key = buildCacheKey(method, params)
           return cache.getOrPut(key) { queryPort.query(method, params) }
       }
   }
   ```

## Files to create

- `server/.../domain/model/TickState.kt`
- `server/.../domain/model/UnitState.kt`
- `server/.../domain/port/GameQueryPort.kt`
- `server/.../domain/model/TickContext.kt`
- `server/.../api/mapper/StateMapper.kt`

## How to verify

Unit tests: create a `TickState` from hardcoded values, verify fields.
Unit test: `TickContext` caches a query result (mock `GameQueryPort`, call twice, verify
port called only once).
