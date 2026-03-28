package no.politiet.avhor.libs.ktor.common.plugins

import io.ktor.http.HttpStatusCode
import io.ktor.server.application.ApplicationCall
import io.ktor.server.plugins.callid.callId
import io.ktor.server.response.respond
import java.util.UUID

val ApplicationCall.traceId: String
    get() = callId ?: UUID.randomUUID().toString()

suspend fun ApplicationCall.respondError(status: HttpStatusCode, message: String) {
    respond(status, ErrorResponse(statusCode = status.value, message = message, traceId = traceId))
}
