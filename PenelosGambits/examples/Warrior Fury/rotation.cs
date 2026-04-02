using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API;

namespace InfernoWow.Modules
{
    /// <summary>
    /// Fury Warrior - Translated from SimulationCraft Midnight APL
    /// Auto-detects hero tree: Slayer (Slayer's Dominance) or Thane (Lightning Strikes).
    /// Each hero tree has ST and AoE sub-rotations.
    /// </summary>
    public class FuryWarriorRotation : Rotation
    {
        List<string> Abilities = new List<string> {
            "Rampage", "Bloodthirst", "Raging Blow", "Crushing Blow",
            "Execute", "Whirlwind", "Bladestorm", "Odyn's Fury",
            "Bloodbath", "Recklessness", "Avatar",
            "Thunder Clap", "Rend", "Storm Bolt", "Wrecking Throw",
            "Charge", "Heroic Leap", "Berserker Stance",
        };
        List<string> TalentChecks = new List<string> {
            "Slayer's Dominance", "Lightning Strikes", "Improved Whirlwind",
            "Deft Experience", "Massacre",
        };
        List<string> DefensiveSpells = new List<string> {
            "Enraged Regeneration", "Ignore Pain", "Rallying Cry",
        };
        List<string> UtilitySpells = new List<string> { "Pummel", "Battle Shout" };

        const int HealthstoneItemID = 5512;
        const int RagePowerType = 1;
        private Random _rng = new Random();
        private int _lastCastingID = 0;
        private int _interruptTargetPct = 0;

        public override void LoadSettings()
        {
            Settings.Add(new Setting("=== Fury Warrior ==="));
            Settings.Add(new Setting("Hero tree auto-detected: Slayer / Thane"));
            Settings.Add(new Setting("Use Recklessness", true));
            Settings.Add(new Setting("Use Avatar", true));
            Settings.Add(new Setting("Use Bladestorm", true));
            Settings.Add(new Setting("Use Trinkets", true));
            Settings.Add(new Setting("=== Defensives ==="));
            Settings.Add(new Setting("Use Defensives", true));
            Settings.Add(new Setting("Enraged Regeneration HP %", 1, 100, 50));
            Settings.Add(new Setting("Ignore Pain HP %", 1, 100, 70));
            Settings.Add(new Setting("Healthstone HP %", 1, 100, 50));
            Settings.Add(new Setting("=== Interrupt ==="));
            Settings.Add(new Setting("Auto Interrupt", true));
            Settings.Add(new Setting("Interrupt at cast % (min)", 0, 100, 40));
            Settings.Add(new Setting("Interrupt at cast % (max)", 0, 100, 90));
            Settings.Add(new Setting("=== Utility ==="));
            Settings.Add(new Setting("Auto Battle Shout", true));
        }

        public override void Initialize()
        {
            Inferno.PrintMessage("             //////////////////////////////////////", Color.OrangeRed);
            Inferno.PrintMessage("             //      FURY - WARRIOR (MIDNIGHT)   //", Color.OrangeRed);
            Inferno.PrintMessage("             //        SLAYER / THANE            //", Color.OrangeRed);
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
            if (!Inferno.HasBuff("Berserker Stance") && Inferno.CanCast("Berserker Stance"))
            { Inferno.Cast("Berserker Stance"); return true; }
            if (HandleBattleShout()) return true;
            return false;
        }

        public override bool CombatTick()
        {
            if (Inferno.IsDead("player") || Inferno.IsGhost("player")) return false;
            if (HandleDefensives()) return true;
            if (HandleInterrupt()) return true;
            if (HandleBattleShout()) return true;
            if (!Inferno.UnitCanAttack("player", "target")) return false;
            if (Inferno.IsChanneling("player")) return false;

            // Don't interrupt cast-time trinkets
            string _castName = Inferno.CastingName("player");
            if (_castName.Contains("Puzzle Box") || _castName.Contains("Emberwing")) return false;
                        if (HandleTrinkets()) return true;

            // Racial damage CDs
            if ((HasBuff("Recklessness")) && HandleRacials()) return true;

            int enemies = Inferno.EnemiesNearUnit(8f, "player");
            if (enemies < 1) enemies = 1; if ((Inferno.IsCustomCodeOn("ForceST") || Inferno.IsCustomCodeOn("forcest"))) enemies = 1;

            bool isSlayer = Inferno.IsSpellKnown("Slayer's Dominance");
            bool isThane = Inferno.IsSpellKnown("Lightning Strikes");

            // APL routing (lines 40-43)
            if (isSlayer)
            {
                if (enemies == 1) return Slayer(enemies);
                return SlayerAoE(enemies);
            }
            if (isThane)
            {
                if (enemies == 1) return Thane(enemies);
                return ThaneAoE(enemies);
            }

            // Fallback
            return Slayer(enemies);
        }

        // =====================================================================
        // SLAYER ST (actions.slayer)
        // =====================================================================
        bool Slayer(int enemies)
        {
            // recklessness
            if (CastCD("Recklessness")) return true;
            // avatar
            if (CastCD("Avatar")) return true;
            // rampage,if=buff.enrage.remains<gcd|rage>=100
            if ((EnrageRemaining() < GCD() || GetRage() >= 100) && Cast("Rampage")) return true;
            // bladestorm,if=(buff.enrage.up&talent.deft_experience|buff.enrage.remains>1)&(buff.recklessness.up|cooldown.recklessness.remains>30)
            if (GetCheckBox("Use Bladestorm") && ((IsEnraged() && Inferno.IsSpellKnown("Deft Experience")) || EnrageRemaining() > 1000) && (HasBuff("Recklessness") || Inferno.SpellCooldown("Recklessness") > 30000))
                if (Cast("Bladestorm")) return true;
            // odyns_fury
            if (Cast("Odyn's Fury")) return true;
            // bloodbath
            if (Cast("Bloodbath")) return true;
            // rampage,if=buff.recklessness.up
            if (HasBuff("Recklessness") && Cast("Rampage")) return true;
            // execute
            if (Cast("Execute")) return true;
            // crushing_blow
            if (Cast("Crushing Blow")) return true;
            // bloodthirst
            if (Cast("Bloodthirst")) return true;
            // rampage
            if (Cast("Rampage")) return true;
            // wrecking_throw
            if (Cast("Wrecking Throw")) return true;
            // rend,if=dot.rend.duration<6
            if (Inferno.DebuffRemaining("Rend") < 6000 && Cast("Rend")) return true;
            // raging_blow
            if (Cast("Raging Blow")) return true;
            // whirlwind
            if (CastWW()) return true;
            return false;
        }

        // =====================================================================
        // SLAYER AoE (actions.slayer_aoe)
        // =====================================================================
        bool SlayerAoE(int enemies)
        {
            // whirlwind,if=talent.improved_whirlwind&buff.whirlwind.stack=0
            if (Inferno.IsSpellKnown("Improved Whirlwind") && Inferno.BuffStacks("Whirlwind") == 0 && CastWW()) return true;
            // recklessness
            if (CastCD("Recklessness")) return true;
            // avatar
            if (CastCD("Avatar")) return true;
            // rampage,if=buff.enrage.remains<gcd|rage>=110
            if ((EnrageRemaining() < GCD() || GetRage() >= 110) && Cast("Rampage")) return true;
            // bladestorm,if=(buff.enrage.up&talent.deft_experience|buff.enrage.remains>1)&(buff.recklessness.up|cooldown.recklessness.remains>10)
            if (GetCheckBox("Use Bladestorm") && ((IsEnraged() && Inferno.IsSpellKnown("Deft Experience")) || EnrageRemaining() > 1000) && (HasBuff("Recklessness") || Inferno.SpellCooldown("Recklessness") > 10000))
                if (Cast("Bladestorm")) return true;
            // odyns_fury
            if (Cast("Odyn's Fury")) return true;
            // bloodbath
            if (Cast("Bloodbath")) return true;
            // execute,if=buff.sudden_death.up
            if (HasBuff("Sudden Death") && Cast("Execute")) return true;
            // rampage,if=buff.recklessness.up
            if (HasBuff("Recklessness") && Cast("Rampage")) return true;
            // whirlwind,if=talent.improved_whirlwind&buff.recklessness.up
            if (Inferno.IsSpellKnown("Improved Whirlwind") && HasBuff("Recklessness") && CastWW()) return true;
            // crushing_blow
            if (Cast("Crushing Blow")) return true;
            // bloodthirst
            if (Cast("Bloodthirst")) return true;
            // rend,if=dot.rend_dot.duration<6
            if (Inferno.DebuffRemaining("Rend") < 6000 && Cast("Rend")) return true;
            // execute
            if (Cast("Execute")) return true;
            // rampage
            if (Cast("Rampage")) return true;
            // whirlwind,if=talent.improved_whirlwind
            if (Inferno.IsSpellKnown("Improved Whirlwind") && CastWW()) return true;
            // raging_blow
            if (Cast("Raging Blow")) return true;
            return false;
        }

        // =====================================================================
        // THANE ST (actions.thane)
        // =====================================================================
        bool Thane(int enemies)
        {
            // odyns_fury
            if (Cast("Odyn's Fury")) return true;
            // recklessness
            if (CastCD("Recklessness")) return true;
            // avatar
            if (CastCD("Avatar")) return true;
            // rampage,if=buff.enrage.remains<gcd|rage>=100
            if ((EnrageRemaining() < GCD() || GetRage() >= 100) && Cast("Rampage")) return true;
            // thunder_blast,if=buff.thunder_blast.stack=2
            if (Inferno.BuffStacks("Thunder Blast") >= 1 && CastTC()) return true;
            // bloodbath
            if (Cast("Bloodbath")) return true;
            // rampage,if=buff.recklessness.up
            if (HasBuff("Recklessness") && Cast("Rampage")) return true;
            // thunder_blast,if=buff.avatar.up
            if (HasBuff("Avatar") && Inferno.BuffStacks("Thunder Blast") >= 1 && CastTC()) return true;
            // bloodthirst
            if (Cast("Bloodthirst")) return true;
            // execute
            if (Cast("Execute")) return true;
            // crushing_blow
            if (Cast("Crushing Blow")) return true;
            // thunder_blast
            if (Inferno.BuffStacks("Thunder Blast") >= 1 && CastTC()) return true;
            // rampage
            if (Cast("Rampage")) return true;
            // raging_blow
            if (Cast("Raging Blow")) return true;
            // thunder_clap
            if (CastTC()) return true;
            // whirlwind
            if (CastWW()) return true;
            return false;
        }

        // =====================================================================
        // THANE AoE (actions.thane_aoe)
        // =====================================================================
        bool ThaneAoE(int enemies)
        {
            // odyns_fury
            if (Cast("Odyn's Fury")) return true;
            // recklessness
            if (CastCD("Recklessness")) return true;
            // avatar
            if (CastCD("Avatar")) return true;
            // thunder_blast,if=buff.thunder_blast.stack=2
            if (Inferno.BuffStacks("Thunder Blast") >= 1 && CastTC()) return true;
            // thunder_blast,if=buff.avatar.up
            if (HasBuff("Avatar") && Inferno.BuffStacks("Thunder Blast") >= 1 && CastTC()) return true;
            // thunder_clap,if=talent.improved_whirlwind&buff.whirlwind.stack=0|(buff.avatar.up&active_enemies>6)
            if ((Inferno.IsSpellKnown("Improved Whirlwind") && Inferno.BuffStacks("Whirlwind") == 0) || (HasBuff("Avatar") && enemies > 6))
                if (CastTC()) return true;
            // rampage,if=buff.enrage.remains<gcd|rage>=100
            if ((EnrageRemaining() < GCD() || GetRage() >= 100) && Cast("Rampage")) return true;
            // bloodbath
            if (Cast("Bloodbath")) return true;
            // rampage,if=buff.recklessness.up
            if (HasBuff("Recklessness") && Cast("Rampage")) return true;
            // thunder_clap,if=buff.avatar.up
            if (HasBuff("Avatar") && CastTC()) return true;
            // bloodthirst
            if (Cast("Bloodthirst")) return true;
            // thunder_blast
            if (Inferno.BuffStacks("Thunder Blast") >= 1 && CastTC()) return true;
            // execute
            if (Cast("Execute")) return true;
            // thunder_clap
            if (CastTC()) return true;
            // crushing_blow
            if (Cast("Crushing Blow")) return true;
            // rampage
            if (Cast("Rampage")) return true;
            // raging_blow
            if (Cast("Raging Blow")) return true;
            // whirlwind
            if (CastWW()) return true;
            return false;
        }

        // =====================================================================
        // DEFENSIVES / INTERRUPT / UTILITY
        // =====================================================================
        bool HandleDefensives()
        {
            if (!GetCheckBox("Use Defensives")) return false;
            int hpPct = GetPlayerHealthPct();
            if (hpPct <= GetSlider("Enraged Regeneration HP %") && Inferno.CanCast("Enraged Regeneration", IgnoreGCD: true))
            { Inferno.Cast("Enraged Regeneration", QuickDelay: true); return true; }
            if (hpPct <= GetSlider("Ignore Pain HP %") && GetRage() >= 40 && Inferno.CanCast("Ignore Pain", IgnoreGCD: true))
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
            if (castingID != _lastCastingID) {
                _lastCastingID = castingID;
                int minPct = GetSlider("Interrupt at cast % (min)"); int maxPct = GetSlider("Interrupt at cast % (max)");
                if (maxPct < minPct) maxPct = minPct;
                _interruptTargetPct = _rng.Next(minPct, maxPct + 1);
            }
            int elapsed = Inferno.CastingElapsed("target"); int remaining = Inferno.CastingRemaining("target");
            int total = elapsed + remaining; if (total <= 0) return false;
            int castPct = (elapsed * 100) / total;
            if (castPct >= _interruptTargetPct && Inferno.CanCast("Pummel", IgnoreGCD: true))
            { Inferno.Cast("Pummel", QuickDelay: true); _lastCastingID = 0; return true; }
            return false;
        }

        bool HandleBattleShout()
        {
            if (!GetCheckBox("Auto Battle Shout")) return false;
            if (Inferno.BuffRemaining("Battle Shout") < GCD() && Inferno.CanCast("Battle Shout"))
            { Inferno.Cast("Battle Shout"); return true; }
            return false;
        }

        bool HandleTrinkets()
        {
            if (!GetCheckBox("Use Trinkets") || (Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;
            if (!HasBuff("Recklessness")) return false;
            if (Inferno.CanUseEquippedItem(13)) { Inferno.Cast("trinket1"); return true; }
            if (Inferno.CanUseEquippedItem(14)) { Inferno.Cast("trinket2"); return true; }
            return false;
        }

        // =====================================================================
        // HELPERS
        // =====================================================================
        int GCD() { return Inferno.GCD(); }
        int GetRage() { return Inferno.Power("player", RagePowerType); }
        bool IsEnraged() { return Inferno.BuffRemaining("Enrage") > GCD(); }
        int EnrageRemaining() { return Inferno.BuffRemaining("Enrage"); }
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

        // Whirlwind has no target range requirement so CanCast always passes — check 8yd range manually
        bool CastWW()
        {
            if (Inferno.DistanceBetween("player", "target") > 8) return false;
            if (Inferno.CanCast("Whirlwind")) { Inferno.Cast("Whirlwind"); Inferno.PrintMessage(">> Whirlwind", Color.White); return true; }
            return false;
        }

        bool CastTC()
        {
            if (Inferno.DistanceBetween("player", "target") > 8) return false;
            if (Inferno.CanCast("Thunder Clap")) { Inferno.Cast("Thunder Clap"); Inferno.PrintMessage(">> Thunder Clap", Color.White); return true; }
            return false;
        }

        bool CastCD(string name)
        {
            if ((Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;
            if (name == "Recklessness" && !GetCheckBox("Use Recklessness")) return false;
            if (name == "Avatar" && !GetCheckBox("Use Avatar")) return false;
            if (Inferno.CanCast(name)) { Inferno.Cast(name); Inferno.PrintMessage(">> " + name, Color.White); return true; }
            return false;
        }
    }
}
