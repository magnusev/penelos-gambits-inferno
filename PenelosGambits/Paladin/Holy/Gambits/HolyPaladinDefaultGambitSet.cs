public class HolyPaladinDefaultGambitSet : GambitSet
{
    private readonly GambitSet DamageGambits = new HolyPaladinDamageGambitSet();

    public override string GetName()
    {
        return "Default Holy Paladin";
    }

    private List<Gambit> gambitSet = new List<Gambit>
    {
        new Gambit(1,
            "Divine Protection if player under 75% hp",
            new List<Condition>
            {
                new InCombatCondition(),
                new IsSpellOffCooldownCondition(DivineProtectionAction.Name),
                new UnitUnderThresholdCondition("player", 75)
            },
            null,
            new DivineProtectionAction()
        ),
        new Gambit(0,
            "Cast Divine Toll",
            new List<Condition>
            {
                new InCombatCondition(),
                new IsSpellOffCooldownCondition(DivineTollAction.Name),
                new MinimumGroupMembersUnderThreshold(80, 2),
                new PlayerSecondaryPowerLessThan(3, 9)
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
            "Light of Dawn if under 90% for 3+ units",
            new List<Condition>
            {
                new InCombatCondition(),
                new MinimumGroupMembersUnderThreshold(95, 5),
                new PlayerSecondaryPowerAtLeast(4, 9)
            },
            null,
            new LightOfDawnAction()
        ),
        new Gambit(2,
            "Word of Glory if under 90%",
            new List<Condition>
            {
                new InCombatCondition(),
                new LowestUnderHPThresholdCondition(95, WordOfGloryAction.Name),
                new PlayerSecondaryPowerAtLeast(3, 9)
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
            3,
            "Cast Holy Shock (Defensive)",
            new List<Condition>
            {
                new InCombatCondition(),
                new ThrottledCondition(2000)
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