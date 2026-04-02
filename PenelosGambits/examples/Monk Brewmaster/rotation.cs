using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API;

namespace InfernoWow.Modules
{
    /// <summary>
    /// Brewmaster Monk - Translated from SimulationCraft Midnight APL
    /// Core: Keg Smash → Blackout Kick → Breath of Fire → Tiger Palm filler
    /// Key talents: Blackout Combo (weave Tiger Palm before BK), Aspect of Harmony,
    /// Wisdom of the Wall, Flurry Strikes, Scalding Brew, Stormstout's Last Keg.
    /// </summary>
    public class BrewmasterMonkRotation : Rotation
    {
        List<string> Abilities = new List<string> {
            "Touch of Death",
            "Keg Smash", "Blackout Kick", "Breath of Fire", "Tiger Palm",
            "Spinning Crane Kick", "Rising Sun Kick", "Exploding Keg",
            "Invoke Niuzao, the Black Ox", "Purifying Brew", "Celestial Brew",
            "Celestial Infusion",
            "Fortifying Brew", "Black Ox Brew", "Rushing Jade Wind",
            "Chi Burst", "Expel Harm", "Empty the Cellar",
        };
        List<string> TalentChecks = new List<string> {
            "Blackout Combo", "Aspect of Harmony", "Wisdom of the Wall",
            "Flurry Strikes", "Scalding Brew", "Stormstout's Last Keg",
            "Celestial Infusion",
        };
        List<string> DefensiveSpells = new List<string> { "Purifying Brew", "Celestial Brew", "Celestial Infusion", "Fortifying Brew" };
        List<string> UtilitySpells = new List<string> { "Spear Hand Strike" };
        const int HealthstoneItemID = 5512;
        const int EnergyPowerType = 3;
        private Random _rng = new Random(); private int _lastCastingID = 0; private int _interruptTargetPct = 0;

        public override void LoadSettings()
        {
            Settings.Add(new Setting("=== Brewmaster Monk ==="));
            Settings.Add(new Setting("Use Touch of Death", true));
            Settings.Add(new Setting("Use Invoke Niuzao", true));
            Settings.Add(new Setting("Use Trinkets", true));
            Settings.Add(new Setting("=== Defensives ==="));
            Settings.Add(new Setting("Use Defensives", true));
            Settings.Add(new Setting("Purifying Brew Stagger %", 1, 100, 60));
            Settings.Add(new Setting("Celestial Brew HP %", 1, 100, 60));
            Settings.Add(new Setting("Fortifying Brew HP %", 1, 100, 30));
            Settings.Add(new Setting("Healthstone HP %", 1, 100, 50));
            Settings.Add(new Setting("=== Interrupt ==="));
            Settings.Add(new Setting("Auto Interrupt", true));
            Settings.Add(new Setting("Interrupt at cast % (min)", 0, 100, 40));
            Settings.Add(new Setting("Interrupt at cast % (max)", 0, 100, 90));
        }

        public override void Initialize()
        {
            Inferno.PrintMessage("             //////////////////////////////////////", Color.DarkCyan);
            Inferno.PrintMessage("             //  BREWMASTER - MONK (MID) V2.00   //", Color.DarkCyan);
            Inferno.PrintMessage("             //////////////////////////////////////", Color.DarkCyan);
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
            if ((true) && HandleRacials()) return true;
            if (Inferno.IsChanneling("player")) return false;

            // Don't interrupt cast-time trinkets
            string _castName = Inferno.CastingName("player");
            if (_castName.Contains("Puzzle Box") || _castName.Contains("Emberwing")) return false;
            int enemies = Inferno.EnemiesNearUnit(8f, "player");
            if (enemies < 1) enemies = 1; if ((Inferno.IsCustomCodeOn("ForceST") || Inferno.IsCustomCodeOn("forcest"))) enemies = 1;
            int energy = Inferno.Power("player", EnergyPowerType);
            bool hasBlackoutCombo = Inferno.IsSpellKnown("Blackout Combo");
            bool blackoutComboUp = HasBuff("Blackout Combo");
            bool hasAoH = Inferno.IsSpellKnown("Aspect of Harmony");
            bool hasWotW = Inferno.IsSpellKnown("Wisdom of the Wall");
            bool hasFlurry = Inferno.IsSpellKnown("Flurry Strikes");
            bool hasScalding = Inferno.IsSpellKnown("Scalding Brew");
            bool hasStormstout = Inferno.IsSpellKnown("Stormstout's Last Keg");

            // Touch of Death — high priority when enemy HP < player HP
            if (GetCheckBox("Use Touch of Death") && Inferno.Health("target") < Inferno.Health("player"))
                if (Cast("Touch of Death")) return true;
            bool niuzaoUp = HasBuff("Invoke Niuzao, the Black Ox");
            bool aohSpenderUp = Inferno.BuffRemaining(450711) > GCD();
            bool emptyBarrelUp = HasBuff("Empty Barrel");
            float kegFrac = Inferno.ChargesFractional("Keg Smash", 8000);
            int kegCharges = (int)kegFrac;
            int maxKegCharges = 1 + (hasStormstout ? 1 : 0);

            // Trinkets
            if (GetCheckBox("Use Trinkets") && !(Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds")))
            { if (Inferno.CanUseEquippedItem(13)) { Inferno.Cast("trinket1"); return true; } if (Inferno.CanUseEquippedItem(14)) { Inferno.Cast("trinket2"); return true; } }

            // black_ox_brew,if=talent.aoh&cooldown.celestial_brew.charges_fractional<1
            if (hasAoH && CBChargesFrac() < 1f)
                if (Cast("Black Ox Brew")) return true;
            // black_ox_brew,if=!talent.aoh&energy<40
            if (!hasAoH && energy < 40)
                if (Cast("Black Ox Brew")) return true;

            // celestial_brew,if=buff.aspect_of_harmony_spender.up&!buff.empty_barrel.up
            if (aohSpenderUp && !emptyBarrelUp)
                if (CastCB()) return true;

            // keg_smash,if=buff.aspect_of_harmony_spender.up&buff.empty_barrel.up
            if (aohSpenderUp && emptyBarrelUp)
                if (CastKS()) return true;

            // breath_of_fire,if=talent.wisdom_of_the_wall&buff.invoke_niuzao.up
            if (hasWotW && niuzaoUp)
                if (Cast("Breath of Fire")) return true;
            // keg_smash,if=talent.wisdom_of_the_wall&buff.invoke_niuzao.up
            if (hasWotW && niuzaoUp)
                if (CastKS()) return true;

            // blackout_kick,if=talent.blackout_combo&!buff.blackout_combo.up
            if (hasBlackoutCombo && !blackoutComboUp)
                if (Cast("Blackout Kick")) return true;

            // purifying_brew
            if (HandlePurifyingBrew()) return true;

            // chi_burst
            if (Cast("Chi Burst")) return true;

            // invoke_niuzao
            if (!(Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds")) && GetCheckBox("Use Invoke Niuzao"))
                if (Cast("Invoke Niuzao, the Black Ox")) return true;

            // tiger_palm,if=buff.blackout_combo.up&cooldown.blackout_kick.remains<1.3
            if (blackoutComboUp && Inferno.SpellCooldown("Blackout Kick") < 1300)
                if (Cast("Tiger Palm")) return true;

            // exploding_keg,if=cooldown.keg_smash.charges_fractional<1
            if (kegFrac < 1f)
                if (Cast("Exploding Keg")) return true;

            // empty_the_cellar,if=talent.aoh&cooldown.celestial_brew.remains>15
            if (hasAoH && CBCD() > 15000)
                if (Cast("Empty the Cellar")) return true;
            // empty_the_cellar,if=!talent.aoh&buff.empty_the_cellar.remains<1.5
            if (!hasAoH && Inferno.BuffRemaining("Empty the Cellar") < 1500)
                if (Cast("Empty the Cellar")) return true;

            // breath_of_fire,if=cooldown.blackout_kick.remains>1.5&!buff.empty_barrel.up&keg_charges<max
            if (Inferno.SpellCooldown("Blackout Kick") > 1500 && !emptyBarrelUp && kegCharges < maxKegCharges)
                if (Cast("Breath of Fire")) return true;

            // tiger_palm,if=buff.blackout_combo.up
            if (blackoutComboUp)
                if (Cast("Tiger Palm")) return true;

            // celestial_brew,if=talent.flurry_strikes
            if (hasFlurry)
                if (CastCB()) return true;
            // breath_of_fire,if=talent.flurry_strikes
            if (hasFlurry)
                if (Cast("Breath of Fire")) return true;
            // keg_smash,if=talent.flurry_strikes
            if (hasFlurry)
                if (CastKS()) return true;
            // keg_smash,if=talent.scalding_brew
            if (hasScalding)
                if (CastKS()) return true;
            // keg_smash,if=buff.empty_barrel.up
            if (emptyBarrelUp)
                if (CastKS()) return true;
            // keg_smash,if=charges=max
            if (kegCharges >= maxKegCharges)
                if (CastKS()) return true;

            // breath_of_fire
            if (Cast("Breath of Fire")) return true;
            // empty_the_cellar
            if (Cast("Empty the Cellar")) return true;
            // rushing_jade_wind
            if (Cast("Rushing Jade Wind")) return true;
            // keg_smash (unconditional)
            if (CastKS()) return true;
            // blackout_kick
            if (Cast("Blackout Kick")) return true;

            // tiger_palm,if=talent.aoh&energy>50-regen*2
            if (hasAoH && energy > 50) if (Cast("Tiger Palm")) return true;
            // tiger_palm,if=energy>65-regen
            if (energy > 55) if (Cast("Tiger Palm")) return true;

            // expel_harm
            if (Cast("Expel Harm")) return true;
            return false;
        }

        bool HandlePurifyingBrew()
        {
            if (!GetCheckBox("Use Defensives")) return false;
            int stagger = GetStaggerLevel();
            if (stagger >= GetSlider("Purifying Brew Stagger %") && Inferno.CanCast("Purifying Brew", IgnoreGCD: true))
            { Inferno.Cast("Purifying Brew", QuickDelay: true); return true; }
            return false;
        }

        bool HandleDefensives()
        {
            if (!GetCheckBox("Use Defensives")) return false;
            int hpPct = GetPlayerHealthPct();
            if (hpPct <= GetSlider("Fortifying Brew HP %") && Inferno.CanCast("Fortifying Brew", IgnoreGCD: true))
            { Inferno.Cast("Fortifying Brew", QuickDelay: true); return true; }
            if (hpPct <= GetSlider("Celestial Brew HP %") && !HasBuff(CBName()) && Inferno.CanCast(CBName(), IgnoreGCD: true))
            { Inferno.Cast(CBName(), QuickDelay: true); return true; }
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
            if ((elapsed * 100 / total) >= _interruptTargetPct && Inferno.CanCast("Spear Hand Strike", IgnoreGCD: true))
            { Inferno.Cast("Spear Hand Strike", QuickDelay: true); _lastCastingID = 0; return true; }
            return false;
        }

        int GCD() { return Inferno.GCD(); }
        bool HasBuff(string n) { return Inferno.BuffRemaining(n) > GCD(); }
        int GetStaggerLevel()
        {
            if (Inferno.DebuffRemaining("Heavy Stagger", "player", false) > 0) return 80;
            if (Inferno.DebuffRemaining("Moderate Stagger", "player", false) > 0) return 50;
            if (Inferno.DebuffRemaining("Light Stagger", "player", false) > 0) return 20;
            return 0;
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
        int GetPlayerHealthPct() { int hp = Inferno.Health("player"); int m = Inferno.MaxHealth("player"); if (m < 1) m = 1; return (hp * 100) / m; }
        bool Cast(string n) { if (Inferno.CanCast(n)) { Inferno.Cast(n); Inferno.PrintMessage(">> " + n, Color.White); return true; } return false; }
        // Keg Smash has a tiny range that fails CanCast range check — bypass it
        bool CastKS() { if (Inferno.CanCast("Keg Smash", CheckRange: false)) { Inferno.Cast("Keg Smash"); Inferno.PrintMessage(">> Keg Smash", Color.White); return true; } return false; }
        // Celestial Infusion replaces Celestial Brew (alternate talent choice)
        string _cbName = null;
        string CBName() { if (_cbName == null) _cbName = Inferno.IsSpellKnown("Celestial Infusion") ? "Celestial Infusion" : "Celestial Brew"; return _cbName; }
        bool CastCB() { return Cast(CBName()); }
        float CBChargesFrac() { return Inferno.ChargesFractional(CBName(), 60000); }
        int CBCD() { return Inferno.SpellCooldown(CBName()); }
    }
}
