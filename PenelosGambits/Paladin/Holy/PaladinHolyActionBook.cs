public class PaladinHolyActionBook : ActionBook
{
    public List<string> GetDefaultActions()
    {
        return new List<string>
        {
            HolyShockDefensiveAction.Name
        };
    }

    public Dictionary<string, string> GetMacroActions()
    {
        return new Dictionary<string, string>
        {
            { HolyShockDefensiveAction.MacroName, HolyShockDefensiveAction.Macro },
        };
    }

    public List<string> GetBuffActions()
    {
        return new List<string>();
    }

    public List<string> GetDebuffActions()
    {
        return new List<string>();
    }

    public List<string> GetCommands()
    {
        return new List<string>();
    }
}