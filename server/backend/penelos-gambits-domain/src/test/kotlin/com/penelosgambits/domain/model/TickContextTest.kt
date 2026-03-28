package com.penelosgambits.domain.model

import com.penelosgambits.domain.port.GameQueryPort
import com.penelosgambits.domain.port.QueryResult
import io.kotest.core.spec.style.BehaviorSpec
import io.kotest.matchers.shouldBe
import io.mockk.coEvery
import io.mockk.coVerify
import io.mockk.mockk

class TickContextTest : BehaviorSpec({

    Given("a TickContext with a mocked GameQueryPort") {
        val queryPort = mockk<GameQueryPort>()
        val state = TickState(
            timestamp = 1L,
            mapId = 2549,
            globalCooldown = 0,
            combatTime = 0,
            player = null,
            target = null,
            group = null,
            bosses = emptyList(),
        )
        val context = TickContext(state, queryPort)

        val expectedResult = QueryResult(success = true, data = mapOf("health" to 95))
        coEvery { queryPort.query("Health", mapOf("unit" to "player")) } returns expectedResult

        When("querying the same method+params twice") {
            val first = context.query("Health", mapOf("unit" to "player"))
            val second = context.query("Health", mapOf("unit" to "player"))

            Then("both calls return the same result") {
                first shouldBe expectedResult
                second shouldBe expectedResult
            }

            Then("the port is called only once (cached)") {
                coVerify(exactly = 1) { queryPort.query("Health", mapOf("unit" to "player")) }
            }
        }
    }

    Given("a TickContext with different query params") {
        val queryPort = mockk<GameQueryPort>()
        val state = TickState(
            timestamp = 1L,
            mapId = 2549,
            globalCooldown = 0,
            combatTime = 0,
            player = null,
            target = null,
            group = null,
            bosses = emptyList(),
        )
        val context = TickContext(state, queryPort)

        val playerResult = QueryResult(success = true, data = mapOf("health" to 95))
        val bossResult = QueryResult(success = true, data = mapOf("health" to 72))
        coEvery { queryPort.query("Health", mapOf("unit" to "player")) } returns playerResult
        coEvery { queryPort.query("Health", mapOf("unit" to "boss1")) } returns bossResult

        When("querying with different params") {
            val first = context.query("Health", mapOf("unit" to "player"))
            val second = context.query("Health", mapOf("unit" to "boss1"))

            Then("they return different results") {
                first shouldBe playerResult
                second shouldBe bossResult
            }

            Then("the port is called once per unique param set") {
                coVerify(exactly = 1) { queryPort.query("Health", mapOf("unit" to "player")) }
                coVerify(exactly = 1) { queryPort.query("Health", mapOf("unit" to "boss1")) }
            }
        }
    }
})

