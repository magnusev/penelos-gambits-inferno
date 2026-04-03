// ========================================
// UNIT SELECTORS
// ========================================
// Find the right target for heals, damage, dispels
// Uses Inferno.CanCast() to ensure the spell can actually fire (GCD, range, resources)

// -- Group Management --
private List<string> GetGroupMembers()
{
    List<string> r = new List<string>();
    if (Inferno.InRaid()) 
    { 
        int sz = Inferno.GroupSize(); 
        for (int i = 1; i <= sz; i++) 
        { 
            string tk = "raid" + i; 
            if (Inferno.UnitName(tk) != "") r.Add(tk); 
        } 
    }
    else if (Inferno.InParty()) 
    { 
        r.Add("player"); 
        int sz = Inferno.GroupSize(); 
        for (int i = 1; i < sz; i++) 
        { 
            string tk = "party" + i; 
            if (Inferno.UnitName(tk) != "") r.Add(tk); 
        } 
    }
    else { r.Add("player"); }
    return r;
}

// -- Heal Selectors --
// Use Inferno.CanCast to check GCD, resources, range, and spell known.
// This prevents queuing spells that can't actually fire.
private string LowestAllyUnder(int pct, string spell)
{
    return GetGroupMembers()
        .Where(u => !Inferno.IsDead(u) && HealthPct(u) < pct && Inferno.CanCast(spell, u))
        .OrderBy(u => HealthPct(u))
        .FirstOrDefault();
}

private string LowestAllyInRange(string spell)
{
    return GetGroupMembers()
        .Where(u => !Inferno.IsDead(u) && Inferno.CanCast(spell, u))
        .OrderBy(u => HealthPct(u))
        .FirstOrDefault();
}

// -- Debuff Selectors --
private string GetAllyWithDebuff(string d, string spell)
{
    return GetGroupMembers()
        .Where(u => !Inferno.IsDead(u) && Inferno.HasDebuff(d, u, false) && Inferno.SpellInRange(spell, u))
        .FirstOrDefault();
}

private string GetAllyWithMostStacks(string d, string spell)
{
    return GetGroupMembers()
        .Where(u => !Inferno.IsDead(u) && Inferno.HasDebuff(d, u, false) && Inferno.SpellInRange(spell, u))
        .OrderByDescending(u => Inferno.DebuffStacks(d, u, false))
        .FirstOrDefault();
}

