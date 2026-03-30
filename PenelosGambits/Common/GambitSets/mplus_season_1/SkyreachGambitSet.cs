public class SkyreachGambitSet : GambitSet
{
    private readonly GambitSet _defaultSet;
    private readonly Action _magicDispel;

    private List<Gambit> gambitSet;

    public SkyreachGambitSet(GambitSet defaultSet, Action magicDispel)
    {
        _defaultSet = defaultSet;
        _magicDispel = magicDispel;

        gambitSet = GenerateGambitSet();
    }

    public override string GetName()
    {
        return "Skyreach GambitSet";
    }

    private List<Gambit> GenerateGambitSet()
    {
        return new List<Gambit>
        {
        };
    }


    public override List<Gambit> GetGambits()
    {
        return gambitSet;
    }

    public override GambitSet GetNextGambitSet()
    {
        return _defaultSet;
    }
}