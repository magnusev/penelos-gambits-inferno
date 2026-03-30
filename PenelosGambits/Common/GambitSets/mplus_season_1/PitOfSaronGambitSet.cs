public class PitOfSaronGambitSet : GambitSet
{
    private readonly GambitSet _defaultSet;
    private readonly Action _magicDispel;
    private readonly Action _diseaseDispel;
    private readonly Action _poisonDispel;

    private List<Gambit> gambitSet;

    public PitOfSaronGambitSet(GambitSet defaultSet, Action magicDispel, Action poisonDispel, Action diseaseDispel)
    {
        _defaultSet = defaultSet;
        _magicDispel = magicDispel;
        _poisonDispel = poisonDispel;
        _diseaseDispel = diseaseDispel;

        gambitSet = GenerateGambitSet();
    }

    public override string GetName()
    {
        return "Pit of Saron GambitSet";
    }

    private List<Gambit> GenerateGambitSet()
    {
        return new List<Gambit>
        {
            new Gambit(
                1,
                "Dispel Cryoshards",
                new List<Condition>
                {
                    new ActionIsNotNullCondition(_magicDispel),
                    new InCombatCondition(),
                    new GroupMemberHasDebuffCondition("Cryoshards"),
                    new IsSpellOffCooldownCondition(_magicDispel.GetName())
                },
                new FilterChainSelector(new List<IUnitFilterChain>
                {
                    new IsInRange(_magicDispel.GetName()),
                    new HasDebuff("Cryoshards"),
                    new GetFirst()
                }),
                _magicDispel
            ),
            new Gambit(
                1,
                "Dispel Rotting Strikes",
                new List<Condition>
                {
                    new ActionIsNotNullCondition(_diseaseDispel),
                    new InCombatCondition(),
                    new GroupMemberHasDebuffCondition("Rotting Strikes", 3),
                    new IsSpellOffCooldownCondition(_diseaseDispel.GetName())
                },
                new FilterChainSelector(new List<IUnitFilterChain>
                {
                    new IsInRange(_diseaseDispel.GetName()),
                    new HasDebuff("Rotting Strikes"),
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