public abstract class PersonalAction : Action
{
    public abstract string GetName();

    public bool IsTargetted()
    {
        return false;
    }

    public abstract bool Cast();

    public bool Cast(Unit unit)
    {
        throw new NotImplementedException();
    }

    public abstract bool CanCast();

    public bool CanCast(Unit unit)
    {
        throw new NotImplementedException();
    }
    
    public abstract string LogString(Unit unit);
}