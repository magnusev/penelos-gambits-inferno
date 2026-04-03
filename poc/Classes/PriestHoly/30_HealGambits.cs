// ========================================
// PRIEST HOLY - HEAL PRIORITY
// ========================================

private bool RunHealGambits()
{
    // Healthstone if player under threshold (combat only)
    if (IsInCombat() && UnitUnder("player", GetSlider("Healthstone HP %")) && HasHealthstone() && Inferno.ItemCooldown(HEALTHSTONE_ID) == 0)
    { 
        Log("Using Healthstone (player " + HealthPct("player") + "%)"); 
        Inferno.Cast(MACRO_USE_HEALTHSTONE, QuickDelay: true); 
        return true; 
    }
    
    // Apotheosis if 0 Serenity charges and player under 75% (instant cast, can cast while moving)
    if (IsInCombat() && Inferno.SpellCharges("Holy Word: Serenity") == 0 && GroupMembersUnder(75, 1) && Inferno.CanCast("Apotheosis"))
    { 
        Log("Casting Apotheosis (0 Serenity charges, ally under 75%)"); 
        return CastPersonal("Apotheosis"); 
    }

    // Holy Word: Serenity with 2 charges - cast on lowest HP party member (instant cast)
    if (IsInCombat() && Inferno.SpellCharges("Holy Word: Serenity") >= 2)
    { 
        string target = LowestAllyInRange("Holy Word: Serenity"); 
        if (target != null) 
        { 
            Log("Casting Holy Word: Serenity on " + target + " (" + HealthPct(target) + "%) [2 charges]"); 
            return CastOnFocus(target, "cast_serenity"); 
        } 
    }
    
    // Halo if 2+ targets under 90% (cannot be cast while moving)
    if (IsInCombat() && !Inferno.IsMoving("player") && GroupMembersUnder(90, 2) && Inferno.CanCast("Halo"))
    { 
        Log("Casting Halo (2+ members under 90%)"); 
        return CastPersonal("Halo"); 
    }

    // Surge of Light buff - cast instant Flash Heal on target under 90%
    if (IsInCombat() && Inferno.HasBuff("Surge of Light", "player", true))
    { 
        string target = LowestAllyUnder(90, "Flash Heal"); 
        if (target != null) 
        { 
            Log("Casting Flash Heal (Surge of Light) on " + target + " (" + HealthPct(target) + "%)"); 
            return CastOnFocus(target, "cast_flash_heal"); 
        } 
    }

    // Holy Word: Serenity with 1 charge - cast on target under 90%
    if (IsInCombat() && Inferno.SpellCharges("Holy Word: Serenity") >= 1)
    { 
        string target = LowestAllyUnder(90, "Holy Word: Serenity"); 
        if (target != null) 
        { 
            Log("Casting Holy Word: Serenity on " + target + " (" + HealthPct(target) + "%) [1 charge]"); 
            return CastOnFocus(target, "cast_serenity"); 
        } 
    }

    // Prayer of Mending on lowest health player if off cooldown
    if (IsInCombat() && Inferno.CanCast("Prayer of Mending"))
    { 
        string target = LowestAllyInRange("Prayer of Mending"); 
        if (target != null) 
        { 
            Log("Casting Prayer of Mending on " + target + " (" + HealthPct(target) + "%)"); 
            return CastOnFocus(target, "cast_pom"); 
        } 
    }
    
    // Flash Heal on target under 85% (cannot be cast while moving)
    if (IsInCombat() && !Inferno.IsMoving("player"))
    { 
        string target = LowestAllyUnder(85, "Flash Heal"); 
        if (target != null) 
        { 
            Log("Casting Flash Heal on " + target + " (" + HealthPct(target) + "%)"); 
            return CastOnFocus(target, "cast_flash_heal"); 
        } 
    }
    
    return false;
}


