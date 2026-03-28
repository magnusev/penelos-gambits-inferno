package com.penelosgambits.libs.ktor.common.plugins

import io.ktor.server.application.Application
import io.ktor.server.application.ApplicationCall
import io.ktor.server.application.install
import io.ktor.server.plugins.callid.CallId
import io.ktor.server.plugins.callid.callId
import io.ktor.server.plugins.calllogging.CallLogging
import io.ktor.server.request.httpMethod
import io.ktor.server.request.path
import org.slf4j.event.Level
import java.util.UUID

const val TRACE_ID_HEADER = "X-Trace-Id"
const val SESSION_ID_HEADER = "X-Session-Id"
private const val TRACE_ID_MDC_KEY = "traceId"
private const val SESSION_ID_MDC_KEY = "sessionId"

fun Application.configureLogging(
    logFilter: (ApplicationCall) -> Boolean = { it.request.path().startsWith("/api") },
) {
    install(CallId) {
        header(TRACE_ID_HEADER)
        generate { UUID.randomUUID().toString() }
        verify { it.isNotEmpty() }
        replyToHeader(TRACE_ID_HEADER)
    }

    install(CallLogging) {
        level = Level.INFO
        mdc(TRACE_ID_MDC_KEY) { call -> call.callId }
        mdc(SESSION_ID_MDC_KEY) { call -> call.request.headers[SESSION_ID_HEADER] }

        format { call ->
            val status = call.response.status()
            val method = call.request.httpMethod.value
            val path = call.request.path()
            val traceId = call.callId
            "HTTP $method $path → ${status?.value ?: "N/A"} (traceId=$traceId)"
        }

        filter { call -> logFilter(call) }
    }
}

