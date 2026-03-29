public class FilterChainSelector : ISelector
{
    private readonly List<IUnitFilterChain> _unitFilterChain;

    public FilterChainSelector(List<IUnitFilterChain> unitFilterChain)
    {
        _unitFilterChain = unitFilterChain;
    }

    public Unit Select(Environment environment)
    {
        var units = UnitUtilities.GetAll(environment);

        foreach (var filterChain in _unitFilterChain)
        {
            units = filterChain.Filter(units);
        }

        return units.FirstOrDefault();
    }

    public void Log(Environment environment)
    {
        var units = UnitUtilities.GetAll(environment);

        foreach (var filterChain in _unitFilterChain)
        {
            Logger.Log("  " + filterChain.LogLine(units));
            units = filterChain.Filter(units);
        }
    }
}