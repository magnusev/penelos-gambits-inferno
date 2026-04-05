// ========================================
// PALADIN HOLY PVP - COMBAT TICK
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
    
    // Priority 1: Defensives (emergency saves)
    if (HandleDefensives()) return true;
    
    // Priority 2: Interrupts (Hammer of Justice)
    if (HandleInterrupt()) return true;
    
    // Skip if channeling
    if (IsChanneling()) return false;
    
    // Priority 3: Trinkets
    if (HandleTrinkets()) return true;
    
    bool inCrusader = HasBuff("Avenging Crusader");
    
    // Priority 4: Cooldowns
    if (HandleCooldowns(inCrusader)) return true;
    
    // Priority 5: Healing
    if (HandleHealing()) return true;
    
    // Priority 6: Damage
    if (HandleDamage()) return true;
    
    return false;
}

public override bool OutOfCombatTick()
{
    // No specific out-of-combat logic for PvP
    return false;
}

public override void OnStop() 
{ 
    Log("Rotation stopped"); 
}

