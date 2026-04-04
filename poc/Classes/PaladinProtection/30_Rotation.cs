// ========================================
// PALADIN PROTECTION - ROTATION
// ========================================

private bool RunRotation()
{
    int holyPower = PowerCurrent(HOLY_POWER);
    
    // Cooldowns
    if (!IsCustomCommandOn("NoCDs"))
    {
        // Avenging Wrath
        if (IsSettingOn("Use Avenging Wrath") && CastOffensive("Avenging Wrath"))
            return true;
        
        // Trinkets during Avenging Wrath
        if (IsSettingOn("Use Trinkets") && HasBuff("Avenging Wrath"))
        {
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
        }
    }
    
    // FINISHERS at 3+ Holy Power
    if (holyPower >= 3)
    {
        // Hammer of Light
        if (CastOffensive("Hammer of Light")) return true;
        
        // Shield of the Righteous
        if (CastOffensive("Shield of the Righteous")) return true;
    }
    
    // GENERATORS
    // Holy Armaments
    if (CastOffensive("Holy Armaments")) return true;
    
    // Avenger's Shield (highest priority generator)
    if (CastOffensive("Avenger's Shield")) return true;
    
    // Judgment
    if (CastOffensive("Judgment")) return true;
    
    // Divine Toll
    if (CastOffensive("Divine Toll")) return true;
    
    // Hammer of Wrath
    if (CastOffensive("Hammer of Wrath")) return true;
    
    // Blessed Hammer / Hammer of the Righteous
    if (CastOffensive("Blessed Hammer")) return true;
    if (CastOffensive("Hammer of the Righteous")) return true;
    
    // Consecration (maintain uptime)
    if (BuffRemaining("Consecration") < GCD() && CastOffensive("Consecration"))
        return true;
    
    return false;
}

