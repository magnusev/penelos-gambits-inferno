package com.penelosgambits.domain.condition

import com.penelosgambits.domain.model.TickContext

// ─────────────────────────────────────────────
//  Query-based conditions (send QUERY to bot)
// ─────────────────────────────────────────────

/** True when [unit] does NOT have [buff]. Sends a HasBuff query to the bot. */
class HasNotBuffCondition(
    private val unit: String,
    private val buff: String,
) : ConditionEvaluator {
    override suspend fun isMet(context: TickContext): Boolean {
        val result = context.query("HasBuff", mapOf("unit" to unit, "buff" to buff, "byPlayer" to true))
        return !result.success
    }
}

/** True when the current target does NOT have [debuff]. Sends a HasDebuff query to the bot. */
class TargetHasNotDebuffCondition(
    private val debuff: String,
) : ConditionEvaluator {
    override suspend fun isMet(context: TickContext): Boolean {
        val result = context.query("HasDebuff", mapOf("unit" to "target", "debuff" to debuff, "byPlayer" to true))
        return !result.success
    }
}

/** True when [spell] is off cooldown and usable. Sends a SpellReady query to the bot. */
class SpellReadyCondition(
    private val spell: String,
) : ConditionEvaluator {
    override suspend fun isMet(context: TickContext): Boolean {
        val result = context.query("CanCast", mapOf("spell" to spell))
        return result.success
    }
}

