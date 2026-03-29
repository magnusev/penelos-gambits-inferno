public class InCombatCondition : Condition
{
    public bool IsMet(Environment environment)
    {
        return Inferno.InCombat("player");
    }
}