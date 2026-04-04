// ========================================
// PRIEST DISCIPLINE PVP - DAMAGE/ATONEMENT
// ========================================

private bool HandleDamage(bool inRift, bool inBurst)
{
    // Power Word: Radiance - only on Ultimate Radiance proc (manual shields otherwise)
    if (IsSettingOn("Auto Radiance on Proc Only") && HasBuff("Ultimate Radiance") && CanCastSpell("Power Word: Radiance"))
    {
        Log("Casting Power Word: Radiance (proc)");
        return CastPersonal("Power Word: Radiance");
    }
    
    // Flash Heal - instant with Surge of Light proc on ally under 88%
    if (IsSettingOn("Auto Flash Heal on Surge of Light < 88%") && HasBuff("Surge of Light") && CanCastSpell("Flash Heal"))
    {
        string target = LowestArenaAllyUnder(88);
        if (!string.IsNullOrEmpty(target))
        {
            Log("Casting Flash Heal (Surge) on " + target + " (" + GetUnitHealthPct(target) + "%)");
            return CastOnUnit("Flash Heal", target);
        }
    }
    
    // Shadow Word: Death - execute damage/insanity
    if (CastOffensive("Shadow Word: Death"))
        return true;
    
    // Mind Blast - insanity generator (Entropic Rift synergy)
    if (CastOffensive("Mind Blast"))
        return true;
    
    // Void Blast - during Entropic Rift
    if (inRift && CastOffensive("Void Blast"))
        return true;
    
    // Penance - damage/healing hybrid
    if (CastOffensive("Penance"))
        return true;
    
    // Purge the Wicked - if talented and not on target
    if (IsTalentKnown("Purge the Wicked") && DebuffRemaining("Purge the Wicked") < GCD())
    {
        if (CastOffensive("Purge the Wicked")) 
            return true;
    }
    
    // Shadow Word: Pain - if not on target
    if (DebuffRemaining("Shadow Word: Pain") < GCD())
    {
        if (CastOffensive("Shadow Word: Pain"))
            return true;
    }
    
    // Voidwraith - pet summon
    if (CastOffensive("Voidwraith"))
        return true;
    
    // Smite - filler to keep rotation cycling
    if (CastOffensive("Smite"))
        return true;
    
    return false;
}

