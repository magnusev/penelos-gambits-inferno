using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.IO;
using InfernoWow.API;

namespace InfernoWow.Modules
{

public class FlashOfLightPOC : Rotation
{
    private string _queuedAction = null;
    private Dictionary<string, long> _throttleTimestamps = new Dictionary<string, long>();
    private string _logFile = null;

    public override void LoadSettings()
    {
        Settings.Add(new Setting("Flash of Light Threshold", 1, 100, 95));
        Settings.Add(new Setting("Enable Logging", true));
    }

    public override void Initialize()
    {
        Spellbook.Add("Flash of Light");
        Spellbook.Add("Cleanse");

        Macros.Add("cast_flash_of_light", "/cast [@focus] Flash of Light");
        Macros.Add("cast_cleanse", "/cast [@focus] Cleanse");

        Macros.Add("focus_player", "/focus player");
        for (int i = 1; i <= 4; i++)
            Macros.Add("focus_party" + i, "/focus party" + i);
        for (int i = 1; i <= 28; i++)
            Macros.Add("focus_raid" + i, "/focus raid" + i);

        _logFile = "poc_flashoflight_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log";
        Inferno.PrintMessage("POC Flash of Light rotation loaded!", Color.Green);
        Log("Initialize complete");
    }

    public override bool CombatTick()
    {
        if (Inferno.IsDead("player")) return false;
        if (ProcessQueue()) return true;

        if (ThrottleIsOpen("diag_tick", 2000))
        {
            ThrottleRestart("diag_tick");
            var members = GetGroupMembers();
            string memberInfo = "";
            foreach (string m in members)
            {
                memberInfo += m + "=" + HealthPct(m) + "% ";
            }
            Log("Tick: combat=" + Inferno.InCombat("player")
                + " group=" + members.Count
                + " inParty=" + Inferno.InParty()
                + " inRaid=" + Inferno.InRaid()
                + " groupSize=" + Inferno.GroupSize()
                + " | " + memberInfo);
        }

        int mapId = Inferno.GetMapID();
        if (RunDungeonGambits(mapId)) return true;
        if (RunHealGambits()) return true;
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

    private bool RunHealGambits()
    {
        if (!ThrottleIsOpen("cast_throttle", 2000)) return false;

        int threshold = GetSlider("Flash of Light Threshold");
        string target = LowestAllyUnder(threshold, "Flash of Light");
        if (target != null)
        {
            Log("Casting Flash of Light on " + target + " (" + HealthPct(target) + "%)");
            return CastOnFocus(target, "cast_flash_of_light");
        }

        if (ThrottleIsOpen("diag_noheal", 3000))
        {
            ThrottleRestart("diag_noheal");
            var members = GetGroupMembers();
            string reason = "No heal target t=" + threshold;
            foreach (string m in members)
            {
                if (Inferno.IsDead(m))
                    reason += " | " + m + "=DEAD";
                else if (HealthPct(m) >= threshold)
                    reason += " | " + m + "=" + HealthPct(m) + "%ok";
                else if (!Inferno.SpellInRange("Flash of Light", m))
                    reason += " | " + m + "=" + HealthPct(m) + "%oor";
                else
                    reason += " | " + m + "=" + HealthPct(m) + "%rdy";
            }
            Log(reason);
        }

        return false;
    }

    private bool RunDmgGambits()
    {
        return false;
    }

    private bool RunDungeonGambits(int mapId)
    {
        switch (mapId)
        {
            case 480:
                return RunProvingGroundsGambits();
            default:
                return false;
        }
    }

    private bool RunProvingGroundsGambits()
    {
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

    private int HealthPct(string unit)
    {
        long max = Inferno.MaxHealth(unit);
        if (max <= 0) return 100;
        return (int)((long)Inferno.Health(unit) * 100L / max);
    }

    private string LowestAllyUnder(int hpThreshold, string spell)
    {
        return GetGroupMembers()
            .Where(u => !Inferno.IsDead(u))
            .Where(u => HealthPct(u) < hpThreshold)
            .Where(u => Inferno.SpellInRange(spell, u))
            .OrderBy(u => HealthPct(u))
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

    private bool CastOnFocus(string unitToken, string macroName)
    {
        Inferno.Cast("focus_" + unitToken);
        _queuedAction = macroName;
        ThrottleRestart("cast_throttle");
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
            catch { }
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