public class ThrottledCondition : Condition
{
    private Throttler _throttler;

    public ThrottledCondition(int throttletimeInMs)
    {
        _throttler = new Throttler(throttletimeInMs);
    }
    
    public bool IsMet(Environment environment)
    {
        return _throttler.IsOpen();
    }
}