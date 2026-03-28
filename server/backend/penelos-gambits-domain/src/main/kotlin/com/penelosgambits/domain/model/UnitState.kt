package com.penelosgambits.domain.model

/**
 * Generic unit representation for selectors and filters.
 * Can represent a player, target, boss, or group member.
 */
data class UnitState(
    val unitId: String,
    val name: String?,
    val health: Int,
    val castingSpellId: Int,
    val isDead: Boolean = false,
)

