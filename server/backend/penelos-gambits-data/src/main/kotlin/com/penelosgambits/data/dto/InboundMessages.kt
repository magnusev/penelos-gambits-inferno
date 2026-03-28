package com.penelosgambits.data.dto

import kotlinx.serialization.Serializable
import kotlinx.serialization.json.JsonObject

// ─────────────────────────────────────────────
//  Messages from Bot → Engine (inbound)
// ─────────────────────────────────────────────

@Serializable
data class ConnectDto(
    val type: String,
    val character: String,
    val spec: String,
)

@Serializable
data class StateUpdateDto(
    val type: String,
    val timestamp: Long,
    val mapId: Int,
    val globalCooldown: Int,
    val combatTime: Int,
    val player: PlayerStateDto?,
    val target: TargetStateDto?,
    val group: GroupStateDto?,
    val bosses: List<BossStateDto>,
)

@Serializable
data class PlayerStateDto(
    val health: Int,
    val spec: String,
    val castingSpellId: Int,
    val inCombat: Boolean,
    val isMoving: Boolean,
)

@Serializable
data class TargetStateDto(
    val exists: Boolean,
    val name: String?,
    val health: Int,
    val castingSpellId: Int,
)

@Serializable
data class GroupStateDto(
    val type: String,
    val size: Int,
)

@Serializable
data class BossStateDto(
    val unitId: String,
    val name: String,
    val health: Int,
    val castingSpellId: Int,
)

@Serializable
data class QueryResponseDto(
    val type: String,
    val queryId: String,
    val result: Boolean,
    val data: JsonObject? = null,
)

@Serializable
data class ExecutionResultDto(
    val type: String,
    val commandId: String,
    val success: Boolean,
    val error: String? = null,
)

@Serializable
data class PongDto(
    val type: String,
)

