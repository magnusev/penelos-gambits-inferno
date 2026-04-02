using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API;

namespace InfernoWow.Modules
{
    /// <summary>
    /// Frost Mage - Translated from SimulationCraft Midnight APL
    /// Auto-detects hero tree: Frostfire (Frostfire Bolt) or Spellslinger (Splinterstorm).
    /// Each has ST and AoE (3+) sub-rotations.
    /// </summary>
    public class FrostMageRotation : Rotation
    {
        List<string> Abilities = new List<string> {
            "Frostbolt", "Ice Lance", "Flurry", "Frozen Orb",
            "Blizzard", "Comet Storm", "Glacial Spike", "Ray of Frost",
            "Ice Nova", "Cone of Cold",
        };
        List<string> TalentChecks = new List<string> {
            "Frostfire Bolt", "Splinterstorm", "Freezing Rain",
            "Freezing Winds", "Cone of Frost", "Thermal Void",
        };
        List<string> DefensiveSpells = new List<string> { "Ice Barrier", "Ice Block" };
        List<string> UtilitySpells = new List<string> { "Counterspell", "Arcane Intellect" };

        const int HealthstoneItemID = 5512;
        private Random _rng = new Random();
        private int _lastCastingID = 0;
        private int _interruptTargetPct = 0;

        public override void LoadSettings()
        {
            Settings.Add(new Setting("=== Frost Mage ==="));
            Settings.Add(new Setting("Use Frozen Orb", true));
            Settings.Add(new Setting("Hero tree auto-detected: Frostfire / Spellslinger"));
            Settings.Add(new Setting("Use Trinkets", true));
            Settings.Add(new Setting("=== Defensives ==="));
            Settings.Add(new Setting("Use Defensives", true));
            Settings.Add(new Setting("Ice Barrier HP %", 1, 100, 80));
            Settings.Add(new Setting("Ice Block HP %", 1, 100, 15));
            Settings.Add(new Setting("Healthstone HP %", 1, 100, 50));
            Settings.Add(new Setting("=== Interrupt ==="));
            Settings.Add(new Setting("Auto Interrupt", true));
            Settings.Add(new Setting("Interrupt at cast % (min)", 0, 100, 40));
            Settings.Add(new Setting("Interrupt at cast % (max)", 0, 100, 90));
            Settings.Add(new Setting("=== Utility ==="));
            Settings.Add(new Setting("Auto Arcane Intellect", true));
        }

        public override void Initialize()
        {
            Inferno.PrintMessage("             //////////////////////////////////////", Color.CornflowerBlue);
            Inferno.PrintMessage("             //       FROST - MAGE (MID)         //", Color.CornflowerBlue);
            Inferno.PrintMessage("             //    FROSTFIRE / SPELLSLINGER       //", Color.CornflowerBlue);
            Inferno.PrintMessage("             //              V 1.00              //", Color.CornflowerBlue);
            Inferno.PrintMessage("             //////////////////////////////////////", Color.CornflowerBlue);
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
            if (HandleArcaneIntellect()) return true;
            return false;
        }

        public override bool CombatTick()
        {
            if (Inferno.IsDead("player") || Inferno.IsGhost("player")) return false;
            if (HandleDefensives()) return true;
            if (HandleInterrupt()) return true;
            if (HandleArcaneIntellect()) return true;

            // Don't interrupt Ray of Frost or Blizzard
            if (Inferno.IsChanneling("player")) return false;

            // Don't interrupt cast-time trinkets
            string _castName = Inferno.CastingName("player");
            if (_castName.Contains("Puzzle Box") || _castName.Contains("Emberwing")) return false;

            if (!Inferno.UnitCanAttack("player", "target")) return false;
                        if (HandleTrinkets()) return true;

            // Racial damage CDs
            if (HandleRacials()) return true;

            int enemies = Inferno.EnemiesNearUnit(10f, "target");
            if (enemies < 1) enemies = 1; if ((Inferno.IsCustomCodeOn("ForceST") || Inferno.IsCustomCodeOn("forcest"))) enemies = 1;

            bool isFF = Inferno.IsSpellKnown("Frostfire Bolt");
            bool isSS = Inferno.IsSpellKnown("Splinterstorm");

            // Movement: use instants while moving
            if (Inferno.IsMoving("player"))
            {
                if (HasBuff("Freezing Rain") && Cast("Blizzard")) return true;
                if (Inferno.IsSpellKnown("Cone of Frost")) { if (Cast("Ice Nova")) return true; if (Cast("Cone of Cold")) return true; }
                if (Cast("Ice Lance")) return true;
            }

            // APL routing (lines 19-22)
            if (isFF)
            {
                if (enemies >= 3) return FFAoE(enemies);
                return FFST(enemies);
            }
            // Spellslinger or fallback
            if (enemies >= 3) return SSAoE(enemies);
            return SSST(enemies);
        }

        // =====================================================================
        // FROSTFIRE AoE (actions.ff_aoe)
        // =====================================================================
        bool FFAoE(int enemies)
        {
            // blizzard,if=buff.freezing_rain.up
            if (HasBuff("Freezing Rain") && Cast("Blizzard")) return true;
            // flurry,if=buff.brain_freeze.react&buff.thermal_void.down
            if (HasBuff("Brain Freeze") && !HasBuff("Thermal Void") && Cast("Flurry")) return true;
            // frozen_orb
            if (!(Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds")) && GetCheckBox("Use Frozen Orb") && Cast("Frozen Orb")) return true;
            // glacial_spike
            if (Cast("Glacial Spike")) return true;
            // comet_storm
            if (Cast("Comet Storm")) return true;
            // blizzard,if=active_enemies>=(5-talent.freezing_rain-talent.freezing_winds)
            int blizzThreshold = 5 - (Inferno.IsSpellKnown("Freezing Rain") ? 1 : 0) - (Inferno.IsSpellKnown("Freezing Winds") ? 1 : 0);
            if (enemies >= blizzThreshold && Cast("Blizzard")) return true;
            // ice_lance,if=buff.fingers_of_frost.react
            if (HasBuff("Fingers of Frost") && Cast("Ice Lance")) return true;
            // ice_lance,if=debuff.freezing.stack>=10
            if (GetFreezingStacks() >= 10 && Cast("Ice Lance")) return true;
            // flurry
            if (Cast("Flurry")) return true;
            // ray_of_frost,if=!buff.frostfire_empowerment.react
            if (!HasBuff("Frostfire Empowerment") && Cast("Ray of Frost")) return true;
            // frostbolt
            if (Cast("Frostbolt")) return true;
            // movement fallbacks
            if (Cast("Ice Lance")) return true;
            return false;
        }

        // =====================================================================
        // FROSTFIRE ST (actions.ff_st)
        // =====================================================================
        bool FFST(int enemies)
        {
            // flurry,if=buff.brain_freeze.react&buff.thermal_void.down
            if (HasBuff("Brain Freeze") && !HasBuff("Thermal Void") && Cast("Flurry")) return true;
            // frozen_orb
            if (!(Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds")) && GetCheckBox("Use Frozen Orb") && Cast("Frozen Orb")) return true;
            // glacial_spike
            if (Cast("Glacial Spike")) return true;
            // comet_storm
            if (Cast("Comet Storm")) return true;
            // ice_lance,if=buff.fingers_of_frost.react
            if (HasBuff("Fingers of Frost") && Cast("Ice Lance")) return true;
            // ice_lance,if=debuff.freezing.stack>=10
            if (GetFreezingStacks() >= 10 && Cast("Ice Lance")) return true;
            // flurry
            if (Cast("Flurry")) return true;
            // ray_of_frost,if=active_enemies=1|!buff.frostfire_empowerment.react
            if (enemies == 1 || !HasBuff("Frostfire Empowerment"))
                if (Cast("Ray of Frost")) return true;
            // frostbolt
            if (Cast("Frostbolt")) return true;
            if (Cast("Ice Lance")) return true;
            return false;
        }

        // =====================================================================
        // SPELLSLINGER AoE (actions.ss_aoe)
        // =====================================================================
        bool SSAoE(int enemies)
        {
            // comet_storm,if=buff.splinterstorm.down
            if (!HasBuff("Splinterstorm") && Cast("Comet Storm")) return true;
            // blizzard,if=buff.freezing_rain.up
            if (HasBuff("Freezing Rain") && Cast("Blizzard")) return true;
            // flurry,if=buff.brain_freeze.react&buff.thermal_void.down
            if (HasBuff("Brain Freeze") && !HasBuff("Thermal Void") && Cast("Flurry")) return true;
            // frozen_orb
            if (!(Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds")) && GetCheckBox("Use Frozen Orb") && Cast("Frozen Orb")) return true;
            // ice_lance,if=buff.fingers_of_frost.react
            if (HasBuff("Fingers of Frost") && Cast("Ice Lance")) return true;
            // glacial_spike
            if (Cast("Glacial Spike")) return true;
            // ice_lance,if=debuff.freezing.react>=6
            if (GetFreezingStacks() >= 6 && Cast("Ice Lance")) return true;
            // ice_nova,if=talent.cone_of_frost&active_enemies>=4
            if (Inferno.IsSpellKnown("Cone of Frost") && enemies >= 4 && Cast("Ice Nova")) return true;
            // cone_of_cold,if=talent.cone_of_frost&active_enemies>=4
            if (Inferno.IsSpellKnown("Cone of Frost") && enemies >= 4 && Cast("Cone of Cold")) return true;
            // blizzard,if=active_enemies>=5&talent.freezing_winds&talent.freezing_rain
            if (enemies >= 5 && Inferno.IsSpellKnown("Freezing Winds") && Inferno.IsSpellKnown("Freezing Rain"))
                if (Cast("Blizzard")) return true;
            // flurry
            if (Cast("Flurry")) return true;
            // ray_of_frost
            if (Cast("Ray of Frost")) return true;
            // frostbolt
            if (Cast("Frostbolt")) return true;
            if (Cast("Ice Lance")) return true;
            return false;
        }

        // =====================================================================
        // SPELLSLINGER ST (actions.ss_st)
        // =====================================================================
        bool SSST(int enemies)
        {
            // comet_storm,if=buff.splinterstorm.down
            if (!HasBuff("Splinterstorm") && Cast("Comet Storm")) return true;
            // flurry,if=buff.brain_freeze.react&buff.thermal_void.down
            if (HasBuff("Brain Freeze") && !HasBuff("Thermal Void") && Cast("Flurry")) return true;
            // frozen_orb
            if (!(Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds")) && GetCheckBox("Use Frozen Orb") && Cast("Frozen Orb")) return true;
            // ice_lance,if=buff.fingers_of_frost.react
            if (HasBuff("Fingers of Frost") && Cast("Ice Lance")) return true;
            // glacial_spike
            if (Cast("Glacial Spike")) return true;
            // ice_lance,if=debuff.freezing.react>=6
            if (GetFreezingStacks() >= 6 && Cast("Ice Lance")) return true;
            // ray_of_frost
            if (Cast("Ray of Frost")) return true;
            // flurry
            if (Cast("Flurry")) return true;
            // frostbolt
            if (Cast("Frostbolt")) return true;
            if (Cast("Ice Lance")) return true;
            return false;
        }

        // =====================================================================
        // DEFENSIVES / INTERRUPT / UTILITY / TRINKETS
        // =====================================================================
        bool HandleDefensives()
        {
            if (!GetCheckBox("Use Defensives")) return false;
            int hpPct = GetPlayerHealthPct();
            if (hpPct <= GetSlider("Ice Block HP %") && Inferno.CanCast("Ice Block", IgnoreGCD: true))
            { Inferno.Cast("Ice Block", QuickDelay: true); return true; }
            if (hpPct <= GetSlider("Ice Barrier HP %") && Inferno.BuffRemaining("Ice Barrier") < GCD() && Inferno.CanCast("Ice Barrier", IgnoreGCD: true))
            { Inferno.Cast("Ice Barrier", QuickDelay: true); return true; }
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
            if (castPct >= _interruptTargetPct && Inferno.CanCast("Counterspell", IgnoreGCD: true))
            { Inferno.Cast("Counterspell", QuickDelay: true); _lastCastingID = 0; return true; }
            return false;
        }

        bool HandleArcaneIntellect()
        {
            if (!GetCheckBox("Auto Arcane Intellect")) return false;
            if (Inferno.BuffRemaining("Arcane Intellect") < GCD() && Inferno.CanCast("Arcane Intellect"))
            { Inferno.Cast("Arcane Intellect"); return true; }
            return false;
        }

        bool HandleTrinkets()
        {
            if (!GetCheckBox("Use Trinkets") || (Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;
            if (Inferno.CanUseEquippedItem(13)) { Inferno.Cast("trinket1"); return true; }
            if (Inferno.CanUseEquippedItem(14)) { Inferno.Cast("trinket2"); return true; }
            return false;
        }

        // =====================================================================
        // HELPERS
        // =====================================================================
        int GCD() { return Inferno.GCD(); }
        bool HasBuff(string name) { return Inferno.BuffRemaining(name) > GCD(); }
        int GetFreezingStacks()
        {
            int stacks = Inferno.DebuffStacks("Freezing");
            // Predict +1 stack if currently casting Frostbolt or Frostfire Bolt
            string casting = Inferno.CastingName("player");
            if (casting == "Frostbolt" || casting == "Frostfire Bolt") stacks += 1;
            return stacks;
        }

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
