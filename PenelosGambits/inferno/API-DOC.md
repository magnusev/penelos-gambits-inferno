# Inferno API Documentation

## Overview

The Inferno API (`InfernoWow.API.Inferno`) provides the interface between rotation/plugin scripts and the game. All methods are static and can be called directly from your rotation or plugin code.

**Supported Modes:** Retail and Classic (Classic, Classic Era, Anniversary). The mode is set in Advanced Settings. Some methods are retail-only, some are classic-only, and many work in both modes. Each method's availability is noted below.

**Important Notes:**
- AoE spells can only be cast with `[@cursor]` or `[@player]` macros. See Meteor in the example rotation.
- The bot uses Numpad 0-9, F1-F12, and `[` `]` keys internally. Do NOT bind these to anything in-game.
- Load and start the rotation BEFORE logging in. After logging in, type `/reload` in chat to load the addon's macros and keybinds.

---

## Slash Commands

Inferno slash commands use the first 5 lowercase letters of your addon name. For example, if your addon is "DragonHunterHelper", the prefix is `/drago`.

| Command | Description |
|---------|-------------|
| `/drago toggle` | Pause and unpause the rotation in-game |
| `/drago wait #` | Pause for # seconds, then auto-resume |
| `/drago COMMAND` | Toggle a custom command on/off (must be added in Initialize) |
| `/drago COMMAND #` | Toggle a custom command on, auto-off after # seconds |
| `/drago queue SpellName` | Queue a spell (retrievable via `GetSpellQueue()`) |

---

## Rotation and Plugin Classes

### Rotation (abstract)

```csharp
public abstract class Rotation
{
    // Lists - populate in Initialize()
    public List<string> Spellbook;        // Spell names to register
    public List<string> CustomCommands;   // Custom toggle commands
    public List<string> Buffs;            // Classic only - buff names to track
    public List<string> Debuffs;          // Classic only - debuff names to track
    public List<string> Items;            // Classic only - item names to track
    public List<string> EnemySpells;      // Classic only - enemy spell names
    public List<string> Totems;           // Classic only - totem names
    public List<string> Talents;          // Classic only - talent names
    public Dictionary<string, string> Macros;          // Macro registration (key=name, value=macro text)
    public Dictionary<string, string> CustomFunctions;  // Custom Lua functions (key=name, value=lua code)
    public List<Setting> Settings;        // User-adjustable settings

    // Override these methods
    virtual void LoadSettings() { }       // Add settings before Initialize
    abstract void Initialize();           // Required - register spells, macros, etc.
    virtual bool CombatTick() { }         // Runs each tick while in combat
    virtual bool MountedTick() { }        // Runs each tick while mounted
    virtual bool OutOfCombatTick() { }    // Runs each tick while out of combat
    virtual void CleanUp() { }            // Runs after every tick
    virtual void OnStop() { }             // Runs when rotation is stopped

    // Setting accessors
    bool GetCheckBox(string SettingName);
    int GetSlider(string SettingName);
    string GetDropDown(string SettingName);
    string GetString(string SettingName);
}
```

### Plugin (same interface as Rotation)

Plugins run before the rotation each tick. If a plugin's tick method returns `true`, the rotation's tick is skipped for that cycle. Plugins listed higher in the plugin manager have higher priority.

### Setting Types

```csharp
// Checkbox (bool)
Settings.Add(new Setting("Use Cooldowns", true));

// Slider (int)
Settings.Add(new Setting("Health %", 1, 100, 75));

// Dropdown (string selection)
Settings.Add(new Setting("Mode", new List<string>(new string[] { "PvE", "PvP", "Auto" }), "PvE"));

// Text input (string)
Settings.Add(new Setting("Custom Potion", "potion name"));

// Label (visual only)
Settings.Add(new Setting("--- Advanced ---"));
```

### Tick Return Values

Tick methods return `bool`. Returning `true` signals that an action was performed that changes game state, causing the bot to immediately start a new tick (refreshing all cached data). Returning `false` continues to the next priority level (next plugin, or the rotation).

---

## Inferno API Reference

### Fields

| Field | Type | Description |
|-------|------|-------------|
| `IsRetail` | bool | True if running in retail mode |
| `Latency` | int | Simulated latency in ms (default 150). Used by CanCast for cooldown tolerance. |
| `Settings` | Dictionary | All registered settings (keyed by rotation/plugin name) |
| `AddonName` | string | Current addon name |
| `AddonDirectory` | string | WoW addons directory path |

---

### Casting and Spells

All spell methods require the spell to be registered with `Spellbook.Add("Spell Name")` in `Initialize()`.

#### CanCast
```csharp
bool CanCast(string SpellName, string unit = "target", bool CheckRange = true,
             bool CheckCasting = false, bool IgnoreGCD = false)
bool CanCast(int SpellId, string unit = "target", bool CheckRange = true,
             bool CheckCasting = false, bool IgnoreGCD = false)
```
Returns true if the spell can be cast. Checks: spell known, spell usable (resources), cooldown ready (within GCD + Latency tolerance), charges available, optionally range and casting state.

| Mode | Availability |
|------|-------------|
| Retail | String and int overloads |
| Classic | String overload only |

#### Cast
```csharp
void Cast(string Name, bool QuickDelay = false)
void Cast(int SpellId, bool QuickDelay = false)
```
Casts a spell from the Spellbook or a macro from the Macros list. `QuickDelay = true` uses a faster key repeat (100ms vs 200ms), useful for off-GCD abilities.
Ground targeted or spells that require aiming need to be cast through macros (For example, Earthquake needs `Macros.Add("EarthquakeGround", "/cast [@cursor] Earthquake")` and then later `Inferno.Cast("EarthquakeGround")`

| Mode | Availability |
|------|-------------|
| Retail | String and int overloads |
| Classic | String overload only |

#### StopCasting
```csharp
void StopCasting()
```
Interrupts the player's current cast or channel. Works in both modes.

#### Spell Information

| Method | Returns | Description | Mode |
|--------|---------|-------------|------|
| `SpellCooldown(string/int)` | int (ms) | Remaining cooldown or recharge time if at 0 charges | Both |
| `SpellCharges(string/int)` | int | Current charges (0 if no charges) | Both |
| `MaxCharges(string/int)` | int | Max charges (0 if no charges) | Both |
| `RechargeTime(string/int)` | int (ms) | Time until next charge gained | Both |
| `ChargesFractional(string/int, int chargeDurationMs)` | float | Fractional charges (e.g. 1.7 = 1 ready + 70% through next). `chargeDurationMs` is the base recharge time in ms (look up on Wowhead). Example: `ChargesFractional("Solar Eclipse", 15000) > 1.5f` | Retail only |
| `FullRechargeTime(string/int, int chargeDurationMs)` | int (ms) | Time until ALL charges are fully recharged. Example: `FullRechargeTime("Celestial Alignment", 60000) > 15000` | Retail only |
| `SpellUsable(string/int)` | bool | Known and enough resources | Both |
| `SpellInRange(string/int, unit)` | bool | Target in range of spell | Both |
| `SpellNameToID(string)` | int | Converts spell name to spell ID | Retail only |
| `IsSpellKnown(int/string)` | bool | Spell is known (includes talents) | Retail only |
| `SpellEnabled(string)` | bool | Spell is enabled | Classic only (use SpellUsable in retail) |
| `GCD()` | int (ms) | Remaining global cooldown | Both |
| `ProjectileSpeed(int SpellId)` | int | Projectile speed for the spell | Retail only |

---

### Unit Information

Unit tokens: `"player"`, `"target"`, `"focus"`, `"party1"`-`"party4"`, `"arena1"`-`"arena3"`, `"boss1"`-`"boss4"`, `"raid1"`-`"raid40"`.

#### Health and Power

| Method | Returns | Description | Mode |
|--------|---------|-------------|------|
| `Health(string Unit)` | int | Current HP percentage (0-100) | Both |
| `MaxHealth(string Unit)` | int | Maximum HP value | Both |
| `Power(string Unit, int PowerType)` | int | Current power value | Both |
| `MaxPower(string Unit, int PowerType)` | int | Maximum power value | Both |

PowerType values: 0=Mana, 1=Rage, 2=Focus, 3=Energy, 4=Combo Points, 5=Runes, 6=Runic Power, 7=Soul Shards, 8=Lunar Power, 9=Holy Power, 11=Maelstrom, 12=Chi, 13=Insanity, 16=Arcane Charges, 17=Fury, 18=Pain, 19=Essence. See: https://warcraft.wiki.gg/wiki/Enum.PowerType

**Classic-only aliases** (use the universal methods above instead):

| Classic Method | Equivalent |
|---------------|------------|
| `TargetCurrentHP()` | `Health("target")` |
| `TargetMaxHP()` | `MaxHealth("target")` |
| `UnitCurrentHP(unit)` | `Health(unit)` |
| `UnitMaxHP(unit)` | `MaxHealth(unit)` |
| `TargetExactCurrentHP()` | Exact HP (not percentage) |
| `TargetExactMaxHP()` | Exact max HP |
| `PlayerMaxPower()` | `MaxPower("player")` |
| `PlayerSecondaryPower()` | Secondary resource |
| `Mana(unit)` | `Power(unit, 0)` |

#### Unit State

| Method | Returns | Description | Mode |
|--------|---------|-------------|------|
| `InCombat(string Unit)` | bool | Unit is in combat | Both |
| `IsDead(string Unit)` | bool | Unit is dead | Both |
| `IsGhost(string Unit)` | bool | Unit is a ghost | Both |
| `GetSpeed(string Unit)` | float | Current speed (0 = stationary) | Both |
| `IsMoving(string Unit)` | bool | Unit is moving | Both |
| `PlayerIsMounted()` | bool | Player is mounted | Both |
| `PlayerIsOutdoors()` | bool | Player is outdoors | Both |
| `UnitCanAttack(string Unit1, Unit2)` | bool | Unit2 can attack Unit1 | Both |
| `IsBoss(string Unit)` | bool | Unit is a boss mob | Both |
| `GetLevel(string Unit)` | int | Unit's level | Both |
| `GetSpec(string Unit)` | string | Spec as "Class: Spec" (e.g. "Paladin: Holy") | Retail only |
| `GetRace(string Unit)` | string | Unit's race name | Both |
| `UnitName(string Unit)` | string | Unit's name | Retail only |
| `IsVisible(string Unit)` | bool | Unit is visible (not phased/despawned) | Retail only |
| `Haste(string Unit)` | float | Haste percentage (e.g. 39.23) | Both |
| `ThreatPercent(string Unit, string Unit2)` | float | Unit's threat percentage on Unit2 (0-100+). Defaults: Unit="player", Unit2="target" | Retail only |
| `ThreatLevel(string Unit, string Unit2)` | int | Unit's threat status on Unit2. 0=None, 1=Threat (not tanking but high threat), 2=Tanking (securely tanking), 3=Primary (tanking and highest threat). Defaults: Unit="player", Unit2="target" | Retail only |

**Classic-only aliases:**

| Classic Method | Equivalent |
|---------------|------------|
| `PlayerIsDead()` | `IsDead("player")` |
| `PlayerIsMoving()` | `IsMoving("player")` |
| `PlayerIsPvP()` | Player is flagged for PvP |
| `PlayerHasPet()` | Player has an active pet |
| `PlayerInVehicle()` | Player is in a vehicle |
| `TargetIsEnemy()` | `UnitCanAttack("target")` |
| `TargetIsBoss()` | `IsBoss("target")` |
| `GetPlayerLevel()` | `GetLevel("player")` |
| `GetPlayerRace()` | `GetRace("player")` |
| `TargetIsUnit(string)` | Check if target matches a unit token |
| `UnitID(string unit)` | NPC ID of the unit |
| `Crit()` | Player crit percentage |

---

### Buffs and Debuffs

All buff/debuff methods support both string (spell name) and int (spell ID) lookups.

| Method | Returns | Description | Mode |
|--------|---------|-------------|------|
| `HasBuff(string/int, unit, ByPlayer)` | bool | Unit has the buff | Both |
| `HasDebuff(string/int, unit, ByPlayer)` | bool | Unit has the debuff | Both |
| `BuffRemaining(string/int, unit, ByPlayer)` | int (ms) | Time remaining on buff | Both |
| `DebuffRemaining(string/int, unit, ByPlayer)` | int (ms) | Time remaining on debuff | Both |
| `BuffDuration(string/int, unit, ByPlayer)` | int (ms) | Total duration of buff | Retail only |
| `DebuffDuration(string/int, unit, ByPlayer)` | int (ms) | Total duration of debuff | Retail only |
| `BuffStacks(string/int, unit, ByPlayer)` | int | Stack count | Both |
| `DebuffStacks(string/int, unit, ByPlayer)` | int | Stack count | Both |
| `BuffInfoDetailed(unit, name, ByPlayer)` | List | Detailed buff info | Classic only |
| `DebuffInfoDetailed(unit, name, ByPlayer)` | List | Detailed debuff info | Classic only |

**Parameters:**
- `unit`: Unit token (default "player" for buffs, "target" for debuffs)
- `ByPlayer`: If true (default), only matches auras cast by the player

**Classic note:** In classic mode, buff/debuff names must be registered in `Initialize()` with `Buffs.Add("name")` or `Debuffs.Add("name")`.

---

### Casting Detection

| Method | Returns | Description | Mode |
|--------|---------|-------------|------|
| `CastingID(string Unit)` | int | Spell ID being cast/channeled (0 if none) | Both |
| `CastingName(string Unit)` | string | Spell name being cast/channeled | Retail only |
| `IsInterruptable(string Unit)` | bool | Current cast is interruptable | Both |
| `IsChanneling(string Unit)` | bool | Unit is channeling | Both |
| `CastingElapsed(string Unit)` | int (ms) | Time elapsed on current cast | Both |
| `CastingRemaining(string Unit)` | int (ms) | Time remaining on current cast | Both |
| `CurrentEmpowerStage(string Unit)` | int | Current empower stage (Evoker) | Retail only |

**Classic-only:**

| Method | Description |
|--------|-------------|
| `LastCast()` | Name of last spell cast |
| `LastCastID(string unit)` | Spell ID of last cast |
| `EnemySpellCast()` | Enemy's current spell name |

---

### Distance and Proximity

| Method | Returns | Description | Mode |
|--------|---------|-------------|------|
| `DistanceBetween(Unit1, Unit2)` | float | Distance between two units | Both |
| `EnemiesNearUnit(float Distance, Unit)` | int | Enemies within Distance of Unit | Both |
| `FriendsNearUnit(float Distance, Unit)` | int | Friendly units within Distance | Both |

**Classic-only aliases:**

| Method | Equivalent |
|--------|------------|
| `EnemiesInMelee()` | `EnemiesNearUnit(8, "player")` |
| `EnemiesNearTarget()` | `EnemiesNearUnit(8, "target")` |
| `AlliesNearTarget()` | `FriendsNearUnit(8, "target")` |
| `Range(string unit)` | `DistanceBetween(unit, "player")` |

---

### Group and Raid

| Method | Returns | Description | Mode |
|--------|---------|-------------|------|
| `GroupSize()` | int | Number of group/raid members (0 if solo) | Both |
| `InParty()` | bool | Player is in a party | Both |
| `InRaid()` | bool | Player is in a raid | Both |
| `Dampening()` | int | Arena dampening percentage | Classic only |

---

### Items and Equipment

| Method | Returns | Description | Mode |
|--------|---------|-------------|------|
| `CanUseEquippedItem(int Slot, bool CheckCasting)` | bool | Equipped item is usable (has on-use, off CD) | Retail only |
| `InventoryItemID(int Slot)` | int | Item ID at inventory slot | Retail only |
| `ItemCooldown(int ItemID)` | int (ms) | Item cooldown remaining | Retail only |
| `ItemCooldown(string ItemName)` | int (ms) | Item cooldown by name | Classic only |

Slot IDs: https://warcraft.wiki.gg/wiki/InventorySlotID (13=Trinket1, 14=Trinket2)

**Classic-only:**

| Method | Description |
|--------|-------------|
| `IsEquipped(string ItemName)` | Item is equipped |
| `CanUseItem(string ItemName, CheckEquipped)` | Item is usable |
| `CanUseTrinket(int slot)` | Trinket is usable (1 or 2) |
| `TrinketCooldown(int slot)` | Trinket cooldown |
| `TrinketEnabled(int slot)` | Trinket is enabled |

---

### Totems and Runes

| Method | Returns | Description | Mode |
|--------|---------|-------------|------|
| `TotemRemaining(string/int)` | float (ms) | Time remaining on totem | Both (int = retail only) |
| `GetAvailableRunes()` | int | Number of ready runes (Death Knight) | Retail only |
| `RuneCooldown(int RuneIndex)` | int (ms) | Individual rune cooldown | Classic only |
| `TimeUntilRunes(int X)` | int (ms) | Time until X runes available | Classic only |
| `TotemTimer()` | int | Generic totem timer | Classic only |

---

### Talents

| Method | Returns | Description | Mode |
|--------|---------|-------------|------|
| `Talent(int TalentID)` | bool | Talent is learned | Classic only |
| `Talent(int row, int col)` | bool | Talent at row/col is learned | Classic only |
| `PvpTalentIDs()` | List\<int\> | Active PvP talent IDs | Classic only |
| `GetActiveConduits()` | List\<int\> | Active conduit IDs | Classic only |
| `GetActiveConduitRanks()` | Dict\<int,int\> | Conduit ID to rank mapping | Classic only |
| `CovenantID()` | int | Current covenant ID | Classic only |
| `SoulbindID()` | int | Current soulbind ID | Classic only |

In retail, use `IsSpellKnown(int SpellId)` to check if a talent is learned.

---

### Custom Commands and Functions

#### Custom Commands
Toggle in-game with `/addon_prefix COMMAND`. Register in `Initialize()`:
```csharp
CustomCommands.Add("AOE");
CustomCommands.Add("BURST");
```

| Method | Returns | Description | Mode |
|--------|---------|-------------|------|
| `IsCustomCodeOn(string Code)` | bool | Custom command is toggled on | Both |

#### Custom Functions
Define custom Lua functions that return an integer (up to 7 digits). Register in `Initialize()`:
```csharp
CustomFunctions.Add("MyLevel", "return UnitLevel('player')");
CustomFunctions.Add("Combo Points", "return GetComboPoints('player', 'target')");
```

| Method | Returns | Description | Mode |
|--------|---------|-------------|------|
| `CustomFunction(string Name)` | int | Value returned by the Lua function (max 9999999) | Both |

---

### Miscellaneous

| Method | Returns | Description | Mode |
|--------|---------|-------------|------|
| `CombatTime()` | int (ms) | Time since combat started | Both |
| `GetMapID()` | int | Current map/zone ID | Both |
| `IsOn()` | bool | Rotation is toggled on in-game | Both |
| `IsChatClosed()` | bool | Chat box is closed | Both |
| `LineOfSighted()` | bool | Last cast was blocked by LoS | Both |
| `NotFacing()` | bool | Last cast failed due to facing | Both |
| `GetSpellQueue()` | string | Spell name queued via `/addon queue SpellName` | Both |
| `GetInfernoID()` | string | Unique ID for rotation licensing | Both |
| `GetAddonName()` | string | Current addon name | Both |
| `PrintMessage(text, color, clear)` | void | Print to bot console | Both |
| `DebugMode(bool)` | void | Enable/disable cast logging | Both |

**Classic-only position/cursor:**

| Method | Description |
|--------|-------------|
| `GetPlayerPositionX()` | Player X coordinate |
| `GetPlayerPositionY()` | Player Y coordinate |
| `GetPlayerFacing()` | Player facing angle |
| `GetCursorX()` | Cursor X position |
| `GetCursorY()` | Cursor Y position |
| `ArenaKickTimer(int ArenaNumber)` | Arena interrupt timer |
| `EnemyDR(string unit, string category)` | Diminishing returns tracker |

---

### Data Export/Import

Used for inter-plugin communication. See ArenaLib plugin for examples.

```csharp
// In a plugin's tick:
ExportObject("healTargets", myHealList);

// In a rotation or lower-priority plugin:
var healList = (List<string>)Inferno.ImportObject("ArenaLib", "healTargets");
```

The exporting plugin must be loaded at higher priority (listed higher in plugin manager).

---

## Quick Start Example

```csharp
using InfernoWow.API;
using InfernoWow.Modules;
using System.Collections.Generic;
using System.Drawing;

public class MyRotation : Rotation
{
    public override void LoadSettings()
    {
        Settings.Add(new Setting("Use Cooldowns", true));
        Settings.Add(new Setting("Health %", 1, 100, 40));
    }

    public override void Initialize()
    {
        Spellbook.Add("Fireball");
        Spellbook.Add("Fire Blast");
        Spellbook.Add("Combustion");
        Spellbook.Add("Ice Block");

        Macros.Add("cursor_flamestrike", "/cast [@cursor] Flamestrike");

        CustomCommands.Add("AOE");

        Inferno.PrintMessage("Rotation loaded!", Color.Green);
    }

    public override bool CombatTick()
    {
        // Defensive
        if (Inferno.Health("player") <= GetSlider("Health %") 
            && Inferno.CanCast("Ice Block"))
        {
            Inferno.Cast("Ice Block");
            return true;
        }

        // Cooldowns
        if (GetCheckBox("Use Cooldowns") && Inferno.CanCast("Combustion"))
        {
            Inferno.Cast("Combustion");
            return true;
        }

        // AOE (custom command toggled in-game)
        if (Inferno.IsCustomCodeOn("AOE") && Inferno.EnemiesNearUnit(8, "target") >= 3)
        {
            Inferno.Cast("cursor_flamestrike");
            return true;
        }

        // Single target
        if (Inferno.CanCast("Fire Blast", "target", true, false, true))
        {
            Inferno.Cast("Fire Blast", true); // QuickDelay for off-GCD
            return true;
        }

        if (Inferno.CanCast("Fireball"))
        {
            Inferno.Cast("Fireball");
            return true;
        }

        return false;
    }
}
```