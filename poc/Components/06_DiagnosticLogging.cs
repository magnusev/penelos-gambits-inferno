// ========================================
// UNIVERSAL - DIAGNOSTIC LOGGING
// ========================================
// Map change and boss information logging
// Requires: _lastMapId variable in class config

private void LogMapChange(int currentMapId)
{
    if (_lastMapId != 0 && _lastMapId != currentMapId)
    {
        Log("Map changed: " + _lastMapId + " -> " + currentMapId);
    }
    _lastMapId = currentMapId;
}

private void LogBossInformation()
{
    for (int i = 1; i <= 4; i++)
    {
        string boss = "boss" + i;
        
        // Check if boss exists (has health > 0)
        int bossHealth = Inferno.Health(boss);
        if (bossHealth > 0)
        {
            // Get boss name (Retail only, so wrap in try-catch for safety)
            string bossName = "Unknown";
            try
            {
                bossName = Inferno.UnitName(boss);
            }
            catch { }
            
            // Get casting ID (0 if not casting)
            int castingId = Inferno.CastingID(boss);
            
            // Build log message
            string logMsg = "Boss" + i + ": " + bossName + " Health: " + bossHealth + "%";
            
            // If boss is casting, add casting information
            if (castingId != 0)
            {
                string castName = "Unknown";
                try
                {
                    castName = Inferno.CastingName(boss);
                }
                catch { }
                
                bool interruptable = Inferno.IsInterruptable(boss);
                bool channeling = Inferno.IsChanneling(boss);
                int elapsed = Inferno.CastingElapsed(boss);
                int remaining = Inferno.CastingRemaining(boss);
                
                logMsg += " | CASTING: " + castName + " (ID:" + castingId + ")";
                logMsg += " Interruptable:" + interruptable;
                logMsg += " Channeling:" + channeling;
                logMsg += " Elapsed:" + elapsed + "ms";
                logMsg += " Remaining:" + remaining + "ms";
            }
            
            Log(logMsg);
        }
    }
}



