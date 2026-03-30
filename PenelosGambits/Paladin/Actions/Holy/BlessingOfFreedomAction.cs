public class BlessingOfFreedomAction: FriendlyTargetedAction
{
    public const string MacroName = "cast_bof";
    public const string Macro = "/stopcasting\\n/cast [@focus] Blessing of Freedom";

    public const string Name = "Blessing of Freedom";
    
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
        return unit.CanCast(Name);
    }
    
    public override string LogString(Unit unit)
    {
        return unit.CanCastReason(Name);
    }

}