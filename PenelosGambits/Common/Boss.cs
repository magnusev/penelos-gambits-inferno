public class Boss
{
    public readonly string Name;
    public readonly string UnitName;
    public readonly int Health;
    public readonly int MaxHealth;
    public readonly int HealthPercentage;
    public readonly int CurrentlyCastingSpellId;
    
    public Boss(string name, string unitName, int health, int maxHealth, int currentlyCastingSpellId)
    {
        Name = name;
        UnitName = unitName;
        Health = health;
        MaxHealth = maxHealth;
        HealthPercentage = maxHealth > 0 ? (health * 100) / maxHealth : 0;
        CurrentlyCastingSpellId = currentlyCastingSpellId;
    }
    
    public void LogBossInfo()
    {
        Logger.Log("Boss Info - " +
                   "Name: "  + Name + ", " +
                   "UnitName: " + UnitName + ", " +
                   "Health: " + Health + "/" + MaxHealth + " (" + HealthPercentage + "%), " +
                   "Casting Spell ID: " + CurrentlyCastingSpellId);
    }
}