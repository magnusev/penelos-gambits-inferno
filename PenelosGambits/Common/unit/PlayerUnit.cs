public class PlayerUnit : Unit
{
    public PlayerUnit(string Id, string UnitType, string Role, int CastingSpell, int Health, int MaxHealth)
        : base(Id, UnitType, Role, CastingSpell, Health, MaxHealth)
    {
    }

    public override void Focus()
    {
        Inferno.Cast("focus_" + Id);
    }
}