using System.Collections.Generic;

public static class SpellMacroRegistry
{
    private static readonly Dictionary<string, SpellMacro> _macros = new Dictionary<string, SpellMacro>();

    public static void Register(string spellName, string macroName, string macroText)
    {
        _macros[spellName] = new SpellMacro(macroName, macroText);
    }

    public static bool HasMacro(string spellName)
    {
        return _macros.ContainsKey(spellName);
    }

    public static string GetMacroName(string spellName)
    {
        if (_macros.ContainsKey(spellName))
        {
            return _macros[spellName].MacroName;
        }
        return null;
    }

    public static Dictionary<string, string> GetAllMacros()
    {
        var result = new Dictionary<string, string>();
        foreach (var entry in _macros)
        {
            result[entry.Value.MacroName] = entry.Value.MacroText;
        }
        return result;
    }

    public static void Clear()
    {
        _macros.Clear();
    }
}

public class SpellMacro
{
    public string MacroName { get; private set; }
    public string MacroText { get; private set; }

    public SpellMacro(string macroName, string macroText)
    {
        MacroName = macroName;
        MacroText = macroText;
    }
}
