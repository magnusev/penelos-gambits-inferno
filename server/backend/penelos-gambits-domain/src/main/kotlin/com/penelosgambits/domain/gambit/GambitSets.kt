package com.penelosgambits.domain.gambit

import com.penelosgambits.domain.condition.InCombatCondition
import com.penelosgambits.domain.condition.SpellReadyCondition
import com.penelosgambits.domain.condition.TargetExistsCondition
import com.penelosgambits.domain.selector.CurrentTargetSelector

/**
 * Starter gambit sets for initial testing.
 * These will be expanded per-spec as the system matures.
 */

/** Emergency gambits — evaluated before everything else. */
private val emergencyGambits = GambitSet(
    name = "Emergency",
    gambits = listOf(),
)

/** Filler / damage gambits — used as fallback when nothing higher-prio matches. */
private val fillerGambits = GambitSet(
    name = "Filler",
    gambits = listOf(),
)

/** Default gambit set — Holy Paladin starter rotation. */
val defaultGambitSet = GambitSet(
    name = "Default",
    before = emergencyGambits,
    gambits = listOf(
        GambitRule(
            priority = 1,
            name = "Holy Shock",
            conditions = listOf(InCombatCondition(), TargetExistsCondition(), SpellReadyCondition("Holy Shock")),
            selector = CurrentTargetSelector(),
            action = ActionIntent.Cast("Holy Shock"),
        ),
    ),
    fallback = fillerGambits,
)

