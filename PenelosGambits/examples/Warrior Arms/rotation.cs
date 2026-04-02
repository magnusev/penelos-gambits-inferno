using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API;

namespace InfernoWow.Modules
{
    /// <summary>
    /// Arms Warrior - Translated from SimulationCraft Midnight APL
    /// Auto-detects hero tree: Colossus (Demolish) or Slayer (Slayer's Dominance).
    /// Each hero tree has AoE, Execute, and ST sub-rotations.
    /// </summary>
    public class ArmsWarriorRotation : Rotation
    {
        // All abilities used in the APL
        List<string> Abilities = new List<string> {
            "Mortal Strike", "Overpower", "Slam", "Execute",
            "Whirlwind", "Cleave", "Bladestorm", "Sweeping Strikes",
            "Colossus Smash", "Ravager", "Thunder Clap", "Rend",
            "Demolish", "Heroic Strike", "Champion's Spear",
            "Avatar", "Storm Bolt", "Wrecking Throw", "Charge",
            "Battle Stance",
        };

        // Talents referenced in APL conditions
        List<string> TalentChecks = new List<string> {
            "Demolish", "Slayer's Dominance", "Massacre",
            "Broad Strokes", "Bloodletting", "Battlelord",
            "Critical Thinking", "Deep Wounds", "Dreadnaught",
            "Executioner's Precision", "Fervor of Battle",
            "Fierce Followthrough", "Improved Execute",
            "Martial Prowess", "Mass Execution", "Opportunist",
            "Rend", "Master of Warfare", "Collateral Damage",
        };

        List<string> DefensiveSpells = new List<string> {
            "Die by the Sword", "Ignore Pain", "Rallying Cry",
        };

        List<string> UtilitySpells = new List<string> {
            "Pummel", "Battle Shout",
        };

        const int HealthstoneItemID = 5512;
        const int RagePowerType = 1;
        private Random _rng = new Random();
        private int _lastCastingID = 0;
        private int _interruptTargetPct = 0;

        public override void LoadSettings()
        {
            Settings.Add(new Setting("=== Arms Warrior ==="));
            Settings.Add(new Setting("Hero tree auto-detected: Colossus (Demolish) / Slayer"));

            Settings.Add(new Setting("=== Offensive Cooldowns ==="));
            Settings.Add(new Setting("Use Avatar", true));
            Settings.Add(new Setting("Use Colossus Smash", true));
            Settings.Add(new Setting("Use Champion's Spear", true));
            Settings.Add(new Setting("Use Bladestorm", true));
            Settings.Add(new Setting("Use Trinkets", true));

            Settings.Add(new Setting("=== Defensives ==="));
            Settings.Add(new Setting("Use Defensives", true));
            Settings.Add(new Setting("Die by the Sword HP %", 1, 100, 40));
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
            Inferno.PrintMessage("             //////////////////////////////////////", Color.Firebrick);
            Inferno.PrintMessage("             //                                  //", Color.Firebrick);
            Inferno.PrintMessage("             //      ARMS - WARRIOR (MIDNIGHT)   //", Color.Firebrick);
            Inferno.PrintMessage("             //       COLOSSUS / SLAYER          //", Color.Firebrick);
            Inferno.PrintMessage("             //              V 1.00              //", Color.Firebrick);
            Inferno.PrintMessage("             //                                  //", Color.Firebrick);
            Inferno.PrintMessage("             //////////////////////////////////////", Color.Firebrick);
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
            if (!Inferno.HasBuff("Battle Stance") && Inferno.CanCast("Battle Stance"))
            { Inferno.Cast("Battle Stance"); return true; }
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

            // APL: call_action_list,name=trinkets
                        if (HandleTrinkets()) return true;

            // Racial damage CDs
            if ((HasBuff("Colossus Smash") || HasBuff("Avatar")) && HandleRacials()) return true;

            int enemies = Inferno.EnemiesNearUnit(8f, "player");
            if (enemies < 1) enemies = 1; if ((Inferno.IsCustomCodeOn("ForceST") || Inferno.IsCustomCodeOn("forcest"))) enemies = 1;

            // APL variable: execute_phase = (talent.massacre&target.health.pct<35)|target.health.pct<20
            bool executePhase = (Inferno.IsSpellKnown("Massacre") && GetTargetHealthPct() < 35) || GetTargetHealthPct() < 20;

            // Hero tree routing (APL lines 40-45)
            bool isColossus = Inferno.IsSpellKnown("Demolish");
            bool isSlayer = Inferno.IsSpellKnown("Slayer's Dominance");

            if (isColossus)
            {
                if (enemies > 2) return ColossusAoE(enemies);
                if (executePhase) return ColossusExecute(enemies);
                return ColossusST(enemies);
            }

            if (isSlayer)
            {
                if (enemies > 2) return SlayerAoE(enemies);
                if (executePhase) return SlayerExecute(enemies);
                return SlayerST(enemies);
            }

            // Fallback: use Colossus ST if no hero tree detected
            return ColossusST(enemies);
        }

        // =====================================================================
        // COLOSSUS AoE (actions.colossus_aoe)
        // =====================================================================
        bool ColossusAoE(int enemies)
        {
            // thunder_clap,if=!dot.rend_dot.remains
            if (RendRemaining() == 0 && CastTC()) return true;
            // rend,if=!dot.rend_dot.remains
            if (RendRemaining() == 0 && Cast("Rend")) return true;
            // sweeping_strikes,if=cooldown.colossus_smash.remains>10&buff.sweeping_strikes.down|!talent.broad_strokes
            if ((Inferno.SpellCooldown("Colossus Smash") > 10000 && !HasBuff("Sweeping Strikes")) || !Inferno.IsSpellKnown("Broad Strokes"))
                if (Cast("Sweeping Strikes")) return true;
            // ravager,if=cooldown.colossus_smash.remains<3
            if (Inferno.SpellCooldown("Colossus Smash") < 3000 && Cast("Ravager")) return true;
            // avatar
            if (CastCD("Avatar")) return true;
            // colossus_smash
            if (CastCD("Colossus Smash")) return true;
            // champions_spear
            if (CastCD("Champion's Spear")) return true;
            // demolish,if=buff.colossal_might.stack=10
            if (BuffStacks("Colossal Might") >= 10 && Cast("Demolish")) return true;
            // cleave
            if (Cast("Cleave")) return true;
            // demolish,if=debuff.colossus_smash.remains>=2
            if (Inferno.DebuffRemaining("Colossus Smash") >= 2000 && Cast("Demolish")) return true;
            // whirlwind,if=talent.fervor_of_battle&buff.collateral_damage.stack=3
            if (Inferno.IsSpellKnown("Fervor of Battle") && BuffStacks("Collateral Damage") >= 3 && CastWW()) return true;
            // mortal_strike
            if (Cast("Mortal Strike")) return true;
            // rend,if=dot.rend_dot.remains<4
            if (RendRemaining() < 4000 && Cast("Rend")) return true;
            // overpower
            if (Cast("Overpower")) return true;
            // execute,if=buff.sudden_death.remains
            if (HasBuff("Sudden Death") && Cast("Execute")) return true;
            // heroic_strike
            if (Cast("Heroic Strike")) return true;
            // rend
            if (Cast("Rend")) return true;
            // slam
            if (Cast("Slam")) return true;
            // execute
            if (Cast("Execute")) return true;
            // bladestorm
            if (GetCheckBox("Use Bladestorm") && Cast("Bladestorm")) return true;
            // wrecking_throw
            if (Cast("Wrecking Throw")) return true;
            // whirlwind
            if (CastWW()) return true;
            return false;
        }

        // =====================================================================
        // COLOSSUS Execute (actions.colossus_execute)
        // =====================================================================
        bool ColossusExecute(int enemies)
        {
            // sweeping_strikes,if=active_enemies=2&(cooldown.colossus_smash.remains&buff.sweeping_strikes.down|!talent.broad_strokes)
            if (enemies == 2 && ((Inferno.SpellCooldown("Colossus Smash") > 0 && !HasBuff("Sweeping Strikes")) || !Inferno.IsSpellKnown("Broad Strokes")))
                if (Cast("Sweeping Strikes")) return true;
            // rend,if=dot.rend_dot.remains<=gcd&!talent.bloodletting
            if (RendRemaining() <= GCD() && !Inferno.IsSpellKnown("Bloodletting") && Cast("Rend")) return true;
            // champions_spear
            if (CastCD("Champion's Spear")) return true;
            // ravager,if=cooldown.colossus_smash.remains<=gcd
            if (Inferno.SpellCooldown("Colossus Smash") <= GCDMAX() && Cast("Ravager")) return true;
            // avatar
            if (CastCD("Avatar")) return true;
            // colossus_smash
            if (CastCD("Colossus Smash")) return true;
            // heroic_strike
            if (Cast("Heroic Strike")) return true;
            // demolish,if=buff.colossal_might.stack=10&debuff.colossus_smash.up
            if (BuffStacks("Colossal Might") >= 10 && HasDebuff("Colossus Smash") && Cast("Demolish")) return true;
            // mortal_strike,if=buff.executioners_precision.stack=2|!talent.executioners_precision|talent.battlelord
            if (BuffStacks("Executioner's Precision") >= 2 || !Inferno.IsSpellKnown("Executioner's Precision") || Inferno.IsSpellKnown("Battlelord"))
                if (Cast("Mortal Strike")) return true;
            // cleave,if=buff.ravager.remains
            if (HasBuff("Ravager") && Cast("Cleave")) return true;
            // overpower
            if (Cast("Overpower")) return true;
            // execute,if=talent.deep_wounds&talent.critical_thinking
            if (Inferno.IsSpellKnown("Deep Wounds") && Inferno.IsSpellKnown("Critical Thinking") && Cast("Execute")) return true;
            // cleave,if=talent.mass_execution
            if (Inferno.IsSpellKnown("Mass Execution") && Cast("Cleave")) return true;
            // execute,if=talent.deep_wounds
            if (Inferno.IsSpellKnown("Deep Wounds") && Cast("Execute")) return true;
            // slam,if=!talent.critical_thinking
            if (!Inferno.IsSpellKnown("Critical Thinking") && Cast("Slam")) return true;
            // execute
            if (Cast("Execute")) return true;
            // bladestorm
            if (GetCheckBox("Use Bladestorm") && Cast("Bladestorm")) return true;
            // wrecking_throw
            if (Cast("Wrecking Throw")) return true;
            return false;
        }

        // =====================================================================
        // COLOSSUS ST (actions.colossus_st)
        // =====================================================================
        bool ColossusST(int enemies)
        {
            // rend,if=dot.rend_dot.remains<=gcd|cooldown.colossus_smash.remains<2&dot.rend_dot.remains<=10
            if ((RendRemaining() <= GCD() || (Inferno.SpellCooldown("Colossus Smash") < 2000 && RendRemaining() <= 10000)) && Cast("Rend")) return true;
            // sweeping_strikes,if=active_enemies=2&(cooldown.colossus_smash.remains&buff.sweeping_strikes.down|!talent.broad_strokes)
            if (enemies == 2 && ((Inferno.SpellCooldown("Colossus Smash") > 0 && !HasBuff("Sweeping Strikes")) || !Inferno.IsSpellKnown("Broad Strokes")))
                if (Cast("Sweeping Strikes")) return true;
            // ravager,if=cooldown.colossus_smash.remains<=gcd
            if (Inferno.SpellCooldown("Colossus Smash") <= GCDMAX() && Cast("Ravager")) return true;
            // avatar
            if (CastCD("Avatar")) return true;
            // colossus_smash
            if (CastCD("Colossus Smash")) return true;
            // cleave,if=buff.ravager.remains&buff.collateral_damage.stack=3
            if (HasBuff("Ravager") && BuffStacks("Collateral Damage") >= 3 && Cast("Cleave")) return true;
            // heroic_strike
            if (Cast("Heroic Strike")) return true;
            // champions_spear
            if (CastCD("Champion's Spear")) return true;
            // demolish,if=debuff.colossus_smash.up&buff.colossal_might.stack>0
            if (HasDebuff("Colossus Smash") && BuffStacks("Colossal Might") > 0 && Cast("Demolish")) return true;
            // mortal_strike
            if (Cast("Mortal Strike")) return true;
            // cleave,if=buff.ravager.remains|buff.collateral_damage.stack=3
            if ((HasBuff("Ravager") || BuffStacks("Collateral Damage") >= 3) && Cast("Cleave")) return true;
            // overpower
            if (Cast("Overpower")) return true;
            // whirlwind,if=active_enemies=2&buff.collateral_damage.stack=3
            if (enemies == 2 && BuffStacks("Collateral Damage") >= 3 && CastWW()) return true;
            // cleave,if=talent.mass_execution&target.health.pct<35
            if (Inferno.IsSpellKnown("Mass Execution") && GetTargetHealthPct() < 35 && Cast("Cleave")) return true;
            // execute
            if (Cast("Execute")) return true;
            // wrecking_throw,if=active_enemies=1
            if (enemies == 1 && Cast("Wrecking Throw")) return true;
            // rend,if=dot.rend_dot.remains<=gcd*5
            if (RendRemaining() <= GCD() * 5 && Cast("Rend")) return true;
            // cleave,if=!talent.martial_prowess
            if (!Inferno.IsSpellKnown("Martial Prowess") && Cast("Cleave")) return true;
            // slam
            if (Cast("Slam")) return true;
            return false;
        }

        // =====================================================================
        // SLAYER AoE (actions.slayer_aoe)
        // =====================================================================
        bool SlayerAoE(int enemies)
        {
            // rend,if=!dot.rend_dot.remains&talent.rend
            if (RendRemaining() == 0 && Inferno.IsSpellKnown("Rend") && Cast("Rend")) return true;
            // sweeping_strikes,if=!buff.sweeping_strikes.up&cooldown.colossus_smash.remains>10|!talent.broad_strokes
            if ((!HasBuff("Sweeping Strikes") && Inferno.SpellCooldown("Colossus Smash") > 10000) || !Inferno.IsSpellKnown("Broad Strokes"))
                if (Cast("Sweeping Strikes")) return true;
            // avatar
            if (CastCD("Avatar")) return true;
            // champions_spear
            if (CastCD("Champion's Spear")) return true;
            // ravager,if=debuff.colossus_smash.up
            if (HasDebuff("Colossus Smash") && Cast("Ravager")) return true;
            // colossus_smash
            if (CastCD("Colossus Smash")) return true;
            // cleave
            if (Cast("Cleave")) return true;
            // whirlwind,if=talent.fervor_of_battle&buff.collateral_damage.stack=3
            if (Inferno.IsSpellKnown("Fervor of Battle") && BuffStacks("Collateral Damage") >= 3 && CastWW()) return true;
            // execute,if=buff.sudden_death.up
            if (HasBuff("Sudden Death") && Cast("Execute")) return true;
            // bladestorm,if=debuff.colossus_smash.up
            if (GetCheckBox("Use Bladestorm") && HasDebuff("Colossus Smash") && Cast("Bladestorm")) return true;
            // mortal_strike
            if (Cast("Mortal Strike")) return true;
            // thunder_clap,if=dot.rend_dot.remains<8&talent.rend
            if (RendRemaining() < 8000 && Inferno.IsSpellKnown("Rend") && CastTC()) return true;
            // overpower,if=talent.dreadnaught
            if (Inferno.IsSpellKnown("Dreadnaught") && Cast("Overpower")) return true;
            // whirlwind,if=talent.fervor_of_battle
            if (Inferno.IsSpellKnown("Fervor of Battle") && CastWW()) return true;
            // overpower
            if (Cast("Overpower")) return true;
            // mortal_strike
            if (Cast("Mortal Strike")) return true;
            // execute
            if (Cast("Execute")) return true;
            // wrecking_throw
            if (Cast("Wrecking Throw")) return true;
            // whirlwind
            if (CastWW()) return true;
            // slam
            if (Cast("Slam")) return true;
            return false;
        }

        // =====================================================================
        // SLAYER Execute (actions.slayer_execute)
        // =====================================================================
        bool SlayerExecute(int enemies)
        {
            // sweeping_strikes,if=active_enemies=2&(cooldown.colossus_smash.remains&buff.sweeping_strikes.down|!talent.broad_strokes)
            if (enemies == 2 && ((Inferno.SpellCooldown("Colossus Smash") > 0 && !HasBuff("Sweeping Strikes")) || !Inferno.IsSpellKnown("Broad Strokes")))
                if (Cast("Sweeping Strikes")) return true;
            // rend,if=dot.rend_dot.remains<2&!talent.bloodletting
            if (RendRemaining() < 2000 && !Inferno.IsSpellKnown("Bloodletting") && Cast("Rend")) return true;
            // avatar
            if (CastCD("Avatar")) return true;
            // colossus_smash
            if (CastCD("Colossus Smash")) return true;
            // heroic_strike
            if (Cast("Heroic Strike")) return true;
            // bladestorm,if=debuff.colossus_smash.up
            if (GetCheckBox("Use Bladestorm") && HasDebuff("Colossus Smash") && Cast("Bladestorm")) return true;
            // mortal_strike,if=buff.executioners_precision.stack=2|debuff.colossus_smash.up
            if ((BuffStacks("Executioner's Precision") >= 2 || HasDebuff("Colossus Smash")) && Cast("Mortal Strike")) return true;
            // overpower,if=buff.opportunist.up&talent.opportunist
            if (HasBuff("Opportunist") && Inferno.IsSpellKnown("Opportunist") && Cast("Overpower")) return true;
            // overpower,if=talent.fierce_followthrough&!buff.battlelord.up&rage<90
            if (Inferno.IsSpellKnown("Fierce Followthrough") && !HasBuff("Battlelord") && GetRage() < 90 && Cast("Overpower")) return true;
            // execute,if=rage>40|buff.sudden_death.up
            if ((GetRage() > 40 || HasBuff("Sudden Death")) && Cast("Execute")) return true;
            // overpower
            if (Cast("Overpower")) return true;
            // execute,if=talent.improved_execute
            if (Inferno.IsSpellKnown("Improved Execute") && Cast("Execute")) return true;
            // cleave,if=talent.mass_execution
            if (Inferno.IsSpellKnown("Mass Execution") && Cast("Cleave")) return true;
            // slam,if=!talent.critical_thinking
            if (!Inferno.IsSpellKnown("Critical Thinking") && Cast("Slam")) return true;
            // execute
            if (Cast("Execute")) return true;
            // wrecking_throw
            if (Cast("Wrecking Throw")) return true;
            return false;
        }

        // =====================================================================
        // SLAYER ST (actions.slayer_st)
        // =====================================================================
        bool SlayerST(int enemies)
        {
            // sweeping_strikes,if=active_enemies=2&(cooldown.colossus_smash.remains&buff.sweeping_strikes.down|!talent.broad_strokes)
            if (enemies == 2 && ((Inferno.SpellCooldown("Colossus Smash") > 0 && !HasBuff("Sweeping Strikes")) || !Inferno.IsSpellKnown("Broad Strokes")))
                if (Cast("Sweeping Strikes")) return true;
            // avatar
            if (CastCD("Avatar")) return true;
            // champions_spear,if=debuff.colossus_smash.up|buff.avatar.up
            if ((HasDebuff("Colossus Smash") || HasBuff("Avatar")) && CastCD("Champion's Spear")) return true;
            // ravager,if=cooldown.colossus_smash.remains<=gcd
            if (Inferno.SpellCooldown("Colossus Smash") <= GCDMAX() && Cast("Ravager")) return true;
            // colossus_smash
            if (CastCD("Colossus Smash")) return true;
            // bladestorm,if=debuff.colossus_smash.up
            if (GetCheckBox("Use Bladestorm") && HasDebuff("Colossus Smash") && Cast("Bladestorm")) return true;
            // mortal_strike
            if (Cast("Mortal Strike")) return true;
            // execute,if=buff.sudden_death.up
            if (HasBuff("Sudden Death") && Cast("Execute")) return true;
            // heroic_strike
            if (Cast("Heroic Strike")) return true;
            // cleave,if=active_enemies=2&buff.collateral_damage.stack=3
            if (enemies == 2 && BuffStacks("Collateral Damage") >= 3 && Cast("Cleave")) return true;
            // overpower
            if (Cast("Overpower")) return true;
            // cleave,if=talent.mass_execution&target.health.pct<35
            if (Inferno.IsSpellKnown("Mass Execution") && GetTargetHealthPct() < 35 && Cast("Cleave")) return true;
            // whirlwind,if=active_enemies=2&buff.collateral_damage.stack=3
            if (enemies == 2 && BuffStacks("Collateral Damage") >= 3 && CastWW()) return true;
            // rend,if=dot.rend_dot.remains<=5
            if (RendRemaining() <= 5000 && Cast("Rend")) return true;
            // wrecking_throw,if=active_enemies=1
            if (enemies == 1 && Cast("Wrecking Throw")) return true;
            // slam
            if (Cast("Slam")) return true;
            return false;
        }

        // =====================================================================
        // DEFENSIVES
        // =====================================================================
        bool HandleDefensives()
        {
            if (!GetCheckBox("Use Defensives")) return false;
            int hpPct = GetPlayerHealthPct();

            if (hpPct <= GetSlider("Die by the Sword HP %") && Inferno.CanCast("Die by the Sword", IgnoreGCD: true))
            { Inferno.Cast("Die by the Sword", QuickDelay: true); return true; }

            if (hpPct <= GetSlider("Ignore Pain HP %") && GetRage() >= 40 && Inferno.CanCast("Ignore Pain", IgnoreGCD: true))
            { Inferno.Cast("Ignore Pain", QuickDelay: true); return true; }

            if (hpPct <= GetSlider("Healthstone HP %") && Inferno.CustomFunction("HasHealthstone") == 1 && Inferno.ItemCooldown(HealthstoneItemID) == 0)
            { Inferno.Cast("use_healthstone", QuickDelay: true); return true; }

            return false;
        }

        // =====================================================================
        // INTERRUPT (randomized)
        // =====================================================================
        bool HandleInterrupt()
        {
            if (!GetCheckBox("Auto Interrupt")) return false;
            int castingID = Inferno.CastingID("target");
            if (castingID == 0 || !Inferno.IsInterruptable("target")) { _lastCastingID = 0; return false; }
            if (castingID != _lastCastingID)
            {
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

        // =====================================================================
        // BATTLE SHOUT
        // =====================================================================
        bool HandleBattleShout()
        {
            if (!GetCheckBox("Auto Battle Shout")) return false;
            if (Inferno.BuffRemaining("Battle Shout") < GCD() && Inferno.CanCast("Battle Shout"))
            { Inferno.Cast("Battle Shout"); return true; }
            return false;
        }

        // =====================================================================
        // TRINKETS (simplified from APL - use during Avatar)
        // =====================================================================
        bool HandleTrinkets()
        {
            if (!GetCheckBox("Use Trinkets") || (Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;
            if (!HasBuff("Avatar")) return false;
            if (Inferno.CanUseEquippedItem(13)) { Inferno.Cast("trinket1"); return true; }
            if (Inferno.CanUseEquippedItem(14)) { Inferno.Cast("trinket2"); return true; }
            return false;
        }

        // =====================================================================
        // HELPERS
        // =====================================================================
        int GCD() { return Inferno.GCD(); }
        int GCDMAX() { int g = (int)(1500f / (1f + Inferno.Haste("player") / 100f)); return g < 750 ? 750 : g; }
        int GetRage() { return Inferno.Power("player", RagePowerType); }

        // Rend is tracked as "Rend" debuff on target. In SimC it's dot.rend_dot.remains
        int RendRemaining() { return Inferno.DebuffRemaining("Rend"); }

        bool HasBuff(string name) { return Inferno.BuffRemaining(name) > GCD(); }
        bool HasDebuff(string name) { return Inferno.DebuffRemaining(name) > GCD(); }
        int BuffStacks(string name) { return Inferno.BuffStacks(name); }

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

        // Cast helper - checks NoCDs toggle for cooldown abilities
        bool CastTC()
        {
            if (Inferno.DistanceBetween("player", "target") > 8) return false;
            if (Inferno.CanCast("Thunder Clap")) { Inferno.Cast("Thunder Clap"); Inferno.PrintMessage(">> Thunder Clap", Color.White); return true; }
            return false;
        }

        bool CastWW()
        {
            if (Inferno.DistanceBetween("player", "target") > 8) return false;
            if (Inferno.CanCast("Whirlwind")) { Inferno.Cast("Whirlwind"); Inferno.PrintMessage(">> Whirlwind", Color.White); return true; }
            return false;
        }

        bool Cast(string name)
        {
            if (Inferno.CanCast(name)) { Inferno.Cast(name); Inferno.PrintMessage(">> " + name, Color.White); return true; }
            return false;
        }

        // CastCD - for cooldown abilities that respect the NoCDs toggle
        bool CastCD(string name)
        {
            if ((Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;
            if (name == "Avatar" && !GetCheckBox("Use Avatar")) return false;
            if (name == "Colossus Smash" && !GetCheckBox("Use Colossus Smash")) return false;
            if (name == "Champion's Spear" && !GetCheckBox("Use Champion's Spear")) return false;
            if (Inferno.CanCast(name)) { Inferno.Cast(name); Inferno.PrintMessage(">> " + name, Color.White); return true; }
            return false;
        }
    }
}
