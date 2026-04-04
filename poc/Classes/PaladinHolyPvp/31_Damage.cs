// ========================================
// PALADIN HOLY PVP - DAMAGE PRIORITY
// ========================================

private bool HandleDamage()
{
    // Judgment - Holy Power generator
    if (CastOffensive("Judgment"))
        return true;
    
    // Crusader Strike - Holy Power generator
    if (CastOffensive("Crusader Strike"))
        return true;
    
    return false;
}

