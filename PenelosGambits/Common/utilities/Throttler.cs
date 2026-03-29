using System.Diagnostics;

public class Throttler
{
    private readonly string name;
    private readonly int throttleTimeMs;
    private Stopwatch stopwatch;
    private bool hasStarted;

    public Throttler(int throttleTimeMs = 100, string name = "Unnamed")
    {
        this.name = name;
        this.throttleTimeMs = throttleTimeMs;
        stopwatch = new Stopwatch();
        hasStarted = false;
    }

    public bool IsLocked()
    {
        if (!hasStarted || stopwatch.ElapsedMilliseconds >= throttleTimeMs)
        {
            return false;
        }

        return true;
    }

    public bool IsOpen()
    {
        return !IsLocked();
    }

    public void Restart()
    {
        hasStarted = true;
        stopwatch.Restart();
    }
}