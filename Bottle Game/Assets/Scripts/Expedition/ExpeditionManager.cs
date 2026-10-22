using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExpeditionManager : MonoBehaviour
{
    public ExpeditionDefinition definition;
    public PlayerStats[] players;
    public EnemyWaveSpawner spawner;
    [Tooltip("Existing sandbox enemies are preserved but disabled during expeditions.")]
    public GameObject[] sceneEnemies;

    public ExpeditionState State { get; private set; } = ExpeditionState.Preparing;
    public int RoundIndex { get; private set; } = -1;
    public ExpeditionRoundDefinition CurrentRound => definition != null && RoundIndex >= 0 && RoundIndex < definition.rounds.Count ? definition.rounds[RoundIndex] : null;
    public ExpeditionSession Session { get; private set; }
    public ExpeditionRouteDefinition LastSelectedRoute { get; private set; }
    public ExpeditionRouteDefinition CurrentVisit { get; private set; }
    public bool PurchaseMade { get; private set; }
    public string VisitMessage { get; private set; } = "";
    public string ResultSummary { get; private set; } = "";
    public int AliveEnemies => spawner != null ? spawner.AliveEnemies : 0;
    public IReadOnlyList<ArtifactDefinition> RewardCandidates => candidates.AsReadOnly();
    public IReadOnlyList<ExpeditionRouteDefinition> AvailableRoutes => routes.AsReadOnly();
    public event Action Changed;
    public event Action<ExpeditionRoundDefinition> RoundCompleted;
    public event Action<ArtifactDefinition> RewardSelected;
    public event Action<bool> ExpeditionEnded;

    private readonly List<ArtifactDefinition> candidates = new List<ArtifactDefinition>();
    private readonly List<ExpeditionRouteDefinition> routes = new List<ExpeditionRouteDefinition>();
    private readonly Dictionary<PlayerStats, Vector3> starts = new Dictionary<PlayerStats, Vector3>();
    private readonly Dictionary<PlayerMovement, bool> originalInput = new Dictionary<PlayerMovement, bool>();
    private readonly List<PlayerStats> boundPlayers = new List<PlayerStats>();
    private EnemyWaveSpawner boundSpawner;
    private System.Random random;
    private ArtifactRewardGenerator rewards;
    private RunBuild rewardRecipient;
    private bool runActive;
    private bool rewardClaimed;

    void Start()
    {
        Bind();
        if (State == ExpeditionState.Preparing) SetInput(false);
        Changed?.Invoke();
    }

    private void Bind()
    {
        if (boundSpawner != spawner)
        {
            if (boundSpawner != null) { boundSpawner.AllRequiredEnemiesDefeated -= OnWaveCleared; boundSpawner.Changed -= OnWaveChanged; }
            boundSpawner = spawner;
            if (boundSpawner != null) { boundSpawner.AllRequiredEnemiesDefeated += OnWaveCleared; boundSpawner.Changed += OnWaveChanged; }
        }
        foreach (PlayerStats player in boundPlayers) if (player != null) player.OnDied -= OnPlayerDied;
        boundPlayers.Clear();
        if (players == null) return;
        foreach (PlayerStats player in players)
        {
            if (player == null || boundPlayers.Contains(player)) continue;
            boundPlayers.Add(player);
            player.OnDied += OnPlayerDied;
            if (!starts.ContainsKey(player)) starts.Add(player, player.transform.position);
            var movement = player.GetComponent<PlayerMovement>();
            if (movement != null && !originalInput.ContainsKey(movement)) originalInput.Add(movement, movement.acceptsLocalInput);
        }
    }

    public bool StartExpedition() => StartExpedition(unchecked(Environment.TickCount));

    public bool StartExpedition(int seed)
    {
        if (!isActiveAndEnabled || runActive || (State != ExpeditionState.Preparing && State != ExpeditionState.Completed && State != ExpeditionState.Failed)) return false;
        Bind();
        if (definition == null || definition.rounds == null || definition.rounds.Count == 0 || spawner == null || boundPlayers.Count == 0)
        {
            Debug.LogError("Expedition needs round data, a spawner, and at least one player.", this);
            return false;
        }
        for (int i = 0; i < definition.rounds.Count; i++)
        {
            if (definition.rounds[i] == null || (i == definition.rounds.Count - 1 && definition.rounds[i].roundType != ExpeditionRoundType.Boss))
            {
                Debug.LogError("The final expedition round must be a boss round.", this);
                return false;
            }
        }
        Session = new ExpeditionSession(seed);
        random = new System.Random(seed);
        rewards = new ArtifactRewardGenerator(random);
        ResultSummary = "";
        VisitMessage = "";
        LastSelectedRoute = null;
        CurrentVisit = null;
        PurchaseMade = false;
        rewardClaimed = false;
        RoundIndex = -1;
        candidates.Clear();
        routes.Clear();
        if (sceneEnemies != null) foreach (GameObject enemy in sceneEnemies) if (enemy != null) enemy.SetActive(false);
        rewardRecipient = null;
        foreach (PlayerStats player in boundPlayers)
        {
            var build = player.GetComponent<RunBuild>();
            if (build == null) build = player.gameObject.AddComponent<RunBuild>();
            build.BeginRun(seed);
            player.ResetForExpedition();
            player.transform.position = starts[player];
            if (rewardRecipient == null) rewardRecipient = build;
        }
        ClearProjectiles();
        runActive = true;
        return BeginNextRound(1f);
    }

    private bool BeginNextRound(float eliteMultiplier)
    {
        if (!runActive || RoundIndex + 1 >= definition.rounds.Count) return false;
        SetInput(false);
        candidates.Clear();
        routes.Clear();
        CurrentVisit = null;
        VisitMessage = "";
        PurchaseMade = false;
        RoundIndex++;
        State = ExpeditionState.Transition;
        // Round spawn coordinates stay relative to the arena start, rather than
        // following the player's last position into a wall at each transition.
        Vector3 origin = starts[boundPlayers[0]];
        ExpeditionSession startingSession = Session;
        if (!spawner.BeginWave(CurrentRound, origin, eliteMultiplier))
        {
            VisitMessage = "라운드 스폰 데이터가 올바르지 않습니다.";
            Finish(false);
            return false;
        }
        if (!runActive || Session != startingSession || State != ExpeditionState.Transition) return false;
        State = CurrentRound.roundType == ExpeditionRoundType.Boss ? ExpeditionState.BossCombat : ExpeditionState.Combat;
        SetInput(true);
        Changed?.Invoke();
        return true;
    }

    private void OnWaveChanged() => Changed?.Invoke();

    private void OnWaveCleared()
    {
        if (!runActive || (State != ExpeditionState.Combat && State != ExpeditionState.BossCombat)) return;
        if (!AnyPlayerAlive()) { Finish(false); return; }
        ExpeditionRoundDefinition cleared = CurrentRound;
        State = ExpeditionState.Reward; // Claim the transition before notifying subscribers.
        SetInput(false);
        spawner.StopAndClear();
        ClearProjectiles();
        Session.ClearRound(Mathf.Max(0, cleared.currencyReward));
        rewardClaimed = false;
        candidates.Clear();
        ArtifactCatalog catalog = definition.ResolveRewardTable(cleared.rewardTableId);
        if (catalog != null && rewardRecipient != null) candidates.AddRange(rewards.Generate(catalog, rewardRecipient, 3));
        Changed?.Invoke();
        RoundCompleted?.Invoke(cleared);
    }

    public bool SelectReward(int index)
    {
        if (!runActive || rewardClaimed || State != ExpeditionState.Reward || index < 0 || index >= candidates.Count || rewardRecipient == null) return false;
        ArtifactDefinition artifact = candidates[index];
        ExpeditionSession claimingSession = Session;
        rewardClaimed = true;
        if (!rewardRecipient.TryAcquire(artifact)) { rewardClaimed = false; return false; }
        // Acquisition raises external health/build callbacks. They may end the
        // session; never continue a reward command into a failed or new run.
        if (!runActive || Session != claimingSession || State != ExpeditionState.Reward) return false;
        Session.RecordReward(new ExpeditionRewardRecord(CurrentRound.roundId, artifact.artifactId, artifact.displayName, CurrentRound.currencyReward));
        candidates.Clear();
        AfterReward();
        RewardSelected?.Invoke(artifact);
        return true;
    }

    public bool SkipReward()
    {
        if (!runActive || rewardClaimed || State != ExpeditionState.Reward || candidates.Count != 0) return false;
        rewardClaimed = true;
        Session.RecordReward(new ExpeditionRewardRecord(CurrentRound.roundId, "", "추가 아티팩트 없음", CurrentRound.currencyReward));
        AfterReward();
        RewardSelected?.Invoke(null);
        return true;
    }

    private void AfterReward()
    {
        if (RoundIndex == definition.rounds.Count - 1) { Finish(true); return; }
        routes.Clear();
        foreach (ExpeditionRouteDefinition route in CurrentRound.nextRoutes ?? new List<ExpeditionRouteDefinition>())
            if (route != null && !routes.Contains(route) && routes.Count < 3) routes.Add(route);
        // Invalid route data must not strand an otherwise completed expedition.
        if (routes.Count == 0) { BeginNextRound(1f); return; }
        State = ExpeditionState.RouteSelection;
        Changed?.Invoke();
    }

    public bool SelectRoute(int index)
    {
        if (!runActive || State != ExpeditionState.RouteSelection || index < 0 || index >= routes.Count) return false;
        LastSelectedRoute = routes[index];
        Session.RecordRoute(LastSelectedRoute.routeId);
        routes.Clear();
        State = ExpeditionState.Transition;
        switch (LastSelectedRoute.routeType)
        {
            case ExpeditionRouteType.NormalCombat: return BeginNextRound(1f);
            case ExpeditionRouteType.EliteCombat: return BeginNextRound(Mathf.Max(1f, LastSelectedRoute.attackMultiplier));
        }
        CurrentVisit = LastSelectedRoute;
        PurchaseMade = false;
        if (CurrentVisit.routeType == ExpeditionRouteType.Recovery)
        {
            HealPlayers(CurrentVisit.healFraction);
            VisitMessage = "회복소: 생존한 플레이어의 HP를 회복했습니다. 다음 전투로 이동합니다.";
        }
        else if (CurrentVisit.routeType == ExpeditionRouteType.Shop)
            VisitMessage = "임시 상점: 고철로 회복을 한 번 구매하거나 바로 출발할 수 있습니다.";
        else
        {
            ApplyEvent(CurrentVisit);
        }
        Changed?.Invoke();
        return true;
    }

    public bool BuyRecovery()
    {
        if (!runActive || State != ExpeditionState.Transition || CurrentVisit == null || CurrentVisit.routeType != ExpeditionRouteType.Shop || PurchaseMade) return false;
        bool needsHealing = boundPlayers.Exists(player => player != null && !player.IsDead && player.currentHealth < player.EffectiveMaxHealth);
        if (!needsHealing || !Session.Spend(Mathf.Max(0, CurrentVisit.cost))) return false;
        PurchaseMade = true;
        HealPlayers(CurrentVisit.healFraction);
        VisitMessage = "회복 구매 완료. 이번 방문에서 다시 구매할 수 없습니다.";
        Changed?.Invoke();
        return true;
    }

    public bool ContinueJourney()
    {
        if (!runActive || State != ExpeditionState.Transition || CurrentVisit == null) return false;
        return BeginNextRound(1f);
    }

    private void HealPlayers(float fraction)
    {
        foreach (PlayerStats player in boundPlayers) if (player != null && !player.IsDead) player.Heal(player.EffectiveMaxHealth * Mathf.Clamp01(fraction));
    }

    private void ApplyEvent(ExpeditionRouteDefinition route)
    {
        double total = 0;
        if (route.eventOutcomes != null)
            foreach (ExpeditionEventOutcome outcome in route.eventOutcomes)
                if (outcome != null) total += Mathf.Max(0, outcome.weight);
        if (total <= 0)
        {
            VisitMessage = "조용한 길목을 지나갑니다. 다음 전투로 이동할 수 있습니다.";
            return;
        }
        double roll = random.NextDouble() * total;
        foreach (ExpeditionEventOutcome outcome in route.eventOutcomes)
        {
            if (outcome == null || outcome.weight <= 0) continue;
            roll -= outcome.weight;
            if (roll >= 0) continue;
            Session.AddCurrency(Mathf.Max(0, outcome.currency));
            HealPlayers(outcome.healFraction);
            VisitMessage = outcome.description;
            return;
        }
    }

    private bool AnyPlayerAlive() => boundPlayers.Exists(player => player != null && !player.IsDead);
    private void OnPlayerDied() { if (runActive && !AnyPlayerAlive()) Finish(false); }

    private void Finish(bool success)
    {
        if (!runActive) return;
        runActive = false;
        State = success ? ExpeditionState.Completed : ExpeditionState.Failed;
        SetInput(false);
        spawner?.StopAndClear();
        ClearProjectiles();
        candidates.Clear();
        routes.Clear();
        CurrentVisit = null;
        var result = new StringBuilder(success ? "탐험 성공" : "탐험 실패");
        result.Append("\n클리어 ").Append(Session.ClearedRounds).Append('/').Append(definition.rounds.Count);
        result.Append(" · 남은 고철 ").Append(Session.Currency);
        foreach (ExpeditionRewardRecord reward in Session.Rewards) result.Append("\n").Append(reward.DisplayName);
        ResultSummary = result.ToString();
        foreach (PlayerStats player in boundPlayers) if (player != null) player.GetComponent<RunBuild>()?.EndRun();
        Changed?.Invoke();
        ExpeditionEnded?.Invoke(success);
    }

    public bool RestartExpedition()
    {
        if (State != ExpeditionState.Completed && State != ExpeditionState.Failed) return false;
        return StartExpedition();
    }

    public void ReturnToHub()
    {
        if (runActive) Finish(false);
        SetInput(false);
        ClearProjectiles();
        string scene = definition != null ? definition.returnSceneName : "MainMenu";
        if (!string.IsNullOrEmpty(scene) && Application.CanStreamedLevelBeLoaded(scene)) SceneManager.LoadScene(scene);
        else Debug.LogWarning("Return scene is not registered in Build Settings: " + scene, this);
    }

    private void SetInput(bool enabled)
    {
        foreach (var pair in originalInput)
        {
            PlayerMovement movement = pair.Key;
            if (movement == null) continue;
            movement.acceptsLocalInput = enabled && pair.Value;
            movement.ResetInput();
            if (!enabled)
            {
                movement.GetComponent<PlayerAttackController>()?.CancelAttack();
                movement.GetComponent<PlayerDodgeController>()?.CancelDodge();
                var body = movement.GetComponent<Rigidbody2D>();
                if (body != null && body.bodyType != RigidbodyType2D.Static) body.velocity = Vector2.zero;
            }
        }
    }

    private void ClearProjectiles()
    {
        foreach (Bullet bullet in FindObjectsOfType<Bullet>()) if (bullet != null) bullet.Despawn();
    }

    void OnDisable()
    {
        if (runActive) Finish(false);
        spawner?.StopAndClear();
        if (boundSpawner != null) { boundSpawner.AllRequiredEnemiesDefeated -= OnWaveCleared; boundSpawner.Changed -= OnWaveChanged; }
        boundSpawner = null;
        foreach (PlayerStats player in boundPlayers) if (player != null) player.OnDied -= OnPlayerDied;
        boundPlayers.Clear();
        foreach (var pair in originalInput) if (pair.Key != null) pair.Key.acceptsLocalInput = pair.Value;
    }
}
