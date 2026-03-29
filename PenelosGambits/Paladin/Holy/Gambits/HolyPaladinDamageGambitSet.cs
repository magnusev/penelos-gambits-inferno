public class HolyPaladinDamageGambitSet : GambitSet
{
    public override string GetName()
    {
        return "Default Holy Paladin Damage";
    }

    private List<Gambit> gambitSet = new List<Gambit>
    { };


    public override List<Gambit> GetGambits()
    {
        return gambitSet;
    }
}