// ========================================
// PALADIN HOLY - DAMAGE PRIORITY
// ========================================

private bool RunDmgGambits()
{
    if (IsSettingOn("Do DPS") && IsInCombat() && !TargetIsEnemy()) 
    { Inferno.Cast("target_enemy", true); return true; }

    if (IsSettingOn("Do DPS") && IsInCombat() && PowerAtLeast(4, HOLY_POWER) && EnemiesInMelee(1) && Inferno.CanCast("Shield of the Righteous"))
    { Log("Casting Shield of the Righteous"); return CastPersonal("Shield of the Righteous"); }

    if (IsSettingOn("Do DPS") && IsInCombat() && TargetIsEnemy() && PowerLessThan(4, HOLY_POWER) && Inferno.CanCast("Judgment", "target"))
    { Log("Casting Judgment"); return CastOnEnemy("Judgment"); }

    // Flash of Light filler - always have something to do
    if (IsInCombat() && CanCastWhileMoving("Flash of Light"))
    {
        string t = LowestAllyInRange("Flash of Light");
        if (t != null) { Log("Filler FoL on " + t + " (" + HealthPct(t) + "%)"); return CastOnFocus(t, "cast_fol"); }
        Log("Filler FoL on player (fallback)"); return CastOnFocus("player", "cast_fol");
    }

    return false;
}

