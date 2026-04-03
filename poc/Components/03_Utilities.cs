// ========================================
// UTILITIES
// ========================================
// Simple helper functions used throughout the rotation

private int HealthPct(string u) 
{ 
    int mx = Inferno.MaxHealth(u); 
    if (mx < 1) mx = 1; 
    return (Inferno.Health(u) * 100) / mx; 
}

