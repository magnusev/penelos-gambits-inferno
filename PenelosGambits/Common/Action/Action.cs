public interface Action
{
    string GetName();
    
    bool IsTargetted();

    bool Cast();

    bool Cast(Unit unit);

    bool CanCast();

    bool CanCast(Unit unit);

    string LogString(Unit unit);
}