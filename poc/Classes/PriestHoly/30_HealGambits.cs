// ========================================
// PRIEST HOLY - HEAL PRIORITY
// ========================================

private bool RunHealGambits()
{
    // Healthstone if player under threshold (combat only)
    if (IsInCombat() && UnitUnder("player", GetSlider("Healthstone HP %")) && HasHealthstone() && Inferno.ItemCooldown(HEALTHSTONE_ID) == 0)
    { 
        Log("Using Healthstone (player " + HealthPct("player") + "%)"); 
        Inferno.Cast(MACRO_USE_HEALTHSTONE, QuickDelay: true); 
        return true; 
    }

    // Prayer of Mending on lowest health player if off cooldown
    if (IsInCombat() && Inferno.CanCast("Prayer of Mending"))
    { 
        string t = LowestAllyInRange("Prayer of Mending"); 
        if (t != null) 
        { 
            Log("Casting Prayer of Mending on " + t + " (" + HealthPct(t) + "%)"); 
            return CastOnFocus(t, "cast_pom"); 
        } 
    }


    return false;
}


