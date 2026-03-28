package no.politiet.avhor.libs.ktor.common.utils

import io.ktor.server.routing.RoutingCall
import no.politiet.avhor.libs.ktor.common.exceptions.PathParameterNotPresentException

inline fun <T> RoutingCall.getPathParameter(name: String, crossinline transform: (String) -> T): T =
    parameters[name]
        ?.let { transform(it) }
        ?: throw PathParameterNotPresentException(fieldName = name)
