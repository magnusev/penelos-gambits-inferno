package com.penelosgambits.domain.gambit

import com.penelosgambits.domain.model.UnitState

/**
 * Result of evaluating a gambit set against a tick context.
 */
data class EvaluationResult(
    val gambitName: String?,
    val action: ActionIntent,
    val target: UnitState?,
) {
    companion object {
        val NONE = EvaluationResult(gambitName = null, action = ActionIntent.None, target = null)
    }
}

