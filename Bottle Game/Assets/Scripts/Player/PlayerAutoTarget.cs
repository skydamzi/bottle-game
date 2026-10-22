using UnityEngine;

[DefaultExecutionOrder(-35)]
public class PlayerAutoTarget : MonoBehaviour
{
    [Min(0f)] public float gunRange = 2f;
    [Min(0f)] public float fistRange = 1.5f;
    [Min(0.02f)] public float searchInterval = 0.1f;
    public LayerMask targetLayers = 1 << 7;
    public Transform CurrentTarget { get; private set; }
    public event System.Action<Transform> TargetChanged;
    public bool IsTrackingActive => isActiveAndEnabled && movement != null && movement.IsSingleStick &&
        movement.CanReadLocalInput && stats != null && !stats.IsDead;
    public float CurrentRange => GetRange(attack != null ? attack.currentMode : PlayerAttackController.WeaponMode.Gun);
    public bool HasTarget => IsTrackingActive && CurrentTarget != null && IsValid(targetStats, targetCollider, CurrentRange);
    public float TargetDistance => HasTarget ? Vector2.Distance(transform.position, CurrentTarget.position) : 0f;
    private readonly Collider2D[] results = new Collider2D[64];
    private MonsterStats targetStats;
    private Collider2D targetCollider;
    private float nextSearch;
    private PlayerAttackController.WeaponMode lastMode;
    private PlayerMovement movement;
    private PlayerAttackController attack;
    private PlayerStats stats;
    private RunBuild build;

    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        attack = GetComponent<PlayerAttackController>();
        stats = GetComponent<PlayerStats>();
    }

    void Update()
    {
        if (IsTrackingActive) GetTarget(attack != null ? attack.currentMode : PlayerAttackController.WeaponMode.Gun);
        else ClearTarget();
    }

    public float GetRange(PlayerAttackController.WeaponMode mode)
    {
        float range = mode == PlayerAttackController.WeaponMode.Gun ? gunRange : fistRange;
        if (mode == PlayerAttackController.WeaponMode.Gun)
        {
            if (build == null) build = GetComponent<RunBuild>();
            if (build != null && build.isActiveAndEnabled && build.IsRunActive) range *= build.ProjectileRangeMultiplier;
        }
        return Mathf.Max(0f, range);
    }

    public Transform GetTarget(PlayerAttackController.WeaponMode mode)
    {
        Transform previous = CurrentTarget;
        float range = GetRange(mode);
        bool invalidated = !ReferenceEquals(CurrentTarget, null) && !IsValid(targetStats, targetCollider, range);
        if (invalidated || mode != lastMode) nextSearch = 0f;
        if (!IsValid(targetStats, targetCollider, range)) CurrentTarget = null;
        if (Time.time >= nextSearch)
        {
            nextSearch = Time.time + Mathf.Max(0.02f, searchInterval);
            lastMode = mode;
            Search(Mathf.Max(0f, range));
        }
        if (!ReferenceEquals(previous, CurrentTarget)) TargetChanged?.Invoke(CurrentTarget);
        return CurrentTarget;
    }

    private bool IsValid(MonsterStats stats, Collider2D col, float range)
    {
        return stats != null && stats.isActiveAndEnabled && stats.currentHealth > 0f &&
            col != null && col.enabled && col.gameObject.activeInHierarchy &&
            (targetLayers.value & (1 << col.gameObject.layer)) != 0 &&
            !stats.transform.IsChildOf(transform) &&
            ((Vector2)(stats.transform.position - transform.position)).sqrMagnitude <= range * range;
    }

    private void Search(float range)
    {
        CurrentTarget = null;
        targetStats = null;
        targetCollider = null;
        int count = Physics2D.OverlapCircleNonAlloc(transform.position, range, results, targetLayers);
        // Saturation is rare; correctness takes precedence over the reusable buffer then.
        Collider2D[] candidates = count == results.Length ? Physics2D.OverlapCircleAll(transform.position, range, targetLayers) : results;
        if (candidates != results) count = candidates.Length;
        float nearest = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            Collider2D col = candidates[i];
            if (col == null || !col.TryGetComponent<IDamageable>(out _)) continue;
            MonsterStats stats = col.GetComponentInParent<MonsterStats>();
            if (!IsValid(stats, col, range)) continue;
            float distance = ((Vector2)(stats.transform.position - transform.position)).sqrMagnitude;
            if (distance >= nearest) continue;
            nearest = distance;
            targetStats = stats;
            targetCollider = col;
            CurrentTarget = stats.transform;
        }
    }

    private void ClearTarget()
    {
        bool hadTarget = !ReferenceEquals(CurrentTarget, null);
        CurrentTarget = null;
        targetStats = null;
        targetCollider = null;
        nextSearch = 0f;
        if (hadTarget) TargetChanged?.Invoke(null);
    }

    void OnDisable() => ClearTarget();
}
