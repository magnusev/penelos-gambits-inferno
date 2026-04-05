// ========================================
// MAP IDS
// ========================================
// Dungeon and zone identifiers used for mechanics

// Eversong Woods
private const int MAP_EVERSONG_WOODS = 2395;
private const string MAP_EVERSONG_WOODS_NAME = "Eversong Woods";

// Silvermoon City
private const int MAP_SILVERMOON_CITY = 2393;
private const string MAP_SILVERMOON_CITY_NAME = "Silvermoon City";

// Quel'Thalas
private const int MAP_QUELTHALAS = 2537;
private const string MAP_QUELTHALAS_NAME = "Quel'Thalas";

// Isle of Quel'Thalas
private const int MAP_ISLE_OF_QUELTHALAS = 2424;
private const string MAP_ISLE_OF_QUELTHALAS_NAME = "Isle of Quel'Thalas";

// Voidstorm
private const int MAP_VOIDSTORM = 2405;
private const string MAP_VOIDSTORM_NAME = "Voidstorm";

// The Den
private const int MAP_THE_DEN = 2576;
private const string MAP_THE_DEN_NAME = "The Den";

// Millenia's Threshold
private const int MAP_MILLENIAS_THRESHOLD = 2266;
private const string MAP_MILLENIAS_THRESHOLD_NAME = "Millenia's Threshold";

// Eredath
private const int MAP_EREDATH = 882;
private const string MAP_EREDATH_NAME = "Eredath";

// Arcantina
private const int MAP_ARCANTINA = 2541;
private const string MAP_ARCANTINA_NAME = "Arcantina";

// Twin Peaks (Battleground)
private const int MAP_TWIN_PEAKS = 206;
private const string MAP_TWIN_PEAKS_NAME = "Twin Peaks (BG)";

// Deephaul Ravine (Battleground)
private const int MAP_DEEPHAUL_RAVINE = 2345;
private const string MAP_DEEPHAUL_RAVINE_NAME = "Deephaul Ravine (BG)";

// Alterac Valley (Battleground)
private const int MAP_ALTERAC_VALLEY = 91;
private const string MAP_ALTERAC_VALLEY_NAME = "Alterac Valley (BG)";

// Orgrimmar
private const int MAP_ORGRIMMAR = 85;
private const string MAP_ORGRIMMAR_NAME = "Orgrimmar";

// Dornogal
private const int MAP_DORNOGAL = 2339;
private const string MAP_DORNOGAL_NAME = "Dornogal";

// Proving Grounds
private const int MAP_PROVING_GROUNDS = 480;
private const string MAP_PROVING_GROUNDS_NAME = "Proving Grounds";

// Magister's Terrace
private const int MAP_MAGISTERS_TERRACE_1 = 2511;
private const int MAP_MAGISTERS_TERRACE_2 = 2515;
private const int MAP_MAGISTERS_TERRACE_3 = 2516;
private const int MAP_MAGISTERS_TERRACE_4 = 2517;
private const int MAP_MAGISTERS_TERRACE_5 = 2518;
private const int MAP_MAGISTERS_TERRACE_6 = 2519;
private const int MAP_MAGISTERS_TERRACE_7 = 2520;
private const string MAP_MAGISTERS_TERRACE_NAME = "Magister's Terrace";

// Skyreach
private const int MAP_SKYREACH_1 = 601;
private const int MAP_SKYREACH_2 = 602;
private const string MAP_SKYREACH_NAME = "Skyreach";

// Pit of Saron
private const int MAP_PIT_OF_SARON = 823;
private const string MAP_PIT_OF_SARON_NAME = "Pit of Saron";

// Windrunner Spire
private const int MAP_WINDRUNNER_SPIRE_1 = 2492;
private const int MAP_WINDRUNNER_SPIRE_2 = 2493;
private const int MAP_WINDRUNNER_SPIRE_3 = 2494;
private const int MAP_WINDRUNNER_SPIRE_4 = 2496;
private const int MAP_WINDRUNNER_SPIRE_5 = 2497;
private const int MAP_WINDRUNNER_SPIRE_6 = 2498;
private const int MAP_WINDRUNNER_SPIRE_7 = 2499;
private const string MAP_WINDRUNNER_SPIRE_NAME = "Windrunner Spire";

// Maisara Caverns
private const int MAP_MAISARA_CAVERNS = 2501;
private const string MAP_MAISARA_CAVERNS_NAME = "Maisara Caverns";

// Algeth'ar Academy
private const int MAP_ALGETHAR_ACADEMY_1 = 2097;
private const int MAP_ALGETHAR_ACADEMY_2 = 2098;
private const int MAP_ALGETHAR_ACADEMY_3 = 2099;
private const string MAP_ALGETHAR_ACADEMY_NAME = "Algeth'ar Academy";

// Map ID to Name lookup dictionary
private Dictionary<int, string> _mapNames = new Dictionary<int, string>
{
    // New zones
    { MAP_EVERSONG_WOODS, MAP_EVERSONG_WOODS_NAME },
    { MAP_SILVERMOON_CITY, MAP_SILVERMOON_CITY_NAME },
    { MAP_QUELTHALAS, MAP_QUELTHALAS_NAME },
    { MAP_ISLE_OF_QUELTHALAS, MAP_ISLE_OF_QUELTHALAS_NAME },
    { MAP_VOIDSTORM, MAP_VOIDSTORM_NAME },
    { MAP_THE_DEN, MAP_THE_DEN_NAME },
    { MAP_MILLENIAS_THRESHOLD, MAP_MILLENIAS_THRESHOLD_NAME },
    { MAP_EREDATH, MAP_EREDATH_NAME },
    { MAP_ARCANTINA, MAP_ARCANTINA_NAME },
    { MAP_TWIN_PEAKS, MAP_TWIN_PEAKS_NAME },
    { MAP_DEEPHAUL_RAVINE, MAP_DEEPHAUL_RAVINE_NAME },
    { MAP_ALTERAC_VALLEY, MAP_ALTERAC_VALLEY_NAME },
    { MAP_ORGRIMMAR, MAP_ORGRIMMAR_NAME },
    { MAP_DORNOGAL, MAP_DORNOGAL_NAME },
    
    // Proving Grounds
    { MAP_PROVING_GROUNDS, MAP_PROVING_GROUNDS_NAME },
    
    // Magister's Terrace
    { MAP_MAGISTERS_TERRACE_1, MAP_MAGISTERS_TERRACE_NAME },
    { MAP_MAGISTERS_TERRACE_2, MAP_MAGISTERS_TERRACE_NAME },
    { MAP_MAGISTERS_TERRACE_3, MAP_MAGISTERS_TERRACE_NAME },
    { MAP_MAGISTERS_TERRACE_4, MAP_MAGISTERS_TERRACE_NAME },
    { MAP_MAGISTERS_TERRACE_5, MAP_MAGISTERS_TERRACE_NAME },
    { MAP_MAGISTERS_TERRACE_6, MAP_MAGISTERS_TERRACE_NAME },
    { MAP_MAGISTERS_TERRACE_7, MAP_MAGISTERS_TERRACE_NAME },
    
    // Skyreach
    { MAP_SKYREACH_1, MAP_SKYREACH_NAME },
    { MAP_SKYREACH_2, MAP_SKYREACH_NAME },
    
    // Pit of Saron
    { MAP_PIT_OF_SARON, MAP_PIT_OF_SARON_NAME },
    
    // Windrunner Spire
    { MAP_WINDRUNNER_SPIRE_1, MAP_WINDRUNNER_SPIRE_NAME },
    { MAP_WINDRUNNER_SPIRE_2, MAP_WINDRUNNER_SPIRE_NAME },
    { MAP_WINDRUNNER_SPIRE_3, MAP_WINDRUNNER_SPIRE_NAME },
    { MAP_WINDRUNNER_SPIRE_4, MAP_WINDRUNNER_SPIRE_NAME },
    { MAP_WINDRUNNER_SPIRE_5, MAP_WINDRUNNER_SPIRE_NAME },
    { MAP_WINDRUNNER_SPIRE_6, MAP_WINDRUNNER_SPIRE_NAME },
    { MAP_WINDRUNNER_SPIRE_7, MAP_WINDRUNNER_SPIRE_NAME },
    
    // Maisara Caverns
    { MAP_MAISARA_CAVERNS, MAP_MAISARA_CAVERNS_NAME },
    
    // Algeth'ar Academy
    { MAP_ALGETHAR_ACADEMY_1, MAP_ALGETHAR_ACADEMY_NAME },
    { MAP_ALGETHAR_ACADEMY_2, MAP_ALGETHAR_ACADEMY_NAME },
    { MAP_ALGETHAR_ACADEMY_3, MAP_ALGETHAR_ACADEMY_NAME }
};


