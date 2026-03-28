public static class ActionQueuer
{
    private static string _action;

    public static void QueueAction(string action)
    {
        if (_action != null) return;
        _action = action;
    }

    public static bool CastQueuedActionIfExists()
    {
        if (_action == null) return false;

        var CurrentAction = _action;

        _action = null;
        Inferno.Cast(CurrentAction, true);
        return true;
    }
}