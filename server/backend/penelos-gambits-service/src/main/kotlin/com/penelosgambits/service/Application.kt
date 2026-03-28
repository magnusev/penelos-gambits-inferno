package com.penelosgambits.service

import com.penelosgambits.api.websocket.BotConnection
import kotlinx.coroutines.runBlocking

fun main() = runBlocking {
    val connection = BotConnection()
    connection.connectForever()
}

