public class PaladinHolyActionBook : ActionBook
{
    public List<string> GetDefaultActions()
    {
        return new List<string>
        {
            AvengingWrathAction.Name,
            CleanseAction.Name,
            DivineProtectionAction.Name,
            DivineTollAction.Name,
            FlashOfLightAction.Name,
            HolyShockDefensiveAction.Name,
            JudgmentAction.Name,
            LightOfDawnAction.Name,
            WordOfGloryAction.Name,
        };
    }

    public Dictionary<string, string> GetMacroActions()
    {
        return new Dictionary<string, string>
        {
            { CleanseAction.MacroName, CleanseAction.Macro },
            { DivineTollAction.MacroName, DivineTollAction.Macro },
            { FlashOfLightAction.MacroName, FlashOfLightAction.Macro },
            { HolyShockDefensiveAction.MacroName, HolyShockDefensiveAction.Macro },
            { WordOfGloryAction.MacroName, WordOfGloryAction.Macro },
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