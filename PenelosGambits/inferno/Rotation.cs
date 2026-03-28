// This file is for development/IntelliSense only - NOT included in builds

using System.Collections.Generic;

public abstract class Rotation
{
    public List<string> Spellbook = new List<string>();
    public List<string> Debuffs = new List<string>();
    public List<string> Buffs = new List<string>();
    public List<string> CustomCommands = new List<string>();
    public Dictionary<string, string> Macros = new Dictionary<string, string>();
    public Dictionary<string, string> CustomFunctions = new Dictionary<string, string>();
    public List<Setting> Settings = new List<Setting>();

    public virtual void LoadSettings() { }
    public abstract void Initialize();
    public virtual bool CombatTick() { return false; }
    public virtual bool MountedTick() { return false; }
    public virtual bool OutOfCombatTick() { return false; }
    public virtual void CleanUp() { }
    public virtual void OnStop() { }
    
    public bool GetCheckBox(string SettingName) { return false; }
    public int GetSlider(string SettingName) { return 0; }
    public string GetDropDown(string SettingName) { return ""; }
    public string GetString(string SettingName) { return ""; }
}
