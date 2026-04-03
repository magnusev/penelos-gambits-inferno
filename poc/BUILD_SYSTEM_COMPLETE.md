# ✅ Component Build System - COMPLETE!

## What We Built

A **component-based development system** that lets you write clean, well-commented, modular code while producing single-file rotations that pass the bot's security validator.

## The Complete Solution

### Development (Source Files)

```
Components/               ← Shared code (all classes)
├─ 00_Core.cs            ← Queue, throttle, logging
├─ 01_Conditions.cs      ← Boolean checks
├─ 02_Selectors.cs       ← Unit selection
└─ 03_Utilities.cs       ← Helper functions

Classes/PaladinHoly/      ← Class-specific code
├─ 10_Config.cs          ← Settings, spells, macros, constants
├─ 11_Spells.cs          ← Holy Shock charge tracking
├─ 20_MainTick.cs        ← Main loop (usually identical across classes)
├─ 30_HealGambits.cs     ← Heal priority logic
├─ 31_DmgGambits.cs      ← Damage priority logic
└─ 32_DungeonGambits.cs  ← Dungeon mechanics (dispels, etc.)
```

**Benefits**:
- ✅ **Well-commented** - explain complex logic
- ✅ **Small files** - each file is 10-75 lines
- ✅ **Focused** - one responsibility per file
- ✅ **Maintainable** - easy to find and edit specific logic

---

### Build Process

```powershell
.\Build\BuildRotation.ps1 -Class PaladinHoly -ClassName HolyPaladinPvE
```

**What the build does**:
1. **Combines** all 10 source files into one
2. **Strips comments** (avoids quote checker bugs)
3. **Validates** with SecurityValidator.csproj
4. **Deploys** to bot directory

**Output**: `Output/PaladinHoly_rotation.cs` (421 lines, no comments, passes security)

---

### Deployment (Bot Files)

```
C:\libs\Live\Rotations\Retail\PenelosPaladinHoly\rotation.cs
```

Single file, ready to run. No dependencies, no plugins required.

---

## Quote Checker Workarounds (Automated!)

The build system automatically applies these fixes:

| Issue | Fix | Applied By |
|-------|-----|-----------|
| Comments confuse parser | Strip all `//` comments | `Strip-Comments` function |
| `!= ""` triggers bug | Use `string.IsNullOrEmpty()` | Source code |
| `\n` in one-liner | Split to multi-line | Source code formatting |
| Long for-loops | Add braces `{ }` | Source code formatting |
| Embedded strings | Extract to variable | Source code |

**You just write normal code - the build handles the rest!**

---

## Build System Features

### ✅ Security Validation

Every build runs the SecurityValidator and blocks deployment if there are issues:

```
🔒 Running security validation...
✅ Security validation PASSED
```

If validation fails, you get detailed error output and the build stops.

### ✅ Local Testing

```powershell
.\BuildRotation.ps1 -Class PaladinHoly -ClassName HolyPaladinPvE -LocalOnly
```

Builds to `Output/` only (doesn't deploy to bot). Perfect for testing changes.

### ✅ Build All Classes

```powershell
.\Build\BuildAll.ps1
```

Builds every registered class. Add new classes to the `$classes` array in `BuildAll.ps1`.

---

## Adding a New Class

### 1. Copy Structure

```powershell
cd C:\Repos\PenelosGambitsInfernoReborn\poc
cp -r Classes\PaladinHoly Classes\PriestHoly
```

### 2. Edit Class Files

Edit the 6 files in `Classes\PriestHoly\`:
- `10_Config.cs` - Change spells, macros, settings
- `30_HealGambits.cs` - Change heal priority
- `31_DmgGambits.cs` - Change damage priority
- `32_DungeonGambits.cs` - Change dungeon mechanics
- `11_Spells.cs` - Class-specific spell tracking (if needed)
- `20_MainTick.cs` - Usually leave unchanged

### 3. Register in BuildAll.ps1

```powershell
$classes = @(
    @{ Class = "PaladinHoly"; ClassName = "HolyPaladinPvE" }
    @{ Class = "PriestHoly"; ClassName = "HolyPriestPvE" }  # ← ADD THIS
)
```

### 4. Build

```powershell
.\Build\BuildRotation.ps1 -Class PriestHoly -ClassName HolyPriestPvE
```

### 5. Test

Load `Output\PriestHoly_rotation.cs` in the bot and test.

**That's it!** Components are automatically included.

---

## File Organization

### What Goes Where?

| File Type | Location | Purpose | Example |
|-----------|----------|---------|---------|
| **Shared logic** | `Components/*.cs` | Used by all classes | `LowestAllyUnder()`, `ThrottleIsOpen()` |
| **Class constants** | `Classes/*/10_Config.cs` | Class-specific setup | `HOLY_POWER = 9` |
| **Spell workarounds** | `Classes/*/11_Spells.cs` | API bug fixes | Holy Shock charge tracking |
| **Main loop** | `Classes/*/20_MainTick.cs` | Tick logic | Usually identical across classes |
| **Heal logic** | `Classes/*/30_HealGambits.cs` | Heal priority | Class-specific |
| **Damage logic** | `Classes/*/31_DmgGambits.cs` | Damage priority | Class-specific |
| **Dungeon logic** | `Classes/*/32_DungeonGambits.cs` | Mechanics | Map-specific |

---

## Best Practices

### ✅ DO:

- **Comment generously** in source files (stripped during build)
- **Keep methods small** - one responsibility each
- **Use descriptive names** - `TryDispel()`, `LowestAllyUnder()`
- **Test after every change** - build is fast (~2 seconds)
- **Commit both source and output** - easy to review changes

### ❌ DON'T:

- **Don't put class-specific code in Components/** - keep it generic
- **Don't duplicate code** - if you need it in 2 classes, put it in Components
- **Don't skip security validation** - always run the build script
- **Don't edit Output/*.cs directly** - edit source files and rebuild

---

## Troubleshooting

### Build Fails with Security Error

Check the SecurityValidator output - it tells you exactly what's wrong:
```
[Security BLOCKED] Line 160: Extremely long string literal
```

**Solution**: The issue is usually earlier in the file. Check for:
- `!= ""` patterns (use `string.IsNullOrEmpty()` instead)
- One-line for-loops with strings (add braces)
- Embedded strings (extract to variables)

### Build Succeeds but Rotation Doesn't Work

1. Check the log file (`penelos_paladin_holy_YYYYMMDD_HHMMSS.log`)
2. Enable logging setting in-game
3. Look for error messages in the Inferno console

### Want to Add Shared Utility Function

1. Add it to the appropriate `Components/*.cs` file
2. Rebuild all classes: `.\Build\BuildAll.ps1`
3. All classes now have the new function!

---

## Performance

| Metric | Value |
|--------|-------|
| **Build time** | ~2 seconds per class |
| **Output size** | ~420 lines (stripped comments) |
| **Source readability** | ✅ Excellent (10-75 lines per file) |
| **Output readability** | ✅ Good (minimal, no clutter) |
| **Runtime performance** | Identical to hand-written rotation |

---

## Summary

🎉 **The refactor is complete!**

- ✅ Component-based development (easy to maintain)
- ✅ Comment stripping (avoids quote checker)
- ✅ Security validation integrated
- ✅ Auto-deployment to bot
- ✅ Ready for multiple classes

**Development workflow**:
1. Edit source files (with comments!)
2. Run `.\Build\BuildRotation.ps1`
3. Test in bot
4. Commit changes

**The best of both worlds**: Clean source code + secure output!

