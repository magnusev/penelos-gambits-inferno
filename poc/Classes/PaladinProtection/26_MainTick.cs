// ========================================
// PALADIN PROTECTION - COMBAT TICK
// ========================================

public override bool CombatTick()
{
    // Skip if player is dead or GCD active
    if (Inferno.IsDead("player") || Inferno.IsGhost("player")) return false;
    if (GCD() != 0) return true;
    
    // Map change and boss logging
    int mapId = Inferno.GetMapID();
    LogMapChange(mapId);
    if (ThrottleIsOpen("boss_log", DIAGNOSTIC_LOG_INTERVAL_MS))
    {
        ThrottleRestart("boss_log");
        LogBossInformation();
    }
    
    // Priority 1: Defensives
    if (HandleDefensives()) return true;
    
    // Priority 2: Interrupts
    if (HandleInterrupt()) return true;
    
    // Skip if no attackable target
    if (!TargetIsEnemy()) return false;
    
    // Priority 3: Racials
    if (HandleRacials()) return true;
    
    // Skip if channeling
    if (IsChanneling()) return false;
    
    // Don't interrupt cast-time trinkets
    string castName = PlayerCastingName();
    if (castName.Contains("Puzzle Box") || castName.Contains("Emberwing")) 
        return false;
    
    // Priority 4: Rotation
    if (RunRotation()) return true;
    
    return false;
}

public override void OnStop() 
{ 
    Log("Rotation stopped"); 
}

