# Adding a New Class - Step-by-Step Guide

## Quick Template

To add a new class (e.g., Priest Holy), follow these steps:

### 1. Create Class Folder

```powershell
cd C:\Repos\PenelosGambitsInfernoReborn\poc
mkdir Classes\PriestHoly
```

### 2. Copy Template from PaladinHoly

```powershell
cp Classes\PaladinHoly\10_Config.cs Classes\PriestHoly\10_Config.cs
cp Classes\PaladinHoly\11_Spells.cs Classes\PriestHoly\11_Spells.cs
cp Classes\PaladinHoly\20_MainTick.cs Classes\PriestHoly\20_MainTick.cs
cp Classes\PaladinHoly\30_HealGambits.cs Classes\PriestHoly\30_HealGambits.cs
cp Classes\PriestHoly\31_DmgGambits.cs Classes\PriestHoly\31_DmgGambits.cs
cp Classes\PaladinHoly\32_DungeonGambits.cs Classes\PriestHoly\32_DungeonGambits.cs
```

### 3. Edit 10_Config.cs

**Remove/Change**:
- Remove `HOLY_POWER` constant (priests don't use Holy Power)
- Remove Holy Shock tracking variables (`_hsCharges`, etc.)
- Change log file name to `penelos_priest_holy_...`

**Update LoadSettings**:
```csharp
public override void LoadSettings()
{
    Settings.Add(new Setting("Enable Logging", true));
    Settings.Add(new Setting("Use Circle of Healing", true));
    Settings.Add(new Setting("Use Prayer of Healing", true));
    Settings.Add(new Setting("Healthstone HP %", 1, 100, 50));
}
```

**Update Initialize**:
```csharp
public override void Initialize()
{
    // Priest spells
    Spellbook.Add("Circle of Healing");
    Spellbook.Add("Flash Heal");
    Spellbook.Add("Guardian Spirit");
    Spellbook.Add("Heal");
    Spellbook.Add("Holy Word: Sanctify");
    Spellbook.Add("Holy Word: Serenity");
    Spellbook.Add("Prayer of Healing");
    Spellbook.Add("Prayer of Mending");
    Spellbook.Add("Renew");
    // ... more spells

    // Priest macros
    Macros.Add("cast_fh", "/cast [@focus] Flash Heal");
    Macros.Add("cast_heal", "/cast [@focus] Heal");
    Macros.Add("cast_coh", "/cast Circle of Healing");
    Macros.Add("cast_poh", "/cast [@focus] Prayer of Healing");
    Macros.Add("cast_gs", "/cast [@focus] Guardian Spirit");
    Macros.Add("cast_sanctify", "/cast [@cursor] Holy Word: Sanctify");
    Macros.Add("cast_serenity", "/cast [@focus] Holy Word: Serenity");
    Macros.Add("cast_pom", "/cast [@focus] Prayer of Mending");
    Macros.Add("cast_renew", "/cast [@focus] Renew");
    Macros.Add("focus_player", "/focus player");
    for (int i = 1; i <= 4; i++) Macros.Add("focus_party" + i, "/focus party" + i);
    for (int i = 1; i <= 28; i++) Macros.Add("focus_raid" + i, "/focus raid" + i);
    Macros.Add("use_healthstone", "/use Healthstone");
    
    CustomFunctions.Add("HasHealthstone", "return GetItemCount(5512) > 0 and 1 or 0");

    _logFile = "penelos_priest_holy_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log";
    Inferno.PrintMessage("Penelos Gambits - Priest Holy loaded!", Color.Green);
    Log("Initialize complete");
}
```

### 4. Edit 11_Spells.cs

If the class has spell-specific workarounds, add them here.

**For Priest Holy**, you might remove Holy Shock tracking and add:
```csharp
// ========================================
// PRIEST HOLY - SPELL-SPECIFIC LOGIC
// ========================================

// Example: Track Holy Word cooldowns if API is buggy
// (Usually not needed - most priests spells work correctly)

// If everything works via API, you can leave this file minimal or delete it
```

### 5. Edit 20_MainTick.cs

Usually this file is **identical across all healer classes**. You might only change it for DPS/Tank specs.

For Priest Holy, **leave it as-is** (no changes needed).

### 6. Edit 30_HealGambits.cs

Replace Paladin spells with Priest spells:

```csharp
// ========================================
// PRIEST HOLY - HEAL PRIORITY
// ========================================

private bool RunHealGambits()
{
    // Healthstone if player under threshold
    if (IsInCombat() && UnitUnder("player", GetSlider("Healthstone HP %")) && HasHealthstone() && Inferno.ItemCooldown(HEALTHSTONE_ID) == 0)
    { Log("Using Healthstone (player " + HealthPct("player") + "%)"); Inferno.Cast("use_healthstone", QuickDelay: true); return true; }

    // Guardian Spirit if player under 30%
    if (IsInCombat() && UnitUnder("player", 30) && Inferno.CanCast("Guardian Spirit"))
    { Log("Casting Guardian Spirit on player"); return CastOnFocus("player", "cast_gs"); }

    // Holy Word: Sanctify (ground AoE) if 3+ under 80%
    if (IsInCombat() && GroupMembersUnder(80, 3) && Inferno.CanCast("Holy Word: Sanctify"))
    { Log("Casting Holy Word: Sanctify"); Inferno.Cast("cast_sanctify"); return true; }

    // Circle of Healing if 3+ under 90%
    if (IsInCombat() && IsSettingOn("Use Circle of Healing") && GroupMembersUnder(90, 3) && Inferno.CanCast("Circle of Healing"))
    { Log("Casting Circle of Healing"); return CastPersonal("Circle of Healing"); }

    // Prayer of Healing if 3+ under 85%
    if (IsInCombat() && IsSettingOn("Use Prayer of Healing") && GroupMembersUnder(85, 3))
    { string t = LowestAllyUnder(85, "Prayer of Healing"); if (t != null) { Log("Casting Prayer of Healing on " + t); return CastOnFocus(t, "cast_poh"); } }

    // Holy Word: Serenity if lowest under 70%
    if (IsInCombat())
    { string t = LowestAllyUnder(70, "Holy Word: Serenity"); if (t != null) { Log("Casting Holy Word: Serenity on " + t + " (" + HealthPct(t) + "%)"); return CastOnFocus(t, "cast_serenity"); } }

    // Flash Heal if lowest under 60%
    if (IsInCombat() && CanCastWhileMoving("Flash Heal"))
    { string t = LowestAllyUnder(60, "Flash Heal"); if (t != null) { Log("Casting Flash Heal on " + t + " (" + HealthPct(t) + "%)"); return CastOnFocus(t, "cast_fh"); } }

    // Heal if lowest under 90%
    if (IsInCombat() && CanCastWhileMoving("Heal"))
    { string t = LowestAllyUnder(90, "Heal"); if (t != null) { Log("Casting Heal on " + t + " (" + HealthPct(t) + "%)"); return CastOnFocus(t, "cast_heal"); } }

    return false;
}
```

### 7. Edit 31_DmgGambits.cs

For healers, this is usually minimal or filler:

```csharp
// ========================================
// PRIEST HOLY - DAMAGE PRIORITY
// ========================================

private bool RunDmgGambits()
{
    // Priest healers typically don't DPS much
    // Add Smite filler if desired
    return false;
}
```

### 8. Edit 32_DungeonGambits.cs

Update dungeon-specific mechanics:

```csharp
private bool RunDungeonGambits(int mapId)
{
    if (!IsInCombat()) return false;
    
    switch (mapId)
    {
        case 480: // Proving Grounds
            return TryDispel("Aqua Bomb");
            
        // Add priest-specific dungeon logic here
        
        default: 
            return false;
    }
}

// TryDispel, TryDispelStacks, TryBof can usually stay the same
// (they use shared selectors and conditions)
```

### 9. Update CanCastWhileMoving (in Components/01_Conditions.cs)

If Priest has different movement buffs, update the shared condition:

```csharp
// Movement checks for cast-time spells
private bool CanCastWhileMoving(string spell)
{
    if (!Inferno.IsMoving("player")) return true;
    
    // Paladin buffs
    if (spell == "Flash of Light" && Inferno.HasBuff("Infusion of Light", "player", true)) return true;
    if (spell == "Holy Light" && Inferno.HasBuff("Hand of Divinity", "player", true)) return true;
    
    // Priest buffs (add these)
    if (spell == "Flash Heal" && Inferno.HasBuff("Surge of Light", "player", true)) return true;
    // Add more as needed
    
    return false;
}
```

### 10. Register in BuildAll.ps1

```powershell
# Edit Build\BuildAll.ps1
$classes = @(
    @{ Class = "PaladinHoly"; ClassName = "HolyPaladinPvE" }
    @{ Class = "PriestHoly"; ClassName = "HolyPriestPvE" }  # ← ADD THIS LINE
)
```

### 11. Build and Test

```powershell
cd Build
.\BuildRotation.ps1 -Class PriestHoly -ClassName HolyPriestPvE -LocalOnly

# Test Output\PriestHoly_rotation.cs in-game

# If successful, deploy to bot:
.\BuildRotation.ps1 -Class PriestHoly -ClassName HolyPriestPvE
```

## Common Changes by Class Type

### Healers
- **Config**: Different spells, no/different secondary resource
- **Spells**: Class-specific mechanics (charges, procs)
- **HealGambits**: Different spells, different thresholds
- **DmgGambits**: Usually minimal
- **MainTick**: Usually identical

### DPS
- **Config**: Different spells, often complex resources
- **Spells**: Rotation-specific tracking (combo points, runes, etc.)
- **DpsGambits**: Main rotation logic (rename from HealGambits)
- **CooldownGambits**: Burst windows (rename from DmgGambits)
- **MainTick**: Same pattern, different gambit names

### Tanks
- **Config**: Different spells, rage/mitigation resources
- **Spells**: Mitigation tracking
- **DefenseGambits**: Active mitigation rotation
- **ThreatGambits**: Threat management
- **MainTick**: Same pattern

## File Naming Convention

| Prefix | Purpose | Example |
|--------|---------|---------|
| 10-19 | Configuration | 10_Config.cs |
| 20-29 | Main loops | 20_MainTick.cs |
| 30-39 | Primary rotation | 30_HealGambits.cs, 30_DpsGambits.cs |
| 40-49 | Secondary rotation | 31_DmgGambits.cs, 40_CooldownGambits.cs |
| 50-59 | Situational | 32_DungeonGambits.cs, 50_PvpGambits.cs |

## Tips

- ✅ **Start with a working rotation** (like PaladinHoly) as template
- ✅ **Make small changes** - edit one file, rebuild, test
- ✅ **Use Components/** for truly generic code
- ✅ **Document class-specific workarounds** in Spells.cs
- ❌ **Don't copy/paste** - if code is duplicated, it belongs in Components/
- ❌ **Don't overthink** - keep it simple, follow the patterns

---

**Ready to add your first new class!** 🚀

