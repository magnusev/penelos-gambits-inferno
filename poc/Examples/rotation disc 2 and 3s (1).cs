using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API;

namespace InfernoWow.Modules
{
    public class DisciplinePriestVoidweaverPvpRotation : Rotation
    {
        private readonly List<string> Abilities = new List<string>
        {
            "Power Word: Radiance", "Penance", "Smite", "Void Blast",
            "Mind Blast", "Shadow Word: Pain", "Purge the Wicked", "Evangelism", "Power Infusion",
            "Pain Suppression", "Leap of Faith", "Desperate Prayer", 
            "Shadow Word: Death", "Flash Heal", "Psychic Scream", "Silence", "Dispel Magic", "Mass Dispel", 
            "Voidwraith"
        };

        private readonly List<string> TalentChecks = new List<string>
        {
            "Ultimate Radiance", "Purge the Wicked", "Phase Shift"
        };

        const int HealthstoneItemID = 5512;

        private Random _rng = new Random();
        private int _lastCastingID = 0;
        private int _interruptTargetPct = 0;

        public override void LoadSettings()
        {
            Settings.Add(new Setting("=== Discipline Priest (VOIDWEAVER ARENA) ==="));
            Settings.Add(new Setting("Use Power Infusion", true));
            Settings.Add(new Setting("Use Evangelism Burst", true));
            Settings.Add(new Setting("Use Trinkets", true));
            Settings.Add(new Setting("=== Defensives ==="));
            Settings.Add(new Setting("Use Defensives", true));
            Settings.Add(new Setting("Auto Pain Suppression < 55%", true));
            Settings.Add(new Setting("Auto Flash Heal on Surge of Light < 88%", true));
            Settings.Add(new Setting("Auto Desperate Prayer < 35%", true));
            Settings.Add(new Setting("Auto Leap of Faith < 25%", true));
            Settings.Add(new Setting("Auto Radiance on Proc Only", true));
            Settings.Add(new Setting("Protect Focus", true));
            Settings.Add(new Setting("Healthstone HP %", 1, 100, 45));
            Settings.Add(new Setting("=== Interrupt / Control ==="));
            Settings.Add(new Setting("Auto Interrupt (Silence)", true));
            Settings.Add(new Setting("Interrupt at cast % (min)", 0, 100, 35));
            Settings.Add(new Setting("Interrupt at cast % (max)", 0, 100, 85));
        }

        public override void Initialize()
        {
            Inferno.PrintMessage("             //////////////////////////////////////", Color.DarkSlateGray);
            Inferno.PrintMessage("             // DISC PRIEST - VOIDWEAVER ARENA  //", Color.DarkSlateGray);
            Inferno.PrintMessage("             //   Shields Fully Manual          //", Color.DarkSlateGray);
            Inferno.PrintMessage("             //////////////////////////////////////", Color.DarkSlateGray);

            Inferno.PrintMessage("Disc Priest Arena loaded! Shields fully manual - smooth cycling.", Color.LimeGreen);
            Inferno.Latency = 195;

            foreach (string s in Abilities) Spellbook.Add(s);
            foreach (string s in TalentChecks) Spellbook.Add(s);

            Macros.Add("use_healthstone", "/use Healthstone");
            Macros.Add("trinket1", "/use 13");
            Macros.Add("trinket2", "/use 14");

            CustomFunctions.Add("HasHealthstone", "return GetItemCount(5512) > 0 and 1 or 0");
            CustomCommands.Add("NoCDs");
            CustomCommands.Add("ForceST");
        }

        public override bool OutOfCombatTick() { return false; }

        public override bool CombatTick()
        {
            if (Inferno.IsDead("player") || Inferno.IsGhost("player")) return false;

            if (HandleDefensives()) return true;
            if (HandleInterrupt()) return true;
            if (Inferno.IsChanneling("player")) return false;

            if (HandleTrinkets()) return true;

            bool inRift = HasBuff("Entropic Rift") || Inferno.SpellCooldown("Mind Blast") <= GCD() + 2500;
            bool inBurst = HasBuff("Power Infusion") || HasBuff("Evangelism") || inRift;

            if (HandleCooldowns(inBurst)) return true;
            if (HandleDamage(inRift, inBurst)) return true;

            // Strong filler to keep rotation cycling smoothly
            if (Inferno.CanCast("Smite"))
                if (Cast("Smite")) return true;

            return false;
        }

        bool HandleDefensives()
        {
            if (!GetCheckBox("Use Defensives")) return false;

            if (GetCheckBox("Auto Desperate Prayer < 35%"))
                if (HandleAutoDesperatePrayer()) return true;

            if (GetCheckBox("Auto Pain Suppression < 55%"))
                if (HandleAutoPainSuppression()) return true;

            if (GetCheckBox("Auto Leap of Faith < 25%"))
                if (HandleAutoLeapOfFaith()) return true;

            int hpPct = GetPlayerHealthPct();
            if (hpPct <= GetSlider("Healthstone HP %") && Inferno.CustomFunction("HasHealthstone") == 1 && Inferno.ItemCooldown(HealthstoneItemID) == 0)
            {
                Inferno.Cast("use_healthstone", QuickDelay: true);
                return true;
            }

            return false;
        }

        bool HandleCooldowns(bool inBurst)
        {
            if (Inferno.IsCustomCodeOn("NoCDs")) return false;

            if (GetCheckBox("Use Power Infusion") && Inferno.CanCast("Power Infusion"))
                if (Cast("Power Infusion")) return true;

            if (GetCheckBox("Use Evangelism Burst") && inBurst && Inferno.CanCast("Evangelism"))
                if (Cast("Evangelism")) return true;

            return false;
        }

        bool HandleDamage(bool inRift, bool inBurst)
        {
            if (GetCheckBox("Auto Radiance on Proc Only") && HasBuff("Radiance Proc") && Inferno.CanCast("Power Word: Radiance"))
                if (Cast("Power Word: Radiance")) return true;

            if (GetCheckBox("Auto Flash Heal on Surge of Light < 88%") && HasBuff("Surge of Light") && Inferno.CanCast("Flash Heal"))
                if (HandleFlashHealProc()) return true;

            if (Inferno.CanCast("Shadow Word: Death"))
                if (Cast("Shadow Word: Death")) return true;

            if (Inferno.CanCast("Mind Blast"))
                if (Cast("Mind Blast")) return true;

            if (inRift && Inferno.CanCast("Void Blast"))
                if (Cast("Void Blast")) return true;

            if (Inferno.CanCast("Penance"))
                if (Cast("Penance")) return true;

            if (Inferno.IsSpellKnown("Purge the Wicked") && !HasDebuff("Purge the Wicked"))
                if (Cast("Purge the Wicked")) return true;

            if (!HasDebuff("Shadow Word: Pain"))
                if (Cast("Shadow Word: Pain")) return true;

            if (Inferno.CanCast("Voidwraith"))
                if (Cast("Voidwraith")) return true;

            return false;
        }

        bool HandleAutoDesperatePrayer()
        {
            if (GetPlayerHealthPct() < 35 && Inferno.CanCast("Desperate Prayer"))
            {
                Inferno.Cast("Desperate Prayer", QuickDelay: true);
                Inferno.PrintMessage(">> Desperate Prayer", Color.OrangeRed);
                return true;
            }
            return false;
        }

        bool HandleAutoPainSuppression()
        {
            if (!Inferno.CanCast("Pain Suppression")) return false;

            if (GetCheckBox("Protect Focus"))
            {
                int focusHp = GetUnitHealthPct("focus");
                if (focusHp > 0 && focusHp < 55)
                {
                    Inferno.Cast("Pain Suppression", QuickDelay: true);
                    Inferno.PrintMessage(">> Pain Suppression on Focus (" + focusHp + "%)", Color.Cyan);
                    return true;
                }
            }

            string bestUnit = "";
            int lowestHp = 101;

            for (int i = 1; i <= 3; i++)
            {
                string unit = "party" + i;
                int hp = GetUnitHealthPct(unit);
                if (hp > 0 && hp < lowestHp) { lowestHp = hp; bestUnit = unit; }
            }

            int selfHp = GetPlayerHealthPct();
            if (selfHp < lowestHp) { lowestHp = selfHp; bestUnit = "player"; }

            if (!string.IsNullOrEmpty(bestUnit) && lowestHp < 55)
            {
                Inferno.Cast("Pain Suppression", QuickDelay: true);
                Inferno.PrintMessage(">> Pain Suppression on " + bestUnit + " (" + lowestHp + "%)", Color.Cyan);
                return true;
            }
            return false;
        }

        bool HandleAutoLeapOfFaith()
        {
            if (!Inferno.CanCast("Leap of Faith")) return false;

            if (GetCheckBox("Protect Focus"))
            {
                int focusHp = GetUnitHealthPct("focus");
                if (focusHp > 0 && focusHp < 25)
                {
                    Inferno.Cast("Leap of Faith", QuickDelay: true);
                    Inferno.PrintMessage(">> Leap of Faith on Focus", Color.Red);
                    return true;
                }
            }

            string bestUnit = "";
            int lowestHp = 101;

            for (int i = 1; i <= 3; i++)
            {
                string unit = "party" + i;
                int hp = GetUnitHealthPct(unit);
                if (hp > 0 && hp < lowestHp) { lowestHp = hp; bestUnit = unit; }
            }

            int selfHp = GetPlayerHealthPct();
            if (selfHp < lowestHp) { lowestHp = selfHp; bestUnit = "player"; }

            if (!string.IsNullOrEmpty(bestUnit) && lowestHp < 25)
            {
                Inferno.Cast("Leap of Faith", QuickDelay: true);
                Inferno.PrintMessage(">> Leap of Faith on " + bestUnit, Color.Red);
                return true;
            }
            return false;
        }

        bool HandleFlashHealProc()
        {
            if (!HasBuff("Surge of Light") || !Inferno.CanCast("Flash Heal")) return false;

            if (GetCheckBox("Protect Focus"))
            {
                int focusHp = GetUnitHealthPct("focus");
                if (focusHp > 0 && focusHp < 88)
                {
                    Inferno.Cast("Flash Heal", QuickDelay: true);
                    Inferno.PrintMessage(">> Flash Heal (Surge) on Focus (" + focusHp + "%)", Color.LightBlue);
                    return true;
                }
            }

            string bestUnit = "";
            int lowestHp = 101;

            for (int i = 1; i <= 3; i++)
            {
                string unit = "party" + i;
                int hp = GetUnitHealthPct(unit);
                if (hp > 0 && hp < lowestHp) { lowestHp = hp; bestUnit = unit; }
            }

            int selfHp = GetPlayerHealthPct();
            if (selfHp < lowestHp) { lowestHp = selfHp; bestUnit = "player"; }

            if (!string.IsNullOrEmpty(bestUnit) && lowestHp < 88)
            {
                Inferno.Cast("Flash Heal", QuickDelay: true);
                Inferno.PrintMessage(">> Flash Heal (Surge) on " + bestUnit + " (" + lowestHp + "%)", Color.LightBlue);
                return true;
            }
            return false;
        }

        int GetUnitHealthPct(string unit)
        {
            int hp = Inferno.Health(unit);
            int maxHp = Inferno.MaxHealth(unit);
            if (maxHp < 1) return 0;
            return (hp * 100) / maxHp;
        }

        bool HandleInterrupt()
        {
            if (!GetCheckBox("Auto Interrupt (Silence)")) return false;

            int castingID = Inferno.CastingID("target");
            if (castingID == 0 || !Inferno.IsInterruptable("target"))
            {
                _lastCastingID = 0;
                return false;
            }

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

            int castPct = (elapsed * 100) / total;
            if (castPct >= _interruptTargetPct && Inferno.CanCast("Silence", IgnoreGCD: true))
            {
                Inferno.Cast("Silence", QuickDelay: true);
                _lastCastingID = 0;
                return true;
            }
            return false;
        }

        bool HandleTrinkets()
        {
            if (!GetCheckBox("Use Trinkets") || Inferno.IsCustomCodeOn("NoCDs")) return false;
            if (!HasBuff("Power Infusion") && !HasBuff("Entropic Rift")) return false;

            if (Inferno.CanUseEquippedItem(13)) { Inferno.Cast("trinket1"); return true; }
            if (Inferno.CanUseEquippedItem(14)) { Inferno.Cast("trinket2"); return true; }
            return false;
        }

        int GCD() { return Inferno.GCD(); }

        bool HasBuff(string name) { return Inferno.BuffRemaining(name) > GCD(); }
        bool HasDebuff(string name) { return Inferno.DebuffRemaining(name) > GCD(); }

        int GetPlayerHealthPct()
        {
            int hp = Inferno.Health("player");
            int maxHp = Inferno.MaxHealth("player");
            if (maxHp < 1) maxHp = 1;
            return (hp * 100) / maxHp;
        }

        bool Cast(string name)
        {
            if (Inferno.CanCast(name))
            {
                Inferno.Cast(name);
                Inferno.PrintMessage(">> " + name, Color.White);
                return true;
            }
            return false;
        }
    }
}
