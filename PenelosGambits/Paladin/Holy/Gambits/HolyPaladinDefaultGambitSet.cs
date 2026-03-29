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
            1,
            "Cast Holy Light (Defensive)",
            new List<Condition>
            {
                new InCombatCondition(),
                new IsSpellOffCooldownCondition(HolyShockDefensiveAction.Name)
            },
            new FilterChainSelector(new List<IUnitFilterChain>
            {
                new IsNotDead(),
                new IsInRange(HolyShockDefensiveAction.Name),
                new GetLowestUnit()
            }),
            new HolyShockDefensiveAction()
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