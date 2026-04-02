using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API;

namespace InfernoWow.Modules
{
    /// <summary>
    /// Fire Mage - Translated from SimulationCraft Midnight APL
    /// Auto-detects hero tree: Frostfire (Frostfire Bolt) or Sunfury (Spellfire Spheres).
    /// Combustion window management with Hot Streak! / Pyroclasm tracking.
    /// Fire Blast weaving handled via off-GCD casts.
    /// </summary>
    public class FireMageRotation : Rotation
    {
        List<string> Abilities = new List<string> {
            "Fireball", "Pyroblast", "Fire Blast", "Flamestrike",
            "Scorch", "Combustion", "Meteor", "Mirror Image",
            "Phoenix Flames", "Dragon's Breath","Frostfire Bolt"
        };
        List<string> TalentChecks = new List<string> {
            "Frostfire Bolt", "Spellfire Spheres", "Firestarter",
            "Fuel the Fire", "Pyroclasm", "Burnout", "Blast Zone",
            "Scald", "Sunfury Execution", "Spontaneous Combustion",
            "Savor the Moment", "Hyperthermia",
        };
        List<string> DefensiveSpells = new List<string> { "Ice Block", "Blazing Barrier" };
        List<string> UtilitySpells = new List<string> { "Counterspell", "Arcane Intellect" };

        const int HealthstoneItemID = 5512;
        private Random _rng = new Random();
        private int _lastCastingID = 0;
        private int _interruptTargetPct = 0;

        public override void LoadSettings()
        {
            Settings.Add(new Setting("=== Fire Mage ==="));
            Settings.Add(new Setting("Hero tree auto-detected: Frostfire / Sunfury"));
            Settings.Add(new Setting("Use Combustion", true));
            Settings.Add(new Setting("Use Meteor", true));
            Settings.Add(new Setting("Use Trinkets", true));
            Settings.Add(new Setting("AoE Flamestrike threshold", 2, 10, 4));
            Settings.Add(new Setting("=== Defensives ==="));
            Settings.Add(new Setting("Use Defensives", true));
            Settings.Add(new Setting("Blazing Barrier HP %", 1, 100, 80));
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
            Inferno.PrintMessage("             //////////////////////////////////////", Color.OrangeRed);
            Inferno.PrintMessage("             //        FIRE - MAGE (MID)         //", Color.OrangeRed);
            Inferno.PrintMessage("             //      FROSTFIRE / SUNFURY         //", Color.OrangeRed);
            Inferno.PrintMessage("             //              V 1.00              //", Color.OrangeRed);
            Inferno.PrintMessage("             //////////////////////////////////////", Color.OrangeRed);
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
            if (!Inferno.UnitCanAttack("player", "target")) return false;
            if (Inferno.IsChanneling("player")) return false;

            // Don't interrupt cast-time trinkets
            string _castName = Inferno.CastingName("player");
            if (_castName.Contains("Puzzle Box") || _castName.Contains("Emberwing")) return false;
                        if (HandleTrinkets()) return true;

            // Racial damage CDs
            if ((HasBuff("Combustion")) && HandleRacials()) return true;

            int enemies = Inferno.EnemiesNearUnit(10f, "target");
            if (enemies < 1) enemies = 1; if ((Inferno.IsCustomCodeOn("ForceST") || Inferno.IsCustomCodeOn("forcest"))) enemies = 1;
            bool isFF = Inferno.IsSpellKnown("Frostfire Bolt");
            bool inCombustion = HasBuff("Combustion");

            // Fire Blast weaving - off-GCD during casts
            if (HandleFireBlast(enemies, inCombustion)) return true;

            // APL routing: Combustion window vs Filler
            if (inCombustion || Inferno.SpellCooldown("Combustion") <= GCDMAX())
            {
                if (isFF) return FFCombustion(enemies);
                return SFCombustion(enemies);
            }

            if (isFF) return FFFiller(enemies);
            return SFFiller(enemies);
        }

        // =====================================================================
        // FROSTFIRE COMBUSTION (actions.ff_combustion)
        // =====================================================================
        bool FFCombustion(int enemies)
        {
            int fsThreshold = GetSlider("AoE Flamestrike threshold");

            // Combustion activation
            if (!HasBuff("Combustion") && CastCD("Combustion")) return true;

            // Meteor
            if (GetCheckBox("Use Meteor") && Cast("Meteor")) return true;

            // Hot Streak! spender
            if (HasBuff("Hot Streak!"))
            {
                if (Inferno.IsSpellKnown("Fuel the Fire") && enemies >= fsThreshold)
                    if (Cast("Flamestrike")) return true;
                if (Cast("Pyroblast")) return true;
            }

            // Pyroclasm spender (if cast finishes before Combustion ends)
            if (HasBuff("Pyroclasm") && !HasBuff("Hot Streak!"))
            {
                if (Cast("Pyroblast")) return true;
            }

            // Scorch with Heat Shimmer or execute
            if (HasBuff("Heat Shimmer") && Cast("Scorch")) return true;
            if (Inferno.IsSpellKnown("Scald") && GetTargetHealthPct() < 30 && Cast("Scorch")) return true;

            // Fireball filler during combustion
            if (Cast("Fireball")) return true;
            if (Cast("Frostfire Bolt")) return true;
            return false;
        }

        // =====================================================================
        // SUNFURY COMBUSTION (actions.sf_combustion)
        // =====================================================================
        bool SFCombustion(int enemies)
        {
            int fsThreshold = GetSlider("AoE Flamestrike threshold");

            // Combustion activation
            if (!HasBuff("Combustion") && CastCD("Combustion")) return true;

            // Meteor
            if (GetCheckBox("Use Meteor") && Cast("Meteor")) return true;

            // Pyroclasm precast (when no Hot Streak!)
            if (HasBuff("Pyroclasm") && !HasBuff("Hot Streak!") && !HasBuff("Combustion"))
            {
                if (Cast("Pyroblast")) return true;
            }

            // Hot Streak! spender
            if (HasBuff("Hot Streak!"))
            {
                if (Inferno.IsSpellKnown("Fuel the Fire") && enemies >= fsThreshold)
                    if (Cast("Flamestrike")) return true;
                if (Cast("Pyroblast")) return true;
            }

            // Pyroclasm during Combustion (if cast time < remaining)
            if (HasBuff("Pyroclasm") && !HasBuff("Hot Streak!"))
            {
                if (Cast("Pyroblast")) return true;
            }

            // Scorch filler in combustion
            if (Cast("Scorch")) return true;
            if (Cast("Fireball")) return true;
            return false;
        }

        // =====================================================================
        // FROSTFIRE FILLER (actions.ff_filler)
        // =====================================================================
        bool FFFiller(int enemies)
        {
            int fsThreshold = GetSlider("AoE Flamestrike threshold");

            // Meteor on CD
            if (GetCheckBox("Use Meteor") && Cast("Meteor")) return true;

            // Hot Streak! spender
            if (HasBuff("Hot Streak!"))
            {
                if (Inferno.IsSpellKnown("Fuel the Fire") && enemies >= fsThreshold)
                    if (Cast("Flamestrike")) return true;
                if (Cast("Pyroblast")) return true;
            }

            // Pyroclasm - spend at 2 stacks or if Combustion far away
            if (HasBuff("Pyroclasm") && (Inferno.BuffStacks("Pyroclasm") >= 2 || Inferno.SpellCooldown("Combustion") > 12000))
            {
                if (Inferno.IsSpellKnown("Fuel the Fire") && enemies >= fsThreshold)
                    if (Cast("Flamestrike")) return true;
                if (Cast("Pyroblast")) return true;
            }

            // Scorch with Heat Shimmer
            if (HasBuff("Heat Shimmer") && Cast("Scorch")) return true;

            // Fireball filler
            if (Cast("Fireball")) return true;
            if (Cast("Frostfire Bolt")) return true;
            return false;
        }

        // =====================================================================
        // SUNFURY FILLER (actions.sf_filler)
        // =====================================================================
        bool SFFiller(int enemies)
        {
            int fsThreshold = GetSlider("AoE Flamestrike threshold") - 1; // SF uses 3 target threshold

            // Hot Streak! spender
            if (HasBuff("Hot Streak!") || HasBuff("Hyperthermia"))
            {
                if (Inferno.IsSpellKnown("Fuel the Fire") && enemies >= fsThreshold)
                    if (Cast("Flamestrike")) return true;
                if (Cast("Pyroblast")) return true;
            }

            // Pyroclasm - spend at 2 stacks or if Combustion far away
            if (HasBuff("Pyroclasm") && (Inferno.BuffStacks("Pyroclasm") >= 2 || Inferno.SpellCooldown("Combustion") > 12000))
            {
                if (Inferno.IsSpellKnown("Fuel the Fire") && enemies >= fsThreshold)
                    if (Cast("Flamestrike")) return true;
                if (Cast("Pyroblast")) return true;
            }

            // Meteor
            if (GetCheckBox("Use Meteor"))
            {
                if (Inferno.IsSpellKnown("Sunfury Execution") && Inferno.SpellCooldown("Combustion") < 12000 && Inferno.BuffStacks("Pyroclasm") < 2)
                    if (Cast("Meteor")) return true;
                if (Inferno.IsSpellKnown("Blast Zone"))
                    if (Cast("Meteor")) return true;
            }

            // Scorch in execute or with Heat Shimmer
            if ((Inferno.IsSpellKnown("Scald") && GetTargetHealthPct() < 30) || HasBuff("Heat Shimmer"))
                if (Cast("Scorch")) return true;

            // Fireball filler
            if (Cast("Fireball")) return true;
            return false;
        }

        // =====================================================================
        // FIRE BLAST (actions.fireblast) - off-GCD weaving
        // =====================================================================
        bool HandleFireBlast(int enemies, bool inCombustion)
        {
            // Fire Blast with Heating Up during Combustion/Hyperthermia
            if (!HasBuff("Hot Streak!") && HasBuff("Heating Up") && (inCombustion || HasBuff("Hyperthermia")))
            {
                if (Inferno.CanCast("Fire Blast", IgnoreGCD: true))
                {
                    Inferno.Cast("Fire Blast", QuickDelay: true);
                    return true;
                }
            }

            // Fire Blast with Heating Up while hardcasting (non-execute)
            if (!HasBuff("Hot Streak!") && HasBuff("Heating Up") && Inferno.CastingID("player") > 0 && !inCombustion)
            {
                if (Inferno.CanCast("Fire Blast", IgnoreGCD: true))
                {
                    Inferno.Cast("Fire Blast", QuickDelay: true);
                    return true;
                }
            }

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
            if (hpPct <= GetSlider("Blazing Barrier HP %") && Inferno.BuffRemaining("Blazing Barrier") < GCD() && Inferno.CanCast("Blazing Barrier", IgnoreGCD: true))
            { Inferno.Cast("Blazing Barrier", QuickDelay: true); return true; }
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
            if (Inferno.BuffRemaining("Combustion") < 6000 && !HasBuff("Combustion")) return false;
            if (Inferno.CanUseEquippedItem(13)) { Inferno.Cast("trinket1"); return true; }
            if (Inferno.CanUseEquippedItem(14)) { Inferno.Cast("trinket2"); return true; }
            return false;
        }

        // =====================================================================
        // HELPERS
        // =====================================================================
        int GCD() { return Inferno.GCD(); }
        int GCDMAX() { int g = (int)(1500f / (1f + Inferno.Haste("player") / 100f)); return g < 750 ? 750 : g; }
        bool HasBuff(string name) { return Inferno.BuffRemaining(name) > GCD(); }
        bool HasDebuff(string name) { return Inferno.DebuffRemaining(name) > GCD(); }
        int GetTargetHealthPct()
        {
            int hp = Inferno.Health("target"); int maxHp = Inferno.MaxHealth("target");
            if (maxHp < 1) return 100; return (hp * 100) / maxHp;
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
        bool CastCD(string name)
        {
            if ((Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;
            if (name == "Combustion" && !GetCheckBox("Use Combustion")) return false;
            if (Inferno.CanCast(name)) { Inferno.Cast(name); Inferno.PrintMessage(">> " + name, Color.White); return true; }
            return false;
        }
    }
}
