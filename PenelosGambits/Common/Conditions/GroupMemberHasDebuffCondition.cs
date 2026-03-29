public class GroupMemberHasDebuffCondition : Condition
{
    private readonly string _debuffName;
    private readonly int _minimumDebuffStacks;

    public GroupMemberHasDebuffCondition(string debuffName, int minimumDebuffStacks = -1)
    {
        _debuffName = debuffName;
        _minimumDebuffStacks = minimumDebuffStacks;
    }

    public bool IsMet(Environment environment)
    {
        var membersWithDebuff = UnitUtilities.GetUnitsWithDebuff(environment, _debuffName);

        if (!membersWithDebuff.Any()) return false;
        if (_minimumDebuffStacks == -1) return true;

        foreach (var unit in membersWithDebuff)
        {
            if (Inferno.DebuffStacks(_debuffName, unit.Id, false) >= _minimumDebuffStacks) return true;
        }

        return false;
    }
}