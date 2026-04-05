// ========================================
// PRIEST SHADOW - COMBAT TICK
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
    
    // Don't interrupt important channels (Void Torrent, Mind Flay: Insanity)
    if (IsChanneling())
    {
        string castName = PlayerCastingName();
        if (castName == "Void Torrent" || castName == "Mind Flay: Insanity")
            return false;
    }
    
    // Don't interrupt cast-time trinkets
    string currentCast = PlayerCastingName();
    if (currentCast.Contains("Puzzle Box") || currentCast.Contains("Emberwing")) 
        return false;
    
    // Priority 3: Buff maintenance
    if (HandleShadowform()) return true;
    if (HandlePowerWordFortitude()) return true;
    
    // Skip if no attackable target
    if (!TargetIsEnemy()) return false;
    
    // Priority 4: Trinkets
    if (HandleTrinkets()) return true;
    
    // Priority 5: Racials (during Voidform or Dark Ascension)
    if ((HasBuff("Voidform") || HasBuff("Dark Ascension")) && HandleRacials()) 
        return true;
    
    // Get enemy count for AoE vs ST decision
    int enemies = Inferno.EnemiesNearUnit(10f, "target");
    if (enemies < 1) enemies = 1;
    if (IsCustomCommandOn("ForceST")) enemies = 1;
    
    // Priority 6: Cooldowns
    if (HandleCooldowns()) return true;
    
    // Priority 7: Rotation (AoE vs ST)
    if (enemies >= GetSlider("AoE enemy count threshold"))
        return RunAoERotation(enemies);
    
    return RunMainRotation(enemies);
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

