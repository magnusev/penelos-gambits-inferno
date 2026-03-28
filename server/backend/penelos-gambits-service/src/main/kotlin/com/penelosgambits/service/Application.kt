package com.penelosgambits.service

import com.penelosgambits.api.websocket.BotConnection
import com.penelosgambits.api.websocket.MessageRouter
import com.penelosgambits.api.websocket.TickStateManager
import kotlinx.coroutines.runBlocking

fun main() = runBlocking {
    val tickStateManager = TickStateManager()
    val messageRouter = MessageRouter(tickStateManager)
    val connection = BotConnection(messageRouter)
    connection.connectForever()
}

