public abstract class Rotation
{
    public abstract void LoadSettings();
    public abstract void Initialize();
    
    public abstract bool CombatTick();
    public abstract bool OutOfCombatTick();
}