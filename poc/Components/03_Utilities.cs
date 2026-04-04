// ========================================
// UTILITIES
// ========================================
// Simple helper functions used throughout the rotation

// Returns the health percentage of a unit (0-100)
private int HealthPct(string unit) 
{ 
    int maxHealth = Inferno.MaxHealth(unit); 
    if (maxHealth < 1) maxHealth = 1; 
    return (Inferno.Health(unit) * 100) / maxHealth; 
}

// Returns the current power value for a power type
private int PowerCurrent(int powerType)
{
    return Inferno.Power("player", powerType);
}

// Returns the remaining GCD time in milliseconds
private int GCD()
{
    return Inferno.GCD();
}

// Returns remaining duration of a buff in milliseconds
private int BuffRemaining(string buffName, string unit = "player")
{
    return Inferno.BuffRemaining(buffName, unit, true);
}

