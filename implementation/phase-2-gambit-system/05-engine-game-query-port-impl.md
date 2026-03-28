# Step 05 — Engine: GameQueryPort WebSocket Implementation + Cache

## What to do

1. Implement `WebSocketGameQueryPort` in `data/gamequery/`:
   ```kotlin
   class WebSocketGameQueryPort(private val botConnection: BotConnection) : GameQueryPort {

       private val pendingQueries = ConcurrentHashMap<String, CompletableDeferred<QueryResult>>()

       override suspend fun query(method: String, params: Map<String, Any>): QueryResult {
           val queryId = generateQueryId()
           val deferred = CompletableDeferred<QueryResult>()
           pendingQueries[queryId] = deferred

           val queryMsg = QueryDto(queryId = queryId, method = method, params = buildJsonParams(params))
           botConnection.send(Json.encodeToString(queryMsg))

           return withTimeoutOrNull(100) { deferred.await() }
               ?: QueryResult(success = false)  // timeout fallback
       }

       // Called by MessageRouter when QUERY_RESPONSE arrives
       fun handleResponse(response: QueryResponseDto) {
           pendingQueries.remove(response.queryId)?.complete(
               QueryResult(success = response.result, data = response.data?.toMap() ?: emptyMap())
           )
       }
   }
   ```

2. Update `BotConnection` to expose a `send(text: String)` method that writes to the
   active WebSocket session.

3. Wire `MessageRouter` to call `queryPort.handleResponse()` when QUERY_RESPONSE arrives.

4. The per-tick cache already exists in `TickContext` (Step 01). Verify it works:
   calling `context.query("Health", mapOf("unit" to "player"))` twice should only send
   one QUERY over the wire.

## Files to create / change

- `server/.../data/gamequery/WebSocketGameQueryPort.kt` (new)
- `server/.../api/websocket/BotConnection.kt` (add send method)
- `server/.../api/websocket/MessageRouter.kt` (wire query responses)

## Manual test — Checkpoint

1. Start bot + engine.
2. From engine code, trigger a query:
   ```kotlin
   val result = queryPort.query("Health", mapOf("unit" to "player"))
   logger.info("Player health from query: {}", result)
   ```
3. Check bot logs show QUERY received and QUERY_RESPONSE sent.
4. Check engine logs show the result.
5. Trigger the same query twice in one tick, verify only one QUERY is sent to bot
   (check bot logs for duplicate).

**Pass**: queries flow end-to-end, cache prevents duplicates.
