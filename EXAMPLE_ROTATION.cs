using System.Collections.Generic;
using System.Drawing;

namespace InfernoWow.Modules
{
    // Example rotation demonstrating usage of all helper classes
    public class ExampleMageRotation : Rotation
    {
        public override void LoadSettings()
        {
            // Checkbox settings
            Settings.Add(new Setting("Use Cooldowns", true));
            Settings.Add(new Setting("Use Trinkets", true));
            Settings.Add(new Setting("Auto Ice Block", true));
            
            // Slider settings
            Settings.Add(new Setting("Ice Block Health %", 1, 100, 25));
            Settings.Add(new Setting("AoE Enemy Count", 2, 10, 3));
            
            // Dropdown settings
            Settings.Add(new Setting("AoE Mode", new List<string> { "Never", "Always", "Auto" }, "Auto"));
            
            // Text input
            Settings.Add(new Setting("Focus Target Name", ""));
            
            // Labels
            Settings.Add(new Setting("--- Advanced ---"));
            Settings.Add(new Setting("Custom Rotation Priority", "Fireball > Fire Blast"));
        }

        public override void Initialize()
        {
            Inferno.PrintMessage("Example Mage Rotation Loaded!", Color.Green);
            
            // Register spells
            Spellbook.Add("Fireball");
            Spellbook.Add("Fire Blast");
            Spellbook.Add("Pyroblast");
            Spellbook.Add("Combustion");
            Spellbook.Add("Ice Block");
            Spellbook.Add("Flamestrike");
            Spellbook.Add("Arcane Intellect");
            
            // Register macros
            Macros.Add("FlamestrikeGround", "/cast [@cursor] Flamestrike");
            Macros.Add("FlamestrikePlayer", "/cast [@player] Flamestrike");
            
            // Register custom commands (toggled with /addon AOE, /addon BURST)
            CustomCommands.Add("AOE");
            CustomCommands.Add("BURST");
            
            // Register custom Lua functions
            CustomFunctions.Add("TargetIsBoss", "return UnitClassification('target') == 'worldboss' and 1 or 0");
        }

        public override bool OutOfCombatTick()
        {
            // Buff check using Units constant
            if (!Inferno.HasBuff("Arcane Intellect", Units.Player) && Inferno.CanCast("Arcane Intellect"))
            {
                Inferno.Cast("Arcane Intellect");
                return true;
            }
            
            return false;
        }

        public override bool CombatTick()
        {
            // Defensive using Settings
            if (GetCheckBox("Auto Ice Block"))
            {
                int healthThreshold = GetSlider("Ice Block Health %");
                if (Inferno.Health(Units.Player) <= healthThreshold && Inferno.CanCast("Ice Block"))
                {
                    Inferno.PrintMessage("Using Ice Block at " + healthThreshold + "%!", Color.Red);
                    Inferno.Cast("Ice Block");
                    return true;
                }
            }
            
            // Trinket usage using InventorySlot constants
            if (GetCheckBox("Use Trinkets"))
            {
                if (Inferno.CanUseEquippedItem(InventorySlot.Trinket1, false))
                {
                    int trinketId = Inferno.InventoryItemID(InventorySlot.Trinket1);
                    if (Inferno.ItemCooldown(trinketId) == 0)
                    {
                        Inferno.Cast("Trinket 1", true);
                        return true;
                    }
                }
                
                if (Inferno.CanUseEquippedItem(InventorySlot.Trinket2, false))
                {
                    int trinketId = Inferno.InventoryItemID(InventorySlot.Trinket2);
                    if (Inferno.ItemCooldown(trinketId) == 0)
                    {
                        Inferno.Cast("Trinket 2", true);
                        return true;
                    }
                }
            }
            
            // Cooldowns with custom command check
            if (GetCheckBox("Use Cooldowns") && Inferno.IsCustomCodeOn("BURST"))
            {
                if (Inferno.CanCast("Combustion"))
                {
                    Inferno.Cast("Combustion");
                    return true;
                }
            }
            
            // AoE logic using Settings and custom commands
            string aoeMode = GetDropDown("AoE Mode");
            int aoeCount = GetSlider("AoE Enemy Count");
            int enemiesNearTarget = Inferno.EnemiesNearUnit(8, Units.Target);
            
            bool shouldAoE = false;
            if (aoeMode == "Always")
            {
                shouldAoE = true;
            }
            else if (aoeMode == "Auto")
            {
                shouldAoE = enemiesNearTarget >= aoeCount;
            }
            else if (Inferno.IsCustomCodeOn("AOE"))
            {
                shouldAoE = true;
            }
            
            if (shouldAoE && enemiesNearTarget >= 3)
            {
                if (Inferno.CanCast("Flamestrike"))
                {
                    Inferno.Cast("FlamestrikeGround");
                    return true;
                }
            }
            
            // Interrupt logic
            if (Inferno.IsInterruptable(Units.Target))
            {
                int castRemaining = Inferno.CastingRemaining(Units.Target);
                if (castRemaining > 0 && castRemaining < 500)
                {
                    Inferno.PrintMessage("Target casting: " + Inferno.CastingName(Units.Target), Color.Yellow);
                }
            }
            
            // Single target rotation with buff checking
            if (Inferno.HasBuff("Hot Streak", Units.Player))
            {
                if (Inferno.CanCast("Pyroblast"))
                {
                    Inferno.Cast("Pyroblast");
                    return true;
                }
            }
            
            // Off-GCD ability (QuickDelay = true)
            if (Inferno.CanCast("Fire Blast", Units.Target, true, false, true))
            {
                int charges = Inferno.SpellCharges("Fire Blast");
                if (charges > 1)
                {
                    Inferno.Cast("Fire Blast", true);
                    return true;
                }
            }
            
            // Filler spell
            if (Inferno.CanCast("Fireball"))
            {
                Inferno.Cast("Fireball");
                return true;
            }
            
            return false;
        }

        public override void CleanUp()
        {
            // Runs after every tick - could be used for cleanup tasks
        }

        public override void OnStop()
        {
            Inferno.PrintMessage("Example Mage Rotation Stopped", Color.Orange);
        }
    }
}
