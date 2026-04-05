using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.IO;
using InfernoWow.API;

namespace InfernoWow.Modules
{

public class HolyPaladinPvP : Rotation
{
private string _queuedAction = null;
private string _lastLoggedAction = null;
private Dictionary<string, long> _throttleTimestamps = new Dictionary<string, long>();
private string _logFile = null;
private bool CastOnFocus(string unit, string macro) 
{ 
    if (_queuedAction != null) return false;
    Inferno.Cast("focus_" + unit); 
    _queuedAction = macro; 
    return true; 
}
private bool CastPersonal(string spell) { Inferno.Cast(spell); return true; }
private bool CastOnEnemy(string spell) { Inferno.Cast(spell); return true; }
private bool ProcessQueue()
{
    if (_queuedAction == null) return false;
    string action = _queuedAction; 
    _queuedAction = null;
    Inferno.Cast(action, QuickDelay: true);
    return true;
}
private long NowMs() { return DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond; }
private bool ThrottleIsOpen(string key, int milliseconds) 
{ 
    if (!_throttleTimestamps.ContainsKey(key)) return true; 
    return (NowMs() - _throttleTimestamps[key]) >= milliseconds; 
}
private void ThrottleRestart(string key) { _throttleTimestamps[key] = NowMs(); }
private void Log(string msg)
{
    if (!GetCheckBox("Enable Logging")) return;
    if (msg == _lastLoggedAction && !msg.StartsWith("Tick:")) return;
    _lastLoggedAction = msg;
    Inferno.PrintMessage(msg, Color.White);
    if (_logFile != null) 
    { 
        try 
        { 
            File.AppendAllText(_logFile,
                DateTime.Now.ToString("HH:mm:ss.fff") + " " + msg + "\n");
        } 
        catch { } 
    }
}

private const int MAP_EVERSONG_WOODS = 2395;
private const string MAP_EVERSONG_WOODS_NAME = "Eversong Woods";
private const int MAP_SILVERMOON_CITY = 2393;
private const string MAP_SILVERMOON_CITY_NAME = "Silvermoon City";
private const int MAP_QUELTHALAS = 2537;
private const string MAP_QUELTHALAS_NAME = "Quel'Thalas";
private const int MAP_ISLE_OF_QUELTHALAS = 2424;
private const string MAP_ISLE_OF_QUELTHALAS_NAME = "Isle of Quel'Thalas";
private const int MAP_VOIDSTORM = 2405;
private const string MAP_VOIDSTORM_NAME = "Voidstorm";
private const int MAP_THE_DEN = 2576;
private const string MAP_THE_DEN_NAME = "The Den";
private const int MAP_MILLENIAS_THRESHOLD = 2266;
private const string MAP_MILLENIAS_THRESHOLD_NAME = "Millenia's Threshold";
private const int MAP_EREDATH = 882;
private const string MAP_EREDATH_NAME = "Eredath";
private const int MAP_ARCANTINA = 2541;
private const string MAP_ARCANTINA_NAME = "Arcantina";
private const int MAP_TWIN_PEAKS = 206;
private const string MAP_TWIN_PEAKS_NAME = "Twin Peaks (BG)";
private const int MAP_DEEPHAUL_RAVINE = 2345;
private const string MAP_DEEPHAUL_RAVINE_NAME = "Deephaul Ravine (BG)";
private const int MAP_ALTERAC_VALLEY = 91;
private const string MAP_ALTERAC_VALLEY_NAME = "Alterac Valley (BG)";
private const int MAP_ORGRIMMAR = 85;
private const string MAP_ORGRIMMAR_NAME = "Orgrimmar";
private const int MAP_DORNOGAL = 2339;
private const string MAP_DORNOGAL_NAME = "Dornogal";
private const int MAP_PROVING_GROUNDS = 480;
private const string MAP_PROVING_GROUNDS_NAME = "Proving Grounds";
private const int MAP_MAGISTERS_TERRACE_1 = 2511;
private const int MAP_MAGISTERS_TERRACE_2 = 2515;
private const int MAP_MAGISTERS_TERRACE_3 = 2516;
private const int MAP_MAGISTERS_TERRACE_4 = 2517;
private const int MAP_MAGISTERS_TERRACE_5 = 2518;
private const int MAP_MAGISTERS_TERRACE_6 = 2519;
private const int MAP_MAGISTERS_TERRACE_7 = 2520;
private const string MAP_MAGISTERS_TERRACE_NAME = "Magister's Terrace";
private const int MAP_SKYREACH_1 = 601;
private const int MAP_SKYREACH_2 = 602;
private const string MAP_SKYREACH_NAME = "Skyreach";
private const int MAP_PIT_OF_SARON = 823;
private const string MAP_PIT_OF_SARON_NAME = "Pit of Saron";
private const int MAP_WINDRUNNER_SPIRE_1 = 2492;
private const int MAP_WINDRUNNER_SPIRE_2 = 2493;
private const int MAP_WINDRUNNER_SPIRE_3 = 2494;
private const int MAP_WINDRUNNER_SPIRE_4 = 2496;
private const int MAP_WINDRUNNER_SPIRE_5 = 2497;
private const int MAP_WINDRUNNER_SPIRE_6 = 2498;
private const int MAP_WINDRUNNER_SPIRE_7 = 2499;
private const string MAP_WINDRUNNER_SPIRE_NAME = "Windrunner Spire";
private const int MAP_MAISARA_CAVERNS = 2501;
private const string MAP_MAISARA_CAVERNS_NAME = "Maisara Caverns";
private const int MAP_ALGETHAR_ACADEMY_1 = 2097;
private const int MAP_ALGETHAR_ACADEMY_2 = 2098;
private const int MAP_ALGETHAR_ACADEMY_3 = 2099;
private const string MAP_ALGETHAR_ACADEMY_NAME = "Algeth'ar Academy";
private Dictionary<int, string> _mapNames = new Dictionary<int, string>
{
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
    { MAP_PROVING_GROUNDS, MAP_PROVING_GROUNDS_NAME },
    { MAP_MAGISTERS_TERRACE_1, MAP_MAGISTERS_TERRACE_NAME },
    { MAP_MAGISTERS_TERRACE_2, MAP_MAGISTERS_TERRACE_NAME },
    { MAP_MAGISTERS_TERRACE_3, MAP_MAGISTERS_TERRACE_NAME },
    { MAP_MAGISTERS_TERRACE_4, MAP_MAGISTERS_TERRACE_NAME },
    { MAP_MAGISTERS_TERRACE_5, MAP_MAGISTERS_TERRACE_NAME },
    { MAP_MAGISTERS_TERRACE_6, MAP_MAGISTERS_TERRACE_NAME },
    { MAP_MAGISTERS_TERRACE_7, MAP_MAGISTERS_TERRACE_NAME },
    { MAP_SKYREACH_1, MAP_SKYREACH_NAME },
    { MAP_SKYREACH_2, MAP_SKYREACH_NAME },
    { MAP_PIT_OF_SARON, MAP_PIT_OF_SARON_NAME },
    { MAP_WINDRUNNER_SPIRE_1, MAP_WINDRUNNER_SPIRE_NAME },
    { MAP_WINDRUNNER_SPIRE_2, MAP_WINDRUNNER_SPIRE_NAME },
    { MAP_WINDRUNNER_SPIRE_3, MAP_WINDRUNNER_SPIRE_NAME },
    { MAP_WINDRUNNER_SPIRE_4, MAP_WINDRUNNER_SPIRE_NAME },
    { MAP_WINDRUNNER_SPIRE_5, MAP_WINDRUNNER_SPIRE_NAME },
    { MAP_WINDRUNNER_SPIRE_6, MAP_WINDRUNNER_SPIRE_NAME },
    { MAP_WINDRUNNER_SPIRE_7, MAP_WINDRUNNER_SPIRE_NAME },
    { MAP_MAISARA_CAVERNS, MAP_MAISARA_CAVERNS_NAME },
    { MAP_ALGETHAR_ACADEMY_1, MAP_ALGETHAR_ACADEMY_NAME },
    { MAP_ALGETHAR_ACADEMY_2, MAP_ALGETHAR_ACADEMY_NAME },
    { MAP_ALGETHAR_ACADEMY_3, MAP_ALGETHAR_ACADEMY_NAME }
};

private const int MANA = 0;
private const int HEALTHSTONE_ID = 5512;
private const int DIAGNOSTIC_LOG_INTERVAL_MS = 2000;
private const string MACRO_TARGET_ENEMY = "target_enemy";
private const string MACRO_USE_HEALTHSTONE = "use_healthstone";
private const string MACRO_FOCUS_PLAYER = "focus_player";
private const string MACRO_FOCUS_PREFIX_PARTY = "focus_party";
private const string MACRO_FOCUS_PREFIX_RAID = "focus_raid";

private void InitializeFocusMacros()
{
    Macros.Add("focus_player", "/focus player");
    for (int i = 1; i <= 4; i++)
    {
        Macros.Add("focus_party" + i, "/focus party" + i);
    }
    for (int i = 1; i <= 28; i++)
    {
        Macros.Add("focus_raid" + i, "/focus raid" + i);
    }
}
private void InitializeUtilityMacros()
{
    Macros.Add(MACRO_TARGET_ENEMY, "/targetenemy");
    Macros.Add(MACRO_USE_HEALTHSTONE, "/use Healthstone");
}
private void InitializeHealthstoneFunction()
{
    string hasHealthstoneCode = "return GetItemCount(" + HEALTHSTONE_ID + ") > 0 and 1 or 0";
    CustomFunctions.Add("HasHealthstone", hasHealthstoneCode);
}
private void InitializeSharedComponents()
{
    InitializeFocusMacros();
    InitializeUtilityMacros();
    InitializeHealthstoneFunction();
}

private bool IsInCombat()
{
    return Inferno.InCombat("player");
}
private bool IsSpellReady(string spellName)
{
    return Inferno.SpellCooldown(spellName) <= 200;
}
private bool CanCast(string spellName, string target = "target")
{
    return Inferno.CanCast(spellName, target);
}
private bool CanCastSpell(string spellName)
{
    return Inferno.CanCast(spellName);
}
private int SpellCharges(string spellName)
{
    return Inferno.SpellCharges(spellName);
}
private bool HasBuff(string buffName, string unit = "player")
{
    return Inferno.HasBuff(buffName, unit, true);
}
private bool IsMoving()
{
    return Inferno.IsMoving("player");
}
private bool IsItemReady(int itemId)
{
    return Inferno.ItemCooldown(itemId) == 0;
}
private bool IsSettingOn(string settingName)
{
    return GetCheckBox(settingName);
}
private bool HasHealthstone()
{
    return Inferno.CustomFunction("HasHealthstone") == 1;
}
private bool TargetIsEnemy()
{
    return Inferno.UnitCanAttack("player", "target");
}
private bool UnitUnder(string unit, int percent)
{
    return HealthPct(unit) < percent;
}
private bool EnemiesInMelee(int count)
{
    return Inferno.EnemiesNearUnit(8, "player") >= count;
}
private bool PowerAtLeast(int amount, int powerType)
{
    return Inferno.Power("player", powerType) >= amount;
}
private bool PowerLessThan(int amount, int powerType)
{
    return Inferno.Power("player", powerType) < amount;
}
private bool GroupMembersUnder(int percent, int minCount)
{
    return GetGroupMembers().Count(unit => !Inferno.IsDead(unit) && HealthPct(unit) < percent) >= minCount;
}
private bool AnyAllyHasDebuff(string debuff)
{
    return GetGroupMembers().Any(unit => !Inferno.IsDead(unit) && Inferno.HasDebuff(debuff, unit, false));
}
private bool AnyAllyHasDebuff(string debuff, int stacks)
{
    return GetGroupMembers().Any(unit => !Inferno.IsDead(unit) && Inferno.HasDebuff(debuff, unit, false) && Inferno.DebuffStacks(debuff, unit, false) >= stacks);
}
private bool CanCastWhileMoving(string spell)
{
    if (!IsMoving())
        return true;
    if (spell == "Flash of Light" && HasBuff("Infusion of Light"))
        return true;
    if (spell == "Holy Light" && HasBuff("Hand of Divinity"))
        return true;
    return false;
}
private bool TargetIsCasting()
{
    return Inferno.CastingID("target") != 0 && Inferno.IsInterruptable("target");
}
private int CastingElapsed()
{
    return Inferno.CastingElapsed("target");
}
private int CastingRemaining()
{
    return Inferno.CastingRemaining("target");
}
private int TargetCastingID()
{
    return Inferno.CastingID("target");
}
private bool IsChanneling()
{
    return Inferno.IsChanneling("player");
}
private string PlayerCastingName()
{
    return Inferno.CastingName("player");
}
private bool CanUseTrinket(int slot)
{
    return Inferno.CanUseEquippedItem(slot);
}
private bool IsCustomCommandOn(string command)
{
    return Inferno.IsCustomCodeOn(command);
}
private bool IsTalentKnown(string talentName)
{
    return Inferno.IsSpellKnown(talentName);
}
private int EnemiesNearPlayer()
{
    return Inferno.EnemiesNearUnit(8f, "player");
}
private bool UnitCastingAtPercent(string unit, int minPct, int maxPct = 100)
{
    if (Inferno.CastingID(unit) == 0 || !Inferno.IsInterruptable(unit))
        return false;
    int elapsed = Inferno.CastingElapsed(unit);
    int remaining = Inferno.CastingRemaining(unit);
    int total = elapsed + remaining;
    if (total <= 0) 
        return false;
    int castPct = (elapsed * 100) / total;
    return castPct >= minPct && castPct <= maxPct;
}

private List<string> GetGroupMembers()
{
    List<string> result = new List<string>();
    if (Inferno.InRaid()) 
    { 
        int size = Inferno.GroupSize(); 
        for (int i = 1; i <= size; i++) 
        { 
            string token = "raid" + i; 
            if (Inferno.UnitName(token) != "") result.Add(token); 
        } 
    }
    else if (Inferno.InParty()) 
    { 
        result.Add("player"); 
        int size = Inferno.GroupSize(); 
        for (int i = 1; i < size; i++) 
        { 
            string token = "party" + i; 
            if (Inferno.UnitName(token) != "") result.Add(token); 
        } 
    }
    else { result.Add("player"); }
    return result;
}
private string LowestAllyUnder(int percent, string spell)
{
    return GetGroupMembers()
        .Where(unit => !Inferno.IsDead(unit) && HealthPct(unit) < percent && Inferno.CanCast(spell, unit))
        .OrderBy(unit => HealthPct(unit))
        .FirstOrDefault();
}
private string LowestAllyInRange(string spell)
{
    return GetGroupMembers()
        .Where(unit => !Inferno.IsDead(unit) && Inferno.CanCast(spell, unit))
        .OrderBy(unit => HealthPct(unit))
        .FirstOrDefault();
}
private string GetAllyWithDebuff(string debuff, string spell)
{
    return GetGroupMembers()
        .Where(unit => !Inferno.IsDead(unit) && Inferno.HasDebuff(debuff, unit, false) && Inferno.SpellInRange(spell, unit))
        .FirstOrDefault();
}
private string GetAllyWithMostStacks(string debuff, string spell)
{
    return GetGroupMembers()
        .Where(unit => !Inferno.IsDead(unit) && Inferno.HasDebuff(debuff, unit, false) && Inferno.SpellInRange(spell, unit))
        .OrderByDescending(unit => Inferno.DebuffStacks(debuff, unit, false))
        .FirstOrDefault();
}

private int HealthPct(string unit) 
{ 
    int maxHealth = Inferno.MaxHealth(unit); 
    if (maxHealth < 1) maxHealth = 1; 
    return (Inferno.Health(unit) * 100) / maxHealth; 
}
private int PowerCurrent(int powerType)
{
    return Inferno.Power("player", powerType);
}
private int PowerMax(int powerType)
{
    return Inferno.MaxPower("player", powerType);
}
private int TargetHealthPct()
{
    int maxHealth = Inferno.MaxHealth("target");
    if (maxHealth < 1) maxHealth = 1;
    return (Inferno.Health("target") * 100) / maxHealth;
}
private int FullRechargeTime(string spellName, int baseRecharge)
{
    return Inferno.FullRechargeTime(spellName, baseRecharge);
}
private int GCD()
{
    return Inferno.GCD();
}
private int GCDMAX()
{
    int gcd = (int)(1500f / (1f + Inferno.Haste("player") / 100f));
    return gcd < 750 ? 750 : gcd;
}
private int BuffRemaining(string buffName, string unit = "player")
{
    return Inferno.BuffRemaining(buffName, unit, true);
}
private int DebuffRemaining(string debuffName, string unit = "target")
{
    return Inferno.DebuffRemaining(debuffName, unit, true);
}
private int CombatTime()
{
    return Inferno.CombatTime();
}
private int SpellCooldown(string spellName)
{
    return Inferno.SpellCooldown(spellName);
}

private Dictionary<string, int> _interruptCastID = new Dictionary<string, int>();
private Dictionary<string, int> _interruptTargetPct = new Dictionary<string, int>();
private bool HandleInterrupt()
{
    return HandleInterruptOnUnit("target", INTERRUPT_SPELL);
}
private bool HandleInterruptOnUnit(string unit, string spell)
{
    try 
    {
        if (!IsSettingOn("Auto Interrupt")) return false;
    }
    catch 
    {
        return false;
    }
    int castingID = Inferno.CastingID(unit);
    if (!Inferno.IsInterruptable(unit) || castingID == 0)
    {
        if (_interruptCastID.ContainsKey(unit))
        {
            _interruptCastID.Remove(unit);
            _interruptTargetPct.Remove(unit);
        }
        return false;
    }
    if (!_interruptCastID.ContainsKey(unit) || _interruptCastID[unit] != castingID)
    {
        int minPct = GetSlider("Interrupt at cast % (min)");
        int maxPct = GetSlider("Interrupt at cast % (max)");
        if (maxPct < minPct) maxPct = minPct;
        int targetPct = _rng.Next(minPct, maxPct + 1);
        _interruptCastID[unit] = castingID;
        _interruptTargetPct[unit] = targetPct;
    }
    int interruptPct = _interruptTargetPct[unit];
    if (UnitCastingAtPercent(unit, interruptPct) && Inferno.CanCast(spell, IgnoreGCD: true))
    {
        int elapsed = Inferno.CastingElapsed(unit);
        int remaining = Inferno.CastingRemaining(unit);
        int total = elapsed + remaining;
        int castPct = total > 0 ? (elapsed * 100) / total : 0;
        Log("Interrupting " + unit + " at " + castPct + "% (target: " + interruptPct + "%)");
        Inferno.Cast(spell, QuickDelay: true);
        _interruptCastID.Remove(unit);
        _interruptTargetPct.Remove(unit);
        return true;
    }
    return false;
}
private bool ShouldInterruptCast(string unit, int minPct, int maxPct = 100)
{
    return UnitCastingAtPercent(unit, minPct, maxPct);
}

private bool HandleRacials()
{
    if (IsCustomCommandOn("NoCDs")) return false;
    if (Inferno.CanCast("Berserking", IgnoreGCD: true))
    {
        Log("Casting Berserking (racial)");
        Inferno.Cast("Berserking", QuickDelay: true);
        return true;
    }
    if (Inferno.CanCast("Blood Fury", IgnoreGCD: true))
    {
        Log("Casting Blood Fury (racial)");
        Inferno.Cast("Blood Fury", QuickDelay: true);
        return true;
    }
    if (Inferno.CanCast("Ancestral Call", IgnoreGCD: true))
    {
        Log("Casting Ancestral Call (racial)");
        Inferno.Cast("Ancestral Call", QuickDelay: true);
        return true;
    }
    if (Inferno.CanCast("Fireblood", IgnoreGCD: true))
    {
        Log("Casting Fireblood (racial)");
        Inferno.Cast("Fireblood", QuickDelay: true);
        return true;
    }
    if (CanCast("Lights Judgment", "target"))
    {
        Log("Casting Lights Judgment (racial)");
        return CastOnEnemy("Lights Judgment");
    }
    return false;
}

private string GetMapName(int mapId)
{
    string name;
    if (_mapNames.TryGetValue(mapId, out name))
        return name;
    return "UNKNOWN_MAP";
}
private void LogMapChange(int currentMapId)
{
    if (_lastMapId != currentMapId)
    {
        string oldMapName = GetMapName(_lastMapId);
        string newMapName = GetMapName(currentMapId);
        Log("Map changed: " + oldMapName + " (" + _lastMapId + ") -> " + newMapName + " (" + currentMapId + ")");
        _lastMapId = currentMapId;
    }
}
private void LogBossInformation()
{
    for (int i = 1; i <= 4; i++)
    {
        string boss = "boss" + i;
        int bossMaxHealth = Inferno.MaxHealth(boss);
        if (bossMaxHealth > 0)
        {
            int bossHealthPct = HealthPct(boss);
            string bossName = "Unknown";
            try
            {
                bossName = Inferno.UnitName(boss);
            }
            catch { }
            int castingId = Inferno.CastingID(boss);
            string logMsg = "Boss" + i + ": " + bossName + " Health: " + bossHealthPct + "%";
            if (castingId != 0)
            {
                string castName = "Unknown";
                try
                {
                    castName = Inferno.CastingName(boss);
                }
                catch { }
                bool interruptable = Inferno.IsInterruptable(boss);
                bool channeling = Inferno.IsChanneling(boss);
                int elapsed = Inferno.CastingElapsed(boss);
                int remaining = Inferno.CastingRemaining(boss);
                logMsg += " | CASTING: " + castName + " (ID:" + castingId + ")";
                logMsg += " Interruptable:" + interruptable;
                logMsg += " Channeling:" + channeling;
                logMsg += " Elapsed:" + elapsed + "ms";
                logMsg += " Remaining:" + remaining + "ms";
            }
            Log(logMsg);
        }
    }
}

private string LowestArenaAlly(int maxCount = 3)
{
    string lowestUnit = "";
    int lowestHp = 101;
    if (IsSettingOn("Protect Focus"))
    {
        int focusHp = GetUnitHealthPct("focus");
        if (focusHp > 0 && focusHp < lowestHp)
        {
            lowestHp = focusHp;
            lowestUnit = "focus";
        }
    }
    for (int i = 1; i <= maxCount; i++)
    {
        string unit = "party" + i;
        int hp = GetUnitHealthPct(unit);
        if (hp > 0 && hp < lowestHp)
        {
            lowestHp = hp;
            lowestUnit = unit;
        }
    }
    int selfHp = HealthPct("player");
    if (selfHp < lowestHp)
    {
        lowestHp = selfHp;
        lowestUnit = "player";
    }
    return lowestUnit;
}
private string LowestArenaAllyUnder(int threshold, int maxCount = 3)
{
    string lowestUnit = "";
    int lowestHp = 101;
    if (IsSettingOn("Protect Focus"))
    {
        int focusHp = GetUnitHealthPct("focus");
        if (focusHp > 0 && focusHp < threshold && focusHp < lowestHp)
        {
            lowestHp = focusHp;
            lowestUnit = "focus";
        }
    }
    for (int i = 1; i <= maxCount; i++)
    {
        string unit = "party" + i;
        int hp = GetUnitHealthPct(unit);
        if (hp > 0 && hp < threshold && hp < lowestHp)
        {
            lowestHp = hp;
            lowestUnit = unit;
        }
    }
    int selfHp = HealthPct("player");
    if (selfHp < threshold && selfHp < lowestHp)
    {
        lowestHp = selfHp;
        lowestUnit = "player";
    }
    return lowestUnit;
}
private int GetUnitHealthPct(string unit)
{
    int maxHealth = Inferno.MaxHealth(unit);
    if (maxHealth < 1) return 0;
    return (Inferno.Health(unit) * 100) / maxHealth;
}

private const int HOLY_POWER = 9;
private const string INTERRUPT_SPELL = "Hammer of Justice";
private Random _rng = new Random();
private int _lastMapId = 0;
public override void LoadSettings()
{
    Settings.Add(new Setting("=== Holy Paladin PVP Arena ==="));
    Settings.Add(new Setting("Enable Logging", true));
    Settings.Add(new Setting("Use Avenging Crusader", true));
    Settings.Add(new Setting("Use Divine Toll", true));
    Settings.Add(new Setting("Use Trinkets", true));
    Settings.Add(new Setting("=== Defensives ==="));
    Settings.Add(new Setting("Use Defensives", true));
    Settings.Add(new Setting("Auto Blessing of Protection < 30%", true));
    Settings.Add(new Setting("Auto Blessing of Sacrifice < 40%", true));
    Settings.Add(new Setting("Auto Lay on Hands < 25%", true));
    Settings.Add(new Setting("Auto Word of Glory < 80%", true));
    Settings.Add(new Setting("Auto Flash of Light on Infusion Proc < 88%", true));
    Settings.Add(new Setting("Protect Focus", true));
    Settings.Add(new Setting("Healthstone HP %", 1, 100, 45));
    Settings.Add(new Setting("=== Interrupt ==="));
    Settings.Add(new Setting("Auto Interrupt", true));
    Settings.Add(new Setting("Interrupt at cast % (min)", 0, 100, 40));
    Settings.Add(new Setting("Interrupt at cast % (max)", 0, 100, 85));
}
public override void Initialize()
{
    Spellbook.Add("Avenging Crusader");
    Spellbook.Add("Crusader Strike");
    Spellbook.Add("Divine Toll");
    Spellbook.Add("Flash of Light");
    Spellbook.Add("Holy Light");
    Spellbook.Add("Holy Prism");
    Spellbook.Add("Holy Shock");
    Spellbook.Add("Judgment");
    Spellbook.Add("Light of Dawn");
    Spellbook.Add("Word of Glory");
    Spellbook.Add("Beacon of Light");
    Spellbook.Add("Blessing of Freedom");
    Spellbook.Add("Blessing of Protection");
    Spellbook.Add("Blessing of Sacrifice");
    Spellbook.Add("Cleanse");
    Spellbook.Add("Divine Shield");
    Spellbook.Add("Hammer of Justice");
    Spellbook.Add("Lay on Hands");
    Spellbook.Add("Ancestral Call");
    Spellbook.Add("Berserking");
    Spellbook.Add("Blood Fury");
    Spellbook.Add("Fireblood");
    Spellbook.Add("Lights Judgment");
    Macros.Add("trinket1", "/use 13");
    Macros.Add("trinket2", "/use 14");
    InitializeSharedComponents();
    CustomCommands.Add("NoCDs");
    CustomCommands.Add("ForceST");
    Inferno.Latency = 185;
    _logFile = "penelos_paladin_holy_pvp_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log";
    Inferno.PrintMessage("Penelos Gambits - Holy Paladin PvP loaded!", Color.Gold);
    Log("Initialize complete");
}

private bool CastOnUnit(string spellName, string unit)
{
    if (CanCast(spellName, unit))
    {
        Log("Casting " + spellName + " on " + unit);
        Inferno.Cast(spellName, QuickDelay: true);
        return true;
    }
    return false;
}
private bool CastOffensive(string spellName)
{
    if (CanCast(spellName, "target"))
    {
        Log("Casting " + spellName);
        return CastOnEnemy(spellName);
    }
    return false;
}
private bool CastCooldown(string spellName)
{
    if (IsCustomCommandOn("NoCDs")) return false;
    if (CanCastSpell(spellName))
    {
        Log("Casting " + spellName + " (cooldown)");
        return CastPersonal(spellName);
    }
    return false;
}

private bool HandleDefensives()
{
    if (!IsSettingOn("Use Defensives")) return false;
    if (IsSettingOn("Auto Blessing of Sacrifice < 40%") && CanCastSpell("Blessing of Sacrifice"))
    {
        string target = LowestArenaAllyUnder(40);
        if (!string.IsNullOrEmpty(target))
        {
            Log("Casting Blessing of Sacrifice on " + target + " (" + GetUnitHealthPct(target) + "%)");
            Inferno.Cast("Blessing of Sacrifice", QuickDelay: true);
            return true;
        }
    }
    if (IsSettingOn("Auto Lay on Hands < 25%") && HealthPct("player") < 25 && Inferno.CanCast("Lay on Hands", IgnoreGCD: true))
    {
        Log("Casting Lay on Hands (emergency)");
        Inferno.Cast("Lay on Hands", QuickDelay: true);
        return true;
    }
    if (IsSettingOn("Auto Blessing of Protection < 30%") && HealthPct("player") < 30 && Inferno.CanCast("Blessing of Protection", IgnoreGCD: true))
    {
        Log("Casting Blessing of Protection (player " + HealthPct("player") + "%)");
        Inferno.Cast("Blessing of Protection", QuickDelay: true);
        return true;
    }
    if (HealthPct("player") <= GetSlider("Healthstone HP %") && HasHealthstone() && IsItemReady(HEALTHSTONE_ID))
    {
        Log("Using Healthstone (player " + HealthPct("player") + "%)");
        Inferno.Cast(MACRO_USE_HEALTHSTONE, QuickDelay: true);
        return true;
    }
    return false;
}

private bool HandleCooldowns(bool inCrusader)
{
    if (IsCustomCommandOn("NoCDs")) return false;
    if (IsSettingOn("Use Avenging Crusader") && TargetIsEnemy() && CastCooldown("Avenging Crusader"))
        return true;
    if (IsSettingOn("Use Divine Toll") && CastCooldown("Divine Toll"))
        return true;
    return false;
}
private bool HandleTrinkets()
{
    if (!IsSettingOn("Use Trinkets") || IsCustomCommandOn("NoCDs"))
        return false;
    if (!HasBuff("Avenging Crusader"))
        return false;
    if (CanUseTrinket(13))
    {
        Log("Using Trinket 1");
        Inferno.Cast("trinket1");
        return true;
    }
    if (CanUseTrinket(14))
    {
        Log("Using Trinket 2");
        Inferno.Cast("trinket2");
        return true;
    }
    return false;
}

public override bool CombatTick()
{
    if (Inferno.IsDead("player") || Inferno.IsGhost("player")) return false;
    int mapId = Inferno.GetMapID();
    LogMapChange(mapId);
    if (ThrottleIsOpen("boss_log", DIAGNOSTIC_LOG_INTERVAL_MS))
    {
        ThrottleRestart("boss_log");
        LogBossInformation();
    }
    if (HandleDefensives()) return true;
    if (HandleInterrupt()) return true;
    if (IsChanneling()) return false;
    if (HandleTrinkets()) return true;
    bool inCrusader = HasBuff("Avenging Crusader");
    if (HandleCooldowns(inCrusader)) return true;
    if (HandleHealing()) return true;
    if (HandleDamage()) return true;
    return false;
}
public override bool OutOfCombatTick()
{
    return false;
}
public override void CleanUp()
{
    int mapId = Inferno.GetMapID();
    LogMapChange(mapId);
}
public override void OnStop() 
{ 
    Log("Rotation stopped"); 
}

private bool HandleHealing()
{
    if (IsSettingOn("Auto Word of Glory < 80%") && PowerAtLeast(3, HOLY_POWER) && CanCastSpell("Word of Glory"))
    {
        string target = LowestArenaAllyUnder(80);
        if (!string.IsNullOrEmpty(target))
        {
            Log("Casting Word of Glory on " + target + " (" + GetUnitHealthPct(target) + "%)");
            return CastOnUnit("Word of Glory", target);
        }
    }
    if (IsSettingOn("Auto Flash of Light on Infusion Proc < 88%") && HasBuff("Infusion of Light") && CanCastSpell("Flash of Light"))
    {
        string target = LowestArenaAllyUnder(88);
        if (!string.IsNullOrEmpty(target))
        {
            Log("Casting Flash of Light (Infusion) on " + target + " (" + GetUnitHealthPct(target) + "%)");
            return CastOnUnit("Flash of Light", target);
        }
    }
    if (CanCastSpell("Holy Shock"))
    {
        Log("Casting Holy Shock");
        return CastPersonal("Holy Shock");
    }
    if (HealthPct("player") < 70 && CanCastSpell("Flash of Light"))
    {
        Log("Casting Flash of Light on player");
        return CastPersonal("Flash of Light");
    }
    if (CanCastSpell("Light of Dawn"))
    {
        Log("Casting Light of Dawn");
        return CastPersonal("Light of Dawn");
    }
    return false;
}

private bool HandleDamage()
{
    if (CastOffensive("Judgment"))
        return true;
    if (CastOffensive("Crusader Strike"))
        return true;
    return false;
}
}

}
