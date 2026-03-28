package no.politiet.avhor.libs.ktor.common.client

import io.ktor.client.HttpClient
import io.ktor.client.request.headers
import io.ktor.client.request.post
import io.ktor.client.request.setBody
import io.ktor.client.request.url
import io.ktor.client.statement.bodyAsText
import io.ktor.http.ContentType
import io.ktor.http.contentType
import kotlinx.serialization.KSerializer
import kotlinx.serialization.Serializable
import kotlinx.serialization.SerializationException
import kotlinx.serialization.json.Json
import kotlinx.serialization.json.JsonElement
import kotlinx.serialization.json.JsonObject
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive
import kotlinx.serialization.serializer
import no.politiet.avhor.libs.ktor.common.utils.DataError
import no.politiet.avhor.libs.ktor.common.utils.Result
import org.slf4j.LoggerFactory

/**
 * A lightweight GraphQL client built on top of [HttpClientFactory].
 *
 * Supports default headers (set once at creation) and per-request headers.
 *
 * Usage:
 * ```
 * val client = GraphQlClient.create(
 *     clientName = "sak-service",
 *     endpoint = "https://api.example.com/graphql",
 *     defaultHeaders = mapOf("bl-brukskontekst-system" to "AVHOR"),
 * )
 *
 * val result = client.execute<HentSakResponse>(
 *     query = "query(${'$'}id: String!) { hentSak(id: ${'$'}id) { ... } }",
 *     variables = buildJsonObject { put("id", "123") },
 *     headers = mapOf("bl-brukskontekst-bid" to "BID000"),
 * )
 * ```
 */
class GraphQlClient private constructor(
    private val httpClient: HttpClient,
    private val endpoint: String,
    private val clientName: String,
    private val defaultHeaders: Map<String, String>,
) {

    private val logger = LoggerFactory.getLogger("GraphQlClient.$clientName")

    private val json = Json { ignoreUnknownKeys = true }

    companion object {
        fun create(
            clientName: String,
            endpoint: String,
            timeoutMs: Long = 20_000L,
            defaultHeaders: Map<String, String> = emptyMap(),
        ): GraphQlClient {
            val httpClient = HttpClientFactory.create(
                clientName = clientName,
                timeoutMs = timeoutMs,
            )

            return GraphQlClient(
                httpClient = httpClient,
                endpoint = endpoint,
                clientName = clientName,
                defaultHeaders = defaultHeaders,
            )
        }
    }
    
    suspend inline fun <reified T> execute(
        query: String,
        variables: JsonObject? = null,
        operationName: String? = null,
        headers: Map<String, String> = emptyMap(),
    ): Result<T, DataError.Remote> {
        return executeWithSerializer(query, variables, operationName, headers, serializer())
    }

    suspend fun executeRaw(
        query: String,
        variables: JsonObject? = null,
        operationName: String? = null,
        headers: Map<String, String> = emptyMap(),
    ): Result<String, DataError.Remote> {
        return platformSafeCall(
            execute = { postQuery(query, variables, operationName, headers) },
            handleResponse = { response ->
                when (response.status.value) {
                    in 200..299 -> Result.Success(response.bodyAsText())
                    else -> responseToResult(response)
                }
            },
        )
    }

    fun close() {
        httpClient.close()
    }

    @PublishedApi
    @Suppress("SwallowedException")
    internal suspend fun <T> executeWithSerializer(
        query: String,
        variables: JsonObject?,
        operationName: String?,
        headers: Map<String, String>,
        deserializer: KSerializer<T>,
    ): Result<T, DataError.Remote> {
        val httpResult = safeCall<GraphQlResponse> {
            postQuery(query, variables, operationName, headers)
        }

        return when (httpResult) {
            is Result.Failure -> httpResult
            is Result.Success -> parseResponse(httpResult.data, deserializer)
        }
    }

    private suspend fun postQuery(
        query: String,
        variables: JsonObject?,
        operationName: String?,
        requestHeaders: Map<String, String>,
    ) = httpClient.post {
        url(endpoint)
        contentType(ContentType.Application.Json)
        headers {
            defaultHeaders.forEach { (key, value) -> append(key, value) }
            requestHeaders.forEach { (key, value) -> append(key, value) }
        }
        setBody(GraphQlRequest(query, variables, operationName))
    }

    @Suppress("SwallowedException")
    private fun <T> parseResponse(
        response: GraphQlResponse,
        deserializer: KSerializer<T>,
    ): Result<T, DataError.Remote> {
        if (!response.errors.isNullOrEmpty()) {
            val messages = response.errors.mapNotNull { error ->
                error.jsonObject["message"]?.jsonPrimitive?.content
            }
            logger.warn("GraphQL errors: {}", messages)
            return Result.Failure(DataError.Remote.BAD_REQUEST)
        }

        val data = response.data
            ?: return Result.Failure(DataError.Remote.UNKNOWN)

        return try {
            Result.Success(json.decodeFromJsonElement(deserializer, data))
        } catch (e: SerializationException) {
            logger.error("Failed to deserialize GraphQL response data", e)
            Result.Failure(DataError.Remote.SERIALIZATION)
        }
    }
}

@Serializable
private data class GraphQlRequest(
    val query: String,
    val variables: JsonObject? = null,
    val operationName: String? = null,
)

@Serializable
data class GraphQlResponse(
    val data: JsonElement? = null,
    val errors: List<JsonElement>? = null,
)
