package com.penelosgambits.domain.model

import com.penelosgambits.domain.port.GameQueryPort
import com.penelosgambits.domain.port.QueryResult

/**
 * Per-tick context that wraps the current game state and provides
 * cached access to the game query port.
 *
 * A new TickContext is created for every tick; the cache lives only
 * for the duration of that single tick evaluation.
 */
class TickContext(
    val state: TickState,
    private val queryPort: GameQueryPort,
) {
    private val cache = mutableMapOf<String, QueryResult>()

    suspend fun query(method: String, params: Map<String, Any> = emptyMap()): QueryResult {
        val key = buildCacheKey(method, params)
        return cache.getOrPut(key) { queryPort.query(method, params) }
    }

    private fun buildCacheKey(method: String, params: Map<String, Any>): String =
        "$method|${params.entries.sortedBy { it.key }.joinToString(",") { "${it.key}=${it.value}" }}"
}

