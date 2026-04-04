// ========================================
// PRIEST SHADOW - BUFF MAINTENANCE
// ========================================

// Maintains Shadowform (combat and out of combat with target)
private bool HandleShadowform()
{
    if (!IsSettingOn("Auto Shadowform")) return false;
    
    if (!Inferno.HasBuff("Shadowform") && CanCastSpell("Shadowform"))
    {
        Log("Casting Shadowform");
        return CastPersonal("Shadowform");
    }
    
    return false;
}

// Maintains Power Word: Fortitude
private bool HandlePowerWordFortitude()
{
    if (!IsSettingOn("Auto Power Word: Fortitude")) return false;
    
    if (BuffRemaining("Power Word: Fortitude") < GCD() && CanCastSpell("Power Word: Fortitude"))
    {
        Log("Casting Power Word: Fortitude");
        return CastPersonal("Power Word: Fortitude");
    }
    
    return false;
}

