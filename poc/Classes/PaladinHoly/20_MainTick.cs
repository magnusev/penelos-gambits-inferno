// ========================================
// PALADIN HOLY - MAIN TICK LOOP
// ========================================

public override bool CombatTick()
{
    // Skip if player is dead
    if (Inferno.IsDead("player")) return true;
    
    // Wait for GCD to finish
    if (Inferno.GCD() != 0) return true;

    // Process queued action first (two-tick casting pattern)
    if (ProcessQueue()) return true;

    // Periodic diagnostic logging
    if (ThrottleIsOpen("diag", DIAGNOSTIC_LOG_INTERVAL_MS))
    {
        ThrottleRestart("diag");
        List<string> groupMembers = GetGroupMembers();
        string info = "";
        // Only log first 5 members to avoid security validator issues with long strings
        int maxLog = groupMembers.Count > 5 ? 5 : groupMembers.Count;
        for (int i = 0; i < maxLog; i++)
            info += groupMembers[i] + "=" + HealthPct(groupMembers[i]) + "% ";
        if (groupMembers.Count > 5)
            info += "... (" + (groupMembers.Count - 5) + " more)";
        Log("Tick: combat=" + Inferno.InCombat("player") + " group=" + groupMembers.Count + " | " + info);
        
        // Log boss information
        LogBossInformation();
    }

    // Execute rotation priorities
    int mapId = Inferno.GetMapID();
    
    // Log map changes
    LogMapChange(mapId);
    if (RunDungeonGambits(mapId)) return true;
    if (RunHealGambits()) return true;
    if (RunDmgGambits()) return true;
    
    // Always return true to keep ticking
    return true;
}

// Use combat tick logic for out-of-combat as well
public override bool OutOfCombatTick() 
{ 
    return CombatTick(); 
}

// Runs after every tick - check for map changes
public override void CleanUp()
{
    int mapId = Inferno.GetMapID();
    LogMapChange(mapId);
}

// Cleanup when rotation stops
public override void OnStop() 
{ 
    Log("Rotation stopped"); 
}

