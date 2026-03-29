public class PaladinHolyGambitPicker : GambitSetPicker
{
    private HolyPaladinDefaultGambitSet defaultGambitSet = new HolyPaladinDefaultGambitSet();
    
    protected override GambitSet SwapGambitSet(int mapId)
    {
        return defaultGambitSet;
    }
}