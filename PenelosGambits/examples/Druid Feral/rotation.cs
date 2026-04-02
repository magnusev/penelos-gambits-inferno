using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API;

namespace InfernoWow.Modules
{
    /// <summary>
    /// Feral Druid - Translated from SimulationCraft Midnight APL
    /// Sub-rotations: cooldown, finisher, aoe_finisher, builder, aoe_builder
    /// Hero trees: Druid of the Claw vs Wildstalker (auto-detected)
    /// Core: Rake/Rip snapshot management, Tiger's Fury windows, Berserk/Incarnation,
    /// Combo Point spending (Ferocious Bite/Primal Wrath), Apex Predator procs,
    /// Moonfire (Lunar Inspiration), Convoke the Spirits timing.
    /// </summary>
    public class FeralDruidRotation : Rotation
    {
        List<string> Abilities = new List<string> {
            "Shred", "Rake", "Rip", "Ferocious Bite", "Moonfire",
            "Swipe", "Thrash", "Primal Wrath", "Tiger's Fury",
            "Berserk", "Incarnation: Avatar of Ashamane",
            "Feral Frenzy", "Frantic Frenzy", "Convoke the Spirits",
            "Chomp",
        };
        List<string> TalentChecks = new List<string> {
            "Incarnation: Avatar of Ashamane", "Convoke the Spirits",
            "Lunar Inspiration", "Primal Wrath", "Rampant Ferocity",
            "Saber Jaws", "Panther's Guile", "Double-Clawed Rake",
            "Wild Slashes", "Infected Wounds", "Frantic Frenzy",
            "Ashamane's Guidance", "Fluid Form", "Apex Predator's Craving","Bloodseeker Vines"
        };
        List<string> DefensiveSpells = new List<string> { "Survival Instincts", "Barkskin" };
        List<string> UtilitySpells = new List<string> { "Skull Bash", "Mark of the Wild" };
        const int HealthstoneItemID = 5512;
        const int EnergyPowerType = 3;
        const int CPPowerType = 4;
        private Random _rng = new Random(); private int _lastCastingID = 0; private int _interruptTargetPct = 0;

        public override void LoadSettings()
        {
            Settings.Add(new Setting("=== Feral Druid ==="));
            Settings.Add(new Setting("Use Berserk/Incarnation", true));
            Settings.Add(new Setting("Use Convoke the Spirits", true));
            Settings.Add(new Setting("Use Trinkets", true));
            Settings.Add(new Setting("=== Defensives ==="));
            Settings.Add(new Setting("Use Defensives", true));
            Settings.Add(new Setting("Survival Instincts HP %", 1, 100, 30));
            Settings.Add(new Setting("Barkskin HP %", 1, 100, 50));
            Settings.Add(new Setting("Healthstone HP %", 1, 100, 50));
            Settings.Add(new Setting("=== Interrupt ==="));
            Settings.Add(new Setting("Auto Interrupt", true));
            Settings.Add(new Setting("Interrupt at cast % (min)", 0, 100, 40));
            Settings.Add(new Setting("Interrupt at cast % (max)", 0, 100, 90));
        }

        public override void Initialize()
        {
            Inferno.PrintMessage("             //////////////////////////////////////", Color.Orange);
            Inferno.PrintMessage("             //    FERAL - DRUID (MID) V2.00     //", Color.Orange);
            Inferno.PrintMessage("             //////////////////////////////////////", Color.Orange);
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
            // Mark of the Wild
            if (Inferno.BuffRemaining("Mark of the Wild") < 60000 && Inferno.CanCast("Mark of the Wild"))
            { Inferno.Cast("Mark of the Wild"); return true; }
            // Cat Form
            if (!InAnyForm() && Inferno.CanCast("Cat Form"))
            { Inferno.Cast("Cat Form"); return true; }
            return false;
        }

        public override bool CombatTick()
        {
            if (Inferno.IsDead("player") || Inferno.IsGhost("player")) return false;
            if (HandleDefensives()) return true;
            if (HandleInterrupt()) return true;
            if (Inferno.IsChanneling("player")) return false;

            // Don't interrupt cast-time trinkets
            string _castName = Inferno.CastingName("player");
            if (_castName.Contains("Puzzle Box") || _castName.Contains("Emberwing")) return false;

            // Cat Form check
            if (!Inferno.HasBuff("Cat Form") && !Inferno.IsSpellKnown("Fluid Form"))
                if (Inferno.CanCast("Cat Form")) { Inferno.Cast("Cat Form"); return true; }

            if (!Inferno.UnitCanAttack("player", "target")) return false;

            int enemies = Inferno.EnemiesNearUnit(8f, "target");
            if (enemies < 1) enemies = 1; if ((Inferno.IsCustomCodeOn("ForceST") || Inferno.IsCustomCodeOn("forcest"))) enemies = 1;
            int cp = Inferno.Power("player", CPPowerType);
            int energy = Inferno.Power("player", EnergyPowerType);
            bool tfUp = HasBuff("Tiger's Fury");
            bool berserkUp = HasBuff("Berserk") || HasBuff("Incarnation: Avatar of Ashamane");
            bool isDotC = !IsWildstalker();
            bool isWS = IsWildstalker();

            // Tiger's Fury on CD — fire when: FF not known, or FF ready (CD=0), or FF on long CD, or ST
            int ffCD = Inferno.SpellCooldown("Frantic Frenzy");
            if (!(Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds")) && (!Inferno.IsSpellKnown("Frantic Frenzy") || ffCD == 0 || ffCD < Inferno.BuffDuration("Tiger's Fury") - 1500 || ffCD > 22000 || enemies == 1))
                if (Cast("Tiger's Fury")) return true;

            // Rake from stealth
            if (Inferno.HasBuff("Prowl") && Cast("Rake")) return true;

            // Chomp if enabler is up
            if (HasBuff("Chomp Enabler") && Cast("Chomp")) return true;

            // Cooldowns
            if (HandleCooldowns(cp, berserkUp, tfUp)) return true;

            // Apex Predator's Craving proc → instant Ferocious Bite
            if (HasBuff("Apex Predator's Craving"))
                if (Cast("Ferocious Bite")) return true;

            // Trinkets during Berserk
                        if (HandleTrinkets(berserkUp, tfUp)) return true;

            // Racial damage CDs
            if ((HasBuff("Berserk") || HasBuff("Incarnation: Avatar of Ashamane")) && HandleRacials()) return true;

            // Finishers / Builders routing
            if (enemies == 1)
            {
                if (Finisher(cp, energy, tfUp, berserkUp)) return true;
                if (cp <= 4)
                    if (Builder(cp, energy, tfUp, berserkUp, isDotC)) return true;
            }
            else
            {
                if (AoEFinisher(cp, energy, enemies, berserkUp, isWS)) return true;
                if (cp <= 4)
                    if (AoEBuilder(cp, energy, enemies, berserkUp, tfUp, isDotC, isWS)) return true;
            }

            return false;
        }

        // =====================================================================
        // COOLDOWNS (actions.cooldown)
        // =====================================================================
        bool HandleCooldowns(int cp, bool berserkUp, bool tfUp)
        {
            if ((Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;

            // Berserk / Incarnation: during Tiger's Fury
            if (GetCheckBox("Use Berserk/Incarnation") && tfUp && !berserkUp)
            {
                if (Inferno.IsSpellKnown("Incarnation: Avatar of Ashamane"))
                { if (Inferno.CanCast("Incarnation: Avatar of Ashamane")) { Inferno.Cast("Incarnation: Avatar of Ashamane"); Inferno.PrintMessage(">> Incarnation: Avatar of Ashamane", Color.White); return true; } }
                else
                { if (Inferno.CanCast("Berserk")) { Inferno.Cast("Berserk"); Inferno.PrintMessage(">> Berserk", Color.White); return true; } }
            }

            // Feral Frenzy / Frantic Frenzy
            if (!Inferno.IsSpellKnown("Frantic Frenzy"))
            {
                // feral_frenzy at 0-2 CP (0-4 during berserk)
                if (cp <= 2 + (berserkUp ? 2 : 0))
                    if (Cast("Feral Frenzy")) return true;
            }
            else
            {
                // frantic_frenzy: during TF in AoE, or low CP in ST
                int enemies = Inferno.EnemiesNearUnit(8f, "target");
                if (tfUp && enemies >= 2)
                    if (Cast("Frantic Frenzy")) return true;
                if (enemies == 1 && cp <= 2 + (berserkUp ? 2 : 0))
                    if (Cast("Frantic Frenzy")) return true;
            }

            // Convoke the Spirits: during Berserk when it's about to expire, or with TF + after Rip/Bite
            if (GetCheckBox("Use Convoke the Spirits"))
            {
                if (berserkUp && Inferno.BuffRemaining("Berserk") < 5000)
                    if (Cast("Convoke the Spirits")) return true;
                if (berserkUp && Inferno.BuffRemaining("Incarnation: Avatar of Ashamane") < 5000)
                    if (Cast("Convoke the Spirits")) return true;
                if (tfUp && !berserkUp)
                    if (Cast("Convoke the Spirits")) return true;
            }

            return false;
        }

        // =====================================================================
        // FINISHER — Single Target (actions.finisher)
        // =====================================================================
        bool Finisher(int cp, int energy, bool tfUp, bool berserkUp)
        {
            if (cp < 4) return false;

            // rip,if=refreshable&(tiger's_fury up or rip will fall off before next TF)
            int ripRemains = Inferno.DebuffRemaining("Rip");
            int tfCD = Inferno.SpellCooldown("Tiger's Fury");
            if (ripRemains < 7200 && (tfUp || ripRemains < tfCD))
                if (Cast("Rip")) return true;

            // ferocious_bite at 4+ CP during berserk, or 4+ without saber jaws
            if (berserkUp || !Inferno.IsSpellKnown("Saber Jaws"))
            {
                if (cp >= 4)
                    if (Cast("Ferocious Bite")) return true;
            }

            // ferocious_bite at 5 CP (or 4 with Panther's Guile)
            int biteThreshold = 5 - (Inferno.IsSpellKnown("Panther's Guile") ? 1 : 0);
            if (!berserkUp && cp >= biteThreshold)
            {
                // Pool to 50 energy for max damage Bite when possible
                if (energy >= 50)
                    if (Cast("Ferocious Bite")) return true;
                // pool_resource,for_next=1 — wait for energy, don't fall through to builders
                return true;
            }

            return false;
        }

        // =====================================================================
        // AOE FINISHER (actions.aoe_finisher)
        // =====================================================================
        bool AoEFinisher(int cp, int energy, int enemies, bool berserkUp, bool isWS)
        {
            if (cp < 4) return false;
            bool hasPW = Inferno.IsSpellKnown("Primal Wrath");
            bool hasRampant = Inferno.IsSpellKnown("Rampant Ferocity");
            bool ravageUp = HasBuff("Ravage");
            int pwRemains = Inferno.DebuffRemaining("Rip"); // Primal Wrath refreshes Rip

            // primal_wrath in pandemic during berserk, or at <6.5s remaining
            if (hasPW && cp >= 5 && enemies > 1)
            {
                if ((pwRemains < 6500 && !berserkUp) || (Inferno.DebuffRemaining("Rip") < 7200 && berserkUp))
                    if (Cast("Primal Wrath")) return true;
            }

            // ferocious_bite with ravage (no PW)
            if (ravageUp && cp >= 4 && !hasPW && enemies >= 2 + (hasRampant ? 0 : 3))
                if (Cast("Ferocious Bite")) return true;

            // rip manually if no primal_wrath — only on current target if very low
            if (!hasPW && cp >= 4 && Inferno.DebuffRemaining("Rip") < 3000)
                if (Cast("Rip")) return true;

            // ferocious_bite with rampant_ferocity or ravage or bloodseeker_vines
            if (cp >= 4 + (hasPW ? 1 : 0))
            {
                if (hasRampant || ravageUp || Inferno.DebuffRemaining("Bloodseeker Vines") > 0)
                    if (Cast("Ferocious Bite")) return true;
            }

            // fallback primal_wrath at 5 CP
            if (hasPW && cp >= 5)
                if (Cast("Primal Wrath")) return true;

            // fallback ferocious_bite
            if (cp >= 4 + (hasPW ? 1 : 0))
                if (Cast("Ferocious Bite")) return true;

            return false;
        }

        // =====================================================================
        // BUILDER — Single Target (actions.builder)
        // =====================================================================
        bool Builder(int cp, int energy, bool tfUp, bool berserkUp, bool isDotC)
        {
            // Rake: refresh during TF, or when low/upgrade snapshot
            int rakeRemains = Inferno.DebuffRemaining("Rake");
            if (tfUp && (rakeRemains < 4500 || rakeRemains < 2000))
                if (Cast("Rake")) return true;
            if (rakeRemains < 2000)
                if (Cast("Rake")) return true;

            // Moonfire (requires Lunar Inspiration to cast in Cat Form)
            if (Inferno.IsSpellKnown("Lunar Inspiration"))
            {
                int mfRemains = Inferno.DebuffRemaining("Moonfire");
                if (tfUp && (mfRemains < 4500 || mfRemains < 2000))
                    if (Cast("Moonfire")) return true;
                if (mfRemains < 2000)
                    if (Cast("Moonfire")) return true;
            }

            // Shred as primary builder
            if (Cast("Shred")) return true;
            return false;
        }

        // =====================================================================
        // AOE BUILDER (actions.aoe_builder)
        // =====================================================================
        bool AoEBuilder(int cp, int energy, int enemies, bool berserkUp, bool tfUp, bool isDotC, bool isWS)
        {
            bool hasDCR = Inferno.IsSpellKnown("Double-Clawed Rake");
            bool hasPG = Inferno.IsSpellKnown("Panther's Guile");
            bool ccUp = HasBuff("Clearcasting");
            bool saUp = HasBuff("Sudden Ambush");

            // Swipe first in AoE with DotC during berserk
            if (isDotC && berserkUp)
                if (Cast("Swipe")) return true;

            // Rake on current target only if clearly needs refresh (not just pandemic)
            if (Inferno.DebuffRemaining("Rake") < 2000)
            {
                if (hasDCR || isWS || enemies <= 3)
                    if (Cast("Rake")) return true;
            }

            // Moonfire on current target only if clearly needs refresh (requires Lunar Inspiration)
            if (Inferno.IsSpellKnown("Lunar Inspiration") && Inferno.DebuffRemaining("Moonfire") < 2000)
                if (Cast("Moonfire")) return true;

            // Swipe with Clearcasting
            if (ccUp && enemies > 2)
                if (Cast("Swipe")) return true;

            // Sudden Ambush Swipe at 5+ targets
            if (saUp && enemies >= 5 + (isWS ? 2 : 0))
                if (Cast("Swipe")) return true;

            // Shred at 0-1 CP on 2 targets with Panther's Guile
            if (cp <= 1 && enemies == 2 && hasPG)
                if (Cast("Shred")) return true;

            // Swipe as default AoE builder
            if (cp > 1 || enemies > 2 || !hasPG)
                if (Cast("Swipe")) return true;

            return false;
        }

        // =====================================================================
        // TRINKETS / DEFENSIVES / INTERRUPT
        // =====================================================================
        bool HandleTrinkets(bool berserkUp, bool tfUp)
        {
            if (!GetCheckBox("Use Trinkets") || (Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;
            // APL: use during Tiger's Fury or Berserk window
            if (berserkUp || tfUp)
            { if (Inferno.CanUseEquippedItem(13)) { Inferno.Cast("trinket1"); return true; } if (Inferno.CanUseEquippedItem(14)) { Inferno.Cast("trinket2"); return true; } }
            return false;
        }

        bool HandleDefensives()
        {
            if (!GetCheckBox("Use Defensives")) return false;
            int hpPct = GetPlayerHealthPct();
            if (hpPct <= GetSlider("Survival Instincts HP %") && !HasBuff("Survival Instincts") && Inferno.CanCast("Survival Instincts", IgnoreGCD: true))
            { Inferno.Cast("Survival Instincts", QuickDelay: true); return true; }
            if (hpPct <= GetSlider("Barkskin HP %") && Inferno.CanCast("Barkskin", IgnoreGCD: true))
            { Inferno.Cast("Barkskin", QuickDelay: true); return true; }
            if (hpPct <= GetSlider("Healthstone HP %") && Inferno.CustomFunction("HasHealthstone") == 1 && Inferno.ItemCooldown(HealthstoneItemID) == 0)
            { Inferno.Cast("use_healthstone", QuickDelay: true); return true; }
            return false;
        }

        bool HandleInterrupt()
        {
            if (!GetCheckBox("Auto Interrupt")) return false;
            int castingID = Inferno.CastingID("target");
            if (castingID == 0 || !Inferno.IsInterruptable("target")) { _lastCastingID = 0; return false; }
            if (castingID != _lastCastingID) { _lastCastingID = castingID; int minPct = GetSlider("Interrupt at cast % (min)"); int maxPct = GetSlider("Interrupt at cast % (max)"); if (maxPct < minPct) maxPct = minPct; _interruptTargetPct = _rng.Next(minPct, maxPct + 1); }
            int elapsed = Inferno.CastingElapsed("target"); int remaining = Inferno.CastingRemaining("target"); int total = elapsed + remaining; if (total <= 0) return false;
            if ((elapsed * 100 / total) >= _interruptTargetPct && Inferno.CanCast("Skull Bash", IgnoreGCD: true))
            { Inferno.Cast("Skull Bash", QuickDelay: true); _lastCastingID = 0; return true; }
            return false;
        }

        // =====================================================================
        // HELPERS
        // =====================================================================
        bool InAnyForm() { return Inferno.HasBuff("Cat Form") || Inferno.HasBuff("Bear Form") || Inferno.HasBuff("Moonkin Form") || Inferno.HasBuff("Travel Form") || Inferno.HasBuff("Flight Form") || Inferno.HasBuff("Aquatic Form"); }
        int GCD() { return Inferno.GCD(); }
        bool HasBuff(string n) { return Inferno.BuffRemaining(n) > GCD(); }

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
        int GetPlayerHealthPct() { int hp = Inferno.Health("player"); int m = Inferno.MaxHealth("player"); if (m < 1) m = 1; return (hp * 100) / m; }

        bool IsWildstalker()
        {
            // Wildstalker is detected by having Bloodseeker Vines talent
            return Inferno.IsSpellKnown("Bloodseeker Vines");
        }

        int GetDotCRakeThreshold()
        {
            // APL variable.dotc_rake_threshold: how many targets to rake
            bool hasWS = Inferno.IsSpellKnown("Wild Slashes");
            bool hasIW = Inferno.IsSpellKnown("Infected Wounds");
            if (hasWS && !hasIW) return 3;
            if (!hasWS && hasIW) return 8;
            return 5; // default
        }

        bool Cast(string n)
        {
            if (Inferno.CanCast(n)) { Inferno.Cast(n); Inferno.PrintMessage(">> " + n, Color.White); return true; }
            return false;
        }
    }
}
