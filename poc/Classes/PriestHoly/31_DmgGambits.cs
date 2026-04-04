// ========================================
// PRIEST HOLY - DAMAGE PRIORITY
// ========================================

private bool RunDmgGambits()
{
    // Target an enemy if none is targeted
    if (IsInCombat() && !TargetIsEnemy()) 
    { 
        Inferno.Cast(MACRO_TARGET_ENEMY, true); 
        return true; 
    }

    // Holy Fire (highest priority damage spell)
    if (IsInCombat() && TargetIsEnemy() && CanCast("Holy Fire", "target"))
    {
        Log("Casting Holy Fire on target"); 
        return CastOnEnemy("Holy Fire");
    }

    // Holy Word: Chastise (second priority)
    if (IsInCombat() && TargetIsEnemy() && CanCast("Holy Word: Chastise", "target"))
    {
        Log("Casting Holy Word: Chastise on target"); 
        return CastOnEnemy("Holy Word: Chastise");
    }

    // Smite (filler)
    if (IsInCombat() && TargetIsEnemy() && CanCast("Smite", "target"))
    {
        Log("Casting Smite on target"); 
        return CastOnEnemy("Smite");
    }


    return false;
}


