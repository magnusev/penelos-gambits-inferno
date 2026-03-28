package com.penelosgambits.data.gamequery

import com.penelosgambits.data.dto.QueryResponseDto
import com.penelosgambits.domain.port.MessageSender
import com.penelosgambits.domain.port.QueryResult
import io.kotest.core.spec.style.BehaviorSpec
import io.kotest.matchers.shouldBe
import io.kotest.matchers.string.shouldContain
import kotlinx.coroutines.launch
import kotlinx.serialization.json.buildJsonObject
import kotlinx.serialization.json.put

class WebSocketGameQueryPortTest : BehaviorSpec({

    Given("a WebSocketGameQueryPort with a captured sender") {
        val sentMessages = mutableListOf<String>()
        val sender = MessageSender { sentMessages.add(it) }
        val queryPort = WebSocketGameQueryPort(messageSender = sender, timeoutMs = 500L)

        When("a query is sent and a response arrives") {
            var result: QueryResult? = null

            val job = launch {
                result = queryPort.query("HasDebuff", mapOf("unit" to "boss1", "debuff" to "Flame Shock"))
            }

            // Wait for the query to be sent
            while (sentMessages.isEmpty()) {
                kotlinx.coroutines.delay(10)
            }

            Then("a QUERY message was sent") {
                sentMessages.size shouldBe 1
                sentMessages[0] shouldContain "HasDebuff"
                sentMessages[0] shouldContain "boss1"
            }

            // Extract the queryId from the sent message and simulate a response
            val queryId = sentMessages[0].let {
                val regex = """"queryId"\s*:\s*"([^"]+)"""".toRegex()
                regex.find(it)!!.groupValues[1]
            }

            queryPort.handleResponse(
                QueryResponseDto(
                    type = "QUERY_RESPONSE",
                    queryId = queryId,
                    result = true,
                    data = buildJsonObject {
                        put("remaining", 12000)
                        put("stacks", 1)
                    },
                ),
            )

            job.join()

            Then("the query returns the response data") {
                result shouldBe QueryResult(
                    success = true,
                    data = mapOf("remaining" to 12000, "stacks" to 1),
                )
            }
        }
    }

    Given("a WebSocketGameQueryPort where the bot never responds") {
        val sender = MessageSender { /* drop the message */ }
        val queryPort = WebSocketGameQueryPort(messageSender = sender, timeoutMs = 50L)

        When("a query times out") {
            val result = queryPort.query("Health", mapOf("unit" to "player"))

            Then("it returns a failed result") {
                result shouldBe QueryResult(success = false)
            }
        }
    }
})

