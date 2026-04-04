// ========================================
// PRIEST SHADOW - OUT OF COMBAT
// ========================================

public override bool OutOfCombatTick()
{
    // Maintain Power Word: Fortitude
    if (HandlePowerWordFortitude()) return true;
    
    // Enter Shadowform if we have a target
    if (TargetIsEnemy() && HandleShadowform()) return true;
    
    return false;
}

