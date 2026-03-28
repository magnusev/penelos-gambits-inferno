package com.penelosgambits.domain.selector

import com.penelosgambits.domain.model.TickContext
import com.penelosgambits.domain.model.UnitState

/** Returns the player as a UnitState. */
class PlayerSelector : TargetSelector {
    override suspend fun select(context: TickContext): UnitState? {
        val player = context.state.player ?: return null
        return UnitState(
            unitId = "player",
            name = null,
            health = player.health,
            castingSpellId = player.castingSpellId,
            isDead = player.health <= 0,
        )
    }
}

/** Returns the current target as a UnitState. */
class CurrentTargetSelector : TargetSelector {
    override suspend fun select(context: TickContext): UnitState? {
        val target = context.state.target ?: return null
        if (!target.exists) return null
        return UnitState(
            unitId = "target",
            name = target.name,
            health = target.health,
            castingSpellId = target.castingSpellId,
            isDead = target.health <= 0,
        )
    }
}

/** Returns a specific boss by [unitId] (e.g. "boss1"). */
class BossSelector(private val unitId: String) : TargetSelector {
    override suspend fun select(context: TickContext): UnitState? {
        val boss = context.state.bosses.find { it.unitId == unitId } ?: return null
        return UnitState(
            unitId = boss.unitId,
            name = boss.name,
            health = boss.health,
            castingSpellId = boss.castingSpellId,
            isDead = boss.health <= 0,
        )
    }
}

