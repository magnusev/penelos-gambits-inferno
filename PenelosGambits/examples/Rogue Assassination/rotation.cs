using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API;

namespace InfernoWow.Modules
{
    /// <summary>
    /// Assassination Rogue - Translated from SimulationCraft Midnight APL
    /// Core mechanics: Garrote/Rupture maintenance, Deathmark+Kingsbane window,
    /// Combo point generation → Envenom spending. Crimson Tempest for AoE bleed spread.
    /// </summary>
    public class AssassinationRogueRotation : Rotation
    {
        List<string> Abilities = new List<string> {
            "Mutilate", "Ambush", "Fan of Knives", "Envenom",
            "Garrote", "Rupture", "Crimson Tempest", "Shiv",
            "Deathmark", "Kingsbane", "Thistle Tea",
            "Slice and Dice", "Vanish",
        };
        List<string> TalentChecks = new List<string> {
            "Improved Garrote", "Blindside", "Razor Wire",
            "Crimson Tempest", "Darkest Night", "Toxic Stiletto",
            "Implacable Tracker",
        };
        List<string> DefensiveSpells = new List<string> { "Evasion", "Feint", "Cloak of Shadows", "Crimson Vial" };
        List<string> UtilitySpells = new List<string> { "Kick" };

        const int HealthstoneItemID = 5512;
        const int EnergyPowerType = 3;
        const int ComboPointType = 4;
        private Random _rng = new Random();
        private int _lastCastingID = 0;
        private int _interruptTargetPct = 0;

        public override void LoadSettings()
        {
            Settings.Add(new Setting("=== Assassination Rogue ==="));
            Settings.Add(new Setting("Use Deathmark", true));
            Settings.Add(new Setting("Use Kingsbane", true));
            Settings.Add(new Setting("Use Trinkets", true));
            Settings.Add(new Setting("=== Defensives ==="));
            Settings.Add(new Setting("Use Defensives", true));
            Settings.Add(new Setting("Evasion HP %", 1, 100, 40));
            Settings.Add(new Setting("Crimson Vial HP %", 1, 100, 60));
            Settings.Add(new Setting("Healthstone HP %", 1, 100, 50));
            Settings.Add(new Setting("=== Interrupt ==="));
            Settings.Add(new Setting("Auto Interrupt", true));
            Settings.Add(new Setting("Interrupt at cast % (min)", 0, 100, 40));
            Settings.Add(new Setting("Interrupt at cast % (max)", 0, 100, 90));
        }

        public override void Initialize()
        {
            Inferno.PrintMessage("             //////////////////////////////////////", Color.DarkGoldenrod);
            Inferno.PrintMessage("             //   ASSASSINATION - ROGUE (MID)    //", Color.DarkGoldenrod);
            Inferno.PrintMessage("             //              V 1.00              //", Color.DarkGoldenrod);
            Inferno.PrintMessage("             //////////////////////////////////////", Color.DarkGoldenrod);
            string addonCmd = Inferno.GetAddonName().Length >= 5 ? Inferno.GetAddonName().Substring(0, 5).ToLower() : Inferno.GetAddonName().ToLower();
            Inferno.PrintMessage("Ready! Use /" + addonCmd + " toggle to pause/resume.", Color.LimeGreen);
            Inferno.PrintMessage("Toggle CDs in-game: /" + addonCmd + " NoCDs", Color.Yellow);
            Inferno.PrintMessage("Force single target: /" + addonCmd + " ForceST", Color.Yellow);
            Inferno.Latency = 250;

            foreach (string s in Abilities) Spellbook.Add(s);
            foreach (string s in TalentChecks) Spellbook.Add(s);
            foreach (string s in DefensiveSpells) Spellbook.Add(s);
            foreach (string s in UtilitySpells) Spellbook.Add(s);

            Macros.Add("use_healthstone", "/use Healthstone");
            Macros.Add("trinket1", "/use 13");
            Macros.Add("trinket2", "/use 14");
            CustomFunctions.Add("HasHealthstone", "return GetItemCount(5512) > 0 and 1 or 0");
            // Racial abilities
            foreach (string r in new string[] { "Berserking", "Blood Fury", "Ancestral Call", "Fireblood", "Lights Judgment" }) Spellbook.Add(r);
            CustomCommands.Add("NoCDs"); CustomCommands.Add("nocds");
            CustomCommands.Add("ForceST"); CustomCommands.Add("forcest");
        }

        public override bool OutOfCombatTick()
        {
            // Slice and Dice if not up
            if (Inferno.BuffRemaining("Slice and Dice") < 5000 && GetCP() >= 1 && Inferno.CanCast("Slice and Dice"))
            { Inferno.Cast("Slice and Dice"); return true; }
            return false;
        }

        public override bool CombatTick()
        {
            if (Inferno.IsDead("player") || Inferno.IsGhost("player")) return false;
            if (HandleDefensives()) return true;
            if (HandleInterrupt()) return true;
            if (!Inferno.UnitCanAttack("player", "target")) return false;
            if (Inferno.IsChanneling("player")) return false;

            // Don't interrupt cast-time trinkets
            string _castName = Inferno.CastingName("player");
            if (_castName.Contains("Puzzle Box") || _castName.Contains("Emberwing")) return false;
                        if (HandleTrinkets()) return true;

            // Racial damage CDs
            if ((HasBuff("Deathmark")) && HandleRacials()) return true;

            int enemies = Inferno.EnemiesNearUnit(10f, "player");
            if (enemies < 1) enemies = 1; if ((Inferno.IsCustomCodeOn("ForceST") || Inferno.IsCustomCodeOn("forcest"))) enemies = 1;
            bool singleTarget = enemies == 1;
            int cp = GetCP();
            int cpMax = GetCPMax();
            bool hasDarkestNight = Inferno.IsSpellKnown("Darkest Night");

            // Thistle Tea edge case
            if (GetEnergyPct() < 50 && Inferno.CanCast("Thistle Tea"))
                if (Cast("Thistle Tea")) return true;

            // Cooldowns
            if (HandleCooldowns(singleTarget)) return true;

            // Core DoTs
            if (HandleCoreDots(cp, singleTarget, enemies)) return true;

            // Generate combo points
            bool needGenerate = (!hasDarkestNight && cp < 5) || (hasDarkestNight && (cpMax - cp) > 0)
                || (Inferno.IsSpellKnown("Crimson Tempest") && enemies >= 5);
            if (needGenerate && HandleGenerate(enemies, cp)) return true;

            // Spend combo points
            bool canSpend = (!hasDarkestNight && cp >= 5) || (hasDarkestNight && (cpMax - cp) == 0);
            if (canSpend && HandleSpend()) return true;

            return false;
        }

        // =====================================================================
        // COOLDOWNS (actions.cds)
        // =====================================================================
        bool HandleCooldowns(bool singleTarget)
        {
            if ((Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;

            // deathmark,if=dot.garrote.ticking&dot.rupture.ticking&cooldown.kingsbane.remains<=2&buff.envenom.up
            if (GetCheckBox("Use Deathmark") && Inferno.DebuffRemaining("Garrote") > GCD() && Inferno.DebuffRemaining("Rupture") > GCD()
                && Inferno.SpellCooldown("Kingsbane") <= 2000 && HasBuff("Envenom"))
                if (Cast("Deathmark")) return true;

            // kingsbane,if=dot.garrote.ticking&dot.rupture.ticking&(dot.deathmark.ticking|cooldown.deathmark.remains>52)
            if (GetCheckBox("Use Kingsbane") && Inferno.DebuffRemaining("Garrote") > GCD() && Inferno.DebuffRemaining("Rupture") > GCD()
                && (HasDebuff("Deathmark") || Inferno.SpellCooldown("Deathmark") > 52000))
                if (Cast("Kingsbane")) return true;

            return false;
        }

        // =====================================================================
        // CORE DOTS (actions.core_dot)
        // =====================================================================
        bool HandleCoreDots(int cp, bool singleTarget, int enemies)
        {
            // Garrote - improved garrote from stealth
            if (HasBuff("Improved Garrote") && Inferno.DebuffRemaining("Garrote") < 14000)
                if (Cast("Garrote")) return true;

            // Garrote - normal maintenance
            if ((cp < 5 || !singleTarget) && Inferno.DebuffRemaining("Garrote") < 5400)
                if (Cast("Garrote")) return true;

            // Rupture - maintenance at 5 CP
            if (cp >= 5 && Inferno.DebuffRemaining("Rupture") < 7200 && (!Inferno.IsSpellKnown("Darkest Night") || Inferno.DebuffRemaining("Rupture") < GCD()))
                if (Cast("Rupture")) return true;

            return false;
        }

        // =====================================================================
        // GENERATE (actions.generate)
        // =====================================================================
        bool HandleGenerate(int enemies, int cp)
        {
            bool singleTarget = enemies == 1;

            // crimson_tempest to spread bleeds in AoE
            if (!singleTarget && Inferno.IsSpellKnown("Crimson Tempest"))
                if (Cast("Crimson Tempest")) return true;

            // shiv for Darkest Night in low cleave
            if (Inferno.IsSpellKnown("Darkest Night") && Inferno.IsSpellKnown("Toxic Stiletto") && (GetCPMax() - cp) == 1 && enemies <= 3)
                if (Cast("Shiv")) return true;

            // ambush on low targets
            int ambushThreshold = 1 + (Inferno.IsSpellKnown("Blindside") ? 1 : 0);
            if (enemies <= ambushThreshold && Cast("Ambush")) return true;

            // mutilate on low targets
            if (enemies <= ambushThreshold && Cast("Mutilate")) return true;

            // fan_of_knives in AoE
            if (enemies > ambushThreshold && Cast("Fan of Knives")) return true;

            return false;
        }

        // =====================================================================
        // SPEND (actions.spend)
        // =====================================================================
        bool HandleSpend()
        {
            // envenom,if=buff.implacable_tracker.stack<4
            if (Inferno.BuffStacks("Implacable Tracker") < 4)
                if (Cast("Envenom")) return true;

            // envenom,if=energy.pct>70
            if (GetEnergyPct() > 70)
                if (Cast("Envenom")) return true;

            return false;
        }

        // =====================================================================
        // DEFENSIVES / INTERRUPT / TRINKETS
        // =====================================================================
        bool HandleDefensives()
        {
            if (!GetCheckBox("Use Defensives")) return false;
            int hpPct = GetPlayerHealthPct();
            if (hpPct <= GetSlider("Evasion HP %") && !HasBuff("Evasion") && Inferno.CanCast("Evasion", IgnoreGCD: true))
            { Inferno.Cast("Evasion", QuickDelay: true); return true; }
            if (hpPct <= 60 && !HasBuff("Feint") && Inferno.CanCast("Feint", IgnoreGCD: true))
            { Inferno.Cast("Feint", QuickDelay: true); return true; }
            if (hpPct <= GetSlider("Crimson Vial HP %") && Inferno.CanCast("Crimson Vial"))
            { Inferno.Cast("Crimson Vial"); return true; }
            if (hpPct <= GetSlider("Healthstone HP %") && Inferno.CustomFunction("HasHealthstone") == 1 && Inferno.ItemCooldown(HealthstoneItemID) == 0)
            { Inferno.Cast("use_healthstone", QuickDelay: true); return true; }
            return false;
        }

        bool HandleInterrupt()
        {
            if (!GetCheckBox("Auto Interrupt")) return false;
            int castingID = Inferno.CastingID("target");
            if (castingID == 0 || !Inferno.IsInterruptable("target")) { _lastCastingID = 0; return false; }
            if (castingID != _lastCastingID) {
                _lastCastingID = castingID;
                int minPct = GetSlider("Interrupt at cast % (min)"); int maxPct = GetSlider("Interrupt at cast % (max)");
                if (maxPct < minPct) maxPct = minPct;
                _interruptTargetPct = _rng.Next(minPct, maxPct + 1);
            }
            int elapsed = Inferno.CastingElapsed("target"); int remaining = Inferno.CastingRemaining("target");
            int total = elapsed + remaining; if (total <= 0) return false;
            int castPct = (elapsed * 100) / total;
            if (castPct >= _interruptTargetPct && Inferno.CanCast("Kick", IgnoreGCD: true))
            { Inferno.Cast("Kick", QuickDelay: true); _lastCastingID = 0; return true; }
            return false;
        }

        bool HandleTrinkets()
        {
            if (!GetCheckBox("Use Trinkets") || (Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;
            if (!HasDebuff("Deathmark") && Inferno.SpellCooldown("Deathmark") > 2000) return false;
            if (Inferno.CanUseEquippedItem(13)) { Inferno.Cast("trinket1"); return true; }
            if (Inferno.CanUseEquippedItem(14)) { Inferno.Cast("trinket2"); return true; }
            return false;
        }

        // =====================================================================
        // HELPERS
        // =====================================================================
        int GCD() { return Inferno.GCD(); }
        int GetCP() { return Inferno.Power("player", 4); }
        int GetCPMax() { return Inferno.MaxPower("player", 4); }
        int GetEnergyPct() { int max = Inferno.MaxPower("player", 3); return max > 0 ? (Inferno.Power("player", 3) * 100) / max : 0; }
        bool HasBuff(string name) { return Inferno.BuffRemaining(name) > GCD(); }
        bool HasDebuff(string name) { return Inferno.DebuffRemaining(name) > GCD(); }

        bool HandleRacials()
        {
            if ((Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;
            if (Inferno.CanCast("Berserking", IgnoreGCD: true)) { Inferno.Cast("Berserking", QuickDelay: true); return true; }
            if (Inferno.CanCast("Blood Fury", IgnoreGCD: true)) { Inferno.Cast("Blood Fury", QuickDelay: true); return true; }
            if (Inferno.CanCast("Ancestral Call", IgnoreGCD: true)) { Inferno.Cast("Ancestral Call", QuickDelay: true); return true; }
            if (Inferno.CanCast("Fireblood", IgnoreGCD: true)) { Inferno.Cast("Fireblood", QuickDelay: true); return true; }
            if (Inferno.CanCast("Lights Judgment")) { Inferno.Cast("Lights Judgment"); return true; }
            return false;
        }
        int GetPlayerHealthPct()
        {
            int hp = Inferno.Health("player"); int maxHp = Inferno.MaxHealth("player");
            if (maxHp < 1) maxHp = 1; return (hp * 100) / maxHp;
        }
        bool Cast(string name)
        {
            if (Inferno.CanCast(name)) { Inferno.Cast(name); Inferno.PrintMessage(">> " + name, Color.White); return true; }
            return false;
        }
    }
}
