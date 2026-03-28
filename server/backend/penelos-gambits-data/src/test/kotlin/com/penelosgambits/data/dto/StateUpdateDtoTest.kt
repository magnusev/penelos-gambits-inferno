package com.penelosgambits.data.dto

import io.kotest.core.spec.style.BehaviorSpec
import io.kotest.matchers.nulls.shouldBeNull
import io.kotest.matchers.nulls.shouldNotBeNull
import io.kotest.matchers.shouldBe
import kotlinx.serialization.json.Json

class StateUpdateDtoTest : BehaviorSpec({

    val json = Json { ignoreUnknownKeys = true }

    Given("a full STATE_UPDATE JSON from the bot") {
        val raw = """
        {
            "type": "STATE_UPDATE",
            "timestamp": 638789012345678901,
            "mapId": 2549,
            "globalCooldown": 0,
            "combatTime": 45000,
            "player": {
                "health": 95,
                "spec": "Holy",
                "castingSpellId": 0,
                "inCombat": true,
                "isMoving": false
            },
            "target": {
                "exists": true,
                "name": "Ragnaros",
                "health": 72,
                "castingSpellId": 0
            },
            "group": {
                "type": "raid",
                "size": 20
            },
            "bosses": [
                {
                    "unitId": "boss1",
                    "name": "Ragnaros",
                    "health": 72,
                    "castingSpellId": 0
                }
            ]
        }
        """.trimIndent()

        When("deserializing to StateUpdateDto") {
            val dto = json.decodeFromString<StateUpdateDto>(raw)

            Then("top-level fields are correct") {
                dto.type shouldBe "STATE_UPDATE"
                dto.timestamp shouldBe 638789012345678901L
                dto.mapId shouldBe 2549
                dto.globalCooldown shouldBe 0
                dto.combatTime shouldBe 45000
            }

            Then("player state is parsed") {
                val player = dto.player.shouldNotBeNull()
                player.health shouldBe 95
                player.spec shouldBe "Holy"
                player.castingSpellId shouldBe 0
                player.inCombat shouldBe true
                player.isMoving shouldBe false
            }

            Then("target state is parsed") {
                val target = dto.target.shouldNotBeNull()
                target.exists shouldBe true
                target.name shouldBe "Ragnaros"
                target.health shouldBe 72
            }

            Then("group state is parsed") {
                val group = dto.group.shouldNotBeNull()
                group.type shouldBe "raid"
                group.size shouldBe 20
            }

            Then("bosses list is parsed") {
                dto.bosses.size shouldBe 1
                dto.bosses[0].unitId shouldBe "boss1"
                dto.bosses[0].name shouldBe "Ragnaros"
                dto.bosses[0].health shouldBe 72
                dto.bosses[0].castingSpellId shouldBe 0
            }
        }
    }

    Given("a STATE_UPDATE JSON with no target and no bosses") {
        val raw = """
        {
            "type": "STATE_UPDATE",
            "timestamp": 638789012345678901,
            "mapId": 2549,
            "globalCooldown": 1200,
            "combatTime": 0,
            "player": {
                "health": 100,
                "spec": "Holy",
                "castingSpellId": 0,
                "inCombat": false,
                "isMoving": true
            },
            "target": null,
            "group": {
                "type": "solo",
                "size": 0
            },
            "bosses": []
        }
        """.trimIndent()

        When("deserializing to StateUpdateDto") {
            val dto = json.decodeFromString<StateUpdateDto>(raw)

            Then("target is null") {
                dto.target.shouldBeNull()
            }

            Then("bosses list is empty") {
                dto.bosses shouldBe emptyList()
            }

            Then("globalCooldown is set") {
                dto.globalCooldown shouldBe 1200
            }
        }
    }
})


