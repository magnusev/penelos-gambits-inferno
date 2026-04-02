using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API;

namespace InfernoWow.Modules
{
    /// <summary>
    /// Devastation Evoker - Translated from SimulationCraft Midnight APL
    /// Hero trees: Flameshaper (detected via !Mass Disintegrate) vs Scalecommander (Mass Disintegrate)
    /// Sub-rotations: st_fs, st_sc, aoe_fs, aoe_sc + es (empowered spells), green (Ancient Flame)
    /// Core: Dragonrage windows, Fire Breath/Eternity Surge empower, Essence management.
    /// </summary>
    public class DevastationEvokerRotation : Rotation
    {
        List<string> Abilities = new List<string> {
            "Living Flame", "Azure Strike", "Azure Sweep", "Disintegrate", "Pyre",
            "Eternity Surge", "Fire Breath", "Deep Breath", "Dragonrage",
            "Tip the Scales", "Hover", "Emerald Blossom", "Verdant Embrace",
        };
        List<string> TalentChecks = new List<string> {
            "Mass Disintegrate", "Azure Sweep", "Feed the Flames", "Volatility",
            "Consume Flame", "Engulfing Blaze", "Ancient Flame", "Scarlet Adaptation",
            "Burnout", "Imminent Destruction", "Slipstream", "Eternity's Span",
            "Animosity", "Strafing Run", "Legacy of the Lifebinder",
        };
        List<string> DefensiveSpells = new List<string> { "Obsidian Scales", "Renewing Blaze" };
        List<string> UtilitySpells = new List<string> { "Quell" };
        const int HealthstoneItemID = 5512;
        const int EssencePowerType = 19;
        private Random _rng = new Random(); private int _lastCastingID = 0; private int _interruptTargetPct = 0;

        public override void LoadSettings()
        {
            Settings.Add(new Setting("=== Devastation Evoker ==="));
            Settings.Add(new Setting("Use Dragonrage", true));
            Settings.Add(new Setting("Use Deep Breath", true));
            Settings.Add(new Setting("Use Trinkets", true));
            Settings.Add(new Setting("=== Defensives ==="));
            Settings.Add(new Setting("Use Defensives", true));
            Settings.Add(new Setting("Obsidian Scales HP %", 1, 100, 50));
            Settings.Add(new Setting("Healthstone HP %", 1, 100, 50));
            Settings.Add(new Setting("=== Interrupt ==="));
            Settings.Add(new Setting("Auto Interrupt", true));
            Settings.Add(new Setting("Interrupt at cast % (min)", 0, 100, 40));
            Settings.Add(new Setting("Interrupt at cast % (max)", 0, 100, 90));
        }

        public override void Initialize()
        {
            Inferno.PrintMessage("             //////////////////////////////////////", Color.Teal);
            Inferno.PrintMessage("             //  DEVASTATION - EVOKER (MID) V2   //", Color.Teal);
            Inferno.PrintMessage("             //////////////////////////////////////", Color.Teal);
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

            // Release empowered spells at stage 1
            if (Inferno.CastingName("player") == "Fire Breath" && Inferno.CurrentEmpowerStage("player") >= 1)
            { Inferno.Cast("Fire Breath", QuickDelay: true); return true; }
            if (Inferno.CastingName("player") == "Eternity Surge" && Inferno.CurrentEmpowerStage("player") >= 1)
            { Inferno.Cast("Eternity Surge", QuickDelay: true); return true; }

            // Don't act while channeling (Disintegrate) or casting (Living Flame, empowers charging)
            if (Inferno.IsChanneling("player")) return false;

            // Don't interrupt cast-time trinkets
            string _castName = Inferno.CastingName("player");
            if (_castName.Contains("Puzzle Box") || _castName.Contains("Emberwing")) return false;
            if (Inferno.CastingID("player") != 0) return false;
            if (!Inferno.UnitCanAttack("player", "target")) return false;
            if ((HasBuff("Dragonrage")) && HandleRacials()) return true;

            int enemies = Inferno.EnemiesNearUnit(10f, "target");
            if (enemies < 1) enemies = 1; if ((Inferno.IsCustomCodeOn("ForceST") || Inferno.IsCustomCodeOn("forcest"))) enemies = 1;
            int essence = GetEssence();
            int essenceDeficit = Inferno.MaxPower("player", EssencePowerType) - essence;
            bool dragonrageUp = HasBuff("Dragonrage");
            bool isSC = Inferno.IsSpellKnown("Mass Disintegrate");
            bool ttsUp = HasBuff("Tip the Scales");
            int drCD = Inferno.SpellCooldown("Dragonrage");
            bool canEmpower = !Inferno.IsSpellKnown("Animosity") || drCD >= GCDMAX() * 6;
            int ebStacks = Inferno.BuffStacks("Essence Burst");
            int ebMax = 2; // typical max

            // Trinkets during Dragonrage
            if (GetCheckBox("Use Trinkets") && !(Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds")) && dragonrageUp)
            { if (Inferno.CanUseEquippedItem(13)) { Inferno.Cast("trinket1"); return true; } if (Inferno.CanUseEquippedItem(14)) { Inferno.Cast("trinket2"); return true; } }

            // Route to sub-rotation
            if (enemies >= 3 && isSC) return AoE_SC(enemies, essence, essenceDeficit, dragonrageUp, canEmpower, ttsUp, drCD, ebStacks, ebMax);
            if (enemies >= 3) return AoE_FS(enemies, essence, essenceDeficit, dragonrageUp, canEmpower, ttsUp, drCD, ebStacks, ebMax);
            if (isSC) return ST_SC(enemies, essence, essenceDeficit, dragonrageUp, canEmpower, ttsUp, drCD, ebStacks, ebMax);
            return ST_FS(enemies, essence, essenceDeficit, dragonrageUp, canEmpower, ttsUp, drCD, ebStacks, ebMax);
        }

        // =====================================================================
        // FLAMESHAPER ST (actions.st_fs)
        // =====================================================================
        bool ST_FS(int enemies, int essence, int essenceDeficit, bool drUp, bool canEmpower, bool ttsUp, int drCD, int ebStacks, int ebMax)
        {
            // dragonrage
            if (CastCD("Dragonrage")) return true;
            // tip_the_scales,if=dragonrage.up&eternity_surge usable before fire_breath
            if (drUp && Inferno.SpellCooldown("Eternity Surge") <= Inferno.SpellCooldown("Fire Breath"))
                if (Inferno.CanCast("Tip the Scales", IgnoreGCD: true)) { Inferno.Cast("Tip the Scales", QuickDelay: true); return true; }
            // eternity_surge (empowered)
            if (canEmpower && Cast("Eternity Surge")) return true;
            // fire_breath,if=can_empower&refreshable
            if (canEmpower && Inferno.DebuffRemaining("Fire Breath") < 2000 && Cast("Fire Breath")) return true;
            // pyre,if=active_enemies>1&feed_the_flames&volatility
            if (enemies > 1 && Inferno.IsSpellKnown("Feed the Flames") && Inferno.IsSpellKnown("Volatility"))
                if (Cast("Pyre")) return true;
            // disintegrate
            if (Cast("Disintegrate")) return true;
            // azure_sweep
            if (Cast("Azure Sweep")) return true;
            // living_flame with burnout/leaping_flames/ancient_flame
            if (HasBuff("Burnout") || HasBuff("Leaping Flames") || HasBuff("Ancient Flame"))
                if (Cast("Living Flame")) return true;
            // azure_strike in cleave
            if (enemies > 1 && Cast("Azure Strike")) return true;
            // living_flame filler
            if (Cast("Living Flame")) return true;
            // green (Ancient Flame refresh)
            if (Inferno.IsSpellKnown("Ancient Flame") && !HasBuff("Ancient Flame") && Inferno.IsSpellKnown("Scarlet Adaptation") && !drUp)
            { if (Cast("Emerald Blossom")) return true; if (Cast("Verdant Embrace")) return true; }
            if (Cast("Azure Strike")) return true;
            return false;
        }

        // =====================================================================
        // SCALECOMMANDER ST (actions.st_sc)
        // =====================================================================
        bool ST_SC(int enemies, int essence, int essenceDeficit, bool drUp, bool canEmpower, bool ttsUp, int drCD, int ebStacks, int ebMax)
        {
            // deep_breath,if=strafing_run about to expire
            if (Inferno.IsSpellKnown("Strafing Run") && Inferno.BuffRemaining("Strafing Run") > 0 && Inferno.BuffRemaining("Strafing Run") <= GCD() * 2)
                // DISABLED: Deep Breath flight causes rotation to stall
            // if (GetCheckBox("Use Deep Breath") && Cast("Deep Breath")) return true;
            // dragonrage
            if (CastCD("Dragonrage")) return true;
            // tip_the_scales
            if (drUp && Inferno.SpellCooldown("Fire Breath") <= Inferno.SpellCooldown("Eternity Surge"))
                if (Inferno.CanCast("Tip the Scales", IgnoreGCD: true)) { Inferno.Cast("Tip the Scales", QuickDelay: true); return true; }
            // eternity_surge
            if (Cast("Eternity Surge")) return true;
            // fire_breath
            if (Cast("Fire Breath")) return true;
            // azure_sweep,if=essence_burst down or not capped
            if (ebStacks == 0 || ebStacks < ebMax)
                if (Cast("Azure Sweep")) return true;
            // disintegrate with mass_disintegrate_stacks
            if (HasBuff("Mass Disintegrate") && Cast("Disintegrate")) return true;
            // disintegrate
            if (Cast("Disintegrate")) return true;
            // azure_sweep
            if (Cast("Azure Sweep")) return true;
            // living_flame with procs
            if (HasBuff("Burnout") || HasBuff("Leaping Flames") || HasBuff("Ancient Flame"))
                if (Cast("Living Flame")) return true;
            if (enemies > 1 && Cast("Azure Strike")) return true;
            if (Cast("Living Flame")) return true;
            if (Inferno.IsSpellKnown("Ancient Flame") && !HasBuff("Ancient Flame") && Inferno.IsSpellKnown("Scarlet Adaptation") && !drUp)
            { if (Cast("Emerald Blossom")) return true; if (Cast("Verdant Embrace")) return true; }
            if (Cast("Azure Strike")) return true;
            return false;
        }

        // =====================================================================
        // FLAMESHAPER AOE (actions.aoe_fs)
        // =====================================================================
        bool AoE_FS(int enemies, int essence, int essenceDeficit, bool drUp, bool canEmpower, bool ttsUp, int drCD, int ebStacks, int ebMax)
        {
            // fire_breath before dragonrage
            if (drCD < GCDMAX() * 2 && Inferno.DebuffRemaining("Fire Breath") == 0)
                if (Cast("Fire Breath")) return true;
            // tip_the_scales
            if (drUp && Inferno.SpellCooldown("Eternity Surge") <= Inferno.SpellCooldown("Fire Breath"))
                if (Inferno.CanCast("Tip the Scales", IgnoreGCD: true)) { Inferno.Cast("Tip the Scales", QuickDelay: true); return true; }
            // eternity_surge with TTS
            if (ttsUp && Cast("Eternity Surge")) return true;
            // fire_breath with consume_flame
            if (Inferno.IsSpellKnown("Consume Flame") && canEmpower && Inferno.DebuffRemaining("Fire Breath") < 2000)
                if (Cast("Fire Breath")) return true;
            // dragonrage
            if (CastCD("Dragonrage")) return true;
            // eternity_surge
            if ((drUp || drCD > 4000) && Cast("Eternity Surge")) return true;
            // pyre
            if (drCD > GCDMAX() * 4 && (Inferno.BuffStacks("Charged Blast") >= 12 || enemies >= 4 || (enemies >= 3 && (Inferno.IsSpellKnown("Feed the Flames") || Inferno.IsSpellKnown("Volatility")))))
                if (Cast("Pyre")) return true;
            // deep_breath with imminent_destruction
            if (Inferno.IsSpellKnown("Imminent Destruction") && Inferno.DebuffRemaining("Fire Breath") == 0)
                // DISABLED: Deep Breath flight causes rotation to stall
            // if (GetCheckBox("Use Deep Breath") && Cast("Deep Breath")) return true;
            // azure_sweep
            if (Cast("Azure Sweep")) return true;
            // living_flame with leaping_flames
            if (HasBuff("Leaping Flames") && (ebStacks == 0 && essenceDeficit > 1))
                if (Cast("Living Flame")) return true;
            // eternity_surge for azure_sweep refresh
            if ((drUp || drCD > 4000) && Inferno.IsSpellKnown("Azure Sweep") && !HasBuff("Azure Sweep"))
                if (Cast("Eternity Surge")) return true;
            // living_flame with engulfing_blaze
            if (Inferno.IsSpellKnown("Engulfing Blaze") && (HasBuff("Leaping Flames") || HasBuff("Burnout") || HasBuff("Scarlet Adaptation") || HasBuff("Ancient Flame")))
                if (Cast("Living Flame")) return true;
            if (Cast("Azure Strike")) return true;
            return false;
        }

        // =====================================================================
        // SCALECOMMANDER AOE (actions.aoe_sc)
        // =====================================================================
        bool AoE_SC(int enemies, int essence, int essenceDeficit, bool drUp, bool canEmpower, bool ttsUp, int drCD, int ebStacks, int ebMax)
        {
            // deep_breath with strafing_run + imminent_destruction
            if (Inferno.IsSpellKnown("Imminent Destruction") && Inferno.IsSpellKnown("Strafing Run") && !HasBuff("Strafing Run"))
                // DISABLED: Deep Breath flight causes rotation to stall
            // if (GetCheckBox("Use Deep Breath") && Cast("Deep Breath")) return true;
            // tip_the_scales
            if (drUp && Inferno.SpellCooldown("Fire Breath") <= Inferno.SpellCooldown("Eternity Surge"))
                if (Inferno.CanCast("Tip the Scales", IgnoreGCD: true)) { Inferno.Cast("Tip the Scales", QuickDelay: true); return true; }
            // dragonrage
            if (CastCD("Dragonrage")) return true;
            // eternity_surge
            if (Cast("Eternity Surge")) return true;
            // fire_breath
            if (Cast("Fire Breath")) return true;
            // deep_breath with imminent_destruction
            if (Inferno.IsSpellKnown("Imminent Destruction"))
                // DISABLED: Deep Breath flight causes rotation to stall
            // if (GetCheckBox("Use Deep Breath") && Cast("Deep Breath")) return true;
            // pyre at 4+ with charged blast stacks
            if (enemies >= 4 && Inferno.BuffStacks("Charged Blast") >= 18 && drCD > GCDMAX() * 4)
                if (Cast("Pyre")) return true;
            // azure_sweep
            if (enemies <= 3 && (ebStacks == 0 || ebStacks < ebMax))
                if (Cast("Azure Sweep")) return true;
            // pyre
            if (drCD > GCDMAX() * 4 && !HasBuff("Mass Disintegrate") && (enemies >= 4 || (enemies >= 3 && Inferno.IsSpellKnown("Feed the Flames"))))
                if (Cast("Pyre")) return true;
            // disintegrate with mass_disintegrate
            if (HasBuff("Mass Disintegrate") && Cast("Disintegrate")) return true;
            // azure_sweep
            if (Cast("Azure Sweep")) return true;
            // living_flame with leaping_flames
            if (HasBuff("Leaping Flames") && ebStacks == 0 && essenceDeficit > 1)
                if (Cast("Living Flame")) return true;
            if (Cast("Azure Strike")) return true;
            return false;
        }

        // =====================================================================
        // DEFENSIVES / INTERRUPT / HELPERS
        // =====================================================================
        bool HandleDefensives()
        {
            if (!GetCheckBox("Use Defensives")) return false; int hpPct = GetPlayerHealthPct();
            if (hpPct <= GetSlider("Obsidian Scales HP %") && !HasBuff("Obsidian Scales") && Inferno.CanCast("Obsidian Scales", IgnoreGCD: true)) { Inferno.Cast("Obsidian Scales", QuickDelay: true); return true; }
            if (hpPct <= 40 && !HasBuff("Renewing Blaze") && Inferno.CanCast("Renewing Blaze", IgnoreGCD: true)) { Inferno.Cast("Renewing Blaze", QuickDelay: true); return true; }
            if (hpPct <= GetSlider("Healthstone HP %") && Inferno.CustomFunction("HasHealthstone") == 1 && Inferno.ItemCooldown(HealthstoneItemID) == 0) { Inferno.Cast("use_healthstone", QuickDelay: true); return true; }
            return false;
        }

        bool HandleInterrupt()
        {
            if (!GetCheckBox("Auto Interrupt")) return false;
            int castingID = Inferno.CastingID("target");
            if (castingID == 0 || !Inferno.IsInterruptable("target")) { _lastCastingID = 0; return false; }
            if (castingID != _lastCastingID) { _lastCastingID = castingID; int minPct = GetSlider("Interrupt at cast % (min)"); int maxPct = GetSlider("Interrupt at cast % (max)"); if (maxPct < minPct) maxPct = minPct; _interruptTargetPct = _rng.Next(minPct, maxPct + 1); }
            int elapsed = Inferno.CastingElapsed("target"); int remaining = Inferno.CastingRemaining("target"); int total = elapsed + remaining; if (total <= 0) return false;
            if ((elapsed * 100 / total) >= _interruptTargetPct && Inferno.CanCast("Quell", IgnoreGCD: true)) { Inferno.Cast("Quell", QuickDelay: true); _lastCastingID = 0; return true; }
            return false;
        }

        int GCD() { return Inferno.GCD(); }
        int GCDMAX() { int g = (int)(1500f / (1f + Inferno.Haste("player") / 100f)); return g < 750 ? 750 : g; }
        int GetEssence()
        {
            int e = Inferno.Power("player", EssencePowerType);
            if (Inferno.CastingName("player") == "Living Flame") e += 2;
            return Math.Min(e, 6);
        }
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
        bool CastCD(string n) { if ((Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false; if (n == "Dragonrage" && !GetCheckBox("Use Dragonrage")) return false; if (Inferno.CanCast(n)) { Inferno.Cast(n); Inferno.PrintMessage(">> " + n, Color.White); return true; } return false; }
    }
}
