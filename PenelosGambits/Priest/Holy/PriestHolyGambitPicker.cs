public class PriestHolyGambitPicker : GambitSetPicker
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
            case 601: // Skyreach
            case 602: // Skyreach
                return new SkyreachGambitSet(defaultGambitSet, new CleanseAction());
            case 823: // Pit of Saron
                return new PitOfSaronGambitSet(defaultGambitSet,
                    new CleanseAction(),
                    new CleanseAction(),
                    new CleanseAction()
                );
            case 2492: // Windrunner Spire
            case 2493: // Windrunner Spire
            case 2494: // Windrunner Spire
            case 2496: // Windrunner Spire
            case 2497: // Windrunner Spire
            case 2498: // Windrunner Spire
            case 2499: // Windrunner Spire
                return new WindrunnerSpireGambitSet(
                    defaultGambitSet,
                    new CleanseAction(),
                    new CleanseAction()
                );
            case 2501: // Maisara Caverns
                return new MaisaraCavernsGambitSet(
                    defaultGambitSet,
                    new CleanseAction(),
                    new CleanseAction(),
                    new CleanseAction()
                );
            case 2097: // Algeth'ar Academy
            case 2098: // Algeth'ar Academy
            case 2099: // Algeth'ar Academy
                return new AlgethArAcademyGambitSet(
                    defaultGambitSet,
                    new CleanseAction(),
                    new CleanseAction(),
                    new CleanseAction()
                );

            default:
                return defaultGambitSet;
        }
    }
}