// ========================================
// PALADIN HOLY - MAIN TICK LOOP
// ========================================

public override bool CombatTick()
{
    if (Inferno.IsDead("player")) return true;
    if (Inferno.GCD() != 0) return true;

    // Process queued action first (matches ActionQueuer.CastQueuedActionIfExists)
    if (ProcessQueue()) return true;

    // Periodic status log
    if (ThrottleIsOpen("diag", DIAGNOSTIC_LOG_INTERVAL_MS))
    {
        ThrottleRestart("diag");
        List<string> gm = GetGroupMembers();
        string info = "";
        for (int i = 0; i < gm.Count; i++) info += gm[i] + "=" + HealthPct(gm[i]) + "% ";
        Log("Tick: combat=" + Inferno.InCombat("player") + " group=" + gm.Count + " | " + info);
    }

    int mapId = Inferno.GetMapID();
    if (RunDungeonGambits(mapId)) return true;
    if (RunHealGambits()) return true;
    if (RunDmgGambits()) return true;
    
    // Always return true to keep ticking (matches old PeneloRotation.Tick)
    return true;
}

public override bool OutOfCombatTick() { return CombatTick(); }

public override void OnStop() { Log("Rotation stopped"); }

