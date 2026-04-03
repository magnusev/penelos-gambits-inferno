# Multi-Class Rotation Architecture Guide

## Current State: Working Holy Paladin POC

The Holy Paladin POC successfully demonstrates the functional approach required by security restrictions:
- ✅ Single file, single class inheriting from `Rotation`
- ✅ Clean queue-based casting (matches old `ActionQueuer`)
- ✅ Simple GCD handling via `Inferno.CanCast()` in selectors
- ✅ No complex lockouts or state machines
- ✅ ~350 lines, human-readable

## Security Constraints

```
ALLOWED:
- Inherit from: Rotation, Plugin
- Using directives: System, System.Collections.Generic, System.Drawing, System.Linq, System.IO, InfernoWow.API
- One class per file

BLOCKED:
- Custom namespaces (e.g., cannot reference a shared Plugin)
- Multiple classes in one file
- Most System.* namespaces
```

**Key limitation**: Rotations cannot reference custom plugins or shared assemblies beyond `InfernoWow.API`.

## Options Analysis

### Option A: Shared Plugin + Class Rotations (VIABLE!)

```
PenelosCore : Plugin
  ├─ Exports shared helper delegates via ExportObject()
  └─ Loaded at HIGH priority in plugin manager

HolyPaladin/rotation.cs : Rotation
  ├─ Imports helpers: var core = ImportObject("PenelosCore", "helpers")
  └─ Uses: core.LowestAllyUnder(), core.ThrottleIsOpen(), etc.

PriestHoly/rotation.cs : Rotation
  └─ Same - imports from PenelosCore
```

**How it works:**
1. PenelosCore plugin exports a dictionary/object of delegate functions
2. Each rotation imports this object in Initialize()
3. Rotations call shared functions through the imported object

**Verdict**: ✅ **Viable if ExportObject supports delegates** - need to verify

**Benefits**:
- ✅ True code sharing (single source of truth)
- ✅ No build step required
- ✅ Each rotation is self-contained (just needs PenelosCore installed)

**Drawbacks**:
- ⚠️ Requires PenelosCore plugin to be installed
- ⚠️ Delegate syntax may be verbose: `_core.CastOnFocus("party2", "cast_fol")`
- ⚠️ Unknown if ExportObject can export method delegates

### Option B: Code Composition via Build Scripts

```
Components/
  ├─ Core.cs              // Queue system, throttle, logging, group members
  ├─ Selectors.cs         // LowestAllyUnder, LowestAllyInRange, etc.
  ├─ Conditions.cs        // IsInCombat, PowerAtLeast, etc.
  └─ Utilities.cs         // HealthPct, NowMs, etc.

ClassSpecific/
  ├─ Paladin/
  │   ├─ HolySpells.cs    // Holy-specific spell logic
  │   └─ HolyGambits.cs   // RunHealGambits, RunDungeonGambits
  └─ Priest/
      ├─ HolySpells.cs
      └─ HolyGambits.cs

Build/
  └─ BuildRotation.ps1    // Combines Components + ClassSpecific into rotation.cs
```

**Build process**:
1. Read all component files
2. Strip `using` statements and class declarations
3. Inject methods into the target class
4. Output single `rotation.cs` file

**Benefits**:
- ✅ Share common code (queue system, selectors, conditions)
- ✅ Each class has clean, focused files during development
- ✅ Output is a single, security-compliant `rotation.cs`
- ✅ Human-readable both pre-build and post-build
- ✅ Easy to maintain - edit components, rebuild

**Drawbacks**:
- Build step required (but you already have BuildFile.ps1)
- Debugging happens in the combined file (but logs help)

---

## Recommended Approach: Test Plugin Pattern First

Let's test if **Option A (Plugin)** is viable before committing to build scripts.

### Test: Can Plugins Export Delegates?

**Step 1**: Create `PenelosCore.cs` (Plugin) that exports helper functions:

```csharp
using System;
using System.Collections.Generic;
using InfernoWow.API;

namespace InfernoWow.Modules
{

public class PenelosCore : Plugin
{
    private Dictionary<string, object> _helpers = new Dictionary<string, object>();
    
    public override void Initialize()
    {
        // Export helper functions as delegates
        _helpers["CastOnFocus"] = new Func<string, string, bool>(CastOnFocus);
        _helpers["LowestAllyUnder"] = new Func<int, string, string>(LowestAllyUnder);
        _helpers["HealthPct"] = new Func<string, int>(HealthPct);
        // ... etc
        
        Inferno.ExportObject("helpers", _helpers);
        Inferno.PrintMessage("PenelosCore loaded!", System.Drawing.Color.Green);
    }
    
    // Shared implementation
    private static string _queuedAction = null;
    
    private static bool CastOnFocus(string unit, string macro)
    {
        if (_queuedAction != null) return false;
        Inferno.Cast("focus_" + unit);
        _queuedAction = macro;
        return true;
    }
    
    private static string LowestAllyUnder(int pct, string spell)
    {
        // ... implementation
    }
    
    private static int HealthPct(string u)
    {
        int mx = Inferno.MaxHealth(u);
        if (mx < 1) mx = 1;
        return (Inferno.Health(u) * 100) / mx;
    }
    
    // ... more helpers
}

}
```

**Step 2**: Test importing in a rotation:

```csharp
public class HolyPaladinPvE : Rotation
{
    private Dictionary<string, object> _core;
    
    public override void Initialize()
    {
        // Import helpers from PenelosCore plugin
        _core = (Dictionary<string, object>)Inferno.ImportObject("PenelosCore", "helpers");
        
        if (_core == null)
        {
            Inferno.PrintMessage("ERROR: PenelosCore plugin not found!", Color.Red);
            return;
        }
        
        // ... rest of initialization
    }
    
    public override bool CombatTick()
    {
        // Call shared helper
        var castOnFocus = (Func<string, string, bool>)_core["CastOnFocus"];
        castOnFocus("party2", "cast_fol");
        
        // ... etc
    }
}
```

**If this works**: Option A is the best choice!  
**If this fails**: Fall back to Option B (build scripts).

---

## Option A: Plugin-Based Architecture (If Test Succeeds)

### Structure

```
PenelosCore/
└─ PenelosCore.cs : Plugin
    └─ All shared code (queue, selectors, conditions, utilities)

Rotations/
├─ PaladinHoly_rotation.cs : Rotation
├─ PriestHoly_rotation.cs : Rotation
└─ DruidRestoration_rotation.cs : Rotation
```

### Benefits

- ✅ **No build step** - direct code sharing
- ✅ **Single source of truth** - fix once, all rotations fixed
- ✅ **Clean rotation files** - only class-specific logic
- ✅ **Easy distribution** - install PenelosCore + rotation(s)
- ✅ **Type safety** - delegates provide compile-time checks

### Drawbacks

- ⚠️ **Verbose syntax**: `((Func<...>)_core["Method"])(args)` 
- ⚠️ **Dependency**: All rotations require PenelosCore installed
- ⚠️ **Complexity**: Users must manage plugin priority (PenelosCore must be highest)

### Plugin Priority Setup

Users must configure plugin load order:
```
Plugin Manager:
1. PenelosCore      ← Must be highest priority
2. (other plugins)

Rotation:
Holy Paladin PvE    ← Imports from PenelosCore
```

---

## Option B: Component-Based Build System (Fallback)

### 1. Shared Components Structure

```
poc/
├─ Components/
│   ├─ 00_Core.cs          // Queue, throttle, logging (ALWAYS INCLUDED)
│   ├─ 01_Conditions.cs    // Reusable conditions
│   ├─ 02_Selectors.cs     // Unit selection logic
│   └─ 03_Utilities.cs     // Helper functions
│
├─ Classes/
│   ├─ PaladinHoly/
│   │   ├─ Config.cs       // LoadSettings, Initialize, constants
│   │   ├─ Spells.cs       // Spell-specific helpers (Holy Shock tracking, etc.)
│   │   ├─ HealGambits.cs  // RunHealGambits
│   │   ├─ DmgGambits.cs   // RunDmgGambits
│   │   └─ DungeonGambits.cs // RunDungeonGambits, TryDispel
│   │
│   └─ PriestHoly/
│       ├─ Config.cs
│       ├─ Spells.cs
│       └─ ...
│
├─ Build/
│   └─ BuildRotation.ps1   // Combines components → rotation.cs
│
└─ Output/
    ├─ PaladinHoly_rotation.cs
    └─ PriestHoly_rotation.cs
```

### 2. Component File Format

Each component file should be **pure methods** - no class wrappers, no using statements (those are added by the build script).

**Example: Components/00_Core.cs**
```csharp
// ========================================
// QUEUE SYSTEM (from ActionQueuer pattern)
// ========================================

private string _queuedAction = null;
private Dictionary<string, long> _throttleTimestamps = new Dictionary<string, long>();
private string _lastLoggedAction = null;
private string _logFile = null;

private bool CastOnFocus(string unit, string macro) 
{ 
    if (_queuedAction != null) return false;
    Inferno.Cast("focus_" + unit); 
    _queuedAction = macro; 
    return true; 
}

private bool CastPersonal(string s) { Inferno.Cast(s); return true; }
private bool CastOnEnemy(string s) { Inferno.Cast(s); return true; }

private bool ProcessQueue()
{
    if (_queuedAction == null) return false;
    string a = _queuedAction; 
    _queuedAction = null;
    Inferno.Cast(a, QuickDelay: true);
    return true;
}

// Throttle system
private long NowMs() { return DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond; }
private bool ThrottleIsOpen(string k, int ms) 
{ 
    if (!_throttleTimestamps.ContainsKey(k)) return true; 
    return (NowMs() - _throttleTimestamps[k]) >= ms; 
}
private void ThrottleRestart(string k) { _throttleTimestamps[k] = NowMs(); }

// Logging
private void Log(string msg)
{
    if (!GetCheckBox("Enable Logging")) return;
    if (msg == _lastLoggedAction && !msg.StartsWith("Tick:")) return;
    _lastLoggedAction = msg;
    Inferno.PrintMessage(msg, Color.White);
    if (_logFile != null) { try { File.AppendAllText(_logFile, DateTime.Now.ToString("HH:mm:ss.fff") + " " + msg + "\n"); } catch { } }
}
```

**Example: Components/01_Conditions.cs**
```csharp
// ========================================
// REUSABLE CONDITIONS
// ========================================

private bool IsInCombat() { return Inferno.InCombat("player"); }
private bool IsSpellReady(string s) { return Inferno.SpellCooldown(s) <= 200; }
private bool IsSettingOn(string s) { return GetCheckBox(s); }
private bool TargetIsEnemy() { return Inferno.UnitCanAttack("player", "target"); }
private bool UnitUnder(string u, int p) { return HealthPct(u) < p; }
private bool EnemiesInMelee(int n) { return Inferno.EnemiesNearUnit(8, "player") >= n; }
private bool PowerAtLeast(int n, int t) { return Inferno.Power("player", t) >= n; }
private bool PowerLessThan(int n, int t) { return Inferno.Power("player", t) < n; }

private bool GroupMembersUnder(int pct, int min)
{
    return GetGroupMembers().Count(u => !Inferno.IsDead(u) && HealthPct(u) < pct) >= min;
}

private bool AnyAllyHasDebuff(string d)
{
    return GetGroupMembers().Any(u => !Inferno.IsDead(u) && Inferno.HasDebuff(d, u, false));
}

private bool AnyAllyHasDebuff(string d, int stacks)
{
    return GetGroupMembers().Any(u => !Inferno.IsDead(u) && Inferno.HasDebuff(d, u, false) && Inferno.DebuffStacks(d, u, false) >= stacks);
}
```

**Example: Classes/PaladinHoly/Config.cs**
```csharp
// ========================================
// PALADIN HOLY - CONFIGURATION
// ========================================

private const int HOLY_POWER = 9;
private const int HEALTHSTONE_ID = 5512;
private const int DIAGNOSTIC_LOG_INTERVAL_MS = 2000;

// Holy Shock charge tracking (API bug workaround)
private int _hsCharges = 2;
private long _hsLastRechargeMs = 0;
private const int HS_MAX_CHARGES = 2;
private const int HS_RECHARGE_MS = 5000;

public override void LoadSettings()
{
    Settings.Add(new Setting("Enable Logging", true));
    Settings.Add(new Setting("Use Light of Dawn", false));
    Settings.Add(new Setting("Do DPS", false));
    Settings.Add(new Setting("Healthstone HP %", 1, 100, 50));
}

public override void Initialize()
{
    Spellbook.Add("Avenging Wrath"); 
    Spellbook.Add("Blessing of Freedom");
    Spellbook.Add("Cleanse"); 
    Spellbook.Add("Divine Protection");
    // ... rest of spellbook

    Macros.Add("cast_fol", "/cast [@focus] Flash of Light");
    Macros.Add("cast_hl", "/cast [@focus] Holy Light");
    // ... rest of macros

    CustomFunctions.Add("HasHealthstone", "return GetItemCount(5512) > 0 and 1 or 0");

    _logFile = "penelos_paladin_holy_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log";
    Inferno.PrintMessage("Penelos Gambits - Holy Paladin loaded!", Color.Green);
    Log("Initialize complete");
}
```

**Example: Classes/PaladinHoly/HealGambits.cs**
```csharp
// ========================================
// PALADIN HOLY - HEAL PRIORITY
// ========================================

private bool RunHealGambits()
{
    // Healthstone
    if (IsInCombat() && UnitUnder("player", GetSlider("Healthstone HP %")) && HasHealthstone() && Inferno.ItemCooldown(HEALTHSTONE_ID) == 0)
    { Log("Using Healthstone (player " + HealthPct("player") + "%)"); Inferno.Cast("use_healthstone", QuickDelay: true); return true; }

    // Divine Protection
    if (IsInCombat() && UnitUnder("player", 75) && Inferno.CanCast("Divine Protection"))
    { Log("Casting Divine Protection (player " + HealthPct("player") + "%)"); return CastPersonal("Divine Protection"); }

    // Avenging Wrath
    if (IsInCombat() && GroupMembersUnder(60, 2) && Inferno.CanCast("Avenging Wrath"))
    { Log("Casting Avenging Wrath"); return CastPersonal("Avenging Wrath"); }

    // Divine Toll
    if (IsInCombat() && GroupMembersUnder(80, 2) && PowerLessThan(3, HOLY_POWER))
    { string t = LowestAllyInRange("Divine Toll"); if (t != null) { Log("Casting Divine Toll on " + t + " (" + HealthPct(t) + "%)"); return CastOnFocus(t, "cast_dt"); } }

    // Word of Glory
    if (IsInCombat() && PowerAtLeast(3, HOLY_POWER))
    { string t = LowestAllyUnder(90, "Word of Glory"); if (t != null) { Log("Casting Word of Glory on " + t + " (" + HealthPct(t) + "%)"); return CastOnFocus(t, "cast_wog"); } }

    // Holy Shock
    if (IsInCombat() && HsChargesAvailable() > 0)
    { string t = LowestAllyUnder(95, "Holy Shock"); if (t != null) { Log("Casting Holy Shock on " + t + " (" + HealthPct(t) + "%) [charges=" + HsChargesAvailable() + "]"); UseHsCharge(); return CastOnFocus(t, "cast_hs"); } }

    // Holy Light
    if (IsInCombat() && CanCastWhileMoving("Holy Light"))
    { string t = LowestAllyUnder(60, "Holy Light"); if (t != null) { Log("Casting Holy Light on " + t + " (" + HealthPct(t) + "%)"); return CastOnFocus(t, "cast_hl"); } }

    // Flash of Light
    if (IsInCombat() && CanCastWhileMoving("Flash of Light"))
    { string t = LowestAllyUnder(95, "Flash of Light"); if (t != null) { Log("Casting Flash of Light on " + t + " (" + HealthPct(t) + "%)"); return CastOnFocus(t, "cast_fol"); } }

    return false;
}
```

### 3. Build Script Design

**BuildRotation.ps1** - Combines components into a complete rotation.cs

```powershell
param(
    [Parameter(Mandatory=$true)]
    [string]$Class,  # e.g., "PaladinHoly"
    
    [string]$OutputDir = "Output"
)

$componentsDir = "Components"
$classDir = "Classes\$Class"
$outputFile = "$OutputDir\${Class}_rotation.cs"

# Template
$template = @"
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.IO;
using InfernoWow.API;

namespace InfernoWow.Modules
{

public class {CLASS_NAME} : Rotation
{
{FIELDS}

{METHODS}
}

}
"@

# 1. Load all component files (ordered by prefix)
$sharedComponents = Get-ChildItem "$componentsDir\*.cs" | Sort-Object Name
$classComponents = Get-ChildItem "$classDir\*.cs" | Sort-Object Name

$fields = ""
$methods = ""

# 2. Extract fields and methods from each component
foreach ($file in ($sharedComponents + $classComponents)) {
    $content = Get-Content $file.FullName -Raw
    
    # Extract private fields (lines starting with "private" before any method)
    # Extract private methods (everything else)
    
    # Simple heuristic: 
    # - Lines with "private.*=" or "private.*;" at class level → fields
    # - Lines with "private.*{" or "public override" → methods
    
    $fields += $content -replace '(?s)^.*?(private[^{]*;).*$', '$1'
    $methods += $content
}

# 3. Replace placeholders
$output = $template -replace '{CLASS_NAME}', $Class
$output = $output -replace '{FIELDS}', $fields
$output = $output -replace '{METHODS}', $methods

# 4. Write output
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
Set-Content -Path $outputFile -Value $output

Write-Host "✅ Built $Class rotation → $outputFile" -ForegroundColor Green
```

**Usage**:
```powershell
.\Build\BuildRotation.ps1 -Class "PaladinHoly"
.\Build\BuildRotation.ps1 -Class "PriestHoly"
```

### 4. Alternative: Template-Based Approach (Simpler)

If build scripts are too complex, use a **template + manual sections** approach:

**Template.cs** (single file)
```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.IO;
using InfernoWow.API;

namespace InfernoWow.Modules
{

public class {CLASS_NAME} : Rotation
{
    // ========================================
    // SHARED INFRASTRUCTURE (DO NOT MODIFY - copy from Components/Core.cs)
    // ========================================
    
    private string _queuedAction = null;
    private string _lastLoggedAction = null;
    private Dictionary<string, long> _throttleTimestamps = new Dictionary<string, long>();
    private string _logFile = null;
    
    // [COPY/PASTE from Components/Core.cs]
    private bool CastOnFocus(string unit, string macro) { ... }
    private bool ProcessQueue() { ... }
    // ... etc
    
    // ========================================
    // CLASS-SPECIFIC CONFIGURATION
    // ========================================
    
    // [EDIT THIS SECTION FOR YOUR CLASS]
    private const int PRIMARY_RESOURCE = 9; // Holy Power, Mana, etc.
    
    public override void LoadSettings()
    {
        // [YOUR SETTINGS HERE]
    }
    
    public override void Initialize()
    {
        // [YOUR SPELLS, MACROS HERE]
    }
    
    // ========================================
    // MAIN TICK LOOP (DO NOT MODIFY)
    // ========================================
    
    public override bool CombatTick()
    {
        if (Inferno.IsDead("player")) return true;
        if (Inferno.GCD() != 0) return true;
        if (ProcessQueue()) return true;
        
        // [DIAGNOSTIC LOG - shared]
        if (ThrottleIsOpen("diag", 2000)) { ... }
        
        int mapId = Inferno.GetMapID();
        if (RunDungeonGambits(mapId)) return true;
        if (RunHealGambits()) return true;
        if (RunDmgGambits()) return true;
        return true;
    }
    
    // ========================================
    // CLASS-SPECIFIC GAMBITS
    // ========================================
    
    // [EDIT THESE METHODS FOR YOUR CLASS]
    private bool RunHealGambits()
    {
        // Your heal priority here
    }
    
    private bool RunDmgGambits()
    {
        // Your damage priority here
    }
    
    private bool RunDungeonGambits(int mapId)
    {
        // Your dungeon-specific logic here
    }
    
    // ========================================
    // SHARED HELPERS (DO NOT MODIFY - copy from Components)
    // ========================================
    
    // [COPY/PASTE from Components/Conditions.cs]
    private bool IsInCombat() { ... }
    // [COPY/PASTE from Components/Selectors.cs]
    private string LowestAllyUnder(int pct, string spell) { ... }
    // [COPY/PASTE from Components/Utilities.cs]
    private int HealthPct(string u) { ... }
}

}
```

**Benefits of template approach**:
- ✅ No build step
- ✅ Clear "DO NOT MODIFY" vs "EDIT THIS" sections
- ✅ Easy for humans to understand
- ✅ Copy/paste shared components once

**Drawbacks**:
- Manual updates when shared components change
- Slightly more code duplication

---

## Recommendation: Hybrid Approach

**For development**:
1. Use component-based structure for maintainability
2. Have a simple build script that concatenates files
3. **Keep the output rotation.cs in source control** - makes it easy to review changes

**For distribution**:
- Distribute the built `rotation.cs` files
- Users don't need to build, just copy the file

**Build script (simple version)**:
```powershell
# BuildPaladinHoly.ps1
param([string]$OutputFile = "Output\PaladinHoly_rotation.cs")

$header = @"
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.IO;
using InfernoWow.API;

namespace InfernoWow.Modules
{

public class HolyPaladinPvE : Rotation
{
"@

$footer = @"
}

}
"@

# Concatenate all component files
$components = @(
    "Components\00_Core.cs",
    "Components\01_Conditions.cs",
    "Components\02_Selectors.cs",
    "Components\03_Utilities.cs",
    "Classes\PaladinHoly\Config.cs",
    "Classes\PaladinHoly\Spells.cs",
    "Classes\PaladinHoly\HealGambits.cs",
    "Classes\PaladinHoly\DmgGambits.cs",
    "Classes\PaladinHoly\DungeonGambits.cs",
    "Classes\PaladinHoly\MainTick.cs"
)

New-Item -ItemType Directory -Force -Path (Split-Path $OutputFile) | Out-Null

$content = $header
foreach ($file in $components) {
    if (Test-Path $file) {
        $content += "`n    // ---- From: $file ----`n"
        $content += Get-Content $file -Raw
        $content += "`n"
    }
}
$content += $footer

Set-Content -Path $OutputFile -Value $content
Write-Host "✅ Built Holy Paladin → $OutputFile" -ForegroundColor Green
```

---

## File Organization Best Practices

### Component Files Should Be:

1. **Pure method collections** - no class wrappers
2. **Well-commented** - explain what each section does
3. **Ordered logically** - fields first, then methods
4. **Self-contained** - each file is one logical unit (Queue, Conditions, Selectors, etc.)

### Class-Specific Files Should:

1. **Focus on ONE aspect** - Config, HealGambits, DmgGambits, etc.
2. **Use clear names** - `RunHealGambits()`, `TryDispel()`, `HsChargesAvailable()`
3. **Follow the same pattern** - check conditions → select target → cast
4. **Document class-specific logic** - e.g., "API bug: Holy Shock charges always return 0"

### The Main Tick Should Be:

**Standardized across all classes** (only class name changes):

```csharp
public override bool CombatTick()
{
    if (Inferno.IsDead("player")) return true;
    if (Inferno.GCD() != 0) return true;
    if (ProcessQueue()) return true;
    
    // Diagnostic logging (shared)
    // ... 
    
    int mapId = Inferno.GetMapID();
    if (RunDungeonGambits(mapId)) return true;
    if (RunHealGambits()) return true;    // or RunDpsGambits for DPS specs
    if (RunDmgGambits()) return true;     // or RunDefenseGambits for tanks
    return true;
}
```

---

## Migration Path: From POC to Multi-Class

### Step 1: Extract POC into Components

Take the current working `poc/rotation.cs` and split it:

1. **Create Components/** folder
   - Extract queue system → `00_Core.cs`
   - Extract conditions → `01_Conditions.cs`
   - Extract selectors → `02_Selectors.cs`
   - Extract utilities → `03_Utilities.cs`

2. **Create Classes/PaladinHoly/** folder
   - Extract constants + LoadSettings + Initialize → `Config.cs`
   - Extract Holy Shock tracking → `Spells.cs`
   - Extract RunHealGambits → `HealGambits.cs`
   - Extract RunDmgGambits → `DmgGambits.cs`
   - Extract RunDungeonGambits + TryDispel → `DungeonGambits.cs`
   - Extract CombatTick + OutOfCombatTick → `MainTick.cs`

3. **Test the build**
   - Run `BuildRotation.ps1 -Class PaladinHoly`
   - Compare output to original POC
   - Test in-game

### Step 2: Add Second Class (Priest Holy)

1. **Create Classes/PriestHoly/** folder
2. **Copy structure from PaladinHoly**
3. **Modify only class-specific logic**:
   - Different spells (Prayer of Mending, Circle of Healing, etc.)
   - Different resource (no Holy Power)
   - Different dungeon mechanics
   - Same Core/Conditions/Selectors (automatically included)

4. **Build**: `BuildRotation.ps1 -Class PriestHoly`

### Step 3: Add More Classes

Repeat Step 2 for each class/spec. The shared components grow stronger with each addition:
- Need a new selector? Add it to `Components/02_Selectors.cs` → all classes get it
- Fix a bug in the queue system? Fix `Components/00_Core.cs` → rebuild all classes

---

## Key Design Principles

### 1. Separation of Concerns

| Component | Responsibility | Changes Often? |
|-----------|---------------|----------------|
| **Core** | Queue, throttle, logging | ❌ Rarely |
| **Conditions** | Reusable checks (IsInCombat, PowerAtLeast) | ⚠️ Occasionally |
| **Selectors** | Unit selection (LowestAllyUnder, etc.) | ⚠️ Occasionally |
| **Utilities** | Helpers (HealthPct, NowMs) | ❌ Rarely |
| **Config** | Settings, spells, macros | ✅ Per class |
| **Gambits** | Priority logic | ✅✅ Constantly |

### 2. Single Responsibility

Each gambit method should answer ONE question:
- `RunHealGambits()` → "What heal should I cast?"
- `RunDmgGambits()` → "What damage should I deal?"
- `RunDungeonGambits()` → "Do I need to handle a dungeon mechanic?"

### 3. Priority Over State

The functional approach works because **we evaluate priorities every tick**, not maintain complex state:

```csharp
// ❌ BAD: Stateful, complex
private HealState _state = HealState.Normal;
if (_state == HealState.Emergency) { ... }

// ✅ GOOD: Priority-based, simple
if (GroupMembersUnder(60, 2)) { /* Emergency */ }
if (GroupMembersUnder(90, 1)) { /* Normal */ }
```

### 4. Trust Inferno.CanCast()

Don't try to outsmart the API - `Inferno.CanCast()` handles:
- GCD timing
- Spell cooldowns
- Resource costs
- Range checks
- Line of sight
- Spell known

**Pattern**: Check high-level conditions → let `CanCast` in selectors handle the rest.

---

## Example: Adding a New Class (Priest Holy)

### Priest-Specific Components

**Classes/PriestHoly/Config.cs**
```csharp
private const int MANA = 0;
private const int HEALTHSTONE_ID = 5512;
private const int DIAGNOSTIC_LOG_INTERVAL_MS = 2000;

public override void LoadSettings()
{
    Settings.Add(new Setting("Enable Logging", true));
    Settings.Add(new Setting("Use Circle of Healing", true));
    Settings.Add(new Setting("Healthstone HP %", 1, 100, 50));
}

public override void Initialize()
{
    Spellbook.Add("Circle of Healing");
    Spellbook.Add("Flash Heal");
    Spellbook.Add("Heal");
    Spellbook.Add("Prayer of Mending");
    Spellbook.Add("Renew");
    // ... etc

    Macros.Add("cast_fh", "/cast [@focus] Flash Heal");
    Macros.Add("cast_heal", "/cast [@focus] Heal");
    // ... etc
    
    _logFile = "penelos_priest_holy_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log";
    Inferno.PrintMessage("Penelos Gambits - Priest Holy loaded!", Color.Green);
    Log("Initialize complete");
}
```

**Classes/PriestHoly/HealGambits.cs**
```csharp
private bool RunHealGambits()
{
    // Guardian Spirit if player under 30%
    if (IsInCombat() && UnitUnder("player", 30) && Inferno.CanCast("Guardian Spirit"))
    { Log("Casting Guardian Spirit on player"); return CastOnFocus("player", "cast_gs"); }
    
    // Circle of Healing if 3+ under 90%
    if (IsInCombat() && IsSettingOn("Use Circle of Healing") && GroupMembersUnder(90, 3) && Inferno.CanCast("Circle of Healing"))
    { Log("Casting Circle of Healing"); return CastPersonal("Circle of Healing"); }
    
    // Flash Heal if lowest under 60%
    if (IsInCombat() && CanCastWhileMoving("Flash Heal"))
    { string t = LowestAllyUnder(60, "Flash Heal"); if (t != null) { Log("Casting Flash Heal on " + t + " (" + HealthPct(t) + "%)"); return CastOnFocus(t, "cast_fh"); } }
    
    // Heal if lowest under 90%
    if (IsInCombat() && CanCastWhileMoving("Heal"))
    { string t = LowestAllyUnder(90, "Heal"); if (t != null) { Log("Casting Heal on " + t + " (" + HealthPct(t) + "%)"); return CastOnFocus(t, "cast_heal"); } }
    
    return false;
}
```

**Build**:
```powershell
.\Build\BuildRotation.ps1 -Class "PriestHoly"
```

**Result**: `Output/PriestHoly_rotation.cs` with all shared + priest-specific code combined.

---

## Comparison: Plugin vs Components

| Aspect | Plugin (Option A) | Components (Option B) |
|--------|------------------|---------------------|
| **Code Sharing** | ❌ Not possible (security blocks references) | ✅ Via build scripts |
| **Development** | ❌ Can't split files | ✅ Clean, focused files |
| **Maintenance** | N/A | ✅ Change once, rebuild all |
| **Debugging** | N/A | ⚠️ Debug in combined output |
| **Distribution** | N/A | ✅ Single rotation.cs per class |
| **Complexity** | N/A | ⚠️ Build step required |
| **Human Readable** | N/A | ✅ Both source and output are readable |

---

## Final Recommendation

**Use Component-Based Build System (Option B)** with this structure:

```
poc/
├─ Components/           # Shared code (reusable across all classes)
│   ├─ 00_Core.cs
│   ├─ 01_Conditions.cs
│   ├─ 02_Selectors.cs
│   └─ 03_Utilities.cs
│
├─ Classes/              # Class-specific code
│   ├─ PaladinHoly/
│   │   ├─ 10_Config.cs
│   │   ├─ 11_Spells.cs
│   │   ├─ 20_MainTick.cs
│   │   ├─ 30_HealGambits.cs
│   │   ├─ 31_DmgGambits.cs
│   │   └─ 32_DungeonGambits.cs
│   │
│   ├─ PriestHoly/
│   │   └─ [same structure]
│   │
│   └─ DruidRestoration/
│       └─ [same structure]
│
├─ Build/
│   └─ BuildRotation.ps1 # Simple concatenation script
│
└─ Output/               # Built rotation files (commit these too)
    ├─ PaladinHoly_rotation.cs
    ├─ PriestHoly_rotation.cs
    └─ DruidRestoration_rotation.cs
```

**Workflow**:
1. Edit components or class-specific files
2. Run `.\Build\BuildRotation.ps1 -Class PaladinHoly`
3. Test the output file in-game
4. Commit both source and output

**Why this works**:
- ✅ Security restrictions satisfied (single file output)
- ✅ Developer experience excellent (clean, focused files)
- ✅ Maintainability high (change once, rebuild)
- ✅ Distribution simple (one file per class)
- ✅ No complicated tooling (PowerShell script everyone has)

---

## Action Items for Implementation

1. **Extract current POC**:
   ```powershell
   # Create folders
   mkdir Components, Classes\PaladinHoly, Build, Output
   
   # Split rotation.cs into components (manual copy/paste)
   ```

2. **Create build script**:
   - Simple concatenation (70 lines of PowerShell)
   - Or use existing `BuildFile.ps1` as template

3. **Test build**:
   ```powershell
   .\Build\BuildRotation.ps1 -Class PaladinHoly
   # Compare Output\PaladinHoly_rotation.cs to poc\rotation.cs
   ```

4. **Add second class**:
   ```powershell
   # Copy structure
   cp -r Classes\PaladinHoly Classes\PriestHoly
   
   # Edit priest-specific files
   # Build
   .\Build\BuildRotation.ps1 -Class PriestHoly
   ```

5. **Document**:
   - Add comments in component files
   - Create README.md explaining the build process
   - Add example for new developers

---

## Final Recommendation

### Phase 1: Test Plugin Viability

Create a minimal test:

**TestCore.cs** (Plugin):
```csharp
using System;
using InfernoWow.API;

namespace InfernoWow.Modules
{
public class TestCore : Plugin
{
    public override void Initialize()
    {
        var helper = new Func<string, int>(TestHelper);
        Inferno.ExportObject("testFunc", helper);
    }
    
    public override bool CombatTick() { return false; }
    
    private static int TestHelper(string msg) 
    { 
        Inferno.PrintMessage("TestCore called: " + msg, System.Drawing.Color.Cyan); 
        return 42; 
    }
}
}
```

**TestRotation.cs**:
```csharp
using System;
using InfernoWow.API;

namespace InfernoWow.Modules
{
public class TestRotation : Rotation
{
    private Func<string, int> _testFunc;
    
    public override void Initialize()
    {
        Spellbook.Add("Fireball");
        
        var imported = Inferno.ImportObject("TestCore", "testFunc");
        if (imported != null)
        {
            _testFunc = (Func<string, int>)imported;
            int result = _testFunc("Hello from rotation!");
            Inferno.PrintMessage("Import SUCCESS! Result: " + result, System.Drawing.Color.Green);
        }
        else
        {
            Inferno.PrintMessage("Import FAILED", System.Drawing.Color.Red);
        }
    }
    
    public override bool CombatTick() { return false; }
}
}
```

**Test procedure**:
1. Load TestCore plugin (set high priority)
2. Load TestRotation rotation
3. Start rotation
4. Check console for "Import SUCCESS!" or "Import FAILED"

**If SUCCESS**: ✅ Use **Option A (Plugin-based)** → cleaner, no build step  
**If FAILED**: ✅ Use **Option B (Build scripts)** → proven approach, slightly more work

### Phase 2: Implement Chosen Approach

**If Option A works**:
- Create `PenelosCore.cs` plugin with all shared code
- Slim down rotations to only class-specific logic
- Document plugin dependency in README

**If Option B is needed**:
- Extract POC into component files
- Create simple build script (concatenate files)
- Build rotation.cs for each class

---

## Summary Table

| Criteria | Option A: Plugin | Option B: Build Scripts |
|----------|-----------------|----------------------|
| **Code Sharing** | ✅ True sharing via delegates | ✅ Via file concatenation |
| **Build Step** | ✅ None | ⚠️ Required |
| **Maintenance** | ✅ Fix once, immediate effect | ✅ Fix once, rebuild all |
| **Distribution** | ⚠️ 2 files (plugin + rotation) | ✅ 1 file (rotation.cs) |
| **Complexity** | ⚠️ Plugin priority, import syntax | ⚠️ Build scripts |
| **Readability** | ⚠️ Delegate casting syntax | ✅ Normal method calls in output |
| **Viability** | ❓ **NEEDS TESTING** | ✅ Guaranteed to work |

**Action**: Test Option A first. If it works, use it. If not, Option B is the proven fallback.

