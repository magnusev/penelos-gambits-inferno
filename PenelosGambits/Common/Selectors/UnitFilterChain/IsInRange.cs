public class IsInRange : IUnitFilterChain
{
    private readonly string _spellName;

    public IsInRange(string spellName)
    {
        _spellName = spellName;
    }

    public override List<Unit> Filter(List<Unit> units)
    {
        return units
            .Where(u => Inferno.SpellInRange(_spellName, u.Id))
            .ToList();
    }
    
    public override string LogLine(List<Unit> units)
    {
        return "IsInRange (" + _spellName + "): " + Stringify(Filter(units));
    }

}