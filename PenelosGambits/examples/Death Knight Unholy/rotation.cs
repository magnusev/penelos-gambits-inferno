using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API;

namespace InfernoWow.Modules
{
    /// <summary>
    /// Unholy Death Knight - Translated from SimulationCraft Midnight APL
    /// Key mechanics: Festering Scythe, Lesser Ghoul Army, Putrefy,
    /// Epidemic vs Death Coil routing based on target count.
    /// </summary>
    public class UnholyDeathKnightRotation : Rotation
    {
        List<string> Abilities = new List<string> {
            "Festering Strike", "Scourge Strike", "Death Coil", "Epidemic",
            "Outbreak", "Death and Decay", "Putrefy", "Soul Reaper",
            "Dark Transformation", "Army of the Dead", "Raise Dead",
            "Summon Gargoyle",
        };
        List<string> TalentChecks = new List<string> {
            "Festering Scythe", "Desecrate", "Pestilence", "Blightburst",
            "Infliction of Sorrow", "Commander of the Dead", "Reaping",
            "Soul Reaper", "Summon Gargoyle", "Army of the Dead",
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
            Settings.Add(new Setting("=== Unholy Death Knight ==="));
            Settings.Add(new Setting("=== Offensive Cooldowns ==="));
            Settings.Add(new Setting("Use Dark Transformation", true));
            Settings.Add(new Setting("Use Army of the Dead", true));
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
            Inferno.PrintMessage("             //////////////////////////////////////", Color.DarkGreen);
            Inferno.PrintMessage("             //   UNHOLY - DEATH KNIGHT (MID)    //", Color.DarkGreen);
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
            // Raise Dead if no pet
            if (Inferno.CustomFunction("PetIsActive") == 0 && Inferno.CanCast("Raise Dead"))
            { Inferno.Cast("Raise Dead"); Inferno.PrintMessage(">> Raise Dead", Color.White); return true; }
            return false;
        }

        public override bool CombatTick()
        {
            if (Inferno.IsDead("player") || Inferno.IsGhost("player")) return false;
            if (HandleDefensives()) return true;
            if (HandleInterrupt()) return true;

            // Raise Dead if no pet
            if (Inferno.CustomFunction("PetIsActive") == 0 && Inferno.CanCast("Raise Dead"))
            { Inferno.Cast("Raise Dead"); Inferno.PrintMessage(">> Raise Dead", Color.White); return true; }

            if (!Inferno.UnitCanAttack("player", "target")) return false;
            if (Inferno.IsChanneling("player")) return false;

            // Don't interrupt cast-time trinkets
            string _castName = Inferno.CastingName("player");
            if (_castName.Contains("Puzzle Box") || _castName.Contains("Emberwing")) return false;
                        if (HandleTrinkets()) return true;

            // Racial damage CDs
            if ((Inferno.BuffRemaining("Dark Transformation") > 5000) && HandleRacials()) return true;

            // Outbreak — maintain Virulent Plague (needed for Epidemic/Scourge Strike, not a CD)
            if (Inferno.DebuffRemaining("Virulent Plague") < 6000 && Inferno.CanCast("Outbreak"))
            { Inferno.Cast("Outbreak"); Inferno.PrintMessage(">> Outbreak", Color.White); return true; }

            if (HandleCooldowns()) return true;

            int enemies = Inferno.EnemiesNearUnit(8f, "player");
            if (enemies < 1) enemies = 1; if ((Inferno.IsCustomCodeOn("ForceST") || Inferno.IsCustomCodeOn("forcest"))) enemies = 1;

            // APL: call_action_list,name=aoe,if=active_enemies>=4
            if (enemies >= 4) return AoERotation(enemies);
            return SingleTarget(enemies);
        }

        // =====================================================================
        // COOLDOWNS (actions.cooldowns)
        // =====================================================================
        bool HandleCooldowns()
        {
            if ((Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;

            // army_of_the_dead
            if (GetCheckBox("Use Army of the Dead") && Inferno.CanCast("Army of the Dead"))
            { Inferno.Cast("Army of the Dead"); Inferno.PrintMessage(">> Army of the Dead", Color.White); return true; }

            // dark_transformation
            if (GetCheckBox("Use Dark Transformation") && Inferno.CanCast("Dark Transformation"))
            { Inferno.Cast("Dark Transformation"); Inferno.PrintMessage(">> Dark Transformation", Color.White); return true; }

            // soul_reaper — APL: !talent.pestilence (unconditional) | pestilence+ios (DT ending/Reaping) | execute
            int targetHpPct = GetTargetHealthPct();
            if (targetHpPct <= 35 && Inferno.CanCast("Soul Reaper"))
            { Inferno.Cast("Soul Reaper"); Inferno.PrintMessage(">> Soul Reaper", Color.White); return true; }
            if (!Inferno.IsSpellKnown("Pestilence") && Inferno.CanCast("Soul Reaper"))
            { Inferno.Cast("Soul Reaper"); Inferno.PrintMessage(">> Soul Reaper", Color.White); return true; }
            if (Inferno.IsSpellKnown("Pestilence") && Inferno.IsSpellKnown("Infliction of Sorrow")
                && (Inferno.BuffRemaining("Dark Transformation") < 5000 || Inferno.BuffRemaining("Reaping") <= GCD()))
                if (Inferno.CanCast("Soul Reaper")) { Inferno.Cast("Soul Reaper"); Inferno.PrintMessage(">> Soul Reaper", Color.White); return true; }

            // putrefy — broaden: DT on CD, or at max charges, or during Reaping with IoS+Pestilence
            int dtCD = Inferno.SpellCooldown("Dark Transformation");
            if (dtCD > 10000 && GetRP() < 90)
                if (Inferno.CanCast("Putrefy")) { Inferno.Cast("Putrefy"); Inferno.PrintMessage(">> Putrefy", Color.White); return true; }
            if (Inferno.ChargesFractional("Putrefy", 15000) >= 1.9f && dtCD > GCDMAX())
                if (Inferno.CanCast("Putrefy")) { Inferno.Cast("Putrefy"); Inferno.PrintMessage(">> Putrefy", Color.White); return true; }
            if (HasBuff("Reaping") && Inferno.IsSpellKnown("Infliction of Sorrow") && Inferno.IsSpellKnown("Pestilence") && Inferno.BuffRemaining("Dark Transformation") > 10000)
                if (Inferno.CanCast("Putrefy")) { Inferno.Cast("Putrefy"); Inferno.PrintMessage(">> Putrefy", Color.White); return true; }

            return false;
        }

        // =====================================================================
        // AoE ROTATION (actions.aoe) - 4+ targets
        // =====================================================================
        bool AoERotation(int enemies)
        {
            // death_and_decay,if=!death_and_decay.ticking&talent.desecrate
            if (Inferno.IsSpellKnown("Desecrate") && Inferno.BuffRemaining("Death and Decay") < GCD() && Inferno.CanCast("Death and Decay"))
            { Inferno.Cast("Death and Decay"); Inferno.PrintMessage(">> Death and Decay", Color.White); return true; }

            // festering_strike,if=talent.festering_scythe&buff/debuff conditions
            if (HandleFesteringScythe()) return true;

            // epidemic,if=variable.spending_rp (4+ targets)
            if (IsSpendingRP() && Inferno.CanCast("Epidemic"))
            { Inferno.Cast("Epidemic"); Inferno.PrintMessage(">> Epidemic", Color.White); return true; }

            // festering_strike,if=buff.lesser_ghoul.stack=0
            if (GetWoundStacks() == 0 && Inferno.CanCast("Festering Strike"))
            { Inferno.Cast("Festering Strike"); Inferno.PrintMessage(">> Festering Strike", Color.White); return true; }

            // scourge_strike,if=buff.lesser_ghoul.stack>=1
            if (GetWoundStacks() >= 1 && Inferno.CanCast("Scourge Strike"))
            { Inferno.Cast("Scourge Strike"); Inferno.PrintMessage(">> Scourge Strike", Color.White); return true; }

            // putrefy
            if (Inferno.CanCast("Putrefy")) { Inferno.Cast("Putrefy"); Inferno.PrintMessage(">> Putrefy", Color.White); return true; }

            // epidemic (fallback)
            if (Inferno.CanCast("Epidemic")) { Inferno.Cast("Epidemic"); Inferno.PrintMessage(">> Epidemic", Color.White); return true; }

            // death_coil (fallback if epidemic not known)
            if (Inferno.CanCast("Death Coil")) { Inferno.Cast("Death Coil"); Inferno.PrintMessage(">> Death Coil", Color.White); return true; }

            return false;
        }

        // =====================================================================
        // SINGLE TARGET (actions.single_target) - <4 targets
        // =====================================================================
        bool SingleTarget(int enemies)
        {
            // festering_strike,if=talent.festering_scythe&conditions
            if (HandleFesteringScythe()) return true;

            // death_coil,if=variable.spending_rp
            if (IsSpendingRP() && Inferno.CanCast("Death Coil"))
            { Inferno.Cast("Death Coil"); Inferno.PrintMessage(">> Death Coil", Color.White); return true; }

            // festering_strike,if=buff.lesser_ghoul.stack=0
            if (GetWoundStacks() == 0 && Inferno.CanCast("Festering Strike"))
            { Inferno.Cast("Festering Strike"); Inferno.PrintMessage(">> Festering Strike", Color.White); return true; }

            // scourge_strike,if=buff.lesser_ghoul.stack>=1
            if (GetWoundStacks() >= 1 && Inferno.CanCast("Scourge Strike"))
            { Inferno.Cast("Scourge Strike"); Inferno.PrintMessage(">> Scourge Strike", Color.White); return true; }

            // putrefy
            if (!Inferno.IsSpellKnown("Soul Reaper") && Inferno.SpellCooldown("Dark Transformation") > 12000)
                if (Inferno.CanCast("Putrefy")) { Inferno.Cast("Putrefy"); Inferno.PrintMessage(">> Putrefy", Color.White); return true; }

            // death_coil
            if (Inferno.CanCast("Death Coil")) { Inferno.Cast("Death Coil"); Inferno.PrintMessage(">> Death Coil", Color.White); return true; }

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
            { Inferno.Cast("Death Strike"); Inferno.PrintMessage(">> Death Strike", Color.White); return true; }
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
            // Use during cds_active: DT up or Forbidden Knowledge or lesser ghoul army
            bool cdsActive = Inferno.BuffRemaining("Dark Transformation") > 5000 || Inferno.BuffRemaining("Forbidden Knowledge") > GCD();
            if (!cdsActive) return false;
            if (Inferno.CanUseEquippedItem(13)) { Inferno.Cast("trinket1"); Inferno.PrintMessage(">> trinket1", Color.White); return true; }
            if (Inferno.CanUseEquippedItem(14)) { Inferno.Cast("trinket2"); Inferno.PrintMessage(">> trinket2", Color.White); return true; }
            return false;
        }

        // =====================================================================
        // HELPERS
        // =====================================================================
        int GCD() { return Inferno.GCD(); }
        int GCDMAX() { int g = (int)(1500f / (1f + Inferno.Haste("player") / 100f)); return g < 750 ? 750 : g; }
        int GetRP() { return Inferno.Power("player", RunicPowerType); }
        int GetRunes() { return Inferno.GetAvailableRunes(); }
        bool HasBuff(string name) { return Inferno.BuffRemaining(name) > GCD(); }

        // Lesser Ghoul stacks — Festering Strike grants stacks (timed buff, remaining > 0)
        // The infinite "commanding lesser ghouls" buff also named Lesser Ghoul has remaining == 0
        int GetWoundStacks()
        {
            int rem = Inferno.BuffRemaining("Lesser Ghoul");
            if (rem <= 0) return 0;
            return Inferno.BuffStacks("Lesser Ghoul");
        }

        // variable,name=spending_rp,value=rune<2|buff.forbidden_knowledge.up&rune<4|buff.sudden_doom.react
        bool IsSpendingRP()
        {
            return GetRunes() < 2
                || (HasBuff("Forbidden Knowledge") && GetRunes() < 4)
                || HasBuff("Sudden Doom");
        }

        // Handle festering_scythe talent priority
        bool HandleFesteringScythe()
        {
            if (!Inferno.IsSpellKnown("Festering Scythe")) return false;
            int fsBuff = Inferno.BuffRemaining("Festering Scythe");
            int fsDebuff = Inferno.DebuffRemaining("Festering Scythe");
            // festering_strike if: buff up & (buff<3s or debuff<3s) or buff not up & debuff<3s
            if ((fsBuff > 0 && (fsBuff <= 3000 || fsDebuff < 3000)) || (fsBuff == 0 && fsDebuff < 3000))
            {
                if (Inferno.CanCast("Festering Strike")) { Inferno.Cast("Festering Strike"); Inferno.PrintMessage(">> Festering Strike", Color.White); return true; }
            }
            return false;
        }

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
    }
}
