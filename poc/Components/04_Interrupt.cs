// ========================================
// UNIVERSAL - INTERRUPT LOGIC
// ========================================
// Intelligent interrupt system with randomized timing
// Requires: INTERRUPT_SPELL constant, _rng in class config
// NOTE: Classes without interrupts can skip defining these - function will return false
// Track interrupt state per unit using two dictionaries
private Dictionary<string, int> _interruptCastID = new Dictionary<string, int>();
private Dictionary<string, int> _interruptTargetPct = new Dictionary<string, int>();
private bool HandleInterrupt()
{
    return HandleInterruptOnUnit("target", INTERRUPT_SPELL);
}
// Generic interrupt handler for any unit with any spell
private bool HandleInterruptOnUnit(string unit, string spell)
{
    // Skip if class doesn't have interrupt configured (healers, etc.)
    try 
    {
        if (!IsSettingOn("Auto Interrupt")) return false;
    }
    catch 
    {
        // Setting doesn't exist - this class doesn't use interrupts
        return false;
    }
    int castingID = Inferno.CastingID(unit);
    if (!Inferno.IsInterruptable(unit) || castingID == 0)
    {
        // Clear tracking when not casting
        if (_interruptCastID.ContainsKey(unit))
        {
            _interruptCastID.Remove(unit);
            _interruptTargetPct.Remove(unit);
        }
        return false;
    }
    // Get or initialize tracking for this unit
    if (!_interruptCastID.ContainsKey(unit) || _interruptCastID[unit] != castingID)
    {
        // New cast started - randomize interrupt percentage
        int minPct = GetSlider("Interrupt at cast % (min)");
        int maxPct = GetSlider("Interrupt at cast % (max)");
        if (maxPct < minPct) maxPct = minPct;
        int targetPct = _rng.Next(minPct, maxPct + 1);
        _interruptCastID[unit] = castingID;
        _interruptTargetPct[unit] = targetPct;
    }
    int interruptPct = _interruptTargetPct[unit];
    // Check if we've reached the interrupt percentage
    if (UnitCastingAtPercent(unit, interruptPct) && Inferno.CanCast(spell, IgnoreGCD: true))
    {
        // Calculate actual cast percentage for logging
        int elapsed = Inferno.CastingElapsed(unit);
        int remaining = Inferno.CastingRemaining(unit);
        int total = elapsed + remaining;
        int castPct = total > 0 ? (elapsed * 100) / total : 0;
        Log("Interrupting " + unit + " at " + castPct + "% (target: " + interruptPct + "%)");
        Inferno.Cast(spell, QuickDelay: true);
        _interruptCastID.Remove(unit);
        _interruptTargetPct.Remove(unit);
        return true;
    }
    return false;
}
// Condition: Returns true if unit is casting and has reached the target interrupt percentage
// Can be used with randomized or fixed percentages for defensive reactions
private bool ShouldInterruptCast(string unit, int minPct, int maxPct = 100)
{
    return UnitCastingAtPercent(unit, minPct, maxPct);
}