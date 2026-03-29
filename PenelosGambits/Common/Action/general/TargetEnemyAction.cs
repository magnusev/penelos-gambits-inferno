public class TargetEnemyAction : PersonalAction
{
    public const string Macro = TargetingMacros.TARGET_ENEMY;

    public const string Name = "TargetEnemy";

    public override string GetName()
    {
        return Name;
    }

    public override bool Cast()
    {
        Inferno.PrintMessage("Casting " + Name);
        Inferno.Cast(Macro, true);
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