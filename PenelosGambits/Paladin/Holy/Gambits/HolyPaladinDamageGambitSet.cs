public class HolyPaladinDamageGambitSet : GambitSet
{
    public override string GetName()
    {
        return "Default Holy Paladin Damage";
    }

    private List<Gambit> gambitSet = new List<Gambit>
    {
        new Gambit(
            -2,
            "Target Enemy",
            new List<Condition>
            {
                new InCombatCondition(),
                new TargetIsNotEnemyCondition()
            },
            null,
            new TargetEnemyAction()),
        new Gambit(
            -1,
            "Casting Judgment",
            new List<Condition>
            {
                new InCombatCondition(),
                new TargetIsEnemyCondition(),
                new PlayerSecondaryPowerLessThan(4, 9),
                new IsSpellOffCooldownCondition(JudgmentAction.Name)
            },
            null,
            new JudgmentAction()
        ),
    };


    public override List<Gambit> GetGambits()
    {
        return gambitSet;
    }
}