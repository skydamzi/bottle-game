using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

public sealed class ArtifactStack
{
    public ArtifactDefinition Definition { get; private set; }
    public int Count { get; internal set; }
    internal ArtifactStack(ArtifactDefinition definition) { Definition = definition; Count = 1; }
}

// One immutable attack snapshot per projectile/punch. A later reward cannot alter
// projectiles already in flight, and a previous session cannot trigger new procs.
public struct RunAttackSnapshot
{
    public float Damage { get; private set; }
    public int PierceCount { get; private set; }
    internal RunBuild Build;
    internal int Version;
    internal float ExtraHitChance, ExtraHitScale;
    internal RunAttackSnapshot(float damage, int pierce, RunBuild build, int version, float chance, float scale)
    {
        Damage = damage; PierceCount = pierce; Build = build; Version = version;
        ExtraHitChance = chance; ExtraHitScale = scale;
    }
}

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerStats))]
public class RunBuild : MonoBehaviour
{
    private static readonly List<RunBuild> activeBuilds = new List<RunBuild>();
    private readonly List<ArtifactStack> entries = new List<ArtifactStack>();
    private ReadOnlyCollection<ArtifactStack> readOnlyEntries;
    private PlayerStats stats;
    private System.Random random;
    private int version;
    private bool nextAttackCharged;
    public bool IsRunActive { get; private set; }
    public IReadOnlyList<ArtifactStack> Entries => readOnlyEntries ?? (readOnlyEntries = entries.AsReadOnly());
    public string LastFeedback { get; private set; } = "";
    public bool HasDodgeCharge => IsRunActive && nextAttackCharged;
    public event Action OnChanged;
    public event Action<string> OnEffectTriggered;

    public float MaxHealthBonus => Sum(ArtifactEffectType.MaxHealthFlat);
    public float ProjectileRangeMultiplier => Mathf.Clamp(1f + Sum(ArtifactEffectType.ProjectileRangePercent), 0.1f, 5f);
    public float AttackSpeedMultiplier
    {
        get
        {
            float bonus = Sum(ArtifactEffectType.AttackSpeedPercent);
            ForEachEffect((effect, count) =>
            {
                if (effect.effectType == ArtifactEffectType.LowHealthAttackSpeedPercent && IsLowHealth(effect.secondaryValue))
                    bonus += effect.value * count;
            });
            return Mathf.Clamp(1f + bonus, 0.1f, 5f);
        }
    }
    public float AttackDamageMultiplier
    {
        get
        {
            float bonus = Sum(ArtifactEffectType.AttackDamagePercent);
            ForEachEffect((effect, count) =>
            {
                if (effect.effectType == ArtifactEffectType.SameTagAttackPercent)
                    bonus += effect.value * count * CountTag(effect.tag);
                else if (effect.effectType == ArtifactEffectType.AllyAttackPercent && HasAlly(effect.secondaryValue))
                    bonus += effect.value * count;
            });
            return Mathf.Max(0f, 1f + bonus);
        }
    }
    public float DamageTakenMultiplier
    {
        get
        {
            float multiplier = Mathf.Max(0f, 1f + Sum(ArtifactEffectType.DamageTakenPercent));
            ForEachEffect((effect, count) =>
            {
                if (effect.effectType == ArtifactEffectType.LowHealthDefensePercent && IsLowHealth(effect.secondaryValue))
                    multiplier *= 1f - Mathf.Clamp(effect.value * count, 0f, 0.8f);
            });
            return multiplier;
        }
    }
    public string Summary
    {
        get
        {
            if (stats == null) stats = GetComponent<PlayerStats>();
            return $"ATK {stats.EffectiveAttackDamage:0.#}  HP {stats.currentHealth:0.#}/{stats.EffectiveMaxHealth:0.#}\n" +
                $"공격 속도 x{AttackSpeedMultiplier:0.##}  탄환 사거리 x{ProjectileRangeMultiplier:0.##}" +
                (HasDodgeCharge ? "\n회피 강화 공격 준비" : "");
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearRegistry() => activeBuilds.Clear();
    void Awake() { stats = GetComponent<PlayerStats>(); }
    void OnEnable()
    {
        if (stats == null) stats = GetComponent<PlayerStats>();
        if (!activeBuilds.Contains(this)) activeBuilds.Add(this);
        stats.OnDied += OnPlayerDied;
    }
    void OnDisable()
    {
        if (stats != null) stats.OnDied -= OnPlayerDied;
        activeBuilds.Remove(this);
        EndRun();
    }

    public void BeginRun(int seed)
    {
        EndRun();
        random = new System.Random(seed);
        IsRunActive = true;
        version++;
        LastFeedback = "";
        OnChanged?.Invoke();
    }

    public void EndRun()
    {
        bool changed = IsRunActive || entries.Count > 0 || nextAttackCharged;
        IsRunActive = false;
        version++;
        entries.Clear();
        nextAttackCharged = false;
        random = null;
        LastFeedback = "";
        if (stats == null) stats = GetComponent<PlayerStats>();
        if (changed) { stats.RefreshEffectiveHealth(); OnChanged?.Invoke(); }
    }

    public bool CanAcquire(ArtifactDefinition definition)
    {
        if (!IsRunActive || definition == null || string.IsNullOrWhiteSpace(definition.artifactId)) return false;
        ArtifactStack current = Find(definition.artifactId);
        if (current == null) return true;
        // ID identity is stable even if duplicate asset references appear in a catalog.
        return current.Definition.duplicatePolicy == ArtifactDuplicatePolicy.Stack &&
            current.Count < Mathf.Max(1, current.Definition.maxStacks);
    }

    public bool TryAcquire(ArtifactDefinition definition)
    {
        if (!CanAcquire(definition)) return false;
        int acquisitionVersion = version;
        ArtifactStack current = Find(definition.artifactId);
        if (current == null) entries.Add(new ArtifactStack(definition));
        else current.Count++;
        stats.RefreshEffectiveHealth();
        if (!IsRunActive || version != acquisitionVersion) return true;
        Feedback($"획득: {definition.displayName}");
        if (IsRunActive && version == acquisitionVersion) OnChanged?.Invoke();
        return true;
    }

    public void ResetTransientEffects()
    {
        nextAttackCharged = false;
        LastFeedback = "";
        OnChanged?.Invoke();
    }

    public void NotifyDodgeStarted()
    {
        if (!IsRunActive || Sum(ArtifactEffectType.DodgeNextAttackPercent) <= 0f) return;
        int dodgeVersion = version;
        nextAttackCharged = true;
        Feedback("회피 강화: 다음 공격 준비");
        if (IsRunActive && version == dodgeVersion) OnChanged?.Invoke();
    }

    public RunAttackSnapshot CreateAttack(float damage, bool bullet)
    {
        if (!IsRunActive) return new RunAttackSnapshot(damage, 0, null, 0, 0f, 0f);
        int attackVersion = version;
        if (bullet) damage *= Mathf.Max(0f, 1f + Sum(ArtifactEffectType.BulletDamagePercent));
        bool consumedCharge = nextAttackCharged;
        if (consumedCharge)
        {
            nextAttackCharged = false;
            damage *= 1f + Mathf.Max(0f, Sum(ArtifactEffectType.DodgeNextAttackPercent));
        }
        float critChance = Mathf.Clamp01(Sum(ArtifactEffectType.CriticalChance));
        bool critical = critChance > 0f && random.NextDouble() < critChance;
        if (critical) damage *= 1.5f;
        float extraChance = 0f, extraScale = 0f;
        if (bullet) ForEachEffect((effect, count) =>
        {
            if (effect.effectType != ArtifactEffectType.ExtraHitChance) return;
            extraChance += effect.value * count;
            extraScale = Mathf.Max(extraScale, effect.secondaryValue > 0f ? effect.secondaryValue : 0.35f);
        });
        var snapshot = new RunAttackSnapshot(damage, bullet ? Mathf.Clamp(Mathf.FloorToInt(Sum(ArtifactEffectType.BulletPierceCount)), 0, 8) : 0,
            this, attackVersion, Mathf.Clamp01(extraChance), extraScale);
        // Complete the snapshot before notifying observers: they may end/restart the run.
        if (consumedCharge)
        {
            Feedback("회피 강화 공격!");
            if (IsAttackCurrent(snapshot)) OnChanged?.Invoke();
        }
        if (critical && IsAttackCurrent(snapshot)) Feedback("치명타!");
        return snapshot;
    }

    public bool IsAttackCurrent(RunAttackSnapshot attack)
    {
        return IsRunActive && isActiveAndEnabled && stats != null && !stats.IsDead &&
            attack.Build == this && attack.Version == version;
    }

    public void NotifyBulletHit(RunAttackSnapshot attack, IDamageable target, IDamageDealer dealer)
    {
        if (!IsAttackCurrent(attack) || attack.ExtraHitChance <= 0f || target == null ||
            random.NextDouble() >= attack.ExtraHitChance) return;
        Component component = target as Component;
        if (component == null) return;
        MonsterStats monster = component.GetComponentInParent<MonsterStats>();
        if (monster != null && monster.currentHealth <= 0f) return;
        target.OnDamage(attack.Damage * attack.ExtraHitScale, dealer);
        if (IsAttackCurrent(attack)) Feedback("폐건전지: 추가 타격!");
    }

    private void OnPlayerDied()
    {
        // A preceding death observer may already have restarted and revived this player.
        if (stats != null && stats.IsDead) ResetTransientEffects();
    }
    private ArtifactStack Find(string id) => entries.Find(item => item.Definition != null && item.Definition.artifactId == id);
    private float Sum(ArtifactEffectType type)
    {
        float value = 0f;
        ForEachEffect((effect, count) => { if (effect.effectType == type) value += effect.value * count; });
        return value;
    }
    private void ForEachEffect(Action<ArtifactEffectDefinition, int> action)
    {
        if (!IsRunActive) return;
        foreach (ArtifactStack entry in entries)
        {
            if (entry.Definition == null || entry.Definition.effects == null) continue;
            foreach (ArtifactEffectDefinition effect in entry.Definition.effects)
                if (effect != null) action(effect, entry.Count);
        }
    }
    private int CountTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return 0;
        int count = 0;
        foreach (ArtifactStack entry in entries)
            if (entry.Definition.tags != null && entry.Definition.tags.Contains(tag)) count += entry.Count;
        return count;
    }
    private bool IsLowHealth(float threshold)
    {
        if (stats == null || stats.IsDead) return false;
        float boundary = stats.EffectiveMaxHealth * Mathf.Clamp01(threshold > 0f ? threshold : 0.35f);
        // Inclusive percentage thresholds must survive floating-point rounding at 35/100.
        return stats.currentHealth <= boundary || Mathf.Approximately(stats.currentHealth, boundary);
    }
    private bool HasAlly(float radius)
    {
        radius = Mathf.Max(0f, radius);
        foreach (RunBuild other in activeBuilds)
            if (other != null && other != this && other.IsRunActive && other.stats != null && !other.stats.IsDead &&
                (other.transform.position - transform.position).sqrMagnitude <= radius * radius) return true;
        return false;
    }
    private void Feedback(string message)
    {
        LastFeedback = message;
        OnEffectTriggered?.Invoke(message);
    }
}
