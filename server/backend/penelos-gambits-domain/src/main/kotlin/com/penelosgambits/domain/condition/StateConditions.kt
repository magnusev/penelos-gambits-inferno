package com.penelosgambits.domain.condition

import com.penelosgambits.domain.model.TickContext

// ─────────────────────────────────────────────
//  State-based conditions (no query needed)
// ─────────────────────────────────────────────

/** Always true — useful as a fallback / default gambit. */
class AlwaysCondition : ConditionEvaluator {
    override suspend fun isMet(context: TickContext): Boolean = true
}

/** True when the player is currently in combat. */
class InCombatCondition : ConditionEvaluator {
    override suspend fun isMet(context: TickContext): Boolean =
        context.state.player?.inCombat == true
}

/** True when the player's health is below [threshold] percent. */
class PlayerHealthBelowCondition(private val threshold: Int) : ConditionEvaluator {
    override suspend fun isMet(context: TickContext): Boolean {
        val health = context.state.player?.health ?: return false
        return health < threshold
    }
}

/** True when the player has a target. */
class TargetExistsCondition : ConditionEvaluator {
    override suspend fun isMet(context: TickContext): Boolean =
        context.state.target?.exists == true
}

/** True when any boss in the state snapshot is currently casting. */
class BossCastingCondition : ConditionEvaluator {
    override suspend fun isMet(context: TickContext): Boolean =
        context.state.bosses.any { it.castingSpellId != 0 }
}

