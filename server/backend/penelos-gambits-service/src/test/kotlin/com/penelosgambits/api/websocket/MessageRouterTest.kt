package com.penelosgambits.api.websocket

import io.kotest.core.spec.style.BehaviorSpec
import io.kotest.matchers.nulls.shouldBeNull
import io.kotest.matchers.nulls.shouldNotBeNull
import io.kotest.matchers.shouldBe

class MessageRouterTest : BehaviorSpec({

    Given("a MessageRouter with a TickStateManager") {
        val tickStateManager = TickStateManager()
        val router = MessageRouter(tickStateManager)

        When("routing a CONNECT message") {
            val json = """{"type":"CONNECT","character":"Penelo","spec":"Holy Paladin"}"""
            router.route(json)

            Then("no crash occurs and state is unaffected") {
                tickStateManager.currentState.shouldBeNull()
            }
        }

        When("routing a STATE_UPDATE message") {
            val json = """
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

            router.route(json)

            Then("TickStateManager holds the parsed domain state") {
                val state = tickStateManager.currentState
                state.shouldNotBeNull()
                state.mapId shouldBe 2549
                state.timestamp shouldBe 638789012345678901L
                state.combatTime shouldBe 45000
                state.globalCooldown shouldBe 0
            }

            Then("player state is mapped correctly") {
                val player = tickStateManager.currentState!!.player
                player.shouldNotBeNull()
                player.health shouldBe 95
                player.spec shouldBe "Holy"
                player.inCombat shouldBe true
                player.isMoving shouldBe false
            }

            Then("target state is mapped correctly") {
                val target = tickStateManager.currentState!!.target
                target.shouldNotBeNull()
                target.name shouldBe "Ragnaros"
                target.health shouldBe 72
            }

            Then("bosses are mapped correctly") {
                val bosses = tickStateManager.currentState!!.bosses
                bosses.size shouldBe 1
                bosses[0].unitId shouldBe "boss1"
                bosses[0].name shouldBe "Ragnaros"
            }
        }

        When("routing a STATE_UPDATE with nulls and empty bosses") {
            val json = """
            {
                "type": "STATE_UPDATE",
                "timestamp": 100,
                "mapId": 1,
                "globalCooldown": 1200,
                "combatTime": 0,
                "player": null,
                "target": null,
                "group": null,
                "bosses": []
            }
            """.trimIndent()

            router.route(json)

            Then("state is updated with null optionals") {
                val state = tickStateManager.currentState
                state.shouldNotBeNull()
                state.player.shouldBeNull()
                state.target.shouldBeNull()
                state.group.shouldBeNull()
                state.bosses shouldBe emptyList()
            }
        }

        When("routing a PONG message") {
            Then("no crash occurs") {
                router.route("""{"type":"PONG"}""")
            }
        }

        When("routing a QUERY_RESPONSE message") {
            Then("no crash occurs") {
                router.route("""{"type":"QUERY_RESPONSE","queryId":"q-1","result":true}""")
            }
        }

        When("routing an EXECUTION_RESULT message") {
            Then("no crash occurs") {
                router.route("""{"type":"EXECUTION_RESULT","commandId":"cmd-1","success":true,"error":null}""")
            }
        }

        When("routing an unknown message type") {
            Then("no crash occurs") {
                router.route("""{"type":"UNKNOWN_THING","data":"foo"}""")
            }
        }

        When("routing invalid JSON") {
            Then("no crash occurs") {
                router.route("not valid json")
            }
        }
    }
})

