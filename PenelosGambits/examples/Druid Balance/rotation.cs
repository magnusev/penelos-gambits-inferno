using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API;

namespace InfernoWow.Modules
{
    /// <summary>
    /// Balance Druid - Translated from SimulationCraft Midnight APL
    /// Hero trees: Elune's Chosen (detected via Boundless Moonlight) / Keeper of the Grove
    /// Sub-rotations: ec_st (Elune's Chosen ST), kotg_st (Keeper ST), aoe (3+ targets)
    /// Core: Eclipse management, Astral Power, CA/Inc windows, Force of Nature sequencing,
    /// Starweaver procs, Harmony of the Grove, Ascendant Stars/Fires.
    /// </summary>
    public class BalanceDruidRotation : Rotation
    {
        List<string> Abilities = new List<string> {
            "Wrath", "Starfire", "Starsurge", "Starfall", "Moonfire", "Sunfire",
            "Celestial Alignment", "Incarnation: Chosen of Elune", "Force of Nature",
            "Fury of Elune", "New Moon", "Half Moon", "Full Moon",
            "Wild Mushroom", "Convoke the Spirits", "Solar Eclipse", "Lunar Eclipse",
        };
        List<string> TalentChecks = new List<string> {
            "Incarnation: Chosen of Elune", "Convoke the Spirits", "Boundless Moonlight",
            "Dream Surge", "Starweaver", "Radiant Moonlight", "Aetherial Kindling",
        };
        List<string> DefensiveSpells = new List<string> { "Barkskin" };
        List<string> UtilitySpells = new List<string> { "Solar Beam", "Mark of the Wild", "Moonkin Form" };

        const int EclipseChargeMs = 15000;
        const int CAChargeMs = 60000;
        const int HealthstoneItemID = 5512;
        private Random _rng = new Random();
        private int _lastCastingID = 0;
        private int _interruptTargetPct = 0;
        private bool _opener = true;

        public override void LoadSettings()
        {
            Settings.Add(new Setting("=== Balance Druid ==="));
            Settings.Add(new Setting("Use Celestial Alignment/Incarnation", true));
            Settings.Add(new Setting("Use Convoke the Spirits", true));
            Settings.Add(new Setting("Use Trinkets", true));
            Settings.Add(new Setting("=== Defensives ==="));
            Settings.Add(new Setting("Use Defensives", true));
            Settings.Add(new Setting("Barkskin HP %", 1, 100, 60));
            Settings.Add(new Setting("Healthstone HP %", 1, 100, 50));
            Settings.Add(new Setting("=== Interrupt ==="));
            Settings.Add(new Setting("Auto Interrupt", true));
            Settings.Add(new Setting("Interrupt at cast % (min)", 0, 100, 40));
            Settings.Add(new Setting("Interrupt at cast % (max)", 0, 100, 90));
        }

        public override void Initialize()
        {
            Inferno.PrintMessage("             //////////////////////////////////////", Color.SlateBlue);
            Inferno.PrintMessage("             //    BALANCE - DRUID (MID)          //", Color.SlateBlue);
            Inferno.PrintMessage("             //              V 2.00               //", Color.SlateBlue);
            Inferno.PrintMessage("             //////////////////////////////////////", Color.SlateBlue);
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
            _opener = true;
            if (Inferno.BuffRemaining("Mark of the Wild") < 60000 && Inferno.CanCast("Mark of the Wild"))
            { Inferno.Cast("Mark of the Wild"); return true; }
            if (!InAnyForm() && Inferno.CanCast("Moonkin Form"))
            { Inferno.Cast("Moonkin Form"); return true; }
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

            int enemies = Inferno.EnemiesNearUnit(10f, "target");
            if (enemies < 1) enemies = 1;
            if ((Inferno.IsCustomCodeOn("ForceST") || Inferno.IsCustomCodeOn("forcest"))) enemies = 1;

            int ap = GetAstralPower();
            int apDeficit = Inferno.MaxPower("player", 8) - ap;
            bool isEC = Inferno.IsSpellKnown("Boundless Moonlight");
            bool caIncUp = HasBuff("Celestial Alignment") || HasBuff("Incarnation: Chosen of Elune");
            bool eclipseLunar = HasBuff("Eclipse (Lunar)");
            bool eclipseSolar = HasBuff("Eclipse (Solar)");
            bool eclipseDown = !eclipseLunar && !eclipseSolar;
            bool harmonyUp = HasBuff("Harmony of the Grove");
            bool ascStarsDown = !HasBuff("Ascendant Stars");
            bool touchCosmos = HasBuff("Touch the Cosmos");
            bool weaverWarp = HasBuff("Starweaver's Warp");
            bool weaverWeft = HasBuff("Starweaver's Weft");
            bool noWeaverProcs = !touchCosmos && !weaverWarp;
            bool ascFires = HasBuff("Ascendant Fires");
            int fonCD = Inferno.SpellCooldown("Force of Nature");
            int caIncCD = GetCAIncCD();
            bool cdWindow = fonCD > 15000 || caIncCD < 44000;
            bool cdWindowNarrow = fonCD > 30000 || (caIncCD > 10000 && caIncCD < 20000);
            bool caSoon = caIncCD < 3000;

            // Opener ends when CA/Inc is used
            if (caIncUp) _opener = false;

            // Trinkets during CA/Inc
                        if (HandleTrinkets(caIncUp, harmonyUp)) return true;

            // Racial damage CDs
            if ((HasBuff("Celestial Alignment") || HasBuff("Incarnation: Chosen of Elune")) && HandleRacials()) return true;

            // Route to sub-rotation
            if (enemies == 1 && isEC) return EC_ST(ap, apDeficit, enemies, caIncUp, eclipseDown, eclipseLunar, eclipseSolar, harmonyUp, ascStarsDown, touchCosmos, weaverWarp, weaverWeft, ascFires, fonCD, caIncCD);
            if (enemies == 1) return KotG_ST(ap, apDeficit, enemies, caIncUp, eclipseDown, eclipseLunar, eclipseSolar, harmonyUp, ascStarsDown, touchCosmos, weaverWarp, weaverWeft, noWeaverProcs, ascFires, fonCD, caIncCD, cdWindow, cdWindowNarrow, caSoon);
            return AoE(ap, apDeficit, enemies, isEC, caIncUp, eclipseDown, eclipseLunar, eclipseSolar, harmonyUp, ascStarsDown, touchCosmos, weaverWarp, weaverWeft, noWeaverProcs, ascFires, fonCD, caIncCD, cdWindow, cdWindowNarrow, caSoon);
        }

        // =====================================================================
        // ELUNE'S CHOSEN - SINGLE TARGET (actions.ec_st)
        // =====================================================================
        bool EC_ST(int ap, int apDeficit, int enemies, bool caIncUp, bool eclipseDown, bool eclipseLunar, bool eclipseSolar, bool harmonyUp, bool ascStarsDown, bool touchCosmos, bool weaverWarp, bool weaverWeft, bool ascFires, int fonCD, int caIncCD)
        {
            // celestial_alignment,if=buff.ca_inc.down&cooldown.eclipse.charges_fractional<1.5&ascendant_stars.down
            if (!caIncUp && ascStarsDown && (Inferno.ChargesFractional("Solar Eclipse", EclipseChargeMs) > 1.5f || Inferno.ChargesFractional("Lunar Eclipse", EclipseChargeMs) > 1.5f))
                if (CastCD("Celestial Alignment") || CastCD("Incarnation: Chosen of Elune")) return true;

            // DoTs
            if (Inferno.DebuffRemaining("Moonfire") < 2000 || (eclipseDown && Inferno.DebuffRemaining("Moonfire") < 5400))
                if (Cast("Moonfire")) return true;
            if (Inferno.DebuffRemaining("Sunfire") < 2000 || (eclipseDown && Inferno.DebuffRemaining("Sunfire") < 5400))
                if (Cast("Sunfire")) return true;

            // Convoke
            if (!(Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds")) && GetCheckBox("Use Convoke the Spirits"))
            {
                if ((caIncUp && ap < 40) || caIncCD > 50000)
                    if (Cast("Convoke the Spirits")) return true;
            }

            // Enter Lunar
            if (caIncCD > 15000 || _opener)
                if (Inferno.BuffRemaining("Eclipse (Lunar)") < GCD())
                    if (Cast("Lunar Eclipse")) return true;

            // Spenders
            if (weaverWarp || (touchCosmos && Inferno.IsSpellKnown("Starweaver") && ascStarsDown))
                if (Cast("Starfall")) return true;
            if (ap > 80 || !eclipseDown || weaverWeft || (touchCosmos && !ascStarsDown))
                if (Cast("Starsurge")) return true;

            // Fury of Elune
            if (Cast("Fury of Elune")) return true;

            // Builders
            if (Cast("Force of Nature")) return true;
            if (apDeficit > 12 && Cast("New Moon")) return true;
            if (apDeficit > 20 && Cast("Half Moon")) return true;
            if (apDeficit > 40 && Cast("Full Moon")) return true;
            if (eclipseSolar || Inferno.SpellCooldown("Wild Mushroom") < caIncCD)
                if (Cast("Wild Mushroom")) return true;

            // Filler
            if (!eclipseDown)
                if (Cast("Starfire")) return true;
            if (eclipseDown)
                if (Cast("Wrath")) return true;

            // Fallback
            if (Cast("Wrath")) return true;
            return false;
        }

        // =====================================================================
        // KEEPER OF THE GROVE - SINGLE TARGET (actions.kotg_st)
        // =====================================================================
        bool KotG_ST(int ap, int apDeficit, int enemies, bool caIncUp, bool eclipseDown, bool eclipseLunar, bool eclipseSolar, bool harmonyUp, bool ascStarsDown, bool touchCosmos, bool weaverWarp, bool weaverWeft, bool noWeaverProcs, bool ascFires, int fonCD, int caIncCD, bool cdWindow, bool cdWindowNarrow, bool caSoon)
        {
            // celestial_alignment,if=prev_gcd.1.force_of_nature
            // Simplified: CA after FoN (check FoN just went on CD)
            if (fonCD > 43000 && !caIncUp)
                if (CastCD("Celestial Alignment") || CastCD("Incarnation: Chosen of Elune")) return true;

            // DoTs
            if (Inferno.DebuffRemaining("Moonfire") < 2000 || (eclipseDown && Inferno.DebuffRemaining("Moonfire") < 5400))
                if (Cast("Moonfire")) return true;
            if (Inferno.DebuffRemaining("Sunfire") < 2000 || (eclipseDown && Inferno.DebuffRemaining("Sunfire") < 5400))
                if (Cast("Sunfire")) return true;

            // Fury of Elune
            if (_opener && !eclipseDown && ascStarsDown)
                if (Cast("Fury of Elune")) return true;
            if (!_opener && (harmonyUp || fonCD < GCDMAX() || (Inferno.IsSpellKnown("Radiant Moonlight") && fonCD > 20000)))
                if (Cast("Fury of Elune")) return true;

            // Enter Solar Eclipse
            if ((_opener || (cdWindow && Inferno.SpellCooldown("Solar Eclipse") < 3000) || cdWindowNarrow))
                if (Inferno.BuffRemaining("Eclipse (Solar)") < GCD())
                    if (Cast("Solar Eclipse")) return true;

            // Treants - Force of Nature
            if (_opener && ascStarsDown && Inferno.DebuffRemaining("Sunfire") > 16000)
                if (Cast("Force of Nature")) return true;
            if (!_opener)
                if (Cast("Force of Nature")) return true;

            // Convoke
            if (!(Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds")) && GetCheckBox("Use Convoke the Spirits"))
            {
                // During CA/Inc with low AP
                if (caIncUp && ap < 40)
                    if (Cast("Convoke the Spirits")) return true;
                // Outside CA/Inc: when Harmony is up (or FoN recently used as proxy) and AP low
                bool harmonyProxy = harmonyUp || (fonCD > 40000); // FoN just went on CD = Harmony active
                if (!caIncUp && harmonyProxy && ap < 50)
                    if (Cast("Convoke the Spirits")) return true;
                // Fallback: CA on long cooldown, just use Convoke
                if (!caIncUp && caIncCD > 30000)
                    if (Cast("Convoke the Spirits")) return true;
            }

            // Cast Wrath if going Solar
            if (!Inferno.IsSpellKnown("Convoke the Spirits") && ascStarsDown)
            {
                if ((_opener && ap < 50) || (!_opener && ap < 80 && noWeaverProcs && fonCD < 15000))
                    if (Cast("Wrath")) return true;
            }

            // Sunfire before CDs
            if ((_opener && ascStarsDown) || (!_opener && Inferno.DebuffRemaining("Sunfire") < 10000 && caSoon && fonCD < 3000))
                if (Inferno.DebuffRemaining("Sunfire") < 10000 && Cast("Sunfire")) return true;

            // Spenders
            if (weaverWarp)
                if (Cast("Starfall")) return true;
            // starsurge with complex conditions
            if (weaverWeft || (touchCosmos && !ascStarsDown))
                if (Cast("Starsurge")) return true;
            if (ap > 60 || eclipseSolar)
                if (Cast("Starsurge")) return true;

            // Instant builder
            if (ascFires && eclipseLunar)
                if (Cast("Starfire")) return true;

            // Builders
            if (apDeficit > 12 && Cast("New Moon")) return true;
            if (apDeficit > 20 && Cast("Half Moon")) return true;
            if (apDeficit > 40 && Cast("Full Moon")) return true;
            if (eclipseSolar || Inferno.SpellCooldown("Wild Mushroom") < caIncCD)
                if (Cast("Wild Mushroom")) return true;

            // Wrath filler
            if (Cast("Wrath")) return true;
            return false;
        }

        // =====================================================================
        // AOE (actions.aoe) — 2+ targets
        // =====================================================================
        bool AoE(int ap, int apDeficit, int enemies, bool isEC, bool caIncUp, bool eclipseDown, bool eclipseLunar, bool eclipseSolar, bool harmonyUp, bool ascStarsDown, bool touchCosmos, bool weaverWarp, bool weaverWeft, bool noWeaverProcs, bool ascFires, int fonCD, int caIncCD, bool cdWindow, bool cdWindowNarrow, bool caSoon)
        {
            // Celestial Alignment
            // KotG: after Force of Nature. EC: after Fury of Elune when eclipse down.
            if (!isEC && fonCD > 43000 && !caIncUp)
            { if (CastCD("Celestial Alignment") || CastCD("Incarnation: Chosen of Elune")) return true; }
            if (isEC && eclipseDown && !caIncUp)
            { if (CastCD("Celestial Alignment") || CastCD("Incarnation: Chosen of Elune")) return true; }

            // DoTs
            if (Inferno.DebuffRemaining("Moonfire") < 2000 || (eclipseDown && Inferno.DebuffRemaining("Moonfire") < 5400))
                if (Cast("Moonfire")) return true;
            if (Inferno.DebuffRemaining("Sunfire") < 2000 || (eclipseDown && Inferno.DebuffRemaining("Sunfire") < 5400))
                if (Cast("Sunfire")) return true;

            // Fury of Elune
            if (isEC)
            { if (Cast("Fury of Elune")) return true; }
            else
            {
                if ((_opener && !eclipseDown && ascStarsDown) || (!_opener && (harmonyUp || fonCD < GCDMAX() || (Inferno.IsSpellKnown("Radiant Moonlight") && fonCD > 20000))))
                    if (Cast("Fury of Elune")) return true;
            }

            // Enter Solar Eclipse
            if ((_opener || (cdWindow && Inferno.SpellCooldown("Solar Eclipse") < 3000) || cdWindowNarrow))
                if (Inferno.BuffRemaining("Eclipse (Solar)") < GCD())
                    if (Cast("Solar Eclipse")) return true;

            // Enter Lunar Eclipse (AoE favors lunar for Starfire cleave)
            if (isEC && (_opener || caIncCD > 15000))
                if (Inferno.BuffRemaining("Eclipse (Lunar)") < GCD())
                    if (Cast("Lunar Eclipse")) return true;
            if (!isEC && enemies > 2)
            {
                if ((_opener || (cdWindow && Inferno.SpellCooldown("Lunar Eclipse") < 3000) || cdWindowNarrow))
                    if (Inferno.BuffRemaining("Eclipse (Lunar)") < GCD())
                        if (Cast("Lunar Eclipse")) return true;
            }

            // Convoke
            if (!(Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds")) && GetCheckBox("Use Convoke the Spirits"))
            {
                if (caIncUp && ap < 40)
                    if (Cast("Convoke the Spirits")) return true;
                bool harmonyProxy = harmonyUp || (fonCD > 40000);
                if (!caIncUp && harmonyProxy)
                    if (Cast("Convoke the Spirits")) return true;
                if (!caIncUp && caIncCD > 30000)
                    if (Cast("Convoke the Spirits")) return true;
            }

            // Wrath if going Solar (low target count)
            if (!Inferno.IsSpellKnown("Convoke the Spirits") && enemies < 3 && ascStarsDown)
            {
                if ((_opener && ap < 50) || (!_opener && ap < 80 && noWeaverProcs && fonCD < 15000))
                    if (Cast("Wrath")) return true;
            }

            // Sunfire before CDs
            if (!Inferno.IsSpellKnown("Aetherial Kindling"))
            {
                if ((_opener && ascStarsDown) || (!_opener && Inferno.DebuffRemaining("Sunfire") < 10000 && caSoon && fonCD < 3000))
                    if (Inferno.DebuffRemaining("Sunfire") < 10000 && Cast("Sunfire")) return true;
            }

            // Treants
            if (!_opener || ascStarsDown)
                if (Cast("Force of Nature")) return true;

            // Spenders
            if (ap > 80 || !eclipseDown || !weaverWeft)
                if (Cast("Starfall")) return true;
            if (weaverWeft)
                if (Cast("Starsurge")) return true;

            // Builders
            if (ascFires && eclipseLunar)
                if (Cast("Starfire")) return true;
            if (apDeficit > 12 && Cast("New Moon")) return true;
            if (apDeficit > 20 && Cast("Half Moon")) return true;
            if (apDeficit > 40 && Cast("Full Moon")) return true;
            if (eclipseSolar || Inferno.SpellCooldown("Wild Mushroom") < caIncCD)
                if (Cast("Wild Mushroom")) return true;

            // Starfire in AoE
            if (isEC)
                if (Cast("Starfire")) return true;
            if (eclipseDown && enemies > 6)
                if (Cast("Starfire")) return true;
            if (eclipseLunar && ((enemies > 2 && caIncUp) || (enemies <= 2 && !caIncUp)))
                if (Cast("Starfire")) return true;

            // Wrath fallback
            if (Cast("Wrath")) return true;
            return false;
        }

        // =====================================================================
        // DEFENSIVES / INTERRUPT / TRINKETS
        // =====================================================================
        bool HandleDefensives()
        {
            if (!GetCheckBox("Use Defensives")) return false;
            int hpPct = GetPlayerHealthPct();
            if (hpPct <= GetSlider("Barkskin HP %") && Inferno.CanCast("Barkskin", IgnoreGCD: true))
            { Inferno.Cast("Barkskin", QuickDelay: true); return true; }
            if (hpPct <= GetSlider("Healthstone HP %") && Inferno.CustomFunction("HasHealthstone") == 1 && Inferno.ItemCooldown(HealthstoneItemID) == 0)
            { Inferno.Cast("use_healthstone", QuickDelay: true); return true; }
            return false;
        }

        bool HandleInterrupt()
        {
            if (!GetCheckBox("Auto Interrupt")) return false;
            int castingID = Inferno.CastingID("target");
            if (castingID == 0 || !Inferno.IsInterruptable("target")) { _lastCastingID = 0; return false; }
            if (castingID != _lastCastingID)
            {
                _lastCastingID = castingID;
                int minPct = GetSlider("Interrupt at cast % (min)");
                int maxPct = GetSlider("Interrupt at cast % (max)");
                if (maxPct < minPct) maxPct = minPct;
                _interruptTargetPct = _rng.Next(minPct, maxPct + 1);
            }
            int elapsed = Inferno.CastingElapsed("target");
            int remaining = Inferno.CastingRemaining("target");
            int total = elapsed + remaining;
            if (total <= 0) return false;
            if ((elapsed * 100 / total) >= _interruptTargetPct && Inferno.CanCast("Solar Beam", IgnoreGCD: true))
            { Inferno.Cast("Solar Beam", QuickDelay: true); _lastCastingID = 0; return true; }
            return false;
        }

        bool HandleTrinkets(bool caIncUp, bool harmonyUp)
        {
            if (!GetCheckBox("Use Trinkets") || (Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;
            if (!caIncUp) return false;
            if (harmonyUp || !Inferno.IsSpellKnown("Dream Surge"))
            {
                if (Inferno.CanUseEquippedItem(13)) { Inferno.Cast("trinket1"); return true; }
                if (Inferno.CanUseEquippedItem(14)) { Inferno.Cast("trinket2"); return true; }
            }
            return false;
        }

        // =====================================================================
        // HELPERS
        // =====================================================================
        bool InAnyForm() { return Inferno.HasBuff("Cat Form") || Inferno.HasBuff("Bear Form") || Inferno.HasBuff("Moonkin Form") || Inferno.HasBuff("Travel Form") || Inferno.HasBuff("Flight Form") || Inferno.HasBuff("Aquatic Form"); }
        int GCD() { return Inferno.GCD(); }
        int GCDMAX() { int g = (int)(1500f / (1f + Inferno.Haste("player") / 100f)); return g < 750 ? 750 : g; }

        int GetAstralPower()
        {
            int ap = Inferno.Power("player", 8);
            string casting = Inferno.CastingName("player");
            if (casting == "Wrath") ap += 8;
            if (casting == "Starfire") ap += 10;
            return Math.Min(ap, 100);
        }

        int GetCAIncCD()
        {
            int caCD = Inferno.SpellCooldown("Celestial Alignment");
            int incCD = Inferno.SpellCooldown("Incarnation: Chosen of Elune");
            if (Inferno.IsSpellKnown("Incarnation: Chosen of Elune")) return incCD;
            return caCD;
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
        int GetPlayerHealthPct()
        {
            int hp = Inferno.Health("player"); int m = Inferno.MaxHealth("player");
            if (m < 1) m = 1; return (hp * 100) / m;
        }

        bool Cast(string n)
        {
            if (Inferno.CanCast(n)) { Inferno.Cast(n); Inferno.PrintMessage(">> " + n, Color.White); return true; }
            return false;
        }

        bool CastCD(string n)
        {
            if ((Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;
            if (!GetCheckBox("Use Celestial Alignment/Incarnation")) return false;
            if (Inferno.CanCast(n)) { Inferno.Cast(n); Inferno.PrintMessage(">> " + n, Color.White); return true; }
            return false;
        }
    }
}
