public class AvengingWrathAction : PersonalAction
{
    public const string Name = "Avenging Wrath";

    public override string GetName()
    {
        return Name;
    }

    public override bool Cast()
    {
        Inferno.PrintMessage("Casting " + Name);
        Inferno.Cast(Name);
        return true;
    }

    public override bool CanCast()
    {
        return true;
    }

    public override string LogString(Unit unit)
    {
        if (Inferno.SpellCooldown(Name) < 2000) return "On Cooldown";

        return "True";
    }
}