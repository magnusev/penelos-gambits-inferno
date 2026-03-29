public class GetFirst : IUnitFilterChain
{
    public override List<Unit> Filter(List<Unit> units)
    {
        var first = units.FirstOrDefault();

        return AsList(first);
    }

    public override string LogLine(List<Unit> units)
    {
        return "GetFirst: " + Stringify(Filter(units));
    }
}