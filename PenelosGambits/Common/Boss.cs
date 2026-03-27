public class Boss
{
    public readonly string Name;
    public readonly string UnitName;
    public readonly int HealthPercentage;
    public readonly int CurrentlyCastingSpellId;
    
    public Boss(string name, string unitName, int healthPercentage, int currentlyCastingSpellId)
    {
        Name = name;
        UnitName = unitName;
        HealthPercentage = healthPercentage;
        CurrentlyCastingSpellId = currentlyCastingSpellId;
    }
}