using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API;

namespace InfernoWow.Modules
{
    /// <summary>
    /// Affliction Warlock - Translated from SimulationCraft Midnight APL
    /// Auto-detects hero tree: Soul Harvester or Hellcaller.
    /// Each has ST, Cleave (2), and AoE (3+) sub-rotations.
    /// Core: Agony/Corruption/Wither DoT maintenance, UA spending, Darkglare windows.
    /// </summary>
    public class AfflictionWarlockRotation : Rotation
    {
        List<string> Abilities = new List<string> {
            "Agony", "Corruption", "Wither", "Unstable Affliction",
            "Drain Soul", "Shadow Bolt", "Malefic Grasp",
            "Seed of Corruption", "Haunt", "Dark Harvest",
            "Summon Darkglare", "Malevolence",
        };
        List<string> TalentChecks = new List<string> {
            "Grimoire of Sacrifice", "Sow the Seeds", "Patient Zero",
            "Nocturnal Yield", "Nightfall", "Cascading Calamity",
            "Shared Agony", "Absolute Corruption",
        };
        List<string> DefensiveSpells = new List<string> { "Unending Resolve", "Dark Pact", "Drain Life" };
        List<string> UtilitySpells = new List<string> { "Spell Lock", "Summon Pet" };

        const int HealthstoneItemID = 5512;
        const int SoulShardType = 7;
        private Random _rng = new Random();
        private int _lastCastingID = 0;
        private int _interruptTargetPct = 0;

        public override void LoadSettings()
        {
            Settings.Add(new Setting("=== Affliction Warlock ==="));
            Settings.Add(new Setting("Hero tree auto-detected: Soul Harvester / Hellcaller"));
            Settings.Add(new Setting("Use Summon Darkglare", true));
            Settings.Add(new Setting("Use Malevolence", true));
            Settings.Add(new Setting("Use Trinkets", true));
            Settings.Add(new Setting("=== Defensives ==="));
            Settings.Add(new Setting("Use Defensives", true));
            Settings.Add(new Setting("Unending Resolve HP %", 1, 100, 40));
            Settings.Add(new Setting("Dark Pact HP %", 1, 100, 50));
            Settings.Add(new Setting("Drain Life HP %", 1, 100, 30));
            Settings.Add(new Setting("Healthstone HP %", 1, 100, 50));
            Settings.Add(new Setting("=== Interrupt ==="));
            Settings.Add(new Setting("Auto Interrupt", true));
            Settings.Add(new Setting("Interrupt at cast % (min)", 0, 100, 40));
            Settings.Add(new Setting("Interrupt at cast % (max)", 0, 100, 90));
        }

        public override void Initialize()
        {
            Inferno.PrintMessage("             //////////////////////////////////////", Color.MediumPurple);
            Inferno.PrintMessage("             //   AFFLICTION - WARLOCK (MID)     //", Color.MediumPurple);
            Inferno.PrintMessage("             //  SOUL HARVESTER / HELLCALLER     //", Color.MediumPurple);
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
            Macros.Add("pet_spell_lock", "/cast [@target] Command Demon");
            CustomFunctions.Add("HasHealthstone", "return GetItemCount(5512) > 0 and 1 or 0");
            CustomFunctions.Add("PetIsActive", "return (UnitExists('pet') and not UnitIsDead('pet')) and 1 or 0");
            // Racial abilities
            foreach (string r in new string[] { "Berserking", "Blood Fury", "Ancestral Call", "Fireblood", "Lights Judgment" }) Spellbook.Add(r);
            CustomCommands.Add("NoCDs"); CustomCommands.Add("nocds");
            CustomCommands.Add("ForceST"); CustomCommands.Add("forcest");
        }

        public override bool OutOfCombatTick()
        {
            if (Inferno.CustomFunction("PetIsActive") == 0 && Inferno.CanCast("Summon Pet"))
            { Inferno.Cast("Summon Pet"); return true; }
            return false;
        }

        public override bool CombatTick()
        {
            if (Inferno.IsDead("player") || Inferno.IsGhost("player")) return false;
            if (HandleDefensives()) return true;
            if (HandleInterrupt()) return true;

            // Don't interrupt Drain Soul/Malefic Grasp channels
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
            bool isHC = Inferno.IsSpellKnown(445465);
            int shards = GetShards();

            // end_of_fight: dump shards when target is about to die
            int targetHpPct = Inferno.Health("target") * 100 / Math.Max(1, Inferno.MaxHealth("target"));
            if (targetHpPct < 10)
            {
                if (shards > 0 && !Inferno.IsSpellKnown("Patient Zero") && !Inferno.IsSpellKnown("Sow the Seeds"))
                    if (Cast("Unstable Affliction")) return true;
                if (shards > 0 && Inferno.IsSpellKnown("Patient Zero") && Inferno.IsSpellKnown("Sow the Seeds"))
                    if (Cast("Seed of Corruption")) return true;
                if (HasBuff("Nightfall"))
                { if (Cast("Drain Soul")) return true; if (Cast("Shadow Bolt")) return true; }
            }

            if (isHC)
            {
                if (enemies == 1) return HC_ST(enemies);
                if (enemies == 2) return HC_Cleave(enemies);
                return HC_AoE(enemies);
            }
            else
            {
                if (enemies == 1) return SH_ST(enemies);
                if (enemies == 2) return SH_Cleave(enemies);
                return SH_AoE(enemies);
            }
        }

        // =====================================================================
        // HELLCALLER ST
        // =====================================================================
        bool HC_ST(int enemies)
        {
            if (Cast("Haunt")) return true;
            if (Inferno.DebuffRemaining("Agony") < 5400 && Cast("Agony")) return true;
            if (WitherNeedsRefresh() && Cast("Wither")) return true;
            if (Cast("Dark Harvest")) return true;
            if (Inferno.DebuffRemaining("Agony") < 20000 && Inferno.SpellCooldown("Summon Darkglare") < GCDMAX())
                if (Cast("Agony")) return true;
            if (CastCD("Summon Darkglare")) return true;
            if (CastCD("Malevolence")) return true;
            // Malefic Grasp/Drain Soul with procs
            if ((Inferno.BuffStacks("Nightfall") > 1 || Inferno.BuffRemaining("Darkglare") < GCD()) && Cast("Malefic Grasp")) return true;
            if (Inferno.BuffStacks("Nightfall") > 1 && Cast("Drain Soul")) return true;
            // UA during CDs or to avoid capping
            bool cdsActive = Inferno.BuffRemaining("Darkglare") > GCD() || Inferno.BuffRemaining("Malevolence") > GCD();
            if (cdsActive || GetShards() > 4 || HasBuff("Shard Instability") || Inferno.BuffRemaining("Cascading Calamity") < GCD())
                if (Cast("Unstable Affliction")) return true;
            // Fillers
            if (Cast("Drain Soul")) return true;
            if (Cast("Shadow Bolt")) return true;
            return false;
        }

        // =====================================================================
        // HELLCALLER CLEAVE
        // =====================================================================
        bool HC_Cleave(int enemies)
        {
            if (Cast("Haunt")) return true;
            // Seed for Wither spread
            if (WitherNeedsRefresh() && Cast("Seed of Corruption")) return true;
            if (Inferno.DebuffRemaining("Agony") < 5400 && Cast("Agony")) return true;
            if (Cast("Dark Harvest")) return true;
            if (CastCD("Summon Darkglare")) return true;
            if (CastCD("Malevolence")) return true;
            if ((Inferno.BuffStacks("Nightfall") > 1 || Inferno.BuffRemaining("Darkglare") < GCD()) && Cast("Malefic Grasp")) return true;
            bool cdsActive = Inferno.BuffRemaining("Darkglare") > GCD() || Inferno.BuffRemaining("Malevolence") > GCD();
            if (!Inferno.IsSpellKnown("Sow the Seeds") && !Inferno.IsSpellKnown("Patient Zero") && (cdsActive || GetShards() > 4 || HasBuff("Shard Instability") || Inferno.BuffRemaining("Cascading Calamity") < GCD()))
                if (Cast("Unstable Affliction")) return true;
            if (Inferno.IsSpellKnown("Patient Zero") && Inferno.IsSpellKnown("Sow the Seeds"))
                if (Cast("Seed of Corruption")) return true;
            if (Cast("Drain Soul")) return true;
            if (Cast("Shadow Bolt")) return true;
            return false;
        }

        // =====================================================================
        // HELLCALLER AoE
        // =====================================================================
        bool HC_AoE(int enemies)
        {
            if (Cast("Haunt")) return true;
            if (WitherNeedsRefresh() && Cast("Seed of Corruption")) return true;
            if (Cast("Dark Harvest")) return true;
            if (Inferno.DebuffRemaining("Agony") < 5000 && Cast("Agony")) return true;
            if (CastCD("Summon Darkglare")) return true;
            if (CastCD("Malevolence")) return true;
            if (Cast("Seed of Corruption")) return true;
            if (HasBuff("Shard Instability") && Cast("Unstable Affliction")) return true;
            if (Cast("Malefic Grasp")) return true;
            if (Cast("Drain Soul")) return true;
            if (Cast("Shadow Bolt")) return true;
            return false;
        }

        // =====================================================================
        // SOUL HARVESTER ST
        // =====================================================================
        bool SH_ST(int enemies)
        {
            if (Cast("Haunt")) return true;
            if (Inferno.DebuffRemaining("Agony") < 5400 && Cast("Agony")) return true;
            if (CorruptionNeedsRefresh() && Cast("Corruption")) return true;
            if (GetShards() < 3 && Cast("Dark Harvest")) return true;
            if (Inferno.SpellCooldown("Dark Harvest") > 0 && CastCD("Summon Darkglare")) return true;
            if ((Inferno.BuffStacks("Nightfall") > 1 || Inferno.BuffRemaining("Darkglare") < GCD()) && Cast("Malefic Grasp")) return true;
            if (Inferno.BuffStacks("Nightfall") > 1 && Cast("Drain Soul")) return true;
            if (GetShards() > 0 || HasBuff("Shard Instability"))
                if (Cast("Unstable Affliction")) return true;
            if (Cast("Drain Soul")) return true;
            if (Cast("Shadow Bolt")) return true;
            return false;
        }

        // =====================================================================
        // SOUL HARVESTER CLEAVE
        // =====================================================================
        bool SH_Cleave(int enemies)
        {
            if (Cast("Haunt")) return true;
            if (CorruptionNeedsRefresh() && Cast("Seed of Corruption")) return true;
            if (Cast("Dark Harvest")) return true;
            if (Inferno.DebuffRemaining("Agony") < 5400 && Cast("Agony")) return true;
            if (CastCD("Summon Darkglare")) return true;
            if ((Inferno.BuffStacks("Nightfall") > 1 || Inferno.BuffRemaining("Darkglare") < GCD()) && Cast("Malefic Grasp")) return true;
            if (!Inferno.IsSpellKnown("Patient Zero") && !Inferno.IsSpellKnown("Sow the Seeds") && (GetShards() > 0 || HasBuff("Shard Instability")))
                if (Cast("Unstable Affliction")) return true;
            if (Inferno.IsSpellKnown("Patient Zero") && Inferno.IsSpellKnown("Sow the Seeds"))
                if (Cast("Seed of Corruption")) return true;
            if (Cast("Drain Soul")) return true;
            if (Cast("Shadow Bolt")) return true;
            return false;
        }

        // =====================================================================
        // SOUL HARVESTER AoE
        // =====================================================================
        bool SH_AoE(int enemies)
        {
            if (Cast("Haunt")) return true;
            if (CorruptionNeedsRefresh() && Cast("Seed of Corruption")) return true;
            if (Cast("Dark Harvest")) return true;
            if (Inferno.DebuffRemaining("Agony") < 5000 && Cast("Agony")) return true;
            if (CastCD("Summon Darkglare")) return true;
            if (Inferno.IsSpellKnown("Sow the Seeds") && Cast("Seed of Corruption")) return true;
            if (!Inferno.IsSpellKnown("Sow the Seeds") && Cast("Unstable Affliction")) return true;
            if (Cast("Malefic Grasp")) return true;
            if (Cast("Drain Soul")) return true;
            if (Cast("Shadow Bolt")) return true;
            return false;
        }

        // =====================================================================
        // DEFENSIVES / INTERRUPT / TRINKETS
        // =====================================================================
        bool HandleDefensives()
        {
            if (!GetCheckBox("Use Defensives")) return false;
            int hpPct = GetPlayerHealthPct();
            if (hpPct <= GetSlider("Unending Resolve HP %") && Inferno.CanCast("Unending Resolve", IgnoreGCD: true))
            { Inferno.Cast("Unending Resolve", QuickDelay: true); return true; }
            if (hpPct <= GetSlider("Dark Pact HP %") && Inferno.CanCast("Dark Pact", IgnoreGCD: true))
            { Inferno.Cast("Dark Pact", QuickDelay: true); return true; }
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
            if (castPct >= _interruptTargetPct && Inferno.CustomFunction("PetIsActive") == 1)
            { Inferno.Cast("pet_spell_lock", QuickDelay: true); _lastCastingID = 0; return true; }
            return false;
        }

        bool HandleTrinkets()
        {
            if (!GetCheckBox("Use Trinkets") || (Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;
            bool cdsActive = Inferno.BuffRemaining("Darkglare") > GCD();
            if (!cdsActive) return false;
            if (Inferno.CanUseEquippedItem(13)) { Inferno.Cast("trinket1"); return true; }
            if (Inferno.CanUseEquippedItem(14)) { Inferno.Cast("trinket2"); return true; }
            return false;
        }

        // =====================================================================
        // HELPERS
        // =====================================================================
        int GCD() { return Inferno.GCD(); }
        int GCDMAX() { int g = (int)(1500f / (1f + Inferno.Haste("player") / 100f)); return g < 750 ? 750 : g; }
        int GetShards()
        {
            int shards = Inferno.Power("player", 7)/10;
            string casting = Inferno.CastingName("player");
            if (casting == "Shadow Bolt") shards += 1;
            if (casting == "Unstable Affliction") shards -= 1;
            if (casting == "Seed of Corruption") shards -= 1;
            return Math.Max(0, Math.Min(shards, 5));
        }
        bool HasBuff(string name) { return Inferno.BuffRemaining(name) > GCD(); }

        // Wither: with Absolute Corruption it's permanent (remaining=0), just check HasDebuff
        bool WitherNeedsRefresh()
        {
            if (Inferno.IsSpellKnown("Absolute Corruption"))
                return !Inferno.HasDebuff("Wither");
            return Inferno.DebuffRemaining("Wither") < 5400;
        }

        // Corruption: with Absolute Corruption it's permanent, just check HasDebuff
        bool CorruptionNeedsRefresh()
        {
            if (Inferno.HasDebuff("Wither")) return false; // Using Wither instead
            if (Inferno.IsSpellKnown("Absolute Corruption"))
                return !Inferno.HasDebuff("Corruption");
            return Inferno.DebuffRemaining("Corruption") < 5400;
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
            if (name == "Summon Darkglare" && !GetCheckBox("Use Summon Darkglare")) return false;
            if (name == "Malevolence" && !GetCheckBox("Use Malevolence")) return false;
            if (Inferno.CanCast(name)) { Inferno.Cast(name); Inferno.PrintMessage(">> " + name, Color.White); return true; }
            return false;
        }
    }
}
