public class JudgmentAction: EnemyTargetedAction
{
    public const string Name = "Judgment";

    public override string GetName()
    {
        return Name;
    }

    public override bool Cast()
    {
        ActionQueuer.QueueAction(Name);
        return true;
    }

    public override bool CanCast()
    {
        return Inferno.CanCast(Name, "target");
    }

    public override string LogString(Unit unit)
    {
        if (Inferno.SpellCooldown(Name) < 2000) return "On Cooldown";
        if (!Inferno.SpellInRange(Name, "target")) return "Out of Range";
        if (!Inferno.CanCast(Name, "target")) return "Cannot Cast";

        return "True";
    }
}