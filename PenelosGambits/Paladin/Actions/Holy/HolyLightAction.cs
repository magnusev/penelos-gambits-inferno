public class HolyLightAction: FriendlyTargetedAction
{
    public const string MacroName = "cast_holy_light";
    public const string Macro = "/cast [@focus] Holy Light";

    public const string Name = "Holy Light";

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