public class ActionIsNotNullCondition : Condition
{
    private readonly Action _action;

    public ActionIsNotNullCondition(Action action)
    {
        _action = action;
    }
    
    public bool IsMet(Environment environment)
    {
        return _action != null;
    }

    public void Consume()
    { }
}