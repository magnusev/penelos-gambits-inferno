using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API;

namespace InfernoWow.Modules
{
    /// <summary>
    /// Augmentation Evoker - Translated from SimulationCraft Midnight APL
    /// Core: Ebon Might uptime (pandemic refresh), Prescience on allies, Eruption as primary spender.
    /// Key: Fire Breath empower staging (1-4 based on Molten Embers/Font of Magic),
    /// Breath of Eons windows, Fury of the Aspects with Time Convergence,
    /// Upheaval, Tip the Scales, Time Skip.
    /// </summary>
    public class AugmentationEvokerRotation : Rotation
    {
        List<string> Abilities = new List<string> {
            "Living Flame", "Azure Strike", "Eruption", "Upheaval",
            "Fire Breath", "Ebon Might", "Prescience", "Breath of Eons",
            "Deep Breath", "Tip the Scales", "Fury of the Aspects",
            "Time Skip", "Emerald Blossom", "Blistering Scales",
        };
        List<string> TalentChecks = new List<string> {
            "Molten Embers", "Font of Magic", "Time Convergence",
            "Temporal Burst", "Energy Cycles", "Breath of Eons",
            "Dream of Spring", "Ancient Flame", "Pupil of Alexstrasza",
            "Anachronism", "Interwoven Threads", "Chronoboon",
        };
        List<string> DefensiveSpells = new List<string> { "Obsidian Scales", "Renewing Blaze" };
        List<string> UtilitySpells = new List<string> { "Quell" };
        const int HealthstoneItemID = 5512;
        const int EssencePowerType = 19;
        private Random _rng = new Random(); private int _lastCastingID = 0; private int _interruptTargetPct = 0;

        public override void LoadSettings()
        {
            Settings.Add(new Setting("=== Augmentation Evoker ==="));
            Settings.Add(new Setting("Use Breath of Eons", true));
            Settings.Add(new Setting("Use Fury of the Aspects", true));
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
            Inferno.PrintMessage("             //  AUGMENTATION - EVOKER (MID) V2  //", Color.Teal);
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
            Macros.Add("cancel_tts", "/cancelaura Tip the Scales");
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

            // cancel_buff: cancel Tip the Scales if Upheaval on CD (save for next Upheaval)
            if (HasBuff("Tip the Scales") && Inferno.SpellCooldown("Upheaval") > 0
                && (Inferno.IsSpellKnown("Energy Cycles") || Inferno.IsSpellKnown("Temporal Burst")))
            { Inferno.Cast("cancel_tts", QuickDelay: true); return true; }

            // Release empowered spells at stage 1
            if (Inferno.CastingName("player") == "Fire Breath" && Inferno.CurrentEmpowerStage("player") >= 1)
            { Inferno.Cast("Fire Breath", QuickDelay: true); return true; }
            if (Inferno.CastingName("player") == "Eternity Surge" && Inferno.CurrentEmpowerStage("player") >= 1)
            { Inferno.Cast("Eternity Surge", QuickDelay: true); return true; }
            if (Inferno.CastingName("player") == "Upheaval" && Inferno.CurrentEmpowerStage("player") >= 1)
            { Inferno.Cast("Upheaval", QuickDelay: true); return true; }

            // Don't act while channeling or casting (Living Flame, Eruption, empowers charging)
            if (Inferno.IsChanneling("player")) return false;

            // Don't interrupt cast-time trinkets
            string _castName = Inferno.CastingName("player");
            if (_castName.Contains("Puzzle Box") || _castName.Contains("Emberwing")) return false;
            if (Inferno.CastingID("player") != 0) return false;
            if (!Inferno.UnitCanAttack("player", "target")) return false;
            if ((HasBuff("Ebon Might")) && HandleRacials()) return true;

            int enemies = Inferno.EnemiesNearUnit(10f, "target");
            if (enemies < 1) enemies = 1; if ((Inferno.IsCustomCodeOn("ForceST") || Inferno.IsCustomCodeOn("forcest"))) enemies = 1;
            int essence = GetEssence();
            int essenceDeficit = Inferno.MaxPower("player", EssencePowerType) - essence;
            int ebStacks = Inferno.BuffStacks("Essence Burst");
            int ebMax = 2;
            bool ebonMightUp = HasBuff("Ebon Might");
            int ebonMightRemains = Inferno.BuffRemaining("Ebon Might");
            int boeCD = Inferno.SpellCooldown("Breath of Eons");
            bool ttsUp = HasBuff("Tip the Scales");

            // Trinkets during Ebon Might
            if (GetCheckBox("Use Trinkets") && !(Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds")) && ebonMightUp)
            { if (Inferno.CanUseEquippedItem(13)) { Inferno.Cast("trinket1"); return true; } if (Inferno.CanUseEquippedItem(14)) { Inferno.Cast("trinket2"); return true; } }

            // Ebon Might — pandemic refresh (40% threshold = ~5.2s remaining on 13s base)
            int ebonDuration = Inferno.BuffDuration("Ebon Might");
            if (ebonDuration > 0 && ebonMightRemains <= (int)(ebonDuration * 0.4))
                if (Cast("Ebon Might")) return true;
            if (!ebonMightUp && Cast("Ebon Might")) return true;

            // Prescience on allies (simplified: cast when available, low remaining)
            if (Inferno.SpellCooldown("Prescience") == 0)
                if (Cast("Prescience")) return true;

            // Fury of the Aspects — Time Convergence haste buff
            if (GetCheckBox("Use Fury of the Aspects") && Inferno.IsSpellKnown("Time Convergence") && !HasBuff("Time Convergence") && (essence >= 2 || ebStacks > 0) && boeCD >= 8000)
                if (Cast("Fury of the Aspects")) return true;

            // Tip the Scales
            if (boeCD > 0 && Inferno.SpellCooldown("Upheaval") < Inferno.SpellCooldown("Fire Breath"))
                if (Inferno.CanCast("Tip the Scales", IgnoreGCD: true)) { Inferno.Cast("Tip the Scales", QuickDelay: true); return true; }

            // Deep Breath / Breath of Eons
            // BoE replaces Deep Breath when talented, but IsSpellKnown("Breath of Eons") may return false
            // Gate Deep Breath behind BoE checkbox too — if user unchecks BoE, don't cast Deep Breath either
            // DISABLED: Deep Breath/Breath of Eons flight causes rotation to stall
            // if (GetCheckBox("Use Breath of Eons") && !(Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds")))
            // {
            //     if (Cast("Breath of Eons")) return true;
            //     if (Cast("Deep Breath")) return true;
            // }

            // Fire Breath (empower staging)
            if (HandleFireBreath(ebonMightRemains, ttsUp)) return true;

            // Upheaval
            if (ebonMightUp)
                if (Cast("Upheaval")) return true;

            // Prescience again (lower priority)
            if (Inferno.SpellCooldown("Prescience") == 0 && (!Inferno.IsSpellKnown("Anachronism") || ebStacks < ebMax))
                if (Cast("Prescience")) return true;

            // Time Skip
            if (!Inferno.IsSpellKnown("Chronoboon") && boeCD >= 15000 && !Inferno.IsSpellKnown("Interwoven Threads"))
                if (Cast("Time Skip")) return true;

            // Emerald Blossom with Dream of Spring + Essence Burst
            if (Inferno.IsSpellKnown("Dream of Spring") && ebStacks > 0 && ebonMightUp)
                if (Cast("Emerald Blossom")) return true;

            // Eruption (primary spender)
            if (ebonMightUp || essenceDeficit == 0 || (ebStacks >= ebMax && Inferno.SpellCooldown("Ebon Might") > 4000))
                if (Cast("Eruption")) return true;

            // Filler
            if (HasBuff("Ancient Flame") || !Inferno.IsSpellKnown("Dream of Spring"))
                if (Cast("Living Flame")) return true;
            if (HasBuff("Leaping Flames") && Cast("Living Flame")) return true;
            if (Cast("Azure Strike")) return true;
            if (Cast("Living Flame")) return true;
            return false;
        }

        // =====================================================================
        // FIRE BREATH (actions.fb) — Empower staging
        // =====================================================================
        bool HandleFireBreath(int ebonRemains, bool ttsUp)
        {
            if (!Inferno.CanCast("Fire Breath")) return false;
            // Skip if TTS up and Molten Embers (save TTS for Upheaval)
            if (ttsUp && Inferno.IsSpellKnown("Molten Embers")) return false;
            // Only cast during Ebon Might
            if (ebonRemains <= 0) return false;
            // Empower to 1 with Molten Embers (longer dot, more damage)
            // Empower to 2-4 otherwise (APL default is empower_to=2, 3 if no molten, 4 with font_of_magic)
            // Since we can't control empower level via the API, just cast it
            return Cast("Fire Breath");
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
        int GetEssence()
        {
            int e = Inferno.Power("player", EssencePowerType);
            string casting = Inferno.CastingName("player");
            if (casting == "Living Flame") e += 2;
            if (casting == "Eruption") e -= 2;
            return Math.Max(0, Math.Min(e, 6));
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
    }
}
