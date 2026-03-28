package com.penelosgambits.domain.gambit

import com.penelosgambits.domain.model.TickContext
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

/**
 * Evaluates a [GambitSet] against a [TickContext] and returns the first matching gambit's action.
 *
 * Evaluation order:
 * 1. before-chain (recursively)
 * 2. This set's own gambits (sorted by priority ascending)
 * 3. fallback-chain (recursively)
 * 4. [EvaluationResult.NONE] if nothing matched
 */
suspend fun evaluate(gambitSet: GambitSet, context: TickContext): EvaluationResult {
    // 1. Before-chain
    gambitSet.before?.let { before ->
        val result = evaluate(before, context)
        if (result != EvaluationResult.NONE) return result
    }

    // 2. This set's gambits, sorted by priority (lower = higher priority)
    for (gambit in gambitSet.gambits.sortedBy { it.priority }) {
        val conditionsMet = gambit.conditions.all { it.isMet(context) }
        if (!conditionsMet) continue

        val target = gambit.selector.select(context) ?: continue

        if (gambit.canExecuteCheck != null && !gambit.canExecuteCheck.invoke(context, target)) {
            continue
        }

        return EvaluationResult(
            gambitName = gambit.name,
            action = gambit.action,
            target = target,
        )
    }

    // 3. Fallback-chain
    gambitSet.fallback?.let { fallback ->
        val result = evaluate(fallback, context)
        if (result != EvaluationResult.NONE) return result
    }

    // 4. Nothing matched
    return EvaluationResult.NONE
}

