# Component-Based Build System - Developer Guide

## Quick Reference

```powershell
# Build all classes
.\Build\BuildAll.ps1

# Build single class
.\Build\BuildRotation.ps1 -Class PaladinHoly -ClassName HolyPaladinPvE

# Local only (no deployment)
.\Build\BuildAll.ps1 -LocalOnly
```

## Editing Workflow

### Modify Existing Class

1. Edit files in `Classes/PaladinHoly/`
2. Rebuild: `.\Build\BuildRotation.ps1 -Class PaladinHoly -ClassName HolyPaladinPvE -LocalOnly`
3. Test `Output/PaladinHoly_rotation.cs` in-game

### Add New Shared Code

1. Edit `Components/*.cs`
2. Rebuild all: `.\Build\BuildAll.ps1`
3. All classes now have the new code

### Add New Class

See main README.md

## Component Files

| File | Purpose | Modify When |
|------|---------|-------------|
| `00_Core.cs` | Queue, throttle, logging | Rarely - core system is stable |
| `01_Conditions.cs` | Boolean checks | Adding new condition types |
| `02_Selectors.cs` | Unit selection | Adding new target selection logic |
| `03_Utilities.cs` | Helper functions | Adding calculations, conversions |

## Class Files

| File | Purpose | Modify When |
|------|---------|-------------|
| `10_Config.cs` | Settings, spells, macros | Changing class setup |
| `11_Spells.cs` | Class-specific logic | Adding spell-specific workarounds |
| `20_MainTick.cs` | Main loop | Rarely - usually identical across classes |
| `30_HealGambits.cs` | Heal priority | Constantly - tuning priorities |
| `31_DmgGambits.cs` | Damage priority | Constantly - tuning priorities |
| `32_DungeonGambits.cs` | Dungeon mechanics | Adding new dungeons |

## Best Practices

- ✅ Keep component files generic (no class-specific constants)
- ✅ Use clear, descriptive method names
- ✅ Comment complex logic
- ✅ Test after every build
- ✅ Commit both source and output files
- ❌ Don't duplicate code between components and class files
- ❌ Don't put class-specific logic in Components/

