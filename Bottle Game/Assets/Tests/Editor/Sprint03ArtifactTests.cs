using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class Sprint03ArtifactTests
{
    private readonly List<Object> objects = new List<Object>();
    private float originalCaptureDeltaTime;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yield return new EnterPlayMode();
        Assert.IsTrue(Application.isPlaying);
        Application.runInBackground = true;
        originalCaptureDeltaTime = Time.captureDeltaTime;
        Time.captureDeltaTime = 1f / 60f;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        foreach (Object obj in objects) if (obj != null) Object.Destroy(obj);
        objects.Clear();
        Time.captureDeltaTime = originalCaptureDeltaTime;
        yield return null;
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator RunAllArtifactScenarios()
    {
        int completed = 0;
        AcquisitionStacksAndRestoresBaseStats(); completed++;
        Debug.Log("SPRINT03 PASS AcquisitionStacksAndRestoresBaseStats");
        CandidatesAreSeededUniqueAndEligible(); completed++;
        Debug.Log("SPRINT03 PASS CandidatesAreSeededUniqueAndEligible");
        yield return DodgeChargeConsumesOnceAndDeathClearsIt(); completed++;
        Debug.Log("SPRINT03 PASS DodgeChargeConsumesOnceAndDeathClearsIt");
        BulletPiercesDistinctEnemiesAndLeaseInitClearsState(); completed++;
        Debug.Log("SPRINT03 PASS BulletPiercesDistinctEnemiesAndLeaseInitClearsState");
        ExtraHitDoesNotRecurseOrSurviveRunEnd(); completed++;
        Debug.Log("SPRINT03 PASS ExtraHitDoesNotRecurseOrSurviveRunEnd");
        ConditionalEffectsAndPlayerBuildsAreIndependent(); completed++;
        Debug.Log("SPRINT03 PASS ConditionalEffectsAndPlayerBuildsAreIndependent");
        TenRunsDoNotAccumulateStatsOrSubscriptions(); completed++;
        Debug.Log("SPRINT03 PASS TenRunsDoNotAccumulateStatsOrSubscriptions");
        yield return FistSpeedStillCompletesExtensionHoldAndRetraction(); completed++;
        Debug.Log("SPRINT03 PASS FistSpeedStillCompletesExtensionHoldAndRetraction");
        GunUsesSnapshotAndRangeWithoutChangingPermanentFields(); completed++;
        Debug.Log("SPRINT03 PASS GunUsesSnapshotAndRangeWithoutChangingPermanentFields");
        FistDeduplicatesMonsterHurtboxesAndRejectsInactiveCallbacks(); completed++;
        Debug.Log("SPRINT03 PASS FistDeduplicatesMonsterHurtboxesAndRejectsInactiveCallbacks");
        AttackFeedbackCannotSpawnAfterRunEnds(); completed++;
        Debug.Log("SPRINT03 PASS AttackFeedbackCannotSpawnAfterRunEnds");
        BulletDamageObserverCannotDespawnTheNextLease(); completed++;
        Debug.Log("SPRINT03 PASS BulletDamageObserverCannotDespawnTheNextLease");
        ExtraHitRunEndKeepsFeedbackCleared(); completed++;
        Debug.Log("SPRINT03 PASS ExtraHitRunEndKeepsFeedbackCleared");
        RevivingDeathObserverPreservesTheNewRunsCharge(); completed++;
        Debug.Log("SPRINT03 PASS RevivingDeathObserverPreservesTheNewRunsCharge");
        Assert.AreEqual(14, completed);
        Debug.Log("SPRINT03 COMPLETE " + completed);
    }

    private void AcquisitionStacksAndRestoresBaseStats()
    {
        RunBuild build = Actor();
        PlayerStats stats = build.GetComponent<PlayerStats>();
        ArtifactDefinition damage = Artifact("damage", ArtifactEffectType.AttackDamagePercent, 0.2f);
        ArtifactDefinition health = Artifact("health", ArtifactEffectType.MaxHealthFlat, 25f);
        damage.maxStacks = 2;
        health.duplicatePolicy = ArtifactDuplicatePolicy.Unique;
        Assert.IsFalse(build.TryAcquire(damage));
        build.BeginRun(7);
        Assert.IsTrue(build.TryAcquire(damage));
        Assert.IsTrue(build.TryAcquire(damage));
        Assert.IsFalse(build.TryAcquire(damage));
        Assert.IsTrue(build.TryAcquire(health));
        Assert.IsFalse(build.TryAcquire(health));
        Assert.AreEqual(2, build.Entries[0].Count);
        Assert.That(stats.EffectiveAttackDamage, Is.EqualTo(14f).Within(0.001f));
        Assert.AreEqual(125f, stats.EffectiveMaxHealth);
        Assert.AreEqual(100f, stats.currentHealth, "Capacity increases are not free healing");
        stats.Heal(25f);
        Assert.AreEqual(125f, stats.currentHealth);
        Assert.AreEqual(10f, stats.attackDamage);
        Assert.AreEqual(100f, stats.maxHealth);
        build.EndRun();
        Assert.AreEqual(0, build.Entries.Count);
        Assert.AreEqual(10f, stats.EffectiveAttackDamage);
        Assert.AreEqual(100f, stats.EffectiveMaxHealth);
        Assert.AreEqual(100f, stats.currentHealth);
        Assert.AreEqual(1f, stats.AttackSpeedMultiplier);
        Assert.IsFalse(build.CanAcquire(damage));
    }

    private void CandidatesAreSeededUniqueAndEligible()
    {
        RunBuild build = Actor();
        build.BeginRun(1);
        ArtifactCatalog catalog = Track(ScriptableObject.CreateInstance<ArtifactCatalog>());
        for (int i = 0; i < 6; i++)
        {
            ArtifactDefinition artifact = Artifact("candidate" + i, ArtifactEffectType.AttackDamagePercent, 0.1f);
            artifact.weight = i + 1;
            artifact.duplicatePolicy = ArtifactDuplicatePolicy.Unique;
            catalog.artifacts.Add(artifact);
        }
        catalog.artifacts.Add(catalog.artifacts[0]);
        catalog.artifacts.Add(null);
        ArtifactDefinition zero = Artifact("disabled", ArtifactEffectType.AttackDamagePercent, 0f);
        zero.weight = 0f;
        catalog.artifacts.Add(zero);
        Assert.IsTrue(build.TryAcquire(catalog.artifacts[0]));
        List<ArtifactDefinition> a = new ArtifactRewardGenerator(new System.Random(72)).Generate(catalog, build);
        List<ArtifactDefinition> b = new ArtifactRewardGenerator(new System.Random(72)).Generate(catalog, build);
        Assert.AreEqual(3, a.Count);
        CollectionAssert.AreEqual(a, b);
        Assert.AreEqual(3, new HashSet<ArtifactDefinition>(a).Count);
        CollectionAssert.DoesNotContain(a, catalog.artifacts[0]);
        CollectionAssert.DoesNotContain(a, zero);
        foreach (ArtifactDefinition artifact in catalog.artifacts)
            if (artifact != null && artifact != zero) build.TryAcquire(artifact);
        Assert.AreEqual(0, new ArtifactRewardGenerator(new System.Random(1)).Generate(catalog, build).Count);
        Assert.AreEqual(0, new ArtifactRewardGenerator(new System.Random(1)).Generate(null, build).Count);
        Assert.Throws<ArgumentNullException>(() => new ArtifactRewardGenerator(null));
    }

    private IEnumerator DodgeChargeConsumesOnceAndDeathClearsIt()
    {
        RunBuild build = Actor();
        build.BeginRun(2);
        build.TryAcquire(Artifact("cap", ArtifactEffectType.DodgeNextAttackPercent, 0.6f));
        PlayerDodgeController dodge = build.GetComponent<PlayerDodgeController>();
        PlayerStats stats = build.GetComponent<PlayerStats>();
        yield return null; // Movement.Start applies mode before the gesture.
        Assert.IsTrue(dodge.TryDodge(Vector2.right));
        Assert.IsTrue(build.HasDodgeCharge);
        Assert.That(build.CreateAttack(10f, true).Damage, Is.EqualTo(16f).Within(0.001f));
        Assert.AreEqual(10f, build.CreateAttack(10f, true).Damage);
        Assert.IsFalse(build.HasDodgeCharge);
        dodge.ResetForExpedition();
        Assert.IsTrue(dodge.TryDodge(Vector2.up));
        Assert.IsTrue(build.HasDodgeCharge);
        stats.OnDamage(1000f, null); // At start of dodge, invulnerability has not opened.
        Assert.IsTrue(stats.IsDead);
        Assert.IsFalse(build.HasDodgeCharge);
        stats.ResetForExpedition();
        Assert.AreEqual(100f, stats.currentHealth);
        Assert.IsFalse(stats.IsStunned);
        Assert.IsTrue(dodge.TryDodge(Vector2.down), "A fresh expedition resets the previous dodge cooldown");
        dodge.CancelDodge();
        Assert.That(build.CreateAttack(10f, false).Damage, Is.EqualTo(16f).Within(0.001f));
        Assert.AreEqual(10f, build.CreateAttack(10f, false).Damage);
        build.EndRun();
    }

    private void BulletPiercesDistinctEnemiesAndLeaseInitClearsState()
    {
        RunBuild build = Actor();
        build.BeginRun(10);
        build.TryAcquire(Artifact("pierce", ArtifactEffectType.BulletPierceCount, 1f));
        PoolManager pool = GetPool();
        GameObject template = NewObject("Piercing bullet template");
        template.SetActive(false);
        template.AddComponent<Bullet>();
        pool.PreparePool(template, 1);
        Bullet bullet = pool.GetFromPool(template, Vector3.zero, Quaternion.identity).GetComponent<Bullet>();
        bullet.Init(10f, build.gameObject);
        bullet.SetRunAttack(build.CreateAttack(10f, true));
        Collider2D first = Enemy(out MonsterStats firstStats);
        Collider2D second = Enemy(out MonsterStats secondStats);
        Collider2D third = Enemy(out MonsterStats thirdStats);
        Hit(bullet, first);
        Hit(bullet, first);
        GameObject otherHurt = NewObject("Second hurtbox on same monster");
        otherHurt.SetActive(false);
        otherHurt.transform.SetParent(firstStats.transform);
        otherHurt.AddComponent<MonsterHurtbox>();
        Collider2D otherCollider = otherHurt.AddComponent<CircleCollider2D>();
        otherHurt.SetActive(true);
        Hit(bullet, otherCollider);
        Assert.AreEqual(90f, firstStats.currentHealth);
        Hit(bullet, second);
        Assert.AreEqual(90f, secondStats.currentHealth);
        Hit(bullet, third);
        Assert.AreEqual(100f, thirdStats.currentHealth);
        // Pool lease initialization removes the previous pierce count and hit set.
        Bullet reused = pool.GetFromPool(template, Vector3.zero, Quaternion.identity).GetComponent<Bullet>();
        Assert.AreSame(bullet, reused);
        bullet.Init(10f, build.gameObject);
        Hit(bullet, third);
        Hit(bullet, first);
        Assert.AreEqual(90f, thirdStats.currentHealth);
        Assert.AreEqual(90f, firstStats.currentHealth);
    }

    private void ExtraHitDoesNotRecurseOrSurviveRunEnd()
    {
        RunBuild build = Actor();
        build.BeginRun(10);
        ArtifactDefinition battery = Artifact("battery", ArtifactEffectType.ExtraHitChance, 1f, 0.5f);
        build.TryAcquire(battery);
        int procs = 0;
        build.OnEffectTriggered += message => { if (message.Contains("추가 타격")) procs++; };
        Bullet bullet = NewObject("Battery bullet").AddComponent<Bullet>();
        bullet.Init(10f, build.gameObject);
        bullet.SetRunAttack(build.CreateAttack(10f, true));
        Collider2D enemy = Enemy(out MonsterStats monster);
        Hit(bullet, enemy);
        Hit(bullet, enemy);
        Assert.AreEqual(85f, monster.currentHealth);
        Assert.AreEqual(1, procs);
        RunAttackSnapshot oldAttack = build.CreateAttack(10f, true);
        build.EndRun();
        build.BeginRun(10);
        build.TryAcquire(battery);
        build.NotifyBulletHit(oldAttack, enemy.GetComponent<IDamageable>(), bullet);
        Assert.AreEqual(85f, monster.currentHealth);
        Assert.AreEqual(1, procs);
    }

    private void ConditionalEffectsAndPlayerBuildsAreIndependent()
    {
        RunBuild a = Actor(), b = Actor();
        a.transform.position = new Vector3(100f, 0f, 0f);
        b.transform.position = new Vector3(200f, 0f, 0f);
        PlayerStats stats = a.GetComponent<PlayerStats>();
        a.BeginRun(1); b.BeginRun(2);
        ArtifactDefinition valve = Artifact("valve", ArtifactEffectType.LowHealthDefensePercent, 0.3f, 0.35f);
        valve.effects.Add(new ArtifactEffectDefinition { effectType = ArtifactEffectType.LowHealthAttackSpeedPercent, value = 0.25f, secondaryValue = 0.35f });
        Assert.IsTrue(a.TryAcquire(valve));
        Assert.AreEqual(1f, stats.AttackSpeedMultiplier);
        stats.currentHealth = 35.01f;
        Assert.AreEqual(1f, stats.AttackSpeedMultiplier);
        stats.currentHealth = 35f;
        Assert.AreEqual(1.25f, stats.AttackSpeedMultiplier);
        stats.OnDamage(10f, null);
        Assert.That(stats.currentHealth, Is.EqualTo(28f).Within(0.001f));
        stats.Heal(100f);
        Assert.AreEqual(1f, stats.AttackSpeedMultiplier);
        a.TryAcquire(Artifact("ally", ArtifactEffectType.AllyAttackPercent, 0.2f, 3f));
        Assert.AreEqual(10f, stats.EffectiveAttackDamage);
        b.transform.position = a.transform.position + Vector3.right;
        Assert.That(stats.EffectiveAttackDamage, Is.EqualTo(12f).Within(0.001f));
        Assert.AreEqual(10f, b.GetComponent<PlayerStats>().EffectiveAttackDamage);
        b.EndRun();
        Assert.AreEqual(10f, stats.EffectiveAttackDamage, "A player outside the exploration is not an active ally");
        ArtifactDefinition recycler = Artifact("recycler", ArtifactEffectType.SameTagAttackPercent, 0.04f);
        recycler.effects[0].tag = "metal";
        ArtifactDefinition metal = Artifact("metal", ArtifactEffectType.AttackDamagePercent, 0f);
        metal.tags.Add("metal");
        a.TryAcquire(recycler); a.TryAcquire(metal); a.TryAcquire(metal);
        Assert.That(stats.EffectiveAttackDamage, Is.EqualTo(10.8f).Within(0.001f));
        a.EndRun();
        Assert.AreEqual(10f, stats.EffectiveAttackDamage);
    }

    private void TenRunsDoNotAccumulateStatsOrSubscriptions()
    {
        RunBuild build = Actor();
        PlayerStats stats = build.GetComponent<PlayerStats>();
        ArtifactDefinition artifact = Artifact("loop", ArtifactEffectType.AttackDamagePercent, 0.3f);
        artifact.effects.Add(new ArtifactEffectDefinition { effectType = ArtifactEffectType.MaxHealthFlat, value = 25f });
        artifact.effects.Add(new ArtifactEffectDefinition { effectType = ArtifactEffectType.AttackSpeedPercent, value = 0.2f });
        for (int i = 0; i < 10; i++)
        {
            build.BeginRun(i);
            Assert.IsTrue(build.TryAcquire(artifact));
            Assert.AreEqual(125f, stats.EffectiveMaxHealth);
            Assert.That(stats.EffectiveAttackDamage, Is.EqualTo(13f).Within(0.001f));
            build.enabled = false;
            Assert.IsFalse(build.IsRunActive);
            Assert.AreEqual(0, build.Entries.Count);
            Assert.AreEqual(100f, stats.EffectiveMaxHealth);
            Assert.AreEqual(10f, stats.EffectiveAttackDamage);
            Assert.AreEqual(1f, stats.AttackSpeedMultiplier);
            build.enabled = true;
            stats.ResetForExpedition();
        }
        build.BeginRun(99);
        int deathChanges = 0;
        build.OnChanged += () => deathChanges++;
        stats.OnDamage(1000f, null);
        Assert.AreEqual(1, deathChanges, "Repeated enables must leave exactly one death subscription");
        stats.ResetForExpedition();
        build.EndRun();
    }

    private IEnumerator FistSpeedStillCompletesExtensionHoldAndRetraction()
    {
        GameObject player = NewObject("Fist build player");
        player.SetActive(false);
        player.AddComponent<PlayerStats>();
        RunBuild build = player.AddComponent<RunBuild>();
        PlayerAttackController attack = player.AddComponent<PlayerAttackController>();
        NeckEffectController effects = player.AddComponent<NeckEffectController>();
        GameObject neck = NewObject("Fist neck"); neck.transform.SetParent(player.transform);
        GameObject glove = NewObject("Fist glove"); glove.transform.SetParent(neck.transform);
        glove.AddComponent<CircleCollider2D>().isTrigger = true;
        FistHitbox hitbox = glove.AddComponent<FistHitbox>();
        effects.neckTransform = neck.transform;
        effects.gloveTransform = glove.transform;
        effects.gloveHitbox = hitbox;
        attack.neckTransform = neck.transform;
        attack.currentMode = PlayerAttackController.WeaponMode.Fist;
        player.SetActive(true);
        build.BeginRun(0);
        build.TryAcquire(Artifact("speed", ArtifactEffectType.AttackSpeedPercent, 0.5f));
        yield return null;
        typeof(PlayerAttackController).GetMethod("HandleRelease", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(attack, null);
        Assert.IsTrue(attack.isAttacking);
        float before = Time.time, previous = 1f;
        bool extended = false, retracted = false, reachedRest = false;
        while (attack.isAttacking && Time.time < before + 2f)
        {
            float y = neck.transform.localScale.y;
            if (y > 1.1f) extended = true;
            if (extended && y > 1f && y < previous) retracted = true;
            if (retracted && effects.IsFistAnimating && Mathf.Abs(y - 1f) < 0.001f) reachedRest = true;
            previous = y;
            yield return null;
        }
        Assert.IsFalse(attack.isAttacking);
        Assert.IsTrue(extended);
        Assert.IsTrue(retracted);
        Assert.IsTrue(reachedRest);
        Assert.That(Time.time - before, Is.GreaterThanOrEqualTo(NeckEffectController.FistDuration / 1.5f));
        Assert.AreEqual(1f, neck.transform.localScale.y);
        build.TryAcquire(Artifact("cancel-cap", ArtifactEffectType.DodgeNextAttackPercent, 0.6f));
        build.NotifyDodgeStarted();
        build.OnEffectTriggered += message =>
        {
            if (message == "회피 강화 공격!") { build.EndRun(); attack.CancelAttack(); }
        };
        typeof(PlayerAttackController).GetMethod("HandleRelease", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(attack, null);
        yield return null;
        Assert.IsFalse(attack.isAttacking);
        Assert.IsFalse(effects.IsFistAnimating);
        Assert.IsFalse(effects.isStretching);
        Assert.AreEqual(1f, neck.transform.localScale.y, "A cancelled activation must not start a child stretch coroutine");
        Assert.IsFalse(glove.activeSelf);
    }

    private void GunUsesSnapshotAndRangeWithoutChangingPermanentFields()
    {
        PoolManager pool = GetPool();
        RunBuild build = Actor();
        GameObject gunObject = NewObject("Artifact gun");
        gunObject.SetActive(false); gunObject.transform.SetParent(build.transform);
        Gun gun = gunObject.AddComponent<Gun>();
        gun.weaponSpawnPoint = gunObject.transform;
        gun.bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bullet/Bullet.prefab");
        gun.bulletLifeTime = 2f;
        gunObject.SetActive(true);
        build.BeginRun(3);
        build.TryAcquire(Artifact("range", ArtifactEffectType.ProjectileRangePercent, 0.5f));
        build.TryAcquire(Artifact("carbon", ArtifactEffectType.BulletDamagePercent, 0.2f));
        Transform target = NewObject("Aim target").transform;
        target.position = gun.transform.position + Vector3.up * 4f;
        Assert.IsTrue(gun.TryAttack(target, 10f));
        Bullet launched = null;
        foreach (Bullet bullet in Object.FindObjectsOfType<Bullet>())
            if (bullet.GetOwner() == build.gameObject) { launched = bullet; break; }
        Assert.NotNull(launched);
        Assert.That(launched.GetDamage(), Is.EqualTo(12f).Within(0.001f));
        float expiresAt = (float)typeof(Bullet).GetField("_expiresAt", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(launched);
        Assert.That(expiresAt - Time.time, Is.EqualTo(3f).Within(0.001f));
        Assert.AreEqual(2f, gun.bulletLifeTime);
        Assert.AreEqual(10f, build.GetComponent<PlayerStats>().attackDamage);
        launched.Despawn();
        PlayerAutoTarget autoTarget = build.gameObject.AddComponent<PlayerAutoTarget>();
        autoTarget.gunRange = 8f; // Explicit range used by the multiplier boundary scenario.
        Collider2D distantHurt = Enemy(out MonsterStats distant);
        distantHurt.gameObject.layer = 7;
        distant.transform.position = build.transform.position + Vector3.up * 10f;
        Physics2D.SyncTransforms();
        Assert.AreSame(distant.transform, autoTarget.GetTarget(PlayerAttackController.WeaponMode.Gun), "Range artifacts also extend stationary gun target acquisition");
        build.EndRun();
        Assert.IsNull(autoTarget.GetTarget(PlayerAttackController.WeaponMode.Gun));
    }

    private RunBuild Actor()
    {
        GameObject obj = NewObject("Artifact player");
        obj.SetActive(false);
        obj.transform.position = new Vector3(500f + objects.Count * 4f, 500f, 0f);
        obj.AddComponent<Rigidbody2D>().gravityScale = 0f;
        obj.AddComponent<PlayerStats>().hitStunDuration = 0f;
        obj.AddComponent<PlayerMovement>().controlMode = PlayerMovement.ControlMode.SingleStickFlick;
        obj.AddComponent<PlayerDodgeController>().showFeedback = false;
        RunBuild build = obj.AddComponent<RunBuild>();
        obj.SetActive(true);
        return build;
    }

    private void FistDeduplicatesMonsterHurtboxesAndRejectsInactiveCallbacks()
    {
        RunBuild build = Actor();
        GameObject glove = NewObject("Dedup glove"); glove.SetActive(false);
        glove.transform.SetParent(build.transform);
        Collider2D hitboxCollider = glove.AddComponent<CircleCollider2D>(); hitboxCollider.isTrigger = true;
        FistHitbox hitbox = glove.AddComponent<FistHitbox>(); glove.SetActive(true);
        Collider2D first = Enemy(out MonsterStats monster);
        GameObject extra = NewObject("Additional enemy hurtbox"); extra.SetActive(false);
        extra.transform.SetParent(monster.transform);
        extra.AddComponent<MonsterHurtbox>();
        Collider2D second = extra.AddComponent<CircleCollider2D>(); extra.SetActive(true);
        MethodInfo hit = typeof(FistHitbox).GetMethod("OnTriggerEnter2D", BindingFlags.Instance | BindingFlags.NonPublic);
        hitbox.ActivateHitbox(10f);
        hit.Invoke(hitbox, new object[] { first }); hit.Invoke(hitbox, new object[] { second });
        Assert.AreEqual(90f, monster.currentHealth);
        hitbox.DeactivateHitbox(); hit.Invoke(hitbox, new object[] { second });
        Assert.AreEqual(90f, monster.currentHealth);
        hitbox.ActivateHitbox(20f); hit.Invoke(hitbox, new object[] { second });
        Assert.AreEqual(70f, monster.currentHealth);
        hitbox.DeactivateHitbox();
        Assert.IsFalse(hitboxCollider.enabled);
    }

    private void AttackFeedbackCannotSpawnAfterRunEnds()
    {
        RunBuild build = Actor(); build.BeginRun(0);
        build.TryAcquire(Artifact("reentrant-cap", ArtifactEffectType.DodgeNextAttackPercent, 0.6f));
        build.TryAcquire(Artifact("reentrant-critical", ArtifactEffectType.CriticalChance, 1f));
        build.NotifyDodgeStarted();
        PoolManager pool = GetPool();
        GameObject gunObject = NewObject("Reentrant gun"); gunObject.SetActive(false); gunObject.transform.SetParent(build.transform);
        Gun gun = gunObject.AddComponent<Gun>(); gun.weaponSpawnPoint = gunObject.transform;
        gun.bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bullet/Bullet.prefab");
        gunObject.SetActive(true);
        Transform target = NewObject("Reentrant aim").transform; target.position = gun.transform.position + Vector3.up;
        int before = pool.GetTotalActiveCount();
        build.OnEffectTriggered += message => { if (message == "회피 강화 공격!") build.EndRun(); };
        Assert.IsFalse(gun.TryAttack(target, 10f));
        Assert.AreEqual(before, pool.GetTotalActiveCount());
        Assert.IsFalse(build.IsRunActive);
        Assert.AreEqual("", build.LastFeedback);
    }

    private void BulletDamageObserverCannotDespawnTheNextLease()
    {
        RunBuild build = Actor();
        PoolManager pool = GetPool();
        GameObject template = NewObject("Reentrant lease template"); template.SetActive(false); template.AddComponent<Bullet>();
        pool.PreparePool(template, 1);
        Poolable leased = pool.GetFromPool(template, Vector3.zero, Quaternion.identity);
        Bullet bullet = leased.GetComponent<Bullet>(); bullet.Init(10f, build.gameObject);
        Collider2D target = Enemy(out MonsterStats monster);
        bool reused = false;
        monster.OnHealthChanged += (health, max) =>
        {
            if (reused) return;
            reused = true;
            bullet.Despawn();
            Poolable next = pool.GetFromPool(template, Vector3.zero, Quaternion.identity);
            Assert.AreSame(leased, next);
            next.GetComponent<Bullet>().Init(7f, build.gameObject);
        };
        Hit(bullet, target);
        Assert.IsTrue(leased.IsLeased, "The previous hit callback must leave the new lease alive");
        Assert.AreEqual(7f, bullet.GetDamage());
        Assert.AreEqual(90f, monster.currentHealth);
        Hit(bullet, target);
        Assert.AreEqual(83f, monster.currentHealth);
        Assert.IsFalse(leased.IsLeased);
    }

    private void ExtraHitRunEndKeepsFeedbackCleared()
    {
        RunBuild build = Actor(); build.BeginRun(0);
        build.TryAcquire(Artifact("ending-battery", ArtifactEffectType.ExtraHitChance, 1f, 0.5f));
        Bullet bullet = NewObject("Ending proc bullet").AddComponent<Bullet>();
        bullet.Init(10f, build.gameObject); bullet.SetRunAttack(build.CreateAttack(10f, true));
        Collider2D target = Enemy(out MonsterStats monster);
        monster.OnHealthChanged += (health, max) => { if (health <= 85f) build.EndRun(); };
        Hit(bullet, target);
        Assert.AreEqual(85f, monster.currentHealth);
        Assert.IsFalse(build.IsRunActive);
        Assert.AreEqual("", build.LastFeedback, "An ending extra hit cannot restore feedback after EndRun cleared it");
    }

    private void RevivingDeathObserverPreservesTheNewRunsCharge()
    {
        GameObject obj = NewObject("Death order player"); obj.SetActive(false);
        PlayerStats stats = obj.AddComponent<PlayerStats>(); stats.hitStunDuration = 0f;
        RunBuild build = null;
        ArtifactDefinition cap = Artifact("new-run-cap", ArtifactEffectType.DodgeNextAttackPercent, 0.6f);
        // Emulate a manager subscribed before the RunBuild component was added.
        stats.OnDied += () =>
        {
            build.EndRun(); build.BeginRun(200); stats.ResetForExpedition();
            build.TryAcquire(cap); build.NotifyDodgeStarted();
        };
        build = obj.AddComponent<RunBuild>(); obj.SetActive(true); build.BeginRun(100);
        stats.OnDamage(1000f, null);
        Assert.IsFalse(stats.IsDead);
        Assert.IsTrue(build.IsRunActive);
        Assert.IsTrue(build.HasDodgeCharge, "A stale death callback cannot clear the revived run's charge");
    }

    private ArtifactDefinition Artifact(string id, ArtifactEffectType type, float value, float secondary = 0f)
    {
        ArtifactDefinition artifact = Track(ScriptableObject.CreateInstance<ArtifactDefinition>());
        artifact.artifactId = id; artifact.displayName = id;
        artifact.effects.Add(new ArtifactEffectDefinition { effectType = type, value = value, secondaryValue = secondary });
        return artifact;
    }

    private Collider2D Enemy(out MonsterStats stats)
    {
        GameObject obj = NewObject("Artifact enemy");
        obj.SetActive(false);
        obj.transform.position = new Vector3(800f + objects.Count * 4f, 0f, 0f);
        stats = obj.AddComponent<MonsterStats>(); stats.maxHealth = 100f;
        GameObject hurt = NewObject("Enemy hurtbox"); hurt.transform.SetParent(obj.transform);
        hurt.transform.localPosition = Vector3.zero;
        hurt.AddComponent<MonsterHurtbox>();
        Collider2D collider = hurt.AddComponent<CircleCollider2D>(); collider.isTrigger = true;
        obj.SetActive(true);
        return collider;
    }

    private void Hit(Bullet bullet, Collider2D target)
    {
        typeof(Bullet).GetMethod("OnTriggerEnter2D", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(bullet, new object[] { target });
    }

    private PoolManager GetPool()
    {
        return PoolManager.Instance != null ? PoolManager.Instance : NewObject("Artifact bullet pool").AddComponent<PoolManager>();
    }

    private GameObject NewObject(string name) { return Track(new GameObject(name)); }
    private T Track<T>(T obj) where T : Object { objects.Add(obj); return obj; }
}
