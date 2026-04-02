using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API;

namespace InfernoWow.Modules
{
    /// <summary>
    /// Havoc Demon Hunter - Translated from SimulationCraft Midnight APL
    /// Handles Metamorphosis (meta) vs non-meta priority, Reaver's Glaive cycle,
    /// Inertia/Initiative talent paths, Essence Break windows.
    /// </summary>
    public class HavocDemonHunterRotation : Rotation
    {
        List<string> Abilities = new List<string> {
            "Annihilation", "Blade Dance", "Chaos Strike", "Death Sweep",
            "Essence Break", "Eye Beam", "Felblade",
            "Immolation Aura", "Metamorphosis", "Reaver's Glaive",
            "The Hunt", "Throw Glaive",
            "Demon's Bite",
        };
        List<string> TalentChecks = new List<string> {
            "Inertia", "Initiative", "First Blood", "Trail of Ruin",
            "Screaming Brutality", "Burning Wound", "Soulscar",
            "Furious Throws", "A Fire Inside", "Blind Fury",
            "Demonic", "Cycle of Hatred", "Ragefire", "Glaive Tempest",
            "Eternal Hunt", "Chaotic Transformation", "Demonic Intensity",
            "Demon Blades", "Burning Blades", "Relentless Onslaught",
            "Chaos Theory",
        };
        List<string> DefensiveSpells = new List<string> { "Blur", "Darkness" };
        List<string> UtilitySpells = new List<string> { "Disrupt", "Consume Magic" };

        const int HealthstoneItemID = 5512;
        const int FuryPowerType = 17;
        private Random _rng = new Random();
        private int _lastCastingID = 0;
        private int _interruptTargetPct = 0;

        public override void LoadSettings()
        {
            Settings.Add(new Setting("=== Havoc Demon Hunter ==="));
            Settings.Add(new Setting("=== Offensive Cooldowns ==="));
            Settings.Add(new Setting("Use Metamorphosis", true));
            Settings.Add(new Setting("Use The Hunt", true));
            Settings.Add(new Setting("Use Eye Beam", true));
            Settings.Add(new Setting("Use Essence Break", true));
            Settings.Add(new Setting("Use Trinkets", true));
            Settings.Add(new Setting("=== Defensives ==="));
            Settings.Add(new Setting("Use Defensives", true));
            Settings.Add(new Setting("Blur HP %", 1, 100, 50));
            Settings.Add(new Setting("Healthstone HP %", 1, 100, 50));
            Settings.Add(new Setting("=== Interrupt ==="));
            Settings.Add(new Setting("Auto Interrupt", true));
            Settings.Add(new Setting("Interrupt at cast % (min)", 0, 100, 40));
            Settings.Add(new Setting("Interrupt at cast % (max)", 0, 100, 90));
        }

        public override void Initialize()
        {
            Inferno.PrintMessage("             //////////////////////////////////////", Color.DarkGreen);
            Inferno.PrintMessage("             //     HAVOC - DEMON HUNTER (MID)   //", Color.DarkGreen);
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

            // disrupt (APL: actions+=/disrupt)
            if (HandleInterrupt()) return true;

            // Don't interrupt Eye Beam channel
            if (Inferno.IsChanneling("player")) return false;

            // Don't interrupt cast-time trinkets
            string _castName = Inferno.CastingName("player");
            if (_castName.Contains("Puzzle Box") || _castName.Contains("Emberwing")) return false;

            if (!Inferno.UnitCanAttack("player", "target")) return false;

            int enemies = Inferno.EnemiesNearUnit(8f, "player");
            if (enemies < 1) enemies = 1; if ((Inferno.IsCustomCodeOn("ForceST") || Inferno.IsCustomCodeOn("forcest"))) enemies = 1;
            bool inMeta = HasBuff("Metamorphosis");

            // Cooldowns
            if (HandleCooldowns(enemies, inMeta)) return true;
                        if (HandleTrinkets()) return true;

            // Racial damage CDs
            if ((HasBuff("Metamorphosis")) && HandleRacials()) return true;

            // Immolation Aura - prevent charge capping for A Fire Inside
            if (Inferno.IsSpellKnown("A Fire Inside") && Inferno.ChargesFractional("Immolation Aura", 30000) >= 1.9f && !HasDebuff("Essence Break"))
                if (Cast("Immolation Aura")) return true;

            // Immolation Aura in AoE with Ragefire
            if (enemies > 2 && Inferno.IsSpellKnown("Ragefire") && !HasDebuff("Essence Break"))
                if (Cast("Immolation Aura")) return true;

            if (Inferno.IsSpellKnown("Inertia") && !HasBuff("Inertia Trigger")
                && Inferno.SpellCooldown("Metamorphosis") >= 5000)

            // APL: run_action_list,name=meta,if=buff.metamorphosis.up
            if (inMeta) return MetaRotation(enemies);

            // Felblade to consume inertia trigger
            if (Inferno.IsSpellKnown("Inertia") && HasBuff("Inertia Trigger") && enemies <= 2)
                if (Cast("Felblade")) return true;

            // Eye Beam — APL: use when BD coming off CD within 7s or not using BD, !inner_demon
            if (GetCheckBox("Use Eye Beam") && !HasBuff("Inner Demon")
                && (Inferno.SpellCooldown("Blade Dance") < 7000 || !UseBladesDance()))
            {
                if (Cast("Eye Beam")) return true;
            }

            // Essence Break - outside meta
            if (GetCheckBox("Use Essence Break") && GetFury() >= 35 && !HasBuff("Inertia Trigger")
                && Inferno.SpellCooldown("Eye Beam") > 5000)
                if (Cast("Essence Break")) return true;

            // Chaos Strike priority with Reaver's Mark on target
            if (HasDebuff("Reaver's Mark") && (HasBuff("Rending Strike") || HasBuff("Glaive Flurry")))
                if (Cast("Chaos Strike")) return true;

            // Blade Dance
            if (UseBladesDance() && !HasDebuff("Essence Break"))
                if (Cast("Blade Dance")) return true;

            // Chaos Strike during Essence Break
            if (HasDebuff("Essence Break") && Cast("Chaos Strike")) return true;

            // Reaver's Glaive - when no buff cycle active and <3 targets
            if (!HasBuff("Glaive Flurry") && !HasBuff("Rending Strike") && enemies < 3 && !HasDebuff("Essence Break"))
                if (Cast("Reaver's Glaive")) return true;

            // Reaver's Glaive in AoE
            if (!HasBuff("Glaive Flurry") && !HasBuff("Rending Strike") && enemies >= 2)
                if (Cast("Reaver's Glaive")) return true;

            // Felblade for fury
            if (!HasBuff("Inertia Trigger") && GetFuryDeficit() >= 15)
                if (Cast("Felblade")) return true;

            // Immolation Aura
            if (GetFuryDeficit() > 20 && Cast("Immolation Aura")) return true;

            // Throw Glaive with Soulscar
            if (Inferno.IsSpellKnown("Soulscar") && !HasDebuff("Essence Break"))
                if (Cast("Throw Glaive")) return true;

            // Chaos Strike as main spender
            if (GetFury() >= 40 && Cast("Chaos Strike")) return true;

            // Demon's Bite (if not Demon Blades)
            if (!Inferno.IsSpellKnown("Demon Blades") && Cast("Demon's Bite")) return true;


            // Throw Glaive filler
            if (Cast("Throw Glaive")) return true;

            return false;
        }

        // =====================================================================
        // META ROTATION (actions.meta)
        // =====================================================================
        bool MetaRotation(int enemies)
        {
            // death_sweep,if=buff.metamorphosis.remains<gcd|debuff.essence_break.up
            if ((Inferno.BuffRemaining("Metamorphosis") < GCD() || HasDebuff("Essence Break")) && Cast("Death Sweep")) return true;

            // annihilation,if=buff.metamorphosis.remains<gcd|debuff.essence_break.up
            if ((Inferno.BuffRemaining("Metamorphosis") < GCD() || HasDebuff("Essence Break")) && Cast("Annihilation")) return true;

            // essence_break in meta
            if (GetCheckBox("Use Essence Break") && GetFury() >= 35 && Inferno.SpellCooldown("Eye Beam") > 5000 && Inferno.SpellCooldown("Metamorphosis") > 5000)
                if (Cast("Essence Break")) return true;

            // eye_beam in meta
            if (GetCheckBox("Use Eye Beam") && !HasDebuff("Essence Break") && !HasBuff("Inner Demon"))
                if (Cast("Eye Beam")) return true;

            // death_sweep
            if (UseBladesDance() && Cast("Death Sweep")) return true;

            // Throw Glaive with Soulscar
            if (Inferno.IsSpellKnown("Soulscar") && !HasDebuff("Essence Break") && Cast("Throw Glaive")) return true;

            // annihilation - main spender in meta
            if ((GetFury() >= 40 || HasBuff("Inertia")) && Cast("Annihilation")) return true;

            // felblade in meta
            if (!HasBuff("Inertia Trigger") && GetFuryDeficit() > 15 && Inferno.BuffRemaining("Metamorphosis") > 5000)
                if (Cast("Felblade")) return true;

            // immolation_aura
            if (Cast("Immolation Aura")) return true;

            // felblade low fury
            if (!HasBuff("Inertia Trigger") && GetFury() < 35 && Cast("Felblade")) return true;

            // fel_rush filler
            if (!HasBuff("Inertia Trigger") && !HasDebuff("Essence Break") && Inferno.BuffRemaining("Metamorphosis") > 5000)

            // throw_glaive filler
            if (!HasDebuff("Essence Break") && Inferno.BuffRemaining("Metamorphosis") > 5000)
                if (Cast("Throw Glaive")) return true;

            return false;
        }

        // =====================================================================
        // COOLDOWNS (actions.cooldown)
        // =====================================================================
        bool HandleCooldowns(int enemies, bool inMeta)
        {
            if ((Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;

            // the_hunt,if=debuff.essence_break.down&!buff.reavers_glaive.up
            if (GetCheckBox("Use The Hunt") && !HasDebuff("Essence Break") && !HasBuff("Reaver's Glaive"))
                if (Cast("The Hunt")) return true;

            // metamorphosis - complex conditions simplified:
            // use when Eye Beam CD > 10 or cycle of hatred alignment, not during Inner Demon
            if (GetCheckBox("Use Metamorphosis") && !inMeta && !HasBuff("Inner Demon")
                && (Inferno.SpellCooldown("Eye Beam") >= 10000 || !Inferno.IsSpellKnown("Chaotic Transformation")))
                if (Cast("Metamorphosis")) return true;

            return false;
        }

        // =====================================================================
        // DEFENSIVES / INTERRUPT / TRINKETS
        // =====================================================================
        bool HandleDefensives()
        {
            if (!GetCheckBox("Use Defensives")) return false;
            int hpPct = GetPlayerHealthPct();
            if (hpPct <= GetSlider("Blur HP %") && Inferno.CanCast("Blur", IgnoreGCD: true))
            { Inferno.Cast("Blur", QuickDelay: true); return true; }
            if (hpPct <= 30 && !HasBuff("Darkness") && Inferno.CanCast("Darkness", IgnoreGCD: true))
            { Inferno.Cast("Darkness", QuickDelay: true); return true; }
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
            if (castPct >= _interruptTargetPct && Inferno.CanCast("Disrupt", IgnoreGCD: true))
            { Inferno.Cast("Disrupt", QuickDelay: true); _lastCastingID = 0; return true; }
            return false;
        }

        bool HandleTrinkets()
        {
            if (!GetCheckBox("Use Trinkets") || (Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;
            // Use during Eye Beam CD or Metamorphosis
            if (Inferno.SpellCooldown("Eye Beam") > 0 || !HasBuff("Metamorphosis")) return false;
            if (Inferno.CanUseEquippedItem(13)) { Inferno.Cast("trinket1"); return true; }
            if (Inferno.CanUseEquippedItem(14)) { Inferno.Cast("trinket2"); return true; }
            return false;
        }

        // =====================================================================
        // HELPERS
        // =====================================================================
        int GCD() { return Inferno.GCD(); }
        int GetFury() { return Inferno.Power("player", FuryPowerType); }
        int GetFuryDeficit() { return Inferno.MaxPower("player", FuryPowerType) - GetFury(); }
        bool HasBuff(string name) { return Inferno.BuffRemaining(name) > GCD(); }
        bool HasDebuff(string name) { return Inferno.DebuffRemaining(name) > GCD(); }

        // variable,name=use_blade_dance: 3+ targets (2+ with Trail of Ruin), always with First Blood or SB
        bool UseBladesDance()
        {
            int enemies = Inferno.EnemiesNearUnit(8f, "player");
            if ((Inferno.IsCustomCodeOn("ForceST") || Inferno.IsCustomCodeOn("forcest"))) enemies = 1;
            return enemies >= (3 - (Inferno.IsSpellKnown("Trail of Ruin") ? 1 : 0))
                || Inferno.IsSpellKnown("First Blood")
                || Inferno.IsSpellKnown("Screaming Brutality");
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

        bool Cast(string name)
        {
            if (Inferno.CanCast(name)) { Inferno.Cast(name); Inferno.PrintMessage(">> " + name, Color.White); return true; }
            return false;
        }
    }
}
