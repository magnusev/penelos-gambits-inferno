using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API;

namespace InfernoWow.Modules
{
    /// <summary>
    /// Blood Death Knight - Translated from SimulationCraft Midnight APL
    /// Hero trees: Deathbringer (Reaper's Mark, Exterminate) vs San'layn (Vampiric Strike, Gift of the San'layn)
    /// Sub-rotations: high_prio_actions, deathbringer, san_gift (Gift window), sanlayn
    /// Core: Bone Shield uptime, RP management via Death Strike, DnD, Dancing Rune Weapon windows.
    /// </summary>
    public class BloodDeathKnightRotation : Rotation
    {
        List<string> Abilities = new List<string> {
            "Heart Strike", "Marrowrend", "Death Strike", "Blood Boil",
            "Death and Decay", "Dancing Rune Weapon", "Vampiric Blood",
            "Consumption", "Reaper's Mark", "Death's Caress", "Raise Dead",
        };
        List<string> TalentChecks = new List<string> {
            "Reaper's Mark", "Vampiric Strike",
            "Coagulopathy",
        };
        List<string> DefensiveSpells = new List<string> { "Vampiric Blood", "Icebound Fortitude", "Anti-Magic Shell" };
        List<string> UtilitySpells = new List<string> { "Mind Freeze", "Death Grip" };
        const int HealthstoneItemID = 5512;
        const int RunicPowerType = 6;
        private Random _rng = new Random(); private int _lastCastingID = 0; private int _interruptTargetPct = 0;

        public override void LoadSettings()
        {
            Settings.Add(new Setting("=== Blood Death Knight ==="));
            Settings.Add(new Setting("Use Dancing Rune Weapon", true));
            Settings.Add(new Setting("Use Trinkets", true));
            Settings.Add(new Setting("=== Defensives ==="));
            Settings.Add(new Setting("Use Defensives", true));
            Settings.Add(new Setting("Death Strike HP %", 1, 100, 60));
            Settings.Add(new Setting("Vampiric Blood HP %", 1, 100, 40));
            Settings.Add(new Setting("Icebound Fortitude HP %", 1, 100, 25));
            Settings.Add(new Setting("Anti-Magic Shell HP %", 1, 100, 60));
            Settings.Add(new Setting("Healthstone HP %", 1, 100, 50));
            Settings.Add(new Setting("=== Interrupt ==="));
            Settings.Add(new Setting("Auto Interrupt", true));
            Settings.Add(new Setting("Interrupt at cast % (min)", 0, 100, 40));
            Settings.Add(new Setting("Interrupt at cast % (max)", 0, 100, 90));
        }

        public override void Initialize()
        {
            Inferno.PrintMessage("             //////////////////////////////////////", Color.DarkRed);
            Inferno.PrintMessage("             //   BLOOD - DEATH KNIGHT (MID) V2  //", Color.DarkRed);
            Inferno.PrintMessage("             //////////////////////////////////////", Color.DarkRed);
            string addonCmd = Inferno.GetAddonName().Length >= 5 ? Inferno.GetAddonName().Substring(0, 5).ToLower() : Inferno.GetAddonName().ToLower();
            Inferno.PrintMessage("Ready! Use /" + addonCmd + " toggle to pause/resume.", Color.LimeGreen);
            Inferno.PrintMessage("Toggle CDs: /" + addonCmd + " NoCDs | Force ST: /" + addonCmd + " ForceST", Color.Yellow);
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
            if ((HasBuff("Dancing Rune Weapon")) && HandleRacials()) return true;
            if (Inferno.IsChanneling("player")) return false;

            // Don't interrupt cast-time trinkets
            string _castName = Inferno.CastingName("player");
            if (_castName.Contains("Puzzle Box") || _castName.Contains("Emberwing")) return false;

            int rp = Inferno.Power("player", RunicPowerType);
            int rpDeficit = Inferno.MaxPower("player", RunicPowerType) - rp;
            int boneShield = Inferno.BuffStacks("Bone Shield");
            int runes = Inferno.GetAvailableRunes();
            bool drwUp = HasBuff("Dancing Rune Weapon");
            bool isDeathbringer = Inferno.IsSpellKnown("Reaper's Mark");
            bool isSanlayn = Inferno.IsSpellKnown("Vampiric Strike");

            // Trinkets
            if (GetCheckBox("Use Trinkets") && !(Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds")))
            { if (Inferno.CanUseEquippedItem(13)) { Inferno.Cast("trinket1"); return true; } if (Inferno.CanUseEquippedItem(14)) { Inferno.Cast("trinket2"); return true; } }

            // vampiric_blood,if=!buff.vampiric_blood.up
            if (!HasBuff("Vampiric Blood") && Inferno.CanCast("Vampiric Blood", IgnoreGCD: true))
            { Inferno.Cast("Vampiric Blood", QuickDelay: true); return true; }

            // High priority actions
            if (HighPrioActions(rp, rpDeficit, drwUp)) return true;

            // Hero tree routing
            if (isDeathbringer) return Deathbringer(rp, rpDeficit, boneShield, runes, drwUp);
            if (isSanlayn && HasBuff("Gift of the San'layn")) return SanGift(rp, rpDeficit);
            if (isSanlayn) return Sanlayn(rp, rpDeficit, boneShield, runes);

            // Fallback (no hero tree detected)
            return Deathbringer(rp, rpDeficit, boneShield, runes, drwUp);
        }

        // =====================================================================
        // HIGH PRIORITY ACTIONS
        // =====================================================================
        bool HighPrioActions(int rp, int rpDeficit, bool drwUp)
        {
            // raise_dead
            if (Cast("Raise Dead")) return true;
            // death_strike,if=buff.coagulopathy.up&buff.coagulopathy.remains<=gcd
            if (Inferno.IsSpellKnown("Coagulopathy") && Inferno.BuffRemaining("Coagulopathy") > 0 && Inferno.BuffRemaining("Coagulopathy") <= GCD())
                if (rp >= 35 && Cast("Death Strike")) return true;
            // dancing_rune_weapon,if=!buff.exterminate.up&!debuff.reapers_mark.up&!buff.dancing_rune_weapon.up
            if (!HasBuff("Exterminate") && Inferno.DebuffRemaining("Reaper's Mark") == 0 && !drwUp)
                if (CastCD("Dancing Rune Weapon")) return true;
            return false;
        }

        // =====================================================================
        // DEATHBRINGER
        // =====================================================================
        bool Deathbringer(int rp, int rpDeficit, int boneShield, int runes, bool drwUp)
        {
            // death_strike,if=rp_deficit<20 or (deficit<26 & drw up)
            if (rpDeficit < 20 || (rpDeficit < 26 && drwUp))
                if (Cast("Death Strike")) return true;
            // death_and_decay,if=!buff.death_and_decay.up
            if (!HasBuff("Death and Decay"))
                if (Cast("Death and Decay")) return true;
            // reapers_mark
            if (Cast("Reaper's Mark")) return true;
            // marrowrend,if=buff.exterminate.up
            if (HasBuff("Exterminate"))
                if (Cast("Marrowrend")) return true;
            // deaths_caress,if=bone_shield low & rune<4
            if ((boneShield == 0 || Inferno.BuffRemaining("Bone Shield") < 3000 || boneShield < 6) && runes < 4)
                if (Cast("Death's Caress")) return true;
            // marrowrend,if=bone_shield needs refresh
            if (boneShield == 0 || Inferno.BuffRemaining("Bone Shield") < 3000 || boneShield < 6)
                if (Cast("Marrowrend")) return true;
            // death_strike
            if (Cast("Death Strike")) return true;
            // blood_boil
            if (Cast("Blood Boil")) return true;
            // consumption,if=!drw
            if (!drwUp && Cast("Consumption")) return true;
            // heart_strike
            if (Cast("Heart Strike")) return true;
            // consumption
            if (Cast("Consumption")) return true;
            return false;
        }

        // =====================================================================
        // SAN'LAYN - GIFT OF THE SAN'LAYN WINDOW
        // =====================================================================
        bool SanGift(int rp, int rpDeficit)
        {
            // heart_strike,if=buff.essence_of_the_blood_queen.remains<1.5
            if (Inferno.BuffRemaining("Essence of the Blood Queen") > 0 && Inferno.BuffRemaining("Essence of the Blood Queen") < 1500)
                if (Cast("Heart Strike")) return true;
            // death_strike,if=rp_deficit<36
            if (rpDeficit < 36)
                if (Cast("Death Strike")) return true;
            // blood_boil (spread blood plague)
            if (Cast("Blood Boil")) return true;
            // death_and_decay,if=buff.crimson_scourge.up
            if (HasBuff("Crimson Scourge"))
                if (Cast("Death and Decay")) return true;
            // heart_strike,if=essence stacks<7
            if (Inferno.BuffStacks("Essence of the Blood Queen") < 7)
                if (Cast("Heart Strike")) return true;
            // death_strike
            if (Cast("Death Strike")) return true;
            // heart_strike
            if (Cast("Heart Strike")) return true;
            // blood_boil
            if (Cast("Blood Boil")) return true;
            return false;
        }

        // =====================================================================
        // SAN'LAYN - STANDARD
        // =====================================================================
        bool Sanlayn(int rp, int rpDeficit, int boneShield, int runes)
        {
            // deaths_caress,if=bone_shield very low
            if (boneShield == 0 || Inferno.BuffRemaining("Bone Shield") < 1500 || boneShield <= 1)
                if (Cast("Death's Caress")) return true;
            // blood_boil,if=blood_plague.remains<3
            if (Inferno.DebuffRemaining("Blood Plague") < 3000)
                if (Cast("Blood Boil")) return true;
            // heart_strike,if=essence_of_blood_queen about to expire & vampiric_strike up
            if (Inferno.BuffRemaining("Essence of the Blood Queen") > 0 && Inferno.BuffRemaining("Essence of the Blood Queen") < 1500 && HasBuff("Vampiric Strike"))
                if (Cast("Heart Strike")) return true;
            // death_strike,if=rp_deficit<20
            if (rpDeficit < 20)
                if (Cast("Death Strike")) return true;
            // bone_shield maintenance
            if (boneShield < 6)
            { if (Cast("Death's Caress")) return true; if (Cast("Marrowrend")) return true; }
            // death_and_decay,if=crimson_scourge
            if (HasBuff("Crimson Scourge"))
                if (Cast("Death and Decay")) return true;
            // heart_strike,if=vampiric_strike.up
            if (HasBuff("Vampiric Strike"))
                if (Cast("Heart Strike")) return true;
            // death_strike
            if (Cast("Death Strike")) return true;
            // consumption
            if (Cast("Consumption")) return true;
            // heart_strike,if=rune>=2
            if (runes >= 2 && Cast("Heart Strike")) return true;
            // blood_boil
            if (Cast("Blood Boil")) return true;
            // heart_strike
            if (Cast("Heart Strike")) return true;
            return false;
        }

        // =====================================================================
        // DEFENSIVES / INTERRUPT
        // =====================================================================
        bool HandleDefensives()
        {
            if (!GetCheckBox("Use Defensives")) return false;
            int hpPct = GetPlayerHealthPct(); int rp = Inferno.Power("player", RunicPowerType);
            if (hpPct <= GetSlider("Icebound Fortitude HP %") && !HasBuff("Icebound Fortitude") && Inferno.CanCast("Icebound Fortitude", IgnoreGCD: true))
            { Inferno.Cast("Icebound Fortitude", QuickDelay: true); return true; }
            if (hpPct <= GetSlider("Vampiric Blood HP %") && Inferno.CanCast("Vampiric Blood", IgnoreGCD: true))
            { Inferno.Cast("Vampiric Blood", QuickDelay: true); return true; }
            if (hpPct <= GetSlider("Anti-Magic Shell HP %") && Inferno.CanCast("Anti-Magic Shell", IgnoreGCD: true))
            { Inferno.Cast("Anti-Magic Shell", QuickDelay: true); return true; }
            if (hpPct <= GetSlider("Death Strike HP %") && rp >= 35 && Inferno.CanCast("Death Strike"))
            { Inferno.Cast("Death Strike"); Inferno.PrintMessage(">> Death Strike (defensive)", Color.White); return true; }
            if (hpPct <= GetSlider("Healthstone HP %") && Inferno.CustomFunction("HasHealthstone") == 1 && Inferno.ItemCooldown(HealthstoneItemID) == 0)
            { Inferno.Cast("use_healthstone", QuickDelay: true); return true; }
            return false;
        }

        bool HandleInterrupt()
        {
            if (!GetCheckBox("Auto Interrupt")) return false;
            int castingID = Inferno.CastingID("target");
            if (castingID == 0 || !Inferno.IsInterruptable("target")) { _lastCastingID = 0; return false; }
            if (castingID != _lastCastingID) { _lastCastingID = castingID; int minPct = GetSlider("Interrupt at cast % (min)"); int maxPct = GetSlider("Interrupt at cast % (max)"); if (maxPct < minPct) maxPct = minPct; _interruptTargetPct = _rng.Next(minPct, maxPct + 1); }
            int elapsed = Inferno.CastingElapsed("target"); int remaining = Inferno.CastingRemaining("target"); int total = elapsed + remaining; if (total <= 0) return false;
            if ((elapsed * 100 / total) >= _interruptTargetPct && Inferno.CanCast("Mind Freeze", IgnoreGCD: true))
            { Inferno.Cast("Mind Freeze", QuickDelay: true); _lastCastingID = 0; return true; }
            return false;
        }

        int GCD() { return Inferno.GCD(); }
        bool HasBuff(string n) { return Inferno.BuffRemaining(n) > GCD(); }

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
        int GetPlayerHealthPct() { int hp = Inferno.Health("player"); int m = Inferno.MaxHealth("player"); if (m < 1) m = 1; return (hp * 100) / m; }
        bool Cast(string n) { if (Inferno.CanCast(n)) { Inferno.Cast(n); Inferno.PrintMessage(">> " + n, Color.White); return true; } return false; }
        bool CastCD(string n) { if ((Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false; if (n == "Dancing Rune Weapon" && !GetCheckBox("Use Dancing Rune Weapon")) return false; if (Inferno.CanCast(n)) { Inferno.Cast(n); Inferno.PrintMessage(">> " + n, Color.White); return true; } return false; }
    }
}
