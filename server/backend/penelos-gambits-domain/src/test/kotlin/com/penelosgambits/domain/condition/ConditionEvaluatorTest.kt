package com.penelosgambits.domain.condition

import com.penelosgambits.domain.model.BossState
import com.penelosgambits.domain.model.PlayerState
import com.penelosgambits.domain.model.TargetState
import com.penelosgambits.domain.model.TickContext
import com.penelosgambits.domain.model.TickState
import com.penelosgambits.domain.port.GameQueryPort
import com.penelosgambits.domain.port.QueryResult
import io.kotest.core.spec.style.BehaviorSpec
import io.kotest.matchers.shouldBe
import io.mockk.coEvery
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

private fun context(state: TickState, queryPort: GameQueryPort = mockk()): TickContext =
    TickContext(state, queryPort)

class StateConditionsTest : BehaviorSpec({

    Given("AlwaysCondition") {
        val condition = AlwaysCondition()

        When("evaluated with any context") {
            val result = condition.isMet(context(tickState()))

            Then("it returns true") {
                result shouldBe true
            }
        }
    }

    Given("InCombatCondition") {
        val condition = InCombatCondition()

        When("player is in combat") {
            val state = tickState(player = PlayerState(95, "Holy", 0, inCombat = true, isMoving = false))
            val result = condition.isMet(context(state))

            Then("it returns true") {
                result shouldBe true
            }
        }

        When("player is not in combat") {
            val state = tickState(player = PlayerState(95, "Holy", 0, inCombat = false, isMoving = false))
            val result = condition.isMet(context(state))

            Then("it returns false") {
                result shouldBe false
            }
        }

        When("player is null") {
            val result = condition.isMet(context(tickState(player = null)))

            Then("it returns false") {
                result shouldBe false
            }
        }
    }

    Given("PlayerHealthBelowCondition(50)") {
        val condition = PlayerHealthBelowCondition(50)

        When("player health is 30") {
            val state = tickState(player = PlayerState(30, "Holy", 0, true, false))
            val result = condition.isMet(context(state))

            Then("it returns true") {
                result shouldBe true
            }
        }

        When("player health is 50") {
            val state = tickState(player = PlayerState(50, "Holy", 0, true, false))
            val result = condition.isMet(context(state))

            Then("it returns false (not strictly below)") {
                result shouldBe false
            }
        }

        When("player health is 80") {
            val state = tickState(player = PlayerState(80, "Holy", 0, true, false))
            val result = condition.isMet(context(state))

            Then("it returns false") {
                result shouldBe false
            }
        }

        When("player is null") {
            val result = condition.isMet(context(tickState(player = null)))

            Then("it returns false") {
                result shouldBe false
            }
        }
    }

    Given("TargetExistsCondition") {
        val condition = TargetExistsCondition()

        When("target exists") {
            val target = TargetState(
                exists = true, name = "Ragnaros", health = 72, castingSpellId = 0,
            )
            val state = tickState(target = target)
            val result = condition.isMet(context(state))

            Then("it returns true") {
                result shouldBe true
            }
        }

        When("target does not exist") {
            val state = tickState(target = TargetState(exists = false, name = null, health = 0, castingSpellId = 0))
            val result = condition.isMet(context(state))

            Then("it returns false") {
                result shouldBe false
            }
        }

        When("target is null") {
            val result = condition.isMet(context(tickState(target = null)))

            Then("it returns false") {
                result shouldBe false
            }
        }
    }

    Given("BossCastingCondition") {
        val condition = BossCastingCondition()

        When("a boss is casting") {
            val state = tickState(bosses = listOf(BossState("boss1", "Ragnaros", 72, castingSpellId = 12345)))
            val result = condition.isMet(context(state))

            Then("it returns true") {
                result shouldBe true
            }
        }

        When("no boss is casting") {
            val state = tickState(bosses = listOf(BossState("boss1", "Ragnaros", 72, castingSpellId = 0)))
            val result = condition.isMet(context(state))

            Then("it returns false") {
                result shouldBe false
            }
        }

        When("no bosses at all") {
            val result = condition.isMet(context(tickState(bosses = emptyList())))

            Then("it returns false") {
                result shouldBe false
            }
        }
    }
})

class QueryConditionsTest : BehaviorSpec({

    Given("HasNotBuffCondition") {
        val queryPort = mockk<GameQueryPort>()
        val state = tickState(player = PlayerState(95, "Holy", 0, true, false))
        val ctx = context(state, queryPort)

        When("the unit does NOT have the buff") {
            coEvery {
                queryPort.query("HasBuff", mapOf("unit" to "player", "buff" to "Earth Shield", "byPlayer" to true))
            } returns QueryResult(success = false)

            val result = HasNotBuffCondition("player", "Earth Shield").isMet(ctx)

            Then("it returns true") {
                result shouldBe true
            }
        }
    }

    Given("HasNotBuffCondition when buff is present") {
        val queryPort = mockk<GameQueryPort>()
        val state = tickState(player = PlayerState(95, "Holy", 0, true, false))
        val ctx = context(state, queryPort)

        When("the unit HAS the buff") {
            coEvery {
                queryPort.query("HasBuff", mapOf("unit" to "player", "buff" to "Earth Shield", "byPlayer" to true))
            } returns QueryResult(success = true, data = mapOf("remaining" to 12000, "stacks" to 1))

            val result = HasNotBuffCondition("player", "Earth Shield").isMet(ctx)

            Then("it returns false") {
                result shouldBe false
            }
        }
    }

    Given("TargetHasNotDebuffCondition") {
        val queryPort = mockk<GameQueryPort>()
        val state = tickState(target = TargetState(true, "Ragnaros", 72, 0))
        val ctx = context(state, queryPort)

        When("the target does NOT have the debuff") {
            coEvery {
                queryPort.query("HasDebuff", mapOf("unit" to "target", "debuff" to "Flame Shock", "byPlayer" to true))
            } returns QueryResult(success = false)

            val result = TargetHasNotDebuffCondition("Flame Shock").isMet(ctx)

            Then("it returns true") {
                result shouldBe true
            }
        }
    }

    Given("TargetHasNotDebuffCondition when debuff is present") {
        val queryPort = mockk<GameQueryPort>()
        val state = tickState(target = TargetState(true, "Ragnaros", 72, 0))
        val ctx = context(state, queryPort)

        When("the target HAS the debuff") {
            coEvery {
                queryPort.query("HasDebuff", mapOf("unit" to "target", "debuff" to "Flame Shock", "byPlayer" to true))
            } returns QueryResult(success = true, data = mapOf("remaining" to 5000))

            val result = TargetHasNotDebuffCondition("Flame Shock").isMet(ctx)

            Then("it returns false") {
                result shouldBe false
            }
        }
    }

    Given("SpellReadyCondition") {
        val queryPort = mockk<GameQueryPort>()
        val state = tickState()
        val ctx = context(state, queryPort)

        When("the spell is ready") {
            coEvery {
                queryPort.query("CanCast", mapOf("spell" to "Holy Shock"))
            } returns QueryResult(success = true)

            val result = SpellReadyCondition("Holy Shock").isMet(ctx)

            Then("it returns true") {
                result shouldBe true
            }
        }
    }

    Given("SpellReadyCondition when spell is on cooldown") {
        val queryPort = mockk<GameQueryPort>()
        val state = tickState()
        val ctx = context(state, queryPort)

        When("the spell is NOT ready") {
            coEvery {
                queryPort.query("CanCast", mapOf("spell" to "Holy Shock"))
            } returns QueryResult(success = false)

            val result = SpellReadyCondition("Holy Shock").isMet(ctx)

            Then("it returns false") {
                result shouldBe false
            }
        }
    }
})

