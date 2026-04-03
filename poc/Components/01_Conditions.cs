// ========================================
// REUSABLE CONDITIONS
// ========================================
// Simple boolean checks used across all heal/damage/dungeon gambits

private bool IsInCombat() { return Inferno.InCombat("player"); }
private bool IsSpellReady(string s) { return Inferno.SpellCooldown(s) <= 200; }
private bool IsSettingOn(string s) { return GetCheckBox(s); }
private bool HasHealthstone() { return Inferno.CustomFunction("HasHealthstone") == 1; }
private bool TargetIsEnemy() { return Inferno.UnitCanAttack("player", "target"); }
private bool UnitUnder(string u, int p) { return HealthPct(u) < p; }
private bool EnemiesInMelee(int n) { return Inferno.EnemiesNearUnit(8, "player") >= n; }
private bool PowerAtLeast(int n, int t) { return Inferno.Power("player", t) >= n; }
private bool PowerLessThan(int n, int t) { return Inferno.Power("player", t) < n; }

private bool GroupMembersUnder(int pct, int min)
{
    return GetGroupMembers().Count(u => !Inferno.IsDead(u) && HealthPct(u) < pct) >= min;
}

private bool AnyAllyHasDebuff(string d)
{
    return GetGroupMembers().Any(u => !Inferno.IsDead(u) && Inferno.HasDebuff(d, u, false));
}

private bool AnyAllyHasDebuff(string d, int stacks)
{
    return GetGroupMembers().Any(u => !Inferno.IsDead(u) && Inferno.HasDebuff(d, u, false) && Inferno.DebuffStacks(d, u, false) >= stacks);
}

// Movement checks for cast-time spells
private bool CanCastWhileMoving(string spell)
{
    if (!Inferno.IsMoving("player")) return true;
    if (spell == "Flash of Light" && Inferno.HasBuff("Infusion of Light", "player", true)) return true;
    if (spell == "Holy Light" && Inferno.HasBuff("Hand of Divinity", "player", true)) return true;
    return false;
}

