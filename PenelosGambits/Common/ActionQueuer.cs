using System.Collections.Generic;

public static class ActionQueuer
{
    private static readonly Queue<string> _queue = new Queue<string>();
    private static readonly object _lock = new object();

    public static void QueueAction(string actionName)
    {
        lock (_lock)
        {
            _queue.Enqueue(actionName);
        }
    }

    public static bool CastQueuedActionIfExists()
    {
        string action = null;
        lock (_lock)
        {
            if (_queue.Count > 0)
            {
                action = _queue.Dequeue();
            }
        }

        if (action == null) return false;

        Inferno.Cast(action);
        return true;
    }

    public static void Clear()
    {
        lock (_lock)
        {
            _queue.Clear();
        }
    }
}
