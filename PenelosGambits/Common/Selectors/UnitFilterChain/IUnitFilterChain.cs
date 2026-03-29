public abstract class IUnitFilterChain
{
    public abstract List<Unit> Filter(List<Unit> units);

    public abstract string LogLine(List<Unit> units);

    protected List<Unit> AsList(Unit unit)
    {
        if (unit == null) return new List<Unit>();

        return new List<Unit>
        {
            unit
        };
    }

    protected string Stringify(List<Unit> units)
    {
        return string.Join(", ", units.Select(u => u.Id));
    }
}