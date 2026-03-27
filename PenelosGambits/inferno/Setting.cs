// This file is for development/IntelliSense only - NOT included in builds

using System.Collections.Generic;

public class Setting
{
    public string Name;
    public object Value;
    public int Min;
    public int Max;
    public List<string> Options;
    public bool IsLabel;

    public Setting(string name, bool defaultValue) { Name = name; Value = defaultValue; }
    public Setting(string name, int min, int max, int defaultValue) { Name = name; Min = min; Max = max; Value = defaultValue; }
    public Setting(string name, List<string> options, string defaultValue) { Name = name; Options = options; Value = defaultValue; }
    public Setting(string name, string defaultValue) { Name = name; Value = defaultValue; }
    public Setting(string labelText) { Name = labelText; IsLabel = true; }
}
