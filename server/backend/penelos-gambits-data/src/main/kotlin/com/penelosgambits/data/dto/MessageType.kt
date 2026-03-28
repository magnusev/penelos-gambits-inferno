package com.penelosgambits.data.dto

import kotlinx.serialization.json.Json
import kotlinx.serialization.json.contentOrNull
import kotlinx.serialization.json.jsonObject
import kotlinx.serialization.json.jsonPrimitive

/**
 * Extracts the `type` discriminator from a raw JSON string
 * without deserializing the full message.
 */
fun extractType(json: String): String? {
    return try {
        val obj = Json.parseToJsonElement(json).jsonObject
        obj["type"]?.jsonPrimitive?.contentOrNull
    } catch (_: Exception) {
        null
    }
}

