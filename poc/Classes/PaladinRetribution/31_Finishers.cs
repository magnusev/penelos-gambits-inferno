// ========================================
// PALADIN RETRIBUTION - FINISHERS
// ========================================

private bool RunFinishers()
{
    int enemies = EnemiesNearPlayer();
    if (enemies < 1) enemies = 1;
    
    // Force single target if command is active
    if (IsCustomCommandOn("ForceST")) 
        enemies = 1;
    
    // Divine Storm conditions: 3+ enemies or Empyrean Power proc (but not Empyrean Legacy)
    bool canDivineStorm = (enemies >= 3 || HasBuff("Empyrean Power")) && !HasBuff("Empyrean Legacy");
    bool hasHammerOfLight = HasBuff("Hammer of Light");
    
    // Hammer of Light - Wake of Ashes becomes Hammer of Light when buff is active
    if (hasHammerOfLight)
    {
        // Cast if Avenging Wrath is up or Hammer of Light buff expiring soon
        if (HasBuff("Avenging Wrath") || BuffRemaining("Hammer of Light") < GCDMAX() * 2)
        {
            if (CastOffensive("Wake of Ashes")) return true;
        }
    }
    
    // Divine Storm - if conditions met and not saving for Hammer of Light
    if (canDivineStorm && !hasHammerOfLight)
    {
        if (CastOffensive("Divine Storm")) return true;
    }
    
    // Templar's Verdict - default finisher
    if (!hasHammerOfLight)
    {
        if (CastOffensive("Templar's Verdict")) return true;
    }
    
    return false;
}

