# Quick Start - Component-Based System

## For Developers

### Build the Rotation

```powershell
# Navigate to build folder
cd C:\Repos\PenelosGambitsInfernoReborn\poc\Build

# Build PaladinHoly and deploy to bot (includes security validation)
.\BuildRotation.ps1 -Class PaladinHoly -ClassName HolyPaladinPvE

# Build all configured rotations (with validation)
.\BuildAll.ps1

# Build locally only (no bot deployment, with validation)
.\BuildRotation.ps1 -Class PaladinHoly -ClassName HolyPaladinPvE -LocalOnly
```

**Security Validation**: Each build automatically runs the security validator. If validation fails, the build stops and shows the errors.

### Edit the Rotation

**To change heal priorities**:
1. Edit `Classes/PaladinHoly/30_HealGambits.cs`
2. Rebuild: `.\Build\BuildRotation.ps1 -Class PaladinHoly -ClassName HolyPaladinPvE -LocalOnly`
3. Test `Output/PaladinHoly_rotation.cs` in-game

**To add a new spell**:
1. Edit `Classes/PaladinHoly/10_Config.cs` → add to Spellbook and Macros
2. Edit `Classes/PaladinHoly/30_HealGambits.cs` → add to priority list
3. Rebuild and test

**To add shared functionality**:
1. Edit appropriate file in `Components/`
2. Rebuild **all** classes: `.\Build\BuildAll.ps1`
3. All rotations now have the new functionality

### Add a New Class

1. Copy template: `cp -r Classes\PaladinHoly Classes\PriestHoly`
2. Edit all files in `Classes\PriestHoly\`
3. Register in `Build\BuildAll.ps1`
4. Build: `.\Build\BuildRotation.ps1 -Class PriestHoly -ClassName HolyPriestPvE`

See `NEW_CLASS_TEMPLATE.md` for detailed instructions.

---

## For End Users

### Installation

1. Download the rotation file from `Output/PaladinHoly_rotation.cs`
2. Copy to `C:\libs\Live\Rotations\Retail\PenelosPaladinHoly\rotation.cs`
3. Load in Inferno bot
4. Start rotation

### No Build Required

End users get the **pre-built rotation.cs** files. No PowerShell scripts needed.

---

## File Reference

| What | Where |
|------|-------|
| Build scripts | `Build/*.ps1` |
| Shared code | `Components/*.cs` |
| Paladin Holy code | `Classes/PaladinHoly/*.cs` |
| Built rotations | `Output/*.cs` |
| Bot deployment | `C:\libs\Live\Rotations\Retail\Penelos*/rotation.cs` |
| Logs | `penelos_*_YYYYMMDD_HHMMSS.log` |

---

## Common Tasks

### Tune Heal Thresholds

Edit `Classes/PaladinHoly/30_HealGambits.cs`:
```csharp
// Change from 90% to 85%
{ string t = LowestAllyUnder(85, "Word of Glory"); ...
```

Rebuild: `.\Build\BuildRotation.ps1 -Class PaladinHoly -ClassName HolyPaladinPvE`

### Add a New Dungeon

Edit `Classes/PaladinHoly/32_DungeonGambits.cs`:
```csharp
switch (mapId)
{
    // ... existing dungeons
    
    case 1234: // New Dungeon
        return TryDispel("New Debuff");
        
    default: return false;
}
```

Rebuild and test.

### Fix a Bug in Queue System

Edit `Components/00_Core.cs`, then rebuild **all** classes:
```powershell
.\Build\BuildAll.ps1
```

All rotations get the fix.

---

## Troubleshooting

### Build Fails

```powershell
# Check if all files exist
Get-ChildItem Components\*.cs
Get-ChildItem Classes\PaladinHoly\*.cs
```

### Output Looks Wrong

The built file includes source markers:
```csharp
// ========================================
// FROM: Components\00_Core.cs
// ========================================
```

Use these to trace where code came from.

### Rotation Doesn't Work

1. Check `Output/PaladinHoly_rotation.cs` for compile errors
2. Enable logging in-game
3. Check log file for errors
4. Compare to original `rotation.cs` if needed

---

**Need Help?** See:
- `README.md` - Overview
- `COMPONENTS_GUIDE.md` - Component details
- `NEW_CLASS_TEMPLATE.md` - Adding new classes
- `SEPARATE_CLASSES.md` - Full architecture guide

