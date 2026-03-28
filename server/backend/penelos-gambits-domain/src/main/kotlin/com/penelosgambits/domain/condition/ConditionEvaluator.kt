package com.penelosgambits.domain.condition

import com.penelosgambits.domain.model.TickContext

/**
 * A composable condition that can be evaluated against the current tick context.
 * Each condition is a small, focused predicate — compose them to build complex rules.
 */
fun interface ConditionEvaluator {
    suspend fun isMet(context: TickContext): Boolean
}

