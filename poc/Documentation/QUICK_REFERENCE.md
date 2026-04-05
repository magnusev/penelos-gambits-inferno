# Quick Reference: UnitCastingAtPercent Condition

## Function Signature
```csharp
private bool UnitCastingAtPercent(string unit, int minPct, int maxPct = 100)
```

## Parameters
- **unit**: Unit to check ("target", "boss1", "boss2", "focus", "arena1", etc.)
- **minPct**: Minimum cast percentage (0-100)
- **maxPct**: Maximum cast percentage (0-100), optional, defaults to 100

## Returns
- `true` if unit is casting an interruptible spell AND cast progress is >= minPct AND <= maxPct
- `false` otherwise

## Quick Examples

### Check if boss1 is 30% into casting
```csharp
if (UnitCastingAtPercent("boss1", 30))
{
    // Use defensive cooldown
}
```

### Check if target is between 70-90% into casting
```csharp
if (UnitCastingAtPercent("target", 70, 90))
{
    // Interrupt late to bait
}
```

### Protect focus when boss is casting
```csharp
if (UnitCastingAtPercent("boss1", 25) && Inferno.UnitExists("focus"))
{
    Inferno.Cast("Blessing of Protection", "focus");
}
```

### Multiple boss checks
```csharp
for (int i = 1; i <= 5; i++)
{
    string boss = "boss" + i;
    if (Inferno.UnitExists(boss) && UnitCastingAtPercent(boss, 30))
    {
        // React to any boss casting
    }
}
```

## Common Patterns

### Early Reaction (10-30%)
Use for defensive cooldowns that need to be preemptive:
```csharp
if (UnitCastingAtPercent("boss1", 10, 30) && CanCastSpell("Aura Mastery"))
    Inferno.Cast("Aura Mastery");
```

### Mid Reaction (30-60%)
Use for standard reactions:
```csharp
if (UnitCastingAtPercent("target", 30, 60) && CanCastSpell("Spell Reflect"))
    Inferno.Cast("Spell Reflect");
```

### Late Reaction (70-90%)
Use for interrupt baiting or last-second reactions:
```csharp
if (UnitCastingAtPercent("target", 70, 90) && CanCast("Kick", "target"))
    Inferno.Cast("Kick");
```

## Combine with Spell Name
```csharp
string spellName = Inferno.CastingName("boss1");
if (spellName == "Pyroblast" && UnitCastingAtPercent("boss1", 20))
{
    // React to specific spell
}
```

## Related Functions

### HandleInterruptOnUnit
```csharp
// Interrupt with randomized timing from settings
HandleInterruptOnUnit("boss1", "Rebuke")
```

### ShouldInterruptCast (alias)
```csharp
// Same as UnitCastingAtPercent, more semantic name
if (ShouldInterruptCast("boss1", 30, 50))
    // Use defensive
```

## Inferno API Used
- `Inferno.CastingID(unit)` - Returns casting spell ID (0 if not casting)
- `Inferno.IsInterruptable(unit)` - Returns true if cast can be interrupted
- `Inferno.CastingElapsed(unit)` - Returns milliseconds elapsed in cast
- `Inferno.CastingRemaining(unit)` - Returns milliseconds remaining in cast
- `Inferno.CastingName(unit)` - Returns name of spell being cast

