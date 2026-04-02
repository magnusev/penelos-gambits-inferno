﻿# Migration Guide: OOP Gambit System → Single-File Functional Style

> **📁 Proof of Concept:** See [`poc/`](poc/) for a working single-file rotation that passes both compilation and security validation. Run `dotnet build poc/CompileCheck/CompileCheck.csproj` and `dotnet run --project poc/SecurityValidator/SecurityValidator.csproj` to verify.

## Context

The Inferno runtime security validator enforces strict constraints on loaded rotation files:

| Constraint | Detail |
|---|---|
| **Allowed `using` directives** | `System`, `System.Collections.Generic`, `System.Drawing`, `System.Linq`, `System.IO`, `InfernoWow.API` |
| **Allowed base classes** | `Rotation`, `Plugin` (only these two may appear in `: BaseClass`) |
| **Namespace** | The rotation must be wrapped in `namespace InfernoWow.Modules { }`. `Rotation` and `Setting` live in this namespace. `Inferno` is in `InfernoWow.API`. |
| **One class per file** | Only a single class definition is permitted in the loaded `.cs` file |
| **No long string literals** | Strings > ~2000 chars are blocked as "potential encoded payload". The validator uses **naive quote pairing** across the entire file - it counts characters between consecutive `"` characters. Long stretches of code without any string literals (e.g. helper methods using only parameter variables) create large gaps that the validator interprets as one giant string literal. Fix: reorder sections so string-heavy code (like `GetGroupMembers` with `"raid"`, `"party"`, `"player"`) is interleaved between string-free helper methods. Use the `poc/Analyze` tool to check gaps. |
| **No banned namespaces** | `System.Diagnostics`, `System.Text`, `System.Net.Http`, `System.Threading.Tasks` are all blocked |
| **No `Environment.` access** | ALL `Environment.` references are blocked (pattern-matched). This includes `System.Environment.TickCount`. Use `DateTime.UtcNow` instead. |
| **Comments are scanned too** | The validator does **naive text pattern matching** — it does NOT skip `//` comments or `///` doc-comments. Mentioning banned words like `Stopwatch`, `System.Diagnostics`, or even `class hierarchy` in a comment will trigger a block. |
| **C# version: no value tuples** | The runtime compiler does NOT support C# 7+ value tuples `(int, string)`. Use if-chains or plain methods instead. |
| **No lambda expressions or LINQ** | ~~Initially thought to be blocked~~ — **LINQ and lambdas ARE allowed**. The actual issue was the "long string literal" validator (see above). LINQ code that uses parameter variables (no inline string literals) creates quote-free stretches that trigger the naive scanner. Fix: interleave string-heavy sections between LINQ-heavy sections. |
| **API doc bug: `Health()` returns raw HP** | Despite the API doc saying `Health(unit)` returns "Current HP percentage (0-100)", it actually returns **raw HP** (e.g. `369540`). Use `MaxHealth()` to calculate percentage: `(Health * 100) / MaxHealth`. |

The current build system concatenates ~70 separate `.cs` files into one mega-file. Every custom class (conditions, actions, selectors, gambit sets, units, groups, etc.) becomes a separate class definition in that file — **all of which are blocked**.

---

## Verdict: Is this migration a good idea?

**Yes — you have no choice.** The validator is non-negotiable. However, it is also a **net positive** in disguise:

- The concatenated mega-file was already brittle (using-directive stripping, long-string detection, namespace collisions).
- A functional style inside a single `Rotation` class is actually **closer to how the Inferno API was designed** (see the Quick Start example in API-DOC.md).
- You can still keep the OOP project for **development, IntelliSense, and unit testing**, and generate the flat rotation file as a build artifact (code-gen).

---

## High-Level Strategy

```
┌──────────────────────────────────────────────────────┐
│  KEEP: OOP project (PenelosGambits.csproj)           │
│  - Used for development, IntelliSense, testing       │
│  - Classes, interfaces, abstractions stay as-is      │
│  - NOT shipped to Inferno                            │
└──────────────┬───────────────────────────────────────┘
               │ Build step (code-gen / template)
               ▼
┌──────────────────────────────────────────────────────┐
│  OUTPUT: Single rotation.cs file                     │
│  - One class: `public class PaladinHolyPvE : Rotation` │
│  - No custom classes, interfaces, or inheritance     │
│  - All logic expressed as methods, delegates,        │
│    lambdas, local functions, and data structures     │
│  - Only allowed `using` directives                   │
└──────────────────────────────────────────────────────┘
```

You have two approaches to get there:

| Approach | Description | Recommended? |
|---|---|---|
| **A) Manual rewrite** | Rewrite each rotation as a self-contained `Rotation` class by hand | Good for 1-2 specs |
| **B) Code-gen from OOP model** | Keep OOP project, write a generator that emits valid flat code | Good if you maintain many specs |

This guide focuses on **Approach A** (manual rewrite) since it is simpler and the codebase is small enough. Approach B notes are at the end.

---

## Architecture of the Target File

```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.IO;
using InfernoWow.API;

public class PaladinHolyPvE : Rotation
{
    // ── Constants ────────────────────────────────────────
    // Spell names, macro names, macro texts — all as const/static strings

    // ── State ────────────────────────────────────────────
    // Mutable fields: throttle timestamps, cached environment snapshot, boss list

    // ── Rotation lifecycle ───────────────────────────────
    // LoadSettings(), Initialize(), CombatTick(), OutOfCombatTick(), OnStop()

    // ── Environment snapshot ─────────────────────────────
    // A private method (or struct-like tuple) that refreshes per-tick state

    // ── Gambit engine (functional) ───────────────────────
    // Gambits as data (list of tuples/records), evaluated with LINQ

    // ── Condition functions ──────────────────────────────
    // Static or instance helper methods: IsInCombat(), IsSpellReady(), etc.

    // ── Selector functions ───────────────────────────────
    // Methods that return a unit token: GetLowestAlly(), GetDebuffedAlly(), etc.

    // ── Action functions ─────────────────────────────────
    // Methods that perform casts: CastOnFocus(), CastPersonal(), CastOnEnemy()
}
```

---

## Step-by-Step Migration

### Step 1: Establish the allowed `using` block

```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.IO;
using InfernoWow.API;
```

These are the ONLY six you may use. In particular:
- **No `System.Diagnostics`** → Replace `Stopwatch` with `DateTime.UtcNow` for timing. Do NOT use `Environment.TickCount` — `Environment.` is blocked.
- **No `System.Threading.Tasks`** → No async/await (not needed anyway).
- **No `InfernoWow.Modules`** → The `Rotation` base class is provided by `InfernoWow.API` at runtime even though your local dev stub is outside that namespace. Do NOT add the `namespace InfernoWow.Modules { }` wrapper.
- **Watch your comments** → The validator scans comments too. Do NOT write banned words (e.g. `Stopwatch`, `System.Diagnostics`, `class hierarchy`) even in `//` comments.

### Step 2: Replace `Stopwatch` (Throttler)

The `Throttler` uses `System.Diagnostics.Stopwatch` which is banned. `Environment.TickCount` is also banned (`Environment.` pattern-matched). Use `DateTime.UtcNow` instead:

**OOP (current):**
```csharp
private Stopwatch stopwatch;
public bool IsOpen() => !hasStarted || stopwatch.ElapsedMilliseconds >= throttleTimeMs;
public void Restart() { hasStarted = true; stopwatch.Restart(); }
```

**Functional (target) — inline helper methods:**
```csharp
private Dictionary<string, long> _throttleTimestamps = new Dictionary<string, long>();

private long GetTimestampMs()
{
    return DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
}

private bool ThrottleIsOpen(string key, int intervalMs)
{
    if (!_throttleTimestamps.ContainsKey(key)) return true;
    return (GetTimestampMs() - _throttleTimestamps[key]) >= intervalMs;
}

private void ThrottleRestart(string key)
{
    _throttleTimestamps[key] = GetTimestampMs();
}
```

### Step 3: Replace the Unit class hierarchy with a simple data carrier

**OOP (current):** `Unit` (abstract) → `PartyUnit`, `RaidUnit`, `PlayerUnit` with `Focus()` method.

**Functional (target):** A string unit token (e.g. `"player"`, `"party1"`, `"raid3"`) is all you need. Inferno API already works with string tokens directly.

```csharp
// No Unit class needed. Use string tokens directly.
// Helpers:
private List<string> GetGroupMembers()
{
    var members = new List<string> { "player" };
    if (Inferno.InRaid())
    {
        for (int i = 1; i <= 40; i++)
        {
            string token = "raid" + i;
            if (!string.IsNullOrEmpty(Inferno.UnitName(token))) members.Add(token);
        }
    }
    else if (Inferno.InParty())
    {
        for (int i = 1; i <= 4; i++)
        {
            string token = "party" + i;
            if (!string.IsNullOrEmpty(Inferno.UnitName(token))) members.Add(token);
        }
    }
    return members;
}

private int UnitHealth(string unit) => Inferno.Health(unit);
private bool UnitIsDead(string unit) => Inferno.IsDead(unit);
```

### Step 4: Replace Condition classes with `Func<bool>` or helper methods

**OOP (current):**
```csharp
new InCombatCondition()                          // class : Condition
new IsSpellOffCooldownCondition("Judgment")      // class : Condition
new UnitUnderThresholdCondition("player", 75)    // class : Condition
new MinimumGroupMembersUnderThreshold(60, 2)     // class : Condition
new PlayerSecondaryPowerAtLeast(3, 9)            // class : Condition
new ThrottledCondition(2000)                     // class : Condition (stateful)
```

**Functional (target):** Either inline lambdas or named helper methods.

```csharp
// Condition helpers — all return bool
private bool IsInCombat() => Inferno.InCombat("player");
private bool IsSpellReady(string spell) => Inferno.SpellCooldown(spell) <= 200;
private bool UnitUnder(string unit, int pct) => Inferno.Health(unit) < pct;
private bool HolyPower() => Inferno.Power("player", 9);
private bool HolyPowerAtLeast(int n) => Inferno.Power("player", 9) >= n;
private bool HolyPowerLessThan(int n) => Inferno.Power("player", 9) < n;

private bool GroupMembersUnder(int pct, int minCount)
{
    return GetGroupMembers().Count(u => !UnitIsDead(u) && UnitHealth(u) < pct) >= minCount;
}

private string LowestAllyUnder(int pct, string spell)
{
    return GetGroupMembers()
        .Where(u => !Inferno.IsDead(u)
                     && Inferno.Health(u) < pct
                     && Inferno.SpellInRange(spell, u))
        .OrderBy(u => Inferno.Health(u))
        .FirstOrDefault();
}
```

### Step 5: Replace Action classes with cast helper methods

**OOP (current):** `PersonalAction`, `FriendlyTargetedAction`, `EnemyTargetedAction` subclasses with `Cast()` / `Cast(Unit)`.

**Functional (target):**

```csharp
// Personal cast (no target needed)
private bool CastPersonal(string spell)
{
    if (!Inferno.CanCast(spell, "player")) return false;
    Inferno.Cast(spell);
    return true;
}

// Targeted cast via focus macro
private bool CastOnFocus(string unit, string macroName)
{
    Inferno.Cast("focus_" + unit);   // focus the unit
    Inferno.Cast(macroName);          // cast [@focus] macro
    return true;
}

// Enemy cast
private bool CastOnEnemy(string spell)
{
    if (!Inferno.CanCast(spell, "target")) return false;
    Inferno.Cast(spell);
    return true;
}
```

### Step 6: Replace the Gambit / GambitSet abstraction with a priority list

This is the core transformation. The OOP version builds a list of `Gambit` objects with conditions, selectors, and actions. In the functional version, this becomes a **list of prioritized lambdas** evaluated top-to-bottom.

**Option A: Simple if-chain (easiest, most readable)**

```csharp
public override bool CombatTick()
{
    if (Inferno.IsDead("player")) return false;

    // -- Defensives --
    if (IsInCombat() && IsSpellReady("Divine Protection") && UnitUnder("player", 75))
    {
        Inferno.Cast("Divine Protection");
        return true;
    }

    // -- Cooldowns --
    if (IsInCombat() && IsSpellReady("Avenging Wrath") && GroupMembersUnder(60, 2))
    {
        Inferno.Cast("Avenging Wrath");
        return true;
    }

    // -- Holy Power spenders --
    if (IsInCombat() && HolyPowerAtLeast(3))
    {
        var target = LowestAllyUnder(90, "Word of Glory");
        if (target != null)
        {
            CastOnFocus(target, "cast_word_of_glory");
            return true;
        }
    }

    // ... and so on for every gambit
    
    // -- Damage fallthrough --
    return DamageTick();
}
```

**Option B: Data-driven gambit list (preserves extensibility)**

```csharp
// A gambit is now a named tuple of (priority, description, condition, execute)
private List<(int pri, string name, Func<bool> condition, Func<bool> execute)> _healGambits;
private List<(int pri, string name, Func<bool> condition, Func<bool> execute)> _dmgGambits;

// Built once in Initialize() or in constructor
private void BuildGambits()
{
    _healGambits = new List<(int, string, Func<bool>, Func<bool>)>
    {
        (-2, "Divine Protection if player < 75%",
            () => IsInCombat() && IsSpellReady("Divine Protection") && UnitUnder("player", 75),
            () => { Inferno.Cast("Divine Protection"); return true; }
        ),
        (-1, "Avenging Wrath if 2+ under 60%",
            () => IsInCombat() && IsSpellReady("Avenging Wrath") && GroupMembersUnder(60, 2),
            () => { Inferno.Cast("Avenging Wrath"); return true; }
        ),
        (0, "Divine Toll",
            () => IsInCombat() && IsSpellReady("Divine Toll")
                  && GroupMembersUnder(80, 2) && HolyPowerLessThan(3),
            () => {
                var t = LowestAllyInRange("Divine Toll");
                if (t == null) return false;
                CastOnFocus(t, "cast_divine_toll");
                return true;
            }
        ),
        // ... etc
    };
}

// Generic evaluator — replaces GambitSet.HandleGambitChain()
private bool RunGambits(List<(int pri, string name, Func<bool> condition, Func<bool> execute)> gambits)
{
    var match = gambits
        .OrderBy(g => g.pri)
        .FirstOrDefault(g => g.condition());

    if (match.execute != null)
        return match.execute();

    return false;
}

public override bool CombatTick()
{
    if (Inferno.IsDead("player")) return false;
    if (RunGambits(_healGambits)) return true;
    return RunGambits(_dmgGambits);
}
```

> **Recommendation:** Use **Option A** — the Inferno runtime does NOT support C# 7+ value tuples, so Option B is not viable. If-chains with helper methods are simple, readable, and fully compliant.

### Step 7: Replace GambitSetPicker / dungeon-specific GambitSets

The dungeon-specific gambit sets (Proving Grounds, Magister's Terrace, etc.) become **additional gambit lists** selected by map ID.

```csharp
private List<(int, string, Func<bool>, Func<bool>)> GetDungeonGambits(int mapId)
{
    switch (mapId)
    {
        case 480: // Proving Grounds
            return new List<(int, string, Func<bool>, Func<bool>)>
            {
                (1, "Dispel Aqua Bomb",
                    () => IsInCombat() && IsSpellReady("Cleanse")
                          && AnyAllyHasDebuff("Aqua Bomb"),
                    () => {
                        var t = GetAllyWithDebuff("Aqua Bomb", "Cleanse");
                        if (t == null) return false;
                        CastOnFocus(t, "cast_cleanse");
                        return true;
                    }
                ),
            };
        // ... other dungeons
        default:
            return new List<(int, string, Func<bool>, Func<bool>)>();
    }
}

public override bool CombatTick()
{
    if (Inferno.IsDead("player")) return false;

    int mapId = Inferno.GetMapID();

    // Dungeon-specific gambits run first (highest priority)
    if (RunGambits(GetDungeonGambits(mapId))) return true;

    // Default healing gambits
    if (RunGambits(_healGambits)) return true;

    // Damage gambits
    return RunGambits(_dmgGambits);
}
```

### Step 8: Replace ActionBook with direct Spellbook/Macro registration

**OOP (current):** `PaladinHolyActionBook` implements `ActionBook` interface, returns lists.

**Functional (target):** Just register directly in `Initialize()`.

```csharp
public override void Initialize()
{
    // Spells
    Spellbook.Add("Avenging Wrath");
    Spellbook.Add("Blessing of Freedom");
    Spellbook.Add("Cleanse");
    Spellbook.Add("Divine Protection");
    Spellbook.Add("Divine Toll");
    Spellbook.Add("Flash of Light");
    Spellbook.Add("Holy Light");
    Spellbook.Add("Holy Shock");
    Spellbook.Add("Judgment");
    Spellbook.Add("Light of Dawn");
    Spellbook.Add("Shield of the Righteous");
    Spellbook.Add("Word of Glory");

    // Macros (focus-cast patterns)
    Macros.Add("cast_word_of_glory", "/cast [@focus] Word of Glory");
    Macros.Add("cast_flash_of_light", "/cast [@focus] Flash of Light");
    Macros.Add("cast_holy_light", "/cast [@focus] Holy Light");
    Macros.Add("cast_holy_shock_def", "/cast [@focus] Holy Shock");
    Macros.Add("cast_cleanse", "/cast [@focus] Cleanse");
    Macros.Add("cast_divine_toll", "/cast [@focus] Divine Toll");
    Macros.Add("cast_blessing_of_freedom", "/cast [@focus] Blessing of Freedom");

    // Focus macros (for targeting)
    Macros.Add("focus_player", "/focus player");
    for (int i = 1; i <= 4; i++)
        Macros.Add("focus_party" + i, "/focus party" + i);
    for (int i = 1; i <= 28; i++)
        Macros.Add("focus_raid" + i, "/focus raid" + i);
    Macros.Add("target_enemy", "/targetenemy");

    // Build gambit lists
    BuildGambits();

    Inferno.PrintMessage("Penelos Gambits - Holy Paladin loaded!", Color.Green);
}
```

### Step 9: Replace Logger

`Logger` uses `System.IO` (which IS allowed) but also `System.Diagnostics` through `Throttler`. You have two options:

1. **Keep file logging** — rewrite `Throttler` to use `DateTime.UtcNow` (see Step 2). File I/O via `System.IO` is allowed.
2. **Use `Inferno.PrintMessage()` only** — simpler, no file I/O needed, but less detailed.

```csharp
// Simple log helper
private void Log(string msg)
{
    // Option 1: Console only
    Inferno.PrintMessage(msg, Color.White);

    // Option 2: File (System.IO is allowed)
    // File.AppendAllText("penelos_log.txt", DateTime.Now + " " + msg + "\n");
}
```

### Step 10: Wrap in `namespace InfernoWow.Modules`

The rotation must be wrapped in `namespace InfernoWow.Modules`. This is where `Rotation` and `Setting` live. `Inferno` is accessed via `using InfernoWow.API;`.

```csharp
using InfernoWow.API;

namespace InfernoWow.Modules
{
public class PaladinHolyPvE : Rotation
{
    // ...
}
}
```

---

## Mapping Cheat-Sheet: OOP Concept → Functional Equivalent

| OOP Concept | Files | Functional Replacement |
|---|---|---|
| `Condition` interface | `Condition.cs` + 14 implementations | `Func<bool>` lambdas or `private bool MethodName()` helpers |
| `Action` interface | `Action.cs` + `PersonalAction`, `FriendlyTargetedAction`, `EnemyTargetedAction` + 12 spell classes | `Func<bool>` lambdas calling `Inferno.Cast()` directly; helper methods like `CastOnFocus()` |
| `Unit` class hierarchy | `Unit.cs`, `PartyUnit.cs`, `RaidUnit.cs`, `PlayerUnit.cs` | Raw `string` unit tokens (`"player"`, `"party1"`, etc.) |
| `Group` interface | `Group.cs`, `PartyGroup.cs`, `RaidGroup.cs`, `Solo.cs` | `GetGroupMembers()` method returning `List<string>` |
| `ISelector` / `FilterChainSelector` | `ISelector.cs`, `FilterChainSelector.cs`, `IUnitFilterChain.cs` + 6 filters | LINQ chains inside lambdas: `.Where().OrderBy().FirstOrDefault()` |
| `Gambit` class | `Gambit.cs` | `(int pri, string name, Func<bool> cond, Func<bool> exec)` tuple |
| `GambitSet` abstract class | `GambitSet.cs` + `HolyPaladinDefaultGambitSet.cs`, `HolyPaladinDamageGambitSet.cs` | `List<(...)>` + `RunGambits()` method |
| `GambitSetPicker` | `GambitSetPicker.cs` + `PaladinHolyGambitPicker.cs` | `switch (mapId)` in `CombatTick()` or `GetDungeonGambits(int mapId)` |
| `ActionBook` interface | `ActionBook.cs` + `PaladinHolyActionBook.cs` | Direct `Spellbook.Add()` / `Macros.Add()` calls in `Initialize()` |
| `Environment` class | `Environment.cs` | Per-tick local variables or a few private fields refreshed at tick start |
| `Throttler` class | `Throttler.cs` (uses `System.Diagnostics.Stopwatch`) | `Dictionary<string, long>` + `DateTime.UtcNow` |
| `Logger` class | `Logger.cs` | `Inferno.PrintMessage()` or `File.AppendAllText()` |
| `Boss` class | `Boss.cs` | Inline boss-checking logic using `Inferno.UnitName("boss1")` etc. |
| `ActionQueuer` | `ActionQueuer.cs` | A `string _queuedAction` field + check at top of tick |
| `SpellMacroRegistry` | `SpellMacroRegistry.cs` | Not needed — macros registered directly in `Initialize()` |

---

## Things to Watch Out For

### 1. Stateful conditions (ThrottledCondition)
`ThrottledCondition` has mutable state (a `Throttler` per instance). In the functional version, use the `_throttleTimestamps` dictionary keyed by a unique string per gambit usage.

```csharp
// In gambit condition:
() => IsInCombat() && ThrottleIsOpen("holy_shock_def", 2000),
// In gambit action:
() => { ThrottleRestart("holy_shock_def"); /* ... cast ... */ return true; }
```

### 2. The `Consume()` pattern
Some conditions have a `Consume()` method called after a successful action (e.g., `ThrottledCondition.Restart()`). In the functional version, move the consume/restart logic into the **action lambda** (which only runs on success).

### 3. String literal length limit
The validator blocks string literals > ~2000 characters. This mainly affects:
- JSON blobs (from `JsonParser`)
- Very long macro strings

**Mitigation:** Split long strings using concatenation (`"part1" + "part2"`) or build them programmatically.

### 4. Focus-cast targeting pattern
Your current system does `unit.Focus()` then `ActionQueuer.QueueAction(macroName)`. The `ActionQueuer` delays the macro cast to the next tick. In the flat version, you can keep this pattern:

```csharp
private string _queuedAction = null;

public override bool CombatTick()
{
    // Always process queued action first
    if (_queuedAction != null)
    {
        Inferno.Cast(_queuedAction, true);
        _queuedAction = null;
        return true;
    }
    // ... rest of tick
}

private bool CastOnFocus(string unit, string macroName)
{
    Inferno.Cast("focus_" + unit);
    _queuedAction = macroName;
    return true;
}
```

### 5. GambitSet chaining (DoBeforeGambitSet / GetNextGambitSet)
Your current system chains gambit sets: dungeon-specific → default healing → damage. In the functional version, this becomes multiple `RunGambits()` calls in sequence (see Step 7).

### 6. Dungeon gambit reuse across specs
If you have multiple specs (Paladin Holy, Priest Holy, etc.) that share dungeon gambits, you'll need to **duplicate** them in each rotation file (since only one class is allowed). Alternatively, use the code-gen approach (Approach B) to inject shared dungeon gambits into each output file.

### 7. Value tuple availability
`(int, string, Func<bool>, Func<bool>)` value tuples require C# 7+. The Inferno runtime loads `.cs` as source and likely compiles it — verify that value tuples work. If not, use a simple `private class` (a single nested private class might be tolerated if it doesn't inherit from anything disallowed — **test this**). Alternatively, just use parallel `List` indexing or a simpler structure.

**Safe fallback if tuples don't work:**
```csharp
// Use parallel arrays or just an if-chain (Option A from Step 6)
```

### 8. `DateTime.UtcNow` precision
`DateTime.UtcNow` has ~15ms resolution on Windows, which is fine for throttle intervals of 100ms+. For sub-15ms precision you'd need a high-resolution timer, but game rotation ticks don't need that.

### 9. Build script changes
The build script (`BuildPaladinHoly.ps1` → `BuildFile.ps1`) will become **much simpler** — it just copies a single file. You may not even need it anymore since the rotation file IS the output.

However, if you keep the OOP project for development: maintain the build scripts but change them to copy the hand-written flat file rather than concatenating OOP files.

---

## Migration Order (Recommended)

Migrate one spec at a time. Suggested order:

1. **Paladin Holy** (your primary spec, most familiar)
2. **Priest Holy** (second spec, reuse patterns from step 1)

For each spec:

| Phase | Task | Est. effort |
|---|---|---|
| 1 | Create empty `Rotation` subclass, register spells/macros in `Initialize()` | 15 min |
| 2 | Port helper methods (group members, throttle, logging) | 30 min |
| 3 | Port default healing gambits as data-driven list | 45 min |
| 4 | Port damage gambits | 20 min |
| 5 | Port dungeon-specific gambits | 30 min per dungeon |
| 6 | Wire up `CombatTick()`, `OutOfCombatTick()` | 15 min |
| 7 | Test in Inferno | Until it works |

---

## Output File: Working Reference

The complete working rotation is in [`poc/rotation.cs`](poc/rotation.cs) (546 lines). It implements:

- **12 spells** registered in `Initialize()`
- **3 cast types**: `CastOnFocus()` (friendly via @focus), `CastPersonal()` (self-buff), `CastOnEnemy()` (current target)
- **7 heal gambits**: Divine Protection, Avenging Wrath, Divine Toll, Word of Glory, Holy Light, Holy Shock, Flash of Light
- **4 damage gambits**: Target enemy, Shield of the Righteous, Judgment, Flash of Light filler
- **7 dungeon gambit sets**: Proving Grounds, Magisters Terrace, Skyreach, Pit of Saron, Windrunner Spire, Maisara Caverns, Algeth'ar Academy
- **3 dungeon helper patterns**: `TryDispel()`, `TryDispelStacks()`, `TryBlessingOfFreedom()`
- **Separate throttle keys**: `heal_throttle`, `dmg_throttle`, `dispel_throttle`, `holy_shock_cd`

### File layout (top to bottom)

```
State fields (_queuedAction, _throttleTimestamps, HOLY_POWER)
LoadSettings()
Initialize() - spell + macro registration
CombatTick() / OutOfCombatTick() / OnStop()
RunHealGambits() - if-chain of heal priorities
RunDmgGambits() - if-chain of damage priorities
RunDungeonGambits(mapId) - switch to dungeon-specific methods
  Run<Dungeon>Gambits() - one method per dungeon
  TryDispel/TryDispelStacks/TryBlessingOfFreedom - shared dispel patterns
Condition helpers - IsInCombat, IsSpellReady, TargetIsEnemy, etc.
Selector helpers - LowestAllyUnder, GetAllyWithDebuff, etc.
Group members - GetGroupMembers()
Cast helpers - CastOnFocus, CastPersonal, CastOnEnemy, ProcessQueue
Throttle - GetTimestampMs, ThrottleIsOpen, ThrottleRestart
Logging - Log()
```

### How to extend

- **Add a new spell**: Add to `Spellbook` + `Macros` in `Initialize()`, add an if-block in the appropriate gambit method
- **Add a new dungeon**: Add map ID cases to `RunDungeonGambits()`, create a `Run<Name>Gambits()` method using `TryDispel()` helpers
- **Add a new condition**: Add a private method in the condition helpers section
- **Add a new selector**: Add a private method in the selector helpers section

---

## Approach B Notes (Code-Gen)

If you want to keep the OOP project as the "source of truth" and generate flat files:

1. Write a C# console app or PowerShell script that uses **reflection** or **Roslyn source generators** to:
   - Walk all `Gambit` definitions in a `GambitSet` subclass
   - Emit the corresponding if-chain code
   - Inline all condition and action logic
2. The generator reads the OOP model and produces a compliant single-file `.cs`.
3. This is more complex to build but pays off if you maintain 5+ specs.

For now, the manual approach (A) is recommended given the scope of 2 specs.

---

## Checklist Before Submitting to Inferno

- [ ] Wrapped in `namespace InfernoWow.Modules { }`
- [ ] File has exactly ONE class, inheriting from `Rotation`
- [ ] Only allowed `using` directives (6 total: System, System.Collections.Generic, System.Drawing, System.Linq, System.IO, InfernoWow.API)
- [ ] No value tuples `(int, string)` — the runtime does not support C# 7+
- [ ] No `Environment.` access (pattern-matched and blocked, even in comments)
- [ ] No banned words in comments (`class`, `Stopwatch`, `System.Diagnostics`, etc. — validator scans comments)
- [ ] No `System.Diagnostics` usage (no `Stopwatch`, no `Debug`, no `Process`)
- [ ] No `System.Threading` usage
- [ ] No `System.Net` usage
- [ ] No string literal exceeds ~2000 characters
- [ ] All state is instance fields on the `Rotation` class
- [ ] Focus macros registered for all unit tokens you use
- [ ] Spell macros registered for all `[@focus]` casts
- [ ] All spells added to `Spellbook` in `Initialize()`
- [ ] `CombatTick()` returns `true` when an action is taken, `false` otherwise
- [ ] Tested: solo, party, and raid group member detection
