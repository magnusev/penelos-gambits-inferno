// ========================================
// PRIEST HOLY - DUNGEON MECHANICS
// ========================================

private bool RunDungeonGambits(int mapId)
{
    if (!IsInCombat()) return false;
    
    switch (mapId)
    {
        case MAP_PROVING_GROUNDS:
            return TryDispel("Aqua Bomb");
            
        case MAP_ALGETHAR_ACADEMY_1:
        case MAP_ALGETHAR_ACADEMY_2:
        case MAP_ALGETHAR_ACADEMY_3:
        case MAP_ALGETHAR_ACADEMY_4:
        case MAP_ALGETHAR_ACADEMY_5:
        case MAP_ALGETHAR_ACADEMY_6:
        case MAP_ALGETHAR_ACADEMY_7:
            if (TryDispel("Consuming Void")) return true;
            return TryDispel("Polymorph");
            
        case MAP_SKYREACH_1:
        case MAP_SKYREACH_2:
            return false;
            
        case MAP_PIT_OF_SARON:
            return TryDispel("Cryoshards");
            
        case MAP_MAISARA_CAVERNS_1:
        case MAP_MAISARA_CAVERNS_2:
        case MAP_MAISARA_CAVERNS_3:
        case MAP_MAISARA_CAVERNS_4:
        case MAP_MAISARA_CAVERNS_5:
        case MAP_MAISARA_CAVERNS_6:
        case MAP_MAISARA_CAVERNS_7:
            if (TryDispel("Poison Spray")) return true;
            if (TryDispel("Soul Torment")) return true;
            return TryDispel("Poison Blades");
            
        case MAP_WINDRUNNER_SPIRE:
            return TryDispel("Infected Pinions");
            
        case MAP_MAGISTERS_TERRACE_1:
        case MAP_MAGISTERS_TERRACE_2:
        case MAP_MAGISTERS_TERRACE_3:
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




