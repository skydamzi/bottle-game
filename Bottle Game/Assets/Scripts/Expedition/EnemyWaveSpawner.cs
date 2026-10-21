using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyWaveSpawner : MonoBehaviour
{
    public event Action AllRequiredEnemiesDefeated;
    public event Action Changed;
    private readonly HashSet<MonsterStats> spawned = new HashSet<MonsterStats>();
    private readonly HashSet<MonsterStats> required = new HashSet<MonsterStats>();
    private GameObject waveRoot;
    private bool waveActive;
    private int generation;
    public int AliveEnemies => required.Count;
    public IReadOnlyCollection<MonsterStats> SpawnedEnemies => new List<MonsterStats>(spawned).AsReadOnly();

    public bool BeginWave(ExpeditionRoundDefinition round, Vector3 origin, float eliteMultiplier = 1f)
    {
        if (waveActive || round == null || round.enemies == null || round.enemies.Count == 0) return false;
        bool hasRequired = false;
        foreach (ExpeditionEnemySpawn entry in round.enemies)
        {
            if (entry == null || entry.prefab == null || entry.count < 1 || entry.prefab.GetComponent<MonsterStats>() == null) return false;
            hasRequired |= entry.required;
        }
        if (!hasRequired) return false;
        StopAndClear();
        waveRoot = new GameObject("Expedition Wave " + round.roundId);
        waveRoot.transform.SetParent(transform, false);
        waveRoot.SetActive(false);
        waveActive = true;
        foreach (ExpeditionEnemySpawn entry in round.enemies)
        {
            for (int i = 0; i < entry.count; i++)
            {
                Vector2 spread = i == 0 ? Vector2.zero : new Vector2(Mathf.Cos(i * 2.4f), Mathf.Sin(i * 2.4f)) * 0.6f * i;
                GameObject instance = Instantiate(entry.prefab, origin + (Vector3)(entry.offset + spread), Quaternion.identity, waveRoot.transform);
                MonsterStats stats = instance.GetComponent<MonsterStats>();
                stats.maxHealth *= Mathf.Max(0.01f, entry.healthMultiplier) * Mathf.Max(1f, eliteMultiplier);
                stats.attackDamage *= Mathf.Max(0f, entry.damageMultiplier) * Mathf.Max(1f, eliteMultiplier);
                var movement = instance.GetComponent<MonsterMovement>();
                if (movement != null) movement.speed *= Mathf.Max(0f, entry.speedMultiplier);
                stats.OnDied += OnEnemyDied;
                spawned.Add(stats);
                if (entry.required) required.Add(stats);
                instance.SetActive(true);
            }
        }
        waveRoot.SetActive(true);
        // Awake initializes health for ordinary prefabs. Inactive scene templates
        // used by designers may already have awoken, so initialize this lease too.
        foreach (MonsterStats stats in spawned) stats.InitializeForSpawn();
        Changed?.Invoke();
        return true;
    }

    private void OnEnemyDied(MonsterStats enemy)
    {
        if (!waveActive || !spawned.Remove(enemy)) return;
        enemy.OnDied -= OnEnemyDied;
        required.Remove(enemy);
        int completedGeneration = generation;
        bool completed = required.Count == 0;
        if (completed) waveActive = false;
        Changed?.Invoke();
        // A Changed listener may kill another enemy or replace/stop the wave.
        if (completed && generation == completedGeneration) AllRequiredEnemiesDefeated?.Invoke();
    }

    public void StopAndClear()
    {
        generation++;
        waveActive = false;
        foreach (MonsterStats enemy in spawned) if (enemy != null) enemy.OnDied -= OnEnemyDied;
        spawned.Clear();
        required.Clear();
        if (waveRoot != null)
        {
            // Disable immediately: Destroy is deferred until the end of the frame.
            waveRoot.SetActive(false);
            Destroy(waveRoot);
            waveRoot = null;
        }
    }

    void OnDisable() => StopAndClear();
}
