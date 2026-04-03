// ========================================
// PRIEST HOLY - DAMAGE PRIORITY
// ========================================

private bool RunDmgGambits()
{
    if (IsSettingOn("Do DPS") && IsInCombat() && !TargetIsEnemy()) 
    { 
        Inferno.Cast("target_enemy", true); 
        return true; 
    }

    // Mindgames (DPS + healing CD)
    if (IsSettingOn("Do DPS") && IsInCombat() && TargetIsEnemy() && Inferno.CanCast("Mindgames", "target"))
    { 
        Log("Casting Mindgames"); 
        return CastOnEnemy("Mindgames"); 
    }

    // Holy Fire (priority DPS spell)
    if (IsSettingOn("Do DPS") && IsInCombat() && TargetIsEnemy() && Inferno.CanCast("Holy Fire", "target"))
    { 
        Log("Casting Holy Fire"); 
        return CastOnEnemy("Holy Fire"); 
    }

    // Shadow Word: Death (execute)
    if (IsSettingOn("Do DPS") && IsInCombat() && TargetIsEnemy() && Inferno.CanCast("Shadow Word: Death", "target") && HealthPct("target") < 20)
    { 
        Log("Casting Shadow Word: Death"); 
        return CastOnEnemy("Shadow Word: Death"); 
    }

    // Maintain Shadow Word: Pain
    if (IsSettingOn("Do DPS") && IsInCombat() && TargetIsEnemy() && Inferno.CanCast("Shadow Word: Pain", "target") && !Inferno.HasDebuff("Shadow Word: Pain", "target", true))
    { 
        Log("Casting Shadow Word: Pain"); 
        return CastOnEnemy("Shadow Word: Pain"); 
    }

    // Halo (AoE damage + healing)
    if (IsSettingOn("Do DPS") && IsInCombat() && EnemiesInMelee(3) && Inferno.CanCast("Halo"))
    { 
        Log("Casting Halo"); 
        return CastPersonal("Halo"); 
    }

    // Divine Star (AoE damage + healing)
    if (IsSettingOn("Do DPS") && IsInCombat() && TargetIsEnemy() && Inferno.CanCast("Divine Star"))
    { 
        Log("Casting Divine Star"); 
        return CastPersonal("Divine Star"); 
    }

    // Smite filler - always have something to do
    if (IsInCombat() && TargetIsEnemy() && Inferno.CanCast("Smite", "target"))
    {
        Log("Filler Smite on target"); 
        return CastOnEnemy("Smite");
    }

    // Renew filler on lowest ally if nothing else to do
    if (IsInCombat())
    {
        string t = LowestAllyInRange("Renew");
        if (t != null && (!HasRenew(t) || RenewRemaining(t) < 3000)) 
        { 
            Log("Filler Renew on " + t + " (" + HealthPct(t) + "%)"); 
            return CastOnFocus(t, "cast_renew"); 
        }
    }

    return false;
}


