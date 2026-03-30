public class GetUnitWithMostStacksOf : IUnitFilterChain
{
    private readonly string _debuffName;
    private readonly bool _byPlayer;

    public GetUnitWithMostStacksOf(string debuffName, bool byPlayer = false)
    {
        _debuffName = debuffName;
        _byPlayer = byPlayer;
    }

    public override List<Unit> Filter(List<Unit> units)
    {
        Unit mostStacks = units
            .Where(u => Inferno.HasDebuff(_debuffName, u.Id, _byPlayer))
            .OrderByDescending(u => Inferno.DebuffStacks(_debuffName, u.Id, _byPlayer))
            .FirstOrDefault();

        return AsList(mostStacks);
    }

    public override string LogLine(List<Unit> units)
    {
        return "GetUnitWithMostStacksOf (" + _debuffName + "): " + Stringify(Filter(units));
    }
}