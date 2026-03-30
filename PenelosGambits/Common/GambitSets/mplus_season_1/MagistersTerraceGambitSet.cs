public class MagistersTerraceGambitSet : GambitSet
{
    private readonly GambitSet _defaultSet;
    private readonly Action _magicDispel;

    private List<Gambit> gambitSet;

    public MagistersTerraceGambitSet(GambitSet defaultSet, Action magicDispel)
    {
        _defaultSet = defaultSet;
        _magicDispel = magicDispel;

        gambitSet = GenerateGambitSet();
    }

    public override string GetName()
    {
        return "Magisters Terrace GambitSet";
    }

    private List<Gambit> GenerateGambitSet()
    {
        return new List<Gambit>
        {
            new Gambit(
                1,
                "Dispel Ethereal Shackles",
                new List<Condition>
                {
                    new ActionIsNotNullCondition(_magicDispel),
                    new InCombatCondition(),
                    new GroupMemberHasDebuffCondition("Ethereal Shackles"),
                    new IsSpellOffCooldownCondition(_magicDispel.GetName())
                },
                new FilterChainSelector(new List<IUnitFilterChain>
                {
                    new IsInRange(_magicDispel.GetName()),
                    new HasDebuff("Ethereal Shackles"),
                    new GetFirst()
                }),
                _magicDispel
            ),
            new Gambit(
                1,
                "Dispel Consuming Void",
                new List<Condition>
                {
                    new ActionIsNotNullCondition(_magicDispel),
                    new InCombatCondition(),
                    new GroupMemberHasDebuffCondition("Consuming Void"),
                    new IsSpellOffCooldownCondition(_magicDispel.GetName())
                },
                new FilterChainSelector(new List<IUnitFilterChain>
                {
                    new IsInRange(_magicDispel.GetName()),
                    new HasDebuff("Consuming Void"),
                    new GetFirst()
                }),
                _magicDispel
            ),
            
            // TODO only if Paladin
            new Gambit(
                2,
                "Blessing of Freedom Ethereal Shackles",
                new List<Condition>
                {
                    new InCombatCondition(),
                    new GroupMemberHasDebuffCondition("Ethereal Shackles"),
                    new IsSpellOffCooldownCondition(BlessingOfFreedomAction.Name)
                },
                new FilterChainSelector(new List<IUnitFilterChain>
                {
                    new IsInRange(BlessingOfFreedomAction.Name),
                    new HasDebuff("Ethereal Shackles"),
                    new GetFirst()
                }),
                new BlessingOfFreedomAction()
            ),
            new Gambit(
                3,
                "Dispel Holy Fire",
                new List<Condition>
                {
                    new ActionIsNotNullCondition(_magicDispel),
                    new InCombatCondition(),
                    new GroupMemberHasDebuffCondition("Holy Fire"),
                    new IsSpellOffCooldownCondition(_magicDispel.GetName())
                },
                new FilterChainSelector(new List<IUnitFilterChain>
                {
                    new IsInRange(_magicDispel.GetName()),
                    new HasDebuff("Holy Fire"),
                    new GetFirst()
                }),
                _magicDispel
            ),
            new Gambit(
                4,
                "Dispel Polymorph",
                new List<Condition>
                {
                    new ActionIsNotNullCondition(_magicDispel),
                    new InCombatCondition(),
                    new GroupMemberHasDebuffCondition("Polymorph"),
                    new IsSpellOffCooldownCondition(_magicDispel.GetName())
                },
                new FilterChainSelector(new List<IUnitFilterChain>
                {
                    new IsInRange(_magicDispel.GetName()),
                    new HasDebuff("Polymorph"),
                    new GetFirst()
                }),
                _magicDispel
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