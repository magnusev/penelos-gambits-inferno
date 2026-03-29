public static class UnitUtilities
{
    public static List<Unit> GetUnitsWithDebuff(Environment environment, string debuffName)
    {
        List<Unit> units = new List<Unit>();

        if (Inferno.HasDebuff(debuffName, environment.Player.Id, false))
        {
            units.Add(environment.Player);
        }

        environment.Group.GetMembers().ForEach(member =>
        {
            if (Inferno.HasDebuff(debuffName, member.Id, false))
            {
                units.Add(member);
            }
        });

        return units;
    }

    public static List<Unit> GetAll(Environment environment)
    {
        List<Unit> units = new List<Unit>(environment.Group.GetMembers());
        units.Add(environment.Player);

        return units;
    }
}