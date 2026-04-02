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
    private Dictionary<string, long> _throttleTimestamps = new Dictionary<string, long>();
    private const int HOLY_POWER = 9;
    private string _logFile = null;

    public override void LoadSettings()
    {
        Settings.Add(new Setting("Enable Logging", true));
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

        _logFile = "penelos_holy_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log";
        Inferno.PrintMessage("Penelos Gambits - Holy Paladin loaded!", Color.Green);
        Log("Initialize complete");
    }

    public override bool CombatTick()
    {
        if (Inferno.IsDead("player")) return false;
        if (ProcessQueue()) return true;
        // Global GCD gate - don't evaluate new actions while GCD is running
        if (!ThrottleIsOpen("gcd", 1000)) return false;

        // Periodic status log
        if (ThrottleIsOpen("diag", 2000))
        {
            ThrottleRestart("diag");
            List<string> gm = GetGroupMembers();
            string info = "";
            for (int i = 0; i < gm.Count; i++) info += gm[i] + "=" + HealthPct(gm[i]) + "% ";
            Log("Tick: combat=" + Inferno.InCombat("player") + " group=" + gm.Count + " | " + info);
        }

        int mapId = Inferno.GetMapID();
        if (RunDungeonGambits(mapId)) return true;
        if (RunHealGambits()) return true;
        if (RunDmgGambits()) return true;
        return false;
    }

    public override bool OutOfCombatTick() { return CombatTick(); }
    public override void OnStop() { Log("Rotation stopped"); }

    // -- Heal Gambits --
    private bool RunHealGambits()
    {
        // Divine Protection if player under 75% (instant, off-GCD)
        if (IsInCombat() && IsSpellReady("Divine Protection") && UnitUnder("player", 75))
        { Log("Casting Divine Protection (player " + HealthPct("player") + "%)"); return CastPersonal("Divine Protection"); }

        // Avenging Wrath if 2+ under 60% (instant, off-GCD)
        if (IsInCombat() && IsSpellReady("Avenging Wrath") && GroupMembersUnder(60, 2))
        { Log("Casting Avenging Wrath"); return CastPersonal("Avenging Wrath"); }

        // Divine Toll if 2+ under 80% and HolyPower < 3 (instant)
        if (IsInCombat() && IsSpellReady("Divine Toll") && GroupMembersUnder(80, 2) && PowerLessThan(3, HOLY_POWER))
        { string t = LowestAllyInRange("Divine Toll"); if (t != null) { Log("Casting Divine Toll on " + t + " (" + HealthPct(t) + "%)"); return CastOnFocus(t, "cast_dt"); } }

        // Word of Glory if lowest under 90% and HP >= 3 (instant)
        if (IsInCombat() && PowerAtLeast(3, HOLY_POWER))
        { string t = LowestAllyUnder(90, "Word of Glory"); if (t != null) { Log("Casting Word of Glory on " + t + " (" + HealthPct(t) + "%)"); return CastOnFocus(t, "cast_wog"); } }

        // Holy Shock on lowest (API bug: cooldown/charges always return 0, manual throttle)
        if (IsInCombat() && ThrottleIsOpen("hs_cd", 5000))
        { string t = LowestAllyInRange("Holy Shock"); if (t != null) { Log("Casting Holy Shock on " + t + " (" + HealthPct(t) + "%)"); ThrottleRestart("hs_cd"); return CastOnFocus(t, "cast_hs"); } }

        // Holy Light if lowest under 60%
        if (IsInCombat())
        { string t = LowestAllyUnder(60, "Holy Light"); if (t != null) { Log("Casting Holy Light on " + t + " (" + HealthPct(t) + "%)"); return CastOnFocus(t, "cast_hl"); } }

        // Flash of Light if lowest under 95%
        if (IsInCombat())
        { string t = LowestAllyUnder(95, "Flash of Light"); if (t != null) { Log("Casting Flash of Light on " + t + " (" + HealthPct(t) + "%)"); return CastOnFocus(t, "cast_fol"); } }

        return false;
    }

    // -- Damage Gambits --
    private bool RunDmgGambits()
    {
        if (IsInCombat() && !TargetIsEnemy()) { Inferno.Cast("target_enemy", true); return true; }

        if (IsInCombat() && PowerAtLeast(4, HOLY_POWER) && EnemiesInMelee(1))
        { Log("Casting Shield of the Righteous"); return CastPersonal("Shield of the Righteous"); }

        if (IsInCombat() && TargetIsEnemy() && PowerLessThan(4, HOLY_POWER) && IsSpellReady("Judgment"))
        { Log("Casting Judgment"); return CastOnEnemy("Judgment"); }

        // Flash of Light filler - always have something to do
        if (IsInCombat())
        {
            string t = LowestAllyInRange("Flash of Light");
            if (t != null) { Log("Filler FoL on " + t + " (" + HealthPct(t) + "%)"); return CastOnFocus(t, "cast_fol"); }
            Log("Filler FoL on player (fallback)"); return CastOnFocus("player", "cast_fol");
        }

        return false;
    }

    // -- Dungeon Gambits --
    private bool RunDungeonGambits(int mapId)
    {
        if (!IsInCombat()) return false;
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
                { string t = GetAllyWithMostStacks("Lasher Toxin", "Cleanse"); if (t != null) { Log("Dispelling Lasher Toxin on " + t); ThrottleRestart("dispel_cd"); return CastOnFocus(t, "cast_cleanse"); } }
                return false;
            default: return false;
        }
    }

    private bool TryDispel(string debuff)
    {
        if (!IsSpellReady("Cleanse") || !AnyAllyHasDebuff(debuff)) return false;
        if (!ThrottleIsOpen("dispel_cd", 500)) return false;
        string t = GetAllyWithDebuff(debuff, "Cleanse");
        if (t == null) return false;
        Log("Dispelling " + debuff + " on " + t);
        ThrottleRestart("dispel_cd"); return CastOnFocus(t, "cast_cleanse");
    }
    private bool TryDispelStacks(string debuff, int min)
    {
        if (!IsSpellReady("Cleanse") || !AnyAllyHasDebuff(debuff, min)) return false;
        if (!ThrottleIsOpen("dispel_cd", 500)) return false;
        string t = GetAllyWithMostStacks(debuff, "Cleanse");
        if (t == null) return false;
        Log("Dispelling " + debuff + " on " + t);
        ThrottleRestart("dispel_cd"); return CastOnFocus(t, "cast_cleanse");
    }
    private bool TryBof(string debuff)
    {
        if (!IsSpellReady("Blessing of Freedom") || !AnyAllyHasDebuff(debuff)) return false;
        if (!ThrottleIsOpen("dispel_cd", 500)) return false;
        string t = GetAllyWithDebuff(debuff, "Blessing of Freedom");
        if (t == null) return false;
        Log("Casting Blessing of Freedom on " + t + " for " + debuff);
        ThrottleRestart("dispel_cd"); Inferno.StopCasting(); return CastOnFocus(t, "cast_bof");
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
    private bool CastOnFocus(string unit, string macro) { Inferno.Cast("focus_" + unit); _queuedAction = macro; return true; }
    private bool CastPersonal(string s) { Inferno.Cast(s); ThrottleRestart("gcd"); return true; }
    private bool CastOnEnemy(string s) { Inferno.Cast(s); ThrottleRestart("gcd"); return true; }
    private bool ProcessQueue() { if (_queuedAction == null) return false; string a = _queuedAction; _queuedAction = null; Inferno.Cast(a, true); ThrottleRestart("gcd"); return true; }

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
    private void Log(string msg)
    {
        if (!GetCheckBox("Enable Logging")) return;
        Inferno.PrintMessage(msg, Color.White);
        if (_logFile != null) { try { File.AppendAllText(_logFile, DateTime.Now.ToString("HH:mm:ss.fff") + " " + msg + "\n"); } catch { } }
    }
}

}
