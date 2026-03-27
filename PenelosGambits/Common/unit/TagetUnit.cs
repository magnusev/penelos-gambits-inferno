public class TagetUnit : Unit
{
    public TagetUnit(string Id, string UnitType, string Role, int CastingSpell, int HealthPercentage)
        : base(Id, UnitType, Role, CastingSpell, HealthPercentage)
    {
    }

    public override void Focus()
    {
        throw new NotImplementedException();
    }
}