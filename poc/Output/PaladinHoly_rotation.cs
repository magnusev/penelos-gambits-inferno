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
        for (int i = 0; i < groupMembers.Count; i++) 
            info += groupMembers[i] + "=" + HealthPct(groupMembers[i]) + "% ";
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

private const int HOLY_POWER = 9;
private int _hsCharges = 2;
private long _hsLastRechargeMs = 0;
private const int HS_MAX_CHARGES = 2;
private const int HS_RECHARGE_MS = 5000;
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
        case 480:
            return TryDispel("Aqua Bomb");
        case 2511: 
        case 2515: 
        case 2516: 
        case 2517: 
        case 2518: 
        case 2519: 
        case 2520:
            if (TryDispel("Ethereal Shackles")) return true;
            if (TryDispel("Consuming Void")) return true;
            if (TryBof("Ethereal Shackles")) return true;
            if (TryDispel("Holy Fire")) return true;
            return TryDispel("Polymorph");
        case 601: 
        case 602:
            return false;
        case 823:
            if (TryDispel("Cryoshards")) return true;
            return TryDispelStacks("Rotting Strikes", 3);
        case 2492: 
        case 2493: 
        case 2494: 
        case 2496: 
        case 2497: 
        case 2498: 
        case 2499:
            if (TryDispel("Poison Spray")) return true;
            if (TryDispel("Soul Torment")) return true;
            return TryDispel("Poison Blades");
        case 2501:
            return TryDispel("Infected Pinions");
        case 2097: 
        case 2098: 
        case 2099:
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
}

}
