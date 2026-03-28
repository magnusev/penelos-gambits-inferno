public abstract class FriendlyTargetedAction : Action
{
    public abstract string GetName();
    
    public bool IsTargetted()
    {
        return true;
    }

    public bool Cast()
    {
        throw new NotImplementedException();
    }

    public abstract bool Cast(Unit unit);

    public bool CanCast()
    {
        throw new NotImplementedException();
    }

    public abstract bool CanCast(Unit unit);
    
    public abstract string LogString(Unit unit);
}