# Interrupt Handler Refactoring Summary

## What Was Changed

The interrupt system has been refactored from a hardcoded target-only function into a flexible condition system that can work with any unit and be used for various reactive abilities.

## Files Modified

### 1. `poc/Components/01_Conditions.cs`
**Added:**
- `UnitCastingAtPercent(string unit, int minPct, int maxPct = 100)` - Core condition function

**Purpose:** 
- Returns true if specified unit is casting an interruptible spell and cast progress is within percentage range
- Can be used for any unit: "target", "boss1", "focus", "arena1", etc.
- Percentage range allows flexible timing windows

### 2. `poc/Components/04_Interrupt.cs`
**Changed:**
- Replaced simple `_lastCastingID` and `_interruptTargetPct` variables with two dictionaries
- Uses `Dictionary<string, int> _interruptCastID` and `Dictionary<string, int> _interruptTargetPct` for tracking
- Updated `HandleInterrupt()` to use dictionary-based tracking
- Added `HandleInterruptOnUnit(string unit, string spell)` for flexible unit targeting
- Updated `ShouldInterruptCast()` to use new `UnitCastingAtPercent` condition

**Note:** Originally attempted to use `Dictionary<string, (int, int)>` tuple syntax, but this caused C# compilation errors in the target environment. The solution uses two separate dictionaries instead, which is compatible with all C# versions.

**Benefits:**
- Can track multiple units simultaneously (target, boss1, boss2, etc.)
- Each cast gets independent randomization
- Cleaner state management
- More flexible for advanced use cases
- Compatible with older C# versions (no tuple syntax required)

### 3. Class Configuration Files (All `10_Config.cs`)
**Modified files:**
- `poc/Classes/PriestShadow/10_Config.cs`
- `poc/Classes/PriestHoly/10_Config.cs`
- `poc/Classes/PriestDiscPvp/10_Config.cs`
- `poc/Classes/PaladinRetribution/10_Config.cs`
- `poc/Classes/PaladinProtection/10_Config.cs`
- `poc/Classes/PaladinHoly/10_Config.cs`
- `poc/Classes/PaladinHolyPvp/10_Config.cs`

**Changed:**
- Removed `_lastCastingID` and `_interruptTargetPct` variables
- Kept only `_rng` (Random number generator)
- Dictionary tracking is now defined in the universal component

**Before:**
```csharp
private Random _rng = new Random();
private int _lastCastingID = 0;
private int _interruptTargetPct = 0;
```

**After:**
```csharp
private Random _rng = new Random();
```

## New Functionality

### Condition: UnitCastingAtPercent
```csharp
// Check if boss1 is 30% into their cast
if (UnitCastingAtPercent("boss1", 30))
{
    // Use defensive ability
}

// Check if target is between 20-40% into their cast
if (UnitCastingAtPercent("target", 20, 40))
{
    // React within specific window
}
```

### Flexible Interrupts
```csharp
// Interrupt any unit with any spell
if (HandleInterruptOnUnit("boss1", "Wind Shear"))
    return true;

// Still works with original target interrupt
if (HandleInterrupt())
    return true;
```

### Defensive Reactions
```csharp
// Use Aura Mastery when boss1 is 30% into casting
if (UnitCastingAtPercent("boss1", 30) && CanCastSpell("Aura Mastery"))
{
    Log("Boss casting - using Aura Mastery");
    Inferno.Cast("Aura Mastery");
    return true;
}
```

## Use Cases

### 1. **Boss Mechanic Reactions**
React to boss casts at specific percentages for defensive cooldowns

### 2. **Multi-Target Interrupts**
Track and interrupt multiple enemies (arena1, arena2, boss1, boss2)

### 3. **Priority Interrupt Systems**
Check multiple targets with different interrupt timings

### 4. **Spell-Specific Reactions**
Combine with `Inferno.CastingName()` to react differently to different spells

### 5. **Ally Protection**
Detect when boss is casting at allies and use protective cooldowns

## Backward Compatibility

All existing code continues to work:
- `HandleInterrupt()` still works for target interrupts
- Settings-based randomization still functions
- No changes needed to existing rotations

## Documentation Files Created

1. `poc/Components/INTERRUPT_USAGE_EXAMPLES.md` - Comprehensive usage guide
2. `poc/Components/INTERRUPT_CONDITION_EXAMPLES.cs` - Practical code examples

## Configuration Requirements

**For DPS/Tank with interrupts:**
```csharp
private const string INTERRUPT_SPELL = "YourInterruptSpell";
private Random _rng = new Random();

// In LoadSettings():
Settings.Add(new Setting("Auto Interrupt", true));
Settings.Add(new Setting("Interrupt at cast % (min)", 0, 100, 40));
Settings.Add(new Setting("Interrupt at cast % (max)", 0, 100, 90));
```

**For Healers without interrupts:**
```csharp
private Random _rng = new Random();
private const string INTERRUPT_SPELL = ""; // Not used
// No settings needed - can still use UnitCastingAtPercent for defensives
```

## Benefits

1. **Flexibility**: Work with any unit, not just target
2. **Multi-tracking**: Track multiple units simultaneously
3. **Reusability**: Use same condition for interrupts and defensive reactions
4. **Cleaner Code**: Dictionary-based tracking is more maintainable
5. **Better Semantics**: `ShouldInterruptCast` reads better for defensive use cases
6. **No Breaking Changes**: All existing code continues to work

## Example Integration

```csharp
private bool HandleDefensives()
{
    // React to boss1 cast with Aura Mastery
    if (UnitCastingAtPercent("boss1", 30) && CanCastSpell("Aura Mastery"))
    {
        Log("Boss1 casting - using Aura Mastery");
        Inferno.Cast("Aura Mastery");
        return true;
    }
    
    return false;
}

private bool HandleInterrupts()
{
    // Interrupt boss2 if casting
    if (HandleInterruptOnUnit("boss2", "Rebuke"))
        return true;
    
    // Default target interrupt
    if (HandleInterrupt())
        return true;
    
    return false;
}
```


