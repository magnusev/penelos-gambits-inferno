using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API;

namespace InfernoWow.Modules
{
    public class ProtectionPaladinRotation : Rotation
    {
        List<string> Abilities = new List<string> {
            "Shield of the Righteous", "Avenger's Shield", "Judgment",
            "Hammer of Wrath", "Blessed Hammer", "Hammer of the Righteous",
            "Consecration", "Avenging Wrath", "Divine Toll",
            "Hammer of Light", "Holy Armaments",
        };
        List<string> DefensiveSpells = new List<string> { "Ardent Defender", "Guardian of Ancient Kings", "Word of Glory", "Lay on Hands", "Divine Shield" };
        List<string> UtilitySpells = new List<string> { "Rebuke", "Devotion Aura" };
        const int HealthstoneItemID = 5512; const int HolyPowerType = 9;
        private Random _rng = new Random(); private int _lastCastingID = 0; private int _interruptTargetPct = 0;

        public override void LoadSettings()
        {
            Settings.Add(new Setting("=== Protection Paladin ===")); Settings.Add(new Setting("Use Avenging Wrath", true));
            Settings.Add(new Setting("Use Trinkets", true));
            Settings.Add(new Setting("=== Defensives ===")); Settings.Add(new Setting("Use Defensives", true));
            Settings.Add(new Setting("Word of Glory HP %", 1, 100, 50)); Settings.Add(new Setting("Ardent Defender HP %", 1, 100, 35));
            Settings.Add(new Setting("Guardian of Ancient Kings HP %", 1, 100, 25)); Settings.Add(new Setting("Lay on Hands HP %", 1, 100, 15));
            Settings.Add(new Setting("Healthstone HP %", 1, 100, 50));
            Settings.Add(new Setting("=== Interrupt ===")); Settings.Add(new Setting("Auto Interrupt", true));
            Settings.Add(new Setting("Interrupt at cast % (min)", 0, 100, 40)); Settings.Add(new Setting("Interrupt at cast % (max)", 0, 100, 90));
        }

        public override void Initialize()
        {
            Inferno.PrintMessage("             //  PROTECTION - PALADIN (MID) V1.00 //", Color.Gold);
            string addonCmd = Inferno.GetAddonName().Length >= 5 ? Inferno.GetAddonName().Substring(0, 5).ToLower() : Inferno.GetAddonName().ToLower();
            Inferno.PrintMessage("Ready! Use /" + addonCmd + " toggle to pause/resume.", Color.LimeGreen);
            Inferno.PrintMessage("Toggle CDs: /" + addonCmd + " NoCDs | Force ST: /" + addonCmd + " ForceST", Color.Yellow);
            Inferno.Latency = 250;
            foreach (string s in Abilities) Spellbook.Add(s); foreach (string s in DefensiveSpells) Spellbook.Add(s); foreach (string s in UtilitySpells) Spellbook.Add(s);
            Macros.Add("use_healthstone", "/use Healthstone"); Macros.Add("trinket1", "/use 13"); Macros.Add("trinket2", "/use 14");
            CustomFunctions.Add("HasHealthstone", "return GetItemCount(5512) > 0 and 1 or 0");
            // Racial abilities
            foreach (string r in new string[] { "Berserking", "Blood Fury", "Ancestral Call", "Fireblood", "Lights Judgment" }) Spellbook.Add(r);
            CustomCommands.Add("NoCDs");
            CustomCommands.Add("ForceST");
        }

        public override bool OutOfCombatTick()
        {
            if (!Inferno.HasBuff("Devotion Aura") && Inferno.CanCast("Devotion Aura"))
            { Inferno.Cast("Devotion Aura"); return true; }
            return false;
        }

        public override bool CombatTick()
        {
            if (Inferno.IsDead("player") || Inferno.IsGhost("player")) return false;
            if (HandleDefensives()) return true; if (HandleInterrupt()) return true;
            if (!Inferno.UnitCanAttack("player", "target")) return false;
            if ((true) && HandleRacials()) return true;
            if (Inferno.IsChanneling("player")) return false;

            // Don't interrupt cast-time trinkets
            string _castName = Inferno.CastingName("player");
            if (_castName.Contains("Puzzle Box") || _castName.Contains("Emberwing")) return false;
            int hp = Inferno.Power("player", HolyPowerType);

            // Avenging Wrath
            if (!Inferno.IsCustomCodeOn("NoCDs") && GetCheckBox("Use Avenging Wrath") && Cast("Avenging Wrath")) return true;
            // Trinkets during Avenging Wrath
            if (GetCheckBox("Use Trinkets") && !Inferno.IsCustomCodeOn("NoCDs") && HasBuff("Avenging Wrath"))
            { if (Inferno.CanUseEquippedItem(13)) { Inferno.Cast("trinket1"); return true; } if (Inferno.CanUseEquippedItem(14)) { Inferno.Cast("trinket2"); return true; } }

            // FINISHERS at 3+ HP
            if (hp >= 3)
            {
                // Hammer of Light
                if (Cast("Hammer of Light")) return true;
                // Shield of the Righteous
                if (Cast("Shield of the Righteous")) return true;
            }

            // GENERATORS
            // Holy Armaments
            if (Cast("Holy Armaments")) return true;
            // Avenger's Shield (highest priority generator)
            if (Cast("Avenger's Shield")) return true;
            // Judgment
            if (Cast("Judgment")) return true;
            // Divine Toll
            if (Cast("Divine Toll")) return true;
            // Hammer of Wrath
            if (Cast("Hammer of Wrath")) return true;
            // Blessed Hammer / Hammer of the Righteous
            if (Cast("Blessed Hammer")) return true;
            if (Cast("Hammer of the Righteous")) return true;
            // Consecration
            if (Inferno.BuffRemaining("Consecration") < GCD() && Cast("Consecration")) return true;
            return false;
        }

        bool HandleDefensives()
        {
            if (!GetCheckBox("Use Defensives")) return false; int hpPct = GetPlayerHealthPct(); int holyPower = Inferno.Power("player", HolyPowerType);
            if (hpPct <= GetSlider("Lay on Hands HP %") && Inferno.CanCast("Lay on Hands", IgnoreGCD: true)) { Inferno.Cast("Lay on Hands", QuickDelay: true); return true; }
            if (hpPct <= GetSlider("Guardian of Ancient Kings HP %") && Inferno.CanCast("Guardian of Ancient Kings", IgnoreGCD: true)) { Inferno.Cast("Guardian of Ancient Kings", QuickDelay: true); return true; }
            if (hpPct <= GetSlider("Ardent Defender HP %") && Inferno.CanCast("Ardent Defender", IgnoreGCD: true)) { Inferno.Cast("Ardent Defender", QuickDelay: true); return true; }
            if (hpPct <= GetSlider("Word of Glory HP %") && holyPower >= 3 && Inferno.CanCast("Word of Glory")) { Inferno.Cast("Word of Glory"); return true; }
            if (hpPct <= GetSlider("Healthstone HP %") && Inferno.CustomFunction("HasHealthstone") == 1 && Inferno.ItemCooldown(HealthstoneItemID) == 0) { Inferno.Cast("use_healthstone", QuickDelay: true); return true; }
            return false;
        }

        bool HandleInterrupt()
        {
            if (!GetCheckBox("Auto Interrupt")) return false; int castingID = Inferno.CastingID("target");
            if (castingID == 0 || !Inferno.IsInterruptable("target")) { _lastCastingID = 0; return false; }
            if (castingID != _lastCastingID) { _lastCastingID = castingID; int minPct = GetSlider("Interrupt at cast % (min)"); int maxPct = GetSlider("Interrupt at cast % (max)"); if (maxPct < minPct) maxPct = minPct; _interruptTargetPct = _rng.Next(minPct, maxPct + 1); }
            int elapsed = Inferno.CastingElapsed("target"); int remaining = Inferno.CastingRemaining("target"); int total = elapsed + remaining; if (total <= 0) return false;
            if ((elapsed * 100 / total) >= _interruptTargetPct && Inferno.CanCast("Rebuke", IgnoreGCD: true)) { Inferno.Cast("Rebuke", QuickDelay: true); _lastCastingID = 0; return true; }
            return false;
        }

        int GCD() { return Inferno.GCD(); } bool HasBuff(string n) { return Inferno.BuffRemaining(n) > GCD(); }

        bool HandleRacials()
        {
            if (Inferno.IsCustomCodeOn("NoCDs")) return false;
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
