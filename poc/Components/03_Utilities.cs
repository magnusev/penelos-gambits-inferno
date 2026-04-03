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

