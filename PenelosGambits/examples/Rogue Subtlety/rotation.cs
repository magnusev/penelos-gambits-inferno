using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API;

namespace InfernoWow.Modules
{
    /// <summary>
    /// Subtlety Rogue - Translated from SimulationCraft Midnight APL
    /// Core: Shadow Dance windows, Shadow Blades, Secret Technique,
    /// Shadowstrike/Gloomblade builders → Eviscerate/Black Powder finishers.
    /// </summary>
    public class SubtletyRogueRotation : Rotation
    {
        List<string> Abilities = new List<string> {
            "Backstab", "Gloomblade", "Shadowstrike", "Shuriken Storm",
            "Eviscerate", "Black Powder", "Secret Technique",
            "Shadow Dance", "Shadow Blades", "Vanish",
            "Goremaw's Bite", "Coup de Grace",
        };
        List<string> TalentChecks = new List<string> {
            "Deathstalker's Mark", "Unseen Blade", "Danse Macabre",
            "Darkest Night", "Potent Powder", "Premeditation",
        };
        List<string> DefensiveSpells = new List<string> { "Evasion", "Feint", "Cloak of Shadows", "Crimson Vial" };
        List<string> UtilitySpells = new List<string> { "Kick" };

        const int HealthstoneItemID = 5512;
        const int ComboPointType = 4;
        const int EnergyPowerType = 3;
        private Random _rng = new Random();
        private int _lastCastingID = 0;
        private int _interruptTargetPct = 0;

        public override void LoadSettings()
        {
            Settings.Add(new Setting("=== Subtlety Rogue ==="));
            Settings.Add(new Setting("Use Shadow Blades", true));
            Settings.Add(new Setting("Use Shadow Dance", true));
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
            Inferno.PrintMessage("             //////////////////////////////////////", Color.DarkSlateGray);
            Inferno.PrintMessage("             //    SUBTLETY - ROGUE (MID)        //", Color.DarkSlateGray);
            Inferno.PrintMessage("             //              V 1.00              //", Color.DarkSlateGray);
            Inferno.PrintMessage("             //////////////////////////////////////", Color.DarkSlateGray);
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

        public override bool OutOfCombatTick() { return false; }

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
            if ((HasBuff("Shadow Dance") || HasBuff("Shadow Blades")) && HandleRacials()) return true;

            int enemies = Inferno.EnemiesNearUnit(8f, "player");
            if (enemies < 1) enemies = 1; if ((Inferno.IsCustomCodeOn("ForceST") || Inferno.IsCustomCodeOn("forcest"))) enemies = 1;
            int cp = GetCP();
            int cpMax = GetCPMax();
            bool inStealth = HasBuff("Shadow Dance") || HasBuff("Stealth") || HasBuff("Vanish");
            bool hasDarkestNight = Inferno.IsSpellKnown("Darkest Night");
            int finishThreshold = cpMax - (hasDarkestNight ? 0 : 1);

            // Cooldowns
            if (HandleCooldowns(enemies, cp, inStealth)) return true;

            // Finish at threshold
            if (cp >= finishThreshold && HandleFinish(enemies, inStealth)) return true;

            // Build when in stealth or energy > 60
            if ((inStealth || Inferno.Power("player", 3) > 60) && HandleBuild(enemies, inStealth)) return true;

            return false;
        }

        // =====================================================================
        // COOLDOWNS (actions.cds)
        // =====================================================================
        bool HandleCooldowns(int enemies, int cp, bool inStealth)
        {
            if ((Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;

            // shadow_blades when dance and secret technique ready
            if (GetCheckBox("Use Shadow Blades") && Inferno.SpellCooldown("Shadow Dance") <= GCDMAX() && Inferno.SpellCooldown("Secret Technique") <= GCDMAX())
                if (Cast("Shadow Blades")) return true;

            // shadow_dance when not in stealth, energy >= 30, and Secret Technique or Darkest Night ready
            if (GetCheckBox("Use Shadow Dance") && !inStealth && Inferno.Power("player", 3) >= 30)
            {
                bool stReady = Inferno.SpellCooldown("Secret Technique") <= GCDMAX() || HasBuff("Darkest Night");
                bool sbOnCD = Inferno.SpellCooldown("Shadow Blades") >= 9000;
                bool sbUp = HasBuff("Shadow Blades");
                if (stReady && (sbOnCD || sbUp))
                    if (Cast("Shadow Dance")) return true;
            }

            // vanish when not in stealth, energy >= 50, low CP
            if (!inStealth && Inferno.Power("player", 3) >= 50 && cp <= 1 && !HasBuff("Subterfuge"))
                if (Cast("Vanish")) return true;

            return false;
        }

        // =====================================================================
        // FINISH (actions.finish)
        // =====================================================================
        bool HandleFinish(int enemies, bool inStealth)
        {
            bool inDance = HasBuff("Shadow Dance");

            // secret_technique,if=buff.shadow_dance.up
            if (inDance && Cast("Secret Technique")) return true;
            // secret_technique if CD < 18 and dance not ready
            if (Inferno.SpellCooldown("Secret Technique") < 18000 && Inferno.SpellCooldown("Shadow Dance") > GCD())
                if (Cast("Secret Technique")) return true;

            // eviscerate,if=buff.darkest_night.up
            if (HasBuff("Darkest Night") && Cast("Eviscerate")) return true;

            // coup_de_grace,if=cooldown.secret_technique.remains>=3|buff.shadow_dance.up
            if (Inferno.SpellCooldown("Secret Technique") >= 3000 || inDance)
                if (Cast("Coup de Grace")) return true;

            // black_powder,if=targets>=3-talent.potent_powder
            int bpThreshold = 3 - (Inferno.IsSpellKnown("Potent Powder") ? 1 : 0);
            if (enemies >= bpThreshold && Cast("Black Powder")) return true;

            // eviscerate
            if (Inferno.SpellCooldown("Secret Technique") >= 3000 || inDance || HasBuff("Shadow Blades") || Inferno.IsSpellKnown("Deathstalker's Mark"))
                if (Cast("Eviscerate")) return true;

            return false;
        }

        // =====================================================================
        // BUILD (actions.build)
        // =====================================================================
        bool HandleBuild(int enemies, bool inStealth)
        {
            // shuriken_storm after shadow_dance with premeditation
            if (HasBuff("Premeditation") && Inferno.IsSpellKnown("Danse Macabre") && HasBuff("Shadow Dance"))
                if (Cast("Shuriken Storm")) return true;

            // shadowstrike with deathstalker's mark or low targets
            if (inStealth)
            {
                if (Inferno.IsSpellKnown("Deathstalker's Mark") && Inferno.DebuffRemaining("Deathstalker's Mark") < GCD() && !HasBuff("Darkest Night"))
                    if (Cast("Shadowstrike")) return true;
                if (enemies <= 3)
                    if (Cast("Shadowstrike")) return true;
            }

            // shuriken_storm in AoE
            if (enemies > 1 && Cast("Shuriken Storm")) return true;

            // goremaws_bite,if=combo_points.deficit>=3
            if ((GetCPMax() - GetCP()) >= 3 && Cast("Goremaw's Bite")) return true;

            // gloomblade / backstab in ST
            if (enemies < 2)
            {
                if (Cast("Gloomblade")) return true;
                if (Cast("Backstab")) return true;
            }

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
            if (!HasBuff("Shadow Blades")) return false;
            if (Inferno.CanUseEquippedItem(13)) { Inferno.Cast("trinket1"); return true; }
            if (Inferno.CanUseEquippedItem(14)) { Inferno.Cast("trinket2"); return true; }
            return false;
        }

        // =====================================================================
        // HELPERS
        // =====================================================================
        int GCD() { return Inferno.GCD(); }
        int GCDMAX() { int g = (int)(1500f / (1f + Inferno.Haste("player") / 100f)); return g < 750 ? 750 : g; }
        int GetCP() { return Inferno.Power("player", 4); }
        int GetCPMax() { return Inferno.MaxPower("player", 4); }
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
