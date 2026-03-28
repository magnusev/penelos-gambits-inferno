package com.penelosgambits.domain.model

/**
 * Snapshot of the full game state received every tick from the bot.
 * Pure domain — no serialization annotations.
 */
data class TickState(
    val timestamp: Long,
    val mapId: Int,
    val globalCooldown: Int,
    val combatTime: Int,
    val player: PlayerState?,
    val target: TargetState?,
    val group: GroupInfo?,
    val bosses: List<BossState>,
)

data class PlayerState(
    val health: Int,
    val spec: String,
    val castingSpellId: Int,
    val inCombat: Boolean,
    val isMoving: Boolean,
)

data class TargetState(
    val exists: Boolean,
    val name: String?,
    val health: Int,
    val castingSpellId: Int,
)

data class GroupInfo(
    val type: String,
    val size: Int,
)

data class BossState(
    val unitId: String,
    val name: String,
    val health: Int,
    val castingSpellId: Int,
)

