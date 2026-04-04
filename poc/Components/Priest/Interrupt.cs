// ========================================
// SHARED PRIEST - INTERRUPT LOGIC
// ========================================
// Used by Shadow Priest (Silence instead of Rebuke)
// Requires: _rng, _lastCastingID, _interruptTargetPct variables in class config

private bool HandleInterrupt()
{
    if (!IsSettingOn("Auto Interrupt")) return false;
    
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
    
    // Interrupt at randomized percentage (Priests use Silence)
    if (castPct >= _interruptTargetPct && Inferno.CanCast("Silence", IgnoreGCD: true))
    {
        Log("Interrupting at " + castPct + "% (target: " + _interruptTargetPct + "%)");
        Inferno.Cast("Silence", QuickDelay: true);
        _lastCastingID = 0;
        return true;
    }
    
    return false;
}

