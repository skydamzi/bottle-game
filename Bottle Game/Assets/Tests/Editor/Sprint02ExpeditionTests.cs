using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class Sprint02ExpeditionTests
{
    private readonly List<Object> owned = new List<Object>();

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yield return new EnterPlayMode();
        Assert.IsTrue(Application.isPlaying);
        Application.runInBackground = true;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        yield return ClearScenario();
        yield return new ExitPlayMode();
    }

    // One Play Mode session and explicit completion markers prevent an EditMode
    // runner reload from making unexecuted scenarios look like successful tests.
    [UnityTest]
    public IEnumerator RunAllExpeditionScenarios()
    {
        int completed = 0;
        Debug.Log("SPRINT02 START SpawnerRequiresRealDeathAndRejectsDuplicateWaves");
        yield return SpawnerRequiresRealDeathAndRejectsDuplicateWaves();
        Debug.Log("SPRINT02 PASS SpawnerRequiresRealDeathAndRejectsDuplicateWaves");
        completed++;
        yield return ClearScenario();
        Debug.Log("SPRINT02 START ClearAndRewardAreGrantedExactlyOnce");
        yield return ClearAndRewardAreGrantedExactlyOnce();
        Debug.Log("SPRINT02 PASS ClearAndRewardAreGrantedExactlyOnce");
        completed++;
        yield return ClearScenario();
        Debug.Log("SPRINT02 START SixRoundsReachBossAndCompleteAfterFinalReward");
        yield return SixRoundsReachBossAndCompleteAfterFinalReward();
        Debug.Log("SPRINT02 PASS SixRoundsReachBossAndCompleteAfterFinalReward");
        completed++;
        yield return ClearScenario();
        Debug.Log("SPRINT02 START EveryRouteAppliesItsResultWithoutSkippingCombatSlots");
        yield return EveryRouteAppliesItsResultWithoutSkippingCombatSlots();
        Debug.Log("SPRINT02 PASS EveryRouteAppliesItsResultWithoutSkippingCombatSlots");
        completed++;
        yield return ClearScenario();
        Debug.Log("SPRINT02 START OnlyAllPlayersDeadFailsAndStopsTheWave");
        yield return OnlyAllPlayersDeadFailsAndStopsTheWave();
        Debug.Log("SPRINT02 PASS OnlyAllPlayersDeadFailsAndStopsTheWave");
        completed++;
        yield return ClearScenario();
        Debug.Log("SPRINT02 START TenRestartsResetSessionAndDoNotAccumulateEnemies");
        yield return TenRestartsResetSessionAndDoNotAccumulateEnemies();
        Debug.Log("SPRINT02 PASS TenRestartsResetSessionAndDoNotAccumulateEnemies");
        completed++;
        yield return ClearScenario();
        Debug.Log("SPRINT02 START SampleSceneHasPlayableExpeditionReferences");
        yield return SampleSceneHasPlayableExpeditionReferences();
        Debug.Log("SPRINT02 PASS SampleSceneHasPlayableExpeditionReferences");
        completed++;
        Assert.AreEqual(7, completed);
        Debug.Log("SPRINT02 COMPLETE " + completed);
    }

    private IEnumerator ClearScenario()
    {
        // Destroy managers before their data and templates so OnDisable can
        // unsubscribe and clear its wave using valid references.
        foreach (Object obj in owned)
        {
            GameObject go = obj as GameObject;
            if (go != null && go.GetComponent<ExpeditionManager>() != null) Object.Destroy(go);
        }
        yield return null;
        foreach (Object obj in owned) if (obj != null) Object.Destroy(obj);
        owned.Clear();
        yield return null;
    }

    private GameObject NewObject(string name)
    {
        var obj = new GameObject(name);
        owned.Add(obj);
        return obj;
    }

    private T Data<T>() where T : ScriptableObject
    {
        T data = ScriptableObject.CreateInstance<T>();
        owned.Add(data);
        return data;
    }

    private GameObject MonsterTemplate()
    {
        GameObject template = NewObject("Expedition Test Enemy Template");
        template.SetActive(false);
        template.AddComponent<MonsterStats>().maxHealth = 25f;
        return template;
    }

    private ExpeditionRoundDefinition Round(GameObject enemy, int index, int count = 2)
    {
        var round = Data<ExpeditionRoundDefinition>();
        round.roundId = "test_round_" + index;
        round.displayName = "Test Round " + index;
        round.roundType = ExpeditionRoundType.Normal;
        round.currencyReward = 100;
        round.enemies = new List<ExpeditionEnemySpawn>
        {
            new ExpeditionEnemySpawn { prefab = enemy, count = count, required = true, offset = Vector2.up * 4f }
        };
        round.nextRoutes = new List<ExpeditionRouteDefinition>();
        return round;
    }

    private ExpeditionRouteDefinition Route(ExpeditionRouteType type)
    {
        var route = Data<ExpeditionRouteDefinition>();
        route.routeId = "route_" + type;
        route.displayName = type.ToString();
        route.routeType = type;
        route.healFraction = 0.3f;
        route.cost = 10;
        route.attackMultiplier = 1.4f;
        return route;
    }

    private ArtifactCatalog Catalog()
    {
        string[] matches = AssetDatabase.FindAssets("t:ArtifactCatalog");
        Assert.Greater(matches.Length, 0, "The playable project needs a connected artifact catalog asset");
        var catalog = AssetDatabase.LoadAssetAtPath<ArtifactCatalog>(AssetDatabase.GUIDToAssetPath(matches[0]));
        Assert.NotNull(catalog);
        return catalog;
    }

    private ExpeditionManager Manager(int roundCount = 2, int playerCount = 1)
    {
        GameObject enemy = MonsterTemplate();
        var definition = Data<ExpeditionDefinition>();
        definition.artifactCatalog = Catalog();
        definition.rounds = new List<ExpeditionRoundDefinition>();
        for (int i = 0; i < roundCount; i++)
        {
            ExpeditionRoundDefinition round = Round(enemy, i);
            round.nextRoutes.Add(Route(ExpeditionRouteType.NormalCombat));
            definition.rounds.Add(round);
        }
        definition.rounds[roundCount - 1].roundType = ExpeditionRoundType.Boss;
        var players = new PlayerStats[playerCount];
        for (int i = 0; i < playerCount; i++)
        {
            players[i] = NewObject("Expedition Test Player " + i).AddComponent<PlayerStats>();
            players[i].hitStunDuration = 0f;
        }
        GameObject host = NewObject("Expedition Test Manager");
        host.SetActive(false);
        var spawner = host.AddComponent<EnemyWaveSpawner>();
        var manager = host.AddComponent<ExpeditionManager>();
        manager.definition = definition;
        manager.players = players;
        manager.spawner = spawner;
        manager.sceneEnemies = new GameObject[0];
        host.SetActive(true);
        return manager;
    }

    private void KillAll(ExpeditionManager manager)
    {
        var enemies = new List<MonsterStats>(manager.spawner.SpawnedEnemies);
        Assert.Greater(enemies.Count, 0);
        foreach (MonsterStats enemy in enemies)
            if (enemy != null && !enemy.IsDead) enemy.OnDamage(float.MaxValue, null);
    }

    private void ClaimReward(ExpeditionManager manager)
    {
        Assert.AreEqual(ExpeditionState.Reward, manager.State);
        if (manager.RewardCandidates.Count == 0) manager.SkipReward();
        else manager.SelectReward(0);
    }

    private IEnumerator SpawnerRequiresRealDeathAndRejectsDuplicateWaves()
    {
        GameObject template = MonsterTemplate();
        ExpeditionRoundDefinition round = Round(template, 0);
        GameObject optionalTemplate = MonsterTemplate();
        optionalTemplate.name = "Optional Expedition Enemy";
        round.enemies.Add(new ExpeditionEnemySpawn { prefab = optionalTemplate, count = 1, required = false });
        var spawner = NewObject("Standalone Test Spawner").AddComponent<EnemyWaveSpawner>();
        Assert.IsTrue(spawner.BeginWave(round, Vector3.zero, 1f));
        Assert.IsFalse(spawner.BeginWave(round, Vector3.zero, 1f));
        Assert.AreEqual(3, spawner.SpawnedEnemies.Count);
        var enemies = new List<MonsterStats>(spawner.SpawnedEnemies);
        MonsterStats requiredEnemy = enemies.Find(enemy => !enemy.name.StartsWith("Optional"));
        MonsterStats optionalEnemy = enemies.Find(enemy => enemy.name.StartsWith("Optional"));
        int initialAlive = spawner.AliveEnemies;
        Assert.AreEqual(2, initialAlive, "Only required enemies count toward clearing");
        requiredEnemy.gameObject.SetActive(false);
        yield return null;
        Assert.AreEqual(initialAlive, spawner.AliveEnemies, "A pooled/deactivated enemy is still alive");
        requiredEnemy.gameObject.SetActive(true);
        int deaths = 0;
        requiredEnemy.OnDied += _ => deaths++;
        requiredEnemy.OnDamage(float.MaxValue, null);
        int afterDeath = spawner.AliveEnemies;
        requiredEnemy.OnDamage(float.MaxValue, null);
        Assert.AreEqual(1, deaths);
        Assert.AreEqual(afterDeath, spawner.AliveEnemies);
        Assert.Less(afterDeath, initialAlive);
        optionalEnemy.OnDamage(float.MaxValue, null);
        Assert.AreEqual(afterDeath, spawner.AliveEnemies, "Optional enemies do not affect the clear requirement");
        spawner.StopAndClear();
        Assert.AreEqual(0, spawner.AliveEnemies);
        Assert.AreEqual(0, spawner.SpawnedEnemies.Count);
        Assert.IsTrue(spawner.BeginWave(round, Vector3.zero, 1f));
        var remaining = new List<MonsterStats>(spawner.SpawnedEnemies);
        remaining.RemoveAll(enemy => enemy.name.StartsWith("Optional"));
        int completed = 0;
        bool nestedKill = false;
        spawner.AllRequiredEnemiesDefeated += () => completed++;
        System.Action killFromChanged = () =>
        {
            if (nestedKill || spawner.AliveEnemies != 1) return;
            nestedKill = true;
            remaining[1].OnDamage(float.MaxValue, null);
        };
        spawner.Changed += killFromChanged;
        try { remaining[0].OnDamage(float.MaxValue, null); }
        finally { spawner.Changed -= killFromChanged; }
        Assert.IsTrue(nestedKill);
        Assert.AreEqual(1, completed, "A nested damage callback must complete the wave exactly once");
        spawner.StopAndClear();
        yield return null;
    }

    private IEnumerator ClearAndRewardAreGrantedExactlyOnce()
    {
        ExpeditionManager manager = Manager();
        GameObject optional = MonsterTemplate();
        optional.name = "Optional Expedition Enemy";
        manager.definition.rounds[0].enemies.Add(new ExpeditionEnemySpawn { prefab = optional, required = false });
        int clears = 0, rewards = 0;
        manager.RoundCompleted += _ => clears++;
        manager.RewardSelected += _ => rewards++;
        manager.StartExpedition(42);
        manager.StartExpedition(99);
        Assert.AreEqual(ExpeditionState.Combat, manager.State);
        Assert.AreEqual(2, manager.AliveEnemies);
        Assert.AreEqual(42, manager.Session.Seed);
        var enemies = new List<MonsterStats>(manager.spawner.SpawnedEnemies);
        enemies.RemoveAll(enemy => enemy.name.StartsWith("Optional"));
        enemies[0].OnDamage(float.MaxValue, null);
        enemies[0].OnDamage(float.MaxValue, null);
        Assert.AreEqual(ExpeditionState.Combat, manager.State, "Every required enemy must be defeated");
        enemies[1].gameObject.SetActive(false);
        yield return null;
        Assert.AreEqual(ExpeditionState.Combat, manager.State);
        enemies[1].gameObject.SetActive(true);
        enemies[1].OnDamage(float.MaxValue, null);
        enemies[1].OnDamage(float.MaxValue, null);
        Assert.AreEqual(ExpeditionState.Reward, manager.State);
        Assert.AreEqual(1, clears);
        Assert.AreEqual(100, manager.Session.Currency);
        Assert.AreEqual(0, manager.spawner.SpawnedEnemies.Count, "Remaining optional enemies are cleaned after the required kills");
        Assert.Greater(manager.RewardCandidates.Count, 0);
        int recordsBefore = manager.Session.Rewards.Count;
        manager.SkipReward();
        Assert.AreEqual(ExpeditionState.Reward, manager.State, "Nonempty choices cannot be skipped");
        manager.SelectReward(-1);
        Assert.AreEqual(ExpeditionState.Reward, manager.State);
        RunBuild build = manager.players[0].GetComponent<RunBuild>();
        bool reentered = false, nestedAccepted = false;
        System.Action<string> reenterReward = _ =>
        {
            if (reentered) return;
            reentered = true;
            nestedAccepted = manager.SelectReward(0);
        };
        build.OnEffectTriggered += reenterReward;
        try { Assert.IsTrue(manager.SelectReward(0)); }
        finally { build.OnEffectTriggered -= reenterReward; }
        int recordsAfter = manager.Session.Rewards.Count;
        manager.SelectReward(0);
        Assert.IsTrue(reentered, "The callback must run during the acquisition transaction");
        Assert.IsFalse(nestedAccepted, "A synchronous effect callback cannot grant the same reward twice");
        Assert.AreEqual(ExpeditionState.RouteSelection, manager.State);
        Assert.AreEqual(1, rewards);
        Assert.Greater(recordsAfter, recordsBefore);
        Assert.AreEqual(recordsAfter, manager.Session.Rewards.Count);
        Assert.AreEqual(100, manager.Session.Currency);
        manager.players[0].OnDamage(float.MaxValue, null);
        Assert.AreEqual(ExpeditionState.Failed, manager.State);
        manager.definition.artifactCatalog = Data<ArtifactCatalog>();
        manager.RestartExpedition();
        KillAll(manager);
        Assert.AreEqual(0, manager.RewardCandidates.Count);
        manager.SelectReward(0);
        Assert.AreEqual(ExpeditionState.Reward, manager.State);
        Assert.IsTrue(manager.SkipReward(), "An exhausted catalog must still allow the journey to continue");
        Assert.AreEqual(ExpeditionState.RouteSelection, manager.State);
        Assert.AreEqual(1, manager.Session.Rewards.Count);
        Assert.IsFalse(manager.SkipReward());
        Assert.AreEqual(1, manager.Session.Rewards.Count);
        manager.players[0].OnDamage(float.MaxValue, null);
        manager.definition.artifactCatalog = Catalog();
        manager.RestartExpedition();
        KillAll(manager);
        build = manager.players[0].GetComponent<RunBuild>();
        System.Action<string> endDuringReward = _ => manager.players[0].OnDamage(float.MaxValue, null);
        build.OnEffectTriggered += endDuringReward;
        try { manager.SelectReward(0); }
        finally { build.OnEffectTriggered -= endDuringReward; }
        Assert.AreEqual(ExpeditionState.Failed, manager.State,
            "A synchronous acquisition callback that ends the run cannot be overwritten by reward continuation");
        Assert.AreEqual(0, manager.RewardCandidates.Count);
        Assert.AreEqual(0, manager.AliveEnemies);
        yield return null;
    }

    private IEnumerator SixRoundsReachBossAndCompleteAfterFinalReward()
    {
        ExpeditionManager manager = Manager(6);
        manager.definition.rounds[4].roundType = ExpeditionRoundType.Special;
        int cleared = 0, ended = 0;
        bool success = false;
        manager.RoundCompleted += _ => cleared++;
        manager.ExpeditionEnded += won => { ended++; success = won; };
        manager.StartExpedition(91);
        for (int i = 0; i < 6; i++)
        {
            Assert.AreEqual(i, manager.RoundIndex);
            Assert.AreSame(manager.definition.rounds[i], manager.CurrentRound);
            Assert.AreEqual(i == 5 ? ExpeditionState.BossCombat : ExpeditionState.Combat, manager.State);
            KillAll(manager);
            Assert.AreEqual(ExpeditionState.Reward, manager.State);
            Assert.AreEqual(0, ended, "The final reward must be claimed before completion");
            ClaimReward(manager);
            if (i < 5)
            {
                Assert.AreEqual(ExpeditionState.RouteSelection, manager.State);
                Assert.Greater(manager.AvailableRoutes.Count, 0);
                manager.SelectRoute(0);
            }
            yield return null;
        }
        Assert.AreEqual(ExpeditionState.Completed, manager.State);
        Assert.AreEqual(6, cleared);
        Assert.AreEqual(1, ended);
        Assert.IsTrue(success);
        Assert.IsNotEmpty(manager.ResultSummary);
        Assert.AreEqual(0, manager.AliveEnemies);
        Assert.AreEqual(0, manager.RewardCandidates.Count);
        manager.SelectReward(0);
        manager.SelectRoute(0);
        manager.ContinueJourney();
        Assert.AreEqual(ExpeditionState.Completed, manager.State);
        Assert.AreEqual(1, ended);
    }

    private IEnumerator EveryRouteAppliesItsResultWithoutSkippingCombatSlots()
    {
        ExpeditionRouteType[] types =
        {
            ExpeditionRouteType.NormalCombat, ExpeditionRouteType.EliteCombat,
            ExpeditionRouteType.Recovery, ExpeditionRouteType.Shop, ExpeditionRouteType.RandomEvent
        };
        foreach (ExpeditionRouteType type in types)
        {
            ExpeditionManager manager = Manager();
            ExpeditionRouteDefinition route = Route(type);
            manager.definition.rounds[0].nextRoutes.Clear();
            manager.definition.rounds[0].nextRoutes.Add(route);
            manager.StartExpedition(7);
            KillAll(manager);
            ClaimReward(manager);
            PlayerStats player = manager.players[0];
            player.currentHealth = 20f;
            int coins = manager.Session.Currency;
            manager.SelectRoute(-1);
            Assert.AreEqual(ExpeditionState.RouteSelection, manager.State);
            manager.SelectRoute(0);
            Assert.AreSame(route, manager.LastSelectedRoute);
            if (type == ExpeditionRouteType.NormalCombat || type == ExpeditionRouteType.EliteCombat)
            {
                Assert.AreEqual(1, manager.RoundIndex);
                Assert.AreEqual(ExpeditionState.BossCombat, manager.State);
                if (type == ExpeditionRouteType.EliteCombat)
                {
                    foreach (MonsterStats enemy in manager.spawner.SpawnedEnemies)
                        Assert.Greater(enemy.maxHealth, 25f, "Elite route strengthens the next encounter");
                }
            }
            else
            {
                Assert.AreEqual(ExpeditionState.Transition, manager.State);
                Assert.AreEqual(0, manager.RoundIndex, "A visit does not consume a combat definition");
                if (type == ExpeditionRouteType.Recovery)
                    Assert.Greater(player.currentHealth, 20f);
                if (type == ExpeditionRouteType.Shop)
                {
                    Assert.That(player.currentHealth, Is.EqualTo(20f).Within(0.01f));
                    manager.BuyRecovery();
                    float healthAfterPurchase = player.currentHealth;
                    int currencyAfterPurchase = manager.Session.Currency;
                    Assert.Greater(healthAfterPurchase, 20f);
                    Assert.AreEqual(coins - route.cost, currencyAfterPurchase);
                    manager.BuyRecovery();
                    Assert.AreEqual(currencyAfterPurchase, manager.Session.Currency);
                    Assert.AreEqual(healthAfterPurchase, player.currentHealth);
                }
                if (type == ExpeditionRouteType.RandomEvent)
                {
                    Assert.IsTrue(manager.Session.Currency > coins || player.currentHealth > 20f,
                        "An event must apply an actual currency or health outcome");
                    Assert.IsNotEmpty(manager.VisitMessage);
                }
                float healthAfterVisit = player.currentHealth;
                int currencyAfterVisit = manager.Session.Currency;
                manager.SelectRoute(0);
                Assert.AreEqual(0, manager.RoundIndex);
                Assert.AreEqual(healthAfterVisit, player.currentHealth);
                Assert.AreEqual(currencyAfterVisit, manager.Session.Currency, "Duplicate selection must not reapply visit rewards");
                Assert.AreEqual(1, manager.Session.SelectedRoutes.Count);
                manager.ContinueJourney();
                Assert.AreEqual(1, manager.RoundIndex);
                Assert.AreEqual(ExpeditionState.BossCombat, manager.State);
                manager.ContinueJourney();
                Assert.AreEqual(1, manager.RoundIndex);
            }
            yield return ClearScenario();
        }
    }

    private IEnumerator OnlyAllPlayersDeadFailsAndStopsTheWave()
    {
        ExpeditionManager manager = Manager(2, 2);
        int ended = 0;
        bool success = true;
        manager.ExpeditionEnded += won => { ended++; success = won; };
        manager.StartExpedition(3);
        manager.players[0].OnDamage(float.MaxValue, null);
        Assert.AreEqual(ExpeditionState.Combat, manager.State);
        manager.players[1].OnDamage(float.MaxValue, null);
        manager.players[1].OnDamage(float.MaxValue, null);
        Assert.AreEqual(ExpeditionState.Failed, manager.State);
        Assert.AreEqual(1, ended);
        Assert.IsFalse(success);
        Assert.AreEqual(0, manager.AliveEnemies);
        Assert.AreEqual(0, manager.spawner.SpawnedEnemies.Count);
        Assert.AreEqual(0, manager.RewardCandidates.Count);
        manager.SelectReward(0);
        manager.SelectRoute(0);
        manager.ContinueJourney();
        Assert.AreEqual(ExpeditionState.Failed, manager.State);
        Assert.AreEqual(1, ended);
        yield return null;
    }

    private IEnumerator TenRestartsResetSessionAndDoNotAccumulateEnemies()
    {
        ExpeditionManager manager = Manager();
        int ended = 0;
        manager.ExpeditionEnded += _ => ended++;
        manager.StartExpedition(100);
        for (int i = 0; i < 10; i++)
        {
            Assert.AreEqual(0, manager.RoundIndex);
            Assert.AreEqual(ExpeditionState.Combat, manager.State);
            Assert.AreEqual(0, manager.Session.Currency);
            Assert.AreEqual(0, manager.Session.Rewards.Count);
            Assert.AreEqual(2, manager.spawner.SpawnedEnemies.Count);
            KillAll(manager);
            ClaimReward(manager);
            Assert.Greater(manager.Session.Rewards.Count, 0);
            manager.SelectRoute(0);
            manager.players[0].OnDamage(float.MaxValue, null);
            Assert.AreEqual(ExpeditionState.Failed, manager.State);
            Assert.AreEqual(i + 1, ended);
            manager.RestartExpedition();
            yield return null;
            Assert.IsFalse(manager.players[0].IsDead, "A fresh run restores the player");
            Assert.That(manager.players[0].currentHealth, Is.EqualTo(manager.players[0].maxHealth).Within(0.01f));
            Assert.AreEqual(2, manager.AliveEnemies);
        }
        Assert.AreEqual(10, ended, "Restart must not accumulate player death subscriptions");
    }

    private IEnumerator SampleSceneHasPlayableExpeditionReferences()
    {
        SceneManager.LoadScene("SampleScene");
        yield return null;
        yield return null;
        ExpeditionManager manager = Object.FindObjectOfType<ExpeditionManager>();
        Assert.NotNull(manager, "SampleScene must expose the playable expedition loop");
        Assert.NotNull(manager.definition);
        Assert.AreEqual(6, manager.definition.rounds.Count);
        Assert.AreEqual(ExpeditionRoundType.Boss, manager.definition.rounds[5].roundType);
        Assert.NotNull(manager.definition.artifactCatalog);
        Assert.NotNull(manager.spawner);
        Assert.Greater(manager.players.Length, 0);
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Assert.NotNull(player);
        Assert.AreSame(player.GetComponent<PlayerStats>(), manager.players[0]);
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        Assert.NotNull(movement.joystick);
        Assert.NotNull(player.GetComponent<PlayerAimController>().joystick);
        Assert.NotNull(player.GetComponent<PlayerDodgeController>());
        Assert.NotNull(player.GetComponent<JoystickFlickDetector>());
        Assert.NotNull(player.GetComponent<PlayerAttackController>());
        Assert.AreEqual(ExpeditionState.Preparing, manager.State);
        manager.StartExpedition(9);
        Assert.AreEqual(ExpeditionState.Combat, manager.State);
        Assert.Greater(manager.AliveEnemies, 0);
        foreach (MonsterStats enemy in manager.spawner.SpawnedEnemies)
        {
            Assert.NotNull(enemy.GetComponent<MonsterMovement>(), "The real existing enemy prefab is reused");
            Assert.NotNull(enemy.GetComponent<Rigidbody2D>());
        }
        KillAll(manager);
        Assert.AreEqual(ExpeditionState.Reward, manager.State);
        Assert.IsFalse(movement.acceptsLocalInput, "Reward cards must suspend combat input");
        ClaimReward(manager);
        Assert.AreEqual(ExpeditionState.RouteSelection, manager.State);
        Assert.That(manager.AvailableRoutes.Count, Is.InRange(2, 3));
        manager.ReturnToHub();
        yield return null;
        yield return null;
        Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name);
        Assert.IsNull(Object.FindObjectOfType<ExpeditionManager>());
        SceneManager.LoadScene("SampleScene");
        yield return null;
        yield return null;
        manager = Object.FindObjectOfType<ExpeditionManager>();
        Assert.NotNull(manager);
        Assert.AreEqual(ExpeditionState.Preparing, manager.State);
        Assert.IsNull(manager.Session, "Returning and reentering creates a fresh scene session");
        Assert.LessOrEqual(Object.FindObjectsOfType<PoolManager>().Length, 1);
        Assert.LessOrEqual(Object.FindObjectsOfType<SoundManager>().Length, 1);
        int cleared = 0;
        manager.RoundCompleted += _ => cleared++;
        manager.StartExpedition(10);
        KillAll(manager);
        Assert.AreEqual(1, cleared, "Scene reentry must not duplicate round event delivery");
    }
}
