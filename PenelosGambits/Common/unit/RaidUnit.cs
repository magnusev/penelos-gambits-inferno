public class RaidUnit : Unit
{
    public RaidUnit(string Id, string UnitType, string Role, int CastingSpell, int HealthPercentage)
        : base(Id, UnitType, Role, CastingSpell, HealthPercentage)
    {
    }

    public override void Focus()
    {
        Inferno.Cast("focus_" + Id);
    }
}