public class CanCastCondition : Condition
{
    private readonly string _spellName;

    public CanCastCondition(string spellName)
    {
        _spellName = spellName;
    }

    public bool IsMet(Environment environment)
    {
        Logger.Log("Can cast " + _spellName + ": " + Inferno.CanCast(_spellName, "player"));
        return Inferno.CanCast(_spellName, "player");
    }
}