public class HasDebuff : IUnitFilterChain
{
    private readonly string _debuffName;
    private readonly bool _byPlayer;

    public HasDebuff(string debuffName, bool byPlayer = false)
    {
        _debuffName = debuffName;
        _byPlayer = byPlayer;
    }

    public override List<Unit> Filter(List<Unit> units)
    {
        return units
            .Where(u => Inferno.HasDebuff(_debuffName, u.Id, _byPlayer))
            .ToList();
    }

    public override string LogLine(List<Unit> units)
    {
        return "HasDebuff: " + Stringify(Filter(units));
    }
}