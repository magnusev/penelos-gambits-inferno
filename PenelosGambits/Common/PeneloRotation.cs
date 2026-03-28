public class PeneloRotation
{
    public bool Tick()
    {
        if (Inferno.IsDead("player")) return false;

        if (ActionQueuer.CastQueuedActionIfExists()) return true;

        Inferno.Cast("Flash of Light");
        
        return true;
    }
}