// ========================================
// CORE: Queue System, Throttle, Logging
// ========================================
// Based on the old ActionQueuer pattern - simple and effective
// The queue allows two-tick casting: focus on tick 1, cast on tick 2

private string _queuedAction = null;
private string _lastLoggedAction = null;
private Dictionary<string, long> _throttleTimestamps = new Dictionary<string, long>();
private string _logFile = null;

// -- Queue System --
// Matches old ActionQueuer.QueueAction: don't overwrite if already queued
private bool CastOnFocus(string unit, string macro) 
{ 
    if (_queuedAction != null) return false;
    Inferno.Cast("focus_" + unit); 
    _queuedAction = macro; 
    return true; 
}

private bool CastPersonal(string spell) { Inferno.Cast(spell); return true; }
private bool CastOnEnemy(string spell) { Inferno.Cast(spell); return true; }

private bool ProcessQueue()
{
    if (_queuedAction == null) return false;
    string action = _queuedAction; 
    _queuedAction = null;
    Inferno.Cast(action, QuickDelay: true);
    return true;
}

// -- Throttle System --
private long NowMs() { return DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond; }

private bool ThrottleIsOpen(string key, int milliseconds) 
{ 
    if (!_throttleTimestamps.ContainsKey(key)) return true; 
    return (NowMs() - _throttleTimestamps[key]) >= milliseconds; 
}

private void ThrottleRestart(string key) { _throttleTimestamps[key] = NowMs(); }

// -- Logging --
private void Log(string msg)
{
    if (!GetCheckBox("Enable Logging")) return;
    // Suppress duplicate log lines (e.g. "Casting Judgment" 30x in a row)
    if (msg == _lastLoggedAction && !msg.StartsWith("Tick:")) return;
    _lastLoggedAction = msg;
    Inferno.PrintMessage(msg, Color.White);
    if (_logFile != null) 
    { 
        try 
        { 
            File.AppendAllText(_logFile,
                DateTime.Now.ToString("HH:mm:ss.fff") + " " + msg + "\n");
        } 
        catch { } 
    }
}

