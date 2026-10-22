using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class Sprint02UIIntegrationTests
{
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yield return new EnterPlayMode();
        Assert.IsTrue(Application.isPlaying);
        Application.runInBackground = true;
        SceneManager.LoadScene("SampleScene");
        yield return UIFrames();
        yield return UIFrames();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        yield return new ExitPlayMode();
    }

    [UnityTest]
    public IEnumerator SceneButtonsPhysicalCombatRewardsResultsAndHubWorkTogether()
    {
        ExpeditionManager manager = Object.FindObjectOfType<ExpeditionManager>();
        Assert.IsNotNull(manager, "Run ExpeditionSetup.Install before integration tests.");
        ExpeditionUI view = manager.GetComponent<ExpeditionUI>();
        Assert.IsNotNull(view);
        Assert.IsNotNull(view.view, "The scene must reference an authored, editable UI view");
        Assert.IsNotNull(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/ExpeditionUI.prefab"));
        Assert.IsNotNull(GameObject.Find("UIRoot"));
        Assert.AreEqual(new Vector2(1080f, 1920f), view.view.GetComponent<CanvasScaler>().referenceResolution);
        Assert.That(MobileSafeArea.Normalize(new Rect(40f, 100f, 920f, 1800f), new Vector2(1000f, 2000f)).yMin, Is.EqualTo(0.05f).Within(0.0001f));
        Assert.That(MobileSafeArea.Normalize(new Rect(40f, 100f, 920f, 1800f), new Vector2(1000f, 2000f)).xMax, Is.EqualTo(0.96f).Within(0.0001f));
        int authoredObjects = view.view.GetComponentsInChildren<Transform>(true).Length;
        int authoredButtons = view.view.GetComponentsInChildren<Button>(true).Length;
        for (int i = 0; i < 10; i++) view.Refresh();
        Assert.AreEqual(authoredObjects, view.view.GetComponentsInChildren<Transform>(true).Length, "Refresh must reuse authored objects");
        Assert.AreEqual(authoredButtons, view.view.GetComponentsInChildren<Button>(true).Length);
        foreach (Button button in view.view.GetComponentsInChildren<Button>(true))
            Assert.GreaterOrEqual(button.GetComponent<LayoutElement>().minHeight, 144f, "Authored actions need a mobile-sized hit area");
        int startRequests = 0;
        view.view.StartRequested += () => startRequests++;
        for (int i = 0; i < 3; i++) { view.enabled = false; view.enabled = true; }
        yield return UIFrames();
        Assert.AreEqual(6, manager.definition.rounds.Count);
        Assert.AreEqual(12, manager.definition.artifactCatalog.artifacts.Count);
        Assert.IsTrue(manager.sceneEnemies.All(enemy => enemy != null && !enemy.activeSelf), "Original sandbox enemies must be preserved and disabled.");
        Assert.AreEqual(ExpeditionState.Preparing, manager.State);

        PlayerStats player = manager.players[0];
        player.maxHealth = 10000f; // Test-only safety while the real enemy AI attacks.
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        Assert.AreEqual(PlayerMovement.ControlMode.SingleStickFlick, movement.controlMode);
        Assert.IsFalse(movement.acceptsLocalInput, "Preparing must gate combat input.");
        Click(FindChoice(view, "탐험 시작"));
        Assert.AreEqual(ExpeditionState.Combat, manager.State);
        Assert.AreEqual(1, startRequests, "Re-enabling the controller/view must not duplicate button listeners");
        Assert.IsTrue(movement.acceptsLocalInput);
        Assert.IsNotNull(player.GetComponent<PlayerTargetingView>(), "The authored Player must have the world targeting view connected");
        yield return UIFrames();
        yield return UIFrames();
        PlayerAutoTarget targeting = player.GetComponent<PlayerAutoTarget>();
        float acquisitionLimit = Time.realtimeSinceStartup + 5f;
        while (!targeting.HasTarget && manager.State == ExpeditionState.Combat && Time.realtimeSinceStartup < acquisitionLimit) yield return UIFrames();
        yield return UIFrames(); // Let the world marker consume the newly acquired target.
        view.Refresh();
        Assert.IsTrue(targeting.HasTarget);
        Text targetingText = FindChild(view, "Targeting Status").GetComponent<Text>();
        StringAssert.Contains("조준 유지", targetingText.text);
        StringAssert.Contains(targeting.CurrentTarget.name.Replace("(Clone)", ""), targetingText.text);
        Assert.IsFalse(targetingText.raycastTarget);
        Assert.IsTrue(player.transform.Find("Target Arrow").GetComponent<LineRenderer>().enabled);
        Assert.IsFalse(view.view.modalLayer.blocksRaycasts);
        Assert.IsTrue(FindChild(view, "Combat HUD").GetComponentsInChildren<Graphic>().All(graphic => !graphic.raycastTarget), "Combat HUD must let joystick and world pointer input through.");
        Vector2 joystickScreenPoint = FindVisibleJoystickPoint(movement.joystick);
        Assert.AreSame(movement.joystick, TopRaycast(joystickScreenPoint).GetComponentInParent<Joystick>(),
            "The actual EventSystem top hit over the movement stick must reach that stick during combat.");

        Button weaponButton = GameObject.Find("WeaponSwapButton").GetComponent<Button>();
        var heldPointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, pointerId = 42 };
        ExecuteEvents.Execute(weaponButton.gameObject, heldPointer, ExecuteEvents.pointerDownHandler);
        var weaponAtPress = player.GetComponent<PlayerAttackController>().currentMode;
        // Do not damage or move the first wave from the test. Existing auto aim,
        // Gun, pooled Bullet, hurtbox and MonsterStats must physically clear it.
        float timeout = Time.realtimeSinceStartup + 12f;
        while (manager.State == ExpeditionState.Combat && Time.realtimeSinceStartup < timeout) yield return UIFrames();
        Assert.AreEqual(ExpeditionState.Reward, manager.State, "The authored first round must be playable with physical autoattack hits.");
        Assert.IsFalse(movement.acceptsLocalInput);
        Assert.IsTrue(view.view.modalLayer.blocksRaycasts);
        ExecuteEvents.Execute(weaponButton.gameObject, heldPointer, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(weaponButton.gameObject, heldPointer, ExecuteEvents.pointerClickHandler);
        Assert.AreEqual(weaponAtPress, player.GetComponent<PlayerAttackController>().currentMode,
            "A gameplay press released after a modal opens must not switch weapons");
        Assert.IsFalse(weaponButton.IsInteractable());
        GameObject rewardHit = TopRaycast(joystickScreenPoint);
        Transform decisionOverlay = FindChild(view, "Decision Overlay").transform;
        Assert.IsTrue(rewardHit.transform == decisionOverlay || rewardHit.transform.IsChildOf(decisionOverlay),
            "At the exact same screen point, the reward overlay must intercept input before the movement stick.");
        Assert.IsNull(rewardHit.GetComponentInParent<Joystick>());
        RunBuild build = player.GetComponent<RunBuild>();
        Assert.AreEqual(0, build.Entries.Count);
        ArtifactDefinition reward = manager.RewardCandidates[0];
        Canvas.ForceUpdateCanvases();
        ScrollRect rewardScroll = view.view.decisionScroll;
        Assert.That(rewardScroll.content.rect.width, Is.EqualTo(rewardScroll.viewport.rect.width).Within(0.1f), "The scroll content must fit the viewport without horizontal overflow");
        if (rewardScroll.content.rect.height > rewardScroll.viewport.rect.height)
        {
            Vector2 position = RectTransformUtility.WorldToScreenPoint(null, rewardScroll.viewport.TransformPoint(rewardScroll.viewport.rect.center));
            PointerEventData drag = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, position = position };
            GameObject dragHit = TopRaycast(position);
            float beforeDrag = rewardScroll.content.anchoredPosition.y;
            ExecuteEvents.ExecuteHierarchy(dragHit, drag, ExecuteEvents.initializePotentialDrag);
            ExecuteEvents.ExecuteHierarchy(dragHit, drag, ExecuteEvents.beginDragHandler);
            drag.position += Vector2.up * 120f;
            ExecuteEvents.ExecuteHierarchy(dragHit, drag, ExecuteEvents.dragHandler);
            ExecuteEvents.ExecuteHierarchy(dragHit, drag, ExecuteEvents.endDragHandler);
            Assert.Greater(rewardScroll.content.anchoredPosition.y, beforeDrag, "A native pointer drag must scroll overflowing cards");
            rewardScroll.StopMovement();
        }
        Button card = FindChoice(view, reward.displayName);
        Click(card);
        Click(card); // The old card is already inactive and its listeners removed.
        Assert.AreEqual(1, build.Entries.Sum(stack => stack.Count));
        Assert.AreEqual(1, manager.Session.Rewards.Count);
        Assert.AreEqual(ExpeditionState.RouteSelection, manager.State);
        Assert.AreEqual(0, manager.RewardCandidates.Count);
        yield return UIFrames(); // A new screen must complete its normal canvas layout before a new touch.

        while (manager.State != ExpeditionState.Completed)
        {
            if (manager.State == ExpeditionState.RouteSelection)
            {
                yield return UIFrames();
                ExpeditionRouteDefinition route = manager.AvailableRoutes.First(candidate => candidate.routeType == ExpeditionRouteType.NormalCombat);
                Click(FindChoice(view, route.displayName));
                Assert.IsFalse(view.view.modalLayer.blocksRaycasts);
            }
            Assert.IsTrue(manager.State == ExpeditionState.Combat || manager.State == ExpeditionState.BossCombat);
            yield return UIFrames(); // Simulate a playable combat frame before immediately killing this test wave.
            foreach (MonsterStats enemy in manager.spawner.SpawnedEnemies.ToArray()) enemy.OnDamage(100000f, null);
            Assert.AreEqual(ExpeditionState.Reward, manager.State);
            yield return UIFrames();
            if (manager.RewardCandidates.Count > 0) Click(FindChoice(view, manager.RewardCandidates[0].displayName));
            else Click(FindChoice(view, "계속"));
            yield return UIFrames();
        }
        Assert.AreEqual(6, manager.Session.Rewards.Count);
        Assert.IsTrue(view.view.modalLayer.blocksRaycasts);
        Assert.AreEqual(authoredObjects, view.view.GetComponentsInChildren<Transform>(true).Length, "A complete expedition must not accumulate decision objects");
        Assert.IsFalse(build.IsRunActive);
        Assert.AreEqual(0, build.Entries.Count);
        Assert.IsFalse(movement.acceptsLocalInput);

        Click(FindChoice(view, "다시 탐험"));
        Assert.AreEqual(ExpeditionState.Combat, manager.State);
        Assert.AreEqual(0, manager.RoundIndex);
        Assert.AreEqual(0, manager.Session.Rewards.Count);
        Assert.AreEqual(0, build.Entries.Count);
        Assert.AreEqual(player.EffectiveMaxHealth, player.currentHealth);
        player.OnDamage(100000f, null);
        Assert.AreEqual(ExpeditionState.Failed, manager.State);
        Assert.AreEqual(0, manager.AliveEnemies);
        Assert.IsTrue(view.view.modalLayer.blocksRaycasts);
        yield return UIFrames();
        Click(FindChoice(view, "마을로 귀환"));
        timeout = Time.realtimeSinceStartup + 8f;
        while (SceneManager.GetActiveScene().name != "MainMenu" && Time.realtimeSinceStartup < timeout) yield return UIFrames();
        Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name);
        Assert.IsNull(Object.FindObjectOfType<ExpeditionManager>());
        Assert.LessOrEqual(Object.FindObjectsOfType<PoolManager>().Length, 1, "Returning must not duplicate persistent pools.");
        yield return UIFrames();
        Click(GameObject.Find("Conquest!").GetComponent<Button>());
        timeout = Time.realtimeSinceStartup + 8f;
        while (SceneManager.GetActiveScene().name != "SampleScene" && Time.realtimeSinceStartup < timeout) yield return UIFrames();
        yield return UIFrames();
        Assert.AreEqual(ExpeditionState.Preparing, Object.FindObjectOfType<ExpeditionManager>().State, "The migrated main menu must open a fresh expedition scene");
        Debug.Log("SPRINT02_UI COMPLETE: physical autoattack, UI start/reward/routes/results/restart/failure/hub, input and raycast gating.");
    }

    private static IEnumerator UIFrames()
    {
        // EditMode's runner can advance several test steps in one player frame.
        // Wait for rendered player frames before simulating the next human touch.
        int until = Time.frameCount + 2;
        float timeout = Time.realtimeSinceStartup + 3f;
        while (Time.frameCount < until && Time.realtimeSinceStartup < timeout) yield return null;
        Assert.GreaterOrEqual(Time.frameCount, until, "The player loop must advance for UI input tests");
    }

    private static Button FindChoice(ExpeditionUI view, string title)
    {
        Button button = view.view.GetComponentsInChildren<Button>().FirstOrDefault(candidate => candidate.gameObject.activeInHierarchy &&
            candidate.GetComponentsInChildren<Text>().Any(label => label.text == title));
        Assert.IsNotNull(button, "Visible UI choice missing: " + title);
        return button;
    }

    private static GameObject FindChild(ExpeditionUI view, string name)
    {
        Transform child = view.view.GetComponentsInChildren<Transform>(true).FirstOrDefault(candidate => candidate.name == name);
        Assert.IsNotNull(child, "UI element missing: " + name);
        return child.gameObject;
    }

    private static void Click(Button button)
    {
        Assert.IsNotNull(EventSystem.current);
        if (!button.gameObject.activeInHierarchy)
        {
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            return;
        }
        Canvas.ForceUpdateCanvases();
        ScrollRect scroll = button.GetComponentInParent<ScrollRect>();
        RectTransform rect = button.GetComponent<RectTransform>();
        if (scroll != null)
        {
            Vector3 local = scroll.viewport.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
            float shift = scroll.viewport.rect.center.y - local.y;
            scroll.content.anchoredPosition += Vector2.up * shift;
            Canvas.ForceUpdateCanvases();
        }
        Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        GameObject hit = TopRaycast(point);
        Assert.AreSame(button, hit.GetComponentInParent<Button>(), $"Raycast at {point} hit {hit.name}; expected visible action {button.name}");
        PointerEventData pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, position = point, pointerId = 3 };
        ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerClickHandler);
    }

    private static GameObject TopRaycast(Vector2 screenPoint)
    {
        Canvas.ForceUpdateCanvases();
        List<RaycastResult> hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screenPoint }, hits);
        string diagnostic = "EventSystem found no UI at screen point " + screenPoint;
        if (hits.Count == 0)
            foreach (GraphicRaycaster raycaster in Object.FindObjectsOfType<GraphicRaycaster>(true))
            {
                Canvas canvas = raycaster.GetComponent<Canvas>();
                diagnostic += $"\n{raycaster.name}: active={raycaster.isActiveAndEnabled}, canvas={canvas.isActiveAndEnabled}, rect={canvas.pixelRect}, root={canvas.rootCanvas.name}";
                foreach (Graphic graphic in raycaster.GetComponentsInChildren<Graphic>())
                    if (graphic.raycastTarget) diagnostic += $"\n  {graphic.name}: cull={graphic.canvasRenderer.cull}, depth={graphic.depth}, rect={graphic.rectTransform.rect}";
            }
        Assert.IsNotEmpty(hits, diagnostic);
        return hits[0].gameObject;
    }

    private static Vector2 FindVisibleJoystickPoint(Joystick joystick)
    {
        Assert.IsNotNull(joystick);
        Canvas.ForceUpdateCanvases();
        Canvas canvas = joystick.GetComponentInParent<Canvas>();
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector3[] corners = new Vector3[4];
        ((RectTransform)joystick.transform).GetWorldCorners(corners);
        Vector2 minimum = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        Vector2 maximum = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
        Rect area = Screen.safeArea;
        minimum = Vector2.Max(minimum, new Vector2(Mathf.Max(0f, area.xMin) + 1f, Mathf.Max(0f, area.yMin) + 1f));
        maximum = Vector2.Min(maximum, new Vector2(Mathf.Min(Screen.width, area.xMax) - 1f, Mathf.Min(Screen.height, area.yMax) - 1f));
        Assert.IsTrue(maximum.x > minimum.x && maximum.y > minimum.y,
            "The movement stick must have visible area inside the current headless Game View and safe area.");

        // Prefer the visual centre. For a small headless Game View, also examine
        // visible interior points instead of aiming outside the screen or skipping.
        float[] fractions = { 0.5f, 0.2f, 0.8f };
        foreach (float x in fractions)
        foreach (float y in fractions)
        {
            Vector2 point = new Vector2(Mathf.Lerp(minimum.x, maximum.x, x), Mathf.Lerp(minimum.y, maximum.y, y));
            if (TopRaycast(point).GetComponentInParent<Joystick>() == joystick) return point;
        }
        Assert.Fail("No visible interior point of the movement stick receives the top EventSystem hit during combat.");
        return Vector2.zero;
    }
}
