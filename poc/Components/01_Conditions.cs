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
private bool IsSpellReady(string s)
{
    return Inferno.SpellCooldown(s) <= 200;
}

// Returns true if the specified setting checkbox is enabled
private bool IsSettingOn(string s)
{
    return GetCheckBox(s);
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
private bool UnitUnder(string u, int p)
{
    return HealthPct(u) < p;
}

// Returns true if at least n enemies are within 8 yards (melee range)
private bool EnemiesInMelee(int n)
{
    return Inferno.EnemiesNearUnit(8, "player") >= n;
}

// Returns true if player has at least n power of type t (mana, energy, rage, etc.)
private bool PowerAtLeast(int n, int t)
{
    return Inferno.Power("player", t) >= n;
}

// Returns true if player has less than n power of type t (mana, energy, rage, etc.)
private bool PowerLessThan(int n, int t)
{
    return Inferno.Power("player", t) < n;
}

// Returns true if at least min group members are alive and below pct health
private bool GroupMembersUnder(int pct, int min)
{
    return GetGroupMembers().Count(u => !Inferno.IsDead(u) && HealthPct(u) < pct) >= min;
}

// Returns true if any alive ally has the specified debuff
private bool AnyAllyHasDebuff(string d)
{
    return GetGroupMembers().Any(u => !Inferno.IsDead(u) && Inferno.HasDebuff(d, u, false));
}

// Returns true if any alive ally has the specified debuff with at least the given stack count
private bool AnyAllyHasDebuff(string d, int stacks)
{
    return GetGroupMembers().Any(u => !Inferno.IsDead(u) && Inferno.HasDebuff(d, u, false) && Inferno.DebuffStacks(d, u, false) >= stacks);
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

