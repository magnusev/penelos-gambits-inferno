using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API;

namespace InfernoWow.Modules
{
    /// <summary>
    /// Arcane Mage - Translated from SimulationCraft Midnight APL
    /// Auto-detects hero tree: Spellslinger (Splinterstorm) or Sunfury (Spellfire Spheres).
    /// Spellslinger further branches on Orb Mastery talent.
    /// Arcane Salvo stack management, Clearcasting/Missiles tracking, Surge+Touch windows.
    /// </summary>
    public class ArcaneMageRotation : Rotation
    {
        List<string> Abilities = new List<string> {
            "Arcane Blast", "Arcane Missiles", "Arcane Barrage",
            "Arcane Orb", "Arcane Surge", "Touch of the Magi",
            "Arcane Pulse", "Arcane Explosion", "Evocation",
            "Presence of Mind", "Mirror Image",
        };
        List<string> TalentChecks = new List<string> {
            "Splintering Sorcery", "Spellfire Spheres", "Orb Mastery",
            "High Voltage", "Overpowered Missiles", "Orb Barrage",
            "Resonance", "Arcane Pulse", "Impetus", "Fuel the Fire",
            "Expanded Mind",
        };
        List<string> DefensiveSpells = new List<string> { "Ice Block", "Prismatic Barrier" };
        List<string> UtilitySpells = new List<string> { "Counterspell", "Arcane Intellect" };

        const int HealthstoneItemID = 5512;
        private Random _rng = new Random();
        private int _lastCastingID = 0;
        private int _interruptTargetPct = 0;

        public override void LoadSettings()
        {
            Settings.Add(new Setting("=== Arcane Mage ==="));
            Settings.Add(new Setting("Hero tree auto-detected: Spellslinger / Sunfury"));
            Settings.Add(new Setting("Use Arcane Surge", true));
            Settings.Add(new Setting("Use Touch of the Magi", true));
            Settings.Add(new Setting("Use Trinkets", true));
            Settings.Add(new Setting("=== Defensives ==="));
            Settings.Add(new Setting("Use Defensives", true));
            Settings.Add(new Setting("Prismatic Barrier HP %", 1, 100, 80));
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
            Inferno.PrintMessage("             //////////////////////////////////////", Color.MediumPurple);
            Inferno.PrintMessage("             //       ARCANE - MAGE (MID)        //", Color.MediumPurple);
            Inferno.PrintMessage("             //    SPELLSLINGER / SUNFURY        //", Color.MediumPurple);
            Inferno.PrintMessage("             //              V 1.00              //", Color.MediumPurple);
            Inferno.PrintMessage("             //////////////////////////////////////", Color.MediumPurple);
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

            // cancel_action: stop Evocation early when mana >= 95%
            if (Inferno.IsChanneling("player"))
            {
                if (Inferno.CastingName("player") == "Evocation" && GetManaPct() >= 95)
                { Inferno.StopCasting(); return true; }
                return false;
            }

            // Don't interrupt cast-time trinkets
            string _castName = Inferno.CastingName("player");
            if (_castName.Contains("Puzzle Box") || _castName.Contains("Emberwing")) return false;

            if (!Inferno.UnitCanAttack("player", "target")) return false;
                        if (HandleTrinkets()) return true;

            // Racial damage CDs
            if ((HasBuff("Arcane Surge")) && HandleRacials()) return true;
            if (HandleCooldowns()) return true;

            int enemies = Inferno.EnemiesNearUnit(10f, "target");
            if (enemies < 1) enemies = 1; if ((Inferno.IsCustomCodeOn("ForceST") || Inferno.IsCustomCodeOn("forcest"))) enemies = 1;

            bool isSS = Inferno.IsSpellKnown("Splintering Sorcery");
            bool isSF = Inferno.IsSpellKnown("Spellfire Spheres");

            if (isSS && Inferno.IsSpellKnown("Orb Mastery"))
                return SpellslingerOrbM(enemies);
            if (isSS)
                return Spellslinger(enemies);
            return Sunfury(enemies);
        }

        // =====================================================================
        // COOLDOWNS (actions.cooldowns)
        // =====================================================================
        bool HandleCooldowns()
        {
            if ((Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;

            // Evocation at low mana, outside of CDs
            int manaPct = GetManaPct();
            if (manaPct < 10 && !HasBuff("Arcane Surge") && !HasDebuff("Touch of the Magi") && Inferno.SpellCooldown("Arcane Surge") > 10000)
                if (Cast("Evocation")) return true;

            // Touch of the Magi - during Arcane Surge
            if (GetCheckBox("Use Touch of the Magi") && HasBuff("Arcane Surge"))
            {
                if (Inferno.CanCast("Touch of the Magi", IgnoreGCD: true))
                { Inferno.Cast("Touch of the Magi", QuickDelay: true); return true; }
            }
            // Touch of the Magi - off-CD if Surge won't be ready soon
            if (GetCheckBox("Use Touch of the Magi") && !HasBuff("Arcane Surge") && Inferno.SpellCooldown("Arcane Surge") > 30000)
            {
                if (Inferno.CanCast("Touch of the Magi", IgnoreGCD: true))
                { Inferno.Cast("Touch of the Magi", QuickDelay: true); return true; }
            }

            // Arcane Surge
            if (GetCheckBox("Use Arcane Surge") && Cast("Arcane Surge")) return true;

            return false;
        }

        // =====================================================================
        // SPELLSLINGER with Orb Mastery (actions.spellslinger_orbm)
        // =====================================================================
        bool SpellslingerOrbM(int enemies)
        {
            int charges = GetArcaneCharges();
            int salvo = GetSalvoStacks();
            bool hasCC = HasBuff("Clearcasting");

            // arcane_orb after barrage or in AoE with CC
            if ((charges < 3 || (enemies >= 4 && salvo <= 14)) && hasCC && salvo <= 14)
                if (Cast("Arcane Orb")) return true;

            // arcane_barrage at 20+ salvo or end of Surge/Touch
            if (salvo >= 20 && (charges == 4 || Inferno.IsSpellKnown("Orb Barrage")))
                if (Cast("Arcane Barrage")) return true;
            if (HasBuff("Arcane Surge") && Inferno.BuffRemaining("Arcane Surge") < GCD() && salvo >= 15)
                if (Cast("Arcane Barrage")) return true;
            if (HasDebuff("Touch of the Magi") && Inferno.DebuffRemaining("Touch of the Magi") < GCD() && salvo >= 15)
                if (Cast("Arcane Barrage")) return true;

            // arcane_missiles with CC and low salvo
            if (hasCC && salvo <= 10 && !HasBuff("Arcane Surge"))
                if (Cast("Arcane Missiles")) return true;

            // presence_of_mind for charges
            if (charges < 2 && !hasCC && Inferno.CanCast("Presence of Mind", IgnoreGCD: true))
            { Inferno.Cast("Presence of Mind", QuickDelay: true); return true; }

            // arcane_blast with presence of mind
            if (HasBuff("Presence of Mind") && Cast("Arcane Blast")) return true;

            // arcane_pulse in AoE or for charges
            if (Inferno.IsSpellKnown("Arcane Pulse"))
            {
                int pulseAoeCount = 3 + (Inferno.IsSpellKnown("Orb Mastery") ? 1 : 0);
                if (enemies >= pulseAoeCount || (charges < 3 && GetManaPct() > 30))
                    if (Cast("Arcane Pulse")) return true;
            }

            // arcane_blast
            if (Cast("Arcane Blast")) return true;

            // arcane_barrage fallback
            if (Cast("Arcane Barrage")) return true;
            return false;
        }

        // =====================================================================
        // SPELLSLINGER (actions.spellslinger)
        // =====================================================================
        bool Spellslinger(int enemies)
        {
            int charges = GetArcaneCharges();
            int salvo = GetSalvoStacks();
            bool hasCC = HasBuff("Clearcasting");

            // arcane_orb for charges
            if (charges < (3 + (enemies >= 2 ? 1 : 0)) && !hasCC)
                if (Cast("Arcane Orb")) return true;

            // arcane_barrage at 20+ salvo
            if (salvo >= 20 && (charges == 4 || Inferno.IsSpellKnown("Orb Barrage")))
                if (Cast("Arcane Barrage")) return true;

            // arcane_barrage in AoE with CC
            if (enemies >= 2 && charges == 4 && hasCC && HasBuff("Overpowered Missiles") && salvo > 5 && salvo < 14)
                if (Cast("Arcane Barrage")) return true;

            // arcane_missiles with CC for salvo stacks
            if (hasCC && salvo < 10)
                if (Cast("Arcane Missiles")) return true;

            // presence_of_mind for charges
            if (charges < 2 && !hasCC && Inferno.CanCast("Presence of Mind", IgnoreGCD: true))
            { Inferno.Cast("Presence of Mind", QuickDelay: true); return true; }

            if (HasBuff("Presence of Mind") && Cast("Arcane Blast")) return true;

            // arcane_pulse
            if (Inferno.IsSpellKnown("Arcane Pulse") && (enemies >= 3 || (charges < 3 && GetManaPct() > 50)))
                if (Cast("Arcane Pulse")) return true;

            // arcane_blast
            if (Cast("Arcane Blast")) return true;
            if (Cast("Arcane Barrage")) return true;
            return false;
        }

        // =====================================================================
        // SUNFURY (actions.sunfury)
        // =====================================================================
        bool Sunfury(int enemies)
        {
            int charges = GetArcaneCharges();
            int salvo = GetSalvoStacks();
            bool hasCC = HasBuff("Clearcasting");

            // arcane_barrage at 25 salvo or during Touch/Soul
            if (salvo >= 25 && charges == 4)
                if (Cast("Arcane Barrage")) return true;
            if (HasDebuff("Touch of the Magi") && Inferno.DebuffRemaining("Touch of the Magi") < GCD() && charges == 4)
                if (Cast("Arcane Barrage")) return true;
            if (HasBuff("Arcane Soul"))
                if (Cast("Arcane Barrage")) return true;

            // arcane_barrage at 6/12/18 salvo increments with CC for Meteorite generation
            if (charges == 4 && hasCC && ((salvo >= 6 && salvo < 7) || (salvo >= 12 && salvo < 13) || (salvo >= 18 && salvo < 19)))
                if (Cast("Arcane Barrage")) return true;

            // arcane_missiles with CC and low salvo
            if (hasCC && salvo < 15)
                if (Cast("Arcane Missiles")) return true;

            // arcane_orb for charges
            if (charges < 2 && Cast("Arcane Orb")) return true;

            // arcane_pulse in AoE or for charges
            if (Inferno.IsSpellKnown("Arcane Pulse") && (enemies >= 3 || (charges < 3 && GetManaPct() > 50)))
                if (Cast("Arcane Pulse")) return true;

            // arcane_explosion in heavy AoE for charges
            if (enemies > 3 && charges < 2 && !Inferno.IsSpellKnown("Impetus"))
                if (Cast("Arcane Explosion")) return true;

            // arcane_blast
            if (Cast("Arcane Blast")) return true;
            if (Cast("Arcane Barrage")) return true;
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
            if (hpPct <= GetSlider("Prismatic Barrier HP %") && Inferno.BuffRemaining("Prismatic Barrier") < GCD() && Inferno.CanCast("Prismatic Barrier", IgnoreGCD: true))
            { Inferno.Cast("Prismatic Barrier", QuickDelay: true); return true; }
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
            if (!HasBuff("Arcane Surge")) return false;
            if (Inferno.CanUseEquippedItem(13)) { Inferno.Cast("trinket1"); return true; }
            if (Inferno.CanUseEquippedItem(14)) { Inferno.Cast("trinket2"); return true; }
            return false;
        }

        // =====================================================================
        // HELPERS
        // =====================================================================
        int GCD() { return Inferno.GCD(); }
        bool HasBuff(string name) { return Inferno.BuffRemaining(name) > GCD(); }
        bool HasDebuff(string name) { return Inferno.DebuffRemaining(name) > GCD(); }
        int GetArcaneCharges()
        {
            int charges = Inferno.Power("player", 16);
            string casting = Inferno.CastingName("player");
            if (casting == "Arcane Blast") charges += 1;
            return Math.Min(charges, 4);
        }
        int GetSalvoStacks()
        {
            int stacks = Inferno.BuffStacks("Arcane Salvo");
            // Predict +2 stacks if Expanded Mind talented and currently casting Arcane Blast/Pulse
            if (Inferno.IsSpellKnown("Expanded Mind"))
            {
                string casting = Inferno.CastingName("player");
                if (casting == "Arcane Blast" || casting == "Arcane Pulse") stacks += 2;
            }
            return stacks;
        }
        int GetManaPct() { int max = Inferno.MaxPower("player", 0); return max > 0 ? (Inferno.Power("player", 0) * 100) / max : 0; }


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
