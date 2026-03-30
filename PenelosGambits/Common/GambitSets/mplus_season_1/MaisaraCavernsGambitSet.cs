public class MaisaraCavernsGambitSet : GambitSet
{
    private readonly GambitSet _defaultSet;
    private readonly Action _magicDispel;
    private readonly Action _poisonDispel;
    private readonly Action _diseaseDispel;
    

    private List<Gambit> gambitSet;

    public MaisaraCavernsGambitSet(
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
        return "Maisara Caverns GambitSet";
    }

    private List<Gambit> GenerateGambitSet()
    {
        return new List<Gambit>
        {
            new Gambit(
                1,
                "Dispel Infected Pinions",
                new List<Condition>
                {
                    new ActionIsNotNullCondition(_diseaseDispel),
                    new InCombatCondition(),
                    new GroupMemberHasDebuffCondition("Infected Pinions"),
                    new IsSpellOffCooldownCondition(_diseaseDispel.GetName())
                },
                new FilterChainSelector(new List<IUnitFilterChain>
                {
                    new IsInRange(_diseaseDispel.GetName()),
                    new HasDebuff("Infected Pinions"),
                    new GetFirst()
                }),
                _diseaseDispel
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