using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API;

namespace InfernoWow.Modules
{
    /// <summary>
    /// Outlaw Rogue - Translated from SimulationCraft Midnight APL
    /// Core: Roll the Bones management, Blade Flurry for AoE,
    /// Adrenaline Rush, Between the Eyes, Pistol Shot/Ambush procs.
    /// </summary>
    public class OutlawRogueRotation : Rotation
    {
        List<string> Abilities = new List<string> {
            "Sinister Strike", "Ambush", "Pistol Shot", "Dispatch",
            "Between the Eyes", "Roll the Bones", "Blade Flurry",
            "Adrenaline Rush", "Blade Rush", "Killing Spree",
            "Coup de Grace", "Keep it Rolling", "Slice and Dice",
            "Preparation", "Vanish",
        };
        List<string> TalentChecks = new List<string> {
            "Hidden Opportunity", "Improved Ambush", "Fan the Hammer",
            "Quick Draw", "Audacity", "Deft Maneuvers",
            "Improved Adrenaline Rush", "Deal Fate", "Supercharger",
            "Zero In", "Preparation",
        };
        List<string> DefensiveSpells = new List<string> { "Evasion", "Feint", "Cloak of Shadows", "Crimson Vial" };
        List<string> UtilitySpells = new List<string> { "Kick" };

        const int HealthstoneItemID = 5512;
        const int ComboPointType = 4;
        private Random _rng = new Random();
        private int _lastCastingID = 0;
        private int _interruptTargetPct = 0;

        public override void LoadSettings()
        {
            Settings.Add(new Setting("=== Outlaw Rogue ==="));
            Settings.Add(new Setting("Use Adrenaline Rush", true));
            Settings.Add(new Setting("Use Killing Spree", true));
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
            Inferno.PrintMessage("             //////////////////////////////////////", Color.DarkOliveGreen);
            Inferno.PrintMessage("             //      OUTLAW - ROGUE (MID)        //", Color.DarkOliveGreen);
            Inferno.PrintMessage("             //              V 1.00              //", Color.DarkOliveGreen);
            Inferno.PrintMessage("             //////////////////////////////////////", Color.DarkOliveGreen);
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
            if (Inferno.BuffRemaining("Slice and Dice") < 5000 && Inferno.Power("player", 4) >= 1 && Inferno.CanCast("Slice and Dice"))
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

            int enemies = Inferno.EnemiesNearUnit(8f, "player");
            if (enemies < 1) enemies = 1; if ((Inferno.IsCustomCodeOn("ForceST") || Inferno.IsCustomCodeOn("forcest"))) enemies = 1;
            int cp = GetCP();
            int cpMax = GetCPMax();
            bool finishCondition = cp >= cpMax - 1;

            // Cooldowns
            if ((HasBuff("Adrenaline Rush")) && HandleRacials()) return true;
            if (HandleCooldowns(enemies, finishCondition)) return true;

            // Finishers
            if (finishCondition && HandleFinish(enemies)) return true;

            // Builders
            if (HandleBuild(enemies, cp)) return true;

            return false;
        }

        // =====================================================================
        // COOLDOWNS (actions.cds)
        // =====================================================================
        bool HandleCooldowns(int enemies, bool finishCondition)
        {
            if ((Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;

            // adrenaline_rush
            if (GetCheckBox("Use Adrenaline Rush") && !HasBuff("Adrenaline Rush"))
                if (Cast("Adrenaline Rush")) return true;

            // blade_flurry at 2+ targets
            if (enemies >= 2 && Inferno.BuffRemaining("Blade Flurry") < GCD())
                if (Cast("Blade Flurry")) return true;

            // roll_the_bones if not up or reroll at 1 buff
            int rtbCount = GetRtBBuffCount();
            if (rtbCount == 0 || rtbCount == 1)
                if (Cast("Roll the Bones")) return true;

            // keep_it_rolling at 2+ RtB buffs
            if (rtbCount >= 2 && Inferno.BuffRemaining("Roll the Bones") < Inferno.SpellCooldown("Adrenaline Rush") && !HasBuff("Loaded Dice"))
                if (Cast("Keep it Rolling")) return true;
            if (rtbCount >= 3 && Cast("Keep it Rolling")) return true;

            // blade_rush
            if (Cast("Blade Rush")) return true;

            // Trinkets during BtE
            if (GetCheckBox("Use Trinkets") && HasBuff("Between the Eyes"))
            {
                if (Inferno.CanUseEquippedItem(13)) { Inferno.Cast("trinket1"); return true; }
                if (Inferno.CanUseEquippedItem(14)) { Inferno.Cast("trinket2"); return true; }
            }

            return false;
        }

        // =====================================================================
        // FINISH (actions.finish)
        // =====================================================================
        bool HandleFinish(int enemies)
        {
            // Slice and Dice if not up
            if (Inferno.BuffRemaining("Slice and Dice") < GCD() && Cast("Dispatch")) return true;

            // between_the_eyes
            if (Inferno.SpellCooldown("Adrenaline Rush") > 30000 || HasBuff("Adrenaline Rush")
                || !Inferno.IsSpellKnown("Supercharger") || !Inferno.IsSpellKnown("Zero In"))
                if (Cast("Between the Eyes")) return true;

            // killing_spree
            if (GetCheckBox("Use Killing Spree") && Cast("Killing Spree")) return true;
            // pool_resource,for_next=1 — if KS is almost ready, wait for it
            if (GetCheckBox("Use Killing Spree") && Inferno.SpellCooldown("Killing Spree") > 0 && Inferno.SpellCooldown("Killing Spree") < 3000)
                return true;

            // coup_de_grace
            if (Cast("Coup de Grace")) return true;

            // dispatch
            if (Cast("Dispatch")) return true;

            return false;
        }

        // =====================================================================
        // BUILD (actions.build)
        // =====================================================================
        bool HandleBuild(int enemies, int cp)
        {
            // ambush with audacity proc
            if (Inferno.IsSpellKnown("Hidden Opportunity") && HasBuff("Audacity"))
                if (Cast("Ambush")) return true;

            // blade_flurry for Deft Maneuvers at 4+ targets
            if (Inferno.IsSpellKnown("Deft Maneuvers") && enemies >= 4)
                if (Cast("Blade Flurry")) return true;

            // coup_de_grace with disorienting strikes
            if (HasBuff("Disorienting Strikes") && Cast("Coup de Grace")) return true;

            // pistol_shot with opportunity (Fan the Hammer or standard)
            if (HasBuff("Opportunity"))
            {
                if (Inferno.IsSpellKnown("Audacity") && Inferno.IsSpellKnown("Hidden Opportunity") && !HasBuff("Audacity"))
                    if (Cast("Pistol Shot")) return true;
                if (Inferno.IsSpellKnown("Fan the Hammer") && (Inferno.BuffStacks("Opportunity") >= 6 || Inferno.BuffRemaining("Opportunity") < 2000))
                    if (Cast("Pistol Shot")) return true;
                if (Inferno.IsSpellKnown("Fan the Hammer") && (GetCPMax() - cp) >= 2)
                    if (Cast("Pistol Shot")) return true;
                if (!Inferno.IsSpellKnown("Fan the Hammer"))
                    if (Cast("Pistol Shot")) return true;
            }

            // pool_resource,for_next=1 — pool energy for Ambush
            if (Inferno.IsSpellKnown("Hidden Opportunity") && Inferno.CanCast("Ambush"))
            { Inferno.Cast("Ambush"); Inferno.PrintMessage(">> Ambush", Color.White); return true; }
            if (Inferno.IsSpellKnown("Hidden Opportunity") && Inferno.Power("player", 3) < 50)
                return true; // pool energy for Ambush

            // sinister_strike
            if (Cast("Sinister Strike")) return true;

            return false;
        }

        // =====================================================================
        // DEFENSIVES / INTERRUPT
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

        // =====================================================================
        // HELPERS
        // =====================================================================
        int GCD() { return Inferno.GCD(); }
        int GetCP() { return Inferno.Power("player", 4); }
        int GetCPMax() { return Inferno.MaxPower("player", 4); }
        int GetRtBBuffCount()
        {
            int c = 0;
            if (Inferno.BuffRemaining("Broadside") > 0) c++;
            if (Inferno.BuffRemaining("Buried Treasure") > 0) c++;
            if (Inferno.BuffRemaining("Grand Melee") > 0) c++;
            if (Inferno.BuffRemaining("Ruthless Precision") > 0) c++;
            if (Inferno.BuffRemaining("Skull and Crossbones") > 0) c++;
            if (Inferno.BuffRemaining("True Bearing") > 0) c++;
            return c;
        }
        bool HasBuff(string name) { return Inferno.BuffRemaining(name) > GCD(); }

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
