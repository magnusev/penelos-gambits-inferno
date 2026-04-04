// ========================================
// PALADIN RETRIBUTION - OUT OF COMBAT
// ========================================

public override bool OutOfCombatTick()
{
    // Maintain Retribution Aura when out of combat
    if (!HasBuff("Retribution Aura") && CanCastSpell("Retribution Aura"))
    {
        Log("Casting Retribution Aura (out of combat)");
        return CastPersonal("Retribution Aura");
    }
    
    return false;
}

