// ========================================
// PRIEST HOLY - DUNGEON MECHANICS
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
            if (TryDispel("Consuming Void")) return true;
            return TryDispel("Polymorph");
            
        case 601: 
        case 602: // Skyreach
            return false;
            
        case 823: // Pit of Saron
            return TryDispel("Cryoshards");
            
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
            if (IsSpellReady("Purify") && AnyAllyHasDebuff("Lasher Toxin", 2))
            { 
                string target = GetAllyWithMostStacks("Lasher Toxin", "Purify"); 
                if (target != null) 
                { 
                    CastOnFocus(target, "cast_purify"); 
                    return true; 
                } 
            }
            return false;
            
        default: 
            return false;
    }
}

// Returns true if successfully cast Purify on an ally with the debuff
private bool TryDispel(string debuff)
{
    if (!IsSpellReady("Purify") || !AnyAllyHasDebuff(debuff)) return false;
    string target = GetAllyWithDebuff(debuff, "Purify");
    if (target == null) return false;
    CastOnFocus(target, "cast_purify");
    return true;
}

// Returns true if successfully cast Purify on ally with most stacks
private bool TryDispelStacks(string debuff, int min)
{
    if (!IsSpellReady("Purify") || !AnyAllyHasDebuff(debuff, min)) return false;
    string target = GetAllyWithMostStacks(debuff, "Purify");
    if (target == null) return false;
    CastOnFocus(target, "cast_purify");
    return true;
}




