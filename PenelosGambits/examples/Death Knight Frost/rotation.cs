using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API;

namespace InfernoWow.Modules
{
    /// <summary>
    /// Frost Death Knight - Translated from SimulationCraft Midnight APL
    /// Supports Breath of Sindragosa and Obliteration builds.
    /// ST and AoE sub-rotations with Frostscythe priority.
    /// </summary>
    public class FrostDeathKnightRotation : Rotation
    {
        List<string> Abilities = new List<string> {
            "Obliterate", "Frost Strike", "Howling Blast", "Frostscythe",
            "Remorseless Winter", "Glacial Advance", "Pillar of Frost",
            "Breath of Sindragosa", "Empower Rune Weapon", "Frostwyrm's Fury",
            "Raise Dead", "Reaper's Mark",
        };
        List<string> TalentChecks = new List<string> {
            "Obliteration", "Breath of Sindragosa", "Gathering Storm",
            "Shattering Blade", "Frostbound Will", "Killing Streak",
            "Icy Onslaught", "Bonegrinder", "Apocalypse Now",
            "Chosen of Frostbrood", "Frostscythe", "Frozen Dominion",
        };
        List<string> DefensiveSpells = new List<string> {
            "Icebound Fortitude", "Anti-Magic Shell", "Death Strike", "Lichborne",
        };
        List<string> UtilitySpells = new List<string> { "Mind Freeze", "Death Grip", "Chains of Ice" };

        const int HealthstoneItemID = 5512;
        const int RunicPowerType = 6;
        private Random _rng = new Random();
        private int _lastCastingID = 0;
        private int _interruptTargetPct = 0;

        public override void LoadSettings()
        {
            Settings.Add(new Setting("=== Frost Death Knight ==="));
            Settings.Add(new Setting("=== Offensive Cooldowns ==="));
            Settings.Add(new Setting("Use Pillar of Frost", true));
            Settings.Add(new Setting("Use Breath of Sindragosa", true));
            Settings.Add(new Setting("Use Empower Rune Weapon", true));
            Settings.Add(new Setting("Use Frostwyrm's Fury", true));
            Settings.Add(new Setting("Use Trinkets", true));
            Settings.Add(new Setting("=== Defensives ==="));
            Settings.Add(new Setting("Use Defensives", true));
            Settings.Add(new Setting("Icebound Fortitude HP %", 1, 100, 30));
            Settings.Add(new Setting("Anti-Magic Shell HP %", 1, 100, 60));
            Settings.Add(new Setting("Death Strike HP %", 1, 100, 50));
            Settings.Add(new Setting("Healthstone HP %", 1, 100, 50));
            Settings.Add(new Setting("=== Interrupt ==="));
            Settings.Add(new Setting("Auto Interrupt", true));
            Settings.Add(new Setting("Interrupt at cast % (min)", 0, 100, 40));
            Settings.Add(new Setting("Interrupt at cast % (max)", 0, 100, 90));
        }

        public override void Initialize()
        {
            Inferno.PrintMessage("             //////////////////////////////////////", Color.DeepSkyBlue);
            Inferno.PrintMessage("             //    FROST - DEATH KNIGHT (MID)    //", Color.DeepSkyBlue);
            Inferno.PrintMessage("             //              V 1.00              //", Color.DeepSkyBlue);
            Inferno.PrintMessage("             //////////////////////////////////////", Color.DeepSkyBlue);
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
            if (Inferno.CustomFunction("PetIsActive") == 0 && Inferno.CanCast("Raise Dead"))
            { Inferno.Cast("Raise Dead"); return true; }
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
            if ((HasBuff("Pillar of Frost")) && HandleRacials()) return true;
            if (HandleCooldowns()) return true;

            int enemies = Inferno.EnemiesNearUnit(8f, "player");
            if (enemies < 1) enemies = 1; if ((Inferno.IsCustomCodeOn("ForceST") || Inferno.IsCustomCodeOn("forcest"))) enemies = 1;

            // APL: run_action_list,name=aoe,if=active_enemies>=3
            if (enemies >= 3) return AoERotation(enemies);
            return SingleTarget(enemies);
        }

        // =====================================================================
        // COOLDOWNS (actions.cooldowns)
        // =====================================================================
        bool HandleCooldowns()
        {
            if ((Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;
            int enemies = Inferno.EnemiesNearUnit(8f, "player");
            if (enemies < 1) enemies = 1; if ((Inferno.IsCustomCodeOn("ForceST") || Inferno.IsCustomCodeOn("forcest"))) enemies = 1;
            bool hasBos = Inferno.IsSpellKnown("Breath of Sindragosa");
            bool bosActive = Inferno.BuffRemaining("Breath of Sindragosa") > GCD();
            bool hasPillar = HasBuff("Pillar of Frost");

            // antimagic_shell,if=runic_power.deficit>40 (offensive RP generation)
            int rpDeficit = Inferno.MaxPower("player", 6) - GetRP();
            if (rpDeficit > 40 && Inferno.CanCast("Anti-Magic Shell", IgnoreGCD: true))
            { Inferno.Cast("Anti-Magic Shell", QuickDelay: true); return true; }

            // remorseless_winter,if=variable.sending_cds&(active_enemies>1|talent.gathering_storm)|(buff.gathering_storm.stack=10&buff.remorseless_winter.remains<gcd.max)
            if (!Inferno.IsSpellKnown("Frozen Dominion") && ((enemies > 1 || Inferno.IsSpellKnown("Gathering Storm")) || (Inferno.BuffStacks("Gathering Storm") >= 10 && Inferno.BuffRemaining("Remorseless Winter") < GCD())))
                if (Cast("Remorseless Winter")) return true;

            // reapers_mark - hold until Pillar is about to be ready
            if (Inferno.SpellCooldown("Pillar of Frost") <= GCDMAX())
                if (Cast("Reaper's Mark")) return true;

            // pillar_of_frost - hold if Reaper's Mark is coming soon (within 20s)
            int rmCD = Inferno.SpellCooldown("Reaper's Mark");
            bool holdForRM = Inferno.IsSpellKnown("Reaper's Mark") && rmCD <= 20000;
            if (GetCheckBox("Use Pillar of Frost") && !holdForRM && (!hasBos || Inferno.SpellCooldown("Breath of Sindragosa") > 20000 || (Inferno.SpellCooldown("Breath of Sindragosa") <= GCDMAX() && GetRP() >= 60)))
                if (CastCD("Pillar of Frost")) return true;

            // breath_of_sindragosa,if=!buff.breath_of_sindragosa.up&buff.pillar_of_frost.up
            if (GetCheckBox("Use Breath of Sindragosa") && hasBos && !bosActive && hasPillar)
                if (Cast("Breath of Sindragosa")) return true;

            // frostwyrms_fury - simplified: use during Pillar or BoS
            if (GetCheckBox("Use Frostwyrm's Fury") && (hasPillar || bosActive))
                if (Cast("Frostwyrm's Fury")) return true;

            // raise_dead
            if (Cast("Raise Dead")) return true;

            // empower_rune_weapon,if=(rune<2|!buff.killing_machine.react)&runic_power<35
            if (GetCheckBox("Use Empower Rune Weapon") && (GetRunes() < 2 || !HasBuff("Killing Machine")) && GetRP() < 35)
                if (Cast("Empower Rune Weapon")) return true;

            // empower_rune_weapon - during Obliteration Pillar window
            if (GetCheckBox("Use Empower Rune Weapon") && Inferno.IsSpellKnown("Obliteration") && Inferno.BuffRemaining("Pillar of Frost") > 4 * GCD() && GetRunes() <= 2 && HasKM())
                if (Cast("Empower Rune Weapon")) return true;

            return false;
        }

        // =====================================================================
        // AoE ROTATION (actions.aoe)
        // =====================================================================
        bool AoERotation(int enemies)
        {
            bool hasFrostscythe = Inferno.IsSpellKnown("Frostscythe");

            // frostscythe,if=buff.killing_machine.react=2&active_enemies>=3
            if (hasFrostscythe && KMStacks() >= 2 && enemies >= 3 && Cast("Frostscythe")) return true;

            // frost_strike,if=debuff.razorice.react=5&buff.frostbane.react
            if (Inferno.DebuffStacks("Razorice") >= 5 && HasBuff("Frostbane") && Cast("Frost Strike")) return true;

            // frostscythe,if=buff.killing_machine.react&rune>=3&active_enemies>=3
            if (hasFrostscythe && HasKM() && GetRunes() >= 3 && enemies >= 3 && Cast("Frostscythe")) return true;

            // obliterate,if=buff.killing_machine.react=2|(buff.killing_machine.react&rune>=3)
            if (KMStacks() >= 2 || (HasKM() && GetRunes() >= 3))
                if (Cast("Obliterate")) return true;

            // howling_blast,if=buff.rime.react&talent.frostbound_will|!dot.frost_fever.ticking
            if ((HasBuff("Rime") && Inferno.IsSpellKnown("Frostbound Will")) || !HasFrostFever())
                if (Cast("Howling Blast")) return true;

            // frost_strike,if=debuff.razorice.react=5&talent.shattering_blade&active_enemies<5&!variable.rp_pooling
            if (Inferno.DebuffStacks("Razorice") >= 5 && Inferno.IsSpellKnown("Shattering Blade") && enemies < 5 && !IsRPPooling())
                if (Cast("Frost Strike")) return true;

            // frostscythe,if=buff.killing_machine.react&!variable.rune_pooling&active_enemies>=3
            if (hasFrostscythe && HasKM() && !IsRunePooling() && enemies >= 3 && Cast("Frostscythe")) return true;

            // obliterate,if=buff.killing_machine.react&!variable.rune_pooling
            if (HasKM() && !IsRunePooling() && Cast("Obliterate")) return true;

            // howling_blast,if=buff.rime.react
            if (HasBuff("Rime") && Cast("Howling Blast")) return true;

            // glacial_advance,if=!variable.rp_pooling
            if (!IsRPPooling() && Cast("Glacial Advance")) return true;

            // frostscythe,if=!variable.rune_pooling&!(talent.obliteration&buff.pillar_of_frost.up)&active_enemies>=3
            if (hasFrostscythe && !IsRunePooling() && !(Inferno.IsSpellKnown("Obliteration") && HasBuff("Pillar of Frost")) && enemies >= 3)
                if (Cast("Frostscythe")) return true;

            // obliterate,if=!variable.rune_pooling&!(talent.obliteration&buff.pillar_of_frost.up)
            if (!IsRunePooling() && !(Inferno.IsSpellKnown("Obliteration") && HasBuff("Pillar of Frost")))
                if (Cast("Obliterate")) return true;

            // howling_blast,if=!buff.killing_machine.react&talent.obliteration&buff.pillar_of_frost.up
            if (!HasKM() && Inferno.IsSpellKnown("Obliteration") && HasBuff("Pillar of Frost"))
                if (Cast("Howling Blast")) return true;

            return false;
        }

        // =====================================================================
        // SINGLE TARGET (actions.single_target)
        // =====================================================================
        bool SingleTarget(int enemies)
        {
            // obliterate,if=buff.killing_machine.react=2|(buff.killing_machine.react&rune>=3)
            if (KMStacks() >= 2 || (HasKM() && GetRunes() >= 3))
                if (Cast("Obliterate")) return true;

            // howling_blast,if=buff.rime.react&talent.frostbound_will
            if (HasBuff("Rime") && Inferno.IsSpellKnown("Frostbound Will") && Cast("Howling Blast")) return true;

            // frost_strike,if=debuff.razorice.react=5&talent.shattering_blade&!variable.rp_pooling
            if (Inferno.DebuffStacks("Razorice") >= 5 && Inferno.IsSpellKnown("Shattering Blade") && !IsRPPooling())
                if (Cast("Frost Strike")) return true;

            // howling_blast,if=buff.rime.react
            if (HasBuff("Rime") && Cast("Howling Blast")) return true;

            // frost_strike,if=!talent.shattering_blade&!variable.rp_pooling&runic_power.deficit<30
            if (!Inferno.IsSpellKnown("Shattering Blade") && !IsRPPooling() && GetRPDeficit() < 30)
                if (Cast("Frost Strike")) return true;

            // obliterate,if=buff.killing_machine.react&!variable.rune_pooling
            if (HasKM() && !IsRunePooling() && Cast("Obliterate")) return true;

            // frost_strike,if=!variable.rp_pooling
            if (!IsRPPooling() && Cast("Frost Strike")) return true;

            // obliterate,if=!variable.rune_pooling&!(talent.obliteration&buff.pillar_of_frost.up)
            if (!IsRunePooling() && !(Inferno.IsSpellKnown("Obliteration") && HasBuff("Pillar of Frost")))
                if (Cast("Obliterate")) return true;

            // howling_blast,if=!buff.killing_machine.react&talent.obliteration&buff.pillar_of_frost.up
            if (!HasKM() && Inferno.IsSpellKnown("Obliteration") && HasBuff("Pillar of Frost"))
                if (Cast("Howling Blast")) return true;

            return false;
        }

        // =====================================================================
        // DEFENSIVES / INTERRUPT / TRINKETS
        // =====================================================================
        bool HandleDefensives()
        {
            if (!GetCheckBox("Use Defensives")) return false;
            int hpPct = GetPlayerHealthPct();
            if (hpPct <= GetSlider("Icebound Fortitude HP %") && !HasBuff("Icebound Fortitude") && Inferno.CanCast("Icebound Fortitude", IgnoreGCD: true))
            { Inferno.Cast("Icebound Fortitude", QuickDelay: true); return true; }
            if (hpPct <= GetSlider("Anti-Magic Shell HP %") && Inferno.CanCast("Anti-Magic Shell", IgnoreGCD: true))
            { Inferno.Cast("Anti-Magic Shell", QuickDelay: true); return true; }
            if (hpPct <= GetSlider("Death Strike HP %") && GetRP() >= 35 && Inferno.CanCast("Death Strike"))
            { Inferno.Cast("Death Strike"); return true; }
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
            if (castPct >= _interruptTargetPct && Inferno.CanCast("Mind Freeze", IgnoreGCD: true))
            { Inferno.Cast("Mind Freeze", QuickDelay: true); _lastCastingID = 0; return true; }
            return false;
        }

        bool HandleTrinkets()
        {
            if (!GetCheckBox("Use Trinkets") || (Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;
            if (!HasBuff("Pillar of Frost")) return false;
            if (Inferno.CanUseEquippedItem(13)) { Inferno.Cast("trinket1"); return true; }
            if (Inferno.CanUseEquippedItem(14)) { Inferno.Cast("trinket2"); return true; }
            return false;
        }

        // =====================================================================
        // HELPERS
        // =====================================================================
        int GCD() { return Inferno.GCD(); }
        int GCDMAX() { int g = (int)(1500f / (1f + Inferno.Haste("player") / 100f)); return g < 750 ? 750 : g; }
        int GetRP() { return Inferno.Power("player", RunicPowerType); }
        int GetRPDeficit() { return Inferno.MaxPower("player", RunicPowerType) - GetRP(); }
        int GetRunes() { return Inferno.GetAvailableRunes(); }

        bool HasBuff(string name) { return Inferno.BuffRemaining(name) > GCD(); }
        bool HasFrostFever() { return Inferno.DebuffRemaining("Frost Fever") > GCD(); }
        bool HasKM() { return Inferno.BuffRemaining("Killing Machine") > GCD(); }
        int KMStacks() { return Inferno.BuffStacks("Killing Machine"); }

        // variable,name=rp_pooling: pool RP if BoS coming up soon
        bool IsRPPooling()
        {
            return Inferno.IsSpellKnown("Breath of Sindragosa")
                && Inferno.SpellCooldown("Breath of Sindragosa") < 4 * GCDMAX()
                && GetRP() < 60;
        }

        // variable,name=rune_pooling: pool runes when Reaper's Mark coming off CD (Deathbringer)
        bool IsRunePooling()
        {
            return Inferno.IsSpellKnown("Reaper's Mark")
                && Inferno.SpellCooldown("Reaper's Mark") < 6000
                && GetRunes() < 3;
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
            if (name == "Pillar of Frost" && !GetCheckBox("Use Pillar of Frost")) return false;
            if (name == "Empower Rune Weapon" && !GetCheckBox("Use Empower Rune Weapon")) return false;
            if (Inferno.CanCast(name)) { Inferno.Cast(name); Inferno.PrintMessage(">> " + name, Color.White); return true; }
            return false;
        }
    }
}
