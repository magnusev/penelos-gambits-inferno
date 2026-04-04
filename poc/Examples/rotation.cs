using System;
using System.Collections.Generic;
using System.Drawing;
using InfernoWow.API;

namespace InfernoWow.Modules
{
    /// <summary>
    /// Shadow Priest - Translated from SimulationCraft Midnight APL
    /// Hero trees: Archon (Halo) vs Voidweaver (Void Torrent) auto-detected.
    /// Sub-rotations: cds, main (ST), aoe (3+ targets)
    /// Core: Insanity management, DoT maintenance (VT/SWP/SWM), Voidform/PI sync,
    /// Tentacle Slam, Mind Blast charge usage, MFI procs, channel protection.
    /// </summary>
    public class ShadowPriestRotation : Rotation
    {
        List<string> Abilities = new List<string> {
            "Shadow Word: Death", "Shadow Word: Pain", "Vampiric Touch",
            "Mind Blast", "Void Torrent", "Mind Flay", "Tentacle Slam",
            "Shadow Word: Madness", "Void Blast", "Void Volley",
            "Mind Flay: Insanity", "Mindbender", "Halo",
            "Voidform", "Power Infusion", "Voidwraith",
        };
        List<string> TalentChecks = new List<string> {
            "Void Torrent", "Halo", "Voidform", "Mind Devourer",
            "Invoked Nightmare", "Void Apparitions", "Maddening Tentacles",
            "Inescapable Torment", "Devour Matter", "Deathspeaker",
            "Idol of Y'Shaarj", "Shadowfiend", "Distorted Reality",
        };
        List<string> DefensiveSpells = new List<string> {
            "Dispersion", "Vampiric Embrace", "Desperate Prayer",
            "Power Word: Shield",
        };
        List<string> UtilitySpells = new List<string> {
            "Silence", "Power Word: Fortitude", "Shadowform",
        };

        const int HealthstoneItemID = 5512;
        const int InsanityPowerType = 13;
        private Random _rng = new Random();
        private int _lastCastingID = 0;
        private int _interruptTargetPct = 0;

        public override void LoadSettings()
        {
            Settings.Add(new Setting("=== Shadow Priest ==="));
            Settings.Add(new Setting("Use Voidform", true));
            Settings.Add(new Setting("Use Halo", true));
            Settings.Add(new Setting("Use Power Infusion", true));
            Settings.Add(new Setting("Use Trinkets", true));
            Settings.Add(new Setting("Auto Shadowform", true));
            Settings.Add(new Setting("Auto Power Word: Fortitude", true));
            Settings.Add(new Setting("AoE enemy count threshold", 2, 10, 3));
            Settings.Add(new Setting("=== Defensives ==="));
            Settings.Add(new Setting("Use Defensives", true));
            Settings.Add(new Setting("Dispersion HP %", 1, 100, 25));
            Settings.Add(new Setting("Vampiric Embrace HP %", 1, 100, 50));
            Settings.Add(new Setting("Desperate Prayer HP %", 1, 100, 60));
            Settings.Add(new Setting("Power Word: Shield HP %", 1, 100, 70));
            Settings.Add(new Setting("Healthstone HP %", 1, 100, 50));
            Settings.Add(new Setting("=== Interrupt ==="));
            Settings.Add(new Setting("Auto Interrupt", true));
            Settings.Add(new Setting("Interrupt at cast % (min)", 0, 100, 40));
            Settings.Add(new Setting("Interrupt at cast % (max)", 0, 100, 90));
        }

        public override void Initialize()
        {
            Inferno.PrintMessage("             //////////////////////////////////////", Color.DarkViolet);
            Inferno.PrintMessage("             //   SHADOW - PRIEST (MID) V2.00    //", Color.DarkViolet);
            Inferno.PrintMessage("             //   ARCHON / VOIDWEAVER            //", Color.DarkViolet);
            Inferno.PrintMessage("             //////////////////////////////////////", Color.DarkViolet);
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
            CustomFunctions.Add("PetIsActive", "return (UnitExists('pet') and not UnitIsDead('pet')) and 1 or 0");
            // Racial abilities
            foreach (string r in new string[] { "Berserking", "Blood Fury", "Ancestral Call", "Fireblood", "Lights Judgment" }) Spellbook.Add(r);
            CustomCommands.Add("NoCDs"); CustomCommands.Add("nocds");
            CustomCommands.Add("ForceST"); CustomCommands.Add("forcest");
        }

        public override bool OutOfCombatTick()
        {
            if (HandlePowerWordFortitude()) return true;
            if (HandleShadowformOOC()) return true;
            return false;
        }

        public override bool CombatTick()
        {
            if (Inferno.IsDead("player") || Inferno.IsGhost("player")) return false;
            if (HandleDefensives()) return true;
            if (HandleInterrupt()) return true;

            // Don't interrupt Void Torrent or Mind Flay: Insanity channels
            if (Inferno.IsChanneling("player"))
            {
                string castName = Inferno.CastingName("player");
                if (castName == "Void Torrent" || castName == "Mind Flay: Insanity")
                    return false;
            }

            // Don't interrupt cast-time trinkets
            string _castName = Inferno.CastingName("player");
            if (_castName.Contains("Puzzle Box") || _castName.Contains("Emberwing")) return false;

            if (HandleShadowform()) return true;
            if (HandlePowerWordFortitude()) return true;
            if (!Inferno.UnitCanAttack("player", "target")) return false;
                        if (HandleTrinkets()) return true;

            // Racial damage CDs
            if ((HasBuff("Voidform") || HasBuff("Dark Ascension")) && HandleRacials()) return true;

            int enemies = Inferno.EnemiesNearUnit(10f, "target");
            if (enemies < 1) enemies = 1; if ((Inferno.IsCustomCodeOn("ForceST") || Inferno.IsCustomCodeOn("forcest"))) enemies = 1;

            // Cooldowns (shared across builds)
            if (HandleCooldowns()) return true;

            // Route to AoE or ST
            if (enemies >= GetSlider("AoE enemy count threshold"))
                return AoERotation(enemies);
            return MainRotation(enemies);
        }

        // =====================================================================
        // COOLDOWNS (actions.cds)
        // =====================================================================
        bool HandleCooldowns()
        {
            if ((Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;
            bool dotsUp = HasDotsUp();
            bool vfUp = HasBuff("Voidform");
            bool hasVoidformTalent = Inferno.IsSpellKnown("Voidform");
            bool piUp = Inferno.BuffRemaining("Power Infusion") > GCD();

            // halo (Archon — on CD)
            if (GetCheckBox("Use Halo") && CastCD("Halo")) return true;

            // voidform,if=dots up
            if (GetCheckBox("Use Voidform") && !vfUp && dotsUp && CastCD("Voidform")) return true;

            // power_infusion,if=(voidform up or no voidform talent) & PI not up
            if (GetCheckBox("Use Power Infusion") && (vfUp || !hasVoidformTalent) && !piUp)
            {
                if (Inferno.CanCast("Power Infusion", IgnoreGCD: true))
                { Inferno.Cast("Power Infusion", QuickDelay: true); return true; }
            }

            return false;
        }

        // =====================================================================
        // MAIN ST ROTATION (actions.main)
        // =====================================================================
        bool MainRotation(int enemies)
        {
            int insanity = GetInsanity();
            int insanityDeficit = GetInsanityDeficit();
            int gcdMax = GetGCDMax();
            int targetHpPct = GetTargetHealthPct();
            bool dotsUp = HasDotsUp();
            bool mindDevourer = HasBuff("Mind Devourer");
            bool entropicRift = HasBuff("Entropic Rift");
            bool hasMDTalent = Inferno.IsSpellKnown("Mind Devourer");
            bool hasDevourMatter = Inferno.IsSpellKnown("Devour Matter");
            bool hasInvokedNightmare = Inferno.IsSpellKnown("Invoked Nightmare");
            bool hasVoidApparitions = Inferno.IsSpellKnown("Void Apparitions");
            bool hasMaddeningTentacles = Inferno.IsSpellKnown("Maddening Tentacles");
            bool hasInescapableTorment = Inferno.IsSpellKnown("Inescapable Torment");
            int swmRemaining = Inferno.DebuffRemaining("Shadow Word: Madness");
            int swmCost = 40;

            // shadow_word_death — force Devour Matter in execute
            if (hasDevourMatter && targetHpPct <= 20)
                if (Cast("Shadow Word: Death")) return true;

            // shadow_word_madness — don't overcap insanity
            if (swmRemaining <= gcdMax || insanityDeficit <= 35 || mindDevourer || (entropicRift && swmCost > 0))
                if (Cast("Shadow Word: Madness")) return true;

            // void_volley
            if (Cast("Void Volley")) return true;

            // void_blast
            if (Cast("Void Blast")) return true;

            // tentacle_slam — refresh VT or prevent charge cap
            if (IsVTRefreshable() || Inferno.FullRechargeTime("Tentacle Slam", 20000) <= GCDMAX() * 2)
                if (Cast("Tentacle Slam")) return true;

            // void_torrent,if=dots_up
            if (dotsUp && Cast("Void Torrent")) return true;

            // shadow_word_pain with Invoked Nightmare
            if (hasInvokedNightmare && IsSWPRefreshable() && HasVTOnTarget())
                if (Cast("Shadow Word: Pain")) return true;

            // mind_blast,if=(!mind_devourer or no talent)
            if (!mindDevourer || !hasMDTalent)
                if (Cast("Mind Blast")) return true;

            // mind_flay_insanity
            if (HasBuff("Mind Flay: Insanity") && Cast("Mind Flay: Insanity")) return true;

            // tentacle_slam — Void Apparitions / Maddening Tentacles value
            if (hasVoidApparitions || hasMaddeningTentacles)
            {
                bool madOk = !hasMaddeningTentacles || (insanity + 6) >= swmCost || Inferno.DebuffRemaining("Shadow Word: Madness") == 0;
                if (madOk && Cast("Tentacle Slam")) return true;
            }

            // vampiric_touch,if=refreshable
            if (IsVTRefreshable() && Cast("Vampiric Touch")) return true;

            // shadow_word_death — with Inescapable Torment or in execute
            int executeThreshold = 20 + (Inferno.IsSpellKnown("Deathspeaker") ? 15 : 0);
            bool petUp = Inferno.CustomFunction("PetIsActive") == 1;
            if ((petUp && hasInescapableTorment) || (targetHpPct < executeThreshold && Inferno.IsSpellKnown("Shadowfiend") && Inferno.IsSpellKnown("Idol of Y'Shaarj")))
                if (Cast("Shadow Word: Death")) return true;

            // mind_flay filler (don't recast during a Mind Flay channel)
            if (Inferno.CastingName("player") != "Mind Flay" && Cast("Mind Flay")) return true;

            // Movement fallbacks
            if (Cast("Tentacle Slam")) return true;
            if (targetHpPct < 20 && Cast("Shadow Word: Death")) return true;
            if (Cast("Shadow Word: Death")) return true;
            if (Cast("Shadow Word: Pain")) return true;

            return false;
        }

        // =====================================================================
        // AOE ROTATION (actions.aoe, 3+ targets)
        // =====================================================================
        bool AoERotation(int enemies)
        {
            int insanity = GetInsanity();
            int insanityDeficit = GetInsanityDeficit();
            int gcdMax = GetGCDMax();
            int targetHpPct = GetTargetHealthPct();
            bool mindDevourer = HasBuff("Mind Devourer");
            bool entropicRift = HasBuff("Entropic Rift");
            bool hasMDTalent = Inferno.IsSpellKnown("Mind Devourer");
            bool hasDevourMatter = Inferno.IsSpellKnown("Devour Matter");
            bool hasInvokedNightmare = Inferno.IsSpellKnown("Invoked Nightmare");
            bool hasVoidApparitions = Inferno.IsSpellKnown("Void Apparitions");
            bool hasMaddeningTentacles = Inferno.IsSpellKnown("Maddening Tentacles");
            bool hasInescapableTorment = Inferno.IsSpellKnown("Inescapable Torment");
            int swmRemaining = Inferno.DebuffRemaining("Shadow Word: Madness");
            int swmCost = 40;

            // shadow_word_death — force Devour Matter
            if (hasDevourMatter && targetHpPct <= 20)
                if (Cast("Shadow Word: Death")) return true;

            // shadow_word_madness — don't overcap
            if (swmRemaining <= gcdMax || insanityDeficit <= 35 || mindDevourer || (entropicRift && swmCost > 0))
                if (Cast("Shadow Word: Madness")) return true;

            // void_volley
            if (Cast("Void Volley")) return true;

            // void_blast
            if (Cast("Void Blast")) return true;

            // tentacle_slam — spread VT / Void Apparitions / Maddening Tentacles
            if (hasVoidApparitions || hasMaddeningTentacles || IsVTRefreshable())
            {
                bool madOk = !hasMaddeningTentacles || (insanity + 6) >= swmCost || Inferno.DebuffRemaining("Shadow Word: Madness") == 0;
                if (madOk && Cast("Tentacle Slam")) return true;
            }

            // void_torrent,if=dots_up
            if (HasDotsUp() && Cast("Void Torrent")) return true;

            // shadow_word_pain with Invoked Nightmare
            if (hasInvokedNightmare && IsSWPRefreshable() && HasVTOnTarget())
                if (Cast("Shadow Word: Pain")) return true;

            // mind_blast,if=(!mind_devourer or no talent)
            if (!mindDevourer || !hasMDTalent)
                if (Cast("Mind Blast")) return true;

            // mind_flay_insanity
            if (HasBuff("Mind Flay: Insanity") && Cast("Mind Flay: Insanity")) return true;

            // vampiric_touch,if=refreshable
            if (IsVTRefreshable() && Cast("Vampiric Touch")) return true;

            // shadow_word_death — execute or Inescapable Torment
            bool petUp = Inferno.CustomFunction("PetIsActive") == 1;
            if ((petUp && hasInescapableTorment) || targetHpPct < 20)
                if (Cast("Shadow Word: Death")) return true;

            // mind_flay filler
            if (Inferno.CastingName("player") != "Mind Flay" && Cast("Mind Flay")) return true;

            // Movement fallbacks
            if (Cast("Tentacle Slam")) return true;
            if (Cast("Shadow Word: Death")) return true;
            if (Cast("Shadow Word: Pain")) return true;

            return false;
        }

        // =====================================================================
        // DEFENSIVES / INTERRUPT / TRINKETS
        // =====================================================================
        bool HandleDefensives()
        {
            if (!GetCheckBox("Use Defensives")) return false;
            int hpPct = GetPlayerHealthPct();
            if (hpPct <= GetSlider("Dispersion HP %") && Inferno.CanCast("Dispersion"))
            { Inferno.Cast("Dispersion"); return true; }
            if (hpPct <= GetSlider("Vampiric Embrace HP %") && Inferno.CanCast("Vampiric Embrace", IgnoreGCD: true))
            { Inferno.Cast("Vampiric Embrace", QuickDelay: true); return true; }
            if (hpPct <= GetSlider("Desperate Prayer HP %") && Inferno.CanCast("Desperate Prayer", IgnoreGCD: true))
            { Inferno.Cast("Desperate Prayer", QuickDelay: true); return true; }
            if (hpPct <= GetSlider("Healthstone HP %") && Inferno.CustomFunction("HasHealthstone") == 1 && Inferno.ItemCooldown(HealthstoneItemID) == 0)
            { Inferno.Cast("use_healthstone", QuickDelay: true); return true; }
            if (hpPct <= GetSlider("Power Word: Shield HP %") && Inferno.BuffRemaining("Power Word: Shield") < GCD() && Inferno.CanCast("Power Word: Shield"))
            { Inferno.Cast("Power Word: Shield"); return true; }
            return false;
        }

        bool HandleInterrupt()
        {
            if (!GetCheckBox("Auto Interrupt")) return false;
            int castingID = Inferno.CastingID("target");
            if (castingID == 0 || !Inferno.IsInterruptable("target")) { _lastCastingID = 0; return false; }
            if (castingID != _lastCastingID) { _lastCastingID = castingID; int minPct = GetSlider("Interrupt at cast % (min)"); int maxPct = GetSlider("Interrupt at cast % (max)"); if (maxPct < minPct) maxPct = minPct; _interruptTargetPct = _rng.Next(minPct, maxPct + 1); }
            int elapsed = Inferno.CastingElapsed("target"); int remaining = Inferno.CastingRemaining("target"); int total = elapsed + remaining; if (total <= 0) return false;
            if ((elapsed * 100 / total) >= _interruptTargetPct && Inferno.CanCast("Silence", IgnoreGCD: true))
            { Inferno.Cast("Silence", QuickDelay: true); _lastCastingID = 0; return true; }
            return false;
        }

        bool HandleTrinkets()
        {
            if (!GetCheckBox("Use Trinkets") || (Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;
            bool vfUp = HasBuff("Voidform");
            bool piUp = Inferno.BuffRemaining("Power Infusion") >= 10000;
            bool riftUp = HasBuff("Entropic Rift");
            if (!vfUp && !piUp && !riftUp) return false;
            if (Inferno.CanUseEquippedItem(13)) { Inferno.Cast("trinket1"); return true; }
            if (Inferno.CanUseEquippedItem(14)) { Inferno.Cast("trinket2"); return true; }
            return false;
        }

        bool HandleShadowform()
        {
            if (!GetCheckBox("Auto Shadowform")) return false;
            // Shadowform is infinite — use Inferno.HasBuff
            if (!Inferno.HasBuff("Shadowform") && Inferno.CanCast("Shadowform"))
            { Inferno.Cast("Shadowform"); return true; }
            return false;
        }

        bool HandleShadowformOOC()
        {
            if (!GetCheckBox("Auto Shadowform")) return false;
            if (!Inferno.UnitCanAttack("player", "target")) return false;
            if (!Inferno.HasBuff("Shadowform") && Inferno.CanCast("Shadowform"))
            { Inferno.Cast("Shadowform"); return true; }
            return false;
        }

        bool HandlePowerWordFortitude()
        {
            if (!GetCheckBox("Auto Power Word: Fortitude")) return false;
            if (Inferno.BuffRemaining("Power Word: Fortitude") < GCD() && Inferno.CanCast("Power Word: Fortitude"))
            { Inferno.Cast("Power Word: Fortitude"); return true; }
            return false;
        }

        // =====================================================================
        // HELPERS
        // =====================================================================
        int GCD() { return Inferno.GCD(); }
        int GCDMAX() { int g = (int)(1500f / (1f + Inferno.Haste("player") / 100f)); return g < 750 ? 750 : g; }
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
        int GetTargetHealthPct() { int hp = Inferno.Health("target"); int m = Inferno.MaxHealth("target"); if (m < 1) m = 1; return (hp * 100) / m; }

        int GetGCDMax()
        {
            int gcdMax = (int)(1500f / (1f + Inferno.Haste("player") / 100f));
            if (gcdMax < 750) gcdMax = 750;
            return gcdMax;
        }

        // Insanity prediction: account for in-flight casts
        int GetInsanity()
        {
            int insanity = Inferno.Power("player", InsanityPowerType);
            string casting = Inferno.CastingName("player");
            if (casting == "Vampiric Touch") insanity += 4;
            if (casting == "Mind Blast") insanity += 6;
            if (casting == "Void Blast") insanity += 6;
            return insanity;
        }

        int GetInsanityDeficit() { return Inferno.MaxPower("player", InsanityPowerType) - GetInsanity(); }

        // VT prediction: if currently casting VT, treat it as applied
        bool HasVTOnTarget()
        {
            if (Inferno.CastingName("player") == "Vampiric Touch") return true;
            return Inferno.DebuffRemaining("Vampiric Touch") > GCD();
        }

        bool IsVTRefreshable()
        {
            if (Inferno.CastingName("player") == "Vampiric Touch") return false;
            return Inferno.DebuffRemaining("Vampiric Touch") < 6300; // 30% of 21s pandemic
        }

        bool IsSWPRefreshable() { return Inferno.DebuffRemaining("Shadow Word: Pain") < 4800; } // 30% of 16s

        bool HasDotsUp() { return HasVTOnTarget() && Inferno.DebuffRemaining("Shadow Word: Pain") > GCD(); }

        bool Cast(string n)
        {
            if (Inferno.CanCast(n)) { Inferno.Cast(n); Inferno.PrintMessage(">> " + n, Color.White); return true; }
            return false;
        }

        bool CastCD(string n)
        {
            if ((Inferno.IsCustomCodeOn("NoCDs") || Inferno.IsCustomCodeOn("nocds"))) return false;
            if (Inferno.CanCast(n)) { Inferno.Cast(n); Inferno.PrintMessage(">> " + n, Color.White); return true; }
            return false;
        }
    }
}
