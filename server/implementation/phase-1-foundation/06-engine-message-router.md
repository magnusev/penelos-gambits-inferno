# Step 06 — Engine: MessageRouter + TickStateManager

## What to do

1. Create `TickStateManager` in `domain/model/` or `api/`:
   ```kotlin
   class TickStateManager {
       private val _currentState = AtomicReference<StateUpdateDto?>(null)
       val currentState: StateUpdateDto? get() = _currentState.get()

       fun update(state: StateUpdateDto) {
           _currentState.set(state)
           logger.info("Tick: mapId={}, player.hp={}, target={}, bosses={}",
               state.mapId,
               state.player?.health,
               state.target?.name ?: "none",
               state.bosses.size
           )
       }
   }
   ```

2. Create `MessageRouter` in `api/websocket/`:
   ```kotlin
   class MessageRouter(private val tickStateManager: TickStateManager) {
       fun route(rawJson: String) {
           val type = extractType(rawJson) ?: return
           when (type) {
               "CONNECT"          -> handleConnect(rawJson)
               "STATE_UPDATE"     -> handleStateUpdate(rawJson)
               "QUERY_RESPONSE"   -> handleQueryResponse(rawJson)
               "EXECUTION_RESULT" -> handleExecutionResult(rawJson)
               "PONG"             -> { /* keep-alive ack, ignore */ }
               else               -> logger.warn("Unknown message type: {}", type)
           }
       }
   }
   ```

3. Wire into `BotConnection`: instead of logging raw JSON, pass it to `MessageRouter.route()`.

## Files to create / change

- `server/.../api/websocket/MessageRouter.kt` (new)
- `server/.../api/websocket/TickStateManager.kt` (new, or domain/model/)
- `server/.../api/websocket/BotConnection.kt` (update to use router)

## Manual test — Checkpoint

1. Start bot, start engine.
2. Engine logs should show structured output:
   ```
   Tick: mapId=2549, player.hp=95, target=Ragnaros, bosses=1
   ```
3. Verify CONNECT is logged: `Connected character: Penelo (Holy Paladin)`.
4. Verify ticks arrive ~2-3 per second.
5. Stop and restart engine. Verify it reconnects and ticks resume.

**Pass**: structured tick data logged correctly, CONNECT message parsed.
