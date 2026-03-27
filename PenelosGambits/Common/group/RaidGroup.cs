public class RaidGroup : Group
{
    private readonly List<Unit> _members = new List<Unit>();

    public RaidGroup()
    {
        _members = CreateMembers();
    }


    public List<Unit> GetMembers()
    {
        return _members;
    }
    
    private static List<Unit> CreateMembers()
    {
        var party = new List<Unit>();

        int i = 1;

        while (i <= Inferno.GroupSize())
        {
            string id = "raid" + i;
            var member = Create(id);
            party.Add(member);
            i++;
        }

        return party;
    }

    private static RaidUnit Create(string name)
    {
        return new RaidUnit(
            name,
            name,
            Inferno.GetSpec(name),
            Inferno.CastingID(name),
            Inferno.Health(name)
        );
    }
}