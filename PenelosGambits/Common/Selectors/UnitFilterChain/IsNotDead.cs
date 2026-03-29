public class IsNotDead : IUnitFilterChain
{
    public override List<Unit> Filter(List<Unit> units)
    {
        return units
            .Where(u => !u.IsDead())
            .ToList();
    }
    
    public override string LogLine(List<Unit> units)
    {
        return "IsNotDead " + Stringify(Filter(units));
    }
}