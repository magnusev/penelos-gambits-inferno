// ========================================
// PALADIN RETRIBUTION - COMBAT TICK
// ========================================

public override bool CombatTick()
{
    // Skip if player is dead
    if (Inferno.IsDead("player") || Inferno.IsGhost("player")) return false;
    
    // Priority 1: Defensives
    if (HandleDefensives()) return true;
    
    // Priority 2: Interrupts
    if (HandleInterrupt()) return true;
    
    // Skip if no attackable target
    if (!TargetIsEnemy()) return false;
    
    // Skip if channeling
    if (IsChanneling()) return false;
    
    // Don't interrupt cast-time trinkets
    string castName = PlayerCastingName();
    if (castName.Contains("Puzzle Box") || castName.Contains("Emberwing")) 
        return false;
    
    // Priority 3: Racials (during Avenging Wrath)
    if ((HasBuff("Avenging Wrath") || HasBuff("Crusade")) && HandleRacials()) 
        return true;
    
    // Priority 4: Cooldowns
    if (HandleCooldowns()) return true;
    
    // Priority 5: Generators (calls Finishers internally)
    if (RunGenerators()) return true;
    
    return false;
}

public override void OnStop() 
{ 
    Log("Rotation stopped"); 
}

