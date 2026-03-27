public class Target
{
    public readonly string name;
    public readonly int castingSpell;
    public readonly int healthPercentage;

    public Target(string name, int castingSpell, int healthPercentage)
    {
        this.name = name;
        this.castingSpell = castingSpell;
        this.healthPercentage = healthPercentage;
    }
}