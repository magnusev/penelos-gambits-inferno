// ========================================
// PALADIN HOLY PVP - DEFENSIVE LOGIC
// ========================================

private bool HandleDefensives()
{
    if (!IsSettingOn("Use Defensives")) return false;
    
    // Blessing of Sacrifice - emergency save on lowest ally under 40%
    if (IsSettingOn("Auto Blessing of Sacrifice < 40%") && CanCastSpell("Blessing of Sacrifice"))
    {
        string target = LowestArenaAllyUnder(40);
        if (!string.IsNullOrEmpty(target))
        {
            Log("Casting Blessing of Sacrifice on " + target + " (" + GetUnitHealthPct(target) + "%)");
            Inferno.Cast("Blessing of Sacrifice", QuickDelay: true);
            return true;
        }
    }
    
    // Lay on Hands - emergency full heal under 25%
    if (IsSettingOn("Auto Lay on Hands < 25%") && HealthPct("player") < 25 && Inferno.CanCast("Lay on Hands", IgnoreGCD: true))
    {
        Log("Casting Lay on Hands (emergency)");
        Inferno.Cast("Lay on Hands", QuickDelay: true);
        return true;
    }
    
    // Blessing of Protection - bubble on player under 30%
    if (IsSettingOn("Auto Blessing of Protection < 30%") && HealthPct("player") < 30 && Inferno.CanCast("Blessing of Protection", IgnoreGCD: true))
    {
        Log("Casting Blessing of Protection (player " + HealthPct("player") + "%)");
        Inferno.Cast("Blessing of Protection", QuickDelay: true);
        return true;
    }
    
    // Healthstone
    if (HealthPct("player") <= GetSlider("Healthstone HP %") && HasHealthstone() && IsItemReady(HEALTHSTONE_ID))
    {
        Log("Using Healthstone (player " + HealthPct("player") + "%)");
        Inferno.Cast(MACRO_USE_HEALTHSTONE, QuickDelay: true);
        return true;
    }
    
    return false;
}

