public class BossUnit : Unit
{
    public BossUnit(string Id, string UnitType, string Role, int CastingSpell, int Health, int MaxHealth)
        : base(Id, UnitType, Role, CastingSpell, Health, MaxHealth)
    {
    }

    public override void Focus()
    {
        throw new NotImplementedException();
    }
}