package com.penelosgambits.data.gamequery

import com.penelosgambits.data.dto.QueryDto
import com.penelosgambits.data.dto.QueryResponseDto
import com.penelosgambits.domain.port.GameQueryPort
import com.penelosgambits.domain.port.MessageSender
import com.penelosgambits.domain.port.QueryResult
import kotlinx.coroutines.CompletableDeferred
import kotlinx.coroutines.withTimeoutOrNull
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.JsonPrimitive
import kotlinx.serialization.json.booleanOrNull
import kotlinx.serialization.json.contentOrNull
import kotlinx.serialization.json.doubleOrNull
import kotlinx.serialization.json.intOrNull
import kotlinx.serialization.json.jsonPrimitive
import kotlinx.serialization.json.longOrNull
import java.util.concurrent.ConcurrentHashMap
import java.util.concurrent.atomic.AtomicLong

/**
 * GameQueryPort implementation that sends QUERY messages over the WebSocket
 * and awaits QUERY_RESPONSE from the bot.
 *
 * The per-tick cache in [com.penelosgambits.domain.model.TickContext] prevents
 * duplicate queries for the same method+params within a single tick.
 */
class WebSocketGameQueryPort(
    private val messageSender: MessageSender,
    private val timeoutMs: Long = 100L,
) : GameQueryPort {

    private val pendingQueries = ConcurrentHashMap<String, CompletableDeferred<QueryResult>>()
    private val queryCounter = AtomicLong(0)
    private val json = Json { ignoreUnknownKeys = true }

    override suspend fun query(method: String, params: Map<String, Any>): QueryResult {
        val queryId = "q-${queryCounter.incrementAndGet()}"
        val deferred = CompletableDeferred<QueryResult>()
        pendingQueries[queryId] = deferred

        val queryMsg = QueryDto(
            queryId = queryId,
            method = method,
            params = buildJsonParams(params),
        )
        messageSender.send(json.encodeToString(QueryDto.serializer(), queryMsg))

        return withTimeoutOrNull(timeoutMs) { deferred.await() }
            ?: run {
                pendingQueries.remove(queryId)
                QueryResult(success = false)
            }
    }

    /**
     * Called by the MessageRouter when a QUERY_RESPONSE arrives from the bot.
     */
    fun handleResponse(response: QueryResponseDto) {
        val data = response.data?.let { jsonObjectToMap(it) } ?: emptyMap()
        pendingQueries.remove(response.queryId)?.complete(
            QueryResult(success = response.result, data = data),
        )
    }

    private fun buildJsonParams(params: Map<String, Any>): JsonObject? {
        if (params.isEmpty()) return null
        val map = params.mapValues { (_, v) ->
            when (v) {
                is String -> JsonPrimitive(v)
                is Int -> JsonPrimitive(v)
                is Long -> JsonPrimitive(v)
                is Double -> JsonPrimitive(v)
                is Boolean -> JsonPrimitive(v)
                else -> JsonPrimitive(v.toString())
            }
        }
        return JsonObject(map)
    }

    private fun jsonObjectToMap(obj: JsonObject): Map<String, Any?> =
        obj.mapValues { (_, element) ->
            val primitive = element.jsonPrimitive
            primitive.intOrNull
                ?: primitive.longOrNull
                ?: primitive.doubleOrNull
                ?: primitive.booleanOrNull
                ?: primitive.contentOrNull
        }
}

