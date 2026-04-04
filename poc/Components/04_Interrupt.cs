// ========================================
// UNIVERSAL - INTERRUPT LOGIC
// ========================================
// Intelligent interrupt system with randomized timing
// Requires: INTERRUPT_SPELL constant, _rng, _lastCastingID, _interruptTargetPct variables in class config
// NOTE: Classes without interrupts can skip defining these - function will return false

private bool HandleInterrupt()
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
    
    int castingID = TargetCastingID();
    if (!TargetIsCasting())
    {
        _lastCastingID = 0;
        return false;
    }
    
    // New cast started - randomize interrupt percentage
    if (castingID != _lastCastingID)
    {
        _lastCastingID = castingID;
        int minPct = GetSlider("Interrupt at cast % (min)");
        int maxPct = GetSlider("Interrupt at cast % (max)");
        if (maxPct < minPct) maxPct = minPct;
        _interruptTargetPct = _rng.Next(minPct, maxPct + 1);
    }
    
    // Calculate cast progress percentage
    int elapsed = CastingElapsed();
    int remaining = CastingRemaining();
    int total = elapsed + remaining;
    if (total <= 0) return false;
    
    int castPct = (elapsed * 100) / total;
    
    // Interrupt at randomized percentage
    if (castPct >= _interruptTargetPct && Inferno.CanCast(INTERRUPT_SPELL, IgnoreGCD: true))
    {
        Log("Interrupting at " + castPct + "% (target: " + _interruptTargetPct + "%)");
        Inferno.Cast(INTERRUPT_SPELL, QuickDelay: true);
        _lastCastingID = 0;
        return true;
    }
    
    return false;
}


