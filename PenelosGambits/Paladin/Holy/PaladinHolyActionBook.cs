public class PaladinHolyActionBook : ActionBook
{
    public List<string> GetDefaultActions()
    {
        return new List<string>
        {
            FlashOfLightAction.Name,
            HolyShockDefensiveAction.Name
        };
    }

    public Dictionary<string, string> GetMacroActions()
    {
        return new Dictionary<string, string>
        {
            { FlashOfLightAction.MacroName, FlashOfLightAction.Macro },
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