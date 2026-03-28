public class Target
{
    public readonly string name;
    public readonly int castingSpell;
    public readonly int health;
    public readonly int maxHealth;
    public readonly int healthPercentage;

    public Target(string name, int castingSpell, int health, int maxHealth)
    {
        this.name = name;
        this.castingSpell = castingSpell;
        this.health = health;
        this.maxHealth = maxHealth;
        this.healthPercentage = maxHealth > 0 ? (health * 100) / maxHealth : 0;
    }
}