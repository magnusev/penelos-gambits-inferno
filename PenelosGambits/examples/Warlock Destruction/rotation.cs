using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API;

namespace InfernoWow.Modules
{
    /// <summary>
    /// Destruction Warlock - Translated from SimulationCraft Midnight APL
    /// Auto-detects hero tree: Hellcaller (Wither) or Diabolist (Diabolic Ritual).
    /// Core: Immolate/Wither maintenance, Chaos Bolt spending, Summon Infernal window.
    /// AoE: Rain of Fire, Havoc, Cataclysm.
    /// </summary>
    public class DestructionWarlockRotation : Rotation
    {
        List<string> Abilities = new List<string> {
            "Incinerate", "Chaos Bolt", "Conflagrate", "Immolate", "Wither",
            "Rain of Fire", "Channel Demonfire", "Cataclysm", "Soul Fire",
            "Shadowburn", "Ruination", "Infernal Bolt", "Havoc",
            "Summon Infernal", "Malevolence",
        };
        List<string> TalentChecks = new List<string> {
            "Diabolic Ritual", "Internal Combustion", "Backdraft",
            "Lake of Fire", "Roaring Blaze", "Conflagration of Chaos",
            "Fire and Brimstone", "Avatar of Destruction",
            "Destructive Rapidity", "Grimoire of Sacrifice",
        };
        List<string> DefensiveSpells = new List<string> { "Unending Resolve", "Dark Pact" };
        List<string> UtilitySpells = new List<string> { "Spell Lock", "Summon Pet" };

        const int HealthstoneItemID = 5512;
        const int SoulShardType = 7;
        private Random _rng = new Random();
        private int _lastCastingID = 0;
        private int _interruptTargetPct = 0;

        public override void LoadSettings()
        {
            Settings.Add(new Setting("=== Destruction Warlock ==="));
            Settings.Add(new Setting("Use Summon Infernal", true));
            Settings.Add(new Setting("Use Malevolence", true));
            Settings.Add(new Setting("Use Trinkets", true));
            Settings.Add(new Setting("=== Defensives ==="));
            Settings.Add(new Setting("Use Defensives", true));
            Settings.Add(new Setting("Unending Resolve HP %", 1, 100, 40));
            Settings.Add(new Setting("Dark Pact HP %", 1, 100, 50));
            Settings.Add(new Setting("Healthstone HP %", 1, 100, 50));
            Settings.Add(new Setting("=== Interrupt ==="));
            Settings.Add(new Setting("Auto Interrupt", true));
            Settings.Add(new Setting("Interrupt at cast % (min)", 0, 100, 40));
            Settings.Add(new Setting("Interrupt at cast % (max)", 0, 100, 90));
        }

        public override void Initialize()
        {
            Inferno.PrintMessage("             //////////////////////////////////////", Color.OrangeRed);
            Inferno.PrintMessage("             //  DESTRUCTION - WARLOCK (MID)     //", Color.OrangeRed);
            Inferno.PrintMessage("             //   HELLCALLER / DIABOLIST         //", Color.OrangeRed);
            Inferno.PrintMessage("             //              V 1.00              //", Color.OrangeRed);
            Inferno.PrintMessage("             //////////////////////////////////////", Color.OrangeRed);
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
            if (!Inferno.UnitCanAttack("player", "target")) return false;
            if (Inferno.IsChanneling("player")) return false;

            // Don't interrupt cast-time trinkets
            string _castName = Inferno.CastingName("player");
            if (_castName.Contains("Puzzle Box") || _castName.Contains("Emberwing")) return false;
                        if (HandleTrinkets()) return true;

            // Racial damage CDs
            if ((HasBuff("Summon Infernal")) && HandleRacials()) return true;

            int enemies = Inferno.EnemiesNearUnit(10f, "target");
            if (enemies < 1) enemies = 1; if ((Inferno.IsCustomCodeOn("ForceST") || Inferno.IsCustomCodeOn("forcest"))) enemies = 1;
            bool isHC = Inferno.IsSpellKnown(445465);
            bool isDia = Inferno.IsSpellKnown("Diabolic Ritual");

            if (enemies >= 2)
            {
                if (isHC) return AoEHC(enemies);
                if (isDia) return AoEDia(enemies);
            }
            return SingleTarget(enemies);
        }

        // =====================================================================
        // SINGLE TARGET (actions main)
        // =====================================================================
        bool SingleTarget(int enemies)
        {
            int shards = GetShards();
            bool isHC = Inferno.IsSpellKnown(445465);

            // soul_fire,if=soul_shard<=4
            if (shards <= 4 && Cast("Soul Fire")) return true;
            // conflagrate,if=soul_shard<=4&buff.backdraft.stack<1
            if (shards <= 4 && Inferno.BuffStacks("Backdraft") < 1 && Cast("Conflagrate")) return true;
            // summon_infernal
            if (CastCD("Summon Infernal")) return true;
            // malevolence
            if (CastCD("Malevolence")) return true;
            // incinerate,if=buff.chaotic_inferno.up&soul_shard<=4
            if (HasBuff("Chaotic Inferno") && shards <= 4 && Cast("Incinerate")) return true;
            // shadowburn in execute or with procs
            if ((GetTargetHealthPct() <= 20 || HasBuff("Fiendish Cruelty") || Inferno.IsSpellKnown("Conflagration of Chaos"))
                && (isHC ? (shards >= 4 || HasBuff("Malevolence") || HasBuff("Infernal")) : true))
                if (Cast("Shadowburn")) return true;
            // wither/immolate maintenance
            if (isHC && Inferno.DebuffRemaining("Wither") < 5400 && Cast("Wither")) return true;
            if (!isHC && Inferno.DebuffRemaining("Immolate") < 5400 && Cast("Immolate")) return true;
            // ruination
            if (Cast("Ruination")) return true;
            // cataclysm,if=talent.lake_of_fire
            if (Inferno.IsSpellKnown("Lake of Fire") && Cast("Cataclysm")) return true;
            // chaos_bolt - HC: high shards or during CDs. Dia: variable ritual length
            if (isHC && (shards >= 4 || HasBuff("Malevolence") || HasBuff("Infernal")))
                if (Cast("Chaos Bolt")) return true;
            if (!isHC && Cast("Chaos Bolt")) return true;
            // infernal_bolt,if=soul_shard<=3
            if (shards <= 3 && Cast("Infernal Bolt")) return true;
            // channel_demonfire
            if (Cast("Channel Demonfire")) return true;
            // incinerate
            if (Cast("Incinerate")) return true;
            return false;
        }

        // =====================================================================
        // AoE HELLCALLER (actions.aoe_hc)
        // =====================================================================
        bool AoEHC(int enemies)
        {
            int shards = GetShards();
            if (CastCD("Summon Infernal")) return true;
            if (CastCD("Malevolence")) return true;
            // rain_of_fire at 4+ targets
            if (enemies >= 4 && shards >= 4 && Cast("Rain of Fire")) return true;
            // shadowburn with malevolence or low targets
            if (HasBuff("Malevolence") || HasBuff("Fiendish Cruelty") || enemies <= 3)
                if (Cast("Shadowburn")) return true;
            // cataclysm
            if (Cast("Cataclysm")) return true;
            // havoc
            if (Cast("Havoc")) return true;
            // rain_of_fire at 4+
            if (enemies >= 4 && Cast("Rain of Fire")) return true;
            // chaos_bolt at <=3 targets
            if (enemies <= 3 && Cast("Chaos Bolt")) return true;
            // soul_fire
            if (shards < 4 && Cast("Soul Fire")) return true;
            // wither maintenance
            if (Inferno.DebuffRemaining("Wither") < 5400 && Cast("Wither")) return true;
            // incinerate with backdraft
            if (Inferno.IsSpellKnown("Fire and Brimstone") && HasBuff("Backdraft") && Cast("Incinerate")) return true;
            // conflagrate
            if (Inferno.BuffStacks("Backdraft") < 2 && Cast("Conflagrate")) return true;
            if (Cast("Incinerate")) return true;
            return false;
        }

        // =====================================================================
        // AoE DIABOLIST (actions.aoe_dia)
        // =====================================================================
        bool AoEDia(int enemies)
        {
            int shards = GetShards();
            if (CastCD("Summon Infernal")) return true;
            // chaos_bolt with diabolic ritual at <=4 targets
            if (enemies <= 4 && Cast("Chaos Bolt")) return true;
            // rain_of_fire at 4+ targets
            if (enemies >= 4 && shards >= 3 && Cast("Rain of Fire")) return true;
            // shadowburn
            if (enemies <= 3 || Inferno.IsSpellKnown("Conflagration of Chaos"))
                if (Cast("Shadowburn")) return true;
            // ruination
            if (Cast("Ruination")) return true;
            // cataclysm
            if (Cast("Cataclysm")) return true;
            // havoc
            if (Cast("Havoc")) return true;
            // infernal_bolt
            if (shards < 3 && Cast("Infernal Bolt")) return true;
            // soul_fire
            if (shards < 4 && enemies <= 5 && Cast("Soul Fire")) return true;
            // immolate maintenance
            if (Inferno.DebuffRemaining("Immolate") < 5400 && Cast("Immolate")) return true;
            // conflagrate
            if (Cast("Conflagrate")) return true;
            if (Cast("Incinerate")) return true;
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
            if (!HasBuff("Infernal")) return false;
            if (Inferno.CanUseEquippedItem(13)) { Inferno.Cast("trinket1"); return true; }
            if (Inferno.CanUseEquippedItem(14)) { Inferno.Cast("trinket2"); return true; }
            return false;
        }

        int GCD() { return Inferno.GCD(); }
        int GetShards()
        {
            int shards = Inferno.Power("player", 7)/10;
            string casting = Inferno.CastingName("player");
            if (casting == "Soul Fire") shards += 1;
            if (casting == "Chaos Bolt") shards -= 2;
            return Math.Max(0, Math.Min(shards, 5));
        }
        bool HasBuff(string name) { return Inferno.BuffRemaining(name) > GCD(); }
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
        bool Cast(string name) { if (Inferno.CanCast(name)) { Inferno.Cast(name); Inferno.PrintMessage(">> " + name, Color.White); return true; } return false; }
        bool CastCD(string name)
        {
            if ((Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;
            if (name == "Summon Infernal" && !GetCheckBox("Use Summon Infernal")) return false;
            if (name == "Malevolence" && !GetCheckBox("Use Malevolence")) return false;
            if (Inferno.CanCast(name)) { Inferno.Cast(name); Inferno.PrintMessage(">> " + name, Color.White); return true; } return false;
        }
    }
}
