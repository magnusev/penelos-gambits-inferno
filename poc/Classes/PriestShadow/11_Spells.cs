// ========================================
// PRIEST SHADOW - SPELL HELPERS
// ========================================

// Returns true if successfully cast an offensive spell
private bool CastOffensive(string spellName)
{
    if (CanCast(spellName, "target"))
    {
        Log("Casting " + spellName);
        return CastOnEnemy(spellName);
    }
    return false;
}

// Returns true if successfully cast a cooldown (respects NoCDs command)
private bool CastCooldown(string spellName)
{
    if (IsCustomCommandOn("NoCDs")) return false;
    
    if (CanCast(spellName, "target"))
    {
        Log("Casting " + spellName + " (cooldown)");
        return CastOnEnemy(spellName);
    }
    return false;
}

