package com.penelosgambits.data.mapper

import com.penelosgambits.data.dto.BossStateDto
import com.penelosgambits.data.dto.GroupStateDto
import com.penelosgambits.data.dto.PlayerStateDto
import com.penelosgambits.data.dto.StateUpdateDto
import com.penelosgambits.data.dto.TargetStateDto
import io.kotest.core.spec.style.BehaviorSpec
import io.kotest.matchers.nulls.shouldBeNull
import io.kotest.matchers.nulls.shouldNotBeNull
import io.kotest.matchers.shouldBe

class StateMapperTest : BehaviorSpec({

    Given("a full StateUpdateDto") {
        val dto = StateUpdateDto(
            type = "STATE_UPDATE",
            timestamp = 638789012345678901L,
            mapId = 2549,
            globalCooldown = 0,
            combatTime = 45000,
            player = PlayerStateDto(
                health = 95,
                spec = "Holy",
                castingSpellId = 0,
                inCombat = true,
                isMoving = false,
            ),
            target = TargetStateDto(
                exists = true,
                name = "Ragnaros",
                health = 72,
                castingSpellId = 0,
            ),
            group = GroupStateDto(
                type = "raid",
                size = 20,
            ),
            bosses = listOf(
                BossStateDto(
                    unitId = "boss1",
                    name = "Ragnaros",
                    health = 72,
                    castingSpellId = 0,
                ),
            ),
        )

        When("mapping to domain TickState") {
            val tickState = dto.toDomain()

            Then("top-level fields map correctly") {
                tickState.timestamp shouldBe 638789012345678901L
                tickState.mapId shouldBe 2549
                tickState.globalCooldown shouldBe 0
                tickState.combatTime shouldBe 45000
            }

            Then("player maps correctly") {
                tickState.player.shouldNotBeNull()
                tickState.player!!.health shouldBe 95
                tickState.player!!.spec shouldBe "Holy"
                tickState.player!!.inCombat shouldBe true
            }

            Then("target maps correctly") {
                tickState.target.shouldNotBeNull()
                tickState.target!!.name shouldBe "Ragnaros"
                tickState.target!!.health shouldBe 72
            }

            Then("group maps correctly") {
                tickState.group.shouldNotBeNull()
                tickState.group!!.type shouldBe "raid"
                tickState.group!!.size shouldBe 20
            }

            Then("bosses map correctly") {
                tickState.bosses.size shouldBe 1
                tickState.bosses[0].unitId shouldBe "boss1"
                tickState.bosses[0].name shouldBe "Ragnaros"
            }
        }
    }

    Given("a StateUpdateDto with null optional fields") {
        val dto = StateUpdateDto(
            type = "STATE_UPDATE",
            timestamp = 100L,
            mapId = 1,
            globalCooldown = 1200,
            combatTime = 0,
            player = null,
            target = null,
            group = null,
            bosses = emptyList(),
        )

        When("mapping to domain TickState") {
            val tickState = dto.toDomain()

            Then("nullable fields are null") {
                tickState.player.shouldBeNull()
                tickState.target.shouldBeNull()
                tickState.group.shouldBeNull()
            }

            Then("bosses list is empty") {
                tickState.bosses shouldBe emptyList()
            }
        }
    }
})

