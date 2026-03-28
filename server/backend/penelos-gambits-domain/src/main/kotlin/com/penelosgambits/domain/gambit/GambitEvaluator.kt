package com.penelosgambits.domain.gambit

import com.penelosgambits.domain.model.TickContext

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
    val matched = findFirstMatchingGambit(gambitSet, context)
    if (matched != null) return matched

    // 3. Fallback-chain
    gambitSet.fallback?.let { fallback ->
        val result = evaluate(fallback, context)
        if (result != EvaluationResult.NONE) return result
    }

    // 4. Nothing matched
    return EvaluationResult.NONE
}

private suspend fun findFirstMatchingGambit(
    gambitSet: GambitSet,
    context: TickContext,
): EvaluationResult? {
    for (gambit in gambitSet.gambits.sortedBy { it.priority }) {
        val result = tryEvaluateGambit(gambit, context)
        if (result != null) return result
    }
    return null
}

private suspend fun tryEvaluateGambit(
    gambit: GambitRule,
    context: TickContext,
): EvaluationResult? {
    val conditionsMet = gambit.conditions.all { it.isMet(context) }
    if (!conditionsMet) return null

    val target = gambit.selector.select(context) ?: return null

    val blocked = gambit.canExecuteCheck?.invoke(context, target) == false
    if (blocked) return null

    return EvaluationResult(
        gambitName = gambit.name,
        action = gambit.action,
        target = target,
    )
}

