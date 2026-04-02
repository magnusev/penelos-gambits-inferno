using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using InfernoWow.API;

namespace InfernoWow.Modules
{

public class HolyPaladinPvE : Rotation
{
    // --- State ---
    private string _queuedAction = null;
    private Dictionary<string, long> _throttleTimestamps = new Dictionary<string, long>();
    private const int HOLY_POWER = 9;

    // --- Settings ---
    public override void LoadSettings()
    {
        Settings.Add(new Setting("Enable Logging", false));
    }

    // --- Spell and Macro Registration ---
    public override void Initialize()
    {
        // Spells
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

        // Cast macros (friendly targeted via @focus)
        Macros.Add("cast_flash_of_light", "/cast [@focus] Flash of Light");
        Macros.Add("cast_holy_light", "/cast [@focus] Holy Light");
        Macros.Add("cast_holy_shock", "/cast [@focus] Holy Shock");
        Macros.Add("cast_word_of_glory", "/cast [@focus] Word of Glory");
        Macros.Add("cast_divine_toll", "/cast [@focus] Divine Toll");
        Macros.Add("cast_cleanse", "/cast [@focus] Cleanse");
        Macros.Add("cast_bof", "/stopcasting\\n/cast [@focus] Blessing of Freedom");

        // Focus macros (targeting)
        Macros.Add("focus_player", "/focus player");
        for (int i = 1; i <= 4; i++)
            Macros.Add("focus_party" + i, "/focus party" + i);
        for (int i = 1; i <= 28; i++)
            Macros.Add("focus_raid" + i, "/focus raid" + i);

        // Enemy targeting
        Macros.Add("target_enemy", "/targetenemy");

        Inferno.PrintMessage("Penelos Gambits - Holy Paladin loaded!", Color.Green);
    }

    // --- Tick ---
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

    public override bool OutOfCombatTick()
    {
        return CombatTick();
    }

    public override void OnStop() { }

    // -- Heal Gambits --

    private bool RunHealGambits()
    {
        if (!ThrottleIsOpen("heal_throttle", 2000)) return false;

        // Divine Protection if player under 75%
        if (IsInCombat()
            && IsSpellReady("Divine Protection")
            && UnitUnder("player", 75))
        {
            return CastPersonal("Divine Protection");
        }

        // Avenging Wrath if 2+ group members under 60%
        if (IsInCombat()
            && IsSpellReady("Avenging Wrath")
            && GroupMembersUnder(60, 2))
        {
            return CastPersonal("Avenging Wrath");
        }

        // Divine Toll if 2+ under 80% and HolyPower < 3
        if (IsInCombat()
            && IsSpellReady("Divine Toll")
            && GroupMembersUnder(80, 2)
            && PowerLessThan(3, HOLY_POWER))
        {
            string target = LowestAllyInRange("Divine Toll");
            if (target != null)
                return CastOnFocus(target, "cast_divine_toll");
        }

        // Word of Glory if lowest under 90% and HolyPower >= 3
        if (IsInCombat()
            && LowestAllyUnder(90, "Word of Glory") != null
            && PowerAtLeast(3, HOLY_POWER))
        {
            string target = LowestAllyUnder(90, "Word of Glory");
            if (target != null)
                return CastOnFocus(target, "cast_word_of_glory");
        }

        // Holy Light if lowest under 60%
        if (IsInCombat()
            && LowestAllyUnder(60, "Holy Light") != null)
        {
            string target = LowestAllyUnder(60, "Holy Light");
            if (target != null)
                return CastOnFocus(target, "cast_holy_light");
        }

        // Holy Shock (defensive) every 2s
        if (IsInCombat()
            && ThrottleIsOpen("holy_shock_cd", 2000))
        {
            string target = LowestAllyInRange("Holy Shock");
            if (target != null)
            {
                ThrottleRestart("holy_shock_cd");
                return CastOnFocus(target, "cast_holy_shock");
            }
        }

        // Flash of Light if lowest under 95%
        if (IsInCombat()
            && LowestAllyUnder(95, "Flash of Light") != null)
        {
            string target = LowestAllyUnder(95, "Flash of Light");
            if (target != null)
                return CastOnFocus(target, "cast_flash_of_light");
        }

        // (Light of Dawn - disabled)
        // if (IsInCombat()
        //     && GroupMembersUnder(95, 5)
        //     && PowerAtLeast(4, HOLY_POWER))
        // {
        //     return CastPersonal("Light of Dawn");
        // }

        return false;
    }

    // -- Damage Gambits --

    private bool RunDmgGambits()
    {
        if (!ThrottleIsOpen("dmg_throttle", 1500)) return false;

        // Target enemy if we don't have one
        if (IsInCombat() && !TargetIsEnemy())
        {
            Inferno.Cast("target_enemy", true);
            return true;
        }

        // Shield of the Righteous if HolyPower >= 4 and enemies in melee
        if (IsInCombat()
            && PowerAtLeast(4, HOLY_POWER)
            && EnemiesInMelee(1))
        {
            return CastPersonal("Shield of the Righteous");
        }

        // Judgment if HolyPower < 4 and spell ready
        if (IsInCombat()
            && TargetIsEnemy()
            && PowerLessThan(4, HOLY_POWER)
            && IsSpellReady("Judgment"))
        {
            return CastOnEnemy("Judgment");
        }

        // Flash of Light filler on lowest ally
        if (IsInCombat())
        {
            string target = LowestAllyInRange("Flash of Light");
            if (target != null)
                return CastOnFocus(target, "cast_flash_of_light");
        }

        return false;
    }

    // -- Dungeon Gambits --

    private bool RunDungeonGambits(int mapId)
    {
        if (!IsInCombat()) return false;
        if (!ThrottleIsOpen("dispel_throttle", 1500)) return false;

        switch (mapId)
        {
            case 480:
                return RunProvingGroundsGambits();
            case 2511: case 2515: case 2516: case 2517:
            case 2518: case 2519: case 2520:
                return RunMagistersTerraceGambits();
            case 601: case 602:
                return RunSkyreachGambits();
            case 823:
                return RunPitOfSaronGambits();
            case 2492: case 2493: case 2494:
            case 2496: case 2497: case 2498: case 2499:
                return RunWindrunnerSpireGambits();
            case 2501:
                return RunMaisaraCavernsGambits();
            case 2097: case 2098: case 2099:
                return RunAlgethArAcademyGambits();
            default:
                return false;
        }
    }

    private bool RunProvingGroundsGambits()
    {
        return TryDispel("Aqua Bomb");
    }

    private bool RunMagistersTerraceGambits()
    {
        if (TryDispel("Ethereal Shackles")) return true;
        if (TryDispel("Consuming Void")) return true;
        if (TryBlessingOfFreedom("Ethereal Shackles")) return true;
        if (TryDispel("Holy Fire")) return true;
        if (TryDispel("Polymorph")) return true;
        return false;
    }

    private bool RunSkyreachGambits()
    {
        return false;
    }

    private bool RunPitOfSaronGambits()
    {
        if (TryDispel("Cryoshards")) return true;
        if (TryDispelStacks("Rotting Strikes", 3)) return true;
        return false;
    }

    private bool RunWindrunnerSpireGambits()
    {
        if (TryDispel("Poison Spray")) return true;
        if (TryDispel("Soul Torment")) return true;
        if (TryDispel("Poison Blades")) return true;
        return false;
    }

    private bool RunMaisaraCavernsGambits()
    {
        if (TryDispel("Infected Pinions")) return true;
        return false;
    }

    private bool RunAlgethArAcademyGambits()
    {
        // Dispel Lasher Toxin on unit with 2+ stacks
        if (IsSpellReady("Cleanse")
            && AnyAllyHasDebuff("Lasher Toxin", 2))
        {
            string target = GetAllyWithMostStacks("Lasher Toxin", "Cleanse");
            if (target != null)
            {
                ThrottleRestart("dispel_throttle");
                return CastOnFocus(target, "cast_cleanse");
            }
        }
        return false;
    }

    // --- Dungeon helper: standard dispel pattern ---
    private bool TryDispel(string debuff)
    {
        if (!IsSpellReady("Cleanse")) return false;
        if (!AnyAllyHasDebuff(debuff)) return false;
        string target = GetAllyWithDebuff(debuff, "Cleanse");
        if (target == null) return false;
        ThrottleRestart("dispel_throttle");
        return CastOnFocus(target, "cast_cleanse");
    }

    // --- Dungeon helper: dispel only if stacks >= min ---
    private bool TryDispelStacks(string debuff, int minStacks)
    {
        if (!IsSpellReady("Cleanse")) return false;
        if (!AnyAllyHasDebuff(debuff, minStacks)) return false;
        string target = GetAllyWithMostStacks(debuff, "Cleanse");
        if (target == null) return false;
        ThrottleRestart("dispel_throttle");
        return CastOnFocus(target, "cast_cleanse");
    }

    // --- Dungeon helper: Blessing of Freedom on debuff ---
    private bool TryBlessingOfFreedom(string debuff)
    {
        if (!IsSpellReady("Blessing of Freedom")) return false;
        if (!AnyAllyHasDebuff(debuff)) return false;
        string target = GetAllyWithDebuff(debuff, "Blessing of Freedom");
        if (target == null) return false;
        ThrottleRestart("dispel_throttle");
        return CastOnFocus(target, "cast_bof");
    }

    // -- Condition Helpers --

    private bool IsInCombat()
    {
        return Inferno.InCombat("player");
    }

    private bool IsSpellReady(string spell)
    {
        return Inferno.SpellCooldown(spell) <= 200;
    }

    private bool TargetIsEnemy()
    {
        return Inferno.UnitCanAttack("player", "target");
    }

    private bool UnitUnder(string unit, int hpPct)
    {
        return HealthPct(unit) < hpPct;
    }

    private bool EnemiesInMelee(int minCount)
    {
        return Inferno.EnemiesNearUnit(8, "player") >= minCount;
    }

    private bool PowerAtLeast(int amount, int type)
    {
        return Inferno.Power("player", type) >= amount;
    }

    private bool PowerLessThan(int amount, int type)
    {
        return Inferno.Power("player", type) < amount;
    }

    private bool GroupMembersUnder(int hpPct, int minCount)
    {
        return GetGroupMembers().Count(u => !Inferno.IsDead(u) && HealthPct(u) < hpPct) >= minCount;
    }

    private bool AnyAllyHasDebuff(string debuff)
    {
        return GetGroupMembers().Any(u => !Inferno.IsDead(u) && Inferno.HasDebuff(debuff, u, false));
    }

    private bool AnyAllyHasDebuff(string debuff, int minStacks)
    {
        return GetGroupMembers().Any(u =>
            !Inferno.IsDead(u)
            && Inferno.HasDebuff(debuff, u, false)
            && Inferno.DebuffStacks(debuff, u, false) >= minStacks);
    }

    private int HealthPct(string unit)
    {
        long max = Inferno.MaxHealth(unit);
        if (max <= 0) return 100;
        return (int)((long)Inferno.Health(unit) * 100L / max);
    }

    // -- Selector Helpers --

    private string LowestAllyUnder(int hpPct, string spell)
    {
        return GetGroupMembers()
            .Where(u => !Inferno.IsDead(u))
            .Where(u => HealthPct(u) < hpPct)
            .Where(u => Inferno.SpellInRange(spell, u))
            .OrderBy(u => HealthPct(u))
            .FirstOrDefault();
    }

    private string LowestAllyInRange(string spell)
    {
        return GetGroupMembers()
            .Where(u => !Inferno.IsDead(u))
            .Where(u => Inferno.SpellInRange(spell, u))
            .OrderBy(u => HealthPct(u))
            .FirstOrDefault();
    }

    private string GetAllyWithDebuff(string debuff, string spell)
    {
        return GetGroupMembers()
            .Where(u => !Inferno.IsDead(u))
            .Where(u => Inferno.HasDebuff(debuff, u, false))
            .Where(u => Inferno.SpellInRange(spell, u))
            .FirstOrDefault();
    }

    private string GetAllyWithMostStacks(string debuff, string spell)
    {
        return GetGroupMembers()
            .Where(u => !Inferno.IsDead(u))
            .Where(u => Inferno.HasDebuff(debuff, u, false))
            .Where(u => Inferno.SpellInRange(spell, u))
            .OrderByDescending(u => Inferno.DebuffStacks(debuff, u, false))
            .FirstOrDefault();
    }

    // -- Group Members --

    private List<string> GetGroupMembers()
    {
        var members = new List<string>();
        if (Inferno.InRaid())
        {
            int size = Inferno.GroupSize();
            for (int i = 1; i <= size; i++)
            {
                string token = "raid" + i;
                if (!string.IsNullOrEmpty(Inferno.UnitName(token)))
                    members.Add(token);
            }
        }
        else if (Inferno.InParty())
        {
            members.Add("player");
            int size = Inferno.GroupSize();
            for (int i = 1; i < size; i++)
            {
                string token = "party" + i;
                if (!string.IsNullOrEmpty(Inferno.UnitName(token)))
                    members.Add(token);
            }
        }
        else
        {
            members.Add("player");
        }
        return members;
    }

    // -- Cast Helpers --

    // Friendly targeted: focus unit, queue [@focus] macro
    private bool CastOnFocus(string unitToken, string macroName)
    {
        Inferno.Cast("focus_" + unitToken);
        _queuedAction = macroName;
        ThrottleRestart("heal_throttle");
        return true;
    }

    // Personal / self-buff: cast directly
    private bool CastPersonal(string spellName)
    {
        Inferno.Cast(spellName);
        ThrottleRestart("heal_throttle");
        return true;
    }

    // Enemy targeted: cast on current target
    private bool CastOnEnemy(string spellName)
    {
        Inferno.Cast(spellName);
        ThrottleRestart("dmg_throttle");
        return true;
    }

    // Process queued macro from previous tick
    private bool ProcessQueue()
    {
        if (_queuedAction == null) return false;
        string action = _queuedAction;
        _queuedAction = null;
        Inferno.Cast(action, true);
        return true;
    }

    // -- Throttle --

    private long GetTimestampMs()
    {
        return DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
    }

    private bool ThrottleIsOpen(string key, int intervalMs)
    {
        if (!_throttleTimestamps.ContainsKey(key)) return true;
        return (GetTimestampMs() - _throttleTimestamps[key]) >= intervalMs;
    }

    private void ThrottleRestart(string key)
    {
        _throttleTimestamps[key] = GetTimestampMs();
    }

    // -- Logging --

    private void Log(string message)
    {
        if (!GetCheckBox("Enable Logging")) return;
        Inferno.PrintMessage(message, Color.White);
    }
}

}

