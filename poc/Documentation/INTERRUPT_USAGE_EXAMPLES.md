# Interrupt/Cast Percentage Condition Usage Guide

## Overview
The interrupt system has been refactored into a flexible condition that can be used for various reactive abilities, not just interrupts.

## Components

### 1. UnitCastingAtPercent Condition (01_Conditions.cs)
```csharp
private bool UnitCastingAtPercent(string unit, int minPct, int maxPct = 100)
```
- **Purpose**: Returns true if the specified unit is casting an interruptible spell and the cast progress is within the percentage range
- **Parameters**:
  - `unit`: The unit to check ("target", "boss1", "focus", etc.)
  - `minPct`: Minimum cast percentage (0-100)
  - `maxPct`: Maximum cast percentage (0-100), defaults to 100
- **Returns**: `true` if unit is casting and cast is between minPct and maxPct

### 2. HandleInterruptOnUnit Function (04_Interrupt.cs)
```csharp
private bool HandleInterruptOnUnit(string unit, string spell)
```
- **Purpose**: Interrupt a unit's cast with randomized timing based on settings
- **Parameters**:
  - `unit`: The unit to interrupt ("target", "boss1", etc.)
  - `spell`: The spell to use for interrupting
- **Uses settings**: 
  - "Auto Interrupt" (checkbox)
  - "Interrupt at cast % (min)" (slider)
  - "Interrupt at cast % (max)" (slider)

### 3. ShouldInterruptCast Condition (04_Interrupt.cs)
```csharp
private bool ShouldInterruptCast(string unit, int minPct, int maxPct = 100)
```
- **Purpose**: Alias for `UnitCastingAtPercent` - more semantic name for defensive use cases
- **Parameters**: Same as `UnitCastingAtPercent`

## Usage Examples

### Example 1: Basic Interrupt on Target
```csharp
// In your damage rotation:
if (HandleInterrupt())
    return true;
```

### Example 2: Interrupt Boss1
```csharp
// Interrupt boss1 when they're 40-90% through their cast
if (HandleInterruptOnUnit("boss1", "Wind Shear"))
    return true;
```

### Example 3: Defensive Cooldown on Boss Cast
```csharp
// Use Aura Mastery when boss1 is 30% into casting
if (ShouldInterruptCast("boss1", 30) && CanCastSpell("Aura Mastery"))
{
    Log("Boss casting detected - using Aura Mastery");
    return CastPersonal("Aura Mastery");
}
```

### Example 4: React to Specific Cast Percentage Range
```csharp
// Use defensive when boss is between 20-40% on their cast
if (UnitCastingAtPercent("boss1", 20, 40) && CanCastSpell("Divine Shield"))
{
    Log("Boss dangerous cast detected early - using Divine Shield");
    return CastPersonal("Divine Shield");
}
```

### Example 5: Multiple Boss Unit Checks
```csharp
// Check multiple boss units for dangerous casts
for (int i = 1; i <= 5; i++)
{
    string boss = "boss" + i;
    
    // React early to dangerous casts (10-30%)
    if (UnitCastingAtPercent(boss, 10, 30) && Inferno.UnitExists(boss))
    {
        if (CanCastSpell("Aura Mastery"))
        {
            Log("Detected dangerous cast from " + boss + " - using Aura Mastery");
            return CastPersonal("Aura Mastery");
        }
    }
}
```

### Example 6: Ally Protection
```csharp
// Protect focus target when boss is casting at them
if (UnitCastingAtPercent("boss1", 40) && CanCastSpell("Blessing of Protection"))
{
    if (Inferno.UnitExists("focus") && !Inferno.IsDead("focus"))
    {
        Log("Protecting focus from boss cast");
        return CastOnUnit("Blessing of Protection", "focus");
    }
}
```

### Example 7: Conditional Interrupt Based on Cast Name
```csharp
// Only interrupt specific spells at certain percentages
if (Inferno.CastingName("target") == "Pyroblast" && UnitCastingAtPercent("target", 70))
{
    if (CanCast("Kick", "target"))
    {
        Log("Interrupting Pyroblast at 70%+");
        Inferno.Cast("Kick");
        return true;
    }
}
```

## Configuration Requirements

To use the interrupt system, your class config (10_Config.cs) needs:

```csharp
// For classes WITH interrupts (DPS/Tank):
private const string INTERRUPT_SPELL = "YourInterruptSpellName";
private Random _rng = new Random();

// Settings in LoadSettings():
Settings.Add(new Setting("Auto Interrupt", true));
Settings.Add(new Setting("Interrupt at cast % (min)", 0, 100, 40));
Settings.Add(new Setting("Interrupt at cast % (max)", 0, 100, 90));
```

For classes without interrupts (healers), you can still use the condition functions:
```csharp
// Minimal config for healers using reactive defensives:
private Random _rng = new Random();
private const string INTERRUPT_SPELL = ""; // Not used but required by component
```

## Tracking System

The interrupt handler uses a dictionary to track each unit independently:
```csharp
private Dictionary<string, (int castID, int targetPct)> _interruptTracking = new Dictionary<string, (int, int)>();
```

This allows:
- Multiple units to be tracked simultaneously (target, boss1, boss2, etc.)
- Each cast gets a new randomized percentage
- Clean state management when casts finish or are interrupted

## Best Practices

1. **Early Detection for Defensives**: Use low percentages (10-30%) for defensive cooldowns to react quickly
2. **Late Interrupt for DPS**: Use higher percentages (40-90%) for interrupts to bait enemy fakes
3. **Range Checks**: Use min/max ranges to create windows of opportunity
4. **Spell Name Filtering**: Combine with `Inferno.CastingName()` to react to specific spells
5. **Priority Systems**: Check dangerous bosses/spells first before falling back to general interrupts

