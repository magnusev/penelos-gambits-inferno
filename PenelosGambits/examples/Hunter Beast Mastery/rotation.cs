using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API;

namespace InfernoWow.Modules
{
    /// <summary>
    /// Beast Mastery Hunter - Translated from SimulationCraft Midnight APL
    /// Auto-detects hero tree: Dark Ranger (Black Arrow) or Pack Leader.
    /// Each has ST and Cleave sub-rotations. Beast Cleave talent affects AoE threshold.
    /// </summary>
    public class BeastMasteryHunterRotation : Rotation
    {
        List<string> Abilities = new List<string> {
            "Kill Command", "Barbed Shot", "Cobra Shot", "Bestial Wrath",
            "Wild Thrash", "Black Arrow", "Wailing Arrow",
            "Kill Shot",
        };
        List<string> TalentChecks = new List<string> {
            "Black Arrow", "Beast Cleave", "Killer Cobra",
            "Hogstrider",
        };
        List<string> DefensiveSpells = new List<string> { "Exhilaration", "Aspect of the Turtle", "Survival of the Fittest" };
        List<string> UtilitySpells = new List<string> { "Counter Shot", "Intimidation", "Misdirection", "Mend Pet", "Call Pet 1", "Hunter's Mark" };

        const int HealthstoneItemID = 5512;
        const int FocusPowerType = 2;
        private Random _rng = new Random();
        private int _lastCastingID = 0;
        private int _interruptTargetPct = 0;

        public override void LoadSettings()
        {
            Settings.Add(new Setting("=== Beast Mastery Hunter ==="));
            Settings.Add(new Setting("Hero tree auto-detected: Dark Ranger / Pack Leader"));
            Settings.Add(new Setting("Use Bestial Wrath", true));
            Settings.Add(new Setting("Use Trinkets", true));
            Settings.Add(new Setting("=== Defensives ==="));
            Settings.Add(new Setting("Use Defensives", true));
            Settings.Add(new Setting("Exhilaration HP %", 1, 100, 50));
            Settings.Add(new Setting("Aspect of the Turtle HP %", 1, 100, 20));
            Settings.Add(new Setting("Survival of the Fittest HP %", 1, 100, 40));
            Settings.Add(new Setting("Healthstone HP %", 1, 100, 50));
            Settings.Add(new Setting("=== Interrupt ==="));
            Settings.Add(new Setting("Use Hunter's Mark in combat", false));
            Settings.Add(new Setting("Auto Interrupt", true));
            Settings.Add(new Setting("Interrupt at cast % (min)", 0, 100, 40));
            Settings.Add(new Setting("Interrupt at cast % (max)", 0, 100, 90));
            Settings.Add(new Setting("=== Utility ==="));
            Settings.Add(new Setting("Auto Mend Pet", true));
        }

        public override void Initialize()
        {
            Inferno.PrintMessage("             //////////////////////////////////////", Color.DarkGreen);
            Inferno.PrintMessage("             //   BEAST MASTERY - HUNTER (MID)   //", Color.DarkGreen);
            Inferno.PrintMessage("             //     DARK RANGER / PACK LEADER    //", Color.DarkGreen);
            Inferno.PrintMessage("             //              V 1.00              //", Color.DarkGreen);
            Inferno.PrintMessage("             //////////////////////////////////////", Color.DarkGreen);
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
            CustomFunctions.Add("PetIsActive", "return (UnitExists('pet') and not UnitIsDead('pet')) and 1 or 0");
            // Racial abilities
            foreach (string r in new string[] { "Berserking", "Blood Fury", "Ancestral Call", "Fireblood", "Lights Judgment" }) Spellbook.Add(r);
            CustomCommands.Add("NoCDs"); CustomCommands.Add("nocds");
            CustomCommands.Add("ForceST"); CustomCommands.Add("forcest");
        }

        public override bool OutOfCombatTick()
        {
            if (Inferno.CustomFunction("PetIsActive") == 0 && Inferno.CanCast("Call Pet 1"))
            { Inferno.Cast("Call Pet 1"); return true; }
            if (HandleMendPet()) return true;
            // Hunter's Mark on target
            if (Inferno.UnitCanAttack("player", "target") && !Inferno.HasDebuff("Hunter's Mark", "target", false) && Inferno.CanCast("Hunter's Mark"))
            { Inferno.Cast("Hunter's Mark"); return true; }
            return false;
        }

        public override bool CombatTick()
        {
            if (Inferno.IsDead("player") || Inferno.IsGhost("player")) return false;
            if (HandleDefensives()) return true;
            if (HandleInterrupt()) return true;
            if (HandleMendPet()) return true;
            if (!Inferno.UnitCanAttack("player", "target")) return false;
            // Hunter's Mark in combat (high priority)
            if (GetCheckBox("Use Hunter's Mark in combat") && !Inferno.HasDebuff("Hunter's Mark", "target", false) && Inferno.CanCast("Hunter's Mark"))
            { Inferno.Cast("Hunter's Mark"); Inferno.PrintMessage(">> Hunter's Mark", Color.White); return true; }
            if (Inferno.IsChanneling("player")) return false;

            // Don't interrupt cast-time trinkets
            string _castName = Inferno.CastingName("player");
            if (_castName.Contains("Puzzle Box") || _castName.Contains("Emberwing")) return false;

            // Trinkets during Bestial Wrath
                        if (HandleTrinkets()) return true;

            // Racial damage CDs
            if ((HasBuff("Bestial Wrath")) && HandleRacials()) return true;

            int enemies = Inferno.EnemiesNearUnit(8f, "target");
            if (enemies < 1) enemies = 1; if ((Inferno.IsCustomCodeOn("ForceST") || Inferno.IsCustomCodeOn("forcest"))) enemies = 1;
            bool hasBeastCleave = Inferno.IsSpellKnown("Beast Cleave");
            bool isDarkRanger = Inferno.IsSpellKnown("Black Arrow");

            // APL routing (lines 17-20)
            bool isCleave = isDarkRanger
                ? (enemies > 2 || (hasBeastCleave && enemies > 1))
                : (enemies > 2 || (hasBeastCleave && enemies > 1));

            if (isDarkRanger)
            {
                if (isCleave) return DRCleave(enemies);
                return DRST(enemies);
            }
            else
            {
                if (isCleave) return Cleave(enemies);
                return ST(enemies);
            }
        }

        // =====================================================================
        // DARK RANGER ST (actions.drst)
        // =====================================================================
        bool DRST(int enemies)
        {
            // bestial_wrath
            if (CastCD("Bestial Wrath")) return true;
            // kill_command,if=buff.natures_ally.up|!apex.3 (unconditional for most builds)
            if (Cast("Kill Command")) return true;
            // black_arrow,if=buff.withering_fire.up
            if (HasBuff("Withering Fire") && Cast("Black Arrow")) return true;
            // cobra_shot,if=talent.killer_cobra&buff.bestial_wrath.up&cooldown.barbed_shot.charges_fractional<1.4
            if (Inferno.IsSpellKnown("Killer Cobra") && HasBuff("Bestial Wrath") && Inferno.ChargesFractional("Barbed Shot", 12000) < 1.4f)
                if (Cast("Cobra Shot")) return true;
            // wailing_arrow
            if (Cast("Wailing Arrow")) return true;
            // barbed_shot
            if (Cast("Barbed Shot")) return true;
            // black_arrow
            if (Cast("Black Arrow")) return true;
            // cobra_shot
            if (Cast("Cobra Shot")) return true;
            return false;
        }

        // =====================================================================
        // DARK RANGER CLEAVE (actions.drcleave)
        // =====================================================================
        bool DRCleave(int enemies)
        {
            // black_arrow,if=buff.beast_cleave.remains<gcd
            if (Inferno.BuffRemaining("Beast Cleave", "pet", false) < GCD())
                if (Cast("Black Arrow")) return true;
            // bestial_wrath
            if (CastCD("Bestial Wrath")) return true;
            // wailing_arrow,if=buff.bestial_wrath.remains<execute_time+gcd
            if (Inferno.BuffRemaining("Bestial Wrath") > 0 && Inferno.BuffRemaining("Bestial Wrath") < GCD() * 2)
                if (Cast("Wailing Arrow")) return true;
            // wild_thrash
            if (Cast("Wild Thrash")) return true;
            // kill_command,if=buff.natures_ally.up|!apex.3 (unconditional for most builds)
            if (Cast("Kill Command")) return true;
            // black_arrow,if=buff.withering_fire.up
            if (HasBuff("Withering Fire") && Cast("Black Arrow")) return true;
            // barbed_shot
            if (Cast("Barbed Shot")) return true;
            // wailing_arrow
            if (Cast("Wailing Arrow")) return true;
            // black_arrow
            if (Cast("Black Arrow")) return true;
            // cobra_shot
            if (Cast("Cobra Shot")) return true;
            return false;
        }

        // =====================================================================
        // PACK LEADER ST (actions.st)
        // =====================================================================
        bool ST(int enemies)
        {
            // barbed_shot,if=cooldown.bestial_wrath.remains<gcd
            if (Inferno.SpellCooldown("Bestial Wrath") < GCDMAX() && Cast("Barbed Shot")) return true;
            // bestial_wrath
            if (CastCD("Bestial Wrath")) return true;
            // kill_command,if=buff.natures_ally.up|!apex.3 (unconditional for most builds)
            if (Cast("Kill Command")) return true;
            // barbed_shot
            if (Cast("Barbed Shot")) return true;
            // cobra_shot
            if (Cast("Cobra Shot")) return true;
            return false;
        }

        // =====================================================================
        // PACK LEADER CLEAVE (actions.cleave)
        // =====================================================================
        bool Cleave(int enemies)
        {
            // barbed_shot,if=cooldown.bestial_wrath.remains<gcd
            if (Inferno.SpellCooldown("Bestial Wrath") < GCDMAX() && Cast("Barbed Shot")) return true;
            // wild_thrash
            if (Cast("Wild Thrash")) return true;
            // bestial_wrath
            if (CastCD("Bestial Wrath")) return true;
            // kill_command
            if (Cast("Kill Command")) return true;
            // cobra_shot,if=cooldown.wild_thrash.remains>gcd&buff.hogstrider.up&active_enemies<4
            if ((!Inferno.IsSpellKnown("Wild Thrash") || Inferno.SpellCooldown("Wild Thrash") > GCDMAX()) && HasBuff("Hogstrider") && enemies < 4)
                if (Cast("Cobra Shot")) return true;
            // barbed_shot
            if (Cast("Barbed Shot")) return true;
            // cobra_shot,if=cooldown.wild_thrash.remains>gcd
            if ((!Inferno.IsSpellKnown("Wild Thrash") || Inferno.SpellCooldown("Wild Thrash") > GCDMAX()) && Cast("Cobra Shot")) return true;
            // cobra_shot unconditional fallback
            if (Cast("Cobra Shot")) return true;
            return false;
        }

        // =====================================================================
        // DEFENSIVES / INTERRUPT / UTILITY
        // =====================================================================
        bool HandleDefensives()
        {
            if (!GetCheckBox("Use Defensives")) return false;
            int hpPct = GetPlayerHealthPct();
            // Aspect of the Turtle — emergency immunity (lowest HP threshold)
            if (hpPct <= GetSlider("Aspect of the Turtle HP %") && !HasBuff("Aspect of the Turtle") && Inferno.CanCast("Aspect of the Turtle", IgnoreGCD: true))
            { Inferno.Cast("Aspect of the Turtle", QuickDelay: true); return true; }
            // Exhilaration — instant heal
            if (hpPct <= GetSlider("Exhilaration HP %") && Inferno.CanCast("Exhilaration", IgnoreGCD: true))
            { Inferno.Cast("Exhilaration", QuickDelay: true); return true; }
            // Survival of the Fittest — damage reduction (2 charges)
            if (hpPct <= GetSlider("Survival of the Fittest HP %") && !HasBuff("Survival of the Fittest") && Inferno.CanCast("Survival of the Fittest", IgnoreGCD: true))
            { Inferno.Cast("Survival of the Fittest", QuickDelay: true); return true; }
            // Healthstone
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
            if (castPct >= _interruptTargetPct && Inferno.CanCast("Counter Shot", IgnoreGCD: true))
            { Inferno.Cast("Counter Shot", QuickDelay: true); _lastCastingID = 0; return true; }
            // Intimidation as backup kick when Counter Shot is on CD
            if (castPct >= _interruptTargetPct && Inferno.SpellCooldown("Counter Shot") > 0 && Inferno.CanCast("Intimidation", IgnoreGCD: true))
            { Inferno.Cast("Intimidation", QuickDelay: true); _lastCastingID = 0; return true; }
            return false;
        }

        bool HandleMendPet()
        {
            if (!GetCheckBox("Auto Mend Pet")) return false;
            if (Inferno.CustomFunction("PetIsActive") == 1 && GetPetHealthPct() < 70
                && Inferno.BuffRemaining("Mend Pet", "pet", false) < GCD() && Inferno.CanCast("Mend Pet"))
            { Inferno.Cast("Mend Pet"); return true; }
            return false;
        }

        bool HandleTrinkets()
        {
            if (!GetCheckBox("Use Trinkets") || (Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;
            if (!HasBuff("Bestial Wrath")) return false;
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

        int GetPetHealthPct()
        {
            int hp = Inferno.Health("pet"); int maxHp = Inferno.MaxHealth("pet");
            if (maxHp < 1) return 100; return (hp * 100) / maxHp;
        }

        bool Cast(string name)
        {
            if (Inferno.CanCast(name)) { Inferno.Cast(name); Inferno.PrintMessage(">> " + name, Color.White); return true; }
            return false;
        }

        bool CastCD(string name)
        {
            if ((Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;
            if (name == "Bestial Wrath" && !GetCheckBox("Use Bestial Wrath")) return false;
            if (Inferno.CanCast(name)) { Inferno.Cast(name); Inferno.PrintMessage(">> " + name, Color.White); return true; }
            return false;
        }
    }
}
