public class FlashOfLightAction: FriendlyTargetedAction
{
    public const string MacroName = "cast_flash_of_light";
    public const string Macro = "/cast [@focus] Flash of Light";

    public const string Name = "Flash of Light";

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