// ========================================
// PRIEST SHADOW - MAIN ST ROTATION
// ========================================

private bool RunMainRotation(int enemies)
{
    int insanity = GetInsanity();
    int insanityDeficit = GetInsanityDeficit();
    int gcdMax = GCDMAX();
    int targetHp = TargetHealthPct();
    bool mindDevourer = HasBuff("Mind Devourer");
    bool entropicRift = HasBuff("Entropic Rift");
    bool hasMindDevourerTalent = IsTalentKnown("Mind Devourer");
    bool hasDevourMatter = IsTalentKnown("Devour Matter");
    bool hasInvokedNightmare = IsTalentKnown("Invoked Nightmare");
    bool hasVoidApparitions = IsTalentKnown("Void Apparitions");
    bool hasMaddeningTentacles = IsTalentKnown("Maddening Tentacles");
    bool hasInescapableTorment = IsTalentKnown("Inescapable Torment");
    int swmRemaining = DebuffRemaining("Shadow Word: Madness");
    int swmCost = 40;
    
    // Shadow Word: Death - force Devour Matter in execute
    if (hasDevourMatter && targetHp <= 20)
    {
        if (CastOffensive("Shadow Word: Death")) return true;
    }
    
    // Shadow Word: Madness - don't overcap insanity
    if (swmRemaining <= gcdMax || insanityDeficit <= 35 || mindDevourer || (entropicRift && swmCost > 0))
    {
        if (CastOffensive("Shadow Word: Madness")) return true;
    }
    
    // Void Volley
    if (CastOffensive("Void Volley")) return true;
    
    // Void Blast
    if (CastOffensive("Void Blast")) return true;
    
    // Tentacle Slam - refresh VT or prevent charge cap
    if (IsVTRefreshable() || FullRechargeTime("Tentacle Slam", 20000) <= GCDMAX() * 2)
    {
        if (CastOffensive("Tentacle Slam")) return true;
    }
    
    // Void Torrent - if DoTs are up
    if (HasDotsUp())
    {
        if (CastOffensive("Void Torrent")) return true;
    }
    
    // Shadow Word: Pain - with Invoked Nightmare talent
    if (hasInvokedNightmare && IsSWPRefreshable() && HasVTOnTarget())
    {
        if (CastOffensive("Shadow Word: Pain")) return true;
    }
    
    // Mind Blast - if no Mind Devourer proc (or talent not taken)
    if (!mindDevourer || !hasMindDevourerTalent)
    {
        if (CastOffensive("Mind Blast")) return true;
    }
    
    // Mind Flay: Insanity - proc usage
    if (HasBuff("Mind Flay: Insanity"))
    {
        if (CastOffensive("Mind Flay: Insanity")) return true;
    }
    
    // Tentacle Slam - Void Apparitions / Maddening Tentacles value
    if (hasVoidApparitions || hasMaddeningTentacles)
    {
        bool madOk = !hasMaddeningTentacles || (insanity + 6) >= swmCost || DebuffRemaining("Shadow Word: Madness") == 0;
        if (madOk)
        {
            if (CastOffensive("Tentacle Slam")) return true;
        }
    }
    
    // Vampiric Touch - if refreshable
    if (IsVTRefreshable())
    {
        if (CastOffensive("Vampiric Touch")) return true;
    }
    
    // Shadow Word: Death - with Inescapable Torment or in execute
    int executeThreshold = 20 + (IsTalentKnown("Deathspeaker") ? 15 : 0);
    bool petUp = IsPetActive();
    if ((petUp && hasInescapableTorment) || (targetHp < executeThreshold && IsTalentKnown("Shadowfiend") && IsTalentKnown("Idol of Y'Shaarj")))
    {
        if (CastOffensive("Shadow Word: Death")) return true;
    }
    
    // Mind Flay filler - don't recast if already channeling
    if (PlayerCastingName() != "Mind Flay")
    {
        if (CastOffensive("Mind Flay")) return true;
    }
    
    // Movement fallbacks
    if (CastOffensive("Tentacle Slam")) return true;
    if (targetHp < 20 && CastOffensive("Shadow Word: Death")) return true;
    if (CastOffensive("Shadow Word: Death")) return true;
    if (CastOffensive("Shadow Word: Pain")) return true;
    
    return false;
}

