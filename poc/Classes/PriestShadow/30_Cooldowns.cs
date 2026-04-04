// ========================================
// PRIEST SHADOW - COOLDOWNS
// ========================================

private bool HandleCooldowns()
{
    if (IsCustomCommandOn("NoCDs")) return false;
    
    bool dotsUp = HasDotsUp();
    bool voidformActive = HasBuff("Voidform");
    bool hasVoidformTalent = IsTalentKnown("Voidform");
    bool powerInfusionActive = BuffRemaining("Power Infusion") > GCD();
    
    // Halo (Archon hero tree - on cooldown)
    if (IsSettingOn("Use Halo"))
    {
        if (CastCooldown("Halo")) return true;
    }
    
    // Voidform - if DoTs are up and not already in Voidform
    if (IsSettingOn("Use Voidform") && !voidformActive && dotsUp)
    {
        if (CastCooldown("Voidform")) return true;
    }
    
    // Power Infusion - if Voidform is up (or no Voidform talent) and PI not already up
    if (IsSettingOn("Use Power Infusion") && (voidformActive || !hasVoidformTalent) && !powerInfusionActive)
    {
        if (Inferno.CanCast("Power Infusion", IgnoreGCD: true))
        {
            Log("Casting Power Infusion (cooldown)");
            Inferno.Cast("Power Infusion", QuickDelay: true);
            return true;
        }
    }
    
    return false;
}

// Trinkets during Voidform, Power Infusion, or Entropic Rift
private bool HandleTrinkets()
{
    if (!IsSettingOn("Use Trinkets") || IsCustomCommandOn("NoCDs")) 
        return false;
    
    bool voidformActive = HasBuff("Voidform");
    bool powerInfusionLong = BuffRemaining("Power Infusion") >= 10000;
    bool entropicRift = HasBuff("Entropic Rift");
    
    if (!voidformActive && !powerInfusionLong && !entropicRift) 
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

