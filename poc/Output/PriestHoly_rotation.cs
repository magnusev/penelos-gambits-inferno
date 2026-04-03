using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.IO;
using InfernoWow.API;

namespace InfernoWow.Modules
{

public class HolyPriestPvE : Rotation
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
    if (!Inferno.IsMoving("player"))
        return true;
    if (spell == "Flash of Light" && Inferno.HasBuff("Infusion of Light", "player", true))
        return true;
    if (spell == "Holy Light" && Inferno.HasBuff("Hand of Divinity", "player", true))
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

public override void LoadSettings()
{
    Settings.Add(new Setting("Enable Logging", true));
    Settings.Add(new Setting("Healthstone HP %", 1, 100, 50));
}
public override void Initialize()
{
    Spellbook.Add("Apotheosis");
    Spellbook.Add("Flash Heal");
    Spellbook.Add("Halo");
    Spellbook.Add("Holy Fire");
    Spellbook.Add("Holy Word: Chastise");
    Spellbook.Add("Holy Word: Serenity");
    Spellbook.Add("Prayer of Mending");
    Spellbook.Add("Purify");
    Spellbook.Add("Smite");
    Macros.Add("cast_pom", "/cast [@focus] Prayer of Mending");
    Macros.Add("cast_purify", "/cast [@focus] Purify");
    Macros.Add("cast_flash_heal", "/cast [@focus] Flash Heal");
    Macros.Add("cast_serenity", "/cast [@focus] Holy Word: Serenity");
    InitializeSharedComponents();
    _logFile = "penelos_priest_holy_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log";
    Inferno.PrintMessage("Penelos Gambits - Holy Priest loaded!", Color.Green);
    Log("Initialize complete");
}



private bool RunHealGambits()
{
    if (IsInCombat() && UnitUnder("player", GetSlider("Healthstone HP %")) && HasHealthstone() && Inferno.ItemCooldown(HEALTHSTONE_ID) == 0)
    { 
        Log("Using Healthstone (player " + HealthPct("player") + "%)"); 
        Inferno.Cast(MACRO_USE_HEALTHSTONE, QuickDelay: true); 
        return true; 
    }
    if (IsInCombat() && Inferno.SpellCharges("Holy Word: Serenity") == 0 && GroupMembersUnder(75, 1) && Inferno.CanCast("Apotheosis"))
    { 
        Log("Casting Apotheosis (0 Serenity charges, ally under 75%)"); 
        return CastPersonal("Apotheosis"); 
    }
    if (IsInCombat() && Inferno.SpellCharges("Holy Word: Serenity") >= 2)
    { 
        string target = LowestAllyInRange("Holy Word: Serenity"); 
        if (target != null) 
        { 
            Log("Casting Holy Word: Serenity on " + target + " (" + HealthPct(target) + "%) [2 charges]"); 
            return CastOnFocus(target, "cast_serenity"); 
        } 
    }
    if (IsInCombat() && !Inferno.IsMoving("player") && GroupMembersUnder(90, 2) && Inferno.CanCast("Halo"))
    { 
        Log("Casting Halo (2+ members under 90%)"); 
        return CastPersonal("Halo"); 
    }
    if (IsInCombat() && Inferno.HasBuff("Surge of Light", "player", true))
    { 
        string target = LowestAllyUnder(90, "Flash Heal"); 
        if (target != null) 
        { 
            Log("Casting Flash Heal (Surge of Light) on " + target + " (" + HealthPct(target) + "%)"); 
            return CastOnFocus(target, "cast_flash_heal"); 
        } 
    }
    if (IsInCombat() && Inferno.SpellCharges("Holy Word: Serenity") >= 1)
    { 
        string target = LowestAllyUnder(80, "Holy Word: Serenity"); 
        if (target != null) 
        { 
            Log("Casting Holy Word: Serenity on " + target + " (" + HealthPct(target) + "%) [1 charge]"); 
            return CastOnFocus(target, "cast_serenity"); 
        } 
    }
    if (IsInCombat() && Inferno.CanCast("Prayer of Mending"))
    { 
        string target = LowestAllyInRange("Prayer of Mending"); 
        if (target != null) 
        { 
            Log("Casting Prayer of Mending on " + target + " (" + HealthPct(target) + "%)"); 
            return CastOnFocus(target, "cast_pom"); 
        } 
    }
    if (IsInCombat() && !Inferno.IsMoving("player"))
    { 
        string target = LowestAllyUnder(85, "Flash Heal"); 
        if (target != null) 
        { 
            Log("Casting Flash Heal on " + target + " (" + HealthPct(target) + "%)"); 
            return CastOnFocus(target, "cast_flash_heal"); 
        } 
    }
    return false;
}

private bool RunDmgGambits()
{
    if (IsInCombat() && !TargetIsEnemy()) 
    { 
        Inferno.Cast(MACRO_TARGET_ENEMY, true); 
        return true; 
    }
    if (IsInCombat() && TargetIsEnemy() && Inferno.CanCast("Holy Fire", "target"))
    {
        Log("Casting Holy Fire on target"); 
        return CastOnEnemy("Holy Fire");
    }
    if (IsInCombat() && TargetIsEnemy() && Inferno.CanCast("Holy Word: Chastise", "target"))
    {
        Log("Casting Holy Word: Chastise on target"); 
        return CastOnEnemy("Holy Word: Chastise");
    }
    if (IsInCombat() && TargetIsEnemy() && Inferno.CanCast("Smite", "target"))
    {
        Log("Casting Smite on target"); 
        return CastOnEnemy("Smite");
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
            if (TryDispel("Consuming Void")) return true;
            return TryDispel("Polymorph");
        case 601: 
        case 602:
            return false;
        case 823:
            return TryDispel("Cryoshards");
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
private bool TryDispel(string debuff)
{
    if (!IsSpellReady("Purify") || !AnyAllyHasDebuff(debuff)) return false;
    string target = GetAllyWithDebuff(debuff, "Purify");
    if (target == null) return false;
    CastOnFocus(target, "cast_purify");
    return true;
}
private bool TryDispelStacks(string debuff, int min)
{
    if (!IsSpellReady("Purify") || !AnyAllyHasDebuff(debuff, min)) return false;
    string target = GetAllyWithMostStacks(debuff, "Purify");
    if (target == null) return false;
    CastOnFocus(target, "cast_purify");
    return true;
}
}

}
