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

// Returns the maximum power value for a power type
private int PowerMax(int powerType)
{
    return Inferno.MaxPower("player", powerType);
}

// Returns target's health percentage (0-100)
private int TargetHealthPct()
{
    int maxHealth = Inferno.MaxHealth("target");
    if (maxHealth < 1) maxHealth = 1;
    return (Inferno.Health("target") * 100) / maxHealth;
}

// Returns the full recharge time for a charge-based spell
private int FullRechargeTime(string spellName, int baseRecharge)
{
    return Inferno.FullRechargeTime(spellName, baseRecharge);
}

// Returns the remaining GCD time in milliseconds
private int GCD()
{
    return Inferno.GCD();
}

// Returns the maximum GCD duration with haste (750ms floor)
private int GCDMAX()
{
    int gcd = (int)(1500f / (1f + Inferno.Haste("player") / 100f));
    return gcd < 750 ? 750 : gcd;
}

// Returns remaining duration of a buff in milliseconds
private int BuffRemaining(string buffName, string unit = "player")
{
    return Inferno.BuffRemaining(buffName, unit, true);
}

// Returns remaining duration of a debuff in milliseconds
private int DebuffRemaining(string debuffName, string unit = "target")
{
    return Inferno.DebuffRemaining(debuffName, unit, true);
}

// Returns time since combat started in milliseconds
private int CombatTime()
{
    return Inferno.CombatTime();
}

// Returns spell cooldown in milliseconds
private int SpellCooldown(string spellName)
{
    return Inferno.SpellCooldown(spellName);
}

