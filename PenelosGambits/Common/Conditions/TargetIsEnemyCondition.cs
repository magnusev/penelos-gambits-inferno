public class TargetIsEnemyCondition : Condition
{
    public bool IsMet(Environment environment)
    {
        return Inferno.UnitCanAttack("player", "target");
    }

    public void Consume() { }
}