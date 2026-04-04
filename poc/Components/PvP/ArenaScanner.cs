// ========================================
// PVP - ARENA GROUP SCANNER
// ========================================
// Scans focus + party1-3 (+ optionally self) for lowest HP ally
// Used by arena healers for emergency saves

// Returns the unit token with lowest HP in arena group (focus + party1-3 + player)
private string LowestArenaAlly(int maxCount = 3)
{
    string lowestUnit = "";
    int lowestHp = 101;
    
    // Check focus if "Protect Focus" setting is enabled
    if (IsSettingOn("Protect Focus"))
    {
        int focusHp = GetUnitHealthPct("focus");
        if (focusHp > 0 && focusHp < lowestHp)
        {
            lowestHp = focusHp;
            lowestUnit = "focus";
        }
    }
    
    // Check party members (party1-3 for arena)
    for (int i = 1; i <= maxCount; i++)
    {
        string unit = "party" + i;
        int hp = GetUnitHealthPct(unit);
        if (hp > 0 && hp < lowestHp)
        {
            lowestHp = hp;
            lowestUnit = unit;
        }
    }
    
    // Check self
    int selfHp = HealthPct("player");
    if (selfHp < lowestHp)
    {
        lowestHp = selfHp;
        lowestUnit = "player";
    }
    
    return lowestUnit;
}

// Returns the unit token with lowest HP below a threshold
private string LowestArenaAllyUnder(int threshold, int maxCount = 3)
{
    string lowestUnit = "";
    int lowestHp = 101;
    
    // Check focus if enabled
    if (IsSettingOn("Protect Focus"))
    {
        int focusHp = GetUnitHealthPct("focus");
        if (focusHp > 0 && focusHp < threshold && focusHp < lowestHp)
        {
            lowestHp = focusHp;
            lowestUnit = "focus";
        }
    }
    
    // Check party members
    for (int i = 1; i <= maxCount; i++)
    {
        string unit = "party" + i;
        int hp = GetUnitHealthPct(unit);
        if (hp > 0 && hp < threshold && hp < lowestHp)
        {
            lowestHp = hp;
            lowestUnit = unit;
        }
    }
    
    // Check self
    int selfHp = HealthPct("player");
    if (selfHp < threshold && selfHp < lowestHp)
    {
        lowestHp = selfHp;
        lowestUnit = "player";
    }
    
    return lowestUnit;
}

// Returns health percentage of any unit (with validation)
private int GetUnitHealthPct(string unit)
{
    int maxHealth = Inferno.MaxHealth(unit);
    if (maxHealth < 1) return 0;
    return (Inferno.Health(unit) * 100) / maxHealth;
}

