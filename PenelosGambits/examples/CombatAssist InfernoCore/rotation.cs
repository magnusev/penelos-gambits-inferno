using System.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API; //needed to access Inferno API
using System.IO;

namespace InfernoWow.Modules
{

    public class OneButtonRotation : Rotation
    {
        static List<int> InterruptableChannels = new List<int>() {
        48045, //mind sear
        };

        static List<string> SpellDataClasses = new List<string>()
        {
                "Shadow_Priest",
                "Marksman_Hunter",
                "Beast_Hunter",
                "Survival_Hunter",
                "Feral_Druid",
                "Guardian_Druid",
                "Balance_Druid" ,
                "Unholy_Deathknight",
                "Frost_Deathknight",
                "Blood_Deathknight",
                "Havoc_Demonhunter",
                "Vengeance_Demonhunter",
                "Devourer_Demonhunter",
                "Fire_Mage",
                "Arcane_Mage",
                "Frost_Mage",
                "Windwalker_Monk",
                "Brewmaster_Monk",
                "Ret_Paladin",
                "Protection_Paladin",
                "Outlaw_Rogue",
                "Subtlety_Rogue",
                "Assassination_Rogue",
                "Enhancement_Shaman",
                "Elemental_Shaman",
                "Fury_Warrior",
                "Arms_Warrior",
                "Protection_Warrior",
                "Demonology_Warlock",
                "Destruction_Warlock",
                "Affliction_Warlock",
                "Devastation_Evoker",
        };

        static List<string> MeleeClasses = new List<string>()
        {
                "Survival_Hunter",
                "Feral_Druid",
                "Guardian_Druid",
                "Unholy_Deathknight",
                "Frost_Deathknight",
                "Blood_Deathknight",
                "Havoc_Demonhunter",
                "Vengeance_Demonhunter",
                "Windwalker_Monk",
                "Brewmaster_Monk",
                "Ret_Paladin",
                "Protection_Paladin",
                "Outlaw_Rogue",
                "Subtlety_Rogue",
                "Assassination_Rogue",
                "Enhancement_Shaman",
                "Fury_Warrior",
                "Arms_Warrior",
                "Protection_Warrior",
        };
        Dictionary<int, string> HekiliSpells = new Dictionary<int, string>();
        List<int> OffGCD = new List<int>();
        List<int> GroundSpell = new List<int>();
        List<int> FocusSpell = new List<int>();
        List<int> PlayerSpell = new List<int>();
        List<int> RacialSpell = new List<int>();
        List<int> TargetSpell = new List<int>();



        string Class = "";
        string Display = "Primary";
        string SelfHeal = "";
        bool CombatCheck = false;
        int InCombatRange = 40;

        public override void LoadSettings()
        {
            Settings.Add(new Setting("One Button Rotation"));
            Settings.Add(new Setting("Specialization:", SpellDataClasses, "Shadow_Priest"));
            Settings.Add(new Setting("Combat Check", false));
            Settings.Add(new Setting("Healthstone %", 0, 100, 35));
            Settings.Add(new Setting("Health Potion %", 0, 100, 25));

            string spellDataDir = Path.Combine(Inferno.GetRotationDirectory(), "spelldata");

            foreach (string s in SpellDataClasses)
            {
                string filePath = Path.Combine(spellDataDir, s + ".txt");
                if (!File.Exists(filePath))
                {
                    try
                    {
                        File.Create(filePath).Close();
                    }
                    catch { }
                }
            }
        }

        public void GenerateSpells()
        {

            // Class specific fixes
            Macros.Add("FixDivineStorm", "/cast Divine Storm");
            Macros.Add("FixImmoAura", "/cast Immolation Aura");
            Macros.Add("RaiseDead", "/cast Raise Dead");

            Macros.Add("VanishChannelMacro", "/cast [nochanneling] Vanish");
            Macros.Add("SoulSunderFix", "/cast Soul Cleave");

            // Addon specific spells
            Macros.Add("Use HS", "/use Healthstone");

            Macros.Add("Use Heal Pot", "/use Invigorating Healing Potion");

            Spellbook.Add("Soul Cleave");

            // Talents
            foreach (var t in new[] { "Plague Leech", "Blood Tap" }) Spellbook.Add(t);


            string line;
            string spellDataPath = Path.Combine(Inferno.GetRotationDirectory(), "spelldata", Class + ".txt");
            StreamReader file = new StreamReader(spellDataPath);


            while ((line = file.ReadLine()) != null)
            {
                if (!(line.Contains("=") && line.Contains("-")))
                    continue;

                string SpellName = "";
                bool GCD = false;
                bool Ground = false;
                bool Focus = false;
                bool Player = false;
                bool Racial = false;
                bool Target = false;

                string[] tmp = line.Split('=');

                if (tmp.Length > 1)
                {
                    SpellName = tmp[0];
                    Spellbook.Add(SpellName);

                    string[] tmp2 = tmp[1].Split('-');

                    if (tmp2.Length > 1)
                    {
                        if (tmp2[1].Contains("gcd"))
                            GCD = true;
                        if (tmp2[1].Contains("ground") || tmp2[1].Contains("cursor"))
                            Ground = true;
                        else if (tmp2[1].Contains("focus"))
                            Focus = true;
                        else if (tmp2[1].Contains("player"))
                            Player = true;
                        else if (tmp2[1].Contains("racial"))
                            Racial = true;
                        else if (tmp2[1].Contains("target"))
                            Target = true;
                    }
                    if (tmp2.Length > 0)
                    {
                        string[] tmp3 = tmp2[0].Split(',');

                        foreach (string id in tmp3)
                        {
                            int spellid = int.Parse(id);
                            bool alreadyAdded = HekiliSpells.ContainsValue(SpellName);
                            HekiliSpells.Add(spellid, SpellName);

                            if (Ground)
                            {
                                GroundSpell.Add(spellid);
                                if (!alreadyAdded)
                                    Macros.Add(SpellName + "C", "/cast [@cursor] " + SpellName);
                            }
                            else if (Focus)
                            {
                                FocusSpell.Add(spellid);
                                if (!alreadyAdded)
                                    Macros.Add(SpellName + "F", "/cast [@focus] " + SpellName);
                            }
                            else if (Player)
                            {
                                PlayerSpell.Add(spellid);
                                if (!alreadyAdded)
                                    Macros.Add(SpellName + "P", "/cast [@player] " + SpellName);
                            }
                            else if (Racial)
                            {
                                RacialSpell.Add(spellid);
                                if (!alreadyAdded)
                                    Macros.Add(SpellName + "R", "/cast " + SpellName);
                            }
                            else if (Target)
                            {
                                TargetSpell.Add(spellid);
                                if (!alreadyAdded)
                                    Macros.Add(SpellName + "T", "/cast [@target] " + SpellName);
                            }

                            if (GCD)
                                OffGCD.Add(spellid);
                        }
                    }
                }
            }

            file.Close();
        }

        public override void Initialize()
        {
            //Inferno.DebugMode();
            Inferno.PrintMessage("Loading InfernoCore rotation...");
            Class = GetDropDown("Specialization:");
            CombatCheck = GetCheckBox("Combat Check");
            InCombatRange = MeleeClasses.Contains(Class) ? 8 : 40;

            GenerateSpells();

            Inferno.PrintMessage("One Button Rotation " + Class + " for Inferno", Color.Purple);

            Inferno.PrintMessage("You need this macro to start the rotation:", Color.Blue);
            Inferno.PrintMessage("/xxxxx Start", Color.Blue);
            Inferno.PrintMessage("--Can be used in or out of combat", Color.Blue);
            Inferno.PrintMessage(" ");
            Inferno.PrintMessage("--Replace xxxxx with first 5 letters of your addon, lowercase.", Color.Blue);

            Inferno.Latency = 75;
            Inferno.QuickDelay = 150;
            Inferno.SlowDelay = 333;

            Macros.Add("PopTrinkets", "/use 13\\n/use 14");
            Macros.Add("UseWeap", "/use 16");

            CustomCommands.Add("Start");
            CustomCommands.Add("start");

            CustomFunctions.Add("HekiliID", "local nextSpell = C_AssistedCombat.GetNextCastSpell() if nextSpell ~= 0 and nextSpell ~= nil then return nextSpell else return 9000000 end");

        }


        // optional override for the CombatTick which executes while in combat
        public override bool CombatTick()
        {
            // Custom commands
            bool Start = Inferno.IsCustomCodeOn("Start") || Inferno.IsCustomCodeOn("start");
            int HekiliID = Inferno.CustomFunction("HekiliID");
            bool IsChanneling = Inferno.IsChanneling("player");

            if (Start)
            {
                // PROTECTION GLOBALE: Si on est en train de channeler, on ne fait rien
                // SAUF si le sort en cours est dans la liste des channels interruptibles
                if (IsChanneling)
                {
                    bool canInterrupt = false;
                    foreach (int interruptableID in InterruptableChannels)
                    {
                        if (HekiliID == interruptableID)
                        {
                            canInterrupt = true;
                            break;
                        }
                    }

                    if (!canInterrupt)
                    {
                        return false; // On ne cast rien si on channel et que ce n'est pas interruptible
                    }
                }

                if (HekiliID == 46585)
                {
                    Inferno.Cast("RaiseDead");
                    return true;
                }

                if (HekiliID == 1856)
                {
                    Inferno.Cast("VanishChannelMacro");
                    return true;
                }

                if (HekiliID == 258920)
                {
                    Inferno.Cast("FixImmoAura");
                    return true;
                }

                if (HekiliID == 53385)
                {
                    Inferno.Cast("FixDivineStorm");
                    return true;
                }

                if (HekiliID > 0)
                {
                    bool QuickCast = OffGCD.Contains(HekiliID);
                    if (HekiliSpells.ContainsKey(HekiliID))
                    {
                        if (QuickCast || true)
                        {
                            if (GroundSpell.Contains(HekiliID))
                            {
                                Inferno.Cast(HekiliSpells[HekiliID] + "C", QuickCast);
                                return true;
                            }
                            else if (FocusSpell.Contains(HekiliID))
                            {
                                Inferno.Cast(HekiliSpells[HekiliID] + "F", QuickCast);
                                return true;
                            }
                            else if (PlayerSpell.Contains(HekiliID))
                            {
                                Inferno.Cast(HekiliSpells[HekiliID] + "P", QuickCast);
                                return true;
                            }
                            else if (RacialSpell.Contains(HekiliID))
                            {
                                Inferno.Cast(HekiliSpells[HekiliID] + "R", QuickCast);
                                return true;
                            }
                            else if (TargetSpell.Contains(HekiliID))
                            {
                                Inferno.Cast(HekiliSpells[HekiliID] + "T", QuickCast);
                                return true;
                            }
                            else
                            {
                                Inferno.Cast(HekiliSpells[HekiliID], QuickCast);
                                return true;
                            }
                        }

                        return false;
                    }
                    else
                        Inferno.PrintMessage("Could not find spelldata definition for Spell ID: " + HekiliID.ToString());
                }

            }

            return false;
        }


        public override bool OutOfCombatTick()
        {
            if (!CombatCheck)
                return CombatTick();

            return false;
        }




    }
}