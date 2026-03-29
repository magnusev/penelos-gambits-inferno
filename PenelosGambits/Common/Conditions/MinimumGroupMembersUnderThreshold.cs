public class MinimumGroupMembersUnderThreshold : Condition
{
    private readonly int _threshold;
    private readonly int _numberOfMembers;

    public MinimumGroupMembersUnderThreshold(int threshold, int numberOfMembers = 1)
    {
        _threshold = threshold;
        _numberOfMembers = numberOfMembers;
    }

    public bool IsMet(Environment environment)
    {
        int i = 0;

        if (environment.Player.HealthPercentage < _threshold) i++;

        environment.Group.GetMembers()
            .ToList()
            .ForEach(member =>
        {
            if (member.HealthPercentage < _threshold) i++;
        });

        return i >= _numberOfMembers;
    }

    public void Consume()
    { }
}