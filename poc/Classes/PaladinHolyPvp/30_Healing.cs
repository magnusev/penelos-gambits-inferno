// ========================================
// PALADIN HOLY PVP - HEALING PRIORITY
// ========================================

private bool HandleHealing()
{
    // Word of Glory - on lowest ally under 80% (Holy Power heal)
    if (IsSettingOn("Auto Word of Glory < 80%") && PowerAtLeast(3, HOLY_POWER) && CanCastSpell("Word of Glory"))
    {
        string target = LowestArenaAllyUnder(80);
        if (!string.IsNullOrEmpty(target))
        {
            Log("Casting Word of Glory on " + target + " (" + GetUnitHealthPct(target) + "%)");
            return CastOnUnit("Word of Glory", target);
        }
    }
    
    // Flash of Light - instant with Infusion of Light proc on ally under 88%
    if (IsSettingOn("Auto Flash of Light on Infusion Proc < 88%") && HasBuff("Infusion of Light") && CanCastSpell("Flash of Light"))
    {
        string target = LowestArenaAllyUnder(88);
        if (!string.IsNullOrEmpty(target))
        {
            Log("Casting Flash of Light (Infusion) on " + target + " (" + GetUnitHealthPct(target) + "%)");
            return CastOnUnit("Flash of Light", target);
        }
    }
    
    // Holy Shock - primary heal/damage hybrid
    if (CanCastSpell("Holy Shock"))
    {
        Log("Casting Holy Shock");
        return CastPersonal("Holy Shock");
    }
    
    // Flash of Light - player under 70%
    if (HealthPct("player") < 70 && CanCastSpell("Flash of Light"))
    {
        Log("Casting Flash of Light on player");
        return CastPersonal("Flash of Light");
    }
    
    // Light of Dawn - AoE heal
    if (CanCastSpell("Light of Dawn"))
    {
        Log("Casting Light of Dawn");
        return CastPersonal("Light of Dawn");
    }
    
    return false;
}

