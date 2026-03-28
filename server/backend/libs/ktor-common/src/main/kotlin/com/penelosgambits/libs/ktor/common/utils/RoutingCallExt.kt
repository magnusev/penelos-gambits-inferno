package com.penelosgambits.libs.ktor.common.utils

import io.ktor.server.routing.RoutingCall
import com.penelosgambits.libs.ktor.common.exceptions.PathParameterNotPresentException

inline fun <T> RoutingCall.getPathParameter(name: String, crossinline transform: (String) -> T): T =
    parameters[name]
        ?.let { transform(it) }
        ?: throw PathParameterNotPresentException(fieldName = name)
