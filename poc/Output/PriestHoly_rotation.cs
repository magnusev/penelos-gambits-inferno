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
private bool CastPersonal(string s) { Inferno.Cast(s); return true; }
private bool CastOnEnemy(string s) { Inferno.Cast(s); return true; }
private bool ProcessQueue()
{
    if (_queuedAction == null) return false;
    string a = _queuedAction; 
    _queuedAction = null;
    Inferno.Cast(a, QuickDelay: true);
    return true;
}
private long NowMs() { return DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond; }
private bool ThrottleIsOpen(string k, int ms) 
{ 
    if (!_throttleTimestamps.ContainsKey(k)) return true; 
    return (NowMs() - _throttleTimestamps[k]) >= ms; 
}
private void ThrottleRestart(string k) { _throttleTimestamps[k] = NowMs(); }
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
        List<string> gm = GetGroupMembers();
        string info = "";
        for (int i = 0; i < gm.Count; i++) 
            info += gm[i] + "=" + HealthPct(gm[i]) + "% ";
        Log("Tick: combat=" + Inferno.InCombat("player") + " group=" + gm.Count + " | " + info);
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
private bool IsSpellReady(string s)
{
    return Inferno.SpellCooldown(s) <= 200;
}
private bool IsSettingOn(string s)
{
    return GetCheckBox(s);
}
private bool HasHealthstone()
{
    return Inferno.CustomFunction("HasHealthstone") == 1;
}
private bool TargetIsEnemy()
{
    return Inferno.UnitCanAttack("player", "target");
}
private bool UnitUnder(string u, int p)
{
    return HealthPct(u) < p;
}
private bool EnemiesInMelee(int n)
{
    return Inferno.EnemiesNearUnit(8, "player") >= n;
}
private bool PowerAtLeast(int n, int t)
{
    return Inferno.Power("player", t) >= n;
}
private bool PowerLessThan(int n, int t)
{
    return Inferno.Power("player", t) < n;
}
private bool GroupMembersUnder(int pct, int min)
{
    return GetGroupMembers().Count(u => !Inferno.IsDead(u) && HealthPct(u) < pct) >= min;
}
private bool AnyAllyHasDebuff(string d)
{
    return GetGroupMembers().Any(u => !Inferno.IsDead(u) && Inferno.HasDebuff(d, u, false));
}
private bool AnyAllyHasDebuff(string d, int stacks)
{
    return GetGroupMembers().Any(u => !Inferno.IsDead(u) && Inferno.HasDebuff(d, u, false) && Inferno.DebuffStacks(d, u, false) >= stacks);
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
    List<string> r = new List<string>();
    if (Inferno.InRaid()) 
    { 
        int sz = Inferno.GroupSize(); 
        for (int i = 1; i <= sz; i++) 
        { 
            string tk = "raid" + i; 
            if (Inferno.UnitName(tk) != "") r.Add(tk); 
        } 
    }
    else if (Inferno.InParty()) 
    { 
        r.Add("player"); 
        int sz = Inferno.GroupSize(); 
        for (int i = 1; i < sz; i++) 
        { 
            string tk = "party" + i; 
            if (Inferno.UnitName(tk) != "") r.Add(tk); 
        } 
    }
    else { r.Add("player"); }
    return r;
}
private string LowestAllyUnder(int pct, string spell)
{
    return GetGroupMembers()
        .Where(u => !Inferno.IsDead(u) && HealthPct(u) < pct && Inferno.CanCast(spell, u))
        .OrderBy(u => HealthPct(u))
        .FirstOrDefault();
}
private string LowestAllyInRange(string spell)
{
    return GetGroupMembers()
        .Where(u => !Inferno.IsDead(u) && Inferno.CanCast(spell, u))
        .OrderBy(u => HealthPct(u))
        .FirstOrDefault();
}
private string GetAllyWithDebuff(string d, string spell)
{
    return GetGroupMembers()
        .Where(u => !Inferno.IsDead(u) && Inferno.HasDebuff(d, u, false) && Inferno.SpellInRange(spell, u))
        .FirstOrDefault();
}
private string GetAllyWithMostStacks(string d, string spell)
{
    return GetGroupMembers()
        .Where(u => !Inferno.IsDead(u) && Inferno.HasDebuff(d, u, false) && Inferno.SpellInRange(spell, u))
        .OrderByDescending(u => Inferno.DebuffStacks(d, u, false))
        .FirstOrDefault();
}

private int HealthPct(string u) 
{ 
    int mx = Inferno.MaxHealth(u); 
    if (mx < 1) mx = 1; 
    return (Inferno.Health(u) * 100) / mx; 
}

public override void LoadSettings()
{
    Settings.Add(new Setting("Enable Logging", true));
    Settings.Add(new Setting("Use Circle of Healing", true));
    Settings.Add(new Setting("Use Prayer of Mending", true));
    Settings.Add(new Setting("Do DPS", false));
    Settings.Add(new Setting("Healthstone HP %", 1, 100, 50));
}
public override void Initialize()
{
    Spellbook.Add("Circle of Healing");
    Spellbook.Add("Desperate Prayer");
    Spellbook.Add("Dispel Magic");
    Spellbook.Add("Divine Hymn");
    Spellbook.Add("Divine Star");
    Spellbook.Add("Flash Heal");
    Spellbook.Add("Guardian Spirit");
    Spellbook.Add("Halo");
    Spellbook.Add("Heal");
    Spellbook.Add("Holy Fire");
    Spellbook.Add("Holy Word: Sanctify");
    Spellbook.Add("Holy Word: Serenity");
    Spellbook.Add("Mindgames");
    Spellbook.Add("Power Word: Fortitude");
    Spellbook.Add("Power Word: Life");
    Spellbook.Add("Power Word: Shield");
    Spellbook.Add("Prayer of Healing");
    Spellbook.Add("Prayer of Mending");
    Spellbook.Add("Renew");
    Spellbook.Add("Shadow Word: Death");
    Spellbook.Add("Shadow Word: Pain");
    Spellbook.Add("Smite");
    Macros.Add("cast_fh", "/cast [@focus] Flash Heal");
    Macros.Add("cast_heal", "/cast [@focus] Heal");
    Macros.Add("cast_renew", "/cast [@focus] Renew");
    Macros.Add("cast_pws", "/cast [@focus] Power Word: Shield");
    Macros.Add("cast_pom", "/cast [@focus] Prayer of Mending");
    Macros.Add("cast_serenity", "/cast [@focus] Holy Word: Serenity");
    Macros.Add("cast_pwl", "/cast [@focus] Power Word: Life");
    Macros.Add("cast_gs", "/cast [@focus] Guardian Spirit");
    Macros.Add("cast_dispel", "/cast [@focus] Dispel Magic");
    InitializeSharedComponents();
    _logFile = "penelos_priest_holy_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log";
    Inferno.PrintMessage("Penelos Gambits - Holy Priest loaded!", Color.Green);
    Log("Initialize complete");
}

private bool HasPrayerOfMending(string unit)
{
    return Inferno.HasBuff("Prayer of Mending", unit, true);
}
private bool HasRenew(string unit)
{
    return Inferno.HasBuff("Renew", unit, true);
}
private bool HasShield(string unit)
{
    return Inferno.HasBuff("Power Word: Shield", unit, true) || Inferno.HasDebuff("Weakened Soul", unit, false);
}
private int RenewRemaining(string unit)
{
    if (!HasRenew(unit)) return 0;
    return Inferno.BuffRemaining("Renew", unit, true);
}

private bool RunHealGambits()
{
    if (IsInCombat() && UnitUnder("player", GetSlider("Healthstone HP %")) && HasHealthstone() && Inferno.ItemCooldown(HEALTHSTONE_ID) == 0)
    { 
        Log("Using Healthstone (player " + HealthPct("player") + "%)"); 
        Inferno.Cast(MACRO_USE_HEALTHSTONE, QuickDelay: true); 
        return true; 
    }
    if (IsInCombat() && UnitUnder("player", 60) && Inferno.CanCast("Desperate Prayer"))
    { 
        Log("Casting Desperate Prayer (player " + HealthPct("player") + "%)"); 
        return CastPersonal("Desperate Prayer"); 
    }
    if (IsInCombat())
    { 
        string t = LowestAllyUnder(25, "Guardian Spirit"); 
        if (t != null) 
        { 
            Log("Casting Guardian Spirit on " + t + " (" + HealthPct(t) + "%)"); 
            return CastOnFocus(t, "cast_gs"); 
        } 
    }
    if (IsInCombat())
    { 
        string t = LowestAllyUnder(35, "Power Word: Life"); 
        if (t != null) 
        { 
            Log("Casting Power Word: Life on " + t + " (" + HealthPct(t) + "%)"); 
            return CastOnFocus(t, "cast_pwl"); 
        } 
    }
    if (IsInCombat() && GroupMembersUnder(50, 3) && Inferno.CanCast("Divine Hymn"))
    { 
        Log("Casting Divine Hymn"); 
        return CastPersonal("Divine Hymn"); 
    }
    if (IsInCombat() && GroupMembersUnder(80, 3) && Inferno.CanCast("Holy Word: Sanctify"))
    { 
        Log("Casting Holy Word: Sanctify"); 
        return CastPersonal("Holy Word: Sanctify"); 
    }
    if (IsSettingOn("Use Circle of Healing") && IsInCombat() && GroupMembersUnder(85, 3) && Inferno.CanCast("Circle of Healing"))
    { 
        Log("Casting Circle of Healing"); 
        return CastPersonal("Circle of Healing"); 
    }
    if (IsInCombat() && GroupMembersUnder(90, 3) && CanCastWhileMoving("Prayer of Healing") && PowerAtLeast(25000, MANA))
    { 
        Log("Casting Prayer of Healing"); 
        return CastPersonal("Prayer of Healing"); 
    }
    if (IsInCombat())
    { 
        string t = LowestAllyUnder(75, "Holy Word: Serenity"); 
        if (t != null) 
        { 
            Log("Casting Holy Word: Serenity on " + t + " (" + HealthPct(t) + "%)"); 
            return CastOnFocus(t, "cast_serenity"); 
        } 
    }
    if (IsInCombat() && CanCastWhileMoving("Flash Heal"))
    { 
        string t = LowestAllyUnder(65, "Flash Heal"); 
        if (t != null) 
        { 
            Log("Casting Flash Heal on " + t + " (" + HealthPct(t) + "%)"); 
            return CastOnFocus(t, "cast_fh"); 
        } 
    }
    if (IsSettingOn("Use Prayer of Mending") && IsInCombat())
    { 
        string t = LowestAllyInRange("Prayer of Mending"); 
        if (t != null && !HasPrayerOfMending(t)) 
        { 
            Log("Casting Prayer of Mending on " + t); 
            return CastOnFocus(t, "cast_pom"); 
        } 
    }
    if (IsInCombat())
    { 
        string t = LowestAllyUnder(90, "Power Word: Shield"); 
        if (t != null && !HasShield(t)) 
        { 
            Log("Casting Power Word: Shield on " + t + " (" + HealthPct(t) + "%)"); 
            return CastOnFocus(t, "cast_pws"); 
        } 
    }
    if (IsInCombat())
    { 
        string t = LowestAllyUnder(95, "Renew"); 
        if (t != null && (!HasRenew(t) || RenewRemaining(t) < 3000)) 
        { 
            Log("Casting Renew on " + t + " (" + HealthPct(t) + "%)"); 
            return CastOnFocus(t, "cast_renew"); 
        } 
    }
    if (IsInCombat() && CanCastWhileMoving("Heal") && PowerAtLeast(15000, MANA))
    { 
        string t = LowestAllyUnder(80, "Heal"); 
        if (t != null) 
        { 
            Log("Casting Heal on " + t + " (" + HealthPct(t) + "%)"); 
            return CastOnFocus(t, "cast_heal"); 
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
    if (IsSettingOn("Do DPS") && IsInCombat() && TargetIsEnemy() && Inferno.CanCast("Mindgames", "target"))
    { 
        Log("Casting Mindgames"); 
        return CastOnEnemy("Mindgames"); 
    }
    if (IsSettingOn("Do DPS") && IsInCombat() && TargetIsEnemy() && Inferno.CanCast("Holy Fire", "target"))
    { 
        Log("Casting Holy Fire"); 
        return CastOnEnemy("Holy Fire"); 
    }
    if (IsSettingOn("Do DPS") && IsInCombat() && TargetIsEnemy() && Inferno.CanCast("Shadow Word: Death", "target") && HealthPct("target") < 20)
    { 
        Log("Casting Shadow Word: Death"); 
        return CastOnEnemy("Shadow Word: Death"); 
    }
    if (IsSettingOn("Do DPS") && IsInCombat() && TargetIsEnemy() && Inferno.CanCast("Shadow Word: Pain", "target") && !Inferno.HasDebuff("Shadow Word: Pain", "target", true))
    { 
        Log("Casting Shadow Word: Pain"); 
        return CastOnEnemy("Shadow Word: Pain"); 
    }
    if (IsSettingOn("Do DPS") && IsInCombat() && EnemiesInMelee(3) && Inferno.CanCast("Halo"))
    { 
        Log("Casting Halo"); 
        return CastPersonal("Halo"); 
    }
    if (IsSettingOn("Do DPS") && IsInCombat() && TargetIsEnemy() && Inferno.CanCast("Divine Star"))
    { 
        Log("Casting Divine Star"); 
        return CastPersonal("Divine Star"); 
    }
    if (IsInCombat() && TargetIsEnemy() && Inferno.CanCast("Smite", "target"))
    {
        Log("Filler Smite on target"); 
        return CastOnEnemy("Smite");
    }
    if (IsInCombat())
    {
        string t = LowestAllyInRange("Renew");
        if (t != null && (!HasRenew(t) || RenewRemaining(t) < 3000)) 
        { 
            Log("Filler Renew on " + t + " (" + HealthPct(t) + "%)"); 
            return CastOnFocus(t, "cast_renew"); 
        }
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
            if (IsSpellReady("Dispel Magic") && AnyAllyHasDebuff("Lasher Toxin", 2))
            { 
                string t = GetAllyWithMostStacks("Lasher Toxin", "Dispel Magic"); 
                if (t != null) 
                { 
                    CastOnFocus(t, "cast_dispel"); 
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
    if (!IsSpellReady("Dispel Magic") || !AnyAllyHasDebuff(debuff)) return false;
    string t = GetAllyWithDebuff(debuff, "Dispel Magic");
    if (t == null) return false;
    CastOnFocus(t, "cast_dispel");
    return true;
}
private bool TryDispelStacks(string debuff, int min)
{
    if (!IsSpellReady("Dispel Magic") || !AnyAllyHasDebuff(debuff, min)) return false;
    string t = GetAllyWithMostStacks(debuff, "Dispel Magic");
    if (t == null) return false;
    CastOnFocus(t, "cast_dispel");
    return true;
}
}

}
