package com.penelosgambits.domain.selector

import com.penelosgambits.domain.model.BossState
import com.penelosgambits.domain.model.PlayerState
import com.penelosgambits.domain.model.TargetState
import com.penelosgambits.domain.model.TickContext
import com.penelosgambits.domain.model.TickState
import com.penelosgambits.domain.model.UnitState
import com.penelosgambits.domain.port.GameQueryPort
import com.penelosgambits.domain.selector.filters.IsNotDeadFilter
import com.penelosgambits.domain.selector.filters.LowestHealthFilter
import com.penelosgambits.domain.selector.filters.LowestHealthUnderThresholdFilter
import io.kotest.core.spec.style.BehaviorSpec
import io.kotest.matchers.nulls.shouldBeNull
import io.kotest.matchers.nulls.shouldNotBeNull
import io.kotest.matchers.shouldBe
import io.mockk.mockk

private fun tickState(
    player: PlayerState? = null,
    target: TargetState? = null,
    bosses: List<BossState> = emptyList(),
) = TickState(
    timestamp = 1L,
    mapId = 2549,
    globalCooldown = 0,
    combatTime = 0,
    player = player,
    target = target,
    group = null,
    bosses = bosses,
)

private fun context(state: TickState): TickContext = TickContext(state, mockk<GameQueryPort>())

class StaticSelectorsTest : BehaviorSpec({

    Given("PlayerSelector") {
        val selector = PlayerSelector()

        When("player exists") {
            val state = tickState(player = PlayerState(95, "Holy", 0, true, false))
            val result = selector.select(context(state))

            Then("it returns a UnitState for the player") {
                result.shouldNotBeNull()
                result.unitId shouldBe "player"
                result.health shouldBe 95
            }
        }

        When("player is null") {
            val result = selector.select(context(tickState()))

            Then("it returns null") {
                result.shouldBeNull()
            }
        }
    }

    Given("CurrentTargetSelector") {
        val selector = CurrentTargetSelector()

        When("target exists") {
            val state = tickState(target = TargetState(true, "Ragnaros", 72, 0))
            val result = selector.select(context(state))

            Then("it returns the target as UnitState") {
                result.shouldNotBeNull()
                result.unitId shouldBe "target"
                result.name shouldBe "Ragnaros"
                result.health shouldBe 72
            }
        }

        When("target exists=false") {
            val state = tickState(target = TargetState(false, null, 0, 0))
            val result = selector.select(context(state))

            Then("it returns null") {
                result.shouldBeNull()
            }
        }

        When("target is null") {
            val result = selector.select(context(tickState()))

            Then("it returns null") {
                result.shouldBeNull()
            }
        }
    }

    Given("BossSelector") {
        val selector = BossSelector("boss1")

        When("boss1 exists") {
            val state = tickState(bosses = listOf(BossState("boss1", "Ragnaros", 72, 12345)))
            val result = selector.select(context(state))

            Then("it returns boss1 as UnitState") {
                result.shouldNotBeNull()
                result.unitId shouldBe "boss1"
                result.name shouldBe "Ragnaros"
                result.castingSpellId shouldBe 12345
            }
        }

        When("boss1 does not exist") {
            val state = tickState(bosses = listOf(BossState("boss2", "Nefarian", 50, 0)))
            val result = selector.select(context(state))

            Then("it returns null") {
                result.shouldBeNull()
            }
        }
    }
})

class FilterTests : BehaviorSpec({

    Given("IsNotDeadFilter") {
        val filter = IsNotDeadFilter()

        When("applied to a mix of alive and dead units") {
            val units = listOf(
                UnitState("a", "Alive", 80, 0, isDead = false),
                UnitState("b", "Dead", 0, 0, isDead = true),
                UnitState("c", "AlsoAlive", 30, 0, isDead = false),
            )
            val result = filter.filter(units)

            Then("dead units are removed") {
                result.size shouldBe 2
                result.map { it.unitId } shouldBe listOf("a", "c")
            }
        }
    }

    Given("LowestHealthFilter") {
        val filter = LowestHealthFilter()

        When("applied to units with varying health") {
            val units = listOf(
                UnitState("a", null, 80, 0),
                UnitState("b", null, 30, 0),
                UnitState("c", null, 45, 0),
            )
            val result = filter.filter(units)

            Then("it returns only the lowest health unit") {
                result.size shouldBe 1
                result[0].unitId shouldBe "b"
                result[0].health shouldBe 30
            }
        }

        When("applied to an empty list") {
            val result = filter.filter(emptyList())

            Then("it returns empty") {
                result shouldBe emptyList()
            }
        }
    }

    Given("LowestHealthUnderThresholdFilter(50)") {
        val filter = LowestHealthUnderThresholdFilter(50)

        When("applied to units at [80, 30, 45]") {
            val units = listOf(
                UnitState("a", null, 80, 0),
                UnitState("b", null, 30, 0),
                UnitState("c", null, 45, 0),
            )
            val result = filter.filter(units)

            Then("it keeps only units below 50 sorted ascending") {
                result.size shouldBe 2
                result[0].unitId shouldBe "b"
                result[0].health shouldBe 30
                result[1].unitId shouldBe "c"
                result[1].health shouldBe 45
            }
        }

        When("no units are below threshold") {
            val units = listOf(
                UnitState("a", null, 80, 0),
                UnitState("b", null, 60, 0),
            )
            val result = filter.filter(units)

            Then("it returns empty") {
                result shouldBe emptyList()
            }
        }
    }
})

class FilterPipelineSelectorTest : BehaviorSpec({

    Given("a pipeline with [IsNotDead, LowestHealth]") {
        val units = listOf(
            UnitState("a", "Alive80", 80, 0, isDead = false),
            UnitState("b", "Dead", 0, 0, isDead = true),
            UnitState("c", "Alive30", 30, 0, isDead = false),
            UnitState("d", "Alive45", 45, 0, isDead = false),
        )

        val selector = FilterPipelineSelector(
            unitProvider = { units },
            filters = listOf(IsNotDeadFilter(), LowestHealthFilter()),
        )

        When("selecting") {
            val state = tickState()
            val result = selector.select(context(state))

            Then("it picks the lowest alive unit") {
                result.shouldNotBeNull()
                result.unitId shouldBe "c"
                result.name shouldBe "Alive30"
                result.health shouldBe 30
            }
        }
    }

    Given("a pipeline where filters eliminate all candidates") {
        val units = listOf(
            UnitState("a", null, 0, 0, isDead = true),
            UnitState("b", null, 0, 0, isDead = true),
        )

        val selector = FilterPipelineSelector(
            unitProvider = { units },
            filters = listOf(IsNotDeadFilter()),
        )

        When("selecting") {
            val result = selector.select(context(tickState()))

            Then("it returns null") {
                result.shouldBeNull()
            }
        }
    }

    Given("a pipeline with empty initial candidates") {
        val selector = FilterPipelineSelector(
            unitProvider = { emptyList() },
            filters = listOf(IsNotDeadFilter(), LowestHealthFilter()),
        )

        When("selecting") {
            val result = selector.select(context(tickState()))

            Then("it returns null") {
                result.shouldBeNull()
            }
        }
    }
})

