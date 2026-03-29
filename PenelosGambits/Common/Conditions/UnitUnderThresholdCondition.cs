public class UnitUnderThresholdCondition : Condition
{
    private readonly string _unitId;
    private readonly int _threshold;

    public UnitUnderThresholdCondition(string unitId, int threshold)
    {
        _unitId = unitId;
        _threshold = threshold;
    }

    public bool IsMet(Environment environment)
    {
        return Inferno.Health(_unitId) < _threshold;
    }

    public void Consume()
    {
    }
}