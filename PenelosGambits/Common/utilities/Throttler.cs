using System.Diagnostics;

public class Throttler
{
    private readonly int throttleTimeMs;
    private Stopwatch stopwatch;
    private bool hasStarted;

    public Throttler(int throttleTimeMs = 100)
    {
        this.throttleTimeMs = throttleTimeMs;
        stopwatch = new Stopwatch();
        hasStarted = false;
    }

    public bool IsLocked()
    {
        if (!hasStarted || stopwatch.ElapsedMilliseconds >= throttleTimeMs)
        {
            hasStarted = true;
            stopwatch.Restart();
            return false;
        }

        Logger.Log("Throttler locked, time remaining: " + (throttleTimeMs - stopwatch.ElapsedMilliseconds) + "ms");
        return true;
    }

    public bool IsOpen()
    {
        return !IsLocked();
    }
}