// ========================================
// PALADIN HOLY - DUNGEON MECHANICS
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
            if (TryDispel("Ethereal Shackles")) return true;
            if (TryDispel("Consuming Void")) return true;
            if (TryBof("Ethereal Shackles")) return true;
            if (TryDispel("Holy Fire")) return true;
            return TryDispel("Polymorph");
            
        case MAP_SKYREACH_1:
        case MAP_SKYREACH_2:
            return false;
            
        case MAP_PIT_OF_SARON:
            if (TryDispel("Cryoshards")) return true;
            return TryDispelStacks("Rotting Strikes", 3);
            
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
            if (IsSpellReady("Cleanse") && AnyAllyHasDebuff("Lasher Toxin", 2))
            { 
                string target = GetAllyWithMostStacks("Lasher Toxin", "Cleanse"); 
                if (target != null) 
                { 
                    CastOnFocus(target, "cast_cleanse"); 
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
    string target = GetAllyWithDebuff(debuff, "Cleanse");
    if (target == null) return false;
    CastOnFocus(target, "cast_cleanse");
    return true;
}

private bool TryDispelStacks(string debuff, int min)
{
    if (!IsSpellReady("Cleanse") || !AnyAllyHasDebuff(debuff, min)) return false;
    string target = GetAllyWithMostStacks(debuff, "Cleanse");
    if (target == null) return false;
    CastOnFocus(target, "cast_cleanse");
    return true;
}

private bool TryBof(string debuff)
{
    if (!IsSpellReady("Blessing of Freedom") || !AnyAllyHasDebuff(debuff)) return false;
    string target = GetAllyWithDebuff(debuff, "Blessing of Freedom");
    if (target == null) return false;
    CastOnFocus(target, "cast_bof");
    return true;
}

