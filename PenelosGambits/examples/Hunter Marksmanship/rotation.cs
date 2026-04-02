using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API;

namespace InfernoWow.Modules
{
    /// <summary>
    /// Marksmanship Hunter - Translated from SimulationCraft Midnight APL
    /// Auto-detects hero tree: Dark Ranger (Black Arrow) or Sentinel (Moonlight Chakram).
    /// Each has ST and AoE sub-rotations.
    /// Tracks: Bulletstorm, Precise Shots, Trick Shots, Double Tap.
    /// </summary>
    public class MarksmanshipHunterRotation : Rotation
    {
        List<string> Abilities = new List<string> {
            "Aimed Shot", "Arcane Shot", "Multi-Shot", "Rapid Fire",
            "Steady Shot", "Trueshot", "Volley", "Black Arrow",
            "Wailing Arrow", "Moonlight Chakram", "Kill Shot",
        };
        List<string> TalentChecks = new List<string> {
            "Trick Shots", "Unload", "No Scope", "Bullseye",
            "Calling the Shots", "Aspect of the Hydra", "Headshot",
            "Unbreakable Bond", "Double Tap", "Volley",
        };
        List<string> DefensiveSpells = new List<string> { "Exhilaration", "Aspect of the Turtle", "Survival of the Fittest" };
        List<string> UtilitySpells = new List<string> { "Counter Shot", "Misdirection", "Hunter's Mark" };

        const int HealthstoneItemID = 5512;
        private Random _rng = new Random();
        private int _lastCastingID = 0;
        private int _interruptTargetPct = 0;

        public override void LoadSettings()
        {
            Settings.Add(new Setting("=== Marksmanship Hunter ==="));
            Settings.Add(new Setting("Hero tree auto-detected: Dark Ranger / Sentinel"));
            Settings.Add(new Setting("Use Trueshot", true));
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
        }

        public override void Initialize()
        {
            Inferno.PrintMessage("             //////////////////////////////////////", Color.DarkOliveGreen);
            Inferno.PrintMessage("             //   MARKSMANSHIP - HUNTER (MID)    //", Color.DarkOliveGreen);
            Inferno.PrintMessage("             //    DARK RANGER / SENTINEL        //", Color.DarkOliveGreen);
            Inferno.PrintMessage("             //              V 2.00              //", Color.DarkOliveGreen);
            Inferno.PrintMessage("             //////////////////////////////////////", Color.DarkOliveGreen);
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
            CustomFunctions.Add("PetIsActive", "return (UnitExists('pet') and not UnitIsDead('pet')) and 1 or 0");
            Macros.Add("call_pet", "/cast Call Pet 1");
            // Racial abilities
            foreach (string r in new string[] { "Berserking", "Blood Fury", "Ancestral Call", "Fireblood", "Lights Judgment" }) Spellbook.Add(r);
            CustomCommands.Add("NoCDs"); CustomCommands.Add("nocds");
            CustomCommands.Add("ForceST"); CustomCommands.Add("forcest");
        }

        public override bool OutOfCombatTick()
        {
            // summon_pet only if talented into Unbreakable Bond (most MM use Lone Wolf = no pet)
            if (Inferno.IsSpellKnown("Unbreakable Bond") && Inferno.CustomFunction("PetIsActive") == 0)
            { Inferno.Cast("call_pet"); return true; }
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

            // Don't interrupt Rapid Fire channel
            if (Inferno.IsChanneling("player")) return false;

            // Don't interrupt cast-time trinkets
            string _castName = Inferno.CastingName("player");
            if (_castName.Contains("Puzzle Box") || _castName.Contains("Emberwing")) return false;

            if (!Inferno.UnitCanAttack("player", "target")) return false;
            // Hunter's Mark in combat (high priority)
            if (GetCheckBox("Use Hunter's Mark in combat") && !Inferno.HasDebuff("Hunter's Mark", "target", false) && Inferno.CanCast("Hunter's Mark"))
            { Inferno.Cast("Hunter's Mark"); Inferno.PrintMessage(">> Hunter's Mark", Color.White); return true; }

            int enemies = Inferno.EnemiesNearUnit(10f, "target");
            if (enemies < 1) enemies = 1; if ((Inferno.IsCustomCodeOn("ForceST") || Inferno.IsCustomCodeOn("forcest"))) enemies = 1;
            bool isDR = Inferno.IsSpellKnown("Black Arrow");
            bool isSent = Inferno.IsSpellKnown("Moonlight Chakram");

            // Trinkets during Trueshot
                        if (HandleTrinkets()) return true;

            // Racial damage CDs
            if ((HasBuff("Trueshot")) && HandleRacials()) return true;

            bool isAoe = enemies > 2 && Inferno.IsSpellKnown("Trick Shots");

            if (isDR)
            {
                if (isAoe) return DRAoE(enemies);
                return DRST(enemies);
            }
            if (isSent)
            {
                if (isAoe) return SentAoE(enemies);
                return SentST(enemies);
            }
            return DRST(enemies);
        }

        // =====================================================================
        // DARK RANGER AoE (actions.draoe)
        // =====================================================================
        bool DRAoE(int enemies)
        {
            // black_arrow
            if (Cast("Black Arrow")) return true;

            // multishot,if=buff.precise_shots.up&!talent.aspect_of_the_hydra|buff.trick_shots.down
            if ((HasBuff("Precise Shots") && !Inferno.IsSpellKnown("Aspect of the Hydra")) || !HasBuff("Trick Shots"))
                if (Cast("Multi-Shot")) return true;

            // rapid_fire,if=buff.trick_shots.remains>execute_time&(talent.unload&(talent.no_scope&buff.bulletstorm.stack<10|target.health.pct<20)|buff.bulletstorm.remains<aimed_shot_cast_time)
            if (HasBuff("Trick Shots"))
            {
                bool unloadCond = Inferno.IsSpellKnown("Unload") && (Inferno.IsSpellKnown("No Scope") && Inferno.BuffStacks("Bulletstorm") < 10 || TargetHpPct() < 20);
                bool bsLow = Inferno.BuffRemaining("Bulletstorm") < 2500;
                if (unloadCond || bsLow)
                    if (Cast("Rapid Fire")) return true;
            }

            // aimed_shot priority boost when Spotter's Mark is on target
            if (HasDebuff("Spotter's Mark") && HasBuff("Trick Shots"))
                if (Cast("Aimed Shot")) return true;

            // trueshot,if=!buff.double_tap.up&variable.trueshot_ready
            if (!HasBuff("Double Tap"))
                if (CastCD("Trueshot")) return true;

            // volley,if=!buff.double_tap.up
            if (!HasBuff("Double Tap"))
                if (Cast("Volley")) return true;

            // aimed_shot,if=buff.trick_shots.remains>cast_time
            if (HasBuff("Trick Shots"))
                if (Cast("Aimed Shot")) return true;

            // wailing_arrow
            if (Cast("Wailing Arrow")) return true;

            // rapid_fire,if=buff.trick_shots.remains>execute_time
            if (HasBuff("Trick Shots"))
                if (Cast("Rapid Fire")) return true;

            // steady_shot
            if (Cast("Steady Shot")) return true;
            return false;
        }

        // =====================================================================
        // DARK RANGER ST (actions.drst)
        // =====================================================================
        bool DRST(int enemies)
        {
            // black_arrow
            if (Cast("Black Arrow")) return true;

            // trueshot,if=!buff.double_tap.up&variable.trueshot_ready
            if (!HasBuff("Double Tap"))
                if (CastCD("Trueshot")) return true;

            // rapid_fire,if=talent.unload&(talent.no_scope&buff.bulletstorm.stack<10|target.health.pct<20)
            if (Inferno.IsSpellKnown("Unload") && (Inferno.IsSpellKnown("No Scope") && Inferno.BuffStacks("Bulletstorm") < 10 || TargetHpPct() < 20))
                if (Cast("Rapid Fire")) return true;

            // aimed_shot priority boost when Spotter's Mark is on target
            if (HasDebuff("Spotter's Mark"))
                if (Cast("Aimed Shot")) return true;

            // aimed_shot,if=buff.volley.remains/aimed_shot_cast>arcane_shot_cast&buff.trueshot.down
            // Simplified: cast Aimed Shot during Volley when Trueshot is down
            if (Inferno.BuffRemaining("Volley") > 2500 && !HasBuff("Trueshot"))
                if (Cast("Aimed Shot")) return true;

            // arcane_shot,if=buff.precise_shots.up
            if (HasBuff("Precise Shots"))
                if (Cast("Arcane Shot")) return true;

            // rapid_fire,if=buff.bulletstorm.remains<action.aimed_shot.execute_time
            if (Inferno.BuffRemaining("Bulletstorm") < 2500)
                if (Cast("Rapid Fire")) return true;

            // volley,if=!buff.double_tap.up
            if (!HasBuff("Double Tap"))
                if (Cast("Volley")) return true;

            // aimed_shot
            if (Cast("Aimed Shot")) return true;

            // wailing_arrow
            if (Cast("Wailing Arrow")) return true;

            // rapid_fire
            if (Cast("Rapid Fire")) return true;

            // steady_shot
            if (Cast("Steady Shot")) return true;
            return false;
        }

        // =====================================================================
        // SENTINEL AoE (actions.sentaoe)
        // =====================================================================
        bool SentAoE(int enemies)
        {
            // multishot,if=buff.precise_shots.up&!talent.aspect_of_the_hydra|buff.trick_shots.down
            if ((HasBuff("Precise Shots") && !Inferno.IsSpellKnown("Aspect of the Hydra")) || !HasBuff("Trick Shots"))
                if (Cast("Multi-Shot")) return true;

            // rapid_fire,if=buff.bulletstorm.remains<action.aimed_shot.execute_time
            if (Inferno.BuffRemaining("Bulletstorm") < 2500)
                if (Cast("Rapid Fire")) return true;

            // aimed_shot priority boost when Sentinel's Mark is on target
            if (HasDebuff("Sentinel's Mark"))
                if (Cast("Aimed Shot")) return true;

            // trueshot,if=!buff.double_tap.up&variable.trueshot_ready
            if (!HasBuff("Double Tap"))
                if (CastCD("Trueshot")) return true;

            // volley,if=!buff.double_tap.up
            if (!HasBuff("Double Tap"))
                if (Cast("Volley")) return true;

            // aimed_shot
            if (Cast("Aimed Shot")) return true;

            // moonlight_chakram
            if (Cast("Moonlight Chakram")) return true;

            // rapid_fire
            if (Cast("Rapid Fire")) return true;

            // steady_shot
            if (Cast("Steady Shot")) return true;
            return false;
        }

        // =====================================================================
        // SENTINEL ST (actions.sentst)
        // =====================================================================
        bool SentST(int enemies)
        {
            // volley,if=!buff.double_tap.up&active_enemies=1
            if (!HasBuff("Double Tap") && enemies == 1)
                if (Cast("Volley")) return true;

            // trueshot,if=!buff.double_tap.up&active_enemies=1&variable.trueshot_ready
            if (!HasBuff("Double Tap") && enemies == 1)
                if (CastCD("Trueshot")) return true;

            // rapid_fire,if=talent.unload&((buff.precise_shots.up&!talent.no_scope)&buff.bulletstorm.stack<10|target.health.pct<20)
            if (Inferno.IsSpellKnown("Unload"))
            {
                bool psCond = HasBuff("Precise Shots") && !Inferno.IsSpellKnown("No Scope") && Inferno.BuffStacks("Bulletstorm") < 10;
                if (psCond || TargetHpPct() < 20)
                    if (Cast("Rapid Fire")) return true;
            }

            // aimed_shot priority boost when Sentinel's Mark is on target
            if (HasDebuff("Sentinel's Mark"))
                if (Cast("Aimed Shot")) return true;

            // aimed_shot,if=active_enemies>2&buff.volley.remains/aimed_cast>arcane_cast&buff.trueshot.down
            if (enemies > 2 && Inferno.BuffRemaining("Volley") > 2500 && !HasBuff("Trueshot"))
                if (Cast("Aimed Shot")) return true;

            // arcane_shot,if=buff.precise_shots.up (prefer when Sentinel's Mark is down to apply it)
            if (HasBuff("Precise Shots"))
                if (Cast("Arcane Shot")) return true;

            // rapid_fire,if=buff.bulletstorm.remains<action.aimed_shot.execute_time
            if (Inferno.BuffRemaining("Bulletstorm") < 2500)
                if (Cast("Rapid Fire")) return true;

            // trueshot,if=!buff.double_tap.up&active_enemies>1&variable.trueshot_ready
            if (!HasBuff("Double Tap") && enemies > 1)
                if (CastCD("Trueshot")) return true;

            // volley,if=!buff.double_tap.up&active_enemies>1
            if (!HasBuff("Double Tap") && enemies > 1)
                if (Cast("Volley")) return true;

            // aimed_shot,if=cooldown.volley.remains>2|buff.trueshot.up|!talent.volley
            if (Inferno.SpellCooldown("Volley") > 2000 || HasBuff("Trueshot") || !Inferno.IsSpellKnown("Volley"))
                if (Cast("Aimed Shot")) return true;

            // moonlight_chakram
            if (Cast("Moonlight Chakram")) return true;

            // rapid_fire
            if (Cast("Rapid Fire")) return true;

            // steady_shot
            if (Cast("Steady Shot")) return true;
            return false;
        }

        // =====================================================================
        // DEFENSIVES / INTERRUPT / TRINKETS
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
            return false;
        }

        bool HandleTrinkets()
        {
            if (!GetCheckBox("Use Trinkets") || (Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;
            if (!HasBuff("Trueshot")) return false;
            if (Inferno.CanUseEquippedItem(13)) { Inferno.Cast("trinket1"); return true; }
            if (Inferno.CanUseEquippedItem(14)) { Inferno.Cast("trinket2"); return true; }
            return false;
        }

        // =====================================================================
        // HELPERS
        // =====================================================================
        int TargetHpPct()
        {
            int hp = Inferno.Health("target"); int maxHp = Inferno.MaxHealth("target");
            if (maxHp < 1) maxHp = 1; return (hp * 100) / maxHp;
        }

        int GCD() { return Inferno.GCD(); }
        int GCDMAX() { int g = (int)(1500f / (1f + Inferno.Haste("player") / 100f)); return g < 750 ? 750 : g; }
        bool HasBuff(string name) { return Inferno.BuffRemaining(name) > GCD(); }
        bool HasDebuff(string name) { return Inferno.DebuffRemaining(name) > GCD(); }

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
            if (name == "Trueshot" && !GetCheckBox("Use Trueshot")) return false;
            if (Inferno.CanCast(name)) { Inferno.Cast(name); Inferno.PrintMessage(">> " + name, Color.White); return true; }
            return false;
        }
    }
}
