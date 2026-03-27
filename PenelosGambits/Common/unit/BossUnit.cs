public class BossUnit : Unit
{
    public BossUnit(string Id, string UnitType, string Role, int CastingSpell, int HealthPercentage)
        : base(Id, UnitType, Role, CastingSpell, HealthPercentage)
    {
    }

    public override void Focus()
    {
        throw new NotImplementedException();
    }
}