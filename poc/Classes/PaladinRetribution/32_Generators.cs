// ========================================
// PALADIN RETRIBUTION - GENERATORS
// ========================================

private bool RunGenerators()
{
    int holyPower = PowerCurrent(HOLY_POWER);
    
    // Call finishers if at 5 HP and Wake of Ashes is on cooldown, or Hammer of Light expiring soon
    if ((holyPower >= 5 && SpellCooldown("Wake of Ashes") > 0) || BuffRemaining("Hammer of Light") < GCDMAX() * 2)
    {
        if (RunFinishers()) return true;
    }
    
    // Blade of Justice - if Holy Flames talented and Expurgation not ticking early in combat
    if (IsTalentKnown("Holy Flames") && DebuffRemaining("Expurgation") < GCD() && CombatTime() < 5000)
    {
        if (CastOffensive("Blade of Justice")) return true;
    }
    
    // Judgment - if Light's Guidance talented and Judgment debuff not up early in combat
    if (IsTalentKnown("Light's Guidance") && DebuffRemaining("Judgment") < GCD() && CombatTime() < 5000)
    {
        if (CastOffensive("Judgment")) return true;
    }
    
    // Wake of Ashes - if Avenging Wrath on cooldown > 6s or Radiant Glory talented
    if (IsSettingOn("Use Wake of Ashes"))
    {
        // Hold Wake if NoCDs + Radiant Glory + Divine Toll (Wake procs AW via Radiant Glory)
        bool holdForNoCDs = IsCustomCommandOn("NoCDs") && IsTalentKnown("Radiant Glory") && IsTalentKnown("Divine Toll");
        
        if (!holdForNoCDs && (SpellCooldown("Avenging Wrath") > 6000 || IsTalentKnown("Radiant Glory")))
        {
            if (CastOffensive("Wake of Ashes")) return true;
        }
    }
    
    // Divine Toll - hold if NoCDs + Radiant Glory
    if (!(IsCustomCommandOn("NoCDs") && IsTalentKnown("Radiant Glory")))
    {
        if (CastOffensive("Divine Toll")) return true;
    }
    
    // Blade of Justice - if Art of War or Righteous Cause proc (but not during Walk Into Light + AW)
    if ((HasBuff("Art of War") || HasBuff("Righteous Cause")) 
        && (!IsTalentKnown("Walk Into Light") || !HasBuff("Avenging Wrath")))
    {
        if (CastOffensive("Blade of Justice")) return true;
    }
    
    // Finishers at 3+ Holy Power
    if (holyPower >= 3)
    {
        if (RunFinishers()) return true;
    }
    
    // Hammer of Wrath - if Walk Into Light talented (priority)
    if (IsTalentKnown("Walk Into Light"))
    {
        if (CastOffensive("Hammer of Wrath")) return true;
    }
    
    // Blade of Justice
    if (CastOffensive("Blade of Justice")) return true;
    
    // Hammer of Wrath
    if (CastOffensive("Hammer of Wrath")) return true;
    
    // Judgment
    if (CastOffensive("Judgment")) return true;
    
    // Templar Strike
    if (CastOffensive("Templar Strike")) return true;
    
    // Templar Slash
    if (CastOffensive("Templar Slash")) return true;
    
    // Crusader Strike (if Crusading Strikes not talented)
    if (!IsTalentKnown("Crusading Strikes"))
    {
        if (CastOffensive("Crusader Strike")) return true;
    }
    
    return false;
}

