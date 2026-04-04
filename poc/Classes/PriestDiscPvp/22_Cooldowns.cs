// ========================================
// PRIEST DISCIPLINE PVP - COOLDOWNS
// ========================================

private bool HandleCooldowns(bool inBurst)
{
    if (IsCustomCommandOn("NoCDs")) return false;
    
    // Power Infusion - damage/haste cooldown
    if (IsSettingOn("Use Power Infusion") && CastCooldown("Power Infusion"))
        return true;
    
    // Evangelism - burst atonement extension (during burst window)
    if (IsSettingOn("Use Evangelism Burst") && inBurst && CastCooldown("Evangelism"))
        return true;
    
    return false;
}

// Trinkets during Power Infusion or Entropic Rift
private bool HandleTrinkets()
{
    if (!IsSettingOn("Use Trinkets") || IsCustomCommandOn("NoCDs"))
        return false;
    
    if (!HasBuff("Power Infusion") && !HasBuff("Entropic Rift"))
        return false;
    
    if (CanUseTrinket(13))
    {
        Log("Using Trinket 1");
        Inferno.Cast("trinket1");
        return true;
    }
    
    if (CanUseTrinket(14))
    {
        Log("Using Trinket 2");
        Inferno.Cast("trinket2");
        return true;
    }
    
    return false;
}

