public class IsSpellOffCooldownCondition : Condition
{
    private readonly string _spellName;

    public IsSpellOffCooldownCondition(string spellName)
    {
        _spellName = spellName;
    }

    public bool IsMet(Environment environment)
    {
        Logger.Log("Spell cooldown for " + _spellName + ": " + Inferno.SpellCooldown(_spellName));
        return Inferno.SpellCooldown(_spellName) <= 200;
    }
}