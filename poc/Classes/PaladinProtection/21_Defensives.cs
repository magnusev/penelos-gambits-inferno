// ========================================
// PALADIN PROTECTION - DEFENSIVE LOGIC
// ========================================

private bool HandleDefensives()
{
    if (!IsSettingOn("Use Defensives")) return false;
    
    int playerHp = HealthPct("player");
    int holyPower = PowerCurrent(HOLY_POWER);
    
    // Lay on Hands - emergency full heal
    if (playerHp <= GetSlider("Lay on Hands HP %") && CastDefensive("Lay on Hands"))
        return true;
    
    // Guardian of Ancient Kings - major defensive cooldown
    if (playerHp <= GetSlider("Guardian of Ancient Kings HP %") && !HasBuff("Guardian of Ancient Kings") && CastDefensive("Guardian of Ancient Kings"))
        return true;
    
    // Ardent Defender - cheat death mechanic
    if (playerHp <= GetSlider("Ardent Defender HP %") && CastDefensive("Ardent Defender"))
        return true;
    
    // Word of Glory - self-heal with Holy Power
    if (playerHp <= GetSlider("Word of Glory HP %") && holyPower >= 3 && CastDefensive("Word of Glory", offGCD: false))
        return true;
    
    // Healthstone - emergency consumable
    if (playerHp <= GetSlider("Healthstone HP %") && HasHealthstone() && IsItemReady(HEALTHSTONE_ID))
    {
        Log("Using Healthstone (player " + playerHp + "%)");
        Inferno.Cast(MACRO_USE_HEALTHSTONE, QuickDelay: true);
        return true;
    }
    
    return false;
}

