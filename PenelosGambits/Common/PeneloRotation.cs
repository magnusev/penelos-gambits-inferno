public class PeneloRotation
{
    private GambitSetPicker _gambitSetPicker;

    public PeneloRotation(GambitSetPicker gambitSetPicker)
    {
        _gambitSetPicker = gambitSetPicker;
    }


    public bool Tick(Environment environment)
    {
        if (Inferno.IsDead("player")) return false;

        if (ActionQueuer.CastQueuedActionIfExists()) return true;

        var gambitSet = _gambitSetPicker.GetGambitSet(Inferno.GetMapID());

        var nextGambit = gambitSet.HandleGambitChain(environment);

        if (nextGambit != null)
        {
            return nextGambit.DoAction(environment);
        }

        return true;
    }
}