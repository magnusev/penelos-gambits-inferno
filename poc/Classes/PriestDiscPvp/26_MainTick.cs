// ========================================
// PRIEST DISCIPLINE PVP - COMBAT TICK
// ========================================

public override bool CombatTick()
{
    // Skip if player is dead
    if (Inferno.IsDead("player") || Inferno.IsGhost("player")) return false;
    
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
    
    // Skip if channeling
    if (IsChanneling()) return false;
    
    // Priority 3: Trinkets
    if (HandleTrinkets()) return true;
    
    // Determine burst window
    bool inRift = HasBuff("Entropic Rift") || SpellCooldown("Mind Blast") <= GCD() + 2500;
    bool inBurst = HasBuff("Power Infusion") || HasBuff("Evangelism") || inRift;
    
    // Priority 4: Cooldowns
    if (HandleCooldowns(inBurst)) return true;
    
    // Priority 5: Damage/Atonement rotation
    if (HandleDamage(inRift, inBurst)) return true;
    
    return false;
}

public override bool OutOfCombatTick()
{
    // No specific out-of-combat logic for PvP
    return false;
}

// Runs after every tick - check for map changes
public override void CleanUp()
{
    int mapId = Inferno.GetMapID();
    LogMapChange(mapId);
}

public override void OnStop() 
{ 
    Log("Rotation stopped"); 
}

