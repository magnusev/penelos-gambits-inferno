using System.Diagnostics;

public class Throttler
{
    private readonly int throttleTimeMs;
    private Stopwatch stopwatch;

    public Throttler(int throttleTimeMs = 100)
    {
        this.throttleTimeMs = throttleTimeMs;
        stopwatch = Stopwatch.StartNew();
    }

    public bool IsLocked()
    {
        if (stopwatch.ElapsedMilliseconds >= throttleTimeMs)
        {
            stopwatch.Restart();
            return false;
        }

        return true;
    }

    public bool IsOpen()
    {
        return !IsLocked();
    }
}