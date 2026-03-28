package com.penelosgambits.domain.model

/**
 * Represents the connection handshake from the bot.
 * Pure domain — no serialization annotations.
 */
data class ConnectionInfo(
    val character: String,
    val spec: String,
)

