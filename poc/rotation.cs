using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using InfernoWow.API;

namespace InfernoWow.Modules
{

public class HolyPaladinPvE : Rotation
{
    private string _queuedAction = null;
    private Dictionary<string, long> _throttleTimestamps = new Dictionary<string, long>();
    private const int HOLY_POWER = 9;

    public override void LoadSettings()
    {
        Settings.Add(new Setting("Enable Logging", false));
    }

    public override void Initialize()
    {
        Spellbook.Add("Avenging Wrath"); Spellbook.Add("Blessing of Freedom");
        Spellbook.Add("Cleanse"); Spellbook.Add("Divine Protection");
        Spellbook.Add("Divine Toll"); Spellbook.Add("Flash of Light");
        Spellbook.Add("Holy Light"); Spellbook.Add("Holy Shock");
        Spellbook.Add("Judgment"); Spellbook.Add("Light of Dawn");
        Spellbook.Add("Shield of the Righteous"); Spellbook.Add("Word of Glory");

        Macros.Add("cast_fol", "/cast [@focus] Flash of Light");
        Macros.Add("cast_hl", "/cast [@focus] Holy Light");
        Macros.Add("cast_hs", "/cast [@focus] Holy Shock");
        Macros.Add("cast_wog", "/cast [@focus] Word of Glory");
        Macros.Add("cast_dt", "/cast [@focus] Divine Toll");
        Macros.Add("cast_cleanse", "/cast [@focus] Cleanse");
        Macros.Add("cast_bof", "/cast [@focus] Blessing of Freedom");
        Macros.Add("focus_player", "/focus player");
        for (int i = 1; i <= 4; i++) Macros.Add("focus_party" + i, "/focus party" + i);
        for (int i = 1; i <= 28; i++) Macros.Add("focus_raid" + i, "/focus raid" + i);
        Macros.Add("target_enemy", "/targetenemy");
        Inferno.PrintMessage("Penelos Gambits - Holy Paladin loaded!", Color.Green);
    }

    public override bool CombatTick()
    {
        if (Inferno.IsDead("player")) return false;
        if (ProcessQueue()) return true;
        int mapId = Inferno.GetMapID();
        if (RunDungeonGambits(mapId)) return true;
        if (RunHealGambits()) return true;
        if (RunDmgGambits()) return true;
        return false;
    }

    public override bool OutOfCombatTick() { return CombatTick(); }
    public override void OnStop() { }

    // -- Heal Gambits --
    private bool RunHealGambits()
    {
        if (!ThrottleIsOpen("heal_throttle", 2000)) return false;
        if (IsInCombat() && IsSpellReady("Divine Protection") && UnitUnder("player", 75))
            return CastPersonal("Divine Protection");
        if (IsInCombat() && IsSpellReady("Avenging Wrath") && GroupMembersUnder(60, 2))
            return CastPersonal("Avenging Wrath");
        if (IsInCombat() && IsSpellReady("Divine Toll") && GroupMembersUnder(80, 2) && PowerLessThan(3, HOLY_POWER))
        { string t = LowestAllyInRange("Divine Toll"); if (t != null) return CastOnFocus(t, "cast_dt"); }
        if (IsInCombat() && PowerAtLeast(3, HOLY_POWER))
        { string t = LowestAllyUnder(90, "Word of Glory"); if (t != null) return CastOnFocus(t, "cast_wog"); }
        if (IsInCombat())
        { string t = LowestAllyUnder(60, "Holy Light"); if (t != null) return CastOnFocus(t, "cast_hl"); }
        if (IsInCombat() && ThrottleIsOpen("holy_shock_cd", 2000))
        { string t = LowestAllyInRange("Holy Shock"); if (t != null) { ThrottleRestart("holy_shock_cd"); return CastOnFocus(t, "cast_hs"); } }
        if (IsInCombat())
        { string t = LowestAllyUnder(95, "Flash of Light"); if (t != null) return CastOnFocus(t, "cast_fol"); }
        return false;
    }

    // -- Damage Gambits --
    private bool RunDmgGambits()
    {
        if (!ThrottleIsOpen("dmg_throttle", 1500)) return false;
        if (IsInCombat() && !TargetIsEnemy()) { Inferno.Cast("target_enemy", true); return true; }
        if (IsInCombat() && PowerAtLeast(4, HOLY_POWER) && EnemiesInMelee(1))
            return CastPersonal("Shield of the Righteous");
        if (IsInCombat() && TargetIsEnemy() && PowerLessThan(4, HOLY_POWER) && IsSpellReady("Judgment"))
            return CastOnEnemy("Judgment");
        if (IsInCombat())
        { string t = LowestAllyInRange("Flash of Light"); if (t != null) return CastOnFocus(t, "cast_fol"); }
        return false;
    }

    // -- Dungeon Gambits --
    private bool RunDungeonGambits(int mapId)
    {
        if (!IsInCombat()) return false;
        if (!ThrottleIsOpen("dispel_throttle", 1500)) return false;
        switch (mapId)
        {
            case 480: return TryDispel("Aqua Bomb");
            case 2511: case 2515: case 2516: case 2517: case 2518: case 2519: case 2520:
                if (TryDispel("Ethereal Shackles")) return true;
                if (TryDispel("Consuming Void")) return true;
                if (TryBof("Ethereal Shackles")) return true;
                if (TryDispel("Holy Fire")) return true;
                return TryDispel("Polymorph");
            case 601: case 602: return false;
            case 823:
                if (TryDispel("Cryoshards")) return true;
                return TryDispelStacks("Rotting Strikes", 3);
            case 2492: case 2493: case 2494: case 2496: case 2497: case 2498: case 2499:
                if (TryDispel("Poison Spray")) return true;
                if (TryDispel("Soul Torment")) return true;
                return TryDispel("Poison Blades");
            case 2501: return TryDispel("Infected Pinions");
            case 2097: case 2098: case 2099:
                if (IsSpellReady("Cleanse") && AnyAllyHasDebuff("Lasher Toxin", 2))
                { string t = GetAllyWithMostStacks("Lasher Toxin", "Cleanse"); if (t != null) { ThrottleRestart("dispel_throttle"); return CastOnFocus(t, "cast_cleanse"); } }
                return false;
            default: return false;
        }
    }

    private bool TryDispel(string debuff)
    {
        if (!IsSpellReady("Cleanse") || !AnyAllyHasDebuff(debuff)) return false;
        string t = GetAllyWithDebuff(debuff, "Cleanse");
        if (t == null) return false;
        ThrottleRestart("dispel_throttle"); return CastOnFocus(t, "cast_cleanse");
    }
    private bool TryDispelStacks(string debuff, int min)
    {
        if (!IsSpellReady("Cleanse") || !AnyAllyHasDebuff(debuff, min)) return false;
        string t = GetAllyWithMostStacks(debuff, "Cleanse");
        if (t == null) return false;
        ThrottleRestart("dispel_throttle"); return CastOnFocus(t, "cast_cleanse");
    }
    private bool TryBof(string debuff)
    {
        if (!IsSpellReady("Blessing of Freedom") || !AnyAllyHasDebuff(debuff)) return false;
        string t = GetAllyWithDebuff(debuff, "Blessing of Freedom");
        if (t == null) return false;
        ThrottleRestart("dispel_throttle"); Inferno.StopCasting(); return CastOnFocus(t, "cast_bof");
    }

    // -- Conditions --
    private bool IsInCombat() { return Inferno.InCombat("player"); }
    private bool IsSpellReady(string s) { return Inferno.SpellCooldown(s) <= 200; }
    private bool TargetIsEnemy() { return Inferno.UnitCanAttack("player", "target"); }
    private bool UnitUnder(string u, int p) { return HealthPct(u) < p; }
    private bool EnemiesInMelee(int n) { return Inferno.EnemiesNearUnit(8, "player") >= n; }
    private bool PowerAtLeast(int n, int t) { return Inferno.Power("player", t) >= n; }
    private bool PowerLessThan(int n, int t) { return Inferno.Power("player", t) < n; }

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
    private int HealthPct(string u) { int mx = Inferno.MaxHealth(u); if (mx < 1) mx = 1; return (Inferno.Health(u) * 100) / mx; }

    // -- Group --
    private List<string> GetGroupMembers()
    {
        List<string> r = new List<string>();
        if (Inferno.InRaid()) { int sz = Inferno.GroupSize(); for (int i = 1; i <= sz; i++) { string tk = "raid" + i; if (Inferno.UnitName(tk) != "") r.Add(tk); } }
        else if (Inferno.InParty()) { r.Add("player"); int sz = Inferno.GroupSize(); for (int i = 1; i < sz; i++) { string tk = "party" + i; if (Inferno.UnitName(tk) != "") r.Add(tk); } }
        else { r.Add("player"); }
        return r;
    }

    // -- Selectors --
    private string LowestAllyUnder(int pct, string spell)
    {
        return GetGroupMembers().Where(u => !Inferno.IsDead(u) && HealthPct(u) < pct && Inferno.SpellInRange(spell, u)).OrderBy(u => HealthPct(u)).FirstOrDefault();
    }
    private string LowestAllyInRange(string spell)
    {
        return GetGroupMembers().Where(u => !Inferno.IsDead(u) && Inferno.SpellInRange(spell, u)).OrderBy(u => HealthPct(u)).FirstOrDefault();
    }

    // -- Cast --
    private bool CastOnFocus(string unit, string macro) { Inferno.Cast("focus_" + unit); _queuedAction = macro; ThrottleRestart("heal_throttle"); return true; }
    private bool CastPersonal(string s) { Inferno.Cast(s); ThrottleRestart("heal_throttle"); return true; }
    private bool CastOnEnemy(string s) { Inferno.Cast(s); ThrottleRestart("dmg_throttle"); return true; }
    private bool ProcessQueue() { if (_queuedAction == null) return false; string a = _queuedAction; _queuedAction = null; Inferno.Cast(a, true); return true; }

    // -- Selectors (continued) --
    private string GetAllyWithDebuff(string d, string spell)
    {
        return GetGroupMembers().Where(u => !Inferno.IsDead(u) && Inferno.HasDebuff(d, u, false) && Inferno.SpellInRange(spell, u)).FirstOrDefault();
    }
    private string GetAllyWithMostStacks(string d, string spell)
    {
        return GetGroupMembers().Where(u => !Inferno.IsDead(u) && Inferno.HasDebuff(d, u, false) && Inferno.SpellInRange(spell, u)).OrderByDescending(u => Inferno.DebuffStacks(d, u, false)).FirstOrDefault();
    }

    // -- Throttle --
    private long NowMs() { return DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond; }
    private bool ThrottleIsOpen(string k, int ms) { if (!_throttleTimestamps.ContainsKey(k)) return true; return (NowMs() - _throttleTimestamps[k]) >= ms; }
    private void ThrottleRestart(string k) { _throttleTimestamps[k] = NowMs(); }

    // -- Log --
    private void Log(string msg) { if (GetCheckBox("Enable Logging")) Inferno.PrintMessage(msg, Color.White); }
}

}

