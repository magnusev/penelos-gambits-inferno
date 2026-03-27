public class Environment
{
    public PlayerUnit Player { get; private set; }
    public Group Group { get; private set; }

    public List<Boss> Bosses { get; private set; }
    public Target Target { get; private set; }

    public int MapId { get; private set; }

    public Environment(List<Boss> OldBosses)
    {
        Player = CreatePlayer();
        Group = CreateGroup();
        Target = CreateTarget();
        Bosses = CreateBosses();
        MapId = Inferno.GetMapID();

        LogBossChanges(OldBosses);
    }

    private PlayerUnit CreatePlayer()
    {
        return new PlayerUnit(
            "player",
            "player",
            Inferno.GetSpec("player"),
            Inferno.CastingID("player"),
            Inferno.Health("player")
        );
    }

    private Group CreateGroup()
    {
        if (Inferno.InParty() && !Inferno.InRaid())
        {
            return new PartyGroup();
        }

        if (Inferno.InRaid())
        {
            return new RaidGroup();
        }

        return new Solo();
    }

    private Target CreateTarget()
    {
        if (!UnitExists("target")) return null;

        return new Target(
            Inferno.UnitName("target"),
            Inferno.CastingID("target"),
            Inferno.Health("target")
        );
    }

    public static List<Boss> CreateBosses()
    {
        var bosses = new List<Boss>();
        if (UnitExists("boss1")) bosses.Add(CreateBoss("boss1"));
        if (UnitExists("boss2")) bosses.Add(CreateBoss("boss2"));
        if (UnitExists("boss3")) bosses.Add(CreateBoss("boss3"));
        if (UnitExists("boss4")) bosses.Add(CreateBoss("boss4"));

        return bosses;
    }

    private static Boss CreateBoss(string name)
    {
        var bossId = Inferno.UnitName(name);
        var bossName = name + " (" + bossId + ")";

        return new Boss(name, bossId, Inferno.Health(name), Inferno.CastingID(name));
    }

    private static bool UnitExists(string unitId)
    {
        var unitName = Inferno.UnitName(unitId);
        return !string.IsNullOrEmpty(unitName);
    }

    private void LogBossChanges(List<Boss> oldBosses)
    {
        var bossesAppeared = Bosses
            .Where(newBoss => !oldBosses.Any(oldBoss => oldBoss.Name.Equals(newBoss.Name)))
            .ToList();

        foreach (var boss in bossesAppeared)
        {
            Inferno.PrintMessage("Boss appeared: " + boss.UnitName);
        }

        var bossesDisappeared = oldBosses
            .Where(oldBoss => !Bosses.Any(newBoss => newBoss.Name.Equals(oldBoss.Name)))
            .ToList();

        foreach (var boss in bossesDisappeared)
        {
            Inferno.PrintMessage("Boss dissapeared: " + boss.UnitName);
        }
    }
}