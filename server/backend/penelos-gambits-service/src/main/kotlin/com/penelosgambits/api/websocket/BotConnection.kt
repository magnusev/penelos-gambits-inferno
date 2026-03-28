package com.penelosgambits.api.websocket

import com.penelosgambits.domain.port.MessageSender
import io.ktor.client.HttpClient
import io.ktor.client.plugins.websocket.WebSockets
import io.ktor.client.plugins.websocket.webSocket
import io.ktor.websocket.DefaultWebSocketSession
import io.ktor.websocket.Frame
import io.ktor.websocket.readText
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock
import org.slf4j.LoggerFactory

class BotConnection(
    private val messageRouter: MessageRouter,
    private val host: String = "DESKTOP-A5OGBKM",
    private val port: Int = 8082,
    private val path: String = "/",
) : MessageSender {
    private val client = HttpClient { install(WebSockets) }
    private val logger = LoggerFactory.getLogger(BotConnection::class.java)

    private var activeSession: DefaultWebSocketSession? = null
    private val sessionMutex = Mutex()

    override suspend fun send(text: String) {
        sessionMutex.withLock {
            activeSession?.send(Frame.Text(text))
                ?: logger.warn("Cannot send — no active WebSocket session")
        }
    }

    suspend fun connectForever() {
        var backoff = 1000L
        while (true) {
            try {
                client.webSocket(host = host, port = port, path = path) {
                    sessionMutex.withLock { activeSession = this }
                    logger.info("Connected to bot at ws://{}:{}{}", host, port, path)
                    backoff = 1000L
                    try {
                        for (frame in incoming) {
                            if (frame is Frame.Text) {
                                val text = frame.readText()
                                val isStateUpdate = messageRouter.route(text)
                                // Launch tick processing concurrently so the receive
                                // loop isn't blocked while gambit queries await
                                // QUERY_RESPONSE on this same channel.
                                if (isStateUpdate) {
                                    launch { messageRouter.processLatestTick() }
                                }
                            }
                        }
                    } finally {
                        sessionMutex.withLock { activeSession = null }
                    }
                }
            } catch (e: CancellationException) {
                throw e
            } catch (e: java.io.IOException) {
                logger.warn("Connection lost: {}. Retrying in {}ms", e.message, backoff)
            }
            delay(backoff)
            backoff = (backoff * 2).coerceAtMost(30_000L)
        }
    }
}

