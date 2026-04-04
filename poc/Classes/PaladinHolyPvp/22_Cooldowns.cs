// ========================================
// PALADIN HOLY PVP - COOLDOWNS
// ========================================

private bool HandleCooldowns(bool inCrusader)
{
    if (IsCustomCommandOn("NoCDs")) return false;
    
    // Avenging Crusader - PvP healing/damage burst
    if (IsSettingOn("Use Avenging Crusader") && TargetIsEnemy() && CastCooldown("Avenging Crusader"))
        return true;
    
    // Divine Toll - burst Holy Power generation
    if (IsSettingOn("Use Divine Toll") && CastCooldown("Divine Toll"))
        return true;
    
    return false;
}

// Trinkets during Avenging Crusader
private bool HandleTrinkets()
{
    if (!IsSettingOn("Use Trinkets") || IsCustomCommandOn("NoCDs"))
        return false;
    
    if (!HasBuff("Avenging Crusader"))
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

