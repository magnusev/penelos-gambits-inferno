using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API;

namespace InfernoWow.Modules
{
    /// <summary>
    /// Elemental Shaman - Translated from SimulationCraft Midnight APL
    /// Core: Flame Shock maintenance, Stormkeeper+Ascendance windows,
    /// Master of the Elements buff management, Tempest spender.
    /// AoE: Chain Lightning + Earthquake at 3+ targets.
    /// </summary>
    public class ElementalShamanRotation : Rotation
    {
        List<string> Abilities = new List<string> {
            "Lightning Bolt", "Chain Lightning", "Lava Burst", "Earth Shock",
            "Earthquake", "Elemental Blast", "Flame Shock", "Frost Shock",
            "Tempest", "Voltaic Blaze", "Stormkeeper", "Ascendance",
            "Ancestral Swiftness", "Nature's Swiftness", "Lightning Shield",
        };
        List<string> TalentChecks = new List<string> {
            "Master of the Elements", "Inferno Arc", "Purging Flames",
            "Molten Wrath", "Call of the Ancestors", "Fusion of Elements",
            "Elemental Blast", "Flametongue Weapon",
        };
        List<string> DefensiveSpells = new List<string> { "Astral Shift", "Earth Elemental" };
        List<string> UtilitySpells = new List<string> { "Wind Shear", "Lightning Shield", "Spiritwalker's Grace", "Thunderstrike Ward" };

        const int HealthstoneItemID = 5512;
        private Random _rng = new Random();
        private int _lastCastingID = 0;
        private int _interruptTargetPct = 0;

        public override void LoadSettings()
        {
            Settings.Add(new Setting("=== Elemental Shaman ==="));
            Settings.Add(new Setting("Use Spiritwalker's Grace", true));
            Settings.Add(new Setting("Use Stormkeeper", true));
            Settings.Add(new Setting("Use Ascendance", true));
            Settings.Add(new Setting("Use Trinkets", true));
            Settings.Add(new Setting("=== Defensives ==="));
            Settings.Add(new Setting("Use Defensives", true));
            Settings.Add(new Setting("Astral Shift HP %", 1, 100, 50));
            Settings.Add(new Setting("Healthstone HP %", 1, 100, 50));
            Settings.Add(new Setting("=== Interrupt ==="));
            Settings.Add(new Setting("Auto Interrupt", true));
            Settings.Add(new Setting("Interrupt at cast % (min)", 0, 100, 40));
            Settings.Add(new Setting("Interrupt at cast % (max)", 0, 100, 90));
        }

        public override void Initialize()
        {
            Inferno.PrintMessage("             //////////////////////////////////////", Color.DodgerBlue);
            Inferno.PrintMessage("             //    ELEMENTAL - SHAMAN (MID)      //", Color.DodgerBlue);
            Inferno.PrintMessage("             //              V 1.00              //", Color.DodgerBlue);
            Inferno.PrintMessage("             //////////////////////////////////////", Color.DodgerBlue);
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
            if (Inferno.BuffRemaining("Lightning Shield") < 5000 && Inferno.CanCast("Lightning Shield"))
            { Inferno.Cast("Lightning Shield"); return true; }
            if (Inferno.BuffRemaining("Thunderstrike Ward") < 5000 && Inferno.CanCast("Thunderstrike Ward"))
            { Inferno.Cast("Thunderstrike Ward"); return true; }
            return false;
        }

        public override bool CombatTick()
        {
            if (Inferno.IsDead("player") || Inferno.IsGhost("player")) return false;
            if (HandleDefensives()) return true;
            if (HandleInterrupt()) return true;
            if (Inferno.BuffRemaining("Lightning Shield") < GCD() && Cast("Lightning Shield")) return true;
            // spiritwalkers_grace while moving
            if (GetCheckBox("Use Spiritwalker's Grace") && !(Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds")) && Inferno.IsMoving("player") && !HasBuff("Spiritwalker's Grace") && Inferno.CanCast("Spiritwalker's Grace", IgnoreGCD: true))
            { Inferno.Cast("Spiritwalker's Grace", QuickDelay: true); return true; }
            if (!Inferno.UnitCanAttack("player", "target")) return false;
            if (Inferno.IsChanneling("player")) return false;

            // Don't interrupt cast-time trinkets
            string _castName = Inferno.CastingName("player");
            if (_castName.Contains("Puzzle Box") || _castName.Contains("Emberwing")) return false;
                        if (HandleTrinkets()) return true;

            // Racial damage CDs
            if ((HasBuff("Ascendance") || HasBuff("Stormkeeper")) && HandleRacials()) return true;

            int enemies = Inferno.EnemiesNearUnit(8f, "target");
            if (enemies < 1) enemies = 1; if ((Inferno.IsCustomCodeOn("ForceST") || Inferno.IsCustomCodeOn("forcest"))) enemies = 1;

            // Nature's Swiftness on CD
            if (!Inferno.IsSpellKnown("Ancestral Swiftness"))
            {
                if (Cast("Nature's Swiftness")) return true;
            }
            else if (Cast("Ancestral Swiftness")) return true;



            if (enemies >= 3) return AoE(enemies);
            return SingleTarget(enemies);
        }

        bool SingleTarget(int enemies)
        {
            bool hasMotE = Inferno.IsSpellKnown("Master of the Elements");
            bool motEUp = HasBuff("Master of the Elements");

            // stormkeeper
            if (Inferno.SpellCooldown("Ascendance") > 10000 || Inferno.SpellCooldown("Ascendance") < GCDMAX())
                if (CastCD("Stormkeeper")) return true;
            // ancestral_swiftness
            if (Cast("Ancestral Swiftness")) return true;
            // ascendance
            if (Inferno.SpellCooldown("Stormkeeper") > 15000)
                if (CastCD("Ascendance")) return true;
            // flame_shock maintenance
            if (!motEUp && Inferno.DebuffRemaining("Flame Shock") < 5400)
                if (Cast("Flame Shock")) return true;
            // voltaic_blaze
            if (!motEUp && (Inferno.DebuffRemaining("Flame Shock") < 5400 || Inferno.IsSpellKnown("Purging Flames")))
                if (Cast("Voltaic Blaze")) return true;
            // lava_burst for MotE or surge procs
            if (!motEUp && (hasMotE || Inferno.BuffRemaining("Lava Surge") > GCD()))
                if (Cast("Lava Burst")) return true;
            // tempest with MotE
            if (motEUp || !hasMotE)
                if (Cast("Tempest")) return true;
            // lightning_bolt with SK + MotE
            if (HasBuff("Stormkeeper") && (motEUp || !hasMotE))
                if (Cast("Lightning Bolt")) return true;
            // elemental_blast
            if (Cast("Elemental Blast")) return true;
            // earth_shock
            if (Cast("Earth Shock")) return true;
            // tempest (fallback)
            if (Cast("Tempest")) return true;
            // chain_lightning with Call of the Ancestors at 2 targets
            if (Inferno.IsSpellKnown("Call of the Ancestors") && enemies == 2)
                if (Cast("Chain Lightning")) return true;
            // Prevent Maelstrom overcap — if casting a generator will push us past cap, wait for spender
            if (GetMaelstrom() >= 80) return false;
            // lightning_bolt
            if (Cast("Lightning Bolt")) return true;
            // frost_shock as movement filler
            if (Cast("Frost Shock")) return true;
            return false;
        }

        bool AoE(int enemies)
        {
            bool motEUp = HasBuff("Master of the Elements");

            // stormkeeper
            if (Inferno.SpellCooldown("Ascendance") > 10000 || Inferno.SpellCooldown("Ascendance") < GCDMAX())
                if (CastCD("Stormkeeper")) return true;
            if (Cast("Ancestral Swiftness")) return true;
            // ascendance
            if (Inferno.SpellCooldown("Stormkeeper") > 15000)
                if (CastCD("Ascendance")) return true;
            // flame_shock at 3 targets for MotE+Inferno Arc
            if (!motEUp && Inferno.DebuffRemaining("Flame Shock") < 5400 && Inferno.IsSpellKnown("Master of the Elements") && Inferno.IsSpellKnown("Inferno Arc") && enemies == 3)
                if (Cast("Flame Shock")) return true;
            // voltaic_blaze
            if (!motEUp && Cast("Voltaic Blaze")) return true;
            // tempest/EB priority when Lightning Rod on target
            if (HasDebuff("Lightning Rod"))
            {
                if (Cast("Tempest")) return true;
                if (Cast("Elemental Blast")) return true;
            }

            // earthquake
            if (Inferno.BuffStacks("Tempest") < 2)
                if (Cast("Earthquake")) return true;
            // elemental_blast at 3 targets
            if (Inferno.BuffStacks("Tempest") < 2 && enemies == 3)
                if (Cast("Elemental Blast")) return true;
            // lava_burst with purging flames
            if (HasBuff("Purging Flames") && Inferno.BuffRemaining("Lava Surge") > GCD())
                if (Cast("Lava Burst")) return true;
            // lava_burst at 3t for MotE
            if (HasBuff("Tempest") && Inferno.BuffRemaining("Lava Surge") > GCD() && Inferno.IsSpellKnown("Master of the Elements") && enemies == 3)
                if (Cast("Lava Burst")) return true;
            // tempest with MotE
            if (motEUp && Cast("Tempest")) return true;
            // chain_lightning with SK
            if (HasBuff("Stormkeeper") && Cast("Chain Lightning")) return true;
            // earthquake filler
            if (Cast("Earthquake")) return true;
            // elemental_blast
            if (Cast("Elemental Blast")) return true;
            // tempest
            if (Cast("Tempest")) return true;
            // Prevent Maelstrom overcap — if casting a generator will push us past cap, wait for spender
            if (GetMaelstrom() >= 80) return false;
            // chain_lightning
            if (Cast("Chain Lightning")) return true;
            return false;
        }

        // =====================================================================
        // DEFENSIVES / INTERRUPT / TRINKETS
        // =====================================================================
        bool HandleDefensives()
        {
            if (!GetCheckBox("Use Defensives")) return false;
            int hpPct = GetPlayerHealthPct();
            if (hpPct <= GetSlider("Astral Shift HP %") && Inferno.CanCast("Astral Shift", IgnoreGCD: true))
            { Inferno.Cast("Astral Shift", QuickDelay: true); return true; }
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
            if (castPct >= _interruptTargetPct && Inferno.CanCast("Wind Shear", IgnoreGCD: true))
            { Inferno.Cast("Wind Shear", QuickDelay: true); _lastCastingID = 0; return true; }
            return false;
        }

        bool HandleTrinkets()
        {
            if (!GetCheckBox("Use Trinkets") || (Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;
            if (Inferno.CanUseEquippedItem(13)) { Inferno.Cast("trinket1"); return true; }
            if (Inferno.CanUseEquippedItem(14)) { Inferno.Cast("trinket2"); return true; }
            return false;
        }

        int GCD() { return Inferno.GCD(); }
        int GCDMAX() { int g = (int)(1500f / (1f + Inferno.Haste("player") / 100f)); return g < 750 ? 750 : g; }
        bool HasDebuff(string name) { return Inferno.DebuffRemaining(name) > GCD(); }
        bool HasBuff(string name) { return Inferno.BuffRemaining(name) > GCD(); }

        // Maelstrom prediction — Power() only updates after cast completes
        int GetMaelstrom()
        {
            int ms = Inferno.Power("player", 11);
            string casting = Inferno.CastingName("player");
            if (casting == "Lightning Bolt") ms += 10;
            else if (casting == "Lava Burst") ms += 10;
            else if (casting == "Chain Lightning") ms += 6;
            else if (casting == "Elemental Blast") ms += 30;
            return Math.Min(ms, Inferno.MaxPower("player", 11));
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
        bool Cast(string name) { if (Inferno.CanCast(name)) { Inferno.Cast(name); Inferno.PrintMessage(">> " + name, Color.White); return true; } return false; }
        bool CastCD(string name)
        {
            if ((Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;
            if (name == "Stormkeeper" && !GetCheckBox("Use Stormkeeper")) return false;
            if (name == "Ascendance" && !GetCheckBox("Use Ascendance")) return false;
            if (Inferno.CanCast(name)) { Inferno.Cast(name); Inferno.PrintMessage(">> " + name, Color.White); return true; } return false;
        }
    }
}
