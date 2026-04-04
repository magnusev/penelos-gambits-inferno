// ========================================
// PRIEST SHADOW - DEFENSIVE LOGIC
// ========================================

private bool HandleDefensives()
{
    if (!IsSettingOn("Use Defensives")) return false;
    
    int playerHp = HealthPct("player");
    
    // Dispersion - emergency damage reduction
    if (playerHp <= GetSlider("Dispersion HP %") && CanCastSpell("Dispersion"))
    {
        Log("Casting Dispersion (player " + playerHp + "%)");
        return CastPersonal("Dispersion");
    }
    
    // Vampiric Embrace - healing cooldown (off-GCD)
    if (playerHp <= GetSlider("Vampiric Embrace HP %") && Inferno.CanCast("Vampiric Embrace", IgnoreGCD: true))
    {
        Log("Casting Vampiric Embrace (player " + playerHp + "%)");
        Inferno.Cast("Vampiric Embrace", QuickDelay: true);
        return true;
    }
    
    // Desperate Prayer - self-heal (off-GCD)
    if (playerHp <= GetSlider("Desperate Prayer HP %") && Inferno.CanCast("Desperate Prayer", IgnoreGCD: true))
    {
        Log("Casting Desperate Prayer (player " + playerHp + "%)");
        Inferno.Cast("Desperate Prayer", QuickDelay: true);
        return true;
    }
    
    // Healthstone
    if (playerHp <= GetSlider("Healthstone HP %") && HasHealthstone() && IsItemReady(HEALTHSTONE_ID))
    {
        Log("Using Healthstone (player " + playerHp + "%)");
        Inferno.Cast(MACRO_USE_HEALTHSTONE, QuickDelay: true);
        return true;
    }
    
    // Power Word: Shield (don't recast if already up)
    if (playerHp <= GetSlider("Power Word: Shield HP %") && BuffRemaining("Power Word: Shield") < GCD() && CanCastSpell("Power Word: Shield"))
    {
        Log("Casting Power Word: Shield (player " + playerHp + "%)");
        return CastPersonal("Power Word: Shield");
    }
    
    return false;
}

