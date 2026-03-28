# Penelos Gambits - Implementation Plan

## Overview

This document outlines the architecture, evaluates key technical decisions, and provides a detailed
implementation plan for integrating the decision engine (Kotlin) with the WoW bot client (C#/Inferno)
using a Gambit System inspired by Final Fantasy XII.

The system will:
1. **Receive** environment state from the bot on every game tick (~2-3 times per second)
2. **Query** additional game data as needed (debuffs, cooldowns, distances, etc.)
3. **Decide** on the next action using the Gambit System
4. **Execute** the action through commands sent back to the bot

### Terminology

To avoid confusion between WebSocket roles and logical roles, this document uses:

| Term | Refers to | WebSocket role | Language |
|------|-----------|----------------|----------|
| **Bot** | PenelosGambits / Inferno integration | WebSocket **server** (hosts `ws://localhost:8082/`) | C# |
| **Engine** | Penelos Gambits decision engine | WebSocket **client** (connects to the bot) | Kotlin/Ktor |

The bot is the WebSocket server because it is the long-running, hard-to-restart process.
The engine is the WebSocket client because it is easy to restart and iterate on.
This topology allows engine restarts and upgrades without interrupting game integration.

---

## Part 1: Technical Evaluation

### 1.1 OpenAPI Specification Analysis

**Status**: ✅ **Good & Complete**

#### Strengths
- **Clear Message Structure**: All 8 message types well-defined with discriminator on `type`
- **Comprehensive Query API**: 30+ query methods with per-method response data documented in tables
- **Bidirectional Flow**: Clearly separates Bot→Engine and Engine→Bot messages with direction labels
- **Type Safety**: Discriminator mapping ensures only valid message types are processed
- **State Snapshots**: `StateUpdateMessage` provides player, target, group, bosses, GCD, combatTime, mapId
- **Error Handling**: `ExecutionResultMessage` reports success/failure with error string
- **Realistic Examples**: Includes JSON examples for every message type

#### Minor Improvements Needed

1. **Latency/Timeout Expectations**: The spec documents "Commands should be responded within 200ms"
   and "falls back to local logic after 5 seconds", but should also document:
   - Expected response time for QUERY messages (recommend < 50ms per query)
   - What "local logic" fallback means in practice (currently: do nothing)

2. **Connection Recovery**: PING/PONG exists but frequency is only a recommendation ("every 5 seconds").
   Should be promoted to a required parameter with explicit timeout.

3. **Query Efficiency (Phase 2)**: When the gambit system needs multiple queries per tick, add:
   - Per-tick memoization so repeated queries (e.g. `Health(party2)` twice) hit cache
   - Batched multi-query request so a compound condition (`health + debuff`) is one round-trip
   - Keep single-query contract for backward compatibility

#### Verdict
The spec is **sufficient for Phase 1**. Enhancements for query batching should happen before Phase 2.

---

### 1.2 WebSocket vs Alternatives Analysis

**Decision**: ✅ **WebSocket is the Right Choice**

| Aspect | WebSocket | HTTP Polling | gRPC | TCP Raw Socket |
|--------|-----------|--------------|------|----------------|
| **Latency** | Low (persistent) | Medium-High | Very Low | Very Low |
| **Bidirectional Push** | ✅ Yes | ❌ Client polls | ✅ Yes | ✅ Yes |
| **Implementation** | Simple | Simple | Complex | Complex |
| **Debugging** | ✅ Easy (JSON text) | ✅ Easy | ❌ Binary | ❌ Binary |

#### Current Implementation Assessment
- **Bot** (C#): Uses `HttpListener` + `System.Net.WebSockets` to host a WebSocket server — **working**.
- **Engine** (Kotlin/Ktor): Needs a WebSocket **client** using `ktor-client-websockets` to connect to the bot.
  The existing `ktor-client` bundle in `libs.versions.toml` already includes core, content negotiation,
  and OkHttp — just needs the websocket plugin added.

> ⚠️ **Note**: Previous versions of this document incorrectly referenced `ktor-server-websockets`.
> The engine is a WebSocket **client**, not a server. The correct dependency is `ktor-client-websockets`.

#### Inverted Topology (Bot = Server, Engine = Client)

This is intentional and correct:
- The bot is attached to the game process — restarting it means reattaching to the game.
- The engine is a standalone JVM process — restart takes seconds, no game state lost.
- The engine connects to the bot as a WebSocket client. If the engine restarts, the bot
  keeps running and simply accepts a new connection when the engine comes back.

#### Verdict
WebSocket is optimal. The inverted topology is a deliberate operational advantage.

---

## Part 2: System Architecture

### 2.0 Principles Alignment (from `server/dokumentasjon/general-principles.md`)

The implementation follows the team principles in `server/dokumentasjon/general-principles.md`:

- **Immutability by default**: `StateUpdateMessage` is an immutable snapshot. Each tick creates a new one.
- **Composition over inheritance**: Gambit conditions, selectors, and filters are composable building blocks, not deep class hierarchies.
- **Given-When-Then tests**: Use Kotest `BehaviorSpec` for gambit evaluation, query handling, and fallback scenarios.
- **Resilience first**: The system must function even when the engine is unavailable. The bot waits safely.
- **Monorepo visibility**: Shared contracts (`openapi.yml`), documentation, and both client/server code live in one repo.
- **Contract-first design**: Update `server/openapi/openapi.yml` before generating DTOs or writing handlers.
- **Hexagonal architecture**: Split Kotlin code into Domain / API / Data / Service modules as described below.

### 2.1 Kotlin-First Responsibility Split

- **Engine (Kotlin) owns all decision logic**: gambit evaluation, condition composition, selector resolution, priority ordering, fallback strategy.
- **Bot (C#) is a thin adapter**: publish `STATE_UPDATE`, answer `QUERY`, execute `COMMAND`, send `EXECUTION_RESULT`.
- **Bot-hosted WebSocket is intentional**: keep the game integration process stable while allowing engine restarts without restarting the bot.
- **No rule logic in C#**: the bot has zero knowledge of what gambit system or healing priority is being used.
- **Query methods are data ports**: the engine asks questions, the bot looks them up in the game API and returns raw data. The bot doesn't interpret the results.

### 2.2 Kotlin Module Structure (Hexagonal)

Following the principles document, the engine should be structured as:

```
server/backend/penelos-gambits-service/
└── src/main/kotlin/com/penelosgambits/
    ├── domain/                          # Domain module — no external dependencies
    │   ├── model/                       # Immutable data classes (TickState, UnitState, etc.)
    │   ├── gambit/                      # GambitRule, GambitSet, GambitSetPicker
    │   ├── condition/                   # ConditionEvaluator implementations
    │   ├── selector/                    # TargetSelector, UnitFilter pipeline
    │   └── port/                        # Interfaces (GameQueryPort, CommandPort)
    │
    ├── api/                             # API module — WebSocket transport, DTOs
    │   ├── websocket/                   # Ktor WebSocket client, message routing
    │   └── dto/                         # Generated/manual DTOs matching OpenAPI
    │
    ├── data/                            # Data module — implements domain ports
    │   └── gamequery/                   # GameQueryPort impl (sends QUERY over WS, returns result)
    │
    └── service/                         # Service module — wires everything together
        └── Application.kt              # Ktor application entry point
```

Key boundaries:
- **Domain** depends on nothing. `GameQueryPort` is an interface here.
- **Data** implements `GameQueryPort` by sending WebSocket QUERY messages.
- **API** receives `STATE_UPDATE`, passes it to domain, gets back a `CommandIntent`, sends `COMMAND`.
- **Service** wires domain ports to data implementations via DI.

### 2.3 Message Flow

```
Bot (C# / WebSocket Server)                    Engine (Kotlin / WebSocket Client)
─────────────────────────────                   ──────────────────────────────────
                                                 │
Game tick fires                                  │ connects to ws://localhost:8082/
    │                                            │
    ├─ Create Environment from game state        │
    ├─ Build StateUpdateMessage                  │
    ├─ Send STATE_UPDATE ──────────────────────► │ Receive STATE_UPDATE
    │                                            ├─ Update TickState snapshot
    │                                            ├─ Evaluate gambits (priority order)
    │                                            │    ├─ Check conditions (may need query)
    │                                            │    │
    │  ◄──────────── QUERY (HasDebuff, etc.) ───┤    │  (only if data not in STATE_UPDATE)
    │  Look up in game API                       │    │
    │  Send QUERY_RESPONSE ────────────────────► │    ├─ Receive response, resume evaluation
    │                                            │    ├─ Resolve target via selector + filters
    │                                            │    └─ First matching gambit wins
    │                                            │
    │  ◄──────────── COMMAND ───────────────────┤ Send CommandMessage
    │  Execute spell/macro via Inferno API       │
    │  Send EXECUTION_RESULT ──────────────────► │ Log result, update metrics
    │                                            │
    │  (next tick)                               │ (wait for next STATE_UPDATE)
```

### 2.4 Component Breakdown

#### Bot Side (C# — already largely implemented)

| Component | Status | File |
|-----------|--------|------|
| WebSocket server | ✅ Done | `WebSocket/WebSocket.cs` |
| Message type constants | ✅ Done | `Common/messages/MessageType.cs` |
| MessageRouter (dispatch by type) | ✅ Done | `WebSocket/Messages/MessageRouter.cs` |
| StateUpdateMessage serialization | ✅ Done | `Common/messages/StateUpdateMessage.cs` |
| ConnectMessage | ✅ Done | `Common/messages/ConnectMessage.cs` |
| CommandMessage parsing | ✅ Done | `Common/messages/CommandMessage.cs` |
| QueryMessage parsing | ✅ Done | `Common/messages/QueryMessage.cs` |
| QueryResponseMessage | ✅ Done | `Common/messages/QueryResponseMessage.cs` |
| ExecutionResultMessage | ✅ Done | `Common/messages/ExecutionResultMessage.cs` |
| PING/PONG handling | ✅ Done | `MessageRouter.cs` (auto-responds PONG) |
| Rotation tick loop | ⚠️ Partial | `rotation.cs` (see known bugs below) |
| Actual command execution | ❌ TODO | `rotation.cs` (currently logs only, doesn't cast) |
| Query handler (answer QUERY) | ❌ TODO | Needs `QueryHandler.cs` to call Inferno API |

#### Engine Side (Kotlin — to be built)

| Component | Status | Module |
|-----------|--------|--------|
| WebSocket client connection | ❌ TODO | api |
| Message DTOs (kotlinx.serialization) | ❌ TODO | api |
| Message routing by type | ❌ TODO | api |
| TickState snapshot model | ❌ TODO | domain |
| GameQueryPort interface | ❌ TODO | domain |
| GameQueryPort WebSocket impl | ❌ TODO | data |
| GambitRule / GambitSet | ❌ TODO (Phase 2) | domain |
| ConditionEvaluator library | ❌ TODO (Phase 2) | domain |
| TargetSelector / UnitFilter | ❌ TODO (Phase 2) | domain |
| Application wiring | ❌ TODO | service |

#### Known Bugs in Current C# Code

1. **Stale environment in `CombatTick()`**: `_environment` is only created in `OutOfCombatTick()`
   under a conditional branch. `CombatTick()` calls `SendStateUpdate()` but never refreshes
   `_environment`, so it sends stale state. Fix: call `RefreshEnvironment()` at the start of
   every `CombatTick()` and `OutOfCombatTick()`.

2. **Port default vs actual**: `WebSocket.cs` has `_port = 8080` as default, but `rotation.cs`
   already overrides it to `8082`. This is correct as-is, not a bug — just noting that the
   default in `WebSocket.cs` is misleading.

---

## Part 3: Gambit System Overview

### 3.1 Final Fantasy XII Gambit Concept

The gambit system is a priority-ordered list of conditional rules. On each tick, evaluate from
top to bottom. The **first** gambit whose conditions are met **and** whose action is executable wins.

```
Gambit = Conditions + Action + Selector

  Priority 1: [HP < 25% on any ally]  → Cast "Holy Shock"  on [Lowest Health Ally]
  Priority 2: [Boss missing debuff]   → Cast "Flame Shock" on [Boss]
  Priority 3: [Always]                → Cast "Crusader Strike" on [Current Target]
```

### 3.2 Three-Part Structure

**Condition (Evaluator)**: Pure predicate over the current tick context.
- Buff/debuff presence, resource thresholds, combat state, boss casting, distance, cooldown ready, etc.
- Multiple conditions per gambit — all must be true (AND logic).
- Some conditions need data not in `STATE_UPDATE` → trigger a QUERY to the bot.

**Action**: What to do if conditions are met.
- `CAST` — cast a spell (must be in spellbook, must be off cooldown and in range)
- `MACRO` — execute a registered macro
- `NONE` — explicitly do nothing

**Selector (Target Resolution)**: Who to do it on.
- Static: `"player"`, `"target"`, `"boss1"`
- Dynamic: apply a chain of `UnitFilter` steps, pick the result (e.g. lowest-health alive ally in range)

### 3.3 The `CanDoAction()` Check

A gambit matching its conditions is **not enough**. The old system correctly separates two checks:
1. `IsMet(environment)` — are the **conditions** satisfied? (e.g. "an ally is below 50% HP")
2. `CanDoAction(environment)` — is the **action** actually executable right now? (e.g. spell off cooldown, target in range, enough mana)

Both must pass for a gambit to fire. This prevents selecting a gambit whose spell is on cooldown,
which would waste a tick. The engine must replicate this two-phase check.

### 3.4 GambitSet and GambitSetPicker (Strategy Swapping)

The old system has an important pattern beyond individual gambits:

**GambitSet**: A named collection of gambits for a specific scenario. Supports chaining:
- `DoBeforeGambitSet()` — a higher-priority set that runs first (e.g. emergency/interrupt set)
- `GetNextGambitSet()` — a fallback set if nothing in this set matched (e.g. damage filler set)

**GambitSetPicker**: Swaps the active GambitSet based on `mapId`. Different dungeons and raids
use different strategies. The picker caches the current set and only re-evaluates on map change.

Kotlin equivalents:
```kotlin
interface GambitSet {
    val name: String
    val gambits: List<GambitRule>
    val before: GambitSet?    // higher-priority chain (interrupts, emergency)
    val fallback: GambitSet?  // lower-priority chain (filler rotation)
}

interface GambitSetPicker {
    fun pick(mapId: Int): GambitSet
}
```

### 3.5 Gambit Priority System

Gambits within a set are evaluated by priority (1 = highest):
```
Priority 1: Emergency (player < 10% HP, dispel curses)
Priority 2: Group Support (heal lowest ally, buff party)
Priority 3: Debuff Application (apply dots, weaknesses)
Priority 4: Main Rotation (filler damage, generates resources)
Priority 5: Movement/Utility (reposition, out of combat)
```

First gambit where `IsMet() && CanDoAction()` → execute.

### 3.6 Legacy Pattern Mapping (Old System → Kotlin)

| Old C# | Kotlin equivalent | Notes |
|--------|-------------------|-------|
| `Condition` interface | `ConditionEvaluator` | Pure predicate over `TickContext`. Uses `GameQueryPort` for extra data. |
| `ISelector` interface | `TargetSelector` | Returns a single `UnitState?`. |
| `FilterChainSelector` | `FilterPipelineSelector` | Applies `List<UnitFilter>` left-to-right, returns `firstOrNull()` |
| `IUnitFilterChain` | `UnitFilter` | `(List<UnitState>) -> List<UnitState>`. Each filter narrows candidates. |
| `Gambit` | `GambitRule` | `priority + conditions + selector + action + canExecute check` |
| `GambitSet` | `GambitSet` | Named collection with before/fallback chaining. |
| `GambitSetPicker` | `GambitSetPicker` | Swaps strategy by mapId. |
| `Action` (.Cast/.Macro) | `ActionIntent` | Immutable intent → serialized to `CommandMessage`. |

Legacy filter primitives to port (17 existed):
`IsNotDead`, `IsInRange`, `IsOfRole`, `GetLowestUnit`, `GetLowestUnitUnderThreshold`,
`HasBuff`, `HasNotBuff`, `HasDebuff`, `IsNotBuffed`, `HasBuffLessThanStacks`,
`GetUnitWithoutBuff`, `GetUnitWithShortestBuffDuration`, `GetUnitWithDebuffUnderTimeLeft`,
`GetFirstTank`, `GetFirst`, `GetMostDamageTakenOfRole`

Legacy conditions to port (20 existed):
`AlwaysCondition`, `InCombatCondition`, `HasNotBuffCondition`, `IsCastingCondition`,
`TargetHasNotDebuffCondition`, `TargetIsEnemyCondition`, `GroupOverThresholdCondition`,
`NumberOfEnemiesMoreThanCondition`, `HasMoreThanChargesCondition`, etc.

Preserve selector traceability by logging filter steps and candidate reductions each tick
(the old `FilterChainSelector.Log()` pattern).

---

## Part 4: Implementation Plan

### Phase 1: Foundation — Receive State on Every Tick

**Goal**: Engine connects to bot, receives `STATE_UPDATE` every tick, logs it. No decisions yet.

#### 1.1 Bot-Side Fixes (C#)

The bot already has most infrastructure in place. Remaining work:

1. **Fix stale environment bug in `rotation.cs`**:
   - Call `RefreshEnvironment()` at the start of every `CombatTick()` and `OutOfCombatTick()`.

2. **Send CONNECT on new client connection**:
   - Currently `ConnectMessage` exists but is never sent automatically when a client connects.
   - Detect new WebSocket client → broadcast `ConnectMessage` with character name and spec.

3. **Implement query handler** (`QueryHandler.cs`):
   - Receive `QueryMessage`, switch on `method`, look up data via `Inferno.*` API, return `QueryResponseMessage`.
   - Wire into `MessageRouter.OnQueryReceived`.
   - Not all 30+ methods needed for Phase 1 — start with `Health`, `HasBuff`, `HasDebuff`, `CanCast`, `SpellCooldown`.

4. **Implement actual command execution**:
   - Currently `CombatTick()` receives commands but only logs them.
   - Add: if `action == CAST` → `Inferno.Cast(spell, target)`, if `action == MACRO` → execute macro.
   - Send `ExecutionResultMessage` with actual success/failure.

#### 1.2 Engine-Side Setup (Kotlin)

1. **Add WebSocket client dependency** to `libs.versions.toml`:
   ```toml
   ktor-client-websockets = { module = "io.ktor:ktor-client-websockets", version.ref = "ktor" }
   ```
   Add to `penelos-gambits-service/build.gradle.kts` dependencies.

2. **Create message DTOs** using `kotlinx.serialization`:
   ```kotlin
   @Serializable sealed interface WebSocketMessage { val type: String }
   @Serializable data class StateUpdateMessage(...) : WebSocketMessage
   @Serializable data class CommandMessage(...) : WebSocketMessage
   // etc.
   ```

3. **Implement WebSocket client connection**:
   ```kotlin
   val client = HttpClient { install(WebSockets) }

   // Connect to bot — retry forever on failure
   while (isActive) {
       try {
           client.webSocket("ws://localhost:8082/") {
               for (frame in incoming) { /* parse + route */ }
           }
       } catch (e: Exception) {
           logger.warn("Connection lost, retrying in ${backoff}ms", e)
           delay(backoff)
           backoff = (backoff * 2).coerceAtMost(30_000)
       }
   }
   ```

4. **Create `TickStateManager`**:
   - Stores latest deserialized `StateUpdateMessage` as an immutable `TickState`.
   - Logs every received state update (Phase 1 verification).

5. **Implement `MessageRouter`**:
   - Parse incoming JSON, discriminate on `type` field.
   - Route `STATE_UPDATE` → `TickStateManager`.
   - Route `CONNECT` → log character name/spec.
   - Route `QUERY_RESPONSE` → pending query coroutine (Phase 2).
   - Route `EXECUTION_RESULT` → logging.

6. **Update application main class**:
   - The current `build.gradle.kts` has a placeholder mainClass (`DokumentServiceApplicationKt`).
   - Create `PenelosGambitsApplication.kt` and update the config.

#### 1.3 Communication Protocol

1. **Connection establishment**:
   ```
   Engine starts → connects to ws://localhost:8082/
   Bot accepts WebSocket connection
   Bot sends CONNECT { character: "Penelo", spec: "Holy Paladin" }
   Bot begins sending STATE_UPDATE every tick
   Engine receives and logs
   ```

2. **Tick loop** (every ~333ms):
   ```
   Bot: Refresh Environment → build StateUpdateMessage → send
   Engine: Receive → update TickState → (Phase 2: evaluate gambits → send COMMAND)
   ```

3. **Keep-alive**:
   ```
   Engine: send PING every 5 seconds
   Bot: auto-responds PONG (already implemented in MessageRouter)
   ```

4. **Engine restart / downtime**:
   ```
   Engine process stops (restart, crash, redeploy)
   Bot: WebSocket client disconnects → bot removes it from client list
   Bot: Continues running, keeps game integration alive
   Bot: No COMMAND arriving → no remote actions executed (safe idle)
   Bot: Keeps calling SendStateUpdate (broadcasts to 0 clients = no-op)

   Engine process starts back up
   Engine: Reconnects to ws://localhost:8082/ (retry forever with backoff)
   Bot: Accepts new connection, sends CONNECT
   Bot: Resumes sending STATE_UPDATE
   Normal flow continues — no bot restart needed
   ```

#### 1.4 Error Handling

1. **Engine connection loss**:
   - Engine detects closed WebSocket → log → retry with exponential backoff (1s, 2s, 4s, … max 30s).
   - **No cap on retry attempts** — the engine should reconnect forever. The entire point of this
     topology is that the engine can restart at any time.

2. **Bot has no connected engine**:
   - Bot broadcasts to 0 clients = no-op. No crash, no error.
   - No commands arrive → bot does nothing (safe idle).
   - When engine reconnects, flow resumes automatically.

3. **Malformed messages**:
   - Parse error → log + ignore (never crash).
   - Unknown message type → log + ignore.
   - Both sides already handle this (C# `MessageRouter` has try/catch, Kotlin should too).

4. **Query timeout** (Phase 2):
   - If query response doesn't arrive within 100ms → treat condition as unresolved → skip gambit.
   - Never block the tick loop indefinitely.

#### 1.5 Testing Strategy

**Phase 1 manual verification**:
1. Start bot with rotation loaded → WebSocket server starts on port 8082.
2. Start Kotlin engine → connects to bot, logs `CONNECT` message.
3. Enter combat → verify `STATE_UPDATE` messages arriving ~2-3/sec in engine logs.
4. Verify state data is correct: player health, target, bosses, mapId.
5. Stop engine → bot keeps running, no errors.
6. Restart engine → reconnects, STATE_UPDATE flow resumes.
7. Repeat reconnect 5+ times to verify stability.

**Automated tests** (Kotlin):
- Unit tests for message deserialization (parse real JSON from bot).
- Unit tests for `TickStateManager` snapshot updates.
- Integration test: mock WebSocket server → verify engine connects + receives state.
- BehaviorSpec (GWT) style for reconnection scenarios.

---

### Phase 2: Decision Engine — Gambit Evaluation

*After Phase 1 is verified end-to-end.*

#### 2.1 Domain Module

1. **Define core abstractions**:
   - `ConditionEvaluator`: `suspend fun isMet(context: TickContext): Boolean`
   - `TargetSelector`: `suspend fun select(context: TickContext): UnitState?`
   - `UnitFilter`: `fun filter(units: List<UnitState>): List<UnitState>`
   - `GambitRule`: `priority + conditions + selector + action + canExecute`
   - `GambitSet`: named collection with before/fallback chaining
   - `GambitSetPicker`: swap strategy by mapId

2. **Implement `TickContext`**:
   - Wraps the latest `TickState` (from STATE_UPDATE)
   - Holds a `GameQueryPort` reference for querying additional data
   - Contains per-tick query cache (`mutableMapOf<QueryKey, QueryResult>`)
   - A query called twice in the same tick returns cached result (no second round-trip)

3. **Implement `GameQueryPort`**:
   - Domain interface: `suspend fun query(method: String, params: Map<String, Any>): QueryResult`
   - Data-layer impl sends `QUERY` over WebSocket, suspends until `QUERY_RESPONSE` arrives.
   - Batched variant (later optimization): collect queries from a single evaluation pass,
     send them in one batch message, correlate responses.

4. **Implement condition library** (port from old system):
   - Start with most-used: `AlwaysCondition`, `InCombatCondition`, `HasNotBuffCondition`,
     `TargetHasNotDebuffCondition`, `GroupOverThresholdCondition`
   - Each is a small composable class, no inheritance tree.

5. **Implement filter/selector library**:
   - Port: `IsNotDead`, `IsInRange`, `GetLowestUnitUnderThreshold`, `HasDebuff`, etc.
   - `FilterPipelineSelector` applies filters left-to-right, returns `firstOrNull()`.

6. **Evaluation loop**:
   ```kotlin
   fun evaluate(gambitSet: GambitSet, context: TickContext): CommandMessage {
       // Check before-chain first
       gambitSet.before?.let { before ->
           evaluate(before, context).takeIf { it.action != NONE }?.let { return it }
       }

       // Evaluate this set's gambits by priority
       for (gambit in gambitSet.gambits.sortedBy { it.priority }) {
           if (gambit.conditions.all { it.isMet(context) }) {
               val target = gambit.selector.select(context)
               if (target != null && gambit.canExecute(context, target)) {
                   return CommandMessage(action = gambit.action, target = target.unitId, ...)
               }
           }
       }

       // Check fallback chain
       gambitSet.fallback?.let { return evaluate(it, context) }

       return CommandMessage(action = NONE)
   }
   ```

#### 2.2 Query Cache and Batching

- **Per-tick cache**: `TickContext` holds a `Map<CacheKey, QueryResult>` that lives for one tick only.
  Cache key = `method + canonicalized params` (sorted keys to avoid ordering issues).
- **Batch queries**: When a gambit condition needs multiple queries (e.g. health AND debuff),
  collect intents first, send one `BATCH_QUERY` message, wait for `BATCH_QUERY_RESPONSE`.
  Update the per-tick cache with all results at once.
- **OpenAPI update needed**: Add `BATCH_QUERY` and `BATCH_QUERY_RESPONSE` message types before
  implementing. C# side needs a handler for batch queries.

#### 2.3 Testing

- Unit test each `ConditionEvaluator` with mock `TickContext`.
- Unit test each `UnitFilter` with known unit lists.
- Unit test `GambitSet` evaluation including before/fallback chaining.
- Integration test: full tick cycle (state → evaluate → command → result).
- BehaviorSpec scenarios for query caching (same query twice = one WS call).

---

### Phase 3: Advanced Features
*Future sprints*

- Real-time gambit editing via web UI (REST API on Ktor port 8080)
- Decision logging and analytics dashboard
- Per-encounter gambit set templates
- Dynamic gambit set hot-reloading without engine restart
- Performance metrics (decision time per tick, query latency percentiles)

---

## Part 5: Technical Specifications

### 5.1 Port Configuration

| Component | Port | Protocol | Role |
|-----------|------|----------|------|
| Bot (C# / Inferno) | 8082 | WebSocket Server | Hosts WS endpoint at `ws://localhost:8082/` |
| Engine (Kotlin / Ktor) | 8082 | WebSocket Client | Connects to bot's WS endpoint |
| Engine HTTP API (future) | 8080 | HTTP | REST API for monitoring/UI |

**Operational rationale**:
- Bot is the long-running, hard-to-restart process (game integration lifecycle).
- Engine is the easy-to-restart process (fast deployment/iteration).
- This topology allows engine restarts and upgrades without interrupting game integration.

### 5.2 Message Size Estimates

| Message | Typical size | Frequency |
|---------|-------------|-----------|
| `STATE_UPDATE` | 2-5 KB | ~3/sec (every tick) |
| `COMMAND` | 100-300 bytes | ~3/sec (one per tick) |
| `QUERY` | 200-500 bytes | 0-5/tick (on demand) |
| `QUERY_RESPONSE` | 100-2 KB | matches query count |
| `PING`/`PONG` | ~20 bytes | 1 every 5 sec |

No compression or optimization needed. Total bandwidth is negligible.

### 5.3 Database Consideration

**Phase 1-2**: Not needed. All state is in-memory.
**Phase 3**: Consider for gambit set storage, execution history, analytics.
Recommendation: Start with in-memory, evaluate PostgreSQL or SQLite when persistence is needed.

---

## Part 6: Dependencies

### Bot (C#)
All built-in, no new dependencies needed:
- ✅ `System.Net.WebSockets`
- ✅ `System.Net.HttpListener`
- ✅ Custom `JsonParser` (suited to Inferno runtime constraints — do not replace with `System.Text.Json`)

### Engine (Kotlin/Ktor)
Already in project:
- ✅ `ktor-client-core`, `ktor-client-okhttp` (in `ktor-client` bundle)
- ✅ `ktor-serialization-kotlinx-json`
- ✅ `kotlinx-serialization-json`
- ✅ `logback-classic` + `logstash-logback-encoder`
- ✅ `kotest-runner-junit5`, `kotest-assertions-core`, `mockk`

Needs adding:
- ❌ `ktor-client-websockets` — add to `libs.versions.toml` and service dependency

---

## Part 7: Known Risks & Mitigations

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|-----------|
| Engine restart during combat | No decisions for a few seconds | High (by design) | Bot safe-idles, engine reconnects automatically |
| Stale `STATE_UPDATE` data | Decision based on old state | Medium | Timestamp in every message, engine checks age |
| Query response slow/lost | Condition can't evaluate | Medium | Per-query timeout (100ms), skip gambit, try next |
| Wrong query cache key | Stale cached result used | Low | Canonical key builder (sorted params) + unit tests |
| Gambit set has no matching rule | Bot does nothing for a tick | Low | Always include a fallback/filler gambit at lowest priority |
| Bot crashes independently | Engine loses connection | Low | Engine retries forever, bot restart is outside our control |
| Stale environment in CombatTick | STATE_UPDATE sends old data | High | Fix: call RefreshEnvironment() every tick (known bug) |

---

## Part 8: Success Criteria

### Phase 1
- [ ] Engine connects to bot WebSocket on startup
- [ ] Engine receives `CONNECT` message with character name and spec
- [ ] `STATE_UPDATE` arrives every ~333ms in engine logs
- [ ] State data is correct: player health, target, bosses, mapId, GCD
- [ ] Engine can be stopped and restarted without bot restart
- [ ] Bot enters safe idle when engine is disconnected (no actions executed)
- [ ] After engine reconnect, `STATE_UPDATE` flow resumes within 1 second
- [ ] `PING`/`PONG` keep-alive works (engine sends, bot auto-responds)
- [ ] Connection survives 10+ minute session without drops
- [ ] All messages log correctly on both sides

### Phase 2
- [ ] Single gambit condition evaluates correctly with live game data
- [ ] `CanDoAction()` check prevents firing gambits with unusable spells
- [ ] Priority-based evaluation picks correct gambit
- [ ] GambitSet chaining works (before → main → fallback)
- [ ] GambitSetPicker swaps strategy on map change
- [ ] Target selector resolves correct unit via filter pipeline
- [ ] Same query repeated within one tick hits cache (no duplicate outbound query)
- [ ] Batched condition (`health + debuff`) executes as one round-trip
- [ ] COMMAND is executed in bot and EXECUTION_RESULT confirms success
- [ ] Engine handles partial/failed query responses gracefully

---

## Part 9: Timeline Estimate

| Phase | Component | Estimate | Notes |
|-------|-----------|----------|-------|
| 1 | Fix `rotation.cs` stale environment bug | 30 min | Quick fix |
| 1 | Bot: CONNECT auto-send + basic query handler | 2-3 hours | Wire existing message classes |
| 1 | Bot: actual command execution | 1-2 hours | Wire Inferno.Cast/macro |
| 1 | Engine: `ktor-client-websockets` + connection | 2-3 hours | With retry/backoff loop |
| 1 | Engine: message DTOs + deserialization | 2-3 hours | `kotlinx.serialization` |
| 1 | Engine: `MessageRouter` + `TickStateManager` | 2-3 hours | Route + store state |
| 1 | Engine: update mainClass + application entry | 30 min | Replace placeholder |
| 1 | Testing (manual + automated) | 2-3 hours | Verify end-to-end |
| 1 | **Phase 1 Total** | **~12-16 hours** | |
| 2 | Domain: core abstractions + `TickContext` | 3-4 hours | Interfaces + query port |
| 2 | Domain: condition library (10 conditions) | 4-5 hours | Port from old system |
| 2 | Domain: filter/selector library (10 filters) | 3-4 hours | Port from old system |
| 2 | Domain: `GambitSet` + `GambitSetPicker` | 2-3 hours | Chaining + map swap |
| 2 | Data: `GameQueryPort` WebSocket impl + cache | 3-4 hours | Suspend + correlate |
| 2 | Integration: evaluation loop + wiring | 3-4 hours | End-to-end |
| 2 | Testing (unit + integration) | 4-5 hours | GWT/BehaviorSpec |
| 2 | **Phase 2 Total** | **~23-29 hours** | |

**Total for MVP**: ~35-45 hours

---

## Part 10: Next Steps

1. **Fix `rotation.cs`** — Call `RefreshEnvironment()` at the start of `CombatTick()` so state isn't stale.
2. **Add `ktor-client-websockets`** to `libs.versions.toml` and service dependencies.
3. **Update mainClass** in `penelos-gambits-service/build.gradle.kts` from placeholder.
4. **Create Kotlin WebSocket client** — connect to bot with retry loop, receive and log `STATE_UPDATE`.
5. **Create message DTOs** — `kotlinx.serialization` data classes matching OpenAPI spec.
6. **Verify end-to-end** — bot sends state, engine receives and logs, survive reconnect.
7. **Then Phase 2**: define domain contracts (`GambitRule`, `ConditionEvaluator`, `TargetSelector`, `UnitFilter`), port one scenario (emergency healing) end-to-end.

---

## Appendix A: OpenAPI Spec Enhancements (for Phase 2)

```yaml
# Add batched query support (Phase 2 only — not needed for Phase 1)

BatchQueryMessage:
  type: object
  required: [type, batchId, tickId, queries]
  properties:
    type:
      type: string
      enum: [BATCH_QUERY]
    batchId:
      type: string
    tickId:
      type: string
      description: Correlates query cache lifetime to a single game tick
    queries:
      type: array
      minItems: 1
      items:
        type: object
        required: [queryId, method]
        properties:
          queryId:
            type: string
          method:
            type: string
          params:
            type: object
            additionalProperties: true

BatchQueryResponseMessage:
  type: object
  required: [type, batchId, tickId, results]
  properties:
    type:
      type: string
      enum: [BATCH_QUERY_RESPONSE]
    batchId:
      type: string
    tickId:
      type: string
    results:
      type: array
      items:
        type: object
        required: [queryId, success]
        properties:
          queryId:
            type: string
          success:
            type: boolean
          data:
            type: object
            additionalProperties: true
          error:
            type: string
```

Keep existing `QUERY`/`QUERY_RESPONSE` for single queries.
Introduce batch variants as an optimization, not a replacement.

## Appendix B: Gambit Examples (FF XII Style)

```
// Emergency — highest priority, always evaluated first
Gambit 1: [Ally HP < 20%]                  → Cast "Lay on Hands"    on [Lowest HP Ally]
Gambit 2: [Ally has Curse]                 → Cast "Cleanse"         on [Ally with Curse]

// Healing — before damage rotation
Gambit 3: [Ally HP < 50%]                  → Cast "Holy Shock"      on [Lowest HP Ally]
Gambit 4: [2+ Allies HP < 70%]            → Cast "Light of Dawn"   on [Self]

// Debuff application
Gambit 5: [Boss missing "Consecration"]    → Cast "Consecration"    on [Self]

// Main rotation
Gambit 6: [Spell "Crusader Strike" ready]  → Cast "Crusader Strike" on [Current Target]
Gambit 7: [Spell "Judgment" ready]         → Cast "Judgment"        on [Current Target]

// Filler — lowest priority, always matches
Gambit 8: [Always]                         → Cast "Auto Attack"     on [Current Target]
```

---

**Document Version**: 2.0
**Last Updated**: March 28, 2026
**Status**: Ready for Phase 1 Implementation

