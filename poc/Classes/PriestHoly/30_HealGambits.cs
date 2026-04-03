// ========================================
// PRIEST HOLY - HEAL PRIORITY
// ========================================

private bool RunHealGambits()
{
    // Healthstone if player under threshold (combat only)
    if (IsInCombat() && UnitUnder("player", GetSlider("Healthstone HP %")) && HasHealthstone() && Inferno.ItemCooldown(HEALTHSTONE_ID) == 0)
    { 
        Log("Using Healthstone (player " + HealthPct("player") + "%)"); 
        Inferno.Cast("use_healthstone", QuickDelay: true); 
        return true; 
    }

    // Desperate Prayer if player under 60% (combat only)
    if (IsInCombat() && UnitUnder("player", 60) && Inferno.CanCast("Desperate Prayer"))
    { 
        Log("Casting Desperate Prayer (player " + HealthPct("player") + "%)"); 
        return CastPersonal("Desperate Prayer"); 
    }

    // Guardian Spirit if ally under 25% (combat only)
    if (IsInCombat())
    { 
        string t = LowestAllyUnder(25, "Guardian Spirit"); 
        if (t != null) 
        { 
            Log("Casting Guardian Spirit on " + t + " (" + HealthPct(t) + "%)"); 
            return CastOnFocus(t, "cast_gs"); 
        } 
    }

    // Power Word: Life if ally under 35% (combat only)
    if (IsInCombat())
    { 
        string t = LowestAllyUnder(35, "Power Word: Life"); 
        if (t != null) 
        { 
            Log("Casting Power Word: Life on " + t + " (" + HealthPct(t) + "%)"); 
            return CastOnFocus(t, "cast_pwl"); 
        } 
    }

    // Divine Hymn if 3+ under 50% (combat only)
    if (IsInCombat() && GroupMembersUnder(50, 3) && Inferno.CanCast("Divine Hymn"))
    { 
        Log("Casting Divine Hymn"); 
        return CastPersonal("Divine Hymn"); 
    }

    // Holy Word: Sanctify if 3+ under 80% (combat only)
    if (IsInCombat() && GroupMembersUnder(80, 3) && Inferno.CanCast("Holy Word: Sanctify"))
    { 
        Log("Casting Holy Word: Sanctify"); 
        return CastPersonal("Holy Word: Sanctify"); 
    }

    // Circle of Healing if 3+ under 85% (combat only, togglable)
    if (IsSettingOn("Use Circle of Healing") && IsInCombat() && GroupMembersUnder(85, 3) && Inferno.CanCast("Circle of Healing"))
    { 
        Log("Casting Circle of Healing"); 
        return CastPersonal("Circle of Healing"); 
    }

    // Prayer of Healing if 3+ under 90%
    if (IsInCombat() && GroupMembersUnder(90, 3) && CanCastWhileMoving("Prayer of Healing") && PowerAtLeast(25000, MANA))
    { 
        Log("Casting Prayer of Healing"); 
        return CastPersonal("Prayer of Healing"); 
    }

    // Holy Word: Serenity if lowest under 75%
    if (IsInCombat())
    { 
        string t = LowestAllyUnder(75, "Holy Word: Serenity"); 
        if (t != null) 
        { 
            Log("Casting Holy Word: Serenity on " + t + " (" + HealthPct(t) + "%)"); 
            return CastOnFocus(t, "cast_serenity"); 
        } 
    }

    // Flash Heal if lowest under 65%
    if (IsInCombat() && CanCastWhileMoving("Flash Heal"))
    { 
        string t = LowestAllyUnder(65, "Flash Heal"); 
        if (t != null) 
        { 
            Log("Casting Flash Heal on " + t + " (" + HealthPct(t) + "%)"); 
            return CastOnFocus(t, "cast_fh"); 
        } 
    }

    // Prayer of Mending (proactive bouncing heal, togglable)
    if (IsSettingOn("Use Prayer of Mending") && IsInCombat())
    { 
        string t = LowestAllyInRange("Prayer of Mending"); 
        if (t != null && !HasPrayerOfMending(t)) 
        { 
            Log("Casting Prayer of Mending on " + t); 
            return CastOnFocus(t, "cast_pom"); 
        } 
    }

    // Power Word: Shield if lowest under 90% and doesn't have shield
    if (IsInCombat())
    { 
        string t = LowestAllyUnder(90, "Power Word: Shield"); 
        if (t != null && !HasShield(t)) 
        { 
            Log("Casting Power Word: Shield on " + t + " (" + HealthPct(t) + "%)"); 
            return CastOnFocus(t, "cast_pws"); 
        } 
    }

    // Renew if lowest under 95% and doesn't have renew or renew expiring soon
    if (IsInCombat())
    { 
        string t = LowestAllyUnder(95, "Renew"); 
        if (t != null && (!HasRenew(t) || RenewRemaining(t) < 3000)) 
        { 
            Log("Casting Renew on " + t + " (" + HealthPct(t) + "%)"); 
            return CastOnFocus(t, "cast_renew"); 
        } 
    }

    // Heal if lowest under 80%
    if (IsInCombat() && CanCastWhileMoving("Heal") && PowerAtLeast(15000, MANA))
    { 
        string t = LowestAllyUnder(80, "Heal"); 
        if (t != null) 
        { 
            Log("Casting Heal on " + t + " (" + HealthPct(t) + "%)"); 
            return CastOnFocus(t, "cast_heal"); 
        } 
    }

    return false;
}


