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
            "Casting Shield of the Righteous",
            new List<Condition>
            {
                new InCombatCondition(),
                new PlayerSecondaryPowerAtLeast(4, 9),
                new EnemiesInMeleeCondition(1),
            },
            null,
            new ShieldOfTheRighteousAction()
        ),
        new Gambit(
            0,
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
        new Gambit(
            9999,
            "Cast Flash of Light",
            new List<Condition>
            {
                new InCombatCondition()
            },
            new FilterChainSelector(new List<IUnitFilterChain>
            {
                new IsNotDead(),
                new IsInRange(FlashOfLightAction.Name),
                new GetLowestUnit()
            }),
            new FlashOfLightAction()
        )

    };


    public override List<Gambit> GetGambits()
    {
        return gambitSet;
    }
}