// ========================================
// PALADIN HOLY - DUNGEON MECHANICS
// ========================================

private bool RunDungeonGambits(int mapId)
{
    if (!IsInCombat()) return false;
    
    switch (mapId)
    {
        case 480: // Proving Grounds
            return TryDispel("Aqua Bomb");
            
        case 2511: 
        case 2515: 
        case 2516: 
        case 2517: 
        case 2518: 
        case 2519: 
        case 2520: // Algeth'ar Academy
            if (TryDispel("Ethereal Shackles")) return true;
            if (TryDispel("Consuming Void")) return true;
            if (TryBof("Ethereal Shackles")) return true;
            if (TryDispel("Holy Fire")) return true;
            return TryDispel("Polymorph");
            
        case 601: 
        case 602: // Skyreach
            return false;
            
        case 823: // Pit of Saron
            if (TryDispel("Cryoshards")) return true;
            return TryDispelStacks("Rotting Strikes", 3);
            
        case 2492: 
        case 2493: 
        case 2494: 
        case 2496: 
        case 2497: 
        case 2498: 
        case 2499: // Maisara Caverns
            if (TryDispel("Poison Spray")) return true;
            if (TryDispel("Soul Torment")) return true;
            return TryDispel("Poison Blades");
            
        case 2501: // Windrunner Spire
            return TryDispel("Infected Pinions");
            
        case 2097: 
        case 2098: 
        case 2099: // Magister's Terrace
            if (IsSpellReady("Cleanse") && AnyAllyHasDebuff("Lasher Toxin", 2))
            { 
                string t = GetAllyWithMostStacks("Lasher Toxin", "Cleanse"); 
                if (t != null) 
                { 
                    CastOnFocus(t, "cast_cleanse"); 
                    return true; 
                } 
            }
            return false;
            
        default: 
            return false;
    }
}

private bool TryDispel(string debuff)
{
    if (!IsSpellReady("Cleanse") || !AnyAllyHasDebuff(debuff)) return false;
    string t = GetAllyWithDebuff(debuff, "Cleanse");
    if (t == null) return false;
    CastOnFocus(t, "cast_cleanse");
    return true;
}

private bool TryDispelStacks(string debuff, int min)
{
    if (!IsSpellReady("Cleanse") || !AnyAllyHasDebuff(debuff, min)) return false;
    string t = GetAllyWithMostStacks(debuff, "Cleanse");
    if (t == null) return false;
    CastOnFocus(t, "cast_cleanse");
    return true;
}

private bool TryBof(string debuff)
{
    if (!IsSpellReady("Blessing of Freedom") || !AnyAllyHasDebuff(debuff)) return false;
    string t = GetAllyWithDebuff(debuff, "Blessing of Freedom");
    if (t == null) return false;
    CastOnFocus(t, "cast_bof");
    return true;
}

