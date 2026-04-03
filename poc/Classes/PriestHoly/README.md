# Holy Priest Rotation - PriestHoly

## Overview
This is a complete Holy Priest healing rotation for World of Warcraft, built using the Penelos Gambits framework.

## File Structure

### 10_Config.cs
- Defines constants (MANA, HEALTHSTONE_ID, etc.)
- Loads settings/checkboxes for user configuration
- Initializes spellbook, macros, and custom functions
- Sets up logging

### 11_Spells.cs
- Holy Priest-specific spell logic
- Helper functions for buff/debuff checking (Prayer of Mending, Renew, Power Word: Shield)
- Buff duration tracking

### 20_MainTick.cs
- Main rotation loop (CombatTick and OutOfCombatTick)
- Processes the action queue
- Periodic diagnostic logging
- Calls dungeon-specific, healing, and damage priorities in order

### 30_HealGambits.cs
- Complete healing priority system
- Emergency cooldowns (Guardian Spirit, Power Word: Life, Desperate Prayer)
- AoE healing (Divine Hymn, Holy Word: Sanctify, Circle of Healing, Prayer of Healing)
- Single-target healing (Holy Word: Serenity, Flash Heal, Heal)
- Proactive healing (Prayer of Mending, Power Word: Shield, Renew)

### 31_DmgGambits.cs
- Damage rotation when "Do DPS" is enabled
- Offensive cooldowns (Mindgames, Holy Fire)
- DoT maintenance (Shadow Word: Pain)
- Execute mechanics (Shadow Word: Death)
- AoE damage (Halo, Divine Star)
- Filler spells (Smite)

### 32_DungeonGambits.cs
- Dungeon-specific mechanics
- Dispel priorities for various dungeons
- Map ID-based logic for different instances

## Key Features

### Intelligent Healing Priority
1. **Emergency Response**: Healthstone, Desperate Prayer, Guardian Spirit (< 25%)
2. **Critical Saves**: Power Word: Life (< 35%)
3. **Group Emergencies**: Divine Hymn (3+ under 50%)
4. **AoE Healing**: Holy Word: Sanctify, Circle of Healing, Prayer of Healing
5. **Single Target**: Holy Word: Serenity, Flash Heal, Heal
6. **Proactive**: Prayer of Mending, Power Word: Shield, Renew

### Smart Buff Management
- Avoids casting Power Word: Shield on targets with Weakened Soul debuff
- Refreshes Renew only when missing or expiring soon (< 3s)
- Maintains Prayer of Mending bouncing on group members

### Configurable Settings
- **Enable Logging**: Toggle detailed logging on/off
- **Use Circle of Healing**: Enable/disable CoH usage
- **Use Prayer of Mending**: Enable/disable PoM usage
- **Do DPS**: Toggle damage rotation
- **Healthstone HP %**: Configurable threshold for Healthstone usage (default 50%)

### Movement-Aware Casting
- Uses the shared `CanCastWhileMoving()` function from 01_Conditions.cs
- Respects cast-time spells and instant-cast buffs

### Dungeon-Specific Dispels
- Configured for common dungeons (Proving Grounds, Algeth'ar Academy, etc.)
- Priority dispel logic for dangerous debuffs
- Stack-based dispel for DoT effects

## Build Instructions

### Single Build
```powershell
.\BuildRotation.ps1 -Class "PriestHoly" -ClassName "HolyPriestPvE" -LocalOnly
```

### Build All Rotations
```powershell
.\BuildAll.ps1 -LocalOnly
```

### Deploy to Bot
```powershell
.\BuildRotation.ps1 -Class "PriestHoly" -ClassName "HolyPriestPvE"
```

## Shared Components
This rotation uses shared components from the `poc\Components` folder:
- **00_Core.cs**: Queue system, throttling, logging
- **01_Conditions.cs**: Reusable boolean checks
- **02_Selectors.cs**: Unit targeting logic
- **03_Utilities.cs**: Helper functions (HealthPct, etc.)

## Notes
- The rotation is built for PvE healing in dungeons and raids
- DPS mode is optional and can be toggled on/off
- All logging can be disabled via the "Enable Logging" setting
- Security validation passes on build

