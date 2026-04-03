// ========================================
// REUSABLE CONDITIONS
// ========================================
// Simple boolean checks used across all heal/damage/dungeon gambits

// Returns true if player is currently in combat
private bool IsInCombat()
{
    return Inferno.InCombat("player");
}

// Returns true if spell is off cooldown (within 200ms threshold)
private bool IsSpellReady(string spellName)
{
    return Inferno.SpellCooldown(spellName) <= 200;
}

// Returns true if the specified setting checkbox is enabled
private bool IsSettingOn(string settingName)
{
    return GetCheckBox(settingName);
}

// Returns true if player has a Healthstone in inventory
private bool HasHealthstone()
{
    return Inferno.CustomFunction("HasHealthstone") == 1;
}

// Returns true if current target is an attackable enemy
private bool TargetIsEnemy()
{
    return Inferno.UnitCanAttack("player", "target");
}

// Returns true if unit's health is below the specified percentage
private bool UnitUnder(string unit, int percent)
{
    return HealthPct(unit) < percent;
}

// Returns true if at least n enemies are within 8 yards (melee range)
private bool EnemiesInMelee(int count)
{
    return Inferno.EnemiesNearUnit(8, "player") >= count;
}

// Returns true if player has at least n power of type t (mana, energy, rage, etc.)
private bool PowerAtLeast(int amount, int powerType)
{
    return Inferno.Power("player", powerType) >= amount;
}

// Returns true if player has less than n power of type t (mana, energy, rage, etc.)
private bool PowerLessThan(int amount, int powerType)
{
    return Inferno.Power("player", powerType) < amount;
}

// Returns true if at least min group members are alive and below percent health
private bool GroupMembersUnder(int percent, int minCount)
{
    return GetGroupMembers().Count(unit => !Inferno.IsDead(unit) && HealthPct(unit) < percent) >= minCount;
}

// Returns true if any alive ally has the specified debuff
private bool AnyAllyHasDebuff(string debuff)
{
    return GetGroupMembers().Any(unit => !Inferno.IsDead(unit) && Inferno.HasDebuff(debuff, unit, false));
}

// Returns true if any alive ally has the specified debuff with at least the given stack count
private bool AnyAllyHasDebuff(string debuff, int stacks)
{
    return GetGroupMembers().Any(unit => !Inferno.IsDead(unit) && Inferno.HasDebuff(debuff, unit, false) && Inferno.DebuffStacks(debuff, unit, false) >= stacks);
}

// Returns true if the spell can be cast while moving (either player is stationary or has instant-cast buff)
private bool CanCastWhileMoving(string spell)
{
    // Not moving = can cast anything
    if (!Inferno.IsMoving("player"))
        return true;

    // Flash of Light becomes instant with Infusion of Light
    if (spell == "Flash of Light" && Inferno.HasBuff("Infusion of Light", "player", true))
        return true;

    // Holy Light becomes instant with Hand of Divinity
    if (spell == "Holy Light" && Inferno.HasBuff("Hand of Divinity", "player", true))
        return true;

    return false;
}

