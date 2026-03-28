# Implementation Steps

Step-by-step guide for building Penelos Gambits. Each file is one bite-sized task
with clear acceptance criteria and a manual test you can run.

Work through these **in order**. Each step builds on the previous one.

## Phase 1: Foundation — Bot ↔ Engine Communication

Goal: Engine connects, receives `STATE_UPDATE` every tick, can send `COMMAND` back.

| # | Side | Task | Checkpoint? | Status |
|---|------|------|-------------|--------|
| 01 | Bot | Fix stale environment in `CombatTick()` | ✅ Manual | ✅ Done |
| 02 | Bot | Send `CONNECT` when engine connects | | ✅ Done |
| 03 | Engine | Project setup + dependency | | ✅ Done |
| 04 | Engine | WebSocket client with retry loop | ✅ Manual | ✅ Done |
| 05 | Engine | Message DTOs (`kotlinx.serialization`) | | ✅ Done |
| 06 | Engine | MessageRouter + TickStateManager | ✅ Manual | ✅ Done |
| 07 | Bot | Execute commands (CAST/MACRO) | | |
| 08 | Bot | Query handler (answer QUERY) | ✅ Manual | |

## Phase 2: Gambit System — Decision Engine in Kotlin

Goal: Engine evaluates gambit rules and sends the right `COMMAND` each tick.

| # | Side | Task | Checkpoint? | Status |
|---|------|------|-------------|--------|
| 01 | Engine | Domain model (`TickContext`, `UnitState`, ports) | | ✅ Done |
| 02 | Engine | Condition evaluators | | |
| 03 | Engine | Selectors + UnitFilter pipeline | | |
| 04 | Engine | GambitRule + GambitSet + evaluation loop | ✅ Unit tests | |
| 05 | Engine | `GameQueryPort` WebSocket impl + cache | ✅ Manual | |
| 06 | Engine | Wire it all together — first live gambit | ✅ Manual (full loop) | |

