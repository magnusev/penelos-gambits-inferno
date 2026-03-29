public class PlayerSecondaryPowerAtLeast : Condition
{
    private readonly int _minimum;
    private readonly int _type;

    public PlayerSecondaryPowerAtLeast(int minimum, int type)
    {
        _minimum = minimum;
        _type = type;
    }

    public bool IsMet(Environment environment)
    {
        return Inferno.Power("player", _type) >= _minimum;
    }

    public void Consume() { }
}