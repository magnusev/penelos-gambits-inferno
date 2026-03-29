public class IsSpellOffCooldownCondition : Condition
{
    private readonly string _spellName;

    public IsSpellOffCooldownCondition(string spellName)
    {
        _spellName = spellName;
    }

    public bool IsMet(Environment environment)
    {
        return Inferno.SpellCooldown(_spellName) <= 200;
    }
}