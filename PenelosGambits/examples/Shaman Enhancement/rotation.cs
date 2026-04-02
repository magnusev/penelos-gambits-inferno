using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API;

namespace InfernoWow.Modules
{
    /// <summary>
    /// Enhancement Shaman - Translated from SimulationCraft Midnight APL
    /// Auto-detects hero tree: Stormbringer or Totemic (Surging Totem).
    /// Maelstrom Weapon stack management, Doom Winds/Ascendance windows.
    /// </summary>
    public class EnhancementShamanRotation : Rotation
    {
        List<string> Abilities = new List<string> {
            "Stormstrike", "Windstrike", "Lava Lash", "Crash Lightning",
            "Lightning Bolt", "Chain Lightning", "Flame Shock",
            "Tempest", "Voltaic Blaze", "Sundering", "Doom Winds",
            "Ascendance", "Surging Totem", "Primordial Storm",
        };
        List<string> TalentChecks = new List<string> {
            "Surging Totem", "Thorim's Invocation", "Splitstream",
            "Fire Nova", "Surging Elements", "Feral Spirit",
            "Storm Unleashed", "Hot Hand", "Elemental Tempo",
            "Lashing Flames", "Deeply Rooted Elements"
        };
        List<string> DefensiveSpells = new List<string> { "Astral Shift" };
        List<string> UtilitySpells = new List<string> { "Wind Shear", "Windfury Weapon", "Flametongue Weapon" };

        const int HealthstoneItemID = 5512;
        private Random _rng = new Random();
        private int _lastCastingID = 0;
        private int _interruptTargetPct = 0;

        public override void LoadSettings()
        {
            Settings.Add(new Setting("=== Enhancement Shaman ==="));
            Settings.Add(new Setting("Hero tree auto-detected: Stormbringer / Totemic"));
            Settings.Add(new Setting("Use Ascendance", true));
            Settings.Add(new Setting("Use Doom Winds", true));
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
            Inferno.PrintMessage("             //   ENHANCEMENT - SHAMAN (MID)     //", Color.DodgerBlue);
            Inferno.PrintMessage("             //  STORMBRINGER / TOTEMIC          //", Color.DodgerBlue);
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
            if (!Inferno.HasBuff("Windfury Weapon") && Inferno.CanCast("Windfury Weapon"))
            { Inferno.Cast("Windfury Weapon"); return true; }
            if (!Inferno.HasBuff("Flametongue Weapon") && Inferno.CanCast("Flametongue Weapon"))
            { Inferno.Cast("Flametongue Weapon"); return true; }
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
                        if (HandleTrinkets()) return true;

            // Racial damage CDs
            if ((HasBuff("Ascendance") || HasBuff("Feral Spirit")) && HandleRacials()) return true;

            int enemies = Inferno.EnemiesNearUnit(8f, "player");
            if (enemies < 1) enemies = 1; if ((Inferno.IsCustomCodeOn("ForceST") || Inferno.IsCustomCodeOn("forcest"))) enemies = 1;
            bool isTotemic = Inferno.IsSpellKnown("Surging Totem");

            if (enemies > 1) return AoE(enemies);
            if (isTotemic) return SingleTotemic(enemies);
            return SingleSB(enemies);
        }

        // =====================================================================
        // STORMBRINGER ST (actions.single_sb)
        // =====================================================================
        bool SingleSB(int enemies)
        {
            int mw = GetMW();
            bool hasTI = Inferno.IsSpellKnown("Thorim's Invocation");

            // primordial_storm at 9+ MW
            if (mw >= 9 && Cast("Primordial Storm")) return true;
            // flame_shock maintenance
            if (Inferno.DebuffRemaining("Flame Shock") < GCD() && Cast("Flame Shock")) return true;
            // sundering with surging elements or feral spirit
            if ((Inferno.IsSpellKnown("Surging Elements") || Inferno.IsSpellKnown("Feral Spirit")) && Cast("Sundering")) return true;
            // doom_winds
            if (!Inferno.IsSpellKnown("Ascendance") && !Inferno.IsSpellKnown("Deeply Rooted Elements"))
                if (CastCD("Doom Winds")) return true;
            // crash_lightning buff maintenance
            if (!HasBuff("Crash Lightning") || Inferno.IsSpellKnown("Storm Unleashed"))
                if (Cast("Crash Lightning")) return true;
            // windstrike with TI
            if (hasTI && mw > 0 && Cast("Windstrike")) return true;
            // ascendance
            if (!Inferno.IsSpellKnown("Deeply Rooted Elements") && CastCD("Ascendance")) return true;
            // stormstrike with doom winds + TI
            if (hasTI && HasBuff("Doom Winds") && Cast("Stormstrike")) return true;
            // tempest at 10 stacks
            if (mw >= 10 && Cast("Tempest")) return true;
            // lightning_bolt at 10 stacks
            if (mw >= 10 && Cast("Lightning Bolt")) return true;
            // stormstrike at near-max charges
            if (Inferno.ChargesFractional("Stormstrike", 7500) >= 1.8f && Cast("Stormstrike")) return true;
            // lava_lash
            if (Cast("Lava Lash")) return true;
            // stormstrike
            if (Cast("Stormstrike")) return true;
            // voltaic_blaze
            if (Cast("Voltaic Blaze")) return true;
            // sundering
            if (Cast("Sundering")) return true;
            // lightning_bolt at 8+ stacks
            if (mw >= 8 && Cast("Lightning Bolt")) return true;
            // crash_lightning
            if (Cast("Crash Lightning")) return true;
            // lightning_bolt at 5+ stacks
            if (mw >= 5 && Cast("Lightning Bolt")) return true;
            // flame_shock
            if (Cast("Flame Shock")) return true;
            return false;
        }

        // =====================================================================
        // TOTEMIC ST (actions.single_totemic)
        // =====================================================================
        bool SingleTotemic(int enemies)
        {

            int mw = GetMW();

            // voltaic_blaze/flame_shock maintenance
            if (Inferno.DebuffRemaining("Flame Shock") < GCD())
            {
                if (Cast("Voltaic Blaze")) return true;
                if (Cast("Flame Shock")) return true;
            }
            // surging_totem
            if (Cast("Surging Totem")) return true;
            // sundering
            if ((Inferno.IsSpellKnown("Surging Elements") || HasBuff("Whirling Earth") || Inferno.IsSpellKnown("Feral Spirit")) && Cast("Sundering")) return true;
            // lava_lash with whirling fire or hot hand
            if ((HasBuff("Whirling Fire") || HasBuff("Hot Hand")) && Cast("Lava Lash")) return true;
            // doom_winds
            if (CastCD("Doom Winds")) return true;
            // crash_lightning
            if (!HasBuff("Crash Lightning") || Inferno.IsSpellKnown("Storm Unleashed"))
                if (Cast("Crash Lightning")) return true;
            // primordial_storm
            if (mw >= 10 && Cast("Primordial Storm")) return true;
            // windstrike with TI + ascendance
            if (Inferno.IsSpellKnown("Thorim's Invocation") && HasBuff("Ascendance") && Cast("Windstrike")) return true;
            // ascendance
            if (!Inferno.IsSpellKnown("Deeply Rooted Elements") && CastCD("Ascendance")) return true;
            // crash_lightning with TI + doom winds/ascendance
            if (Inferno.IsSpellKnown("Thorim's Invocation") && (HasBuff("Doom Winds") || HasBuff("Ascendance")))
                if (Cast("Crash Lightning")) return true;
            // stormstrike with TI + doom winds
            if (Inferno.IsSpellKnown("Thorim's Invocation") && HasBuff("Doom Winds") && Cast("Stormstrike")) return true;
            // lightning_bolt with elemental tempo
            if (Inferno.IsSpellKnown("Elemental Tempo") && mw >= 10 && Cast("Lightning Bolt")) return true;
            // lava_lash
            if (Cast("Lava Lash")) return true;
            // sundering fallback
            if (Inferno.SpellCooldown("Surging Totem") > 25000 && Cast("Sundering")) return true;
            // stormstrike
            if (Cast("Stormstrike")) return true;
            // voltaic_blaze
            if (Cast("Voltaic Blaze")) return true;
            // crash_lightning
            if (Cast("Crash Lightning")) return true;
            // lightning_bolt at 5+ stacks
            if (mw >= 5 && Cast("Lightning Bolt")) return true;
            // flame_shock
            if (Cast("Flame Shock")) return true;
            return false;
        }

        // =====================================================================
        // AoE (actions.aoe)
        // =====================================================================
        bool AoE(int enemies)
        {
            int mw = GetMW();
            bool hasTI = Inferno.IsSpellKnown("Thorim's Invocation");

            // flame_shock maintenance
            if (Inferno.DebuffRemaining("Flame Shock") < GCD())
            {
                if (Inferno.IsSpellKnown("Surging Totem") && Cast("Voltaic Blaze")) return true;
                if (Cast("Flame Shock")) return true;
            }
            // surging_totem
            if (Cast("Surging Totem")) return true;
            // ascendance
            if (!Inferno.IsSpellKnown("Deeply Rooted Elements") && CastCD("Ascendance")) return true;
            // sundering with surging elements or whirling earth
            if ((Inferno.IsSpellKnown("Surging Elements") || HasBuff("Whirling Earth")) && Cast("Sundering")) return true;
            // lava_lash with whirling fire
            if (HasBuff("Whirling Fire") && Cast("Lava Lash")) return true;
            // doom_winds
            if (!Inferno.IsSpellKnown("Ascendance") && !Inferno.IsSpellKnown("Deeply Rooted Elements"))
                if (Cast("Doom Winds")) return true;
            // crash_lightning with TI + whirling air + doom winds/ascendance
            if (hasTI && HasBuff("Whirling Air") && (HasBuff("Doom Winds") || HasBuff("Ascendance")))
                if (Cast("Crash Lightning")) return true;
            // windstrike with TI + whirling air
            if (hasTI && HasBuff("Whirling Air") && Cast("Windstrike")) return true;
            // lava_lash with splitstream + hot hand
            if (Inferno.IsSpellKnown("Splitstream") && HasBuff("Hot Hand") && Cast("Lava Lash")) return true;
            // tempest at 10 MW
            if (mw >= 10 && !HasBuff("Ascendance") && Cast("Tempest")) return true;
            // primordial_storm at 10 MW
            if (mw >= 10 && Cast("Primordial Storm")) return true;
            // voltaic_blaze with fire nova
            if (Inferno.IsSpellKnown("Fire Nova") && Cast("Voltaic Blaze")) return true;
            // crash_lightning
            if (Cast("Crash Lightning")) return true;
            // windstrike with TI
            if (hasTI && Cast("Windstrike")) return true;
            // chain_lightning at 9-10 MW
            if (mw >= 9 && Cast("Chain Lightning")) return true;
            // sundering with feral spirit
            if (Inferno.IsSpellKnown("Feral Spirit") && Cast("Sundering")) return true;
            // voltaic_blaze
            if (Cast("Voltaic Blaze")) return true;
            // lava_lash
            if (Cast("Lava Lash")) return true;
            // windstrike
            if (Cast("Windstrike")) return true;
            // stormstrike
            if (Cast("Stormstrike")) return true;
            // chain_lightning at 5+ MW
            if (mw >= 5 && Cast("Chain Lightning")) return true;
            // flame_shock
            if (Cast("Flame Shock")) return true;
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
            if (HasBuff("Ascendance") || HasBuff("Doom Winds"))
            {
                if (Inferno.CanUseEquippedItem(13)) { Inferno.Cast("trinket1"); return true; }
                if (Inferno.CanUseEquippedItem(14)) { Inferno.Cast("trinket2"); return true; }
            }
            return false;
        }

        int GCD() { return Inferno.GCD(); }
        int GetMW() { return Inferno.BuffStacks("Maelstrom Weapon"); }
        bool HasBuff(string name) { return Inferno.BuffRemaining(name) > GCD(); }

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
            if (name == "Ascendance" && !GetCheckBox("Use Ascendance")) return false;
            if (name == "Doom Winds" && !GetCheckBox("Use Doom Winds")) return false;
            if (Inferno.CanCast(name)) { Inferno.Cast(name); Inferno.PrintMessage(">> " + name, Color.White); return true; } return false;
        }
    }
}
