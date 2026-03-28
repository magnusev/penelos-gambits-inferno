package no.politiet.avhor.libs.ktor.common.client

import io.ktor.client.HttpClient
import io.ktor.client.engine.HttpClientEngine
import io.ktor.client.engine.okhttp.OkHttp
import io.ktor.client.plugins.HttpTimeout
import io.ktor.client.plugins.contentnegotiation.ContentNegotiation
import io.ktor.client.plugins.defaultRequest
import io.ktor.client.plugins.observer.ResponseObserver
import io.ktor.client.statement.request
import io.ktor.http.fullPath
import io.ktor.serialization.kotlinx.json.json
import kotlinx.serialization.json.Json
import no.politiet.avhor.libs.ktor.common.plugins.SESSION_ID_HEADER
import no.politiet.avhor.libs.ktor.common.plugins.TRACE_ID_HEADER
import org.slf4j.LoggerFactory
import org.slf4j.MDC
import java.util.UUID

private const val DEFAULT_TIMEOUT_MS = 20_000L
private const val TRACE_ID_MDC_KEY = "traceId"
private const val SESSION_ID_MDC_KEY = "sessionId"

object HttpClientFactory {

    private val logger = LoggerFactory.getLogger(javaClass)

    fun create(
        clientName: String,
        useDefaultHeaders: Boolean = true,
        engine: HttpClientEngine = OkHttp.create(),
        timeoutMs: Long = DEFAULT_TIMEOUT_MS,
        jsonConfig: Json = Json {
            ignoreUnknownKeys = true
            encodeDefaults = true
        },
    ): HttpClient = HttpClient(engine) {

        install(ContentNegotiation) {
            json(json = jsonConfig)
        }

        install(HttpTimeout) {
            socketTimeoutMillis = timeoutMs
            requestTimeoutMillis = timeoutMs
        }

        defaultRequest {
            if (useDefaultHeaders) {
                val traceId = MDC.get(TRACE_ID_MDC_KEY) ?: UUID.randomUUID().toString()
                if (!headers.contains(TRACE_ID_HEADER)) {
                    headers.append(TRACE_ID_HEADER, traceId)
                }

                MDC.get(SESSION_ID_MDC_KEY)?.let { sessionId ->
                    if (!headers.contains(SESSION_ID_HEADER)) {
                        headers.append(SESSION_ID_HEADER, sessionId)
                    }
                }
            }
        }

        install(ResponseObserver) {
            onResponse { response ->
                val elapsed = response.responseTime.timestamp - response.requestTime.timestamp
                logger.info(
                    "[$clientName] " +
                            "← ${response.request.method.value} ${response.request.url.fullPath} " +
                            "→ ${response.status.value} ${response.status.description} " +
                            "(${elapsed} ms)",
                )
            }
        }
    }
}
