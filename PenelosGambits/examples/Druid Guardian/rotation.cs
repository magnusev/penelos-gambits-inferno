using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API;

namespace InfernoWow.Modules
{
    /// <summary>
    /// Guardian Druid - Translated from SimulationCraft Midnight APL
    /// Core: Bear Form priority with cat-weaving via Fluid Form / Heart of the Wild.
    /// Key: Ravage Maul, Red Moon Mangle, Thrash stacking, Ironfur uptime,
    /// Feline Potential counter tracking, HotW cat/moonkin windows.
    /// </summary>
    public class GuardianDruidRotation : Rotation
    {
        List<string> Abilities = new List<string> {
            "Maul", "Mangle", "Thrash", "Swipe", "Moonfire", "Ironfur",
            "Ferocious Bite", "Rake", "Rip", "Shred",
            "Bear Form", "Cat Form", "Moonkin Form",
            "Barkskin", "Bristling Fur", "Lunar Beam", "Berserk",
            "Incarnation: Guardian of Ursoc", "Heart of the Wild",
            "Convoke the Spirits", "Wild Guardian", "Sundering Roar",
        };
        List<string> TalentChecks = new List<string> {
            "Heart of the Wild", "Fluid Form", "Killing Blow", "Fount of Strength",
            "Wildpower Surge", "Red Moon", "Lunar Calling", "Galactic Guardian",
            "Lunation", "Ravage", "Soul of the Forest", "Moonkin Form",
            "Boundless Moonlight", "Incarnation: Guardian of Ursoc",
        };
        List<string> DefensiveSpells = new List<string> { "Ironfur", "Barkskin", "Survival Instincts", "Frenzied Regeneration" };
        List<string> UtilitySpells = new List<string> { "Skull Bash", "Mark of the Wild" };
        const int HealthstoneItemID = 5512; const int RagePowerType = 1;
        private Random _rng = new Random(); private int _lastCastingID = 0; private int _interruptTargetPct = 0;

        public override void LoadSettings()
        {
            Settings.Add(new Setting("=== Guardian Druid ==="));
            Settings.Add(new Setting("Use Berserk/Incarnation", true));
            Settings.Add(new Setting("Use Heart of the Wild (cat-weave)", true));
            Settings.Add(new Setting("Use Trinkets", true));
            Settings.Add(new Setting("=== Defensives ==="));
            Settings.Add(new Setting("Use Defensives", true));
            Settings.Add(new Setting("Ironfur uptime", true));
            Settings.Add(new Setting("Barkskin HP %", 1, 100, 50));
            Settings.Add(new Setting("Survival Instincts HP %", 1, 100, 30));
            Settings.Add(new Setting("Frenzied Regen HP %", 1, 100, 50));
            Settings.Add(new Setting("Healthstone HP %", 1, 100, 50));
            Settings.Add(new Setting("=== Interrupt ==="));
            Settings.Add(new Setting("Auto Interrupt", true));
            Settings.Add(new Setting("Interrupt at cast % (min)", 0, 100, 40));
            Settings.Add(new Setting("Interrupt at cast % (max)", 0, 100, 90));
        }

        public override void Initialize()
        {
            Inferno.PrintMessage("             //////////////////////////////////////", Color.SaddleBrown);
            Inferno.PrintMessage("             //   GUARDIAN - DRUID (MID) V2.00    //", Color.SaddleBrown);
            Inferno.PrintMessage("             //////////////////////////////////////", Color.SaddleBrown);
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
            if (Inferno.BuffRemaining("Mark of the Wild") < 60000 && Inferno.CanCast("Mark of the Wild"))
            { Inferno.Cast("Mark of the Wild"); return true; }
            if (!InAnyForm() && Inferno.CanCast("Bear Form"))
            { Inferno.Cast("Bear Form"); return true; }
            return false;
        }

        public override bool CombatTick()
        {
            if (Inferno.IsDead("player") || Inferno.IsGhost("player")) return false;
            if (HandleDefensives()) return true;
            if (HandleInterrupt()) return true;
            if (!Inferno.UnitCanAttack("player", "target")) return false;

            // Always be in Bear Form in combat
            if (!Inferno.HasBuff("Bear Form") && Inferno.CanCast("Bear Form"))
            { Inferno.Cast("Bear Form"); return true; }
            if (Inferno.IsChanneling("player")) return false;

            // Don't interrupt cast-time trinkets
            string _castName = Inferno.CastingName("player");
            if (_castName.Contains("Puzzle Box") || _castName.Contains("Emberwing")) return false;

            int enemies = Inferno.EnemiesNearUnit(8f, "player");
            if (enemies < 1) enemies = 1; if ((Inferno.IsCustomCodeOn("ForceST") || Inferno.IsCustomCodeOn("forcest"))) enemies = 1;

            if ((true) && HandleRacials()) return true;
            if (HandleCooldowns(enemies)) return true;
            return BearRotation(enemies);
        }

        // =====================================================================
        // COOLDOWNS (actions.cooldowns)
        // =====================================================================
        bool HandleCooldowns(int enemies)
        {
            // Trinkets
            if (GetCheckBox("Use Trinkets") && !(Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds")))
            { if (Inferno.CanUseEquippedItem(13)) { Inferno.Cast("trinket1"); return true; } if (Inferno.CanUseEquippedItem(14)) { Inferno.Cast("trinket2"); return true; } }

            int rage = Inferno.Power("player", RagePowerType);
            bool hasKillingBlow = Inferno.IsSpellKnown("Killing Blow");
            bool ravageUp = HasBuff("Ravage");
            bool bearFormUp = Inferno.HasBuff("Bear Form");

            // bristling_fur when low rage and no ravage
            if (!ravageUp && ((rage < 60 && hasKillingBlow) || (rage < 40 && !hasKillingBlow)))
                if (Cast("Bristling Fur")) return true;

            // barkskin in bear form
            if (bearFormUp) { int hpPct = GetPlayerHealthPct(); if (hpPct <= GetSlider("Barkskin HP %") && Inferno.CanCast("Barkskin", IgnoreGCD: true)) { Inferno.Cast("Barkskin", QuickDelay: true); return true; } }

            // lunar_beam synced with berserk/incarn
            if (Inferno.SpellCooldown("Berserk") == 0 || Inferno.SpellCooldown("Incarnation: Guardian of Ursoc") == 0 || Inferno.SpellCooldown("Berserk") > 60000)
                if (CastCD("Lunar Beam")) return true;

            // heart_of_the_wild (cat-weave entry)
            if (GetCheckBox("Use Heart of the Wild (cat-weave)") && Inferno.IsSpellKnown("Heart of the Wild"))
            {
                bool inCat = Inferno.HasBuff("Cat Form");
                bool inMoonkin = Inferno.HasBuff("Moonkin Form");
                if ((enemies <= 5 && inCat) || (inMoonkin && enemies >= 6 && Inferno.IsSpellKnown("Moonkin Form")))
                    if (CastCD("Heart of the Wild")) return true;
            }

            // convoke
            if (CastCD("Convoke the Spirits")) return true;

            // berserk/incarnation
            bool hasHotW = Inferno.IsSpellKnown("Heart of the Wild");
            if (!hasHotW || Inferno.SpellCooldown("Heart of the Wild") > 0 || !Inferno.IsSpellKnown("Ravage"))
            {
                if (CastCD("Incarnation: Guardian of Ursoc")) return true;
                if (CastCD("Berserk")) return true;
            }

            // wild_guardian
            if (ravageUp || !Inferno.IsSpellKnown("Ravage"))
                if (Cast("Wild Guardian")) return true;

            // sundering_roar
            if (Cast("Sundering Roar")) return true;

            return false;
        }

        // =====================================================================
        // BEAR ROTATION (actions.bear)
        // =====================================================================
        bool BearRotation(int enemies)
        {
            int rage = Inferno.Power("player", RagePowerType);
            bool bearFormUp = Inferno.HasBuff("Bear Form");
            bool catFormUp = Inferno.HasBuff("Cat Form");
            bool ravageUp = HasBuff("Ravage");
            bool hasKillingBlow = Inferno.IsSpellKnown("Killing Blow");
            bool hasFountOfStrength = Inferno.IsSpellKnown("Fount of Strength");
            bool hasFluidForm = Inferno.IsSpellKnown("Fluid Form");
            bool hasHotW = Inferno.IsSpellKnown("Heart of the Wild");
            bool hasWildpower = Inferno.IsSpellKnown("Wildpower Surge");
            bool hasRedMoon = Inferno.IsSpellKnown("Red Moon");
            bool berserkUp = Inferno.HasBuff("Berserk") || Inferno.HasBuff("Incarnation: Guardian of Ursoc");
            bool felinePotential = HasBuff("Feline Potential");
            int fpCounter = Inferno.BuffStacks("Feline Potential Counter");
            bool hotWReady = hasHotW && Inferno.SpellCooldown("Heart of the Wild") == 0;

            // bear_form,if=!bear_form.up&!feline_potential.up
            if (!bearFormUp && !felinePotential)
                if (Cast("Bear Form")) return true;

            // Maul with Ravage
            if (ravageUp && rage >= 40 && !hasKillingBlow) if (Cast("Maul")) return true;
            if (ravageUp && rage >= 60 && hasKillingBlow) if (Cast("Maul")) return true;
            // Maul without Ravage at high rage (no Fount of Strength)
            if (!ravageUp && rage >= 55 && !hasFountOfStrength) if (Cast("Maul")) return true;

            // mangle,if=dot.red_moon.ticking
            if (Inferno.DebuffRemaining("Red Moon") > 0)
                if (Cast("Mangle")) return true;

            // Cat-weave DISABLED — drops Bear Form mitigation, causes deaths in M+
            // if (hotWReady && hasHotW) { ... }

            // Thrash (refreshable or stacking)
            if (Inferno.DebuffRemaining("Thrash") < 3600 || Inferno.DebuffStacks("Thrash") < 3)
                if (Cast("Thrash")) return true;

            // Ironfur uptime — maintain stacks, cast with rage > 50 or if no stacks
            if (GetCheckBox("Ironfur uptime") && !ravageUp)
            {
                int ironfurStacks = Inferno.BuffStacks("Ironfur");
                int ironfurRemaining = Inferno.BuffRemaining("Ironfur");
                if ((ironfurStacks == 0 && rage >= 40) || (ironfurRemaining < 2000 && rage >= 50) || rage >= 80)
                    if (Inferno.CanCast("Ironfur", IgnoreGCD: true)) { Inferno.Cast("Ironfur", QuickDelay: true); return true; }
            }

            // ferocious_bite DISABLED (cat-weave removed)

            // Rake/Rip/Shred DISABLED (cat-weave removed)

            // mangle during berserk with wildpower (keep FP counter low)
            if (berserkUp && fpCounter < 6 && hasWildpower)
                if (Cast("Mangle")) return true;

            // thrash with lunar_calling
            if (Inferno.IsSpellKnown("Lunar Calling"))
                if (Cast("Thrash")) return true;

            // Maul rage dump with Fount of Strength at 90+
            if (!ravageUp && rage >= 90 && hasFountOfStrength)
                if (Cast("Maul")) return true;
            // Maul outside berserk
            if (!berserkUp && !ravageUp)
            {
                if (rage >= 40 && !hasKillingBlow) if (Cast("Maul")) return true;
                if (rage >= 55 && hasKillingBlow && hasFountOfStrength) if (Cast("Maul")) return true;
            }

            // Mangle as rage generator
            int rageThreshold = hasFountOfStrength ? 108 : 88;
            if (Inferno.IsSpellKnown("Soul of the Forest")) rageThreshold -= 5;
            if (berserkUp || (rage < rageThreshold))
                if (Cast("Mangle")) return true;

            // Thrash filler
            if (Cast("Thrash")) return true;

            // Moonfire with Galactic Guardian
            if (Inferno.IsSpellKnown("Galactic Guardian") && HasBuff("Galactic Guardian") && bearFormUp && Inferno.IsSpellKnown("Boundless Moonlight") && !hasRedMoon)
                if (Cast("Moonfire")) return true;

            // Swipe filler
            if (Cast("Swipe")) return true;

            // Moonfire with Lunation
            if (Inferno.IsSpellKnown("Lunation") && bearFormUp && !hasRedMoon)
                if (Cast("Moonfire")) return true;

            return false;
        }

        // =====================================================================
        // DEFENSIVES / INTERRUPT / HELPERS
        // =====================================================================
        bool HandleDefensives()
        {
            if (!GetCheckBox("Use Defensives")) return false;
            int hpPct = GetPlayerHealthPct();
            if (hpPct <= GetSlider("Survival Instincts HP %") && !Inferno.HasBuff("Survival Instincts") && Inferno.CanCast("Survival Instincts", IgnoreGCD: true))
            { Inferno.Cast("Survival Instincts", QuickDelay: true); return true; }
            if (hpPct <= GetSlider("Barkskin HP %") && !Inferno.HasBuff("Barkskin") && Inferno.CanCast("Barkskin", IgnoreGCD: true))
            { Inferno.Cast("Barkskin", QuickDelay: true); return true; }
            if (hpPct <= GetSlider("Frenzied Regen HP %") && Inferno.CanCast("Frenzied Regeneration", IgnoreGCD: true))
            { Inferno.Cast("Frenzied Regeneration", QuickDelay: true); return true; }
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
            if ((elapsed * 100 / total) >= _interruptTargetPct && Inferno.CanCast("Skull Bash", IgnoreGCD: true))
            { Inferno.Cast("Skull Bash", QuickDelay: true); _lastCastingID = 0; return true; }
            return false;
        }

        bool InAnyForm() { return Inferno.HasBuff("Cat Form") || Inferno.HasBuff("Bear Form") || Inferno.HasBuff("Moonkin Form") || Inferno.HasBuff("Travel Form") || Inferno.HasBuff("Flight Form") || Inferno.HasBuff("Aquatic Form"); }
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
        bool Cast(string n) { if (Inferno.CanCast(n)) { Inferno.Cast(n); Inferno.PrintMessage(">> " + n, Color.White); return true; } return false; }
        bool CastCD(string n) { if ((Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false; if ((n == "Berserk" || n == "Incarnation: Guardian of Ursoc") && !GetCheckBox("Use Berserk/Incarnation")) return false; if (Inferno.CanCast(n)) { Inferno.Cast(n); Inferno.PrintMessage(">> " + n, Color.White); return true; } return false; }
    }
}
