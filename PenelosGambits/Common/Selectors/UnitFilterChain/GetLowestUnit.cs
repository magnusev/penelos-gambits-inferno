public class GetLowestUnit : IUnitFilterChain
{
    public override List<Unit> Filter(List<Unit> units)
    {
        Unit lowest = units
            .OrderBy(unit => unit.HealthPercentage)
            .FirstOrDefault();

        return AsList(lowest);
    }

    public override string LogLine(List<Unit> units)
    {
        return "GetLowestUnit: " + Stringify(Filter(units));
    }
}