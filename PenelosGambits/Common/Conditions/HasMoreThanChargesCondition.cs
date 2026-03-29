public class HasMoreThanChargesCondition : Condition
{
    private readonly string _spellId;
    private readonly int _charges;

    public HasMoreThanChargesCondition(string spellId, int charges)
    {
        _spellId = spellId;
        _charges = charges;
    }

    public bool IsMet(Environment environment)
    {
        return Inferno.SpellCharges(_spellId) > _charges;
    }
}