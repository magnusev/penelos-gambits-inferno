# Implementation Guide: Holy Shock on Lowest Health (2 Charges)

**Gambit:** *If the player is in combat, and Holy Shock has 2 charges, cast Holy Shock on the party/raid member with the lowest health percentage.*

---

## Architecture Overview

This gambit spans **two systems**:

| Layer | Technology | Role |
|-------|-----------|------|
| **C# Bot** (`PenelosGambits/`) | C# / Inferno API | Reads game state, responds to queries, executes commands |
| **Kotlin Engine** (`server/`) | Kotlin / Ktor | Evaluates gambit rules, sends queries & commands to the bot |

**Data flow per tick:**
```
Bot: CombatTick()
  → RefreshEnvironment()           // snapshot game state
  → ProcessPendingQueries()        // answer any QUERY from engine
  → SendStateUpdate(environment)   // push STATE_UPDATE to engine
  → ExecuteNextCommand()           // execute any COMMAND from engine

Engine receives STATE_UPDATE:
  → Maps DTO → domain TickState
  → Evaluates gambit rules (conditions → selector → action)
  → Sends QUERY messages for live checks (CanCast, SpellCharges, etc.)
  → Sends COMMAND message with the winning action
```

---

## What Needs to Change

### Gap: Group member health data is NOT sent to the engine

Currently `GroupState` only sends `type` and `size`. The engine has no per-member health data to select the lowest-health member. **This is the main prerequisite.**

---

## Step-by-Step Implementation

### Step 1 — C# Bot: Add member data to `GroupState`

**File:** `PenelosGambits/Common/messages/StateUpdateMessage.cs`

Add a `MemberState` inner class and include members in `GroupState`:

```csharp
public class MemberState
{
    public string UnitId { get; private set; }
    public int Health { get; private set; }
    public bool IsDead { get; private set; }

    public MemberState(Unit member)
    {
        UnitId = member.Id;
        Health = member.HealthPercentage;
        IsDead = member.HealthPercentage == 0;
    }

    public string ToJson()
    {
        var sb = new StringBuilder();
        sb.Append("{");
        sb.Append("\"unitId\":" + MessageBase.EscapeJson(UnitId) + ",");
        sb.Append("\"health\":" + Health + ",");
        sb.Append("\"isDead\":" + MessageBase.BoolToJson(IsDead));
        sb.Append("}");
        return sb.ToString();
    }
}
```

Update `GroupState` to include members:

```csharp
public class GroupState
{
    public string GroupType { get; private set; }
    public int Size { get; private set; }
    public List<MemberState> Members { get; private set; }     // ← NEW

    public GroupState(Group group)
    {
        // ... existing GroupType logic ...
        Size = Inferno.GroupSize();

        Members = new List<MemberState>();                     // ← NEW
        foreach (var member in group.GetMembers())             // ← NEW
        {                                                      // ← NEW
            Members.Add(new MemberState(member));              // ← NEW
        }                                                      // ← NEW
    }

    public string ToJson()
    {
        var sb = new StringBuilder();
        sb.Append("{");
        sb.Append("\"type\":" + MessageBase.EscapeJson(GroupType) + ",");
        sb.Append("\"size\":" + Size + ",");

        // ── NEW: serialize members array ──
        sb.Append("\"members\":[");
        for (int i = 0; i < Members.Count; i++)
        {
            if (i > 0) sb.Append(",");
            sb.Append(Members[i].ToJson());
        }
        sb.Append("]");

        sb.Append("}");
        return sb.ToString();
    }
}
```

**What this gives us:** Every tick, the engine now receives `party1`, `party2`, etc. with their health % and dead status.

---

### Step 2 — Kotlin Data Layer: Add member DTO

**File:** `server/backend/penelos-gambits-data/src/main/kotlin/com/penelosgambits/data/dto/InboundMessages.kt`

Add a `MemberStateDto` and update `GroupStateDto`:

```kotlin
@Serializable
data class MemberStateDto(
    val unitId: String,
    val health: Int,
    val isDead: Boolean,
)

@Serializable
data class GroupStateDto(
    val type: String,
    val size: Int,
    val members: List<MemberStateDto> = emptyList(),   // ← NEW (default for backwards compat)
)
```

---

### Step 3 — Kotlin Domain Model: Add members to `GroupInfo`

**File:** `server/backend/penelos-gambits-domain/src/main/kotlin/com/penelosgambits/domain/model/TickState.kt`

```kotlin
data class GroupInfo(
    val type: String,
    val size: Int,
    val members: List<GroupMember> = emptyList(),       // ← NEW
)

data class GroupMember(                                 // ← NEW
    val unitId: String,
    val health: Int,
    val isDead: Boolean,
)
```

---

### Step 4 — Kotlin Mapper: Wire DTO → domain

**File:** `server/backend/penelos-gambits-data/src/main/kotlin/com/penelosgambits/data/mapper/StateMapper.kt`

Update the `group` mapping:

```kotlin
group = group?.let {
    GroupInfo(
        type = it.type,
        size = it.size,
        members = it.members.map { m ->                // ← NEW
            GroupMember(                                // ← NEW
                unitId = m.unitId,                      // ← NEW
                health = m.health,                      // ← NEW
                isDead = m.isDead,                      // ← NEW
            )                                          // ← NEW
        },                                             // ← NEW
    )
},
```

---

### Step 5 — Kotlin: Create `SpellChargesCondition`

**File:** `server/backend/penelos-gambits-domain/src/main/kotlin/com/penelosgambits/domain/condition/QueryConditions.kt`

Add at the end of the file:

```kotlin
/**
 * True when [spell] has at least [minCharges] charges.
 * Sends a SpellCharges query to the bot.
 *
 * The bot's QueryHandler returns: { "charges": N }
 * with success = true if charges > 0.
 */
class SpellChargesCondition(
    private val spell: String,
    private val minCharges: Int,
) : ConditionEvaluator {
    override suspend fun isMet(context: TickContext): Boolean {
        val result = context.query("SpellCharges", mapOf("spell" to spell))
        val charges = (result.data["charges"] as? Number)?.toInt() ?: 0
        return charges >= minCharges
    }
}
```

**How the query works end-to-end:**

1. Engine calls `context.query("SpellCharges", mapOf("spell" to "Holy Shock"))`
2. `TickContext` delegates to `GameQueryPort.query()` → sends JSON over WebSocket:
   ```json
   { "type": "QUERY", "queryId": "abc123", "method": "SpellCharges", "params": { "spell": "Holy Shock" } }
   ```
3. C# `QueryHandler.HandleSpellCharges()` calls `Inferno.SpellCharges("Holy Shock")` and responds:
   ```json
   { "type": "QUERY_RESPONSE", "queryId": "abc123", "result": true, "data": { "charges": 2 } }
   ```
4. The condition checks `charges >= 2` → `true`

---

### Step 6 — Kotlin: Create `GroupMembersUnitProvider`

**File:** `server/backend/penelos-gambits-domain/src/main/kotlin/com/penelosgambits/domain/selector/StaticSelectors.kt`

Add a unit-provider function that converts group members + player into `UnitState` candidates:

```kotlin
/**
 * Provides all group members (+ the player) as UnitState candidates.
 * Used with FilterPipelineSelector to find healing targets.
 */
fun groupMembersWithPlayer(context: TickContext): List<UnitState> {
    val candidates = mutableListOf<UnitState>()

    // Add the player
    context.state.player?.let { p ->
        candidates.add(
            UnitState(
                unitId = "player",
                name = null,
                health = p.health,
                castingSpellId = p.castingSpellId,
                isDead = p.health <= 0,
            )
        )
    }

    // Add group members
    context.state.group?.members?.forEach { m ->
        candidates.add(
            UnitState(
                unitId = m.unitId,
                name = null,
                health = m.health,
                castingSpellId = 0,
                isDead = m.isDead,
            )
        )
    }

    return candidates
}
```

---

### Step 7 — Kotlin: Wire the gambit rule

**File:** `server/backend/penelos-gambits-domain/src/main/kotlin/com/penelosgambits/domain/gambit/GambitSets.kt`

Add the new gambit to `defaultGambitSet`:

```kotlin
import com.penelosgambits.domain.condition.InCombatCondition
import com.penelosgambits.domain.condition.SpellChargesCondition
import com.penelosgambits.domain.condition.SpellReadyCondition
import com.penelosgambits.domain.selector.FilterPipelineSelector
import com.penelosgambits.domain.selector.groupMembersWithPlayer
import com.penelosgambits.domain.selector.filters.IsNotDeadFilter
import com.penelosgambits.domain.selector.filters.LowestHealthFilter

val defaultGambitSet = GambitSet(
    name = "Default",
    before = emergencyGambits,
    gambits = listOf(
        // ── NEW: Holy Shock on lowest health when 2 charges ──
        GambitRule(
            priority = 1,
            name = "Holy Shock (2 charges → lowest HP)",
            conditions = listOf(
                InCombatCondition(),
                SpellReadyCondition("Holy Shock"),
                SpellChargesCondition("Holy Shock", 2),
            ),
            selector = FilterPipelineSelector(
                unitProvider = ::groupMembersWithPlayer,
                filters = listOf(
                    IsNotDeadFilter(),
                    LowestHealthFilter(),
                ),
            ),
            action = ActionIntent.Cast("Holy Shock"),
        ),

        // existing: offensive Holy Shock on target
        GambitRule(
            priority = 2,
            name = "Holy Shock",
            conditions = listOf(InCombatCondition(), TargetExistsCondition(), SpellReadyCondition("Holy Shock")),
            selector = CurrentTargetSelector(),
            action = ActionIntent.Cast("Holy Shock"),
        ),
    ),
    fallback = fillerGambits,
)
```

---

### Step 8 — Verify the C# cast flow

When the engine resolves this gambit, it sends a `COMMAND` like:

```json
{ "type": "COMMAND", "commandId": "xyz", "action": "CAST", "spell": "Holy Shock", "target": "party2" }
```

The `CommandExecutor.ExecuteCast()` method already handles targeted casts on party members:

```csharp
// From CommandExecutor.cs (already implemented)
if (target != null && target != "target")
{
    Inferno.Cast("focus_" + target);               // focus_party2 macro → /focus party2
    string macroName = SpellMacroRegistry.GetMacroName(spell);
    ActionQueuer.QueueAction(macroName ?? spell);   // queue "cast_holy_shock" → /cast [@focus] Holy Shock
    return true;
}
```

This **already works** because:
- `rotation.cs` line 48 registers: `SpellMacroRegistry.Register("Holy Shock", "cast_holy_shock", "/cast [@focus] Holy Shock")`
- `rotation.cs` lines 42-45 registers all targeting macros (`focus_party1`, etc.)
- The actual cast happens on the **next tick** via `ActionQueuer.CastQueuedActionIfExists()`

**No changes needed in the C# command execution flow.**

---

## Evaluation Flow Summary

```
Tick N:
  ┌─ Engine receives STATE_UPDATE with group members health data
  │
  ├─ Evaluate "Holy Shock (2 charges → lowest HP)":
  │   ├─ InCombatCondition        → check state.player.inCombat == true  ✓
  │   ├─ SpellReadyCondition      → QUERY "CanCast" { spell: "Holy Shock" }
  │   │                             Bot: Inferno.CanCast("Holy Shock") → true  ✓
  │   ├─ SpellChargesCondition    → QUERY "SpellCharges" { spell: "Holy Shock" }
  │   │                             Bot: Inferno.SpellCharges("Holy Shock") → 2  ✓
  │   │
  │   ├─ Selector: FilterPipelineSelector
  │   │   ├─ unitProvider: groupMembersWithPlayer()
  │   │   │   → [player(85%), party1(60%), party2(92%), party3(45%), party4(78%)]
  │   │   ├─ IsNotDeadFilter → remove dead
  │   │   └─ LowestHealthFilter → party3 (45%)
  │   │
  │   └─ Result: Cast("Holy Shock") on party3
  │
  └─ Send COMMAND: { action: "CAST", spell: "Holy Shock", target: "party3" }

Tick N (bot handles command):
  → CommandExecutor.ExecuteCast()
  → Inferno.Cast("focus_party3")            // set focus to party3
  → ActionQueuer.Queue("cast_holy_shock")   // queue the @focus macro

Tick N+1 (bot):
  → ActionQueuer.CastQueuedActionIfExists()
  → Inferno.Cast("cast_holy_shock")         // executes /cast [@focus] Holy Shock
  → Holy Shock lands on party3 ✓
```

---

## Files Changed Summary

| File | Change |
|------|--------|
| `PenelosGambits/Common/messages/StateUpdateMessage.cs` | Add `MemberState` class; update `GroupState` to include members list |
| `server/.../data/dto/InboundMessages.kt` | Add `MemberStateDto`; add `members` field to `GroupStateDto` |
| `server/.../domain/model/TickState.kt` | Add `GroupMember` data class; add `members` field to `GroupInfo` |
| `server/.../data/mapper/StateMapper.kt` | Map `MemberStateDto` → `GroupMember` in group mapping |
| `server/.../domain/condition/QueryConditions.kt` | Add `SpellChargesCondition` |
| `server/.../domain/selector/StaticSelectors.kt` | Add `groupMembersWithPlayer()` unit provider function |
| `server/.../domain/gambit/GambitSets.kt` | Add the gambit rule using new condition + selector |

---

## Unit Tests to Add

### `SpellChargesConditionTest`
```kotlin
@Test
fun `returns true when charges meet threshold`() = runTest {
    val queryPort = mockk<GameQueryPort>()
    coEvery { queryPort.query("SpellCharges", mapOf("spell" to "Holy Shock")) } returns
        QueryResult(success = true, data = mapOf("charges" to 2))

    val context = TickContext(state = someTickState(), queryPort = queryPort)
    val condition = SpellChargesCondition("Holy Shock", 2)

    assertTrue(condition.isMet(context))
}

@Test
fun `returns false when charges below threshold`() = runTest {
    val queryPort = mockk<GameQueryPort>()
    coEvery { queryPort.query("SpellCharges", mapOf("spell" to "Holy Shock")) } returns
        QueryResult(success = true, data = mapOf("charges" to 1))

    val context = TickContext(state = someTickState(), queryPort = queryPort)
    val condition = SpellChargesCondition("Holy Shock", 2)

    assertFalse(condition.isMet(context))
}
```

### `GroupMembersWithPlayerTest`
```kotlin
@Test
fun `includes player and all group members`() {
    val state = TickState(
        // ...
        player = PlayerState(health = 80, spec = "Paladin: Holy", castingSpellId = 0, inCombat = true, isMoving = false),
        group = GroupInfo(type = "party", size = 3, members = listOf(
            GroupMember("party1", 60, false),
            GroupMember("party2", 90, false),
        )),
        // ...
    )
    val context = TickContext(state = state, queryPort = mockk())

    val units = groupMembersWithPlayer(context)

    assertEquals(3, units.size)
    assertEquals("player", units[0].unitId)
    assertEquals("party1", units[1].unitId)
    assertEquals("party2", units[2].unitId)
}
```

### Integration test for full gambit evaluation
Test that when the player is in combat, Holy Shock has 2 charges, and party1 is at 45% HP, the evaluator returns `Cast("Holy Shock")` targeting `party1`.

