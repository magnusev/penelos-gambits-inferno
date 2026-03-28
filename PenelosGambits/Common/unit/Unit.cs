﻿public abstract class Unit
{
    public string Id { get; private set; }
    public string UnitType { get; private set; }
    public string Role { get; private set; }
    public int CastingSpell { get; private set; }
    public int Health { get; private set; }
    public int MaxHealth { get; private set; }
    public int HealthPercentage { get; private set; }

    public abstract void Focus();

    protected Unit(string Id, string UnitType, string Role, int CastingSpell, int Health, int MaxHealth)
    {
        this.Id = Id;
        this.UnitType = UnitType;
        this.Role = Role;
        this.CastingSpell = CastingSpell;
        this.Health = Health;
        this.MaxHealth = MaxHealth;
        this.HealthPercentage = MaxHealth > 0 ? (Health * 100) / MaxHealth : 0;
    }

    public bool CanCast(string spellName)
    {
        return !IsDead()
               && Inferno.SpellInRange(spellName, Id)
               && Inferno.CanCast(spellName, Id);
    }

    public bool IsDead()
    {
        return Health == 0;
    }

    public string CanCastReason(string spellName)
    {
        if (IsDead()) return "Dead";
        if (!Inferno.SpellInRange(spellName, Id)) return "Out of Range";
        if (!Inferno.CanCast(spellName, Id)) return "Cannot Cast";

        return "True";
    }
}