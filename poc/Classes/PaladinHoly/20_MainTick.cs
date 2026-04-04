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
        for (int i = 0; i < groupMembers.Count; i++) 
            info += groupMembers[i] + "=" + HealthPct(groupMembers[i]) + "% ";
        Log("Tick: combat=" + Inferno.InCombat("player") + " group=" + groupMembers.Count + " | " + info);
    }

    // Execute rotation priorities
    int mapId = Inferno.GetMapID();
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

// Cleanup when rotation stops
public override void OnStop() 
{ 
    Log("Rotation stopped"); 
}

