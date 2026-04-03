// ========================================
// PALADIN HOLY - HEAL PRIORITY
// ========================================

private bool RunHealGambits()
{
    // Healthstone if player under threshold (combat only)
    if (IsInCombat() && UnitUnder("player", GetSlider("Healthstone HP %")) && HasHealthstone() && Inferno.ItemCooldown(HEALTHSTONE_ID) == 0)
    { Log("Using Healthstone (player " + HealthPct("player") + "%)"); Inferno.Cast("use_healthstone", QuickDelay: true); return true; }

    // Divine Protection if player under 75% (combat only)
    if (IsInCombat() && UnitUnder("player", 75) && Inferno.CanCast("Divine Protection"))
    { Log("Casting Divine Protection (player " + HealthPct("player") + "%)"); return CastPersonal("Divine Protection"); }

    // Avenging Wrath if 2+ under 60% (combat only)
    if (IsInCombat() && GroupMembersUnder(60, 2) && Inferno.CanCast("Avenging Wrath"))
    { Log("Casting Avenging Wrath"); return CastPersonal("Avenging Wrath"); }

    // Divine Toll if 2+ under 80% and HolyPower < 3 (combat only)
    if (IsInCombat() && GroupMembersUnder(80, 2) && PowerLessThan(3, HOLY_POWER))
    { string t = LowestAllyInRange("Divine Toll"); if (t != null) { Log("Casting Divine Toll on " + t + " (" + HealthPct(t) + "%)"); return CastOnFocus(t, "cast_dt"); } }

    // Light of Dawn if 5+ under 95% and HP >= 4 (combat only, togglable)
    if (IsSettingOn("Use Light of Dawn") && IsInCombat() && GroupMembersUnder(95, 5) && PowerAtLeast(4, HOLY_POWER) && Inferno.CanCast("Light of Dawn"))
    { Log("Casting Light of Dawn"); return CastPersonal("Light of Dawn"); }

    // Word of Glory if lowest under 90% and HP >= 3
    if (IsInCombat() && PowerAtLeast(3, HOLY_POWER))
    { string t = LowestAllyUnder(90, "Word of Glory"); if (t != null) { Log("Casting Word of Glory on " + t + " (" + HealthPct(t) + "%)"); return CastOnFocus(t, "cast_wog"); } }

    // Holy Shock on lowest injured ally (manual charge tracking - API bug)
    if (IsInCombat() && HsChargesAvailable() > 0)
    { string t = LowestAllyUnder(95, "Holy Shock"); if (t != null) { Log("Casting Holy Shock on " + t + " (" + HealthPct(t) + "%) [charges=" + HsChargesAvailable() + "]"); UseHsCharge(); return CastOnFocus(t, "cast_hs"); } }

    // Holy Light if lowest under 60%
    if (IsInCombat() && CanCastWhileMoving("Holy Light"))
    { string t = LowestAllyUnder(60, "Holy Light"); if (t != null) { Log("Casting Holy Light on " + t + " (" + HealthPct(t) + "%)"); return CastOnFocus(t, "cast_hl"); } }

    // Flash of Light if lowest under 95%
    if (IsInCombat() && CanCastWhileMoving("Flash of Light"))
    { string t = LowestAllyUnder(95, "Flash of Light"); if (t != null) { Log("Casting Flash of Light on " + t + " (" + HealthPct(t) + "%)"); return CastOnFocus(t, "cast_fol"); } }

    return false;
}

