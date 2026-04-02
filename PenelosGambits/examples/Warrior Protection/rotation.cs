using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API;

namespace InfernoWow.Modules
{
    /// <summary>
    /// Protection Warrior - Translated from SimulationCraft Midnight APL
    /// Hero trees: Colossus (Demolish, Colossal Might) vs Thane (Thunder Blast, Lightning Strikes)
    /// Sub-rotations: colossus_st, thane_st, aoe (3+ targets)
    /// Core: Shield Block uptime, Ignore Pain, Shield Slam priority, Ravager, Avatar windows.
    /// </summary>
    public class ProtectionWarriorRotation : Rotation
    {
        List<string> Abilities = new List<string> {
            "Shield Slam", "Thunder Clap", "Revenge", "Devastate", "Execute",
            "Shield Block", "Ignore Pain", "Avatar", "Demoralizing Shout",
            "Ravager", "Champion's Spear", "Champion's Leap",
            "Demolish", "Thunder Blast", "Shattering Throw", "Wrecking Throw",
            "Defensive Stance",
        };
        List<string> TalentChecks = new List<string> {
            "Demolish", "Lightning Strikes", "Booming Voice", "Heavy Repercussions",
            "Practiced Strikes", "Massacre", "Heavy Handed", "Barbaric Training",
            "Javelineer",
        };
        List<string> DefensiveSpells = new List<string> { "Shield Block", "Ignore Pain", "Shield Wall", "Last Stand" };
        List<string> UtilitySpells = new List<string> { "Pummel", "Battle Shout" };
        const int HealthstoneItemID = 5512; const int RagePowerType = 1;
        private Random _rng = new Random(); private int _lastCastingID = 0; private int _interruptTargetPct = 0;

        public override void LoadSettings()
        {
            Settings.Add(new Setting("=== Protection Warrior ==="));
            Settings.Add(new Setting("Use Avatar", true));
            Settings.Add(new Setting("Use Trinkets", true));
            Settings.Add(new Setting("=== Defensives ==="));
            Settings.Add(new Setting("Use Defensives", true));
            Settings.Add(new Setting("Shield Block uptime", true));
            Settings.Add(new Setting("Ignore Pain HP %", 1, 100, 70));
            Settings.Add(new Setting("Shield Wall HP %", 1, 100, 30));
            Settings.Add(new Setting("Last Stand HP %", 1, 100, 25));
            Settings.Add(new Setting("Healthstone HP %", 1, 100, 50));
            Settings.Add(new Setting("=== Interrupt ==="));
            Settings.Add(new Setting("Auto Interrupt", true));
            Settings.Add(new Setting("Interrupt at cast % (min)", 0, 100, 40));
            Settings.Add(new Setting("Interrupt at cast % (max)", 0, 100, 90));
        }

        public override void Initialize()
        {
            Inferno.PrintMessage("             //////////////////////////////////////", Color.DarkRed);
            Inferno.PrintMessage("             //  PROTECTION - WARRIOR (MID) V2   //", Color.DarkRed);
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

        public override bool OutOfCombatTick()
        {
            if (!Inferno.HasBuff("Defensive Stance") && Inferno.CanCast("Defensive Stance"))
            { Inferno.Cast("Defensive Stance"); return true; }
            if (!Inferno.HasBuff("Battle Shout") && Inferno.CanCast("Battle Shout"))
            { Inferno.Cast("Battle Shout"); return true; }
            return false;
        }

        public override bool CombatTick()
        {
            if (Inferno.IsDead("player") || Inferno.IsGhost("player")) return false;
            if (HandleDefensives()) return true;
            if (HandleInterrupt()) return true;
            if (!Inferno.UnitCanAttack("player", "target")) return false;
            if ((HasBuff("Avatar")) && HandleRacials()) return true;
            if (Inferno.IsChanneling("player")) return false;

            // Don't interrupt cast-time trinkets
            string _castName = Inferno.CastingName("player");
            if (_castName.Contains("Puzzle Box") || _castName.Contains("Emberwing")) return false;
            int enemies = Inferno.EnemiesNearUnit(8f, "player");
            if (enemies < 1) enemies = 1; if ((Inferno.IsCustomCodeOn("ForceST") || Inferno.IsCustomCodeOn("forcest"))) enemies = 1;
            int rage = Inferno.Power("player", RagePowerType);
            int rageDeficit = Inferno.MaxPower("player", RagePowerType) - rage;
            int targetHpPct = GetTargetHealthPct();
            bool executePhase = (Inferno.IsSpellKnown("Massacre") && targetHpPct < 35) || targetHpPct < 20;
            bool isColossus = Inferno.IsSpellKnown("Demolish");
            bool isThane = Inferno.IsSpellKnown("Lightning Strikes");
            int thunderBlastStacks = Inferno.BuffStacks("Thunder Blast");

            // Trinkets
            if (GetCheckBox("Use Trinkets") && !(Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds")))
            { if (Inferno.CanUseEquippedItem(13)) { Inferno.Cast("trinket1"); return true; } if (Inferno.CanUseEquippedItem(14)) { Inferno.Cast("trinket2"); return true; } }

            // avatar,if=thunder_blast.down|thunder_blast.stack<=2
            if (thunderBlastStacks <= 2)
                if (CastCD("Avatar")) return true;

            // ignore_pain - complex rage conditions simplified
            if (targetHpPct >= 20 && rageDeficit <= 20 && Inferno.CanCast("Ignore Pain", IgnoreGCD: true))
            { Inferno.Cast("Ignore Pain", QuickDelay: true); return true; }

            // ravager
            if (CastCD("Ravager")) return true;
            // demoralizing_shout,if=talent.booming_voice
            if (Inferno.IsSpellKnown("Booming Voice") && Cast("Demoralizing Shout")) return true;
            // champions_leap + champions_spear
            if (Cast("Champion's Leap")) return true;
            if (Cast("Champion's Spear")) return true;

            // thunder_blast,if=spell_targets>=2&stack=2
            if (enemies >= 2 && thunderBlastStacks >= 2)
                if (CastTC()) return true;
            // demolish,if=buff.colossal_might.stack>=3
            if (Inferno.BuffStacks("Colossal Might") >= 3)
                if (Cast("Demolish")) return true;
            // shield_charge
            // shield_block,if=remains<=10
            if (Inferno.BuffRemaining("Shield Block") <= 10000 && rage >= 30 && Inferno.CanCast("Shield Block", IgnoreGCD: true))
            { Inferno.Cast("Shield Block", QuickDelay: true); return true; }

            // Route to sub-rotation
            if (enemies >= 3) return AoE(rage, enemies, executePhase, thunderBlastStacks);
            if (isColossus) return ColossusST(rage, executePhase);
            if (isThane) return ThaneST(rage, executePhase, thunderBlastStacks);
            return ColossusST(rage, executePhase); // fallback
        }

        // =====================================================================
        // AOE (3+ targets)
        // =====================================================================
        bool AoE(int rage, int enemies, bool executePhase, int tbStacks)
        {
            // thunder_blast/thunder_clap for rend maintenance
            if (Inferno.DebuffRemaining("Rend") <= 1000)
            { if (CastTC()) return true; }
            // thunder_blast with avatar
            if (enemies >= 2 && HasBuff("Avatar"))
                if (CastTC()) return true;
            // execute cleave with Heavy Handed
            if (enemies >= 2 && (rage >= 50 || HasBuff("Sudden Death")) && Inferno.IsSpellKnown("Heavy Handed"))
                if (Cast("Execute")) return true;
            // thunder_clap with avatar in AoE
            if (enemies >= 4 && HasBuff("Avatar"))
                if (CastTC()) return true;
            // revenge rage dump
            if (rage >= 70 && enemies >= 3)
                if (Cast("Revenge")) return true;
            // shield_slam
            if (rage <= 60 || HasBuff("Violent Outburst"))
                if (Cast("Shield Slam")) return true;
            // thunder_clap
            if (CastTC()) return true;
            // revenge
            if (rage >= 30 || (rage >= 40 && Inferno.IsSpellKnown("Barbaric Training")))
                if (Cast("Revenge")) return true;
            return false;
        }

        // =====================================================================
        // COLOSSUS ST
        // =====================================================================
        bool ColossusST(int rage, bool executePhase)
        {
            if (Cast("Shield Slam")) return true;
            if (CastTC()) return true;
            // revenge,if=ravager up
            if (HasBuff("Ravager") && Cast("Revenge")) return true;
            // execute with deep wounds or rage
            if ((HasBuff("Sudden Death") || rage >= 40) && Cast("Execute")) return true;
            // revenge priority
            if ((rage >= 80 && !executePhase) || (HasBuff("Revenge!") && !executePhase))
                if (Cast("Revenge")) return true;
            // wrecking_throw / shattering_throw with javelineer
            if (Inferno.IsSpellKnown("Javelineer"))
            { if (Cast("Wrecking Throw")) return true; if (Cast("Shattering Throw")) return true; }
            // revenge fallback
            if (Cast("Revenge")) return true;
            if (Cast("Devastate")) return true;
            return false;
        }

        // =====================================================================
        // THANE ST
        // =====================================================================
        bool ThaneST(int rage, bool executePhase, int tbStacks)
        {
            if (CastTC()) return true;
            // thunder_clap,if=ravager up
            if (HasBuff("Ravager") && CastTC()) return true;
            if (Cast("Shield Slam")) return true;
            if (CastTC()) return true;
            // execute
            if ((HasBuff("Sudden Death") || rage >= 40) && Cast("Execute")) return true;
            // wrecking_throw / shattering_throw
            if (Inferno.IsSpellKnown("Javelineer"))
            { if (Cast("Wrecking Throw")) return true; if (Cast("Shattering Throw")) return true; }
            // revenge
            if ((rage >= 80 && !executePhase) || (HasBuff("Revenge!") && !executePhase))
                if (Cast("Revenge")) return true;
            if (Cast("Revenge")) return true;
            if (Cast("Devastate")) return true;
            return false;
        }

        // =====================================================================
        // DEFENSIVES / INTERRUPT
        // =====================================================================
        bool HandleDefensives()
        {
            if (!GetCheckBox("Use Defensives")) return false;
            int hpPct = GetPlayerHealthPct(); int rage = Inferno.Power("player", RagePowerType);
            if (GetCheckBox("Shield Block uptime") && Inferno.BuffRemaining("Shield Block") < 2000 && rage >= 30 && Inferno.CanCast("Shield Block", IgnoreGCD: true))
            { Inferno.Cast("Shield Block", QuickDelay: true); return true; }
            if (hpPct <= GetSlider("Last Stand HP %") && Inferno.CanCast("Last Stand", IgnoreGCD: true))
            { Inferno.Cast("Last Stand", QuickDelay: true); return true; }
            if (hpPct <= GetSlider("Shield Wall HP %") && Inferno.CanCast("Shield Wall", IgnoreGCD: true))
            { Inferno.Cast("Shield Wall", QuickDelay: true); return true; }
            if (hpPct <= GetSlider("Ignore Pain HP %") && rage >= 40 && Inferno.CanCast("Ignore Pain", IgnoreGCD: true))
            { Inferno.Cast("Ignore Pain", QuickDelay: true); return true; }
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
            if ((elapsed * 100 / total) >= _interruptTargetPct && Inferno.CanCast("Pummel", IgnoreGCD: true))
            { Inferno.Cast("Pummel", QuickDelay: true); _lastCastingID = 0; return true; }
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
        int GetTargetHealthPct() { int hp = Inferno.Health("target"); int m = Inferno.MaxHealth("target"); if (m < 1) return 100; return (hp * 100) / m; }
        bool CastTC()
        {
            if (Inferno.DistanceBetween("player", "target") > 8) return false;
            if (Inferno.CanCast("Thunder Clap")) { Inferno.Cast("Thunder Clap"); Inferno.PrintMessage(">> Thunder Clap", Color.White); return true; }
            return false;
        }

        bool Cast(string n) { if (Inferno.CanCast(n)) { Inferno.Cast(n); Inferno.PrintMessage(">> " + n, Color.White); return true; } return false; }
        bool CastCD(string n) { if ((Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false; if (n == "Avatar" && !GetCheckBox("Use Avatar")) return false; if (Inferno.CanCast(n)) { Inferno.Cast(n); Inferno.PrintMessage(">> " + n, Color.White); return true; } return false; }
    }
}
