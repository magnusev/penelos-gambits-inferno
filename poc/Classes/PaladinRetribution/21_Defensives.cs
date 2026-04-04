// ========================================
// PALADIN RETRIBUTION - DEFENSIVE LOGIC
// ========================================

private bool HandleDefensives()
{
    if (!IsSettingOn("Use Defensives")) return false;
    
    int playerHp = HealthPct("player");
    int holyPower = PowerCurrent(HOLY_POWER);
    
    // Divine Shield - bubble (emergency)
    if (playerHp <= GetSlider("Divine Shield HP %") && CastDefensive("Divine Shield"))
        return true;
    
    // Lay on Hands - full heal
    if (playerHp <= GetSlider("Lay on Hands HP %") && CastDefensive("Lay on Hands"))
        return true;
    
    // Word of Glory - Holy Power heal
    if (playerHp <= GetSlider("Word of Glory HP %") && holyPower >= 3 && CastDefensive("Word of Glory", offGCD: false))
        return true;
    
    // Shield of Vengeance - damage reduction + damage
    if (playerHp <= GetSlider("Shield of Vengeance HP %") && CastDefensive("Shield of Vengeance"))
        return true;
    
    // Healthstone
    if (playerHp <= GetSlider("Healthstone HP %") && HasHealthstone() && IsItemReady(HEALTHSTONE_ID))
    {
        Log("Using Healthstone (player " + playerHp + "%)");
        Inferno.Cast(MACRO_USE_HEALTHSTONE, QuickDelay: true);
        return true;
    }
    
    return false;
}

