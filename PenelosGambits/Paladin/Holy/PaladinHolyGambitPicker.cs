public class PaladinHolyGambitPicker : GambitSetPicker
{
    private HolyPaladinDefaultGambitSet defaultGambitSet = new HolyPaladinDefaultGambitSet();
    
    protected override GambitSet SwapGambitSet(int mapId)
    {
        switch (mapId)
        {
            case 480: // Proving Grounds
                return new ProvingGroundGambitSet(defaultGambitSet, new CleanseAction());
            default:
                return defaultGambitSet;
            
        }
    }
}