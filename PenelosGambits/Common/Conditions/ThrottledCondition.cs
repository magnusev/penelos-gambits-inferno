public class ThrottledCondition : Condition
{
    private Throttler _throttler;

    public ThrottledCondition(int throttletimeInMs)
    {
        _throttler = new Throttler(throttletimeInMs, "ThrottledCondition(" + throttletimeInMs + "ms)");
    }
    
    public bool IsMet(Environment environment)
    {
        return _throttler.IsOpen();
    }

    public void Consume()
    {
        _throttler.Restart();
    }
}