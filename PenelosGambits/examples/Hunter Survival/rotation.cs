using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API;

namespace InfernoWow.Modules
{
    /// <summary>
    /// Survival Hunter - Translated from SimulationCraft Midnight APL
    /// Auto-detects hero tree: Pack Leader (Howl of the Pack Leader) or Sentinel.
    /// Tip of the Spear management is central to the rotation.
    /// </summary>
    public class SurvivalHunterRotation : Rotation
    {
        List<string> Abilities = new List<string> {
            "Kill Command", "Raptor Strike", "Wildfire Bomb", "Takedown",
            "Boomstick", "Flamefang Pitch", "Moonlight Chakram",
            "Aspect of the Eagle", "Kill Shot",
        };
        List<string> TalentChecks = new List<string> {
            "Howl of the Pack Leader", "Twin Fangs", "Wildfire Shells",
            "Flamefang Pitch", "Takedown",
        };
        List<string> DefensiveSpells = new List<string> { "Exhilaration", "Aspect of the Turtle", "Survival of the Fittest" };
        List<string> UtilitySpells = new List<string> { "Muzzle", "Mend Pet", "Call Pet 1", "Hunter's Mark" };

        const int HealthstoneItemID = 5512;
        const int FocusPowerType = 2;
        private Random _rng = new Random();
        private int _lastCastingID = 0;
        private int _interruptTargetPct = 0;

        public override void LoadSettings()
        {
            Settings.Add(new Setting("=== Survival Hunter ==="));
            Settings.Add(new Setting("Hero tree auto-detected: Pack Leader / Sentinel"));
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
            Inferno.PrintMessage("             //////////////////////////////////////", Color.OliveDrab);
            Inferno.PrintMessage("             //    SURVIVAL - HUNTER (MID)       //", Color.OliveDrab);
            Inferno.PrintMessage("             //    PACK LEADER / SENTINEL        //", Color.OliveDrab);
            Inferno.PrintMessage("             //              V 1.00              //", Color.OliveDrab);
            Inferno.PrintMessage("             //////////////////////////////////////", Color.OliveDrab);
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
                        if (HandleTrinkets()) return true;

            // Racial damage CDs
            if ((HasBuff("Coordinated Assault")) && HandleRacials()) return true;

            int enemies = Inferno.EnemiesNearUnit(8f, "player");
            if (enemies < 1) enemies = 1; if ((Inferno.IsCustomCodeOn("ForceST") || Inferno.IsCustomCodeOn("forcest"))) enemies = 1;
            bool isPL = Inferno.IsSpellKnown("Howl of the Pack Leader");

            if (isPL)
            {
                if (enemies > 2) return PLCleave(enemies);
                return PLST(enemies);
            }
            else
            {
                if (enemies > 2) return SentCleave(enemies);
                return SentST(enemies);
            }
        }

        // =====================================================================
        // PACK LEADER ST (actions.plst)
        // =====================================================================
        bool PLST(int enemies)
        {
            int tots = GetToTS();
            // kill_command with howl buffs
            if (tots < 2 && (HasBuff("Howl of the Pack Leader: Wyvern") || HasBuff("Howl of the Pack Leader: Boar") || HasBuff("Howl of the Pack Leader: Bear")))
                if (Cast("Kill Command")) return true;
            // kill_command before takedown
            if (Inferno.SpellCooldown("Takedown") < GCDMAX() && tots < 2 && !Inferno.IsSpellKnown("Twin Fangs"))
                if (Cast("Kill Command")) return true;
            // takedown
            if ((tots > 0 && !Inferno.IsSpellKnown("Twin Fangs")) || (tots == 0 && Inferno.IsSpellKnown("Twin Fangs")))
                if (CastCD("Takedown")) return true;
            // flamefang_pitch
            if (Cast("Flamefang Pitch")) return true;
            // boomstick with tip
            if (HasTip() && CastCD("Boomstick")) return true;
            // wildfire_bomb with tip
            if (HasTip() && Cast("Wildfire Bomb")) return true;
            // raptor_strike with tip or no raptor swipe
            if (HasTip() || !HasBuff("Raptor Swipe"))
                if (Cast("Raptor Strike")) return true;
            // kill_command
            if (Inferno.SpellCooldown("Takedown") > 0 && Cast("Kill Command")) return true;
            // wildfire_bomb
            if (Cast("Wildfire Bomb")) return true;
            // takedown
            if (CastCD("Takedown")) return true;
            return false;
        }

        // =====================================================================
        // PACK LEADER CLEAVE (actions.plcleave)
        // =====================================================================
        bool PLCleave(int enemies)
        {
            int tots = GetToTS();
            if (tots < 2 && (HasBuff("Howl of the Pack Leader: Wyvern") || HasBuff("Howl of the Pack Leader: Boar") || HasBuff("Howl of the Pack Leader: Bear")))
                if (Cast("Kill Command")) return true;
            if (Inferno.SpellCooldown("Takedown") < GCDMAX() && tots < 2 && !Inferno.IsSpellKnown("Twin Fangs"))
                if (Cast("Kill Command")) return true;
            if ((tots > 0 && !Inferno.IsSpellKnown("Twin Fangs")) || (tots == 0 && Inferno.IsSpellKnown("Twin Fangs")))
                if (CastCD("Takedown")) return true;
            if (Cast("Flamefang Pitch")) return true;
            // wildfire_bomb at max charges
            if (Inferno.ChargesFractional("Wildfire Bomb", 18000) >= 1.9f && Cast("Wildfire Bomb")) return true;
            if (HasTip() && CastCD("Boomstick")) return true;
            if (HasTip() && Cast("Wildfire Bomb")) return true;
            if (HasTip() || !HasBuff("Raptor Swipe"))
                if (Cast("Raptor Strike")) return true;
            if (Inferno.SpellCooldown("Takedown") > 0 && Cast("Kill Command")) return true;
            if (Cast("Wildfire Bomb")) return true;
            if (CastCD("Takedown")) return true;
            return false;
        }

        // =====================================================================
        // SENTINEL ST (actions.sentst)
        // =====================================================================
        bool SentST(int enemies)
        {
            int tots = GetToTS();
            // kill_command at 0 stacks
            if (tots == 0 && Cast("Kill Command")) return true;
            // boomstick with tip & no sentinel mark
            if (HasTip() && !Inferno.CanCast("Takedown") && Inferno.DebuffRemaining("Sentinel's Mark") < GCD())
                if (CastCD("Boomstick")) return true;
            // wildfire_bomb with tip & sentinel mark
            if (HasTip() && (Inferno.DebuffRemaining("Sentinel's Mark") > GCD() || Inferno.ChargesFractional("Wildfire Bomb", 18000) >= 1.9f))
                if (Cast("Wildfire Bomb")) return true;
            // kill_command before takedown
            if (Inferno.SpellCooldown("Takedown") < GCDMAX() && tots < 2 && !Inferno.IsSpellKnown("Twin Fangs"))
                if (Cast("Kill Command")) return true;
            // takedown
            if ((tots > 0 && !Inferno.IsSpellKnown("Twin Fangs")) || (tots == 0 && Inferno.IsSpellKnown("Twin Fangs")))
                if (CastCD("Takedown")) return true;
            // boomstick with tip
            if (HasTip() && CastCD("Boomstick")) return true;
            // moonlight_chakram with tip
            if (HasTip() && Cast("Moonlight Chakram")) return true;
            // flamefang_pitch
            if (Cast("Flamefang Pitch")) return true;
            // raptor_strike with tip or no raptor swipe
            if (HasTip() || !HasBuff("Raptor Swipe"))
                if (Cast("Raptor Strike")) return true;
            // kill_command
            if (Inferno.SpellCooldown("Takedown") > 0 && Cast("Kill Command")) return true;
            // takedown
            if (CastCD("Takedown")) return true;
            return false;
        }

        // =====================================================================
        // SENTINEL CLEAVE (actions.sentcleave)
        // =====================================================================
        bool SentCleave(int enemies)
        {
            int tots = GetToTS();
            if (tots == 0 && Cast("Kill Command")) return true;
            if (HasTip() && CastCD("Boomstick")) return true;
            if (HasTip() && (Inferno.DebuffRemaining("Sentinel's Mark") > GCD() || Inferno.ChargesFractional("Wildfire Bomb", 18000) >= 1.9f))
                if (Cast("Wildfire Bomb")) return true;
            if (Inferno.SpellCooldown("Takedown") < GCDMAX() && tots < 2 && !Inferno.IsSpellKnown("Twin Fangs"))
                if (Cast("Kill Command")) return true;
            if (HasTip() && CastCD("Takedown")) return true;
            if (HasTip() && Cast("Moonlight Chakram")) return true;
            if (HasTip() && Cast("Flamefang Pitch")) return true;
            if (HasTip() || !HasBuff("Raptor Swipe"))
                if (Cast("Raptor Strike")) return true;
            if (Cast("Kill Command")) return true;
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
            if (castPct >= _interruptTargetPct && Inferno.CanCast("Muzzle", IgnoreGCD: true))
            { Inferno.Cast("Muzzle", QuickDelay: true); _lastCastingID = 0; return true; }
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
            if (HasBuff("Takedown") || Inferno.SpellCooldown("Takedown") < GCDMAX())
            {
                if (Inferno.CanUseEquippedItem(13)) { Inferno.Cast("trinket1"); return true; }
                if (Inferno.CanUseEquippedItem(14)) { Inferno.Cast("trinket2"); return true; }
            }
            return false;
        }

        // =====================================================================
        // HELPERS
        // =====================================================================
        int GCD() { return Inferno.GCD(); }
        int GCDMAX() { int g = (int)(1500f / (1f + Inferno.Haste("player") / 100f)); return g < 750 ? 750 : g; }
        bool HasBuff(string name) { return Inferno.BuffRemaining(name) > GCD(); }
        bool HasTip() { return Inferno.BuffRemaining("Tip of the Spear") > GCD(); }
        int GetToTS() { return Inferno.BuffStacks("Tip of the Spear"); }


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
            if (Inferno.CanCast(name)) { Inferno.Cast(name); Inferno.PrintMessage(">> " + name, Color.White); return true; }
            return false;
        }
    }
}
