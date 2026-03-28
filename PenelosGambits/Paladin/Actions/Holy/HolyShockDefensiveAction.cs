public class HolyShockDefensiveAction : FriendlyTargetedAction
{
    public const string MacroName = "cast_holy_shock";
    public const string Macro = "/cast [@focus] Holy Shock";

    public const string Name = "Holy Shock";

    public override string GetName()
    {
        return Name;
    }

    public override bool Cast(Unit unit)
    {
        unit.Focus();
        ActionQueuer.QueueAction(MacroName);
        return true;
    }

    public override bool CanCast(Unit unit)
    {
        if (unit == null) return false;
        return unit.CanCast(Name);
    }

    public override string LogString(Unit unit)
    {
        return unit.CanCastReason(Name);
    }
}