public class PaladinHolyGambitPicker : GambitSetPicker
{
    private HolyPaladinDefaultGambitSet defaultGambitSet = new HolyPaladinDefaultGambitSet();
    
    protected override GambitSet SwapGambitSet(int mapId)
    {
        switch (mapId)
        {
            case 480: // Proving Grounds
                return new ProvingGroundGambitSet(defaultGambitSet, new CleanseAction());
            case 2511: // Magister's Terrace
            case 2515: // Magister's Terrace
            case 2516: // Magister's Terrace
            case 2517: // Magister's Terrace
            case 2518: // Magister's Terrace
            case 2519: // Magister's Terrace
            case 2520: // Magister's Terrace
                return new MagistersTerraceGambitSet(defaultGambitSet, new CleanseAction());
            default:
                return defaultGambitSet;
            
        }
    }
}