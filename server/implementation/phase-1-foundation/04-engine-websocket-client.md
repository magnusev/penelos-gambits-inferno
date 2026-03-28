# Step 04 — Engine: WebSocket Client With Retry Loop

## What to do

Create a WebSocket client that connects to the bot and prints every raw message it receives.
Retry forever on disconnect with exponential backoff.

1. Create `BotConnection.kt` in the api/websocket package:
   ```kotlin
   package com.penelosgambits.api.websocket

   class BotConnection(private val url: String = "ws://localhost:8082/") {
       private val client = HttpClient { install(WebSockets) }
       private val logger = LoggerFactory.getLogger(BotConnection::class.java)

       suspend fun connectForever() {
           var backoff = 1000L
           while (true) {
               try {
                   client.webSocket(url) {
                       logger.info("Connected to bot at {}", url)
                       backoff = 1000L  // reset on successful connect
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
   ```

2. Call it from `Application.kt`:
   ```kotlin
   fun main() = runBlocking {
       val connection = BotConnection()
       connection.connectForever()
   }
   ```

## Files to create / change

- `server/.../api/websocket/BotConnection.kt` (new)
- `server/.../service/Application.kt` (update)

## Manual test — Checkpoint

1. **Bot NOT running**: Start engine. Verify it logs retry messages every 1s, 2s, 4s... up to 30s.
2. **Start bot**: Engine should connect within one retry cycle and log "Connected to bot".
3. If bot sends STATE_UPDATE, you should see raw JSON in the engine logs.
4. **Stop engine, restart it**: Should reconnect and resume receiving.
5. **Stop bot while engine is connected**: Engine logs "Connection lost", starts retrying.
6. **Restart bot**: Engine reconnects automatically.

**Pass**: engine reconnects reliably in all scenarios, raw messages appear in logs.
