public class CanCastCondition : Condition
{
    private readonly string _spellName;

    public CanCastCondition(string spellName)
    {
        _spellName = spellName;
    }

    public bool IsMet(Environment environment)
    {
        return Inferno.CanCast(_spellName);
    }
}