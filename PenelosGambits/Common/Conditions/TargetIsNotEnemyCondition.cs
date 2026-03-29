public class TargetIsNotEnemyCondition : Condition
{
    public bool IsMet(Environment environment)
    {
        return !Inferno.UnitCanAttack("player", "target");
    }
}