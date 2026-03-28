# Penelos Gambits - Implementation Plan

## Overview
This document outlines the architecture, evaluates key technical decisions, and provides a detailed implementation plan for integrating the decision engine (server) with the WoW bot client (PenelosGambits) using the Gambit System from Final Fantasy XII.

The system will:
1. **Receive** environment state from the WoW bot on every game tick (~2-3 times per second)
2. **Query** additional game data as needed (debuffs, cooldowns, distances, etc.)
3. **Decide** on the next action using the Gambit System
4. **Execute** the action through commands sent back to the bot

---

## Part 1: Technical Evaluation

### 1.1 OpenAPI Specification Analysis

**Status**: ✅ **Good & Complete**

#### Strengths
- **Clear Message Structure**: All 8 message types are well-defined with discriminator pattern
- **Comprehensive Query API**: Over 30 query methods covering buffs, spells, distance, item management
- **Bidirectional Flow**: Clearly separates client→server and server→client messages
- **Type Safety**: Discriminator pattern ensures only valid message types are processed
- **State Snapshots**: `StateUpdateMessage` provides rich context (player, target, group, bosses)
- **Error Handling**: `ExecutionResultMessage` allows reporting of failures

#### Minor Improvements Needed
1. **Latency Expectations**: Should document:
   - Expected response time for QUERY messages (recommend < 50ms)
   - Timeout strategy if decision takes too long (recommend 100ms fallback to local logic)
   - How to handle dropped messages or connection interruption

2. **Query Response Data**: Some queries could benefit from explicit type definitions:
   ```
   Currently: "data: {...}" with additionalProperties: true
   Should specify exact schema per method:
   - HasBuff: {remaining: int, stacks: int}
   - CanCast: {reason: string}
   - etc.
   ```

3. **Connection Recovery**: No explicit reconnection strategy or heartbeat timeout definition
   - PING/PONG exists but frequency not specified
   - Recommend: PING every 5 seconds, PONG timeout after 10 seconds

4. **Query Efficiency Contract**: Add explicit support for per-tick memoization and batched queries
   - Cache key should be `tickId + method + normalized params`
   - Repeated query in same tick (for example `health(party2)` twice) should return cached value
   - Add multi-query request/response for one-round-trip conditions (for example `health + debuff`)
   - Keep single-query contract for backward compatibility during migration

#### Verdict
The spec is **sufficient for Phase 1** but should be enhanced before full deployment.

---

### 1.2 WebSocket vs Alternatives Analysis

**Decision**: ✅ **WebSocket is the Right Choice**

#### Why WebSocket
| Aspect | WebSocket | HTTP Polling | gRPC | TCP Raw Socket |
|--------|-----------|--------------|------|----------------|
| **Latency** | Low (persistent) | Medium-High | Very Low | Very Low |
| **State Push** | ✅ Yes | ❌ Client polls | ✅ Yes | ✅ Yes |
| **Implementation** | Simple | Simple | Complex | Complex |
| **Firewall Friendly** | ✅ HTTP/443 | ✅ HTTP/80 | ❌ Custom port | ❌ Custom port |
| **Browser Compatible** | ✅ Yes | ✅ Yes | ❌ No | ❌ No |
| **Debugging** | ✅ Easy (text) | ✅ Easy | ❌ Binary | ❌ Binary |
| **Production Ready** | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |

#### Current Implementation Assessment
- **Client** (C#): Uses `HttpListener` + `System.Net.WebSockets` - good choice
- **Server** (Kotlin/Ktor): Needs WebSocket support
  - Ktor has excellent WebSocket support via `ktor-server-websockets`
  - Handles routing, serialization, and connection management well

#### Potential Issues & Mitigation
1. **Network Latency**: State updates arrive, decision takes time, action gets old data
   - Mitigation: Timestamp each state, allow decision engine to consider age
   
2. **Connection Instability**: Bot/server disconnects mid-combat
   - Mitigation: Implement fallback to local bot logic, reconnect with backoff
   
3. **Message Ordering**: TCP guarantees but consider async processing
   - Mitigation: Add sequence numbers to critical messages

#### Verdict
WebSocket is **optimal for this use case** with the existing C# implementation good; just needs Ktor support on server side.

---

## Part 2: System Architecture

### 2.0 Principles Alignment (from `server/dokumentasjon/general-principles.md`)

The implementation should follow the team principles already defined in `server/dokumentasjon/general-principles.md`:

- **Immutability by default**: Treat `StateUpdateMessage` as immutable snapshot input for each decision tick.
- **Composition over inheritance**: Build gambit conditions/selectors as composable evaluators rather than deep class trees.
- **Given-When-Then tests**: Use `BehaviorSpec` style scenarios for query cache, batch query, timeout, and fallback behavior.
- **Resilience first**: Decision flow should degrade safely when external dependencies or query responses are unavailable.
- **Monorepo-first visibility**: Keep shared contracts, message docs, and examples easy to find in this repository.
- **Contract-first design**: Update `server/openapi/openapi.yml` first, then generate/update DTOs and handlers.
- **Hexagonal architecture**: Split into Domain (gambit rules), API (OpenAPI DTO + websocket API), Data (game query adapters), and Service (wiring/runtime).

### 2.1 Kotlin-First Responsibility Split

To match your preference and keep most logic in Kotlin:

- **Server (Kotlin) owns decision logic**: gambit evaluation, condition composition, selector resolution, priority ordering, and fallback strategy.
- **Client (C#) stays thin**: publish `STATE_UPDATE`, answer `QUERY`/`BATCH_QUERY`, execute `COMMAND`, send `EXECUTION_RESULT`.
- **Bot-hosted WebSocket is intentional**: keep the game integration process stable while allowing Kotlin decision engine restarts without restarting the bot.
- **No rule logic in C#**: avoid embedding selector/condition heuristics in the integration layer.
- **Treat query methods as ports**: game queries are data access adapters into Kotlin domain logic, not where decisions are made.

### 2.2 Message Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                     WoW Game Tick (333ms)                       │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
                   ┌──────────────────────┐
                   │  PenelosGambits      │
                   │  (C# Inferno Bot)    │
                   └──────────────────────┘
                              │
                   (1) Create Environment
                       from game state
                              │
                              ▼
                   ┌──────────────────────┐
                   │  StateUpdateMessage  │
                   │  + ConnectMessage    │
                   │  (on first connect)  │
                   └──────────────────────┘
                              │
                              │ WebSocket
                              │ (send every tick)
                              ▼
        ┌────────────────────────────────────────┐
        │      Penelos Gambits Server            │
        │      (Kotlin/Ktor Decision Engine)     │
        └────────────────────────────────────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        │                     │                     │
    (2) Process         (3) Optional             (4) Evaluate
   STATE_UPDATE        QUERY messages            Gambits
        │                   │                     │
        │       ┌───────────┤                     │
        │       │           │                     │
        ▼       ▼           ▼                     │
    ┌────────────────────────────┐               │
    │  GameState/Context         │               │
    │  - Player, Target, Bosses  │──────────────►│
    │  - Cooldowns (via QUERY)   │   Conditions  │
    │  - Buffs/Debuffs (QUERY)   │               │
    │  - Distance (via QUERY)    │               │
    └────────────────────────────┘               │
                                                │
                                    ┌───────────┴────────────┐
                                    │                        │
                                    ▼                        ▼
                            ┌──────────────────┐    ┌──────────────────┐
                            │  Condition Pass? │    │  Selector        │
                            │  e.g. "Boss has  │    │  e.g. "Lowest    │
                            │   Debuff + HP>50%"    │   Health Ally"   │
                            └──────────────────┘    └──────────────────┘
                                    │                        │
                                    └────────┬───────────────┘
                                             │
                    (5) Execute Gambit       │
                                             ▼
                            ┌────────────────────────────┐
                            │  CommandMessage            │
                            │  - action: CAST/MACRO/NONE │
                            │  - spell: "Holy Shock"     │
                            │  - target: "boss1"         │
                            └────────────────────────────┘
                                             │
                                             │ WebSocket
                                             │ (< 200ms)
                                             ▼
                            ┌────────────────────────────┐
                            │  PenelosGambits (C#)       │
        (6) Execute           │  - Execute spell/macro    │
        the action            │  - Send EXECUTION_RESULT  │
                              └────────────────────────────┘
                                             │
                    ┌────────────────────────┴────────────────┐
                    │                                         │
                    ▼                                         ▼
            ┌──────────────────┐                  ┌──────────────────┐
            │ Success?         │                  │ ✗ Failed?        │
            │ "Spell cast OK"  │                  │ "Out of range"   │
            └──────────────────┘                  └──────────────────┘
                    │                                         │
                    ▼                                         ▼
        EXECUTION_RESULT(success: true)    EXECUTION_RESULT(success: false)
            (echo back to server)            (echo back to server)
                    │
                    └──────────────────────┬─────────────────────┘
                                          │
                                          ▼
                        ┌─────────────────────────────────┐
                        │  Server processes result        │
                        │  (update metrics/debug info)    │
                        │  Loop back to next tick         │
                        └─────────────────────────────────┘
```

### 2.3 Component Breakdown

#### Client Side (PenelosGambits - C#)
- **Environment**: Current game state snapshot
- **WebSocket**: Connection to server
- **StateUpdateMessage**: Sends ~2-3 times/second
- **CommandMessage Handler**: Receives & executes commands
- **ExecutionResultMessage**: Reports success/failure

#### Server Side (Decision Engine - Kotlin)
- **WebSocket Handler**: Manages connections, routes messages
- **GameState Cache**: Holds latest StateUpdateMessage
- **Gambit System**: Evaluates conditions & selectors
- **Query Handler**: Responds to QUERY from client
- **Decision Loop**: Runs every tick, sends CommandMessage

---

## Part 3: Gambit System Overview

### 3.1 Final Fantasy XII Gambit System Concept

The gambit system is a priority-based conditional action system:

```
Gambit Format: [Condition] → [Action] on [Target]

Examples:
1. "If enemy has Poison → Cast Remedy on Ally"
2. "If HP < 50% → Cast Cure on Self"
3. "If ally MP > 50% → Attack target"
4. "If boss casting → Interrupt on boss"
```

### 3.2 Three-Part Gambit Structure

#### Part 1: Condition (Evaluator)
Checks game state to determine if the gambit should execute:
- **Buff/Debuff Conditions**: "Target has X buff", "Boss missing Y debuff"
- **Resource Conditions**: "HP < 50%", "Mana > 30%"
- **Combat Conditions**: "In combat", "Boss casting interrupt"
- **Group Conditions**: "Lowest health ally < 25%", "2+ enemies nearby"
- **Distance Conditions**: "Boss in range", "Target > 10 yards away"
- **Cooldown Conditions**: "Spell ready", "Item not on cooldown"

#### Part 2: Action (Decision)
The spell, macro, or command to execute:
- **CAST**: Cast a spell from spellbook
- **MACRO**: Execute a predefined macro
- **NONE**: Do nothing this tick

#### Part 3: Selector (Target Selection)
Determines which unit to execute the action on:
- **Explicit**: "player", "target", "boss1"
- **Smart**: "lowest health ally", "furthest enemy", "casting boss"
- **Conditional**: "ally with debuff", "enemy without buff"

### 3.3 Gambit Priority System

Gambits are evaluated in order of priority (1 = highest):
```
Priority 1: Emergency (player < 10% HP, dispel curses)
Priority 2: Group Support (heal lowest ally, buff party)
Priority 3: Debuff Application (apply dots, weaknesses)
Priority 4: Main Rotation (filler damage, generates resources)
Priority 5: Movement/Utility (reposition, out of combat)
```

First matching gambit → Execute

### 3.4 Legacy Pattern Mapping (Old System Inspiration)

The old system is a good inspiration source for behavior design, even though integration details have changed:

- Reference `old-penelos-gambits/PenelosGambits/Common/Gambit.cs` as inspiration for a rule aggregate (`priority + conditions + selector + action`).
- Reference `old-penelos-gambits/PenelosGambits/Common/condition/Condition.cs` for small, composable condition interfaces.
- Reference `old-penelos-gambits/PenelosGambits/Common/selectors/FilterChainSelector.cs` for left-to-right unit filtering before target selection.
- Reference `old-penelos-gambits/PenelosGambits/Common/unitFilterChain/IUnitFilterChain.cs` for reusable unit-filter primitives.

Suggested Kotlin mapping:

- Legacy `Condition` -> Kotlin `ConditionEvaluator` (pure predicate over tick context).
- Legacy `ISelector` -> Kotlin `TargetSelector` (single responsibility: choose target).
- Legacy `IUnitFilterChain` -> Kotlin `UnitFilter` pipeline (`List<UnitState> -> List<UnitState>`).
- Legacy `Gambit` -> Kotlin `GambitRule` (`priority + conditions + selector + action`).

Implementation note: preserve selector traceability by logging filter steps and candidate reductions each tick for debugging.

---

## Part 4: Implementation Plan

### Phase 1: Foundation (Receive State - Current Sprint)

**Goal**: Establish bi-directional WebSocket communication with reliable state updates

#### 1.1 Server-Side Setup (Kotlin/Ktor)

**Tasks**:
1. Add WebSocket dependency to `build.gradle.kts`:
   ```kotlin
   implementation(libs.ktor.server.websockets)
   implementation(libs.ktor.serialization.kotlinx.json)
   ```

2. Create message data classes:
   - `ConnectMessage` (client → server)
   - `StateUpdateMessage` with nested objects (client → server)
   - `CommandMessage` (server → client)
   - `ExecutionResultMessage` (client → server)
   - `QueryMessage` (server → client)
   - `QueryResponseMessage` (client → server)
   - `PingMessage`, `PongMessage` (keep-alive)

3. Implement WebSocket route:
   ```kotlin
   webSocket("/ws") {
       // Handle connections
       // Route incoming messages by type
       // Maintain client session state
   }
   ```

4. Create `GameStateManager`:
   - Stores latest state per connected client
   - Tracks timestamp of last update
   - Provides query interface for decision engine
   - Creates a per-tick `QueryExecutionContext` with memoized query results
   - Reuses identical lookups in the same tick (method + params)

5. Create `BatchQueryCoordinator`:
   - Accepts a list of query intents from a single gambit condition
   - Sends one batched request over websocket when possible
   - Correlates responses by `queryId` and returns an aggregated result map
   - Supports partial failures without crashing the decision loop

6. Implement `MessageRouter`:
   - Parse incoming JSON by type
   - Deserialize to appropriate message class
   - Route to handlers (stateUpdate, command result, etc.)
   - Route both single-query and batch-query responses

#### 1.2 Client-Side Enhancement (C#)

**Tasks**:
1. Update `WebSocket.cs`:
   - Change port from 8080 → 8082 (match OpenAPI spec)
   - Add message type routing
   - Implement `OnMessageReceived` event properly

2. Create message parsing:
   - Implement `CommandMessage.FromJson()` (already exists)
   - Add `PingMessage.FromJson()`, `QueryMessage.FromJson()` etc.
   - Add dispatcher based on message type

3. Implement command execution loop:
   - Receive `CommandMessage` from server
   - Parse action (CAST/MACRO/NONE)
   - Execute via Inferno API
   - Send `ExecutionResultMessage` back

4. Already have:
   - ✅ `Environment` class creating state snapshots
   - ✅ `StateUpdateMessage` serialization
   - ✅ Message type definitions
   - ✅ WebSocket infrastructure

#### 1.3 Communication Protocol Implementation

**Tasks**:
1. Handshake on connect:
   ```
   Client connects to ws://localhost:8082/
   
   Server accepts connection
   Server sends: PING (optional keep-alive test)
   Client responds: PONG
   
   Client sends: CONNECT (character name, spec)
   Server processes: RegisterClient(characterName)
   
   Server ready to receive STATE_UPDATE
   ```

2. Tick Loop (every ~333ms):
   ```
   Client: Create Environment → StateUpdateMessage → Send to server
   Server: Receive StateUpdateMessage → Update GameStateManager
   Server: Decision engine checks QueryExecutionContext cache first
   Server: Missing data is requested as one batch query when possible
   Server: Batch response updates per-tick cache
   Server: Send CommandMessage back
   Client: Execute command → Send ExecutionResultMessage
   Server: Process result → Loop
   ```

3. Keep-alive:
   ```
   Server: PING (every 5 seconds)
   Client: PONG (< 1 second)
   Server: If no PONG after 10 seconds → Close connection
   Client: If disconnected → Reconnect with exponential backoff
   ```

4. Backend restart / temporary downtime behavior:
   ```
   Kotlin engine restarts or is temporarily unavailable
   Bot process remains running (no restart of game integration)
   Bot executes no remote commands while disconnected (safe idle / wait mode)
   Kotlin reconnects to bot websocket endpoint
   Normal tick processing and command execution resumes automatically
   ```

#### 1.4 Error Handling

**Implementation**:
1. **Connection Loss**:
   - Client: Detect closed WebSocket → log error → wait 1s → reconnect
   - Server: Detect closed connection → clean up client state → ready for reconnect
   - Max reconnect attempts: 5 (with backoff: 1s, 2s, 4s, 8s, 16s)

2. **Message Timeout**:
   - Server expecting CommandMessage from decision engine
   - If > 150ms with no response → send COMMAND(action: NONE)
   - If decision takes > 200ms → use fallback

3. **Batch Query Timeout / Partial Results**:
   - If one query in a batch times out, continue with available results
   - Prefer same-tick cache; otherwise use safe fallback value and mark condition as unresolved
   - If critical condition data is unresolved, send COMMAND(action: NONE)

4. **Malformed Messages**:
   - Parse error → log + ignore (don't crash)
   - Unknown message type → log + ignore
   - Missing required fields → send error response

5. **Backend Downtime (Kotlin unavailable)**:
   - Bot must keep running and keep game integration alive while decision engine is down
   - While disconnected, bot waits for backend reconnect and does not execute remote decisions
   - Kotlin side should retry reconnect with backoff until session is restored
   - On reconnect, resume normal command loop without requiring bot restart

#### 1.5 Testing Strategy

**Manual Testing**:
1. Start bot with WebSocket enabled
2. Start server with WebSocket listener
3. Connect bot to server (watch handshake)
4. Verify STATE_UPDATE arrives ~2-3/second
5. Send test COMMAND → verify execution
6. Send test QUERY → verify response
7. Disconnect → verify reconnection

**Automated Testing**:
1. Unit tests for message serialization/deserialization
2. Integration tests for WebSocket message routing
3. Load tests: 5-10 concurrent bot connections

---

### Phase 2: Decision Engine (Gambit System Evaluation)
*Future sprint - after Phase 1 complete*

Will include:
- Build Kotlin domain module for gambits first; no C# rule classes.
- Implement `GambitRule`, `ConditionEvaluator`, `TargetSelector`, and `UnitFilter` abstractions.
- Implement reusable selector/filter primitives inspired by legacy chain patterns:
  - `IsNotDead`
  - `IsInRange`
  - `HasDebuff`
  - `LowestHealthUnderThreshold`
- Implement condition library in Kotlin (combat, buffs/debuffs, resources, cooldowns, throttling).
- Integrate with `QueryExecutionContext` so conditions/selectors can request missing data without transport leakage.
- Evaluate gambits by priority, resolve one action intent, and return only final `CommandMessage` to client.
- Keep robust fallbacks: unresolved critical condition -> `NONE` and continue next tick.

---

### Phase 3: Advanced Features
*Future sprints*

- Weighted condition matching
- Macro system integration
- Real-time gambit editing via UI
- Decision logging & analytics
- Machine learning condition suggestions

---

## Part 5: Technical Specifications

### 5.1 Port Configuration

| Component | Port | Protocol | Role |
|-----------|------|----------|------|
| PenelosGambits (WoW Bot) | 8082 | WebSocket Server | Hosts WebSocket for server to connect |
| Penelos Gambits Server | 8082 | WebSocket Client | Connects to bot as client |
| Ktor HTTP (future) | 8080 | HTTP | REST API for UI/monitoring |

**Note**: Currently bot runs WebSocket **server** and remote runs **client** (unusual but valid)

Operational rationale:
- Bot is the long-running, hard-to-restart process (game integration lifecycle)
- Kotlin engine is the easy-to-restart process (fast deployment/iteration)
- This topology allows Kotlin restarts and upgrades without interrupting game integration

### 5.2 Message Size Constraints

Based on OpenAPI spec:
- **StateUpdateMessage**: ~2-5 KB typical (player, target, group, up to 4 bosses)
- **CommandMessage**: ~100-300 bytes
- **QueryMessage**: ~200-500 bytes
- **QueryResponseMessage**: ~100-2 KB

No optimization needed at Phase 1.

### 5.3 Database Consideration

**For Phase 1**: Not needed
**For Future**: Consider storing:
- Gambit templates
- Execution history
- Performance metrics
- Error logs

Recommendation: Start with in-memory, migrate to PostgreSQL in Phase 3

---

## Part 6: Dependencies & Libraries

### Client (C#)
- ✅ `System.Net.WebSockets` (built-in)
- ✅ `System.Net.HttpListener` (built-in)
- `System.Text.Json` (consider for JSON parsing instead of custom)

### Server (Kotlin/Ktor)
- `ktor-server-websockets`
- `ktor-server-serialization`
- `kotlinx.serialization` (for JSON)
- `kotlin-logging` (logging)
- `junit5` + `kotest` (testing - already in project)

---

## Part 7: Known Risks & Mitigations

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|-----------|
| Network latency spike | Decision based on stale state | Medium | Cache state, add age timestamp |
| Client/server sync issues | Wrong command sent | Low | Message IDs, sequence numbers |
| Gambit parsing errors | Rotation breaks | Medium | Extensive validation, fallback to NONE |
| WebSocket connection drop mid-combat | Bot frozen | Low | Reconnection + local fallback |
| Kotlin engine restart window | No decisions available briefly | High | Bot waits in safe idle until backend reconnects |
| Decision engine takes >200ms | Action queues up | Medium | Timeout + NONE action, async processing |
| Query response slow | Blocking decision loop | Medium | Parallel queries, cache common queries |
| Wrong cache key normalization | Incorrect reused result | Medium | Canonical key builder + tests for param ordering |
| Partial batch-query failure | Incomplete condition evaluation | Medium | Per-item status handling + safe fallback policy |

---

## Part 8: Success Criteria

### Phase 1 Completion Criteria
- [ ] Bot connects to server on startup
- [ ] StateUpdateMessage arrives every 333ms (±50ms)
- [ ] Server receives all state data correctly
- [ ] Server sends CommandMessage → bot executes within 200ms
- [ ] ExecutionResultMessage confirms execution
- [ ] Same query repeated within one tick hits cache (no duplicate outbound query)
- [ ] Combined condition (`health + debuff`) executes as one batch query round-trip
- [ ] Connection survives 5+ minute session
- [ ] Graceful reconnection on disconnect
- [ ] Kotlin engine can be restarted while bot stays running (no game integration restart)
- [ ] During backend downtime bot enters safe idle/wait mode and executes no remote commands
- [ ] After Kotlin reconnect, command flow resumes automatically
- [ ] Logging shows all message types flowing

### Phase 2 (Gambit System)
- [ ] Single gambit condition evaluates correctly
- [ ] Priority-based gambit selection works
- [ ] Smart target selection resolves targets
- [ ] Action executes as specified

---

## Part 9: Timeline Estimate

| Phase | Component | Estimate | Notes |
|-------|-----------|----------|-------|
| 1 | Ktor WebSocket setup | 2-3 hours | Straightforward |
| 1 | Message classes (Kotlin) | 3-4 hours | Serialization + parsing |
| 1 | MessageRouter | 2-3 hours | Type discriminator |
| 1 | GameStateManager | 2-3 hours | Thread-safe caching |
| 1 | C# client updates | 2-3 hours | Message handlers |
| 1 | Integration testing | 3-4 hours | Manual + automated |
| 1 | **Phase 1 Total** | **~15-20 hours** | **1 full sprint** |
| 2 | Gambit evaluator | 8-10 hours | Condition engine |
| 2 | Target selector | 4-6 hours | Smart matching |
| 2 | Decision loop | 4-5 hours | Integration |
| 2 | Testing | 4-5 hours | |
| 2 | **Phase 2 Total** | **~20-26 hours** | **2 sprints** |

**Total for MVP**: 35-46 hours (4-5 weeks)

---

## Part 10: Next Steps

1. **Update OpenAPI contract first** - Add query cache semantics and batch query message schemas in `server/openapi/openapi.yml`.
2. **Define Kotlin gambit domain contracts** - Create `GambitRule`, `ConditionEvaluator`, `TargetSelector`, and `UnitFilter` before transport handlers.
3. **Create Kotlin message classes** - Model `StateUpdateMessage`, `QueryMessage`, `BatchQueryMessage`, response variants, and `CommandMessage`.
4. **Implement Ktor WebSocket route** - Handle `/ws` connections and batched query dispatch.
5. **Build MessageRouter + QueryExecutionContext** - Route by type discriminator and cache per tick.
6. **Enhance C# WebSocket** - Keep as thin adapter for state/query/command execution only.
7. **Port one legacy-inspired scenario end-to-end** - Example: emergency healing chain using condition + selector + filter pipeline.
8. **Integration test** - Verify cache hits, batch query behavior, and end-to-end flow.
9. **Then**: Move to full Phase 2 gambit library expansion.

---

## Appendix: OpenAPI Spec Enhancements (Recommended)

```yaml
# Add to StateUpdateMessage
responseTimeMs:
  type: integer
  description: Estimated time since last decision loop ran, in milliseconds
  example: 50

# Add to QueryMessage
timeout:
  type: integer
  description: How long server should wait for response, in milliseconds
  default: 100
  minimum: 50
  maximum: 500

# Add a batched query contract
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
      description: Correlates query cache lifetime to a game tick
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

# Backward-compatibility note
# Keep QueryMessage/QueryResponseMessage while introducing batch variants.
# Server can prefer batch and fall back to single-query based on client capability.

# Add connection parameters
ConnectionParameters:
  type: object
  properties:
    heartbeatIntervalSeconds:
      type: integer
      default: 5
    heartbeatTimeoutSeconds:
      type: integer
      default: 10
    maxReconnectAttempts:
      type: integer
      default: 5
    maxDecisionTimeMs:
      type: integer
      default: 200
```

---

## Appendix: Final Fantasy XII Gambit Examples (Reference)

```
// Emergency Healing
Gambit 1: Condition "Ally HP < 25%" → Action "Cast Cure" on "Lowest Health Ally"
Gambit 2: Condition "Ally HP < 50%" → Action "Cast Cure II" on "Lowest Health Ally"

// Debuff Application
Gambit 3: Condition "Boss missing Poison" → Action "Cast Poison" on "Boss"
Gambit 4: Condition "Boss missing Silence" → Action "Cast Silence" on "Boss"

// Defensive
Gambit 5: Condition "Self HP < 50%" → Action "Cast Protect" on "Self"
Gambit 6: Condition "Ally Silenced" → Action "Cast Esuna" on "Silenced Ally"

// Main Rotation
Gambit 7: Condition "Target in range" → Action "Cast Holy" on "Target"
Gambit 8: Condition "Mana > 60%" → Action "Cast Aero" on "Target"

// Utility
Gambit 9: Condition "Out of Combat" → Action "Cast Haste" on "Self"
Gambit 10: Condition "Never" → Action "NONE" (do nothing)
```

This shows the flexibility: conditions + actions + targets = complete behavior

---

**Document Version**: 1.1
**Last Updated**: March 28, 2026
**Status**: Ready for Phase 1 Implementation


