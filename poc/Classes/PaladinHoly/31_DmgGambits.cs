// ========================================
// PALADIN HOLY - DAMAGE PRIORITY
// ========================================

private bool RunDmgGambits()
{
    if (IsSettingOn("Do DPS") && IsInCombat() && !TargetIsEnemy()) 
    { 
        Inferno.Cast(MACRO_TARGET_ENEMY, true); 
        return true; 
    }

    if (IsSettingOn("Do DPS") && IsInCombat() && PowerAtLeast(4, HOLY_POWER) && EnemiesInMelee(1) && CanCastSpell("Shield of the Righteous"))
    { 
        Log("Casting Shield of the Righteous"); 
        return CastPersonal("Shield of the Righteous"); 
    }

    if (IsSettingOn("Do DPS") && IsInCombat() && TargetIsEnemy() && PowerLessThan(4, HOLY_POWER) && CanCast("Judgment", "target"))
    { 
        Log("Casting Judgment"); 
        return CastOnEnemy("Judgment"); 
    }

    // Flash of Light filler - always have something to do
    if (IsInCombat() && CanCastWhileMoving("Flash of Light"))
    {
        string target = LowestAllyInRange("Flash of Light");
        if (target != null) 
        { 
            Log("Filler FoL on " + target + " (" + HealthPct(target) + "%)"); 
            return CastOnFocus(target, "cast_fol"); 
        }
        Log("Filler FoL on player (fallback)"); 
        return CastOnFocus("player", "cast_fol");
    }

    return false;
}

