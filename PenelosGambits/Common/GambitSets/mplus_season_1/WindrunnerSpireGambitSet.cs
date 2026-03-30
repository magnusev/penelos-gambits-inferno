public class WindrunnerSpireGambitSet : GambitSet
{
    private readonly GambitSet _defaultSet;
    private readonly Action _magicDispel;
    private readonly Action _poisonDispel;

    private List<Gambit> gambitSet;

    public WindrunnerSpireGambitSet(
        GambitSet defaultSet,
        Action magicDispel,
        Action poisonDispel
    )
    {
        _defaultSet = defaultSet;
        _magicDispel = magicDispel;
        _poisonDispel = poisonDispel;

        gambitSet = GenerateGambitSet();
    }

    public override string GetName()
    {
        return "Windrunner Spire GambitSet";
    }

    private List<Gambit> GenerateGambitSet()
    {
        return new List<Gambit>
        {
            new Gambit(
                1,
                "Dispel Poison Spray",
                new List<Condition>
                {
                    new ActionIsNotNullCondition(_poisonDispel),
                    new InCombatCondition(),
                    new GroupMemberHasDebuffCondition("Poison Spray"),
                    new IsSpellOffCooldownCondition(_poisonDispel.GetName())
                },
                new FilterChainSelector(new List<IUnitFilterChain>
                {
                    new IsInRange(_poisonDispel.GetName()),
                    new HasDebuff("Poison Spray"),
                    new GetFirst()
                }),
                _poisonDispel
            )
        };
    }


    public override List<Gambit> GetGambits()
    {
        return gambitSet;
    }

    public override GambitSet GetNextGambitSet()
    {
        return _defaultSet;
    }
}