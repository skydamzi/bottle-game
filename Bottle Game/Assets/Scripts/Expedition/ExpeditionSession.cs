using System.Collections.Generic;

public enum ExpeditionState { Preparing, Combat, Reward, RouteSelection, Transition, BossCombat, Completed, Failed }

public sealed class ExpeditionRewardRecord
{
    public string RoundId { get; }
    public string ArtifactId { get; }
    public string DisplayName { get; }
    public int Currency { get; }
    public ExpeditionRewardRecord(string roundId, string artifactId, string displayName, int currency)
    {
        RoundId = roundId;
        ArtifactId = artifactId;
        DisplayName = displayName;
        Currency = currency;
    }
}

// Session records are exposed read-only; a future authority can supply decisions
// through the same manager commands without putting rules into the view.
public sealed class ExpeditionSession
{
    public int Seed { get; }
    public int Currency { get; private set; }
    public int ClearedRounds { get; private set; }
    private readonly List<ExpeditionRewardRecord> rewards = new List<ExpeditionRewardRecord>();
    private readonly List<string> routes = new List<string>();
    public IReadOnlyList<ExpeditionRewardRecord> Rewards => rewards.AsReadOnly();
    public IReadOnlyList<string> SelectedRoutes => routes.AsReadOnly();
    public ExpeditionSession(int seed) { Seed = seed; }
    internal void ClearRound(int currency) { ClearedRounds++; Currency += currency; }
    internal bool Spend(int amount)
    {
        if (amount < 0 || Currency < amount) return false;
        Currency -= amount;
        return true;
    }
    internal void AddCurrency(int amount) { Currency += amount; }
    internal void RecordReward(ExpeditionRewardRecord reward) { rewards.Add(reward); }
    internal void RecordRoute(string routeId) { routes.Add(routeId); }
}
