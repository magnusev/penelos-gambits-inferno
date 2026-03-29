public class HolyPaladinDefaultGambitSet : GambitSet
{
    private readonly GambitSet DamageGambits = new HolyPaladinDamageGambitSet();

    public override string GetName()
    {
        return "Default Holy Paladin";
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
                new PlayerSecondaryPowerLessThan(5, 9),
                new IsSpellOffCooldownCondition(JudgmentAction.Name)
            },
            null,
            new JudgmentAction()
        ),
        new Gambit(0,
            "Cast Divine Toll",
            new List<Condition>
            {
                new InCombatCondition(),
                new IsSpellOffCooldownCondition(DivineTollAction.Name),
            },
            new FilterChainSelector(new List<IUnitFilterChain>
            {
                new IsNotDead(),
                new IsInRange(DivineTollAction.Name),
                new GetLowestUnit()
            }),
            new DivineTollAction()
        ),
        new Gambit(1,
            "Word of Glory if under 90%",
            new List<Condition>
            {
                new InCombatCondition(),
                new LowestUnderHPThresholdCondition(85, WordOfGloryAction.Name),
                new PlayerSecondaryPowerAtLeast(5, 9)
            },
            new FilterChainSelector(new List<IUnitFilterChain>
            {
                new IsNotDead(),
                new IsInRange(WordOfGloryAction.Name),
                new GetLowestUnit()
            }),
            new WordOfGloryAction()
        ),
        new Gambit(
            2,
            "Cast Holy Shock (Defensive)",
            new List<Condition>
            {
                new InCombatCondition(),
                new IsSpellOffCooldownCondition(HolyShockDefensiveAction.Name),
                new CanCastCondition(HolyShockDefensiveAction.Name),
                new HasMoreThanChargesCondition(HolyShockDefensiveAction.Name, 1)
            },
            new FilterChainSelector(new List<IUnitFilterChain>
            {
                new IsNotDead(),
                new IsInRange(HolyShockDefensiveAction.Name),
                new GetLowestUnit()
            }),
            new HolyShockDefensiveAction()
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

    public override GambitSet GetNextGambitSet()
    {
        return DamageGambits;
    }
}