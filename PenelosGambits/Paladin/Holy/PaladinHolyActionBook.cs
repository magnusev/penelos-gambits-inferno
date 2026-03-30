public class PaladinHolyActionBook : ActionBook
{
    public List<string> GetDefaultActions()
    {
        return new List<string>
        {
            AvengingWrathAction.Name,
            BlessingOfFreedomAction.Name,
            CleanseAction.Name,
            DivineProtectionAction.Name,
            DivineTollAction.Name,
            FlashOfLightAction.Name,
            HolyLightAction.Name,
            HolyShockDefensiveAction.Name,
            JudgmentAction.Name,
            LightOfDawnAction.Name,
            ShieldOfTheRighteousAction.Name,
            WordOfGloryAction.Name,
        };
    }

    public Dictionary<string, string> GetMacroActions()
    {
        return new Dictionary<string, string>
        {
            { BlessingOfFreedomAction.MacroName, BlessingOfFreedomAction.Macro },
            { CleanseAction.MacroName, CleanseAction.Macro },
            { DivineTollAction.MacroName, DivineTollAction.Macro },
            { FlashOfLightAction.MacroName, FlashOfLightAction.Macro },
            { HolyLightAction.MacroName, HolyLightAction.Macro },
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