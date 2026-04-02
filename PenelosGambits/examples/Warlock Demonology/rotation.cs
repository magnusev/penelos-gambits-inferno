using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API;

namespace InfernoWow.Modules
{
    /// <summary>
    /// Demonology Warlock - Translated from SimulationCraft Midnight APL
    /// Auto-detects hero tree: Diabolist (Diabolic Ritual) or Soul Harvester (Demonic Soul).
    /// Core: Demonic Tyrant window, Hand of Gul'dan shard spending, Demonic Core procs.
    /// </summary>
    public class DemonologyWarlockRotation : Rotation
    {
        List<string> Abilities = new List<string> {
            "Shadow Bolt", "Demonbolt", "Hand of Guldan", "Hand of Gul'dan", "Call Dreadstalkers",
            "Summon Demonic Tyrant", "Implosion", "Power Siphon",
            "Ruination", "Infernal Bolt", "Grimoire: Imp Lord",
            "Grimoire: Fel Ravager", "Summon Doomguard",
        };
        List<string> TalentChecks = new List<string> {
            "Diabolic Ritual", "Demonic Soul", "Reign of Tyranny",
            "Doom", "To Hell and Back", "Grimoire: Imp Lord",
        };
        List<string> DefensiveSpells = new List<string> { "Unending Resolve", "Dark Pact" };
        List<string> UtilitySpells = new List<string> { "Spell Lock", "Summon Pet" };

        const int HealthstoneItemID = 5512;
        const int SoulShardType = 7;
        private Random _rng = new Random();
        private int _lastCastingID = 0;
        private int _interruptTargetPct = 0;

        public override void LoadSettings()
        {
            Settings.Add(new Setting("=== Demonology Warlock ==="));
            Settings.Add(new Setting("Use Summon Demonic Tyrant", true));
            Settings.Add(new Setting("Use Trinkets", true));
            Settings.Add(new Setting("Implosion AoE threshold", 2, 10, 3));
            Settings.Add(new Setting("=== Defensives ==="));
            Settings.Add(new Setting("Use Defensives", true));
            Settings.Add(new Setting("Unending Resolve HP %", 1, 100, 40));
            Settings.Add(new Setting("Dark Pact HP %", 1, 100, 50));
            Settings.Add(new Setting("Healthstone HP %", 1, 100, 50));
            Settings.Add(new Setting("=== Interrupt ==="));
            Settings.Add(new Setting("Auto Interrupt", true));
            Settings.Add(new Setting("Interrupt at cast % (min)", 0, 100, 40));
            Settings.Add(new Setting("Interrupt at cast % (max)", 0, 100, 90));
        }

        public override void Initialize()
        {
            Inferno.PrintMessage("             //////////////////////////////////////", Color.DarkMagenta);
            Inferno.PrintMessage("             //   DEMONOLOGY - WARLOCK (MID)     //", Color.DarkMagenta);
            Inferno.PrintMessage("             //   DIABOLIST / SOUL HARVESTER     //", Color.DarkMagenta);
            Inferno.PrintMessage("             //              V 2.00              //", Color.DarkMagenta);
            Inferno.PrintMessage("             //////////////////////////////////////", Color.DarkMagenta);
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
            Macros.Add("pet_spell_lock", "/cast [@target] Command Demon");
            CustomFunctions.Add("HasHealthstone", "return GetItemCount(5512) > 0 and 1 or 0");
            CustomFunctions.Add("PetIsActive", "return (UnitExists('pet') and not UnitIsDead('pet')) and 1 or 0");
            // Racial abilities
            foreach (string r in new string[] { "Berserking", "Blood Fury", "Ancestral Call", "Fireblood", "Lights Judgment" }) Spellbook.Add(r);
            CustomCommands.Add("NoCDs"); CustomCommands.Add("nocds");
            CustomCommands.Add("ForceST"); CustomCommands.Add("forcest");
        }

        public override bool OutOfCombatTick()
        {
            if (Inferno.CustomFunction("PetIsActive") == 0 && Inferno.CanCast("Summon Pet"))
            { Inferno.Cast("Summon Pet"); return true; }
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
            if ((HasBuff("Demonic Tyrant")) && HandleRacials()) return true;

            bool isDiabolist = Inferno.IsSpellKnown("Diabolic Ritual");
            if (isDiabolist) return Diabolist();
            return SoulHarvest();
        }

        // =====================================================================
        // DIABOLIST (actions.diabolist)
        // =====================================================================
        bool Diabolist()
        {
            int shards = GetShards();
            int enemies = Inferno.EnemiesNearUnit(8f, "target");
            if (enemies < 1) enemies = 1; if ((Inferno.IsCustomCodeOn("ForceST") || Inferno.IsCustomCodeOn("forcest"))) enemies = 1;

            // power_siphon,if=buff.demonic_core.stack<=1
            if (Inferno.BuffStacks("Demonic Core") <= 1 && Cast("Power Siphon")) return true;
            // hand_of_guldan,if=buff.dominion_of_argus.up
            if (HasBuff("Dominion of Argus") && shards >= 1 && Cast("Hand of Gul'dan")) return true;
            // grimoire_imp_lord / fel_ravager / doomguard
            if (!HasBuff("Singe Magic") && Cast("Grimoire: Imp Lord")) return true;
            if (!HasBuff("Spell Lock") && Cast("Grimoire: Fel Ravager")) return true;
            if (!(Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds")) && CastNoRange("Summon Doomguard")) return true;
            // call_dreadstalkers
            if (Inferno.IsSpellKnown("Reign of Tyranny"))
            {
                int tyrantCD = Inferno.SpellCooldown("Summon Demonic Tyrant");
                if (tyrantCD >= 20000 || tyrantCD <= 12000)
                    if (Cast("Call Dreadstalkers")) return true;
            }
            else { if (Cast("Call Dreadstalkers")) return true; }
            // summon_demonic_tyrant,if=soul_shard=5
            if (shards >= 5 && CastCD("Summon Demonic Tyrant")) return true;
            // implosion,if=wild_imps>=6&(active_enemies>2|talent.to_hell_and_back)
            if (Inferno.BuffStacks("Wild Imp") >= 6 && (enemies > 2 || Inferno.IsSpellKnown("To Hell and Back")))
                if (Cast("Implosion")) return true;
            // ruination
            if (Cast("Ruination")) return true;
            // hand_of_guldan,if=soul_shard>=3&cooldown.summon_demonic_tyrant.remains>5|soul_shard=5
            if ((shards >= 3 && Inferno.SpellCooldown("Summon Demonic Tyrant") > 5000) || shards >= 5)
                if (Cast("Hand of Gul'dan")) return true;
            // infernal_bolt,if=soul_shard<3
            if (shards < 3 && Cast("Infernal Bolt")) return true;
            // demonbolt with doom — prioritize targets without Doom debuff
            if (shards < 4 && Inferno.BuffStacks("Demonic Core") >= 1 && Inferno.IsSpellKnown("Doom") && Inferno.DebuffRemaining("Doom") == 0)
                if (Cast("Demonbolt")) return true;
            // demonbolt with demonic_core
            if (shards < 4 && Inferno.BuffStacks("Demonic Core") >= 1 && Cast("Demonbolt")) return true;
            // hand_of_guldan
            if (Cast("Hand of Guldan")) return true;

            // shadow_bolt
            if (Cast("Shadow Bolt")) return true;
            // infernal_bolt fallback
            if (Cast("Infernal Bolt")) return true;
            return false;
        }

        // =====================================================================
        // SOUL HARVESTER (actions.soulharvest)
        // =====================================================================
        bool SoulHarvest()
        {
            int shards = GetShards();
            int enemies = Inferno.EnemiesNearUnit(8f, "target");
            if (enemies < 1) enemies = 1; if ((Inferno.IsCustomCodeOn("ForceST") || Inferno.IsCustomCodeOn("forcest"))) enemies = 1;

            if (Inferno.BuffStacks("Demonic Core") <= 1 && Cast("Power Siphon")) return true;
            if (HasBuff("Dominion of Argus") && shards >= 1 && Cast("Hand of Gul'dan")) return true;
            if (!HasBuff("Singe Magic") && Cast("Grimoire: Imp Lord")) return true;
            if (!HasBuff("Spell Lock") && Cast("Grimoire: Fel Ravager")) return true;
            if (!(Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds")) && CastNoRange("Summon Doomguard")) return true;
            // call_dreadstalkers (unconditional for SH)
            if (Cast("Call Dreadstalkers")) return true;
            // summon_demonic_tyrant (unconditional for SH — no shard gating)
            if (CastCD("Summon Demonic Tyrant")) return true;
            // implosion
            if (Inferno.BuffStacks("Wild Imp") >= 6 && (enemies > 2 || Inferno.IsSpellKnown("To Hell and Back")))
                if (Cast("Implosion")) return true;
            // hand_of_guldan (unconditional for SH — just needs shards to cast, CanCast handles it)
            if (Cast("Hand of Gul'dan")) return true;
            // infernal_bolt,if=soul_shard<3
            if (shards < 3 && Cast("Infernal Bolt")) return true;
            // demonbolt with doom — prioritize if target has no Doom debuff
            if (shards < 4 && Inferno.BuffStacks("Demonic Core") >= 1 && Inferno.IsSpellKnown("Doom") && Inferno.DebuffRemaining("Doom") == 0)
                if (Cast("Demonbolt")) return true;
            // demonbolt at 2+ stacks without doom
            if (shards < 4 && Inferno.BuffStacks("Demonic Core") >= 2 && !Inferno.IsSpellKnown("Doom"))
                if (Cast("Demonbolt")) return true;
            // demonbolt with any core
            if (shards < 4 && Inferno.BuffStacks("Demonic Core") >= 1 && Cast("Demonbolt"))
                return true;
            // shadow_bolt
            if (Cast("Shadow Bolt")) return true;
            return false;
        }

        // =====================================================================
        // DEFENSIVES / INTERRUPT / TRINKETS
        // =====================================================================
        bool HandleDefensives()
        {
            if (!GetCheckBox("Use Defensives")) return false;
            int hpPct = GetPlayerHealthPct();
            if (hpPct <= GetSlider("Unending Resolve HP %") && Inferno.CanCast("Unending Resolve", IgnoreGCD: true))
            { Inferno.Cast("Unending Resolve", QuickDelay: true); return true; }
            if (hpPct <= GetSlider("Dark Pact HP %") && Inferno.CanCast("Dark Pact", IgnoreGCD: true))
            { Inferno.Cast("Dark Pact", QuickDelay: true); return true; }
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
            if (castPct >= _interruptTargetPct && Inferno.CustomFunction("PetIsActive") == 1)
            { Inferno.Cast("pet_spell_lock", QuickDelay: true); _lastCastingID = 0; return true; }
            return false;
        }

        bool HandleTrinkets()
        {
            if (!GetCheckBox("Use Trinkets") || (Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;
            // pet.demonic_tyrant.active — Tyrant grants "Demonic Power" buff while summoned
            if (!HasBuff("Demonic Power")) return false;
            if (Inferno.CanUseEquippedItem(13)) { Inferno.Cast("trinket1"); return true; }
            if (Inferno.CanUseEquippedItem(14)) { Inferno.Cast("trinket2"); return true; }
            return false;
        }

        int GCD() { return Inferno.GCD(); }
        int GetShards()
        {
            int shards = Inferno.Power("player", 7) / 10;
            string casting = Inferno.CastingName("player");
            if (casting == "Shadow Bolt") shards += 1;
            if (casting == "Demonbolt") shards += 2;
            if (casting == "Hand of Gul'dan") shards -= 3;
            return Math.Max(0, Math.Min(shards, 5));
        }
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
        bool CastNoRange(string name) { if (Inferno.CanCast(name, CheckRange: false)) { Inferno.Cast(name); Inferno.PrintMessage(">> " + name, Color.White); return true; } return false; }
        bool CastCD(string name)
        {
            if ((Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;
            if (name == "Summon Demonic Tyrant" && !GetCheckBox("Use Summon Demonic Tyrant")) return false;
            if (Inferno.CanCast(name)) { Inferno.Cast(name); Inferno.PrintMessage(">> " + name, Color.White); return true; } return false;
        }
    }
}
