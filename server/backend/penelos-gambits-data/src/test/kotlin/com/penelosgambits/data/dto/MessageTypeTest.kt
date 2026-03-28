package com.penelosgambits.data.dto

import io.kotest.core.spec.style.BehaviorSpec
import io.kotest.matchers.nulls.shouldBeNull
import io.kotest.matchers.shouldBe

class MessageTypeTest : BehaviorSpec({

    Given("a valid JSON with a type field") {
        val raw = """{"type": "STATE_UPDATE", "timestamp": 123}"""

        When("extracting the type") {
            val type = extractType(raw)

            Then("it returns the type value") {
                type shouldBe "STATE_UPDATE"
            }
        }
    }

    Given("a CONNECT JSON") {
        val raw = """{"type": "CONNECT", "character": "Penelo", "spec": "Holy Paladin"}"""

        When("extracting the type") {
            val type = extractType(raw)

            Then("it returns CONNECT") {
                type shouldBe "CONNECT"
            }
        }
    }

    Given("a JSON without a type field") {
        val raw = """{"foo": "bar"}"""

        When("extracting the type") {
            val type = extractType(raw)

            Then("it returns null") {
                type.shouldBeNull()
            }
        }
    }

    Given("invalid JSON") {
        val raw = "not json at all"

        When("extracting the type") {
            val type = extractType(raw)

            Then("it returns null") {
                type.shouldBeNull()
            }
        }
    }
})

