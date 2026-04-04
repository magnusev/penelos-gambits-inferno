// ========================================
// PRIEST DISCIPLINE PVP - DEFENSIVE LOGIC
// ========================================

private bool HandleDefensives()
{
    if (!IsSettingOn("Use Defensives")) return false;
    
    // Desperate Prayer - player under 35%
    if (IsSettingOn("Auto Desperate Prayer < 35%") && HealthPct("player") < 35 && Inferno.CanCast("Desperate Prayer", IgnoreGCD: true))
    {
        Log("Casting Desperate Prayer (player " + HealthPct("player") + "%)");
        Inferno.Cast("Desperate Prayer", QuickDelay: true);
        return true;
    }
    
    // Pain Suppression - lowest ally under 55%
    if (IsSettingOn("Auto Pain Suppression < 55%") && CanCastSpell("Pain Suppression"))
    {
        string target = LowestArenaAllyUnder(55);
        if (!string.IsNullOrEmpty(target))
        {
            Log("Casting Pain Suppression on " + target + " (" + GetUnitHealthPct(target) + "%)");
            return CastOnUnit("Pain Suppression", target);
        }
    }
    
    // Leap of Faith - lowest ally under 25% (emergency)
    if (IsSettingOn("Auto Leap of Faith < 25%") && CanCastSpell("Leap of Faith"))
    {
        string target = LowestArenaAllyUnder(25);
        if (!string.IsNullOrEmpty(target) && target != "player")
        {
            Log("Casting Leap of Faith on " + target + " (" + GetUnitHealthPct(target) + "%) - EMERGENCY");
            return CastOnUnit("Leap of Faith", target);
        }
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

