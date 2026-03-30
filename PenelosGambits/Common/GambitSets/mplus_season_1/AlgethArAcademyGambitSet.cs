public class AlgethArAcademyGambitSet : GambitSet
{
    private readonly GambitSet _defaultSet;
    private readonly Action _magicDispel;
    private readonly Action _poisonDispel;
    private readonly Action _diseaseDispel;


    private List<Gambit> gambitSet;

    public AlgethArAcademyGambitSet(
        GambitSet defaultSet,
        Action magicDispel,
        Action poisonDispel,
        Action diseaseDispel
    )
    {
        _defaultSet = defaultSet;
        _magicDispel = magicDispel;
        _poisonDispel = poisonDispel;
        _diseaseDispel = diseaseDispel;

        gambitSet = GenerateGambitSet();
    }

    public override string GetName()
    {
        return "Algeth'ar Academy GambitSet";
    }

    private List<Gambit> GenerateGambitSet()
    {
        return new List<Gambit>
        {
            new Gambit(
                1,
                "Dispel Lasher Toxin",
                new List<Condition>
                {
                    new ActionIsNotNullCondition(_poisonDispel),
                    new InCombatCondition(),
                    new GroupMemberHasDebuffCondition("Lasher Toxin", 2),
                    new IsSpellOffCooldownCondition(_poisonDispel.GetName())
                },
                new FilterChainSelector(new List<IUnitFilterChain>
                {
                    new IsInRange(_poisonDispel.GetName()),
                    new GetUnitWithMostStacksOf("Lasher Toxin")
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