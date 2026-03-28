package com.penelosgambits.domain.port

/**
 * Port for querying live game data from the bot.
 * Implemented in the data layer via WebSocket.
 */
interface GameQueryPort {
    suspend fun query(method: String, params: Map<String, Any> = emptyMap()): QueryResult
}

data class QueryResult(
    val success: Boolean,
    val data: Map<String, Any?> = emptyMap(),
)

