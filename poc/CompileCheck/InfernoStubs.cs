// Minimal Inferno API stubs for compile-checking the POC rotation.
// These simulate what the Inferno runtime provides at load time.
// Only types referenced by rotation.cs are stubbed here.

using System;
using System.Collections.Generic;
using System.Drawing;

namespace InfernoWow.API
{
    // The Rotation base class — provided by Inferno runtime
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

        public bool GetCheckBox(string name) { return false; }
        public int GetSlider(string name) { return 0; }
        public string GetDropDown(string name) { return ""; }
        public string GetString(string name) { return ""; }
    }

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

    public static class Inferno
    {
        // Casting
        public static bool CanCast(string spell, string unit = "player", bool checkRange = true, bool checkCasting = false, bool ignoreGCD = false) => false;
        public static void Cast(string name, bool quickDelay = false) { }
        public static void StopCasting() { }

        // Spell Info
        public static int SpellCooldown(string spell) => 0;
        public static int SpellCharges(string spell) => 0;
        public static int MaxCharges(string spell) => 0;
        public static bool SpellUsable(string spell) => false;
        public static bool SpellInRange(string spell, string unit = "target") => false;
        public static bool IsSpellKnown(string spell) => false;
        public static int GCD() => 0;

        // Buffs/Debuffs
        public static bool HasBuff(string name, string unit = "player", bool byPlayer = true) => false;
        public static bool HasDebuff(string name, string unit = "target", bool byPlayer = true) => false;
        public static int BuffRemaining(string name, string unit = "player", bool byPlayer = true) => 0;
        public static int DebuffRemaining(string name, string unit = "target", bool byPlayer = true) => 0;
        public static int BuffStacks(string name, string unit = "player", bool byPlayer = true) => 0;
        public static int DebuffStacks(string name, string unit = "target", bool byPlayer = true) => 0;

        // Health/Power
        public static int Health(string unit) => 0;
        public static int MaxHealth(string unit) => 0;
        public static int Power(string unit, int powerType) => 0;
        public static int MaxPower(string unit, int powerType) => 0;

        // Unit State
        public static bool InCombat(string unit) => false;
        public static bool IsDead(string unit) => false;
        public static bool IsMoving(string unit) => false;
        public static bool UnitCanAttack(string unit1, string unit2) => false;
        public static bool PlayerIsMounted() => false;
        public static bool IsVisible(string unit) => false;
        public static int GetLevel(string unit) => 0;
        public static string GetSpec(string unit) => "";
        public static string UnitName(string unit) => "";
        public static float Haste(string unit) => 0f;

        // Casting Detection
        public static bool IsInterruptable(string unit) => false;
        public static bool IsChanneling(string unit) => false;
        public static int CastingID(string unit) => 0;
        public static string CastingName(string unit) => "";
        public static int CastingRemaining(string unit) => 0;

        // Distance
        public static float DistanceBetween(string unit1, string unit2) => 0f;
        public static int EnemiesNearUnit(float distance, string unit) => 0;
        public static int FriendsNearUnit(float distance, string unit) => 0;

        // Group
        public static int GroupSize() => 0;
        public static bool InParty() => false;
        public static bool InRaid() => false;

        // Items
        public static bool CanUseEquippedItem(int slot, bool checkCasting = false) => false;
        public static int InventoryItemID(int slot) => 0;
        public static int ItemCooldown(int itemId) => 0;

        // Misc
        public static void PrintMessage(string text, Color color = default, bool clear = false) { }
        public static int CombatTime() => 0;
        public static int GetMapID() => 0;
        public static bool IsOn() => false;
        public static bool IsCustomCodeOn(string code) => false;
    }
}
