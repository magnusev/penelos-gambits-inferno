public class PartyGroup : Group
{
    private readonly List<Unit> _members = new List<Unit>();

    public PartyGroup()
    {
        _members = CreateMembers();
    }

    private static List<Unit> CreateMembers()
    {
        var party = new List<Unit>();

        int i = 1;

        while (i < Inferno.GroupSize())
        {
            string id = "party" + i;
            var member = Create(id);
            party.Add(member);
            i++;
        }

        return party;
    }

    private static PartyUnit Create(string name)
    {
        return new PartyUnit(
            name,
            name,
            Inferno.GetSpec(name),
            Inferno.CastingID(name),
            Inferno.Health(name),
            Inferno.MaxHealth(name)
        );
    }

    public List<Unit> GetMembers()
    {
        return _members;
    }
}