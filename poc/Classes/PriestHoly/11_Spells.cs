// ========================================
// PRIEST HOLY - SPELL-SPECIFIC LOGIC
// ========================================

// Returns true if target has Prayer of Mending buff
private bool HasPrayerOfMending(string unit)
{
    return Inferno.HasBuff("Prayer of Mending", unit, true);
}

// Returns true if target has Renew buff
private bool HasRenew(string unit)
{
    return Inferno.HasBuff("Renew", unit, true);
}

// Returns true if target has Power Word: Shield buff or debuff (Weakened Soul)
private bool HasShield(string unit)
{
    return Inferno.HasBuff("Power Word: Shield", unit, true) || Inferno.HasDebuff("Weakened Soul", unit, false);
}

// Returns remaining duration of Renew on target (in milliseconds)
private int RenewRemaining(string unit)
{
    if (!HasRenew(unit)) return 0;
    return Inferno.BuffRemaining("Renew", unit, true);
}


