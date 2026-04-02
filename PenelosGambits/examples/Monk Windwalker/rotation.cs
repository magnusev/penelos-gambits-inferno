using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API;

namespace InfernoWow.Modules
{
    /// <summary>
    /// Windwalker Monk - Translated from SimulationCraft Midnight APL
    /// Hero trees: Celestial Conduit / Flurry Strikes (auto-detected via Zenith).
    /// Core: Combo Strike mastery (never repeat same ability), Chi management,
    /// Zenith+Xuen windows, Fists of Fury, Rising Sun Kick priority.
    /// </summary>
    public class WindwalkerMonkRotation : Rotation
    {
        List<string> Abilities = new List<string> {
            "Tiger Palm", "Blackout Kick", "Rising Sun Kick",
            "Fists of Fury", "Spinning Crane Kick", "Whirling Dragon Punch",
            "Strike of the Windlord", "Rushing Wind Kick", "Slicing Winds",
            "Touch of Death", "Celestial Conduit", "Zenith",
            "Invoke Xuen, the White Tiger", "Flying Serpent Kick",
        };
        List<string> TalentChecks = new List<string> {
            "Celestial Conduit", "Flurry Strikes", "Sequenced Strikes",
            "Shadowboxing Treads", "Crane Vortex", "Inner Peace",
            "Energy Burst", "Obsidian Spiral", "Dance of Chi-Ji",
            "Storm Unleashed", "Invoke Xuen, the White Tiger",
            "Efficient Training", "Spiritual Focus",
        };
        List<string> DefensiveSpells = new List<string> { "Touch of Karma", "Diffuse Magic", "Fortifying Brew" };
        List<string> UtilitySpells = new List<string> { "Spear Hand Strike" };

        const int HealthstoneItemID = 5512;
        const int ChiPowerType = 12;
        const int EnergyPowerType = 3;
        private Random _rng = new Random();
        private int _lastCastingID = 0;
        private int _interruptTargetPct = 0;

        public override void LoadSettings()
        {
            Settings.Add(new Setting("=== Windwalker Monk ==="));
            Settings.Add(new Setting("Use Invoke Xuen", true));
            Settings.Add(new Setting("Use Zenith", true));
            Settings.Add(new Setting("Use Touch of Death", true));
            Settings.Add(new Setting("Use Trinkets", true));
            Settings.Add(new Setting("=== Defensives ==="));
            Settings.Add(new Setting("Use Defensives", true));
            Settings.Add(new Setting("Touch of Karma HP %", 1, 100, 50));
            Settings.Add(new Setting("Fortifying Brew HP %", 1, 100, 35));
            Settings.Add(new Setting("Healthstone HP %", 1, 100, 50));
            Settings.Add(new Setting("=== Interrupt ==="));
            Settings.Add(new Setting("Auto Interrupt", true));
            Settings.Add(new Setting("Interrupt at cast % (min)", 0, 100, 40));
            Settings.Add(new Setting("Interrupt at cast % (max)", 0, 100, 90));
        }

        public override void Initialize()
        {
            Inferno.PrintMessage("             //////////////////////////////////////", Color.MediumSpringGreen);
            Inferno.PrintMessage("             //   WINDWALKER - MONK (MID)        //", Color.MediumSpringGreen);
            Inferno.PrintMessage("             //              V 1.00              //", Color.MediumSpringGreen);
            Inferno.PrintMessage("             //////////////////////////////////////", Color.MediumSpringGreen);
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

            // Don't interrupt Fists of Fury
            if (Inferno.IsChanneling("player")) return false;

            // Don't interrupt cast-time trinkets
            string _castName = Inferno.CastingName("player");
            if (_castName.Contains("Puzzle Box") || _castName.Contains("Emberwing")) return false;

            if (!Inferno.UnitCanAttack("player", "target")) return false;
                        if (HandleTrinkets()) return true;

            // Racial damage CDs
            if ((HasBuff("Storm, Earth, and Fire") || HasBuff("Serenity")) && HandleRacials()) return true;

            int enemies = Inferno.EnemiesNearUnit(8f, "player");
            if (enemies < 1) enemies = 1; if ((Inferno.IsCustomCodeOn("ForceST") || Inferno.IsCustomCodeOn("forcest"))) enemies = 1;
            int chi = GetChi();

            // Cooldowns (Xuen + Zenith)
            if (HandleCooldowns(enemies)) return true;

            if (enemies > 1) return MultiTarget(enemies, chi);
            return SingleTarget(enemies, chi);
        }

        bool HandleCooldowns(int enemies)
        {
            if ((Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;
            bool hasCelestialConduit = Inferno.IsSpellKnown("Celestial Conduit");
            bool zenithUp = HasBuff("Zenith");
            bool hotJSUp = HasBuff("Heart of the Jade Serpent");
            bool hotJSUnity = HasBuff("Heart of the Jade Serpent: Unity Within");
            int chi = GetChi();

            // Invoke Xuen — sync with Zenith readiness (big_coc: zenith.up or zenith CD ready)
            if (GetCheckBox("Use Invoke Xuen"))
            {
                if (hasCelestialConduit)
                {
                    if ((Inferno.SpellCooldown("Zenith") == 0 || (zenithUp && Inferno.BuffRemaining("Zenith") > 13000)) && !hotJSUp)
                        if (CastCombo("Invoke Xuen, the White Tiger")) return true;
                }
                else
                    if (CastCombo("Invoke Xuen, the White Tiger")) return true;
            }

            // Zenith (actions.zenith — simplified from APL)
            if (GetCheckBox("Use Zenith"))
            {
                bool rskOnCD = Inferno.SpellCooldown("Rising Sun Kick") > 0;
                bool rskOrAoE = rskOnCD || enemies > 2;
                bool hasFlurry = Inferno.IsSpellKnown("Flurry Strikes");
                int zenithFRT = Inferno.FullRechargeTime("Zenith", ZenithRechargeMs());

                // zenith,if=buff.invoke_xuen.up&(!buff.zenith.up|talent.flurry_strikes)
                if (HasBuff("Invoke Xuen, the White Tiger") && (!zenithUp || hasFlurry))
                    if (CastCombo("Zenith")) return true;

                // zenith,if=buff.bloodlust.remains>10&(enemies>2|rsk_on_cd)&!buff.zenith.up
                if (HasLust() && rskOrAoE && !zenithUp)
                    if (CastCombo("Zenith")) return true;

                // zenith,if=flurry_strikes&about_to_cap_charges
                if (hasFlurry && zenithFRT < 5000 && rskOrAoE)
                    if (CastCombo("Zenith")) return true;

                // zenith,if=flurry_strikes&charges_pooling&(rsk_on_cd|aoe)
                if (hasFlurry && zenithFRT < 20000 && rskOrAoE && !zenithUp)
                    if (CastCombo("Zenith")) return true;

                // zenith,if=celestial_conduit&xuen_far_away&(rsk_on_cd|aoe)
                if (hasCelestialConduit && Inferno.SpellCooldown("Invoke Xuen, the White Tiger") > zenithFRT && rskOrAoE && !zenithUp)
                    if (CastCombo("Zenith")) return true;
            }

            // Celestial Conduit (big_coc timing)
            if (hasCelestialConduit && zenithUp && chi > 1)
            {
                // Best: during Zenith when HotJS is down or about to expire, and major CDs on cooldown
                bool hotJSExpiring = hotJSUp && Inferno.BuffRemaining("Heart of the Jade Serpent") < 4000;
                if ((!hotJSUp && !hotJSUnity) || hotJSExpiring)
                {
                    if (Inferno.SpellCooldown("Rising Sun Kick") > 0 || enemies > 2)
                        if (CastCombo("Celestial Conduit")) return true;
                }
                // Also fire during Zenith with <12s remaining
                if (Inferno.BuffRemaining("Zenith") < 12000)
                    if (CastCombo("Celestial Conduit")) return true;
            }
            return false;
        }

        // =====================================================================
        // SINGLE TARGET (actions.default_st)
        // =====================================================================
        bool SingleTarget(int enemies, int chi)
        {
            // whirling_dragon_punch
            if (!HasBuff("Heart of the Jade Serpent: Unity Within"))
                if (CastCombo("Whirling Dragon Punch")) return true;
            // fists_of_fury with Heart buffs or flurry charges
            if (HasBuff("Heart of the Jade Serpent") || HasBuff("Heart of the Jade Serpent: Unity Within") || Inferno.BuffStacks("Flurry Charge") >= 30)
                if (CastCombo("Fists of Fury")) return true;
            // strike_of_the_windlord
            if (CastCombo("Strike of the Windlord")) return true;
            // fists_of_fury general
            if (CastCombo("Fists of Fury")) return true;
            // rushing_wind_kick
            if (HasBuff("Rushing Wind Kick") && !Inferno.HasBuff("Combo Strikes: Rushing Wind Kick") && CastRWK()) return true;
            // rising_sun_kick
            if (CastCombo("Rising Sun Kick")) return true;
            // spinning_crane_kick with Dance of Chi-Ji
            if (HasBuff("Dance of Chi-Ji") && Inferno.BuffStacks("Combo Breaker") < 2)
                if (CastCombo("Spinning Crane Kick")) return true;
            // touch_of_death — APL places it here, after core abilities
            if (GetCheckBox("Use Touch of Death") && !HasBuff("Zenith") && Inferno.Health("target") < Inferno.Health("player"))
                if (CastCombo("Touch of Death")) return true;
            // blackout_kick with Combo Breaker
            if (HasBuff("Combo Breaker") && CastCombo("Blackout Kick")) return true;
            // slicing_winds
            if (CastCombo("Slicing Winds")) return true;
            // tiger_palm for chi
            if (chi < 4 && GetEnergy() > 55 && CastCombo("Tiger Palm")) return true;
            // blackout_kick
            if (chi > 1 && CastCombo("Blackout Kick")) return true;
            // tiger_palm filler
            if (chi < 5 && CastCombo("Tiger Palm")) return true;
            return false;
        }

        // =====================================================================
        // MULTITARGET (actions.multitarget)
        // =====================================================================
        bool MultiTarget(int enemies, int chi)
        {
            bool zenithUp = HasBuff("Zenith");
            bool hotJSUp = HasBuff("Heart of the Jade Serpent");
            bool hotJSUnity = HasBuff("Heart of the Jade Serpent: Unity Within");

            // fists_of_fury,if=buff.heart_of_the_jade_serpent.remains<1&buff.heart_of_the_jade_serpent.up
            if (hotJSUp && Inferno.BuffRemaining("Heart of the Jade Serpent") < 1000)
                if (CastCombo("Fists of Fury")) return true;
            // whirling_dragon_punch
            if (!hotJSUnity)
                if (CastCombo("Whirling Dragon Punch")) return true;
            // fists_of_fury (flurry charges, HotJS, etc.)
            if (Inferno.BuffStacks("Flurry Charge") >= 30 && !zenithUp || hotJSUp || hotJSUnity || Inferno.IsSpellKnown("Flurry Strikes"))
                if (CastCombo("Fists of Fury")) return true;
            // spinning_crane_kick with Dance of Chi-Ji
            if (HasBuff("Dance of Chi-Ji") && Inferno.BuffStacks("Combo Breaker") < 2 && Inferno.BuffRemaining("Dance of Chi-Ji") < 3000)
                if (CastCombo("Spinning Crane Kick")) return true;
            // rushing_wind_kick
            if (HasBuff("Rushing Wind Kick") && !Inferno.HasBuff("Combo Strikes: Rushing Wind Kick") && CastRWK()) return true;
            // rising_sun_kick
            if ((enemies < 5 || Inferno.SpellCooldown("Fists of Fury") > 0 || zenithUp) && (HasBuff("Rushing Wind Kick") || hotJSUp || hotJSUnity))
                if (CastCombo("Rising Sun Kick")) return true;
            // touch_of_death — APL places it here, after core abilities
            if (GetCheckBox("Use Touch of Death") && !zenithUp && Inferno.Health("target") < Inferno.Health("player"))
                if (CastCombo("Touch of Death")) return true;
            // strike_of_the_windlord
            if (zenithUp || Inferno.SpellCooldown("Zenith") > 5000)
                if (CastCombo("Strike of the Windlord")) return true;
            // whirling_dragon_punch (second entry)
            if (zenithUp || Inferno.SpellCooldown("Zenith") > 5000)
                if (CastCombo("Whirling Dragon Punch")) return true;
            // fists_of_fury general
            if (Inferno.IsSpellKnown("Flurry Strikes") || !zenithUp)
                if (CastCombo("Fists of Fury")) return true;
            // rising_sun_kick
            if ((enemies < 5 || Inferno.SpellCooldown("Fists of Fury") > 4000 || zenithUp))
                if (CastCombo("Rising Sun Kick")) return true;
            // blackout_kick with zenith and combo breaker
            if (zenithUp && chi > 1 && HasBuff("Combo Breaker"))
                if (CastCombo("Blackout Kick")) return true;
            // spinning_crane_kick with Dance of Chi-Ji
            if (HasBuff("Dance of Chi-Ji") && Inferno.BuffStacks("Combo Breaker") < 2)
                if (CastCombo("Spinning Crane Kick")) return true;
            // slicing_winds
            if (CastCombo("Slicing Winds")) return true;
            // spinning_crane_kick zenith
            if (Inferno.IsSpellKnown("Flurry Strikes") && zenithUp && chi > 3)
                if (CastCombo("Spinning Crane Kick")) return true;
            // blackout_kick with Combo Breaker + HotJS
            if (HasBuff("Combo Breaker") && (hotJSUp || hotJSUnity))
                if (CastCombo("Blackout Kick")) return true;
            // tiger_palm for chi
            if (chi < 5 && GetEnergy() > 55 && !zenithUp)
                if (CastCombo("Tiger Palm")) return true;
            // blackout_kick with Combo Breaker
            if (HasBuff("Combo Breaker"))
                if (CastCombo("Blackout Kick")) return true;
            // spinning_crane_kick for AoE
            if (enemies > 2 && chi > 2)
                if (CastCombo("Spinning Crane Kick")) return true;
            // rising_sun_kick fallback
            if (CastCombo("Rising Sun Kick")) return true;
            // blackout_kick fallback
            if (CastCombo("Blackout Kick")) return true;
            // tiger_palm
            if (CastCombo("Tiger Palm")) return true;
            return false;
        }

        // =====================================================================
        // DEFENSIVES / INTERRUPT / TRINKETS
        // =====================================================================
        bool HandleDefensives()
        {
            if (!GetCheckBox("Use Defensives")) return false;
            int hpPct = GetPlayerHealthPct();
            if (hpPct <= GetSlider("Touch of Karma HP %") && Inferno.CanCast("Touch of Karma", IgnoreGCD: true))
            { Inferno.Cast("Touch of Karma", QuickDelay: true); return true; }
            if (hpPct <= 50 && !HasBuff("Diffuse Magic") && Inferno.CanCast("Diffuse Magic", IgnoreGCD: true))
            { Inferno.Cast("Diffuse Magic", QuickDelay: true); return true; }
            if (hpPct <= GetSlider("Fortifying Brew HP %") && Inferno.CanCast("Fortifying Brew", IgnoreGCD: true))
            { Inferno.Cast("Fortifying Brew", QuickDelay: true); return true; }
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
            if (castPct >= _interruptTargetPct && Inferno.CanCast("Spear Hand Strike", IgnoreGCD: true))
            { Inferno.Cast("Spear Hand Strike", QuickDelay: true); _lastCastingID = 0; return true; }
            return false;
        }

        bool HandleTrinkets()
        {
            if (!GetCheckBox("Use Trinkets") || (Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;
            if (!HasBuff("Zenith") && !HasBuff("Invoke Xuen, the White Tiger")) return false;
            if (Inferno.CanUseEquippedItem(13)) { Inferno.Cast("trinket1"); return true; }
            if (Inferno.CanUseEquippedItem(14)) { Inferno.Cast("trinket2"); return true; }
            return false;
        }

        // =====================================================================
        // HELPERS - Combo Strike tracking
        // =====================================================================
        int GCD() { return Inferno.GCD(); }
        int GetChi() { return Inferno.Power("player", 12); }
        int GetEnergy() { return Inferno.Power("player", 3); }
        bool HasBuff(string name) { return Inferno.BuffRemaining(name) > GCD(); }

        // Zenith base recharge: 90s - 10s (Efficient Training) - 20s (Spiritual Focus)
        int ZenithRechargeMs()
        {
            int ms = 90000;
            if (Inferno.IsSpellKnown("Efficient Training")) ms -= 10000;
            if (Inferno.IsSpellKnown("Spiritual Focus")) ms -= 20000;
            return ms;
        }

        // Bloodlust and all variants
        bool HasLust()
        {
            return HasBuff("Bloodlust") || HasBuff("Heroism")
                || HasBuff("Time Warp") || HasBuff("Fury of the Aspects")
                || HasBuff("Primal Rage") || HasBuff("Ancient Hysteria")
                || HasBuff("Netherwinds") || HasBuff("Drums of Deathly Ferocity");
        }

        /// <summary>Casts if CanCast and ability is a combo strike (different from last used).</summary>
        bool CastCombo(string name)
        {
            // Combo Strikes mastery: game provides "Combo Strikes: X" buff for last ability used
            if (Inferno.HasBuff("Combo Strikes: " + name)) return false;
            if (Inferno.CanCast(name)) { Inferno.Cast(name); Inferno.PrintMessage(">> " + name, Color.White); return true; }
            return false;
        }

        // Rushing Wind Kick: CanCast is bugged, cast Rising Sun Kick instead
        bool CastRWK()
        {
            if (Inferno.CanCast("Rising Sun Kick")) { Inferno.Cast("Rising Sun Kick"); Inferno.PrintMessage(">> Rushing Wind Kick", Color.White); return true; }
            return false;
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
