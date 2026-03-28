package com.penelosgambits.domain.gambit

import com.penelosgambits.domain.condition.ConditionEvaluator
import com.penelosgambits.domain.model.TickContext
import com.penelosgambits.domain.model.UnitState
import com.penelosgambits.domain.selector.TargetSelector

/**
 * A single gambit rule: "when [conditions] are met, use [action] on the unit from [selector]".
 *
 * @param priority Lower number = higher priority (evaluated first).
 * @param name Human-readable label for logging/debugging.
 * @param conditions All conditions must be met for this gambit to fire.
 * @param selector Picks the target unit for the action.
 * @param action What to do (Cast, Macro, or None).
 * @param canExecuteCheck Optional extra check with the selected unit (e.g. range check).
 */
data class GambitRule(
    val priority: Int,
    val name: String,
    val conditions: List<ConditionEvaluator>,
    val selector: TargetSelector,
    val action: ActionIntent,
    val canExecuteCheck: (suspend (TickContext, UnitState) -> Boolean)? = null,
)

