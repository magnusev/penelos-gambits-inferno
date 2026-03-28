package com.penelosgambits.service

import com.penelosgambits.api.websocket.BotConnection
import com.penelosgambits.api.websocket.MessageRouter
import com.penelosgambits.api.websocket.TickProcessor
import com.penelosgambits.api.websocket.TickStateManager
import com.penelosgambits.data.gamequery.WebSocketGameQueryPort
import kotlinx.coroutines.runBlocking

fun main(): Unit = runBlocking {
    val tickStateManager = TickStateManager()
    val messageRouter = MessageRouter(tickStateManager)
    val connection = BotConnection(messageRouter)

    val queryPort = WebSocketGameQueryPort(messageSender = connection)
    val tickProcessor = TickProcessor(queryPort = queryPort, messageSender = connection)

    messageRouter.queryPort = queryPort
    messageRouter.tickProcessor = tickProcessor

    connection.connectForever()
}

