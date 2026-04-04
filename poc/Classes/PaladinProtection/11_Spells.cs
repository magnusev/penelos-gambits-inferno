// ========================================
// PALADIN PROTECTION - SPELL-SPECIFIC LOGIC
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

// Returns true if successfully cast a defensive spell (off-GCD capable)
private bool CastDefensive(string spellName, bool offGCD = true)
{
    if (offGCD && Inferno.CanCast(spellName, IgnoreGCD: true))
    {
        Log("Casting " + spellName + " (defensive)");
        Inferno.Cast(spellName, QuickDelay: true);
        return true;
    }
    else if (!offGCD && CanCastSpell(spellName))
    {
        Log("Casting " + spellName + " (defensive)");
        return CastPersonal(spellName);
    }
    return false;
}

