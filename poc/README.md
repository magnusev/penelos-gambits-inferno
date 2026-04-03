# POC - Component-Based Rotation System

## Overview

This POC demonstrates a **component-based architecture** for multi-class rotations that complies with Inferno's security restrictions.

**Key Achievement**: Shared code reuse across multiple classes without violating single-file, single-class constraints.

## Folder Structure

```
poc/
├─ Components/              # Shared code (used by all classes)
│   ├─ 00_Core.cs          # Queue system, throttle, logging
│   ├─ 01_Conditions.cs    # Reusable conditions (IsInCombat, PowerAtLeast, etc.)
│   ├─ 02_Selectors.cs     # Unit selection (LowestAllyUnder, GetAllyWithDebuff, etc.)
│   └─ 03_Utilities.cs     # Helpers (HealthPct, etc.)
│
├─ Classes/                 # Class-specific code
│   └─ PaladinHoly/
│       ├─ 10_Config.cs            # Settings, initialization, constants
│       ├─ 11_Spells.cs            # Holy Shock charge tracking (class-specific)
│       ├─ 20_MainTick.cs          # CombatTick, OutOfCombatTick
│       ├─ 30_HealGambits.cs       # RunHealGambits - heal priority logic
│       ├─ 31_DmgGambits.cs        # RunDmgGambits - damage priority logic
│       └─ 32_DungeonGambits.cs    # RunDungeonGambits, TryDispel, etc.
│
├─ Build/
│   ├─ BuildRotation.ps1   # Build single class
│   └─ BuildAll.ps1        # Build all configured classes
│
├─ Output/                  # Built rotation files (git tracked)
│   └─ PaladinHoly_rotation.cs
│
├─ rotation.cs              # Working POC (monolithic version - for reference)
├─ CompileCheck/            # Compile validation project
├─ SecurityValidator/       # Security rule validator
└─ README.md                # This file
```

## Quick Start

### Build All Rotations

```powershell
cd Build
.\BuildAll.ps1              # Deploy to C:\libs\Live\Rotations\Retail\ (with validation)
.\BuildAll.ps1 -LocalOnly   # Build to Output/ only (with validation, no deployment)
```

**Security Validation**: Every build automatically validates the output against Inferno's security rules. If validation fails, the build stops with error details.

### Build Single Rotation

```powershell
cd Build
.\BuildRotation.ps1 -Class PaladinHoly -ClassName HolyPaladinPvE
```

## How It Works

### Build Process

The build script **concatenates component files** into a single `rotation.cs`:

```
Components/00_Core.cs        ─┐
Components/01_Conditions.cs   │
Components/02_Selectors.cs    ├─> Combined into single class
Components/03_Utilities.cs    │
Classes/PaladinHoly/*.cs     ─┘
```

**Output**: Single `rotation.cs` with all methods in one `HolyPaladinPvE : Rotation` class.

### File Naming Convention

Files are prefixed with numbers to control merge order:
- `00-09`: Core components (queue, logging)
- `10-19`: Class configuration
- `20-29`: Main tick loops
- `30-39`: Gambit logic (heals, damage, dungeons)

## Adding a New Class

### Step 1: Create Class Folder

```powershell
mkdir Classes\PriestHoly
```

### Step 2: Copy Template Structure

```powershell
# Copy PaladinHoly as template
cp -r Classes\PaladinHoly\* Classes\PriestHoly\
```

### Step 3: Edit Class-Specific Files

**10_Config.cs**:
- Change constants (e.g., no HOLY_POWER for priests)
- Update LoadSettings (class-specific options)
- Update Initialize (different spells, macros)
- Change log file name

**30_HealGambits.cs**:
- Replace Paladin spells with Priest spells
- Adjust priority logic
- Keep the same pattern: check conditions → select target → cast

**32_DungeonGambits.cs**:
- Add/remove dungeon-specific mechanics as needed

### Step 4: Register in BuildAll.ps1

```powershell
# Edit Build\BuildAll.ps1
$classes = @(
    @{ Class = "PaladinHoly"; ClassName = "HolyPaladinPvE" }
    @{ Class = "PriestHoly"; ClassName = "HolyPriestPvE" }  # ← ADD THIS
)
```

### Step 5: Build and Test

```powershell
.\Build\BuildRotation.ps1 -Class PriestHoly -ClassName HolyPriestPvE -LocalOnly
# Test Output\PriestHoly_rotation.cs in-game
```

## Modifying Shared Code

**To add a new selector** (e.g., "Get ally with most missing health"):

1. Edit `Components/02_Selectors.cs`:
   ```csharp
   private string MostMissingHealthAlly(string spell)
   {
       return GetGroupMembers()
           .Where(u => !Inferno.IsDead(u) && Inferno.CanCast(spell, u))
           .OrderBy(u => Inferno.Health(u) - Inferno.MaxHealth(u))
           .FirstOrDefault();
   }
   ```

2. Rebuild all classes:
   ```powershell
   .\Build\BuildAll.ps1
   ```

3. **All classes now have this selector** - use it in any gambit.

## Key Patterns

### Gambit Pattern

**Always follow this pattern** in gambit methods:

```csharp
// 1. Check conditions (combat, cooldowns, thresholds)
if (IsInCombat() && PowerAtLeast(3, HOLY_POWER))
{
    // 2. Select target (use CanCast for automatic GCD/range/resource checks)
    string t = LowestAllyUnder(90, "Word of Glory");
    
    // 3. If target found, log and cast
    if (t != null) 
    { 
        Log("Casting Word of Glory on " + t + " (" + HealthPct(t) + "%)"); 
        return CastOnFocus(t, "cast_wog"); 
    }
}
```

### Two-Tick Casting

All targeted spells use the queue system:

**Tick 1**: `CastOnFocus(unit, macro)` → sets focus → queues macro  
**Tick 2**: `ProcessQueue()` → fires macro → spell casts on focused target

This is required because WoW macros need the focus to be set before `[@focus]` works.

### Instant Casts

For instant-cast personal spells (cooldowns, defensives):

```csharp
if (IsInCombat() && Inferno.CanCast("Divine Protection"))
{ 
    Log("Casting Divine Protection"); 
    return CastPersonal("Divine Protection"); 
}
```

No queue needed - fires immediately.

## Debugging

### Check Build Output

The built file includes source comments:
```csharp
// ========================================
// FROM: Components\00_Core.cs
// ========================================
```

If something breaks, you can trace it back to the source component.

### Enable Logging

In-game, ensure "Enable Logging" setting is checked. Logs go to:
```
penelos_paladin_holy_YYYYMMDD_HHMMSS.log
```

### Common Issues

| Issue | Cause | Fix |
|-------|-------|-----|
| Build fails | Missing component file | Check Components/ folder |
| Compile error | Duplicate method | Check for name conflicts in class files |
| Spell spam | Missing `Inferno.CanCast()` check | Add CanCast to gambit condition |
| Spells don't cast | Queue bug | Check ProcessQueue is called first in CombatTick |

## Component Guidelines

### Components/ Files

- ✅ Generic, reusable across all classes
- ✅ No class-specific constants or spells
- ✅ Well-commented
- ❌ Don't include class-specific logic

### Classes/ Files

- ✅ Class-specific constants, spells, macros
- ✅ Gambit priority logic
- ✅ Spell-specific workarounds (e.g., Holy Shock charges)
- ❌ Don't duplicate shared code (use Components/)

---

**Status**: ✅ **Production Ready**  
**Architecture**: Component-Based Build System with Integrated Security Validation  
**Classes Implemented**: Paladin Holy  
**Next**: Add more classes as needed

## Documentation

- **QUICKSTART.md** - Fast reference for common tasks
- **NEW_CLASS_TEMPLATE.md** - Step-by-step guide for adding new classes
- **COMPONENTS_GUIDE.md** - Component editing workflow
- **VISUAL_GUIDE.md** - Diagrams and flow charts
- **SECURITY_VALIDATION.md** - Security validator integration and troubleshooting
- **SEPARATE_CLASSES.md** - Full architectural analysis and design decisions
- **REFACTOR_SUMMARY.md** - What changed from monolithic to component-based


