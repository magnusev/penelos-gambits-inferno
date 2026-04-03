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

    // Cast Smite on current target
    if (IsInCombat() && TargetIsEnemy() && Inferno.CanCast("Smite", "target"))
    {
        Log("Casting Smite on target"); 
        return CastOnEnemy("Smite");
    }


    return false;
}


