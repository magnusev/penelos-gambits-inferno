// ========================================
// PALADIN PROTECTION - OUT OF COMBAT
// ========================================

public override bool OutOfCombatTick()
{
    // Maintain Devotion Aura when out of combat
    if (!HasBuff("Devotion Aura") && CanCastSpell("Devotion Aura"))
    {
        Log("Casting Devotion Aura (out of combat)");
        return CastPersonal("Devotion Aura");
    }
    
    return false;
}

