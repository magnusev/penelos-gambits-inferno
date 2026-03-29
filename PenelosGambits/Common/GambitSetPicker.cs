public abstract class GambitSetPicker
{
    private int CurrentMapId = -1;
    private GambitSet CurrentGambitSet = null;

    public GambitSet GetGambitSet(int mapId)
    {
        if (CurrentMapId == mapId && CurrentGambitSet != null)
        {
            return CurrentGambitSet;
        }

        Logger.Log("Swapping from MapId " + CurrentMapId + " to " + mapId);
        CurrentMapId = mapId;

        var newGambitSet = SwapGambitSet(mapId);

        CurrentGambitSet = newGambitSet;
        return newGambitSet;
    }

    protected abstract GambitSet SwapGambitSet(int mapId);
}