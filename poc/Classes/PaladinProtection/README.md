# Protection Paladin Rotation - PaladinProtection

## Overview
This is a complete Protection Paladin tanking rotation converted from the example rotation.cs to the new POC modular architecture.

## File Structure

### 10_Config.cs
- Defines constants (HOLY_POWER)
- Interrupt tracking variables
- Loads settings (cooldowns, defensives, interrupt thresholds)
- Initializes spellbook, macros, and custom commands

### 11_Spells.cs
- `CastOffensive(spellName)` - Helper to cast offensive spells
- `CastDefensive(spellName, offGCD)` - Helper to cast defensive spells

### 20_Interrupt.cs
- `HandleInterrupt()` - Intelligent interrupt system
- Randomizes interrupt timing between min/max cast percentage
- Tracks casting IDs to avoid duplicate interrupts

### 21_Defensives.cs
- `HandleDefensives()` - Priority-based defensive cooldown system
- Lay on Hands → Guardian → Ardent Defender → Word of Glory → Healthstone

### 22_Racials.cs
- `HandleRacials()` - Uses racial abilities (Berserking, Blood Fury, etc.)
- Respects NoCDs custom command

### 25_OutOfCombat.cs
- `OutOfCombatTick()` - Maintains Devotion Aura

### 26_MainTick.cs
- `CombatTick()` - Main combat loop with priority system
- Custom tick (NOT using shared MainTick due to different structure)

### 30_Rotation.cs
- `RunRotation()` - DPS rotation priority
- Cooldowns → Finishers (3+ HP) → Generators → Consecration maintenance

## Rotation Priority

### Combat Flow:
1. **Defensives** - Emergency self-heals based on HP thresholds
2. **Interrupts** - Smart interrupt at randomized cast percentage
3. **Racials** - Damage cooldowns
4. **Rotation** - DPS priority

### Offensive Priority:
1. **Cooldowns**: Avenging Wrath, Trinkets (during AW)
2. **Finishers (3+ HP)**: Hammer of Light → Shield of the Righteous
3. **Generators**: Holy Armaments → Avenger's Shield → Judgment → Divine Toll → Hammer of Wrath → Blessed Hammer → Hammer of the Righteous
4. **Maintenance**: Consecration (keep uptime)

### Defensive Priority:
1. **Lay on Hands** - Emergency full heal (15% default)
2. **Guardian of Ancient Kings** - Major cooldown (25% default)
3. **Ardent Defender** - Cheat death (35% default)
4. **Word of Glory** - Holy Power heal (50% default, 3+ HP)
5. **Healthstone** - Consumable (50% default)

## Key Features

### Intelligent Interrupts
- Randomizes interrupt timing between configurable min/max percentages
- Prevents predictable interrupt patterns
- Tracks casting IDs to avoid duplicate attempts

### Smart Cooldown Usage
- Toggle Avenging Wrath on/off
- Toggle all cooldowns with `/addon NoCDs` command
- Trinkets automatically used during Avenging Wrath

### Movement-Aware
- Skips rotation if channeling
- Doesn't interrupt cast-time trinkets (Puzzle Box, Emberwing)

### Configurable Settings
- All defensive HP thresholds adjustable
- Interrupt timing randomization (min/max)
- Toggle each feature independently

## Custom Commands

- `/addon NoCDs` - Disable all cooldowns (Avenging Wrath, Trinkets, Racials)
- `/addon ForceST` - Force single-target (placeholder for future AoE logic)

## Build Instructions

### Single Build
```powershell
.\BuildRotation.ps1 -Class "PaladinProtection" -ClassName "ProtectionPaladinPvE" -LocalOnly
```

### Build All Rotations
```powershell
.\BuildAll.ps1 -LocalOnly
```

## Comparison to Original

### Original (rotation.cs):
- 140 lines
- Everything in one file
- Hardcoded magic strings
- Direct Inferno API calls throughout

### POC Version:
- 554 lines (with abstraction layer)
- Modular: 7 files organized by purpose
- DRY: Shared components reused
- Abstraction: No direct Inferno calls in rotation logic
- Readable: Full variable names, clear comments

## Notes
- Uses custom CombatTick (not shared) due to different flow than healers
- All racials supported (Berserking, Blood Fury, Ancestral Call, Fireblood, Lights Judgment)
- Interrupt system uses randomization for human-like behavior
- Security validation passes

