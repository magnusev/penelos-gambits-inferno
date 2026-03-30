public class ShieldOfTheRighteousAction : PersonalAction
{
    public const string Name = "Shield of the Righteous";

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
        return "True";
    }
}