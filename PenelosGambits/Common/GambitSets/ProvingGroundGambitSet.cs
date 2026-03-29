public class ProvingGroundGambitSet : GambitSet
{
    private readonly GambitSet _defaultSet;
    private readonly Action _magicDispel;

    private List<Gambit> gambitSet;

    public ProvingGroundGambitSet(GambitSet defaultSet, Action magicDispel)
    {
        _defaultSet = defaultSet;
        _magicDispel = magicDispel;

        gambitSet = GenerateGambitSet();
    }

    public override string GetName()
    {
        return "Proving Grounds GambitSet";
    }

    private List<Gambit> GenerateGambitSet()
    {
        return new List<Gambit>
        {
            new Gambit(
                1,
                "Dispel Aqua Bomb",
                new List<Condition>
                {
                    new InCombatCondition(),
                    new GroupMemberHasDebuffCondition("Aqua Bomb"),
                    new IsSpellOffCooldownCondition(_magicDispel.GetName())
                },
                new FilterChainSelector(new List<IUnitFilterChain>
                {
                    new IsInRange(_magicDispel.GetName()),
                    new HasDebuff("Aqua Bomb"),
                    new GetFirst()
                }),
                _magicDispel
            ),
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