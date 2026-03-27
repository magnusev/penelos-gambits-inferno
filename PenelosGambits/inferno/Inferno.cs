// This file is for development/IntelliSense only - NOT included in builds
// The actual Inferno implementation is provided by the Inferno framework at runtime

using System;
using System.Collections.Generic;
using System.Drawing;

public static class Inferno
{
    // Casting
    public static bool CanCast(string SpellName, string unit = "player", bool CheckRange = true, bool CheckCasting = false, bool IgnoreGCD = false) { return false; }
    public static bool CanCast(int SpellId, string unit = "player", bool CheckRange = true, bool CheckCasting = false, bool IgnoreGCD = false) { return false; }
    public static void Cast(string Name, bool QuickDelay = false) { }
    public static void Cast(int SpellId, bool QuickDelay = false) { }
    public static void StopCasting() { }
    
    // Spell Info
    public static int SpellCooldown(string SpellName) { return 0; }
    public static int SpellCooldown(int SpellId) { return 0; }
    public static int SpellCharges(string SpellName) { return 0; }
    public static int SpellCharges(int SpellId) { return 0; }
    public static bool SpellUsable(string SpellName) { return false; }
    public static bool SpellUsable(int SpellId) { return false; }
    public static bool SpellInRange(string SpellName, string unit = "target") { return false; }
    public static bool SpellInRange(int SpellId, string unit = "target") { return false; }
    public static bool IsSpellKnown(string SpellName) { return false; }
    public static bool IsSpellKnown(int SpellId) { return false; }
    public static int GCD() { return 0; }
    
    // Buffs/Debuffs
    public static bool HasBuff(string BuffName, string unit = "player", bool ByPlayer = true) { return false; }
    public static bool HasBuff(int SpellId, string unit = "player", bool ByPlayer = true) { return false; }
    public static bool HasDebuff(string DebuffName, string unit = "target", bool ByPlayer = true) { return false; }
    public static bool HasDebuff(int SpellId, string unit = "target", bool ByPlayer = true) { return false; }
    public static int BuffRemaining(string BuffName, string unit = "player", bool ByPlayer = true) { return 0; }
    public static int BuffRemaining(int SpellId, string unit = "player", bool ByPlayer = true) { return 0; }
    public static int DebuffRemaining(string DebuffName, string unit = "target", bool ByPlayer = true) { return 0; }
    public static int DebuffRemaining(int SpellId, string unit = "target", bool ByPlayer = true) { return 0; }
    public static int BuffStacks(string BuffName, string unit = "player", bool ByPlayer = true) { return 0; }
    public static int BuffStacks(int SpellId, string unit = "player", bool ByPlayer = true) { return 0; }
    public static int DebuffStacks(string DebuffName, string unit = "target", bool ByPlayer = true) { return 0; }
    public static int DebuffStacks(int SpellId, string unit = "target", bool ByPlayer = true) { return 0; }
    
    // Health/Power
    public static int Health(string Unit) { return 0; }
    public static int MaxHealth(string Unit) { return 0; }
    public static int Power(string Unit, int PowerType) { return 0; }
    public static int MaxPower(string Unit, int PowerType) { return 0; }
    
    // Unit State
    public static bool InCombat(string Unit) { return false; }
    public static bool IsDead(string Unit) { return false; }
    public static bool IsMoving(string Unit) { return false; }
    public static bool PlayerIsMounted() { return false; }
    public static bool IsVisible(string Unit) { return false; }
    public static int GetLevel(string Unit) { return 0; }
    public static string UnitName(string Unit) { return ""; }
    
    // Casting Detection
    public static bool IsInterruptable(string Unit) { return false; }
    public static bool IsChanneling(string Unit) { return false; }
    public static string CastingName(string Unit) { return ""; }
    public static int CastingRemaining(string Unit) { return 0; }
    
    // Distance
    public static float DistanceBetween(string Unit1, string Unit2) { return 0; }
    public static int EnemiesNearUnit(float Distance, string Unit) { return 0; }
    public static int FriendsNearUnit(float Distance, string Unit) { return 0; }
    
    // Group
    public static int GroupSize() { return 0; }
    public static bool InParty() { return false; }
    public static bool InRaid() { return false; }
    
    // Items
    public static bool CanUseEquippedItem(int Slot, bool CheckCasting = false) { return false; }
    public static int InventoryItemID(int Slot) { return 0; }
    public static int ItemCooldown(int ItemID) { return 0; }
    
    // Misc
    public static void PrintMessage(string text, Color color = default(Color), bool clear = false) { }
    public static int CombatTime() { return 0; }
    public static bool IsOn() { return false; }
    public static bool IsCustomCodeOn(string Code) { return false; }
}
