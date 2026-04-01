using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.IO;
using InfernoWow.API;

namespace InfernoWow.Modules
{

// POC: Flash of Light on lowest HP team member
    public class FlashOfLightPOC : Rotation
    {
        // ═══════════════════════════════════════════════════════
        //  STATE
        // ═══════════════════════════════════════════════════════

        // ActionQueuer replacement — queued macro for next tick
        private string _queuedAction = null;

        // Throttler replacement — keyed timestamps using DateTime
        private Dictionary<string, long> _throttleTimestamps = new Dictionary<string, long>();

        // Logger — file path for file-based logging (System.IO is allowed)
        private string _logFile = null;

        // ═══════════════════════════════════════════════════════
        //  ROTATION LIFECYCLE
        // ═══════════════════════════════════════════════════════

        public override void LoadSettings()
        {
            Settings.Add(new Setting("Flash of Light Threshold", 1, 100, 95));
            Settings.Add(new Setting("Enable Logging", true));
        }

        public override void Initialize()
        {
            // --- Spellbook ---
            Spellbook.Add("Flash of Light");
            Spellbook.Add("Cleanse");

            // --- Macros: focus-cast patterns ---
            Macros.Add("cast_flash_of_light", "/cast [@focus] Flash of Light");
            Macros.Add("cast_cleanse", "/cast [@focus] Cleanse");

            // --- Macros: focus targeting (party) ---
            Macros.Add("focus_player", "/focus player");
            for (int i = 1; i <= 4; i++)
                Macros.Add("focus_party" + i, "/focus party" + i);

            // --- Macros: focus targeting (raid) ---
            for (int i = 1; i <= 28; i++)
                Macros.Add("focus_raid" + i, "/focus raid" + i);

            // --- Logger init ---
            _logFile = "poc_flashoflight_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log";

            Inferno.PrintMessage("POC Flash of Light rotation loaded!", Color.Green);
            Log("Initialize complete");
        }

        public override bool CombatTick()
        {
            if (Inferno.IsDead("player")) return false;

            // Process queued macro from previous tick first
            if (ProcessQueue()) return true;

            int mapId = Inferno.GetMapID();

            // Dungeon-specific gambits (highest priority)
            if (RunDungeonGambits(mapId)) return true;

            // Default heal gambits
            if (RunHealGambits()) return true;

            // Damage fallback (empty for this POC)
            if (RunDmgGambits()) return true;

            return false;
        }

        public override bool OutOfCombatTick()
        {
            return CombatTick();
        }

        public override void OnStop()
        {
            Log("Rotation stopped");
        }

        // ═══════════════════════════════════════════════════════
        //  GAMBIT CHAINS (if-chain style — replaces GambitSet + Gambit)
        // ═══════════════════════════════════════════════════════

        private bool RunHealGambits()
        {
            int threshold = GetSlider("Flash of Light Threshold");

            // Flash of Light on lowest ally under threshold
            if (IsInCombat())
            {
                string target = LowestAllyUnder(threshold, "Flash of Light");
                if (target != null)
                {
                    Log("Casting Flash of Light on " + target + " (HP: " + Inferno.Health(target) + "%)");
                    return CastOnFocus(target, "cast_flash_of_light");
                }
            }

            return false;
        }

        private bool RunDmgGambits()
        {
            // Empty for this POC
            return false;
        }

        private bool RunDungeonGambits(int mapId)
        {
            switch (mapId)
            {
                case 480: // Proving Grounds — dispel Aqua Bomb before healing
                    return RunProvingGroundsGambits();
                default:
                    return false;
            }
        }

        private bool RunProvingGroundsGambits()
        {
            // Dispel Aqua Bomb
            if (IsInCombat()
                && IsSpellReady("Cleanse")
                && AnyAllyHasDebuff("Aqua Bomb"))
            {
                string target = GetAllyWithDebuff("Aqua Bomb", "Cleanse");
                if (target != null)
                {
                    ThrottleRestart("dispel_aqua_bomb");
                    Log("Dispelling Aqua Bomb on " + target);
                    return CastOnFocus(target, "cast_cleanse");
                }
            }

            return false;
        }

        // ═══════════════════════════════════════════════════════
        //  CONDITION HELPERS
        // ═══════════════════════════════════════════════════════

        private bool IsInCombat()
        {
            return Inferno.InCombat("player");
        }

        private bool IsSpellReady(string spell)
        {
            return Inferno.SpellCooldown(spell) <= 200;
        }

        private bool AnyAllyHasDebuff(string debuff)
        {
            return GetGroupMembers().Any(u => !Inferno.IsDead(u) && Inferno.HasDebuff(debuff, u, false));
        }

        // ═══════════════════════════════════════════════════════
        //  SELECTOR HELPERS
        // ═══════════════════════════════════════════════════════

        private string LowestAllyUnder(int hpThreshold, string spell)
        {
            return GetGroupMembers()
                .Where(u => !Inferno.IsDead(u))
                .Where(u => Inferno.Health(u) < hpThreshold)
                .Where(u => Inferno.SpellInRange(spell, u))
                .OrderBy(u => Inferno.Health(u))
                .FirstOrDefault();
        }

        private string GetAllyWithDebuff(string debuff, string spell)
        {
            return GetGroupMembers()
                .Where(u => !Inferno.IsDead(u))
                .Where(u => Inferno.HasDebuff(debuff, u, false))
                .Where(u => Inferno.SpellInRange(spell, u))
                .FirstOrDefault();
        }

        // ═══════════════════════════════════════════════════════
        //  GROUP MEMBER DETECTION
        // ═══════════════════════════════════════════════════════

        private List<string> GetGroupMembers()
        {
            var members = new List<string>();

            if (Inferno.InRaid())
            {
                int size = Inferno.GroupSize();
                for (int i = 1; i <= size; i++)
                {
                    string token = "raid" + i;
                    if (!string.IsNullOrEmpty(Inferno.UnitName(token)))
                        members.Add(token);
                }
            }
            else if (Inferno.InParty())
            {
                members.Add("player");
                int size = Inferno.GroupSize();
                for (int i = 1; i < size; i++)
                {
                    string token = "party" + i;
                    if (!string.IsNullOrEmpty(Inferno.UnitName(token)))
                        members.Add(token);
                }
            }
            else
            {
                members.Add("player");
            }

            return members;
        }

        // ═══════════════════════════════════════════════════════
        //  CAST HELPERS
        // ═══════════════════════════════════════════════════════

        private bool CastOnFocus(string unitToken, string macroName)
        {
            Inferno.Cast("focus_" + unitToken);
            _queuedAction = macroName;
            return true;
        }

        private bool ProcessQueue()
        {
            if (_queuedAction == null) return false;
            string action = _queuedAction;
            _queuedAction = null;
            Inferno.Cast(action, true);
            return true;
        }

        // ═══════════════════════════════════════════════════════
        //  THROTTLE HELPERS
        // ═══════════════════════════════════════════════════════

        private long GetTimestampMs()
        {
            return DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
        }

        private bool ThrottleIsOpen(string key, int intervalMs)
        {
            if (!_throttleTimestamps.ContainsKey(key)) return true;
            return (GetTimestampMs() - _throttleTimestamps[key]) >= intervalMs;
        }

        private void ThrottleRestart(string key)
        {
            _throttleTimestamps[key] = GetTimestampMs();
        }

        // ═══════════════════════════════════════════════════════
        //  LOGGING
        // ═══════════════════════════════════════════════════════

        private void Log(string message)
        {
            if (!GetCheckBox("Enable Logging")) return;

            Inferno.PrintMessage(message, Color.White);

            if (_logFile != null)
            {
                try
                {
                    File.AppendAllText(_logFile,
                        DateTime.Now.ToString("HH:mm:ss.fff") + " " + message + "\n");
                }
                catch
                {
                    // Swallow — file logging is best-effort
                }
            }
        }

        private void LogThrottled(string key, int intervalMs, string message)
        {
            if (ThrottleIsOpen(key, intervalMs))
            {
                ThrottleRestart(key);
                Log(message);
            }
        }
    }

}