// ========================================
// PRIEST SHADOW - HELPER FUNCTIONS
// ========================================

// Returns current Insanity (accounting for in-flight casts)
private int GetInsanity()
{
    int insanity = PowerCurrent(INSANITY);
    string casting = PlayerCastingName();
    
    // Predict insanity from in-flight casts
    if (casting == "Vampiric Touch") insanity += 4;
    if (casting == "Mind Blast") insanity += 6;
    if (casting == "Void Blast") insanity += 6;
    
    return insanity;
}

// Returns insanity deficit (max - current)
private int GetInsanityDeficit()
{
    return PowerMax(INSANITY) - GetInsanity();
}

// Returns true if Vampiric Touch is on target (or currently casting it)
private bool HasVTOnTarget()
{
    if (PlayerCastingName() == "Vampiric Touch") return true;
    return DebuffRemaining("Vampiric Touch") > GCD();
}

// Returns true if Vampiric Touch needs refresh (pandemic window)
private bool IsVTRefreshable()
{
    if (PlayerCastingName() == "Vampiric Touch") return false;
    return DebuffRemaining("Vampiric Touch") < 6300; // 30% of 21s
}

// Returns true if Shadow Word: Pain needs refresh (pandemic window)
private bool IsSWPRefreshable()
{
    return DebuffRemaining("Shadow Word: Pain") < 4800; // 30% of 16s
}

// Returns true if both DoTs are active
private bool HasDotsUp()
{
    return HasVTOnTarget() && DebuffRemaining("Shadow Word: Pain") > GCD();
}

// Returns true if pet (Shadowfiend/Mindbender/Voidwraith) is active
private bool IsPetActive()
{
    return Inferno.CustomFunction("PetIsActive") == 1;
}

