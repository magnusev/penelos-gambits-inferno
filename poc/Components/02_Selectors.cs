// ========================================
// UNIT SELECTORS
// ========================================
// Find the right target for heals, damage, dispels
// Uses Inferno.CanCast() to ensure the spell can actually fire (GCD, range, resources)

// -- Group Management --
private List<string> GetGroupMembers()
{
    List<string> result = new List<string>();
    if (Inferno.InRaid()) 
    { 
        int size = Inferno.GroupSize(); 
        for (int i = 1; i <= size; i++) 
        { 
            string token = "raid" + i; 
            if (Inferno.UnitName(token) != "") result.Add(token); 
        } 
    }
    else if (Inferno.InParty()) 
    { 
        result.Add("player"); 
        int size = Inferno.GroupSize(); 
        for (int i = 1; i < size; i++) 
        { 
            string token = "party" + i; 
            if (Inferno.UnitName(token) != "") result.Add(token); 
        } 
    }
    else { result.Add("player"); }
    return result;
}

// -- Heal Selectors --
// Use Inferno.CanCast to check GCD, resources, range, and spell known.
// This prevents queuing spells that can't actually fire.
private string LowestAllyUnder(int percent, string spell)
{
    return GetGroupMembers()
        .Where(unit => !Inferno.IsDead(unit) && HealthPct(unit) < percent && Inferno.CanCast(spell, unit))
        .OrderBy(unit => HealthPct(unit))
        .FirstOrDefault();
}

private string LowestAllyInRange(string spell)
{
    return GetGroupMembers()
        .Where(unit => !Inferno.IsDead(unit) && Inferno.CanCast(spell, unit))
        .OrderBy(unit => HealthPct(unit))
        .FirstOrDefault();
}

// -- Debuff Selectors --
private string GetAllyWithDebuff(string debuff, string spell)
{
    return GetGroupMembers()
        .Where(unit => !Inferno.IsDead(unit) && Inferno.HasDebuff(debuff, unit, false) && Inferno.SpellInRange(spell, unit))
        .FirstOrDefault();
}

private string GetAllyWithMostStacks(string debuff, string spell)
{
    return GetGroupMembers()
        .Where(unit => !Inferno.IsDead(unit) && Inferno.HasDebuff(debuff, unit, false) && Inferno.SpellInRange(spell, unit))
        .OrderByDescending(unit => Inferno.DebuffStacks(debuff, unit, false))
        .FirstOrDefault();
}

