package com.penelosgambits.domain.gambit

import com.penelosgambits.domain.condition.AlwaysCondition
import com.penelosgambits.domain.condition.ConditionEvaluator
import com.penelosgambits.domain.condition.InCombatCondition
import com.penelosgambits.domain.model.PlayerState
import com.penelosgambits.domain.model.TickContext
import com.penelosgambits.domain.model.TickState
import com.penelosgambits.domain.model.UnitState
import com.penelosgambits.domain.port.GameQueryPort
import com.penelosgambits.domain.selector.PlayerSelector
import io.kotest.core.spec.style.BehaviorSpec
import io.kotest.matchers.shouldBe
import io.mockk.mockk

private fun tickState(inCombat: Boolean = true) = TickState(
    timestamp = 1L,
    mapId = 2549,
    globalCooldown = 0,
    combatTime = if (inCombat) 5000 else 0,
    player = PlayerState(95, "Holy", 0, inCombat, false),
    target = null,
    group = null,
    bosses = emptyList(),
)

private fun context(state: TickState = tickState()): TickContext =
    TickContext(state, mockk<GameQueryPort>())

/** A condition that always fails. */
private class NeverCondition : ConditionEvaluator {
    override suspend fun isMet(context: TickContext): Boolean = false
}

class GambitEvaluatorTest : BehaviorSpec({

    Given("a gambit set with 3 gambits at priority 1, 2, 3") {
        val gambitSet = GambitSet(
            name = "Test",
            gambits = listOf(
                GambitRule(1, "Priority1-Cast", listOf(AlwaysCondition()), PlayerSelector(), ActionIntent.Cast("Holy Shock")),
                GambitRule(2, "Priority2-Cast", listOf(AlwaysCondition()), PlayerSelector(), ActionIntent.Cast("Flash of Light")),
                GambitRule(3, "Priority3-None", listOf(AlwaysCondition()), PlayerSelector(), ActionIntent.None),
            ),
        )

        When("all conditions are met") {
            val result = evaluate(gambitSet, context())

            Then("priority 1 gambit is selected") {
                result.gambitName shouldBe "Priority1-Cast"
                result.action shouldBe ActionIntent.Cast("Holy Shock")
            }
        }
    }

    Given("priority 1 conditions met but canExecuteCheck fails") {
        val gambitSet = GambitSet(
            name = "Test",
            gambits = listOf(
                GambitRule(
                    priority = 1,
                    name = "Priority1-Blocked",
                    conditions = listOf(AlwaysCondition()),
                    selector = PlayerSelector(),
                    action = ActionIntent.Cast("Holy Shock"),
                    canExecuteCheck = { _, _ -> false },
                ),
                GambitRule(
                    priority = 2,
                    name = "Priority2-Fallthrough",
                    conditions = listOf(AlwaysCondition()),
                    selector = PlayerSelector(),
                    action = ActionIntent.Cast("Flash of Light"),
                ),
            ),
        )

        When("evaluated") {
            val result = evaluate(gambitSet, context())

            Then("priority 2 is selected instead") {
                result.gambitName shouldBe "Priority2-Fallthrough"
                result.action shouldBe ActionIntent.Cast("Flash of Light")
            }
        }
    }

    Given("priority 1 conditions NOT met, priority 2 met") {
        val gambitSet = GambitSet(
            name = "Test",
            gambits = listOf(
                GambitRule(1, "Priority1-NotMet", listOf(NeverCondition()), PlayerSelector(), ActionIntent.Cast("Holy Shock")),
                GambitRule(2, "Priority2-Met", listOf(AlwaysCondition()), PlayerSelector(), ActionIntent.Cast("Flash of Light")),
            ),
        )

        When("evaluated") {
            val result = evaluate(gambitSet, context())

            Then("priority 2 is selected") {
                result.gambitName shouldBe "Priority2-Met"
                result.action shouldBe ActionIntent.Cast("Flash of Light")
            }
        }
    }

    Given("a gambit set with a before-chain") {
        val beforeSet = GambitSet(
            name = "Emergency",
            gambits = listOf(
                GambitRule(1, "Emergency-Heal", listOf(AlwaysCondition()), PlayerSelector(), ActionIntent.Cast("Lay on Hands")),
            ),
        )

        val mainSet = GambitSet(
            name = "Main",
            gambits = listOf(
                GambitRule(1, "Main-Cast", listOf(AlwaysCondition()), PlayerSelector(), ActionIntent.Cast("Holy Shock")),
            ),
            before = beforeSet,
        )

        When("before-chain has a matching gambit") {
            val result = evaluate(mainSet, context())

            Then("before-chain gambit wins over main gambits") {
                result.gambitName shouldBe "Emergency-Heal"
                result.action shouldBe ActionIntent.Cast("Lay on Hands")
            }
        }
    }

    Given("a gambit set with a before-chain that doesn't match") {
        val beforeSet = GambitSet(
            name = "Emergency",
            gambits = listOf(
                GambitRule(1, "Emergency-NoMatch", listOf(NeverCondition()), PlayerSelector(), ActionIntent.Cast("Lay on Hands")),
            ),
        )

        val mainSet = GambitSet(
            name = "Main",
            gambits = listOf(
                GambitRule(1, "Main-Cast", listOf(AlwaysCondition()), PlayerSelector(), ActionIntent.Cast("Holy Shock")),
            ),
            before = beforeSet,
        )

        When("evaluated") {
            val result = evaluate(mainSet, context())

            Then("main gambit is selected") {
                result.gambitName shouldBe "Main-Cast"
                result.action shouldBe ActionIntent.Cast("Holy Shock")
            }
        }
    }

    Given("a gambit set with a fallback chain") {
        val fallbackSet = GambitSet(
            name = "Fallback",
            gambits = listOf(
                GambitRule(1, "Fallback-Idle", listOf(AlwaysCondition()), PlayerSelector(), ActionIntent.None),
            ),
        )

        val mainSet = GambitSet(
            name = "Main",
            gambits = listOf(
                GambitRule(1, "Main-NoMatch", listOf(NeverCondition()), PlayerSelector(), ActionIntent.Cast("Holy Shock")),
            ),
            fallback = fallbackSet,
        )

        When("main gambits don't match") {
            val result = evaluate(mainSet, context())

            Then("fallback gambit is selected") {
                result.gambitName shouldBe "Fallback-Idle"
                result.action shouldBe ActionIntent.None
            }
        }
    }

    Given("no gambits match at all") {
        val gambitSet = GambitSet(
            name = "Empty",
            gambits = listOf(
                GambitRule(1, "NoMatch", listOf(NeverCondition()), PlayerSelector(), ActionIntent.Cast("Holy Shock")),
            ),
        )

        When("evaluated") {
            val result = evaluate(gambitSet, context())

            Then("result is NONE") {
                result shouldBe EvaluationResult.NONE
            }
        }
    }

    Given("gambits are added out of priority order") {
        val gambitSet = GambitSet(
            name = "Unordered",
            gambits = listOf(
                GambitRule(3, "Third", listOf(AlwaysCondition()), PlayerSelector(), ActionIntent.Cast("Spell3")),
                GambitRule(1, "First", listOf(AlwaysCondition()), PlayerSelector(), ActionIntent.Cast("Spell1")),
                GambitRule(2, "Second", listOf(AlwaysCondition()), PlayerSelector(), ActionIntent.Cast("Spell2")),
            ),
        )

        When("evaluated") {
            val result = evaluate(gambitSet, context())

            Then("the lowest priority number wins") {
                result.gambitName shouldBe "First"
                result.action shouldBe ActionIntent.Cast("Spell1")
            }
        }
    }

    Given("a gambit with multiple conditions where one fails") {
        val gambitSet = GambitSet(
            name = "MultiCondition",
            gambits = listOf(
                GambitRule(
                    priority = 1,
                    name = "PartialMatch",
                    conditions = listOf(AlwaysCondition(), NeverCondition()),
                    selector = PlayerSelector(),
                    action = ActionIntent.Cast("Holy Shock"),
                ),
                GambitRule(
                    priority = 2,
                    name = "AllMatch",
                    conditions = listOf(AlwaysCondition(), InCombatCondition()),
                    selector = PlayerSelector(),
                    action = ActionIntent.Cast("Flash of Light"),
                ),
            ),
        )

        When("evaluated with player in combat") {
            val result = evaluate(gambitSet, context(tickState(inCombat = true)))

            Then("only the gambit with ALL conditions met fires") {
                result.gambitName shouldBe "AllMatch"
                result.action shouldBe ActionIntent.Cast("Flash of Light")
            }
        }
    }
})

