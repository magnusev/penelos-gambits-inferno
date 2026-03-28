package com.penelosgambits.api.websocket

import io.ktor.client.HttpClient
import io.ktor.client.plugins.websocket.WebSockets
import io.ktor.client.plugins.websocket.webSocket
import io.ktor.websocket.Frame
import io.ktor.websocket.readText
import kotlinx.coroutines.delay
import org.slf4j.LoggerFactory

class BotConnection(private val url: String = "ws://DESKTOP-A5OGBKM:8082/") {
    private val client = HttpClient { install(WebSockets) }
    private val logger = LoggerFactory.getLogger(BotConnection::class.java)

    suspend fun connectForever() {
        var backoff = 1000L
        while (true) {
            try {
                client.webSocket(url) {
                    logger.info("Connected to bot at {}", url)
                    backoff = 1000L // reset on successful connect
                    for (frame in incoming) {
                        if (frame is Frame.Text) {
                            val text = frame.readText()
                            logger.info("Received: {}", text)
                        }
                    }
                }
            } catch (e: Exception) {
                logger.warn("Connection lost: {}. Retrying in {}ms", e.message, backoff)
            }
            delay(backoff)
            backoff = (backoff * 2).coerceAtMost(30_000L)
        }
    }
}

