package com.penelosgambits.api.websocket

import com.penelosgambits.data.dto.ConnectDto
import com.penelosgambits.data.dto.ExecutionResultDto
import com.penelosgambits.data.dto.QueryResponseDto
import com.penelosgambits.data.dto.StateUpdateDto
import com.penelosgambits.data.dto.extractType
import com.penelosgambits.data.gamequery.WebSocketGameQueryPort
import com.penelosgambits.data.mapper.toDomain
import kotlinx.serialization.json.Json
import org.slf4j.LoggerFactory

/**
 * Routes raw JSON messages from the bot to the appropriate handler.
 * Deserializes each message to its DTO, maps to domain where needed,
 * and delegates to the relevant component.
 */
class MessageRouter(
    private val tickStateManager: TickStateManager,
    private val json: Json = Json { ignoreUnknownKeys = true },
) {
    private val logger = LoggerFactory.getLogger(MessageRouter::class.java)

    /** Set after construction once the query port is available (avoids circular init). */
    var queryPort: WebSocketGameQueryPort? = null

    /** Set after construction once the tick processor is available. */
    var tickProcessor: TickProcessor? = null

    /**
     * Routes a raw JSON message to the appropriate handler.
     * @return true if this was a STATE_UPDATE (caller should launch tick processing).
     */
    fun route(rawJson: String): Boolean {
        val type = extractType(rawJson)
        if (type == null) {
            logger.warn("Could not extract type from message: {}", rawJson.take(200))
            return false
        }

        return when (type) {
            "CONNECT" -> { handleConnect(rawJson); false }
            "STATE_UPDATE" -> { handleStateUpdate(rawJson); true }
            "QUERY_RESPONSE" -> { handleQueryResponse(rawJson); false }
            "EXECUTION_RESULT" -> { handleExecutionResult(rawJson); false }
            "PONG" -> false
            else -> { logger.warn("Unknown message type: {}", type); false }
        }
    }

    /**
     * Processes the latest tick state through the gambit system.
     * Called from a separate coroutine to avoid blocking the receive loop.
     */
    suspend fun processLatestTick() {
        val state = tickStateManager.currentState ?: return
        tickProcessor?.processTick(state)
    }

    private fun handleConnect(rawJson: String) {
        val dto = json.decodeFromString<ConnectDto>(rawJson)
        val info = dto.toDomain()
        logger.info("Connected character: {} ({})", info.character, info.spec)
    }

    private fun handleStateUpdate(rawJson: String) {
        val dto = json.decodeFromString<StateUpdateDto>(rawJson)
        val tickState = dto.toDomain()
        tickStateManager.update(tickState)
    }

    private fun handleQueryResponse(rawJson: String) {
        val dto = json.decodeFromString<QueryResponseDto>(rawJson)
        queryPort?.handleResponse(dto)
            ?: logger.warn("Query response received but no queryPort is wired: queryId={}", dto.queryId)
    }

    private fun handleExecutionResult(rawJson: String) {
        val dto = json.decodeFromString<ExecutionResultDto>(rawJson)
        logger.debug(
            "Execution result: commandId={}, success={}, error={}",
            dto.commandId,
            dto.success,
            dto.error,
        )
    }
}

