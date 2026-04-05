using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.IO;
using InfernoWow.API;

namespace InfernoWow.Modules
{

public class HolyPaladinPvE : Rotation
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

private const int MAP_PROVING_GROUNDS = 480;
private const int MAP_ALGETHAR_ACADEMY_1 = 2511;
private const int MAP_ALGETHAR_ACADEMY_2 = 2515;
private const int MAP_ALGETHAR_ACADEMY_3 = 2516;
private const int MAP_ALGETHAR_ACADEMY_4 = 2517;
private const int MAP_ALGETHAR_ACADEMY_5 = 2518;
private const int MAP_ALGETHAR_ACADEMY_6 = 2519;
private const int MAP_ALGETHAR_ACADEMY_7 = 2520;
private const int MAP_SKYREACH_1 = 601;
private const int MAP_SKYREACH_2 = 602;
private const int MAP_PIT_OF_SARON = 823;
private const int MAP_MAISARA_CAVERNS_1 = 2492;
private const int MAP_MAISARA_CAVERNS_2 = 2493;
private const int MAP_MAISARA_CAVERNS_3 = 2494;
private const int MAP_MAISARA_CAVERNS_4 = 2496;
private const int MAP_MAISARA_CAVERNS_5 = 2497;
private const int MAP_MAISARA_CAVERNS_6 = 2498;
private const int MAP_MAISARA_CAVERNS_7 = 2499;
private const int MAP_WINDRUNNER_SPIRE = 2501;
private const int MAP_MAGISTERS_TERRACE_1 = 2097;
private const int MAP_MAGISTERS_TERRACE_2 = 2098;
private const int MAP_MAGISTERS_TERRACE_3 = 2099;

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

private const int HOLY_POWER = 9;
private int _hsCharges = 2;
private long _hsLastRechargeMs = 0;
private const int HS_MAX_CHARGES = 2;
private const int HS_RECHARGE_MS = 5000;
private Random _rng = new Random();
private const string INTERRUPT_SPELL = "";
public override void LoadSettings()
{
    Settings.Add(new Setting("Enable Logging", true));
    Settings.Add(new Setting("Use Light of Dawn", false));
    Settings.Add(new Setting("Do DPS", false));
    Settings.Add(new Setting("Healthstone HP %", 1, 100, 50));
}
public override void Initialize()
{
    Spellbook.Add("Avenging Wrath"); 
    Spellbook.Add("Blessing of Freedom");
    Spellbook.Add("Cleanse"); 
    Spellbook.Add("Divine Protection");
    Spellbook.Add("Divine Toll"); 
    Spellbook.Add("Flash of Light");
    Spellbook.Add("Holy Light"); 
    Spellbook.Add("Holy Shock");
    Spellbook.Add("Judgment"); 
    Spellbook.Add("Light of Dawn");
    Spellbook.Add("Shield of the Righteous"); 
    Spellbook.Add("Word of Glory");
    Macros.Add("cast_fol", "/cast [@focus] Flash of Light");
    Macros.Add("cast_hl", "/cast [@focus] Holy Light");
    Macros.Add("cast_hs", "/cast [@focus] Holy Shock");
    Macros.Add("cast_wog", "/cast [@focus] Word of Glory");
    Macros.Add("cast_dt", "/cast [@focus] Divine Toll");
    Macros.Add("cast_cleanse", "/cast [@focus] Cleanse");
    Macros.Add("cast_bof", "/cast [@focus] Blessing of Freedom");
    InitializeSharedComponents();
    _logFile = "penelos_paladin_holy_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log";
    Inferno.PrintMessage("Penelos Gambits - Holy Paladin loaded!", Color.Green);
    Log("Initialize complete");
}

private int HsChargesAvailable()
{
    if (_hsCharges < HS_MAX_CHARGES)
    {
        long elapsed = NowMs() - _hsLastRechargeMs;
        int recharged = (int)(elapsed / HS_RECHARGE_MS);
        if (recharged > 0) 
        { 
            _hsCharges = _hsCharges + recharged; 
            if (_hsCharges > HS_MAX_CHARGES) _hsCharges = HS_MAX_CHARGES; 
            _hsLastRechargeMs = _hsLastRechargeMs + recharged * HS_RECHARGE_MS; 
        }
    }
    return _hsCharges;
}
private void UseHsCharge() 
{ 
    HsChargesAvailable(); 
    _hsCharges = _hsCharges - 1; 
    if (_hsCharges < 0) _hsCharges = 0; 
    if (_hsCharges == HS_MAX_CHARGES - 1) _hsLastRechargeMs = NowMs(); 
}

public override bool CombatTick()
{
    if (Inferno.IsDead("player")) return true;
    if (Inferno.GCD() != 0) return true;
    if (ProcessQueue()) return true;
    if (ThrottleIsOpen("diag", DIAGNOSTIC_LOG_INTERVAL_MS))
    {
        ThrottleRestart("diag");
        List<string> groupMembers = GetGroupMembers();
        string info = "";
        int maxLog = groupMembers.Count > 5 ? 5 : groupMembers.Count;
        for (int i = 0; i < maxLog; i++)
            info += groupMembers[i] + "=" + HealthPct(groupMembers[i]) + "% ";
        if (groupMembers.Count > 5)
            info += "... (" + (groupMembers.Count - 5) + " more)";
        Log("Tick: combat=" + Inferno.InCombat("player") + " group=" + groupMembers.Count + " | " + info);
    }
    int mapId = Inferno.GetMapID();
    if (RunDungeonGambits(mapId)) return true;
    if (RunHealGambits()) return true;
    if (RunDmgGambits()) return true;
    return true;
}
public override bool OutOfCombatTick() 
{ 
    return CombatTick(); 
}
public override void OnStop() 
{ 
    Log("Rotation stopped"); 
}

private bool RunHealGambits()
{
    if (IsInCombat() && UnitUnder("player", GetSlider("Healthstone HP %")) && HasHealthstone() && IsItemReady(HEALTHSTONE_ID))
    { 
        Log("Using Healthstone (player " + HealthPct("player") + "%)"); 
        Inferno.Cast(MACRO_USE_HEALTHSTONE, QuickDelay: true); 
        return true; 
    }
    if (IsInCombat() && UnitUnder("player", 75) && CanCastSpell("Divine Protection"))
    { 
        Log("Casting Divine Protection (player " + HealthPct("player") + "%)"); 
        return CastPersonal("Divine Protection"); 
    }
    if (IsInCombat() && GroupMembersUnder(60, 2) && CanCastSpell("Avenging Wrath"))
    { 
        Log("Casting Avenging Wrath"); 
        return CastPersonal("Avenging Wrath"); 
    }
    if (IsInCombat() && GroupMembersUnder(80, 2) && PowerLessThan(3, HOLY_POWER))
    { 
        string target = LowestAllyInRange("Divine Toll"); 
        if (target != null) 
        { 
            Log("Casting Divine Toll on " + target + " (" + HealthPct(target) + "%)"); 
            return CastOnFocus(target, "cast_dt"); 
        } 
    }
    if (IsSettingOn("Use Light of Dawn") && IsInCombat() && GroupMembersUnder(95, 5) && PowerAtLeast(4, HOLY_POWER) && CanCastSpell("Light of Dawn"))
    { 
        Log("Casting Light of Dawn"); 
        return CastPersonal("Light of Dawn"); 
    }
    if (IsInCombat() && PowerAtLeast(3, HOLY_POWER))
    { 
        string target = LowestAllyUnder(90, "Word of Glory"); 
        if (target != null) 
        { 
            Log("Casting Word of Glory on " + target + " (" + HealthPct(target) + "%)"); 
            return CastOnFocus(target, "cast_wog"); 
        } 
    }
    if (IsInCombat() && HsChargesAvailable() > 0)
    { 
        string target = LowestAllyUnder(95, "Holy Shock"); 
        if (target != null) 
        { 
            Log("Casting Holy Shock on " + target + " (" + HealthPct(target) + "%) [charges=" + HsChargesAvailable() + "]"); 
            UseHsCharge(); 
            return CastOnFocus(target, "cast_hs"); 
        } 
    }
    if (IsInCombat() && CanCastWhileMoving("Holy Light") && PowerAtLeast(20000, MANA))
    { 
        string target = LowestAllyUnder(60, "Holy Light"); 
        if (target != null) 
        { 
            Log("Casting Holy Light on " + target + " (" + HealthPct(target) + "%)"); 
            return CastOnFocus(target, "cast_hl"); 
        } 
    }
    if (IsInCombat() && CanCastWhileMoving("Flash of Light"))
    { 
        string target = LowestAllyUnder(95, "Flash of Light"); 
        if (target != null) 
        { 
            Log("Casting Flash of Light on " + target + " (" + HealthPct(target) + "%)"); 
            return CastOnFocus(target, "cast_fol"); 
        } 
    }
    return false;
}

private bool RunDmgGambits()
{
    if (IsSettingOn("Do DPS") && IsInCombat() && !TargetIsEnemy()) 
    { 
        Inferno.Cast(MACRO_TARGET_ENEMY, true); 
        return true; 
    }
    if (IsSettingOn("Do DPS") && IsInCombat() && PowerAtLeast(4, HOLY_POWER) && EnemiesInMelee(1) && CanCastSpell("Shield of the Righteous"))
    { 
        Log("Casting Shield of the Righteous"); 
        return CastPersonal("Shield of the Righteous"); 
    }
    if (IsSettingOn("Do DPS") && IsInCombat() && TargetIsEnemy() && PowerLessThan(4, HOLY_POWER) && CanCast("Judgment", "target"))
    { 
        Log("Casting Judgment"); 
        return CastOnEnemy("Judgment"); 
    }
    if (IsInCombat() && CanCastWhileMoving("Flash of Light"))
    {
        string target = LowestAllyInRange("Flash of Light");
        if (target != null) 
        { 
            Log("Filler FoL on " + target + " (" + HealthPct(target) + "%)"); 
            return CastOnFocus(target, "cast_fol"); 
        }
        Log("Filler FoL on player (fallback)"); 
        return CastOnFocus("player", "cast_fol");
    }
    return false;
}

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
}

}
