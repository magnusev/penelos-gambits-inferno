public class LowestUnderHPThresholdCondition : Condition
{
    private readonly string _spellId;
    private readonly int _threshold;

    public LowestUnderHPThresholdCondition(int threshold, string spellId)
    {
        _threshold = threshold;
        _spellId = spellId;
    }

    public bool IsMet(Environment environment)
    {
        var members = new List<Unit>(environment.Group.GetMembers()); 
        members.Add(environment.Player);
        
        return members
            .Where(member => member.CanCast(_spellId))
            .Where(member => !member.IsDead())
            .Where(member => member.HealthPercentage < _threshold)
            .ToList()
            .Any();
    }
}