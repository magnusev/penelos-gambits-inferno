// ========================================
// PALADIN RETRIBUTION - COOLDOWNS
// ========================================

private bool HandleCooldowns()
{
    if (IsCustomCommandOn("NoCDs")) return false;
    
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
    
    // Execution Sentence - if Wake of Ashes coming up soon and Expurgation is ticking (or Holy Flames not talented)
    if (IsSettingOn("Use Execution Sentence") && SpellCooldown("Wake of Ashes") < GCDMAX() 
        && (!IsTalentKnown("Holy Flames") || DebuffRemaining("Expurgation") > GCD()))
    {
        if (CastOffensive("Execution Sentence")) return true;
    }
    
    // Avenging Wrath - if Expurgation is ticking (or Holy Flames not talented) and Judgment is up (or Light's Guidance not talented)
    if (IsSettingOn("Use Avenging Wrath") && !IsTalentKnown("Radiant Glory")
        && (!IsTalentKnown("Holy Flames") || DebuffRemaining("Expurgation") > GCD())
        && (!IsTalentKnown("Light's Guidance") || DebuffRemaining("Judgment") > GCD()))
    {
        if (CastCooldown("Avenging Wrath")) return true;
    }
    
    return false;
}

