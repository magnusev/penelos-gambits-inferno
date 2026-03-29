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
        Logger.Log("Charges for " + _spellId + ": " + Inferno.SpellCharges(_spellId));
        Logger.Log("Max Charges for " + _spellId + ": " + Inferno.MaxCharges(_spellId));
        Logger.Log("ChargesFractional for " + _spellId + ": " +  Inferno.ChargesFractional(_spellId, 5200));
        return Inferno.SpellCharges(_spellId) > _charges;
    }
}