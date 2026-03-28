# Step 05 — Engine: Message DTOs

## What to do

Create Kotlin data classes matching the OpenAPI spec, using `kotlinx.serialization`.

1. Create DTOs in `api/dto/` package:

   ```kotlin
   // Messages from Bot -> Engine
   @Serializable data class ConnectDto(val type: String, val character: String, val spec: String)
   @Serializable data class StateUpdateDto(
       val type: String,
       val timestamp: Long,
       val mapId: Int,
       val globalCooldown: Int,
       val combatTime: Int,
       val player: PlayerStateDto?,
       val target: TargetStateDto?,
       val group: GroupStateDto?,
       val bosses: List<BossStateDto>
   )
   @Serializable data class PlayerStateDto(val health: Int, val spec: String, val castingSpellId: Int, val inCombat: Boolean, val isMoving: Boolean)
   @Serializable data class TargetStateDto(val exists: Boolean, val name: String?, val health: Int, val castingSpellId: Int)
   @Serializable data class GroupStateDto(val type: String, val size: Int)
   @Serializable data class BossStateDto(val unitId: String, val name: String, val health: Int, val castingSpellId: Int)

   @Serializable data class QueryResponseDto(val type: String, val queryId: String, val result: Boolean, val data: JsonObject? = null)
   @Serializable data class ExecutionResultDto(val type: String, val commandId: String, val success: Boolean, val error: String? = null)
   @Serializable data class PongDto(val type: String)

   // Messages from Engine -> Bot
   @Serializable data class CommandDto(val type: String = "COMMAND", val commandId: String, val action: String, val spell: String? = null, val target: String? = null, val macro: String? = null)
   @Serializable data class QueryDto(val type: String = "QUERY", val queryId: String, val method: String, val params: JsonObject? = null)
   @Serializable data class PingDto(val type: String = "PING")
   ```

2. Create a helper to extract `type` from raw JSON:
   ```kotlin
   fun extractType(json: String): String? {
       val obj = Json.parseToJsonElement(json).jsonObject
       return obj["type"]?.jsonPrimitive?.contentOrNull
   }
   ```

## Files to create

- `server/.../api/dto/Messages.kt` (or split into multiple files)

## How to verify

Write a unit test that deserializes a real STATE_UPDATE JSON string from the bot.
Parse it into `StateUpdateDto` and assert fields are correct.
