using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API;

namespace InfernoWow.Modules
{
    public class HolyPaladinPvpRotation : Rotation
    {
        private readonly List<string> Abilities = new List<string>
        {
            "Holy Shock", "Flash of Light", "Holy Light", "Light of Dawn", "Word of Glory",
            "Judgment", "Crusader Strike", "Divine Toll", "Avenging Crusader", "Lay on Hands",
            "Blessing of Protection", "Blessing of Sacrifice", "Blessing of Freedom", 
            "Hammer of Justice", "Cleanse", "Beacon of Light", "Divine Shield", "Holy Prism"
        };

        const int HealthstoneItemID = 5512;

        private Random _rng = new Random();
        private int _lastCastingID = 0;
        private int _interruptTargetPct = 0;

        public override void LoadSettings()
        {
            Settings.Add(new Setting("=== Holy Paladin PVP Arena ==="));
            Settings.Add(new Setting("Use Avenging Crusader", true));
            Settings.Add(new Setting("Use Divine Toll", true));
            Settings.Add(new Setting("Use Trinkets", true));
            Settings.Add(new Setting("=== Defensives ==="));
            Settings.Add(new Setting("Use Defensives", true));
            Settings.Add(new Setting("Auto Blessing of Protection < 30%", true));
            Settings.Add(new Setting("Auto Blessing of Sacrifice < 40%", true));           // Set to 40%
            Settings.Add(new Setting("Auto Lay on Hands < 25%", true));
            Settings.Add(new Setting("Auto Word of Glory < 80%", true));
            Settings.Add(new Setting("Auto Flash of Light on Infusion Proc < 88%", true));
            Settings.Add(new Setting("Protect Focus", true));
            Settings.Add(new Setting("Healthstone HP %", 1, 100, 45));
            Settings.Add(new Setting("=== Interrupt ==="));
            Settings.Add(new Setting("Auto Hammer of Justice", true));
            Settings.Add(new Setting("Interrupt at cast % (min)", 0, 100, 40));
            Settings.Add(new Setting("Interrupt at cast % (max)", 0, 100, 85));
        }

        public override void Initialize()
        {
            Inferno.PrintMessage("             //////////////////////////////////////", Color.DarkSlateGray);
            Inferno.PrintMessage("             //   HOLY PALADIN - ARENA PVP     //", Color.DarkSlateGray);
            Inferno.PrintMessage("             //   Smart Group Emergency Heals   //", Color.DarkSlateGray);
            Inferno.PrintMessage("             //////////////////////////////////////", Color.DarkSlateGray);

            Inferno.PrintMessage("Holy Paladin Arena loaded! Sacrifice at 40% HP.", Color.LimeGreen);
            Inferno.Latency = 185;

            foreach (string s in Abilities) Spellbook.Add(s);

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

            bool inCrusader = HasBuff("Avenging Crusader");

            if (HandleCooldowns(inCrusader)) return true;
            if (HandleHealing()) return true;
            if (HandleDamage()) return true;

            return false;
        }

        bool HandleDefensives()
        {
            if (!GetCheckBox("Use Defensives")) return false;

            // Emergency saves: Sacrifice at 40%, Lay on Hands at 25%
            if (GetCheckBox("Auto Blessing of Sacrifice < 40%"))
                if (HandleAutoBlessingOfSacrifice()) return true;

            if (GetCheckBox("Auto Lay on Hands < 25%") && GetPlayerHealthPct() < 25 && Inferno.CanCast("Lay on Hands", IgnoreGCD: true))
            {
                Inferno.Cast("Lay on Hands", QuickDelay: true);
                Inferno.PrintMessage(">> Lay on Hands", Color.Green);
                return true;
            }

            if (GetCheckBox("Auto Blessing of Protection < 30%") && GetPlayerHealthPct() < 30 && Inferno.CanCast("Blessing of Protection", IgnoreGCD: true))
            {
                Inferno.Cast("Blessing of Protection", QuickDelay: true);
                Inferno.PrintMessage(">> BoP", Color.Green);
                return true;
            }

            int hpPct = GetPlayerHealthPct();
            if (hpPct <= GetSlider("Healthstone HP %") && Inferno.CustomFunction("HasHealthstone") == 1 && Inferno.ItemCooldown(HealthstoneItemID) == 0)
            {
                Inferno.Cast("use_healthstone", QuickDelay: true);
                return true;
            }

            return false;
        }

        // Scans Focus + Party1-3 + Self and applies Sacrifice to the lowest HP below 40%
        bool HandleAutoBlessingOfSacrifice()
        {
            if (!Inferno.CanCast("Blessing of Sacrifice")) return false;

            string bestUnit = "";
            int lowestHp = 101;

            // Check Focus
            if (GetCheckBox("Protect Focus"))
            {
                int hp = GetUnitHealthPct("focus");
                if (hp > 0 && hp < lowestHp)
                {
                    lowestHp = hp;
                    bestUnit = "focus";
                }
            }

            // Check Party 1-3
            for (int i = 1; i <= 3; i++)
            {
                string unit = "party" + i;
                int hp = GetUnitHealthPct(unit);
                if (hp > 0 && hp < lowestHp)
                {
                    lowestHp = hp;
                    bestUnit = unit;
                }
            }

            // Check Self
            int selfHp = GetPlayerHealthPct();
            if (selfHp < lowestHp)
            {
                lowestHp = selfHp;
                bestUnit = "player";
            }

            if (!string.IsNullOrEmpty(bestUnit) && lowestHp < 40)
            {
                Inferno.Cast("Blessing of Sacrifice", QuickDelay: true);
                Inferno.PrintMessage(">> Blessing of Sacrifice on " + bestUnit + " (" + lowestHp + "%)", Color.Green);
                return true;
            }

            return false;
        }

        bool HandleCooldowns(bool inCrusader)
        {
            if (Inferno.IsCustomCodeOn("NoCDs")) return false;

            if (GetCheckBox("Use Avenging Crusader") && Inferno.UnitCanAttack("player", "target") && Inferno.CanCast("Avenging Crusader"))
                if (Cast("Avenging Crusader")) return true;

            if (GetCheckBox("Use Divine Toll") && Inferno.CanCast("Divine Toll"))
                if (Cast("Divine Toll")) return true;

            return false;
        }

        bool HandleHealing()
        {
            if (GetCheckBox("Auto Word of Glory < 80%"))
                if (HandleWordOfGlory()) return true;

            if (GetCheckBox("Auto Flash of Light on Infusion Proc < 88%") && HasBuff("Infusion of Light") && Inferno.CanCast("Flash of Light"))
                if (HandleInfusionOfLightProc()) return true;

            int playerHp = GetPlayerHealthPct();

            if (Inferno.CanCast("Holy Shock"))
                if (Cast("Holy Shock")) return true;

            if (playerHp < 70 && Inferno.CanCast("Flash of Light"))
                if (Cast("Flash of Light")) return true;

            if (Inferno.CanCast("Light of Dawn"))
                if (Cast("Light of Dawn")) return true;

            return false;
        }

        bool HandleWordOfGlory()
        {
            if (!Inferno.CanCast("Word of Glory")) return false;

            string bestUnit = "";
            int lowestHp = 101;

            if (GetCheckBox("Protect Focus"))
            {
                int hp = GetUnitHealthPct("focus");
                if (hp > 0 && hp < lowestHp) { lowestHp = hp; bestUnit = "focus"; }
            }

            for (int i = 1; i <= 3; i++)
            {
                string unit = "party" + i;
                int hp = GetUnitHealthPct(unit);
                if (hp > 0 && hp < lowestHp) { lowestHp = hp; bestUnit = unit; }
            }

            int selfHp = GetPlayerHealthPct();
            if (selfHp < lowestHp) { lowestHp = selfHp; bestUnit = "player"; }

            if (!string.IsNullOrEmpty(bestUnit) && lowestHp < 80)
            {
                Inferno.Cast("Word of Glory", QuickDelay: true);
                Inferno.PrintMessage(">> Word of Glory on " + bestUnit + " (" + lowestHp + "%)", Color.LightGreen);
                return true;
            }

            return false;
        }

        bool HandleInfusionOfLightProc()
        {
            if (!HasBuff("Infusion of Light") || !Inferno.CanCast("Flash of Light")) return false;

            string bestUnit = "";
            int lowestHp = 101;

            if (GetCheckBox("Protect Focus"))
            {
                int hp = GetUnitHealthPct("focus");
                if (hp > 0 && hp < lowestHp) { lowestHp = hp; bestUnit = "focus"; }
            }

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
                Inferno.Cast("Flash of Light", QuickDelay: true);
                Inferno.PrintMessage(">> Flash of Light (Infusion) on " + bestUnit + " (" + lowestHp + "%)", Color.LightBlue);
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

        bool HandleDamage()
        {
            if (Inferno.CanCast("Judgment"))
                if (Cast("Judgment")) return true;

            if (Inferno.CanCast("Crusader Strike"))
                if (Cast("Crusader Strike")) return true;

            return false;
        }

        bool HandleInterrupt()
        {
            if (!GetCheckBox("Auto Hammer of Justice")) return false;

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
            if (castPct >= _interruptTargetPct && Inferno.CanCast("Hammer of Justice", IgnoreGCD: true))
            {
                Inferno.Cast("Hammer of Justice", QuickDelay: true);
                _lastCastingID = 0;
                return true;
            }
            return false;
        }

        bool HandleTrinkets()
        {
            if (!GetCheckBox("Use Trinkets") || Inferno.IsCustomCodeOn("NoCDs")) return false;
            if (!HasBuff("Avenging Crusader")) return false;

            if (Inferno.CanUseEquippedItem(13)) { Inferno.Cast("trinket1"); return true; }
            if (Inferno.CanUseEquippedItem(14)) { Inferno.Cast("trinket2"); return true; }
            return false;
        }

        int GCD() { return Inferno.GCD(); }

        bool HasBuff(string name) { return Inferno.BuffRemaining(name) > GCD(); }

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
