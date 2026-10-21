using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class Sprint01ACombatTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private const string BulletPath = "Assets/Prefabs/Bullet/Bullet.prefab";

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yield return new EnterPlayMode();
        Assert.IsTrue(Application.isPlaying, "Integration checks must run inside Play Mode");
        Application.runInBackground = true;
        NewObject("Test EventSystem").AddComponent<EventSystem>();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        foreach (GameObject obj in objects) if (obj != null) Object.Destroy(obj);
        objects.Clear();
        yield return null;
        yield return new ExitPlayMode();
    }

    // EditMode runner supports null yields even while the Editor is in Play Mode.
    private IEnumerator Wait(float seconds)
    {
        float until = Time.time + seconds;
        while (Time.time < until) yield return null;
    }

    private IEnumerator NextPhysicsStep()
    {
        float before = Time.fixedTime;
        while (Time.fixedTime <= before) yield return null;
    }

    [UnityTest]
    public IEnumerator RunAllCombatAndInputScenarios()
    {
        // Count every completed scenario in one Play Mode session.
        int completed = 0;
        Debug.Log("SPRINT01A START AutoGunFiresAtRangeEdgeWithoutWaitingForVisualAim");
        yield return AutoGunFiresAtRangeEdgeWithoutWaitingForVisualAim();
        Debug.Log("SPRINT01A PASS AutoGunFiresAtRangeEdgeWithoutWaitingForVisualAim");
        completed++;
        yield return ClearScenario();
        Debug.Log("SPRINT01A START PoolExpandsWithoutDuplicateLeasesAndSurvives120Hits");
        yield return PoolExpandsWithoutDuplicateLeasesAndSurvives120Hits();
        Debug.Log("SPRINT01A PASS PoolExpandsWithoutDuplicateLeasesAndSurvives120Hits");
        completed++;
        yield return ClearScenario();
        Debug.Log("SPRINT01A START BulletLifetimeSurvivesWeaponDisableAndLeaseReuse");
        yield return BulletLifetimeSurvivesWeaponDisableAndLeaseReuse();
        Debug.Log("SPRINT01A PASS BulletLifetimeSurvivesWeaponDisableAndLeaseReuse");
        completed++;
        yield return ClearScenario();
        Debug.Log("SPRINT01A START LegacyChargeCancelQuickSwapDisableAndEventIsolation");
        yield return LegacyChargeCancelQuickSwapDisableAndEventIsolation();
        Debug.Log("SPRINT01A PASS LegacyChargeCancelQuickSwapDisableAndEventIsolation");
        completed++;
        yield return ClearScenario();
        Debug.Log("SPRINT01A START LegacyFistRequiresOneReleasePerCharge");
        yield return LegacyFistRequiresOneReleasePerCharge();
        Debug.Log("SPRINT01A PASS LegacyFistRequiresOneReleasePerCharge");
        completed++;
        yield return ClearScenario();
        Debug.Log("SPRINT01A START LegacyNeckReturnsSmoothlyAfterDirectionalPunch");
        yield return LegacyNeckReturnsSmoothlyAfterDirectionalPunch();
        Debug.Log("SPRINT01A PASS LegacyNeckReturnsSmoothlyAfterDirectionalPunch");
        completed++;
        yield return ClearScenario();
        Debug.Log("SPRINT01A START LegacyMovementCannotChangeFacingDuringPunch");
        yield return LegacyMovementCannotChangeFacingDuringPunch();
        Debug.Log("SPRINT01A PASS LegacyMovementCannotChangeFacingDuringPunch");
        completed++;
        yield return ClearScenario();
        Debug.Log("SPRINT01A START StoppedVisualCoroutineCannotKeepAttackLocked");
        yield return StoppedVisualCoroutineCannotKeepAttackLocked();
        Debug.Log("SPRINT01A PASS StoppedVisualCoroutineCannotKeepAttackLocked");
        completed++;
        yield return ClearScenario();
        Debug.Log("SPRINT01A START FlickRequiresCenterTouchOutwardDragAndNeverRelease");
        yield return FlickRequiresCenterTouchOutwardDragAndNeverRelease();
        Debug.Log("SPRINT01A PASS FlickRequiresCenterTouchOutwardDragAndNeverRelease");
        completed++;
        yield return ClearScenario();
        Debug.Log("SPRINT01A START MultitouchCannotStealOrReleaseOwnedJoystick");
        yield return MultitouchCannotStealOrReleaseOwnedJoystick();
        Debug.Log("SPRINT01A PASS MultitouchCannotStealOrReleaseOwnedJoystick");
        completed++;
        yield return ClearScenario();
        Debug.Log("SPRINT01A START DodgeTravelsConfiguredDistanceAndStopsAtSolidWall");
        yield return DodgeTravelsConfiguredDistanceAndStopsAtSolidWall();
        Debug.Log("SPRINT01A PASS DodgeTravelsConfiguredDistanceAndStopsAtSolidWall");
        completed++;
        yield return ClearScenario();
        Debug.Log("SPRINT01A START DodgeDamageWindowsStunDeathAndFocusHavePriority");
        yield return DodgeDamageWindowsStunDeathAndFocusHavePriority();
        Debug.Log("SPRINT01A PASS DodgeDamageWindowsStunDeathAndFocusHavePriority");
        completed++;
        yield return ClearScenario();
        Debug.Log("SPRINT01A START DodgeResumesHeldInputAfterCooldown");
        yield return DodgeResumesHeldInputAfterCooldown();
        Debug.Log("SPRINT01A PASS DodgeResumesHeldInputAfterCooldown");
        completed++;
        yield return ClearScenario();
        Debug.Log("SPRINT01A START AutoGunWaitsForNeutralDelaySelectsNearestAndStopsOnMovement");
        yield return AutoGunWaitsForNeutralDelaySelectsNearestAndStopsOnMovement();
        Debug.Log("SPRINT01A PASS AutoGunWaitsForNeutralDelaySelectsNearestAndStopsOnMovement");
        completed++;
        yield return ClearScenario();
        Debug.Log("SPRINT01A START AutomaticAimTurnsSmoothlyAndFistWaitsForAlignment");
        yield return AutomaticAimTurnsSmoothlyAndFistWaitsForAlignment();
        Debug.Log("SPRINT01A PASS AutomaticAimTurnsSmoothlyAndFistWaitsForAlignment");
        completed++;
        yield return ClearScenario();
        Debug.Log("SPRINT01A START TrackingPersistsWhileMovingAndPresentationMatchesTarget");
        yield return TrackingPersistsWhileMovingAndPresentationMatchesTarget();
        Debug.Log("SPRINT01A PASS TrackingPersistsWhileMovingAndPresentationMatchesTarget");
        completed++;
        yield return ClearScenario();
        Debug.Log("SPRINT01A START AutomaticFistFinishesRetractionAfterTargetLoss");
        yield return AutomaticFistFinishesRetractionAfterTargetLoss();
        Debug.Log("SPRINT01A PASS AutomaticFistFinishesRetractionAfterTargetLoss");
        completed++;
        yield return ClearScenario();
        Debug.Log("SPRINT01A START AutomaticFistFinishesRetractionAtLowFrameRate");
        yield return AutomaticFistFinishesRetractionAtLowFrameRate();
        Debug.Log("SPRINT01A PASS AutomaticFistFinishesRetractionAtLowFrameRate");
        completed++;
        yield return ClearScenario();
        Debug.Log("SPRINT01A START AutomaticFistReusesDamageFormulaAndCancelsOnMovementOrDodge");
        yield return AutomaticFistReusesDamageFormulaAndCancelsOnMovementOrDodge();
        Debug.Log("SPRINT01A PASS AutomaticFistReusesDamageFormulaAndCancelsOnMovementOrDodge");
        completed++;
        yield return ClearScenario();
        Debug.Log("SPRINT01A START ModeSwitchRestoresLegacyUIAndSupportsProportionalMovement");
        yield return ModeSwitchRestoresLegacyUIAndSupportsProportionalMovement();
        Debug.Log("SPRINT01A PASS ModeSwitchRestoresLegacyUIAndSupportsProportionalMovement");
        completed++;
        yield return ClearScenario();
        Debug.Log("SPRINT01A START SampleSceneKeepsExistingReferencesAndCanRestoreDualStick");
        yield return SampleSceneKeepsExistingReferencesAndCanRestoreDualStick();
        Debug.Log("SPRINT01A PASS SampleSceneKeepsExistingReferencesAndCanRestoreDualStick");
        completed++;
        yield return ClearScenario();
        Assert.AreEqual(21, completed, "Every scenario must reach its final assertion");
        Debug.Log("SPRINT01A COMPLETE " + completed);
    }

    private IEnumerator ClearScenario()
    {
        foreach (GameObject obj in objects) if (obj != null) Object.Destroy(obj);
        objects.Clear();
        yield return null;
        NewObject("Test EventSystem").AddComponent<EventSystem>();
    }

    private GameObject NewObject(string name)
    {
        var obj = new GameObject(name);
        objects.Add(obj);
        return obj;
    }

    private PoolManager Pool(int size = 1)
    {
        var pool = NewObject("Test Pool").AddComponent<PoolManager>();
        pool.PreparePool(AssetDatabase.LoadAssetAtPath<GameObject>(BulletPath), size);
        return pool;
    }

    private FixedJoystick Stick(string name)
    {
        var canvasObj = NewObject(name + " Canvas");
        canvasObj.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(canvasObj.transform, false);
        var rect = (RectTransform)obj.transform;
        rect.sizeDelta = new Vector2(200f, 200f);
        var handle = new GameObject("Handle", typeof(RectTransform));
        handle.transform.SetParent(rect, false);
        var stick = obj.AddComponent<FixedJoystick>();
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        typeof(Joystick).GetField("background", flags).SetValue(stick, rect);
        typeof(Joystick).GetField("handle", flags).SetValue(stick, (RectTransform)handle.transform);
        return stick;
    }

    private PointerEventData Pointer(FixedJoystick stick, Vector2 input, int id = 1)
    {
        var rect = (RectTransform)stick.transform;
        Vector2 center = RectTransformUtility.WorldToScreenPoint(null, rect.position);
        float scale = stick.GetComponentInParent<Canvas>().scaleFactor;
        return new PointerEventData(EventSystem.current) { pointerId = id, position = center + input * rect.sizeDelta.x * 0.5f * scale };
    }

    private void Down(FixedJoystick stick, Vector2 input, int id = 1) => stick.OnPointerDown(Pointer(stick, input, id));
    private void Drag(FixedJoystick stick, Vector2 input, int id = 1) => stick.OnDrag(Pointer(stick, input, id));
    private void Up(FixedJoystick stick, int id = 1) => stick.OnPointerUp(Pointer(stick, Vector2.zero, id));

    private GameObject Player(PlayerMovement.ControlMode mode = PlayerMovement.ControlMode.SingleStickFlick, bool withAnimator = false)
    {
        GameObject obj = NewObject("Test Player");
        obj.SetActive(false);
        var rb = obj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        obj.AddComponent<SpriteRenderer>();
        if (withAnimator)
        {
            var animator = obj.AddComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Resources/Sprites/Bottle/Animation/BottleController.controller");
        }
        obj.AddComponent<PlayerStats>();
        var movement = obj.AddComponent<PlayerMovement>();
        movement.controlMode = mode;
        movement.joystick = Stick("Move");
        var aim = obj.AddComponent<PlayerAimController>();
        aim.joystick = Stick("Attack");
        var neck = new GameObject("Neck").transform;
        neck.SetParent(obj.transform, false);
        aim.neckTransform = neck;
        var glove = new GameObject("Glove");
        glove.transform.SetParent(neck, false);
        glove.layer = 8;
        glove.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        glove.AddComponent<CircleCollider2D>().isTrigger = true;
        var hitbox = glove.AddComponent<FistHitbox>();
        var foot = new GameObject("Foot");
        foot.transform.SetParent(obj.transform, false);
        foot.layer = 10;
        foot.AddComponent<BoxCollider2D>().size = new Vector2(0.4f, 0.2f);
        var gunObj = new GameObject("Gun");
        gunObj.transform.SetParent(obj.transform, false);
        var gun = gunObj.AddComponent<Gun>();
        gun.bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BulletPath);
        var spawn = new GameObject("Muzzle").transform;
        spawn.SetParent(neck, false);
        spawn.localPosition = new Vector3(0f, 0.5f, 0f);
        gun.weaponSpawnPoint = spawn;
        gun.bulletLifeTime = 0.15f;
        var effects = obj.AddComponent<NeckEffectController>();
        effects.neckTransform = neck;
        effects.gloveTransform = glove.transform;
        effects.gloveHitbox = hitbox;
        var attack = obj.AddComponent<PlayerAttackController>();
        attack.neckTransform = neck;
        attack.maxChargeTime = 0.8f;
        var flick = obj.AddComponent<JoystickFlickDetector>();
        flick.joystick = movement.joystick;
        obj.AddComponent<PlayerDodgeController>().flickDetector = flick;
        obj.AddComponent<PlayerAutoTarget>().gunRange = 8f; // Explicit long-range combat fixture.
        obj.SetActive(true);
        return obj;
    }

    private MonsterStats Monster(Vector2 position, float health = 10000f)
    {
        GameObject obj = NewObject("Test Monster");
        obj.SetActive(false);
        obj.transform.position = position;
        var stats = obj.AddComponent<MonsterStats>();
        stats.maxHealth = health;
        var hurt = new GameObject("Hurtbox");
        hurt.transform.SetParent(obj.transform, false);
        hurt.layer = 7;
        hurt.AddComponent<CircleCollider2D>().isTrigger = true;
        hurt.AddComponent<MonsterHurtbox>();
        obj.SetActive(true);
        return stats;
    }

    private IEnumerator PoolExpandsWithoutDuplicateLeasesAndSurvives120Hits()
    {
        PoolManager pool = Pool(0);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BulletPath);
        Assert.NotNull(prefab.GetComponent<Poolable>());
        var leases = new HashSet<Poolable>();
        for (int i = 0; i < 4; i++) Assert.IsTrue(leases.Add(pool.GetFromPool(prefab, Vector3.zero, Quaternion.identity)));
        Assert.AreEqual(4, pool.GetTotalActiveCount());
        foreach (Poolable item in leases) { item.ReturnToPool(); item.ReturnToPool(); }
        Assert.AreEqual(0, pool.GetTotalActiveCount());
        MonsterStats target = Monster(Vector2.up * 30f);
        Collider2D col = target.GetComponentInChildren<Collider2D>();
        GameObject owner = NewObject("Shooter");
        owner.AddComponent<PlayerStats>();
        var gunObject = new GameObject("Gun");
        gunObject.transform.SetParent(owner.transform, false);
        Gun gun = gunObject.AddComponent<Gun>();
        gun.bulletPrefab = prefab;
        gun.weaponSpawnPoint = owner.transform;
        var trigger = typeof(Bullet).GetMethod("OnTriggerEnter2D", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        for (int i = 0; i < 120; i++)
        {
            Assert.IsTrue(gun.TryAttack(target.transform, 10f));
            Bullet bullet = pool.GetComponentInChildren<Bullet>();
            Assert.NotNull(bullet);
            Assert.That(bullet.GetComponent<Rigidbody2D>().velocity.magnitude, Is.EqualTo(10f).Within(0.01f));
            Assert.AreSame(owner, bullet.GetOwner());
            trigger.Invoke(bullet, new object[] { col });
            trigger.Invoke(bullet, new object[] { col });
            Assert.AreEqual(0, pool.GetTotalActiveCount());
        }
        Assert.AreEqual(8800f, target.currentHealth);
        yield return null;
    }

    private IEnumerator BulletLifetimeSurvivesWeaponDisableAndLeaseReuse()
    {
        PoolManager pool = Pool();
        GameObject obj = Player();
        yield return null;
        Gun gun = obj.GetComponentInChildren<Gun>();
        Transform target = NewObject("Aim").transform;
        target.position = Vector3.up * 10f;
        Assert.IsTrue(gun.TryAttack(target, 10f));
        obj.GetComponent<PlayerAttackController>().SwapWeapon(PlayerAttackController.WeaponMode.Fist);
        yield return Wait(0.2f);
        Assert.AreEqual(0, pool.GetTotalActiveCount());
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BulletPath);
        Poolable item = pool.GetFromPool(prefab, Vector3.zero, Quaternion.identity);
        item.GetComponent<Bullet>().Init(10f, obj, 0.05f);
        item.ReturnToPool();
        Poolable reused = pool.GetFromPool(prefab, Vector3.zero, Quaternion.identity);
        reused.GetComponent<Bullet>().Init(10f, obj, 0.25f);
        yield return Wait(0.1f);
        Assert.IsTrue(reused.IsLeased);
        yield return Wait(0.2f);
        Assert.AreEqual(0, pool.GetTotalActiveCount());
    }

    private IEnumerator LegacyChargeCancelQuickSwapDisableAndEventIsolation()
    {
        Pool();
        GameObject first = Player(PlayerMovement.ControlMode.LegacyDualStick);
        GameObject second = Player(PlayerMovement.ControlMode.LegacyDualStick);
        second.transform.position = Vector3.right * 20f;
        second.GetComponent<PlayerMovement>().acceptsLocalInput = false;
        yield return null;
        PlayerStats stats = first.GetComponent<PlayerStats>();
        NeckEffectController effects = first.GetComponent<NeckEffectController>();
        NeckEffectController other = second.GetComponent<NeckEffectController>();
        PlayerAttackController attack = first.GetComponent<PlayerAttackController>();
        PlayerEvents.RaiseGunFired(stats);
        Assert.IsTrue(effects.isStretching);
        Assert.IsFalse(other.isStretching);
        attack.SwapWeapon(PlayerAttackController.WeaponMode.Fist);
        FixedJoystick stick = first.GetComponent<PlayerAimController>().joystick;
        Down(stick, Vector2.up);
        yield return Wait(0.05f);
        Assert.IsTrue(attack.IsCharging);
        stick.CancelInput();
        yield return null;
        Assert.IsFalse(attack.IsCharging);
        Assert.IsFalse(attack.isAttacking);
        Down(stick, Vector2.up);
        yield return Wait(0.05f);
        Up(stick);
        yield return null;
        Assert.IsTrue(attack.isAttacking);
        Assert.IsFalse(second.GetComponent<PlayerAttackController>().isAttacking);
        Assert.IsTrue(effects.gloveHitbox.GetComponent<Collider2D>().enabled);
        first.SetActive(false);
        Assert.IsFalse(attack.isAttacking);
        Assert.IsFalse(effects.gloveHitbox.GetComponent<Collider2D>().enabled);
        Assert.AreEqual(Vector3.one, effects.neckTransform.localScale);
        first.SetActive(true);
        yield return null;
        Assert.IsFalse(attack.isAttacking);
    }

    private IEnumerator StoppedVisualCoroutineCannotKeepAttackLocked()
    {
        GameObject obj = Player(PlayerMovement.ControlMode.LegacyDualStick);
        yield return null;
        var attack = obj.GetComponent<PlayerAttackController>();
        attack.SwapWeapon(PlayerAttackController.WeaponMode.Fist);
        FixedJoystick stick = obj.GetComponent<PlayerAimController>().joystick;
        Down(stick, Vector2.up);
        yield return Wait(0.03f);
        Up(stick);
        yield return null;
        obj.GetComponent<NeckEffectController>().StopAllCoroutines();
        yield return Wait(0.95f);
        Assert.IsFalse(attack.isAttacking);
        Assert.IsFalse(obj.GetComponent<NeckEffectController>().gloveHitbox.GetComponent<Collider2D>().enabled);
    }

    private IEnumerator LegacyMovementCannotChangeFacingDuringPunch()
    {
        GameObject obj = Player(PlayerMovement.ControlMode.LegacyDualStick, true);
        PlayerMovement movement = obj.GetComponent<PlayerMovement>();
        PlayerAttackController attack = obj.GetComponent<PlayerAttackController>();
        PlayerAimController aim = obj.GetComponent<PlayerAimController>();
        Animator animator = obj.GetComponent<Animator>();
        movement.tiltAngle = 0f;
        attack.SwapWeapon(PlayerAttackController.WeaponMode.Fist);
        yield return Wait(0.03f);
        Vector2[] directions = { Vector2.right, Vector2.left, Vector2.down, Vector2.up,
            new Vector2(1f, 1f), new Vector2(-1f, 1f), new Vector2(1f, -1f), new Vector2(-1f, -1f) };
        foreach (Vector2 direction in directions)
        {
            Vector2 initial = direction.normalized;
            Vector2 later = new Vector2(-initial.y, initial.x);
            Down(movement.joystick, initial * 0.7f);
            yield return Wait(0.05f);
            Up(movement.joystick);
            yield return Wait(0.03f);
            Down(aim.joystick, later);
            yield return Wait(0.06f);
            Up(aim.joystick);
            yield return Wait(0.04f);
            Assert.IsTrue(attack.isAttacking);
            Vector2 lockedFacing = new Vector2(animator.GetFloat("InputX"), animator.GetFloat("InputY"));
            Assert.That(Vector2.Distance(lockedFacing, initial), Is.LessThan(0.01f));
            Quaternion lockedAim = attack.LockedRotation;
            Down(movement.joystick, -initial * 0.7f);
            yield return Wait(0.05f);
            Assert.IsTrue(attack.isAttacking);
            Assert.That(Vector2.Distance(movement.LastMoveDirection, lockedFacing), Is.LessThan(0.01f),
                "Movement input must not update the facing direction while punching");
            Drag(movement.joystick, later * 0.7f);
            float limit = Time.time + 1.5f;
            while (attack.isAttacking && Time.time < limit)
            {
                Vector2 current = new Vector2(animator.GetFloat("InputX"), animator.GetFloat("InputY"));
                Assert.That(Vector2.Distance(current, lockedFacing), Is.LessThan(0.01f),
                    "Attack Blend Tree direction must stay at the launch-facing direction");
                Assert.That(Quaternion.Angle(aim.neckTransform.rotation, lockedAim), Is.LessThan(0.1f));
                Assert.AreEqual(Vector2.zero, obj.GetComponent<Rigidbody2D>().velocity);
                Assert.IsFalse(animator.GetBool("isRunning"));
                yield return null;
            }
            Assert.IsFalse(attack.isAttacking);
            yield return Wait(0.08f);
            Vector2 resumed = new Vector2(animator.GetFloat("InputX"), animator.GetFloat("InputY"));
            Assert.That(Vector2.Distance(resumed, later), Is.LessThan(0.01f), "Apply held input after the punch finishes");
            Assert.That(Vector2.Distance(obj.GetComponent<Rigidbody2D>().velocity, later * 3f), Is.LessThan(0.01f));
            Up(movement.joystick);
            yield return Wait(0.03f);
        }
    }

    private IEnumerator LegacyNeckReturnsSmoothlyAfterDirectionalPunch()
    {
        GameObject obj = Player(PlayerMovement.ControlMode.LegacyDualStick, true);
        PlayerAttackController attack = obj.GetComponent<PlayerAttackController>();
        PlayerAimController aim = obj.GetComponent<PlayerAimController>();
        attack.SwapWeapon(PlayerAttackController.WeaponMode.Fist);
        bool ended = false;
        Quaternion rotationAtEnd = Quaternion.identity;
        System.Action<PlayerStats> onEnd = player =>
        {
            if (player != obj.GetComponent<PlayerStats>()) return;
            rotationAtEnd = aim.neckTransform.rotation;
            ended = true;
        };
        PlayerEvents.OnPlayerAttackEnded += onEnd;
        try
        {
            yield return Wait(0.03f);
            Vector2[] directions = { Vector2.right, Vector2.left, Vector2.down, Vector2.up,
                new Vector2(1f, 1f), new Vector2(-1f, 1f), new Vector2(1f, -1f), new Vector2(-1f, -1f) };
            foreach (Vector2 direction in directions)
            {
                ended = false;
                Quaternion expected = Quaternion.AngleAxis(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f, Vector3.forward);
                Down(aim.joystick, direction.normalized);
                yield return Wait(0.08f);
                Up(aim.joystick);
                yield return Wait(0.06f);
                Assert.IsTrue(attack.isAttacking);
                Assert.That(Quaternion.Angle(aim.neckTransform.rotation, expected), Is.LessThan(0.1f));
                float limit = Time.time + 1.5f;
                while (!ended && Time.time < limit) yield return null;
                Assert.IsTrue(ended);
                Assert.That(Quaternion.Angle(rotationAtEnd, expected), Is.LessThan(0.1f),
                    "Ending the punch must preserve its rotation for PlayerAimController to return smoothly");
                float fullAngle = Quaternion.Angle(expected, Quaternion.identity);
                yield return Wait(0.03f);
                float earlyAngle = Quaternion.Angle(aim.neckTransform.rotation, Quaternion.identity);
                if (fullAngle > 1f)
                {
                    Assert.Greater(earlyAngle, 0.1f, "The neck must not snap straight to rest");
                    Assert.Less(earlyAngle, fullAngle, "The existing aim return must make progress");
                }
                yield return Wait(0.7f);
                Assert.That(Quaternion.Angle(aim.neckTransform.rotation, Quaternion.identity), Is.LessThan(0.2f));
            }

            Down(aim.joystick, Vector2.right);
            yield return Wait(0.15f);
            Quaternion beforeCancel = aim.neckTransform.rotation;
            aim.joystick.CancelInput();
            Assert.That(Quaternion.Angle(beforeCancel, aim.neckTransform.rotation), Is.LessThan(0.1f),
                "Cancelling stretch/charge must not overwrite aim rotation either");
            yield return Wait(0.7f);
            Assert.That(Quaternion.Angle(aim.neckTransform.rotation, Quaternion.identity), Is.LessThan(0.2f));
        }
        finally { PlayerEvents.OnPlayerAttackEnded -= onEnd; }
    }

    private IEnumerator LegacyFistRequiresOneReleasePerCharge()
    {
        GameObject obj = Player(PlayerMovement.ControlMode.LegacyDualStick);
        PlayerAttackController attack = obj.GetComponent<PlayerAttackController>();
        attack.SwapWeapon(PlayerAttackController.WeaponMode.Fist);
        Monster(Vector2.up);
        int punches = 0;
        System.Action<PlayerStats, float> released = (player, ratio) =>
        {
            if (player == obj.GetComponent<PlayerStats>()) punches++;
        };
        PlayerEvents.OnFistAttackReleased += released;
        try
        {
            FixedJoystick stick = obj.GetComponent<PlayerAimController>().joystick;
            yield return Wait(0.03f);
            yield return Wait(0.9f);
            Assert.AreEqual(0, punches, "Idle must not punch without charge input");
            Down(stick, Vector2.up);
            yield return Wait(0.95f);
            Assert.IsTrue(attack.IsCharging);
            Assert.AreEqual(0, punches, "Holding beyond maximum charge must not release");
            Up(stick);
            yield return Wait(1.6f);
            Assert.AreEqual(1, punches, "One release must never restart another punch");
            Down(stick, Vector2.up);
            yield return Wait(0.1f);
            stick.CancelInput();
            yield return Wait(0.9f);
            Assert.AreEqual(1, punches, "Cancellation must not behave as release");
        }
        finally { PlayerEvents.OnFistAttackReleased -= released; }
    }

    private IEnumerator FlickRequiresCenterTouchOutwardDragAndNeverRelease()
    {
        GameObject obj = Player();
        FixedJoystick stick = obj.GetComponent<PlayerMovement>().joystick;
        var detector = obj.GetComponent<JoystickFlickDetector>();
        int flicks = 0;
        detector.FlickDetected += direction => flicks++;
        yield return null;
        Down(stick, Vector2.zero);
        yield return Wait(0.15f);
        Drag(stick, Vector2.right);
        Up(stick);
        Assert.AreEqual(0, flicks);
        Down(stick, Vector2.right);
        yield return null;
        Drag(stick, Vector2.left);
        Up(stick);
        Assert.AreEqual(0, flicks);
        Down(stick, Vector2.zero);
        yield return null;
        Drag(stick, Vector2.right);
        Assert.AreEqual(1, flicks);
        Assert.IsTrue(obj.GetComponent<PlayerDodgeController>().IsDodging);
        Drag(stick, Vector2.zero);
        Drag(stick, Vector2.left);
        Up(stick);
        Assert.AreEqual(1, flicks);
        Down(stick, Vector2.zero);
        stick.CancelInput();
        Drag(stick, Vector2.up);
        Assert.AreEqual(1, flicks);
    }

    private IEnumerator MultitouchCannotStealOrReleaseOwnedJoystick()
    {
        GameObject obj = Player();
        FixedJoystick stick = obj.GetComponent<PlayerMovement>().joystick;
        yield return null;
        Down(stick, Vector2.zero, 1);
        Down(stick, Vector2.right, 2);
        Drag(stick, Vector2.right, 2);
        Up(stick, 2);
        Assert.IsTrue(stick.IsPointerDown);
        Assert.AreEqual(Vector2.zero, stick.Direction);
        yield return Wait(0.14f);
        Drag(stick, Vector2.up * 0.5f, 1);
        Assert.That(stick.Vertical, Is.EqualTo(0.5f).Within(0.01f));
        Up(stick, 1);
        Assert.IsFalse(stick.IsPointerDown);
        Assert.AreEqual(Vector2.zero, stick.Direction);
    }

    private IEnumerator DodgeTravelsConfiguredDistanceAndStopsAtSolidWall()
    {
        GameObject obj = Player();
        obj.GetComponent<PlayerMovement>().tiltAngle = 0f;
        PlayerDodgeController dodge = obj.GetComponent<PlayerDodgeController>();
        yield return null;
        Assert.IsTrue(dodge.TryDodge(Vector2.right));
        Assert.IsFalse(dodge.TryDodge(Vector2.left));
        yield return Wait(0.36f);
        Assert.IsFalse(dodge.IsDodging);
        Assert.That(obj.transform.position.x, Is.EqualTo(2.5f).Within(0.1f));
        yield return Wait(0.2f);
        obj.GetComponent<Rigidbody2D>().position = Vector2.zero;
        GameObject wall = NewObject("Wall");
        wall.transform.position = Vector3.right;
        wall.AddComponent<BoxCollider2D>().size = new Vector2(0.05f, 4f);
        Physics2D.SyncTransforms();
        Assert.IsTrue(dodge.TryDodge(Vector2.right));
        yield return Wait(0.36f);
        Assert.Less(obj.transform.position.x, 0.8f);
        Assert.IsTrue(obj.GetComponentInChildren<BoxCollider2D>().enabled);
    }

    private IEnumerator DodgeDamageWindowsStunDeathAndFocusHavePriority()
    {
        GameObject obj = Player();
        PlayerStats stats = obj.GetComponent<PlayerStats>();
        PlayerDodgeController dodge = obj.GetComponent<PlayerDodgeController>();
        yield return null;
        Assert.IsTrue(dodge.TryDodge(Vector2.right));
        stats.OnDamage(10f, null);
        Assert.AreEqual(90f, stats.currentHealth);
        Assert.IsFalse(dodge.IsDodging);
        Assert.IsTrue(stats.IsStunned);
        obj.GetComponent<PlayerAttackController>().CompleteAttack();
        Assert.IsFalse(dodge.TryDodge(Vector2.right));
        yield return Wait(0.55f);
        Assert.IsTrue(dodge.TryDodge(Vector2.right));
        yield return Wait(0.1f);
        Assert.IsTrue(stats.IsInvulnerable);
        stats.OnDamage(10f, null);
        Assert.AreEqual(90f, stats.currentHealth);
        yield return Wait(0.18f);
        Assert.IsFalse(stats.IsInvulnerable);
        stats.OnDamage(10f, null);
        Assert.AreEqual(80f, stats.currentHealth);
        obj.SendMessage("OnApplicationFocus", false);
        Assert.IsFalse(dodge.IsDodging);
        Assert.IsFalse(obj.GetComponent<PlayerMovement>().CanReadLocalInput);
        obj.SendMessage("OnApplicationFocus", true);
        yield return Wait(0.55f);
        Assert.IsTrue(dodge.TryDodge(Vector2.up));
        stats.OnDamage(100f, null);
        Assert.IsTrue(stats.IsDead);
        Assert.IsFalse(dodge.IsDodging);
        Assert.IsFalse(dodge.TryDodge(Vector2.up));
        yield return NextPhysicsStep();
        Assert.AreEqual(Vector2.zero, obj.GetComponent<Rigidbody2D>().velocity);
    }

    private IEnumerator AutoGunWaitsForNeutralDelaySelectsNearestAndStopsOnMovement()
    {
        Pool();
        GameObject obj = Player();
        int shots = 0;
        var fireTimes = new List<float>();
        System.Action<PlayerStats> fired = player =>
        {
            if (player != obj.GetComponent<PlayerStats>()) return;
            shots++;
            fireTimes.Add(Time.time);
        };
        PlayerEvents.OnGunFired += fired;
        try
        {
            Monster(Vector2.up * 12f); // Alive, but outside the gun's configured range.
            yield return Wait(0.2f);
            Assert.AreEqual(0, shots);
            MonsterStats far = Monster(Vector2.up * 6f);
            MonsterStats near = Monster(Vector2.up * 3f);
            MonsterStats dead = Monster(Vector2.up);
            dead.currentHealth = 0f;
            MonsterStats inactive = Monster(Vector2.up * 0.5f);
            inactive.gameObject.SetActive(false);
            yield return Wait(0.15f);
            Assert.Greater(shots, 0);
            Assert.AreSame(near.transform, obj.GetComponent<PlayerAutoTarget>().CurrentTarget);
            FixedJoystick stick = obj.GetComponent<PlayerMovement>().joystick;
            Down(stick, Vector2.up * 0.5f);
            yield return null;
            int before = shots;
            yield return Wait(0.25f);
            Assert.AreEqual(before, shots);
            Up(stick);
            yield return Wait(0.06f);
            Assert.AreEqual(before, shots);
            yield return Wait(0.15f);
            Assert.Greater(shots, before);
            near.gameObject.SetActive(false);
            yield return Wait(0.15f);
            Assert.AreSame(far.transform, obj.GetComponent<PlayerAutoTarget>().CurrentTarget);
            for (int i = 1; i < fireTimes.Count; i++) Assert.GreaterOrEqual(fireTimes[i] - fireTimes[i - 1], 0.099f);
        }
        finally { PlayerEvents.OnGunFired -= fired; }
    }

    private IEnumerator DodgeResumesHeldInputAfterCooldown()
    {
        GameObject obj = Player();
        PlayerMovement movement = obj.GetComponent<PlayerMovement>();
        PlayerDodgeController dodge = obj.GetComponent<PlayerDodgeController>();
        FixedJoystick stick = movement.joystick;
        yield return null;
        Down(stick, Vector2.zero);
        yield return Wait(0.02f);
        Drag(stick, Vector2.right);
        Assert.IsTrue(dodge.IsDodging);
        Drag(stick, Vector2.up * 0.5f);
        yield return Wait(0.08f);
        Assert.Greater(movement.AppliedVelocity.x, 8f);
        Assert.AreEqual(0f, movement.AppliedVelocity.y);
        yield return Wait(0.26f);
        Assert.IsFalse(dodge.IsDodging);
        Assert.That(movement.AppliedVelocity.y, Is.EqualTo(3f).Within(0.02f));
        Assert.IsFalse(dodge.TryDodge(Vector2.left));
        yield return Wait(0.2f);
        Assert.IsTrue(dodge.TryDodge(Vector2.left));
        Up(stick);
        yield return Wait(0.36f);
        Assert.IsFalse(dodge.IsDodging);
        Assert.AreEqual(Vector2.zero, movement.AppliedVelocity);
    }

    private IEnumerator AutomaticFistReusesDamageFormulaAndCancelsOnMovementOrDodge()
    {
        GameObject obj = Player();
        Monster(Vector2.up);
        PlayerAttackController attack = obj.GetComponent<PlayerAttackController>();
        attack.SwapWeapon(PlayerAttackController.WeaponMode.Fist);
        yield return Wait(0.18f);
        Assert.IsTrue(attack.isAttacking);
        FistHitbox fist = obj.GetComponent<NeckEffectController>().gloveHitbox;
        Assert.AreEqual(10f, fist.GetDamage());
        FixedJoystick stick = obj.GetComponent<PlayerMovement>().joystick;
        Down(stick, Vector2.right * 0.5f);
        yield return null;
        Assert.IsFalse(attack.isAttacking);
        Assert.IsFalse(fist.GetComponent<Collider2D>().enabled);
        Up(stick);
        attack.autoFistChargeTime = 0.2f;
        yield return Wait(0.18f);
        Assert.IsTrue(attack.IsCharging);
        Assert.IsTrue(obj.GetComponent<PlayerDodgeController>().TryDodge(Vector2.right));
        Assert.IsFalse(attack.IsCharging);
        Assert.IsFalse(attack.isAttacking);
        yield return Wait(0.4f);
        Assert.IsFalse(attack.isAttacking); // Dodge moved us outside fist range.
        obj.GetComponent<Rigidbody2D>().position = Vector2.zero;
        Physics2D.SyncTransforms();
        yield return Wait(0.4f);
        Assert.IsTrue(attack.isAttacking);
        Assert.That(fist.GetDamage(), Is.EqualTo(12.5f).Within(0.5f));
        attack.CancelAttack();
        obj.GetComponent<PlayerMovement>().controlMode = PlayerMovement.ControlMode.LegacyDualStick;
        yield return null;
        FixedJoystick aim = obj.GetComponent<PlayerAimController>().joystick;
        Down(aim, Vector2.up);
        yield return Wait(0.9f);
        Up(aim);
        yield return null;
        Assert.IsTrue(attack.isAttacking);
        Assert.AreEqual(20f, fist.GetDamage());
    }

    private IEnumerator AutoGunFiresAtRangeEdgeWithoutWaitingForVisualAim()
    {
        float originalCaptureDeltaTime = Time.captureDeltaTime;
        Time.captureDeltaTime = 1f / 60f;
        PlayerStats stats = null;
        PlayerAutoTarget targeting = null;
        Gun gun = null;
        PoolManager pool = null;
        int shots = 0;
        bool firedWhileTracking = false;
        System.Action<PlayerStats> onFire = player =>
        {
            if (player != stats) return;
            shots++;
            firedWhileTracking |= !player.GetComponent<PlayerAimController>().IsAutoAimAligned;
            Vector2 expected = (targeting.CurrentTarget.position - gun.weaponSpawnPoint.position).normalized;
            bool found = false;
            foreach (Bullet bullet in pool.GetComponentsInChildren<Bullet>())
            {
                if (bullet.GetOwner() != player.gameObject || Vector2.Distance(bullet.transform.position, gun.weaponSpawnPoint.position) > 0.001f) continue;
                Assert.Less(Vector2.Angle(expected, bullet.GetComponent<Rigidbody2D>().velocity), 0.1f,
                    "Automatic shots must head toward the selected enemy, not the lagging neck rotation");
                found = true;
            }
            Assert.IsTrue(found, "The gun event must correspond to a real leased projectile");
        };
        PlayerEvents.OnGunFired += onFire;
        try
        {
            pool = Pool();
            GameObject obj = Player();
            stats = obj.GetComponent<PlayerStats>();
            targeting = obj.GetComponent<PlayerAutoTarget>();
            targeting.gunRange = 2f;
            obj.GetComponent<PlayerAimController>().rotationSpeed = 10f;
            gun = obj.GetComponentInChildren<Gun>();
            gun.bulletLifeTime = 1f;
            MonsterStats enemy = Monster(Vector2.right * 1.9f, 500f);
            float started = Time.time;
            while (Time.time < started + 0.6f)
            {
                float angle = (Time.time - started) * Mathf.Deg2Rad * 60f;
                enemy.transform.position = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 1.9f;
                Physics2D.SyncTransforms();
                yield return null;
            }
            Assert.AreSame(enemy.transform, targeting.CurrentTarget);
            Assert.That(targeting.TargetDistance, Is.EqualTo(1.9f).Within(0.001f), "The enemy never moves closer to enable firing");
            Assert.Greater(shots, 1, "A stationary player must repeatedly fire at a selected in-range moving enemy");
            Assert.IsTrue(firedWhileTracking, "Visual smoothing must not act as an extra firing range");
            Assert.Less(enemy.currentHealth, 500f, "Actual projectile physics must hit the enemy");
            int before = shots;
            enemy.transform.position = Vector3.right * 2.1f;
            Physics2D.SyncTransforms();
            yield return Wait(0.2f);
            Assert.AreEqual(before, shots, "Leaving the configured range must stop new shots");
        }
        finally
        {
            Time.captureDeltaTime = originalCaptureDeltaTime;
            PlayerEvents.OnGunFired -= onFire;
        }
    }

    private IEnumerator TrackingPersistsWhileMovingAndPresentationMatchesTarget()
    {
        float originalCaptureDeltaTime = Time.captureDeltaTime;
        Time.captureDeltaTime = 1f / 60f;
        ArtifactDefinition rangeArtifact = null;
        PlayerStats stats = null;
        int shots = 0;
        System.Action<PlayerStats> onFire = player =>
        {
            if (player != stats) return;
            Assert.IsTrue(player.GetComponent<PlayerAutoTarget>().HasTarget,
                "Automatic bullets need a valid selected target");
            shots++;
        };
        PlayerEvents.OnGunFired += onFire;
        try
        {
            Pool();
            GameObject obj = Player();
            stats = obj.GetComponent<PlayerStats>();
            var movement = obj.GetComponent<PlayerMovement>();
            movement.tiltAngle = 0f;
            var aim = obj.GetComponent<PlayerAimController>();
            var targeting = obj.GetComponent<PlayerAutoTarget>();
            obj.AddComponent<PlayerTargetingView>();
            MonsterStats near = Monster(Vector2.right * 3f);
            MonsterStats far = Monster(Vector2.up * 6f);
            yield return null;
            Down(movement.joystick, Vector2.up * 0.5f);
            yield return Wait(0.4f);
            Assert.AreSame(near.transform, targeting.CurrentTarget);
            Assert.IsTrue(targeting.HasTarget);
            Assert.AreEqual(0, shots, "Tracking while moving must not enable attacks");
            Assert.Greater(Quaternion.Angle(aim.neckTransform.rotation, Quaternion.identity), 60f);
            Up(movement.joystick);
            yield return Wait(0.05f);
            Assert.AreEqual(0, shots, "Neutral input delay still applies");
            Assert.IsTrue(targeting.HasTarget);
            Assert.Less(Quaternion.Angle(aim.neckTransform.rotation, aim.FinalAimRotation), 6f,
                "Stopping must not return the neck to its vertical rest pose");
            yield return Wait(0.5f);
            Assert.Greater(shots, 0);
            var range = obj.transform.Find("Attack Range Outline").GetComponent<LineRenderer>();
            var marker = obj.transform.Find("Target Arrow").GetComponent<LineRenderer>();
            Assert.IsTrue(range.enabled);
            Assert.IsTrue(marker.enabled);
            Assert.That(Vector2.Distance(range.GetPosition(0), obj.transform.position), Is.EqualTo(targeting.CurrentRange).Within(0.001f));
            Assert.That(marker.GetPosition(0).x, Is.EqualTo(near.transform.position.x).Within(0.001f));
            Assert.Greater(marker.GetPosition(0).y, near.GetComponentInChildren<Collider2D>().bounds.max.y);

            var build = obj.AddComponent<RunBuild>();
            build.BeginRun(11);
            rangeArtifact = ScriptableObject.CreateInstance<ArtifactDefinition>();
            rangeArtifact.artifactId = "tracking_range";
            rangeArtifact.effects.Add(new ArtifactEffectDefinition { effectType = ArtifactEffectType.ProjectileRangePercent, value = 0.25f });
            Assert.IsTrue(build.TryAcquire(rangeArtifact));
            yield return Wait(0.05f);
            Assert.That(targeting.CurrentRange, Is.EqualTo(10f).Within(0.001f));
            Assert.That(Vector2.Distance(range.GetPosition(0), obj.transform.position), Is.EqualTo(10f).Within(0.001f));
            build.EndRun();
            targeting.searchInterval = 10f;
            yield return Wait(0.12f);
            near.OnDamage(float.MaxValue, null);
            yield return Wait(0.05f);
            Assert.AreSame(far.transform, targeting.CurrentTarget);
            Assert.That(marker.GetPosition(0).x, Is.EqualTo(far.transform.position.x).Within(0.001f));
            far.gameObject.SetActive(false);
            yield return Wait(0.05f);
            Assert.IsFalse(targeting.HasTarget);
            Assert.IsFalse(marker.enabled);
            Assert.IsTrue(range.enabled, "Range is visible even with no selected enemy");
            movement.acceptsLocalInput = false;
            yield return Wait(0.05f);
            Assert.IsFalse(range.enabled);
            movement.acceptsLocalInput = true;
            movement.controlMode = PlayerMovement.ControlMode.LegacyDualStick;
            yield return Wait(0.05f);
            Assert.IsFalse(range.enabled);
            Assert.IsFalse(marker.enabled);
        }
        finally
        {
            Time.captureDeltaTime = originalCaptureDeltaTime;
            PlayerEvents.OnGunFired -= onFire;
            if (rangeArtifact != null) Object.Destroy(rangeArtifact);
        }
    }

    private IEnumerator AutomaticAimTurnsSmoothlyAndFistWaitsForAlignment()
    {
        float originalCaptureDeltaTime = Time.captureDeltaTime;
        Time.captureDeltaTime = 1f / 60f;
        try
        {
            GameObject obj = Player(PlayerMovement.ControlMode.SingleStickFlick, true);
            var aim = obj.GetComponent<PlayerAimController>();
            var attack = obj.GetComponent<PlayerAttackController>();
            attack.enabled = false;
            obj.GetComponent<PlayerMovement>().tiltAngle = 0f;
            MonsterStats right = Monster(Vector2.right);
            MonsterStats left = Monster(Vector2.left * 2f);
            yield return null;
            Quaternion beforeSelection = aim.neckTransform.rotation;
            aim.SetAutoTarget(right.transform);
            Assert.Less(Quaternion.Angle(beforeSelection, aim.neckTransform.rotation), 0.01f,
                "Selecting a target must not directly rotate the visible neck");
            yield return null;
            float remaining = Quaternion.Angle(aim.neckTransform.rotation, aim.FinalAimRotation);
            Assert.That(remaining, Is.GreaterThan(1f).And.LessThan(89f));
            yield return Wait(0.5f);
            Assert.IsTrue(aim.IsAutoAimAligned);
            Quaternion before = aim.neckTransform.rotation;
            right.gameObject.SetActive(false);
            aim.SetAutoTarget(left.transform);
            Assert.Less(Quaternion.Angle(before, aim.neckTransform.rotation), 0.01f);
            yield return null;
            remaining = Quaternion.Angle(aim.neckTransform.rotation, aim.FinalAimRotation);
            Assert.That(remaining, Is.GreaterThan(1f).And.LessThan(179f));
            yield return Wait(0.6f);
            Assert.IsTrue(aim.IsAutoAimAligned);
            before = aim.neckTransform.rotation;
            left.gameObject.SetActive(false);
            aim.SetAutoTarget(null);
            Assert.Less(Quaternion.Angle(before, aim.neckTransform.rotation), 0.01f);
            yield return null;
            Assert.That(Quaternion.Angle(aim.neckTransform.rotation, Quaternion.identity), Is.GreaterThan(1f).And.LessThan(89f));
            yield return Wait(0.6f);
            Assert.Less(Quaternion.Angle(aim.neckTransform.rotation, Quaternion.identity), 0.1f);

            left.gameObject.SetActive(false);
            right.gameObject.SetActive(true);
            attack.SwapWeapon(PlayerAttackController.WeaponMode.Fist);
            attack.enabled = true;
            yield return null;
            Assert.IsFalse(attack.isAttacking, "An automatic punch must wait for the visible turn");
            float limit = Time.time + 1f;
            while (!attack.isAttacking && Time.time < limit) yield return null;
            Assert.IsTrue(attack.isAttacking);
            Assert.Less(Quaternion.Angle(aim.neckTransform.rotation, attack.LockedRotation), 1.01f);
            Assert.Less(Quaternion.Angle(attack.LockedRotation, Quaternion.Euler(0f, 0f, -90f)), 0.1f);
            right.gameObject.SetActive(false);
            yield return ObserveFullRetraction(attack, obj.GetComponent<NeckEffectController>(), obj.GetComponent<Animator>());
        }
        finally { Time.captureDeltaTime = originalCaptureDeltaTime; }
    }

    private IEnumerator AutomaticFistFinishesRetractionAfterTargetLoss()
    {
        float originalCaptureDeltaTime = Time.captureDeltaTime;
        Time.captureDeltaTime = 1f / 60f;
        GameObject obj = Player(PlayerMovement.ControlMode.SingleStickFlick, true);
        PlayerAttackController attack = obj.GetComponent<PlayerAttackController>();
        NeckEffectController effects = obj.GetComponent<NeckEffectController>();
        Animator animator = obj.GetComponent<Animator>();
        attack.SwapWeapon(PlayerAttackController.WeaponMode.Fist);
        int started = 0, ended = 0;
        System.Action<PlayerStats> onStart = player =>
        {
            if (player != obj.GetComponent<PlayerStats>()) return;
            Assert.AreEqual(ended, started, "A new punch must wait for the previous full retraction");
            Assert.That(effects.neckTransform.localScale.y, Is.EqualTo(1f).Within(0.001f));
            Assert.That(animator.GetFloat("animTime"), Is.EqualTo(0f).Within(0.001f));
            started++;
        };
        System.Action<PlayerStats> onEnd = player => { if (player == obj.GetComponent<PlayerStats>()) ended++; };
        PlayerEvents.OnPlayerAttackStarted += onStart;
        PlayerEvents.OnPlayerAttackEnded += onEnd;
        try
        {
            for (int loss = 0; loss < 3; loss++)
            {
                MonsterStats target = Monster(Vector2.up);
                float startLimit = Time.time + 0.5f;
                while (!attack.isAttacking && Time.time < startLimit) yield return null;
                Assert.IsTrue(attack.isAttacking);
                Assert.AreEqual(loss + 1, started);
                yield return Wait(0.06f);
                if (loss == 0) target.gameObject.SetActive(false);
                else if (loss == 1) target.currentHealth = 0f;
                else { target.transform.position = Vector3.up * 10f; Physics2D.SyncTransforms(); }
                yield return Wait(0.04f);
                Assert.IsTrue(attack.isAttacking, "Target loss must not snap an executing punch back to idle");
                yield return ObserveFullRetraction(attack, effects, animator);
                Assert.AreEqual(loss + 1, ended);
                Assert.IsFalse(effects.gloveTransform.gameObject.activeSelf);
                yield return Wait(0.15f);
                Assert.AreEqual(loss + 1, started, "No new punch without a valid target");
            }
        }
        finally
        {
            Time.captureDeltaTime = originalCaptureDeltaTime;
            PlayerEvents.OnPlayerAttackStarted -= onStart;
            PlayerEvents.OnPlayerAttackEnded -= onEnd;
        }
    }

    private IEnumerator ObserveFullRetraction(PlayerAttackController attack, NeckEffectController effects, Animator animator)
    {
        float previous = effects.neckTransform.localScale.y;
        bool retracted = false, reachedRestPose = false;
        float limit = Time.time + 3f;
        while (attack.isAttacking && Time.time < limit)
        {
            float current = effects.neckTransform.localScale.y;
            if (current < previous - 0.001f && current > 1.001f) retracted = true;
            if (retracted && effects.IsFistAnimating && Mathf.Abs(current - 1f) < 0.001f && animator.GetFloat("animTime") <= 0.001f)
                reachedRestPose = true;
            previous = current;
            yield return null;
        }
        Assert.IsFalse(attack.isAttacking, "Animation must eventually finish");
        Assert.IsTrue(retracted, "The visible inward motion must be played");
        Assert.IsTrue(reachedRestPose, "Retraction must reach the rest pose before attack completion");
    }

    private IEnumerator AutomaticFistFinishesRetractionAtLowFrameRate()
    {
        float originalCaptureDeltaTime = Time.captureDeltaTime;
        try
        {
            Time.captureDeltaTime = 0.2f; // Simulate five gameplay updates per second.
            GameObject obj = Player(PlayerMovement.ControlMode.SingleStickFlick, true);
            MonsterStats target = Monster(Vector2.up);
            PlayerAttackController attack = obj.GetComponent<PlayerAttackController>();
            attack.SwapWeapon(PlayerAttackController.WeaponMode.Fist);
            float limit = Time.time + 1f;
            while (!attack.isAttacking && Time.time < limit) yield return null;
            Assert.IsTrue(attack.isAttacking);
            yield return ObserveFullRetraction(attack, obj.GetComponent<NeckEffectController>(), obj.GetComponent<Animator>());
            target.gameObject.SetActive(false);
        }
        finally { Time.captureDeltaTime = originalCaptureDeltaTime; }
    }

    private IEnumerator ModeSwitchRestoresLegacyUIAndSupportsProportionalMovement()
    {
        GameObject obj = Player();
        PlayerMovement movement = obj.GetComponent<PlayerMovement>();
        FixedJoystick move = movement.joystick;
        FixedJoystick right = obj.GetComponent<PlayerAimController>().joystick;
        yield return null;
        Assert.IsFalse(right.gameObject.activeSelf);
        movement.proportionalSpeed = true;
        Down(move, Vector2.right * 0.5f);
        yield return NextPhysicsStep();
        yield return NextPhysicsStep();
        Assert.That(movement.AppliedVelocity.x, Is.EqualTo(1.5f).Within(0.02f));
        movement.controlMode = PlayerMovement.ControlMode.LegacyDualStick;
        yield return null;
        Assert.IsTrue(right.gameObject.activeSelf);
        Assert.AreSame(move, movement.joystick);
        Down(move, Vector2.right * 0.5f);
        yield return NextPhysicsStep();
        yield return NextPhysicsStep();
        Assert.That(movement.AppliedVelocity.x, Is.EqualTo(3f).Within(0.02f));
    }

    private IEnumerator SampleSceneKeepsExistingReferencesAndCanRestoreDualStick()
    {
        SceneManager.LoadScene("SampleScene");
        yield return Wait(0.05f);
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Assert.NotNull(player);
        // Sprint 02 gates combat input while the expedition preparation view is open.
        var expedition = Object.FindObjectOfType<ExpeditionManager>();
        if (expedition != null) Assert.IsTrue(expedition.StartExpedition(101));
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        PlayerAimController aim = player.GetComponent<PlayerAimController>();
        Assert.AreEqual(PlayerMovement.ControlMode.SingleStickFlick, movement.controlMode);
        Assert.NotNull(movement.joystick);
        Assert.NotNull(aim.joystick);
        Assert.IsFalse(aim.joystick.gameObject.activeSelf);
        Assert.AreSame(movement.joystick, player.GetComponent<JoystickFlickDetector>().joystick);
        Assert.NotNull(player.GetComponent<PlayerDodgeController>());
        Assert.NotNull(player.GetComponent<PlayerAutoTarget>());
        Assert.NotNull(player.GetComponentInChildren<Gun>().weaponSpawnPoint);
        var button = GameObject.Find("WeaponSwapButton").GetComponent<UnityEngine.UI.Button>();
        Assert.AreSame(player.GetComponent<PlayerAttackController>(), button.onClick.GetPersistentTarget(0));
        movement.controlMode = PlayerMovement.ControlMode.LegacyDualStick;
        yield return Wait(0.05f);
        Assert.IsTrue(aim.joystick.gameObject.activeSelf);
        button.onClick.Invoke();
        Assert.AreEqual(PlayerAttackController.WeaponMode.Fist, player.GetComponent<PlayerAttackController>().currentMode);
    }
}
