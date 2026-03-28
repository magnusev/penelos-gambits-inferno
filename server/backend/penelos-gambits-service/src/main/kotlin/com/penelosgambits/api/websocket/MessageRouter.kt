package com.penelosgambits.api.websocket

import com.penelosgambits.data.dto.ConnectDto
import com.penelosgambits.data.dto.ExecutionResultDto
import com.penelosgambits.data.dto.QueryResponseDto
import com.penelosgambits.data.dto.StateUpdateDto
import com.penelosgambits.data.dto.extractType
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

    fun route(rawJson: String) {
        val type = extractType(rawJson)
        if (type == null) {
            logger.warn("Could not extract type from message: {}", rawJson.take(200))
            return
        }

        when (type) {
            "CONNECT" -> handleConnect(rawJson)
            "STATE_UPDATE" -> handleStateUpdate(rawJson)
            "QUERY_RESPONSE" -> handleQueryResponse(rawJson)
            "EXECUTION_RESULT" -> handleExecutionResult(rawJson)
            "PONG" -> { /* keep-alive ack, nothing to do */ }
            else -> logger.warn("Unknown message type: {}", type)
        }
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
        logger.debug("Query response: queryId={}, result={}", dto.queryId, dto.result)
        // Will be wired to WebSocketGameQueryPort in phase 2
    }

    private fun handleExecutionResult(rawJson: String) {
        val dto = json.decodeFromString<ExecutionResultDto>(rawJson)
        logger.debug(
            "Execution result: commandId={}, success={}, error={}",
            dto.commandId,
            dto.success,
            dto.error,
        )
        // Will be wired to command tracking in phase 2
    }
}

