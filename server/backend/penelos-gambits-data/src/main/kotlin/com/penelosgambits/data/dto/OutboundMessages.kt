package com.penelosgambits.data.dto

import kotlinx.serialization.Serializable
import kotlinx.serialization.json.JsonObject

// ─────────────────────────────────────────────
//  Messages from Engine → Bot (outbound)
// ─────────────────────────────────────────────

@Serializable
data class CommandDto(
    val type: String = "COMMAND",
    val commandId: String,
    val action: String,
    val spell: String? = null,
    val target: String? = null,
    val macro: String? = null,
)

@Serializable
data class QueryDto(
    val type: String = "QUERY",
    val queryId: String,
    val method: String,
    val params: JsonObject? = null,
)

@Serializable
data class PingDto(
    val type: String = "PING",
)

