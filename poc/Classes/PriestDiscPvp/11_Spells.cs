// ========================================
// PRIEST DISCIPLINE PVP - SPELL HELPERS
// ========================================

// PvP uses direct casting on units
private bool CastOnUnit(string spellName, string unit)
{
    if (CanCast(spellName, unit))
    {
        Log("Casting " + spellName + " on " + unit);
        Inferno.Cast(spellName, QuickDelay: true);
        return true;
    }
    return false;
}

// Cast offensive spell on target
private bool CastOffensive(string spellName)
{
    if (CanCast(spellName, "target"))
    {
        Log("Casting " + spellName);
        return CastOnEnemy(spellName);
    }
    return false;
}

// Cast cooldown (respects NoCDs)
private bool CastCooldown(string spellName)
{
    if (IsCustomCommandOn("NoCDs")) return false;
    
    if (CanCastSpell(spellName))
    {
        Log("Casting " + spellName + " (cooldown)");
        return CastPersonal(spellName);
    }
    return false;
}

