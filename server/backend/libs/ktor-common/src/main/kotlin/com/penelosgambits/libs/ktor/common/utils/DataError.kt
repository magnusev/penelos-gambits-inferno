package com.penelosgambits.libs.ktor.common.utils

sealed interface DataError : Error {
    enum class Remote : DataError {
        BAD_REQUEST,
        REQUEST_TIMEOUT,
        UNAUTHORIZED,
        FORBIDDEN,
        NOT_FOUND,
        CONFLICT,
        TOO_MANY_REQUESTS,
        NO_INTERNET,
        PAYLOAD_TOO_LARGE,
        INTERNAL_SERVER_ERROR,
        SERVICE_UNAVAILABLE,
        SERVER_ERROR,
        SERIALIZATION,
        UNKNOWN
    }
}
