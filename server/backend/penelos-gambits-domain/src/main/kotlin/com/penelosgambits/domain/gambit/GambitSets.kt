package com.penelosgambits.domain.gambit

import com.penelosgambits.domain.condition.AlwaysCondition
import com.penelosgambits.domain.condition.BossCastingCondition
import com.penelosgambits.domain.condition.InCombatCondition
import com.penelosgambits.domain.condition.PlayerHealthBelowCondition
import com.penelosgambits.domain.condition.SpellReadyCondition
import com.penelosgambits.domain.condition.TargetExistsCondition
import com.penelosgambits.domain.condition.TargetHasNotDebuffCondition
import com.penelosgambits.domain.selector.CurrentTargetSelector
import com.penelosgambits.domain.selector.PlayerSelector

/**
 * Starter gambit sets for initial testing.
 * These will be expanded per-spec as the system matures.
 */

/** Emergency gambits — evaluated before everything else. */
private val emergencyGambits = GambitSet(
    name = "Emergency",
    gambits = listOf(
        GambitRule(
            priority = 1,
            name = "Boss Interrupt",
            conditions = listOf(InCombatCondition(), BossCastingCondition()),
            selector = CurrentTargetSelector(),
            action = ActionIntent.Cast("Rebuke"),
        ),
        GambitRule(
            priority = 2,
            name = "Emergency Self-Heal",
            conditions = listOf(InCombatCondition(), PlayerHealthBelowCondition(30)),
            selector = PlayerSelector(),
            action = ActionIntent.Cast("Word of Glory"),
        ),
    ),
)

/** Filler / damage gambits — used as fallback when nothing higher-prio matches. */
private val fillerGambits = GambitSet(
    name = "Filler",
    gambits = listOf(
        GambitRule(
            priority = 1,
            name = "Judgment",
            conditions = listOf(InCombatCondition(), TargetExistsCondition(), SpellReadyCondition("Judgment")),
            selector = CurrentTargetSelector(),
            action = ActionIntent.Cast("Judgment"),
        ),
        GambitRule(
            priority = 2,
            name = "Crusader Strike",
            conditions = listOf(InCombatCondition(), TargetExistsCondition(), SpellReadyCondition("Crusader Strike")),
            selector = CurrentTargetSelector(),
            action = ActionIntent.Cast("Crusader Strike"),
        ),
        GambitRule(
            priority = 99,
            name = "Idle",
            conditions = listOf(AlwaysCondition()),
            selector = PlayerSelector(),
            action = ActionIntent.None,
        ),
    ),
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
        GambitRule(
            priority = 2,
            name = "Maintain Consecration",
            conditions = listOf(InCombatCondition(), TargetExistsCondition(), SpellReadyCondition("Consecration")),
            selector = CurrentTargetSelector(),
            action = ActionIntent.Cast("Consecration"),
        ),
    ),
    fallback = fillerGambits,
)

