public abstract class GambitSet
{
    private Throttler logThrottler = new Throttler(1000, "GambitSet-LogThrottler");

    public abstract string GetName();
    
    public abstract List<Gambit> GetGambits();


    public virtual GambitSet DoBeforeGambitSet()
    {
        return null;
    }

    public virtual GambitSet GetNextGambitSet()
    {
        return null;
    }

    private Gambit GetNextGambit(Environment environment)
    {
        var sortedGambits = GetGambits()
            .Where(gambit => gambit.IsMet(environment))
            .OrderBy(gambit => gambit.Priority)
            .ToList();

        var nextGambit = sortedGambits
            .FirstOrDefault(gambit => gambit.CanDoAction(environment));

        if (sortedGambits.Count > 0 && nextGambit != null)
        {
            if (logThrottler.IsOpen())
            {
                logThrottler.Restart();
                Logger.Log("Gambit Checker for GambitSet " + GetName());
                Logger.Log("IsMoving: " + Inferno.IsMoving("player"));
                Logger.Log("Secondary Power: " + Inferno.Power("player", 9));
                Logger.Log("Conditions met:");
                sortedGambits.ForEach(g => Logger.Log("  " + g.ToString(environment)));
                Logger.Log("Selector Logic: ");
                nextGambit.LogSelector(environment);
                Logger.Log("Next Gambit: " + nextGambit.ToString(environment));
                Logger.Log("------------------");
                Logger.Log("Debug Info: ");
                environment.Bosses.ForEach(boss => boss.LogBossInfo());
                Logger.Log("------------------");
            }
        }

        return nextGambit;
    }

    public Gambit HandleGambitChain(Environment environment)
    {
        if (DoBeforeGambitSet() != null)
        {
            var Gambit = DoBeforeGambitSet().HandleGambitChain(environment);
            if (Gambit != null) return Gambit;
        }


        var CurrentChainGambit = GetNextGambit(environment);
        if (CurrentChainGambit != null) return CurrentChainGambit;

        if (GetNextGambitSet() != null)
        {
            var NextGambitChain = GetNextGambitSet().HandleGambitChain(environment);
            if (NextGambitChain != null) return NextGambitChain;
        }

        return null;
    }
}