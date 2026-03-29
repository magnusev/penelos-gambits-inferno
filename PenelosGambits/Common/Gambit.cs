public class Gambit
{
    public int Priority { get; private set; }
    public string Name { get; private set; }
    public Action Action { get; private set; }

    private readonly ISelector _selector;
    private readonly List<Condition> _conditions;

    public Gambit(
        int priority,
        String name,
        List<Condition> conditions,
        ISelector selector,
        Action action
    )
    {
        Priority = priority;
        Name = name;
        _conditions = conditions;
        _selector = selector;
        Action = action;
    }

    public bool IsMet(Environment environment)
    {
        return _conditions.All(condition => condition.IsMet(environment));
    }

    public Unit GetTarget(Environment environment)
    {
        return _selector.Select(environment);
    }

    public bool CanDoAction(Environment environment)
    {
        if (Action.IsTargetted())
        {
            var unit = _selector.Select(environment);
            if (unit == null) return false;

            return Action.CanCast(unit);
        }

        return Action.CanCast();
    }

    public bool DoAction(Environment environment)
    {
        bool result;
        if (Action.IsTargetted())
        {
            result = Action.Cast(_selector.Select(environment));
        }
        else
        {
            result = Action.Cast();
        }

        if (result)
        {
            _conditions.ForEach(condition => condition.Consume());
        }

        return result;
    }

    public string ToString(Environment environment)
    {
        if (_selector != null)
        {
            Unit target = _selector.Select(environment);
            if (target == null) return "";
            return "(" + Priority + ") Casting " + Name + " on " + target.Id + " (" + Action.LogString(target) + ")";
        }

        return "(" + Priority + ") Casting " + Action.GetName() + " (" + Action.LogString(null) + ")";
    }

    public void LogSelector(Environment environment)
    {
        if (_selector == null) return;
        _selector.Log(environment);
    }
}