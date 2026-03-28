package com.penelosgambits.domain.port

/**
 * Port for sending messages to the bot.
 * Implemented by BotConnection in the service layer.
 */
fun interface MessageSender {
    suspend fun send(text: String)
}

