public interface ActionBook
{
    List<string> GetDefaultActions();
    Dictionary<string, string> GetMacroActions();
    List<string> GetBuffActions();
    List<string> GetDebuffActions();
    List<string> GetCommands();
}