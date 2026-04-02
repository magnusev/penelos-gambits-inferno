using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API;

namespace InfernoWow.Modules
{
    /// <summary>
    /// Vengeance Demon Hunter - Translated from SimulationCraft Midnight APL
    /// Hero trees: Aldrachi Reaver (Reaver's Glaive cycle) vs Annihilator (Voidfall + Meta cycling)
    /// AR sub-rotations: ar_glaive_cycle, ar_cooldowns, ar_fillers
    /// Anni sub-rotations: anni_voidfall, anni_meta_entry, anni_meta, anni_cooldowns, anni_fillers, ur_fishing
    /// Core: Soul Fragment management, Spirit Bomb thresholds, Fiery Brand/Demise windows, Metamorphosis.
    /// </summary>
    public class VengeanceDemonHunterRotation : Rotation
    {
        List<string> Abilities = new List<string> {
            "Soul Cleave", "Spirit Bomb", "Fracture", "Immolation Aura",
            "Sigil of Flame", "Sigil of Spite", "Fiery Brand", "Fel Devastation",
            "Soul Carver", "Metamorphosis", "Felblade", "Throw Glaive",
            "Reaver's Glaive", "Demon Spikes",
            "Infernal Strike",
        };
        List<string> TalentChecks = new List<string> {
            "Reaver's Glaive", "Untethered Rage", "Fiery Demise", "Charred Flesh",
            "Down in Flames", "Soul Sigils", "Darkglare Boon", "Fallout",
            "Unhindered Assault",
        };
        List<string> DefensiveSpells = new List<string> { "Demon Spikes", "Fiery Brand" };
        List<string> UtilitySpells = new List<string> { "Disrupt" };
        const int HealthstoneItemID = 5512;
        const int FuryPowerType = 17;
        private Random _rng = new Random(); private int _lastCastingID = 0; private int _interruptTargetPct = 0;

        public override void LoadSettings()
        {
            Settings.Add(new Setting("=== Vengeance Demon Hunter ==="));
            Settings.Add(new Setting("Use Metamorphosis", true));
            Settings.Add(new Setting("Use Fel Devastation", true));
            Settings.Add(new Setting("Use Trinkets", true));
            Settings.Add(new Setting("=== Defensives ==="));
            Settings.Add(new Setting("Use Defensives", true));
            Settings.Add(new Setting("Demon Spikes uptime", true));
            Settings.Add(new Setting("Fiery Brand HP %", 1, 100, 50));
            Settings.Add(new Setting("Healthstone HP %", 1, 100, 50));
            Settings.Add(new Setting("=== Interrupt ==="));
            Settings.Add(new Setting("Auto Interrupt", true));
            Settings.Add(new Setting("Interrupt at cast % (min)", 0, 100, 40));
            Settings.Add(new Setting("Interrupt at cast % (max)", 0, 100, 90));
        }

        public override void Initialize()
        {
            Inferno.PrintMessage("             //////////////////////////////////////", Color.DarkMagenta);
            Inferno.PrintMessage("             //  VENGEANCE - DH (MID) V2.00      //", Color.DarkMagenta);
            Inferno.PrintMessage("             //////////////////////////////////////", Color.DarkMagenta);
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
            if (!Inferno.UnitCanAttack("player", "target")) return false;
            if (Inferno.IsChanneling("player")) return false;

            // Don't interrupt cast-time trinkets
            string _castName = Inferno.CastingName("player");
            if (_castName.Contains("Puzzle Box") || _castName.Contains("Emberwing")) return false;

            int enemies = Inferno.EnemiesNearUnit(8f, "player");
            if (enemies < 1) enemies = 1; if ((Inferno.IsCustomCodeOn("ForceST") || Inferno.IsCustomCodeOn("forcest"))) enemies = 1;

            // Demon Spikes uptime
            if (GetCheckBox("Demon Spikes uptime") && !HasBuff("Demon Spikes") && Inferno.CanCast("Demon Spikes", IgnoreGCD: true))
            { Inferno.Cast("Demon Spikes", QuickDelay: true); return true; }

            bool isAR = Inferno.IsSpellKnown("Reaver's Glaive");
            if (isAR) return AR(enemies);
            return Anni(enemies);
        }

        // =====================================================================
        // ALDRACHI REAVER (actions.ar)
        // =====================================================================
        bool AR(int enemies)
        {
            int fury = Inferno.Power("player", FuryPowerType);
            int frags = Inferno.BuffStacks("Soul Fragments");
            bool metaUp = HasBuff("Metamorphosis");
            bool brandTicking = Inferno.DebuffRemaining("Fiery Brand") > 0;
            bool fieryDemise = Inferno.IsSpellKnown("Fiery Demise") && brandTicking;
            bool aoe = enemies >= 3;
            int fragTarget = fieryDemise ? 3 : (metaUp ? 4 : 5);
            bool rendingStrike = HasBuff("Rending Strike");
            bool glaiveFlurry = HasBuff("Glaive Flurry");
            bool urProc = HasBuff("Untethered Rage");

            // Trinkets
                        if (HandleTrinkets(metaUp)) return true;

            // Racial damage CDs
            if ((HasBuff("Metamorphosis")) && HandleRacials()) return true;

            // fiery_brand if overcapped or not using fiery_demise
            if (!brandTicking && (Inferno.ChargesFractional("Fiery Brand", 60000) >= 1.9f || !Inferno.IsSpellKnown("Fiery Demise")))
                if (Cast("Fiery Brand")) return true;
            // fiery_brand before meta with fire CD soon
            if (Inferno.IsSpellKnown("Fiery Demise") && !brandTicking && !metaUp && Inferno.SpellCooldown("Metamorphosis") == 0)
                if (Cast("Fiery Brand")) return true;

            // UR proc Meta
            if (urProc && !HasBuff("Voidfall Spending"))
                if (CastCD("Metamorphosis")) return true;
            // Hardcast Meta
            if (!metaUp)
                if (CastCD("Metamorphosis")) return true;

            // Glaive Cycle
            if (AR_GlaiveCycle(fury, frags, aoe, rendingStrike, glaiveFlurry, fragTarget)) return true;
            // Cooldowns
            if (AR_Cooldowns(frags, fieryDemise, brandTicking, rendingStrike, glaiveFlurry, fragTarget)) return true;
            // Fillers
            return AR_Fillers(fury, frags, metaUp, aoe, fragTarget);
        }

        // === AR Glaive Cycle ===
        bool AR_GlaiveCycle(int fury, int frags, bool aoe, bool rending, bool flurry, int fragTarget)
        {
            // reavers_glaive,if=buff.reavers_glaive.up&!rending&!flurry
            if (HasBuff("Reaver's Glaive") && !rending && !flurry)
                if (Cast("Reaver's Glaive")) return true;
            // AoE: fracture first when both buffs up
            if (rending && flurry && aoe)
                if (Cast("Fracture")) return true;
            // soul_cleave when both buffs up
            if (rending && flurry)
                if (Cast("Soul Cleave")) return true;
            // fracture with rending only
            if (rending && !flurry)
                if (Cast("Fracture")) return true;
            // spirit_bomb at 5+ frags during flurry
            if (flurry && !rending && frags >= 5)
                if (Cast("Spirit Bomb")) return true;
            // soul_cleave during flurry
            if (flurry && !rending)
                if (Cast("Soul Cleave")) return true;
            return false;
        }

        // === AR Cooldowns ===
        bool AR_Cooldowns(int frags, bool fieryDemise, bool brandTicking, bool rending, bool flurry, int fragTarget)
        {
            // spirit_bomb during fiery_demise
            if (fieryDemise && frags >= 3 && Cast("Spirit Bomb")) return true;
            // immolation_aura with charred_flesh + brand
            if (brandTicking && Inferno.IsSpellKnown("Charred Flesh") && Cast("Immolation Aura")) return true;
            // sigil_of_spite
            if (frags <= 2 + (Inferno.IsSpellKnown("Soul Sigils") ? 1 : 0))
                if (Cast("Sigil of Spite")) return true;
            // soul_carver
            if (Cast("Soul Carver")) return true;
            // fel_devastation,if=!rending&!flurry (don't interrupt glaive cycle)
            if (!rending && !flurry)
                if (CastFelDev()) return true;
            // immolation_aura in brand without charred_flesh
            if (fieryDemise && !Inferno.IsSpellKnown("Charred Flesh"))
                if (Cast("Immolation Aura")) return true;
            return false;
        }

        // === AR Fillers ===
        bool AR_Fillers(int fury, int frags, bool metaUp, bool aoe, int fragTarget)
        {
            if (aoe && Cast("Immolation Aura")) return true;
            if (frags < fragTarget && Cast("Fracture")) return true;
            if (frags >= fragTarget && Cast("Spirit Bomb")) return true;
            if (metaUp && Cast("Fracture")) return true;
            if (aoe && Cast("Sigil of Flame")) return true;
            if (Cast("Immolation Aura")) return true;
            if (Cast("Fracture")) return true;
            if (Cast("Felblade")) return true;
            if (Cast("Sigil of Flame")) return true;
            if (Cast("Soul Cleave")) return true;
            // vengeful_retreat with unhindered_assault
            if (Cast("Throw Glaive")) return true;
            return false;
        }

        // =====================================================================
        // ANNIHILATOR (actions.anni)
        // =====================================================================
        bool Anni(int enemies)
        {
            int fury = Inferno.Power("player", FuryPowerType);
            int frags = Inferno.BuffStacks("Soul Fragments");
            bool metaUp = HasBuff("Metamorphosis");
            bool brandTicking = Inferno.DebuffRemaining("Fiery Brand") > 0;
            bool fieryDemise = Inferno.IsSpellKnown("Fiery Demise") && brandTicking;
            bool aoe = enemies >= 3;
            int fragTarget = fieryDemise ? 3 : (metaUp ? 4 : 5);
            bool urProc = HasBuff("Untethered Rage");
            bool vfBuilding = HasBuff("Voidfall Building");
            bool vfSpending = HasBuff("Voidfall Spending");
            int vfBuildStacks = Inferno.BuffStacks("Voidfall Building");
            int vfSpendStacks = Inferno.BuffStacks("Voidfall Spending");
            bool metaReady = !metaUp && Inferno.SpellCooldown("Metamorphosis") == 0;
            bool metaEntry = !metaUp && !vfSpending && vfBuildStacks < 2 && metaReady;

            // Trinkets
            if (HandleTrinkets(metaUp)) return true;

            // Voidfall handling (always runs)
            if (Anni_Voidfall(frags, fragTarget, fury, vfBuildStacks, vfSpendStacks, vfSpending, fieryDemise, brandTicking)) return true;

            // UR proc Meta
            if (urProc && !vfSpending)
                if (CastCD("Metamorphosis")) return true;

            // Coordinated Meta entry
            bool scReady = Inferno.SpellCooldown("Soul Carver") == 0;
            bool sosReady = Inferno.SpellCooldown("Sigil of Spite") == 0;
            int spbCD = Inferno.SpellCooldown("Spirit Bomb");
            bool burstReady = metaEntry && Inferno.SpellCooldown("Metamorphosis") == 0 && (spbCD < GCDMAX() * 2 || spbCD > 20000) && (scReady || sosReady);
            if (burstReady)
                if (Anni_MetaEntry(frags, brandTicking)) return true;

            // Standalone Meta entry (no burst CDs available)
            if (metaEntry && !burstReady && frags >= 3)
            {
                if (Cast("Spirit Bomb")) return true;
                if (CastCD("Metamorphosis")) return true;
            }

            // UR Fishing (last 6s of Meta)
            bool urFishing = Inferno.IsSpellKnown("Untethered Rage") && metaUp && Inferno.BuffRemaining("Metamorphosis") < 6000 && !urProc;
            if (urFishing)
                if (Anni_URFishing(frags, fragTarget)) return true;

            // During Meta (not UR fishing)
            if (metaUp && !urFishing)
                if (Anni_Meta(frags, fragTarget, aoe, fieryDemise, brandTicking, vfSpending)) return true;

            // Cooldowns
            if (Anni_Cooldowns(frags, fieryDemise, brandTicking, vfSpending, metaUp, aoe)) return true;

            // Fillers
            return Anni_Fillers(fury, frags, fragTarget, aoe, vfSpending);
        }

        // === Anni Voidfall ===
        bool Anni_Voidfall(int frags, int fragTarget, int fury, int buildStacks, int spendStacks, bool vfSpending, bool fieryDemise, bool brandTicking)
        {
            // fiery_brand at peak building (2) or spending (3)
            if (Inferno.IsSpellKnown("Fiery Demise") && !brandTicking && (buildStacks >= 2 || spendStacks >= 3))
                if (Cast("Fiery Brand")) return true;
            // At peak spending (3): generate frags → SpB
            if (spendStacks >= 3)
            {
                if (frags < fragTarget)
                {
                    if (CastFelDev()) return true;
                    if (Cast("Soul Carver")) return true;
                    if (Cast("Sigil of Spite")) return true;
                    if (Inferno.IsSpellKnown("Fallout") && Cast("Immolation Aura")) return true;
                }
                if (frags >= fragTarget && Cast("Spirit Bomb")) return true;
            }
            // soul_cleave during spending
            if (vfSpending && Cast("Soul Cleave")) return true;
            // Pool fury at peak building
            if (buildStacks >= 2 && fury >= 70 && Cast("Fracture")) return true;
            return false;
        }

        // === Anni Meta Entry (coordinated burst) ===
        bool Anni_MetaEntry(int frags, bool brandTicking)
        {
            if (Inferno.IsSpellKnown("Fiery Demise") && !brandTicking)
                if (Cast("Fiery Brand")) return true;
            if (Inferno.IsSpellKnown("Charred Flesh") && brandTicking && Inferno.BuffRemaining("Immolation Aura") < 2000)
                if (Cast("Immolation Aura")) return true;
            if (frags >= 3 && Cast("Spirit Bomb")) return true;
            // Meta off-GCD after SpB
            if (Inferno.SpellCooldown("Spirit Bomb") > 20000)
                if (CastCD("Metamorphosis")) return true;
            if (frags < 3 && Cast("Fracture")) return true;
            return false;
        }

        // === Anni Meta (during active Meta) ===
        bool Anni_Meta(int frags, int fragTarget, bool aoe, bool fieryDemise, bool brandTicking, bool vfSpending)
        {
            // Maintain Brand during Meta
            if (Inferno.IsSpellKnown("Fiery Demise") && !brandTicking)
                if (Cast("Fiery Brand")) return true;
            if (Inferno.IsSpellKnown("Charred Flesh") && brandTicking)
                if (Cast("Immolation Aura")) return true;
            // Burst follow-up: SC/SoS after SpB
            int scCD = Inferno.SpellCooldown("Soul Carver");
            int sosCD = Inferno.SpellCooldown("Sigil of Spite");
            if (frags <= 3 && scCD == 0 && Cast("Soul Carver")) return true;
            if (frags <= 2 + (Inferno.IsSpellKnown("Soul Sigils") ? 1 : 0) && sosCD == 0 && scCD > 0)
                if (Cast("Sigil of Spite")) return true;
            // SpB at fragment target
            if (frags >= fragTarget && Cast("Spirit Bomb")) return true;
            // Fracture as primary generator during Meta
            if (frags < fragTarget && !vfSpending && Cast("Fracture")) return true;
            // Fel Devastation
            if (!vfSpending)
                if (CastFelDev()) return true;
            // Late-Meta SoS/SC
            if (Inferno.SpellCooldown("Metamorphosis") > 25000)
            {
                if (frags <= 2 + (Inferno.IsSpellKnown("Soul Sigils") ? 1 : 0) && Cast("Sigil of Spite")) return true;
                if (frags <= 3 && Cast("Soul Carver")) return true;
            }
            return false;
        }

        // === Anni UR Fishing (last 6s of Meta) ===
        bool Anni_URFishing(int frags, int fragTarget)
        {
            if (HasBuff("Seething Anger") && frags >= 3 && Cast("Spirit Bomb")) return true;
            if (frags >= fragTarget && Cast("Spirit Bomb")) return true;
            if (frags <= 2 + (Inferno.IsSpellKnown("Soul Sigils") ? 1 : 0))
            { if (Cast("Sigil of Spite")) return true; if (Cast("Soul Carver")) return true; }
            if (Cast("Fracture")) return true;
            if (frags >= 1 && Cast("Soul Cleave")) return true;
            return false;
        }

        // === Anni Cooldowns ===
        bool Anni_Cooldowns(int frags, bool fieryDemise, bool brandTicking, bool vfSpending, bool metaUp, bool aoe)
        {
            // Fiery Brand
            if (!brandTicking && (Inferno.ChargesFractional("Fiery Brand", 60000) >= 1.9f || !Inferno.IsSpellKnown("Fiery Demise") || !Inferno.IsSpellKnown("Down in Flames")))
                if (Cast("Fiery Brand")) return true;
            if (Inferno.IsSpellKnown("Charred Flesh") && brandTicking)
                if (Cast("Immolation Aura")) return true;
            if (frags <= 2 + (Inferno.IsSpellKnown("Soul Sigils") ? 1 : 0))
                if (Cast("Sigil of Spite")) return true;
            if (frags <= 3 && Cast("Soul Carver")) return true;
            if (!vfSpending && (!metaUp || Inferno.IsSpellKnown("Darkglare Boon")))
                if (CastFelDev()) return true;
            return false;
        }

        // === Anni Fillers ===
        bool Anni_Fillers(int fury, int frags, int fragTarget, bool aoe, bool vfSpending)
        {
            if (frags >= fragTarget && Cast("Spirit Bomb")) return true;
            // fracture if about to cap charges
            if (Inferno.ChargesFractional("Fracture", 4500) >= 1.9f && frags < 6)
                if (Cast("Fracture")) return true;
            if (aoe && Cast("Immolation Aura")) return true;
            if (!vfSpending && Cast("Fracture")) return true;
            if (aoe && Cast("Sigil of Flame")) return true;
            if (Cast("Felblade")) return true;
            if (Cast("Immolation Aura")) return true;
            if (Cast("Sigil of Flame")) return true;
            if (Cast("Soul Cleave")) return true;
            if (Cast("Fracture")) return true;
            if (Cast("Throw Glaive")) return true;
            return false;
        }

        // =====================================================================
        // SHARED: DEFENSIVES / INTERRUPT / TRINKETS / HELPERS
        // =====================================================================
        bool HandleTrinkets(bool metaUp)
        {
            if (!GetCheckBox("Use Trinkets") || (Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;
            if (metaUp || Inferno.SpellCooldown("Metamorphosis") < 10000)
            { if (Inferno.CanUseEquippedItem(13)) { Inferno.Cast("trinket1"); return true; } if (Inferno.CanUseEquippedItem(14)) { Inferno.Cast("trinket2"); return true; } }
            return false;
        }

        bool HandleDefensives()
        {
            if (!GetCheckBox("Use Defensives")) return false;
            int hpPct = GetPlayerHealthPct();
            if (hpPct <= GetSlider("Fiery Brand HP %") && Inferno.DebuffRemaining("Fiery Brand") == 0 && Inferno.CanCast("Fiery Brand", IgnoreGCD: true))
            { Inferno.Cast("Fiery Brand", QuickDelay: true); return true; }
            if (hpPct <= 25 && !HasBuff("Metamorphosis") && Inferno.CanCast("Metamorphosis"))
            { Inferno.Cast("Metamorphosis"); Inferno.PrintMessage(">> Metamorphosis (emergency)", Color.White); return true; }
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
            if ((elapsed * 100 / total) >= _interruptTargetPct && Inferno.CanCast("Disrupt", IgnoreGCD: true))
            { Inferno.Cast("Disrupt", QuickDelay: true); _lastCastingID = 0; return true; }
            return false;
        }

        int GCD() { return Inferno.GCD(); }
        int GCDMAX() { int g = (int)(1500f / (1f + Inferno.Haste("player") / 100f)); return g < 750 ? 750 : g; }
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
        bool CastCD(string n)
        {
            if ((Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;
            if (n == "Metamorphosis" && !GetCheckBox("Use Metamorphosis")) return false;
            if (Inferno.CanCast(n)) { Inferno.Cast(n); Inferno.PrintMessage(">> " + n, Color.White); return true; }
            return false;
        }
        bool CastFelDev()
        {
            if (!GetCheckBox("Use Fel Devastation")) return false;
            return Cast("Fel Devastation");
        }
    }
}
