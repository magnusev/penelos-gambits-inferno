using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API;

namespace InfernoWow.Modules
{
    /// <summary>
    /// Devourer Demon Hunter - Translated from SimulationCraft Midnight APL
    /// New hero spec with unique abilities: Void Ray, Voidblade, Collapsing Star,
    /// Pierce the Veil, Devour, Consume, Reap/Cull/Eradicate finishers.
    /// Void Metamorphosis stack management, Moment of Craving windows.
    /// </summary>
    public class DevourerDemonHunterRotation : Rotation
    {
        List<string> Abilities = new List<string> {
            "Void Ray", "Voidblade", "Pierce the Veil", "Collapsing Star",
            "Devour", "Consume", "Reap", "Cull", "Eradicate",
            "The Hunt", "Metamorphosis",
            "Soul Immolation", "Hungering Slash", "Reaper's Toll",
            "Predator's Wake",
        };
        List<string> TalentChecks = new List<string> {
            "Eradicate", "Voidsurge", "Devourer's Bite", "Voidfall",
            "Moment of Craving", "Collapsing Star", "Voidrush",
            "Star Fragments", "Emptiness", "Dark Matter",
            "Duty Eternal", "Hungering Slash",
        };
        List<string> DefensiveSpells = new List<string> { "Blur", "Darkness" };
        List<string> UtilitySpells = new List<string> { "Disrupt" };

        const int HealthstoneItemID = 5512;
        private Random _rng = new Random();
        private int _lastCastingID = 0;
        private int _interruptTargetPct = 0;

        public override void LoadSettings()
        {
            Settings.Add(new Setting("=== Devourer Demon Hunter ==="));
            Settings.Add(new Setting("=== Offensive Cooldowns ==="));
            Settings.Add(new Setting("Use Metamorphosis", true));
            Settings.Add(new Setting("Use The Hunt", true));
            Settings.Add(new Setting("Use Collapsing Star", true));
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
            Inferno.PrintMessage("             //////////////////////////////////////", Color.DarkViolet);
            Inferno.PrintMessage("             //   DEVOURER - DEMON HUNTER (MID)  //", Color.DarkViolet);
            Inferno.PrintMessage("             //              V 1.00              //", Color.DarkViolet);
            Inferno.PrintMessage("             //////////////////////////////////////", Color.DarkViolet);
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
            if (HandleInterrupt()) return true;
            if (!Inferno.UnitCanAttack("player", "target")) return false;
            if (Inferno.IsChanneling("player")) return false;

            // Don't interrupt cast-time trinkets
            string _castName = Inferno.CastingName("player");
            if (_castName.Contains("Puzzle Box") || _castName.Contains("Emberwing")) return false;

            int enemies = Inferno.EnemiesNearUnit(8f, "player");
            if (enemies < 1) enemies = 1; if ((Inferno.IsCustomCodeOn("ForceST") || Inferno.IsCustomCodeOn("forcest"))) enemies = 1;
            bool inMeta = HasBuff("Metamorphosis");
            bool hasEradicate = Inferno.IsSpellKnown("Eradicate");
            bool hasVoidsurge = Inferno.IsSpellKnown("Voidsurge");
            bool hasDevourersBite = Inferno.IsSpellKnown("Devourer's Bite");
            bool hasMoC = HasBuff("Moment of Craving");
            bool eradicateUp = Inferno.HasBuff("Eradicate");
            bool voidMetaMaxStacks = Inferno.BuffStacks("Void Metamorphosis") >= 50;

            // Trinkets during Meta
                        if (HandleTrinkets(inMeta)) return true;

            // Racial damage CDs
            if ((HasBuff("Metamorphosis")) && HandleRacials()) return true;

            // APL: Smuggle an Eradicate into Meta if on AoE
            if (hasEradicate && enemies > 1 && !eradicateUp)
                if (Cast("Void Ray")) return true;

            // pierce_the_veil,if=buff.moment_of_craving.up&action.collapsing_star.ready
            if (hasMoC && Inferno.SpellCooldown("Collapsing Star") <= GCDMAX())
                if (Cast("Pierce the Veil")) return true;

            // voidblade,if=void_meta_max_stacks&talent.devourers_bite&talent.voidsurge
            if (voidMetaMaxStacks && hasDevourersBite && hasVoidsurge)
                if (Cast("Voidblade")) return true;

            // the_hunt,if=void_meta_max_stacks&talent.devourers_bite&talent.voidsurge
            if (voidMetaMaxStacks && hasDevourersBite && hasVoidsurge)
                if (CastCD("The Hunt")) return true;

            // metamorphosis,if=buff.eradicate.up|!talent.eradicate|active_enemies=1
            if (eradicateUp || !hasEradicate || enemies == 1)
                if (CastCD("Metamorphosis")) return true;

            // Reaps during Meta with Moment of Craving (pre-Voidfall)
            if (Inferno.IsSpellKnown("Moment of Craving") && inMeta && !Inferno.IsSpellKnown("Voidfall")
                && Inferno.SpellCooldown("Void Ray") <= GCDMAX())
                if (HandleReaps()) return true;

            // void_ray,if=!buff.eradicate.up|active_enemies=1
            if (!eradicateUp || enemies == 1)
                if (Cast("Void Ray")) return true;

            // voidblade,if=buff.moment_of_craving.up&(collapsing_star_stacking max or near max)&talent.devourers_bite
            if (hasMoC && hasDevourersBite && Inferno.BuffStacks("Collapsing Star") + Inferno.BuffStacks("Soul Fragments") >= 30)
                if (Cast("Voidblade")) return true;

            // collapsing_star - use when conditions met
            if (GetCheckBox("Use Collapsing Star") && Inferno.IsMoving("player") && ShouldUseCollapsingStar(enemies))
            {
                if (!Inferno.IsSpellKnown("Voidrush") || (!HasBuff("Hungering Slash") && Inferno.SpellCooldown("Voidblade") >= 6000))
                    if (CastQuick("Collapsing Star")) return true;
            }

            // Reaps - Meta Cull line
            if (inMeta && !Inferno.IsSpellKnown("Voidfall"))
                if (HandleReaps()) return true;

            // Reaps - Annihilator line (Voidfall stacks or Eradicate AoE)
            if (Inferno.BuffStacks("Voidfall") >= 3 || (eradicateUp && enemies > 1))
                if (HandleReaps()) return true;

            // Melee combo
            if (HandleMeleeCombo(enemies, inMeta)) return true;

            // soul_immolation in AoE outside Meta
            if (enemies > 1 && !inMeta && Inferno.DebuffRemaining("Soul Immolation") < GCD())
                if (Cast("Soul Immolation")) return true;

            // Collapsing Star fallback
            if (GetCheckBox("Use Collapsing Star") && Inferno.IsMoving("player") && ShouldUseCollapsingStar(enemies))
                if (CastQuick("Collapsing Star")) return true;

            // Reaps outside Meta with Moment of Craving
            if (!inMeta && hasMoC)
                if (HandleReaps()) return true;

            // devour
            if (Cast("Devour")) return true;

            // consume
            if (Cast("Consume")) return true;

            // Void Ray as absolute filler — never sit idle
            if (Cast("Void Ray")) return true;

            return false;
        }

        // =====================================================================
        // REAPS (actions.reaps)
        // =====================================================================
        bool HandleReaps()
        {
            // APL: eradicate → cull → reap (all are castable spells)
            if (Cast("Eradicate")) return true;
            if (Cast("Cull")) return true;
            if (Cast("Reap")) return true;
            return false;
        }

        // =====================================================================
        // MELEE COMBO (actions.melee_combo)
        // =====================================================================
        bool HandleMeleeCombo(int enemies, bool inMeta)
        {
            // vengeful_retreat with voidstep buff

            // hungering_slash,if=active_enemies>1
            if (enemies > 1 && Cast("Hungering Slash")) return true;

            // reapers_toll,if=buff.voidsurge.up|active_enemies>1
            if (HasBuff("Voidsurge") || enemies > 1)
                if (Cast("Reaper's Toll")) return true;

            // the_hunt - non-voidsurge/non-devourers_bite, or devourers_bite without voidsurge during meta
            if (!Inferno.IsSpellKnown("Voidsurge") && !Inferno.IsSpellKnown("Devourer's Bite"))
                if (CastCD("The Hunt")) return true;
            if (Inferno.IsSpellKnown("Devourer's Bite") && !Inferno.IsSpellKnown("Voidsurge") && inMeta)
                if (CastCD("The Hunt")) return true;

            // pierce_the_veil with voidsurge or various talent conditions
            if (HasBuff("Voidsurge") || Inferno.IsSpellKnown("Duty Eternal") && EnemiesNear() == 1
                || Inferno.IsSpellKnown("Devourer's Bite") || (Inferno.IsSpellKnown("Hungering Slash") && EnemiesNear() > 1))
                if (Cast("Pierce the Veil")) return true;

            // predators_wake
            if (Cast("Predator's Wake")) return true;

            // voidblade - various conditions
            if ((Inferno.IsSpellKnown("Duty Eternal") && EnemiesNear() == 1) || (Inferno.IsSpellKnown("Hungering Slash") && EnemiesNear() > 1))
            {
                if (!Inferno.IsSpellKnown("Devourer's Bite"))
                    if (Cast("Voidblade")) return true;
            }
            if (Inferno.IsSpellKnown("Devourer's Bite") && !Inferno.IsSpellKnown("Voidsurge") && inMeta)
                if (Cast("Voidblade")) return true;

            return false;
        }

        // =====================================================================
        // SHOULD USE COLLAPSING STAR
        // =====================================================================
        bool ShouldUseCollapsingStar(int enemies)
        {
            if (!Inferno.IsSpellKnown("Collapsing Star")) return false;
            return enemies > 1
                || HasBuff("Dark Matter")
                || (Inferno.IsSpellKnown("Star Fragments") && Inferno.IsSpellKnown("Emptiness"));
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

        bool HandleTrinkets(bool inMeta)
        {
            if (!GetCheckBox("Use Trinkets") || (Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;
            if (!inMeta) return false;
            if (Inferno.CanUseEquippedItem(13)) { Inferno.Cast("trinket1"); return true; }
            if (Inferno.CanUseEquippedItem(14)) { Inferno.Cast("trinket2"); return true; }
            return false;
        }

        // =====================================================================
        // HELPERS
        // =====================================================================
        int GCD() { return Inferno.GCD(); }
        int GCDMAX() { int g = (int)(1500f / (1f + Inferno.Haste("player") / 100f)); return g < 750 ? 750 : g; }
        int EnemiesNear() { int e = Inferno.EnemiesNearUnit(8f, "player"); return e < 1 ? 1 : e; }
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

        bool Cast(string name)
        {
            if (Inferno.CanCast(name)) { Inferno.Cast(name); Inferno.PrintMessage(">> " + name, Color.White); return true; }
            return false;
        }

        bool CastQuick(string name)
        {
            if (Inferno.CanCast(name)) { Inferno.Cast(name, QuickDelay: true); Inferno.PrintMessage(">> " + name, Color.White); return true; }
            return false;
        }

        bool CastCD(string name)
        {
            if ((Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;
            if (name == "Metamorphosis" && !GetCheckBox("Use Metamorphosis")) return false;
            if (name == "The Hunt" && !GetCheckBox("Use The Hunt")) return false;
            if (Inferno.CanCast(name)) { Inferno.Cast(name); Inferno.PrintMessage(">> " + name, Color.White); return true; }
            return false;
        }
    }
}
