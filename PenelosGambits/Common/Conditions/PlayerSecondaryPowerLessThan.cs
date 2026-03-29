public class PlayerSecondaryPowerLessThan : Condition
{
    private readonly int _value;
    private readonly int _type;

    public PlayerSecondaryPowerLessThan(int value, int type)
    {
        _value = value;
        _type = type;
    }

    public bool IsMet(Environment environment)
    {
        return Inferno.Power("player", _type) < _value;
    }
}