public class EnemiesInMeleeCondition : Condition
{
    private readonly int _minumumNumberOfEnemies;

    public EnemiesInMeleeCondition(int minumumNumberOfEnemies)
    {
        _minumumNumberOfEnemies = minumumNumberOfEnemies;
    }

    public bool IsMet(Environment environment)
    {
        return Inferno.EnemiesNearUnit(8, "player") >= _minumumNumberOfEnemies;
    }

    public void Consume()
    {
    }
}