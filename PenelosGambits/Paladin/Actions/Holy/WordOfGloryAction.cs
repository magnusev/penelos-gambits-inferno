public class WordOfGloryAction : FriendlyTargetedAction
{
    public const string MacroName = "cast_word_of_glory";
    public const string Macro = "/cast [@focus] Word of Glory";

    public const string Name = "Word of Glory";

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