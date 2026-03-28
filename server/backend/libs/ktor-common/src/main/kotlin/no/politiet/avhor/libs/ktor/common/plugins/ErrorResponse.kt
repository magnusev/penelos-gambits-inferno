package no.politiet.avhor.libs.ktor.common.plugins

import kotlinx.serialization.Serializable

@Serializable
data class ErrorResponse(
    val statusCode: Int,
    val message: String,
    val traceId: String,
)

