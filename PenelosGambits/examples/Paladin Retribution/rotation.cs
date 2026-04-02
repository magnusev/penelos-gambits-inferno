using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API;

namespace InfernoWow.Modules
{
    /// <summary>
    /// Retribution Paladin - Translated from SimulationCraft Midnight APL
    /// Generator → Finisher priority with Holy Power management.
    /// Hammer of Light, Wake of Ashes, Divine Toll integration.
    /// </summary>
    public class RetributionPaladinRotation : Rotation
    {
        List<string> Abilities = new List<string> {
            "Templar's Verdict", "Divine Storm", "Hammer of Light",
            "Blade of Justice", "Judgment", "Crusader Strike",
            "Hammer of Wrath", "Wake of Ashes", "Divine Toll",
            "Execution Sentence", "Avenging Wrath",
            "Templar Strike", "Templar Slash",
        };
        List<string> TalentChecks = new List<string> {
            "Radiant Glory", "Holy Flames", "Execution Sentence",
            "Light's Guidance", "Walk Into Light", "Empyrean Power",
            "Art of War", "Righteous Cause",
        };
        List<string> DefensiveSpells = new List<string> {
            "Divine Shield", "Lay on Hands", "Word of Glory",
            "Shield of Vengeance",
        };
        List<string> UtilitySpells = new List<string> { "Rebuke", "Retribution Aura" };

        const int HealthstoneItemID = 5512;
        const int HolyPowerType = 9;
        private Random _rng = new Random();
        private int _lastCastingID = 0;
        private int _interruptTargetPct = 0;

        public override void LoadSettings()
        {
            Settings.Add(new Setting("=== Retribution Paladin ==="));
            Settings.Add(new Setting("=== Offensive Cooldowns ==="));
            Settings.Add(new Setting("Use Avenging Wrath", true));
            Settings.Add(new Setting("Use Wake of Ashes", true));
            Settings.Add(new Setting("Use Execution Sentence", true));
            Settings.Add(new Setting("Use Trinkets", true));
            Settings.Add(new Setting("=== Defensives ==="));
            Settings.Add(new Setting("Use Defensives", true));
            Settings.Add(new Setting("Divine Shield HP %", 1, 100, 15));
            Settings.Add(new Setting("Lay on Hands HP %", 1, 100, 20));
            Settings.Add(new Setting("Word of Glory HP %", 1, 100, 50));
            Settings.Add(new Setting("Shield of Vengeance HP %", 1, 100, 70));
            Settings.Add(new Setting("Healthstone HP %", 1, 100, 50));
            Settings.Add(new Setting("=== Interrupt ==="));
            Settings.Add(new Setting("Auto Interrupt", true));
            Settings.Add(new Setting("Interrupt at cast % (min)", 0, 100, 40));
            Settings.Add(new Setting("Interrupt at cast % (max)", 0, 100, 90));
        }

        public override void Initialize()
        {
            Inferno.PrintMessage("             //////////////////////////////////////", Color.Gold);
            Inferno.PrintMessage("             //   RETRIBUTION - PALADIN (MID)    //", Color.Gold);
            Inferno.PrintMessage("             //              V 1.00              //", Color.Gold);
            Inferno.PrintMessage("             //////////////////////////////////////", Color.Gold);
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
            CustomCommands.Add("NoCDs");
            CustomCommands.Add("ForceST");
        }

        public override bool OutOfCombatTick()
        {
            if (!Inferno.HasBuff("Retribution Aura") && Inferno.CanCast("Retribution Aura"))
            { Inferno.Cast("Retribution Aura"); return true; }
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

            // APL: call_action_list,name=cooldowns
            if (HandleCooldowns()) return true;

            // APL: call_action_list,name=generators (which calls finishers internally)
            return Generators();
        }

        // =====================================================================
        // COOLDOWNS (actions.cooldowns)
        // =====================================================================
        bool HandleCooldowns()
        {
            if (Inferno.IsCustomCodeOn("NoCDs")) return false;

            // Trinkets during Avenging Wrath
            if (GetCheckBox("Use Trinkets") && HasBuff("Avenging Wrath"))
            {
                if (Inferno.CanUseEquippedItem(13)) { Inferno.Cast("trinket1"); return true; }
                if (Inferno.CanUseEquippedItem(14)) { Inferno.Cast("trinket2"); return true; }
            }

            // execution_sentence,if=cooldown.wake_of_ashes.remains<gcd&(!talent.holy_flames|dot.expurgation.ticking)
            if (GetCheckBox("Use Execution Sentence") && Inferno.SpellCooldown("Wake of Ashes") < GCD()
                && (!Inferno.IsSpellKnown("Holy Flames") || Inferno.DebuffRemaining("Expurgation") > GCD()))
                if (Cast("Execution Sentence")) return true;

            // avenging_wrath,if=(!talent.holy_flames|dot.expurgation.ticking)&(!talent.lights_guidance|debuff.judgment.up|time>5)
            if (GetCheckBox("Use Avenging Wrath") && !Inferno.IsSpellKnown("Radiant Glory")
                && (!Inferno.IsSpellKnown("Holy Flames") || Inferno.DebuffRemaining("Expurgation") > GCD())
                && (!Inferno.IsSpellKnown("Light's Guidance") || Inferno.DebuffRemaining("Judgment") > GCD()))
                if (CastCD("Avenging Wrath")) return true;

            return false;
        }

        // =====================================================================
        // FINISHERS (actions.finishers)
        // =====================================================================
        bool Finishers()
        {
            int hp = GetHolyPower();
            int enemies = Inferno.EnemiesNearUnit(8f, "player");
            if (enemies < 1) enemies = 1; if (Inferno.IsCustomCodeOn("ForceST")) enemies = 1;

            // variable,name=ds_castable,value=(active_enemies>=3|buff.empyrean_power.up)&!buff.empyrean_legacy.up
            bool dsCastable = (enemies >= 3 || HasBuff("Empyrean Power")) && !HasBuff("Empyrean Legacy");
            bool holUp = HasBuff("Hammer of Light");

            // hammer_of_light — Wake of Ashes becomes Hammer of Light when buff is active
            if (holUp)
            {
                if (HasBuff("Avenging Wrath") || Inferno.BuffRemaining("Hammer of Light") < GCDMAX() * 2)
                    if (Cast("Wake of Ashes")) return true;
            }

            // divine_storm,if=ds_castable&(!buff.hammer_of_light_ready.up|buff.hammer_of_light_free.up)
            if (dsCastable && !holUp)
                if (Cast("Divine Storm")) return true;

            // templars_verdict
            if (!holUp)
                if (Cast("Templar's Verdict")) return true;

            return false;
        }

        // =====================================================================
        // GENERATORS (actions.generators)
        // =====================================================================
        bool m()
        {
            int hp = GetHolyPower();
            int enemies = Inferno.EnemiesNearUnit(8f, "player");
            if (Inferno.IsCustomCodeOn("ForceST")) enemies = 1;
            if ((hp >= 5 && Inferno.SpellCooldown("Wake of Ashes") > 0) || Inferno.BuffRemaining("Hammer of Light") < GCDMAX() * 2)
                if (Finishers()) return true;

            // blade_of_justice,if=talent.holy_flames&!dot.expurgation.ticking&time<5
            if (Inferno.IsSpellKnown("Holy Flames") && Inferno.DebuffRemaining("Expurgation") < GCD() && Inferno.CombatTime() < 5000)
                if (Cast("Blade of Justice")) return true;

            // judgment,if=talent.lights_guidance&!debuff.judgment.up&time<5
            if (Inferno.IsSpellKnown("Light's Guidance") && Inferno.DebuffRemaining("Judgment") < GCD() && Inferno.CombatTime() < 5000)
                if (Cast("Judgment")) return true;

            // wake_of_ashes,if=(cooldown.avenging_wrath.remains>6|talent.radiant_glory)
            if (GetCheckBox("Use Wake of Ashes") && (Inferno.SpellCooldown("Avenging Wrath") > 6000 || Inferno.IsSpellKnown("Radiant Glory")))
                if (Cast("Wake of Ashes")) return true;

            // divine_toll
            if (Cast("Divine Toll")) return true;

            // blade_of_justice,if=(buff.art_of_war.up|buff.righteous_cause.up)&(!talent.walk_into_light|!buff.avenging_wrath.up)
            if ((HasBuff("Art of War") || HasBuff("Righteous Cause")) && (!Inferno.IsSpellKnown("Walk Into Light") || !HasBuff("Avenging Wrath")))
                if (Cast("Blade of Justice")) return true;

            // call_action_list,name=finishers (at any HP >= 3)
            if (hp >= 3)
                if (Finishers()) return true;

            // hammer_of_wrath,if=talent.walk_into_light
            if (Inferno.IsSpellKnown("Walk Into Light") && Cast("Hammer of Wrath")) return true;

            // blade_of_justice
            if (Cast("Blade of Justice")) return true;

            // hammer_of_wrath
            if (Cast("Hammer of Wrath")) return true;

            // judgment
            if (Cast("Judgment")) return true;

            // templar_strike
            if (Cast("Templar Strike")) return true;

            // templar_slash
            if (Cast("Templar Slash")) return true;

            // crusader_strike
            if (Cast("Crusader Strike")) return true;

            return false;
        }

        // =====================================================================
        // DEFENSIVES / INTERRUPT
        // =====================================================================
        bool HandleDefensives()
        {
            if (!GetCheckBox("Use Defensives")) return false;
            int hpPct = GetPlayerHealthPct();
            if (hpPct <= GetSlider("Divine Shield HP %") && Inferno.CanCast("Divine Shield", IgnoreGCD: true))
            { Inferno.Cast("Divine Shield", QuickDelay: true); return true; }
            if (hpPct <= GetSlider("Lay on Hands HP %") && Inferno.CanCast("Lay on Hands", IgnoreGCD: true))
            { Inferno.Cast("Lay on Hands", QuickDelay: true); return true; }
            if (hpPct <= GetSlider("Word of Glory HP %") && GetHolyPower() >= 3 && Inferno.CanCast("Word of Glory"))
            { Inferno.Cast("Word of Glory"); return true; }
            if (hpPct <= GetSlider("Shield of Vengeance HP %") && Inferno.CanCast("Shield of Vengeance", IgnoreGCD: true))
            { Inferno.Cast("Shield of Vengeance", QuickDelay: true); return true; }
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
            if (castPct >= _interruptTargetPct && Inferno.CanCast("Rebuke", IgnoreGCD: true))
            { Inferno.Cast("Rebuke", QuickDelay: true); _lastCastingID = 0; return true; }
            return false;
        }

        // =====================================================================
        // HELPERS
        // =====================================================================
        int GCD() { return Inferno.GCD(); }
        int GCDMAX() { int g = (int)(1500f / (1f + Inferno.Haste("player") / 100f)); return g < 750 ? 750 : g; }
        int GetHolyPower() { return Inferno.Power("player", HolyPowerType); }
        bool HasBuff(string name) { return Inferno.BuffRemaining(name) > GCD(); }

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
            if (Inferno.IsCustomCodeOn("NoCDs")) return false;
            if (Inferno.CanCast(name)) { Inferno.Cast(name); Inferno.PrintMessage(">> " + name, Color.White); return true; }
            return false;
        }
    }
}
